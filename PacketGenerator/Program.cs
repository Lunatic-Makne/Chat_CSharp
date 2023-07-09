using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;

namespace PacketGenerator
{
    internal class Program
    {
        private static readonly string _PDFileName = "PacketDefinition.json";
        public static string PDFilePath { get { return Directory.GetCurrentDirectory() + "\\" + _PDFileName; } }
        private static readonly string _PacketFileName = "Packet.cs";
        public static string PacketFilePath { get { return Directory.GetCurrentDirectory() + "\\" + _PacketFileName; } }

        private static string Header;
        private static string Body;

        private static readonly string SHARED_STRUCT = "SharedStruct";
        private static readonly string C2S = "ClientToServer";
        private static readonly string S2C = "ServerToClient";

        private static readonly string TAP = "\t";
        private static readonly string NEWLINE = "\n";
        private static readonly string START_SCOPE = @"{";
        private static readonly string END_SCOPE = @"}";

        private static readonly string DEFI_TYPE = "definition_type";
        private static readonly string TYPE = "type";
        private static readonly string NAME = "name";
        private static readonly string PROP = "properties";
        private static readonly string IN_TYPE = "inner_type";
        private static readonly string PACKET = "packet";
        private static readonly string STRUCT = "struct";

        static void Main(string[] args)
        {
            try
            {
                var json_string = File.ReadAllText(PDFilePath);
                var root_object = JsonConvert.DeserializeObject<JObject>(json_string);

                // Shared
                var shared_struct = root_object.GetValue(SHARED_STRUCT);
                if (shared_struct != null)
                {
                    ParseNamespace(shared_struct, SHARED_STRUCT);
                }
                else
                {
                    throw new InvalidDataException($"[SharedStruct] Parse Failed.");
                }

                // C2S
                var client_to_server = root_object.GetValue(C2S);
                if (client_to_server != null)
                {
                    ParseNamespace(client_to_server, C2S);
                }
                else
                {
                    throw new InvalidDataException($"[ClientToServer] Parse Failed.");
                }

                // S2C
                var server_to_client = root_object.GetValue(S2C);
                if (server_to_client != null)
                {
                    ParseNamespace(server_to_client, S2C);
                }
                else
                {
                    throw new InvalidDataException($"[ServerToClient] Parse Failed.");
                }

                Body += END_SCOPE;

                MakeHeader();

                var result = Header + Body;
                Console.WriteLine($"[Result] :\n{result}");

                File.WriteAllText(PacketFilePath, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static List<string> _PacketNameList = new List<string>();
        private static readonly string PACKET_ID_ENUM_FORMAT = "_{0}_";

        public static string MakeEnum()
        {
            var result = "";
            result += "public enum PacketId : long" + NEWLINE;
            result += START_SCOPE + NEWLINE;

            result += TAP + "_Unknown_ = 0" + NEWLINE;

            foreach (var packet in _PacketNameList )
            {
                var enum_string = string.Format(PACKET_ID_ENUM_FORMAT, packet.ToUpper());
                result += TAP + ", " + enum_string + NEWLINE;
            }

            result += TAP + ", _MAX_" + NEWLINE;
            result += END_SCOPE + NEWLINE;

            return Indent(result);
        }

        public static void MakeHeader()
        {
            Header += "using NetworkCore;" + NEWLINE;
            Header += "using System.Text;" + NEWLINE;
            Header += NEWLINE;

            Header += "namespace Protocol";
            Header += NEWLINE;
            Header += START_SCOPE + NEWLINE;

            Header += MakeEnum();
            Header += Indent(
@"
public abstract class IPacket
{
    public short Size = 0;
    public PacketId Id = PacketId._Unknown_;

    public IPacket(PacketId id)
    {
        Id = id;

        Size = sizeof(short);
        Size += sizeof(PacketId);
    }

    public ArraySegment<byte>? Write(int send_buffer_size)
    {
        var openSegment = SendBufferHelper.Open(send_buffer_size);
        if (openSegment == null || openSegment.HasValue == false) { return null; }
        if (openSegment.Value.Array == null) { return null; }

        var buffer = openSegment.Value;
        Size = (short)WriteImpl(buffer);
        if (Size <= 0) { return null; }

        var size = BitConverter.GetBytes(Size);
        Array.Copy(size, 0, buffer.Array, buffer.Offset, sizeof(short));

        return SendBufferHelper.Close(Size);
    }

    protected virtual int WriteImpl(ArraySegment<byte> buffer)
    {
        if (buffer.Array == null) { return 0; }

        int offset = 0;
        // reserve byte for packet size
        offset += sizeof(short);

        var span = new Span<byte>(buffer.Array, buffer.Offset, buffer.Count);

        bool result = BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), (long) Id);
        offset += sizeof(long);

        if (result == false) { return 0; }

        return offset;
    }

    public abstract bool Read(ArraySegment<byte> buffer);
}");
        }

        public static void ParseNamespace(JToken token, string ns)
        {
            Console.WriteLine($"[{ns}] Parse start");

            var result = $"namespace {ns}" + NEWLINE;
            result += START_SCOPE + NEWLINE;

            var struct_list = token.ToArray();
            foreach (var element in struct_list)
            {
                var jobject = element.ToObject<JObject>();
                if (jobject != null)
                {
                    var def_token = jobject.GetValue(DEFI_TYPE);
                    if (def_token != null)
                    {
                        var def_type = def_token.ToString();
                        if (def_type == PACKET)
                        {
                            result += ParsePacket(jobject);
                        }
                        else if (def_type == STRUCT)
                        {
                            result += ParseStruct(jobject);
                        }
                        else
                        {
                            throw new InvalidDataException($"[{ns}] Invalid definition_type. value: {def_type}");
                        }                        
                    }
                    else
                    {
                        throw new InvalidDataException($"[{ns}] Parse element failed. target: \n{jobject}");
                    }
                }
                else
                {
                    throw new InvalidDataException($"[{ns}] Convert JObject failed. target: \n{element}");
                }
            }

            result += END_SCOPE + NEWLINE;
            Body += Indent(result);
        }

        public static string Indent(string text)
        {
            var result = "";
            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    result += TAP + line + NEWLINE;
                }
            }

            return result;
        }

