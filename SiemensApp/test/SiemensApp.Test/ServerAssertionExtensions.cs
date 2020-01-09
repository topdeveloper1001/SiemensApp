using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Willow.Tests.Infrastructure
{
    public static class ServerAssertionExtensions
    {
        public static T GetDbContext<T>(this ServerAssertion assertion) where T : DbContext
        {
            return assertion.MainServices.GetRequiredService<T>();
        }

    }
}
