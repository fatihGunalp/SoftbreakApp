using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.ExternalServices
{
    public class YoutubeDataService : IYoutubeDataService
    {
        private readonly YouTubeService _youTubeService;
        private readonly IConfiguration _configuration;

        public YoutubeDataService(IConfiguration configuration)
        {
            _configuration = configuration;
            var clientSecretsPath = configuration["YouTube:ClientSecretsPath"];
            var applicationName = configuration["YouTube:ApplicationName"];
            _youTubeService = GetYouTubeService(clientSecretsPath, applicationName).Result;
        }

        private static async Task<YouTubeService> GetYouTubeService(string clientSecretsPath, string applicationName)
        {
            using var stream = new FileStream(clientSecretsPath, FileMode.Open, FileAccess.Read);
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                new[] { YouTubeService.Scope.YoutubeForceSsl },
                "user",
                CancellationToken.None,
                new FileDataStore("YouTube.Auth.Store")
            );

            return new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName
            });
        }

        public async Task<List<YoutubeVideo>> GetAllChannelVideos()
        {
            var videoList = new List<YoutubeVideo>();

            // Kanalın yüklemeler oynatma listesini al
            var channelRequest = _youTubeService.Channels.List("contentDetails");
            channelRequest.Id = _configuration["YouTube:ChannelId"];
            var channelResponse = await channelRequest.ExecuteAsync();

            if (channelResponse.Items.Count == 0)
            {
                Console.WriteLine("Kanal bulunamadı veya geçersiz bir Kanal ID'si kullanıldı.");
                return videoList;
            }

            var uploadsPlaylistId = channelResponse.Items[0].ContentDetails.RelatedPlaylists.Uploads;

            // Oynatma listesindeki videoları çek
            string nextPageToken = null;

            do
            {
                var playlistItemsRequest = _youTubeService.PlaylistItems.List("snippet");
                playlistItemsRequest.PlaylistId = uploadsPlaylistId;
                playlistItemsRequest.MaxResults = 50;
                playlistItemsRequest.PageToken = nextPageToken;

                var playlistItemsResponse = await playlistItemsRequest.ExecuteAsync();

                foreach (var playlistItem in playlistItemsResponse.Items)
                {
                    var videoId = playlistItem.Snippet.ResourceId.VideoId;

                    // Videonun detaylarını alarak etiketleri çek
                    var videoDetailsRequest = _youTubeService.Videos.List("snippet,statistics");
                    videoDetailsRequest.Id = videoId;

                    var videoDetailsResponse = await videoDetailsRequest.ExecuteAsync();
                    var videoDetails = videoDetailsResponse.Items.FirstOrDefault();

                    string tags = videoDetails?.Snippet.Tags != null
                        ? string.Join(",", videoDetails.Snippet.Tags)
                        : string.Empty;

                    // Videonun yorumlarını al
                    var comments = await GetVideoComments(videoId);

                    // Videonun izlenme sayısını al
                    var viewCount = videoDetails?.Statistics.ViewCount ?? 0;

                    // YoutubeVideo modelini ekle
                    var video = new YoutubeVideo
                    {
                        VideoId = videoId,
                        Title = playlistItem.Snippet.Title,
                        Description = playlistItem.Snippet.Description,
                        Tags = tags,
                        PublishedAt = (playlistItem.Snippet.PublishedAt ?? DateTime.MinValue).ToUniversalTime(),
                        Comments = comments,
                    };

                    videoList.Add(video);
                }

                nextPageToken = playlistItemsResponse.NextPageToken;

            } while (!string.IsNullOrEmpty(nextPageToken));

            // Videoları tarih sırasına göre sırala
            return videoList.OrderBy(x => x.PublishedAt).ToList();
        }

        public async Task<List<YoutubeComment>> GetVideoComments(string videoId)
        {
           var comments = new List<YoutubeComment>();
            string nextPageToken = null;

            try
            {
                do
                {
                    var commentRequest = _youTubeService.CommentThreads.List("snippet");
                    commentRequest.VideoId = videoId;
                    commentRequest.MaxResults = 50;
                    commentRequest.PageToken = nextPageToken;

                    var commentResponse = await commentRequest.ExecuteAsync();

                    foreach (var commentThread in commentResponse.Items)
                    {
                        var topLevelComment = commentThread.Snippet.TopLevelComment.Snippet;
                        var comment = new YoutubeComment
                        {
                            Author = topLevelComment.AuthorDisplayName,
                            Text = topLevelComment.TextOriginal,
                            LikeCount = (int)(topLevelComment.LikeCount ?? 0),
                            PublishedAt = (topLevelComment.PublishedAt ?? DateTime.MinValue).ToUniversalTime(),
                            Vector = new float[] { }
                        };
                        comments.Add(comment);
                    }

                    nextPageToken = commentResponse.NextPageToken;

                } while (!string.IsNullOrEmpty(nextPageToken));
            }
            catch (Google.GoogleApiException ex) when (ex.Error != null && ex.Error.Code == 403 && ex.Error.Errors[0].Reason == "commentsDisabled")
            {
                Console.WriteLine($"VideoID {videoId} için yorumlar devre dışı bırakılmış.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VideoID {videoId} için yorumları alırken bir hata oluştu: {ex.Message}");
            }

            return comments;
        }

        public async Task<List<YoutubeVideo>> GetNewChannelVideos(DateTime? lastVideoDate = null)
        {
           var videoList = new List<YoutubeVideo>();

            try
            {
                // YouTube Search API kullanarak videoları belirli bir tarihten sonra getir
                var searchRequest = _youTubeService.Search.List("snippet");
                searchRequest.ChannelId = _configuration["YouTube:ChannelId"];
                searchRequest.MaxResults = 50; // Maksimum 50 sonuç
                searchRequest.Type = "video";

                // Eğer son video tarihi varsa, bu tarihten sonrasını ara
                if (lastVideoDate.HasValue)
                {
                    searchRequest.PublishedAfter = lastVideoDate.Value.ToUniversalTime();
                }

                var searchResponse = await searchRequest.ExecuteAsync();

                foreach (var searchResult in searchResponse.Items)
                {
                    var videoId = searchResult.Id.VideoId;

                    // Video detaylarını al
                    var videoDetailsRequest = _youTubeService.Videos.List("snippet,statistics");
                    videoDetailsRequest.Id = videoId;
                    var videoDetailsResponse = await videoDetailsRequest.ExecuteAsync();
                    var videoDetails = videoDetailsResponse.Items.FirstOrDefault();

                    if (videoDetails != null)
                    {
                        // Videoya ait yorumları çek
                        var comments = await GetVideoComments(videoId);

                        // YoutubeVideo modelini oluştur
                        var video = new YoutubeVideo
                        {
                            VideoId = videoId,
                            Title = videoDetails.Snippet.Title,
                            Description = videoDetails.Snippet.Description,
                            Tags = videoDetails.Snippet.Tags != null
                                ? string.Join(",", videoDetails.Snippet.Tags)
                                : string.Empty,
                            PublishedAt = videoDetails.Snippet.PublishedAt ?? DateTime.UtcNow,
                            Comments = comments
                        };

                        videoList.Add(video);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"YouTube API'den videolar çekilirken hata oluştu: {ex.Message}");
            }

            return videoList;
 
        }

        
    }
}