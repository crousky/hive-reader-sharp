namespace SendToKindle.Models;

public class ConvertRequest
{
    public string Html { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = "Unknown";
    public string SourceUrl { get; set; } = string.Empty;
}

public class ConvertResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FilePath { get; set; }
}
