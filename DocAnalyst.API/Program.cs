using DocAnalyst.Core.Interfaces;
using DocAnalyst.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
builder.Services.AddControllers();

// --- USE THIS FOR THE BLUE PAGE (Classic Swagger) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register our PDF Service
builder.Services.AddScoped<IPdfService, PdfPigService>();
builder.Services.AddScoped<DocAnalyst.Core.Interfaces.IAiService, DocAnalyst.Infrastructure.Services.OllamaService>();

// Register Qdrant Vector DB Service
var qdrantHost = builder.Configuration["Qdrant:Host"] ?? "localhost";
var qdrantPort = int.Parse(builder.Configuration["Qdrant:Port"] ?? "6334");
builder.Services.AddSingleton<IVectorDbService>(sp => new QdrantService(qdrantHost, qdrantPort));

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 2. Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // This turns on the blue page
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();