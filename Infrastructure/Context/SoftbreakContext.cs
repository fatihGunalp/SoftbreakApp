
using System.Text.Json;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
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
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
        v => v.ToUniversalTime(), // Kaydederken UTC'ye dönüştür
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            // YoutubeVideo ve YoutubeComment ilişkisi (1 - N)
            modelBuilder.Entity<YoutubeVideo>()
                .HasMany(v => v.Comments)
                .WithOne(c => c.Video)
                .HasForeignKey(c => c.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            // ValueConverter for float[] (Vector property) to JSON
            var vectorConverter = new ValueConverter<float[], string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),  // Convert float[] to JSON string
                v => JsonSerializer.Deserialize<float[]>(v, (JsonSerializerOptions)null)); // Convert JSON string back to float[]

            modelBuilder.Entity<YoutubeVideo>()
        .Property(v => v.Vector)
        .IsRequired(false);

            modelBuilder.Entity<YoutubeComment>()
        .Property(v => v.Vector)
        .IsRequired(false);

            modelBuilder.Entity<YoutubeVideo>()
                .Property(v => v.Vector)
                .HasConversion(vectorConverter);

            modelBuilder.Entity<YoutubeComment>()
                .Property(c => c.Vector)
                .HasConversion(vectorConverter);
        }
    }
}