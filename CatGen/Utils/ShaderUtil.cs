using System;
using System.IO;

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;

using ShaderBytecode = SharpDX.Direct3D12.ShaderBytecode;

namespace CatGen.Utils;

public static class ShaderUtil
{
    public static ShaderBytecode CompileShader(string fileName, string entryPoint, string profile, ShaderMacro[] defines = null)
    {
        var shaderFlags = ShaderFlags.None;
#if DEBUG
        shaderFlags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization;
#endif
        var result = SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile(
            fileName,
            entryPoint,
            profile,
            shaderFlags,
            include: FileIncludeHandler.Default,
            defines: defines);

        return new ShaderBytecode(result);
    }

    // Required for ShaderBytecode.CompileFromFile API in order to resolve #includes in shader files.
    // Equivalent for D3D_COMPILE_STANDARD_FILE_INCLUDE.
    internal class FileIncludeHandler : CallbackBase, Include
    {
        public static FileIncludeHandler Default { get; } = new();

        public Stream Open(IncludeType type, string fileName, Stream parentStream)
        {
            var filePath = fileName;

            if (!Path.IsPathRooted(filePath))
            {
                var selectedFile = Path.Combine(Environment.CurrentDirectory, fileName);
                if (File.Exists(selectedFile))
                    filePath = selectedFile;
            }

            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }

        public void Close(Stream stream) =>
            stream.Close();
    }
}
