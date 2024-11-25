using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlyssModLoader
{
    internal class LoadOrderJsonData
    {
        public int JsonVersion {  get; set; }
        public List<LoadOrderEntry> LoadOrderEntries { get; set; }
    }
}
