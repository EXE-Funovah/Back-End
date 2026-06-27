using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IS3Service _s3Service;

        public UserService(IUserRepository userRepository, IMapper mapper, IS3Service s3Service)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _s3Service = s3Service;
        }

        /// <summary>Map User → UserResponse và đổi AvatarUrl (S3 key) thành presigned URL.</summary>
        private async Task<UserResponse?> ToResponseAsync(User? user)
        {
            if (user == null) return null;
            var res = _mapper.Map<UserResponse>(user);
            if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
                res.AvatarUrl = await _s3Service.GeneratePresignedDownloadUrlAsync(user.AvatarUrl);
            return res;
        }

        public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var list = new List<UserResponse>();
            foreach (var u in users)
            {
                var r = await ToResponseAsync(u);
                if (r != null) list.Add(r);
            }
            return list;
        }

        public async Task<UserResponse?> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return await ToResponseAsync(user);
        }

        public async Task<UserResponse?> GetCurrentUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return await ToResponseAsync(user);
        }

        public async Task<UserResponse?> UpdateAvatarAsync(int userId, string? avatarKey)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            user.AvatarUrl = string.IsNullOrWhiteSpace(avatarKey) ? null : avatarKey.Trim();
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            return await ToResponseAsync(user);
        }

        public async Task<bool> UpdateAsync(int id, UserUpdateRequest request)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.FullName = request.FullName;
            user.Email = request.Email;

            _userRepository.Update(user);
            return await _userRepository.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _userRepository.GetAccountDeletionGraphAsync(id);
            if (user == null) return false;

            var s3KeysToDelete = user.Documents
                .Select(document => document.FileUrl)
                .Append(user.AvatarUrl)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key!.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            await using var transaction = await _userRepository.BeginTransactionAsync();

            try
            {
                _userRepository.HardDeleteAccountGraph(user);
                var changed = await _userRepository.SaveChangesAsync() > 0;
                await transaction.CommitAsync();

                try
                {
                    await _s3Service.DeleteObjectsAsync(s3KeysToDelete);
                }
                catch
                {
                    // Best-effort cleanup: the account is already deleted from the database.
                }

                return changed;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<UserResponse?> ToggleDeleteAsync(int id)
        {
            var user = await _userRepository.GetByIdIncludingDeletedAsync(id);
            if (user == null) return null;

            user.IsDeleted = !user.IsDeleted;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            return _mapper.Map<UserResponse>(user);
        }

        // dùng nội bộ bởi AuthService
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email);
        }

        public async Task<bool> RegisterUserAsync(User user)
        {
            await _userRepository.AddAsync(user);
            return await _userRepository.SaveChangesAsync() > 0;
        }
    }
}
