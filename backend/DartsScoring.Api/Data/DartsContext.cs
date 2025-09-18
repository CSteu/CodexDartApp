using DartsScoring.Api.Domain.Entities;
using DartsScoring.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DartsScoring.Api.Data;

public class DartsContext(DbContextOptions<DartsContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchPlayer> MatchPlayers => Set<MatchPlayer>();
    public DbSet<Leg> Legs => Set<Leg>();
    public DbSet<Turn> Turns => Set<Turn>();
    public DbSet<Throw> Throws => Set<Throw>();
    public DbSet<CricketState> CricketStates => Set<CricketState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Match>(builder =>
        {
            builder.Property(m => m.Mode).HasConversion<string>();
            builder.Property(m => m.Status).HasConversion<string>();
            builder.HasMany(m => m.Legs)
                   .WithOne(l => l.Match)
                   .HasForeignKey(l => l.MatchId);
            builder.HasMany(m => m.Players)
                   .WithOne(mp => mp.Match)
                   .HasForeignKey(mp => mp.MatchId);
        });

        modelBuilder.Entity<MatchPlayer>(builder =>
        {
            builder.HasIndex(mp => new { mp.MatchId, mp.PlayerId }).IsUnique();
            builder.HasOne(mp => mp.Player)
                   .WithMany(p => p.Matches)
                   .HasForeignKey(mp => mp.PlayerId);
        });

        modelBuilder.Entity<Leg>(builder =>
        {
            builder.HasOne(l => l.StartingPlayer)
                   .WithMany(p => p.StartingLegs)
                   .HasForeignKey(l => l.StartingPlayerId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(l => l.WinnerPlayer)
                   .WithMany(p => p.WonLegs)
                   .HasForeignKey(l => l.WinnerPlayerId)
                   .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Turn>(builder =>
        {
            builder.HasOne(t => t.Player)
                   .WithMany(p => p.Turns)
                   .HasForeignKey(t => t.PlayerId)
                   .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CricketState>(builder =>
        {
            builder.HasIndex(c => new { c.LegId, c.PlayerId }).IsUnique();
            builder.HasOne(c => c.Player)
                   .WithMany(p => p.CricketStates)
                   .HasForeignKey(c => c.PlayerId)
                   .OnDelete(DeleteBehavior.Restrict);
        });

        Seed(modelBuilder);
    }

    private static void Seed(ModelBuilder modelBuilder)
    {
        var seedStartedAt = new DateTime(2024, 01, 01, 12, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Player>().HasData(
            new Player { Id = 1, DisplayName = "Alice" },
            new Player { Id = 2, DisplayName = "Bob" }
        );

        modelBuilder.Entity<Match>().HasData(
            new Match
            {
                Id = 1,
                Mode = MatchMode.X01,
                TargetScore = 501,
                DoubleOut = true,
                StartedAt = seedStartedAt,
                Status = MatchStatus.InProgress
            },
            new Match
            {
                Id = 2,
                Mode = MatchMode.Cricket,
                TargetScore = 0,
                DoubleOut = false,
                StartedAt = seedStartedAt.AddMinutes(30),
                Status = MatchStatus.InProgress
            }
        );

        modelBuilder.Entity<MatchPlayer>().HasData(
            new MatchPlayer { Id = 1, MatchId = 1, PlayerId = 1, Order = 0 },
            new MatchPlayer { Id = 2, MatchId = 1, PlayerId = 2, Order = 1 },
            new MatchPlayer { Id = 3, MatchId = 2, PlayerId = 1, Order = 0 },
            new MatchPlayer { Id = 4, MatchId = 2, PlayerId = 2, Order = 1 }
        );

        modelBuilder.Entity<Leg>().HasData(
            new Leg
            {
                Id = 1,
                MatchId = 1,
                LegNumber = 1,
                StartingPlayerId = 1
            },
            new Leg
            {
                Id = 2,
                MatchId = 2,
                LegNumber = 1,
                StartingPlayerId = 2
            }
        );

        modelBuilder.Entity<CricketState>().HasData(
            new CricketState { Id = 1, LegId = 2, PlayerId = 1 },
            new CricketState { Id = 2, LegId = 2, PlayerId = 2 }
        );
    }
}
