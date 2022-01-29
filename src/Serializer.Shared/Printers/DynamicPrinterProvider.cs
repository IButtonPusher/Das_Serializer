﻿#if GENERATECODE

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer.CodeGen;
using Das.Serializer.Collections;
using Das.Serializer.Types;

namespace Das.Serializer.Printers
{
   public class DynamicPrinterProvider : IProxyProvider
   {
      static DynamicPrinterProvider()
      {
         _readWriteLock = new UpgradableReadWriteLock();
         _readWriteLock2 = new object();
         _jsonProxies2 = new Dictionary<long, Object>();

         var asmName = new AssemblyName("PRINT.Stuff");

         //var access = AssemblyBuilderAccess.RunAndSave;
         var access = AssemblyBuilderAccess.RunAndCollect;

        #if NET45 || NET40

         AssemblyBuilder asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, access);

         #else

         AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, access);

         #endif

         _moduleBuilder = asmBuilder.DefineDynamicModule(AssemblyName);
        
      }

      public void DumpProxies()
      {
          _jsonProxyBuilder2.DumpProxies();
      }

      public DynamicPrinterProvider(ITypeInferrer typeInferrer,
                                    INodeTypeProvider nodeTypes,
                                    ITypeManipulator typeManipulator,
                                    IInstantiator instantiator)
      {
         
         _jsonProxyBuilder2 = new DynamicJsonPrinterBuilder2(typeInferrer, nodeTypes,
            typeManipulator, _moduleBuilder, instantiator);
      }


      public ISerializerTypeProxy<TType> GetJsonProxy<TType>(ISerializerSettings settings)
      {
         var lookup = (typeof(TType).GetHashCode() + (Int64) settings.GetPrintScanSignature()) << 32;

         lock (_readWriteLock2)
         {
             if (!_jsonProxies2.TryGetValue(lookup, out var found))
             {
                 found = BuildJsonProxy<TType>(settings);
                 _jsonProxies2.Add(lookup, found);
             }

             return (ISerializerTypeProxy<TType>)found;
         }


         //_readWriteLock.EnterRead();
         //try
         //{
         //   if (_jsonProxies2.TryGetValue(lookup, out var found))
         //      return (ISerializerTypeProxy<TType>) found;

         //   _readWriteLock.Upgrade();

         //   try
         //   {
         //      found = BuildJsonProxy<TType>(settings);
         //      _jsonProxies2.Add(lookup, found);

         //      return (ISerializerTypeProxy<TType>) found;
         //   }
         //   finally
         //   {
         //      _readWriteLock.Downgrade();
         //   }
         //}
         //finally
         //{
         //   _readWriteLock.ExitRead();
         //}
      }

      private Object BuildJsonProxy<TType>(ISerializerSettings settings)
      {
         var type2 = _jsonProxyBuilder2.BuildProxyType(typeof(TType), settings);

         #if DEBUG

         //_jsonProxyBuilder2.DumpProxies();
         

         #endif

         return Activator.CreateInstance(type2, this, settings);
      }

      private const string AssemblyName = "PRINT.Stuff";
      //private static readonly String SaveFile = $"{AssemblyName}.dll";

      
      private static readonly Dictionary<Int64, Object> _jsonProxies2;
      private static readonly UpgradableReadWriteLock _readWriteLock;
      private static readonly Object _readWriteLock2;
      private readonly DynamicJsonPrinterBuilder2 _jsonProxyBuilder2;
      private static readonly ModuleBuilder _moduleBuilder;
   }
}

#endif
