using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fededim.Extensions.Configuration.Protected
{
    /// <summary>
    /// ConfigurationBuilderExtensions is a static class providing different extensions methods to IConfigurationBuilder and IProtectProvider interfaces
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// the <see cref="ProtectFilesOptions"/> entry for <see cref="JsonProtectFileProcessor"/>
        /// </summary>
        public static ProtectFileOptions JsonProtectFileOption = new ProtectFileOptions(new Regex("(.*)\\.json"), new JsonProtectFileProcessor());

        /// <summary>
        /// the <see cref="ProtectFilesOptions"/> entry for <see cref="JsonWithCommentsProtectFileProcessor"/>
        /// </summary>
        public static ProtectFileOptions JsonWithCommentsProtectFileOption = new ProtectFileOptions(new Regex("(.*)\\.json"), new JsonWithCommentsProtectFileProcessor());

        /// <summary>
        /// the <see cref="ProtectFilesOptions"/> entry for <see cref="XmlProtectFileProcessor"/>
        /// </summary>
        public static ProtectFileOptions XmlProtectFileOption = new ProtectFileOptions(new Regex("(.*)\\.xml"), new XmlProtectFileProcessor());

        /// <summary>
        /// the <see cref="ProtectFilesOptions"/> entry for <see cref="RawProtectFileProcessor"/>, it must always be the last one of the list (the filenameRegex matches everything).
        /// </summary>
        public static ProtectFileOptions RawProtectFileOption = new ProtectFileOptions(new Regex("(.*)"), new RawProtectFileProcessor());

        /// <summary>
        /// It is a list of <see cref="Protected.ProtectFileOptions"/> classes used to specify the custom options for tweaking the behaviour of the <see cref="ProtectFiles"/> method according to the particular filename matching a regular expression.  <br /><br />
        /// This list gets processed in first-in first-out order (FIFO) and stops as soon as a matching is found. By default three types of custom processors are supported:<br/>
        /// - One for JSON files (<see cref="JsonProtectFileOption"/> and <see cref="JsonProtectFileProcessor"/>, you can optionally activate <see cref="JsonWithCommentsProtectFileOption"/> and <see cref="JsonWithCommentsProtectFileProcessor"/> by calling static method <see cref="UseJsonWithCommentsProtectFileOption"/>)<br/>
        /// - One for XML files (<see cref="XmlProtectFileOption"/> and <see cref="XmlProtectFileProcessor"/>)<br/>
        /// - One for RAW files (<see cref="RawProtectFileOption"/> and <see cref="RawProtectFileProcessor"/>)<br/><br />
        /// This list has a public getter so you can add any additional decoding function you want or replace an existing one for your needs.
        /// </summary>
        public static List<ProtectFileOptions> ProtectFilesOptions { get; private set; } = new List<ProtectFileOptions>()
        {
         JsonProtectFileOption,
         XmlProtectFileOption,
         RawProtectFileOption
        };


        /// <summary>
        /// Turns on <see cref="JsonWithCommentsProtectFileProcessor"/> in order to allow the preservation of comments inside JSON files when encrypting by using <see cref="ProtectFiles"/> (e.g. swaps <see cref="JsonProtectFileOption"/> with <see cref="JsonWithCommentsProtectFileOption"/>)
        /// </summary>
        public static void UseJsonWithCommentsProtectFileOption()
        {
            ProtectFilesOptions.Remove(JsonProtectFileOption);
            ProtectFilesOptions.Insert(0, JsonWithCommentsProtectFileOption);
        }



        /// <summary>
        /// Perform wildcard search of files in path encrypting any data enclosed by protectRegexString the inside files with the protectedReplaceString
        /// </summary>
        /// <param name="protectProvider">an IProtectProvider interface obtained from a one of the supported providers</param>
        /// <param name="path">directory to be searched</param>
        /// <param name="searchPattern">wildcard pattern to filter files</param>
        /// <param name="searchOption">search options</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a String expression used to generate the final encrypted String using ${protectedData} as a placeholder parameter for encrypted data and ${subPurposePattern} as a placeholder parameter for the key custom subpurpose</param>
        /// <param name="backupOriginalFile">boolean which indicates whether to make a backupof original file with extension .bak</param>
        /// <returns>a list of filenames which have been successfully encrypted</returns>
        /// <exception cref="ArgumentException"></exception>
        public static IList<String> ProtectFiles(this IProtectProvider protectProvider, String path, String searchPattern = "*.json", SearchOption searchOption = SearchOption.TopDirectoryOnly, String protectRegexString = null, String protectedReplaceString = null, bool backupOriginalFile = true)
        {
            var protectRegex = new Regex(!String.IsNullOrEmpty(protectRegexString) ? protectRegexString : ProtectedConfigurationBuilder.DefaultProtectRegexString);
            if (!protectRegex.GetGroupNames().Contains("protectData"))
                throw new ArgumentException("Regex must contain a group named protectData!", nameof(protectRegexString));

            var result = new List<String>();

            foreach (var f in Directory.EnumerateFiles(path, searchPattern, searchOption))
            {
                var fileContent = File.ReadAllText(f);
                var replacedContent = fileContent;

                foreach (var protectFileOption in ProtectFilesOptions)
                    if (protectFileOption.FilenameRegex.Match(f).Success)
                    {
                        replacedContent = protectFileOption.ProtectFileProcessor.ProtectFile(fileContent, protectRegex, (value) => ProtectConfigurationValue(protectProvider, value, protectRegexString, protectedReplaceString));
                        break;
                    }

                if (replacedContent != fileContent)
                {
                    if (backupOriginalFile)
                        File.Copy(f, f + ".bak", true);

                    File.WriteAllText(f, replacedContent);

                    result.Add(f);
                }
            }

            return result;
        }






        /// <summary>
        /// Encrypts the String value using the specified protectProvider
        /// </summary>
        /// <param name="protectProvider">an IProtectProvider interface obtained from a one of the supported providers</param>
        /// <param name="value">a String literal which needs to be encrypted</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a String used to replace the protectRegex token with the protected format (e.g. matching the protectRegexString of ProtectedConfigurationBuilder), the encrypted data is injected by using the placeholder ${protectedData} and ${subPurposePattern} as a placeholder parameter for the key custom subpurpose</param>
        /// <returns>the encrypted configuration value</returns>
        /// <exception cref="ArgumentException"></exception>
        public static String ProtectConfigurationValue(this IProtectProvider protectProvider, String value, String protectRegexString = null, String protectedReplaceString = null)
        {
            return ProtectConfigurationValueInternal(protectProvider, value, protectRegexString, protectedReplaceString);
        }



        /// <summary>
        /// internal method actually performing the encryption using the <see cref="protectRegex"/> and <see cref="IProtectProvider"/> interface
        /// </summary>
        /// <param name="protectProvider">an IProtectProvider interface obtained from a one of the supported providers</param>
        /// <param name="value">a String literal which needs to be encrypted</param>
        /// <param name="protectRegex">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a String used to replace the protectRegex token with the protected format (e.g. matching the protectRegexString of ProtectedConfigurationBuilder), the encrypted data is injected by using the placeholder ${protectedData} and ${subPurposePattern} as a placeholder parameter for the key custom subpurpose</param>
        /// <returns></returns>
        private static String ProtectConfigurationValueInternal(IProtectProvider protectProvider, String value, String protectRegexString, String protectedReplaceString)
        {
            protectedReplaceString = !String.IsNullOrEmpty(protectedReplaceString) ? protectedReplaceString : ProtectedConfigurationBuilder.DefaultProtectedReplaceString;
            var protectRegex = new Regex(!String.IsNullOrEmpty(protectRegexString) ? protectRegexString : ProtectedConfigurationBuilder.DefaultProtectRegexString);
            if (!protectRegex.GetGroupNames().Contains("protectData"))
                throw new ArgumentException("protectRegexString must contain a group named protectData!", nameof(protectRegexString));


            return protectRegex.Replace(value, (me) =>
            {
                var subPurposePresent = !String.IsNullOrEmpty(me.Groups["subPurpose"]?.Value);

                if (subPurposePresent)
                    protectProvider = protectProvider.CreateNewProviderFromSubkey(me.Groups["subPurpose"].Value);

                return protectedReplaceString.Replace("${subPurposePattern}", subPurposePresent ? me.Groups["subPurposePattern"].Value : String.Empty).Replace("${protectedData}", protectProvider.Encrypt(me.Groups["protectData"].Value));
            });
        }



        /// <summary>
        /// Encrypts the Dictionary<String, String> initialData using the specified protectProvider (used for in-memory collections)
        /// </summary>
        /// <param name="protectProvider">an IProtectProvider interface obtained from a one of the supported providers</param>
        /// <param name="initialData">a Dictionary<String, String> whose values need to be encrypted</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a String used to replace the protectRegex token with the protected format (e.g. matching the protectRegexString of IConfigurationBuilder), the encrypted data is injected by using the placeholder ${protectedData} and ${subPurposePattern} as a placeholder parameter for the key custom subpurpose</param>
        public static void ProtectConfigurationValue(this IProtectProvider protectProvider, Dictionary<String, String> initialData, String protectRegexString = null, String protectedReplaceString = null)
        {
            if (initialData != null)
                foreach (var key in initialData.Keys.ToList())
                    initialData[key] = protectProvider.ProtectConfigurationValue(initialData[key], protectRegexString, protectedReplaceString);
        }



        /// <summary>
        /// Encrypts the IEnumerable<String> arguments using the specified protectProvider
        /// </summary>
        /// <param name="protectProvider">an IProtectProvider interface obtained from a one of the supported providers</param>
        /// <param name="arguments">a IEnumerable<String> whose elements need to be encrypted</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a String used to replace the protectRegex token with the protected format (e.g. matching the protectRegexString of IConfigurationBuilder), the encrypted data is injected by using the placeholder ${protectedData} and ${subPurposePattern} as a placeholder parameter for the key custom subpurpose</param>
        /// <returns>a newer encrypted IEnumerable<String></returns>
        public static IEnumerable<String> ProtectConfigurationValue(this IProtectProvider protectProvider, IEnumerable<String> arguments, String protectRegexString = null, String protectedReplaceString = null)
        {
            return arguments?.Select(argument => protectProvider.ProtectConfigurationValue(argument, protectRegexString, protectedReplaceString));
        }



        /// <summary>
        /// Encrypts the String[] arguments using the specified protectProvider (used for command-line arguments)
        /// </summary>
        /// <param name="protectProvider">an IProtectProvider interface obtained from a one of the supported providers</param>
        /// <param name="arguments">a String array whose elements need to be encrypted</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a String used to replace the protectRegex token with the protected format (e.g. matching the protectRegexString of IConfigurationBuilder), the encrypted data is injected by using the placeholder ${protectedData} and ${subPurposePattern} as a placeholder parameter for the key custom subpurpose</param>
        /// <returns>a newer encrypted String[] array</returns>
        public static String[] ProtectConfigurationValue(this IProtectProvider protectProvider, String[] arguments, String protectRegexString = null, String protectedReplaceString = null)
        {
            return arguments?.Select(argument => protectProvider.ProtectConfigurationValue(argument, protectRegexString, protectedReplaceString)).ToArray();
        }



        /// <summary>
        /// Encrypts all the environment variables using the specified protectProvider (used for environment variables)
        /// </summary>
        /// <param name="protectProvider">an IProtectProvider interface obtained from a one of the supported providers</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a String used to replace the protectRegex token with the protected format (e.g. matching the protectRegexString of IConfigurationBuilder), the encrypted data is injected by using the placeholder ${protectedData} and ${subPurposePattern} as a placeholder parameter for the key custom subpurpose</param>
        public static void ProtectEnvironmentVariables(this IProtectProvider protectProvider, String protectRegexString = null, String protectedReplaceString = null)
        {
            var environmentVariables = Environment.GetEnvironmentVariables();

            if (environmentVariables != null)
                foreach (String key in environmentVariables.Keys)
                    Environment.SetEnvironmentVariable(key, protectProvider.ProtectConfigurationValue(environmentVariables[key].ToString(), protectRegexString, protectedReplaceString));
        }
    }
}
