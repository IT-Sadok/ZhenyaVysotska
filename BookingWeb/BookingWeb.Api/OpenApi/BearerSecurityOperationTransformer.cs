using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BookingWeb.Api.OpenApi;

internal sealed class BearerSecurityOperationTransformer : IOpenApiOperationTransformer
{
    private const string SchemeName = "Bearer";
    
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        
        var requiresAuth = metadata.OfType<IAuthorizeData>().Any();
        var allowsAnonymous = metadata.OfType<IAllowAnonymous>().Any();

        if (requiresAuth && !allowsAnonymous)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeName)] = []
            });
        }

        return Task.CompletedTask;
    }
}