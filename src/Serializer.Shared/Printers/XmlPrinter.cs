using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Das.Serializer;

#pragma warning disable 8604
#pragma warning disable 8602

namespace Das.Printers;

public class XmlPrinter : TextPrinter
{
   public XmlPrinter(ITypeInferrer typeInferrer,
                     INodeTypeProvider nodeTypes,
                     IObjectManipulator objectManipulator,
                     ITypeManipulator typeManipulator)
      : base(typeInferrer,
         nodeTypes, objectManipulator, '/', typeManipulator)
   {
      _formatStack = new Stack<StackFormat>();
      PathAttribute = Const.RefTag;
      _formatStack.Push(new StackFormat(-1, false));
   }

        

   public override void PrintNamedObject(String nodeName,
                                         Type? propType,
                                         Object? nodeValue,
                                         NodeTypes nodeType,
                                         ITextRemunerable Writer,
                                         ISerializerSettings settings,
                                         ICircularReferenceHandler circularReferenceHandler)
   {
      if (nodeValue == null)
         return;

      circularReferenceHandler.AddPathReference("/", nodeName);
            

      try
      {
         var valType = nodeValue.GetType();
                
         var isWrapping = IsWrapNeeded(propType, valType, nodeType, settings);

         var parent = _formatStack.Pop();

         /////////////////////////
         //close parent tag or print as attribute
         /////////////////////////

         if (parent.IsTagOpen) //try to print like inline attributes zb <tag val="5" ...
         {
            if (nodeType == NodeTypes.Primitive && !isWrapping &&
                settings.IsUseAttributesInXml)
            {
               //does a leaf need to go in the stack?
               _formatStack.Push(parent);
               propType = valType;

               PrintLeafAttribute(nodeValue, nodeName, propType,
                  Writer, settings, circularReferenceHandler);


               return;
            }
            else
            {
               Writer.Append(CloseTag);
               Writer.PrintCurrentTabs();
               parent.IsTagOpen = false;
            }
         }

         /////////////////////////

         _formatStack.Push(parent);
         var current = new StackFormat(parent.Tabs + 1, true);

         /////////////////////////
         // open node's tag
         /////////////////////////

         if (circularReferenceHandler.CanPrintObject(nodeValue))
            //don't open a tag unless we need it
         {
            Writer.IndentRepeatedly(current.Tabs);

            Writer.Append(OpenAttributes, nodeName);
            if (current.Tabs == 0)
               Writer.Append(Const.XmlXsiNamespace);
         }
         else
            return; //we're ignoring circular refs and this was a circular ref...

         if (isWrapping)
         {
            var amAnonymous = TypeCore.IsAnonymousType(valType);

            if (!amAnonymous)
            {
               //embed type info
               var typeName = _typeInferrer.ToClearName(valType);

               typeName = SecurityElement.Escape(typeName);

               Writer.Append(" ", Const.XmlType);

               Writer.Append(Const.Equal, Const.StrQuote);
               // ReSharper disable once AssignNullToNotNullAttribute
               Writer.Append(typeName, Const.StrQuote);
            }

            if (nodeType == NodeTypes.Primitive || nodeType == NodeTypes.Fallback)
            {
               current.IsTagOpen = false;
               Writer.Append(CloseAttributes);
            }
         }
         else if (_typeInferrer.IsLeaf(propType, true)
                  || nodeType == NodeTypes.Fallback && current.IsTagOpen
                  || _typeInferrer.TryGetNullableType(propType, out _))
         {
            //if we got here with a leaf then the regular logic of trying to make it an attribute 
            //doesn't apply => self close
            Writer.Append(CloseAttributes);
            current.IsTagOpen = false;
         }
         /////////////////////////


         /////////////////////////
         // print node contents
         /////////////////////////
         _formatStack.Push(current);

         propType = valType;

         var couldPrint = PrintObject(nodeValue, propType, nodeType, Writer,
            settings, circularReferenceHandler);

         _formatStack.Pop();
         /////////////////////////


         /////////////////////////
         // close tag
         /////////////////////////

         if (current.IsTagOpen)
         {
            if (couldPrint)
            {
               Writer.Append(SelfClose);
               Writer.PrintCurrentTabs();
            }
         }
         else
         {
            switch (nodeType)
            {
               case NodeTypes.Primitive:
               case NodeTypes.Fallback:
                  break;
               case NodeTypes.Collection:
                  var isEmpty = nodeValue is ICollection {Count: 0};
                  if (isEmpty)
                     return;
                  break;
               default:
                  Writer.IndentRepeatedly(current.Tabs);
                            
                  break;
            }

            Writer.Append($"</{nodeName}>\r\n");
         }

         /////////////////////////
      }
      finally
      {
         circularReferenceHandler.PopPathReference();
      }
   }

   protected override void PrintChar(ITextRemunerable writer,
                                     Char c)
   {
      var parent = _formatStack.Pop();

      if (!IsPrintingLeaf && parent.IsTagOpen)
      {
         writer.Append(CloseAttributes);
         parent.IsTagOpen = false;
      }

      writer.Append(c);

      _formatStack.Push(parent);
   }


