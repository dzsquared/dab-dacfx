using System.Diagnostics;
using CommandLine;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;

namespace Dab.DacFx
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<CommandLineOptions>(args)
                    .MapResult(
                        (CommandLineOptions opts) =>
                        {
                            opts.Validate();
                            RunOptions(opts);
                            return 0;
                        },
                        errors => 1
                    );
                return 0;
            }
            catch (ArgumentValidationException e)
            {
                Console.WriteLine("Invalid arguments detected: {0}", e);
                return 1;
            }
        }

        static void RunOptions(CommandLineOptions opts)
        {
            // CONFIG OPTIONS
            bool includeViews = opts.IncludeViews;
            string standardPermissions = opts.StandardPermissions;
            string configPath = opts.ConfigPath;

            string dacpath = opts.DacpacPath;
            if (string.IsNullOrEmpty(dacpath))
            {
                dacpath = ExtractDacpac(opts.ConnectionString, configPath);
            }
            TSqlModel model = new TSqlModel(dacpath);

            // TableSet contains a list of tables to add to dab
            TableSet tableSet = new TableSet();

            // dab cli is used to perform the preliminary add of the table to the config
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

        // connects to db and writes dacpac to file
        // contains object definitions in the db at that time
        static string ExtractDacpac(string connectionString, string configPath)
        {
            DacServices dacServices = new DacServices(connectionString);
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
            string dbName = builder.InitialCatalog;

            // place the dacpac next to the config file
            // remove the filename from the path
            string dacpacPath = Path.Combine(Path.GetDirectoryName(configPath) ?? "./", $"{dbName}.dacpac");

            try
            {
                dacServices.Extract(dacpacPath, dbName, null, null);
            }
            catch (DacServicesException e)
            {
                Console.WriteLine($"Error extracting dacpac: {e.Message}");
            }

            return dacpacPath;
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
            string tableName = tableObj.Name.Parts[1] ?? tableObj.Name.Parts[0];

            // lookup tablename (entityname) in list for conflicting names
            // try to append schema name to table name
            // try appending random 3 digit number
            if (Tables.Any(t => t.EntityName == tableName))
            {
                tableName = $"{tableObj.Name.Parts[0]}_{tableObj.Name.Parts[1]}";
            }
            if (Tables.Any(t => t.EntityName == tableName))
            {
                Random rand = new Random();
                tableName += $"{rand.Next(100, 999)}";
            }

            EntityTable table = new EntityTable(tableName, tableObj.Name.ToString());
            Tables.Add(table);
        }
    }
}
