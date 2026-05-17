using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using TaskStorm.Controller;
using TaskStorm.Event;
using TaskStorm.Log;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Entity.Masterdata;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Service;

namespace TaskStorm.Data;

public class PostgresqlDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Issue> Issues { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Key> Keys { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<ActivityPropertyUpdated> ActivitiesPropertyUpdated { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<CommentAttachment> Attachments { get; set; }
    public DbSet<MasterdataValue> MasterdataValues { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public PostgresqlDbContext(DbContextOptions<PostgresqlDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        try
        {
            var dbName = Environment.GetEnvironmentVariable("TS_DB_NAME") ?? "testdb";
            var dbHost = Environment.GetEnvironmentVariable("TS_DB_HOST") ?? "localhost";
            var dbPort = Environment.GetEnvironmentVariable("TS_DB_PORT") ?? "5432";
            var dbUser = Environment.GetEnvironmentVariable("TS_DB_USER") ?? "postgres";
            var dbPassword = Environment.GetEnvironmentVariable("TS_DB_PASSWORD") ?? "postgres";

            var connectionString =
                $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SearchPath=public";
            Console.WriteLine("DB connection details:");
            Console.WriteLine($"DB={dbName}");
            Console.WriteLine($"Host={dbHost}");
            Console.WriteLine($"Port={dbPort}");
            Console.WriteLine($"Username={dbUser}");
            Console.WriteLine($"Password={dbPassword}");

            optionsBuilder
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention();

        }
        catch (System.Exception ex)
        {
            Console.WriteLine("!!! EXCEPTION DURING DB CONFIGURATION !!!");
            Console.WriteLine(ex.GetType().FullName);
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            throw; 
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        //  USER - ROLE many-to-many relationship
        modelBuilder.Entity<User>()
            .HasMany(u => u.Roles)
            .WithMany(r => r.Users);

        // USER - RefreshToken one-to-many relationship
        modelBuilder.Entity<User>()
            .HasMany(u => u.RefreshTokens)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ISSUE - relationship to USER (Assignee)
        modelBuilder.Entity<Issue>()
          .HasOne(i => i.Assignee)
          .WithMany(u => u.AssignedIssues)
          .HasForeignKey("AssigneeId")
          .OnDelete(DeleteBehavior.Restrict);
        ;
        // ISSUE - relationship to USER (Author)
        modelBuilder.Entity<Issue>()
          .HasOne(i => i.Author)
          .WithMany(u => u.AuthoredIssues)
          .HasForeignKey("AuthorId")
          .OnDelete(DeleteBehavior.Restrict);

        // COMMENT - relationships to USER (Author) and ISSUE (Issue)
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Issue)
            .WithMany(i => i.Comments)
            .HasForeignKey("IssueId")
            .OnDelete(DeleteBehavior.Cascade);

        // PROJECT - relationship to ISSUE (Issues)
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Issues)
            .WithOne(i => i.Project)
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        // Project - CreatedAt default value to UTC now
        modelBuilder.Entity<Project>()
            .Property(p => p.CreatedAt)
            .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        // KEY - one-to-one relationship with ISSUE 
        modelBuilder.Entity<Key>()
            .HasOne(k => k.Issue)
            .WithOne(i => i.Key)
            .HasForeignKey<Key>(k => k.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        // KEY - many-to-one relationship with PROJECT
        modelBuilder.Entity<Key>()
            .HasOne(k => k.Project)
            .WithMany(p => p.Keys)
            .HasForeignKey(k => k.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // ACTIVITY - relationship to ISSUE (Issue)
        modelBuilder.Entity<Issue>()
                    .HasMany(i => i.Activities)
                    .WithOne(a => a.Issue)
                    .HasForeignKey(a => a.IssueId)
                    .OnDelete(DeleteBehavior.Cascade);

        // ACTIVITY - Table per type mapping
        modelBuilder.Entity<Activity>()
            .HasDiscriminator<string>("ActivityType")
            .HasValue<ActivityPropertyCreated>("Created")
            .HasValue<ActivityPropertyUpdated>("Updated");

        // ISSUE - relationship to TEAM (Team)
        modelBuilder.Entity<Issue>()
            .HasOne(i => i.Team)
            .WithMany(t => t.Issues)
            .HasForeignKey(i => i.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        // Team - relationship to User
        modelBuilder.Entity<Team>()
        .HasMany(t => t.Users)
        .WithMany(u => u.Teams)
        .UsingEntity<Dictionary<string, object>>(
        "team_user",
        j => j.HasOne<User>()
              .WithMany()
              .HasForeignKey("UserId")
              .OnDelete(DeleteBehavior.Cascade),
        j => j.HasOne<Team>()
              .WithMany()
              .HasForeignKey("TeamId")
              .OnDelete(DeleteBehavior.Cascade),
        j =>
        {
            j.HasKey("TeamId", "UserId");
            j.ToTable("team_user");
        }
    );

        // Comment -- Attachment
        modelBuilder.Entity <Comment>()
            .HasMany(c => c.Attachments)
            .WithOne(a => a.Comment)
            .HasForeignKey(a => a.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        //  Issue <-> Labels

        modelBuilder.Entity<Issue>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity
                .HasMany(i => i.Labels)
                .WithMany(m => m.Issues)
                .UsingEntity<Dictionary<string, object>>(
                    "IssueLabels",
                    j => j
                        .HasOne<MasterdataValue>()
                        .WithMany()
                        .HasForeignKey("MasterdataValueId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<Issue>()
                        .WithMany()
                        .HasForeignKey("IssueId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("IssueId", "MasterdataValueId");
                        j.ToTable("issue_labels");
                    }
                );
        });

        // MasterdataValue def

        modelBuilder.Entity<MasterdataValue>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Code)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(m => m.Value)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(m => m.Type)
                .IsRequired();

            entity.Property(m => m.Order)
                .HasDefaultValue(0);

            entity.Property(m => m.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(m => new { m.Type, m.Code })
                .IsUnique();

            entity.ToTable("masterdata_values");
        });

        // Notification
        modelBuilder.Entity<Notification>()
            .Property(x => x.Properties)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null)!
            );

        // Seed roles
        modelBuilder.Entity<Role>().HasData(
        new Role { Id = 1, Name = "ROLE_USER" },
        new Role { Id = 2, Name = "ROLE_ADMIN" }
       );

        // Seed System user
        modelBuilder.Entity<User>().HasData(
            new User { Id= -1, FirstName = "System User", LastName = "System User", Email = "system.user@tasksystem.com", Password = "Password", Salt = Encoding.UTF8.GetBytes("W W=èÔUÌ-§ÂNï^ÎX"), Disabled = true });

        // Seed dummyProjectId
        modelBuilder.Entity<Project>().HasData(
            new Project { Id = -1, ShortName = "Dummy", Description = "Predefined dummy project",
                CreatedAt = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc)
            });

    }

    // Automatically set CreatedAt for entities implementing IAutomaticDates
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;


        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAutomaticDates &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var auditable = (IAutomaticDates)entry.Entity;

            if (entry.State == EntityState.Added)
                auditable.CreatedAt = nowUtc;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
