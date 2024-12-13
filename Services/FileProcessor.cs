using ChargerUptime.Models;

namespace ChargerUptime.Services;

public class FileProcessor
{
    private const string StationsHeader = "[Stations]";
    private const string ReportsHeader = "[Charger Availability Reports]";

    public async Task<StationData?> ProcessInputFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine("Input file not found.");
            return null;
        }

        var stationData = new StationData();
        var lines = await File.ReadAllLinesAsync(filePath);
        var currentSection = "";
        var lineNumber = 0;
        
        foreach (var line in lines)
        {
            lineNumber++;
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine)) continue;

            if (trimmedLine == StationsHeader)
            {
                currentSection = StationsHeader;
                continue;
            }
            else if (trimmedLine == ReportsHeader)
            {
                currentSection = ReportsHeader;
                continue;
            }

            if (currentSection == StationsHeader)
            {
                var (station, error) = ParseStationLine(trimmedLine, lineNumber);
                if (station != null)
                {
                    stationData.Stations.Add(station);
                }
                else
                {
                    Console.Error.WriteLine(error);
                    return null;
                }
            }
            else if (currentSection == ReportsHeader)
            {
                var (report, error) = ParseReportLine(trimmedLine, lineNumber);
                if (report != null)
                {
                    stationData.AvailabilityReports.Add(report);
                }
                else
                {
                    Console.Error.WriteLine(error);
                    return null;
                }
            }
        }

        var validationError = ValidateStationData(stationData);
        if (!string.IsNullOrEmpty(validationError))
        {
            Console.Error.WriteLine(validationError);
            return null;
        }

        return stationData;
    }

    private (Station? station, string? error) ParseStationLine(string line, int lineNumber)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return (null, $"Line {lineNumber}: Invalid station format. Expected: <Station ID> <Charger ID 1> [Charger ID 2...]");

        // Validate Station ID (unsigned 32-bit integer)
        if (!uint.TryParse(parts[0], out uint stationId))
            return (null, $"Line {lineNumber}: Station ID must be an unsigned 32-bit integer (0 to 4294967295)");

        var station = new Station { StationId = stationId };
        var seenChargerIds = new HashSet<uint>();
        
        // Validate Charger IDs
        for (int i = 1; i < parts.Length; i++)
        {
            if (!uint.TryParse(parts[i], out uint chargerId))
                return (null, $"Line {lineNumber}: Charger ID must be an unsigned 32-bit integer (0 to 4294967295)");

            if (!seenChargerIds.Add(chargerId))
                return (null, $"Line {lineNumber}: Duplicate Charger ID {chargerId} found in station {stationId}");

            station.ChargerIds.Add(chargerId);
        }

        return (station, null);
    }

    private (ChargerAvailabilityReport? report, string? error) ParseReportLine(string line, int lineNumber)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
            return (null, $"Line {lineNumber}: Invalid report format. Expected: <Charger ID> <start time nanos> <end time nanos> <up (true/false)>");

        // Validate Charger ID
        if (!uint.TryParse(parts[0], out uint chargerId))
            return (null, $"Line {lineNumber}: Charger ID must be an unsigned 32-bit integer (0 to 4294967295)");

        // Validate start time
        if (!ulong.TryParse(parts[1], out ulong startTime))
            return (null, $"Line {lineNumber}: Start time must be an unsigned 64-bit integer (0 to 18446744073709551615)");

        // Validate end time
        if (!ulong.TryParse(parts[2], out ulong endTime))
            return (null, $"Line {lineNumber}: End time must be an unsigned 64-bit integer (0 to 18446744073709551615)");

        // Validate up status
        if (!bool.TryParse(parts[3], out bool isUp))
            return (null, $"Line {lineNumber}: Up status must be 'true' or 'false'");

        // Validate time range
        if (endTime < startTime)
            return (null, $"Line {lineNumber}: End time ({endTime}) must be greater than or equal to start time ({startTime})");

        return (new ChargerAvailabilityReport
        {
            ChargerId = chargerId,
            StartTimeNanos = startTime,
            EndTimeNanos = endTime,
            IsUp = isUp
        }, null);
    }

    private string? ValidateStationData(StationData data)
    {
        if (data.Stations.Count == 0)
            return "No stations found in input file.";
            
        if (data.AvailabilityReports.Count == 0)
            return "No availability reports found in input file.";

        // Check for unique station IDs
        var stationIds = new HashSet<uint>();
        foreach (var station in data.Stations)
        {
            if (!stationIds.Add(station.StationId))
                return $"Duplicate Station ID found: {station.StationId}";
        }

        // Check for unique charger IDs across all stations
        var allChargerIds = new HashSet<uint>();
        foreach (var station in data.Stations)
        {
            foreach (var chargerId in station.ChargerIds)
            {
                if (!allChargerIds.Add(chargerId))
                    return $"Duplicate Charger ID found across stations: {chargerId}";
            }
        }

        // Check for missing reports and overlapping time periods
        var reportsByCharger = data.AvailabilityReports.GroupBy(r => r.ChargerId).ToDictionary(g => g.Key, g => g.ToList());
        
        // Check for chargers without reports
        foreach (var chargerId in allChargerIds)
        {
            if (!reportsByCharger.ContainsKey(chargerId))
            {
                Console.WriteLine($"Warning: No availability reports found for Charger ID {chargerId}");
                continue;
            }

            var chargerReports = reportsByCharger[chargerId].OrderBy(r => r.StartTimeNanos).ToList();
            
            // Check for overlapping time periods
            if (HasOverlappingPeriods(chargerReports))
            {
                return $"Overlapping time periods found for Charger ID {chargerId}";
            }
        }

        // Check for reports with non-existent charger IDs
        foreach (var report in data.AvailabilityReports)
        {
            if (!allChargerIds.Contains(report.ChargerId))
                return $"Report found for non-existent Charger ID: {report.ChargerId}";
        }

        return null;
    }

    private bool HasOverlappingPeriods(List<ChargerAvailabilityReport> reports)
    {
        var sortedReports = reports.OrderBy(r => r.StartTimeNanos).ToList();

        for (int i = 0; i < sortedReports.Count - 1; i++)
        {
            if (sortedReports[i].EndTimeNanos > sortedReports[i + 1].StartTimeNanos)
            {
                return true;
            }
        }

        return false;
    }
} 