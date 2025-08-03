using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Data;
using System.Windows;
using Application = System.Windows.Application;

namespace SpAnalyzerTool
{
    public partial class App : Application
    {
        public static IConfiguration Configuration { get; private set; }
        static App()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
        }
    }

}
