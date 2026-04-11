using System;
using System.Collections.Generic;
using GlobalExceptionHandler.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using PaymentService.Configuration;
using PaymentService.DataAccess.Marten;
using PaymentService.Domain;
using PaymentService.Infrastructure;
using PaymentService.Init;
using PaymentService.Jobs;
using PaymentService.Messaging.RabbitMq;
using PolicyService.Api.Events;

// Thêm 2 using này
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Eureka;

namespace PaymentService;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc()
            .AddNewtonsoftJson(opt => { opt.SerializerSettings.TypeNameHandling = TypeNameHandling.Auto; });

        services.AddMarten(Configuration.GetConnectionString("PgConnection"));
        services.AddPaymentDemoInitializer();
        services.AddMediatR(opts => opts.RegisterServicesFromAssemblyContaining<Startup>());
        services.AddLogingBehaviour();
        services.AddSingleton<PolicyAccountNumberGenerator>();
        services.AddRabbitListeners();
        services.AddBackgroundJobs(Configuration.GetSection("BackgroundJobs").Get<BackgroundJobsConfig>());
        services.AddSwaggerGen();

        // ==================== THÊM DÒNG NÀY ĐỂ ĐĂNG KÝ EUREKA ====================
        services.AddDiscoveryClient(Configuration);   // Hoặc: services.AddEurekaDiscoveryClient();
        // =====================================================================
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseGlobalExceptionHandler(cfg => cfg.MapExceptions());

        if (!env.IsDevelopment()) app.UseHsts();

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseInitializer();
        app.UseRabbitListeners(new List<Type> { typeof(PolicyCreated), typeof(PolicyTerminated) });
        app.UseBackgroundJobs();

        // ==================== THÊM DÒNG NÀY ĐỂ KÍCH HOẠT EUREKA ====================
        app.UseDiscoveryClient();
        // =====================================================================

        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}