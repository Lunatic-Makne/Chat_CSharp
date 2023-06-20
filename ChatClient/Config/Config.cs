using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ChatClient
{
    public struct ConfigInfo
    {
        public string target_ip;
        public short target_port;
    }

    public class Config
    {
        private Config() { }
        private static readonly Lazy<Config> _Inst = new Lazy<Config>(() => new Config());
        public static Config Inst { get { return _Inst.Value; } }

        private string _config_file_name = "config.json";

        private string config_file_path { get { return Directory.GetCurrentDirectory() + "\\" + _config_file_name; } }
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
