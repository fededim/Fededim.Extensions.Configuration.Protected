using System;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Collections.Generic;

namespace Fededim.Extensions.Configuration.Protected
{
    /// <summary>
    /// This is the interface which must be implemented by a custom FileProtectProcessor. It contains a single method <see cref="ProtectFile"/> used to decode, encrypt and re-encode the input file and return it as string.
    /// </summary>
    public interface IFileProtectProcessor
    {
        /// <summary>
        /// This method actually implements a custom FileProtectProcessor which must decode, encrypt and re-encode the input file and return it as string.
        /// </summary>
        /// <param name="rawFileText">The is the raw input file as a string</param>
        /// <param name="protectRegex">This is the configured protected regex which must be matched in file values in order to choose whether to encrypt or not the data.</param>
        /// <param name="protectFunction">This is the protect function taking the plaintext data as input and producing encrypted base64 data as output</param>
        /// <returns>the encrypted re-encoded file as a string</returns>
        String ProtectFile(String rawFileText, Regex protectRegex, Func<String, String> protectFunction);
    }



    /// <summary>
    /// ProtectFilesOptions is a class used to specify the custom ProtectFileProcessors used by <see cref="ConfigurationBuilderExtensions.ProtectFiles"/> method. Essentially for each processor you must provide a class <see cref="FileProtectProcessor"/> which must implement <br/>
    /// the <see cref="IFileProtectProcessor"/> interface in order to process the raw string data of the input file according to its format conventions (<see cref="JsonFileProtectProcessor"/> and <see cref="XmlFileProtectProcessor"/>) when the filename matches <br/>
    /// the provided regular expression (<see cref="FilenameRegex"/>)
    /// </summary>
    public class FilesProtectOptions
    {
        /// <summary>
        /// Specifies the regex on the filename which if matched applies the associated FileProcessorFunction
        /// </summary>
        public Regex FilenameRegex { get; private set; }

        /// <summary>
        /// Specifies the FileProtectProcessor class implementing the <see cref="IFileProtectProcessor"/> interface used to decode, encrypt and re-encode the input file and return it as string.
        /// </summary>
        public IFileProtectProcessor FileProtectProcessor { get; private set; }


        public FilesProtectOptions(Regex filenameRegex, IFileProtectProcessor protectFileProcessor)
        {
            FilenameRegex = filenameRegex;
            FileProtectProcessor = protectFileProcessor;
        }
    }



    /// <summary>
    /// A raw file processor which parses and writes back input files as plain raw text files (no conventions are used).
    /// </summary>
    public class RawFileProtectProcessor : IFileProtectProcessor
    {
        /// <summary>
        /// Please see <see cref="IFileProtectProcessor.ProtectFile"/> interface
        /// </summary>
        /// <param name="rawFileText">The is the raw input file as a string</param>
        /// <param name="protectRegex">This is the configured protected regex which must be matched in file values in order to choose whether to encrypt or not the data.</param>
        /// <param name="protectFunction">This is the protect function taking the plaintext data as input and producing encrypted base64 data as output</param>
        /// <returns>the encrypted re-encoded file as a string</returns>
        public String ProtectFile(String rawFileText, Regex protectRegex, Func<String, String> ProtectFunction)
        {
            rawFileText = ProtectFunction(rawFileText);

            return rawFileText;
        }
    }



    /// <summary>
    /// A JSON file processor which parses and writes back input files according to the JSON file format, e.g. converts \\ into \, \u0022 into ", etc.
    /// </summary>
    public class JsonFileProtectProcessor : IFileProtectProcessor
    {
        protected JsonSerializerOptions JsonSerializerOptions { get; set; }
        protected JsonNodeOptions JsonNodeOptions { get; set; }
        protected JsonDocumentOptions JsonDocumentOptions { get; set; }


