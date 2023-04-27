using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer;

public abstract class BaseNode<TNode> : TypeCore,
                                        INode<TNode>
   where TNode : INode<TNode>
{
#pragma warning disable 8618
   protected BaseNode(ISerializerSettings settings) :
#pragma warning restore 8618
      base(settings)
   {
      DynamicProperties = new Dictionary<String, Object?>();
      _attributes = new Dictionary<String, AttributeValue>();
      _name = Const.Empty;
   }

   public Boolean IsForceNullValue { get; set; }

   public String Name
   {
      get => _name;
      set => _name = value ?? throw new InvalidOperationException();
   }


   TNode INode<TNode>.Parent
   {
      get => _parent;
      set => _parent = value;
   }

   public Type? Type { get; set; }

   public Object? Value { get; set; }

   public IEnumerable<KeyValuePair<string, AttributeValue>> Attributes => _attributes;

   public Boolean TryGetAttribute(String key,
                                  Boolean isRemoveIfFound,
                                  out AttributeValue value)
   {
      if (!_attributes.TryGetValue(key, out value))
         return false;

      if (isRemoveIfFound)
         _attributes.Remove(key);

      return true;
   }

   public void AddAttribute(String key,
                            String value,
                            Boolean wasValueInQuotes)
   {

      _attributes.Add(key, new AttributeValue(value, wasValueInQuotes));
   }

   public IDictionary<String, Object?> DynamicProperties { get; }

   public INode Parent => _parent;

   public NodeTypes NodeType { get; set; }

   public virtual Boolean IsEmpty => false;


   public virtual void Clear()
   {
      Name = Const.Empty;
      IsForceNullValue = false;
      Type = default;
      Value = default;
      _attributes.Clear();
      DynamicProperties.Clear();

      _parent = default!;
      NodeType = NodeTypes.None;
   }

   public override String ToString()
   {
      return $"Name: {Name} Type: {Type}: Val: {Value} ";
   }

   private readonly Dictionary<String, AttributeValue> _attributes;

   private String _name;

   private TNode _parent;
}