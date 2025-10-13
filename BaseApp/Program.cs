using RealGen;

namespace DX12GameProgramming
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            using var app = new InitDirect3DApp();
            app.Init();
            app.Run();
        }
    }
}
