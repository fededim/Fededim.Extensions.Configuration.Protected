using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Fededim.Extensions.Configuration.ProtectedJson
{
    public static class ProtectedJsonConfigurationExtensions
    {
        public static IConfigurationBuilder AddProtectedJsonFile(this IConfigurationBuilder builder, string path, Action<IDataProtectionBuilder> dataProtectionConfigureAction)
        {
            return AddProtectedJsonFile(builder, provider: null, path: path, optional: false, reloadOnChange: false, dataProtectionConfigureAction: dataProtectionConfigureAction);
        }

        public static IConfigurationBuilder AddProtectedJsonFile(this IConfigurationBuilder builder, string path, IServiceProvider serviceProvider)
        {
            return AddProtectedJsonFile(builder, provider: null, path: path, optional: false, reloadOnChange: false, serviceProvider: serviceProvider);
        }



        public static IConfigurationBuilder AddProtectedJsonFile(this IConfigurationBuilder builder, string path, bool optional, Action<IDataProtectionBuilder> dataProtectionConfigureAction)
        {
            return AddProtectedJsonFile(builder, provider: null, path: path, optional: optional, reloadOnChange: false, dataProtectionConfigureAction: dataProtectionConfigureAction);
        }

        public static IConfigurationBuilder AddProtectedJsonFile(this IConfigurationBuilder builder, string path, bool optional, IServiceProvider serviceProvider)
        {
            return AddProtectedJsonFile(builder, provider: null, path: path, optional: optional, reloadOnChange: false, serviceProvider: serviceProvider);
        }



        public static IConfigurationBuilder AddProtectedJsonFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange, Action<IDataProtectionBuilder> dataProtectionConfigureAction)
        {
            return AddProtectedJsonFile(builder, provider: null, path: path, optional: optional, reloadOnChange: reloadOnChange, dataProtectionConfigureAction: dataProtectionConfigureAction);
        }

        public static IConfigurationBuilder AddProtectedJsonFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange, IServiceProvider serviceProvider)
        {
            return AddProtectedJsonFile(builder, provider: null, path: path, optional: optional, reloadOnChange: reloadOnChange, serviceProvider: serviceProvider);
        }



        public static IConfigurationBuilder AddProtectedJsonFile(this IConfigurationBuilder builder, IFileProvider provider, string path, bool optional, bool reloadOnChange, String protectedRegexString = null, IServiceProvider serviceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));


            if (serviceProvider == null && dataProtectionConfigureAction == null)
                throw new ArgumentException("Either serviceProvider or dataProtectionConfigureAction must not be null", serviceProvider == null ? nameof(serviceProvider) : nameof(dataProtectionConfigureAction));

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Invalid file path", nameof(path));

            var protectedRegex = new Regex(protectedRegexString ?? ProtectedJsonConfigurationSource.DefaultProtectedRegexString);
            if (!protectedRegex.GetGroupNames().Contains("protectedData"))
                throw new ArgumentException("Regex must contain a group named protectedData!", nameof(protectedRegexString));

            return builder.AddProtectedJsonFile(s =>
            {
                s.FileProvider = provider;
                s.Path = path;
                s.Optional = optional;
                s.ReloadOnChange = reloadOnChange;
                s.ProtectedRegex = protectedRegex;
                s.DataProtectionBuildAction = dataProtectionConfigureAction;
                s.ServiceProvider = serviceProvider;
                s.ResolveFileProvider();
            });
        }


        public static IConfigurationBuilder AddProtectedJsonFile(this IConfigurationBuilder builder, Action<ProtectedJsonConfigurationSource> configureSource)
        {
            var configurationSource = new ProtectedJsonConfigurationSource();
            configureSource(configurationSource);
            return builder.Add(configurationSource);
        }



        public static IConfigurationBuilder AddProtectedJsonStream(this IConfigurationBuilder builder, Action<ProtectedJsonStreamConfigurationSource> configureSource)
        {
            var configurationSource = new ProtectedJsonStreamConfigurationSource();
            configureSource(configurationSource);
            return builder.Add(configurationSource);
        }

        public static IConfigurationBuilder AddProtectedJsonStream(this IConfigurationBuilder builder, Stream stream, string protectedRegexString = ProtectedJsonConfigurationSource.DefaultProtectedRegexString, IServiceProvider serviceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (serviceProvider == null && dataProtectionConfigureAction == null)
                throw new ArgumentException("Either serviceProvider or dataProtectionConfigureAction must not be null", serviceProvider == null ? nameof(serviceProvider) : nameof(dataProtectionConfigureAction));

            var protectedRegex = new Regex(protectedRegexString ?? ProtectedJsonConfigurationSource.DefaultProtectedRegexString);
            if (!protectedRegex.GetGroupNames().Contains("protectedData"))
                throw new ArgumentException("Regex must contain a group named protectedData!", nameof(protectedRegexString));

            return builder.Add<ProtectedJsonStreamConfigurationSource>(s =>
            {
                s.Stream = stream;
                s.ProtectedRegex = protectedRegex;
                s.DataProtectionBuildAction = dataProtectionConfigureAction;
                s.ServiceProvider = serviceProvider;
            }
            );
        }


        /// <summary>
        /// Perform wildcard search of files in path encrypting any data enclosed by protectRegexString the inside files with the protectedReplaceString
        /// </summary>
        /// <param name="path">directory to be searched</param>
        /// <param name="searchPattern">wildcard pattern to filter files</param>
        /// <param name="searchOption">search options</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a string expression used to generate the final encrypted string using ${protectedData} as a placeholder parameter for encrypted data</param>
        /// <param name="backupOriginalFile">boolean which indicates whether to make a backupof original file with extension .bak</param>
        /// <returns>a list of filenames which have been successfully encrypted</returns>
        /// <exception cref="ArgumentException"></exception>
        public static IList<String> ProtectFiles(this IDataProtector dataProtector, string path, string searchPattern = "*.json", SearchOption searchOption = SearchOption.TopDirectoryOnly, String protectRegexString = null, String protectedReplaceString = "Protected:{${protectedData}}", bool backupOriginalFile = true)
        {
            var protectRegex = new Regex(protectRegexString ?? ProtectedJsonConfigurationSource.DefaultProtectRegexString);
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
                        File.Copy(f, f + ".bak",true);

                    File.WriteAllText(f, replacedContent);

                    result.Add(f);
                }
            }

            return result;
        }
    }
}
