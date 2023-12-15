using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.CSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fededim.Extensions.Configuration.Protected
{
    public interface IProtectedConfigurationBuilder : IConfigurationBuilder
    {
        IConfigurationBuilder WithProtectedConfigurationOptions(String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, int keyNumber= 1);
    }



    public class ProtectedConfigurationBuilder : IProtectedConfigurationBuilder
    {
        public static String DataProtectionPurpose(int keyNumber=1) => $"ProtectedConfigurationBuilder.key{keyNumber}";

        public const String DefaultProtectRegexString = "Protect:{(?<protectData>.+?)}";
        public const String DefaultProtectedRegexString = "Protected:{(?<protectedData>.+?)}";
        public const String DefaultProtectedReplaceString = "Protected:{${protectedData}}";


        protected ProtectedConfigurationData ProtectedGlobalConfigurationData { get; }

        protected IDictionary<int, ProtectedConfigurationData> ProtectedProviderConfigurationData { get; } = new Dictionary<int, ProtectedConfigurationData>();


        protected readonly List<IConfigurationSource> _sources = new List<IConfigurationSource>();

        public ProtectedConfigurationBuilder()
        {
        }

        public ProtectedConfigurationBuilder(String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, int keyNumber=1)
        {
            ProtectedGlobalConfigurationData = new ProtectedConfigurationData(protectedRegexString, dataProtectionServiceProvider, dataProtectionConfigureAction, keyNumber);
        }


        /// <summary>
        /// Returns the sources used to obtain configuration values.
        /// </summary>
        public IList<IConfigurationSource> Sources => _sources;

        /// <summary>
        /// Gets a key/value collection that can be used to share data between the <see cref="IConfigurationBuilder"/>
        /// and the registered <see cref="IConfigurationProvider"/>s.
        /// </summary>
        public IDictionary<String, object> Properties { get; } = new Dictionary<String, object>();

        /// <summary>
        /// Adds a new configuration source.
        /// </summary>
        /// <param name="source">The configuration source to add.</param>
        /// <returns>The same <see cref="IConfigurationBuilder"/>.</returns>
        public virtual IConfigurationBuilder Add(IConfigurationSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            _sources.Add(source);
            return this;
        }

        /// <summary>
        /// Builds an <see cref="IConfiguration"/> with keys and values from the set of providers registered in
        /// <see cref="Sources"/>.
        /// </summary>
        /// <returns>An <see cref="IConfigurationRoot"/> with keys and values from the registered providers.</returns>
        public virtual IConfigurationRoot Build()
        {
            var providers = new List<IConfigurationProvider>();
            foreach (IConfigurationSource source in _sources)
            {
                IConfigurationProvider provider = source.Build(this);

                // if we have a custom configuration move from the ConfigurationSource to ConfigurationProvider
                ProtectedProviderConfigurationData.TryGetValue(source.GetHashCode(), out var protectedConfigurationData);
                if (protectedConfigurationData != null)
                {
                    ProtectedProviderConfigurationData[provider.GetHashCode()] = protectedConfigurationData;
                    ProtectedProviderConfigurationData.Remove(source.GetHashCode());
                }

                providers.Add(CreateProtectedConfigurationProvider(provider));
            }
            return new ConfigurationRoot(providers);
        }

        /// <summary>
        /// It's a helper method used to override the ProtectedGlobalConfigurationData for a particular provider (e.g. the last one added)
        /// </summary>
        /// <param name="protectedRegexString"></param>
        /// <param name="dataProtectionServiceProvider"></param>
        /// <param name="dataProtectionConfigureAction"></param>
        public IConfigurationBuilder WithProtectedConfigurationOptions(String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, int keyNumber=1)
        {
            ProtectedProviderConfigurationData[Sources[Sources.Count - 1].GetHashCode()] = new ProtectedConfigurationData(protectedRegexString, dataProtectionServiceProvider, dataProtectionConfigureAction, keyNumber);

            return this;
        }



        protected IConfigurationProvider CreateProtectedConfigurationProvider(IConfigurationProvider provider)
        {
            var providerType = provider.GetType();

            if (!providerType.IsSubclassOf(typeof(ConfigurationProvider)))
                return provider;

            // we merge Provider and Global ProtectedDataConfiguration, if it is not valid we return the existing original provider undecrypted
            var actualProtectedConfigurationData = ProtectedProviderConfigurationData.ContainsKey(provider.GetHashCode()) ? ProtectedConfigurationData.Merge(ProtectedGlobalConfigurationData,ProtectedProviderConfigurationData[provider.GetHashCode()]) : ProtectedGlobalConfigurationData;
            if (actualProtectedConfigurationData?.IsValid != true)
                return provider;

            // we use composition to perform decryption of all provider values
            return new ProtectedConfigurationProvider(provider, actualProtectedConfigurationData);
        }
    }
}
