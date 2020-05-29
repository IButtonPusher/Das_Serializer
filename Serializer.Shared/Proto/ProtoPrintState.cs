using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Das.Serializer.Proto
{
    public class ProtoPrintState : ProtoStateBase, IEnumerable<ProtoPrintState>
    {
        private readonly Action<ILGenerator> _loadObject;

        public Boolean IsArrayMade { get; set; }

        public LocalBuilder? LocalString { get; set; }

        public LocalBuilder FieldByteArray { get; }

        public LocalBuilder? LocalBytes { get; set; }

        public Type ParentType { get; }

        public FieldInfo UtfField { get; }

        public Boolean HasPushed { get; set; }

        public LocalBuilder? ChildObjectStream { get; }

        public ProtoPrintState(ProtoPrintState s, ICollection<IProtoFieldAccessor> subFields,
            Type parentType,Action<ILGenerator> loadObject,ITypeCore typeCore)
        : this(s.IL, s.IsArrayMade, s.LocalString, s.FieldByteArray, s.LocalBytes,
            subFields, parentType, s.UtfField, loadObject, s.HasPushed ,typeCore, s.ChildProxies)
        {
            
        }

        public ProtoPrintState(ILGenerator il, Boolean isArrayMade,
            LocalBuilder? localString,
            LocalBuilder fieldByteArray, LocalBuilder? localBytes,
            ICollection<IProtoFieldAccessor> fields, Type parentType,
            FieldInfo utfField, Action<ILGenerator> loadObject,
            Boolean hasPushed, ITypeCore typeCore, 
            Dictionary<IProtoFieldAccessor, LocalBuilder>? childProxies = null) 
            : base(il, fields, typeCore, childProxies)
        {
            _loadObject = loadObject;
            
            IsArrayMade = isArrayMade;
            LocalString = localString;
            FieldByteArray = fieldByteArray;
            LocalBytes = localBytes;
            ParentType = parentType;
            UtfField = utfField;
            HasPushed = hasPushed;
            Fields = fields.ToArray();

            if (ChildProxies.Count > 0 && childProxies == null)
            {
                ChildObjectStream = il.DeclareLocal(typeof(MemoryStream));
                typeCore.TryGetEmptyConstructor(typeof(MemoryStream), out var ctor);
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Stloc, ChildObjectStream);
            }
        }

        public void LoadParentToStack()
        {
            _loadObject(IL);
        }

        public void LoadCurrentFieldValueToStack()
        {
            LoadParentToStack();
            IL.Emit(OpCodes.Call, CurrentField.GetMethod);
        }

        public void LoadProxyToStack()
        {
            IL.Emit(OpCodes.Ldarg_0);
        }

        public void MergeLocals(ProtoPrintState s)
        {
            IsArrayMade |= s.IsArrayMade;
            HasPushed |= s.HasPushed;
            LocalBytes ??= s.LocalBytes;
            LocalString ??= s.LocalString;
        }

        public IProtoFieldAccessor[] Fields { get; }

        public IProtoFieldAccessor CurrentField { get; set; }

        public IEnumerator<ProtoPrintState> GetEnumerator()
        {
            for (var c = 0; c < Fields.Length; c++)
            {
                CurrentField = Fields[c];
                yield return this;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