   protected override void PrintCollection(Object? value,
                                           Type valType,
                                           ITextRemunerable writer,
                                           ISerializerSettings settings,
                                           ICircularReferenceHandler circularReferenceHandler)
   {
      if (ReferenceEquals(value, null))
         return;

      var nodeType = value.GetType();

      var isEmpty = value is ICollection { Count: 0 };

      var parent = _formatStack.Peek();

      if (parent.IsTagOpen)
      {
         writer.Append(isEmpty ? SelfClose : CloseTag);
         parent.IsTagOpen = false;
      }

      if (isEmpty)
         return;

      var germane = _typeInferrer.GetGermaneType(nodeType);

      if (!_typeInferrer.IsInstantiable(germane) &&
          value is IList { Count: > 0 } ilist)
      {
         germane = ilist[0].GetType();
      }


      var germaneNodeType = _nodeTypes.GetNodeType(germane);

      writer.TabOut();

      PrintSeries(ExplodeIterator(value as IEnumerable, germane),
         writer, PrintCollectionObject, germaneNodeType, settings,
         circularReferenceHandler);

      writer.TabIn();
   }

   protected sealed override void PrintCollectionObject(Object? o,
                                                        Type propType,
                                                        Int32 index,
                                                        ITextRemunerable Writer,
                                                        NodeTypes germaneNodeType,
                                                        ISerializerSettings settings,
                                                        ICircularReferenceHandler circularReferenceHandler)
   {
      //circularReferenceHandler.AddPathReference($"[{index}]");
      circularReferenceHandler.AddPathReference("[", index, "]");

      Writer.TabOut();

      PrintNamedObject(_typeInferrer.ToClearName(propType,
            TypeNameOption.OmitGenericArguments),
         propType, o, germaneNodeType, Writer, settings, circularReferenceHandler);

      Writer.TabIn();

      circularReferenceHandler.PopPathReference();
   }

   protected sealed override bool ShouldPrintValue(Object obj,
                                                   NodeTypes nodeType,
                                                   IPropertyAccessor prop,
                                                   ISerializerSettings settings,
                                                   out Object? value)
   {
      if (!prop.TryGetAttribute<XmlIgnoreAttribute>(out _) &&
          !prop.TryGetAttribute<IgnoreDataMemberAttribute>(out _))
      {
         value = prop.GetPropertyValue(obj);
         return !settings.IsOmitDefaultValues ||
                !_typeInferrer.IsDefaultValue(value);
      }

      value = default;
      return false;
   }

   protected override void PrintInteger(Object val,
                                        ITextRemunerable Writer)
   {
      PrintStringWithoutEscaping(val.ToString(), Writer);
   }

   protected override void PrintReal(String str, 
                                     ITextRemunerable Writer)
   {
      PrintStringWithoutEscaping(str, Writer);
   }

   protected sealed override void PrintString(String str,
                                              ITextRemunerable Writer)
   {
      var parent = _formatStack.Pop();

      if (!IsPrintingLeaf && parent.IsTagOpen)
      {
         Writer.Append(CloseAttributes);
         parent.IsTagOpen = false;
      }

      if (SecurityElement.Escape(str) is {} escaped)
         Writer.Append(escaped);

      _formatStack.Push(parent);
   }

   protected override void PrintString(String input,
                                       Boolean isInQuotes,
                                       ITextRemunerable Writer)
   {
      var parent = _formatStack.Pop();

      if (!IsPrintingLeaf && parent.IsTagOpen)
      {
         Writer.Append(CloseAttributes);
         parent.IsTagOpen = false;
      }

      if (SecurityElement.Escape(input) is { } escaped)
         Writer.Append(escaped);

      _formatStack.Push(parent);
   }

   protected sealed override void PrintStringWithoutEscaping(String str,
                                                             ITextRemunerable Writer)
   {
      var parent = _formatStack.Pop();

      if (!IsPrintingLeaf && parent.IsTagOpen)
      {
         Writer.Append(CloseAttributes);
         parent.IsTagOpen = false;
      }

      Writer.Append(str);

      _formatStack.Push(parent);
   }

   private void PrintLeafAttribute(Object? val,
                                   String nodeName,
                                   Type propType,
                                   ITextRemunerable Writer,
                                   ISerializerSettings settings,
                                   ICircularReferenceHandler circularReferenceHandler)
   {
      IsPrintingLeaf = true;
      Writer.Append($" {nodeName}={Const.Quote}");
      var res = _nodeTypes.GetNodeType(propType);
            
      PrintObject(val, propType, res, Writer, settings, circularReferenceHandler);
      Writer.Append(Const.Quote);
   }

   private const String SelfClose = " />\r\n";
   private const String CloseTag = ">\r\n";
   private const Char CloseAttributes = '>';
   private const Char OpenAttributes = '<';
   protected readonly Stack<StackFormat> _formatStack;

   protected Boolean IsPrintingLeaf;
}