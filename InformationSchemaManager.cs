namespace DAOGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;    

    public class InformationSchemaManager
    {
        private DbManager _objDbManager;
        private string _dbName;

        public InformationSchemaManager(string connectionString, string databaseName)
        {
            _objDbManager = new DbManager(connectionString);
            _dbName = databaseName;
        }

        public List<Table> GetTablesInformation()
        {
            try
            {
                string sqlGetTables = "SELECT TABLE_NAME, TABLE_TYPE FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @tableSchema";
                var prmsTables = new List<Parameter>();
                prmsTables.Add(new Parameter { Name = "@tableSchema", Value = _dbName });
                var tables = _objDbManager.ExecuteQuery<Table>(sqlGetTables, prmsTables);

                foreach (var table in tables)
                {
                    string sql = "SELECT TABLE_CATALOG, TABLE_NAME, ORDINAL_POSITION, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_KEY FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @databaseName AND TABLE_NAME = @tableName";
                    var prms = new List<Parameter>();
                    prms.Add(new Parameter { Name = "@databaseName", Value = _dbName });
                    prms.Add(new Parameter { Name = "@tableName", Value = table.Name });

                    var stg = _objDbManager.ExecuteQuery<Schema>(sql, prms);
                    table.Schemas = stg.ToList();
                }

                return tables.ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
