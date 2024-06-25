# About
Fededim.Extensions.Configuration.Protected.DataProtectionAPI is an improved ConfigurationBuilder which allows partial or full encryption of configuration values stored inside any possible ConfigurationSource and fully integrated in the ASP.NET Core architecture. Basically, it implements a custom ConfigurationBuilder and a custom ConfigurationProvider defining a custom tokenization tag which whenever found decrypts the enclosed encrypted data using the Microsoft Data Protection API.

# Key Features
- Encrypt partially or fully a configuration value
- Works with any existing and (hopefully) future ConfigurationSource and ConfigurationProvider (tested with CommandLine, EnvironmentVariables, Json, Xml and InMemoryCollection)
- Trasparent in memory decryption of encrypted values without almost any additional line of code
- Supports a global configuration and an eventual custom override for any ConfigurationSource
- Supports almost any NET framework (net6.0, netstandard2.0 and net462)
- Pluggable into any project with almost no changes to original NET / NET Core.
- Supports automatic re-decryption on configuration reload if underlying IConfigurationProvider supports it
- Supports per configuration value encryption derived subkey (called "subpurposes")
- Supports pluggable encryption/decryption with different providers implementing a standard interface IProtectProvider (keep in mind that implementing a secure and robust encryption/decryption provider requires a deep knowledge of security!).

# How to Use

- Modify the configuration sources by enclosing with the encryption tokenization tag (e.g. Protect:{<data to be encrypted}) all the values or part of values you would like to encrypt
- Configure the data protection api in a helper method (e.g. ConfigureDataProtection)
- Encrypt all appsettings values by calling IProtectProviderConfigurationData.ProtectFiles, IProtectProviderConfigurationData.ProtectConfigurationValue and IProtectProviderConfigurationData.ProtectEnvironmentVariables extension methods
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
using Fededim.Extensions.Configuration.Protected.DataProtectionAPI;

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


        // creates all the DataProtectionAPIProtectConfigurationData classes specifying three different provider configurations

        // standard configuration using key number purpose
        var standardProtectConfigurationData = new DataProtectionAPIProtectConfigurationData(serviceProviderDataProtection);

        // standard configuration using key number purpose overridden with a custom tokenization
        var otherProtectedTokenizationProtectConfigurationData = new DataProtectionAPIProtectConfigurationData(serviceProviderDataProtection,2, protectRegexString: "OtherProtect(?<subPurposePattern>(:{(?<subPurpose>[^:}]+)})?):{(?<protectData>.+?)}", protectedRegexString: "OtherProtected(?<subPurposePattern>(:{(?<subPurpose>[^:}]+)})?):{(?<protectedData>.+?)}", protectedReplaceString: "OtherProtected${subPurposePattern}:{${protectedData}}");

        // standard configuration using string purpose
        var magicPurposeStringProtectConfigurationData = new DataProtectionAPIProtectConfigurationData(serviceProviderDataProtection, "MagicPurpose"); 



        // activates JsonWithCommentsProtectFileProcessor
        ConfigurationBuilderExtensions.UseJsonWithCommentsProtectFileOption();

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
        var encryptedArgs = standardProtectConfigurationData.ProtectConfigurationValue(args);

        // encrypts all Protect:{<data>} token tags inside im-memory dictionary
        magicPurposeStringProtectConfigurationData.ProtectConfigurationValue(memoryConfiguration);

        // encrypts all Protect:{<data>} token tags inside .json files and all OtherProtect:{<data>} inside .xml files 
        var encryptedJsonFiles = standardProtectConfigurationData.ProtectFiles(".");
        var encryptedXmlFiles = otherProtectedTokenizationProtectConfigurationData.ProtectFiles(".", searchPattern: "*.xml");

        // encrypts all Protect:{<data>} token tags inside environment variables
        magicPurposeStringProtectConfigurationData.ProtectEnvironmentVariables();

        // please check that all configuration source defined above are encrypted (check also Environment.GetEnvironmentVariable("SecretEnvironmentPassword") in Watch window)
        // note the per key purpose string override in file appsettings.development.json inside Nullable:DoubleArray contains two elements one with "Protect:{3.14}" and one with "Protect:{%customSubPurpose%}:{3.14}", even though the value is the same (3.14) they are encrypted differently due to the custom key purpose string
        // note the per key purpose string override in file appsettings.xml inside TransientFaultHandlingOptions contains two elements AutoRetryDelay with "OtherProtect:{00:00:07}" and AutoRetryDelaySubPurpose with "OtherProtect:{sUbPuRpOsE}:{00:00:07}", even though the value is the same (00:00:07) they are encrypted differently due to the custom key purpose string
        Debugger.Break();

        // define the application configuration using almost all possible known ConfigurationSources
        var configuration = new ProtectedConfigurationBuilder(standardProtectConfigurationData)  // global configuration
                .AddCommandLine(encryptedArgs)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")}.json", false, true)
                .AddXmlFile("appsettings.xml").WithProtectedConfigurationOptions(otherProtectedTokenizationProtectConfigurationData) // overrides global configuration for XML file
                .AddInMemoryCollection(memoryConfiguration).WithProtectedConfigurationOptions(magicPurposeStringProtectConfigurationData) // overrides global configuration for in-memory collection file
                .AddEnvironmentVariables().WithProtectedConfigurationOptions(magicPurposeStringProtectConfigurationData) // overrides global configuration for enviroment variables file
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
        Debug.Assert(appSettings.EncryptedInMemorySecretKey == appSettings.PlainTextInMemorySecretKey);

        // appsettings.json assertions
        Debug.Assert(appSettings.EncryptedJsonSpecialCharacters == appSettings.PlainTextJsonSpecialCharacters);
        Debug.Assert(appSettings.ConnectionStrings["PartiallyEncryptedConnectionString"].Contains("(local)\\SECONDINSTANCE"));
        Debug.Assert(appSettings.ConnectionStrings["PartiallyEncryptedConnectionString"].Contains("Secret_Catalog"));
        Debug.Assert(appSettings.ConnectionStrings["PartiallyEncryptedConnectionString"].Contains("secret_user"));
        Debug.Assert(appSettings.ConnectionStrings["PartiallyEncryptedConnectionString"].Contains("secret_password"));
        Debug.Assert(appSettings.ConnectionStrings["FullyEncryptedConnectionString"].Contains("Data Source=server1\\THIRDINSTANCE; Initial Catalog=DB name; User ID=sa; Password=pass5678; MultipleActiveResultSets=True;"));

        // appsettings.development.json assertions
        Debug.Assert(appSettings.Nullable.DateTime.Value.ToUniversalTime() == new DateTime(2016, 10, 1, 20, 34, 56, 789, DateTimeKind.Utc));
        Debug.Assert(appSettings.Nullable.Double == 123.456);
        Debug.Assert(appSettings.Nullable.Int == 98765);
        Debug.Assert(appSettings.Nullable.Bool == true);
        Debug.Assert(appSettings.Nullable.DoubleArray[1] == 3.14);
        Debug.Assert(appSettings.Nullable.DoubleArray[3] == 3.14);

        // appsettings.xml assertions
        Debug.Assert(appSettings.TransientFaultHandlingOptions["AutoRetryDelay"] == appSettings.TransientFaultHandlingOptions["AutoRetryDelaySubPurpose"]);
        Debug.Assert(appSettings.Logging.LogLevel["Microsoft"] == "Warning");
        Debug.Assert(appSettings.EncryptedXmlSecretKey == appSettings.PlainTextXmlSecretKey);


        // multiple configuration reload example (in order to check that the ReloadToken re-registration works)
        int i = 0;
        while (i++ < 5)
        {
            // updates inside appsettings.<environment>.json the property "Int": <whatever>, --> "Int": "Protected:{<random number>},"
            var environmentAppSettings = File.ReadAllText($"appsettings.{Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")}.json");
            environmentAppSettings = new Regex("\"Int\":.+?,").Replace(environmentAppSettings, $"\"Int\": \"{standardProtectConfigurationData.ProtectConfigurationValue($"Protect:{{{new Random().Next(0, 1000000)}}}")}\",");
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

