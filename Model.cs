namespace DAOGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class Parameter
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    public class Table
    {
        public string Name { get; set; }
        public string Type { get; set; }
        // Note : List or Array should be declared at the bottom of the class
        public List<Schema> Schemas { get; set; }
    }

    public class Schema
    {
        public string TableCatalog { get; set; }
        public string TableName { get; set; }
        public int? OrdinalPosition { get; set; }
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string Nullable { get; set; }
        public string ColumnKey { get; set; }
    }
}
