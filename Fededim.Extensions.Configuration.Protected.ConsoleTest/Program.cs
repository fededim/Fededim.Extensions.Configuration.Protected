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
        args = new String[] { "--EncryptedCommandLinePassword", "Protect:{secretArgPassword!\\*+?|{[()^$.#}", "--PlainTextCommandLinePassword", "secretArgPassword!\\*+?|{[()^$.#" };

        // define the DI services: setup Data Protection API
        var servicesDataProtection = new ServiceCollection();
        ConfigureDataProtection(servicesDataProtection.AddDataProtection());
        var serviceProviderDataProtection = servicesDataProtection.BuildServiceProvider();

        // define the DI services: setup a Data Protection API custom tailored for a particular providers (InMemory and Environment Variables)

        // retrieve IDataProtector interfaces for encrypting data
        var dataProtector = serviceProviderDataProtection.GetRequiredService<IDataProtectionProvider>().CreateProtector(ProtectedConfigurationBuilder.ProtectedConfigurationBuilderKeyNumberPurpose(1));
        var dataProtectorAdditional = serviceProviderDataProtection.GetRequiredService<IDataProtectionProvider>().CreateProtector(ProtectedConfigurationBuilder.ProtectedConfigurationBuilderStringPurpose("MagicPurpose"));

        // activates JsonWithCommentsFileProtectProcessor
        ConfigurationBuilderExtensions.UseJsonWithCommentsFileProtectOption();

        // define in-memory configuration key-value pairs to be encrypted
        var memoryConfiguration = new Dictionary<String, String>
        {
            ["EncryptedInMemorySecretKey"] = "Protect:{InMemory MyKey Value}",
            ["PlainTextInMemorySecretKey"] = "InMemory MyKey Value",
            ["TransientFaultHandlingOptions:Enabled"] = bool.FalseString,
            ["Logging:LogLevel:Default"] = "Protect:{Warning}",
            ["UserDomain"] = "Protect:{DOMAIN\\USER}",
            ["EncryptedInMemorySpecialCharacters"] = "Protect:{\\!*+?|{[()^$.#}",
            ["PlainTextInMemorySpecialCharacters"] = "\\!*+?|{[()^$.#"
        };

        // define an environment variable to be encrypted
        Environment.SetEnvironmentVariable("EncryptedEnvironmentPassword", "Protect:{SecretEnvPassword\\!*+?|{[()^$.#}");
        Environment.SetEnvironmentVariable("PlainTextEnvironmentPassword", "SecretEnvPassword\\!*+?|{[()^$.#");

        // encrypts all configuration sources (must be done before reading the configuration)

        // encrypts all Protect:{<data>} token tags inside command line argument (you can use also the same method to encrypt String, IEnumerable<String>, IDictionary<String,String> value of any configuration source
        var encryptedArgs = dataProtector.ProtectConfigurationValue(args);

        // encrypts all Protect:{<data>} token tags inside im-memory dictionary
        dataProtectorAdditional.ProtectConfigurationValue(memoryConfiguration);

        // encrypts all Protect:{<data>} token tags inside .json files and all OtherProtect:{<data>} inside .xml files 
        var encryptedJsonFiles = dataProtector.ProtectFiles(".");
        var encryptedXmlFiles = dataProtector.ProtectFiles(".", searchPattern: "*.xml", protectRegexString: "OtherProtect(?<subPurposePattern>(:{(?<subPurpose>.+)})?):{(?<protectData>.+?)}", protectedReplaceString: "OtherProtected${subPurposePattern}:{${protectedData}}");

        // encrypts all Protect:{<data>} token tags inside environment variables
        dataProtectorAdditional.ProtectEnvironmentVariables();

        // please check that all configuration source defined above are encrypted (check also Environment.GetEnvironmentVariable("SecretEnvironmentPassword") in Watch window)
        // note the per key purpose string override in file appsettings.development.json inside Nullable:DoubleArray contains two elements one with "Protect:{3.14}" and one with "Protect:{%customSubPurpose%}:{3.14}", even though the value is the same (3.14) they are encrypted differently due to the custom key purpose string
        // note the per key purpose string override in file appsettings.xml inside TransientFaultHandlingOptions contains two elements AutoRetryDelay with "OtherProtect:{00:00:07}" and AutoRetryDelaySubPurpose with "OtherProtect:{sUbPuRpOsE}:{00:00:07}", even though the value is the same (00:00:07) they are encrypted differently due to the custom key purpose string
        Debugger.Break();

        // define the application configuration using almost all possible known ConfigurationSources
        var configuration = new ProtectedConfigurationBuilder(dataProtectionServiceProvider: serviceProviderDataProtection)
                .AddCommandLine(encryptedArgs)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")}.json", false, true)
                .AddXmlFile("appsettings.xml").WithProtectedConfigurationOptions(protectedRegexString: "OtherProtected(?<subPurposePattern>(:{(?<subPurpose>.+)})?):{(?<protectedData>.+?)}", keyNumber: 1)
                .AddInMemoryCollection(memoryConfiguration).WithProtectedConfigurationOptions(dataProtectionServiceProvider: serviceProviderDataProtection, purposeString: "MagicPurpose")
                .AddEnvironmentVariables().WithProtectedConfigurationOptions(dataProtectionServiceProvider: serviceProviderDataProtection, purposeString: "MagicPurpose")
                .Build();

        // define other DI services: configure strongly typed AppSettings configuration class (must be done after having read the configuration)
        var services = new ServiceCollection();
        services.Configure<AppSettings>(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // retrieve the strongly typed AppSettings configuration class, we use IOptionsMonitor in order to be notified on any reloads of appSettings
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<AppSettings>>();
        var appSettings = optionsMonitor.CurrentValue;
        optionsMonitor.OnChange(appSettingsReloaded =>
        {
            // this breakpoint gets hit when the appsettings have changed due to a configuration reload, please check that the value of "Int" property inside appSettingsReloaded class is different from the one inside appSettings class
            // note that also there is an unavoidable framework bug on ChangeToken.OnChange which could get called multiple times when using FileSystemWatchers see https://github.com/dotnet/aspnetcore/issues/2542
            // see also the remarks section of FileSystemWatcher https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.created?view=net-8.0#remarks
            Console.WriteLine($"OnChangeEvent: appsettings.{Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")}.json has been reloaded! appSettings Int {appSettings.Int} appSettingsReloaded {appSettingsReloaded.Int}");
            Debugger.Break();
        });

        // please check that all values inside appSettings class are actually decrypted with the right value, make a note of the value of "Int" property it will change on the next second breakpoint
        Debugger.Break();

        // added some simple assertions to test that decrypted value is the same as original plaintext one
        Debug.Assert(appSettings.EncryptedCommandLinePassword == appSettings.PlainTextCommandLinePassword);
        Debug.Assert(appSettings.EncryptedEnvironmentPassword == appSettings.PlainTextEnvironmentPassword);
        Debug.Assert(appSettings.EncryptedJsonSpecialCharacters == appSettings.PlainTextJsonSpecialCharacters);
        Debug.Assert(appSettings.EncryptedXmlSecretKey == appSettings.PlainTextXmlSecretKey);
        Debug.Assert(appSettings.EncryptedInMemorySecretKey == appSettings.PlainTextInMemorySecretKey);

        // multiple configuration reload example
        int i = 0;
        while (i++ < 5)
        {
            // updates inside appsettings.<environment>.json the property "Int": <whatever>, --> "Int": "Protected:{<random number>},"
            var environmentAppSettings = File.ReadAllText($"appsettings.{Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")}.json");
            environmentAppSettings = new Regex("\"Int\":.+?,").Replace(environmentAppSettings, $"\"Int\": \"{dataProtector.ProtectConfigurationValue($"Protect:{{{new Random().Next(0, 1000000)}}}")}\",");
            File.WriteAllText($"appsettings.{Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")}.json", environmentAppSettings);

            // wait 5 seconds for the reload to take place, please check on this breakpoint that the value of "Int" property has changed in appSettings class and it is the same of appSettingsReloaded
            Thread.Sleep(5000);
            appSettings = optionsMonitor.CurrentValue;
            Console.WriteLine($"ConfigurationReloadLoop: appSettings Int {appSettings.Int}");
            Debugger.Break();
        }
    }
}