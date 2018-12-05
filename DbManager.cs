namespace DAOGenerator
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Transactions;

    public class DbManager
    {
        private string _connectionString;

        public DbManager(string connetionString)
        {
            _connectionString = connetionString;
        }

        public IList<T> ExecuteQuery<T>(string selectStatement, List<Parameter> parameters, bool check = true)
        {
            var results = new List<T>();
            var factory = MySqlClientFactory.Instance;
            var type = typeof(T);
            var properties = type.GetProperties();

            try
            {
                using (var con = factory.CreateConnection())
                {
                    con.ConnectionString = _connectionString;
                    con.Open();

                    var cmd = con.CreateCommand();
                    cmd.CommandText = selectStatement;

                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            var prm = new MySqlParameter();
                            prm.ParameterName = parameter.Name;
                            prm.Value = parameter.Value;
                            cmd.Parameters.Add(prm);
                        }
                    }

                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            if (check)
                            {
                                int countCheck = properties.Count();
                                int offset = properties.Count(c => c.PropertyType.IsGenericType == true);

                                //verify generic is a List
                                foreach (var prop in properties.Where(p => p.PropertyType.IsGenericType == true))
                                {
                                    if (prop.PropertyType.GetGenericTypeDefinition() != typeof(List<>))
                                    {
                                        offset--;
                                    }
                                }

                                countCheck -= offset;

                                if (countCheck != dr.FieldCount)
                                {
                                    throw new Exception("Return columns missmatch with POCO object properties.");
                                }
                            }                            

                            var obj = Activator.CreateInstance(typeof(T));

                            for (int i = 0; i < dr.FieldCount; i++)
                            {
                                var property = properties[i];

                                var valueType = dr[i].GetType();
                                switch (valueType.FullName)
                                {
                                    case "System.String":
                                        var valueString = DbConversion.ConvertToString(dr[i]);
                                        var xString = obj.GetType().GetProperty(property.Name);
                                        xString.SetValue(obj, valueString, null);
                                        break;
                                    case "System.Int16":
                                    case "System.Int32":
                                    case "System.Int64":
                                    case "System.UInt64":
                                        var valueInt = DbConversion.ConvertToInt(dr[i]);
                                        var xInt = obj.GetType().GetProperty(property.Name);
                                        xInt.SetValue(obj, valueInt, null);
                                        break;
                                }
                            }

                            results.Add((T)obj);
                        }
                    }

                    con.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to execute ExecuteQuery method. Error message : {0}.", ex.Message));
            }

            return results;
        }
    }
}
