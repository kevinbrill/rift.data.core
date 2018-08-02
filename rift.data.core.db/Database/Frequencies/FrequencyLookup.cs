using System.Collections.Generic;
using System.Linq;

namespace Assets.Database.Frequencies
{
	public static class FrequencyLookup
    {
	 	static Dictionary<long, Frequency> _frequencies = new Dictionary<long, Frequency>();

		static FrequencyLookup()
        {
			var repository = new TelaraDbSqliteRepository();

			_frequencies = repository.GetFrequencies().ToDictionary(x => x.DatasetId);
        }

		public static Frequency Get(long datasetId)
		{
			return _frequencies[datasetId];
		}
    }
}
