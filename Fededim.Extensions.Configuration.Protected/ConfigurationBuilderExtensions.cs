using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

namespace Fededim.Extensions.Configuration.Protected
{
    /// <summary>
    /// ConfigurationBuilderExtensions is a static class providing different extensions methods to IConfigurationBuilder and IDataProtect interfaces
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// the <see cref="ProtectFilesOptions"/> entry for <see cref="JsonFileProtectProcessor"/>
        /// </summary>
        public static FilesProtectOptions JsonFileProtectOption = new FilesProtectOptions(new Regex("(.*)\\.json"), new JsonFileProtectProcessor());

        /// <summary>
        /// the <see cref="ProtectFilesOptions"/> entry for <see cref="XmlFileProtectProcessor"/>
        /// </summary>
        public static FilesProtectOptions XmlFileProtectOption = new FilesProtectOptions(new Regex("(.*)\\.xml"), new XmlFileProtectProcessor());

        /// <summary>
        /// the <see cref="ProtectFilesOptions"/> entry for <see cref="RawFileProtectProcessor"/>, it must always be the last one of the list (the filenameRegex matches everything).
        /// </summary>
        public static FilesProtectOptions RawFileProtectOption = new FilesProtectOptions(new Regex("(.*)"), new RawFileProtectProcessor());

        /// <summary>
        /// It is a list of <see cref="Protected.FilesProtectOptions"/> classes used to specify the custom options for tweaking the behaviour of the <see cref="ProtectFiles"/> method according to the particular filename matching a regular expression. 
        /// This list gets processed in first-in first-out order (FIFO) and stops as soon as a matching is found. By default three types of custom processors are supported:<br/>
        /// - One for JSON files (<see cref="JsonFileProtectOption"/> and <see cref="JsonFileProtectProcessor"/>)<br/>
        /// - One for XML files (<see cref="XmlFileProtectOption"/> and <see cref="XmlFileProtectProcessor"/>)<br/>
        /// - One for RAW files (<see cref="RawFileProtectOption"/> and <see cref="RawFileProtectProcessor"/>)<br/>
        /// This list has a public getter so you can add any additional decoding function you want or replace an existing one for your needs.
        /// </summary>
        public static List<FilesProtectOptions> ProtectFilesOptions { get; private set; } = new List<FilesProtectOptions>()
        {
         JsonFileProtectOption,
         XmlFileProtectOption,
         RawFileProtectOption
        };


        /// <summary>
        /// WithProtectedConfigurationOptions is a helper method which allows to override the global protected configuration data specified in the ProtectedConfigurationBuilder for a particular ConfigurationProvider (the last one added)
        /// </summary>
        /// <param name="configurationBuilder">the IConfigurationBuilder instance</param>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="dataProtectionServiceProvider">a service provider configured with Data Protection API, this parameters is mutually exclusive to dataProtectionConfigureAction</param>
        /// <param name="dataProtectionConfigureAction">a configure action to setup the Data Protection API, this parameters is mutually exclusive to dataProtectionServiceProvider</param>
        /// <param name="keyNumber">a number specifying the index of the key to use</param>
        /// <returns>The <see cref="IConfigurationBuilder"/> interface for method chaining</returns>
        /// <exception cref="ArgumentException"></exception>
        public static IConfigurationBuilder WithProtectedConfigurationOptions(this IConfigurationBuilder configurationBuilder, String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, int keyNumber = 1)
        {
            var protectedConfigurationBuilder = configurationBuilder as IProtectedConfigurationBuilder;

            if (protectedConfigurationBuilder != null)
                return protectedConfigurationBuilder.WithProtectedConfigurationOptions(protectedRegexString, dataProtectionServiceProvider, dataProtectionConfigureAction, keyNumber);
            else
                throw new ArgumentException("Please use ProtectedConfigurationBuilder instead of ConfigurationBuilder class!", nameof(configurationBuilder));

        }




