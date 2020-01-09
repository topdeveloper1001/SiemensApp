using Xunit.Abstractions;

namespace Willow.Tests.Infrastructure
{
    public class BaseDbTest : BaseTest
    {
        public BaseDbTest(ITestOutputHelper output) : base(output)
        {
        }

        protected override TestContext TestContext => new TestContext(Output, ConditionalDatabaseFixture.GetDatabaseInstance(true));
    }
}
