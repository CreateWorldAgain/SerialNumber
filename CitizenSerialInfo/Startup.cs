using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CitizenSerialInfo.Domains;
using CitizenSerialInfo.Models;
using CitizenSerialInfo.Models.ViewModels;
using CitizenSerialInfo.Services;
using static CitizenSerialInfo.Models.ImportFileInfo;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.WebSockets;
using System.Threading;

namespace CitizenSerialInfo
{
    public class Startup
    {
        private static AppDbContext _dbContext;
        private static ILoggerFactory _loggerFactory;

        public static Dictionary<string, WebSocket> webSocketsList = new Dictionary<string, WebSocket>();

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static IConfiguration Configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>(option =>
                    option.UseSqlServer(Configuration.GetConnectionString("MyConnString")));

            services.Configure<AppConfigurations>(
                     Configuration.GetSection("AppConfigurations"));


            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddScoped<IViewRenderService, ViewRenderService>();

            services.AddIdentity<ApplicationUser, ApplicationRole>(s=>
                 {
                     s.SignIn.RequireConfirmedEmail = true;
                     s.User.RequireUniqueEmail = true;
                     s.Password.RequireDigit = false;
                     s.Password.RequiredLength = 6;
                     s.Password.RequireNonAlphanumeric = false;
                     s.Password.RequireUppercase = false;
                     s.Password.RequireLowercase = false;
                 })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(config => config.ExpireTimeSpan = TimeSpan.FromDays(36500));

            #region Localization 
            //Adding Localisation to an ASP.NET Core application
            //https://andrewlock.net/adding-localisation-to-an-asp-net-core-application/
            //Globalization and localization
            //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization
            services.AddLocalization(opts => { opts.ResourcesPath = "Resources"; });

            services.AddMvc()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix, opts => { opts.ResourcesPath = "Resources"; })
                .AddDataAnnotationsLocalization();

            services.Configure<RequestLocalizationOptions>(opts =>
            {
                //https://msdn.microsoft.com/en-us/library/cc233982.aspx
                var supportedCultures = new List<CultureInfo>
                {
                    new CultureInfo("de"),
                    new CultureInfo("en")
                };

                opts.DefaultRequestCulture = new RequestCulture("en");
                // Formatting numbers, dates, etc.
                opts.SupportedCultures = supportedCultures;
                // UI strings that we have localized.
                opts.SupportedUICultures = supportedCultures;

            });
            #endregion
            services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));
            services.AddMvc().AddJsonOptions(options => options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            try
            {
                _loggerFactory = loggerFactory;

                loggerFactory.AddConsole(Configuration.GetSection("Logging"));
                loggerFactory.AddDebug();
                loggerFactory.AddFile("logs/webapp-{Date}.txt");

                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                    app.UseBrowserLink();
                }
                else
                {
                    app.UseExceptionHandler("/Home/Error");
                }

                app.UseStaticFiles();

                app.UseAuthentication();

                // локализация
                var options = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
                app.UseRequestLocalization(options.Value);

                // Автоматическая миграция
                var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();


                // основной контекст данных
                _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                var logger = loggerFactory.CreateLogger("Initialize");

                if (!_dbContext.AllMigrationsApplied())
                {
                    _dbContext.Database.Migrate();
                    _dbContext.EnsureSeedData(userManager, roleManager, logger).Wait();
                }

                #region WebSocket
                var webSocketOptions = new WebSocketOptions()
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(120),
                    ReceiveBufferSize = 4 * 1024
                };
                app.UseWebSockets(webSocketOptions);

                app.Use(async (context, next) =>
                {
                    if (context.Request.Path == "/ws")
                    {
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

                            var guid = context.Request.Query["guid"];

                            webSocketsList.Add(guid,webSocket);                            

                            while (webSocket.State == WebSocketState.Open)
                            {
                                var token = CancellationToken.None;
                                var buffer = new ArraySegment<Byte>(new Byte[4096]);
                                var received = await webSocket.ReceiveAsync(buffer, token);

                                switch (received.MessageType)
                                {
                                    case WebSocketMessageType.Close:

                                        webSocketsList.Remove(guid);
                                       
                                        break;
                                    default:
                                        break;
                                }
                            }

                        }
                        else
                        {
                            await next();
                        }
                    }
                    else
                    {
                        await next();
                    }
                });
                #endregion

                app.UseMvc(routes =>
                {
                    routes.MapRoute(
                        name: "default",
                        template: "{controller=Home}/{action=Index}/{id?}");
                });

                FileSystemWatcher watcher = new FileSystemWatcher();
                watcher.Path = Configuration["AppConfigurations:WatchMonitorigForImport"];
                watcher.NotifyFilter = NotifyFilters.Size;
                watcher.Filter = "*.*";
                watcher.Changed += new FileSystemEventHandler(OnChangedDirectory);
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }

        private void OnChangedDirectory(object sender, FileSystemEventArgs e)
        {
            var fileName = e.FullPath;

            var userId = _dbContext.Users.FirstOrDefault(s => s.NormalizedUserName == "INTERNAL").Id;

            ILogger _logger = _loggerFactory.CreateLogger("Directory observer");
            string error = "";
             
            // filter file types 
            if (Regex.IsMatch(fileName, @"\.xml|\.xlsx", RegexOptions.IgnoreCase))
            {
                _logger.LogWarning($"Finded new file for inport: {fileName}");

                try
                {
                    if (fileName.ToLower().EndsWith(".xml"))
                        error = ImportFile.ImportXml(fileName, (new FileInfo(fileName)).Name, _dbContext, _logger, Configuration.GetSection("AppConfigurations").GetValue<string>("ImportedExcelArchive", ""), userId, null);
                    if (fileName.ToLower().EndsWith(".xlsx"))
                        error = ImportFile.ImportExcel(fileName, (new FileInfo(fileName)).Name, _dbContext, _logger, Configuration.GetSection("AppConfigurations").GetValue<string>("ImportedExcelArchive", ""), userId, null);
                }
                catch(Exception ex)
                {
                    _logger.LogError($"Import {fileName} failed. {ex.Message}. {ex.StackTrace}");
                }

            }
        }


    }
}
