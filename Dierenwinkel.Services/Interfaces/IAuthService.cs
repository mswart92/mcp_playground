using Dierenwinkel.Services.DTOs;

namespace Dierenwinkel.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<bool> UserExistsAsync(string email);
        string GenerateJwtToken(string userId, string email, List<string> roles);
    }
}
