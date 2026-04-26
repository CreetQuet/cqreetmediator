using Microsoft.Extensions.DependencyInjection;
using CQReetMediator.Abstractions;
using Xunit;

namespace CQReetMediator.DependencyInjection.Tests;

public class CollectionQueryHandlerTests {
    #region Test Queries and Handlers

    public record GetAllProductsQuery : IQuery<IReadOnlyList<ProductDto>>;
    public record ProductDto(int Id, string Name, decimal Price);

    public class GetAllProductsHandler : IQueryHandler<GetAllProductsQuery, IReadOnlyList<ProductDto>> {
        public Task<IReadOnlyList<ProductDto>?> HandleAsync(GetAllProductsQuery query, CancellationToken ct) {
            ProductDto[] products = [
                new(1, "Keyboard", 99.99m),
                new(2, "Mouse", 49.99m),
                new(3, "Monitor", 299.99m)
            ];
            return Task.FromResult<IReadOnlyList<ProductDto>?>(products);
        }
    }

    public record GetCategoriesQuery : IQuery<List<string>>;

    public class GetCategoriesHandler : IQueryHandler<GetCategoriesQuery, List<string>> {
        public Task<List<string>?> HandleAsync(GetCategoriesQuery query, CancellationToken ct) {
            var categories = new List<string> { "Electronics", "Clothing", "Books" };
            return Task.FromResult<List<string>?>(categories);
        }
    }

    public record GetTagsQuery : IQuery<IEnumerable<string>>;

    public class GetTagsHandler : IQueryHandler<GetTagsQuery, IEnumerable<string>> {
        public Task<IEnumerable<string>?> HandleAsync(GetTagsQuery query, CancellationToken ct) {
            IEnumerable<string> tags = new[] { "featured", "sale", "new" };
            return Task.FromResult<IEnumerable<string>?>(tags);
        }
    }

    public record GetProductIdsQuery : IQuery<int[]>;

    public class GetProductIdsHandler : IQueryHandler<GetProductIdsQuery, int[]> {
        public Task<int[]?> HandleAsync(GetProductIdsQuery query, CancellationToken ct)
            => Task.FromResult<int[]?>(new[] { 1, 2, 3, 4, 5 });
    }

    public record GetOrdersQuery(int CustomerId) : IQuery<IReadOnlyList<OrderDto>>;
    public record OrderDto(int Id, int CustomerId, decimal Total);

    public class GetOrdersHandler : IQueryHandler<GetOrdersQuery, IReadOnlyList<OrderDto>> {
        public async Task<IReadOnlyList<OrderDto>?> HandleAsync(GetOrdersQuery query, CancellationToken ct) {
            await Task.Delay(1, ct);
            OrderDto[] orders = [
                new(1, query.CustomerId, 150.00m),
                new(2, query.CustomerId, 275.50m)
            ];
            return orders;
        }
    }

    public record GetCustomerNamesQuery : IQuery<List<string>>;

    public class GetCustomerNamesHandler : IRequestHandler<GetCustomerNamesQuery, List<string>> {
        public async Task<List<string>?> HandleAsync(GetCustomerNamesQuery query, CancellationToken ct) {
            await Task.Delay(1, ct);
            return ["Alice", "Bob", "Charlie"];
        }
    }

    public record GetEmptyResultsQuery : IQuery<IReadOnlyList<string>>;

    public class GetEmptyResultsHandler : IQueryHandler<GetEmptyResultsQuery, IReadOnlyList<string>> {
        public Task<IReadOnlyList<string>?> HandleAsync(GetEmptyResultsQuery query, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<string>?>(Array.Empty<string>());
    }

    public record GetProductCountQuery : IQuery<int>;

    public class GetProductCountHandler : IQueryHandler<GetProductCountQuery, int> {
        public Task<int> HandleAsync(GetProductCountQuery query, CancellationToken ct)
            => Task.FromResult(42);
    }

    public record VoidQuery : IQuery;

    public class VoidQuerySpy {
        public bool WasCalled { get; set; }
    }

