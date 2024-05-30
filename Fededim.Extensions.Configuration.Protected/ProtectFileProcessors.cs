using System;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;

namespace Fededim.Extensions.Configuration.Protected
{
    /// <summary>
    /// This is the interface which must be implemented by a custom FileProtectProcessor. It contains a single method <see cref="ProtectFile"/> used to read, encrypt and return the encrypted file as string.
    /// </summary>
    public interface IFileProtectProcessor
    {
        /// <summary>
        /// This method actually implements a custom FileProtectProcessor which must read, encrypt and return the encrypted file as string.
        /// </summary>
        /// <param name="rawFileText">The is the raw input file as a string</param>
        /// <param name="protectRegex">This is the configured protected regex which must be matched in file values in order to choose whether to encrypt or not the data.</param>
        /// <param name="protectFunction">This is the protect function taking the plaintext data as input and producing encrypted base64 data as output</param>
        /// <returns>the encrypted file as a string</returns>
        String ProtectFile(String rawFileText, Regex protectRegex, Func<String, String> protectFunction);
    }



    /// <summary>
    /// ProtectFilesOptions is a class used to specify the custom ProtectFileProcessors used by <see cref="ConfigurationBuilderExtensions.ProtectFiles"/> method. Essentially for each processor you must provide a class <see cref="FileProtectProcessor"/> which must implement 
    /// the <see cref="IFileProtectProcessor"/> interface in order to process the raw string data of the input file according to its format conventions (<see cref="JsonFileProtectProcessor"/> and <see cref="XmlFileProtectProcessor"/>) when the filename matches 
    /// the provided regular expression (<see cref="FilenameRegex"/>)
    /// </summary>
    public class FilesProtectOptions
    {
        /// <summary>
        /// Specifies the regex on the filename which if matched applies the associated FileProcessorFunction
        /// </summary>
        public Regex FilenameRegex { get; private set; }

        /// <summary>
        /// Specifies the FileProtectProcessor class implementing the <see cref="IFileProtectProcessor"/> interface used to read, encrypt and return the encrypted file as string.
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
        /// <returns>the encrypted file as a string</returns>
        public String ProtectFile(String rawFileText, Regex protectRegex, Func<String, String> ProtectFunction)
        {
            if (protectRegex.IsMatch(rawFileText))
                rawFileText = ProtectFunction(rawFileText);

            return rawFileText;
        }
    }



    /// <summary>
    /// A JSON file processor which parses and writes back input files according to the JSON file format, e.g. converts \\ into \, \u0022 into ", etc.
    /// </summary>
    public class JsonFileProtectProcessor : IFileProtectProcessor
    {
        /// <summary>
        /// Please see <see cref="IFileProtectProcessor.ProtectFile"/> interface
        /// </summary>
        /// <param name="rawFileText">The is the raw input file as a string</param>
        /// <param name="protectRegex">This is the configured protected regex which must be matched in file values in order to choose whether to encrypt or not the data.</param>
        /// <param name="protectFunction">This is the protect function taking the plaintext data as input and producing encrypted base64 data as output</param>
        /// <returns>the encrypted file as a string</returns>
        public String ProtectFile(String rawFileText, Regex protectRegex, Func<String, String> protectFunction)
        {
            // Loads the JSON file
            var document = JsonNode.Parse(rawFileText);

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
            return document.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
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
    /// A XML file processor which parses and writes back input files according to the XML file format, e.g. converts <![CDATA[&amp; into &, &gt; into >]]>, etc.
    /// </summary>
    public class XmlFileProtectProcessor : IFileProtectProcessor
    {
        /// <summary>
        /// Please see <see cref="IFileProtectProcessor.ProtectFile"/> interface
        /// </summary>
        /// <param name="rawFileText">The is the raw input file as a string</param>
        /// <param name="protectRegex">This is the configured protected regex which must be matched in file values in order to choose whether to encrypt or not the data.</param>
        /// <param name="protectFunction">This is the protect function taking the plaintext data as input and producing encrypted base64 data as output</param>
        /// <returns>the encrypted file as a string</returns>
        public String ProtectFile(String rawFileText, Regex protectRegex, Func<String, String> protectFunction)
        {
            // Loads the XML File
            var document = XDocument.Parse(rawFileText);

            ProtectXmlNodes(document.Root, protectRegex, protectFunction);

            // returns back the encrypted xml file
            using (var xmlBytes = new MemoryStream())
            {
                document.Save(xmlBytes);
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
