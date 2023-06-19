namespace ChatServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var config_result = Config.Inst.Load();
            if (config_result == false)
            {
                Console.WriteLine("Load Config Failed.");
            }
        }
    }
}