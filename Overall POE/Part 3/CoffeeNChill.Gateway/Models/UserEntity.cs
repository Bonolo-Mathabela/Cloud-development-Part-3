using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Gateway.Models
{
    // One row in the Users table.
    // PartitionKey = Role ("Admin", "Staff" or "Customer")
    // RowKey = the user's email
    public class UserEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string PasswordHash { get; set; } = "";
        public string FullName { get; set; } = "";
        public DateTime CreatedDate { get; set; }
    }
}
