using ChargerUptime.Models;
using ChargerUptime.Services;
using Xunit;

namespace ChargerUptime.Tests;

public class UptimeCalculatorTests
{
    private readonly UptimeCalculator _calculator;

    public UptimeCalculatorTests()
    {
        _calculator = new UptimeCalculator();
    }

    [Fact]
    public void CalculateStationUptimes_SingleCharger_FullUptime()
    {
        var stationData = new StationData
        {
            Stations = new List<Station>
            {
                new() { StationId = 1, ChargerIds = new List<uint> { 1 } }
            },
            AvailabilityReports = new List<ChargerAvailabilityReport>
            {
                new() { ChargerId = 1, StartTimeNanos = 0, EndTimeNanos = 100, IsUp = true }
            }
        };

        var results = _calculator.CalculateStationUptimes(stationData).ToList();

        Assert.Single(results);
        Assert.Equal(1u, results[0].StationId);
        Assert.Equal(100, results[0].UptimePercentage);
    }

    [Fact]
    public void CalculateStationUptimes_SingleCharger_PartialUptime()
    {
        var stationData = new StationData
        {
            Stations = new List<Station>
            {
                new() { StationId = 1, ChargerIds = new List<uint> { 1 } }
            },
            AvailabilityReports = new List<ChargerAvailabilityReport>
            {
                new() { ChargerId = 1, StartTimeNanos = 0, EndTimeNanos = 100, IsUp = true },
                new() { ChargerId = 1, StartTimeNanos = 100, EndTimeNanos = 200, IsUp = false }
            }
        };

        var results = _calculator.CalculateStationUptimes(stationData).ToList();

        Assert.Single(results);
        Assert.Equal(1u, results[0].StationId);
        Assert.Equal(50, results[0].UptimePercentage);
    }

    [Fact]
    public void CalculateStationUptimes_MultipleChargers_OverlappingUptime()
    {
        var stationData = new StationData
        {
            Stations = new List<Station>
            {
                new() { StationId = 1, ChargerIds = new List<uint> { 1, 2 } }
            },
            AvailabilityReports = new List<ChargerAvailabilityReport>
            {
                new() { ChargerId = 1, StartTimeNanos = 0, EndTimeNanos = 100, IsUp = true },
                new() { ChargerId = 2, StartTimeNanos = 50, EndTimeNanos = 150, IsUp = true }
            }
        };

        var results = _calculator.CalculateStationUptimes(stationData).ToList();

        Assert.Single(results);
        Assert.Equal(1u, results[0].StationId);
        Assert.Equal(100, results[0].UptimePercentage);
    }

    [Fact]
    public void CalculateStationUptimes_MultipleStations_DifferentUptimes()
    {
        var stationData = new StationData
        {
            Stations = new List<Station>
            {
                new() { StationId = 1, ChargerIds = new List<uint> { 1 } },
                new() { StationId = 2, ChargerIds = new List<uint> { 2 } }
            },
            AvailabilityReports = new List<ChargerAvailabilityReport>
            {
                new() { ChargerId = 1, StartTimeNanos = 0, EndTimeNanos = 100, IsUp = true },
                new() { ChargerId = 2, StartTimeNanos = 0, EndTimeNanos = 100, IsUp = false }
            }
        };

        var results = _calculator.CalculateStationUptimes(stationData).OrderBy(r => r.StationId).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal(1u, results[0].StationId);
        Assert.Equal(100, results[0].UptimePercentage);
        Assert.Equal(2u, results[1].StationId);
        Assert.Equal(0, results[1].UptimePercentage);
    }

    [Fact]
    public void CalculateStationUptimes_TimeGaps_CountedAsDowntime()
    {
        var stationData = new StationData
        {
            Stations = new List<Station>
            {
                new() { StationId = 1, ChargerIds = new List<uint> { 1 } }
            },
            AvailabilityReports = new List<ChargerAvailabilityReport>
            {
                new() { ChargerId = 1, StartTimeNanos = 0, EndTimeNanos = 50, IsUp = true },
                new() { ChargerId = 1, StartTimeNanos = 100, EndTimeNanos = 150, IsUp = true }
            }
        };

        var results = _calculator.CalculateStationUptimes(stationData).ToList();

        Assert.Single(results);
        Assert.Equal(1u, results[0].StationId);
        Assert.Equal(66, results[0].UptimePercentage); 
    }
} 