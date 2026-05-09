namespace CRAnalyzer.Core.Interfaces;

public interface IRepositoryScannerService
{
    Task<string> ScanFolderAsync(string folderPath);
    Task<string> ScanZipAsync(Stream zipStream);
}
