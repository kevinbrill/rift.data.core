
using System;
using System.IO;
using log4net;
using rift.data.core.IO;
using rift.data.core.Objects;
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

        public static CObject CreateObject(Stream stream)
        {
            var dataStream = new SpecializedBinaryReader(stream);

            // Read the root code
            int rootCode = dataStream.ReadUnsignedLeb128();

            // Create the new root object
            CObject root = new CObject(rootCode, new byte[0], rootCode, null);

            // If the code is an 8, this indicates that the end of the class object
            //  has been reached.  Return the root
            if (rootCode == 8) 
            {
                return root;
            }

            logger.Debug($"Creating root object with code: {rootCode}");

            bool continueParsing;

            do
            {
                // Read the bitpacked code of the next object in the data
                BitResult result = readCodeAndExtract(dataStream);

                if (result == null)
                {
                    throw new Exception($"Invalid bit code from root code {rootCode}");
                }

                // Create the object hierarchy for the next item
                continueParsing = handleCode(root, dataStream, result.Code, result.Data, 1);

            } while (continueParsing);

            return root;
        }

        public static bool handleCode(CObject parent, SpecializedBinaryReader dataStream, int dataCode, int extraData, int indent)
        {
			logger.Debug($"Reading DataCode {dataCode}");

            //parent.index = codedata;
            switch (dataCode)
            {
                case 0:
#if (PLOG)
                    log("handleCode:" + datacode + ", possibly boolean 0", indent);
#endif
					parent.addMember(new BooleanObject(false, extraData));
                    //parent.addMember(new CObject(0, new byte[] { 0x0 }, extradata, CBooleanConvertor.inst));
                    return true;
                case 1:
#if (PLOG)
                    log("handleCode:" + datacode + ", possibly boolean 1", indent);
#endif
					if (parent.Type == 127)
						parent.addMember(new LongObject(new byte[] { 0x01 }, extraData));
						//parent.addMember(new CObject(1, new byte[] { 0x1 }, extradata, CLongConvertor.inst));
					else
						parent.addMember(new BooleanObject(true, extraData));
                        //parent.addMember(new CObject(1, new byte[] { 0x1 }, extradata,  CBooleanConvertor.inst));
                    return true;
                case 2:
                    {
                        // Variable length encoded long
                        MemoryStream bos = new MemoryStream(20);
                        long x = dataStream.readUnsignedVarLong(bos);
                        parent.addMember(new CObject(2, bos, extraData, CUnsignedVarLongConvertor.inst));
#if (PLOG)
                        log("handleCode:" + datacode + ", unsigned long: " + x, indent);
#endif
                        return true;
                    }
                case 3:
                    {
                        // Variable length encoded long
                        MemoryStream bos = new MemoryStream(20);
                        long x = dataStream.readSignedVarLong(bos);
                        parent.addMember(new CObject(3, bos, extraData, CSignedVarLongConvertor.inst));
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
						var numericData = dataStream.ReadBytes(4);

						CObject child = null;

						if(parent.Type == 7703 || parent.Type == 7319 || parent.Type == 7318 || parent.Type == 602 || parent.Type == 603)
						{
							child = new IntegerObject(numericData, extraData);
						}
						else
						{
							child = new FloatObject(numericData, extraData);
						}

						parent.addMember(child);
                        return true;
                    }
                case 5:
                    // 8 bytes, double maybe?

#if (PLOG)
                    log("handleCode:" + datacode + ", long?", indent);
#endif
                    byte[] d = dataStream.ReadBytes(8);

                    if ((parent.Type == 4086))
                    {
                        parent.addMember(new CObject(5, d, extraData,  CFileTimeConvertor.inst));
                        //parent.addMember(readFileTime(diss));
                    }
                    else
                    {
                        parent.addMember(new CObject(5, d, extraData,  CDoubleConvertor.inst));
                    }
                    return true;

                case 6:
#if (PLOG)
                    log("handleCode:" + datacode + ", string/data?", indent);
#endif
					// string or data
					int strLength = dataStream.ReadUnsignedLeb128();
                    byte[] data = dataStream.ReadBytes(strLength);
					//String newString = ASCIIEncoding.ASCII.GetString(data);


					//parent.addMember(new CObject(6, data, extradata,  CStringConvertor.inst));
					parent.addMember(new StringObject(data, extraData));

                    return true;
                case 10:
                case 9:
                    {
                        CObject obj = new CObject(dataCode, new byte[0], extraData, null);
                        parent.addMember(obj);
                        obj.Parent = parent;

                        if (dataCode == 10)
                        {
                            // NEW OBJECT
							int objclass = dataStream.ReadUnsignedLeb128();
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
                            rr = readCodeAndExtract(dataStream);
                            if (rr == null)
                            {
                                loge("WARN: rr null for code [" + dataCode + "][" + x + "], assume it is a boolean", indent);
                                // KLUDGE - Treat as a boolean
                                rr = new BitResult(0, 0);
                                //break;
                            }
                            if (rr.Code == 8)
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
                        } while (handleCode(obj, dataStream, rr.Code, rr.Data, indent + 2));
                        loge("overun while code [" + dataCode + "]:" + rr, indent + 1);

                        return false;
                    }
                case 11:
                    {
                        // array?
                        BitResult bitResult = readCodeAndExtract(dataStream);

                        if (bitResult == null)
                        {
                            logger.Error($"Bad BitResult code 11 for {indent + 1}");
                            return false;
                        }

                        var arrayObject = new ArrayObject(bitResult, indent + 2, dataStream);
                        parent.addMember(arrayObject);

                        return true;


//                        int count = bitResult.Data;
//                        if (count == 0)
//                            return true;
//                        int i = 0;

//                        CObject obj = new CObject(dataCode, new byte[0], count, null);

//                        obj.hintCapacity(count);
//                        obj.index = extradata;
//                        parent.addMember(obj);

//                        int codeOfChildren = bitResult.Code;
//#if (PLOG)
//                        log("array size: " + count + " of type[" + codeOfChildren + "]", indent + 1);
//#endif
//                        while (handleCode(obj, dis, codeOfChildren, i, indent + 2))
//                        {
//#if (PLOG)
//                            log("code 11: handled  item[" + i + " of " + count + "], childcode[" + codeOfChildren + "]",
//                                    indent + 1);
//#endif
                        //    if (++i >= count)
                        //        return true;
                        //}
                        //loge("overun while code 11 [i == " + i + ", count=" + count, indent + 1);

                        //return false;

                    }
                case 12:
                    {
#if (PLOG)
                        log("handleCode:" + datacode + ", array3?", indent);
#endif
                        int[] result = readCodeThenReadTwice(dataStream);

                        int count = result[2];
                        if (count == 0)
                            return true;
                        int i = 0;
                        int ii = 0;
                        CObject obj = new CObject(dataCode, new byte[0], count, null);
                        obj.index = extraData;
                        parent.addMember(obj);
                        while (handleCode(obj, dataStream, result[0], ii++, indent + 1) && handleCode(obj, dataStream, result[1], ii++, indent + 1))
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
                    loge("unk code:" + dataCode, indent);
                    break;

            }

            loge("exit case", indent);
            return false;
        }


        static BitResult readCodeAndExtract(SpecializedBinaryReader dataStream)
        {
			int byteX = dataStream.ReadUnsignedLeb128();

            BitResult result = splitCode(byteX);

			return byteX == 0 ? null : result;
        }

		static int[] readCodeThenReadTwice(SpecializedBinaryReader dis)
        {

			int result = dis.ReadUnsignedLeb128();
            if (result == 0)
                return null;
            int codeA;
            int codeB;

            BitResult a = splitCode(result);
            if (a == null)
                return null;
            codeA = a.Code;

            BitResult b = splitCode(a.Data);

            codeB = b.Code;
            return new int[] { codeA, codeB, b.Data };

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
