using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Fededim.Extensions.Configuration.Protected.ConsoleTest;
using Microsoft.Extensions.Options;
using Fededim.Extensions.Configuration.Protected;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

public class Program
{
    private static void ConfigureDataProtection(IDataProtectionBuilder builder)
    {
        builder.UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
        {
            EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
            ValidationAlgorithm = ValidationAlgorithm.HMACSHA256,

        }).SetDefaultKeyLifetime(TimeSpan.FromDays(365 * 15)).PersistKeysToFileSystem(new DirectoryInfo("..\\..\\..\\Keys"));
    }



    public static void Main(String[] args)
    {
        args = new String[] { "--password Protect:{secretArgPassword!}" };

        // define the DI services: setup Data Protection API
        var servicesDataProtection = new ServiceCollection();
        ConfigureDataProtection(servicesDataProtection.AddDataProtection());
        var serviceProviderDataProtection = servicesDataProtection.BuildServiceProvider();

        // define the DI services: setup a Data Protection API custom tailored for a particular providers (InMemory and Environment Variables)

        // retrieve IDataProtector interfaces for encrypting data
        var dataProtector = serviceProviderDataProtection.GetRequiredService<IDataProtectionProvider>().CreateProtector(ProtectedConfigurationBuilder.DataProtectionPurpose());
        var dataProtectorAdditional = serviceProviderDataProtection.GetRequiredService<IDataProtectionProvider>().CreateProtector(ProtectedConfigurationBuilder.DataProtectionPurpose(2));


        // define in-memory configuration key-value pairs to be encrypted
        var memoryConfiguration = new Dictionary<String, String>
        {
            ["SecretKey"] = "Protect:{InMemory MyKey Value}",
            ["TransientFaultHandlingOptions:Enabled"] = bool.FalseString,
            ["Logging:LogLevel:Default"] = "Protect:{Warning}"
        };

        // define an environment variable to be encrypted
        Environment.SetEnvironmentVariable("SecretEnvironmentPassword", "Protect:{SecretEnvPassword!}");



        // encrypts all configuration sources (must be done before reading the configuration)

        // encrypts all Protect:{<data>} token tags inside command line argument (you can use also the same method to encrypt String, IEnumerable<String>, IDictionary<String,String> value of any configuration source
        var encryptedArgs = dataProtector.ProtectConfigurationValue(args);

        // encrypts all Protect:{<data>} token tags inside im-memory dictionary
        dataProtectorAdditional.ProtectConfigurationValue(memoryConfiguration);

        // encrypts all Protect:{<data>} token tags inside .json files and all OtherProtect:{<data>} inside .xml files 
        var encryptedJsonFiles = dataProtector.ProtectFiles(".");
        var encryptedXmlFiles = dataProtector.ProtectFiles(".", searchPattern: "*.xml", protectRegexString: "OtherProtect:{(?<protectData>.+?)}", protectedReplaceString: "OtherProtected:{${protectedData}}");

        // encrypts all Protect:{<data>} token tags inside environment variables
        dataProtectorAdditional.ProtectEnvironmentVariables();

        // please check that all configuration source defined above are encrypted (check also Environment.GetEnvironmentVariable("SecretEnvironmentPassword") in Watch window)
        Debugger.Break();

        // define the application configuration using almost all possible known ConfigurationSources
        var configuration = new ProtectedConfigurationBuilder(dataProtectionServiceProvider: serviceProviderDataProtection)
                .AddCommandLine(encryptedArgs)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")}.json", false, true)
                .AddXmlFile("appsettings.xml").WithProtectedConfigurationOptions(protectedRegexString: "OtherProtected:{(?<protectedData>.+?)}")
                .AddInMemoryCollection(memoryConfiguration).WithProtectedConfigurationOptions(dataProtectionServiceProvider: serviceProviderDataProtection, keyNumber: 2)
                .AddEnvironmentVariables().WithProtectedConfigurationOptions(dataProtectionServiceProvider: serviceProviderDataProtection, keyNumber: 2)
                .Build();

        // define other DI services: configure strongly typed AppSettings configuration class (must be done after having read the configuration)
        var services = new ServiceCollection();
        services.Configure<AppSettings>(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // retrieve the strongly typed AppSettings configuration class, we use IOptionsMonitor in order to be notified on any reloads of appSettings
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<AppSettings>>();
        optionsMonitor.OnChange(appSettingsReloaded => {
            // this breakpoint gets hit when the appsettings have changed due to a configuration reload, please check that the value of "Int" property inside appSettingsReloaded class is different from the one inside appSettings class
            Console.WriteLine($"appsettings.{Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")}.json has been reloaded!");
            Debugger.Break();
        });
        var appSettings = optionsMonitor.CurrentValue;

        // please check that all values inside appSettings class are actually decrypted with the right value, make a note of the value of "Int" property it will change on the next second breakpoint
        Debugger.Break();

        // configuration reload example, updates inside appsettings.<environment>.json the property "Int": <whatever>, --> "Int": "Protected:{<random number>},"
        var environmentAppSettings = File.ReadAllText($"appsettings.{Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")}.json");
        environmentAppSettings = new Regex("\"Int\":.+?,").Replace(environmentAppSettings, $"\"Int\": \"{dataProtector.ProtectConfigurationValue($"Protect:{{{new Random().Next(0, 100000)}}}")}\",");
        File.WriteAllText($"appsettings.{Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")}.json", environmentAppSettings);

        // wait 3 seconds for the reload to take place, please check on this breakpoint that the value of "Int" property has changed in appSettings class and it is the same of appSettingsReloaded
        Thread.Sleep(5000);
        appSettings = optionsMonitor.CurrentValue;
        Debugger.Break();
    }
}