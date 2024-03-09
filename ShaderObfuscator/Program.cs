using System.Text;
using ZstdSharp;

namespace ShaderObfuscator
{
    struct ShaderPackBytecode
    {
        public byte[] Bytecode;
        public int DataSize;
    }

    public class Program
    {
        private const int COMPRESSION_LEVEL = 22;
        private const long XOR_KEY = 0x5CAEC09A6E750D6C;

        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: ShaderObfuscator <shader_folder_path> <output_file_path>");
                return;
            }

            string folder = args[0];
            if (!Directory.Exists(folder))
            {
                Console.WriteLine("Folder does not exist");
                return;
            }

            string[] shaderFiles = Directory.GetFiles(folder, "*.pack");
            if (shaderFiles.Length != 4)
            {
                Console.WriteLine("Folder does not contain the required files");
                return;
            }

            if (!shaderFiles.Contains(Path.Combine(folder, "shaders_glsl3.pack")) ||
                !shaderFiles.Contains(Path.Combine(folder, "shaders_glsles3.pack")) ||
                !shaderFiles.Contains(Path.Combine(folder, "classic_shaders_glsl3.pack")) ||
                !shaderFiles.Contains(Path.Combine(folder, "classic_shaders_glsles3.pack"))
            )
            {
                Console.WriteLine("Folder does not contain the required files");
                return;
            }

            string outputFilePath = args[1];

            StringBuilder arrays = new();
            StringBuilder dictionary = new();

            dictionary.AppendLine("static const ShaderPackBytecode gShaderPacks[] = {");

            string[] shaderNames = ["shaders_glsl3.pack", "shaders_glsles3.pack", "classic_shaders_glsl3.pack", "classic_shaders_glsles3.pack"];

            for (int i = 0; i < shaderNames.Length; i++)
            {
                string shaderName = shaderNames[i];
                string path = shaderFiles.First(file => Path.GetFileName(file) == shaderName);
                ShaderPackBytecode shader = ObfuscateShaderPack(path);

                string arrayName = $"a{i:0000}";

                PrintBuffer(arrays, arrayName, string.Join(", ", shader.Bytecode.Select(b => $"0x{b:X2}")));
                dictionary.AppendLine($"\t{{ {arrayName}, {shader.DataSize} }},");
            }

            dictionary.AppendLine("};");

            File.WriteAllText(outputFilePath, arrays.ToString() + dictionary.ToString());
        }

        private static ShaderPackBytecode ObfuscateShaderPack(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);

            using var compressor = new Compressor(COMPRESSION_LEVEL);
            byte[] compressed = compressor.Wrap(bytes).ToArray();

            byte[] key = BitConverter.GetBytes(XOR_KEY);
            byte[] encrypted = new byte[compressed.Length];

            for (int i = 0; i < compressed.Length; i++)
            {
                encrypted[i] = (byte)(compressed[i] ^ key[i % key.Length]);
            }

            Array.Reverse(encrypted);

            return new ShaderPackBytecode
            {
                Bytecode = encrypted,
                DataSize = encrypted.Length
            };
        }

        private static void PrintBuffer(StringBuilder arrays, string arrayName, string processedShader)
        {
            arrays.AppendLine($"static const unsigned char {arrayName}[] = {{ {processedShader} }};");
        }
    }
}
