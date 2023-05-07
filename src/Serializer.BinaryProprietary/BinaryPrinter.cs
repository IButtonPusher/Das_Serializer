using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer;
using Das.Serializer.Remunerators;
using Microsoft.CodeAnalysis.Operations;
using Reflection.Common;
using Serializer.BinaryProprietary;

namespace Das.Printers;

public class BinaryPrinter<TWriter> : PrinterBase<Byte[], Byte, TWriter>,
                                      IDisposable,
                                      IBinaryPrimitivePrinter
   where TWriter : IBinaryWriter<TWriter>
{
   public BinaryPrinter(ITypeInferrer typeInferrer,
                        INodeTypeProvider nodeTypes,
                        IObjectManipulator objectManipulator,
                        ITypeManipulator typeManipulator)
      : base(typeInferrer, nodeTypes, objectManipulator,
         true, '.', typeManipulator)
   {
      //_fallbackFormatter = new BinaryFormatter();

      IsTextPrinter = false;
   }

   public void Dispose()
   {
   }


   #if !PARTIALTRUST


   public static unsafe Byte[] GetBytes(String str)
   {
      var len = str.Length * 2;
      var bytes = new Byte[len];
      fixed (void* ptr = str)
      {
         Marshal.Copy(new IntPtr(ptr), bytes, 0, len);
      }

      return bytes;
   }

   #else
        public static Byte[] GetBytes(String str)
        {
        return System.Text.Encoding.UTF8.GetBytes(str);
        }

   #endif

   public override void PrintNamedObject(String name,
                                         Type? propType,
                                         Object? val,
                                         NodeTypes valueNodeType,
                                         TWriter writer,
                                         ISerializerSettings settings,
                                         ICircularReferenceHandler circularReferenceHandler)
   {
      var valType = val?.GetType() ?? propType;
      circularReferenceHandler.AddPathReference(name);

      try
      {
         var isWrapping = TryWrap(propType!, ref val, ref valType!, settings);
         var isLeaf = _typeInferrer.IsLeaf(valType, true);

         switch (isWrapping)
         {
            case false when !isLeaf:
               propType = valType;
               break;

            case false:
               break;

            default:
               propType = valType;
               break;
         }

         var nodeType = _nodeTypes.GetNodeType(propType);

         PrintBinaryNode(val, propType!, nodeType, ref writer, isWrapping,
            !isLeaf || isWrapping, settings, circularReferenceHandler);
      }
      finally
      {
         circularReferenceHandler.PopPathReference();
      }
   }

   [MethodImpl(256)]
   protected Boolean Print(Object? o,
                           TypeCode code,
                           IBinaryWriter _bWriter)
   {
      Byte[] bytes;

      switch (code)
      {
         case TypeCode.String:
            WriteString(o?.ToString(), _bWriter);
            return true;
         case TypeCode.Boolean:
            bytes = BitConverter.GetBytes((Boolean)o!);
            break;
         case TypeCode.Char:
            bytes = BitConverter.GetBytes((Char)o!);
            break;
         case TypeCode.SByte:
            _bWriter.WriteInt8((SByte)o!);
            return true;
         case TypeCode.Byte:
            _bWriter.WriteInt8((Byte)o!);
            return true;
         case TypeCode.Int16:
            _bWriter.WriteInt16((Int16)o!);
            return true;
         case TypeCode.UInt16:
            _bWriter.WriteInt16((UInt16)o!);
            return true;
         case TypeCode.Int32:
            _bWriter.WriteInt32((Int32)o!);
            return true;
         case TypeCode.UInt32:
            _bWriter.WriteInt32((UInt32)o!);
            return true;
         case TypeCode.Int64:
            _bWriter.WriteInt64((Int64)o!);
            return true;
         case TypeCode.UInt64:
            _bWriter.WriteInt64((UInt64)o!);
            return true;
         case TypeCode.Single:
            bytes = BitConverter.GetBytes((Single)o!);
            break;
         case TypeCode.Double:
            bytes = BitConverter.GetBytes((Double)o!);
            break;
         case TypeCode.Decimal:
            bytes = ((Decimal)o!).ToByteArray();
            break;
         case TypeCode.DateTime:
            _bWriter.WriteInt64(((DateTime)o!).Ticks);
            return true;
         default:
            return false;
      }

      _bWriter.Write(bytes);
      return true;
   }


   protected Boolean PrintBinaryNode(Object? nodeValue,
                                     Type propType,
                                     NodeTypes nodeType,
                                     ref TWriter _bWriter,
                                     Boolean isWrapping,
                                     Boolean isPush,
                                     ISerializerSettings settings,
                                     ICircularReferenceHandler circularReferenceHandler)
   {
      if (isPush)
      {
         Push(nodeValue, nodeType, ref _bWriter, isWrapping);
         PrintObject(nodeValue, propType, nodeType, _bWriter, settings, circularReferenceHandler);

         _bWriter = _bWriter.Pop();
      }
      else
         PrintObject(nodeValue, propType, nodeType, _bWriter, settings, circularReferenceHandler);

      return nodeValue != null;
   }


   public override void PrintCircularDependency(Int32 index,
                                                TWriter _bWriter,
                                                ISerializerSettings settings,
                                                IEnumerable<String> pathStack,
                                                ICircularReferenceHandler circularReferenceHandler)
   {
      if (index > 255)
         throw new FieldAccessException();

      _bWriter.WriteInt8((SByte)index);
   }


   protected override void PrintCollection(Object? value,
                                           Type valType,
                                           TWriter _bWriter,
                                           ISerializerSettings settings,
                                           ICircularReferenceHandler circularReferenceHandler)
   {
      var germane = _typeInferrer.GetGermaneType(valType);

      switch (value)
      {
         case null:
            _bWriter.WriteInt8(0);
            break;
         case IEnumerable list:
            //var boom = ExplodeList(list, germane);
            var boom = ExplodeIterator(list, germane);

            var isLeaf = _typeInferrer.IsLeaf(germane, true);

            if (isLeaf)
               PrintSeries(boom, _bWriter,
                  PrintPrimitiveItem, NodeTypes.Primitive, settings,
                  circularReferenceHandler);
            else
            {
               var germaneNodeType = _nodeTypes.GetNodeType(germane);
               PrintSeries(boom, _bWriter, PrintCollectionObject, germaneNodeType,
                  settings, circularReferenceHandler);
            }

            break;
         default:
            throw new InvalidOperationException(
               $"{value} is not valid to be printed as a collection");
      }
   }


   protected override void PrintFallback(Object? o,
                                         TWriter bWriter,
                                         Type useType)
   {
      if (o != null && _typeInferrer.IsUseless(useType))
         useType = o.GetType();

      if (o == null)
         //specify that the actual object is 0 bytes
         bWriter.WriteInt32(0);
      else if (_typeInferrer.IsLeaf(useType, true))
         PrintPrimitive(o, bWriter, useType);
      else
      {
         switch (o)
         {
            case TimeSpan ts:
               PrintPrimitive(ts.Milliseconds, bWriter, typeof(Int32));
               break;

            default:
               var ggetValue = GetType()
                               .GetMethodOrDie(nameof(PrintFallbackValue))
                               .MakeGenericMethod(useType);

               ggetValue.Invoke(this, new [] { o, bWriter });
               break;

               //throw new InvalidOperationException($"Unable to serialize to binary type {useType}");
         }
      }
   }

   public void PrintFallbackValue<T>(T value,
                                TWriter writer)
   {
      var printFunc = BinarySurrogate<T>.PrintFunc;
      if (printFunc == null)
         throw new InvalidOperationException($"Unable to deserialize from binary type {typeof(T)}");

      printFunc(value, this, writer);
   }

   public void PrintPrimitive<T>(T o,
                                 IBinaryWriter bWriter)
   {
      PrintPrimitiveImpl(o, bWriter, typeof(T));
   }

   protected override void PrintPrimitive(Object? o,
                                          TWriter bWriter,
                                          Type type)
   {
    PrintPrimitiveImpl(o, bWriter, type);
   }

   private void PrintPrimitiveImpl<TWrite>(Object? o,
                                          TWrite bWriter,
                                          Type type)
   where TWrite : IBinaryWriter
   {
      while (true)
      {
         var code = Type.GetTypeCode(type);

         if (Print(o, code, bWriter))
            return;

         if (!_typeInferrer.TryGetNullableType(type, out var primType))
            throw new NotSupportedException($"Type {type} cannot be printed as a primitive");

         if (o == null)
            bWriter.WriteInt8(0);
         else
         {
            //flag that there is a value
            bWriter.WriteInt8(1);
            type = primType;
            continue;
         }

         return;
      }
   }

   protected sealed override bool ShouldPrintValue(Object obj,
                                                   NodeTypes nodeType,
                                                   IPropertyAccessor prop,
                                                   ISerializerSettings settings,
                                                   out Object? value)
   {
      if (!prop.CanWrite && nodeType != NodeTypes.PropertiesToConstructor)
      {
         value = default;
         return false;
      }

      value = prop.GetPropertyValue(obj);
      return true;
   }


   protected virtual void WriteString(String? str,
                                      IBinaryWriter _bWriter)
   {
      if (str == null)
      {
         //null string indicate such with -1 length
         _bWriter.WriteInt32(-1);
         return;
      }

      var bytes = GetBytes(str);

      var len = bytes.Length;
      _bWriter.WriteInt32(len);
      _bWriter.Append(bytes);
   }

   private void PrintPrimitiveItem(Object? o,
                                   Type propType,
                                   Int32 index,
                                   TWriter _bWriter,
                                   NodeTypes nodeType,
                                   ISerializerSettings settings,
                                   ICircularReferenceHandler circularReferenceHandler)
   {
      PrintPrimitive(o, _bWriter, propType);
   }

   private void Push(Object? nodeValue,
                     NodeTypes nodeType,
                     ref TWriter _bWriter,
                     Boolean isWrapping)
   {
      _bWriter = _bWriter.Push(nodeType, isWrapping);

      if (isWrapping)
         WriteType(nodeValue!.GetType(), _bWriter);
   }

   private Boolean TryWrap(Type propType,
                           ref Object? val,
                           ref Type valType,
                           ISerializerSettings settings)
   {
      var nodeType = _nodeTypes.GetNodeType(valType);
      var isWrapping = val != null && IsWrapNeeded(propType, valType, nodeType, settings);

      if (!isWrapping)
         return false;

      if (!_typeInferrer.TryGetNullableType(propType, out _))
         return true;

      val = Activator.CreateInstance(propType, val);

      valType = propType;

      return false;
   }

   private void WriteType(Type type,
                          IBinaryWriter _bWriter)
   {
      var typeName = _typeInferrer.ToClearName(type);

      WriteString(typeName, _bWriter);
   }

   //private readonly BinaryFormatter _fallbackFormatter;
   
}
