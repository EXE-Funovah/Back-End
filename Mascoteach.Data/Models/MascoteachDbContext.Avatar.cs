using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Models;

// Cấu hình cột avatar_url qua hook OnModelCreatingPartial (scaffold gọi sẵn ở
// cuối OnModelCreating) — không đụng file DbContext scaffold.
public partial class MascoteachDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("avatar_url");
        });
    }
}
