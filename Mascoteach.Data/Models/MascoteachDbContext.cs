using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Models;

public partial class MascoteachDbContext : DbContext
{
    public MascoteachDbContext()
    {
    }

    public MascoteachDbContext(DbContextOptions<MascoteachDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<GameTemplate> GameTemplates { get; set; }

    public virtual DbSet<LiveSession> LiveSessions { get; set; }

    public virtual DbSet<Option> Options { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Quiz> Quizzes { get; set; }

    public virtual DbSet<SessionParticipant> SessionParticipants { get; set; }

    public virtual DbSet<User> Users { get; set; }

    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3213E83F06A6C57F");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FileUrl)
                .IsUnicode(false)
                .HasColumnName("file_url");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("uploaded_at");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Documents)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Documents_Users");
        });

        modelBuilder.Entity<GameTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Game_Tem__3213E83F4C166024");

            entity.ToTable("Game_Templates");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.JsBundleUrl)
                .IsUnicode(false)
                .HasColumnName("js_bundle_url");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.ThumbnailUrl)
                .IsUnicode(false)
                .HasColumnName("thumbnail_url");
        });

        modelBuilder.Entity<LiveSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Live_Ses__3213E83F35A4B00F");

            entity.ToTable("Live_Sessions");

            entity.HasIndex(e => e.GamePin, "UQ__Live_Ses__BBB79854AE64FD58").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.GamePin)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("game_pin");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            entity.Property(e => e.TemplateId).HasColumnName("template_id");

            entity.HasOne(d => d.Quiz).WithMany(p => p.LiveSessions)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LiveSessions_Quizzes");

            entity.HasOne(d => d.Teacher).WithMany(p => p.LiveSessions)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LiveSessions_Users");

            entity.HasOne(d => d.Template).WithMany(p => p.LiveSessions)
                .HasForeignKey(d => d.TemplateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LiveSessions_GameTemplates");
        });

        modelBuilder.Entity<Option>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Options__3213E83F2C2BEC40");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.OptionText).HasColumnName("option_text");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");

            entity.HasOne(d => d.Question).WithMany(p => p.Options)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Options_Questions");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Question__3213E83F0BA585BF");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.QuestionText).HasColumnName("question_text");
            entity.Property(e => e.QuestionType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("MultipleChoice")
                .HasColumnName("question_type");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");

            entity.HasOne(d => d.Quiz).WithMany(p => p.Questions)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Questions_Quizzes");
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Quizzes__3213E83F70093524");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.Document).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Quizzes_Documents");
        });

        modelBuilder.Entity<SessionParticipant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Session___3213E83F15FAEA13");

            entity.ToTable("Session_Participants");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.StudentName)
                .HasMaxLength(255)
                .HasColumnName("student_name");
            entity.Property(e => e.TotalScore)
                .HasDefaultValue(0)
                .HasColumnName("total_score");

            entity.HasOne(d => d.Session).WithMany(p => p.SessionParticipants)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Participants_LiveSessions");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3213E83F8238BA73");

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E61644394921A").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DocumentsProcessed)
                .HasDefaultValue(0)
                .HasColumnName("documents_processed");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .IsUnicode(true)
                .HasColumnName("full_name");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.SubscriptionTier)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("subscription_tier");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
