using System;
using System.IO;
using Assets.Database.Frequencies;
using Assets.DatParser;

namespace Assets.Database
{
    public class Entry
    {
		byte[] _data;
		CObject _object;

		public Entry(long datasetId, long key, string name, byte[] data)
        {
			DatasetId = datasetId;
			Key = key;
			Name = name;
			CompressedData = data;
        }

		public long DatasetId { get; set; }
		public long Key { get; set; }
		public string Name { get; set; }
		public byte[] CompressedData { get; set; }

		public byte[] Data 
		{
			get
			{
				if(_data == null)
				{
					_data = FrequencyLookup.DecompressData(this);
				}

				return _data;
			}
		}

		public CObject Object
		{
			get
			{
				if(_object == null)
				{
					_object = Parser.processStreamObject(Data);
				}

				return _object;
			}
		}
    }
}
