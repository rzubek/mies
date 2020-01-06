using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mies
{
    internal static class YamlUtils
    {
        /// <summary>
        /// Takes a markdown file that has a yaml header delimited by "---" lines,
        /// and returns the header as an instance of the specific class.
        /// </summary>
        internal static T ExtractYamlHeader<T> (FileInfo filename, string markdown) {
            string header = ExtractHeaderText(filename, markdown);
            return ParseYaml<T>(filename, header);
        }

        /// <summary>
        /// Takes a markdown file that has a header delimited by "---" lines,
        /// and returns just the text of that header.
        ///
        /// It assumes that the first line of the file is the delimiter,
        /// and reads everything up to the last line before the second delimiter.
        /// </summary>
        private static string ExtractHeaderText (FileInfo filename, string markdown) {
            var lines = markdown.Split(new char[] { '\r', '\n' })
                .Where(line => !string.IsNullOrWhiteSpace(line));

            var first = lines.FirstOrDefault();
            if (first.StartsWith("---")) {
                StringBuilder sb = new StringBuilder();
                var next = lines.Skip(1);
                foreach (var line in next) {
                    if (line.StartsWith("---")) { return sb.ToString(); }
                    sb.AppendLine(line);
                }
            }

            throw new InvalidOperationException($"Markdown file missing a header block: {filename.Name}");
        }

        // This class is more than a bit of a hack. :) I wanted to convert yaml files into
        // strongly typed class instances, but the sharpyaml library that I like is case sensitive,
        // while I wanted the md files to use lowercase header field names.
        // So like the shameless person that I am, I just deserialize things into a hash table and then
        // push it through the excellent newtonsoft serializer which is case insensitive. :)
        internal static T ParseYaml<T> (FileInfo filename, string yaml) {
            Dictionary<object, object> data;

            try {
                var serializer = new SharpYaml.Serialization.Serializer();
                data = (Dictionary<object, object>)serializer.Deserialize(yaml);
            } catch (Exception e) {
                throw new InvalidOperationException($"Error while parsing yaml header for {filename.Name}", e);
            }

            try {
                var serialized = JsonConvert.SerializeObject(data);
                var deserialized = JsonConvert.DeserializeObject<T>(serialized);
                return deserialized;
            } catch (Exception e) {
                throw new InvalidOperationException($"Error recognizing header variables for {filename.Name}", e);
            }
        }
    }
}
