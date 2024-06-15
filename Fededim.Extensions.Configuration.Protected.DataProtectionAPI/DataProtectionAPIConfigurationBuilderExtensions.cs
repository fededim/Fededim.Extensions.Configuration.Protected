using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using System;

namespace Fededim.Extensions.Configuration.Protected.DataProtectionAPI
{
    /// <summary>
    /// ConfigurationBuilderExtensions is a static class providing different extensions methods to IConfigurationBuilder and IDataProtect interfaces
    /// </summary>
    public static class DataProtectionAPIConfigurationBuilderExtensions
    {
        /// <summary>
        /// WithProtectedConfigurationOptions is a helper method which allows to override the global protected configuration data specified in the ProtectedConfigurationBuilder for a particular ConfigurationProvider (the last one added)
        /// </summary>
        /// <param name="configurationBuilder">the IConfigurationBuilder instance</param>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="dataProtectionServiceProvider">a service provider configured with Data Protection API, this parameters is mutually exclusive to dataProtectionConfigureAction</param>
        /// <param name="dataProtectionConfigureAction">a configure action to setup the Data Protection API, this parameters is mutually exclusive to dataProtectionServiceProvider</param>
        /// <param name="keyNumber">a number specifying the index of the key to use</param>
        /// <returns>The <see cref="IConfigurationBuilder"/> interface for method chaining</returns>
        /// <exception cref="ArgumentException">if configurationBuilder is not an instance of ProtectedConfigurationBuilder class</exception>
        public static IConfigurationBuilder WithProtectedConfigurationOptions(this IConfigurationBuilder configurationBuilder, String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, int keyNumber = 1)
        {
            var protectedConfigurationBuilder = configurationBuilder as IProtectedConfigurationBuilder;

            if (protectedConfigurationBuilder != null)
                return protectedConfigurationBuilder.WithProtectedConfigurationOptions(new DataProtectionAPIProtectConfigurationData(protectedRegexString, dataProtectionServiceProvider, dataProtectionConfigureAction, DataProtectionAPIProtectConfigurationData.DataProtectionAPIProtectConfigurationKeyNumberToString(keyNumber)));
            else
                throw new ArgumentException("Please use ProtectedConfigurationBuilder instead of ConfigurationBuilder class!", nameof(configurationBuilder));

        }




        /// <summary>
        /// WithProtectedConfigurationOptions is a helper method which allows to override the global protected configuration data specified in the ProtectedConfigurationBuilder for a particular ConfigurationProvider (the last one added)
        /// </summary>
        /// <param name="configurationBuilder">the IConfigurationBuilder instance</param>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="dataProtectionServiceProvider">a service provider configured with Data Protection API, this parameters is mutually exclusive to dataProtectionConfigureAction</param>
        /// <param name="dataProtectionConfigureAction">a configure action to setup the Data Protection API, this parameters is mutually exclusive to dataProtectionServiceProvider</param>
        /// <param name="purposeString">a string used to derive the encryption key</param>
        /// <returns>The <see cref="IConfigurationBuilder"/> interface for method chaining</returns>
        /// <exception cref="ArgumentException">if configurationBuilder is not an instance of ProtectedConfigurationBuilder class</exception>
        public static IConfigurationBuilder WithProtectedConfigurationOptions(this IConfigurationBuilder configurationBuilder, String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, string purposeString = null)
        {
            var protectedConfigurationBuilder = configurationBuilder as IProtectedConfigurationBuilder;

            if (protectedConfigurationBuilder != null)
                return protectedConfigurationBuilder.WithProtectedConfigurationOptions(new DataProtectionAPIProtectConfigurationData(protectedRegexString, dataProtectionServiceProvider, dataProtectionConfigureAction, purposeString));
            else
                throw new ArgumentException("Please use ProtectedConfigurationBuilder instead of ConfigurationBuilder class!", nameof(configurationBuilder));

        }
    }
}
