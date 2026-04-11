using ChatService.Hubs;
using ChatService.Messaging.RabbitMq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using PolicyService.Api.Events;
using RawRabbit;                          // ← Thêm
using RawRabbit.Configuration;            // ← Thêm
using RawRabbit.DependencyInjection;      // ← Thêm
using RawRabbit.Instantiation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ChatService;

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

        services.AddMediatR(opts => opts.RegisterServicesFromAssemblyContaining<Startup>());

        services.AddCors(opt => opt.AddPolicy("CorsPolicy",
            builder =>
            {
                var origins = appSettingsSection.Get<AppSettings>()?.AllowedChatOrigins ?? Array.Empty<string>();
                builder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithOrigins(origins);
            }));

        services.AddMvc().AddNewtonsoftJson();

        // ====================== JWT AUTHENTICATION ======================
        var appSettings = appSettingsSection.Get<AppSettings>();
        var key = Encoding.ASCII.GetBytes(appSettings.Secret);

        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x =>
        {
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateActor = false
            };

            x.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/agentsChat"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        // ====================== SIGNALR ======================
        services.AddSignalR();
        services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

        // ====================== RAWRABBIT (RabbitMQ) ======================
        var rawRabbitOptions = new RawRabbitOptions
        {
            ClientConfiguration = new RawRabbitConfiguration
            {
                Hostnames = new List<string>
        {
            Configuration["RabbitMq:Host"] ?? "localhost"
        },
                Port = Configuration.GetValue<ushort>("RabbitMq:Port", 5672),
                VirtualHost = Configuration["RabbitMq:VirtualHost"] ?? "/",
                Username = Configuration["RabbitMq:Username"] ?? "guest",
                Password = Configuration["RabbitMq:Password"] ?? "guest",
                RequestTimeout = TimeSpan.FromSeconds(30)
            }
        };

        services.AddSingleton(rawRabbitOptions);

        services.AddSingleton<IBusClient>(provider =>
            RawRabbitFactory.CreateSingleton(rawRabbitOptions));

        // Đăng ký listener (nếu bạn có extension AddRabbitListeners)
        services.AddRabbitListeners();

        // ====================== SWAGGER ======================
        services.AddSwaggerGen();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();
        else
            app.UseHsts();

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("CorsPolicy");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHttpsRedirection();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHub<AgentChatHub>("/agentsChat");   // Hub cho chat realtime
        });

        // RabbitMQ Listeners
        app.UseRabbitListeners(new List<Type>
        {
            typeof(PolicyCreated) 
            // Thêm các event khác bạn muốn lắng nghe ở đây, ví dụ:
            // typeof(ChatMessageSent)
        });
    }
}