using Microsoft.Extensions.Configuration;
using Xunit;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Xml.XPath;

namespace Fededim.Extensions.Configuration.Protected.DataProtectionAPITest
{
    public enum LevelMove { Current, Next, Previous };
    public enum DataTypes { Null, String, Integer, Double, Boolean, DateTimeOffset, StringArray, IntegerArray, DoubleArray, BooleanArray, DateTimeOffsetArray }


    public abstract class ProtectedConfigurationBuilderTest
    {
        const int NUMENTRIES = 100;
        const int STRINGMAXLENGTH = 20;
        const int ARRAYMAXLENGTH = 10;

        Random Random { get; } = new Random();

        protected DataTypes[] DataTypesValues = (DataTypes[])Enum.GetValues(typeof(DataTypes));
        protected LevelMove[] LevelMoveValues = (LevelMove[])Enum.GetValues(typeof(LevelMove));
        protected TimeZoneInfo[] TimeZoneInfoValues = TimeZoneInfo.GetSystemTimeZones().ToArray();

        protected IProtectProviderConfigurationData ProtectProviderConfigurationData { get; set; }


        public ProtectedConfigurationBuilderTest(IProtectProviderConfigurationData protectProviderConfigurationData)
        {
            ProtectProviderConfigurationData = protectProviderConfigurationData;
        }


        protected String GenerateRandomString(int len)
        {
            var stringBuilder = new StringBuilder();

            for (int i = 0; i < Random.Next(1, len); i++)
                stringBuilder.Append(Convert.ToChar(Random.Next(32, 126)));

            return stringBuilder.ToString();
        }




        protected Int64 NextInt64(Int64 minValue, Int64 maxValue)
        {
            return minValue + (Int64) ((UInt64) (Random.NextDouble() * ((UInt64) (maxValue - minValue))));
        }



        protected (DataTypes dataType, object Value) GenerateRandomValue()
        {
            var dataType = (DataTypes)DataTypesValues.GetValue(Random.Next(DataTypesValues.Length));

            switch (dataType)
            {
                case DataTypes.Null:
                    return (dataType, null);

                case DataTypes.String:
                    return (dataType, GenerateRandomString(STRINGMAXLENGTH).Replace("}", "|"));

                case DataTypes.Integer:
                    return (dataType, NextInt64(Int64.MinValue, Int64.MaxValue));

                case DataTypes.DateTimeOffset:
                    return (dataType, new DateTimeOffset(NextInt64(DateTimeOffset.MinValue.Ticks, DateTimeOffset.MaxValue.Ticks), TimeZoneInfoValues[Random.Next(TimeZoneInfoValues.Length)].BaseUtcOffset));

                case DataTypes.Double:
                    return (dataType, NextInt64(Int64.MinValue, Int64.MaxValue) * Random.NextDouble());

                case DataTypes.Boolean:
                    return (dataType, (Random.Next() % 2 == 0) ? true : false);

                case DataTypes.StringArray:
                    var stringArray = new String[Random.Next(0, ARRAYMAXLENGTH)];
                    for (int i = 0; i < stringArray.Length; i++)
                    {
                        stringArray[i] = GenerateRandomString(STRINGMAXLENGTH).Replace("}", "|"); ;
                    }
                    return (dataType, stringArray);

                case DataTypes.BooleanArray:
                    var booleanArray = new Boolean[Random.Next(0, ARRAYMAXLENGTH)];
                    for (int i = 0; i < booleanArray.Length; i++)
                        booleanArray[i] = (Random.Next() % 2 == 0) ? true : false;
                    return (dataType, booleanArray);

                case DataTypes.IntegerArray:
                    var integerArray = new Int64[Random.Next(0, ARRAYMAXLENGTH)];
                    for (int i = 0; i < integerArray.Length; i++)
                        integerArray[i] = NextInt64(Int64.MinValue, Int64.MaxValue);
                    return (dataType, integerArray);

                case DataTypes.DateTimeOffsetArray:
                    var dateTimeArray = new DateTimeOffset[Random.Next(0, ARRAYMAXLENGTH)];
                    for (int i = 0; i < dateTimeArray.Length; i++)
                        dateTimeArray[i] = new DateTimeOffset(NextInt64(DateTimeOffset.MinValue.Ticks, DateTimeOffset.MaxValue.Ticks), TimeZoneInfoValues[Random.Next(TimeZoneInfoValues.Length)].BaseUtcOffset);
                    return (dataType, dateTimeArray);

                case DataTypes.DoubleArray:
                    var doubleArray = new Double[Random.Next(0, ARRAYMAXLENGTH)];
                    for (int i = 0; i < doubleArray.Length; i++)
                        doubleArray[i] = NextInt64(Int64.MinValue, Int64.MaxValue) * Random.NextDouble();
                    return (dataType, doubleArray);

                default:
                    throw new NotSupportedException($"Datatype not supported {dataType}!");
            }
        }




