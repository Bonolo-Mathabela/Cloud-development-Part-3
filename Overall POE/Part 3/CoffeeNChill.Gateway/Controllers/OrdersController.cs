using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Models;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers
{
    /// <summary>Place orders and follow their status.</summary>
    [ApiController]
    [Route("api/orders")]
    [Produces("application/json")]
    public class OrdersController : ControllerBase
    {
        private readonly FunctionsClient _functions;

        public OrdersController(FunctionsClient functions)
        {
            _functions = functions;
        }

        /// <summary>Puts an order on the queue. No login is needed.</summary>
        /// <param name="dto">The order.</param>
        /// <response code="202">The order was accepted and queued.</response>
        /// <response code="400">A field is missing or not valid.</response>
        /// <response code="503">The queue is not available.</response>
        [HttpPost("queue")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OrderQueuedResponseDto), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> QueueOrder([FromBody] OrderRequestDto dto)
        {
            var order = new Order
            {
                OrderId = dto.OrderId ?? "",
                CustomerName = dto.CustomerName,
                SelectedItemSKUs = dto.SelectedItemSKUs,
                TotalPrice = dto.TotalPrice,
                OrderTimestamp = dto.OrderTimestamp ?? default
            };

            var result = await _functions.QueueOrderAsync(order);

            return StatusCode(StatusCodes.Status202Accepted, result);
        }

        /// <summary>Gets one order and its status, so a student can follow it. No login is needed.</summary>
        /// <param name="orderId">The id of the order.</param>
        /// <response code="200">The order.</response>
        /// <response code="404">The order has not been processed yet or does not exist.</response>
        [HttpGet("{orderId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OrderStatusResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrder(string orderId)
        {
            return Ok(await _functions.GetOrderAsync(orderId));
        }

        /// <summary>Gets all orders. Admin or Staff only.</summary>
        /// <response code="200">The list of orders.</response>
        /// <response code="401">No valid token was sent.</response>
        /// <response code="403">The user is not Admin or Staff.</response>
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(typeof(List<OrderStatusResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetOrders()
        {
            return Ok(await _functions.GetOrdersAsync());
        }

        /// <summary>Changes the status of an order. Admin or Staff only.</summary>
        /// <param name="dto">The order id and its new status.</param>
        /// <response code="200">The status was updated.</response>
        /// <response code="400">A field is missing or not valid.</response>
        /// <response code="401">No valid token was sent.</response>
        /// <response code="403">The user is not Admin or Staff.</response>
        /// <response code="404">The order does not exist.</response>
        [HttpPut("status")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateOrderStatusDto dto)
        {
            string message = await _functions.UpdateOrderStatusAsync(dto);

            return Ok(new MessageResponseDto { Message = message });
        }
    }
}
