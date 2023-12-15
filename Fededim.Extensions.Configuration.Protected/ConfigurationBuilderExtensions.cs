using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fededim.Extensions.Configuration.Protected
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder WithProtectedConfigurationOptions(this IConfigurationBuilder configurationBuilder, String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, int keyNumber=1)
        {
            var protectedConfigurationBuilder = configurationBuilder as IProtectedConfigurationBuilder;

            if (protectedConfigurationBuilder != null)
                return protectedConfigurationBuilder.WithProtectedConfigurationOptions(protectedRegexString, dataProtectionServiceProvider, dataProtectionConfigureAction, keyNumber);
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
        /// <param name="protectedReplaceString">a String expression used to generate the final encrypted String using ${protectedData} as a placeholder parameter for encrypted data</param>
        /// <param name="backupOriginalFile">boolean which indicates whether to make a backupof original file with extension .bak</param>
        /// <returns>a list of filenames which have been successfully encrypted</returns>
        /// <exception cref="ArgumentException"></exception>
        public static IList<String> ProtectFiles(this IDataProtector dataProtector, String path, String searchPattern = "*.json", SearchOption searchOption = SearchOption.TopDirectoryOnly, String protectRegexString = null, String protectedReplaceString = "Protected:{${protectedData}}", bool backupOriginalFile = true)
        {
            var protectRegex = new Regex(protectRegexString ?? ProtectedConfigurationBuilder.DefaultProtectRegexString);
            if (!protectRegex.GetGroupNames().Contains("protectData"))
                throw new ArgumentException("Regex must contain a group named protectData!", nameof(protectRegexString));

            var result = new List<String>();

            foreach (var f in Directory.EnumerateFiles(path, searchPattern, searchOption))
            {
                var fileContent = File.ReadAllText(f);

                var replacedContent = protectRegex.Replace(fileContent, (me) =>
                        protectedReplaceString.Replace("${protectedData}", dataProtector.Protect(me.Groups["protectData"].Value)));

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


        public static String ProtectConfigurationValue(this IDataProtector dataProtector, String value, String protectRegexString = null, String protectedReplaceString = ProtectedConfigurationBuilder.DefaultProtectedReplaceString)
        {
            var protectRegex = new Regex(protectRegexString ?? ProtectedConfigurationBuilder.DefaultProtectRegexString);
            if (!protectRegex.GetGroupNames().Contains("protectData"))
                throw new ArgumentException("Regex must contain a group named protectData!", nameof(protectRegexString));


            return protectRegex.Replace(value, (me) => protectedReplaceString.Replace("${protectedData}", dataProtector.Protect(me.Groups["protectData"].Value)));
        }


        public static void ProtectConfigurationValue(this IDataProtector dataProtector, Dictionary<String, String> initialData, String protectRegexString = null, String protectedReplaceString = ProtectedConfigurationBuilder.DefaultProtectedReplaceString)
        {
            if (initialData != null)
                foreach (var key in initialData.Keys.ToList())
                    initialData[key] = dataProtector.ProtectConfigurationValue(initialData[key], protectRegexString, protectedReplaceString);
        }


        public static IEnumerable<String> ProtectConfigurationValue(this IDataProtector dataProtector, IEnumerable<String> arguments, String protectRegexString = null, String protectedReplaceString = ProtectedConfigurationBuilder.DefaultProtectedReplaceString)
        {
            return arguments?.Select(argument => dataProtector.ProtectConfigurationValue(argument, protectRegexString, protectedReplaceString));
        }



        public static String[] ProtectConfigurationValue(this IDataProtector dataProtector, String[] arguments, String protectRegexString = null, String protectedReplaceString = ProtectedConfigurationBuilder.DefaultProtectedReplaceString)
        {
            return arguments?.Select(argument => dataProtector.ProtectConfigurationValue(argument, protectRegexString, protectedReplaceString)).ToArray();
        }


        public static void ProtectEnvironmentVariables(this IDataProtector dataProtector, String protectRegexString = null, String protectedReplaceString = ProtectedConfigurationBuilder.DefaultProtectedReplaceString)
        {
            var environmentVariables = Environment.GetEnvironmentVariables();

            if (environmentVariables != null)
                foreach (string key in environmentVariables.Keys)
                    Environment.SetEnvironmentVariable(key, dataProtector.ProtectConfigurationValue(environmentVariables[key].ToString(), protectRegexString, protectedReplaceString));
        }
    }
}