        protected String PrimitiveEntryValueToString(DataTypes dataType, object value)
        {
            if (value == null)
                return null;

            switch (dataType)
            {
                case DataTypes.DateTimeOffset:
                case DataTypes.DateTimeOffsetArray:
                    return ((DateTimeOffset)value).ToString("O");

                case DataTypes.Boolean:
                case DataTypes.BooleanArray:
                    return String.Format(CultureInfo.InvariantCulture, "{0}", value).ToLower();

                default:
                    return String.Format(CultureInfo.InvariantCulture, "{0}", value);
            }
        }



        protected String CreateProtectValue(string subPurpose, DataTypes dataType, object value)
        {
            if (value == null)
                return null;

            return (subPurpose != null) ? $"Protect:{{{subPurpose}}}:{{{PrimitiveEntryValueToString(dataType, value)}}}" : $"Protect:{{{PrimitiveEntryValueToString(dataType, value)}}}";
        }



        [Fact]
        public void JsonFilesTest()
        {
            String subPurpose;

            // Generate hierarchical JSON file with random NUMENTRIES in both datatype and value
            var jsonObject = new JsonObject();

            var currentNode = jsonObject;

            int level = 1;

            for (int i = 0; i < NUMENTRIES; i++)
            {
                var levelMove = (LevelMove)LevelMoveValues.GetValue(Random.Next(LevelMoveValues.Length));

                var entryValue = GenerateRandomValue();
                var entryKey = $"Entry_{i + 1}_{entryValue.dataType}_";

                switch (entryValue.dataType)
                {
                    case DataTypes.Null:
                    case DataTypes.String:
                    case DataTypes.Integer:
                    case DataTypes.Double:
                    case DataTypes.Boolean:
                    case DataTypes.DateTimeOffset:
                        subPurpose = (Random.Next() % 4 == 0) ? GenerateRandomString(8).Replace(":", "*").Replace("}", "|") : null;
                        currentNode[entryKey + "Plaintext"] = JsonValue.Create(entryValue.Value);
                        currentNode[entryKey + "Encrypted"] = JsonValue.Create(CreateProtectValue(subPurpose,entryValue.dataType,entryValue.Value));
                        break;

                    case DataTypes.StringArray:
                    case DataTypes.IntegerArray:
                    case DataTypes.DoubleArray:
                    case DataTypes.BooleanArray:
                    case DataTypes.DateTimeOffsetArray:
                        currentNode[entryKey + "Plaintext"] = new JsonArray(((Array)entryValue.Value).Cast<object>().Select(obj => JsonValue.Create(obj)).ToArray());
                        currentNode[entryKey + "Encrypted"] = new JsonArray(((Array)entryValue.Value).Cast<object>().Select(obj =>
                        {
                            subPurpose = (Random.Next() % 4 == 0) ? GenerateRandomString(8).Replace(":", "*").Replace("}", "|") : null;
                            return JsonValue.Create(CreateProtectValue(subPurpose, entryValue.dataType, obj));
                        }).ToArray());
                        break;
                }

                if (levelMove == LevelMove.Next && level < 62)
                {
                    var nextSubLevelKey = $"Sublevel_{++level}";

                    if (!currentNode.ContainsKey(nextSubLevelKey))
                        currentNode[nextSubLevelKey] = new JsonObject();

                    currentNode = currentNode[nextSubLevelKey].AsObject();
                }
                else if (levelMove == LevelMove.Previous && level > 1)
                {
                    level--;
                    currentNode = currentNode.Parent.AsObject();
                }
            }


            File.WriteAllText("random.json", jsonObject.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true, TypeInfoResolver = new DefaultJsonTypeInfoResolver() }));


            // Encrypts the json file
            Assert.True(ProtectProviderConfigurationData.ProtectProvider.ProtectFiles(".")?.Any());


            // Reads the encrypted json file and checks that all encrypted entries match DefaultProtectedRegexString
            var protectRegex = new Regex(ProtectedConfigurationBuilder.DefaultProtectRegexString);
            var encryptedJsonDocument = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(".\\random.json"));
            foreach (var node in encryptedJsonDocument)
            {
                if (node.Key.Contains("_Encrypted") && node.Value!=null)
                    if (protectRegex.IsMatch(node.Value.ToString()))
                        throw new InvalidDataException($"Found an invalid un-encrypted node Key {node.Key} Value {node.Value}!");
            }


            // load the json with ProtectedConfigurationBuilder
            var configuration = new ProtectedConfigurationBuilder(ProtectProviderConfigurationData).AddJsonFile("random.json").Build();
            
            foreach (var key in ExtractAllKeys(configuration))
            {
                if (key.Contains("_Plaintext"))
                {
                    var encryptedKey = key.Replace("_Plaintext", "_Encrypted");
                    if (configuration[key] != configuration[encryptedKey])
                        throw new InvalidDataException($"Value mismatch: Plaintext Key {key} Value {configuration[key]} Encrypted Key {encryptedKey} Value {configuration[encryptedKey]}");
                }
            }          
        }




        protected List<String> ExtractAllKeys(IConfiguration configurationRoot)
        {
            var result = new List<String>();

            foreach (var key in configurationRoot.GetChildren())
            {
                var childKeys = ExtractAllKeys(key);

                if (childKeys.Any())
                    result.AddRange(childKeys);
                else
                    result.Add(key.Path);
            }

            return result;
        }

    }
}
