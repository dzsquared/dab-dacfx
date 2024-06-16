using System.Diagnostics;
using Microsoft.SqlServer.Dac.Model;
using Newtonsoft.Json.Linq;

namespace Dab.DacFx
{
    class Program
    {
        static void Main(string[] args)
        {
            // CONFIG OPTIONS
            string dacpath = "/Users/drewsk/Documents/CodeRepos/dzsquared/dab-dacfx/AdventureWorksLT.dacpac";
            TSqlModel model = new TSqlModel(dacpath);

            bool includeViews = true;
            string standardPermissions = "anonymous:*";

            string configPath = "/Users/drewsk/Documents/CodeRepos/dzsquared/dab-dacfx/testproj/dab-config.json";
            bool validConfig = CheckForConfig(configPath);
            if (!validConfig)
            {
                Console.WriteLine("Invalid config file");
                return;
            }

            TableSet tableSet = new TableSet();

            // execute process dab add Author --source "dbo.authors" --permissions "anonymous:*"
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "dab";
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            // for each table in the model, add to the tableSet
            foreach (TSqlObject table in model.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass))
            {
                tableSet.AddTable(table);
            }

            // if includeViews, add views to tableSet
            if (includeViews)
            {
                foreach (TSqlObject view in model.GetObjects(DacQueryScopes.UserDefined, View.TypeClass))
                {
                    tableSet.AddTable(view);
                }
            }

            // for each table in the tableset, add it to dab
            foreach (EntityTable table in tableSet.Tables)
            {
                Console.WriteLine($"Adding {table.EntityName}...");
                Process process = new Process();
                startInfo.Arguments = $"add \"{table.EntityName}\" --config \"{configPath}\" --source \"{table.TableName}\" --permissions \"{standardPermissions}\" ";
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

    // describes a table in the database
    // entity name and TSqlObject table
    // could be expanded to include relationship information
    public class EntityTable
    {
        public string EntityName { get; set; }
        public string TableName { get; set; }

        public EntityTable(string entityName, string tableName)
        {
            EntityName = entityName;
            TableName = tableName;
        }
    }


    public class TableSet
    {
        public List<EntityTable> Tables { get; set; }

        public TableSet()
        {
            Tables = new List<EntityTable>();
        }

        public void AddTable(TSqlObject tableObj)
        {
            string tableName = tableObj.Name.Parts[1];

            // lookup tablename in list for conflicting names
            if (Tables.Any(t => t.EntityName == tableName))
            {
                tableName = $"{tableObj.Name.Parts[0]}_{tableObj.Name.Parts[1]}";
            }

            EntityTable table = new EntityTable(tableName, tableObj.Name.ToString());
            Tables.Add(table);
        }
    }
}
 