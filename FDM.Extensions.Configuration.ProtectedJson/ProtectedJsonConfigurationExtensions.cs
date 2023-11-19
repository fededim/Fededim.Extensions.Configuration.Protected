using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;

namespace FDM.Extensions.Configuration.ProtectedJson
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



        public static IConfigurationBuilder AddProtectedJsonFile(this IConfigurationBuilder builder, IFileProvider? provider, string path, bool optional, bool reloadOnChange, String? protectedRegexString = null, IServiceProvider? serviceProvider = null, Action<IDataProtectionBuilder>? dataProtectionConfigureAction = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            if (serviceProvider == null && dataProtectionConfigureAction == null)
                throw new ArgumentException("Either serviceProvider or dataProtectionConfigureAction must not be null", serviceProvider == null ? nameof(serviceProvider) : nameof(dataProtectionConfigureAction));

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Invalid file path", nameof(path));

            var protectedRegex = new Regex(protectedRegexString??ProtectedJsonConfigurationSource.DefaultProtectedRegexString);
            if (!protectedRegex.GetGroupNames().Contains("protectedSection"))
                throw new ArgumentException("Regex must contain a group named protectedSection!", nameof(protectedRegexString));

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

        public static IConfigurationBuilder AddProtectedJsonStream(this IConfigurationBuilder builder, Stream stream, string protectedRegexString = ProtectedJsonConfigurationSource.DefaultProtectedRegexString, IServiceProvider? serviceProvider = null, Action<IDataProtectionBuilder>? dataProtectionConfigureAction = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            if (serviceProvider == null && dataProtectionConfigureAction == null)
                throw new ArgumentException("Either serviceProvider or dataProtectionConfigureAction must not be null", serviceProvider == null ? nameof(serviceProvider) : nameof(dataProtectionConfigureAction));

            var protectedRegex = new Regex(protectedRegexString ?? ProtectedJsonConfigurationSource.DefaultProtectedRegexString);
            if (!protectedRegex.GetGroupNames().Contains("protectedSection"))
                throw new ArgumentException("Regex must contain a group named protectedSection!", nameof(protectedRegexString));

            return builder.Add<ProtectedJsonStreamConfigurationSource>(s => {
                s.Stream = stream;
                s.ProtectedRegex = protectedRegex;
                s.DataProtectionBuildAction = dataProtectionConfigureAction;
                s.ServiceProvider = serviceProvider;
            }
            );
        }
    }
}
