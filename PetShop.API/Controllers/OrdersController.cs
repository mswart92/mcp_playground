using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dierenwinkel.Services.DTOs;
using Dierenwinkel.Services.Interfaces;
using System.Security.Claims;

namespace PetShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        /// <summary>
        /// Plaats een nieuwe bestelling
        /// </summary>
        /// <param name="createOrderDto">Bestelling gegevens</param>
        /// <returns>Nieuwe bestelling</returns>
        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sessionId = createOrderDto.SessionId ?? 
                               Request.Headers["X-Session-Id"].FirstOrDefault() ?? 
                               Guid.NewGuid().ToString();

                var order = await _orderService.CreateOrderAsync(userId, sessionId, createOrderDto);
                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating order");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het plaatsen van de bestelling" });
            }
        }

        /// <summary>
        /// Haal een specifieke bestelling op
        /// </summary>
        /// <param name="id">Bestelling ID</param>
        /// <returns>Bestelling details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrder(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                // Admin kan alle bestellingen bekijken, andere gebruikers alleen hun eigen bestellingen
                var order = await _orderService.GetOrderByIdAsync(id, isAdmin ? null : userId);
                
                if (order == null)
                {
                    return NotFound(new { message = "Bestelling niet gevonden" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order with ID: {OrderId}", id);
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het ophalen van de bestelling" });
            }
        }

        /// <summary>
        /// Haal alle bestellingen op (met paginering)
        /// </summary>
        /// <param name="pageNumber">Pagina nummer</param>
        /// <param name="pageSize">Aantal items per pagina</param>
        /// <returns>Gepagineerde lijst van bestellingen</returns>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<PagedResultDto<OrderSummaryDto>>> GetOrders(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                // Admin kan alle bestellingen bekijken, andere gebruikers alleen hun eigen bestellingen
                var orders = await _orderService.GetOrdersAsync(isAdmin ? null : userId, pageNumber, pageSize);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het ophalen van bestellingen" });
            }
        }

        /// <summary>
        /// Haal alle bestellingen van een specifieke gebruiker op
        /// </summary>
        /// <returns>Lijst van bestellingen van de gebruiker</returns>
        [HttpGet("my-orders")]
        [Authorize]
        public async Task<ActionResult<List<OrderDto>>> GetMyOrders()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Gebruiker niet gevonden" });
                }

                var orders = await _orderService.GetOrdersByUserAsync(userId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user orders");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het ophalen van uw bestellingen" });
            }
        }

        /// <summary>
        /// Haal top 10 meest bestelde producten op
        /// </summary>
        /// <param name="count">Aantal producten om op te halen (standaard 10)</param>
        /// <returns>Lijst van top producten</returns>
        [HttpGet("top-products")]
        public async Task<ActionResult<List<TopProductDto>>> GetTopProducts([FromQuery] int count = 10)
        {
            try
            {
                var topProducts = await _orderService.GetTopProductsAsync(count);
                return Ok(topProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top products");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het ophalen van top producten" });
            }
        }

        /// <summary>
        /// Update bestelling status (alleen voor Admin)
        /// </summary>
        /// <param name="id">Bestelling ID</param>
        /// <param name="status">Nieuwe status</param>
        /// <returns>Geen content</returns>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateOrderStatus(int id, [FromBody] string status)
        {
            try
            {
                var success = await _orderService.UpdateOrderStatusAsync(id, status);
                if (!success)
                {
                    return NotFound(new { message = "Bestelling niet gevonden" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order: {OrderId}", id);
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het bijwerken van de bestelling status" });
            }
        }

        /// <summary>
        /// Annuleer een bestelling
        /// </summary>
        /// <param name="id">Bestelling ID</param>
        /// <returns>Geen content</returns>
        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<ActionResult> CancelOrder(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                // Admin kan alle bestellingen annuleren, andere gebruikers alleen hun eigen bestellingen
                var success = await _orderService.CancelOrderAsync(id, isAdmin ? null : userId);
                if (!success)
                {
                    return NotFound(new { message = "Bestelling niet gevonden" });
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when cancelling order");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order: {OrderId}", id);
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het annuleren van de bestelling" });
            }
        }
    }
}
