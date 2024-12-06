using System;

namespace Fededim.Extensions.Configuration.Protected
{
    /// <summary>
    /// A passthrough protect provider for Fededim.Extensions.Configuration.Protected, implementing the <see cref="IProtectProvider"/> interface.
    /// It does not encrypt or decrypt any entries, it leaves all the entries untouched, it can be used for development purposes.
    /// </summary>
    public class PassthroughProtectProvider : IProtectProvider
    {
        /// <summary>
        /// The main constructor
        /// </summary>
        public PassthroughProtectProvider()
        {
        }

        /// <summary>
        /// This methods create a new <see cref="IProtectProvider"/> for supporting per configuration value encryption subkey (e.g. "subpurposes")
        /// </summary>
        /// <param name="subkey">the per configuration value encryption subkey</param>
        /// <returns>a derived <see cref="IProtectProvider"/> based on the <see cref="subkey"/> parameter</returns>
        public IProtectProvider CreateNewProviderFromSubkey(String key, String subkey)
        {
            return this;
        }


        /// <summary>
        /// This method decrypts an encrypted string 
        /// </summary>
        /// <param name="encryptedValue">the encrypted string to be decrypted</param>
        /// <returns>the decrypted string</returns>
        public String Decrypt(String key, String encryptedValue)
        {
            return encryptedValue;
        }


        /// <summary>
        /// This method encrypts a plain-text string 
        /// </summary>
        /// <param name="plainTextValue">the plain-text string to be encrypted</param>
        /// <returns>the encrypted string</returns>
        public String Encrypt(String key, String plainTextValue)
        {
            return plainTextValue;
        }
    }
}

