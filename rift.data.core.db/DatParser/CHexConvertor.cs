using Assets.RiftAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    class CHexConvertor : CObjectConverter
    {

        public override object convert(CObject obj)
        {
		if (obj.Data == null)
			return "";

		return Util.bytesToHexString(obj.Data);
	}
}
}
