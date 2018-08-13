using System;
using Assets.DatParser;

namespace rift.data.core.Objects.Primitives
{
	public class BooleanObject : BaseObject<bool>
    {
		public BooleanObject(bool value, int dataCode) : 
			base(value ? 1 : 0, 
		         value ? new byte[] { 0x1 } : new byte[] {0x0},
		         dataCode,
		         CBooleanConvertor.inst)
		{
			Value = value;
		}
	}
}
