using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using System.Diagnostics;

namespace Fededim.Extensions.Configuration.Protected
{
    [DebuggerDisplay("Provider = {Provider}")]
    public class ProtectedConfigurationProvider : IConfigurationProvider
    {
        protected IConfigurationProvider Provider { get; }
        protected ProtectedConfigurationData ProtectedConfigurationData { get; }


        public ProtectedConfigurationProvider(IConfigurationProvider provider, ProtectedConfigurationData protectedConfigurationData)
        {
            Provider = provider;
            ProtectedConfigurationData = protectedConfigurationData;
        }


        public IEnumerable<String> GetChildKeys(IEnumerable<String> earlierKeys, String parentPath)
        {
            return Provider.GetChildKeys(earlierKeys, parentPath);
        }

        public IChangeToken GetReloadToken()
        {
            return Provider.GetReloadToken();
        }


        public void Load()
        {
            Provider.Load();

            DecryptChildKeys();
        }


        private void DecryptChildKeys(String parentPath=null)
        {
            // decrypt all values using just IConfigurationBuilder interface methods
            // unluckily there Data dictionary is not exposed on the interface, but we can get all keys by using the GetChildKeys methods, look at its implementation https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Configuration/src/ConfigurationProvider.cs#L61-L94
            // the only drawback of this method is that it returns the child keys of the level of the hierarchy specified by the parentPath parameter (it's at line 71 in MS source code "Segment(kv.Key,0)" )
            // so you have to use a recursive function to cycle all existing keys and also to issue a distinct due to the strange way it has been implemented
            foreach (var key in Provider.GetChildKeys(new List<String>(), parentPath).Distinct())
            {
                var fullKey = parentPath != null ? $"{parentPath}:{key}" : key;
                if (Provider.TryGet(fullKey, out var value))
                {
                    if (!String.IsNullOrEmpty(value))
                        Provider.Set(fullKey, ProtectedConfigurationData.ProtectedRegex.Replace(value, me => ProtectedConfigurationData.DataProtector.Unprotect(me.Groups["protectedData"].Value)));
                }
                else DecryptChildKeys(fullKey);
            }
        }

        public void Set(String key, String value)
        {
            Provider.Set(key, value);
        }

        public bool TryGet(String key, out String value)
        {
            return Provider.TryGet(key, out value);
        }
    }
}
