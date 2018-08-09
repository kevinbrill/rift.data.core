using System;
using Assets.DatParser;

namespace rift.data.core.Objects.Primitives
{
	public class FloatObject : BaseObject<float>
	{
		public FloatObject(byte[] data, int datacode) : base(4, data, datacode, CFloatConvertor.inst)
		{
			Value = BitConverter.ToSingle(data, 0);
		}
	}
}
