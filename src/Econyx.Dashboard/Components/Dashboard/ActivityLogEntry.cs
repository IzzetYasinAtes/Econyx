namespace Econyx.Dashboard.Components.Dashboard;

public sealed record ActivityLogEntry(DateTime Timestamp, string Message, string Color = "#e6edf3");
