# About
Fededim.Extensions.Configuration.Protected is an improved ConfigurationBuilder which allows partial or full encryption of configuration values stored inside any possible ConfigurationSource and fully integrated in the ASP.NET Core architecture. Basically, it implements a custom ConfigurationBuilder and a custom ConfigurationProvider defining a custom tokenization tag which whenever found decrypts the enclosed encrypted data using ASP.NET Core Data Protection API.

# Key Features
- Encrypt partially or fully a configuration value
- Works with any existing and (hopefully) future ConfigurationSource and ConfigurationProvider (tested with CommandLine, EnvironmentVariables, Json, Xml and InMemoryCollection)
- Trasparent in memory decryption of encrypted values without almost any additional line of code
- Supports a global configuration and an eventual custom override for any ConfigurationSource
- Supports almost any NET framework (net6.0, netstandard2.0 and net462)
- Pluggable into any project with almost no changes to original NET / NET Core.
- Supports automatic re-decryption on configuration reload if underlying IConfigurationProvider supports it
- Supports per configuration value derived encryption subkey (called "subpurposes" in Data Protection API)

# How to Use

- Modify the configuration sources by enclosing with the encryption tokenization tag (e.g. Protect:{<data to be encrypted}) all the values or part of values you would like to encrypt
- Configure the data protection api in a helper method (e.g. ConfigureDataProtection)
- Encrypt all appsettings values by calling IDataProtect.ProtectFiles, IDataProtect.ProtectConfigurationValue and IDataProtect.ProtectEnvironmentVariables extension methods (use ProtectedConfigurationBuilder.DataProtectionPurpose as CreateProtector purpose)
- Define the application configuration using ProtectedConfigurationBuilder and adding any standard framework provided or custom configuration source
- Call ProtectedConfigurationBuilder.Build to automatically decrypt the encrypted values and retrieve the cleartext ones into a IConfigurationRoot class.
- Map the Configuration object to a strongly typed hierarchical class using DI Configure

```csharp

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
        var dataProtectorAdditional = serviceProviderDataProtection.GetRequiredService<IDataProtectionProvider>().CreateProtector(ProtectedConfigurationBuilder.ProtectedConfigurationBuilderKeyNumberPurpose(2));

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
                .AddXmlFile("appsettings.xml").WithProtectedConfigurationOptions(protectedRegexString: "OtherProtected(?<subPurposePattern>(:{(?<subPurpose>.+)})?):{(?<protectedData>.+?)}")
                .AddInMemoryCollection(memoryConfiguration).WithProtectedConfigurationOptions(dataProtectionServiceProvider: serviceProviderDataProtection, keyNumber: 2)
                .AddEnvironmentVariables().WithProtectedConfigurationOptions(dataProtectionServiceProvider: serviceProviderDataProtection, keyNumber: 2)
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
```

The main types provided by this library are:

- Fededim.Extensions.Configuration.Protected.ProtectedConfigurationBuilder
- Fededim.Extensions.Configuration.Protected.ProtectedConfigurationProvider
- Fededim.Extensions.Configuration.Protected.ProtectedConfigurationData
- Fededim.Extensions.Configuration.Protected.ConfigurationBuilderExtensions
- Fededim.Extensions.Configuration.Protected.FilesProtectOptions


# Version History
v1.0.0
- Initial commit: it does not support re-decryption on configuration reload
     
v1.0.1
- Added support for automatic re-decryption on configuration reload if underlying IConfigurationProvider supports it.
- Cleaned code and added documentation on most methods.

v1.0.2
- Added more comments on code
- Enabled SourceLink support to GitHub for debugging

v1.0.3
- SourceLink bugfix: removed SourceRevisionId tag in csproj

v1.0.4
- Commented initial unneeded code inside CreateProtectedConfigurationProvider method of ProtectedConfigurationBuilder

v1.0.5
- Commented other initial unneeded code inside CreateProtectedConfigurationProvider method of ProtectedConfigurationBuilder

v1.0.6
- Bugfix: the ProtectFiles method simply read the raw files which need to be encrypted using File.ReadAllText, whereas it should also decode the file according to its format. By default two decoders are now provided for both JSON and XML files and an extension point (FilesDecoding public property) if additional formats must be supported.

v1.0.7
- Improvement: the ProtectedConfigurationProvider.RegisterReloadCallback now uses the framework standard static utility class ChangeToken.OnChange to register the underlying provider configuration changes
- Improvement: added two additional public static properties inside ConfigurationBuilderExtensions in order to allow them to be referenced if needed: JsonDecodingFunction and XmlDecodingFunction

v1.0.8
- Improvement: introduced FileProtectProcessor and IFileProtectProcessor interface for implementing a custom file protect processor used to read, encrypt and return the encrypted file as string. Json, Xml and Raw processors are provided by default.
- Improvement: added custom string parameter purposeString in ProtectedConfigurationBuilder constructor in order to specify a custom purpose string for encryption/decryption, besides the integer keyNumber parameter.
- Improvement: added subPurpose optional part in DefaultProtectRegexString, DefaultProtectedRegexString and DefaultProtectedReplaceString in order to allow an optional per key purpose string override.
- Improvement: added some data to json (one element in Nullable:DoubleArray of appsettings.development.json) and xml file (AutoRetryDelaySubPurpose under TransientFaultHandlingOptions) of Fededim.Extensions.Configuration.Protected.ConsoleTest in order to exemplify the per key purpose string override.

v1.0.9
- No changes, just a rebuild due to a misalignment with symbols.

v1.0.10
- Improvement: Allow the specification of JsonSerializationOptions for JsonFileProtectProcessor to tweak its settings (comments inside JSON files are now skipped by default)
- Improvement: Allow the specification of LoadOptions and SaveOptions for XmlFileProtectProcessor to tweak its settings

v1.0.11
- Improvement: Implemented additional JsonWithCommentsFileProtectProcessor ("hacky" optional FilerProtectProcessor) to allow the preservation of JSON comments when encrypting files using ProtectFiles
- Improvement: Implemented UseJsonWithCommentsFileProtectOption extension method to replace JsonFileProtectProcessor (active by default for compliance with JSON standard of System.Text.Json) with JsonWithCommentsFileProtectProcessor

# Detailed guide

You can find a [detailed article on CodeProject](https://www.codeproject.com/Articles/5374311/Fededim-Extensions-Configuration-Protected-the-ult) explaning the origin, how to use it and the main point of the implementation.


# Feedback & Contributing
Fededim.Extensions.Configuration.Protected is released as open source under the MIT license. Bug reports and contributions are welcome at the [GitHub repository](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.Protected).