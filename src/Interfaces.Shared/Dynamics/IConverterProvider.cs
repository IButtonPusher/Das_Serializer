using System;
using System.Threading.Tasks;

namespace Das.Serializer;

public interface IConverterProvider
{
   IObjectConverter ObjectConverter { get; }
}