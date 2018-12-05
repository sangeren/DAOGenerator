namespace DAOGenerator
{
    using Microsoft.CSharp;
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Transactions;

    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Data Access Layer Generator " + typeof(Program).Assembly.GetName().Version.ToString());
            Console.WriteLine("MySQL Edition");

            if (args.Count() < 2)
                throw new Exception("invalid arguements. Must pass server IP address, username and password.");

            string outputPath = ConfigurationManager.AppSettings["OutputPath"];
            string nameSpace = ConfigurationManager.AppSettings["Namespace"];
            string connectionString = string.Format("server={0};user={1};password={2};port=3306;database=INFORMATION_SCHEMA", args[0], args[1], args[2]);

            
            Console.WriteLine("Connecting to " + args[0]);

            var objIsMgr = new InformationSchemaManager(connectionString, args[3]);
            var tables = objIsMgr.GetTablesInformation();

            Console.WriteLine("Information Schemas acquired. Generating source code...");

            var generator = new POCOGenerator(outputPath, nameSpace, args[3]);

            foreach (var table in tables)
            {
                generator.Generate(table);
            }

            var defgenerator = new TableDefinitionGenerator(outputPath, nameSpace, args[3]);
            defgenerator.Generate(tables.ToList());

            var opgenerator = new OrdinalPositionGenerator(outputPath, nameSpace, args[3]);
            opgenerator.Generate(tables.ToList());

            //var oprovider = new ProviderGenerator(outputPath, nameSpace, args[3]);

            //foreach (var table in tables)
            //{
            //    oprovider.Generate(table);
            //}

            Console.WriteLine("Done.");
        }
    }

        
}
