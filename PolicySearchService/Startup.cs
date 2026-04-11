using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PolicySearchService.DataAccess.ElasticSearch;
using PolicySearchService.Messaging.RabbitMq;
using PolicyService.Api.Events;
using Steeltoe.Discovery.Client;

namespace PolicySearchService;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Service Discovery (Eureka)
        services.AddDiscoveryClient(Configuration);

        services.AddMvc()
            .AddNewtonsoftJson();

        services.AddMediatR(opts => opts.RegisterServicesFromAssemblyContaining<Startup>());

        // ElasticSearch
        services.AddElasticSearch(Configuration.GetConnectionString("ElasticSearchConnection"));

        // RabbitMQ Listeners
        services.AddRabbitListeners();

        services.AddSwaggerGen();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        // Quan trọng: Phải có dòng này để đăng ký với Eureka
        app.UseDiscoveryClient();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        // RabbitMQ Listeners
        app.UseRabbitListeners(new List<Type>
        {
            typeof(PolicyCreated) 
            // Thêm các event khác nếu cần sau này
        });
    }
}