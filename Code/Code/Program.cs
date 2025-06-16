using System;
using System.Collections.Generic;
using System.IO;

namespace Code
{
    internal class Program
    {
        static string outputDir;

        static void Main()
        {
            try
            {
                // 获取当前程序目录和名称
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string currentExecutable = Path.GetFileName(AppDomain.CurrentDomain.FriendlyName);

                // 创建输出目录
                outputDir = Path.Combine(currentDirectory, "decode");
                Directory.CreateDirectory(outputDir);

                Console.WriteLine($"当前程序: {currentExecutable}");
                Console.WriteLine($"遍历目录: {currentDirectory}");
                Console.WriteLine($"输出目录: {outputDir}");
                Console.WriteLine(new string('-', 50));

                // 开始递归遍历（排除输出目录）
                var excludedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { currentExecutable };
                TraverseDirectory(currentDirectory, excludedFiles, 0);

                Console.WriteLine("\n处理完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        // 递归遍历目录
        static void TraverseDirectory(string path, HashSet<string> excludedFiles, int depth)
        {
            try
            {
                // 排除输出目录，防止无限递归
                if (string.Equals(path, outputDir, StringComparison.OrdinalIgnoreCase))
                    return;

                // 处理当前目录下的文件
                foreach (string file in Directory.GetFiles(path))
                {
                    string fileName = Path.GetFileName(file);
                    if (!excludedFiles.Contains(fileName))
                    {
                        PrintEntry(fileName, depth, isDirectory: false);
                        ProcessFile(file);
                    }
                }

                // 递归处理子目录
                foreach (string directory in Directory.GetDirectories(path))
                {
                    // 跳过输出目录及其子目录
                    if (directory.StartsWith(outputDir, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string dirName = Path.GetFileName(directory);
                    PrintEntry(dirName, depth, isDirectory: true);
                    TraverseDirectory(directory, excludedFiles, depth + 1);
                }
            }
            catch (UnauthorizedAccessException)
            {
                PrintEntry("[访问受限]", depth, isDirectory: true);
            }
        }

        // 处理单个文件：二进制读取 -> 添加字符0 -> 保存
        static void ProcessFile(string filePath)
        {
            try
            {
                // 计算相对路径（兼容旧版.NET）
                string relativePath = GetRelativePath(AppDomain.CurrentDomain.BaseDirectory, filePath);
                string outputPath = Path.Combine(outputDir, relativePath);

                // 创建输出目录（如果不存在）
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                // 二进制读取文件
                byte[] fileData = File.ReadAllBytes(filePath);

                // 写入新文件
                File.WriteAllBytes(outputPath, fileData);

                Console.WriteLine($"  → 已处理: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ! 处理失败: {filePath} ({ex.Message})");
            }
        }

        // 替代实现：计算相对路径（兼容.NET Framework）
        static string GetRelativePath(string basePath, string fullPath)
        {
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                basePath += Path.DirectorySeparatorChar;

            Uri baseUri = new Uri(basePath);
            Uri fullUri = new Uri(fullPath);

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        // 格式化输出条目
        static void PrintEntry(string name, int depth, bool isDirectory)
        {
            string prefix = GetIndentation(depth);
            string typeMarker = isDirectory ? "[DIR] " : "      ";
            Console.WriteLine($"{prefix}{typeMarker}{name}");
        }

        // 生成缩进
        static string GetIndentation(int depth)
        {
            if (depth == 0) return "";

            string indent = "";
            for (int i = 0; i < depth - 1; i++)
            {
                indent += "│   ";
            }
            return indent + "├── ";
        }
    }
}