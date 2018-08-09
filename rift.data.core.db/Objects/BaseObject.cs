using System;
using Assets.DatParser;

namespace rift.data.core.Objects
{
	public class BaseObject<T> : CObject
    {
		public BaseObject(int type, byte[] data, int datacode, CObjectConverter convertor) :
			base(type, data, datacode, convertor) {}

		public T Value { get; set; }
    }
}
