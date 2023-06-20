using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace ChatServer
{
    public struct ConfigInfo
    {
        public struct NetworkInfo
        {
            public NetworkInfo() { }

            public short listen_port { get; set; } = 8282;
            public int listen_backlog { get; set; } = 100;
        }

        public NetworkInfo network;
    }

    public sealed class Config
    {
        private Config() { }
        private static readonly Lazy<Config> _inst = new Lazy<Config>(() => new Config());
        public static Config Inst { get { return _inst.Value; } }

        private string _config_file_name = "config.json";

        private string config_file_path { get { return Directory.GetCurrentDirectory() + "\\" +_config_file_name; } }

        public ConfigInfo Info { get; private set; }

        public bool Load()
        {
            try
            {
                var json_string = File.ReadAllText(config_file_path, Encoding.UTF8);
                Debug.WriteLine(json_string);

                Info = JsonConvert.DeserializeObject<ConfigInfo>(json_string);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            return true;
        }
    }
}
