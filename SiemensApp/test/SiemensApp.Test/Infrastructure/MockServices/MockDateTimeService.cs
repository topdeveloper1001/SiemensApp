using System;
using Willow.Infrastructure.Services;

namespace Willow.Tests.Infrastructure.MockServices
{
    public class MockDateTimeService : IDateTimeService
    {
        public DateTime UtcNow { get; set; } = DateTime.UtcNow;
    }
}