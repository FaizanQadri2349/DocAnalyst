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

var app = builder.Build();

// 2. Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // This turns on the blue page
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();