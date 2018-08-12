using System;
using Assets.DatParser;

namespace rift.data.core.Objects.Primitives
{
	public class LongObject : BaseObject<long>
	{
		public LongObject(byte[] data, int datacode) : base(1, data, datacode, CLongConvertor.inst)
		{
			Value = BitConverter.ToInt64(data, 0);
		}
	}
}
