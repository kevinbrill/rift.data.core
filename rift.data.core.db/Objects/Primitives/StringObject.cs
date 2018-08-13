using System;
using System.Text;
using Assets.DatParser;

namespace rift.data.core.Objects.Primitives
{
	public class StringObject : BaseObject<string>
	{
		public StringObject(byte[] data, int datacode) : base(6, data, datacode, CStringConvertor.inst)
		{
			Value = Encoding.ASCII.GetString(this.Data);
		}
	}
}