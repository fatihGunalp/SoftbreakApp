namespace Core.Entities
{
    public class YoutubeVideo
    {
         public int Id { get; set; } 
        public string VideoId { get; set; } 
        public string Title { get; set; } 
        public string Description { get; set; } 
        public string? Tags { get; set; } 
        public DateTime PublishedAt { get; set; } 

    
        public float[]? Vector { get; set; }  // OpenAI Embedding Verisi

       
        public ICollection<YoutubeComment> Comments { get; set; }
    }
}