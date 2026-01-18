using Microsoft.Extensions.DependencyInjection;
using CQReetMediator.Abstractions;
using Xunit;

namespace CQReetMediator.DependencyInjection.Tests;

/// <summary>
/// Integration tests for collection-based query handlers.
/// Validates that queries returning IReadOnlyList, List, IEnumerable, and arrays work correctly.
/// </summary>
public class CollectionQueryHandlerTests
{
    #region Test Queries and Handlers
    
    // IReadOnlyList<T> collection query (sync)
    public record GetAllProductsQuery : IQuery<IReadOnlyList<ProductDto>>;
    
    public record ProductDto(int Id, string Name, decimal Price);
    
    public class GetAllProductsHandler : IQueryHandler<GetAllProductsQuery, IReadOnlyList<ProductDto>>
    {
        public ValueTask<IReadOnlyList<ProductDto>?> HandleAsync(GetAllProductsQuery query, CancellationToken ct)
        {
            // Return array as IReadOnlyList - zero overhead
            ProductDto[] products = 
            [
                new(1, "Keyboard", 99.99m),
                new(2, "Mouse", 49.99m),
                new(3, "Monitor", 299.99m)
            ];
            return new ValueTask<IReadOnlyList<ProductDto>?>(products);
        }
    }
    
    // List<T> collection query (sync)
    public record GetCategoriesQuery : IQuery<List<string>>;
    
    public class GetCategoriesHandler : IQueryHandler<GetCategoriesQuery, List<string>>
    {
        public ValueTask<List<string>?> HandleAsync(GetCategoriesQuery query, CancellationToken ct)
        {
            var categories = new List<string> { "Electronics", "Clothing", "Books" };
            return new ValueTask<List<string>?>(categories);
        }
    }
    
    // IEnumerable<T> collection query (sync)
    public record GetTagsQuery : IQuery<IEnumerable<string>>;
    
    public class GetTagsHandler : IQueryHandler<GetTagsQuery, IEnumerable<string>>
    {
        public ValueTask<IEnumerable<string>?> HandleAsync(GetTagsQuery query, CancellationToken ct)
        {
            IEnumerable<string> tags = new[] { "featured", "sale", "new" };
            return new ValueTask<IEnumerable<string>?>(tags);
        }
    }
    
    // Array collection query (sync)
    public record GetProductIdsQuery : IQuery<int[]>;
    
    public class GetProductIdsHandler : IQueryHandler<GetProductIdsQuery, int[]>
    {
        public ValueTask<int[]?> HandleAsync(GetProductIdsQuery query, CancellationToken ct)
        {
            return new ValueTask<int[]?>(new[] { 1, 2, 3, 4, 5 });
        }
    }
    
    // Async IReadOnlyList<T> collection query
    public record GetOrdersQuery(int CustomerId) : IQuery<IReadOnlyList<OrderDto>>;
    
    public record OrderDto(int Id, int CustomerId, decimal Total);
    
    public class GetOrdersHandler : IAsyncQueryHandler<GetOrdersQuery, IReadOnlyList<OrderDto>>
    {
        public async Task<IReadOnlyList<OrderDto>?> HandleAsync(GetOrdersQuery query, CancellationToken ct)
        {
            await Task.Delay(1, ct); // Simulate async I/O
            
            OrderDto[] orders = 
            [
                new(1, query.CustomerId, 150.00m),
                new(2, query.CustomerId, 275.50m)
            ];
            return orders;
        }
    }
    
    // Async List<T> collection query
    public record GetCustomerNamesQuery : IQuery<List<string>>;
    
    public class GetCustomerNamesAsyncHandler : IAsyncRequestHandler<GetCustomerNamesQuery, List<string>>
    {
        public async Task<List<string>?> HandleAsync(GetCustomerNamesQuery query, CancellationToken ct)
        {
            await Task.Delay(1, ct);
            return new List<string> { "Alice", "Bob", "Charlie" };
        }
    }
    
    // Empty collection query
    public record GetEmptyResultsQuery : IQuery<IReadOnlyList<string>>;
    
    public class GetEmptyResultsHandler : IQueryHandler<GetEmptyResultsQuery, IReadOnlyList<string>>
    {
        public ValueTask<IReadOnlyList<string>?> HandleAsync(GetEmptyResultsQuery query, CancellationToken ct)
        {
            // Best practice: use Array.Empty<T>() for empty collections
            return new ValueTask<IReadOnlyList<string>?>(Array.Empty<string>());
        }
    }
    
    // Scalar query for backward compatibility testing
    public record GetProductCountQuery : IQuery<int>;
    
    public class GetProductCountHandler : IQueryHandler<GetProductCountQuery, int>
    {
        public ValueTask<int> HandleAsync(GetProductCountQuery query, CancellationToken ct)
        {
            return new ValueTask<int>(42);
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Creates a configured service collection with required dependencies.
    /// Registers PipelineSpy to satisfy the open generic pipeline behavior from TestArtifacts.
    /// </summary>
    private static ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<PipelineSpy>();
        return services;
    }
    
    #endregion
    
    #region Integration Tests
    
