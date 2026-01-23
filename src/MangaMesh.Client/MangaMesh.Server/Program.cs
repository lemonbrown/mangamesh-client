using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Implementations;

var root = "C:\\Users\\cameron\\source\\repos\\mangamesh-client\\src\\MangaMesh.Client\\MangaMesh.Client\\bin\\Debug\\net8.0\\input";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IBlobStore>(new BlobStore(root));
builder.Services.AddSingleton<IManifestStore>(new ManifestStore(root));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
