using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fededim.Extensions.Configuration.Protected.DataProtectionAPI;
using System.IO;

namespace Fededim.Extensions.Configuration.Protected.DataProtectionAPITest
{
    public class DataProtectionAPIProtectProviderTest : ProtectedConfigurationBuilderTest
    {
        private static void ConfigureDataProtection(IDataProtectionBuilder builder)
        {
            builder.UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
            {
                EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                ValidationAlgorithm = ValidationAlgorithm.HMACSHA256,

            }).SetDefaultKeyLifetime(TimeSpan.FromDays(365 * 15)).PersistKeysToFileSystem(new DirectoryInfo("..\\..\\..\\Keys"));
        }

        public DataProtectionAPIProtectProviderTest():base(new DataProtectionAPIProtectConfigurationData(ConfigureDataProtection))
        {
            
        }
    }
}
