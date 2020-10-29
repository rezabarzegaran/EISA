using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EISA.Model;

namespace EISA.Interface
{
    class DataLoader
    {
        public static (Application_model, Architecture_model) Load(params string[] path)
        {
            Application_model workload = Load<Application_model>(path[0]);
            Architecture_model configuration = Load<Architecture_model>(path[1]);

            return (workload, configuration);
        }
        public static T Load<T>(string file)
        {
            object obj = null;
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (FileStream fileStream = new FileStream(file, FileMode.Open))
            {
                obj = serializer.Deserialize(fileStream);
            }

            return (T)obj;
        }

    }
}
