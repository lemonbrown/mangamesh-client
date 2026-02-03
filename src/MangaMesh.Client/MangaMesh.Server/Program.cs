using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Implementations;
using MangaMesh.Client.Services;
using MangaMesh.Server.Services;

var dataPath = Path.Combine(AppContext.BaseDirectory, "input");

var builder = WebApplication.CreateBuilder(args);

var trackerUrl = builder.Configuration["TrackerUrl"] ?? "https://localhost:7030";

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

builder.Services.AddSingleton<IBlobStore>(new BlobStore(dataPath));
builder.Services.AddSingleton<IManifestStore>(new ManifestStore(Path.Combine(dataPath, "manifests")));
builder.Services.AddSingleton<ISubscriptionStore>(new SubscriptionStore());
builder.Services.AddSingleton<INodeIdentityService>(sp => new NodeIdentityService(sp.GetRequiredService<ILogger<NodeIdentityService>>()));

builder.Services
        .AddScoped<ImportChapterService>()
        .AddScoped<IPeerFetcher, PeerFetcher>()
        .AddScoped<IKeyPairService, KeyPairService>()
        .AddScoped<IKeyStore, KeyStore>()
        .AddScoped<IKeyPairService, KeyPairService>()
        .AddScoped<IKeyStore, KeyStore>()
        .AddScoped<MangaMesh.Server.Services.IImportChapterService, ImportChapterServiceWrapper>()
        .AddSingleton<INodeConnectionInfoProvider, ServerNodeConnectionInfoProvider>();

builder.Services.AddHostedService<ReplicationService>();

builder.Services.AddHttpClient<IMetadataClient, HttpMetadataClient>(client =>
{
    client.BaseAddress = new Uri("https://metadata.mangamesh.net");
});

builder.Services.AddHttpClient<ITrackerClient, TrackerClient>(client =>
{
    client.BaseAddress = new Uri(trackerUrl);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    // Allow self-signed certs in development (e.g. Docker to Host)
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
});

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

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
