using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketGenerator
{
    partial class PacketGenerator
    {
        static readonly string CPRFileName = "ClientPacketHandler.cs";
        static string CPRFilePath { get { return Directory.GetCurrentDirectory() + '\\' + CPRFileName; } }
        static void GenClientPacketRegister()
        {
            try
            {
                var json_string = File.ReadAllText(PDFilePath);
                var root_object = JsonConvert.DeserializeObject<JObject>(json_string);
                if (root_object == null)
                {
                    throw new InvalidDataException($"Parse Json Failed.");
                }

                var client_to_server = root_object.GetValue(C2S);
                if (client_to_server == null)
                {
                    throw new InvalidDataException($"[ServerToClient] Parse Failed.");
                }

                ParsePacketNameList(client_to_server, S2C, ref C2SPacketNameList);

                Header += "using NetworkCore;" + NEWLINE;
                Header += NEWLINE;

                Body += $"namespace Protocol.{C2S}";
                Body += NEWLINE;
                Body += START_SCOPE + NEWLINE;
                Body += MakePacketHandlerString();

                {
                    var inner = "";

                    inner += $"partial class PacketHandler" + NEWLINE;
                    inner += START_SCOPE + NEWLINE;
                    inner += MakeServerRegisterString(C2SPacketNameList);
                    inner += END_SCOPE + NEWLINE;

                    Body += Indent(inner);
                }

                Body += END_SCOPE + NEWLINE;

                File.WriteAllText(CPRFilePath, Header + Body);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
