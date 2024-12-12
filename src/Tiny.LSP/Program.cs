using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
//using Serilog;
using MediatR;
using Parkour;
using Parkour.LSP;

namespace Tiny.LSP;
 
internal class Program
{
    private static void Main(string[] args)
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        MainAsync(args).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }

    private static async Task MainAsync(string[] args)
    {
        // Debugger.Launch();
        // while (!Debugger.IsAttached)
        // {
        //     await Task.Delay(100);
        // }

        //Log.Logger = new LoggerConfiguration()
        //            .Enrich.FromLogContext()
        //            .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
        //            .MinimumLevel.Verbose()
        //            .CreateLogger();

        //Log.Logger.Information("This only goes file...");

        //IObserver<WorkDoneProgressReport> workDone = null!;

        var server = await LanguageServer.From(
            options =>
                options
                   .WithInput(Console.OpenStandardInput())
                   .WithOutput(Console.OpenStandardOutput())
                   .ConfigureLogging(
                        x => x
                            //.AddSerilog(Log.Logger)
                            .AddLanguageProtocolLogging()
                            .SetMinimumLevel(LogLevel.Debug)
                    )
                   .WithHandler<ParkourDocumentHandler>()
                   .WithHandler<ParkourSemanticTokensHandler>()
                   //.WithHandler<DidChangeWatchedFilesHandler>()
                   //.WithHandler<FoldingRangeHandler>()
                   //.WithHandler<MyWorkspaceSymbolsHandler>()
                   //.WithHandler<MyDocumentSymbolHandler>()
                   //.WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace)))
                   .WithServices(
                        services =>
                        {
                            services
                                .AddSingleton<ParkourLanguage, TinyLanguage>()
                                .AddSingleton<ParkourDocumentManager>()
                                .AddSingleton<ParkourDocumentServicesManager>();
                                //.AddSingleton(new ConfigurationItem { Section = "typescript" })
                                //.AddSingleton(new ConfigurationItem { Section = "terminal" });
                        }
                    )
                   .OnInitialize(
                        (server, request, token) =>
                        {
                            return Unit.Task;
                            //var manager = server.WorkDoneManager.For(
                            //    request, new WorkDoneProgressBegin
                            //    {
                            //        Title = "Server is starting...",
                            //        Percentage = 10,
                            //    }
                            //);
                            //workDone = manager;

                            //await Task.Delay(2000).ConfigureAwait(false);

                            //manager.OnNext(
                            //    new WorkDoneProgressReport
                            //    {
                            //        Percentage = 20,
                            //        Message = "loading in progress"
                            //    }
                            //);
                        }
                    )
                   .OnInitialized(
                        (server, request, response, token) =>
                        {
                            return Unit.Task;
                            //workDone.OnNext(
                            //    new WorkDoneProgressReport
                            //    {
                            //        Percentage = 40,
                            //        Message = "loading almost done",
                            //    }
                            //);

                            //await Task.Delay(2000).ConfigureAwait(false);

                            //workDone.OnNext(
                            //    new WorkDoneProgressReport
                            //    {
                            //        Message = "loading done",
                            //        Percentage = 100,
                            //    }
                            //);
                            //workDone.OnCompleted();
                        }
                    )
                   .OnStarted(
                        (languageServer, token) =>
                        {
                            return Unit.Task;

                            //using var manager = await languageServer.WorkDoneManager.Create(
                            //    new WorkDoneProgressBegin { Title = "Doing some work..." })
                            //    .ConfigureAwait(false);

                            //manager.OnNext(new WorkDoneProgressReport { Message = "doing things..." });
                            //await Task.Delay(10000).ConfigureAwait(false);
                            //manager.OnNext(new WorkDoneProgressReport { Message = "doing things... 1234" });
                            //await Task.Delay(10000).ConfigureAwait(false);
                            //manager.OnNext(new WorkDoneProgressReport { Message = "doing things... 56789" });

                            ////var logger = languageServer.Services.GetService<ILogger<Foo>>();
                            //var configuration = await languageServer.Configuration.GetConfiguration(
                            //    new ConfigurationItem
                            //    {
                            //        Section = "typescript",
                            //    }, new ConfigurationItem
                            //    {
                            //        Section = "terminal",
                            //    }
                            //).ConfigureAwait(false);

                            //var baseConfig = new JObject();
                            //foreach (var config in languageServer.Configuration.AsEnumerable())
                            //{
                            //    baseConfig.Add(config.Key, config.Value);
                            //}

                            ////logger.LogInformation("Base Config: {@Config}", baseConfig);

                            //var scopedConfig = new JObject();
                            //foreach (var config in configuration.AsEnumerable())
                            //{
                            //    scopedConfig.Add(config.Key, config.Value);
                            //}

                            //logger.LogInformation("Scoped Config: {@Config}", scopedConfig);
                        }
                    )
        ).ConfigureAwait(false);

        await server.WaitForExit.ConfigureAwait(false);
    }
}