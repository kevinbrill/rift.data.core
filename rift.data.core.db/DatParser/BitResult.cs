using System;

namespace Assets.DatParser
{
    public class BitResult
    {
        public BitResult(int code, int memberIndex)
        {
            Code = code;
            Data = memberIndex;
        }

        public int Code { get; set; }
        public int Data { get; set; }

        public  override String ToString()
        {
            return "c[" + Code + "]d[" + Data + "]";
        }
    }
}
