using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Database.Frequencies;
using Assets.RiftAssets;
using log4net;
using Newtonsoft.Json;

namespace Assets.Language
{
	public class LanguageMap
    {
		static readonly ILog logger = LogManager.GetLogger(typeof(LanguageMap));

		AssetDatabase _assetDatabase;

		public Dictionary<int, LanguageEntry> Entries { get; set; }

		public Languages Language { get; private set; }

		public LanguageMap(AssetDatabase assetDatabase, Languages language)
        {
			_assetDatabase = assetDatabase;
			Language = language;

			Entries = new Dictionary<int, LanguageEntry>();
        }

		public void Load(Action<string> progress = null)
		{
			var assetData = _assetDatabase.extractUsingFilename($"lang_{Language}.cds");

			Process(assetData, progress);
		}

        public string ToJson()
        {
            using(var writer = new StringWriter())
            {
                var serializer = new JsonSerializer();

                ToJson(writer);

                return writer.ToString();
            }
        }

        public void ToJson(TextWriter writer)
        {
            var serializer = new JsonSerializer();

            var orderedEntries = Entries.Values.OrderBy(x => x.Key);

            serializer.Serialize(writer, orderedEntries);   
        }

		void Process(byte[] cdsData, Action<String> progress)
        {
            logger.Info($"Loading {Language} language data");

            using (MemoryStream memStream = new MemoryStream(cdsData))
            {
                using (BinaryReader dis = new BinaryReader(memStream))
                {
                    int entryCount = dis.ReadInt32();
                    byte[] freqData = dis.ReadBytes(1024);
                    HuffmanReader reader = new HuffmanReader(freqData);

                    logger.Debug($"Found {entryCount} {Language} language entries.  Loading now.");

                    List<int> keys = new List<int>(entryCount);
                    for (int i = 0; i < entryCount; i++)
                    {
                        int key = dis.ReadInt32();
                        int offset = Util.readUnsignedLeb128_X(dis.BaseStream);
                        keys.Add(key);
                    }
                    for (int i = 0; i < entryCount; i++)
                    {
                        progress?.Invoke("english " + i + "/" + entryCount);
                        int compressedSize = Util.readUnsignedLeb128_X(dis.BaseStream);
                        int uncompressedSize = Util.readUnsignedLeb128_X(dis.BaseStream);
                        byte[] data = dis.ReadBytes(compressedSize);
                        byte[] dataOut = new byte[uncompressedSize];

                        dataOut = reader.Read(data, data.Length, dataOut.Length);

						var entry = new LanguageEntry(keys[i], dataOut);
                        Entries[entry.Key] = entry;
                    }
                    
                }

                logger.Info($"Completed loading of {Language} language data.  {Entries.Count} language entries were loaded");
            }

			progress?.Invoke("done");
        }
    }
}
