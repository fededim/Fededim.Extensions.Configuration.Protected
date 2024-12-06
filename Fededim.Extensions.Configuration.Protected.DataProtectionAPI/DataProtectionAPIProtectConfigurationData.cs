using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.RegularExpressions;

namespace Fededim.Extensions.Configuration.Protected.DataProtectionAPI
{
    /// <summary>
    /// DataProtectionAPIProtectConfigurationData is a custom data structure which stores all Microsoft Data Protection Api configuration options needed by ProtectedConfigurationBuilder and ProtectConfigurationProvider
    /// </summary>
    public class DataProtectionAPIProtectConfigurationData : IProtectProviderConfigurationData
    {
        /// <summary>
        /// The basic purpose string common to all purposes
        /// </summary>
        public const String DataProtectionAPIProtectConfigurationPurpose = "ProtectedConfigurationBuilder";

        /// <summary>
        /// A purpose string based on a key number
        /// </summary>
        /// <param name="keyNumber">a key number used to derive the encryption key</param>
        /// <returns>a purpose string</returns>
        public static String DataProtectionAPIProtectConfigurationKeyNumberPurpose(int keyNumber) => DataProtectionAPIProtectConfigurationStringPurpose(DataProtectionAPIProtectConfigurationKeyNumberToString(keyNumber));

        /// <summary>
        /// A purpose string based on a custom string
        /// </summary>
        /// <param name="purpose">a string used to derive the encryption key</param>
        /// <returns>a purpose string</returns>
        public static String DataProtectionAPIProtectConfigurationStringPurpose(String purpose)
        {
            if (String.IsNullOrEmpty(purpose))
                return DataProtectionAPIProtectConfigurationPurpose;

            return $"{DataProtectionAPIProtectConfigurationPurpose}.{purpose}";
        }

        /// <summary>
        /// internal function for mapping key number into a string
        /// </summary>
        /// <param name="keyNumber">a key number used to derive the encryption key</param>
        /// <returns>a string containing the key number</returns>
        internal static String DataProtectionAPIProtectConfigurationKeyNumberToString(int keyNumber) => $"Key{keyNumber}";




        /// <summary>
        /// Creates a standard DataProtection API configuration using the specified <see cref="dataProtectionServiceProvider"/><br/><br/>
        /// - default tokenization (e.g. <see cref="IProtectProviderConfigurationData.DefaultProtectRegexString"/>, <see cref="IProtectProviderConfigurationData.DefaultProtectedRegexString"/> and <see cref="IProtectProviderConfigurationData.DefaultProtectedReplaceString"/>)<br/>
        /// - key number purpose set to 1
        /// </summary>
        /// <param name="dataProtectionServiceProvider">a service provider configured with Data Protection API, which must resolve the <see cref="IDataProtectionProvider"/> interface</param>
        /// <exception cref="ArgumentException">if either dataProtectionServiceProvider or dataProtectionConfigureAction are null or not well configured</exception>
        public DataProtectionAPIProtectConfigurationData(IServiceProvider dataProtectionServiceProvider)
            : this(null, null, null, dataProtectionServiceProvider, null, 1)
        {
        }


        /// <summary>
        /// Creates a DataProtection API configuration using the specified <see cref="dataProtectionServiceProvider"/> and <see cref="keyNumber"/><br/><br/>
        /// - default tokenization (e.g. <see cref="IProtectProviderConfigurationData.DefaultProtectRegexString"/>, <see cref="IProtectProviderConfigurationData.DefaultProtectedRegexString"/> and <see cref="IProtectProviderConfigurationData.DefaultProtectedReplaceString"/>)<br/>
        /// </summary>
        /// <param name="dataProtectionServiceProvider">a service provider configured with Data Protection API, which must resolve the <see cref="IDataProtectionProvider"/> interface</param>
        /// <param name="keyNumber">a number specifying the index of the key to use</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="protectedReplaceString">a string replacement expression which captures the substitution which must be applied for transforming unencrypted tokenization into an encrypted tokenization</param>
        /// <exception cref="ArgumentException">if either dataProtectionServiceProvider or dataProtectionConfigureAction are null or not well configured</exception>
        public DataProtectionAPIProtectConfigurationData(IServiceProvider dataProtectionServiceProvider, int keyNumber, String protectRegexString = null, String protectedRegexString = null, String protectedReplaceString = null)
            : this(protectRegexString, protectedRegexString, protectedReplaceString, dataProtectionServiceProvider, null, keyNumber)
        {
        }



