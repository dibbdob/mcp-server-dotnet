using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ticketdesk.Mcp;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(TicketStore.LoadDefault());
builder.Services.AddMcpServer().WithStdioServerTransport().WithTools<TicketTools>();
await builder.Build().RunAsync();
