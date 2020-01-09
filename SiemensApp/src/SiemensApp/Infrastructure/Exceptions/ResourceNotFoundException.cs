using System;

namespace Willow.Infrastructure.Exceptions
{
    public class ResourceNotFoundException : Exception
    {
        public string ResourceType { get; set; }
        public string ResourceId { get; set; }

        public ResourceNotFoundException(string resourceType, string resourceId)
            : this(resourceType, resourceId, string.Empty, null)
        {
        }

        public ResourceNotFoundException(string resourceType, Guid resourceId)
            : this(resourceType, resourceId.ToString(), string.Empty, null)
        {
        }

        public ResourceNotFoundException(string resourceType, string resourceId, string message)
            : this(resourceType, resourceId, message, null)
        {
        }

        public ResourceNotFoundException(string resourceType, string resourceId, Exception innerException)
            : this(resourceType, resourceId, string.Empty, innerException)
        {
        }

        public ResourceNotFoundException(string resourceType, string resourceId, string message, Exception innerException)
            : base($"Resource({resourceType}: {resourceId}) cannot be found. {message}", innerException)
        {
            ResourceType = resourceType;
            ResourceId = resourceId;
        }
    }
}
