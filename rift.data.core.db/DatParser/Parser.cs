
using System;
using System.IO;
using System.Linq;
using log4net;
using rift.data.core.IO;
using rift.data.core.Model;
using rift.data.core.Objects;
using rift.data.core.Objects.Primitives;

namespace Assets.DatParser
{
	public class Parser
    {
        static Parser()
        {
            DataModel = new DataModel();
        }

		static readonly ILog logger = LogManager.GetLogger(typeof(Parser));

        public static DataModel DataModel { get; set; }

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

            Class rootClass = null;

            // Lookup the code type for the root
            if(DataModel.Classes.ContainsKey(rootCode))
            {
                rootClass = DataModel.Classes[rootCode];

                if (rootClass != null)
                {
                    root.Name = rootClass.Name;
                    root.TypeDescription = rootClass.Name;
                }
            }

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
                continueParsing = handleCode(root, dataStream, result.Code, result.Data, 1, rootClass);

            } while (continueParsing);

            return root;
        }

        public static bool handleCode(CObject parent, SpecializedBinaryReader dataStream, int dataCode, int extraData, int indent, Class parentClass = null)
        {
			logger.Debug($"Reading DataCode {dataCode}");

            CObject newMember;

            //parent.index = codedata;
            switch (dataCode)
            {
                case 0:
#if (PLOG)
                    log("handleCode:" + datacode + ", possibly boolean 0", indent);
#endif
                    newMember = new BooleanObject(false, extraData);

                    SetDataModelProperties(newMember, parentClass);

                    parent.addMember(newMember);

                    //parent.addMember(new CObject(0, new byte[] { 0x0 }, extradata, CBooleanConvertor.inst));
                    return true;
                case 1:
#if (PLOG)
                    log("handleCode:" + datacode + ", possibly boolean 1", indent);
#endif
                    newMember = parent.Type == 127 ? 
                                      new LongObject(new byte[] { 0x01 }, extraData) : 
                                      (CObject)new BooleanObject(true, extraData);

                    //parent.addMember(new CObject(1, new byte[] { 0x1 }, extradata, CLongConvertor.inst));

                    SetDataModelProperties(newMember, parentClass);

                    parent.addMember(newMember);

                        //parent.addMember(new CObject(1, new byte[] { 0x1 }, extradata,  CBooleanConvertor.inst));
                    return true;
                case 2:
                    {
                        // Variable length encoded long
                        MemoryStream bos = new MemoryStream(20);
                        long longValue = dataStream.readUnsignedVarLong(bos);

						//newMember = new CObject(2, bos, extraData, CUnsignedVarLongConvertor.inst);
						newMember = new UnsignedLongObject(longValue, extraData);

                        SetDataModelProperties(newMember, parentClass);

                        parent.addMember(newMember);
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

						newMember = new CObject(3, bos, extraData, CSignedVarLongConvertor.inst);

                        SetDataModelProperties(newMember, parentClass);

                        parent.addMember(newMember);
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

						if(parent.Type == 7703 || parent.Type == 7319 || parent.Type == 7318 || parent.Type == 602 || parent.Type == 603)
						{
                            newMember = new IntegerObject(numericData, extraData);
						}
						else
						{
                            newMember = new FloatObject(numericData, extraData);
						}

                        SetDataModelProperties(newMember, parentClass);

                        parent.addMember(newMember);
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
                        newMember = new CObject(5, d, extraData,  CFileTimeConvertor.inst);
                        //parent.addMember(readFileTime(diss));
                    }
                    else
                    {
                        newMember = new CObject(5, d, extraData,  CDoubleConvertor.inst);
                    }

                    SetDataModelProperties(newMember, parentClass);

                    parent.addMember(newMember);

                    return true;

                case 6:
#if (PLOG)
                    log("handleCode:" + datacode + ", string/data?", indent);
#endif
					// string or data
					int strLength = dataStream.ReadUnsignedLeb128();
                    byte[] data = dataStream.ReadBytes(strLength);
                    //String newString = ASCIIEncoding.ASCII.GetString(data);

                    newMember = new StringObject(data, extraData);

                    SetDataModelProperties(newMember, parentClass);

                    //parent.addMember(new CObject(6, data, extradata,  CStringConvertor.inst));
                    parent.addMember(newMember);

                    return true;
                case 10:
                case 9:
                    {
                        Class classDefinition = null;

                        CObject obj = new CObject(dataCode, new byte[0], extraData, null);
                        parent.addMember(obj);
                        obj.Parent = parent;

                        // NEW OBJECT
                        if (dataCode == 10)
                        {
                            //  Read the class code
                            int objectClassCode = dataStream.ReadUnsignedLeb128();

                            // Look up the new class definition from with the data model
                            if(DataModel.Classes.ContainsKey(objectClassCode))
                            {
                                classDefinition = DataModel.Classes[objectClassCode];
                            }

                            // Set the properties on the object
                            SetDataModelProperties(obj, parentClass);

                            // Set the typeo
                            obj.Type = objectClassCode;

                            if (objectClassCode > 0xFFFF || objectClassCode == 0)
                            {
                                logger.Warn($"Data Type 10 has an out of range value code '{objectClassCode}'");
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
                            if(DataModel.Classes.ContainsKey(obj.Type))
                            {
                                classDefinition = DataModel.Classes[obj.Type];

                                obj.TypeDescription = classDefinition.Name;
                                obj.Name = classDefinition.Name;
                                obj.DataCode = extraData;
                            }

                            rr = dataStream.ReadAndExtractCode();

                            if (rr == null)
                            {
                                logger.Warn($"Received a null code for the member type '{dataCode}' at position '{x}'.  Forcing it to boolean false");

                                // KLUDGE - Treat as a boolean
                                rr = new BitResult(0, 0);
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
                        } while (handleCode(obj, dataStream, rr.Code, rr.Data, indent + 2, classDefinition));
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

                        newMember = new ArrayObject(bitResult, indent + 2, dataStream, extraData);

                        SetDataModelProperties(newMember, parentClass);

                        parent.addMember(newMember);

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
						newMember = new CObject(dataCode, new byte[0], count, null);
						newMember.index = extraData;

                        SetDataModelProperties(newMember, parentClass);

                        parent.addMember(newMember);
						while (handleCode(newMember, dataStream, result[0], ii++, indent + 1) && handleCode(newMember, dataStream, result[1], ii++, indent + 1))
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

        static void SetDataModelProperties(CObject @object, Class parentClass)
        {
            if(parentClass == null)
            {
                return;
            }

            var matchedProperty = parentClass.Properties.FirstOrDefault(x => x.Index == @object.DataCode);

            if(matchedProperty == null)
            {
                return;
            }

            @object.Name = matchedProperty?.Name;
            @object.TypeDescription = matchedProperty.Type;
        }
    }
}
