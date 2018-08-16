using Assets.DatParser;
using Newtonsoft.Json;

namespace Assets.Language
{
    [JsonObject(MemberSerialization.OptIn)]
    public class LanguageEntry
    {
		string _text;

		public LanguageEntry(int key, byte[] data)
		{
			Key = key;
			CData = data;
		}

        [JsonProperty("key")]
		public int Key { get; set; }

        [JsonProperty("text")]
        public string Text
        {
            get
            {
				if (_text == null)
					_text = ParseText();

				return _text;
            }
        }

        public byte[] CData { get; set; }

		string ParseText()
		{
			var parsedText = string.Empty;

			try
			{
				var obj = Parser.CreateObject(CData);

				parsedText = obj.get(0).get(1).get(0).convert().ToString();
			}
			catch
			{
			}

			return parsedText;			
		}
    }
}
