using System;

namespace Willow.Infrastructure.Services
{
    public interface IDateTimeService
    {
        DateTime UtcNow { get; }
    }

    public class DateTimeService : IDateTimeService
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
