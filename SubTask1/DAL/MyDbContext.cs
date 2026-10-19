using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using webapp_f26_exam.Models;

namespace webapp_f26_exam.DAL;

public class MyDbContext : IdentityDbContext<IdentityUser>
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }

    public DbSet<Room> Rooms { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Room>()
            .Property(r => r.Amenities)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => ParseAmenities(v)
            );
    }

    private static List<string> ParseAmenities(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(v) ?? new List<string>();
        }
        catch
        {
            // Fallback: If invalid JSON (like unescaped quotes or comma-separated text),
            // clean up brackets and split by comma or quotes
            var cleaned = v.Trim('[', ']', ' ', '\r', '\n');
            return cleaned.Split(new[] { ',', '"' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => s.Trim())
                          .Where(s => !string.IsNullOrEmpty(s))
                          .ToList();
        }
    }
}
