using ChargerUptime.Services;
using ChargerUptime.Models;
using Xunit;

namespace ChargerUptime.Tests;

public class FileProcessorTests : IDisposable
{
    private readonly FileProcessor _processor;
    private readonly string _testFilePath;

    private const string STATIONS_HEADER = "[Stations]";
    private const string REPORTS_HEADER = "[Charger Availability Reports]";

    public FileProcessorTests()
    {
        _processor = new FileProcessor();
        _testFilePath = "test_input.txt";
    }

    private string CreateTestInput(string stations, string reports)
    {
        return $"{STATIONS_HEADER}\n{stations}\n\n{REPORTS_HEADER}\n{reports}";
    }

    private async Task<StationData?> ProcessTestInput(string stations, string reports)
    {
        var input = CreateTestInput(stations, reports);
        await File.WriteAllTextAsync(_testFilePath, input);
        var result = await _processor.ProcessInputFileAsync(_testFilePath);
        return result;
    }

    [Fact]
    public async Task ProcessInputFile_ValidInput_Success()
    {
        var stations = @"1 1 2
2 3 4";

        var reports = @"1 0 100 true
2 50 150 false
3 0 100 true
4 0 100 false";

        var result = await ProcessTestInput(stations, reports);

        Assert.NotNull(result);
        Assert.Equal(2, result.Stations.Count);
        Assert.Equal(4, result.AvailabilityReports.Count);

        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_StationIdOverflow_ReturnsNull()
    {
        var stations = "4294967296 1 2";
        var reports = "1 0 100 true";

        var result = await ProcessTestInput(stations, reports);

        Assert.Null(result);
        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_ChargerIdOverflow_ReturnsNull()
    {
        var stations = "1 4294967296";
        var reports = "4294967296 0 100 true";

        var result = await ProcessTestInput(stations, reports);

        Assert.Null(result);
        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_TimeOverflow_ReturnsNull()
    {
        var stations = "1 1";
        var reports = "1 18446744073709551616 100 true";

        var result = await ProcessTestInput(stations, reports);

        Assert.Null(result);
        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_InvalidBooleanValue_ReturnsNull()
    {
        var stations = "1 1";
        var reports = "1 0 100 maybe";

        var result = await ProcessTestInput(stations, reports);

        Assert.Null(result);
        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_DuplicateStationId_ReturnsNull()
    {
        var stations = @"1 1 2
1 3 4";

        var reports = "1 0 100 true";

        var result = await ProcessTestInput(stations, reports);

        Assert.Null(result);
        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_DuplicateChargerIdInStation_ReturnsNull()
    {
        var stations = "1 1 1";
        var reports = "1 0 100 true";

        var result = await ProcessTestInput(stations, reports);

        Assert.Null(result);
        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_DuplicateChargerIdAcrossStations_ReturnsNull()
    {
        var stations = @"1 1 2
2 1 3";

        var reports = "1 0 100 true";

        var result = await ProcessTestInput(stations, reports);

        Assert.Null(result);
        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_EndTimeBeforeStartTime_ReturnsNull()
    {
        var stations = "1 1";
        var reports = "1 100 50 true";

        var result = await ProcessTestInput(stations, reports);

        Assert.Null(result);
        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_OverlappingTimeRanges_ReturnsNull()
    {
        var stations = @"1 1";

        var reports = @"1 0 100 true
1 50 150 true";

        var result = await ProcessTestInput(stations, reports);

        Assert.Null(result);
        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_AdjacentTimeRanges_Success()
    {
        var stations = @"1 1";

        var reports = @"1 0 100 true
1 100 200 true";

        var result = await ProcessTestInput(stations, reports);

        Assert.NotNull(result);
        Assert.Single(result.Stations);
        Assert.Equal(2, result.AvailabilityReports.Count);

        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_NonExistentChargerId_ReturnsNull()
    {
        var stations = "1 1";
        var reports = "2 0 100 true";

        var result = await ProcessTestInput(stations, reports);

        Assert.Null(result);
        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_NoStations_ReturnsNull()
    {
        var stations = "";
        var reports = "1 0 100 true";

        var result = await ProcessTestInput(stations, reports);

        Assert.Null(result);
        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_NoReports_ReturnsNull()
    {
        var stations = "1 1";
        var reports = "";

        var result = await ProcessTestInput(stations, reports);

        Assert.Null(result);
        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_NonContiguousTimeRanges_Success()
    {
        var stations = @"1 1";

        var reports = @"1 0 50 true
1 100 150 true";

        var result = await ProcessTestInput(stations, reports);

        Assert.NotNull(result);
        Assert.Single(result.Stations);
        Assert.Equal(2, result.AvailabilityReports.Count);

        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_MissingChargerReports_WarningButSuccess()
    {
        var stations = "1 1 2";
        var reports = "1 0 100 true";

        var result = await ProcessTestInput(stations, reports);

        Assert.NotNull(result);
        Assert.Single(result.Stations);
        Assert.Single(result.AvailabilityReports);

        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task ProcessInputFile_InvalidFormat_ReturnsNull()
    {
        var input = "[Invalid Section]\n1 1";
        await File.WriteAllTextAsync(_testFilePath, input);
        var result = await _processor.ProcessInputFileAsync(_testFilePath);

        Assert.Null(result);
        File.Delete(_testFilePath);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
} 