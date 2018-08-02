using System;
using System.Collections.Generic;
using Assets.Database.Frequencies;
using log4net;
using Microsoft.Data.Sqlite;

namespace Assets.Database
{
    public class TelaraDbSqliteRepository
    {
		static readonly ILog logger = LogManager.GetLogger(typeof(TelaraDbSqliteRepository));

		const string connectionString = "Data Source=file:/Users/kevin/Desktop/telaradb.db3";

		public List<Entry> GetEntries()
		{
			var query = $"SELECT * FROM dataset";

			using (var command = new SqliteCommand(query))
			{
				return GetEntriesData(command);
			}
		}

		public List<Entry> GetEntriesForId(long datasetID)
		{
			var query = $"SELECT * FROM dataset WHERE datasetID={datasetID}";

			using (var command = new SqliteCommand(query))
			{
				return GetEntriesData(command);
			}
		}

		public Frequency GetFrequency(long id)
		{
			try
			{
				var query = $"SELECT * FROM dataset_compression WHERE datasetId={id}";

				using (var connection = new SqliteConnection(connectionString))
				{
					connection.Open();

					using (var command = new SqliteCommand(query, connection))
					{
						using (var reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult))
						{
							if (reader.Read())
							{
								return new Frequency
								{
									DatasetId = reader.GetInt64(0),
									Frequencies = (byte[])reader.GetValue(1),
								};
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex);

				throw;
			}

			return null;
		}

		public List<Frequency> GetFrequencies()
		{
			List<Frequency> frequencies = new List<Frequency>();

			try
			{
				var query = $"SELECT * FROM dataset_compression";

				using (var connection = new SqliteConnection(connectionString))
				{
					connection.Open();

					using (var command = new SqliteCommand(query, connection))
					{
						using (var reader = command.ExecuteReader())
						{
							while(reader.Read())
							{
								frequencies.Add(
									new Frequency
									{
										DatasetId = reader.GetInt64(0),
										Frequencies = (byte[])reader.GetValue(1),
									}
								);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex);

				throw;
			}

			return frequencies;			
		}

		List<Entry> GetEntriesData(SqliteCommand command)
		{
			List<Entry> entries = new List<Entry>();

			try
			{
				using (var connection = new SqliteConnection(connectionString))
				{
					connection.Open();

					command.Connection = connection;

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							entries.Add(CreateEntry(reader));
						}
					}
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex);
			}

			return entries;
		}


		Entry CreateEntry(SqliteDataReader reader)
		{
			var id = reader.GetInt64(reader.GetOrdinal("datasetId"));
			var key = reader.GetInt64(reader.GetOrdinal("datasetKey"));
			var name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString(reader.GetOrdinal("name"));
			var data = (byte[])reader.GetValue(reader.GetOrdinal("value"));

			return new Entry(id, key, name, data);
		}
    }
}
