using System;
using Assets.DatParser;
using Newtonsoft.Json;

namespace rift.data.core.Objects
{
	public class BaseObject<T> : CObject
    {
		public BaseObject(int type, byte[] data, int datacode, CObjectConverter convertor) :
			base(type, data, datacode, convertor) {}

		[JsonProperty("value")]
		public T Value { get; set; }

		[JsonProperty("position")]
        public override int Position => DataCode;

		public override string ToString()
		{
			return $"{GetType().Name}: {Value}";
		}
	}
}
