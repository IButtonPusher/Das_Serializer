using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.CodeGen
{
    public delegate Int32 GetFieldIndex(PropertyInfo prop,
                                        Int32 lastFieldIndex);
}
