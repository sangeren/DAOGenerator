namespace DAOGenerator
{
    using Microsoft.CSharp;
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Data.Entity.Design.PluralizationServices;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class POCOGenerator : BaseGenerator, IGenerator
    {
        private string _outputPath;
        private string _namespaceName;
        private List<Mapping> _mapping;
        private PluralizationService _pluralSvc;

        public POCOGenerator(string outputPath, string namespaceName, string databaseName)
        {
            _outputPath = outputPath;
            _namespaceName = string.Format("{0}.{1}.{2}", namespaceName, databaseName, "Model");
            _mapping = MapperHelper.GetMappingInformation().ToList();
            _pluralSvc = PluralizationService.CreateService(new CultureInfo("en-us"));
        }

        public void Generate(Table table)
        {
            //grammar
            string tableName = MiscHelper.ReformatString(table.Name);

            if (_pluralSvc.IsPlural(tableName))
            {
                tableName = _pluralSvc.Singularize(tableName);
            }            

            string fileName = string.Format("{0}\\{1}.{2}", _outputPath, tableName, _fileType);            

            TextWriter tw = new StreamWriter(new FileStream(fileName, FileMode.Create));

            var codeProvider = CodeDomProvider.CreateProvider(_codeToGenerate);

            var ns = CodeGenHelper.CreateNamespace(_namespaceName);
            ns.Imports.Add(new CodeNamespaceImport("System"));

            var cls = CodeGenHelper.CreateClass(tableName, true);

            foreach(var schema in table.Schemas)
            {
                bool isNullable = false;               
                string Name = MiscHelper.ReformatString(schema.ColumnName);
                string type = _mapping.First(p => p.mySqlDataType.Equals(schema.DataType)).ClassName;
                if (schema.Nullable.Equals("YES"))
                {
                    if (!schema.DataType.ToLower().Equals("varchar"))
                        isNullable = true;
                }
                cls.Members.AddRange(CodeGenHelper.CreateGetSetMethod(schema.ColumnName, Name, type, isNullable));
            }            

            ns.Types.Add(cls);

            //generate the source code file
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BracingStyle = "C";

            codeProvider.GenerateCodeFromNamespace(ns, tw, options);

            //close the text writer
            tw.Close();

        }

        public void Generate(List<Table> tables)
        {
            throw new NotImplementedException();
        }
    }
}
