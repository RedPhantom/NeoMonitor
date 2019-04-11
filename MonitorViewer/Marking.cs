using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MonitorViewer
{
    [DataContract]
    public class Marking
    {
        [DataMember]
        public int c { get; set; }
        /// <summary>
        /// String
        /// </summary>
        [DataMember]
        public string s { get; set; }
    
        /// <summary>
        /// Starting sample
        /// </summary>
        [DataMember]
        public double to { get; set; }

        /// <summary>
        /// Endling sample
        /// </summary>
        [DataMember]
        public double tc { get; set; }

        public Marking(int Code, string Comment, double Open, double Close)
        {
            c = Code;
            s = Comment;
            to = Open;
            tc = Close;
        }

        public static string GetXML(List<Marking> mks)
        {
            var serializer = new DataContractSerializer(typeof(List<Marking>), new DataContractSerializerSettings() { PreserveObjectReferences = false });

            string xml;

            using (var sw = new System.IO.StringWriter())
            {
                using (var writer = new XmlTextWriter(sw))
                {
                    writer.Formatting = Formatting.None; // indent the Xml so it's human readable
                    serializer.WriteObject(writer, mks);
                    writer.Flush();
                    xml = sw.ToString();
                }
                    
            }

            return xml;
        }

        public static List<Marking> GetMarkings(string XML)
        {
            var serializer = new DataContractSerializer(typeof(List<Marking>));

            // https://stackoverflow.com/questions/12554186/how-to-serialize-deserialize-to-dictionaryint-string-from-custom-xml-not-us
            // https://stackoverflow.com/questions/1879395/how-do-i-generate-a-stream-from-a-string

            object result = (List<Marking>)(serializer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(XML ?? ""))));

            return (List<Marking>)result;
        }
    }
}
