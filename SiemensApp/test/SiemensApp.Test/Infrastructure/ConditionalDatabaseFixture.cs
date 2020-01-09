namespace Willow.Tests.Infrastructure
{
    public class ConditionalDatabaseFixture
    {
        private static object _syncObject = new object();
        private static DatabaseFixture _instance = null;

        public static DatabaseFixture GetDatabaseInstance(bool mustUseSqlServer)
        {
            if (!mustUseSqlServer)
            {
                if (TestEnvironment.UseInMemoryDatabase)
                {
                    return null;
                }
            }
            InitializeInstance();
            return _instance;
        }

        private static void InitializeInstance()
        {
            if (_instance != null)
            {
                return;
            }

            lock(_syncObject)
            {
                if (_instance != null)
                {
                    return;
                }
                _instance = new DatabaseFixture();
            }
        }

    }
}