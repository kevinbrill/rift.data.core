using System;
using Assets.DatParser;
using rift.data.core.IO;

namespace rift.data.core.Objects.Primitives
{
	public class UnsignedLongObject : BaseObject<long>
    {
		public UnsignedLongObject(long value, int datacode) : base(2, new byte[0], datacode, CUnsignedVarLongConvertor.inst)
        {
			Value = value;
        }
    }
}
