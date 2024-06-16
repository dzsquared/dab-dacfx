using System.Diagnostics;
using Microsoft.SqlServer.Dac.Model;
using Newtonsoft.Json.Linq;

namespace Dab.DacFx
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            string dacpath = "/Users/drewsk/Documents/CodeRepos/dzsquared/dab-dacfx/AdventureWorksLT.dacpac";
            TSqlModel model = new TSqlModel(dacpath);

            string configPath = "/Users/drewsk/Documents/CodeRepos/dzsquared/dab-dacfx/testproj/dab-config.json";
            bool validConfig = CheckForConfig(configPath);
            if (!validConfig)
            {
                Console.WriteLine("Invalid config file");
                return;
            }

            // execute process dab add Author --source "dbo.authors" --permissions "anonymous:*"
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "dab";

            // Get the tables in the model
            foreach (TSqlObject table in model.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass))
            {
                Console.WriteLine($"Adding {table.Name}...");

                Process process = new Process();
                startInfo.Arguments = $"add \"{table.Name}\" --config \"{configPath}\" --source \"{table.Name}\" --permissions \"anonymous:*\"";
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                Console.WriteLine(process.StandardOutput.ReadToEnd());
            }
        }

        // checks for a config file in the current directory
        // json file must contain entities object
        static bool CheckForConfig(string filePath)
        {
            bool validConfig = false;

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                JObject config = JObject.Parse(json);

                if (config.ContainsKey("entities"))
                {
                    validConfig = true;
                }
            }

            return validConfig;
        }
    }
}
 