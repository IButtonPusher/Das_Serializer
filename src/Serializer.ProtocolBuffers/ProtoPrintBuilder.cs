#if GENERATECODE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer.CodeGen;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        protected ILGenerator OpenPrintMethod(TypeBuilder bldr,
                                              Type dtoType,
                                              IEnumerable<IProtoFieldAccessor> fields,
                                              IDictionary<Type, ProxiedInstanceField> typeProxies,
                                              out ProtoPrintState? initialState,
                                              out MethodBuilder method)
        {
            var genericParent = typeof(ProtoDynamicBase<>).MakeGenericType(dtoType);

            var abstractMethod = genericParent.GetMethod(
                                     nameof(ProtoDynamicBase<Object>.Print))
                                 ?? throw new InvalidOperationException();

            method = bldr.DefineMethod(nameof(ProtoDynamicBase<Object>.Print),
                MethodOverride, typeof(void), new[] { dtoType, typeof(Stream) });
            bldr.DefineMethodOverride(method, abstractMethod);

            var il = method.GetILGenerator();

            initialState = GetInitialState(dtoType, fields, typeProxies, il);

            return il;
        }

        private ProtoPrintState? GetInitialState(Type parentType,
                                                 IEnumerable<IProtoFieldAccessor> fields,
                                                 IDictionary<Type, ProxiedInstanceField> typeProxies,
                                                 ILGenerator il)
        {
            Action<ILGenerator> loadDto = parentType.IsValueType
                ? LoadValueDto
                : LoadReferenceDto;

            var fArr = fields.ToArray();

            if (fArr.Length == 0)
                return null;

            var startField = fArr[0];

            var state = new ProtoPrintState(il, false,
                fArr, parentType,
                loadDto, _types, this, startField,
                typeProxies, this);

            if (typeProxies.Count > 0)
                state.EnsureChildObjectStream();


            return state;
        }


        private MethodBuilder AddPrintMethod(Type parentType,
                                    TypeBuilder bldr,
                                    IEnumerable<IProtoFieldAccessor> fields,
                                    IDictionary<Type, ProxiedInstanceField> typeProxies,
                                    out ILGenerator il)
        {
            il = OpenPrintMethod(bldr, parentType, fields, typeProxies, 
               out var state,
               out var method);


            if (state == null)
                goto endOfMethod;


            foreach (var protoField in state)
            {
               /////////////////////////////////////////
               AddFieldToPrintMethod(protoField);
               /////////////////////////////////////////
            }


            endOfMethod:
            il.Emit(OpCodes.Ret);

            return method;
        }

        
    }
}

#endif
