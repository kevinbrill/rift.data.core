using System;
using System.Collections.Generic;
using System.IO;
using Assets.DatParser;
using log4net;

namespace Assets.RiftAssets
{

	public class LangEntry
    {
        public int key;
        public byte[] cdata;
        private string str;

        public string text
        {
            get
            {
                try
                {
                    if (str != null)
                        return str;

                    CObject obj = DatParser.Parser.processStreamObject(cdata);
                    str = "" + obj.get(0).get(1).get(0).convert();
                }
                catch (Exception ex)
                {
                    str = "";
                }
                return str;
            }
        }
    }
    public class DBLang
    {
		static readonly ILog logger = LogManager.GetLogger(typeof(DBLang));

        public Dictionary<int, LangEntry> stringMap = new Dictionary<int, LangEntry>();

        public IEnumerable<int> keys {  get { return stringMap.Keys;  } }
        public string get(int i)
        {
            return stringMap[i].text;
        }
        public string getOrDefault(int i, string txt)
        {
            if (stringMap.ContainsKey(i))
                return stringMap[i].text;
            return txt;
        }
        public DBLang(AssetDatabase adb, string lang, Action<String> progress)
        {
            process(adb.extractUsingFilename("lang_" + lang + ".cds"), progress);
        }

        public void process(byte[] cdsData, Action<String> progress)
        {
            logger.Debug("process lang");

            using (MemoryStream memStream = new MemoryStream(cdsData))
            {
                using (BinaryReader dis = new BinaryReader(memStream))
                {
                    int entryCount = dis.ReadInt32();
                    byte[] freqData = dis.ReadBytes(1024);
                    HuffmanReader reader = new HuffmanReader(freqData);

                    List<int> keys = new List<int>(entryCount);
                    for (int i = 0; i < entryCount; i++)
                    {
                        int key = dis.ReadInt32();
                        int offset = Util.readUnsignedLeb128_X(dis.BaseStream);
                        keys.Add(key);
                    }
                    for (int i = 0; i < entryCount; i++)
                    {
                        if (progress != null)
                            progress.Invoke("english " + i + "/" + entryCount);
                        int compressedSize = Assets.RiftAssets.Util.readUnsignedLeb128_X(dis.BaseStream);
                        int uncompressedSize = Assets.RiftAssets.Util.readUnsignedLeb128_X(dis.BaseStream);
                        byte[] data = dis.ReadBytes(compressedSize);
                        byte[] dataOut = new byte[uncompressedSize];

                        dataOut = reader.read(data, data.Length, dataOut.Length);

                        LangEntry entry = new LangEntry();
                        entry.key = keys[i];
                        entry.cdata = dataOut;
                        stringMap[entry.key] = entry;
                    }
                    
                }
            }
            logger.Debug("done process lang");
            progress.Invoke("done");
        }

    }
}
