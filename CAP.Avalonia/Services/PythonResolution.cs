using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CAP_Core.Export;

namespace CAP.Avalonia.Services;

/// <summary>
/// Shared Python-interpreter resolution used by features that shell out to Python
/// (PDK/Nazca preview, mode solver). Kept in one place so each feature's DI
/// registration doesn't reinvent (or drift on) interpreter discovery. This is
/// deliberately a small shared kernel — not feature-specific logic.
/// </summary>
public static class PythonResolution
{
    /// <summary>
    /// Validates a user-saved custom Python path: a real interpreter file is trusted
    /// as-is; a bare command on Windows is rejected (may be a Store execution-alias
    /// stub whose <see cref="Process.Start(ProcessStartInfo)"/> blocks indefinitely);
    /// otherwise it's probed for an importable nazca. Returns null when unusable.
    /// </summary>
    public static string? ValidatedNazcaPython(string? customPath)
    {
        if (string.IsNullOrWhiteSpace(customPath))
            return null;

        if (File.Exists(customPath))
            return customPath;

        if (OperatingSystem.IsWindows())
            return null;
        try
        {
            var discovery = new PythonDiscoveryService();
            // Task.Run avoids a UI-thread deadlock — see DiscoverNazcaPython.
            var info = Task.Run(() => discovery.CheckPythonInstallation(customPath, "Custom")).GetAwaiter().GetResult();
            return info?.HasNazca == true ? customPath : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Discovers the first interpreter that can <c>import nazca</c> via
    /// <see cref="PythonDiscoveryService"/>, or null when none is found.
    /// </summary>
    public static string? DiscoverNazcaPython()
    {
        try
        {
            var discovery = new PythonDiscoveryService();
            // Task.Run offloads to the thread pool. Calling .Result on the UI thread would
            // deadlock: awaits inside capture the UI SynchronizationContext, but that thread
            // is blocked here. FindFirst… short-circuits at the first nazca-capable interpreter.
            return Task.Run(() => discovery.FindFirstNazcaPythonPathAsync()).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Picks the first runnable Python interpreter from a per-platform candidate list.
    /// Naive PATH probe — the final fallback when no validated/discovered path exists.
    /// </summary>
    public static string ResolvePythonExecutable()
    {
        var candidates = OperatingSystem.IsWindows()
            ? new[] { "python", "py", "python3" }
            : new[] { "python3", "python" };

        foreach (var candidate in candidates)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                if (p == null) continue;
                p.WaitForExit(2000);
                if (p.ExitCode == 0) return candidate;
            }
            catch (Win32Exception) { }
            catch (Exception) { }
        }

        return OperatingSystem.IsWindows() ? "python" : "python3";
    }

    /// <summary>
    /// Resolves a script under a <c>scripts/</c> folder: first next to the binary
    /// (publish layout), then by walking up the directory tree. Returns the primary
    /// candidate path even if missing, so callers can surface a graceful error.
    /// </summary>
    public static string FindScript(params string[] relativeUnderScripts)
    {
        var relative = Path.Combine(new[] { "scripts" }.Concat(relativeUnderScripts).ToArray());
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        var local = Path.Combine(baseDir, relative);
        if (File.Exists(local)) return local;

        var current = new DirectoryInfo(baseDir);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, relative);
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }
        return local;
    }
}
