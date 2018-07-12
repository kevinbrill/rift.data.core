using System;
namespace Assets.Database
{
    public class Entry
    {
        public Entry(long id, long key, string name, byte[] data)
        {
			Id = id;
			Key = key;
			Name = name;
			Data = data;
        }

		public long Id { get; set; }
		public long Key { get; set; }
		public string Name { get; set; }
		public byte[] Data { get; set; }
    }
}
