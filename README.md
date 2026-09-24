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

You can find a [detailed article on my personal homepage](https://fededim.github.io/Articles/Fededim.Extensions.Configuration.Protected.DataProtectionAPI.html) explaning the origin, how to use it and the main point of the implementation.


# Fededim.Extensions.Configuration.Protected.DataProtectionAPITest
This a xUnit test project which tests thoroughly the two above packages in order to improve the reliability and the code quality. It creates sample data for all ConfigurationSources provided by Microsoft .NET (a JSON file, a XML file, environment variables, an in-memory dictionary and command line arguments) containing a 2\*fixed set of entries (100000, except for environment variables limited up to 2000 for technical reasons), one in plaintext with random datatype and value, and another with the same value but encrypted. Arrays are generated with a random number of elements, while XML and JSON files are generated with a random number of hierarchical levels, consisting of an approximate size from 60 MB up to 100 MB each. It loads then the sample data with ProtectedConfigurationBuilder in order to decrypt it and tests that all plaintext values are the same as those that have been decrypted. For example a test case on the JsonConfigurationProvider generated a plain-text file with a total size of 60MB and an encrypted file with a total size of 91MB, the test has ended in around 10 seconds for generating the random JSON file, encrypting it, decrypting it using the ProtectedConfigurationBuilder, and checking that every decrypted key was equal to the plaintext one. Moreover all the whole set of five test cases was repeated for 1000 iterations (Test Explorer --> Run Until Failure, unluckily it is not available for all tests, I had to do it separately for the two frameworks), both for net48 (total runtime 3318 minutes) and net10.0 (total runtime 2336 minutes) without raising any error as you can see in the pictures below.

<!---
**Net472 Endurance Test**
![image](https://github.com/user-attachments/assets/7675c2aa-b24f-4e09-8422-55f531e6ca30)
-->

**Net48 Endurance Test** [Test results trace files](https://raw.githubusercontent.com/fededim/Fededim.Extensions.Configuration.Protected/master/misc/endurance_test_results_net48-x64.zip)
![image](https://raw.githubusercontent.com/fededim/Fededim.Extensions.Configuration.Protected/master/misc/net48_endurance_test.png)

**Net8.0 Endurance Test** [Test results trace files](https://raw.githubusercontent.com/fededim/Fededim.Extensions.Configuration.Protected/master/misc/endurance_test_results_net8.0-x64.zip)
<br/>Updated image coming soon
<!---
![image](https://github.com/user-attachments/assets/36fe482a-1400-489a-83e8-cf0c88118e2c)
-->

**Net10.0 Endurance Test** [Test results trace files](https://raw.githubusercontent.com/fededim/Fededim.Extensions.Configuration.Protected/master/misc/endurance_test_results_net10.0-x64.zip)
![image](https://raw.githubusercontent.com/fededim/Fededim.Extensions.Configuration.Protected/master/misc/net10.0_endurance_test.png)

# Fededim.Extensions.Configuration.ProtectedJson (OBSOLETE PLEASE USE Fededim.Extensions.Configuration.Protected.DataProtectionAPI)

Fededim.Extensions.Configuration.ProtectedJson is my first package and it is an improved JSON configuration provider which allows partial or full encryption of configuration values stored in appsettings.json files and fully integrated in the ASP.NET Core architecture. Basically, it implements a custom ConfigurationSource and a custom ConfigurationProvider defining a custom tokenization tag which whenever found decrypts the enclosed encrypted data using ASP.NET Core Data Protection API.

You can find the source code here [Fededim.Extensions.Configuration.ProtectedJson](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.ProtectedJson) but this package has
become however obsolete in favour of the more versatile [Fededim.Extensions.Configuration.Protected.DataProtectionAPI](https://github.com/fededim/Fededim.Extensions.Configuration.Protected/tree/master/Fededim.Extensions.Configuration.Protected.DataProtectionAPI).

You can find a [detailed article on my personal homepage](https://fededim.github.io/Articles/ProtectedJson_%20Integrating%20ASP.NET%20Core%20Configuration%20and%20Data%20Protection-%20CodeProje.html) explaning the origin, how to use it and the main point of the implementation.
