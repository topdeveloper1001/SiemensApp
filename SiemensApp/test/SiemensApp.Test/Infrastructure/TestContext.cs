using Xunit.Abstractions;

namespace Willow.Tests.Infrastructure
{
    public class TestContext
    {
        public ITestOutputHelper Output { get; }
        public DatabaseFixture DatabaseFixture { get; }

        public TestContext(ITestOutputHelper output, DatabaseFixture databaseFixture)
        {
            Output = output;
            DatabaseFixture = databaseFixture;
        }
    }
}