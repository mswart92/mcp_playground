using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Dierenwinkel.Services.Data;
using Dierenwinkel.Services.DTOs;
using Dierenwinkel.Services.Interfaces;
using Dierenwinkel.Services.Models;

namespace Dierenwinkel.Services.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IEmailService _emailService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            ApplicationDbContext context,
            IShoppingCartService shoppingCartService,
            IEmailService emailService,
            ILogger<OrderService> logger)
        {
            _context = context;
            _shoppingCartService = shoppingCartService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<OrderDto> CreateOrderAsync(string? userId, string sessionId, CreateOrderDto createOrderDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Get the cart
                var cart = await _shoppingCartService.GetCartAsync(userId, sessionId);
                
                if (!cart.Items.Any())
                {
                    throw new InvalidOperationException("Cart is empty");
                }

                // Validate stock for all items
                foreach (var item in cart.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null || !product.IsActive)
                    {
                        throw new InvalidOperationException($"Product {item.ProductName} is no longer available");
                    }

                    if (product.StockQuantity < item.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for product {item.ProductName}");
                    }
                }

                // Create order
                var order = new Order
                {
                    UserId = userId ?? string.Empty,
                    OrderNumber = GenerateOrderNumber(),
                    TotalAmount = cart.TotalAmount,
                    Status = "Pending",
                    ShippingAddress = createOrderDto.ShippingAddress,
                    ShippingCity = createOrderDto.ShippingCity,
                    ShippingPostalCode = createOrderDto.ShippingPostalCode,
                    ShippingCountry = createOrderDto.ShippingCountry,
                    CustomerEmail = createOrderDto.CustomerEmail,
                    CustomerName = createOrderDto.CustomerName,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create order items and update stock
                foreach (var item in cart.Items)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    };

                    _context.OrderItems.Add(orderItem);

                    // Update product stock
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= item.Quantity;
                        product.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                // Clear the cart
                await _shoppingCartService.ClearCartAsync(userId, sessionId);

                await transaction.CommitAsync();

                // Send confirmation email
                var orderItems = cart.Items.Select(item => $"{item.ProductName} x{item.Quantity}").ToList();
                await _emailService.SendOrderConfirmationEmailAsync(
                    createOrderDto.CustomerEmail,
                    createOrderDto.CustomerName,
                    order.OrderNumber,
                    order.TotalAmount,
                    orderItems
                );

                // Return order DTO
                return await GetOrderByIdAsync(order.Id, userId) ?? throw new InvalidOperationException("Order not found after creation");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order for user: {UserId}, session: {SessionId}", userId, sessionId);
                throw;
            }
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int id, string? userId = null)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(o => o.UserId == userId);
                }

                var order = await query.FirstOrDefaultAsync(o => o.Id == id);
                
                if (order == null)
                {
                    return null;
                }

                return new OrderDto
                {
                    Id = order.Id,
                    UserId = order.UserId,
                    OrderNumber = order.OrderNumber,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    ShippingAddress = order.ShippingAddress,
                    ShippingCity = order.ShippingCity,
                    ShippingPostalCode = order.ShippingPostalCode,
                    ShippingCountry = order.ShippingCountry,
                    CustomerEmail = order.CustomerEmail,
                    CustomerName = order.CustomerName,
                    CreatedAt = order.CreatedAt,
                    UpdatedAt = order.UpdatedAt,
                    OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                    {
                        Id = oi.Id,
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        ProductImageUrl = oi.Product.ImageUrl ?? "",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        TotalPrice = oi.TotalPrice
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order by ID: {OrderId}", id);
                throw;
            }
        }

        public async Task<PagedResultDto<OrderSummaryDto>> GetOrdersAsync(string? userId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.Orders.AsQueryable();

                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(o => o.UserId == userId);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Include(o => o.OrderItems)
                    .Select(o => new OrderSummaryDto
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status,
                        CreatedAt = o.CreatedAt,
                        ItemCount = o.OrderItems.Count
                    })
                    .ToListAsync();

                return new PagedResultDto<OrderSummaryDto>
                {
                    Items = orders,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = pageNumber > 1,
                    HasNextPage = pageNumber < totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<TopProductDto>> GetTopProductsAsync(int count = 10)
        {
            try
            {
                var topProducts = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new TopProductDto
                    {
                        ProductId = g.Key,
                        ProductName = g.First().Product.Name,
                        ProductImageUrl = g.First().Product.ImageUrl ?? "",
                        Category = g.First().Product.Category,
                        Price = g.First().Product.Price,
                        TotalQuantitySold = g.Sum(oi => oi.Quantity),
                        TotalOrders = g.Count()
                    })
                    .OrderByDescending(tp => tp.TotalQuantitySold)
                    .Take(count)
                    .ToListAsync();

                return topProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top products");
                throw;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return false;
                }

                order.Status = status;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<List<OrderDto>> GetOrdersByUserAsync(string userId)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new OrderDto
                    {
                        Id = o.Id,
                        UserId = o.UserId,
                        OrderNumber = o.OrderNumber,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status,
                        ShippingAddress = o.ShippingAddress,
                        ShippingCity = o.ShippingCity,
                        ShippingPostalCode = o.ShippingPostalCode,
                        ShippingCountry = o.ShippingCountry,
                        CustomerEmail = o.CustomerEmail,
                        CustomerName = o.CustomerName,
                        CreatedAt = o.CreatedAt,
                        UpdatedAt = o.UpdatedAt,
                        OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                        {
                            Id = oi.Id,
                            ProductId = oi.ProductId,
                            ProductName = oi.Product.Name,
                            ProductImageUrl = oi.Product.ImageUrl ?? "",
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice,
                            TotalPrice = oi.TotalPrice
                        }).ToList()
                    })
                    .ToListAsync();

                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId, string? userId = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var query = _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).AsQueryable();
                
                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(o => o.UserId == userId);
                }

                var order = await query.FirstOrDefaultAsync(o => o.Id == orderId);
                
                if (order == null)
                {
                    return false;
                }

                if (order.Status != "Pending")
                {
                    throw new InvalidOperationException("Order cannot be cancelled");
                }

                // Restore stock
                foreach (var orderItem in order.OrderItems)
                {
                    var product = orderItem.Product;
                    if (product != null)
                    {
                        product.StockQuantity += orderItem.Quantity;
                        product.UpdatedAt = DateTime.UtcNow;
                    }
                }

                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling order: {OrderId}", orderId);
                throw;
            }
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        }
    }
}
