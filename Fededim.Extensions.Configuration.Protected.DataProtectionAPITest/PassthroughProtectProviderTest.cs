using Xunit.Abstractions;
using Xunit;

namespace Fededim.Extensions.Configuration.Protected.DataProtectionAPITest
{

    public abstract class PassthroughProtectProviderBaseTest : ProtectedConfigurationBuilderTest, IClassFixture<ProtectedConfigurationBuilderTestFixture>
    {
        public PassthroughProtectProviderBaseTest(ProtectedConfigurationBuilderTestFixture context, ITestOutputHelper testOutputHelper, IProtectProviderConfigurationData protectProviderConfigurationData)
            : base(context, testOutputHelper, protectProviderConfigurationData)
        {
        }


        protected override string TrimRegexCharsFromSubpurpose(string subpurpose)
        {
            return subpurpose.Replace(":", "*").Replace("}", "|");
        }


        protected override string TrimRegexCharsFromProtectData(string value)
        {
            return value.Replace("}", "|");
        }
    }


    public class PassthroughProtectProviderTest : DataProtectionAPIProtectProviderBaseTest
    {
        public PassthroughProtectProviderTest(ProtectedConfigurationBuilderTestFixture context, ITestOutputHelper testOutputHelper) : base(context, testOutputHelper, PassthroughProtectConfigurationData.CreateInstance)
        {
        }
    }


    public class ReverseChainedPassthroughProtectProviderTest : DataProtectionAPIProtectProviderBaseTest
    {
        public ReverseChainedPassthroughProtectProviderTest(ProtectedConfigurationBuilderTestFixture context, ITestOutputHelper testOutputHelper) : base(context, testOutputHelper, PassthroughProtectConfigurationData.CreateInstance.Chain(pp => new ReverseChainedProtectProvider(pp)))
        {
        }
    }
}
