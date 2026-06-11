using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Models;

/// <summary>
/// Partial extension cho gamification (User_Stats, Quiz_Attempts).
/// Tách riêng để không đụng file DbContext scaffold từ DB —
/// khi re-scaffold, file này vẫn giữ nguyên.
/// </summary>
public partial class MascoteachDbContext
{
    public virtual DbSet<UserStat> UserStats { get; set; }

    public virtual DbSet<QuizAttempt> QuizAttempts { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserStat>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK_User_Stats");

            entity.ToTable("User_Stats");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.Xp).HasColumnName("xp");
            entity.Property(e => e.CurrentStreak).HasColumnName("current_streak");
            entity.Property(e => e.LongestStreak).HasColumnName("longest_streak");
            entity.Property(e => e.LastActiveDate).HasColumnName("last_active_date");
            entity.Property(e => e.TotalLearningSeconds).HasColumnName("total_learning_seconds");
            entity.Property(e => e.TotalCorrectAnswers).HasColumnName("total_correct_answers");
            entity.Property(e => e.TotalQuestionsAnswered).HasColumnName("total_questions_answered");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User).WithOne()
                .HasForeignKey<UserStat>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UserStats_Users");
        });

        modelBuilder.Entity<QuizAttempt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Quiz_Attempts");

            entity.ToTable("Quiz_Attempts");

            entity.HasIndex(
                e => new { e.UserId, e.CompletedAt },
                "IX_QuizAttempts_user_completed");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.CorrectCount).HasColumnName("correct_count");
            entity.Property(e => e.TotalQuestions).HasColumnName("total_questions");
            entity.Property(e => e.DurationSeconds).HasColumnName("duration_seconds");
            entity.Property(e => e.XpEarned).HasColumnName("xp_earned");
            entity.Property(e => e.CompletedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("completed_at");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizAttempts_Users");

            entity.HasOne(d => d.Quiz).WithMany()
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizAttempts_Quizzes");
        });
    }
}
