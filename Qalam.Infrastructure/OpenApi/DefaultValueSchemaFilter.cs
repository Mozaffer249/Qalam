using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Qalam.Infrastructure.OpenApi
{
    /// <summary>
    /// Schema filter to set default property examples based on type instead of null
    /// </summary>
    public class DefaultValueSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema?.Properties == null)
                return;

            foreach (var property in schema.Properties)
            {
                // Skip if property already has an example set
                if (property.Value.Example != null)
                    continue;

                // Set default examples based on type
                switch (property.Value.Type?.ToLower())
                {
                    case "string":
                        property.Value.Example = new OpenApiString("");
                        break;
                    
                    case "integer":
                        property.Value.Example = new OpenApiInteger(0);
                        break;
                    
                    case "number":
                        property.Value.Example = new OpenApiDouble(0);
                        break;
                    
                    case "boolean":
                        property.Value.Example = new OpenApiBoolean(false);
                        break;
                    
                    case "array":
                        property.Value.Example = new OpenApiArray();
                        break;
                }
            }
        }
    }
}
