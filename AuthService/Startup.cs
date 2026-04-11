using AuthService.DataAccess;
using AuthService.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Steeltoe.Discovery.Client;        // ← Thêm using này
using System;
using System.Text;

namespace AuthService;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        var appSettingsSection = Configuration.GetSection("AppSettings");
        services.Configure<AppSettings>(appSettingsSection);

        var appSettings = appSettingsSection.Get<AppSettings>();
        var key = Encoding.ASCII.GetBytes(appSettings.Secret);

        // ====================== CORS ======================
        services.AddCors(options => options.AddPolicy("CorsPolicy",
            builder =>
            {
                builder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithOrigins(appSettings.AllowedAuthOrigins ?? Array.Empty<string>());
            }));

        services.AddMvc().AddNewtonsoftJson();

        // ====================== JWT AUTHENTICATION ======================
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };
        });

        // ====================== EUREKA (Service Discovery) ======================
        services.AddDiscoveryClient(Configuration);        // ← Quan trọng

        // ====================== BUSINESS SERVICES ======================
        services.AddSingleton<Domain.AuthService>();
        services.AddSingleton<IInsuranceAgents, InsuranceAgentsInMemoryDb>();

        services.AddSwaggerGen();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
        }

        app.UseCors("CorsPolicy");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHttpsRedirection();

        // ====================== EUREKA ======================
        app.UseDiscoveryClient();           // ← Rất quan trọng

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}