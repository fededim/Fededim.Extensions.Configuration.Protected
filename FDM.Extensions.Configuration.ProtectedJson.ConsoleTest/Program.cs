using Microsoft.Extensions.Configuration;
using FDM.Extensions.Configuration.ProtectedJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using FDM.Extensions.Configuration.ProtectedJson.ConsoleTest;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

public class Program
{
    private static void ConfigureDataProtection(IDataProtectionBuilder builder)
    {
        builder.UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
        {
            EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
            ValidationAlgorithm = ValidationAlgorithm.HMACSHA256,

        }).PersistKeysToFileSystem(new DirectoryInfo("..\\..\\Keys"));
    }


    public static void Main(string[] args)
    {
        // define the application configuration
        var configuration = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddProtectedJsonFile("appsettings.json", ConfigureDataProtection)
                .AddProtectedJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")}.json", ConfigureDataProtection)
                .AddEnvironmentVariables()
                .Build();

        // define the DI services: Data Protection API and configure strongly typed AppSettings configuration class
        var services = new ServiceCollection();
        ConfigureDataProtection(services.AddDataProtection());
        services.Configure<AppSettings>(configuration);
        var serviceProvider = services.BuildServiceProvider();


        // retrieve IDataProtector interface for encrypting data and the strongly typed AppSettings configuration class
        var dataProtector = serviceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector(ProtectedJsonConfigurationProvider.DataProtectionPurpose);
        var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;


        // generate the encrypted values
        var encryptedInt = dataProtector.Protect("98765");
        var encryptedServerName = dataProtector.Protect("local");
        var encryptedDatabaseName = dataProtector.Protect("databaseName");
        var encryptedUserId = dataProtector.Protect("userIdNew");
        var encryptedPassword = dataProtector.Protect("passwordNew");
        var encryptedFullConnectionString = dataProtector.Protect(appSettings.ConnectionStrings["PlainTextConnectionString"]);

    }
}