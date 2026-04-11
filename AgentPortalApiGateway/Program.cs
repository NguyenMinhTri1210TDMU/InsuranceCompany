using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Eureka;

namespace AgentPortalApiGateway;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ====================== CẤU HÌNH ======================
        builder.Configuration
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("ocelot.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables();
        // =====================================================

        // ====================== OCELOT + EUREKA ======================
        builder.Services.AddOcelot()
                        .AddEureka()
                        .AddCacheManager(x => x.WithDictionaryHandle());
        // ===========================================================

        var app = builder.Build();

        // Không dùng Authentication và Authorization nữa
        app.UseOcelot().Wait();

        app.Run();
    }
}