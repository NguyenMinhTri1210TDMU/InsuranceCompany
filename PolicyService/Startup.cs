using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PolicyService.DataAccess.NHibernate;
using PolicyService.RestClients;
using Steeltoe.Discovery.Client;
using RawRabbit;
using RawRabbit.Configuration;
using RawRabbit.Instantiation;

namespace PolicyService;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // ====================== EUREKA ======================
        services.AddDiscoveryClient(Configuration);

        // ====================== MVC ======================
        services.AddControllers()
            .AddNewtonsoftJson();

        // ====================== MEDIATR ======================
        services.AddMediatR(opts =>
            opts.RegisterServicesFromAssemblyContaining<Startup>());

        // ====================== REST CLIENT ======================
        services.AddPricingRestClient();

        // ====================== NHIBERNATE ======================
        services.AddNHibernate(
            Configuration.GetConnectionString("DefaultConnection"));

        



        // ====================== SWAGGER ======================
        services.AddSwaggerGen();
    }

    public void Configure(IApplicationBuilder app,
        IWebHostEnvironment env)
    {
        app.UseExceptionHandler("/error");

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

        // ====================== EUREKA ======================
        app.UseDiscoveryClient();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}