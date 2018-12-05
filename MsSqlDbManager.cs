namespace DAOGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class MsSqlDbManager
    {
        private string _connectionString;

        public MsSqlDbManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IList<T> ExecuteQuery<T>(string selectStatement, List<Parameter> parameters, bool check = true)
        {
            var results = new List<T>();
            

            return results;
        }
    }
}
