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
        public const String DefaultProtectRegexString = "Protect(?<subPurposePattern>(:{(?<subPurpose>[^:}]+)})?):{(?<protectData>.+?)}";
        public const String DefaultProtectedRegexString = "Protected(?<subPurposePattern>(:{(?<subPurpose>[^:}]+)})?):{(?<protectedData>.+?)}";
        public const String DefaultProtectedReplaceString = "Protected${subPurposePattern}:{${protectedData}}";

        /// <summary>
        /// The actual provider performing the encryption/decryption, <see cref="IProtectProvider"/> interface
        /// </summary>
        public IProtectProvider ProtectProvider { get; protected set; }

        /// <summary>
        /// a regular expression which captures the data to be decrypted in a named group called protectData
        /// </summary>
        public Regex ProtectedRegex { get; protected set; }


        /// <summary>
        /// a regular expression which captures the data to be encrypted in a named group called protectData
        /// </summary>
        public Regex ProtectRegex { get; protected set; }


        /// <summary>
        /// a string replacement expression which captures the substitution which must be applied for transforming unencrypted tokenization <see cref="DefaultProtectRegexString" /> into an encrypted tokenization <see cref="DefaultProtectedRegexString" />
        /// </summary>
        public String ProtectedReplaceString { get; protected set; }


        /// <summary>
        /// A helper overridable method for checking that the configuation data is valid (e.g. ProtectProvider is not null, ProtectedRegex and ProtectRegex contains both a regex group named protectedData) 
        /// </summary>
        public virtual void CheckConfigurationIsValid()
        {
            ProtectRegex = ProtectRegex ?? new Regex(DefaultProtectRegexString);
            if (!ProtectRegex.GetGroupNames().Contains("protectData"))
                throw new ArgumentException("ProtectRegex must contain a group named protectedData!", nameof(ProtectRegex));

            ProtectedRegex = ProtectedRegex ?? new Regex(DefaultProtectedRegexString);
            if (!ProtectedRegex.GetGroupNames().Contains("protectedData"))
                throw new ArgumentException("ProtectedRegex must contain a group named protectedData!", nameof(ProtectedRegex));

            ProtectedReplaceString = !String.IsNullOrEmpty(ProtectedReplaceString) ? ProtectedReplaceString : DefaultProtectedReplaceString;
            if (!ProtectedReplaceString.Contains("${protectedData}"))
                throw new ArgumentException("ProtectedReplaceString must contain ${protectedData}!", nameof(ProtectedReplaceString));

            if (ProtectProvider == null)
                throw new ArgumentException("ProtectProvider must not be null!", nameof(ProtectProvider));
        }
    }



    /// <summary>
    /// ProtectedConfigurationData is a custom data structure which stores all configuration options needed by ProtectedConfigurationBuilder and ProtectConfigurationProvider
    /// </summary>
    public class ProtectProviderConfigurationData : IProtectProviderConfigurationData
    {
        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectData</param>
        /// <param name="protectedReplaceString">a string replacement expression which captures the substitution which must be applied for transforming unencrypted tokenization <see cref="DefaultProtectRegexString" /> into an encrypted tokenization <see cref="DefaultProtectedRegexString" /></param>
        /// <param name="protectProvider">an IProtectProvider interface obtained from a one of the supported providers</param>
        /// <exception cref="ArgumentException">thrown if the Regex does not containg a group named protectedData</exception>
        public ProtectProviderConfigurationData(String protectRegexString, String protectedRegexString, String protectedReplaceString, IProtectProvider protectProvider)
        {
            if (!String.IsNullOrEmpty(protectRegexString))
                ProtectRegex = new Regex(protectRegexString);

            if (!String.IsNullOrEmpty(protectedRegexString))
                ProtectedRegex = new Regex(protectedRegexString);

            ProtectedReplaceString = protectedReplaceString;
            ProtectProvider = protectProvider;

            // check resulting configuration is valid, if it is not valid we raise an exception in order to be notified that something is wrong
            CheckConfigurationIsValid();
        }


        /// <summary>
        /// A static helper method which calculates the merge of the global and local protected configuration data. The resulting configuration is checked for validity inside <see cref="ProtectProviderConfigurationData"/> constructor.
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

            // perform merge
            var result = new ProtectProviderConfigurationData(local.ProtectRegex?.ToString() ?? global.ProtectRegex?.ToString(), local.ProtectedRegex?.ToString() ?? global.ProtectedRegex?.ToString(), local.ProtectedReplaceString ?? global.ProtectedReplaceString, local.ProtectProvider ?? global.ProtectProvider);

            return result;
        }
    }

}
