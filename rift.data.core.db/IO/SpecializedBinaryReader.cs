using System;
using System.IO;

namespace rift.data.core.IO
{
	public class SpecializedBinaryReader : BinaryReader
    {
		public SpecializedBinaryReader(Stream input) : base(input)
        {
        }

		public new int Read7BitEncodedInt()
		{
			return base.Read7BitEncodedInt();
		}

		/*
		* Reads an unsigned integer from {@code in}.
		*/
		public int ReadUnsignedLeb128()
		{
			int result = 0;
			int i = 0;
			int index = 0;
			sbyte currentByte;

			while (i < 35)
			{
				index = i;
				i += 7;
				currentByte = ReadSByte();
				result |= (currentByte & 0x7f) << index;

				if (currentByte >= 0)
				{
					return result;
				}
			}

			return 0;
		}
    }
}
