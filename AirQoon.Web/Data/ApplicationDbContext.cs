using AirQoon.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AirQoon.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ConversationContextEntity> ConversationContexts => Set<ConversationContextEntity>();
    public DbSet<AdminSetting> AdminSettings => Set<AdminSetting>();
    public DbSet<DomainApiKey> DomainApiKeys => Set<DomainApiKey>();
    public DbSet<DomainAppearance> DomainAppearances => Set<DomainAppearance>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SavedAirQualityQuery> SavedAirQualityQueries => Set<SavedAirQualityQuery>();
    public DbSet<SavedReport> SavedReports => Set<SavedReport>();
    public DbSet<DomainTenantMapping> DomainTenantMappings => Set<DomainTenantMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(x => x.SessionId);
            entity.Property(x => x.SessionId).HasMaxLength(255);
            entity.Property(x => x.Domain).HasMaxLength(255);
            entity.Property(x => x.TenantSlug).HasMaxLength(255);
            entity.Property(x => x.UserAgent).HasMaxLength(500);
            entity.Property(x => x.IpAddress).HasMaxLength(45);

            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.Domain);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SessionId).HasMaxLength(255);
            entity.Property(x => x.IntentType).HasMaxLength(50);
            entity.Property(x => x.Content).HasColumnType("text");
            entity.Property(x => x.ErrorMessage).HasColumnType("text");
            entity.Property(x => x.ParametersJson).HasColumnType("jsonb");
            entity.Property(x => x.ResponseDataJson).HasColumnType("jsonb");

            entity.HasOne(x => x.Session)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => x.Timestamp);
        });

        modelBuilder.Entity<ConversationContextEntity>(entity =>
        {
            entity.HasKey(x => x.SessionId);
            entity.Property(x => x.SessionId).HasMaxLength(255);
            entity.Property(x => x.CurrentIntent).HasMaxLength(50);
            entity.Property(x => x.CollectedParametersJson).HasColumnType("jsonb");
            entity.Property(x => x.Domain).HasMaxLength(255);
            entity.Property(x => x.TenantSlug).HasMaxLength(255);
            entity.Property(x => x.Pollutant).HasMaxLength(50);
            entity.Property(x => x.Month1).HasMaxLength(10);
            entity.Property(x => x.Month2).HasMaxLength(10);

            entity.HasOne(x => x.Session)
                .WithOne(x => x.ConversationContext)
                .HasForeignKey<ConversationContextEntity>(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.LastActivity);
        });

        modelBuilder.Entity<AdminSetting>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.LlmProvider).HasMaxLength(50);
            entity.Property(x => x.ModelName).HasMaxLength(100);
            entity.Property(x => x.ApiKey).HasColumnType("text");
            entity.Property(x => x.OllamaBaseUrl).HasMaxLength(255);
            entity.Property(x => x.SystemPrompt).HasColumnType("text");
            entity.Property(x => x.ApiBaseUrl).HasMaxLength(255);
        });

        modelBuilder.Entity<DomainApiKey>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Domain).HasMaxLength(255);
            entity.Property(x => x.ApiKey).HasMaxLength(255);

            entity.HasIndex(x => x.Domain).IsUnique();
            entity.HasIndex(x => x.ApiKey).IsUnique();
        });

        modelBuilder.Entity<DomainAppearance>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Domain).HasMaxLength(255);
            entity.Property(x => x.ChatbotName).HasMaxLength(255);
            entity.Property(x => x.ChatbotLogoUrl).HasColumnType("text");
            entity.Property(x => x.PrimaryColor).HasMaxLength(7);
            entity.Property(x => x.SecondaryColor).HasMaxLength(7);
            entity.Property(x => x.WelcomeMessage).HasColumnType("text");
            entity.Property(x => x.QuickRepliesJson).HasColumnType("jsonb");
            entity.Property(x => x.GreetingResponse).HasColumnType("text");
            entity.Property(x => x.ThanksResponse).HasColumnType("text");

            entity.HasIndex(x => x.Domain).IsUnique();
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(100);
            entity.Property(x => x.PasswordHash).HasMaxLength(255);
            entity.Property(x => x.Email).HasMaxLength(255);
            entity.Property(x => x.Role).HasMaxLength(50);

            entity.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(100);
            entity.Property(x => x.Details).HasColumnType("text");
            entity.Property(x => x.IpAddress).HasMaxLength(45);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.Timestamp);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Action);
        });

        modelBuilder.Entity<SavedAirQualityQuery>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SessionId).HasMaxLength(255);
            entity.Property(x => x.QueryType).HasMaxLength(50);
            entity.Property(x => x.Location).HasMaxLength(255);
            entity.Property(x => x.Pollutant).HasMaxLength(50);
            entity.Property(x => x.ParametersJson).HasColumnType("jsonb");
            entity.Property(x => x.ResultSummary).HasColumnType("text");

            entity.HasOne(x => x.Session)
                .WithMany()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<SavedReport>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ReportName).HasMaxLength(255);
            entity.Property(x => x.ReportType).HasMaxLength(50);
            entity.Property(x => x.TenantSlug).HasMaxLength(255);
            entity.Property(x => x.ReportDataJson).HasColumnType("jsonb");
            entity.Property(x => x.FilePath).HasColumnType("text");

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.TenantSlug);
        });

        modelBuilder.Entity<DomainTenantMapping>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Domain).HasMaxLength(255);
            entity.Property(x => x.TenantSlug).HasMaxLength(255);

            entity.HasIndex(x => x.Domain).IsUnique();
            entity.HasIndex(x => x.TenantSlug);
        });
    }
}
