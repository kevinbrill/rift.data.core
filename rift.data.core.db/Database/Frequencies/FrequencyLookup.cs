using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Assets.Database.Frequencies
{
	public static class FrequencyLookup
    {
	 	static Dictionary<long, Frequency> _frequencies = new Dictionary<long, Frequency>();
		static Dictionary<long, HuffmanReader> _readers = new Dictionary<long, HuffmanReader>();

		static FrequencyLookup()
        {
			var repository = new TelaraDbSqliteRepository();

			_frequencies = repository.GetFrequencies().ToDictionary(x => x.DatasetId);
        }

		public static Frequency GetFrequency(long datasetId)
		{
			return _frequencies[datasetId];
		}

		public static HuffmanReader GetReader(long datasetId)
		{
			if(!_readers.ContainsKey(datasetId))
			{
				var frequency = GetFrequency(datasetId);
				var reader = new HuffmanReader(frequency.Frequencies);

				_readers.Add(datasetId, reader);
			}

			return _readers[datasetId];
		}

		public static byte[] DecompressData(Entry entry)
		{
			var reader = GetReader(entry.DatasetId);

			var memoryStream = new MemoryStream(entry.CompressedData);

			// Compute the size of the uncompressed data
			int uncompressedSize = RiftAssets.Util.readUnsignedLeb128_X(memoryStream);

			var compressedStream = new MemoryStream();

			memoryStream.WriteTo(compressedStream);

			byte[] compressedByes = compressedStream.ToArray();

			return reader.Read(compressedByes, compressedByes.Length, uncompressedSize);
		}
    }
}
