namespace backend.Services.Files;

public interface IFileUtil
{
    bool DirectoryExists(string? path);
    bool FileExists(string? path);
    Task WriteAllTextAsync(string filePath, string? content);
    DirectoryInfo CreateDirectory(string path);
    Task<string> ReadAllTextAsync(string filePath);
}

public class FileUtil : IFileUtil
{
    public DirectoryInfo CreateDirectory(string path) => Directory.CreateDirectory(path);

    public bool DirectoryExists(string? path) => Directory.Exists(path);

    public bool FileExists(string? path) => File.Exists(path);

    public async Task<string> ReadAllTextAsync(string filePath) =>
        await File.ReadAllTextAsync(filePath);

    public async Task WriteAllTextAsync(string filePath, string? content) =>
        await File.WriteAllTextAsync(filePath, content);
}
