using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Implementations;
using MangaMesh.Client.Services;
using MangaMesh.Server.Services;
using Microsoft.Extensions.DependencyInjection;

var root = "C:\\Users\\cameron\\source\\repos\\mangamesh-client\\src\\MangaMesh.Client\\MangaMesh.Client\\bin\\Debug\\net8.0\\input";

var builder = WebApplication.CreateBuilder(args);

var trackerUrl = "https://localhost:7030";


// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IBlobStore>(new BlobStore(root));
builder.Services.AddSingleton<IManifestStore>(new ManifestStore(root));
builder.Services.AddSingleton<ISubscriptionStore>(new SubscriptionStore());

builder.Services
        .AddScoped<ImportChapterService>()
        .AddScoped<IPeerFetcher, PeerFetcher>()
        .AddScoped<IKeyPairService, KeyPairService>()
        .AddScoped<IKeyStore, KeyStore>()
        .AddScoped<MangaMesh.Server.Services.IImportChapterService, ImportChapterServiceWrapper>();

builder.Services.AddHostedService<ReplicationService>();

builder.Services.AddHttpClient<IMetadataClient, HttpMetadataClient>(client =>
{
    client.BaseAddress = new Uri("https://metadata.mangamesh.net");
});

builder.Services.AddHttpClient<ITrackerClient, TrackerClient>(client =>
{
    client.BaseAddress = new Uri(trackerUrl);
});

//builder.Services.AddHostedService(provider =>
//        new ReplicationService(
//            tracker: provider.GetRequiredService<ITrackerClient>(),
//            fetcher: null,
//            subscriptionStore: provider.GetRequiredService<ISubscriptionStore>(),
//            manifests: provider.GetRequiredService<IManifestStore>(),
//            metadata: provider.GetRequiredService<IMetadataClient>(),
//            logger: provider.GetRequiredService<ILogger<ReplicationService>>(),
//            nodeId: Guid.NewGuid().ToString(),
//            publicIp: "1.2.3.4",
//            port: 5000
//        )
//    );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
