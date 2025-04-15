using System.Data.Common;
using System.Reflection;
using FPTU_ELibrary.Application.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using Azure.Identity;
using CloudinaryDotNet;
using FluentValidation;
using FPTU_ELibrary.Application.HealthChecks;
using FPTU_ELibrary.Application.Services;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Data.SqlClient;
using Serilog.Core;
using StackExchange.Redis;
using Azure.Security.KeyVault.Secrets;
using System.Text.Json;
using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using Microsoft.Extensions.Azure;
using Expression = System.Linq.Expressions.Expression;

namespace FPTU_ELibrary.API.Extensions
{
    //  Summary:
    //      This class is to configure services for presentation layer 
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureEndpoints(this IServiceCollection services)
        {
            // Add controllers
            services.AddControllers();
            // Configures ApiExplorer
            services.AddEndpointsApiExplorer();
            // Add swagger
            services.AddSwaggerGen();
            // Add HttpContextAccessor
            services.AddHttpContextAccessor();
            // Add HttpClient
            services.AddHttpClient();

            return services;
        }

        public static IServiceCollection ConfigureSerilog(this IServiceCollection services,
            WebApplicationBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .WriteTo.Console()
                .Enrich.WithProperty("Environment", builder.Environment)
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            builder.Host.UseSerilog();

            // Register the Serilog logger
            services.AddSingleton(Log.Logger);

            return services;
        }

