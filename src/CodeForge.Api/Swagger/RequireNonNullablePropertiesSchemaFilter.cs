using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CodeForge.Api.Swagger
{
    /// <summary>
    /// Swashbuckle's SupportNonNullableReferenceTypes() correctly sets each property's
    /// `nullable` flag from C# nullable-reference annotations, but leaves the schema's
    /// `required` array untouched — so a non-nullable `string Title` still generates as
    /// optional (`title?: string`) for consumers like openapi-typescript. This promotes
    /// every non-nullable property to `required`, so the generated TS types are exactly
    /// as strict as the C# DTOs they're generated from.
    /// </summary>
    public class RequireNonNullablePropertiesSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Properties is null)
            {
                return;
            }

            foreach (var (name, property) in schema.Properties)
            {
                if (property.Nullable == false)
                {
                    schema.Required.Add(name);
                }
            }
        }
    }
}