        /// <summary>
        /// WithProtectedConfigurationOptions is a helper method which allows to override the global protected configuration data specified in the ProtectedConfigurationBuilder for a particular ConfigurationProvider (the last one added)
        /// </summary>
        /// <param name="configurationBuilder">the IConfigurationBuilder instance</param>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="dataProtectionServiceProvider">a service provider configured with Data Protection API, this parameters is mutually exclusive to dataProtectionConfigureAction</param>
        /// <param name="dataProtectionConfigureAction">a configure action to setup the Data Protection API, this parameters is mutually exclusive to dataProtectionServiceProvider</param>
        /// <param name="keyNumber">a number specifying the index of the key to use</param>
        /// <returns>The <see cref="IConfigurationBuilder"/> interface for method chaining</returns>
        /// <exception cref="ArgumentException"></exception>
        public static IConfigurationBuilder WithProtectedConfigurationOptions(this IConfigurationBuilder configurationBuilder, String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, string purposeString = ProtectedConfigurationBuilder.ProtectedConfigurationBuilderPurpose)
        {
            var protectedConfigurationBuilder = configurationBuilder as IProtectedConfigurationBuilder;

            if (protectedConfigurationBuilder != null)
                return protectedConfigurationBuilder.WithProtectedConfigurationOptions(protectedRegexString, dataProtectionServiceProvider, dataProtectionConfigureAction, ProtectedConfigurationBuilder.ProtectedConfigurationBuilderStringPurpose(purposeString));
            else
                throw new ArgumentException("Please use ProtectedConfigurationBuilder instead of ConfigurationBuilder class!", nameof(configurationBuilder));

        }



        /// <summary>
        /// Perform wildcard search of files in path encrypting any data enclosed by protectRegexString the inside files with the protectedReplaceString
        /// </summary>
        /// <param name="path">directory to be searched</param>
        /// <param name="searchPattern">wildcard pattern to filter files</param>
        /// <param name="searchOption">search options</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a String expression used to generate the final encrypted String using ${protectedData} as a placeholder parameter for encrypted data and ${subPurposePattern} as a placeholder parameter for the key custom subpurpose</param>
        /// <param name="backupOriginalFile">boolean which indicates whether to make a backupof original file with extension .bak</param>
        /// <returns>a list of filenames which have been successfully encrypted</returns>
        /// <exception cref="ArgumentException"></exception>
        public static IList<String> ProtectFiles(this IDataProtector dataProtector, String path, String searchPattern = "*.json", SearchOption searchOption = SearchOption.TopDirectoryOnly, String protectRegexString = null, String protectedReplaceString = null, bool backupOriginalFile = true)
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
                        replacedContent = protectFileOption.FileProtectProcessor.ProtectFile(fileContent, protectRegex, (value) => ProtectConfigurationValue(dataProtector, value, protectRegexString, protectedReplaceString));
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
        /// Encrypts the String value using the specified dataProtector
        /// </summary>
        /// <param name="dataProtector">an IDataProtect interface obtained from a configured Data Protection API</param>
        /// <param name="value">a String literal which needs to be encrypted</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a String used to replace the protectRegex token with the protected format (e.g. matching the protectRegexString of IConfigurationBuilder), the encrypted data is injected by using the placeholder ${protectedData} and ${subPurposePattern} as a placeholder parameter for the key custom subpurpose</param>
        /// <returns>the encrypted configuration value</returns>
        /// <exception cref="ArgumentException"></exception>
        public static String ProtectConfigurationValue(this IDataProtector dataProtector, String value, String protectRegexString = null, String protectedReplaceString = null)
        {
            var protectRegex = new Regex(!String.IsNullOrEmpty(protectRegexString) ? protectRegexString : ProtectedConfigurationBuilder.DefaultProtectRegexString);
            if (!protectRegex.GetGroupNames().Contains("protectData"))
                throw new ArgumentException("Regex must contain a group named protectData!", nameof(protectRegexString));

            return ProtectConfigurationValueInternal(dataProtector, value, protectRegex, protectedReplaceString);
        }



        private static String ProtectConfigurationValueInternal(IDataProtector dataProtector, String value, Regex protectRegex, String protectedReplaceString)
        {
            protectedReplaceString = !String.IsNullOrEmpty(protectedReplaceString) ? protectedReplaceString : ProtectedConfigurationBuilder.DefaultProtectedReplaceString;

            return protectRegex.Replace(value, (me) =>
            {
                var subPurposePresent = !String.IsNullOrEmpty(me.Groups["subPurpose"]?.Value);

                if (subPurposePresent)
                    dataProtector = dataProtector.CreateProtector(me.Groups["subPurpose"].Value);

                return protectedReplaceString.Replace("${subPurposePattern}", subPurposePresent ? me.Groups["subPurposePattern"].Value : String.Empty).Replace("${protectedData}", dataProtector.Protect(me.Groups["protectData"].Value));
            });
        }