        public static IServiceCollection ConfigureAppSettings(this IServiceCollection services,
            WebApplicationBuilder builder,
            IWebHostEnvironment env)
        {
            #region Development stage

            if (env.IsDevelopment()) // Is Development env
            {
                // Retrieve the App Configuration connection string from existing configuration
                var appConfigConnectionString = builder.Configuration.GetConnectionString("AzureAppConfiguration");
                builder.Configuration.AddAzureAppConfiguration(option =>
                {
                    option.Connect(appConfigConnectionString)
                        .ConfigureRefresh(refresh =>
                        {
                            refresh.Register("AppSettings:RefreshValue", refreshAll: true);
                        });
                });
            }

            #endregion

            #region Production stage

            else if (env.IsProduction()) // Is Production env
            {
                // Retrieve the App Configuration connection string from existing configuration
                var appConfigConnectionString = builder.Configuration.GetConnectionString("AzureAppConfiguration");

                // Add azure app configuration
                builder.Configuration.AddAzureAppConfiguration(option =>
                {
                    // Connect to connection string 
                    option.Connect(appConfigConnectionString)
                        .ConfigureRefresh(refresh =>
                        {
                            refresh.Register("AppSettings:RefreshValue", refreshAll: true);
                        });
                });

                // Make sure to add Azure App Configuration middleware
                // so that the refresh token and auto-update are effective across your app
                builder.Services.AddAzureAppConfiguration();
            }

            #endregion

            #region Staging

            else if (env.IsStaging()) // Is Staging env
            {
            }

            #endregion


            // Configure AppSettings
            services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
            // Configure BorrowSettings
            services.Configure<BorrowSettings>(builder.Configuration.GetSection("BorrowSettings"));
            // Configure ElasticSettings
            services.Configure<ElasticSettings>(builder.Configuration.GetSection("ElasticSettings"));
            // Configure WebTokenSettings
            services.Configure<WebTokenSettings>(builder.Configuration.GetSection("WebTokenSettings"));
            // Configure GoogleAuthSettings
            services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("GoogleAuthSettings"));
            // Configure FacebookAuthSettings
            services.Configure<FacebookAuthSettings>(builder.Configuration.GetSection("FacebookAuthSettings"));
            // Configure CloudinarySettings
            services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
            // Configure AzureSettings
            services.Configure<AzureSettings>(builder.Configuration.GetSection("AzureSettings"));
            // Configure OCRSettings
            services.Configure<AISettings>(builder.Configuration.GetSection("AISettings"));
            // Configure CustomVisionSettings
            services.Configure<CustomVisionSettings>(builder.Configuration.GetSection("CustomVision"));
            // Configure DetectSettings
            services.Configure<DetectSettings>(builder.Configuration.GetSection("DetectSettings"));
            // Configure AzureSpeechSettings
            services.Configure<AzureSpeechSettings>(builder.Configuration.GetSection("AzureSpeechSettings"));
            // Configure FaceDetectionSettings
            services.Configure<FaceDetectionSettings>(builder.Configuration.GetSection("FaceDetectionSettings"));
            // Configure PayOS
            services.Configure<PayOSSettings>(builder.Configuration.GetSection("PayOSSettings"));
            // Configure PayOS
            services.Configure<PaymentSettings>(builder.Configuration.GetSection("PaymentSettings"));
            //Configure DigitalBorrowSettings
            services.Configure<DigitalResourceSettings>(builder.Configuration.GetSection("DigitalResourceSettings"));
            //Configure AdsScriptSettings
            services.Configure<AdsScriptSettings>(builder.Configuration.GetSection("AdsScriptSettings"));
            //Configure RedisSettings
            services.Configure<RedisSettings>(builder.Configuration.GetSection("RedisSettings"));
            //Configure FFMPEGSettings
            services.Configure<FFMPEGSettings>(builder.Configuration.GetSection("FFMPEGSettings"));
            //Configure AWSS3
            services.Configure<AWSStorageSettings>(builder.Configuration.GetSection("AWSStorageSettings"));
            return services;
        }

        public static IServiceCollection ConfigureAzureSpeech(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped(provider =>
            {
                var subscriptionKey = configuration["AzureSpeechSettings:SubscriptionKey"];
                var serviceRegion = configuration["AzureSpeechSettings:Region"];
                return SpeechConfig.FromSubscription(subscriptionKey, serviceRegion);
            });

            return services;
        }

        public static IServiceCollection EstablishApplicationConfiguration(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            // Config PayOS
            var payOsConfig = configuration.GetSection("PayOSSettings").Get<PayOSSettings>();
            if (payOsConfig != null)
            {
                var payGate = "https://api-merchant.payos.vn";
                var returnUrl = env.IsDevelopment()
                    ? "http://localhost:3000/payment-return"
                    : "https://elibrary-capstone.vercel.app/payment-return";
                var cancelUrl = env.IsDevelopment()
                    ? "http://localhost:3000/payment-cancel"
                    : "https://elibrary-capstone.vercel.app/payment-cancel";

                services.Configure<PayOSSettings>(options =>
                {
                    options.ClientId = payOsConfig.ClientId;
                    options.ApiKey = payOsConfig.ApiKey;
                    options.ChecksumKey = payOsConfig.ChecksumKey;
                    options.ReturnUrl = returnUrl;
                    options.CancelUrl = cancelUrl;
                    options.PaymentUrl = $"{payGate}/v2/payment-requests";
                    options.GetPaymentLinkInformationUrl = $"{payGate}/v2/payment-requests/{{0}}";
                    options.CancelPaymentUrl = $"{payGate}/v2/payment-requests/{{0}}/cancel";
                    options.ConfirmWebHookUrl = "https://api-merchant.payos.vn/confirm-webhook";
                });
            }

            return services;
        }


        public static IServiceCollection ConfigureRedis(this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            // Define redis configuration
            var redisConfig = env.IsDevelopment()
                ? $"{configuration["RedisSettings:Host"]}:{configuration["RedisSettings:Port"]},abortConnect=false"
                : $"{Environment.GetEnvironmentVariable("REDIS_URL")},abortConnect=false";

            // Add Redis distributed caching services
            services.AddStackExchangeRedisCache(config => { config.Configuration = redisConfig; });

            try
            {
                // Register IConnectionMultiplexer (used in CacheHealthCheck and custom Redis operations)
                services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig));
            }
            catch (RedisConnectionException ex)
            {
                Logger.None.Error("Redis connection failed: {msg}", ex.Message);
            }

            return services;
        }

        public static IServiceCollection ConfigureCloudinary(this IServiceCollection services,
            IConfiguration configuration)
        {
            Cloudinary cloudinary = new Cloudinary(configuration["CloudinarySettings:CloudinaryUrl"]!)
            {
                Api = { Secure = true }
            };

            services.AddSingleton(cloudinary);

            return services;
        }

        public static IServiceCollection ConfigureOCR(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ComputerVisionClient>(sp =>
            {
                var aiSettings = configuration.GetSection("AISettings").Get<AISettings>();
                return new ComputerVisionClient(new ApiKeyServiceClientCredentials(aiSettings.SubscriptionKey))
                {
                    Endpoint = aiSettings.Endpoint
                };
            });

            return services;
        }

        public static IServiceCollection ConfigureSignalR(this IServiceCollection services)
        {
            services.AddSignalR();
            return services;
        }

        public static IServiceCollection ConfigureHealthCheckServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddSingleton<AggregatedHealthCheckService>();
            services.AddScoped<DbConnection>(sp =>
                new SqlConnection(configuration.GetConnectionString("DefaultConnectionStr")));

            return services;
        }

        public static IServiceCollection ConfigureBackgroundServices(this IServiceCollection services)
        {
            // Register ReminderService inheriting from BackgroundService
            services.AddHostedService<ReminderService>();
            services.AddHostedService<ChangeStatusService>();
            services.AddHostedService<DigitalBorrowChangeStatus>();
            return services;
        }

        public static IServiceCollection ConfigureCamelCaseForValidation(this IServiceCollection services)
        {
            ValidatorOptions.Global.PropertyNameResolver = CamelCasePropertyNameResolver.ResolvePropertyName;

            return services;
        }


        public static IServiceCollection AddAuthentication(this IServiceCollection services,
            IConfiguration configuration)
        {
            // Define TokenValidationParameters
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = bool.Parse(configuration["WebTokenSettings:ValidateIssuerSigningKey"]!),
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["WebTokenSettings:IssuerSigningKey"]!)),
                ValidateIssuer = bool.Parse(configuration["WebTokenSettings:ValidateIssuer"]!),
                ValidAudience = configuration["WebTokenSettings:ValidAudience"],
                ValidIssuer = configuration["WebTokenSettings:ValidIssuer"],
                ValidateAudience = bool.Parse(configuration["WebTokenSettings:ValidateAudience"]!),
                RequireExpirationTime = bool.Parse(configuration["WebTokenSettings:RequireExpirationTime"]!),
                ValidateLifetime = bool.Parse(configuration["WebTokenSettings:ValidateLifetime"]!),
                ClockSkew = TimeSpan.Zero
            };

            // Register TokenValidationParameters in the DI container
            services.AddSingleton(tokenValidationParameters);

            // Add authentication
            services.AddAuthentication(options =>
            {
                // Define default scheme
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; // For API requests
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // For login challenge
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => // Enables JWT-bearer authentication
            {
                // Disable Https required for the metadata address or authority
                options.RequireHttpsMetadata = false;
                // Define type and definitions required for validating a token
                options.TokenValidationParameters = services.BuildServiceProvider()
                    .GetRequiredService<TokenValidationParameters>();
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        // If the request is for our hub...
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            // Read the token out of the query string
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        public static IServiceCollection AddCors(this IServiceCollection services, string policyName)
        {
            // Configure CORS
            services.AddCors(p => p.AddPolicy(policyName, policy =>
            {
                // allow all with any header, method
                policy.WithOrigins("*")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }));

            return services;
        }

        public static IServiceCollection AddLazyResolution(this IServiceCollection services)
        {
            return services.AddTransient(
                typeof(Lazy<>),
                typeof(LazilyResolved<>));
        }

        private class LazilyResolved<T> : Lazy<T>
        {
            public LazilyResolved(IServiceProvider serviceProvider)
                : base(serviceProvider.GetRequiredService<T>)
            {
            }
        }

        #region KeyVault

        //      public static IServiceCollection ConfigureAppSettings(this IServiceCollection services,
        //     IConfiguration configuration,
        //     IWebHostEnvironment env)
        // {
        //     var keyVaultUrl = configuration["AzureSettings:KeyVaultUrl"];
        //     var clientId = configuration["AzureSettings:KeyVaultClientId"];
        //     var clientSecret = configuration["AzureSettings:KeyVaultClientSecret"];
        //     var tenantId = configuration["AzureSettings:KeyVaultDirectoryID"];
        //
        //     var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        //     var client = new SecretClient(new Uri(keyVaultUrl), credential);
        //
        //     //  Load assembly of `FPTU_ELibrary.Application`
        //     string targetNamespace = "FPTU_ELibrary.Application.Configurations";
        //     string assemblyName = "FPTU_ELibrary.Application";
        //     var assembly = LoadAssembly(assemblyName);
        //
        //     //  Get all class 
        //     var configTypes = assembly.GetTypes()
        //         .Where(t => t.IsClass && t.Namespace == targetNamespace)
        //         .ToList();
        //
        //     foreach (var configType in configTypes)
        //     {
        //         var secretName = configType.Name;
        //         var secretResponse = client.GetSecret(secretName);
        //
        //         if (secretResponse?.Value?.Value != null)
        //         {
        //             // parse json in key vault to object
        //             var configInstance = JsonSerializer.Deserialize(secretResponse.Value.Value, configType);
        //
        //             if (configInstance != null)
        //             {
        //                 // Get Configure<TOptions> method
        //                 var configureGenericMethod = typeof(OptionsServiceCollectionExtensions)
        //                     .GetMethods()
        //                     .FirstOrDefault(m =>
        //                         m.Name == "Configure" &&
        //                         m.IsGenericMethodDefinition &&
        //                         m.GetParameters().Length == 2 &&
        //                         m.GetParameters()[0].ParameterType == typeof(IServiceCollection) &&
        //                         m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Action<>))
        //                     ?.MakeGenericMethod(configType);
        //
        //                 if (configureGenericMethod != null)
        //                 {
        //                     // Create action to assign value from parsed instance to DI container
        //                     var configureAction = CreateConfigureAction(configType, configInstance);
        //
        //                     // Invoke Configure<TOptions> method
        //                     configureGenericMethod.Invoke(null, new object[] { services, configureAction });
        //                 }
        //             }
        //         }
        //     }
        //
        //     return services;
        // }

        // /// <summary>
        // /// Create Action<T> for assigning value from parsed instance to DI container.
        // /// </summary>
        // private static object CreateConfigureAction(Type configType, object instance)
        // {
        //     var method = typeof(ServiceCollectionExtensions)
        //         .GetMethod(nameof(CreateConfigureActionGeneric), BindingFlags.NonPublic | BindingFlags.Static)
        //         ?.MakeGenericMethod(configType);
        //
        //     return method?.Invoke(null, new object[] { instance });
        // }
        //
        // /// <summary>
        // /// Create Action for Configure<TOptions> method.
        // /// </summary>
        // private static Action<T> CreateConfigureActionGeneric<T>(T instance)
        // {
        //     return options =>
        //     {
        //         foreach (var property in typeof(T).GetProperties())
        //         {
        //             property.SetValue(options, property.GetValue(instance));
        //         }
        //     };
        // }
        //
        // /// <summary>
        // /// Load assembly by name.
        // /// </summary>
        // private static Assembly LoadAssembly(string assemblyName)
        // {
        //     return Assembly.Load(assemblyName) ?? throw new Exception($"Cannot load assembly {assemblyName}");
        // }

        #endregion
    }
}