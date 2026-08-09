namespace Mascoteach.Service.Interfaces;

public interface IAuthenticatedAccountValidator
{
    Task<bool> IsAllowedAsync(int userId, string role);
}
