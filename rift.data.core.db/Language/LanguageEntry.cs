using Assets.DatParser;

namespace Assets.Language
{
    public class LanguageEntry
    {
		string _text;

		public LanguageEntry(int key, byte[] data)
		{
			Key = key;
			CData = data;
		}

		public int Key { get; set; }
        public byte[] CData { get; set; }

        public string Text
        {
            get
            {
				if (_text == null)
					_text = ParseText();

				return _text;
            }
        }

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
