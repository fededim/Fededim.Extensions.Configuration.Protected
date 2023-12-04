using Microsoft.Extensions.Configuration;
using Fededim.Extensions.Configuration.ProtectedJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Fededim.Extensions.Configuration.ProtectedJson.ConsoleTest;
using Microsoft.Extensions.Options;
using System;
using System.IO;

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


    public static void Main(string[] args)
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