using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MonitorViewer
{
    [DataContract]
    public class Metadata
    {
        [DataMember]
        public DateTimeOffset DTCreation { get; set; }

        [DataMember]
        public DateTimeOffset DTChanged { get; set; }

        [DataMember]
        public string AuthorName { get; set; }
    }
}
