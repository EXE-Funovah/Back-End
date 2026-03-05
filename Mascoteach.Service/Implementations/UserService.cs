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

        public UserService(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserResponse>>(users);
        }

        public async Task<UserResponse?> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return _mapper.Map<UserResponse>(user);
        }

        public async Task<UserResponse?> GetCurrentUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return _mapper.Map<UserResponse>(user);
        }

        public async Task<bool> UpdateAsync(int id, UserUpdateRequest request)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.Role = request.Role;
            user.SubscriptionTier = request.SubscriptionTier;

            _userRepository.Update(user);
            return await _userRepository.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            _userRepository.Delete(user);
            return await _userRepository.SaveChangesAsync() > 0;
        }

        public async Task<UserResponse?> ToggleDeleteAsync(int id)
        {
            var user = await _userRepository.GetAllIncludingDeletedAsync(id);
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
