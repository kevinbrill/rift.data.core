using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Assets.Language;
using Assets.RiftAssets;
using log4net;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Assets.Database
{
    public class TelaraDbFactory
    {
        static readonly ILog logger = LogManager.GetLogger(typeof(TelaraDbFactory));
		const string KEY = "IoooW3zsQgm22XaVQ0YONAKehPyJqEyaoQ7sEqf1XDc=";

        Task loadTask;
        CancellationToken token;
        CancellationTokenSource tokenSource;

        public AssetDatabase AssetDatabase { get; set; }
        public LanguageMap LanguageMap { get; set; }

        public async Task Load()
        {
            if (loadTask != null && loadTask.Status == TaskStatus.Running)
            {
                logger.Warn("Scanner is already running");

                return;
            }

            try
            {
                tokenSource = new CancellationTokenSource();
                token = tokenSource.Token;

                // Load the database
                loadTask = await Task.Factory.StartNew(LoadDatabase, token);
            }
            catch (Exception ex)
            {
                logger.Error("An unexpected error occurred loading the database", ex);
            }
        }

        async Task LoadDatabase()
        {
            var stopwatch = new System.Diagnostics.Stopwatch();

            stopwatch.Start();

			logger.Info("Loading the database out of the asset files");

			// Get the file bytes
			var encryptedBytes = AssetDatabase.extractUsingFilename("telara.db");

			logger.Info($"Loaded {encryptedBytes.LongLength} bytes.  Decrypting now.");

			var decryptedBytes = DecryptSqliteData(encryptedBytes);

			logger.Info($"Decrypted the database.  Writing to file.");

			File.WriteAllBytes("/Users/kevin/Desktop/telaradb.db3", decryptedBytes);

            stopwatch.Stop();

            logger.Debug($"Loaded the TelaraDB in {stopwatch.Elapsed}");
        }

		byte[] DecryptSqliteData(byte[] encryptedData)
		{
			try
			{
				byte[] key = Convert.FromBase64String(KEY);

				using (var reader = new BinaryReader(new MemoryStream(encryptedData)))
				{
					reader.BaseStream.Seek(16, SeekOrigin.Begin);

					UInt16 pageSize = (UInt16)IPAddress.NetworkToHostOrder(reader.ReadInt16());

					logger.Debug($"Page size is {pageSize}");

					reader.BaseStream.Seek(0, SeekOrigin.Begin);

					MemoryStream decryptedStream = new MemoryStream(encryptedData.Length);

					int pageCount = encryptedData.Length / pageSize;

					logger.Debug($"Given the page size of {pageSize}, there are {pageCount} pages");

					for (int i = 1; i < pageCount + 1; i++)
					{
						byte[] iv = GetIV(i);
						BufferedBlockCipher cipher = new BufferedBlockCipher(new OfbBlockCipher(new AesEngine(), 128));
						ICipherParameters cparams = new ParametersWithIV(new KeyParameter(key), iv);
						cipher.Init(false, cparams);

						byte[] bdata = reader.ReadBytes(pageSize);
						byte[] ddata = new byte[pageSize];
						cipher.ProcessBytes(bdata, 0, bdata.Length, ddata, 0);

						// bytes 16-23 on the first page are NOT encrypted, 
						// so we need to replace them once we decrypt the page
						if (i == 1)
						{
							for (int x = 16; x <= 23; x++)
							{
								ddata[x] = bdata[x];
							}
						}

						decryptedStream.Write(ddata, 0, ddata.Length);
					}

					decryptedStream.Seek(0, SeekOrigin.Begin);

					logger.Debug("Successfully decrypted the database");

					return decryptedStream.ToArray();
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex);
				throw ex;
			}
		}

        static byte[] GetIV(int i)
        {
            byte[] iv = new byte[16];
            MemoryStream str = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(str))
            {
                writer.Write((long)i);
                writer.Write(0L);
                writer.Flush();
                str.Seek(0, SeekOrigin.Begin);
                return str.ToArray();
            }
        }
    }
}
