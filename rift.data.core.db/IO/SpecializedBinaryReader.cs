using System;
using System.IO;
using Assets.DatParser;

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

        public BitResult ReadAndExtractCode()
        {
            int byteX = ReadUnsignedLeb128();

            BitResult result = SplitCode(byteX);

            return byteX == 0 ? null : result;
        }

        static BitResult SplitCode(int inv)
        {
            int code = inv & 7;
            int data = inv >> 3;

            if (code == 7)
            {
                data = inv >> 6;
                int v5 = (inv >> 3) & 7;
                if (v5 <= 4)
                {
                    code = v5 + 8;
                    return new BitResult(code, data);
                }
            }
            else if (code <= 7)
            {
                return new BitResult(code, data);
            }

            return null;
        }
    }
}
