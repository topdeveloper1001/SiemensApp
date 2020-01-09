using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace Willow.Infrastructure.Swagger
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            // Consumes content types
            var requiredContentTypes = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<ConsumesAttribute>()
                .SelectMany(attr => attr.ContentTypes)
                .Distinct();

            if (requiredContentTypes.Any() && requiredContentTypes.Any(x => x == "multipart/form-data"))
            {
                var formDataParameters = operation.Parameters.Where(x => x.In == "formData").ToArray();
                foreach (var formDataParameter in formDataParameters)
                {
                    operation.Parameters.Remove(formDataParameter);
                }

                operation.Parameters.Add(new NonBodyParameter
                {
                    Name = operation.OperationId == "UploadTicketMedia" ? "mediaFile" : "imageFile",
                    In = "formData",
                    Description = "Upload File",
                    Required = true,
                    Type = "file"
                });
                operation.Consumes.Add("multipart/form-data");
            }
        }
    }
}
