﻿# About
Fededim.Extensions.Configuration.Protected is an improved ConfigurationBuilder which allows partial or full encryption of configuration values stored inside any possible ConfigurationSource and fully integrated in the ASP.NET Core architecture. Basically, it implements a custom ConfigurationBuilder and a custom ConfigurationProvider defining a custom tokenization tag which whenever found decrypts the enclosed encrypted data using ASP.NET Core Data Protection API.

# Key Features
- Encrypt partially or fully a configuration value
- Works with any existant and (hopefully) future ConfigurationSource and ConfigurationProvider (tested with CommandLine, EnvironmentVariables, Json, Xml and InMemoryCollection)
- Trasparent in memory decryption of encrypted values without almost any additional line of code
- Supports a global ConfigurationBuilder configuration and an eventual custom override for any ConfigurationSource
- Supports almost any NET framework (net6.0, netstandard2.0 and net462)
- Pluggable into any project with almost no changes to original NET / NET Core.

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


    private static void AnotherConfigureDataProtection(IDataProtectionBuilder builder)
    {
        builder.UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
        {
            EncryptionAlgorithm = EncryptionAlgorithm.AES_128_GCM,
            ValidationAlgorithm = ValidationAlgorithm.HMACSHA512,

        }).SetDefaultKeyLifetime(TimeSpan.FromDays(365 * 15)).PersistKeysToFileSystem(new DirectoryInfo("..\\..\\..\\Keys"));
    }


    public static void Main(String[] args)
    {
        args = new String[] { "--password Protect:{secretArgPassword!}" };
        
        // define the DI services: setup global Data Protection API
        var servicesDataProtection = new ServiceCollection();
        ConfigureDataProtection(servicesDataProtection.AddDataProtection());
        var serviceProviderDataProtection = servicesDataProtection.BuildServiceProvider();

        // define the DI services: setup a Data Protection API custom tailored for a particular providers (InMemory and Environment Variables)
        var servicesAdditionalDataProtection = new ServiceCollection();
        AnotherConfigureDataProtection(servicesAdditionalDataProtection.AddDataProtection());
        var serviceProviderAdditionalDataProtection = servicesAdditionalDataProtection.BuildServiceProvider();

        // retrieve IDataProtector interfaces for encrypting data
        var dataProtector = serviceProviderDataProtection.GetRequiredService<IDataProtectionProvider>().CreateProtector(ProtectedConfigurationBuilder.DataProtectionPurpose);
        var dataProtectorAdditional = serviceProviderAdditionalDataProtection.GetRequiredService<IDataProtectionProvider>().CreateProtector(ProtectedConfigurationBuilder.DataProtectionPurpose);


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
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")}.json")
                .AddXmlFile("appsettings.xml").WithProtectedConfigurationOptions(protectedRegexString: "OtherProtected:{(?<protectedData>.+?)}")
                .AddInMemoryCollection(memoryConfiguration).WithProtectedConfigurationOptions(dataProtectionServiceProvider: serviceProviderAdditionalDataProtection)
                .AddEnvironmentVariables().WithProtectedConfigurationOptions(dataProtectionConfigureAction: AnotherConfigureDataProtection)
                .Build();

        // define other DI services: configure strongly typed AppSettings configuration class (must be done after having read the configuration)
        var services = new ServiceCollection();
        services.Configure<AppSettings>(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // retrieve the strongly typed AppSettings configuration class
        var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

        // please check that all values inside appSettings class are actually decrypted with the right value.
        Debugger.Break();
    }
}

```

The main types provided by this library are:

- Fededim.Extensions.Configuration.Protected.ProtectedConfigurationBuilder
- Fededim.Extensions.Configuration.Protected.ProtectedConfigurationProvider
- Fededim.Extensions.Configuration.Protected.ProtectedConfigurationData

# Feedback & Contributing
Fededim.Extensions.Configuration.Protected is released as open source under the MIT license. Bug reports and contributions are welcome at the [GitHub repository](https://github.com/fededim/Fededim.Extensions.Configuration.Protected).