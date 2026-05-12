using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Windows.Forms;

namespace Prodright
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Build a Generic Host so WinForms can use DI, HttpClientFactory, Options, Logging
            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Optional: add appsettings.json support (recommended)
                    // Make sure you set file property "Copy to Output Directory" to "Copy if newer"
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IProductLookupService, ProductLookupService>();
                    // ---- Options ----
                    services.Configure<SapODataOptions>(context.Configuration.GetSection("SapOData"));
                    services.Configure<MsalOptions>(context.Configuration.GetSection("Msal"));

                    // ---- Logging (optional but recommended) ----
                    services.AddLogging(b =>
                    {
                        b.AddConsole();
                        b.AddDebug();
                    });

                    // ---- Auth token provider (Desktop SSO via MSAL) ----
                    services.AddSingleton<IAccessTokenProvider, MsalAccessTokenProvider>();

                    // ---- Delegating handler to attach bearer tokens ----
                    services.AddTransient<SapBearerTokenHandler>();

                    // ---- Typed HttpClient for SAP OData ----
                    services.AddHttpClient<SapProductClient>((sp, http) =>
                    {
                        var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SapODataOptions>>().Value;

                        http.BaseAddress = new Uri(new Uri(opts.BaseUrl), opts.ServiceRoot);

                        http.DefaultRequestHeaders.Accept.Clear();
                        http.DefaultRequestHeaders.Accept.Add(
                            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    })
                    .AddHttpMessageHandler<SapBearerTokenHandler>();

                    // ---- Your WinForms ----
                    services.AddTransient<Form1>(); // Form1 can take dependencies in its constructor
                })
                .Build();

            // Resolve Form1 from DI so it can receive SapProductClient etc.
            var form = host.Services.GetRequiredService<Form1>();
            Application.Run(form);
        }
    }
}