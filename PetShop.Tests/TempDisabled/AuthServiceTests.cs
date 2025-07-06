using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Dierenwinkel.Services.DTOs;
using Dierenwinkel.Services.Models;
using Dierenwinkel.Services.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace PetShop.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);
            
            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object,
                Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                null, null, null, null);
            
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<AuthService>>();

            // Setup configuration
            _configurationMock.Setup(x => x["Jwt:Key"]).Returns("ThisIsASecretKeyForJWTTokenGenerationThatShouldBeLongEnough");
            _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("PetShopAPI");
            _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("PetShopAPI");
            _configurationMock.Setup(x => x["Jwt:ExpiryMinutes"]).Returns("60");

            _authService = new AuthService(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _configurationMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_ShouldCreateUser()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                Password = "Password123!",
                FirstName = "Test",
                LastName = "User"
            };

            var user = new ApplicationUser
            {
                Id = "user1",
                Email = registerDto.Email,
                UserName = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName
            };

            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("User registered successfully", result.Message);
            Assert.NotNull(result.Token);
            Assert.NotEmpty(result.Token);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ShouldReturnError()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "existing@example.com",
                Password = "Password123!",
                FirstName = "Test",
                LastName = "User"
            };

            var errors = new List<IdentityError>
            {
                new IdentityError { Code = "DuplicateEmail", Description = "Email already exists" }
            };

            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Failed(errors.ToArray()));

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Email already exists", result.Message);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var user = new ApplicationUser
            {
                Id = "user1",
                Email = loginDto.Email,
                UserName = loginDto.Email,
                FirstName = "Test",
                LastName = "User"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, loginDto.Password, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Customer" });

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Login successful", result.Message);
            Assert.NotNull(result.Token);
            Assert.NotEmpty(result.Token);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidEmail_ShouldReturnError()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "Password123!"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Invalid email or password", result.Message);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ShouldReturnError()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            var user = new ApplicationUser
            {
                Id = "user1",
                Email = loginDto.Email,
                UserName = loginDto.Email,
                FirstName = "Test",
                LastName = "User"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, loginDto.Password, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Invalid email or password", result.Message);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
        {
            // Arrange
            var userId = "user1";
            var user = new ApplicationUser
            {
                Id = userId,
                Email = "test@example.com",
                UserName = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal("test@example.com", result.Email);
            Assert.Equal("Test", result.FirstName);
            Assert.Equal("User", result.LastName);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var userId = "nonexistent";

            _userManagerMock.Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _authService.GetUserByIdAsync(userId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByEmailAsync_WithValidEmail_ShouldReturnUser()
        {
            // Arrange
            var email = "test@example.com";
            var user = new ApplicationUser
            {
                Id = "user1",
                Email = email,
                UserName = email,
                FirstName = "Test",
                LastName = "User"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.GetUserByEmailAsync(email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("user1", result.Id);
            Assert.Equal(email, result.Email);
            Assert.Equal("Test", result.FirstName);
            Assert.Equal("User", result.LastName);
        }

        [Fact]
        public async Task GetUserByEmailAsync_WithInvalidEmail_ShouldReturnNull()
        {
            // Arrange
            var email = "nonexistent@example.com";

            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _authService.GetUserByEmailAsync(email);

            // Assert
            Assert.Null(result);
        }
    }
}
