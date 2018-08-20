using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace rift.data.core.Model
{
    public class DataModel
    {
        static readonly Regex classRegex = new Regex(@"(class) (.*)<(\d*)>");
        static readonly Regex propertyRegex = new Regex(@"<(\d{1,2}),([A-Fa-f0-9]+)> (...*) (\w*)$");

        public DataModel()
        {
            Classes = new Dictionary<int, Class>();
        }

        public Dictionary<int, Class> Classes
        {
            get;
            private set;
        }

        public string ToJson()
        {
            using (var writer = new StringWriter())
            {
                var serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented
                };

                ToJson(writer);

                return writer.ToString();
            }
        }

        public void ToJson(TextWriter writer)
        {
            var serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented
            };

            serializer.Serialize(writer, Classes.Values);
        }

        public void Load(string filename) 
        {
            using(var streamReader = new StreamReader(filename))
            {
                string currentLine = streamReader.ReadLine();
                Class currentClass = null;

                while ( currentLine != null)
                {
                    if (classRegex.IsMatch(currentLine))
                    {
                        var match = classRegex.Match(currentLine);
                        var code = int.Parse(match.Groups[3].Value);
                        var name = match.Groups[2].Value;

                        currentClass = new Class(code, name);
                    }
                    else if (propertyRegex.IsMatch(currentLine))
                    {
                        var match = propertyRegex.Match(currentLine);

                        var index = int.Parse(match.Groups[1].Value);
                        var type = match.Groups[3].Value;
                        var name = match.Groups[4].Value;

                        var property = new Property
                        {
                            Name = name,
                            Type = type,
                            Index = index
                        };

                        currentClass.Properties.Add(property);
                    }
                    else if(currentLine.StartsWith("}", StringComparison.CurrentCulture))
                    {
                        Classes.Add(currentClass.Code, currentClass);
                    }
                        
                    currentLine = streamReader.ReadLine();
                }
            }
        }
    }
}
