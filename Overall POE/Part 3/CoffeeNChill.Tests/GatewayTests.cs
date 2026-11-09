using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Middleware;
using CoffeeNChill.Gateway.Models;
using CoffeeNChill.Gateway.Services;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace CoffeeNChill.Tests
{
    public class GatewayTests
    {
        private static TokenService CreateTokenService()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "unit-test-key-that-is-longer-than-32-characters",
                    ["Jwt:Issuer"] = "CoffeeNChill.Gateway",
                    ["Jwt:Audience"] = "CoffeeNChill.Clients",
                    ["Jwt:ExpiryMinutes"] = "60"
                })
                .Build();

            return new TokenService(configuration);
        }

        private static bool IsValid(object dto)
        {
            return Validator.TryValidateObject(dto, new ValidationContext(dto), null, true);
        }

        [Fact]
        public void Password_Is_Hashed_And_Can_Be_Verified()
        {
            string hash = BCrypt.Net.BCrypt.HashPassword("Barista@2026");

            Assert.NotEqual("Barista@2026", hash);
            Assert.True(UserService.PasswordMatches("Barista@2026", hash));
            Assert.False(UserService.PasswordMatches("WrongPassword", hash));
        }

        [Fact]
        public void Token_Has_Name_Email_And_Role_Claims()
        {
            var user = new UserEntity
            {
                PartitionKey = "Staff",
                RowKey = "barista@coffeenchill.ac.za",
                FullName = "Sipho Dlamini"
            };

            var (token, expires) = CreateTokenService().CreateToken(user);

            // Turning off the mapping so the claim names can be read as they are in the token
            var handler = new JwtSecurityTokenHandler { InboundClaimTypeMap = new Dictionary<string, string>() };
            var jwt = handler.ReadJwtToken(token);

            Assert.Equal("Sipho Dlamini", jwt.Claims.First(c => c.Type == "unique_name" || c.Type == ClaimTypes.Name).Value);
            Assert.Equal("barista@coffeenchill.ac.za", jwt.Claims.First(c => c.Type == "email" || c.Type == ClaimTypes.Email).Value);
            Assert.Equal("Staff", jwt.Claims.First(c => c.Type == "role" || c.Type == ClaimTypes.Role).Value);
            Assert.Equal("CoffeeNChill.Gateway", jwt.Issuer);
            Assert.True(expires > DateTime.UtcNow);
        }

        [Fact]
        public void Register_Dto_Accepts_Valid_User()
        {
            var dto = new RegisterRequestDto
            {
                FullName = "Sipho Dlamini",
                Email = "barista@coffeenchill.ac.za",
                Password = "Barista@2026",
                Role = "Staff"
            };

            Assert.True(IsValid(dto));
        }

        [Theory]
        [InlineData("not-an-email", "Barista@2026", "Staff")]
        [InlineData("barista@coffeenchill.ac.za", "short", "Staff")]
        [InlineData("barista@coffeenchill.ac.za", "Barista@2026", "Manager")]
        public void Register_Dto_Rejects_Bad_Values(string email, string password, string role)
        {
            var dto = new RegisterRequestDto
            {
                FullName = "Sipho Dlamini",
                Email = email,
                Password = password,
                Role = role
            };

            Assert.False(IsValid(dto));
        }

        [Fact]
        public void Update_Status_Dto_Rejects_Unknown_Status()
        {
            var dto = new UpdateOrderStatusDto { OrderId = "ORD-2026-8801", Status = "Lost" };

            Assert.False(IsValid(dto));
        }

        [Fact]
        public void Menu_Model_Is_Mapped_To_Dto()
        {
            var item = new MenuItem { PartitionKey = "coffee", RowKey = "1", Name = "Cappuccino", Price = 35 };

            var dto = MenuItemResponseDto.FromModel(item);

            Assert.Equal("coffee", dto.Category);
            Assert.Equal("1", dto.Id);
            Assert.Equal("Cappuccino", dto.Name);
        }

        [Theory]
        [InlineData(404, 404)]
        [InlineData(409, 409)]
        [InlineData(401, 502)]
        [InlineData(500, 502)]
        public void Middleware_Maps_Function_Errors(int functionStatus, int expectedStatus)
        {
            var (statusCode, _) = ExceptionMiddleware.GetError(new FunctionsException(functionStatus, "error"));

            Assert.Equal(expectedStatus, statusCode);
        }

        [Fact]
        public void Middleware_Hides_Unknown_Errors()
        {
            var (statusCode, message) = ExceptionMiddleware.GetError(new Exception("secret details"));

            Assert.Equal(500, statusCode);
            Assert.DoesNotContain("secret", message);
        }
    }
}
