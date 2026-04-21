using System.Text;

using CatGen;
Console.OutputEncoding = Encoding.UTF8;

try
{
    var app = new DirectXApp();
    app.Init();
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}
