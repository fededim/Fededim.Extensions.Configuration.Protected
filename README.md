# FDM.Extensions.Configuration.ProtectedJson © 2023 Federico Di Marco

ProtectedJson is an improved JSON configuration provider which allows partial or full encryption of configuration values stored in appsettings.json files and fully integrated in the ASP.NET Core architecture. Basically, it implements a custom ConfigurationSource and a custom ConfigurationProvider defining a custom tokenization tag which whenever found decrypts the enclosed encrypted data using ASP.NET Core Data Protection API.

For information and usage check my article on CodeProject: [ProtectedJson: integrating ASP.NET Core Configuration and Data Protection](https://www.codeproject.com/Articles/5372873/ProtectedJson-integrating-ASP-NET-Core-Configurati)

To use it immediately check the [Programs.cs of TestConsole app](https://github.com/fededim/FDM.Extensions.Configuration.ProtectedJson/blob/master/FDM.Extensions.Configuration.ProtectedJson.ConsoleTest/Program.cs)