        public static string ParseProperty(JToken jobject)
        {
            var result = "";

            var prop_array = jobject.ToArray();
            foreach (var prop_token in prop_array)
            {
                result += "public ";
                var type_string = prop_token.Value<string>(TYPE);
                if (type_string == null)
                {
                    throw new InvalidDataException($"[ParseStructWhiteFunc][Type] ParseProperties failed. target: \n{jobject}");
                }

                var name_string = prop_token.Value<string>(NAME);
                if (name_string == null)
                {
                    throw new InvalidDataException($"[ParseStructWhiteFunc][Name] ParseProperties failed. target: \n{jobject}");
                }

                type_string = type_string.ToLower().Trim();

                switch (type_string)
                {
                    case "string":
                    case "bool":
                    case "int":
                    case "short":
                    case "long":
                    case "float":
                    case "double":
                        result += type_string + " " + name_string;
                        break;
                    case "list":
                        {
                            var inner_type_string = prop_token.Value<string>(IN_TYPE);
                            if (inner_type_string == null)
                            {
                                throw new InvalidDataException($"[ParseStructWhiteFunc][List] ParseProperties failed. target: \n{jobject}");
                            }

                            result += $"List<{SHARED_STRUCT}.{inner_type_string}> " + name_string + $" = new List<{SHARED_STRUCT}.{inner_type_string}>()";
                        }
                        break;
                    default:
                        throw new InvalidDataException($"[ParseProperty][Type] Invalid type[{type_string}]. target: \n{jobject}");
                }

                result += ";" + NEWLINE;
            }
            result += NEWLINE;

            return Indent(result);
        }

        public static string ParsePropertyWrite(JToken properties)
        {
            var result = "";

            var prop_array = properties.ToArray();
            foreach (var prop_token in prop_array)
            {
                var type_string = prop_token.Value<string>(TYPE);
                if (type_string == null)
                {
                    throw new InvalidDataException($"[ParsePropertyWrite] ParseProperties failed. target: \n{properties}");
                }

                var name_string = prop_token.Value<string>(NAME);
                if (name_string == null)
                {
                    throw new InvalidDataException($"[ParsePropertyWrite] ParseProperties failed. target: \n{properties}");
                }

                switch (type_string)
                {
                    case "string":
                        result += $"var {name_string.ToLower()}_length = Encoding.Unicode.GetBytes({name_string}, 0, {name_string}.Length, buffer.Array, buffer.Offset + offset + sizeof(int));";
                        result += NEWLINE;
                        result += $"result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), {name_string.ToLower()}_length);";
                        result += NEWLINE;
                        result += "offset += sizeof(int); offset += name_length;";
                        result += NEWLINE;
                        break;
                    case "bool":
                        result += $"result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), {name_string});";
                        result += NEWLINE;
                        result += "offset += sizeof(bool);";
                        result += NEWLINE;
                        break;
                    case "int":
                        result += $"result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), {name_string});";
                        result += NEWLINE;
                        result += "offset += sizeof(int);";
                        result += NEWLINE;
                        break;
                    case "short":
                        result += $"result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), {name_string});";
                        result += NEWLINE;
                        result += "offset += sizeof(short);";
                        result += NEWLINE;
                        break;
                    case "long":
                        result += $"result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), {name_string});";
                        result += NEWLINE;
                        result += "offset += sizeof(long);";
                        result += NEWLINE;
                        break;
                    case "float":
                        break;
                    case "double":
                        break;
                    case "list":
                        result += $"result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), {name_string}.Count);";
                        result += NEWLINE;
                        result += "offset += sizeof(int);";
                        result += NEWLINE;
                        result += $"foreach( var element in {name_string} )";
                        result += NEWLINE;
                        result += START_SCOPE + NEWLINE;
                        result += TAP + $"result &= element.Write(span, ref offset);";
                        result += NEWLINE;
                        result += END_SCOPE + NEWLINE;
                        break;
                    default:
                        throw new InvalidDataException($"[ParsePropertyWrite][Type] Invalid type[{type_string}]. target: \n{properties}");
                }
            }

            return result;
        }

