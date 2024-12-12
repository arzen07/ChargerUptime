using ChargerUptime.Models;
using ChargerUptime.Services;

await ProcessCommandAsync(args);

static async Task<int> ProcessCommandAsync(string[] args)
{
    if (args.Length != 1)
    {
        Console.WriteLine("ERROR");
        Console.Error.WriteLine("Please provide a single input file path argument.");
        return 1;
    }

    string inputFilePath = args[0];

    try
    {
        var fileProcessor = new FileProcessor();
        var uptimeCalculator = new UptimeCalculator();
        var stationData = await fileProcessor.ProcessInputFileAsync(inputFilePath);
        
        if (stationData == null)
        {
            Console.WriteLine("ERROR");
            return 1;
        }

        var results = uptimeCalculator.CalculateStationUptimes(stationData);

        // Output results in ascending order of station IDs
        foreach (var result in results.OrderBy(r => r.StationId))
        {
            Console.WriteLine($"{result.StationId} {result.UptimePercentage}");
        }

        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine("ERROR");
        Console.Error.WriteLine($"Error processing file: {ex.Message}");
        return 1;
    }
}
