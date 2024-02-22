using ZstdSharp;

namespace ShaderObfuscator;

public class Program
{
    private const int COMPRESSION_LEVEL = 22;
    private const int XOR_KEY = 0xA8;

    public static void Main(string[] args)
    {
        using var compressor = new Compressor(COMPRESSION_LEVEL);
        string output = "";

        foreach (string path in args)
        {
            if (path == args[^1])
            {
                break;
            }

            string filename = Path.GetFileNameWithoutExtension(path).Replace("shaders_", "");

            byte[] bytes = File.ReadAllBytes(path);
            bytes = bytes[4..]; // strip RBXS magic header

            byte[] compressed = compressor.Wrap(bytes).ToArray();

            Array.Reverse(compressed);

            for (var i = 0; i < compressed.Length; i++)
            {
                compressed[i] ^= XOR_KEY;
            }

            string pack = string.Join(", ", compressed.Select(b => $"0x{b:X2}"));
            output += $"static const unsigned char {filename}[] = {{ {pack} }};\n";
        }

        output = output.Substring(0, output.Length - 1);

        File.WriteAllText(args[^1], output);
    }
}