        /// <summary>
        /// JsonFileProtectProcessor constructor accepting a JsonSerializerOptions. <br/><br/>
        /// JSON files in contrast to XML files must not have comments according to the standard, but this constraint can be relaxed by setting JsonCommentHandling property in the <see cref="jsonSerializerOptions"/> <br/>
        /// Since NET Core 3.1 JsonCommentHandling.Allow option always raises an exception (see <see href="https://github.com/dotnet/runtime/blob/main/src/libraries/System.Text.Json/src/System/Text/Json/Document/JsonDocumentOptions.cs#L27-L38"/> and <see href="https://github.com/dotnet/runtime/blob/main/src/libraries/System.Text.Json/src/System/Text/Json/Document/JsonDocument.cs#L1083-L1094"/>), so we set by default to allow comments but to skip them (JsonCommentHandling.Skip) in order to parse the file. <br/>
        /// Moreover JsonSerializer generates always strict JSON files, so it won't output any comments in the encrypted re-encoded file. <br/><br/>
        /// By default we set ReadCommentHandling = JsonCommentHandling.Skip and WriteIndented = true 
        /// </summary>
        /// <param name="jsonSerializerOptions">a custom JsonSerializerOptions if you want to override the default one</param>
        public JsonFileProtectProcessor(JsonSerializerOptions jsonSerializerOptions = null)
        {
            jsonSerializerOptions = jsonSerializerOptions ?? new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, WriteIndented = true };

            JsonSerializerOptions = jsonSerializerOptions;
            JsonNodeOptions = new JsonNodeOptions { PropertyNameCaseInsensitive = jsonSerializerOptions.PropertyNameCaseInsensitive };
            JsonDocumentOptions = new JsonDocumentOptions { CommentHandling = jsonSerializerOptions.ReadCommentHandling, AllowTrailingCommas = jsonSerializerOptions.AllowTrailingCommas, MaxDepth = jsonSerializerOptions.MaxDepth };
        }


        /// <summary>
        /// Please see <see cref="IFileProtectProcessor.ProtectFile"/> interface
        /// </summary>
        /// <param name="rawFileText">The is the raw input file as a string</param>
        /// <param name="protectRegex">This is the configured protected regex which must be matched in file values in order to choose whether to encrypt or not the data.</param>
        /// <param name="protectFunction">This is the protect function taking the plaintext data as input and producing encrypted base64 data as output</param>
        /// <returns>the encrypted re-encoded file as a string</returns>
        public String ProtectFile(String rawFileText, Regex protectRegex, Func<String, String> protectFunction)
        {
            // Loads the JSON file
            var document = JsonNode.Parse(rawFileText, JsonNodeOptions, JsonDocumentOptions);

            // extract and encrypts all string node values
            // extraction must be done first because if you change any value while enumerating the collection it raises InvalidOperationException
            foreach (var node in ExtractAllStringNodes(document, protectRegex, protectFunction))
            {
                String value = node.GetValue<String>();

                // encrypts node value if it matches the regex
                if (protectRegex.IsMatch(value))
                {
                    var parent = node.Parent;
                    var parentType = parent.GetValueKind();

                    // to change the actual value you have to differentiate if the parent node is a JSON object or a JSON array
                    if (parentType == JsonValueKind.Object)
                    {
                        parent[node.GetPropertyName()] = protectFunction(value);
                    }
                    else if (parentType == JsonValueKind.Array)
                    {
                        parent[node.GetElementIndex()] = protectFunction(value);
                    }
                }
            }

            // returns back the encrypted json file
            return document.ToJsonString(JsonSerializerOptions);
        }



