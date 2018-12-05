namespace DAOGenerator
{
    using System.Collections.Generic;

    interface IGenerator
    {
        void Generate(Table table);
        void Generate(List<Table> tables);
    }
}
