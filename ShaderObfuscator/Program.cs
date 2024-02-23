using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZstdSharp;

namespace ShaderObfuscator
{
    public class Program
    {
        private const string ShaderOutputArrayName = "gShaderPacks";
        private const int COMPRESSION_LEVEL = 22;
        private const int XOR_KEY = 0xA8;

        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: ShaderObfuscator <shader_file_paths> <output_file_path>");
                return;
            }

            List<string> shaderFiles = args.Take(args.Length - 1).ToList();
            string outputFilePath = args[^1];

            StringBuilder arrays = new StringBuilder();
            StringBuilder shaders = new StringBuilder();
            shaders.AppendLine($"static const ShaderPackBytecode {ShaderOutputArrayName}[] = {{");

            for (int i = 0; i < shaderFiles.Count; i++)
            {
                string path = shaderFiles[i];
                string processedShader = ProcessShaderFile(path);

                string encName = Rot13(Path.GetFileNameWithoutExtension(path));
                string arrayName = $"a{i:0000}";

                PrintBuffer(arrays, arrayName, processedShader);
                shaders.AppendLine($"    {{ \"{encName}\", {arrayName}, {processedShader.Length} / 5 }},");
            }

            shaders.AppendLine("};");

            File.WriteAllText(outputFilePath, arrays.ToString() + shaders.ToString());
        }

        private static string ProcessShaderFile(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var compressor = new Compressor(COMPRESSION_LEVEL);
            byte[] compressed = compressor.Wrap(bytes).ToArray();

            Array.Reverse(compressed);

            for (var i = 0; i < compressed.Length; i++)
            {
                compressed[i] ^= XOR_KEY;
            }

            return string.Join(", ", compressed.Select(b => $"0x{b:X2}"));
        }

        private static void PrintBuffer(StringBuilder arrays, string arrayName, string processedShader)
        {
            arrays.AppendLine($"static const unsigned char {arrayName}[] = {{ {processedShader} }};");
        }

        private static string Rot13(string input)
        {
            return new string(input.Select(c => c switch
            {
                >= 'a' and <= 'z' => (char)('a' + (c - 'a' + 13) % 26),
                >= 'A' and <= 'Z' => (char)('A' + (c - 'A' + 13) % 26),
                _ => c
            }).ToArray());
        }
    }
}