    public class VoidQueryHandler(VoidQuerySpy spy) : IQueryHandler<VoidQuery> {
        public Task HandleAsync(VoidQuery request, CancellationToken ct) {
            spy.WasCalled = true;
            return Task.CompletedTask;
        }
    }

    #endregion

    #region Helper Methods

    private static ServiceCollection CreateServiceCollection() {
        var services = new ServiceCollection();
        services.AddSingleton<PipelineSpy>();
        services.AddSingleton<VoidPipelineSpy>();
        return services;
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task SendAsync_IReadOnlyList_Should_Return_Correct_Collection() {
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetAllProductsHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new GetAllProductsQuery());

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Keyboard", result[0].Name);
        Assert.Equal("Mouse", result[1].Name);
        Assert.Equal("Monitor", result[2].Name);
    }

    [Fact]
    public async Task SendAsync_ListT_Should_Return_Mutable_Collection() {
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetCategoriesHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new GetCategoriesQuery());

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("Electronics", result);

        result.Add("Sports");
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task SendAsync_IEnumerable_Should_Return_Enumerable_Collection() {
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetTagsHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new GetTagsQuery());

        Assert.NotNull(result);
        var list = result.ToList();
        Assert.Equal(3, list.Count);
        Assert.Contains("featured", list);
    }

    [Fact]
    public async Task SendAsync_Array_Should_Return_Array_Collection() {
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetProductIdsHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new GetProductIdsQuery());

        Assert.NotNull(result);
        Assert.Equal(5, result.Length);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result);
    }

    [Fact]
    public async Task SendAsync_IReadOnlyList_Should_Return_Collection_From_Async_Handler() {
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetOrdersHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new GetOrdersQuery(CustomerId: 123));

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, order => Assert.Equal(123, order.CustomerId));
    }

    [Fact]
    public async Task SendAsync_ListT_Should_Return_Collection_From_Handler() {
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetCustomerNamesHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new GetCustomerNamesQuery());

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(["Alice", "Bob", "Charlie"], result);
    }

    [Fact]
    public async Task SendAsync_EmptyCollection_Should_Return_Empty_Array() {
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetEmptyResultsHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new GetEmptyResultsQuery());

        Assert.NotNull(result);
        Assert.Empty(result);
        Assert.Same(Array.Empty<string>(), result);
    }

    [Fact]
    public async Task ScalarQuery_Should_Still_Work_With_Collection_Handlers_Registered() {
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(GetAllProductsHandler), typeof(GetProductCountHandler));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var scalarResult = await mediator.SendAsync(new GetProductCountQuery());
        var collectionResult = await mediator.SendAsync(new GetAllProductsQuery());

        Assert.Equal(42, scalarResult);
        Assert.NotNull(collectionResult);
        Assert.Equal(3, collectionResult.Count);
    }

    [Fact]
    public async Task MultipleCollectionTypes_Should_Work_In_Same_Registration() {
        var services = CreateServiceCollection();
        services.AddCQReetMediator(
            typeof(GetAllProductsHandler),
            typeof(GetCategoriesHandler),
            typeof(GetTagsHandler),
            typeof(GetProductIdsHandler)
        );
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var products = await mediator.SendAsync(new GetAllProductsQuery());
        Assert.NotNull(products);
        Assert.Equal(3, products.Count);

        var categories = await mediator.SendAsync(new GetCategoriesQuery());
        Assert.NotNull(categories);
        Assert.Equal(3, categories.Count);

        var tags = await mediator.SendAsync(new GetTagsQuery());
        Assert.NotNull(tags);
        Assert.Equal(3, tags.Count());

        var ids = await mediator.SendAsync(new GetProductIdsQuery());
        Assert.NotNull(ids);
        Assert.Equal(5, ids.Length);
    }

    [Fact]
    public async Task VoidQuery_Should_Execute_Handler() {
        var services = CreateServiceCollection();
        services.AddCQReetMediator(typeof(VoidQueryHandler));

        var spy = new VoidQuerySpy();
        services.AddSingleton(spy);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new VoidQuery());

        Assert.True(spy.WasCalled);
    }

    #endregion
}
