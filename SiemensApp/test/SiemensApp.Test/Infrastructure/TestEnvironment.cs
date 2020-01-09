using System;

namespace Willow.Tests.Infrastructure
{
    public static class TestEnvironment
    {
        public static bool UseInMemoryDatabase
        {
            get
            {
                const bool defaultSetting = true;
                var useInMemoryString = Environment.GetEnvironmentVariable("TEST_UseInMemoryDatabase");
                if (string.IsNullOrEmpty(useInMemoryString))
                {
                    return defaultSetting;
                }
                if (!bool.TryParse(useInMemoryString, out bool useInMemory))
                {
                    return defaultSetting;
                }
                return useInMemory;
            }
        }
    }
}