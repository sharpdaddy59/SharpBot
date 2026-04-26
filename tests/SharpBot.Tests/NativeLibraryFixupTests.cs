using SharpBot.Hosting;

namespace SharpBot.Tests;

public class NativeLibraryFixupTests
{
    [Fact]
    public void Creates_missing_sonames_for_present_libraries()
    {
        if (!CanCreateSymlinks()) return;

        var dir = Path.Combine(Path.GetTempPath(), "sharpbot-fixup-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "libggml.so"), "fake");
            File.WriteAllText(Path.Combine(dir, "libllama.so"), "fake");

            var created = NativeLibraryFixup.CreateSonames(dir);

            Assert.Equal(2, created);
            Assert.True(File.Exists(Path.Combine(dir, "libggml.so.0")));
            Assert.True(File.Exists(Path.Combine(dir, "libllama.so.0")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Skips_libraries_that_arent_present()
    {
        if (!CanCreateSymlinks()) return;

        var dir = Path.Combine(Path.GetTempPath(), "sharpbot-fixup-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            // Only one of the known libs is present.
            File.WriteAllText(Path.Combine(dir, "libllama.so"), "fake");

            var created = NativeLibraryFixup.CreateSonames(dir);

            Assert.Equal(1, created);
            Assert.False(File.Exists(Path.Combine(dir, "libggml.so.0")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Idempotent_when_sonames_already_exist()
    {
        if (!CanCreateSymlinks()) return;

        var dir = Path.Combine(Path.GetTempPath(), "sharpbot-fixup-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "libggml.so"), "fake");

            var firstPass = NativeLibraryFixup.CreateSonames(dir);
            var secondPass = NativeLibraryFixup.CreateSonames(dir);

            Assert.Equal(1, firstPass);
            Assert.Equal(0, secondPass);
            Assert.True(File.Exists(Path.Combine(dir, "libggml.so.0")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Returns_zero_when_directory_has_no_known_libs()
    {
        if (!CanCreateSymlinks()) return;

        var dir = Path.Combine(Path.GetTempPath(), "sharpbot-fixup-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "unrelated.so"), "fake");

            var created = NativeLibraryFixup.CreateSonames(dir);

            Assert.Equal(0, created);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// Symlink creation requires developer mode or admin on Windows; on Linux/macOS it
    /// just works. Skip the body of these tests if we can't even create one as a sanity
    /// check — beats false positives in restricted CI environments.
    /// </summary>
    private static bool CanCreateSymlinks()
    {
        var probe = Path.Combine(Path.GetTempPath(), "sharpbot-symlink-probe-" + Guid.NewGuid().ToString("N")[..8]);
        var target = probe + ".target";
        try
        {
            File.WriteAllText(target, "x");
            File.CreateSymbolicLink(probe, target);
            File.Delete(probe);
            File.Delete(target);
            return true;
        }
        catch
        {
            try { if (File.Exists(target)) File.Delete(target); } catch { }
            return false;
        }
    }
}
