using CommandLine;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace Dab.DacFx
{
    class CommandLineOptions
    {
        [Option('d', "dacpac", Required = false, HelpText = "Path to the dacpac file")]
        public string DacpacPath { get; set; }

        [Option('s', "connectionString", Required = false, HelpText = "Connection string to the database for reading object definitions")]
        public string ConnectionString { get; set; }

        [Option('c', "config", Required = true, HelpText = "Path to the dab config file")]
        public string ConfigPath { get; set; }

        [Option('i', "include-views", Required = false, Default = false, HelpText = "Include views in the dab config, default is false")]
        public bool IncludeViews { get; set; }

        [Option('p', "permissions", Required = false, Default = "anonymous:*", HelpText = "Standard permissions for the tables, default is anonymous:*")]
        public string StandardPermissions { get; set; }

        // validation extension
        // checks for a config file in the current directory
        // command must have a connection string or dacpac path
        public void Validate()
        {

            if (!CheckForConfig(ConfigPath))
            {
                string errMsg = $"Valid config file not found at {ConfigPath}";
                throw new ArgumentValidationException(errMsg);
            }

            if (string.IsNullOrEmpty(ConnectionString) && string.IsNullOrEmpty(DacpacPath))
            {
                string errMsg = "Either a connection string or dacpac path must be provided";
                throw new ArgumentValidationException(errMsg);
            }

            if (!string.IsNullOrEmpty(ConnectionString) && !ValidateConnectionString(ConnectionString))
            {
                string errMsg = "Invalid connection string provided";
                throw new ArgumentValidationException(errMsg);
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

        static bool ValidateConnectionString(string connectionString)
        {
            bool valid = false;

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                valid = true;
            }
            catch (Exception)
            {
                valid = false;
            }

            return valid;

        }
    }


    class ArgumentValidationException : Exception
    {
        public ArgumentValidationException(string message) : base(message) { }
    }
}