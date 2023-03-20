using System.IO;
using System.Linq;
using SoulsFormats;

namespace ERClipGeneratorTool.ViewModels.Interactions;

public class FileSource
{
    public FileSource(string filePath)
    {
        FilePath = filePath;
    }

    public string? BndFileName { get; set; }
    public string FilePath { get; }

    /// <summary>
    /// Contains both the file path and the bnd file name
    /// </summary>
    public string DisplayPath => Path.Join(FilePath, Path.GetFileName(BndFileName));

    public Stream GetReadStream()
    {
        if (BndFileName is null)
        {
            return File.OpenRead(FilePath);
        }

        BND4 bnd = BND4.Read(FilePath);
        byte[] data = bnd.Files.Single(x => x.Name == BndFileName).Bytes;
        return new MemoryStream(data);
    }

    public void Write(byte[] data)
    {
        if (BndFileName is null)
        {
            File.WriteAllBytes(FilePath, data);
            return;
        }

        BND4 bnd = BND4.Read(FilePath);
        BinderFile file = bnd.Files.Single(x => x.Name == BndFileName);
        file.Bytes = data;
        bnd.Write(FilePath);
    }
}