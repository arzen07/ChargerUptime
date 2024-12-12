namespace ChargerUptime.Models;

public class Station
{
    public uint StationId { get; set; }
    public List<uint> ChargerIds { get; set; } = new();
}

public class ChargerAvailabilityReport
{
    public uint ChargerId { get; set; }
    public ulong StartTimeNanos { get; set; }
    public ulong EndTimeNanos { get; set; }
    public bool IsUp { get; set; }
}

public class StationData
{
    public List<Station> Stations { get; set; } = new();
    public List<ChargerAvailabilityReport> AvailabilityReports { get; set; } = new();
}

public class StationUptimeResult
{
    public uint StationId { get; set; }
    public int UptimePercentage { get; set; }
} 