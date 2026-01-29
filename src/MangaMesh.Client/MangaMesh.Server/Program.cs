using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Implementations;
using MangaMesh.Client.Services;
using MangaMesh.Server.Services;

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
builder.Services.AddScoped<ImportChapterService>();
builder.Services.AddScoped<IKeyPairService, KeyPairService>()
        .AddScoped<IKeyStore, KeyStore>();

builder.Services.
    AddScoped<MangaMesh.Server.Services.IImportChapterService, ImportChapterServiceWrapper>();

builder.Services.AddHttpClient<IMetadataClient, HttpMetadataClient>(client =>
{
    client.BaseAddress = new Uri("https://metadata.mangamesh.net");
});

builder.Services.AddHttpClient<ITrackerClient, TrackerClient>(client =>
{
    client.BaseAddress = new Uri(trackerUrl);
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

app.MapControllers();

app.Run();
