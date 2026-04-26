using System.Diagnostics;
using CQReetMediator.Abstractions;
using CQReetMediator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CQReetMediator.Benchmarks;

public static class ComparisonLoadTest {
    private const int ITERATIONS = 1_000_000;

    public static async Task RunAsync() {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("===========================================================");
        Console.WriteLine("    CQReetMediator vs MediatR vs Direct Call Performance");
        Console.WriteLine("===========================================================");
        Console.ResetColor();

        // --- Setup CQReetMediator ---
        var cqreetServices = new ServiceCollection();
        cqreetServices.AddCQReetMediator(typeof(FastPing));
        var cqreetProvider = cqreetServices.BuildServiceProvider();
        var cqreetMediator = cqreetProvider.GetRequiredService<IMediator>();

        // --- Setup MediatR ---
        var mediatrServices = new ServiceCollection();
        mediatrServices.AddLogging();
        mediatrServices.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<MediatRFastPing>());
        var mediatrProvider = mediatrServices.BuildServiceProvider();
        var mediatr = mediatrProvider.GetRequiredService<MediatR.IMediator>();

        // --- Setup Direct ---
        var directHandler = new FastPingHandler();
        var cqreetRequest = new FastPing();
        var mediatrRequest = new MediatRFastPing();

        // --- Warm up ---
        Console.Write("Warming up... ");
        await cqreetMediator.SendAsync(cqreetRequest);
        await mediatr.Send(mediatrRequest);
        await directHandler.HandleAsync(cqreetRequest, CancellationToken.None);
        Console.WriteLine("Ready.\n");

        // --- TEST A: Direct Call ---
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[TEST A] Executing {ITERATIONS:N0} DIRECT calls...");
        Console.ResetColor();

        var swDirect = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++)
            await directHandler.HandleAsync(cqreetRequest, CancellationToken.None);
        swDirect.Stop();
        PrintResult("Direct Call", swDirect.Elapsed, ITERATIONS);

        // --- TEST B: CQReetMediator ---
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n[TEST B] Executing {ITERATIONS:N0} calls via CQReetMediator...");
        Console.ResetColor();

        var swCQReet = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++)
            await cqreetMediator.SendAsync(cqreetRequest);
        swCQReet.Stop();
        PrintResult("CQReetMediator", swCQReet.Elapsed, ITERATIONS);

        // --- TEST C: MediatR ---
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n[TEST C] Executing {ITERATIONS:N0} calls via MediatR...");
        Console.ResetColor();

        var swMediatR = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++)
            await mediatr.Send(mediatrRequest);
        swMediatR.Stop();
        PrintResult("MediatR", swMediatR.Elapsed, ITERATIONS);

        // --- Summary ---
        PrintSummary(swDirect.Elapsed, swCQReet.Elapsed, swMediatR.Elapsed);
    }

    private static void PrintResult(string name, TimeSpan elapsed, int count) {
        double rps = count / elapsed.TotalSeconds;
        double avgUs = (elapsed.TotalMilliseconds * 1000) / count;

        Console.WriteLine($"   Total Time:   {elapsed.TotalSeconds:F4} s");
        Console.WriteLine($"   Throughput:   {rps:N0} ops/sec");
        Console.WriteLine($"   Avg Latency:  {avgUs:F4} us/op");
    }

    private static void PrintSummary(TimeSpan directTime, TimeSpan cqreetTime, TimeSpan mediatrTime) {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("===========================================================");
        Console.WriteLine("                     FINAL SUMMARY");
        Console.WriteLine("===========================================================");
        Console.ResetColor();

        double cqreetOverheadNs = (cqreetTime.TotalNanoseconds - directTime.TotalNanoseconds) / ITERATIONS;
        double mediatrOverheadNs = (mediatrTime.TotalNanoseconds - directTime.TotalNanoseconds) / ITERATIONS;
        double speedup = mediatrTime.TotalMilliseconds / cqreetTime.TotalMilliseconds;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  CQReetMediator overhead:  {cqreetOverheadNs:F2} ns/call");
        Console.WriteLine($"  MediatR overhead:         {mediatrOverheadNs:F2} ns/call");
        Console.WriteLine($"  CQReetMediator is {speedup:F2}x faster than MediatR");
        Console.ResetColor();
        Console.WriteLine("===========================================================");
        Console.WriteLine();
    }
}

// --- MediatR artifacts for load tests ---

public record MediatRFastPing : MediatR.IRequest<int>;

public class MediatRFastPingHandler : MediatR.IRequestHandler<MediatRFastPing, int> {
    public Task<int> Handle(MediatRFastPing request, CancellationToken ct)
        => Task.FromResult(1);
}
