using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace PacketGenerator
{
    partial class PacketGenerator
    {
        static readonly string SPRFileName = "ServerPacketHandler.cs";
        static string SPRFilePath { get { return Directory.GetCurrentDirectory() + '\\' +  SPRFileName; } }
        static void GenServerPacketRegister()
        {
            try
            {
                var json_string = File.ReadAllText(PDFilePath);
                var root_object = JsonConvert.DeserializeObject<JObject>(json_string);
                if (root_object == null)
                {
                    throw new InvalidDataException($"Parse Json Failed.");
                }

                var server_to_client = root_object.GetValue(S2C);
                if (server_to_client == null)
                {
                    throw new InvalidDataException($"[ServerToClient] Parse Failed.");
                }

                ParsePacketNameList(server_to_client, S2C, ref S2CPacketNameList);

                Header += "using NetworkCore;" + NEWLINE;
                Header += NEWLINE;

                Body += $"namespace Protocol.{S2C}";
                Body += NEWLINE;
                Body += START_SCOPE + NEWLINE;
                Body += MakePacketHandlerString();

                {
                    var inner = "";

                    inner += $"partial class PacketHandler" + NEWLINE;
                    inner += START_SCOPE + NEWLINE;
                    inner += MakeServerRegisterString(S2CPacketNameList);
                    inner += END_SCOPE + NEWLINE;

                    Body += Indent(inner);
                }
                
                Body += END_SCOPE + NEWLINE;

                File.WriteAllText(SPRFilePath, Header + Body);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static string MakePacketHandlerString()
        {
            var result = "";

            result += $"using OnReceiveFunc = Action<PacketHandleConnection, ArraySegment<byte>>;" + NEWLINE;
            result += $"using PacketHandleFunc = Action<PacketHandleConnection, IPacket>;" + NEWLINE;

            result += $"partial class PacketHandler" + NEWLINE;
            result += START_SCOPE + NEWLINE;
            {
                var inner = "";
                inner += "private static readonly Lazy<PacketHandler> _Inst = new Lazy<PacketHandler>(() => new PacketHandler());" + NEWLINE;
                inner += "public static PacketHandler Inst { get { return _Inst.Value; } }" + NEWLINE;
                inner += "private PacketHandler() { }" + NEWLINE;

                inner += "void MakePacket<T>(PacketHandleConnection connection, ArraySegment<byte> buffer) where T : IPacket, new()" + NEWLINE;
                inner += START_SCOPE + NEWLINE;
                {
                    var inner2 = "";
                    inner2 += "T packet = new T();" + NEWLINE;
                    inner2 += "packet.Read(buffer);" + NEWLINE;
                    inner2 += "PacketHandleFunc func = null;" + NEWLINE;
                    inner2 += "if (_PacketHandlerDic.TryGetValue(packet.Id, out func)) func.Invoke(connection, packet);" + NEWLINE;
                    inner += Indent(inner2);
                }
                inner += END_SCOPE + NEWLINE;

                inner += "public bool Dispatch(PacketHandleConnection connection, ArraySegment<byte> buffer)" + NEWLINE;
                inner += START_SCOPE + NEWLINE;
                {
                    var inner2 = "";
                    inner2 += "if (buffer.Array == null) { return false; }" + NEWLINE;
                    inner2 += "int offset = 0;" + NEWLINE;
                    inner2 += "var size = BitConverter.ToInt16(buffer.Array, buffer.Offset + offset);" + NEWLINE;
                    inner2 += "offset += sizeof(short);" + NEWLINE;
                    inner2 += "var id = BitConverter.ToInt64(buffer.Array, buffer.Offset + offset);" + NEWLINE;
                    inner2 += "offset += sizeof(long);" + NEWLINE;
                    inner2 += "if (offset + size > buffer.Array.Length) { return false; }" + NEWLINE;
                    inner2 += "OnReceiveFunc func = null;" + NEWLINE;
                    inner2 += "if (_OnReceiveHandlerDic.TryGetValue(id, out func)) func.Invoke(connection, new ArraySegment<byte>(buffer.Array, buffer.Offset + offset, buffer.Count - offset));" + NEWLINE;
                    inner2 += "return true;" + NEWLINE;
                    inner += Indent(inner2);
                }
                inner += END_SCOPE + NEWLINE;
                result += Indent(inner);
            }
            result += END_SCOPE + NEWLINE;

            return Indent(result);
        }

        static string MakeServerRegisterString(List<string> packet_name_list)
        {
            var result = "";

            result += $"private Dictionary<long, OnReceiveFunc> _OnReceiveHandlerDic = new Dictionary<long, OnReceiveFunc>();" + NEWLINE;
            result += $"private Dictionary<long, PacketHandleFunc> _PacketHandlerDic = new Dictionary<long, PacketHandleFunc>();" + NEWLINE;
            result += "public void Register()" + NEWLINE;
            result += START_SCOPE + NEWLINE;

            var inner = "";

            foreach(var element in packet_name_list)
            {
                var enum_string = string.Format(PACKET_ID_ENUM_FORMAT, element.ToUpper());
                inner += $"_OnReceiveHandlerDic.Add((long)PacketId.{enum_string}, MakePacket<{element}>);" + NEWLINE;
                inner += $"_PacketHandlerDic.Add((long)PacketId.{enum_string}, {element}Handler);" + NEWLINE;
            }

            result += Indent(inner);

            result += END_SCOPE + NEWLINE;

            return Indent(result);
        }
    }
}
