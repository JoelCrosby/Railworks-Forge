using System.Security.Cryptography;
using System.Text;

namespace RailworksForge.Core.Packaging;

public record Package
{
    public required string Name { get; init; }

    public required string Author { get; init; }

    public required Protection Protection { get; init; }

    public required HashSet<string> Assets { get; init; }

    public void SavePackageInfo()
    {
        var filename = Name + ".pi";
        var path = Path.Join(Paths.GetGameDirectory(), "PackageInfo", filename);

        var directory = Path.GetDirectoryName(path);

        if (directory is null)
        {
            throw new Exception($"Could not get directory for path {path}");
        }

        Directory.CreateDirectory(directory);

        using var output = File.OpenWrite(path);
        using var writer = new StreamWriter(output);

        writer.WriteLine(Assets.Count);
        writer.WriteLine(Author);
        writer.WriteLine(Enum.GetName(typeof(Protection), Protection));

        foreach (var asset in Assets)
        {
            var normalised = asset.Replace('/', '\\');
            writer.WriteLine(normalised);
        }

        var hash = CalculateMd5();

        foreach (var num in hash)
        {
            writer.Write(num.ToString("x2"));
        }

        writer.WriteLine();
    }

    private byte[] CalculateMd5()
    {
        const string salt = "Oo Look!  A squirrel!";

        var buffer = new StringBuilder(salt, 200000);

        buffer.Append(this.Name);
        buffer.Append(this.Author);
        buffer.Append(this.Protection.ToString());

        foreach (var path in this.Assets)
        {
            buffer.Append(path);
        }

        return MD5.HashData(new UTF8Encoding().GetBytes(buffer.ToString()));
    }
}
