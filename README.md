# FDM.Extensions.Configuration.ProtectedJson © 2023 Federico Di Marco

ProtectedJson is an improved JSON configuration provider which allows partial or full encryption of configuration values stored in appsettings.json files and fully integrated in the ASP.NET Core architecture. Basically, it implements a custom ConfigurationSource and a custom ConfigurationProvider defining a custom tokenization tag which whenever found decrypts the enclosed encrypted data using ASP.NET Core Data Protection API.

For information and usage check my article on CodeProject: [ProtectedJson: integrating ASP.NET Core Configuration and Data Protection](https://www.codeproject.com/Articles/5372873/ProtectedJson-integrating-ASP-NET-Core-Configurati)

Key Features
Encrypt partially or fully a configuration value
Trasparent in memory decryption of encrypted values without almost any additional line of code

How to Use

  - Modify appsettings JSON files by enclose with the encryption tokenization tag (e.g. Protect:{<data to be encrypted}) all the values or part of values you would like to encrypt
  - Configure the data protection api in a helper method (e.g. ConfigureDataProtection)
  - Encrypt all appsettings values by calling IDataProtect.ProtectFiles extension method (use ProtectedJsonConfigurationProvider.DataProtectionPurpose as CreateProtector purpose)
  - Define the application configuration using ConfigurationBuilder and adding encrypted json files using AddProtectedJsonFile extension method
  - Call ConfigurationBuilder.Build to automatically decrypt the encrypted values and retrieve the cleartext ones.
  - Map the Configuration object to a strongly typed hierarchical class using DI Configure

For code check the [Programs.cs of TestConsole app](https://github.com/fededim/FDM.Extensions.Configuration.ProtectedJson/blob/master/FDM.Extensions.Configuration.ProtectedJson.ConsoleTest/Program.cs)
