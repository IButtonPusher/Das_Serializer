using System;
using System.Reflection;
using System.Threading.Tasks;
using Das.Serializer.State;

namespace Das.Serializer.Properties
{
    public interface IFieldActionProvider
    {
        FieldAction GetProtoFieldAction(Type pType);

        /// <summary>
        /// Assumes everything is on the stack and only the corrent "write" method needs to be called
        /// </summary>
        void AppendPrimitive(IDynamicPrintState s,
                             TypeCode typeCode);

        Boolean TryGetSpecialProperty(Type pType,
                                      out PropertyInfo propInfo);
    }
}
