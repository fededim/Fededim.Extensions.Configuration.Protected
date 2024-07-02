using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Collections.ObjectModel;

namespace Fededim.Extensions.Configuration.Protected
{
    /// <summary>
    /// ProtectedConfigurationProvider is a custom <see cref="ConfigurationProvider" /> which is actually responsible for real time decryption of configuration values for any existing or even future ConfigurationProviders. It uses the composition pattern.
    /// </summary>
    [DebuggerDisplay("Provider = Protected{Provider}")]
    public class ProtectedConfigurationProvider : IConfigurationProvider, IDisposable
    {
        protected IConfigurationProvider Provider { get; }
        protected IProtectProviderConfigurationData ProtectProviderConfigurationData { get; }

        protected ConfigurationReloadToken ReloadToken;

        protected IDisposable ProviderReloadTokenRegistration { get; set; }


        public ProtectedConfigurationProvider(IConfigurationProvider provider, IProtectProviderConfigurationData protectedConfigurationData)
        {
            Provider = provider;
            ProtectProviderConfigurationData = protectedConfigurationData;

            RegisterReloadCallback();
        }



        /// <summary>
        /// Registers a reload callback which redecrypts all values if the underlying IConfigurationProvider supports it
        /// </summary>
        protected void RegisterReloadCallback()
        {
            // check if underlying provider supports reloading
            if (Provider.GetReloadToken() != null)
            {
                // Create our reload token
                ReloadToken = new ConfigurationReloadToken();

                // registers Provider on Change event using framework static utility method ChangeToken.OnChange in order to be notified of configuration reload and redecrypts subsequently the needed keys
                ProviderReloadTokenRegistration = ChangeToken.OnChange(() => Provider.GetReloadToken(), (configurationProvider) =>
                {
                    var protectedConfigurationProvider = configurationProvider as ProtectedConfigurationProvider;

                    // redecrypts all needed keys
                    protectedConfigurationProvider.DecryptChildKeys();

                    // notifies all subscribes
                    OnReload();
                }, this);
            }
        }



