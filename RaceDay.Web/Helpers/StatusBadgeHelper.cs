namespace RaceDay.Web.Helpers;

public static class StatusBadgeHelper
{
    // Returns the appropriate Bootstrap badge CSS class based on the status
    public static string GetBadgeClass(string status)
    {
        return status?.ToLower() switch
        {
            "confirmed" => "bg-success text-white", // Green
            "pending" => "bg-warning text-dark",    // Amber / Yellow
            "completed" => "bg-info text-white",     // Blue
            _ => "bg-secondary text-white"
        };
    }
}