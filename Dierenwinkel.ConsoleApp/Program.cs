using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Dierenwinkel.Services.Data;
using Dierenwinkel.Services.Interfaces;
using Dierenwinkel.Services.Services;
using Dierenwinkel.Services.DTOs;

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
    })
    .Build();

try
{
    Console.WriteLine("=== Pet Shop Console Application ===");
    Console.WriteLine("Connecting to database and retrieving products...\n");

    // Get the product service from DI container
    using var scope = host.Services.CreateScope();
    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Retrieve all products using GetProductsAsync with empty search criteria
    var searchDto = new ProductSearchDto
    {
        PageNumber = 1,
        PageSize = 100 // Get first 100 products
    };
    
    var pagedResult = await productService.GetProductsAsync(searchDto);

    if (pagedResult?.Items?.Any() == true)
    {
        Console.WriteLine($"Found {pagedResult.Items.Count} products (Total: {pagedResult.TotalCount}):");
        Console.WriteLine(new string('-', 80));
        
        foreach (var product in pagedResult.Items)
        {
            Console.WriteLine($"ID: {product.Id}");
            Console.WriteLine($"Name: {product.Name}");
            Console.WriteLine($"Description: {product.Description}");
            Console.WriteLine($"Price: ${product.Price:F2}");
            Console.WriteLine($"Stock: {product.StockQuantity}");
            Console.WriteLine($"Category: {product.Category}");
            Console.WriteLine($"Active: {product.IsActive}");
            Console.WriteLine($"Created: {product.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine(new string('-', 80));
        }
        
        // Also test getting categories
        Console.WriteLine("\n=== Available Categories ===");
        var categories = await productService.GetCategoriesAsync();
        foreach (var category in categories)
        {
            Console.WriteLine($"- {category}");
        }
    }
    else
    {
        Console.WriteLine("No products found in the database.");
        logger.LogInformation("No products were found in the database");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error occurred: {ex.Message}");
    
    // Check if it's a database connection issue
    if (ex.Message.Contains("database") || ex.Message.Contains("SQLite") || ex.Message.Contains("connection"))
    {
        Console.WriteLine("\nThis might be a database connection issue. Please ensure:");
        Console.WriteLine("1. The PetShop.API has been run at least once to create the database");
        Console.WriteLine("2. The database file 'petshop_dev.db' exists in the PetShop.API directory");
        Console.WriteLine("3. You have the necessary permissions to access the database file");
    }
    
    Console.WriteLine($"\nFull error details: {ex}");
}
finally
{
    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
}
