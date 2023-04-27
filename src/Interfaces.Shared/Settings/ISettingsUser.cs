using System;
using System.Threading.Tasks;

namespace Das.Serializer;

/// <summary>
///     Base interface for any type with a public property of type ISerializerSettings
/// </summary>
/// <see cref="ISerializerSettings" />
public interface ISettingsUser
{
   ISerializerSettings Settings { get; }
}