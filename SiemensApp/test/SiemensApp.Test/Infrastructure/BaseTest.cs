using AutoFixture;
using Xunit.Abstractions;

namespace Willow.Tests.Infrastructure
{
    public abstract class BaseTest
    {
        protected ITestOutputHelper Output { get; }
        protected abstract TestContext TestContext { get;  }

        protected BaseTest(ITestOutputHelper output)
        {
            Output = output;
        }

        public ServerFixture CreateServerFixture(ServerFixtureConfiguration serverConfiguration)
        {
            return new ServerFixture(serverConfiguration, TestContext);
        }

        public Fixture Fixture = new Fixture();
    }
}
