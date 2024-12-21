using Core.Entities;
using Infrastructure.Context;
using Infrastructure.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Core.Interfaces;
using Google.Apis.YouTube.v3.Data;


namespace Infrastructure.BackgroundServices
{
    public class YoutubeSyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public YoutubeSyncBackgroundService(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1).AddHours(0).AddMinutes(0); // Gece 00:00
                var delay = nextRun - now;

                await Task.Delay(delay, stoppingToken);
                //await Task.Delay(1000, stoppingToken); // Test için 1 saniye gecikme

                using (var scope = _serviceProvider.CreateScope())
                {
                    var youtubeDataService = scope.ServiceProvider.GetRequiredService<IYoutubeDataService>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<SoftbreakContext>();

                    var channelId = _configuration["YouTube:ChannelId"];

                    try
                    {
                        // En son video yayın tarihini kontrol et
                        var lastVideoDate = await dbContext.YoutubeVideos
                            .OrderByDescending(v => v.PublishedAt)
                            .Select(v => v.PublishedAt)
                            .FirstOrDefaultAsync();

                        // YouTube'dan yeni videoları çek
                        var newVideos = await youtubeDataService.GetNewChannelVideos(lastVideoDate);

                        foreach (var videoInfo in newVideos)
                        {
                            // Video veritabanında mevcut mu kontrol et
                            var exists = await dbContext.YoutubeVideos
                                .AnyAsync(v => v.VideoId == videoInfo.VideoId);

                            if (!exists) // Eğer mevcut değilse ekle
                            {
                                var comments = await youtubeDataService.GetVideoComments(videoInfo.VideoId);

                                var video = new YoutubeVideo
                                {
                                    VideoId = videoInfo.VideoId,
                                    Title = videoInfo.Title,
                                    Description = videoInfo.Description,
                                    PublishedAt = videoInfo.PublishedAt.ToUniversalTime(),
                                    Comments = comments.Select(c => new YoutubeComment
                                    {
                                        Author = c.Author,
                                        Text = c.Text,
                                        LikeCount = c.LikeCount,
                                        PublishedAt = c.PublishedAt.ToUniversalTime()
                                    }).ToList()
                                };

                                dbContext.YoutubeVideos.Add(video);
                                Console.WriteLine($"Yeni video eklendi: {video.Title}");

                            }
                            else
                            {
                                Console.WriteLine($"Video ve yorumlar güncel!");
                            }
                        }

                        await dbContext.SaveChangesAsync();
                        Console.WriteLine($"Yeni videolar ve yorumlar ekleme işlemi tamamlandı.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"YouTube veri kontrolü sırasında hata oluştu: {ex.Message}");
                    }
                }
            }
        }
    }
}
