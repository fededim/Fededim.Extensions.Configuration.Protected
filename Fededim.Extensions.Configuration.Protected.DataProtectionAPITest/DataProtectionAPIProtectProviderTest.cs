using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection;
using System;
using Fededim.Extensions.Configuration.Protected.DataProtectionAPI;
using System.IO;
using Xunit.Abstractions;
using Xunit;

namespace Fededim.Extensions.Configuration.Protected.DataProtectionAPITest
{

    public class DataProtectionAPIProtectProviderTest : ProtectedConfigurationBuilderTest, IClassFixture<ProtectedConfigurationBuilderTestFixture>
    {
        private static void ConfigureDataProtection(IDataProtectionBuilder builder)
        {
            builder.UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
            {
                EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                ValidationAlgorithm = ValidationAlgorithm.HMACSHA256,

            }).SetDefaultKeyLifetime(TimeSpan.FromDays(365 * 15)).PersistKeysToFileSystem(new DirectoryInfo("..\\..\\..\\Keys"));
        }



        public DataProtectionAPIProtectProviderTest(ProtectedConfigurationBuilderTestFixture context, ITestOutputHelper testOutputHelper) :base(context, testOutputHelper,new DataProtectionAPIProtectConfigurationData(ConfigureDataProtection))
        {
            
        }


        protected override string TrimRegexCharsFromSubpurpose(string subpurpose)
        {
            return subpurpose.Replace(":", "*").Replace("}", "|");
        }


        protected override string TrimRegexCharsFromProtectData(string value)
        {
            return value.Replace("}", "|");
        }
    }
}
