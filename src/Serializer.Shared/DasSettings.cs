using System;
using System.Threading.Tasks;
using Das.Extensions;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

namespace Das.Serializer;

public class DasSettings : ISerializerSettings
{
   static DasSettings()
   {
      _default = new DasSettings();
   }

   public DasSettings()
   {
      IsPropertyNamesCaseSensitive = true;
      TypeSpecificity = TypeSpecificity.Discrepancy;
      SerializationDepth = SerializationDepth.GetSetProperties;
      TypeSearchNameSpaces = new[] {Const.Tsystem};
      CacheTypeConstructors = true;
      IsUseAttributesInXml = true;
      PropertyNameFormat = PropertyNameFormat.Default;
      PropertyNotFoundBehavior = PropertyNotFoundBehavior.Ignore;
      CircularReferenceBehavior = CircularReference.NoValidation;
   }

   /// <summary>
   ///     If an unknown type is in the serialized data, a dynamic type can be built
   ///     at runtime including properties.
   /// </summary>
   public TypeNotFoundBehavior TypeNotFoundBehavior { get; set; }

   public PropertyNameFormat ScanPropertyNameFormat
   {
      get => _scanPropertyNameFormat;
      set => SetPrintScanProperty(ref _scanPropertyNameFormat, value);
   }

   public PropertyNotFoundBehavior PropertyNotFoundBehavior { get; set; }

   /// <summary>
   ///     Particularly for Json pascal case is often used.  Setting this to false
   ///     and having multiple properties with the "same" name will be problematic
   /// </summary>
   public Boolean IsPropertyNamesCaseSensitive { get; set; }

   public PropertyNameFormat PropertyNameFormat { get; set; }


   public Boolean IsUseAttributesInXml
   {
      get => _isUseAttributesInXml;
      set => SetPrintScanProperty(ref _isUseAttributesInXml, value);
   }

   /// <summary>
   ///     Specifies under which circumstances the serializer will embed type information for
   ///     properties.  For xml the type of the root object is always the root node.  Choosing
   ///     All will cause the Json and binary formats to wrap their output in an extra node
   ///     which may make it impossible for other deserializers or services to understand the data
   /// </summary>
   public TypeSpecificity TypeSpecificity
   {
      get => _typeSpecificity;
      set => SetPrintScanProperty(ref _typeSpecificity, value);
   }

   public Boolean IsFormatSerializedText
   {
      get => _isFormatSerializedText;
      set => SetPrintScanProperty(ref _isFormatSerializedText, value);
   }

   private void SetPrintScanProperty<TField>(ref TField current,
                                             TField newValue)
   {
      if (Equals(current, newValue))
         return;

      current = newValue;
      ComputePrintScanSignature();
   }

   private void ComputePrintScanSignature()
   {
      _printScanSignature = GetI4(IsUseAttributesInXml, 0) +
                            GetI4(TypeSpecificity, 1) +
                            GetI4(IsFormatSerializedText, 3) +
                            GetI4(PrintPropertyNameFormat, 4) +
                            GetI4(ScanPropertyNameFormat, 6) +
                            GetI4(IsOmitDefaultValues, 8);
   }

   public Int32 GetPrintScanSignature()
   {
      return _printScanSignature;
   }

   public ISerializerSettings DeepCopy()
   {
      var res = new DasSettings
      {
         IsFormatSerializedText = IsFormatSerializedText,
         TypeSpecificity = TypeSpecificity,
         TypeSearchNameSpaces = new String[TypeSearchNameSpaces.Length],
         SerializationDepth = SerializationDepth,
         CacheTypeConstructors = CacheTypeConstructors,
         IsUseAttributesInXml = IsUseAttributesInXml,
         PropertyNameFormat = PropertyNameFormat,
         PropertyNotFoundBehavior = PropertyNotFoundBehavior,
         CircularReferenceBehavior = CircularReferenceBehavior
      };

      TypeSearchNameSpaces.CopyTo(res.TypeSearchNameSpaces, 0);

      return res;
   }

   private static Int32 GetI4(Boolean b,
                              Int32 push)
   {
      return (b ? 1 : 0) << push;
   }

   private static Int32 GetI4<TEnum>(TEnum val,
                                     Int32 push)
      where TEnum : Enum, IConvertible
   {
      return Convert.ToInt32(val) << push;
   }

   public CircularReference CircularReferenceBehavior { get; set; }

   /// <summary>
   ///     In Xml/Json only.  0 for integers, false for booleans, and any
   ///     val == default(ValsType) will be ommitted from the markup
   /// </summary>
   public Boolean IsOmitDefaultValues
   {
      get => _isOmitDefaultValues;
      set => SetPrintScanProperty(ref _isOmitDefaultValues, value);
   }

   /// <summary>
   ///     Allows to set whether properties without setters and whether private fields
   ///     will be serialized.  Default is GetSetProperties
   /// </summary>
   public SerializationDepth SerializationDepth { get; set; }

   Boolean ISerializationDepth.IsRespectXmlIgnore => false;

   /// <summary>
   ///     Types from xml/json that are not namespace or assembly qualified will be
   ///     searched for in this collection of namespaces. Defaults to just System
   /// </summary>
   public String[] TypeSearchNameSpaces { get; set; }

   /// <summary>
   ///     Defines the depth of the search to resolve elements to their types when
   ///     deserializing text as JSON or XML
   /// </summary>
   public TextPropertySearchDepths PropertySearchDepth { get; set; }

   /// <summary>
   ///     Allows control over how much nested elements in json and xml are indented.
   ///     Use whitespace like spaces and tabs only
   /// </summary>
   public String Indentation { get; set; } = "  ";

   public String NewLine { get; set; } = "\r\n";

   public PropertyNameFormat PrintPropertyNameFormat
   {
      get => _printPropertyNameFormat;
      set => SetPrintScanProperty(ref _printPropertyNameFormat, value);
   }

   public Boolean CacheTypeConstructors { get; set; }

   /// <summary>
   ///     Returns a mutable copy of the defaults. A new copy is generated each time
   ///     this property is accessed.
   /// </summary>
   public static DasSettings CloneDefault()
   {
      return (DasSettings) _default.MemberwiseClone();
   }

   public Boolean Equals(ISerializationDepth? other)
   {
      return this.AreEqual(other);
   }


   private static readonly DasSettings _default;
   private Boolean _isUseAttributesInXml;
   private TypeSpecificity _typeSpecificity;
   private Boolean _isFormatSerializedText;
   private PropertyNameFormat _printPropertyNameFormat;
   private PropertyNameFormat _scanPropertyNameFormat;
   private Boolean _isOmitDefaultValues;
   private Int32 _printScanSignature;
}