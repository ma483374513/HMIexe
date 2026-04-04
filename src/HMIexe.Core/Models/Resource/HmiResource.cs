/// <summary>
/// HMI 资源文件模型。
/// 定义资源类型枚举及资源元数据，用于管理项目引用的图片、音视频、字体、SVG 等外部文件。
/// </summary>
namespace HMIexe.Core.Models.Resource;

/// <summary>
/// HMI 资源文件类型枚举。
/// 标识资源文件的媒体类型，用于在资源管理器中分类展示和过滤。
/// </summary>
public enum ResourceType
{
    /// <summary>图片资源（PNG、JPG、BMP、GIF 等）。</summary>
    Image,
    /// <summary>音频资源（MP3、WAV 等）。</summary>
    Audio,
    /// <summary>视频资源（MP4、AVI 等）。</summary>
    Video,
    /// <summary>字体资源（TTF、OTF、WOFF 等）。</summary>
    Font,
    /// <summary>可缩放矢量图形资源（SVG）。</summary>
    Svg,
    /// <summary>其他类型资源，不属于以上分类的文件。</summary>
    Other
}

/// <summary>
/// HMI 资源元数据，描述项目中导入的一个外部资源文件的基本信息。
/// 资源通过 <see cref="UsedByControlIds"/> 追踪哪些控件正在引用该资源，便于清理未使用资源。
/// </summary>
public class HmiResource
{
    /// <summary>资源的唯一标识符（GUID）。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>资源名称，在项目中用于标识和引用该资源，应在项目内唯一。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>资源文件的媒体类型（图片、音频、视频等）。</summary>
    public ResourceType Type { get; set; }

    /// <summary>资源文件在磁盘上的绝对路径或相对路径。</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>资源文件的大小（字节）。</summary>
    public long FileSize { get; set; }

    /// <summary>资源的描述信息，便于设计者了解该资源的用途。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>资源导入到项目的时间戳。</summary>
    public DateTime ImportedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 引用此资源的控件 ID 列表。
    /// 用于追踪资源使用情况，在删除资源前可检查此列表以避免断链。
    /// </summary>
    public List<string> UsedByControlIds { get; set; } = new();
}
