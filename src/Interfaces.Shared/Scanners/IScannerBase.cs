using System;
using System.Threading.Tasks;

namespace Das.Serializer;

public interface IScannerBase<in TInput> : IScannerBase
{
   TOutput Deserialize<TOutput>(TInput source);
}

public interface IScannerBase
{
   void Invalidate();
}