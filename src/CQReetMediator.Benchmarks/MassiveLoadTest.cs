using System.Diagnostics;
using CQReetMediator.Abstractions;
using CQReetMediator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CQReetMediator.Benchmarks;

public static class MassiveLoadTest {
    public static async Task RunAsync() {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== STARTING MASSIVE LOAD TEST ===");
        Console.ResetColor();

        var services = new ServiceCollection();
        services.AddCQReetMediator(typeof(FastPing));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        Console.WriteLine("Warming up...");
        await mediator.Send(new FastPing());

        int requestCount = 1_000_000;
        var request = new FastPing();

        Console.WriteLine($"Executing {requestCount:N0} concurrent requests...");

        var tasks = new Task[requestCount];
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < requestCount; i++) {
            tasks[i] = mediator.Send(request).AsTask();
        }

        await Task.WhenAll(tasks);

        sw.Stop();

        double seconds = sw.Elapsed.TotalSeconds;
        double rps = requestCount / seconds;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✅ COMPLETED in {seconds:F4} seconds");
        Console.WriteLine($"🚀 Throughput: {rps:N0} req/sec");
        Console.WriteLine($"⏱️ Average: {(sw.Elapsed.TotalMilliseconds * 1000 / requestCount):F4} µs/req");
        Console.ResetColor();
    }
}

public record FastPing : IRequest<int>;

public class FastPingHandler : IRequestHandler<FastPing, int> {
    public ValueTask<int> HandleAsync(FastPing request, CancellationToken ct) => new(1);
}