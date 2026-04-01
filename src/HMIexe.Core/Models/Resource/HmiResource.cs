namespace HMIexe.Core.Models.Resource;

public enum ResourceType { Image, Audio, Video, Font, Svg, Other }

public class HmiResource
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public ResourceType Type { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; } = DateTime.Now;
    public List<string> UsedByControlIds { get; set; } = new();
}
