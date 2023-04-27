using System;
using System.IO;
using System.Threading.Tasks;
using Das.Printers;
using Das.Serializer.CircularReferences;
using Das.Serializer.Remunerators;

// ReSharper disable UnusedMember.Global

namespace Das.Serializer;

public partial class DasCoreSerializer
{
   public String ToXml(Object o)
   {
      var oType = o.GetType();
      return ObjectToTypedXml(o, oType, Settings);
   }

   public String ToXml<TObject>(TObject o)
   {
      var oType = typeof(TObject);
      return ObjectToTypedXml(o!, oType, Settings);
   }

   public String ToXml<TTarget>(Object o)
   {
      var obj = ObjectManipulator.CastDynamic<TTarget>(o);
      return ObjectToTypedXml(obj!, typeof(TTarget), Settings);
   }

   public async Task ToXmlAsync(Object o,
                                FileInfo fi)
   {
      var xml = ToXml(o);
      await WriteTextToFileInfoAsync(xml, fi);
   }

   public async Task ToXmlAsync<TTarget>(Object o,
                                         FileInfo fi)
   {
      var xml = ToXml<TTarget>(o);
      await WriteTextToFileInfoAsync(xml, fi);
   }

   public async Task ToXmlAsync<TObject>(TObject o,
                                         FileInfo fileName)
   {
      String xml;
      if (!ReferenceEquals(null, o))
      {
         var ot = o.GetType();
         xml = ObjectToTypedXml(o, ot, Settings);
      }
      else
      {
         xml = ObjectToTypedXml(o!, typeof(TObject), Settings);
      }

      await WriteTextToFileInfoAsync(xml, fileName);
   }

   private String ObjectToTypedXml(Object? o,
                                   Type asType,
                                   ISerializerSettings settings)
   {
      using (var writer = GetTextWriter(settings))
      {
         var amAnonymous = IsAnonymousType(asType);

         if (amAnonymous)
         {
                    
            settings = StateProvider.ObjectConverter.Copy(settings, settings);

            settings.TypeSpecificity = TypeSpecificity.All;
            settings.CacheTypeConstructors = false;
         }

         var printer = new XmlPrinter(StateProvider.TypeInferrer, 
            StateProvider.NodeTypeProvider,
            StateProvider.ObjectManipulator, StateProvider.TypeManipulator);

         var rootText = TypeInferrer.ToClearName(asType, TypeNameOption.OmitGenericArguments);
                

         printer.PrintNamedObject(rootText, asType, o, 
            StateProvider.NodeTypeProvider.GetNodeType(asType),
            writer, settings,
            GetCircularReferenceHandler(settings));

         return writer.ToString();

      }
   }

   protected StringBuilderWrapper GetTextWriter(ISerializerSettings settings)
   {
      return settings.IsFormatSerializedText
         ? new FormattingStringBuilderWrapper(settings)
         : new CompactStringBuilderWrapper();
   }

   protected static ICircularReferenceHandler GetCircularReferenceHandler(ISerializerSettings s)
   {
      switch (s.CircularReferenceBehavior)
      {
         case CircularReference.IgnoreObject:
            _ignoringCircularReferenceHandler ??= new IgnoringCircularReferenceHandler();
            _ignoringCircularReferenceHandler.Clear();
            return _ignoringCircularReferenceHandler;

         case CircularReference.SerializePath:
            _pathSerializingCircularReferenceHandler ??= 
               new PathSerializingCircularReferenceHandler();
            _pathSerializingCircularReferenceHandler.Clear();
            return _pathSerializingCircularReferenceHandler;

         case CircularReference.ThrowException:
            _exceptionCircularReferenceHandler ??= new ExceptionCircularReferenceHandler();
            _exceptionCircularReferenceHandler.Clear();
            return _exceptionCircularReferenceHandler;

         case CircularReference.NoValidation:
            return NullCircularReferenceHandler.Instance;

         default:
            throw new ArgumentOutOfRangeException();
      }
   }

   [ThreadStatic]
   private static PathSerializingCircularReferenceHandler? _pathSerializingCircularReferenceHandler;

   [ThreadStatic]
   private static ExceptionCircularReferenceHandler? _exceptionCircularReferenceHandler;

   [ThreadStatic]
   private static IgnoringCircularReferenceHandler? _ignoringCircularReferenceHandler;
}