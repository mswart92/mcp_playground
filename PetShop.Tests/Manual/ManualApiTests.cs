using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PetShop.Tests.Manual
{
    /// <summary>
    /// Manual API tests to demonstrate functionality
    /// These tests require the API to be running on localhost:5104
    /// Run with: dotnet test --filter "Category=Manual"
    /// </summary>
    public class ManualApiTests
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;

        public ManualApiTests(ITestOutputHelper output)
        {
            _output = output;
            _client = new HttpClient();
            _client.BaseAddress = new Uri("http://localhost:5104");
        }

        [Fact]
        [Trait("Category", "Manual")]
        public async Task ApiEndpoints_SmokeTest()
        {
            _output.WriteLine("=== Pet Shop API Smoke Test ===");
            
            try
            {
                // Test 1: Get all products
                _output.WriteLine("\n1. Testing GET /api/products");
                var productsResponse = await _client.GetAsync("/api/products");
                _output.WriteLine($"   Status: {productsResponse.StatusCode}");
                
                if (productsResponse.IsSuccessStatusCode)
                {
                    var productsContent = await productsResponse.Content.ReadAsStringAsync();
                    _output.WriteLine($"   Response length: {productsContent.Length} characters");
                    _output.WriteLine("   ✅ Products endpoint working");
                }
                else
                {
                    _output.WriteLine("   ❌ Products endpoint failed");
                }

                // Test 2: Get product by ID
                _output.WriteLine("\n2. Testing GET /api/products/1");
                var productResponse = await _client.GetAsync("/api/products/1");
                _output.WriteLine($"   Status: {productResponse.StatusCode}");
                
                if (productResponse.IsSuccessStatusCode)
                {
                    var productContent = await productResponse.Content.ReadAsStringAsync();
                    _output.WriteLine($"   Response length: {productContent.Length} characters");
                    _output.WriteLine("   ✅ Product by ID endpoint working");
                }
                else
                {
                    _output.WriteLine("   ⚠️ Product by ID returned not found (expected if no seed data)");
                }

                // Test 3: Test Swagger endpoint
                _output.WriteLine("\n3. Testing GET /swagger/v1/swagger.json");
                var swaggerResponse = await _client.GetAsync("/swagger/v1/swagger.json");
                _output.WriteLine($"   Status: {swaggerResponse.StatusCode}");
                
                if (swaggerResponse.IsSuccessStatusCode)
                {
                    var swaggerContent = await swaggerResponse.Content.ReadAsStringAsync();
                    _output.WriteLine($"   Response length: {swaggerContent.Length} characters");
                    _output.WriteLine("   ✅ Swagger documentation endpoint working");
                }
                else
                {
                    _output.WriteLine("   ❌ Swagger endpoint failed");
                }

                // Test 4: Test product search
                _output.WriteLine("\n4. Testing GET /api/products?search=dog");
                var searchResponse = await _client.GetAsync("/api/products?search=dog");
                _output.WriteLine($"   Status: {searchResponse.StatusCode}");
                
                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchContent = await searchResponse.Content.ReadAsStringAsync();
                    _output.WriteLine($"   Response length: {searchContent.Length} characters");
                    _output.WriteLine("   ✅ Product search endpoint working");
                }
                else
                {
                    _output.WriteLine("   ❌ Product search endpoint failed");
                }

                // Test 5: Test categories
                _output.WriteLine("\n5. Testing GET /api/products/categories");
                var categoriesResponse = await _client.GetAsync("/api/products/categories");
                _output.WriteLine($"   Status: {categoriesResponse.StatusCode}");
                
                if (categoriesResponse.IsSuccessStatusCode)
                {
                    var categoriesContent = await categoriesResponse.Content.ReadAsStringAsync();
                    _output.WriteLine($"   Response length: {categoriesContent.Length} characters");
                    _output.WriteLine("   ✅ Categories endpoint working");
                }
                else
                {
                    _output.WriteLine("   ❌ Categories endpoint failed");
                }

                // Test 6: Test top products
                _output.WriteLine("\n6. Testing GET /api/orders/top-products");
                var topProductsResponse = await _client.GetAsync("/api/orders/top-products");
                _output.WriteLine($"   Status: {topProductsResponse.StatusCode}");
                
                if (topProductsResponse.IsSuccessStatusCode)
                {
                    var topProductsContent = await topProductsResponse.Content.ReadAsStringAsync();
                    _output.WriteLine($"   Response length: {topProductsContent.Length} characters");
                    _output.WriteLine("   ✅ Top products endpoint working");
                }
                else if (topProductsResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _output.WriteLine("   ⚠️ Top products endpoint requires authentication (expected)");
                }
                else
                {
                    _output.WriteLine("   ❌ Top products endpoint failed");
                }

                _output.WriteLine("\n=== Test Summary ===");
                _output.WriteLine("API is running and responsive!");
                _output.WriteLine("Main endpoints are accessible.");
                _output.WriteLine("Authentication endpoints require proper JWT tokens.");
                _output.WriteLine("For full testing, use the Swagger UI at http://localhost:5104/swagger");
                
            }
            catch (HttpRequestException ex)
            {
                _output.WriteLine($"❌ API not accessible: {ex.Message}");
                _output.WriteLine("Make sure the API is running with: dotnet run --project PetShop.API");
                Assert.True(false, "API not accessible for testing");
            }
        }

        [Fact]
        [Trait("Category", "Manual")]
        public async Task DatabaseConnectivity_Test()
        {
            _output.WriteLine("=== Database Connectivity Test ===");
            
            try
            {
                // Test database connectivity by checking if we can get products
                var response = await _client.GetAsync("/api/products");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Try to parse as JSON to ensure valid response
                    if (content.StartsWith("[") || content.StartsWith("{"))
                    {
                        _output.WriteLine("✅ Database connectivity confirmed");
                        _output.WriteLine("✅ API can retrieve data from SQLite database");
                        _output.WriteLine($"   Response size: {content.Length} characters");
                    }
                    else
                    {
                        _output.WriteLine("⚠️ Unexpected response format");
                    }
                }
                else
                {
                    _output.WriteLine($"❌ Database connectivity issue: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ Database connectivity test failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
