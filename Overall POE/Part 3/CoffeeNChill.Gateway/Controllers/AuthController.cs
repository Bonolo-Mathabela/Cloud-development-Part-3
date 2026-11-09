using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers
{
    /// <summary>Register users and log in.</summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _users;
        private readonly TokenService _tokens;

        public AuthController(UserService users, TokenService tokens)
        {
            _users = users;
            _tokens = tokens;
        }

        /// <summary>Registers a new user in the Users table.</summary>
        /// <remarks>
        /// Anyone can register as a Customer. To create an Admin or Staff user,
        /// send the Bearer token of a logged in Admin.
        /// </remarks>
        /// <param name="request">The new user's details.</param>
        /// <response code="201">The user was created.</response>
        /// <response code="400">A field is missing or not valid.</response>
        /// <response code="403">Only an Admin can create Admin or Staff users.</response>
        /// <response code="409">The email is already registered.</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            // Without this check anyone could give themselves the Admin role.
            if (request.Role != "Customer" && !User.IsInRole("Admin"))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponseDto
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Only an Admin can create Admin or Staff users."
                });
            }

            var user = await _users.CreateUserAsync(
                request.FullName, request.Email, request.Password, request.Role);

            if (user == null)
            {
                return Conflict(new ErrorResponseDto
                {
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "This email is already registered."
                });
            }

            return StatusCode(StatusCodes.Status201Created, new UserResponseDto
            {
                FullName = user.FullName,
                Email = user.RowKey,
                Role = user.PartitionKey,
                CreatedDate = user.CreatedDate
            });
        }

        /// <summary>Checks a user's login details and returns a signed JWT.</summary>
        /// <param name="request">The email and password.</param>
        /// <response code="200">Login worked. The response has the JWT.</response>
        /// <response code="400">A field is missing.</response>
        /// <response code="401">The email or password is wrong.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var user = await _users.FindByEmailAsync(request.Email);

            // Same message for both cases so nobody can find out which emails exist.
            if (user == null || !UserService.PasswordMatches(request.Password, user.PasswordHash))
            {
                return Unauthorized(new ErrorResponseDto
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = "Invalid email or password."
                });
            }

            var (token, expires) = _tokens.CreateToken(user);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Expires = expires,
                FullName = user.FullName,
                Email = user.RowKey,
                Role = user.PartitionKey
            });
        }
    }
}
