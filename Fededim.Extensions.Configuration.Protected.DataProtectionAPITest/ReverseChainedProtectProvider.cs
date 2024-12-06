using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fededim.Extensions.Configuration.Protected.DataProtectionAPITest
{
    public class ReverseChainedProtectProvider : IProtectProvider
    {
        public IProtectProvider ProtectProvider { get; protected set; }


        public ReverseChainedProtectProvider(IProtectProvider protectProvider)
        {
            ProtectProvider = protectProvider;
        }


        protected string Reverse(string text)
        {
            if (text == null)
                return null;

            char[] array = text.ToCharArray();
            Array.Reverse(array);
            return new String(array);
        }


        public IProtectProvider CreateNewProviderFromSubkey(string key, string subkey)
        {
            return new ReverseChainedProtectProvider(ProtectProvider.CreateNewProviderFromSubkey(key, subkey));
        }


        public string Decrypt(string key, string encryptedValue)
        {
            return ProtectProvider.Decrypt(key, Reverse(encryptedValue));
        }


        public string Encrypt(string key, string plainTextValue)
        {
            return Reverse(ProtectProvider.Encrypt(key, plainTextValue));
        }
    }
}
