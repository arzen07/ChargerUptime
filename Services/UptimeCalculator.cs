using ChargerUptime.Models;

namespace ChargerUptime.Services;

public class UptimeCalculator
{
    public IEnumerable<StationUptimeResult> CalculateStationUptimes(StationData stationData)
    {
        return stationData.Stations
            .OrderBy(s => s.StationId)
            .Select(station =>
            {
                var stationReports = stationData.AvailabilityReports
                    .Where(r => station.ChargerIds.Contains(r.ChargerId))
                    .OrderBy(r => r.StartTimeNanos)
                    .ToList();

                return new StationUptimeResult
                {
                    StationId = station.StationId,
                    UptimePercentage = CalculateStationUptime(stationReports, station.ChargerIds)
                };
            });
    }

    private int CalculateStationUptime(List<ChargerAvailabilityReport> reports, List<uint> chargerIds)
    {
        if (reports.Count == 0) return 0;

        // Calculate uptime for each charger
        var chargerUptimes = new List<int>();

        foreach (var chargerId in chargerIds)
        {
            var chargerReports = reports.Where(r => r.ChargerId == chargerId).OrderBy(r => r.StartTimeNanos).ToList();
            if (chargerReports.Count == 0) continue;

            // Get first and last time for this charger
            var firstTime = chargerReports.First().StartTimeNanos;
            var lastTime = chargerReports.Last().EndTimeNanos;
            var totalTime = lastTime - firstTime;

            // Sum up all UP times
            var uptime = 0UL;
            foreach (var report in chargerReports)
            {
                if (report.IsUp)
                {
                    uptime += report.EndTimeNanos - report.StartTimeNanos;
                }
            }

            // Calculate percentage for this charger
            if (totalTime > 0)
            {
                decimal percentage = (decimal)uptime * 100M / (decimal)totalTime;
                chargerUptimes.Add((int)Math.Floor(percentage));
            }
        }

        // Return average uptime across all chargers
        return chargerUptimes.Count > 0 ? chargerUptimes.Sum() / chargerUptimes.Count : 0;
    }
} 