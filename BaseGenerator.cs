namespace DAOGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public abstract class BaseGenerator
    {
        public string _codeToGenerate;
        public string _fileType;

        public BaseGenerator()
        {
            _codeToGenerate = ConfigurationManager.AppSettings["CodeToGenerate"];

            switch (_codeToGenerate)
            {
                case "CSharp":
                    _fileType = "cs";
                    break;
                case "VisualBasic":
                    _fileType = "vb";
                    break;
            }
        }
    }
}
