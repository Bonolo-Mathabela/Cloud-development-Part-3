using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Models;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers
{
    /// <summary>View and manage the canteen menu.</summary>
    [ApiController]
    [Route("api/menu")]
    [Produces("application/json")]
    public class MenuController : ControllerBase
    {
        private readonly FunctionsClient _functions;

        public MenuController(FunctionsClient functions)
        {
            _functions = functions;
        }

        /// <summary>Gets all menu items. No login is needed.</summary>
        /// <response code="200">The list of menu items.</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<MenuItemResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMenu()
        {
            var items = await _functions.GetMenuAsync();

            return Ok(items.Select(MenuItemResponseDto.FromModel));
        }

        /// <summary>Gets the menu items in one category. No login is needed.</summary>
        /// <param name="category">The category, for example coffee.</param>
        /// <response code="200">The list of menu items.</response>
        [HttpGet("category/{category}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<MenuItemResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByCategory(string category)
        {
            var items = await _functions.GetMenuByCategoryAsync(category);

            return Ok(items.Select(MenuItemResponseDto.FromModel));
        }

        /// <summary>Adds a menu item. Admin or Staff only.</summary>
        /// <param name="dto">The new menu item.</param>
        /// <response code="201">The menu item was added.</response>
        /// <response code="400">A field is missing or not valid.</response>
        /// <response code="401">No valid token was sent.</response>
        /// <response code="403">The user is not Admin or Staff.</response>
        /// <response code="409">This category and id already exist.</response>
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(typeof(MenuItemResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateMenuItemDto dto)
        {
            var item = new MenuItem
            {
                PartitionKey = dto.Category,
                RowKey = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                IsAvailable = dto.IsAvailable
            };

            await _functions.CreateMenuItemAsync(item);

            return StatusCode(StatusCodes.Status201Created, MenuItemResponseDto.FromModel(item));
        }

        /// <summary>Updates a menu item, for example its price. Admin or Staff only.</summary>
        /// <param name="category">The category of the item.</param>
        /// <param name="id">The id of the item.</param>
        /// <param name="dto">The new values.</param>
        /// <response code="200">The menu item was updated.</response>
        /// <response code="400">A field is missing or not valid.</response>
        /// <response code="401">No valid token was sent.</response>
        /// <response code="403">The user is not Admin or Staff.</response>
        [HttpPut("{category}/{id}")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(typeof(MenuItemResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(string category, string id, [FromBody] UpdateMenuItemDto dto)
        {
            var item = new MenuItem
            {
                PartitionKey = category,
                RowKey = id,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                IsAvailable = dto.IsAvailable
            };

            await _functions.UpdateMenuItemAsync(category, id, item);

            return Ok(MenuItemResponseDto.FromModel(item));
        }

        /// <summary>Deletes a menu item. Admin or Staff only.</summary>
        /// <param name="category">The category of the item.</param>
        /// <param name="id">The id of the item.</param>
        /// <response code="200">The menu item was deleted.</response>
        /// <response code="401">No valid token was sent.</response>
        /// <response code="403">The user is not Admin or Staff.</response>
        /// <response code="404">The menu item does not exist.</response>
        [HttpDelete("{category}/{id}")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string category, string id)
        {
            await _functions.DeleteMenuItemAsync(category, id);

            return Ok(new MessageResponseDto { Message = "Menu item deleted." });
        }
    }
}
