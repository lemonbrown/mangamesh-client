// See https://aka.ms/new-console-template for more information
using MangaMesh.Client.Implementations;
using MangaMesh.Client.Models;
using MangaMesh.Client.Services;

Console.WriteLine("                                                   _     \r\n  /\\/\\   __ _ _ __   __ _  __ _    /\\/\\   ___  ___| |__  \r\n /    \\ / _` | '_ \\ / _` |/ _` |  /    \\ / _ \\/ __| '_ \\ \r\n/ /\\/\\ \\ (_| | | | | (_| | (_| | / /\\/\\ \\  __/\\__ \\ | | |\r\n\\/    \\/\\__,_|_| |_|\\__, |\\__,_| \\/    \\/\\___||___/_| |_|\r\n                    |___/                                ");

var root = "input";

var trackerUrl = "localhost:5000";

Console.WriteLine($"Root: [/{root}]");

Console.WriteLine("Enter Chapter Location: ");

var chapterPathLocation = Console.ReadLine();

var blobStore = new BlobStore(root);

var manifestStore = new ManifestStore(root);

var importChapterService = new ImportChapterService(blobStore, manifestStore);

var chapterMetadataService = new ChapterMetadataService();

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

var trackerClient = new TrackerClient(trackerUrl);

Console.WriteLine("Announcing node...");

Console.WriteLine($"Announcing to [{trackerUrl}]");

var announcementResponse = await trackerClient.AnnounceAsync(
    nodeId: "node123",
    ip: "192.168.1.100",
    port: 5000,
    manifestHashes: new List<string> { "abc123", "def456" }
);

Console.WriteLine($"Announcement complete [success]");

Console.WriteLine("Querying peers for manifests...");

// Query peers for a manifest
var peers = await trackerClient.GetPeersForManifestAsync("abc123");
foreach (var peer in peers)
{
    Console.WriteLine($"Peer: {peer.NodeId} at {peer.IP}:{peer.Port}");
}

Console.ReadLine();