using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer;

[SuppressMessage("ReSharper", "UseMethodIsInstanceOfType")]
public partial class ObjectConverter : SerializerCore,
                                       IObjectConverter
{
   public ObjectConverter(IStateProvider dynamicFacade,
                          ISerializerSettings settings)
      : base(dynamicFacade, settings)
   {
      _dynamicFacade = dynamicFacade;
      _nodeTypes = dynamicFacade.NodeTypeProvider;
      _instantiate = dynamicFacade.ObjectInstantiator;
      _types = dynamicFacade.TypeInferrer;
      _objects = dynamicFacade.ObjectManipulator;
   }


   public T ConvertEx<T>(Object obj,
                         ISerializerSettings settings)
   {
      if (obj is T already)
         return already;

      var outType = typeof(T);

      var outObj = _instantiate.BuildDefault(outType, settings.CacheTypeConstructors);
      var _currentNodeType = _nodeTypes.GetNodeType(outType);
        
      var refs = new Dictionary<Object, Object>();
      refs.Clear();

      outObj = Copy(obj, ref outObj, refs, settings, _currentNodeType);

      refs.Clear();
      return (T) outObj;
   }

   public T ConvertEx<T>(Object obj)
   {
      return ConvertEx<T>(obj, Settings);
   }

   public Object ConvertEx(Object obj,
                           Type newObjectType,
                           ISerializerSettings settings)
   {
      var newObject = _dynamicFacade.ObjectInstantiator.BuildDefault(newObjectType,
         settings.CacheTypeConstructors) ?? throw new NullReferenceException(newObjectType.Name);

      return ConvertEx(obj, newObject, settings);
   }

    

   public Object SpawnCollection(Object?[] objects,
                                 Type collectionType,
                                 ISerializerSettings settings,
                                 Type? collectionGenericArgs = null)
   {
      var itemType = collectionGenericArgs ?? TypeInferrer.GetGermaneType(collectionType);

      if (collectionType.IsArray)
      {
         //build via initializer if possible
         var arr2 = Array.CreateInstance(itemType, objects.Length);
         var i = 0;

         foreach (var child in objects)
         {
            arr2.SetValue(child, i++);
         }

         return arr2;
      }


      var ctor = collectionType.GetConstructor(new[] {itemType});

      if (ctor != null)
         return Activator.CreateInstance(collectionType, objects);


      var gargs = itemType.GetGenericArguments();

      var buildDictionary = gargs.Length == 2 &&
                            collectionType.IsAssignableFrom(collectionType) &&
                            TryGetCtor(out ctor);

      if (!buildDictionary)
         return BuildCollectionDynamically(collectionType, objects, settings);

      var regularDic = typeof(Dictionary<,>).MakeGenericType(gargs);
      var dicObj = BuildCollectionDynamically(regularDic, objects, settings);
      return Activator.CreateInstance(collectionType, dicObj);

      Boolean TryGetCtor(out ConstructorInfo c)
      {
         var otherDic = typeof(IDictionary<,>).MakeGenericType(gargs);
         c = collectionType.GetConstructor(new[] {otherDic})!;
         return ctor != null;
      }
   }

   private Object BuildCollectionDynamically(Type collectionType,
                                             Object?[] objects,
                                             ISerializerSettings settings)
   {
      var val = _instantiate.BuildDefault(collectionType,
         settings.CacheTypeConstructors) ?? throw new MissingMethodException(
         collectionType.Name);

      if (objects.Length == 0)
         return val;

      switch (val)
      {
         case IList ilist:
            foreach (var o in objects)
            {
               ilist.Add(o);
            }

            return val;

         case IEnumerable ienum:
            var addDelegate = _dynamicFacade.TypeManipulator.GetAdder(ienum);

            foreach (var child in objects)
            {
               addDelegate(val, child);
            }

            return val;

         default:
            throw new InvalidOperationException(collectionType.Name);
      }
   }


   // ReSharper disable once UnusedParameter.Local
   private T ConvertEx<T>(Object obj,
                          T newObject,
                          ISerializerSettings settings)
   {
      return ConvertEx<T>(obj, settings);
   }

      

        

       

   private Object CopyLists(Object from,
                            Object to,
                            Type toType,
                            Dictionary<object, object> references,
                            ISerializerSettings settings)
   {
      var toListType = TypeInferrer.GetGermaneType(toType);
      var fromList = from as IEnumerable;
      var tempTo = new List<Object?>();
      var _currentNodeType = _nodeTypes.GetNodeType(toListType);

      if (fromList == null)
         return to;

      foreach (var fromItem in fromList)
      {
         if (fromItem == null)
         {
            tempTo.Add(null);
            continue;
         }

         //var itemNodeType = _nodeTypes.GetNodeType(toListType);
         var toItem = IsInstantiable(toListType)
            ? _instantiate.BuildDefault(toListType, settings.CacheTypeConstructors)
            : _instantiate.BuildDefault(fromItem.GetType(), settings.CacheTypeConstructors);

         if (_currentNodeType == NodeTypes.Dynamic)
         {
            _currentNodeType = _nodeTypes.GetNodeType(fromItem.GetType());
         }

         toItem = Copy(fromItem, ref toItem, references, settings, _currentNodeType);
         if (toItem != null)
            tempTo.Add(toItem);
      }

      to = SpawnCollection(tempTo.ToArray(), toType, settings, toListType);

      return to;
   }

   private Object?[]? GetPropertiesToConstructorArgs(Object from,
                                                     Type toType,
                                                     Dictionary<object, object> references,
                                                     ISerializerSettings settings,
                                                     out ConstructorInfo cInfo)
   {
      Boolean hadPropCtor;

      if (!_instantiate.TryGetPropertiesConstructor(toType, out cInfo))
      {
         hadPropCtor = false;
         if (toType.GetConstructors() is { } ctors && ctors.Length > 0)
            cInfo = ctors[0];
         else
            return default;
      }
      else hadPropCtor = true;

      var tstruct = _dynamicFacade.TypeManipulator.GetTypeStructure(toType);

      var ctorParams = cInfo.GetParameters();
      var args = new Object?[ctorParams.Length];
      var p = 0;
      for (; p < ctorParams.Length; p++)
      {
         if (tstruct.TryGetValueForParameter(from, ctorParams[p],
                settings.SerializationDepth, out var val, 
                out var isMemberSerializable))
         {
            var vcopy = val != null && isMemberSerializable ? Copy(val, settings) : val;
            args[p] = vcopy;
         }
         else break;
      }

      if (p == ctorParams.Length)
         return args;

      if (!hadPropCtor)
         return default;

            
      var props = new Dictionary<String, Object>();

      foreach (var prop in _types.GetPublicProperties(toType))
      {
         if (prop.GetIndexParameters().Length > 0)
            continue;

         if (!_objects.TryGetPropertyValue(from, prop, out var fromProp))
            continue;

         var nextNodeType = _nodeTypes.GetNodeType(prop.PropertyType);

         var toPropType = IsInstantiable(prop.PropertyType)
            ? prop.PropertyType
            : prop.GetValue(from, null)?.GetType() ?? prop.PropertyType;

         var toProp = _instantiate.BuildDefault(toPropType,
            settings.CacheTypeConstructors) ?? throw new NullReferenceException(prop.Name);
         toProp = Copy(fromProp, ref toProp, references, settings,
            nextNodeType);

         props.Add(prop.Name, toProp);
      }

      var values = new List<Object?>();


      foreach (var conParam in cInfo.GetParameters())
      {
         if (String.IsNullOrEmpty(conParam.Name))
         {
            if (toType.Assembly.IsDynamic)
            {
               var useProp = props.Values.FirstOrDefault(v =>
                  conParam.ParameterType.IsAssignableFrom(v.GetType()));
               if (useProp != null)
                  values.Add(useProp);
               else
               {
                  var useVal = _dynamicFacade.TypeManipulator.TryGetFieldValueOfType(from,
                     conParam.ParameterType);
                  //if (useVal == null)
                  //    return default;
                  values.Add(useVal);
               }
            }

            continue;
         }

         var search = _dynamicFacade.TypeInferrer.ToPascalCase(conParam.Name);

         if (props.TryGetValue(search, out var found))
            values.Add(found);
      }

      return values.ToArray();

   }

        

   private T FromType<T>(T example,
                         ISerializerSettings settings) where T : class
   {
      if (!IsInstantiable(typeof(T)))
      {
         var etype = example.GetType();
         var enodetype = _nodeTypes.GetNodeType(etype);

         if (enodetype == NodeTypes.PropertiesToConstructor)
         {

         }

         return (ObjectInstantiator.BuildDefault(etype, settings.CacheTypeConstructors) as T)!;
      }

      return ObjectInstantiator.BuildDefault<T>(settings.CacheTypeConstructors);
   }

   //private static readonly ThreadLocal<Dictionary<Object, Object>> References
   //    = new(() => new Dictionary<Object, Object>());

   //[ThreadStatic]
   //private static NodeTypes _currentNodeType;

   private readonly IStateProvider _dynamicFacade;
   private readonly IInstantiator _instantiate;
   private readonly INodeTypeProvider _nodeTypes;
   private readonly IObjectManipulator _objects;
   private readonly ITypeInferrer _types;
}