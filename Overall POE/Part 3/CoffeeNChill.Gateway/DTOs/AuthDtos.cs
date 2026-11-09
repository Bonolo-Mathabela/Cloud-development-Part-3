using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs
{
    /// <summary>Details needed to create a new user.</summary>
    public class RegisterRequestDto
    {
        /// <summary>The user's full name.</summary>
        /// <example>Sipho Dlamini</example>
        [Required(ErrorMessage = "FullName is required.")]
        [StringLength(100, ErrorMessage = "FullName cannot be longer than 100 characters.")]
        public string FullName { get; set; } = "";

        /// <summary>The user's email. It is saved as the RowKey in the Users table.</summary>
        /// <example>barista@coffeenchill.ac.za</example>
        [Required(ErrorMessage = "Email is required.")]
        [RegularExpression(@"^[^@\s/\\#?]+@[^@\s/\\#?]+\.[^@\s/\\#?]+$", ErrorMessage = "Email is not valid.")]
        [StringLength(254, ErrorMessage = "Email cannot be longer than 254 characters.")]
        public string Email { get; set; } = "";

        /// <summary>The password, between 8 and 72 characters. Only its BCrypt hash is saved.</summary>
        /// <example>Barista@2026</example>
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(72, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 72 characters.")]
        public string Password { get; set; } = "";

        /// <summary>Admin, Staff or Customer. Only an Admin can create Admin or Staff users.</summary>
        /// <example>Customer</example>
        [RegularExpression("^(Admin|Staff|Customer)$", ErrorMessage = "Role must be Admin, Staff or Customer.")]
        public string Role { get; set; } = "Customer";
    }

    /// <summary>Details needed to log in.</summary>
    public class LoginRequestDto
    {
        /// <summary>The email the user registered with.</summary>
        /// <example>barista@coffeenchill.ac.za</example>
        [Required(ErrorMessage = "Email is required.")]
        public string Email { get; set; } = "";

        /// <summary>The user's password.</summary>
        /// <example>Barista@2026</example>
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = "";
    }

    /// <summary>A user without the password hash.</summary>
    public class UserResponseDto
    {
        /// <example>Sipho Dlamini</example>
        public string FullName { get; set; } = "";

        /// <example>barista@coffeenchill.ac.za</example>
        public string Email { get; set; } = "";

        /// <example>Staff</example>
        public string Role { get; set; } = "";

        /// <summary>When the user was created (UTC).</summary>
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>The token returned after a successful login.</summary>
    public class LoginResponseDto
    {
        /// <summary>Signed JWT. Send it as: Authorization: Bearer {token}</summary>
        public string Token { get; set; } = "";

        /// <summary>When the token stops working (UTC).</summary>
        public DateTime Expires { get; set; }

        /// <example>Sipho Dlamini</example>
        public string FullName { get; set; } = "";

        /// <example>barista@coffeenchill.ac.za</example>
        public string Email { get; set; } = "";

        /// <example>Staff</example>
        public string Role { get; set; } = "";
    }
}
