
using System.Text.Json;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Context
{
    public class SoftbreakContext:DbContext
    {
        public SoftbreakContext(){}
        public SoftbreakContext(DbContextOptions<SoftbreakContext> options) :base(options){}

        //DbSet
        public DbSet<YoutubeVideo> YoutubeVideos { get; set; }
        public DbSet<YoutubeComment> YoutubeComments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(!optionsBuilder.IsConfigured){
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=softbreak_test;Username=postgres;Password=84rdi");
            }
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // DateTime için ValueConverter (UTC Dönüşümü)
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.ToUniversalTime(), // Kaydederken UTC'ye dönüştür
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)); // Okurken UTC türünü belirle

            // YoutubeVideo ve YoutubeComment ilişkisi (1 - N)
            modelBuilder.Entity<YoutubeVideo>()
                .HasMany(v => v.Comments)
                .WithOne(c => c.Video)
                .HasForeignKey(c => c.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Vector (float[]) için ValueConverter ve ValueComparer
            var vectorConverter = new ValueConverter<float[], string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null), // float[] -> JSON string
                v => JsonSerializer.Deserialize<float[]>(v, (JsonSerializerOptions)null)); // JSON string -> float[]

            var vectorComparer = new ValueComparer<float[]>(
                (v1, v2) => v1.SequenceEqual(v2), // İki float[] eşit mi?
                v => v.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())), // HashCode hesaplama
                v => v.ToArray()); // Derin kopya

            // YoutubeVideo.Vector için Converter ve Comparer
            modelBuilder.Entity<YoutubeVideo>()
                .Property(v => v.Vector)
                .IsRequired(false)
                .HasConversion(vectorConverter)
                .Metadata.SetValueComparer(vectorComparer);

            // YoutubeComment.Vector için Converter ve Comparer
            modelBuilder.Entity<YoutubeComment>()
                .Property(c => c.Vector)
                .IsRequired(false)
                .HasConversion(vectorConverter)
                .Metadata.SetValueComparer(vectorComparer);
        }

    }
}