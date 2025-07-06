using Microsoft.AspNetCore.Mvc;
using Dierenwinkel.Services.DTOs;
using Dierenwinkel.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace PetShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Gebruiker inloggen
        /// </summary>
        /// <param name="loginDto">Login gegevens</param>
        /// <returns>Authenticatie token en gebruikersinformatie</returns>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.LoginAsync(loginDto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized login attempt for email: {Email}", loginDto.Email);
                return Unauthorized(new { message = "Ongeldige email of wachtwoord" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", loginDto.Email);
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het inloggen" });
            }
        }

        /// <summary>
        /// Nieuwe gebruiker registreren
        /// </summary>
        /// <param name="registerDto">Registratie gegevens</param>
        /// <returns>Authenticatie token en gebruikersinformatie</returns>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterAsync(registerDto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Registration failed for email: {Email}", registerDto.Email);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}", registerDto.Email);
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het registreren" });
            }
        }

        /// <summary>
        /// Controleer of een email adres al bestaat
        /// </summary>
        /// <param name="email">Email adres om te controleren</param>
        /// <returns>True als email bestaat, anders false</returns>
        [HttpGet("check-email")]
        public async Task<ActionResult<bool>> CheckEmailExists([Required] string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { message = "Email adres is vereist" });
                }

                var exists = await _authService.UserExistsAsync(email);
                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence: {Email}", email);
                return StatusCode(500, new { message = "Er is een fout opgetreden" });
            }
        }
    }
}
