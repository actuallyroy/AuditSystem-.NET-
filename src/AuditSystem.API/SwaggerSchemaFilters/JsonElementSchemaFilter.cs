using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;

namespace AuditSystem.API.SwaggerSchemaFilters
{
    public class JsonElementSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(JsonElement) || context.Type == typeof(JsonElement?))
            {
                schema.Type = "object";
                schema.Properties = null;
                schema.AdditionalProperties = null;
                schema.Description = "JSON object";
                schema.Example = null;
            }
        }
    }
} 