using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace APIPSI16.Filters
{
    /// <summary>
    /// Swagger operation filter to handle file upload endpoints.
    /// This filter resolves issues with IFormFile parameters in Swagger schema generation.
    /// </summary>
    public class FileUploadOperation : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Check if the endpoint has the SwaggerFileUpload attribute
            var hasFileUploadAttribute = context.MethodInfo.GetCustomAttributes(true)
                .Any(attr => attr.GetType().Name == nameof(SwaggerFileUploadAttribute));

            if (!hasFileUploadAttribute)
                return;

            // Check if any parameter is IFormFile
            var formFileParams = context.ApiDescription.ParameterDescriptions
                .Where(p => p.ModelMetadata?.ModelType == typeof(IFormFile))
                .ToList();

            if (!formFileParams.Any())
                return;

            // Clear existing parameters for file upload
            operation.Parameters?.Clear();

            // Set up request body for multipart/form-data
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["file"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary",
                                    Description = "The file to upload"
                                }
                            },
                            Required = new HashSet<string> { "file" }
                        }
                    }
                }
            };
        }
    }
}