        /// <summary>
        /// Creates a DataProtection API configuration using the specified <see cref="dataProtectionServiceProvider"/> and <see cref="purposeString"/><br/><br/>
        /// - default tokenization (e.g. <see cref="IProtectProviderConfigurationData.DefaultProtectRegexString"/>, <see cref="IProtectProviderConfigurationData.DefaultProtectedRegexString"/> and <see cref="IProtectProviderConfigurationData.DefaultProtectedReplaceString"/>)<br/>
        /// </summary>
        /// <param name="dataProtectionServiceProvider">a service provider configured with Data Protection API, which must resolve the <see cref="IDataProtectionProvider"/> interface</param>
        /// <param name="purposeString">a string used to derive the encryption key</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="protectedReplaceString">a string replacement expression which captures the substitution which must be applied for transforming unencrypted tokenization into an encrypted tokenization</param>
        /// <exception cref="ArgumentException">if either dataProtectionServiceProvider or dataProtectionConfigureAction are null or not well configured</exception>
        public DataProtectionAPIProtectConfigurationData(IServiceProvider dataProtectionServiceProvider, String purposeString, String protectRegexString = null, String protectedRegexString = null, String protectedReplaceString = null)
            : this(protectRegexString, protectedRegexString, protectedReplaceString, dataProtectionServiceProvider, null, purposeString)
        {
        }




        /// <summary>
        /// Creates a standard DataProtection API configuration using the specified <see cref="dataProtectionConfigureAction"/><br/><br/>
        /// - default tokenization (e.g. <see cref="IProtectProviderConfigurationData.DefaultProtectRegexString"/>, <see cref="IProtectProviderConfigurationData.DefaultProtectedRegexString"/> and <see cref="IProtectProviderConfigurationData.DefaultProtectedReplaceString"/>)<br/>
        /// - key number purpose set to 1
        /// </summary>
        /// <param name="dataProtectionConfigureAction">a configure action to setup the Data Protection API</param>
        /// <exception cref="ArgumentException">if either dataProtectionServiceProvider or dataProtectionConfigureAction are null or not well configured</exception>
        public DataProtectionAPIProtectConfigurationData(Action<IDataProtectionBuilder> dataProtectionConfigureAction)
            : this(null, null, null, null, dataProtectionConfigureAction, 1)
        {
        }



        /// <summary>
        /// Creates a DataProtection API configuration using the specified <see cref="dataProtectionConfigureAction"/> and <see cref="keyNumber"/><br/><br/>
        /// - default tokenization (e.g. <see cref="IProtectProviderConfigurationData.DefaultProtectRegexString"/>, <see cref="IProtectProviderConfigurationData.DefaultProtectedRegexString"/> and <see cref="IProtectProviderConfigurationData.DefaultProtectedReplaceString"/>)<br/>
        /// </summary>
        /// <param name="dataProtectionConfigureAction">a configure action to setup the Data Protection API</param>
        /// <param name="keyNumber">a number specifying the index of the key to use</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="protectedReplaceString">a string replacement expression which captures the substitution which must be applied for transforming unencrypted tokenization into an encrypted tokenization</param>
        /// <exception cref="ArgumentException">if either dataProtectionServiceProvider or dataProtectionConfigureAction are null or not well configured</exception>
        public DataProtectionAPIProtectConfigurationData(Action<IDataProtectionBuilder> dataProtectionConfigureAction, int keyNumber, String protectRegexString = null, String protectedRegexString = null, String protectedReplaceString = null)
            : this(protectRegexString, protectedRegexString, protectedReplaceString, null, dataProtectionConfigureAction, keyNumber)
        {
        }



        /// <summary>
        /// Creates a DataProtection API configuration using the specified <see cref="dataProtectionServiceProvider"/> and <see cref="purposeString"/><br/><br/>
        /// - default tokenization (e.g. <see cref="IProtectProviderConfigurationData.DefaultProtectRegexString"/>, <see cref="IProtectProviderConfigurationData.DefaultProtectedRegexString"/> and <see cref="IProtectProviderConfigurationData.DefaultProtectedReplaceString"/>)<br/>
        /// </summary>
        /// <param name="dataProtectionConfigureAction">a configure action to setup the Data Protection API</param>
        /// <param name="purposeString">a string used to derive the encryption key</param>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="protectedReplaceString">a string replacement expression which captures the substitution which must be applied for transforming unencrypted tokenization into an encrypted tokenization</param>
        /// <exception cref="ArgumentException">if either dataProtectionServiceProvider or dataProtectionConfigureAction are null or not well configured</exception>
        public DataProtectionAPIProtectConfigurationData(Action<IDataProtectionBuilder> dataProtectionConfigureAction, String purposeString, String protectRegexString = null, String protectedRegexString = null, String protectedReplaceString = null)
            : this(protectRegexString, protectedRegexString, protectedReplaceString, null, dataProtectionConfigureAction, purposeString)
        {
        }




