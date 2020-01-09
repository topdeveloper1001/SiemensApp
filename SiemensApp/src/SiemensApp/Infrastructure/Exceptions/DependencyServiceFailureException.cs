using System;
using System.Net;

namespace Willow.Infrastructure.Exceptions
{
    public class DependencyServiceFailureException : Exception
    {
        public string ServiceName { get; set; }
        public HttpStatusCode ServiceStatusCode { get; set; }

        public DependencyServiceFailureException(string serviceName, HttpStatusCode serviceStatusCode)
            : this(serviceName, serviceStatusCode, string.Empty, null)
        {
        }

        public DependencyServiceFailureException(string serviceName, HttpStatusCode serviceStatusCode, string message)
            : this(serviceName, serviceStatusCode, message, null)
        {
        }

        public DependencyServiceFailureException(string serviceName, HttpStatusCode serviceStatusCode, Exception innerException)
            : this(serviceName, serviceStatusCode, string.Empty, innerException)
        {
        }

        public DependencyServiceFailureException(string serviceName, HttpStatusCode serviceStatusCode, string message, Exception innerException)
            : base($"Service {serviceName} returns failure ({serviceStatusCode}). {message}", innerException)
        {
            ServiceName = serviceName;
            ServiceStatusCode = serviceStatusCode;
        }
    }
}
