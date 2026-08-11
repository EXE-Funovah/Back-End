using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Models;

public partial class MascoteachDbContext : DbContext
{
    public MascoteachDbContext(DbContextOptions<MascoteachDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminAuditLog> AdminAuditLogs { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<GameTemplate> GameTemplates { get; set; }

    public virtual DbSet<LiveSession> LiveSessions { get; set; }

    public virtual DbSet<Option> Options { get; set; }

    public virtual DbSet<PaymentOrder> PaymentOrders { get; set; }

    public virtual DbSet<PaymentWebhookEvent> PaymentWebhookEvents { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Quiz> Quizzes { get; set; }

    public virtual DbSet<QuizAttempt> QuizAttempts { get; set; }

    public virtual DbSet<SessionAnswer> SessionAnswers { get; set; }

    public virtual DbSet<SessionParticipant> SessionParticipants { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserStat> UserStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.ToTable("Admin_Audit_Logs");

            entity.HasIndex(e => new { e.Action, e.CreatedAt, e.Id }, "IX_AdminAuditLogs_Action_CreatedAt").IsDescending(false, true, true);

            entity.HasIndex(e => new { e.ActorUserId, e.CreatedAt, e.Id }, "IX_AdminAuditLogs_Actor_CreatedAt").IsDescending(false, true, true);

            entity.HasIndex(e => new { e.CreatedAt, e.Id }, "IX_AdminAuditLogs_CreatedAt").IsDescending();

            entity.HasIndex(e => new { e.TargetType, e.TargetId, e.CreatedAt, e.Id }, "IX_AdminAuditLogs_Target_CreatedAt").IsDescending(false, false, true, true);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.ActorEmail)
                .HasMaxLength(255)
                .HasColumnName("actor_email");
            entity.Property(e => e.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(e => e.AfterJson).HasColumnName("after_json");
            entity.Property(e => e.BeforeJson).HasColumnName("before_json");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("ip_address");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.RiskLevel)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("risk_level");
            entity.Property(e => e.TargetId)
                .HasMaxLength(100)
                .HasColumnName("target_id");
            entity.Property(e => e.TargetType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("target_type");
            entity.Property(e => e.UserAgent)
                .HasMaxLength(512)
                .HasColumnName("user_agent");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.AdminAuditLogs)
                .HasForeignKey(d => d.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AdminAuditLogs_Users_Actor");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3213E83F80515809");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");
            entity.Property(e => e.FileUrl)
                .IsUnicode(false)
                .HasColumnName("file_url");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("uploaded_at");

            entity.HasOne(d => d.Owner).WithMany(p => p.Documents)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Documents_Users");
        });

        modelBuilder.Entity<GameTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Game_Tem__3213E83F5C2E8953");

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
            entity.HasKey(e => e.Id).HasName("PK__Live_Ses__3213E83F21E5C4D5");

            entity.ToTable("Live_Sessions");

            entity.HasIndex(e => e.GamePin, "UQ__Live_Ses__BBB79854C8CEC8E0").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Options__3213E83F65D587A0");

            entity.HasIndex(e => new { e.QuestionId, e.IsDeleted }, "IX_Options_Question_Deleted");

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

        modelBuilder.Entity<PaymentOrder>(entity =>
        {
            entity.ToTable("Payment_Orders");

            entity.HasIndex(e => e.UserId, "IX_Payment_Orders_user_id");

            entity.HasIndex(e => e.OrderCode, "UX_Payment_Orders_order_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CheckoutUrl)
                .HasMaxLength(1000)
                .HasColumnName("checkout_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("VND")
                .HasColumnName("currency");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.OrderCode).HasColumnName("order_code");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PaymentLinkId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("payment_link_id");
            entity.Property(e => e.PayosReference)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("payos_reference");
            entity.Property(e => e.PlanCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("plan_code");
            entity.Property(e => e.Provider)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("PayOS")
                .HasColumnName("provider");
            entity.Property(e => e.QrCode).HasColumnName("qr_code");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Pending")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.PaymentOrders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Orders_Users");
        });

        modelBuilder.Entity<PaymentWebhookEvent>(entity =>
        {
            entity.ToTable("Payment_Webhook_Events");

            entity.HasIndex(e => e.OrderCode, "IX_Payment_Webhook_Events_order_code");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsProcessed).HasColumnName("is_processed");
            entity.Property(e => e.OrderCode).HasColumnName("order_code");
            entity.Property(e => e.Payload).HasColumnName("payload");
            entity.Property(e => e.PaymentLinkId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("payment_link_id");
            entity.Property(e => e.ProcessedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("processed_at");
            entity.Property(e => e.ProcessingError).HasColumnName("processing_error");
            entity.Property(e => e.Provider)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("PayOS")
                .HasColumnName("provider");
            entity.Property(e => e.Reference)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("reference");
            entity.Property(e => e.Signature)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("signature");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Question__3213E83F183DEF03");

            entity.HasIndex(e => new { e.QuizId, e.IsDeleted, e.Position }, "IX_Questions_Quiz_Deleted_Position");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Position).HasColumnName("position");
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
            entity.HasKey(e => e.Id).HasName("PK__Quizzes__3213E83FB6D49418");

            entity.HasIndex(e => new { e.DocumentId, e.ActivityType, e.IsDeleted, e.CreatedAt }, "IX_Quizzes_Document_Activity_Deleted_Created").IsDescending(false, false, false, true);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActivityType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Quiz")
                .HasColumnName("activity_type");
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

        modelBuilder.Entity<QuizAttempt>(entity =>
        {
            entity.ToTable("Quiz_Attempts");

            entity.HasIndex(e => e.QuizId, "IX_QuizAttempts_quiz_id");

            entity.HasIndex(e => new { e.UserId, e.CompletedAt }, "IX_QuizAttempts_user_completed");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompletedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("completed_at");
            entity.Property(e => e.CorrectCount).HasColumnName("correct_count");
            entity.Property(e => e.DurationSeconds).HasColumnName("duration_seconds");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.TotalQuestions).HasColumnName("total_questions");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.XpEarned).HasColumnName("xp_earned");

            entity.HasOne(d => d.Quiz).WithMany(p => p.QuizAttempts)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizAttempts_Quizzes");

            entity.HasOne(d => d.User).WithMany(p => p.QuizAttempts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizAttempts_Users");
        });

        modelBuilder.Entity<SessionAnswer>(entity =>
        {
            entity.ToTable("Session_Answers");

            entity.HasIndex(e => new { e.SessionId, e.AnsweredAt }, "IX_SessionAnswers_Session_AnsweredAt");

            entity.HasIndex(e => new { e.SessionId, e.ParticipantId, e.QuestionId }, "UQ_SessionAnswers_Participant_Question").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AnsweredAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("answered_at");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.ParticipantId).HasColumnName("participant_id");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.ScoreAwarded).HasColumnName("score_awarded");
            entity.Property(e => e.SelectedOptionId).HasColumnName("selected_option_id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");

            entity.HasOne(d => d.Participant).WithMany(p => p.SessionAnswers)
                .HasForeignKey(d => d.ParticipantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SessionAnswers_Participants");

            entity.HasOne(d => d.Question).WithMany(p => p.SessionAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SessionAnswers_Questions");

            entity.HasOne(d => d.SelectedOption).WithMany(p => p.SessionAnswers)
                .HasForeignKey(d => d.SelectedOptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SessionAnswers_Options");

            entity.HasOne(d => d.Session).WithMany(p => p.SessionAnswers)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SessionAnswers_LiveSessions");
        });

        modelBuilder.Entity<SessionParticipant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Session___3213E83F637607BF");

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
            entity.HasKey(e => e.Id).HasName("PK__Users__3213E83F8F516609");

            entity.HasIndex(e => e.EmailVerificationTokenHash, "IX_Users_email_verification_token_hash").HasFilter("([email_verification_token_hash] IS NOT NULL)");

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E616426BBBC00").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Authenticator)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Local")
                .HasColumnName("authenticator");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("avatar_url");
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
            entity.Property(e => e.EmailVerificationTokenExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("email_verification_token_expires_at");
            entity.Property(e => e.EmailVerificationTokenHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email_verification_token_hash");
            entity.Property(e => e.EmailVerified).HasColumnName("email_verified");
            entity.Property(e => e.EmailVerifiedAt)
                .HasColumnType("datetime")
                .HasColumnName("email_verified_at");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.GoogleSubject)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("google_subject");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.PremiumExpiresAt).HasColumnName("premium_expires_at");
            entity.Property(e => e.ResetTokenExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("reset_token_expires_at");
            entity.Property(e => e.ResetTokenHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("reset_token_hash");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.SubscriptionTier)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("subscription_tier");
        });

        modelBuilder.Entity<UserStat>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("User_Stats");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.CurrentStreak).HasColumnName("current_streak");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.LastActiveDate).HasColumnName("last_active_date");
            entity.Property(e => e.LongestStreak).HasColumnName("longest_streak");
            entity.Property(e => e.TotalCorrectAnswers).HasColumnName("total_correct_answers");
            entity.Property(e => e.TotalLearningSeconds).HasColumnName("total_learning_seconds");
            entity.Property(e => e.TotalQuestionsAnswered).HasColumnName("total_questions_answered");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.Xp).HasColumnName("xp");

            entity.HasOne(d => d.User).WithOne(p => p.UserStat)
                .HasForeignKey<UserStat>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserStats_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
