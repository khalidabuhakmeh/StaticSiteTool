using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oakton;
using Oakton.AspNetCore;

namespace StaticSiteTool.Commands
{
    public class StaticPagesInput : NetCoreInput
    {
        [FlagAlias("url", 'u')]
        public string UrlFlag { get; set; }
            = "http://localhost:5000";

        [FlagAlias("sitemap", 's')]
        public string SitemapFlag { get; set; }
            = "sitemap.xml";
    }

    public class StaticPagesCommand : OaktonAsyncCommand<StaticPagesInput>
    {
        public override async Task<bool> Execute(StaticPagesInput input)
        {
            using var buildHost = input.BuildHost();

            var lifetime = buildHost
                .Services
                .GetRequiredService<IHostApplicationLifetime>();

            // process HTML files after the server 
            // has started up
            lifetime.ApplicationStarted.Register(async (state) =>
            {
                var host = (IHost) state;
                var webHostEnvironment = host
                    .Services
                    .GetRequiredService<IWebHostEnvironment>();

                var logger = host
                    .Services
                    .GetRequiredService<ILogger<StaticPagesCommand>>();

                logger.LogInformation($"Attempting to access {input.UrlFlag }.");

                var client = new HttpClient
                {
                    BaseAddress = new Uri(input.UrlFlag )
                };

                var siteMapResponse =
                    await client.GetAsync(input.SitemapFlag);

                var siteMap = await siteMapResponse.Content.ReadAsStringAsync();
                var xml = new XmlDocument();

                // load sitemap
                xml.LoadXml(siteMap);

                var locations = xml
                    .GetElementsByTagName("loc")
                    .Cast<XmlElement>();

                var wwwRoot = webHostEnvironment.WebRootPath;

                foreach (var location in locations)
                {
                    var uri = new Uri(location.InnerText);
                    var localPath = uri.LocalPath;

                    // write html to disk
                    if (Path.GetExtension(localPath) is "" or null)
                    {
                        localPath = Path.Combine(localPath, "index.html");
                    }

                    localPath = wwwRoot + localPath;

                    // delete the file so it doesn't
                    // get served instead of our endpoint
                    if (File.Exists(localPath))
                    {
                        File.Delete(localPath);
                    }

                    var page = await client.GetStringAsync(uri);
                    var directory = Directory.GetParent(localPath);

                    if (!directory.Exists)
                    {
                        directory.Create();
                    }

                    await File.WriteAllTextAsync(localPath, page);
                }
                
                await host.StopAsync();
                
            }, buildHost);

            await buildHost.RunAsync();

            return true;
        }
    }
}