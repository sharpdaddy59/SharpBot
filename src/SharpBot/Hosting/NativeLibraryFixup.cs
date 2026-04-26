namespace SharpBot.Hosting;

/// <summary>
/// Workaround for a single-file-publish quirk on Linux: the .NET self-extracting
/// bundle places LLamaSharp's native .so files into the extract directory but
/// doesn't create the versioned SONAME symlinks (e.g. <c>libggml.so.0 → libggml.so</c>)
/// that the Linux dynamic linker needs to resolve cross-library dependencies.
///
/// Without this, <c>libllama.so</c> internally tries to <c>dlopen libggml.so.0</c>
/// and fails, even though <c>libggml.so</c> is sitting right next to it. We detect
/// the extracted native dirs at startup and create the missing symlinks ourselves.
///
/// Idempotent — safe to call every launch. No-op on platforms that don't need it
/// (Windows, macOS) or when the symlinks already exist.
/// </summary>
internal static class NativeLibraryFixup
{
    // Libraries that LLamaSharp's native chain expects via SONAME on Linux.
    // Identified empirically when the v0.4.1 linux-x64 archive failed to load on
    // a real Ryzen 5 / Ubuntu 24.04 box.
    private static readonly string[] LibBaseNames =
    {
        "libggml", "libggml-base", "libggml-cpu", "libllama", "libmtmd",
    };

    public static void EnsureLinuxSonames()
    {
        if (!OperatingSystem.IsLinux()) return;

        // Single-file with native extraction: AppContext.BaseDirectory points at
        // the extract root, which contains the runtimes/ tree.
        var nativeRoot = Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native");
        if (!Directory.Exists(nativeRoot)) return;

        // The extract dir contains variant subdirs (avx2, avx512, noavx, ...) — we don't
        // know which one the runtime will pick at load time, so cover all of them.
        foreach (var variantDir in Directory.EnumerateDirectories(nativeRoot))
        {
            CreateSonames(variantDir);
        }
    }

    internal static int CreateSonames(string variantDir)
    {
        var created = 0;
        foreach (var baseName in LibBaseNames)
        {
            var unversioned = Path.Combine(variantDir, $"{baseName}.so");
            var versioned = Path.Combine(variantDir, $"{baseName}.so.0");
            if (!File.Exists(unversioned)) continue;
            if (Path.Exists(versioned)) continue;

            try
            {
                File.CreateSymbolicLink(versioned, $"{baseName}.so");
                created++;
            }
            catch
            {
                // Read-only filesystem, missing privileges, race with another instance.
                // Best-effort: if we can't create the link, the user will see the same
                // load failure they would have without us trying.
            }
        }
        return created;
    }
}
