using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartTask.Application;
using SmartTask.Infrastructure;
using SmartTask.Application.Common.Interfaces;

// MCP için stub CurrentUserService (sistem kullanıcısı)
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// MCP context'inde HTTP yok — sabit bir sistem kullanıcısı simüle edilir
// Production'da gerçek auth mekanizması eklenebilir
builder.Services.AddScoped<ICurrentUserService, McpSystemUserService>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

// MCP için basit sistem kullanıcısı — gerçek projede token ile değiştir
public class McpSystemUserService : ICurrentUserService
{
    // Test için sabit bir user ID — gerçek kullanımda JWT parse edilmeli
    public Guid? UserId => Guid.Parse("00000000-0000-0000-0000-000000000001");
    public bool IsAuthenticated => true;
}
