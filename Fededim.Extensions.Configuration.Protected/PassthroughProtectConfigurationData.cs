using System;

namespace Fededim.Extensions.Configuration.Protected
{
    /// <summary>
    /// PassthroughProtectConfigurationData is the ProtectProviderConfigurationData class for the PassthroughProtectProvider
    /// </summary>
    public class PassthroughProtectConfigurationData : ProtectProviderConfigurationData
    {
        public static PassthroughProtectConfigurationData CreateInstance => new PassthroughProtectConfigurationData();

        /// <summary>
        /// Main constructor for PassthroughProtectConfigurationData
        /// </summary>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="protectedReplaceString">a string replacement expression which captures the substitution which must be applied for transforming unencrypted tokenization into an encrypted tokenization</param>
        /// <exception cref="ArgumentException">if one of the input parameters is not well configured</exception>
        public PassthroughProtectConfigurationData(String protectRegexString = null, String protectedRegexString = null, String protectedReplaceString = null)
        : base(protectRegexString, protectedRegexString, protectedReplaceString, new PassthroughProtectProvider())
        {

        }
    }
}
