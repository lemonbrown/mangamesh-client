// See https://aka.ms/new-console-template for more information
using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Implementations;
using MangaMesh.Client.Models;
using MangaMesh.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

Console.WriteLine("                                                   _     \r\n  /\\/\\   __ _ _ __   __ _  __ _    /\\/\\   ___  ___| |__  \r\n /    \\ / _` | '_ \\ / _` |/ _` |  /    \\ / _ \\/ __| '_ \\ \r\n/ /\\/\\ \\ (_| | | | | (_| | (_| | / /\\/\\ \\  __/\\__ \\ | | |\r\n\\/    \\/\\__,_|_| |_|\\__, |\\__,_| \\/    \\/\\___||___/_| |_|\r\n                    |___/                                ");

var root = "input";

var trackerUrl = "https://localhost:7030";

Console.WriteLine($"Root: [/{root}]");

Console.WriteLine("Enter Chapter Location: ");

var chapterPathLocation = Console.ReadLine();

var builder = new HostBuilder().ConfigureServices(services =>
{
    services
    .AddLogging(n => n.AddConsole())
    .AddScoped<ITrackerClient, TrackerClient>()
    .AddScoped<IPeerFetcher, PeerFetcher>()
    .AddScoped<IManifestStore, ManifestStore>()
    .AddSingleton<IBlobStore>(new BlobStore(root))
    .AddSingleton<IManifestStore>(new ManifestStore(root))
    .AddScoped<IKeyStore, KeyStore>();

    services.AddHttpClient<IMetadataClient, HttpMetadataClient>(client =>
    {
        client.BaseAddress = new Uri("https://metadata.mangamesh.net");
    });

    services.AddHttpClient<ITrackerClient, TrackerClient>(client =>
    {
        client.BaseAddress = new Uri(trackerUrl);
    });


    //services.AddHostedService(provider =>
    //    new ReplicationService(
    //        tracker: provider.GetRequiredService<ITrackerClient>(),
    //        fetcher: null,
    //        subscriptionStore: provider.GetRequiredService<ISubscriptionStore>(),
    //        manifests: provider.GetRequiredService<IManifestStore>(),
    //        metadata: provider.GetRequiredService<IMetadataClient>(),
    //        logger: provider.GetRequiredService<ILogger<ReplicationService>>(),
    //        nodeId: Guid.NewGuid().ToString(),
    //        publicIp: "1.2.3.4",
    //        port: 5000
    //    )
    //);
});

Console.WriteLine("Running replication services...");

builder.Build().Run();

Console.ReadLine();