using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByEmailIncludingDeletedAsync(string email);
        Task<User?> GetByGoogleSubjectAsync(string googleSubject);
        Task<User?> GetByGoogleSubjectIncludingDeletedAsync(string googleSubject);
        Task<User?> GetByResetTokenHashAsync(string resetTokenHash);
        Task<User?> GetByEmailVerificationTokenHashAsync(string emailVerificationTokenHash);
        Task<User?> GetByIdIncludingDeletedAsync(int id);
        Task<User?> GetAccountDeletionGraphAsync(int id);
        Task<bool> TransferOwnedClassesBeforeDeactivationAsync(int teacherId);
        void HardDeleteAccountGraph(User user);
    }
}
