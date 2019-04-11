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
    public class SaveFile
    {
        public enum Algorithm
        {
            /// <summary>
            /// An algorithm of LSB->MSB 16-bit data.
            /// See more: https://physionet.org/physiotools/wag/signal-5.htm.
            /// </summary>
            DumbDB
        }

        /// <summary>
        /// The algorithm used to save (and read) the data. 
        /// </summary>
        [DataMember]
        public Algorithm SavingAlgorithm { get; set; }

        [DataMember]
        public short[] FHR_Data { get; set; }

        [DataMember]
        public short[] UC_Data { get; set; }

        /// <summary>
        /// Header record information for this data.
        /// </summary>
        [DataMember]
        public Header Header { get; set; } = new Header();

        /// <summary>
        /// All of the markings/annotations made.
        /// </summary>
        [DataMember]
        public List<Marking> Markings { get; set; } = new List<Marking>();

        /// <summary>
        /// Metadata about the file.
        /// </summary>
        [DataMember]
        public Metadata Metadata { get; set; } = new Metadata();

        public static SaveFile GetSaveFile(string XML)
        {
            var serializer = new DataContractSerializer(typeof(SaveFile));

            // https://stackoverflow.com/questions/12554186/how-to-serialize-deserialize-to-dictionaryint-string-from-custom-xml-not-us
            // https://stackoverflow.com/questions/1879395/how-do-i-generate-a-stream-from-a-string

            object result = (SaveFile)(serializer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(XML ?? ""))));

            return (SaveFile)result;
        }

        public string GetXML()
        {
            var serializer = new DataContractSerializer(typeof(SaveFile), new DataContractSerializerSettings() { PreserveObjectReferences = false });

            string xml;

            using (var sw = new System.IO.StringWriter())
            {
                using (var writer = new XmlTextWriter(sw))
                {
                    writer.Formatting = Formatting.None; // indent the Xml so it's human readable
                    serializer.WriteObject(writer, this);
                    writer.Flush();
                    xml = sw.ToString();
                }
            }

            return xml;
        }
    }
}
