namespace DAOGenerator
{
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class TableDefinitionGenerator : BaseGenerator, IGenerator
    {
        private string _outputPath;
        private string _namespaceName;
        //private List<Mapping> _mapping;

        public TableDefinitionGenerator(string outputPath, string namespaceName, string databaseName)
        {
            _outputPath = outputPath;
            _namespaceName = string.Format("{0}.{1}.{2}", namespaceName, databaseName, "Definition");
            //_mapping = MapperHelper.GetMappingInformation().ToList();
        }

        public void Generate(Table table)
        {
            throw new NotImplementedException();            
        }

        public void Generate(List<Table> tables)
        {
            string fileName = string.Format("{0}\\{1}.{2}", _outputPath, "Definition", _fileType);

            TextWriter tw = new StreamWriter(new FileStream(fileName, FileMode.Create));

            var codeProvider = CodeDomProvider.CreateProvider(_codeToGenerate);

            var ns = CodeGenHelper.CreateNamespace(_namespaceName);
            ns.Imports.Add(new CodeNamespaceImport("System"));

            foreach(var table in tables)
            {
                var cls = CodeGenHelper.CreateClass(table.Name, false);

                foreach(var schema in table.Schemas)
                {
                    cls.Members.Add(CodeGenHelper.CreateStaticDeclaration(schema.ColumnName, schema.ColumnName));
                }

                ns.Types.Add(cls);
            }            

            //generate the source code file
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BracingStyle = "C";

            codeProvider.GenerateCodeFromNamespace(ns, tw, options);

            //close the text writer
            tw.Close();
        }
    }
}
