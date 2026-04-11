using GlobalExceptionHandler.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using PricingService.Configuration;
using PricingService.DataAccess.Marten;
using PricingService.Infrastructure;
using PricingService.Init;
using Steeltoe.Discovery.Client;

namespace PricingService;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Eureka Service Discovery
        services.AddDiscoveryClient(Configuration);

        // Controllers + Newtonsoft.Json
        services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore; // Tùy chọn tốt
            });

        // Marten (PostgreSQL Document DB)
        services.AddMarten(Configuration.GetConnectionString("DefaultConnection"));

        // Initializer & MediatR
        services.AddPricingDemoInitializer();
        services.AddMediatR(options => options.RegisterServicesFromAssemblyContaining<Program>());

        // Logging Behavior (nếu bạn có pipeline behavior)
        services.AddLoggingBehavior();

        // Swagger
        services.AddSwaggerGen();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        // Global Exception Handler
        app.UseGlobalExceptionHandler(cfg => cfg.MapExceptions());

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();     // Thêm để debug tốt hơn
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
        }

        app.UseInitializer();

        // Eureka Discovery Client
        app.UseDiscoveryClient();                // ← Rất quan trọng

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}