using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dierenwinkel.Services.DTOs;
using Dierenwinkel.Services.Interfaces;
using System.Security.Claims;

namespace PetShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShoppingCartController : ControllerBase
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ILogger<ShoppingCartController> _logger;

        public ShoppingCartController(IShoppingCartService shoppingCartService, ILogger<ShoppingCartController> logger)
        {
            _shoppingCartService = shoppingCartService;
            _logger = logger;
        }

        /// <summary>
        /// Haal de winkelwagen op
        /// </summary>
        /// <param name="sessionId">Sessie ID voor anonieme gebruikers</param>
        /// <returns>Winkelwagen inhoud</returns>
        [HttpGet]
        public async Task<ActionResult<ShoppingCartDto>> GetCart([FromQuery] string? sessionId = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                sessionId ??= Request.Headers["X-Session-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

                var cart = await _shoppingCartService.GetCartAsync(userId, sessionId);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shopping cart");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het ophalen van de winkelwagen" });
            }
        }

        /// <summary>
        /// Voeg product toe aan winkelwagen
        /// </summary>
        /// <param name="addToCartDto">Product en hoeveelheid</param>
        /// <returns>Bijgewerkte winkelwagen</returns>
        [HttpPost("add")]
        public async Task<ActionResult<ShoppingCartDto>> AddToCart([FromBody] AddToCartDto addToCartDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sessionId = addToCartDto.SessionId ?? 
                               Request.Headers["X-Session-Id"].FirstOrDefault() ?? 
                               Guid.NewGuid().ToString();

                var cart = await _shoppingCartService.AddToCartAsync(userId, sessionId, addToCartDto);
                return Ok(cart);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when adding to cart");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het toevoegen aan de winkelwagen" });
            }
        }

        /// <summary>
        /// Update hoeveelheid van een product in de winkelwagen
        /// </summary>
        /// <param name="itemId">Winkelwagen item ID</param>
        /// <param name="updateCartItemDto">Nieuwe hoeveelheid</param>
        /// <returns>Bijgewerkte winkelwagen</returns>
        [HttpPut("items/{itemId}")]
        public async Task<ActionResult<ShoppingCartDto>> UpdateCartItem(int itemId, [FromBody] UpdateCartItemDto updateCartItemDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sessionId = Request.Headers["X-Session-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

                var cart = await _shoppingCartService.UpdateCartItemAsync(userId, sessionId, itemId, updateCartItemDto);
                return Ok(cart);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when updating cart item");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het bijwerken van het winkelwagen item" });
            }
        }

        /// <summary>
        /// Verwijder product uit winkelwagen
        /// </summary>
        /// <param name="itemId">Winkelwagen item ID</param>
        /// <returns>Geen content</returns>
        [HttpDelete("items/{itemId}")]
        public async Task<ActionResult> RemoveFromCart(int itemId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sessionId = Request.Headers["X-Session-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

                var success = await _shoppingCartService.RemoveFromCartAsync(userId, sessionId, itemId);
                if (!success)
                {
                    return NotFound(new { message = "Winkelwagen item niet gevonden" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from cart");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het verwijderen uit de winkelwagen" });
            }
        }

        /// <summary>
        /// Leeg de winkelwagen
        /// </summary>
        /// <returns>Geen content</returns>
        [HttpDelete("clear")]
        public async Task<ActionResult> ClearCart()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sessionId = Request.Headers["X-Session-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

                var success = await _shoppingCartService.ClearCartAsync(userId, sessionId);
                if (!success)
                {
                    return NotFound(new { message = "Winkelwagen niet gevonden" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het legen van de winkelwagen" });
            }
        }

        /// <summary>
        /// Samenvoegen van anonieme winkelwagen met gebruiker winkelwagen na login
        /// </summary>
        /// <param name="sessionId">Sessie ID van anonieme winkelwagen</param>
        /// <returns>Samengevoegde winkelwagen</returns>
        [HttpPost("merge")]
        [Authorize]
        public async Task<ActionResult<ShoppingCartDto>> MergeCarts([FromBody] string sessionId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Gebruiker niet gevonden" });
                }

                var cart = await _shoppingCartService.MergeCartsAsync(userId, sessionId);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging carts");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het samenvoegen van winkelwagens" });
            }
        }

        /// <summary>
        /// Haal het aantal items in de winkelwagen op
        /// </summary>
        /// <param name="sessionId">Sessie ID voor anonieme gebruikers</param>
        /// <returns>Aantal items</returns>
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetCartItemCount([FromQuery] string? sessionId = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                sessionId ??= Request.Headers["X-Session-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

                var count = await _shoppingCartService.GetCartItemCountAsync(userId, sessionId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item count");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het ophalen van het winkelwagen aantal" });
            }
        }
    }
}
