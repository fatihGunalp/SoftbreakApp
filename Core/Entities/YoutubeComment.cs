
namespace Core.Entities
{
    public class YoutubeComment
    {
        public int Id { get; set; }
        public string Author { get; set; }
        public string Text { get; set; }
        public int LikeCount { get; set; }
        public DateTime PublishedAt { get; set; }
        public string? Reply { get; set; }

        // Foreign Key for Video
        public int VideoId { get; set; }
        public YoutubeVideo Video { get; set; }

        public float[]? Vector { get; set; } // OpenAI Embedding Verisi

    }
}