using Mascoteach.Data.Interfaces;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public class AuthenticatedAccountValidator : IAuthenticatedAccountValidator
{
    private readonly IUserRepository _userRepository;

    public AuthenticatedAccountValidator(IUserRepository userRepository) =>
        _userRepository = userRepository;

    public async Task<bool> IsAllowedAsync(int userId, string role)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(role))
            return false;

        var user = await _userRepository.GetByIdIncludingDeletedAsync(userId);
        return user != null
            && !user.IsDeleted
            && string.Equals(user.Role, role, StringComparison.OrdinalIgnoreCase);
    }
}
