using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Fededim.Extensions.Configuration.Protected
{
    /// <summary>
    /// ConfigurationBuilderExtensions is a static class providing different extensions methods to IConfigurationBuilder and IProtectProviderConfigurationData interfaces
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
        /// Perform wildcard search of files in path encrypting any data using the specified <see cref="protectProviderConfigurationData"/>
        /// </summary>
        /// <param name="protectProviderConfigurationData">an IProtectProviderConfigurationData interface obtained from a one of the supported providers</param>
        /// <param name="path">directory to be searched</param>
        /// <param name="searchPattern">wildcard pattern to filter files</param>
        /// <param name="searchOption">search options</param>
        /// <param name="backupOriginalFile">boolean which indicates whether to make a backupof original file with extension .bak</param>
        /// <returns>a list of filenames which have been successfully encrypted</returns>
        /// <exception cref="ArgumentException"></exception>
        public static IList<String> ProtectFiles(this IProtectProviderConfigurationData protectProviderConfigurationData, String path, String searchPattern = "*.json", SearchOption searchOption = SearchOption.TopDirectoryOnly, bool backupOriginalFile = true)
        {
            var result = new List<String>();

            foreach (var f in Directory.EnumerateFiles(path, searchPattern, searchOption))
            {
                if (protectProviderConfigurationData.ProtectFile(f, backupOriginalFile))
                    result.Add(f);
            }

            return result;
        }




        /// <summary>
        /// Encrypts a single file using the specified <see cref="protectProviderConfigurationData"/>
        /// </summary>
        /// <param name="protectProviderConfigurationData">an IProtectProviderConfigurationData interface obtained from a one of the supported providers</param>
        /// <param name="path">the filename to encrypt</param>
        /// <param name="backupOriginalFile">boolean which indicates whether to make a backupof original file with extension .bak</param>
        /// <returns>true if filename has been successfully encrypted, false otherwise</returns>
        /// <exception cref="ArgumentException"></exception>
        public static bool ProtectFile(this IProtectProviderConfigurationData protectProviderConfigurationData, String filename, bool backupOriginalFile = true)
        {
            protectProviderConfigurationData.CheckConfigurationIsValid();

            var fileContent = File.ReadAllText(filename);
            var replacedContent = fileContent;

            foreach (var protectFileOption in ProtectFilesOptions)
                if (protectFileOption.FilenameRegex.Match(filename).Success)
                {
                    replacedContent = protectFileOption.ProtectFileProcessor.ProtectFile(fileContent, protectProviderConfigurationData.ProtectRegex, (key, value) => ProtectConfigurationValue(protectProviderConfigurationData, key, value));
                    break;
                }

            if (replacedContent != fileContent)
            {
                if (backupOriginalFile)
                    File.Copy(filename, filename + ".bak", true);

                File.WriteAllText(filename, replacedContent);

                return true;
            }

            return false;
        }





        /// <summary>
        /// Encrypts the String value using the specified <see cref="protectProviderConfigurationData"/>
        /// </summary>
        /// <param name="protectProviderConfigurationData">an IProtectProviderConfigurationData interface obtained from a one of the supported providers</param>
        /// <param name="value">a String literal which needs to be encrypted</param>
        /// <returns>the encrypted configuration value</returns>
        /// <exception cref="ArgumentException"></exception>
        public static String ProtectConfigurationValue(this IProtectProviderConfigurationData protectProviderConfigurationData, String key, String value)
        {
            return ProtectConfigurationValueInternal(protectProviderConfigurationData, key, value);
        }



        /// <summary>
        /// internal method actually performing the encryption using the specified <see cref="protectProviderConfigurationData"/>
        /// </summary>
        /// <param name="protectProviderConfigurationData">an IProtectProviderConfigurationData interface obtained from a one of the supported providers</param>
        /// <param name="value">a String literal which needs to be encrypted</param>
        /// <returns></returns>
        private static String ProtectConfigurationValueInternal(IProtectProviderConfigurationData protectProviderConfigurationData, String key, String value)
        {
            if (value == null)
                return null;

            protectProviderConfigurationData.CheckConfigurationIsValid();

            return protectProviderConfigurationData.ProtectRegex.Replace(value, (me) =>
            {
                var subPurposePresent = !String.IsNullOrEmpty(me.Groups["subPurpose"]?.Value);

                var protectProvider = protectProviderConfigurationData.ProtectProvider;

                if (subPurposePresent)
                    protectProvider = protectProviderConfigurationData.ProtectProvider.CreateNewProviderFromSubkey(key, me.Groups["subPurpose"].Value);

                var encryptedValue = protectProvider.Encrypt(key, me.Groups["protectData"].Value);

                // if the encryption function returns null or empty store the original value unencrypted
                if (String.IsNullOrEmpty(encryptedValue))
                    return me.Groups["protectData"].Value;
                else
                    return protectProviderConfigurationData.ProtectedReplaceString.Replace("${subPurposePattern}", subPurposePresent ? me.Groups["subPurposePattern"].Value : String.Empty).Replace("${protectedData}", encryptedValue);
            });
        }



        /// <summary>
        /// Encrypts the Dictionary<String, String> initialData using the specified <see cref="protectProviderConfigurationData"/> (used for in-memory collections)
        /// </summary>
        /// <param name="protectProviderConfigurationData">an IProtectProviderConfigurationData interface obtained from a one of the supported providers</param>
        /// <param name="initialData">a Dictionary<String, String> whose values need to be encrypted</param>
        public static void ProtectConfigurationValue(this IProtectProviderConfigurationData protectProviderConfigurationData, Dictionary<String, String> initialData)
        {
            if (initialData != null)
                foreach (var key in initialData.Keys.ToList())
                    initialData[key] = protectProviderConfigurationData.ProtectConfigurationValue(key, initialData[key]);
        }



        /// <summary>
        /// Encrypts the IEnumerable<String> arguments using the specified <see cref="protectProviderConfigurationData"/>
        /// </summary>
        /// <param name="protectProviderConfigurationData">an IProtectProviderConfigurationData interface obtained from a one of the supported providers</param>
        /// <param name="arguments">a IEnumerable<String> whose elements need to be encrypted</param>
        /// <returns>a newer encrypted IEnumerable<String></returns>
        public static IEnumerable<String> ProtectConfigurationValue(this IProtectProviderConfigurationData protectProviderConfigurationData, IEnumerable<String> arguments)
        {
            return arguments?.Select(argument => protectProviderConfigurationData.ProtectConfigurationValue(String.Empty, argument));
        }



        /// <summary>
        /// Encrypts the String[] arguments using the specified <see cref="protectProviderConfigurationData"/> (used for command-line arguments)
        /// </summary>
        /// <param name="protectProviderConfigurationData">an IProtectProviderConfigurationData interface obtained from a one of the supported providers</param>
        /// <param name="arguments">a String array whose elements need to be encrypted</param>
        /// <returns>a newer encrypted String[] array</returns>
        public static String[] ProtectConfigurationValue(this IProtectProviderConfigurationData protectProviderConfigurationData, String[] arguments)
        {
            return arguments?.Select(argument => protectProviderConfigurationData.ProtectConfigurationValue(String.Empty, argument)).ToArray();
        }



        /// <summary>
        /// Encrypts all the environment variables using the specified <see cref="protectProviderConfigurationData"/> (used for environment variables)
        /// </summary>
        /// <param name="protectProviderConfigurationData">an IProtectProviderConfigurationData interface obtained from a one of the supported providers</param>
        /// <param name="environmentTarget">a target EnvironmentVariableTarget (e.g. User, Machine, Process)</param>
        public static void ProtectEnvironmentVariables(this IProtectProviderConfigurationData protectProviderConfigurationData, EnvironmentVariableTarget environmentTarget = EnvironmentVariableTarget.User)
        {
            var environmentVariables = Environment.GetEnvironmentVariables(environmentTarget);

            foreach (String key in environmentVariables.Keys)
                if (key.StartsWith($"TID_{Thread.CurrentThread.ManagedThreadId}"))
                    Environment.SetEnvironmentVariable(key, protectProviderConfigurationData.ProtectConfigurationValue(key, environmentVariables[key].ToString()), environmentTarget);
        }




        /// <summary>
        /// WithProtectedConfigurationOptions is a helper method used to override the ProtectedGlobalConfigurationData for a particular provider (e.g. the last one added)
        /// </summary>
        /// <param name="configurationBuilder">the IConfigurationBuilder instance</param>
        /// <param name="protectProviderLocalConfigurationData">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <returns>The <see cref="IConfigurationBuilder"/> interface for method chaining</returns>
        /// <exception cref="ArgumentException">if configurationBuilder is not an instance of ProtectedConfigurationBuilder class</exception>
        public static IConfigurationBuilder WithProtectedConfigurationOptions(this IConfigurationBuilder configurationBuilder, IProtectProviderConfigurationData protectProviderLocalConfigurationData)
        {
            var protectedConfigurationBuilder = configurationBuilder as IProtectedConfigurationBuilder;

            if (protectedConfigurationBuilder != null)
                return protectedConfigurationBuilder.WithProtectedConfigurationOptions(protectProviderLocalConfigurationData);
            else
                throw new ArgumentException("Please use ProtectedConfigurationBuilder instead of ConfigurationBuilder class!", nameof(configurationBuilder));

        }
    }
}