- Fededim.Extensions.Configuration.Protected.DataProtectionAPI.DataProtectionAPIProtectConfigurationData
- Fededim.Extensions.Configuration.Protected.DataProtectionAPI.DataProtectionAPIProtectProvider

# Version History
v1.0.0
- Initial release: extracted Microsoft Data Protection API dependencies from Fededim.Extensions.Configuration.Protected version 1.0.11 into DataProtectionAPIProtectConfigurationData and DataProtectionAPIConfigurationBuilderExtensions and implemented DataProtectionAPIProtectProvider using the standard interface IProtectProvider.

v1.0.1
- Refinement: Just renamed from FileProtect... classes to ProtectFile... for naming consistency among code
- Dependency: requires at least Fededim.Extensions.Configuration.Protected version 1.0.13

v1.0.2
- Dependency: requires at least Fededim.Extensions.Configuration.Protected version 1.0.14
- Breaking change: removed anymore needed DataProtectionAPIConfigurationBuilderExtensions
- Refinement: adapted DataProtectionAPIProtectConfigurationData according to the new IProtectProviderConfigurationData, streamlined its constructors and added missing comments
     
# Detailed guide

You can find a [detailed article on CodeProject](https://www.codeproject.com/Articles/5374311/Fededim-Extensions-Configuration-Protected-the-ult) explaning the origin, how to use it and the main point of the implementation.


# Feedback & Contributing
Fededim.Extensions.Configuration.Protected.DataProtectionAPI is released as open source under the MIT license. Bug reports and contributions are welcome at the [GitHub repository](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.Protected).