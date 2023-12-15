using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fededim.Extensions.Configuration.Protected
{
    public class ProtectedConfigurationData
    {
        public IDataProtector DataProtector { get; }
        public Regex ProtectedRegex { get; }


        public ProtectedConfigurationData(Regex protectedRegex, IDataProtector dataProtector)
        {
            DataProtector = dataProtector;
            ProtectedRegex = protectedRegex;
        }


        public ProtectedConfigurationData(String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, int keyNumber=1)
        {
            // check that at least one parameter is not null
            if (String.IsNullOrEmpty(protectedRegexString) && dataProtectionServiceProvider == null && dataProtectionConfigureAction == null)
                throw new ArgumentException("Either protectedRegexString or dataProtectionServiceProvider or dataProtectionConfigureAction must not be null!");

            // if dataProtectionServiceProvider is null and we pass a dataProtectionConfigureAction configure a new service provider
            if (dataProtectionServiceProvider == null && dataProtectionConfigureAction != null)
            {
                var services = new ServiceCollection();
                dataProtectionConfigureAction(services.AddDataProtection());
                dataProtectionServiceProvider = services.BuildServiceProvider();
            }

            // if dataProtectionServiceProvider is not null check that it resolves the IDataProtector
            if ((dataProtectionServiceProvider != null) &&
                ((DataProtector = dataProtectionServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector(ProtectedConfigurationBuilder.DataProtectionPurpose(keyNumber))) == null))
                throw new ArgumentException("Either dataProtectionServiceProvider or dataProtectionConfigureAction must configure the DataProtection services!", dataProtectionServiceProvider == null ? nameof(dataProtectionServiceProvider) : nameof(dataProtectionConfigureAction));


            // check that Regex contains a group named protectedData
            ProtectedRegex = new Regex(protectedRegexString ?? ProtectedConfigurationBuilder.DefaultProtectedRegexString);
            if (!ProtectedRegex.GetGroupNames().Contains("protectedData"))
                throw new ArgumentException("Regex must contain a group named protectedData!", nameof(protectedRegexString));
        }



        public bool IsValid => (DataProtector != null) && (ProtectedRegex?.GetGroupNames()?.Contains("protectedData") == true);


        public static ProtectedConfigurationData Merge(ProtectedConfigurationData global, ProtectedConfigurationData local)
        {
            if (local == null)
                return global;
            
            if (global == null)
                return local;

            return new ProtectedConfigurationData(local.ProtectedRegex ?? global.ProtectedRegex, local.DataProtector ?? global.DataProtector);                  
        }
    }
}
