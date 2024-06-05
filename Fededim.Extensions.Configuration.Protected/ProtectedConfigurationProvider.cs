using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.DataProtection;
using System.Diagnostics;
using System.Threading;

namespace Fededim.Extensions.Configuration.Protected
{
    /// <summary>
    /// ProtectedConfigurationProvider is a custom ConfigurationProvider which is actually responsible for real time decryption of configuration values for any existing or even future ConfigurationProvider. It uses the composition pattern.
    /// </summary>
    [DebuggerDisplay("Provider = Protected{Provider}")]
    public class ProtectedConfigurationProvider : IConfigurationProvider, IDisposable
    {
        protected IConfigurationProvider Provider { get; }
        protected ProtectedConfigurationData ProtectedConfigurationData { get; }

        protected ConfigurationReloadToken ReloadToken;

        protected IDisposable ProviderReloadTokenRegistration { get; set; }


        public ProtectedConfigurationProvider(IConfigurationProvider provider, ProtectedConfigurationData protectedConfigurationData)
        {
            Provider = provider;
            ProtectedConfigurationData = protectedConfigurationData;

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
        /// This is a helper method actually responsible for the decryption of all configuration values. It decrypts all values using just IConfigurationBuilder interface methods so it should work on any existing or even future IConfigurationProvider <br /><br />
        /// Note: unluckily there Data dictionary property of ConfigurationProvider is not exposed on the interface IConfigurationProvider, but we can manage to get all keys by using the GetChildKeys methods, look at its implementation <see href="https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Configuration/src/ConfigurationProvider.cs#L61-L94"/> <br /><br />
        /// The only drawback of this method is that it returns the child keys of the level of the hierarchy specified by the parentPath parameter (it's at line 71 in MS source code "Segment(kv.Key,0)" <see href="https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Configuration/src/ConfigurationProvider.cs#L71"/>) <br />
        /// So you have to use a recursive function to gather all existing keys and also to issue a distinct due to the way the GetChildKeys method has been implemented <br />
        /// </summary>
        /// <param name="parentPath"></param>
        protected void DecryptChildKeys(String parentPath = null)
        {
            foreach (var key in Provider.GetChildKeys(new List<String>(), parentPath).Distinct())
            {
                var fullKey = parentPath != null ? $"{parentPath}:{key}" : key;
                if (Provider.TryGet(fullKey, out var value))
                {
                    if (!String.IsNullOrEmpty(value))
                        Provider.Set(fullKey, ProtectedConfigurationData.ProtectedRegex.Replace(value, me =>
                        {

                            var subPurposePresent = !String.IsNullOrEmpty(me.Groups["subPurpose"]?.Value);

                            var dataProtector = ProtectedConfigurationData.DataProtector;

                            if (subPurposePresent)
                                dataProtector = dataProtector.CreateProtector(me.Groups["subPurpose"].Value);

                            return dataProtector.Unprotect(me.Groups["protectedData"].Value);
                        }));
                }
                else DecryptChildKeys(fullKey);
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