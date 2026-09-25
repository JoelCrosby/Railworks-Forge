using System.Diagnostics;

using AngleSharp.Xml;

using CliWrap;

using RailworksForge.Core.External;

if (args.Length == 0)
{
    Console.Error.WriteLine("Pass BIN file paths. Set SERZ_EXE_PATH to also compare the game converter.");

    return 1;
}

const int iterations = 5;
var executable = Environment.GetEnvironmentVariable("SERZ_EXE_PATH");
var directory = Directory.CreateTempSubdirectory("serz-benchmark-");
var outputPath = Path.Join(directory.FullName, "output.xml");

try
{
    foreach (var argument in args)
    {
        var inputPath = Path.GetFullPath(argument);
        Console.WriteLine($"{inputPath} ({new FileInfo(inputPath).Length:N0} bytes)");
        Action roundTrip = () =>
        {
            var document = SerzInternal.Convert(inputPath);
            var xml = document.ToXml();
            File.WriteAllText(outputPath, xml);
        };
        Action direct = () => SerzInternal.Convert(inputPath, outputPath);

        roundTrip();
        direct();
        var roundTripSamples = new List<Measurement>();
        var directSamples = new List<Measurement>();
        var readSamples = new List<Measurement>();

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            readSamples.Add(Measure(() => File.ReadAllBytes(inputPath)));
            roundTripSamples.Add(Measure(roundTrip));
            directSamples.Add(Measure(direct));
        }

        Print("Read BIN", readSamples);
        Print("DOM round trip", roundTripSamples);
        Print("Direct file conversion", directSamples);

        if (!string.IsNullOrEmpty(executable))
        {
            var windows = OperatingSystem.IsWindows();
            var command = windows ? executable : "wine";
            var inputArgument = windows ? inputPath : "Z:" + inputPath.Replace('/', '\\');
            var outputArgument = windows ? outputPath : "Z:" + outputPath.Replace('/', '\\');
            var converterArguments = new List<string>();

            if (!windows)
            {
                converterArguments.Add(executable);
            }

            converterArguments.Add(inputArgument);
            converterArguments.Add("\\xml: " + outputArgument);
            var wineSamples = new List<double>();

            for (var iteration = 0; iteration <= iterations; iteration++)
            {
                File.Delete(outputPath);
                var timer = Stopwatch.StartNew();
                await Cli.Wrap(command).WithArguments(converterArguments).ExecuteAsync();
                var elapsed = timer.Elapsed.TotalMilliseconds;

                if (!File.Exists(outputPath))
                {
                    throw new IOException("The game converter did not produce XML.");
                }

                if (iteration > 0)
                {
                    wineSamples.Add(elapsed);
                }
            }

            Console.WriteLine($"  Game converter: {Median(wineSamples):F2} ms");
        }
    }
}
finally
{
    directory.Delete(true);
}

return 0;

static Measurement Measure(Action action)
{
    var allocated = GC.GetAllocatedBytesForCurrentThread();
    var timer = Stopwatch.StartNew();
    action();
    var elapsed = timer.Elapsed.TotalMilliseconds;
    var allocationMiB = (GC.GetAllocatedBytesForCurrentThread() - allocated) / 1048576d;

    return new Measurement(elapsed, allocationMiB);
}

static void Print(string label, List<Measurement> samples)
{
    var time = Median(samples.Select(sample => sample.Milliseconds));
    var allocations = Median(samples.Select(sample => sample.AllocatedMiB));
    Console.WriteLine($"  {label}: {time:F2} ms, {allocations:F2} MiB allocated");
}

static double Median(IEnumerable<double> values)
{
    var sorted = values.Order().ToArray();

    return sorted[sorted.Length / 2];
}

internal sealed record Measurement(double Milliseconds, double AllocatedMiB);
