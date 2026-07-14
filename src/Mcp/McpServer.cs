using GoodRP.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;

namespace GoodRP.Mcp;

public static class McpServer
{
    public static async Task RunAsync(DiscordManager discordManager, IMediaWatcher mediaWatcher)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddSingleton(discordManager);
        builder.Services.AddSingleton(mediaWatcher);

        builder.Services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        var app = builder.Build();
        await app.RunAsync();
    }
}
