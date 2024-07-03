﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Fededim.Extensions.Configuration.Protected
{
    /// <summary>
    /// IProtectedConfigurationBuilder derives from <see cref="IConfigurationBuilder"/> and adds a single method WithProtectedConfigurationOptions used to override the ProtectedConfigurationOptions for a particular provider (e.g. the last one added)
    /// </summary>
    public interface IProtectedConfigurationBuilder : IConfigurationBuilder
    {
        /// <summary>
        /// WithProtectedConfigurationOptions is used to override the ProtectedConfigurationOptions for a particular provider (e.g. the last one added)
        /// </summary>
        /// <param name="protectProviderLocalConfigurationData">the local configuration data implemeting the <see cref="IProtectProviderConfigurationData"/ > interface</param>
        /// <returns>The <see cref="IConfigurationBuilder"/> interface for method chaining</returns>
        IConfigurationBuilder WithProtectedConfigurationOptions(IProtectProviderConfigurationData protectProviderLocalConfigurationData);
    }



    /// <summary>
    /// ProtectedConfigurationBuilder is an improved <see cref="ConfigurationBuilder"/> which allows partial or full encryption of configuration values stored inside any possible ConfigurationSource and fully integrated in the ASP.NET Core architecture.
    /// </summary>
    public class ProtectedConfigurationBuilder : IProtectedConfigurationBuilder
    {
        /// <summary>
        /// A property used to store the global configuration data <see cref="IProtectProviderConfigurationData"/ > interface
        /// </summary>
        protected IProtectProviderConfigurationData ProtectedProviderGlobalConfigurationData { get; }

        /// <summary>
        /// A dictionary property used to store the local configuration data overriding the global one, <see cref="IProtectProviderConfigurationData"/ > interface
        /// </summary>
        protected IDictionary<int, IProtectProviderConfigurationData> ProtectProviderLocalConfigurationData { get; } = new Dictionary<int, IProtectProviderConfigurationData>();


        protected readonly List<IConfigurationSource> _sources = new List<IConfigurationSource>();


        /// <summary>
        /// This is the only constructor which takes in input the global configuration data specifying the regex and the encryption/decryption provider
        /// </summary>
        /// <param name="protectedProviderGlobalConfigurationData">the global configuration data specifying the regex and the encryption/decryption provider</param>
        public ProtectedConfigurationBuilder(IProtectProviderConfigurationData protectedProviderGlobalConfigurationData)
        {
            protectedProviderGlobalConfigurationData.CheckConfigurationIsValid();
            ProtectedProviderGlobalConfigurationData = protectedProviderGlobalConfigurationData;
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
        /// Builds an <see cref="IConfiguration"/> with keys and values from the set of configuration sources registered in <see cref="Sources"/>.
        /// </summary>
        /// <returns>An <see cref="IConfigurationRoot"/> with keys and values from the providers generated by registered configuration sources.</returns>
        public virtual IConfigurationRoot Build()
        {
            var providers = new List<IConfigurationProvider>();
            foreach (IConfigurationSource source in _sources)
            {
                IConfigurationProvider provider = source.Build(this);

                // if we have a custom configuration we move the index from the ConfigurationSource object to the newly created ConfigurationProvider object
                ProtectProviderLocalConfigurationData.TryGetValue(source.GetHashCode(), out var protectedConfigurationData);
                if (protectedConfigurationData != null)
                {
                    ProtectProviderLocalConfigurationData[provider.GetHashCode()] = protectedConfigurationData;
                    ProtectProviderLocalConfigurationData.Remove(source.GetHashCode());
                }

                providers.Add(CreateProtectedConfigurationProvider(provider));
            }
            return new ConfigurationRoot(providers);
        }



        /// <summary>
        /// WithProtectedConfigurationOptions is a helper method used to override the ProtectedGlobalConfigurationData for a particular provider (e.g. the last one added)
        /// </summary>
        /// <param name="protectProviderLocalConfigurationData">the local configuration data overriding the global one, <see cref="IProtectProviderConfigurationData"/ > interface</param>
        /// <returns>The <see cref="IConfigurationBuilder"/> interface for method chaining</returns>
        IConfigurationBuilder IProtectedConfigurationBuilder.WithProtectedConfigurationOptions(IProtectProviderConfigurationData protectProviderLocalConfigurationData)
        {
            protectProviderLocalConfigurationData.CheckConfigurationIsValid();
            ProtectProviderLocalConfigurationData[Sources[Sources.Count - 1].GetHashCode()] = protectProviderLocalConfigurationData;

            return this;
        }



        /// <summary>
        /// CreateProtectedConfigurationProvider creates a new ProtectedConfigurationProvider using the composition approach
        /// </summary>
        /// <param name="provider">an existing IConfigurationProvider to instrument in order to perform the decryption of the encrypted keys</param>
        /// <returns>a newer decrypted <see cref="IConfigurationProvider"/> if we have a valid protected configuration data, otherwise it returns the existing original undecrypted provider</returns>
        protected virtual IConfigurationProvider CreateProtectedConfigurationProvider(IConfigurationProvider provider)
        {
            // this code is an initial one of when I was thinking of casting IConfigurationProvider to ConfigurationProvider (all MS classes derive from this one)
            // in order to retrieve all configuration keys inside DecryptChildKeys using the Data property (through reflection since it is protected) without using the recursive "hack" of GetChildKeys 
            // it has been commented because it is not needed anymore, but I keep it as workaround for accessing all configuration keys just in case MS changes the implementation of GetChildKeys "forbidding" the actual way
            //var providerType = provider.GetType();

            //if (!providerType.IsSubclassOf(typeof(ConfigurationProvider)))
            //    return provider;

            // we merge ProtectedProviderGlobalConfigurationData and ProtectProviderLocalConfigurationData
            var actualProtectedConfigurationData = ProtectProviderLocalConfigurationData.ContainsKey(provider.GetHashCode()) ? ProtectProviderConfigurationData.Merge(ProtectedProviderGlobalConfigurationData, ProtectProviderLocalConfigurationData[provider.GetHashCode()]) : ProtectedProviderGlobalConfigurationData;

            // we use composition to perform decryption of all provider values
            return new ProtectedConfigurationProvider(provider, actualProtectedConfigurationData);
        }
    }
}
