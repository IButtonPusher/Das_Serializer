using System;

namespace Das
{
	public class SerializeAsTypeAttribute : Attribute
	{
		public Type TargetType { get; set; }
		public SerializeAsTypeAttribute(Type type)
		{
			TargetType = type;
		}
	}
}