        /// <summary>
        /// Encrypts the Dictionary<String, String> initialData using the specified dataProtector (used for in-memory collections)
        /// </summary>
        /// <param name="dataProtector">an IDataProtect interface obtained from a configured Data Protection API</param>
        /// <param name="initialData">a Dictionary<String, String> whose values need to be encrypted</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a String used to replace the protectRegex token with the protected format (e.g. matching the protectRegexString of IConfigurationBuilder), the encrypted data is injected by using the placeholder ${protectedData} and ${subPurposePattern} as a placeholder parameter for the key custom subpurpose</param>
        public static void ProtectConfigurationValue(this IDataProtector dataProtector, Dictionary<String, String> initialData, String protectRegexString = null, String protectedReplaceString = null)
        {
            if (initialData != null)
                foreach (var key in initialData.Keys.ToList())
                    initialData[key] = dataProtector.ProtectConfigurationValue(initialData[key], protectRegexString, protectedReplaceString);
        }



        /// <summary>
        /// Encrypts the IEnumerable<String> arguments using the specified dataProtector
        /// </summary>
        /// <param name="dataProtector">an IDataProtect interface obtained from a configured Data Protection API</param>
        /// <param name="arguments">a IEnumerable<String> whose elements need to be encrypted</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a String used to replace the protectRegex token with the protected format (e.g. matching the protectRegexString of IConfigurationBuilder), the encrypted data is injected by using the placeholder ${protectedData} and ${subPurposePattern} as a placeholder parameter for the key custom subpurpose</param>
        /// <returns>a newer encrypted IEnumerable<String></returns>
        public static IEnumerable<String> ProtectConfigurationValue(this IDataProtector dataProtector, IEnumerable<String> arguments, String protectRegexString = null, String protectedReplaceString = null)
        {
            return arguments?.Select(argument => dataProtector.ProtectConfigurationValue(argument, protectRegexString, protectedReplaceString));
        }



        /// <summary>
        /// Encrypts the String[] arguments using the specified dataProtector (used for command-line arguments)
        /// </summary>
        /// <param name="dataProtector">an IDataProtect interface obtained from a configured Data Protection API</param>
        /// <param name="arguments">a String array whose elements need to be encrypted</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a String used to replace the protectRegex token with the protected format (e.g. matching the protectRegexString of IConfigurationBuilder), the encrypted data is injected by using the placeholder ${protectedData} and ${subPurposePattern} as a placeholder parameter for the key custom subpurpose</param>
        /// <returns>a newer encrypted String[] array</returns>
        public static String[] ProtectConfigurationValue(this IDataProtector dataProtector, String[] arguments, String protectRegexString = null, String protectedReplaceString = null)
        {
            return arguments?.Select(argument => dataProtector.ProtectConfigurationValue(argument, protectRegexString, protectedReplaceString)).ToArray();
        }



        /// <summary>
        /// Encrypts all the environment variables using the specified dataProtector (used for environment variables)
        /// </summary>
        /// <param name="dataProtector">an IDataProtect interface obtained from a configured Data Protection API</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a String used to replace the protectRegex token with the protected format (e.g. matching the protectRegexString of IConfigurationBuilder), the encrypted data is injected by using the placeholder ${protectedData} and ${subPurposePattern} as a placeholder parameter for the key custom subpurpose</param>
        public static void ProtectEnvironmentVariables(this IDataProtector dataProtector, String protectRegexString = null, String protectedReplaceString = null)
        {
            var environmentVariables = Environment.GetEnvironmentVariables();

            if (environmentVariables != null)
                foreach (String key in environmentVariables.Keys)
                    Environment.SetEnvironmentVariable(key, dataProtector.ProtectConfigurationValue(environmentVariables[key].ToString(), protectRegexString, protectedReplaceString));
        }
    }
}
