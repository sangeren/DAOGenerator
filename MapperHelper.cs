namespace DAOGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class Mapping
    {
        public string mySqlDataType { get; set; }
        public string ClassName { get; set; }
        public string DeclarationString { get; set; }
    }

    public class MapperHelper
    {
        public static IList<Mapping> GetMappingInformation()
        {
            var Result = new List<Mapping>();
            string dataFile = "Mapping.dat";
            string data = System.IO.File.ReadAllText(dataFile);            

            string[] raw = data.Split('\r', '\n');
            var maps = raw.Where(p => !string.IsNullOrEmpty(p)).ToList();

            foreach (var map in maps)
            {
                string[] decode = map.Split('-');
                var item = new Mapping();
                item.mySqlDataType = decode[0];
                item.ClassName = decode[1];
                item.DeclarationString = decode[2];
                Result.Add(item);
            }

            return Result;
        }
    }
}
