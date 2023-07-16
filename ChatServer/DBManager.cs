using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    public class DBManager
    {
        private DBManager() { }
        private static Lazy<DBManager> _Inst = new Lazy<DBManager>(() => new DBManager());
        public static DBManager Inst { get { return _Inst.Value; } }

        // [TODO] DB 연동 전까지 파일로 저장
        #region File DB
        private string _DBFileName = "DB.json";
        private string DBFilePath { get { return Directory.GetCurrentDirectory() + '\\' + _DBFileName; } }

        private object _DBLock = new object();
        private JObject _DBRootObject = new JObject();
        public void LoadFromFile()
        {
            lock (_DBLock)
            {
                var file_string = File.ReadAllText(DBFilePath);
                _DBRootObject = JObject.Parse(file_string);
                if (_DBRootObject == null)
                {
                    _DBRootObject = new JObject();
                }
            }
        }

        public void SaveToFile()
        {
            lock(_DBLock)
            {
                File.WriteAllText(DBFilePath, _DBRootObject.ToString());
            }
        }

        public JArray? Load(string table_name)
        {
            lock(_DBLock)
            {
                var table_token = _DBRootObject.GetValue(table_name);
                if (table_token == null)
                {
                    return null;
                }

                return table_token.ToObject<JArray>();
            }
        }

        public void Update(JObject data)
        {
            lock (_DBLock)
            {
                foreach (var prop in data.Properties())
                {
                    var target_prop = _DBRootObject.Property(prop.Name);

                    if (target_prop == null)
                    {
                        _DBRootObject.Add(prop.Name, prop.Value);
                    }
                    else
                    {
                        target_prop.Value = prop.Value;
                    }
                }
            }
        }
        #endregion File DB
    }
}
