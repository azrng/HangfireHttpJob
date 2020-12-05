using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace JobsServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
              //.UseUrls(HangfireSettings.Instance.ServiceAddress)//启用配置的地址
              .ConfigureWebHostDefaults(webBuilder =>
              {
                  webBuilder.UseStartup<Startup>();
              });
    }
}
