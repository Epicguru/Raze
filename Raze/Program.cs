using Raze.Defs;
using System;
using System.IO;

namespace Raze
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            RunTest();

            using var game = new Main();
            game.Run();
        }

        public static void RunTest()
        {
            const string PATH = @"D:\Dev\C#\Raze\Raze\Content\Defs";
            string[] defFiles = Directory.GetFiles(PATH, "*.json", SearchOption.AllDirectories);

            using DefinitionLoader loader = new DefinitionLoader();
            foreach (string path in defFiles)
            {
                loader.Add(new DefinitionFile(path, File.ReadAllText(path)));
            }

            loader.OnError += (msg, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {msg}");
                if(e != null)
                    Console.WriteLine(e.ToString());
                Console.ForegroundColor = ConsoleColor.White;
            };
            var defs = loader.ProcessAll();
            foreach (var def in defs)
            {
                Console.WriteLine(def);
                var customData = def.TryGetAdditionalData("SomeOtherThing");
                if (customData != null)
                {
                    Console.WriteLine("Custom Data:");
                    Console.WriteLine($"Type: {customData.Type}");
                    Console.WriteLine($"Raw: {customData}");
                }
            }

            Console.ReadKey();
        }
    }
}
