# Overview

In this repository you will find the source code of my NuGet package: Fededim.Extensions.Configuration.Protected

[![Build status](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/actions/workflows/dotnet.yml/badge.svg)](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/actions/workflows/dotnet.yml?query=branch%3Amaster)
[![Test Coverage](https://raw.githubusercontent.com/fededim/Fededim.Extensions.Configuration.Protected/master/misc/last_build_artifacts/badge_combined.svg)](https://htmlpreview.github.io/?https://raw.githubusercontent.com/fededim/Fededim.Extensions.Configuration.Protected/master/misc/last_build_artifacts/index.html)


# Fededim.Extensions.Configuration.Protected

Fededim.Extensions.Configuration.Protected is an improved ConfigurationBuilder which allows partial or full encryption of configuration values stored inside any possible ConfigurationSource and fully integrated in the ASP.NET Core architecture. Fededim.Extensions.Configuration.Protected implements a custom ConfigurationBuilder and a custom ConfigurationProvider defining a custom tokenization tag which whenever found inside a configuration value decrypts the enclosed encrypted data using a pluggable encryption/decryption provider (a default one based on ASP.NET Core Data Protection API is provided).

You can find the source code here [Fededim.Extensions.Configuration.Protected](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.Protected)


# Fededim.Extensions.Configuration.Protected.DataProtectionAPI

Fededim.Extensions.Configuration.Protected.DataProtectionAPI is the standard Microsoft Data Protection API encryption/decryption provider for Fededim.Extensions.Configuration.Protected

You can find the source code here [Fededim.Extensions.Configuration.Protected.DataProtectionAPI](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.Protected.DataProtectionAPI)

You can find a [detailed article on CodeProject](https://www.codeproject.com/Articles/5374311/Fededim-Extensions-Configuration-Protected) explaning the origin, how to use it and the main point of the implementation.


# Fededim.Extensions.Configuration.Protected.DataProtectionAPITest
This a xUnit test project which tests thoroughly the two above packages in order to improve the reliability and the code quality. It creates sample data for all ConfigurationSources provided by Microsoft .NET (a JSON file, a XML file, environment variables, an in-memory dictionary and command line arguments) containing a 2\*fixed set of entries (10000), one in plaintext with random datatype and value and another with the same value but encrypted. It loads then the sample data with ProtectedConfigurationBuilder in order to decrypt it and tests that all plaintext values are the same as those that have been decrypted. For example a test case on the JsonConfigurationProvider generated a plain-text file with a total size of 60MB and an encrypted file with a total size of 91MB, the test has ended in around 10 seconds for generating the random JSON file, encrypting it, decrypting it using the ProtectedConfigurationBuilder (in order to decrypt 250k encrypted values this step took around 5 seconds in .Net462 and around 3 seconds in net6.0 which is faster) and checking that every decrypted key was equal to the plaintext one. Moreover all the whole set of five test cases was repeated for 1000 iterations (Test Explorer Run Until Failure, unluckily it is not available for all tests, I had to do it separately), both for net462 (total runtime 705 minutes) and net6.0 (total runtime 424 minutes) without raising any error as you can see in the pictures below.

Net462 Endurance Test 
![image](https://github.com/user-attachments/assets/66b4a6ec-ea3f-4004-8eea-b4d2d554ee33)

Net6.0 Endurance Test
![image](https://github.com/user-attachments/assets/fc73e3ef-e5e6-4b1c-a4bd-d1a2dbf30e10)


# Fededim.Extensions.Configuration.ProtectedJson (OBSOLETE PLEASE USE Fededim.Extensions.Configuration.Protected.DataProtectionAPI)

Fededim.Extensions.Configuration.ProtectedJson is my first package and it is an improved JSON configuration provider which allows partial or full encryption of configuration values stored in appsettings.json files and fully integrated in the ASP.NET Core architecture. Basically, it implements a custom ConfigurationSource and a custom ConfigurationProvider defining a custom tokenization tag which whenever found decrypts the enclosed encrypted data using ASP.NET Core Data Protection API.

You can find the source code here [Fededim.Extensions.Configuration.ProtectedJson](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.ProtectedJson) but this package has
become however obsolete in favour of the more versatile [Fededim.Extensions.Configuration.Protected.DataProtectionAPI](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.Protected.DataProtectionAPI).

You can find a [detailed article on CodeProject](https://www.codeproject.com/Articles/5372873/ProtectedJson-Integrating-ASP-NET-Core-Configurati) explaning the origin, how to use it and the main point of the implementation.
