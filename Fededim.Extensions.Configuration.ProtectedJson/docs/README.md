# About
Fededim.Extensions.Configuration.ProtectedJson is an improved JSON configuration provider which allows partial or full encryption of configuration values stored in appsettings.json files and fully integrated in the ASP.NET Core architecture. Basically, it implements a custom ConfigurationSource and a custom ConfigurationProvider defining a custom tokenization tag which whenever found decrypts the enclosed encrypted data using ASP.NET Core Data Protection API.

This package is however deprecated in favour of the more versatile [Fededim.Extensions.Configuration.Protected](https://www.nuget.org/packages/Fededim.Extensions.Configuration.Protected).

# Key Features
- Encrypt partially or fully a configuration value
- Trasparent in memory decryption of encrypted values without almost any additional line of code

# How to Use

- Modify appsettings JSON files by enclose with the encryption tokenization tag (e.g. Protect:{<data to be encrypted}) all the values or part of values you would like to encrypt
- Configure the data protection api in a helper method (e.g. ConfigureDataProtection)
- Encrypt all appsettings values by calling IDataProtect.ProtectFiles extension method (use ProtectedJsonConfigurationProvider.DataProtectionPurpose as CreateProtector purpose)
- Define the application configuration using ConfigurationBuilder and adding encrypted json files using AddProtectedJsonFile extension method
- Call ConfigurationBuilder.Build to automatically decrypt the encrypted values and retrieve the cleartext ones.
- Map the Configuration object to a strongly typed hierarchical class using DI Configure

```csharp

using Microsoft.Extensions.Configuration;
using Fededim.Extensions.Configuration.ProtectedJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Fededim.Extensions.Configuration.ProtectedJson.ConsoleTest;
using Microsoft.Extensions.Options;

public class Program
{
    private static void ConfigureDataProtection(IDataProtectionBuilder builder)
    {
        builder.UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
        {
            EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
            ValidationAlgorithm = ValidationAlgorithm.HMACSHA256,

        }).SetDefaultKeyLifetime(TimeSpan.FromDays(365*15)).PersistKeysToFileSystem(new DirectoryInfo("..\\..\\..\\Keys"));
    }


    public static void Main(String[] args)
    {
        // define the DI services: Data Protection API
        var servicesDataProtection = new ServiceCollection();
        ConfigureDataProtection(servicesDataProtection.AddDataProtection());
        var serviceProviderDataProtection = servicesDataProtection.BuildServiceProvider();

        // retrieve IDataProtector interface for encrypting data
        var dataProtector = serviceProviderDataProtection.GetRequiredService<IDataProtectionProvider>().CreateProtector(ProtectedJsonConfigurationProvider.DataProtectionPurpose);

        // encrypt all Protect:{<data>} token tags of all .json files (must be done before reading the configuration)
        var encryptedFiles = dataProtector.ProtectFiles(".");

        // define the application configuration and read .json files
        var configuration = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddProtectedJsonFile("appsettings.json", ConfigureDataProtection)
                .AddProtectedJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")}.json", ConfigureDataProtection)
                .AddEnvironmentVariables()
                .Build();

        // define other DI services: configure strongly typed AppSettings configuration class (must be done after having read the configuration)
        var services = new ServiceCollection();
        services.Configure<AppSettings>(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // retrieve the strongly typed AppSettings configuration class
        var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
    }
}

```

The main types provided by this library are:

- Fededim.Extensions.Configuration.ProtectedJson.ProtectedJsonStreamConfigurationProvider
- Fededim.Extensions.Configuration.ProtectedJson.ProtectedJsonStreamConfigurationSource
- Fededim.Extensions.Configuration.ProtectedJson.ProtectedJsonConfigurationProvider
- Fededim.Extensions.Configuration.ProtectedJson.ProtectedJsonConfigurationSource

# Detailed guide

You can find a [detailed article on CodeProject](https://www.codeproject.com/Articles/5372873/ProtectedJson-Integrating-ASP-NET-Core-Configurati) explaning the origin, how to use it and the main point of the implementation.

# Feedback & Contributing
Fededim.Extensions.Configuration.ProtectedJson is released as open source under the MIT license. Bug reports and contributions are welcome at the [GitHub repository](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.ProtectedJson).