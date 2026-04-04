/// <summary>
/// 工程发布服务实现文件。
/// 通过调用 dotnet publish CLI 将 HMI 工程打包为目标平台的可执行发布包，
/// 并将工程配置数据随包一同输出。
/// </summary>
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using HMIexe.Core.Models.Project;
using HMIexe.Core.Services;

namespace HMIexe.Runtime.Services;

/// <summary>
/// <see cref="IPublishService"/> 的默认实现。
/// 发布流程：
/// 1. 验证参数并确定输出目录。
/// 2. 将工程 JSON 写入临时目录。
/// 3. 运行 <c>dotnet publish</c> 生成目标平台可执行文件（若 SDK 可用）。
/// 4. 将工程数据文件复制到发布目录，并写入运行时配置文件。
/// 若 SDK 不可用，则退化为仅输出工程数据包（供手工集成）。
/// </summary>
public class PublishService : IPublishService
{
    /// <summary>JSON 序列化选项：缩进输出，方便人工阅读和差异对比。</summary>
    private static readonly JsonSerializerOptions s_jsonOptions =
        new() { WriteIndented = true };

    /// <summary>
    /// 异步发布当前 HMI 工程。
    /// </summary>
    public async Task<string> PublishAsync(
        HmiProject project,
        PublishSettings settings,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // ── 1. 确定输出目录 ──────────────────────────────────────────────
        var outputDir = ResolveOutputDirectory(project, settings);
        Directory.CreateDirectory(outputDir);

        progress?.Report($"发布目录：{outputDir}");

        // ── 2. 序列化工程数据 ────────────────────────────────────────────
        var projectJson = JsonSerializer.Serialize(project, s_jsonOptions);
        var projectDataPath = Path.Combine(outputDir, "project.hmiproj");
        await File.WriteAllTextAsync(projectDataPath, projectJson, Encoding.UTF8, cancellationToken);
        progress?.Report("已写入工程数据文件。");

        // ── 3. 写入运行时配置文件（分辨率、脚本模式等）──────────────────
        var runtimeConfig = new
        {
            Platform        = settings.Platform.ToString(),
            ResolutionWidth  = settings.ResolutionWidth,
            ResolutionHeight = settings.ResolutionHeight,
            ScriptMode       = settings.ScriptMode.ToString(),
            OptimizeResources = settings.OptimizeResources,
            SingleFilePublish = settings.SingleFilePublish,
            ProjectFile      = "project.hmiproj"
        };
        var configPath = Path.Combine(outputDir, "runtime.config.json");
        await File.WriteAllTextAsync(
            configPath,
            JsonSerializer.Serialize(runtimeConfig, s_jsonOptions),
            Encoding.UTF8,
            cancellationToken);
        progress?.Report("已写入运行时配置文件。");

        // ── 4. 若 SDK 可用，执行 dotnet publish ─────────────────────────
        if (await IsDotnetSdkAvailableAsync())
        {
            progress?.Report($"检测到 .NET SDK，开始编译目标平台：{settings.Platform} …");
            await RunDotnetPublishAsync(project, settings, outputDir, progress, cancellationToken);
        }
        else
        {
            // SDK 不可用时退化为纯数据包，并写入说明文档
            progress?.Report("未检测到 .NET SDK，已生成工程数据包（需手工集成运行时）。");
            await WriteReadmeAsync(outputDir, project, settings, cancellationToken);
        }

        progress?.Report("✅ 发布完成！");
        return outputDir;
    }

