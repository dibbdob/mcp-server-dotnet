namespace Ticketdesk.Mcp;

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

/// <summary>
/// What an MCP server reachable over HTTP must check before the protocol sees a request.
///
/// - Origin: a page in the user's browser can make requests to localhost. If a request carries an Origin
///   header, it must be one the server was told to accept, or the request is refused. Without this, a
///   site the user visits can drive the server (DNS rebinding).
/// - Authorization: every request carries the bearer token the server was started with. It is compared in
///   constant time.
/// </summary>
public sealed class HttpGuard(string token, IReadOnlySet<string> allowedOrigins)
{
    private readonly byte[] expected = Encoding.UTF8.GetBytes(token);

    public async Task Invoke(HttpContext context, RequestDelegate next)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (origin.Length > 0 && !allowedOrigins.Contains(origin))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Origin not allowed.");
            return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        var presented = header.StartsWith("Bearer ", StringComparison.Ordinal) ? Encoding.UTF8.GetBytes(header[7..]) : [];
        if (!CryptographicOperations.FixedTimeEquals(presented, this.expected))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            return;
        }

        await next(context);
    }
}
