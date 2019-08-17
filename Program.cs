using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaSync
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
#if DEBUG
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
#else
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>().UseUrls("http://*:8080");
#endif
        }
    }
}