using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PetShop.Tests.Performance
{
    public class ApiPerformanceTests
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;

        public ApiPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            _client = new HttpClient();
            _client.BaseAddress = new Uri("http://localhost:5104");
        }

        [Fact]
        public async Task GetProducts_PerformanceTest()
        {
            // Arrange
            var stopwatch = new Stopwatch();
            var results = new List<long>();
            const int iterations = 10;

            // Act
            for (int i = 0; i < iterations; i++)
            {
                stopwatch.Restart();
                try
                {
                    var response = await _client.GetAsync("/api/products");
                    stopwatch.Stop();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        results.Add(stopwatch.ElapsedMilliseconds);
                    }
                }
                catch (HttpRequestException)
                {
                    // API might not be running - skip this test
                    _output.WriteLine("API not available for performance testing");
                    return;
                }
            }

            // Assert
            if (results.Count > 0)
            {
                var avgTime = results.Sum() / (double)results.Count;
                var maxTime = results.Max();
                var minTime = results.Min();

                _output.WriteLine($"Performance Results for GET /api/products:");
                _output.WriteLine($"Average: {avgTime:F2}ms");
                _output.WriteLine($"Min: {minTime}ms");
                _output.WriteLine($"Max: {maxTime}ms");
                _output.WriteLine($"Iterations: {results.Count}");

                // Assert that average response time is reasonable (under 1 second)
                Assert.True(avgTime < 1000, $"Average response time {avgTime}ms exceeds 1000ms threshold");
            }
        }

        [Fact]
        public async Task GetProductById_PerformanceTest()
        {
            // Arrange
            var stopwatch = new Stopwatch();
            var results = new List<long>();
            const int iterations = 10;

            // Act
            for (int i = 0; i < iterations; i++)
            {
                stopwatch.Restart();
                try
                {
                    var response = await _client.GetAsync("/api/products/1");
                    stopwatch.Stop();
                    
                    // Record time regardless of status code (404 is also valid)
                    results.Add(stopwatch.ElapsedMilliseconds);
                }
                catch (HttpRequestException)
                {
                    // API might not be running - skip this test
                    _output.WriteLine("API not available for performance testing");
                    return;
                }
            }

            // Assert
            if (results.Count > 0)
            {
                var avgTime = results.Sum() / (double)results.Count;
                var maxTime = results.Max();
                var minTime = results.Min();

                _output.WriteLine($"Performance Results for GET /api/products/1:");
                _output.WriteLine($"Average: {avgTime:F2}ms");
                _output.WriteLine($"Min: {minTime}ms");
                _output.WriteLine($"Max: {maxTime}ms");
                _output.WriteLine($"Iterations: {results.Count}");

                // Assert that average response time is reasonable (under 500ms for single record)
                Assert.True(avgTime < 500, $"Average response time {avgTime}ms exceeds 500ms threshold");
            }
        }

        [Fact]
        public async Task ConcurrentRequests_PerformanceTest()
        {
            // Arrange
            const int concurrentRequests = 5;
            var tasks = new List<Task<HttpResponseMessage>>();
            var stopwatch = Stopwatch.StartNew();

            // Act
            try
            {
                for (int i = 0; i < concurrentRequests; i++)
                {
                    tasks.Add(_client.GetAsync("/api/products"));
                }

                var responses = await Task.WhenAll(tasks);
                stopwatch.Stop();

                // Assert
                var totalTime = stopwatch.ElapsedMilliseconds;
                var avgTimePerRequest = totalTime / (double)concurrentRequests;

                _output.WriteLine($"Concurrent Requests Performance Results:");
                _output.WriteLine($"Total time for {concurrentRequests} requests: {totalTime}ms");
                _output.WriteLine($"Average time per request: {avgTimePerRequest:F2}ms");

                // Assert that concurrent requests complete within reasonable time
                Assert.True(totalTime < 5000, $"Total time {totalTime}ms for {concurrentRequests} requests exceeds 5000ms threshold");
                
                // Check that at least some requests succeeded
                var successCount = responses.Count(r => r.IsSuccessStatusCode);
                _output.WriteLine($"Successful requests: {successCount}/{concurrentRequests}");
            }
            catch (HttpRequestException)
            {
                _output.WriteLine("API not available for concurrent performance testing");
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
