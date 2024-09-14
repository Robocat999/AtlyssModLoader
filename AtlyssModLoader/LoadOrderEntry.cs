using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlyssModLoader
{
    internal class LoadOrderEntry
    {
        public string ModName { get; set; }
        public int LoadOrder {  get; set; }
        public int InternalVersion { get; set; }
        public string ExternalVersion { get; set; }
    }
}