        public static string ParsePropertyRead(JToken properties)
        {
            var result = "";

            var prop_array = properties.ToArray();
            foreach (var prop_token in prop_array)
            {
                var type_string = prop_token.Value<string>(TYPE);
                if (type_string == null)
                {
                    throw new InvalidDataException($"[ParsePropertyRead] ParseProperties failed. target: \n{properties}");
                }

                var name_string = prop_token.Value<string>(NAME);
                if (name_string == null)
                {
                    throw new InvalidDataException($"[ParsePropertyRead] ParseProperties failed. target: \n{properties}");
                }

                switch (type_string)
                {
                    case "string":
                        result += $"var {name_string.ToLower()}_length = BitConverter.ToInt32(readonly_span.Slice(offset, readonly_span.Length - offset));";
                        result += NEWLINE;
                        result += $"offset += sizeof(int);";
                        result += NEWLINE;
                        result += $"{name_string} = Encoding.Unicode.GetString(readonly_span.Slice(offset, readonly_span.Length - offset));";
                        result += NEWLINE;
                        result += $"offset += {name_string.ToLower()}_length;";
                        result += NEWLINE;
                        break;
                    case "bool":
                        result += $"{name_string} = BitConverter.ToBoolean(readonly_span.Slice(offset, readonly_span.Length - offset));";
                        result += NEWLINE;
                        result += "offset += sizeof(bool);";
                        result += NEWLINE;
                        break;
                    case "short":
                        result += $"{name_string} = BitConverter.ToInt16(readonly_span.Slice(offset, readonly_span.Length - offset));";
                        result += NEWLINE;
                        result += "offset += sizeof(short);";
                        result += NEWLINE;
                        break;
                    case "int":
                        result += $"{name_string} = BitConverter.ToInt32(readonly_span.Slice(offset, readonly_span.Length - offset));";
                        result += NEWLINE;
                        result += "offset += sizeof(int);";
                        result += NEWLINE;
                        break;                    
                    case "long":
                        result += $"{name_string} = BitConverter.ToInt64(readonly_span.Slice(offset, readonly_span.Length - offset));";
                        result += NEWLINE;
                        result += "offset += sizeof(long);";
                        result += NEWLINE;
                        break;
                    case "float":
                        result += $"{name_string} = BitConverter.ToSingle(readonly_span.Slice(offset, readonly_span.Length - offset));";
                        result += NEWLINE;
                        result += "offset += sizeof(float);";
                        result += NEWLINE;
                        break;
                    case "double":
                        break;
                    case "list":
                        result += $"var {name_string}_list_count = BitConverter.ToInt32(readonly_span.Slice(offset, readonly_span.Length - offset));";
                        result += NEWLINE;
                        result += $"offset += sizeof(int);";
                        result += NEWLINE;
                        result += $"for(int i = 0; i < {name_string}_list_count; ++i)";
                        result += NEWLINE;
                        result += START_SCOPE + NEWLINE;

                        var inner_type_string = prop_token.Value<string>(IN_TYPE);
                        if (inner_type_string == null)
                        {
                            throw new InvalidDataException($"[ParsePropertyRead][List] ParseProperties failed. target: \n{properties}");
                        }
                        result += TAP + $"var element = new {SHARED_STRUCT}.{inner_type_string}();";
                        result += NEWLINE;
                        result += TAP + $"element.Read(readonly_span, ref offset);";
                        result += NEWLINE;
                        result += TAP + $"{name_string}.Add(element);";
                        result += NEWLINE;
                        result += END_SCOPE + NEWLINE;                        
                        break;
                    default:
                        throw new InvalidDataException($"[ParsePropertyRead][Type] Invalid type[{type_string}]. target: \n{properties}");
                }
            }

            return result;
        }

