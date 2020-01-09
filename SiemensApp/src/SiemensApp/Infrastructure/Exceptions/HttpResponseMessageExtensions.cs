using Willow.Infrastructure.Exceptions;

namespace System.Net.Http
{
    public static class HttpResponseMessageExtensions
    {
        public static void EnsureSuccessStatusCode(this HttpResponseMessage message, string dependencyServiceName)
        {
            if (!message.IsSuccessStatusCode)
            {
                throw new DependencyServiceFailureException(dependencyServiceName, message.StatusCode);
            }
        }
    }
}