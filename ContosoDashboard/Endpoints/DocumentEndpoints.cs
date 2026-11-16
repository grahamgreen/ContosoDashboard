using System.Security.Claims;
using ContosoDashboard.Services;
using Microsoft.Net.Http.Headers;

namespace ContosoDashboard.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/documents").RequireAuthorization();

        // 404 for both "missing" and "no access" so document IDs can't be probed.
        group.MapGet("/{id:int}/download", async (int id, ClaimsPrincipal user, IDocumentService documents) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await documents.OpenForReadAsync(id, userId, preview: false);
            return result is { } r
                ? Results.File(r.Content, r.Document.FileType, r.Document.OriginalFileName)
                : Results.NotFound();
        });

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out int userId) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