        /// <summary>
        /// Helper method which extracts all string nodes from the JSON document. It implements a recursive DFS of the JSON parsed tree.
        /// </summary>
        /// <param name="node">the actual node which must be visited, it starts with the root node</param>
        /// <param name="protectRegex">This is the configured protected regex which must be matched in file values in order to choose whether to encrypt or not the data.</param>
        /// <param name="protectFunction">This is the protect function taking the plaintext data as input and producing encrypted base64 data as output</param>
        /// <returns>list of all string nodes</returns>
        protected List<JsonNode> ExtractAllStringNodes(JsonNode node, Regex protectRegex, Func<String, String> protectFunction)
        {
            var result = new List<JsonNode>();

            var nodeType = node.GetValueKind();

            if (nodeType == JsonValueKind.Object)
            {
                foreach (var innerNodes in node.AsObject())
                    if (innerNodes.Value != null)
                        result.AddRange(ExtractAllStringNodes(innerNodes.Value, protectRegex, protectFunction));
            }
            else if (nodeType == JsonValueKind.Array)
            {
                foreach (var innerNodes in node.AsArray())
                    if (innerNodes != null)
                        result.AddRange(ExtractAllStringNodes(innerNodes, protectRegex, protectFunction));
            }
            else if (nodeType == JsonValueKind.String)
            {
                result.Add(node);
            }

            return result;
        }
    }



    /// <summary>
    /// A JSON file processor which parses and writes back input files according to the JSON file format, e.g. converts \\ into \, \u0022 into ", etc. supporting also comments
    /// </summary>
    public class JsonWithCommentsFileProtectProcessor : IFileProtectProcessor
    {
        protected JsonSerializerOptions JsonSerializerOptions { get; set; }
        protected JsonReaderOptions JsonReaderOptions { get; set; }
        protected JsonWriterOptions JsonWriterOptions { get; set; }


        /// <summary>
        /// JsonWithCommentsFileProtectProcessor constructor accepting a JsonSerializerOptions. <br/><br/>
        /// Comments are not supported in the JSON standard <see cref="JsonFileProtectProcessor.JsonFileProtectProcessor" /> <br/>
        /// This is a kind of hack since in order to support comments we just need to treat the input file a rawFileText, matching the protectRegex, decoding any value before calling protectFunction and re-encoding the encrypted value after calling protectFunction. <br/><br/>
        /// By default we set ReadCommentHandling = JsonCommentHandling.Skip and WriteIndented = true <br/>
        /// </summary>
        /// <param name="jsonSerializerOptions">a custom JsonSerializerOptions if you want to override the default one</param>
        public JsonWithCommentsFileProtectProcessor(JsonSerializerOptions jsonSerializerOptions = null)
        {
            JsonSerializerOptions = jsonSerializerOptions ?? new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, WriteIndented = true };
            JsonReaderOptions = new JsonReaderOptions { AllowTrailingCommas = JsonSerializerOptions.AllowTrailingCommas, MaxDepth = JsonSerializerOptions.MaxDepth };
            JsonWriterOptions = new JsonWriterOptions { Indented = JsonSerializerOptions.WriteIndented, MaxDepth = JsonSerializerOptions.MaxDepth };
        }


        /// <summary>
        /// Please see <see cref="IFileProtectProcessor.ProtectFile"/> interface
        /// </summary>
        /// <param name="rawFileText">The is the raw input file as a string</param>
        /// <param name="protectRegex">This is the configured protected regex which must be matched in file values in order to choose whether to encrypt or not the data.</param>
        /// <param name="protectFunction">This is the protect function taking the plaintext data as input and producing encrypted base64 data as output</param>
        /// <returns>the encrypted re-encoded file as a string</returns>
        public String ProtectFile(String rawFileText, Regex protectRegex, Func<String, String> protectFunction)
        {
            return protectRegex.Replace(rawFileText, me =>
            {
                var utf8JsonReader = new Utf8JsonReader(Encoding.UTF8.GetBytes($"\"{me.Value}\"").AsSpan(), JsonReaderOptions);
                var reencodedJsonMemoryStream = new MemoryStream();
                var utf8JsonWriter = new Utf8JsonWriter(reencodedJsonMemoryStream, JsonWriterOptions);

                if (utf8JsonReader.Read())
                {
                    utf8JsonWriter.WriteStringValue(protectFunction(utf8JsonReader.GetString()));
                    utf8JsonWriter.Flush();
                    return Encoding.UTF8.GetString(reencodedJsonMemoryStream.ToArray()).Replace("\"",String.Empty);
                }
                else
                    throw new JsonException($"Invalid JSON value: {me.Value}!");
            });
        }
    }



    /// <summary>
    /// A XML file processor which parses and writes back input files according to the XML file format, e.g. converts <![CDATA[&amp; into &, &gt; into >]]>, etc.
    /// </summary>
    public class XmlFileProtectProcessor : IFileProtectProcessor
    {
        protected LoadOptions LoadOptions { get; set; }
        protected SaveOptions? SaveOptions { get; set; }

        /// <summary>
        /// XmlFileProtectProcessor constructor accepting a LoadOptions and/or SaveOptions.
        /// By default we set LoadOptions.None and SaveOptions are instead taken from XML annotations (e.g. the first parent node with such SaveOptions annotation,
        /// for XML annotations you can see GetSaveOptionsFromAnnotations method inside XLinq.cs <see href="https://github.com/microsoft/referencesource/blob/master/System.Xml.Linq/System/Xml/Linq/XLinq.cs#L1303-L1318"/> )
        /// </summary>
        /// <param name="loadOptions">a custom LoadOptions if you want to override the default one</param>
        /// <param name="saveOptions">a custom SaveOptions if you want to override the default one</param>

        public XmlFileProtectProcessor(LoadOptions loadOptions = LoadOptions.None, SaveOptions? saveOptions = null)
        {
            LoadOptions = loadOptions;
            SaveOptions = saveOptions;
        }

        /// <summary>
        /// Please see <see cref="IFileProtectProcessor.ProtectFile"/> interface
        /// </summary>
        /// <param name="rawFileText">The is the raw input file as a string</param>
        /// <param name="protectRegex">This is the configured protected regex which must be matched in file values in order to choose whether to encrypt or not the data.</param>
        /// <param name="protectFunction">This is the protect function taking the plaintext data as input and producing encrypted base64 data as output</param>
        /// <returns>the encrypted re-encoded file as a string</returns>
        public String ProtectFile(String rawFileText, Regex protectRegex, Func<String, String> protectFunction)
        {
            // Loads the XML File
            var document = XDocument.Parse(rawFileText, LoadOptions);

            ProtectXmlNodes(document.Root, protectRegex, protectFunction);

            // returns back the encrypted xml file
            using (var xmlBytes = new MemoryStream())
            {
                if (SaveOptions.HasValue)
                    document.Save(xmlBytes, SaveOptions.Value);  // use the SaveOptions specified in constructor
                else
                    document.Save(xmlBytes);   // use the SaveOptions from XML Annotations

                return Encoding.UTF8.GetString(xmlBytes.ToArray());
            }
        }



        /// <summary>
        /// Helper method which protects a node, all its attributes and its nested elements of the XML document. It implements a recursive DFS of the XML parsed tree.
        /// </summary>
        /// <param name="node">the actual node which must be visited, it starts with the root node</param>
        /// <param name="protectRegex">This is the configured protected regex which must be matched in file values in order to choose whether to encrypt or not the data.</param>
        /// <param name="protectFunction">This is the protect function taking the plaintext data as input and producing encrypted base64 data as output</param>
        /// <returns>list of all string nodes</returns>
        private static void ProtectXmlNodes(XElement element, Regex protectRegex, Func<string, string> protectFunction)
        {
            String value;

            // protects all element attribute values
            foreach (var attribute in element.Attributes())
            {
                value = attribute.Value;
                if (protectRegex.IsMatch(value))
                    attribute.Value = protectFunction(value);
            }

            if (element.HasElements)
            {
                // recursively protects nested elements
                foreach (var nestedElement in element.Descendants())
                    ProtectXmlNodes(nestedElement, protectRegex, protectFunction);

            }
            else
            {
                // protects element value if it has no children elements
                value = element.Value;
                if (protectRegex.IsMatch(value))
                    element.Value = protectFunction(value);
            }
        }
    }
}
