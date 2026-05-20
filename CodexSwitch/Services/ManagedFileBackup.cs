namespace CodexSwitch.Services;

internal static class ManagedFileBackup
{
    public static string GetBackupPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return path + ".bak";
    }

    public static bool HasBackup(string path)
    {
        return File.Exists(GetBackupPath(path));
    }

    public static void EnsureBackedUp(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var backupPath = GetBackupPath(path);
        if (File.Exists(backupPath) || !File.Exists(path))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.Move(path, backupPath);
    }

    public static void RestoreOriginal(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var backupPath = GetBackupPath(path);
        if (File.Exists(backupPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.Move(backupPath, path, overwrite: true);
            return;
        }

        if (File.Exists(path))
            File.Delete(path);
    }

    public static void CopyBackupToOriginal(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var backupPath = GetBackupPath(path);
        if (File.Exists(backupPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.Copy(backupPath, path, overwrite: true);
            return;
        }

        if (File.Exists(path))
            File.Delete(path);
    }
}
