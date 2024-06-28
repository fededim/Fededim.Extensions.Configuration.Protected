# Overview

In this repository you will find the sources of my three NuGet packages


# Fededim.Extensions.Configuration.Protected

Fededim.Extensions.Configuration.Protected is an improved ConfigurationBuilder which allows partial or full encryption of configuration values stored inside any possible ConfigurationSource and fully integrated in the ASP.NET Core architecture. Fededim.Extensions.Configuration.Protected implements a custom ConfigurationBuilder and a custom ConfigurationProvider defining a custom tokenization tag which whenever found inside a configuration value decrypts the enclosed encrypted data using a pluggable encryption/decryption provider (a default one based on ASP.NET Core Data Protection API is provided).

You can find the source code here [Fededim.Extensions.Configuration.Protected](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.Protected)


# Fededim.Extensions.Configuration.Protected.DataProtectionAPI

Fededim.Extensions.Configuration.Protected.DataProtectionAPI is the standard Microsoft Data Protection API encryption/decryption provider for Fededim.Extensions.Configuration.Protected

You can find the source code here [Fededim.Extensions.Configuration.Protected.DataProtectionAPI](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.Protected.DataProtectionAPI)

You can find a [detailed article on CodeProject](https://www.codeproject.com/Articles/5374311/Fededim-Extensions-Configuration-Protected) explaning the origin, how to use it and the main point of the implementation.


# Fededim.Extensions.Configuration.Protected.DataProtectionAPITest
This a xUnit test project which tests thoroughly the two above packages in order to improve the reliability and the code quality. It creates sample data for all ConfigurationSources provided by Microsoft .NET (a JSON file, a XML file, environment variables, an in-memory dictionary and command line arguments) containing a 2\*fixed set of entries (10000), one in plaintext with random datatype and value and another with the same value but encrypted. It loads then the sample data with ProtectedConfigurationBuilder in order to decrypt it and tests that all plaintext values are the same as those that have been decrypted. On my personal laptop I have successfully tested it also with 100000 entries for a JSON file total size of 65MB, it has run in less than 8 seconds without any issues (Setting environment variables is terribly slow, it seems to be a Windows issue since it needs every time to broadcast all environment changes to the listening windows!)

![image](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/assets/8364158/7b0dee4f-e8d8-4d2f-b9d1-d73ce6abe690)



# Fededim.Extensions.Configuration.ProtectedJson (OBSOLETE PLEASE USE Fededim.Extensions.Configuration.Protected.DataProtectionAPI)

Fededim.Extensions.Configuration.ProtectedJson is my first package and it is an improved JSON configuration provider which allows partial or full encryption of configuration values stored in appsettings.json files and fully integrated in the ASP.NET Core architecture. Basically, it implements a custom ConfigurationSource and a custom ConfigurationProvider defining a custom tokenization tag which whenever found decrypts the enclosed encrypted data using ASP.NET Core Data Protection API.

You can find the source code here [Fededim.Extensions.Configuration.ProtectedJson](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.ProtectedJson) but this package is 
however deprecated in favour of the more versatile [Fededim.Extensions.Configuration.Protected.DataProtectionAPI](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.Protected.DataProtectionAPI).

You can find a [detailed article on CodeProject](https://www.codeproject.com/Articles/5372873/ProtectedJson-Integrating-ASP-NET-Core-Configurati) explaning the origin, how to use it and the main point of the implementation.
