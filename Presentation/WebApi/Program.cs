using Application.Services;
using Core.Interfaces;
using Infrastructure.BackgroundServices;
using Infrastructure.Context;
using Infrastructure.ExternalServices;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// PostgreSQL veritabanı bağlantısı ve Context'in eklenmesi
builder.Services.AddDbContext<SoftbreakContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQLConnection"), b => b.MigrationsAssembly("WebApi")));

//OpenAI_ApiKey
var apiKey = Environment.GetEnvironmentVariable("OpenAI_ApiKey");

// Dependency Injection
builder.Services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
builder.Services.AddScoped<IYoutubeDataService, YoutubeDataService>();
builder.Services.AddScoped<IYoutubeVideoRepository, YoutubeVideoRepository>();
builder.Services.AddScoped<IOpenAIEmbeddingService, OpenAIEmbeddingService>();
builder.Services.AddScoped<IYoutubeVideoRepository,YoutubeVideoRepository>();
builder.Services.AddScoped<YoutubeVideoService>();
builder.Services.AddScoped<DatabaseEmbedding>();

// YouTube veri güncellemelerini zamanlayarak kontrol eden Background Service ekleniyor.
builder.Services.AddHostedService<YoutubeSyncBackgroundService>();

// CORS Ayarları (Özelleştirin)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Veritabanı başlatma işlemi
using (var scope = app.Services.CreateScope())
{
    var databaseInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    await databaseInitializer.InitializeAsync();
}

// Veritabanı Embedding güncelleme servisini çalıştır
using (var scope = app.Services.CreateScope())
{
    var updaterService = scope.ServiceProvider.GetRequiredService<DatabaseEmbedding>();
    await updaterService.UpdateEmbeddingsAsync();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll"); // CORS politikası uygulanıyor
app.UseAuthorization();

app.MapControllers();

app.Run();