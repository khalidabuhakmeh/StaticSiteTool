using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Oakton.AspNetCore;

[assembly:Oakton.OaktonCommandAssembly]

namespace StaticSiteTool
{
    public class Program
    {
        public static Task<int> Main(string[] args)
        {
            return CreateHostBuilder(args)
                .RunOaktonCommands(args);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}