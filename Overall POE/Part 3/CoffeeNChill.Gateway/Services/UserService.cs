using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Gateway.Models;

namespace CoffeeNChill.Gateway.Services
{
    // Reads and writes the Users table directly with the Azure Tables SDK.
    public class UserService
    {
        public const string TableName = "Users";

        private readonly TableClient _table;

        public UserService(IConfiguration configuration)
        {
            _table = new TableClient(configuration["AzureWebJobsStorage"], TableName);
        }

        public async Task CreateTableAsync()
        {
            await _table.CreateIfNotExistsAsync();
        }

        // The email is the RowKey and the role is the PartitionKey, so a login
        // only knows the RowKey and has to look through every role.
        public async Task<UserEntity?> FindByEmailAsync(string email)
        {
            string cleanEmail = CleanEmail(email);

            await foreach (var user in _table.QueryAsync<UserEntity>(x => x.RowKey == cleanEmail))
            {
                return user;
            }

            return null;
        }

        // Returns null if the email is already registered.
        public async Task<UserEntity?> CreateUserAsync(string fullName, string email, string password, string role)
        {
            if (await FindByEmailAsync(email) != null)
            {
                return null;
            }

            var user = new UserEntity
            {
                PartitionKey = role,
                RowKey = CleanEmail(email),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = fullName.Trim(),
                CreatedDate = DateTime.UtcNow
            };

            try
            {
                await _table.AddEntityAsync(user);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return null;
            }

            return user;
        }

        public static bool PasswordMatches(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        private static string CleanEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }
}
