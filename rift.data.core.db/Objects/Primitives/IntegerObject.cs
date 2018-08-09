using System;
using Assets.DatParser;

namespace rift.data.core.Objects.Primitives
{
	public class IntegerObject : BaseObject<int>
	{
		public IntegerObject(byte[] data, int datacode) : base(4, data, datacode, CIntConvertor.inst)
		{
			Value = BitConverter.ToInt32(data, 0);
		}
	}
}