        public static string ParseStructWriteFunc(JToken jobject)
        {
            var result = "public bool Write(Span<byte> span, ref int offset)" + NEWLINE;

            result += START_SCOPE + NEWLINE;

            var inner = "bool result = true;" + NEWLINE;
            inner += ParsePropertyWrite(jobject);
            inner += "return result;";

            result += Indent(inner);

            result += END_SCOPE + NEWLINE;

            return Indent(result);
        }

        public static string ParseStructReadFunc(JToken jobject)
        {
            var result = "public void Read(ReadOnlySpan<byte> readonly_span, ref int offset)" + NEWLINE;

            result += START_SCOPE + NEWLINE;

            var inner = ParsePropertyRead(jobject);

            result += Indent(inner);
            result += END_SCOPE + NEWLINE;

            return Indent(result);
        }

        public static string ParseStruct(JObject jobject)
        {
            var result = "";

            var name_token = jobject.GetValue(NAME);
            if (name_token != null)
            {
                result += $"public struct {name_token}" + NEWLINE;
            }
            else
            {
                throw new InvalidDataException($"[ParseStruct] Invalid name. target: \n{jobject}");
            }

            result += START_SCOPE + NEWLINE;

            var property_token = jobject.GetValue(PROP);
            if (property_token != null)
            {
                result += ParseProperty(property_token);
                result += ParseStructWriteFunc(property_token);
                result += ParseStructReadFunc(property_token);
            }
            else
            {
                throw new InvalidDataException($"[ParseStruct] Invalid Properties. target: \n{jobject}");
            }

            result += END_SCOPE + NEWLINE;

            return Indent(result);
        }

        public static string ParsePacketWriteFunc(JToken token)
        {
            var result = "protected override int WriteImpl(ArraySegment<byte> buffer)" + NEWLINE;

            result += START_SCOPE + NEWLINE;

            result += Indent(
@"if (buffer.Array == null) { return 0; }

var offset = base.WriteImpl(buffer);
if (offset == 0) { return 0; }

bool result = true;
var span = new Span<byte>(buffer.Array, buffer.Offset, buffer.Count);
");

            var inner = ParsePropertyWrite(token);

            result += Indent(inner);

            result += Indent(
@"
if (result == false) { return 0; }

return offset;");
            result += END_SCOPE + NEWLINE;

            return Indent(result);
        }

        public static string ParsePacketReadFunc(JToken token)
        {
            var result = "public override bool Read(ArraySegment<byte> buffer)" + NEWLINE;

            result += START_SCOPE + NEWLINE;

            result += Indent(
@"if (buffer.Array == null) { return false; }

var readonly_span = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);
int offset = 0;
");

            var inner = ParsePropertyRead(token);

            result += Indent(inner);

            result += Indent("return true;");
            result += END_SCOPE + NEWLINE;

            return Indent(result);
        }

        public static string ParsePacketConstructor(string packet_name)
        {
            var result = "";

            result += $"public {packet_name}() : base(PacketId.{string.Format(PACKET_ID_ENUM_FORMAT, packet_name.ToUpper())})";
            result += NEWLINE;
            result += START_SCOPE + NEWLINE;
            result += END_SCOPE + NEWLINE;

            return Indent(result);
        }

        public static string ParsePacket(JObject jobject)
        {
            var result = "";
            var name_token = jobject.GetValue(NAME);
            if (name_token != null)
            {
                result += $"public class {name_token} : IPacket" + NEWLINE;
                _PacketNameList.Add(name_token.ToString());
            }
            else
            {
                throw new InvalidDataException($"[ParsePacket] Invalid name. target: \n{jobject}");
            }

            result += START_SCOPE + NEWLINE;

            var property_token = jobject.GetValue(PROP);
            if (property_token != null)
            {
                result += ParseProperty(property_token);
                result += ParsePacketConstructor(name_token.ToString());
                result += ParsePacketWriteFunc(property_token);
                result += ParsePacketReadFunc(property_token);
            }
            else
            {
                throw new InvalidDataException($"[ParseStruct] Invalid Properties. target: \n{jobject}");
            }

            result += END_SCOPE + NEWLINE;

            return Indent(result);
        }
    }
}