        /// <summary>
        /// Dispatches all the callbacks waiting for the reload event from this configuration provider (and creates a new ReloadToken)
        /// </summary>
        protected void OnReload()
        {
            ConfigurationReloadToken previousToken = Interlocked.Exchange(ref ReloadToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }




        /// <summary>
        /// Static PropertyInfo of protected property Data of Microsoft.Extensions.Configuration.ConfigurationProvider class (even though it is protected and not available here, you can use reflection in order to retrieve its value)
        /// </summary>
        public static PropertyInfo ConfigurationProviderDataProperty = typeof(ConfigurationProvider).GetProperty("Data", BindingFlags.NonPublic | BindingFlags.Instance);



        /// <summary>
        /// Hacky and fastest, tough safe method which gives access to the provider Data dictionary in readonly mode (it could be null in the future or for other providers not deriving from ConfigurationProvider, be sure to always check that it is not null!)
        /// </summary>
        public IReadOnlyDictionary<String, String> ProviderDataReadOnly
        {
            get
            {
                IDictionary<String, String> providerData = ProviderData;

                if (providerData != null)
                    return new ReadOnlyDictionary<String, String>(providerData);

                return null;
            }
        }




        /// <summary>
        /// Hacky and fastest, tough safe method which gives access to the provider Data dictionary (it could be null in the future or for other providers not deriving from ConfigurationProvider, be sure to always check that it is not null!)
        /// </summary>
        protected IDictionary<String, String> ProviderData
        {
            get
            {
                IDictionary<String, String> providerData = null;

                if (Provider is ConfigurationProvider && ConfigurationProviderDataProperty != null)
                    providerData = ConfigurationProviderDataProperty.GetValue(Provider) as IDictionary<string, string>;

                return providerData;
            }
        }



        /// <summary>
        /// This is a helper method actually responsible for the decryption of all configuration values. It decrypts all values using just IConfigurationBuilder interface methods so it should work on any existing or even future IConfigurationProvider <br /><br />
        /// Note: unluckily there Data dictionary property of ConfigurationProvider is not exposed on the interface IConfigurationProvider, but we can manage to get all keys by using the GetChildKeys methods, look at its implementation <see href="https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Configuration/src/ConfigurationProvider.cs#L61-L94"/> <br /><br />
        /// The only drawback of this method is that it returns the child keys of the level of the hierarchy specified by the parentPath parameter (it's at line 84 in MS source code "Segment(kv.Key, parentPath.Length + 1)" <see href="https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Configuration/src/ConfigurationProvider.cs#L84"/>) <br />
        /// So you have to use a recursive function to gather all existing keys and also to issue a distinct due to the way the GetChildKeys method has been implemented <br />
        /// </summary>
        /// <param name="parentPath"></param>
        protected void DecryptChildKeys(String parentPath = null)
        {
            IDictionary<String, String> dataProperty;

            // this is a hacky yet safe way to speed up key enumeration
            // we access the Data dictionary of ConfigurationProvider using reflection avoiding enumerating all keys with recursive function
            // the speed improvement is more than 3000 times!
            if (((dataProperty = ProviderData) != null))
            {
                foreach (var key in dataProperty.Keys.ToList())
                {
                    if (!String.IsNullOrEmpty(dataProperty[key]))
                        Provider.Set(key, ProtectProviderConfigurationData.ProtectedRegex.Replace(dataProperty[key], me =>
                        {

                            var subPurposePresent = !String.IsNullOrEmpty(me.Groups["subPurpose"]?.Value);

                            IProtectProvider protectProvider = ProtectProviderConfigurationData.ProtectProvider;

                            if (subPurposePresent)
                                protectProvider = protectProvider.CreateNewProviderFromSubkey(me.Groups["subPurpose"].Value);

                            return protectProvider.Decrypt(me.Groups["protectedData"].Value);
                        }));
                }
            }
            else
            {
                foreach (var key in Provider.GetChildKeys(new List<String>(), parentPath).Distinct())
                {
                    var fullKey = parentPath != null ? $"{parentPath}:{key}" : key;
                    if (Provider.TryGet(fullKey, out var value))
                    {
                        if (!String.IsNullOrEmpty(value))
                            Provider.Set(fullKey, ProtectProviderConfigurationData.ProtectedRegex.Replace(value, me =>
                            {

                                var subPurposePresent = !String.IsNullOrEmpty(me.Groups["subPurpose"]?.Value);

                                IProtectProvider protectProvider = ProtectProviderConfigurationData.ProtectProvider;

                                if (subPurposePresent)
                                    protectProvider = protectProvider.CreateNewProviderFromSubkey(me.Groups["subPurpose"].Value);

                                return protectProvider.Decrypt(me.Groups["protectedData"].Value);
                            }));
                    }
                    else DecryptChildKeys(fullKey);
                }
            }
        }



        /// <summary>
        /// Returns our reload token
        /// </summary>
        /// <returns>the <see cref="ReloadToken"/></returns>
        public IChangeToken GetReloadToken()
        {
            return ReloadToken;
        }



        /// <summary>
        /// Calls the underlying provider Load method in order to load configuration values and then decrypts them by calling DecryptChildKeys helper method
        /// </summary>
        public void Load()
        {
            Provider.Load();

            // call DecryptChildKeys after Load
            DecryptChildKeys();
        }




        /// <summary>
        /// Calls the underlying provider GetChildKeys method
        /// </summary>
        /// <param name="earlierKeys"></param>
        /// <param name="parentPath"></param>
        /// <returns>the child keys.</returns>

        public IEnumerable<String> GetChildKeys(IEnumerable<String> earlierKeys, String parentPath)
        {
            return Provider.GetChildKeys(earlierKeys, parentPath);
        }



        /// <summary>
        /// Calls the underlying provider Set method
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set(String key, String value)
        {
            Provider.Set(key, value);
        }



        /// <summary>
        /// Calls the underlying provider TryGet method
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns><c>True</c> if a value for the specified key was found, otherwise <c>false</c>.</returns>
        public bool TryGet(String key, out String value)
        {
            return Provider.TryGet(key, out value);
        }



        /// <summary>
        /// Disposes the potential provider reload token (when supported)
        /// </summary>
        public void Dispose()
        {
            ProviderReloadTokenRegistration?.Dispose();
        }
    }
}