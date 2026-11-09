using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace CoffeeNChill.Gateway.Services
{
    // Puts the padlock in Swagger only on the endpoints that really need a token,
    // so public and protected endpoints can be told apart.
    public class SwaggerAuthFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var method = context.MethodInfo;

            bool isPublic = method.GetCustomAttributes<AllowAnonymousAttribute>().Any();
            bool isProtected = method.GetCustomAttributes<AuthorizeAttribute>().Any()
                || method.DeclaringType!.GetCustomAttributes<AuthorizeAttribute>().Any();

            if (isPublic || !isProtected)
            {
                return;
            }

            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = new List<string>()
                }
            };
        }
    }
}