    [Fact]
    public async Task Send_IReadOnlyList_Should_Return_Correct_Collection()
    {
        // Arrange
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetAllProductsHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act
        var result = await mediator.Send(new GetAllProductsQuery());
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Keyboard", result[0].Name);
        Assert.Equal("Mouse", result[1].Name);
        Assert.Equal("Monitor", result[2].Name);
    }
    
    [Fact]
    public async Task Send_ListT_Should_Return_Mutable_Collection()
    {
        // Arrange
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetCategoriesHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act
        var result = await mediator.Send(new GetCategoriesQuery());
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("Electronics", result);
        Assert.Contains("Clothing", result);
        Assert.Contains("Books", result);
        
        // Verify mutability - List<T> should be modifiable
        result.Add("Sports");
        Assert.Equal(4, result.Count);
    }
    
    [Fact]
    public async Task Send_IEnumerable_Should_Return_Enumerable_Collection()
    {
        // Arrange
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetTagsHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act
        var result = await mediator.Send(new GetTagsQuery());
        
        // Assert
        Assert.NotNull(result);
        var list = result.ToList();
        Assert.Equal(3, list.Count);
        Assert.Contains("featured", list);
        Assert.Contains("sale", list);
        Assert.Contains("new", list);
    }
    
    [Fact]
    public async Task Send_Array_Should_Return_Array_Collection()
    {
        // Arrange
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetProductIdsHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act
        var result = await mediator.Send(new GetProductIdsQuery());
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Length);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result);
    }
    
    [Fact]
    public async Task SendAsync_IReadOnlyList_Should_Return_Collection_From_Async_Handler()
    {
        // Arrange
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetOrdersHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act
        var result = await mediator.SendAsync(new GetOrdersQuery(CustomerId: 123));
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, order => Assert.Equal(123, order.CustomerId));
        Assert.Equal(150.00m, result[0].Total);
        Assert.Equal(275.50m, result[1].Total);
    }
    
    [Fact]
    public async Task SendAsync_ListT_Should_Return_Collection_From_Async_Handler()
    {
        // Arrange
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetCustomerNamesAsyncHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act
        var result = await mediator.SendAsync(new GetCustomerNamesQuery());
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { "Alice", "Bob", "Charlie" }, result);
    }
    
    [Fact]
    public async Task Send_EmptyCollection_Should_Return_Empty_Array()
    {
        // Arrange
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetEmptyResultsHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act
        var result = await mediator.Send(new GetEmptyResultsQuery());
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        // Verify it's using Array.Empty<T>() (should be same reference)
        Assert.Same(Array.Empty<string>(), result);
    }
    
    [Fact]
    public async Task ScalarQuery_Should_Still_Work_With_Collection_Handlers_Registered()
    {
        // Arrange - Register both scalar and collection handlers
        var services = CreateServiceCollection();
        services.AddCQReetMediator(
            typeof(GetAllProductsHandler),
            typeof(GetProductCountHandler)
        );
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act - Execute scalar query
        var scalarResult = await mediator.Send(new GetProductCountQuery());
        
        // Act - Execute collection query
        var collectionResult = await mediator.Send(new GetAllProductsQuery());
        
        // Assert - Both work correctly
        Assert.Equal(42, scalarResult);
        Assert.NotNull(collectionResult);
        Assert.Equal(3, collectionResult.Count);
    }
    
    [Fact]
    public async Task MultipleCollectionTypes_Should_Work_In_Same_Registration()
    {
        // Arrange
        var services = CreateServiceCollection();
        services.AddCQReetMediator(
            typeof(GetAllProductsHandler),     // IReadOnlyList<T>
            typeof(GetCategoriesHandler),       // List<T>
            typeof(GetTagsHandler),             // IEnumerable<T>
            typeof(GetProductIdsHandler)        // T[]
        );
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act & Assert - All collection types work
        var products = await mediator.Send(new GetAllProductsQuery());
        Assert.NotNull(products);
        Assert.Equal(3, products.Count);
        
        var categories = await mediator.Send(new GetCategoriesQuery());
        Assert.NotNull(categories);
        Assert.Equal(3, categories.Count);
        
        var tags = await mediator.Send(new GetTagsQuery());
        Assert.NotNull(tags);
        Assert.Equal(3, tags.Count());
        
        var ids = await mediator.Send(new GetProductIdsQuery());
        Assert.NotNull(ids);
        Assert.Equal(5, ids.Length);
    }
    
    [Fact]
    public async Task SyncAndAsyncHandlers_Should_Work_Together()
    {
        // Arrange
        var services = CreateServiceCollection();
        services.AddCQReetMediator(
            typeof(GetAllProductsHandler),   // Sync handler
            typeof(GetOrdersHandler)         // Async handler
        );
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act - Execute sync collection query
        var syncResult = await mediator.Send(new GetAllProductsQuery());
        
        // Act - Execute async collection query
        var asyncResult = await mediator.SendAsync(new GetOrdersQuery(CustomerId: 1));
        
        // Assert
        Assert.NotNull(syncResult);
        Assert.Equal(3, syncResult.Count);
        
        Assert.NotNull(asyncResult);
        Assert.Equal(2, asyncResult.Count);
    }
    
    #endregion
}
