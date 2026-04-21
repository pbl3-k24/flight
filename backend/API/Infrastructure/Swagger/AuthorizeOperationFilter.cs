using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace API.Infrastructure.Swagger;

/// <summary>
/// Operation filter to automatically add Bearer token security requirement to endpoints with [Authorize]
/// This ensures Swagger UI sends the token with requests that need authentication
/// </summary>
public class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Kiểm tra xem endpoint có [Authorize] attribute không
        var hasAuthorizeAttribute = context.MethodInfo.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Any();

        // Kiểm tra xem class controller có [Authorize] attribute không
        if (!hasAuthorizeAttribute)
        {
            hasAuthorizeAttribute = context.MethodInfo.DeclaringType?
                .GetCustomAttributes(inherit: true)
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                .Any() ?? false;
        }

        // Chỉ thêm Bearer requirement nếu endpoint yêu cầu authorization
        if (hasAuthorizeAttribute)
        {
            if (operation.Security == null)
            {
                operation.Security = new List<OpenApiSecurityRequirement>();
            }

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new List<string>()
                }
            };

            operation.Security.Add(securityRequirement);
        }
    }
}
