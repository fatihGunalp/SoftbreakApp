using Core.Entities;
using Infrastructure.Context;
using Infrastructure.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Core.Interfaces;

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
                //await Task.Delay(5000, stoppingToken);
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

                        // Yeni videoları al
                        var newVideos = await youtubeDataService.GetNewChannelVideos(lastVideoDate);

                        foreach (var videoInfo in newVideos)
                        {
                            // Video veritabanında mevcut mu kontrol et
                            var existingVideo = await dbContext.YoutubeVideos
                                .Include(v => v.Comments) // Yorumları da dahil et
                                .FirstOrDefaultAsync(v => v.VideoId == videoInfo.VideoId);

                            if (existingVideo == null)
                            {
                                // Yeni video ekle
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
                                // Mevcut video için yeni yorumları kontrol et
                                var latestCommentDate = existingVideo.Comments
                                    .OrderByDescending(c => c.PublishedAt)
                                    .Select(c => c.PublishedAt)
                                    .FirstOrDefault();

                                var newComments = await youtubeDataService.GetVideoComments(videoInfo.VideoId);

                                foreach (var comment in newComments)
                                {
                                    if (comment.PublishedAt > latestCommentDate)
                                    {
                                        var newComment = new YoutubeComment
                                        {
                                            Author = comment.Author,
                                            Text = comment.Text,
                                            LikeCount = comment.LikeCount,
                                            PublishedAt = comment.PublishedAt.ToUniversalTime(),
                                            VideoId = existingVideo.Id
                                        };

                                        dbContext.YoutubeComments.Add(newComment);
                                        Console.WriteLine($"Yeni yorum eklendi: {newComment.Text}");
                                    }
                                }
                            }
                        }

                        await dbContext.SaveChangesAsync();
                        Console.WriteLine("Videolar ve yorumlar başarıyla güncellendi.");
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
