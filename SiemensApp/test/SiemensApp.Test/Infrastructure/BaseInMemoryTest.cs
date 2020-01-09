using Xunit.Abstractions;

namespace Willow.Tests.Infrastructure
{
    public class BaseInMemoryTest : BaseTest
    {
        public BaseInMemoryTest(ITestOutputHelper output) : base(output)
        {
        }

        protected override TestContext TestContext => new TestContext(Output, ConditionalDatabaseFixture.GetDatabaseInstance(false));
    }
}
