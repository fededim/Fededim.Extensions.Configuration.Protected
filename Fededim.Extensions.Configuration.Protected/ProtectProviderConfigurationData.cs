using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fededim.Extensions.Configuration.Protected
{
    /// <summary>
    /// A standard interface which must implement by an encryption/decryption provider
    /// </summary>
    public interface IProtectProvider
    {
        /// <summary>
        /// This method encrypts a plain-text string 
        /// </summary>
        /// <param name="plainTextValue">the plain-text string to be encrypted</param>
        /// <returns>the encrypted string</returns>
        String Encrypt(String plainTextValue);


        /// <summary>
        /// This method decrypts an encrypted string 
        /// </summary>
        /// <param name="encryptedValue">the encrypted string to be decrypted</param>
        /// <returns>the decrypted string</returns>
        String Decrypt(String encryptedValue);


        /// <summary>
        /// This methods create a new <see cref="IProtectProvider"/> for supporting per configuration value encryption subkey (e.g. "subpurposes")
        /// </summary>
        /// <param name="subkey">the per configuration value encryption subkey</param>
        /// <returns>a derived <see cref="IProtectProvider"/> based on the <see cref="subkey"/> parameter</returns>
        IProtectProvider CreateNewProviderFromSubkey(string subkey);
    }



    /// <summary>
    /// an abstract class for specifying the configuration data of the encryption/decryption provider
    /// </summary>
    public abstract class IProtectProviderConfigurationData
    {
        /// <summary>
        /// The actual provider performing the encryption/decryption, <see cref="IProtectProvider"/> interface
        /// </summary>
        public IProtectProvider ProtectProvider { get; protected set; }

        /// <summary>
        /// a regular expression which captures the data to be encrypted in a named group called protectData
        /// </summary>
        public Regex ProtectedRegex { get; protected set; }


        /// <summary>
        /// A helper overridable method for checking that the configuation data is valid.
        /// </summary>
        public virtual bool IsValid => (ProtectProvider != null) && (ProtectedRegex?.GetGroupNames()?.Contains("protectedData") == true);
    }



    /// <summary>
    /// ProtectedConfigurationData is a custom data structure which stores all configuration options needed by ProtectedConfigurationBuilder and ProtectConfigurationProvider
    /// </summary>
    public class ProtectProviderConfigurationData : IProtectProviderConfigurationData
    {
        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="protectedRegexString">a regular expression which captures the encrypted data in a named group called protectData</param>
        /// <param name="protectProvider">an IProtectProvider interface obtained from a one of the supported providers</param>
        /// <exception cref="ArgumentException">thrown if the Regex does not containg a group named protectedData</exception>
        public ProtectProviderConfigurationData(String protectedRegexString, IProtectProvider protectProvider)
        {
            // check that Regex contains a group named protectedData
            ProtectedRegex = new Regex(!String.IsNullOrEmpty(protectedRegexString) ? protectedRegexString : ProtectedConfigurationBuilder.DefaultProtectedRegexString);
            if (!ProtectedRegex.GetGroupNames().Contains("protectedData"))
                throw new ArgumentException("Regex must contain a group named protectedData!", nameof(protectedRegexString));

            ProtectProvider = protectProvider;
        }


        /// <summary>
        /// A static helper method which calculates the merge of the global and local protected configuration data
        /// </summary>
        /// <param name="global">the global configuration data</param>
        /// <param name="local">the local configuration data</param>
        /// <returns></returns>
        public static IProtectProviderConfigurationData Merge(IProtectProviderConfigurationData global, IProtectProviderConfigurationData local)
        {
            if (local == null)
                return global;

            if (global == null)
                return local;

            return new ProtectProviderConfigurationData(local.ProtectedRegex?.ToString() ?? global.ProtectedRegex?.ToString(), local.ProtectProvider ?? global.ProtectProvider);
        }
    }

}
