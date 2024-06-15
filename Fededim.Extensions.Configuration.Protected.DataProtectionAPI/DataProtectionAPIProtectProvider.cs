using Microsoft.AspNetCore.DataProtection;
using System;

namespace Fededim.Extensions.Configuration.Protected.DataProtectionAPI
{
    /// <summary>
    /// The standard Microsoft DataProtectionAPI protect provider for Fededim.Extensions.Configuration.Protected, implementing the <see cref="IProtectProvider"/> interface.
    /// </summary>
    public class DataProtectionAPIProtectProvider : IProtectProvider
    {
        public IDataProtector DataProtector { get; }


        /// <summary>
        /// The main constructor
        /// </summary>
        /// <param name="dataProtector">the <see cref="IDataProtect"/> interface obtained from Data Protection API</param>
        public DataProtectionAPIProtectProvider(IDataProtector dataProtector)
        {
            DataProtector = dataProtector;
        }

        /// <summary>
        /// This methods create a new <see cref="IProtectProvider"/> for supporting per configuration value encryption subkey (e.g. "subpurposes")
        /// </summary>
        /// <param name="subkey">the per configuration value encryption subkey</param>
        /// <returns>a derived <see cref="IProtectProvider"/> based on the <see cref="subkey"/> parameter</returns>
        public IProtectProvider CreateNewProviderFromSubkey(String subkey)
        {
            return new DataProtectionAPIProtectProvider(DataProtector.CreateProtector(subkey));
        }


        /// <summary>
        /// This method decrypts an encrypted string 
        /// </summary>
        /// <param name="encryptedValue">the encrypted string to be decrypted</param>
        /// <returns>the decrypted string</returns>
        public String Decrypt(String encryptedValue)
        {
            return DataProtector.Unprotect(encryptedValue);
        }


        /// <summary>
        /// This method encrypts a plain-text string 
        /// </summary>
        /// <param name="plainTextValue">the plain-text string to be encrypted</param>
        /// <returns>the encrypted string</returns>
        public String Encrypt(String plainTextValue)
        {
            return DataProtector.Protect(plainTextValue);
        }
    }
}
