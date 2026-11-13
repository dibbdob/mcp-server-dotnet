namespace Ticketdesk.Mcp.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Text;

public sealed class HttpTransportTests
{
    private const string Initialize = """
        {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"probe","version":"1"}}}
        """;

    private static async Task<HttpStatusCode> Post(Uri endpoint, string? token, string? origin)
    {
        using var http = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = new StringContent(Initialize, Encoding.UTF8, "application/json") };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        using var response = await http.SendAsync(request);
        return response.StatusCode;
    }

    [Test]
    public async Task A_client_with_the_token_gets_the_same_tools_as_over_stdio()
    {
        await using var server = await HttpServer.Start();
        await using var client = await server.Connect();

        var names = (await client.ListToolsAsync()).Select(t => t.Name).Order(StringComparer.Ordinal).ToList();
        var ticket = await client.CallToolAsync("get_ticket", new Dictionary<string, object?> { ["ticketId"] = "T-1001" });

        await Assert.That(names).IsEquivalentTo(["add_internal_note", "get_response_deadline", "get_ticket", "search_tickets"]);
        await Assert.That(ToolDesignTests.Text(ticket)).Contains("T-1001");
    }

    [Test]
    [Arguments(null)]
    [Arguments("wrong-token")]
    public async Task A_request_without_the_token_is_refused_before_the_protocol_sees_it(string? token)
    {
        await using var server = await HttpServer.Start();

        await Assert.That(await Post(server.Endpoint, token, null)).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task A_request_from_a_web_page_the_server_does_not_know_is_refused_even_with_the_token()
    {
        await using var server = await HttpServer.Start(allowedOrigins: "https://desk.example");

        await Assert.That(await Post(server.Endpoint, HttpServer.Token, "https://attacker.example")).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(await Post(server.Endpoint, HttpServer.Token, "https://desk.example")).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    [Arguments("")]
    [Arguments("too-short")]
    public async Task The_server_will_not_serve_http_without_a_long_enough_token(string token)
    {
        using var process = HttpServer.Launch(["--http", "--port", "5999"], new() { ["TICKETDESK_MCP_TOKEN"] = token });
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        await Assert.That(process.ExitCode).IsEqualTo(1);
        await Assert.That(stderr).Contains("Refusing to serve HTTP");
    }
}
