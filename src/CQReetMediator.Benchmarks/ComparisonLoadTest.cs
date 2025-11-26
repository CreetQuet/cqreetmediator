using System.Diagnostics;
using CQReetMediator.Abstractions;
using CQReetMediator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CQReetMediator.Benchmarks;

public static class ComparisonLoadTest {
    private const int ITERATIONS = 1_000_000;

    public static async Task RunAsync() {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("    CQReetMediator vs Direct Call Performance");
        Console.WriteLine("==================================================");
        Console.ResetColor();

       
        var services = new ServiceCollection();
        services.AddCQReetMediator(typeof(FastPing));
        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();
        var request = new FastPing();
        var directHandler = new FastPingHandler();

       
        Console.Write("Warming up... ");
        await mediator.Send(request);
        await directHandler.HandleAsync(request, CancellationToken.None);
        Console.WriteLine("Ready.\n");

       
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[TEST A] Executing {ITERATIONS:N0} DIRECT calls...");
        Console.ResetColor();

        var swDirect = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++) {
            await directHandler.HandleAsync(request, CancellationToken.None);
        }
        swDirect.Stop();

        PrintResult("Direct Call", swDirect.Elapsed, ITERATIONS);
       
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n[TEST B] Executing {ITERATIONS:N0} calls via MEDIATOR...");
        Console.ResetColor();

        var swMediator = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++) {
            await mediator.Send(request);
        }
        swMediator.Stop();

        PrintResult("Mediator", swMediator.Elapsed, ITERATIONS);
        
        PrintSummary(swDirect.Elapsed, swMediator.Elapsed);
    }

    private static void PrintResult(string name, TimeSpan elapsed, int count) {
        double rps = count / elapsed.TotalSeconds;
        double avgUs = (elapsed.TotalMilliseconds * 1000) / count;

        Console.WriteLine($"   ⏱️  Total Time:   {elapsed.TotalSeconds:F4} s");
        Console.WriteLine($"   🚀  Throughput:   {rps:N0} ops/sec");
        Console.WriteLine($"   ⚡  Avg Latency:  {avgUs:F4} µs/op");
    }

    private static void PrintSummary(TimeSpan directTime, TimeSpan mediatorTime) {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("               FINAL SUMMARY");
        Console.WriteLine("==================================================");

        double ratio = mediatorTime.TotalMilliseconds / directTime.TotalMilliseconds;
        double overheadNs = (mediatorTime.TotalNanoseconds - directTime.TotalNanoseconds) / ITERATIONS;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"🏆 Winner: {(ratio < 1 ? "Mediator (Impossible!)" : "Direct Call")}");
        Console.ResetColor();

        Console.WriteLine($"📊 Ratio: 1 Direct Call equals {ratio:F2} calls to Mediator");

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"🐢 Overhead: Your Mediator adds only {overheadNs:F2} nanoseconds per call.");
        Console.ResetColor();
        Console.WriteLine("==================================================");
        Console.WriteLine();
    }
}