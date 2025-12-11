using Microsoft.EntityFrameworkCore;
using TrackEvent.WebApi.Domain.Entities;

namespace TrackEvent.WebApi.Infrastructure.Data;

/// <summary>
/// 追蹤事件資料庫上下文
/// </summary>
public class TrackEventDbContext : DbContext
{
    public TrackEventDbContext(DbContextOptions<TrackEventDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserEvent> UserEvents => Set<UserEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserEvent>(entity =>
        {
            entity.ToTable("user_events");

            // 主鍵
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            // 唯一索引
            entity.HasIndex(e => e.EventId)
                .IsUnique();

            // 欄位對應
            entity.Property(e => e.EventId).HasColumnName("event_id").HasMaxLength(64).IsRequired();

            // 身份識別
            entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(64);
            entity.Property(e => e.AnonymousId).HasColumnName("anonymous_id").HasMaxLength(128);
            entity.Property(e => e.ClientId).HasColumnName("client_id").HasMaxLength(128).IsRequired();
            entity.Property(e => e.SessionId).HasColumnName("session_id").HasMaxLength(128).IsRequired();

            // 事件核心資訊
            entity.Property(e => e.EventTime).HasColumnName("event_time").IsRequired();
            entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(32).IsRequired();
            entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(32).IsRequired();

            // 功能與行為資訊
            entity.Property(e => e.FeatureId).HasColumnName("feature_id").HasMaxLength(128).IsRequired();
            entity.Property(e => e.FeatureName).HasColumnName("feature_name").HasMaxLength(256);
            entity.Property(e => e.FeatureType).HasColumnName("feature_type").HasMaxLength(64);
            entity.Property(e => e.Action).HasColumnName("action").HasMaxLength(64);

            // 場景上下文 (Web)
            entity.Property(e => e.PageUrl).HasColumnName("page_url").HasColumnType("text");
            entity.Property(e => e.PageName).HasColumnName("page_name").HasMaxLength(256);
            entity.Property(e => e.PreviousPageUrl).HasColumnName("previous_page_url").HasColumnType("text");
            entity.Property(e => e.PreviousPageName).HasColumnName("previous_page_name").HasMaxLength(256);

            // 場景上下文 (App)
            entity.Property(e => e.ScreenName).HasColumnName("screen_name").HasMaxLength(256);
            entity.Property(e => e.PreviousScreenName).HasColumnName("previous_screen_name").HasMaxLength(256);

            // 環境資訊
            entity.Property(e => e.DeviceType).HasColumnName("device_type").HasMaxLength(32);
            entity.Property(e => e.Os).HasColumnName("os").HasMaxLength(64);
            entity.Property(e => e.OsVersion).HasColumnName("os_version").HasMaxLength(64);
            entity.Property(e => e.Browser).HasColumnName("browser").HasMaxLength(64);
            entity.Property(e => e.BrowserVersion).HasColumnName("browser_version").HasMaxLength(64);
            entity.Property(e => e.AppVersion).HasColumnName("app_version").HasMaxLength(64);
            entity.Property(e => e.BuildNumber).HasColumnName("build_number").HasMaxLength(64);
            entity.Property(e => e.NetworkType).HasColumnName("network_type").HasMaxLength(32);
            entity.Property(e => e.Locale).HasColumnName("locale").HasMaxLength(16);

            // JSONB 欄位
            entity.Property(e => e.Experiments).HasColumnName("experiments").HasColumnType("jsonb");
            entity.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb");

            // 系統欄位
            entity.Property(e => e.ReceivedAt)
                .HasColumnName("received_at")
                .HasDefaultValueSql("NOW()")
                .IsRequired();

            // 索引設計
            entity.HasIndex(e => e.EventTime).HasDatabaseName("idx_user_events_event_time");
            entity.HasIndex(e => new { e.FeatureId, e.EventTime }).HasDatabaseName("idx_user_events_feature_time");
            entity.HasIndex(e => new { e.UserId, e.EventTime }).HasDatabaseName("idx_user_events_user_time");
            entity.HasIndex(e => new { e.ClientId, e.SessionId, e.EventTime }).HasDatabaseName("idx_user_events_client_session_time");
            entity.HasIndex(e => new { e.Source, e.EventTime }).HasDatabaseName("idx_user_events_source_time");
        });
    }
}