        /// <summary>
        /// Main constructor for DataProtectionAPIProtectConfigurationData using a key number
        /// </summary>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="protectedReplaceString">a string replacement expression which captures the substitution which must be applied for transforming unencrypted tokenization into an encrypted tokenization</param>
        /// <param name="dataProtectionServiceProvider">a service provider configured with Data Protection API, this parameters is mutually exclusive to dataProtectionConfigureAction</param>
        /// <param name="dataProtectionConfigureAction">a configure action to setup the Data Protection API, this parameters is mutually exclusive to dataProtectionServiceProvider</param>
        /// <param name="keyNumber">a number specifying the index of the key to use</param>
        /// <exception cref="ArgumentException">if either dataProtectionServiceProvider or dataProtectionConfigureAction are null or not well configured</exception>
        public DataProtectionAPIProtectConfigurationData(String protectRegexString = null, String protectedRegexString = null, String protectedReplaceString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, int keyNumber = 1)
            : this(protectRegexString, protectedRegexString, protectedReplaceString, dataProtectionServiceProvider, dataProtectionConfigureAction, DataProtectionAPIProtectConfigurationKeyNumberToString(keyNumber))
        {

        }



        /// <summary>
        /// Main constructor for DataProtectionAPIProtectConfigurationData using a purpose string
        /// </summary>
        /// <param name="protectRegexString">a regular expression which captures the data to be encrypted in a named group called protectData</param>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="protectedReplaceString">a string replacement expression which captures the substitution which must be applied for transforming unencrypted tokenization into an encrypted tokenization</param>
        /// <param name="dataProtectionServiceProvider">a service provider configured with Data Protection API, this parameters is mutually exclusive to dataProtectionConfigureAction</param>
        /// <param name="dataProtectionConfigureAction">a configure action to setup the Data Protection API, this parameters is mutually exclusive to dataProtectionServiceProvider</param>
        /// <param name="purposeString">a string used to derive the encryption key</param>
        /// <exception cref="ArgumentException">if either dataProtectionServiceProvider or dataProtectionConfigureAction are null or not well configured</exception>
        public DataProtectionAPIProtectConfigurationData(String protectRegexString = null, String protectedRegexString = null, String protectedReplaceString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, String purposeString = null)
        {
            // check that at least one parameter is not null
            if (dataProtectionServiceProvider == null && dataProtectionConfigureAction == null)
                throw new ArgumentException("Either dataProtectionServiceProvider or dataProtectionConfigureAction must not be null!");

            // if dataProtectionServiceProvider is null and we pass a dataProtectionConfigureAction configure a new service provider
            if (dataProtectionServiceProvider == null && dataProtectionConfigureAction != null)
            {
                var services = new ServiceCollection();
                dataProtectionConfigureAction(services.AddDataProtection());
                dataProtectionServiceProvider = services.BuildServiceProvider();
            }

            // check that dataProtectionServiceProvider resolves the IDataProtector
            var dataProtect = dataProtectionServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector(DataProtectionAPIProtectConfigurationStringPurpose(purposeString));
            if (dataProtect == null)
                throw new ArgumentException("Either dataProtectionServiceProvider or dataProtectionConfigureAction must configure the DataProtection services!", dataProtectionServiceProvider == null ? nameof(dataProtectionServiceProvider) : nameof(dataProtectionConfigureAction));

            // sets the abstract class base properties and calls CheckConfigurationIsValid
            if (!String.IsNullOrEmpty(protectRegexString))
                ProtectRegex = new Regex(protectRegexString);

            if (!String.IsNullOrEmpty(protectedRegexString))
                ProtectedRegex = new Regex(protectedRegexString);

            ProtectedReplaceString = protectedReplaceString;

            ProtectProvider = new DataProtectionAPIProtectProvider(dataProtect);

            CheckConfigurationIsValid();
        }
    }
}
