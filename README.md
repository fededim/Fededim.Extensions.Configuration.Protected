# Overview

In this repository you will find the sources of two my NuGet packages

# Fededim.Extensions.Configuration.ProtectedJson

Fededim.Extensions.Configuration.ProtectedJson is my first package and it is an improved JSON configuration provider which allows partial or full encryption of configuration values stored in appsettings.json files and fully integrated in the ASP.NET Core architecture. Basically, it implements a custom ConfigurationSource and a custom ConfigurationProvider defining a custom tokenization tag which whenever found decrypts the enclosed encrypted data using ASP.NET Core Data Protection API.

You can find the source code here [Fededim.Extensions.Configuration.ProtectedJson](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.ProtectedJson) but this package is 
however deprecated in favour of the more versatile [Fededim.Extensions.Configuration.Protected](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.Protected).

You can find a [detailed article on CodeProject](https://www.codeproject.com/Articles/5372873/ProtectedJson-Integrating-ASP-NET-Core-Configurati) explaning the origin, how to use it and the main point of the implementation.


# Fededim.Extensions.Configuration.Protected

Fededim.Extensions.Configuration.Protected is an improved ConfigurationBuilder which allows partial or full encryption of configuration values stored inside any possible ConfigurationSource and fully integrated in the ASP.NET Core architecture. Basically, it implements a custom ConfigurationBuilder and a custom ConfigurationProvider defining a custom tokenization tag which whenever found decrypts the enclosed encrypted data using ASP.NET Core Data Protection API.

You can find the source code here [Fededim.Extensions.Configuration.Protected](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.Protected)

You can find a [detailed article on CodeProject](https://www.codeproject.com/Articles/5374311/Fededim-Extensions-Configuration-Protected-the-ult) explaning the origin, how to use it and the main point of the implementation.
