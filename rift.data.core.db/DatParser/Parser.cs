
using System;
using System.IO;
using log4net;
using rift.data.core.IO;
using rift.data.core.Objects.Primitives;

namespace Assets.DatParser
{
	public class Parser
    {
		static readonly ILog logger = LogManager.GetLogger(typeof(Parser));

        public static CObject CreateObject(byte[] data)
        {
            return CreateObject(new MemoryStream(data));
        }

        public static CObject CreateObject(Stream ins)
        {
            var dis = new SpecializedBinaryReader(ins);

			int code1 = dis.ReadUnsignedLeb128();
#if (PLOG)
            log("code1:" + code1, 0);
#endif
            CObject root = new CObject(code1, new byte[0], code1, null);
            root.Type = code1;
            if (code1 == 8)
                return root;
            bool r;
            int i = 0;
            do
            {
                BitResult result = Parser.readCodeAndExtract(dis, 0);
                if (result == null)
                    throw new Exception("Unable to process result, class code:" + code1 + ":" + result);
#if (PLOG)
                log("do member " + (++i) + ": with code:" + result, 0);
#endif
                r = Parser.handleCode(root, dis, result.code, result.data, 1);
            } while (r);
            return root;
        }

		public static bool handleCode(CObject parent, SpecializedBinaryReader dis, int datacode, int extradata, int indent)
        {
			logger.Debug($"Reading DataCode {datacode}");

            //parent.index = codedata;
            switch (datacode)
            {
                case 0:
#if (PLOG)
                    log("handleCode:" + datacode + ", possibly boolean 0", indent);
#endif
					parent.addMember(new BooleanObject(false, extradata));
                    //parent.addMember(new CObject(0, new byte[] { 0x0 }, extradata, CBooleanConvertor.inst));
                    return true;
                case 1:
#if (PLOG)
                    log("handleCode:" + datacode + ", possibly boolean 1", indent);
#endif
					if (parent.Type == 127)
						parent.addMember(new LongObject(new byte[] { 0x01 }, extradata));
						//parent.addMember(new CObject(1, new byte[] { 0x1 }, extradata, CLongConvertor.inst));
					else
						parent.addMember(new BooleanObject(true, extradata));
                        //parent.addMember(new CObject(1, new byte[] { 0x1 }, extradata,  CBooleanConvertor.inst));
                    return true;
                case 2:
                    {
                        // Variable length encoded long
                        MemoryStream bos = new MemoryStream(20);
                        long x = dis.readUnsignedVarLong(bos);
                        parent.addMember(new CObject(2, bos, extradata, CUnsignedVarLongConvertor.inst));
#if (PLOG)
                        log("handleCode:" + datacode + ", unsigned long: " + x, indent);
#endif
                        return true;
                    }
                case 3:
                    {
                        // Variable length encoded long
                        MemoryStream bos = new MemoryStream(20);
                        long x = dis.readSignedVarLong(bos);
                        parent.addMember(new CObject(3, bos, extradata, CSignedVarLongConvertor.inst));
#if (PLOG)
                        log("handleCode:" + datacode + ", signed long: " + x, indent);

#endif
                        return true;
                    }
                case 4:
                    {
						// 4 bytes, int maybe?
#if (PLOG)
						log("handleCode:" + datacode + ", int?", indent);
#endif
						var numericData = dis.ReadBytes(4);

						CObject child = null;

						if(parent.Type == 7703 || parent.Type == 7319 || parent.Type == 7318 || parent.Type == 602 || parent.Type == 603)
						{
							child = new IntegerObject(numericData, extradata);
						}
						else
						{
							child = new FloatObject(numericData, extradata);
						}

						parent.addMember(child);
                        return true;
                    }
                case 5:
                    // 8 bytes, double maybe?

#if (PLOG)
                    log("handleCode:" + datacode + ", long?", indent);
#endif
                    byte[] d = dis.ReadBytes(8);

                    if ((parent.Type == 4086))
                    {
                        parent.addMember(new CObject(5, d, extradata,  CFileTimeConvertor.inst));
                        //parent.addMember(readFileTime(diss));
                    }
                    else
                    {
                        parent.addMember(new CObject(5, d, extradata,  CDoubleConvertor.inst));
                    }
                    return true;

                case 6:
#if (PLOG)
                    log("handleCode:" + datacode + ", string/data?", indent);
#endif
					// string or data
					int strLength = dis.ReadUnsignedLeb128();
                    byte[] data = dis.ReadBytes(strLength);
					//String newString = ASCIIEncoding.ASCII.GetString(data);


					//parent.addMember(new CObject(6, data, extradata,  CStringConvertor.inst));
					parent.addMember(new StringObject(data, extradata));

                    return true;
                case 10:
                case 9:
                    {
                        CObject obj = new CObject(datacode, new byte[0], extradata, null);
                        parent.addMember(obj);
                        obj.Parent = parent;

                        if (datacode == 10)
                        {
                            // NEW OBJECT
							int objclass = dis.ReadUnsignedLeb128();
                            //obj.addMember(value);

                            obj.Type = objclass;
                            if (objclass > 0xFFFF || objclass == 0)
                            {
                                loge("bad value code 10", indent);
                                return false;
                            }
                        }
#if (PLOG)
                        log("handleCode:" + datacode + ", array: " + obj.type, indent + 1);
#endif
                        // array?
                        BitResult rr;
                        int x = 0;
                        do
                        {
                            rr = readCodeAndExtract(dis, indent + 2);
                            if (rr == null)
                            {
                                loge("WARN: rr null for code [" + datacode + "][" + x + "], assume it is a boolean", indent);
                                // KLUDGE - Treat as a boolean
                                rr = new BitResult(0, 0);
                                //break;
                            }
                            if (rr.code == 8)
                            {
#if (PLOG)
                                log("end object, read [" + x + "], objects", indent + 1);
#endif
                                return true;
                            }
#if (PLOG)
                            log("handle code[" + rr.code + "]", indent + 1);
#endif
                            x++;
                        } while (handleCode(obj, dis, rr.code, rr.data, indent + 2));
                        loge("overun while code [" + datacode + "]:" + rr, indent + 1);

                        return false;
                    }
                case 11:
                    {
                        // array?
#if (PLOG)
                        log("handlecode:" + datacode + ", get data", indent + 1);
#endif
                        BitResult r = readCodeAndExtract(dis, indent + 1);
                        if (r == null)
                        {
                            loge("bad bitresult code 11", indent + 1);
                            return false;
                        }
#if (PLOG)
                        log("bitresult:" + r, indent+1);
#endif
                        int count = r.data;
                        if (count == 0)
                            return true;
                        int i = 0;
                        CObject obj = new CObject(datacode, new byte[0], count, null);
                        obj.hintCapacity(count);
                        obj.index = extradata;
                        parent.addMember(obj);

                        int codeOfChildren = r.code;
#if (PLOG)
                        log("array size: " + count + " of type[" + codeOfChildren + "]", indent + 1);
#endif
                        while (handleCode(obj, dis, codeOfChildren, i, indent + 2))
                        {
#if (PLOG)
                            log("code 11: handled  item[" + i + " of " + count + "], childcode[" + codeOfChildren + "]",
                                    indent + 1);
#endif
                            if (++i >= count)
                                return true;
                        }
                        loge("overun while code 11 [i == " + i + ", count=" + count, indent + 1);

                        return false;

                    }
                case 12:
                    {
#if (PLOG)
                        log("handleCode:" + datacode + ", array3?", indent);
#endif
                        int[] result = readCodeThenReadTwice(dis, indent + 1);

                        int count = result[2];
                        if (count == 0)
                            return true;
                        int i = 0;
                        int ii = 0;
                        CObject obj = new CObject(datacode, new byte[0], count, null);
                        obj.index = extradata;
                        parent.addMember(obj);
                        while (handleCode(obj, dis, result[0], ii++, indent + 1) && handleCode(obj, dis, result[1], ii++, indent + 1))
                        {
                            if (++i >= count)
                                return true;
                        }
                        loge("overun while code 12", indent + 1);
                        return false;
                    }
                case 8:
#if (PLOG)
                    log("handleCode:" + datacode + ", end of object", indent);
#endif
                    // END OF OBJECT
                    return false;
                default:
                    loge("unk code:" + datacode, indent);
                    break;

            }

            loge("exit case", indent);
            return false;
        }


		static BitResult readCodeAndExtract(SpecializedBinaryReader dis, int indent)
        {
			int byteX = dis.ReadUnsignedLeb128();
#if (PLOG)
            log("byteX:" + byteX, indent);
#endif
            BitResult result = splitCode(byteX);

			return byteX == 0 ? null : result;
        }

		static int[] readCodeThenReadTwice(SpecializedBinaryReader dis, int indent)
        {

			int result = dis.ReadUnsignedLeb128();
            if (result == 0)
                return null;
            int codeA;
            int codeB;

            BitResult a = splitCode(result);
            if (a == null)
                return null;
            codeA = a.code;

            BitResult b = splitCode(a.data);

            codeB = b.code;
            return new int[] { codeA, codeB, b.data
    };

        }

        static BitResult splitCode(int inv)
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

        public static void log(String s, int indent)
        {
            //Debug.Log(s);
        }

        static void loge(String s, int indent)
        {
        }
        
    }


}