    /// <summary>
    /// 检查当前环境是否安装了 .NET SDK（dotnet CLI）。
    /// </summary>
    public async Task<bool> IsDotnetSdkAvailableAsync()
    {
        try
        {
            var psi = new ProcessStartInfo("dotnet", "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return false;
            await proc.WaitForExitAsync();
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    // ── 私有辅助方法 ─────────────────────────────────────────────────────

    /// <summary>
    /// 根据发布设置确定输出目录路径：
    /// 若 <see cref="PublishSettings.OutputDirectory"/> 已指定，则直接使用；
    /// 否则在系统桌面下创建 <c>HMI_Publish/{工程名}_{平台}</c> 子目录。
    /// </summary>
    private static string ResolveOutputDirectory(HmiProject project, PublishSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.OutputDirectory))
            return settings.OutputDirectory;

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var dirName = $"HMI_Publish_{SanitizeName(project.Name)}_{settings.Platform}";
        return Path.Combine(desktop, dirName);
    }

    /// <summary>
    /// 运行 <c>dotnet publish</c> 命令，将应用程序编译到目标运行时。
    /// 标准输出/错误均通过 <paramref name="progress"/> 实时转发。
    /// </summary>
    private static async Task RunDotnetPublishAsync(
        HmiProject project,
        PublishSettings settings,
        string outputDir,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        // 定位 HMIexe.App.csproj（从当前程序集所在目录向上查找）
        var appCsproj = FindAppCsproj();
        if (appCsproj == null)
        {
            progress?.Report("⚠ 未能找到 HMIexe.App.csproj，跳过 dotnet publish。");
            return;
        }

        var rid         = settings.ToRuntimeIdentifier();
        var config      = settings.ScriptMode == ScriptMode.Debug ? "Debug" : "Release";
        var singleFile  = settings.SingleFilePublish ? " -p:PublishSingleFile=true" : string.Empty;
        var selfContain = " --self-contained true";
        var resArgs     = $" -p:HmiResolutionWidth={settings.ResolutionWidth}" +
                          $" -p:HmiResolutionHeight={settings.ResolutionHeight}";

        var args = $"publish \"{appCsproj}\" -r {rid} -c {config}" +
                   $" -o \"{outputDir}\"{singleFile}{selfContain}{resArgs}" +
                   " --nologo";

        progress?.Report($"执行：dotnet {args}");

        var psi = new ProcessStartInfo("dotnet", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using var proc = new Process { StartInfo = psi };
        var outputSb = new StringBuilder();

        proc.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputSb.AppendLine(e.Data);
                progress?.Report(e.Data);
            }
        };
        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputSb.AppendLine(e.Data);
                progress?.Report($"[stderr] {e.Data}");
            }
        };

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        await proc.WaitForExitAsync(cancellationToken);

        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet publish 失败（退出码 {proc.ExitCode}）：\n{outputSb}");
        }

        progress?.Report($"dotnet publish 完成（配置：{config}，RID：{rid}）。");
    }

    /// <summary>
    /// 从当前程序集所在目录向上最多遍历 8 层，寻找 <c>HMIexe.App.csproj</c>。
    /// </summary>
    private static string? FindAppCsproj()
    {
        var dir = Path.GetDirectoryName(typeof(PublishService).Assembly.Location);
        for (var i = 0; i < 8 && dir != null; i++)
        {
            var candidate = Directory.GetFiles(dir, "HMIexe.App.csproj", SearchOption.AllDirectories)
                                     .FirstOrDefault();
            if (candidate != null) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    /// <summary>
    /// 当 SDK 不可用时，写入一份 README.txt 说明如何手动构建和运行工程包。
    /// </summary>
    private static async Task WriteReadmeAsync(
        string outputDir,
        HmiProject project,
        PublishSettings settings,
        CancellationToken cancellationToken)
    {
        var rid        = settings.ToRuntimeIdentifier();
        var config     = settings.ScriptMode == ScriptMode.Debug ? "Debug" : "Release";
        var singleFile = settings.SingleFilePublish ? " -p:PublishSingleFile=true" : string.Empty;
        var readme = $"""
            HMI 工程发布包 — {project.Name}
            ======================================
            目标平台   : {settings.Platform} ({rid})
            分辨率     : {settings.ResolutionWidth} × {settings.ResolutionHeight}
            脚本模式   : {settings.ScriptMode}
            资源优化   : {(settings.OptimizeResources ? "启用" : "禁用")}
            单文件发布 : {(settings.SingleFilePublish ? "是" : "否")}

            此包因未检测到 .NET SDK 而未执行编译，仅包含工程数据文件。
            如需生成可执行文件，请在安装了 .NET 8 SDK 的环境中执行：

              dotnet publish src/HMIexe.App/HMIexe.App.csproj \
                -r {rid} -c {config}{singleFile} \
                --self-contained true \
                -o <输出目录>

            然后将本目录中的 project.hmiproj 和 runtime.config.json
            复制到输出目录中，启动 HMIexe.App{settings.ExecutableExtension()} 即可运行。
            """;

        await File.WriteAllTextAsync(
            Path.Combine(outputDir, "README.txt"),
            readme,
            Encoding.UTF8,
            cancellationToken);
    }

    /// <summary>
    /// 移除文件/目录名称中不允许的字符。
    /// </summary>
    private static string SanitizeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
