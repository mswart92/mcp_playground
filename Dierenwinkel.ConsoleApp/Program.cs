using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Dierenwinkel.Services.Data;
using Dierenwinkel.Services.Interfaces;
using Dierenwinkel.Services.Services;
using Dierenwinkel.Services.DTOs;
using ModelContextProtocol.Server;

// Create and configure the host
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Configure SQLite database
        var connectionString = "Data Source=../PetShop.API/petshop_dev.db";
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register services that actually exist
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IShoppingCartService, ShoppingCartService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
            services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();
    })
    .Build();


[McpServerToolType]
public static class ProductTool
{
    [McpServerTool, Description("Retrieves a list of products.")]
    public static async Task<string> GetProducts(IProductService productService)
    {
        // Retrieve all products using GetProductsAsync with empty search criteria
        var searchDto = new ProductSearchDto
        {
            PageNumber = 1,
            PageSize = 100 // Get first 100 products
        };

        var products = await productService.GetProductsAsync(searchDto);
        return JsonSerializer.Serialize(products);
    }
}