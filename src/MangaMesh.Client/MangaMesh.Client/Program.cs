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

var blobStore = new BlobStore(root);

var manifestStore = new ManifestStore(root);

var importChapterService = new ImportChapterService(blobStore, manifestStore);

var chapterMetadataService = new ChapterMetadataService();

if (!string.IsNullOrWhiteSpace(chapterPathLocation))
{

    var metadata = await chapterMetadataService.LoadMetadataAsync(chapterPathLocation);

    Console.WriteLine($"Found Chapter Metadata [{metadata.SeriesId}] chapter [{metadata.ChapterNumber}]");

    // Build the chapter manifest
    var manifest = new ChapterManifest
    {
        ManifestVersion = 1,
        SeriesId = metadata.SeriesId,
        ChapterNumber = metadata.ChapterNumber,
        Language = metadata.Language,
        ClaimedScanGroup = metadata.ClaimedScanGroup
    };

    await importChapterService.ImportChapterAsync(chapterPathLocation, manifest);

    Console.WriteLine($"Import complete");

}
else
{
    Console.WriteLine("Skipping import...");
}

var trackerClient = new TrackerClient(trackerUrl);

Console.WriteLine("Announcing node...");

Console.WriteLine($"Announcing to [{trackerUrl}]");

var announcementResponse = await trackerClient.AnnounceAsync(
    nodeId: "node123",
    ip: "192.168.1.100",
    port: 5000,
    manifestHashes: new List<string> {}
);

Console.WriteLine($"Announcement complete [success]");

Console.WriteLine("Querying peers for manifests...");

// Query peers for a manifest
var peers = await trackerClient.GetPeersForManifestAsync("abc123");
foreach (var peer in peers)
{
    Console.WriteLine($"Peer: {peer.NodeId} at {peer.IP}:{peer.Port}");
}

var builder = new HostBuilder().ConfigureServices(services =>
{
    services.AddHostedService(provider =>
        new ReplicationService(
            tracker: provider.GetRequiredService<ITrackerClient>(),
            fetcher: provider.GetRequiredService<IPeerFetcher>(),
            subscriptions: provider.GetRequiredService<ISubscriptionStore>(),
            manifests: provider.GetRequiredService<IManifestStore>(),
            metadata: provider.GetRequiredService<IMetadataClient>(),
            logger: provider.GetRequiredService<ILogger<ReplicationService>>(),
            nodeId: Guid.NewGuid().ToString(),
            publicIp: "1.2.3.4",
            port: 5000
        )
    );
});

Console.WriteLine("Running replication services...");

builder.Build().Run();

Console.ReadLine();