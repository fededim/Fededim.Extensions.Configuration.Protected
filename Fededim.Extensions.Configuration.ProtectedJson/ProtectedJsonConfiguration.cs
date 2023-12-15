using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fededim.Extensions.Configuration.ProtectedJson
{
    public class ProtectedJsonStreamConfigurationProvider : JsonStreamConfigurationProvider
    {
        protected IDataProtector DataProtector { get; set; }

        public ProtectedJsonStreamConfigurationProvider(ProtectedJsonStreamConfigurationSource source) : base(source)
        {
            // configure data protection
            if (source.DataProtectionBuildAction != null)
            {
                var services = new ServiceCollection();
                source.DataProtectionBuildAction(services.AddDataProtection());
                source.ServiceProvider = services.BuildServiceProvider();
            }
            else if (source.ServiceProvider == null)
                throw new ArgumentNullException(nameof(source.ServiceProvider));

            DataProtector = source.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector(ProtectedJsonConfigurationProvider.DataProtectionPurpose);
        }

        public override void Load(Stream stream)
        {
            base.Load(stream);

            var protectedSource = (ProtectedJsonStreamConfigurationSource)Source;

            // decrypt needed values
            foreach (var kvp in Data)
            {
                if (!String.IsNullOrEmpty(kvp.Value))
                    Data[kvp.Key] = protectedSource.ProtectedRegex.Replace(kvp.Value, me => DataProtector.Unprotect(me.Value));
            }
        }
    }


    public class ProtectedJsonStreamConfigurationSource : JsonStreamConfigurationSource
    {
        public Regex ProtectedRegex { get; set; }
        public Action<IDataProtectionBuilder> DataProtectionBuildAction { get; set; }
        public IServiceProvider ServiceProvider { get; set; }


        public ProtectedJsonStreamConfigurationSource():this(null)
        {

        }

        public ProtectedJsonStreamConfigurationSource(String protectedRegexString = null)
        {
            var protectedRegex = new Regex(protectedRegexString ?? ProtectedJsonConfigurationSource.DefaultProtectedRegexString);
            if (!protectedRegex.GetGroupNames().Contains("protectedData"))
                throw new ArgumentException("Regex must contain a group named protectedData!", nameof(protectedRegexString));

            ProtectedRegex = protectedRegex;
        }


        public override IConfigurationProvider Build(IConfigurationBuilder builder)
            => new ProtectedJsonStreamConfigurationProvider(this);
    }


    public class ProtectedJsonConfigurationProvider : JsonConfigurationProvider
    {
        public const String DataProtectionPurpose = "ProtectedJsonConfigurationProvider";

        protected IDataProtector DataProtector { get; set; }

        public ProtectedJsonConfigurationProvider(ProtectedJsonConfigurationSource source) : base(source)
        {
            // configure data protection
            if (source.DataProtectionBuildAction != null)
            {
                var services = new ServiceCollection();
                source.DataProtectionBuildAction(services.AddDataProtection());
                source.ServiceProvider = services.BuildServiceProvider();
            }
            else if (source.ServiceProvider==null)
                throw new ArgumentNullException(nameof(source.ServiceProvider));

            DataProtector = source.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector(DataProtectionPurpose);
        }

        public override void Load()
        {
            base.Load();

            var protectedSource = (ProtectedJsonConfigurationSource)Source;

            // decrypt needed values
            foreach (var key in Data.Keys.ToList())
            {
                if (!String.IsNullOrEmpty(Data[key]))
                    Data[key] = protectedSource.ProtectedRegex.Replace(Data[key], me => DataProtector.Unprotect(me.Groups["protectedData"].Value));
            }
        }
    }


    public class ProtectedJsonConfigurationSource : JsonConfigurationSource
    {
        public const String DefaultProtectedRegexString = "Protected:{(?<protectedData>.+?)}";
        public const String DefaultProtectRegexString = "Protect:{(?<protectData>.+?)}";

        public Regex ProtectedRegex { get; set; }
        public Action<IDataProtectionBuilder> DataProtectionBuildAction { get; set; }
        public IServiceProvider ServiceProvider { get; set; }

        public ProtectedJsonConfigurationSource(String protectedRegexString = null)
        {
            var protectedRegex = new Regex(protectedRegexString ?? DefaultProtectedRegexString);
            if (!protectedRegex.GetGroupNames().Contains("protectedData"))
                throw new ArgumentException("Regex must contain a group named protectedData!", nameof(protectedRegexString));

            ProtectedRegex = protectedRegex;
        }


        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            ResolveFileProvider();
            EnsureDefaults(builder);
            return new ProtectedJsonConfigurationProvider(this);
        }
    }
}
