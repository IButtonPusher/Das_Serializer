using System;
using System.Collections.Generic;

namespace Das.Serializer
{
	public class DasAttribute
	{
		public Type Type { get; set; }
		public Dictionary<String, Object> PropertyValues { get; set; }
		public Object[] ConstructionValues { get; set; }

		public DasAttribute()
		{
			PropertyValues = new Dictionary<string, object>();
		}
	}
}
