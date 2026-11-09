using CoffeeNChill.Gateway.Models;
using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs
{
    /// <summary>A new menu item.</summary>
    public class CreateMenuItemDto
    {
        /// <summary>The menu category. Saved as the PartitionKey.</summary>
        /// <example>coffee</example>
        [Required(ErrorMessage = "Category is required.")]
        [RegularExpression(@"^[^/\\#?]+$", ErrorMessage = "Category cannot contain / \\ # or ?.")]
        public string Category { get; set; } = "";

        /// <summary>The id of the item in its category. Saved as the RowKey.</summary>
        /// <example>1</example>
        [Required(ErrorMessage = "Id is required.")]
        [RegularExpression(@"^[^/\\#?]+$", ErrorMessage = "Id cannot contain / \\ # or ?.")]
        public string Id { get; set; } = "";

        /// <example>Cappuccino</example>
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = "";

        /// <example>Espresso with steamed milk</example>
        public string Description { get; set; } = "";

        /// <example>35.00</example>
        [Range(0.01, 100000, ErrorMessage = "Price must be more than 0.")]
        public double Price { get; set; }

        /// <example>true</example>
        public bool IsAvailable { get; set; }
    }

    /// <summary>New values for an existing menu item.</summary>
    public class UpdateMenuItemDto
    {
        /// <example>Cappuccino</example>
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = "";

        /// <example>Espresso with steamed milk</example>
        public string Description { get; set; } = "";

        /// <example>38.50</example>
        [Range(0.01, 100000, ErrorMessage = "Price must be more than 0.")]
        public double Price { get; set; }

        /// <example>true</example>
        public bool IsAvailable { get; set; }
    }

    /// <summary>A menu item returned to the client.</summary>
    public class MenuItemResponseDto
    {
        /// <example>coffee</example>
        public string Category { get; set; } = "";

        /// <example>1</example>
        public string Id { get; set; } = "";

        /// <example>Cappuccino</example>
        public string Name { get; set; } = "";

        /// <example>Espresso with steamed milk</example>
        public string Description { get; set; } = "";

        /// <example>35.00</example>
        public double Price { get; set; }

        /// <example>true</example>
        public bool IsAvailable { get; set; }

        // Changes the storage model into the DTO the client sees.
        public static MenuItemResponseDto FromModel(MenuItem item)
        {
            return new MenuItemResponseDto
            {
                Category = item.PartitionKey,
                Id = item.RowKey,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                IsAvailable = item.IsAvailable
            };
        }
    }
}
