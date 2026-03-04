using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Service.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var users = await _userRepository.GetAllAsync();
            return users.FirstOrDefault(x => x.Email == email);
        }

        public async Task<bool> RegisterUserAsync(User user)
        {
            await _userRepository.AddAsync(user);
            var result = await _userRepository.SaveChangesAsync();
            return result > 0;
        }
    }
}
