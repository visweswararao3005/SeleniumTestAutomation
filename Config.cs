using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace LoginAutomation.Tests
{
    public static class Config
    {
        private static readonly IConfigurationRoot _config;

        static Config()
        {
            // BaseDirectory works well for test runs (bin/Debug…)
            var basePath = AppContext.BaseDirectory;
            _config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static string Get(string key, string @default = "")
            => _config[key] ?? @default;

        public static bool GetBool(string key, bool @default = false)
            => bool.TryParse(Get(key), out var b) ? b : @default;

        public static int GetInt(string key, int @default = 0)
            => int.TryParse(Get(key), out var i) ? i : @default;
    }
}
