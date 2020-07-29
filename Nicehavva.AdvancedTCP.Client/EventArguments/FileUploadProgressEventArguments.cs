using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nicehavva.AdvancedTCP.Client.EventArguments
{
    public class FileUploadProgressEventArguments
    {
        public String DestinationPath { get; set; }
        public String FileName { get; set; }
        public long CurrentPosition { get; set; }
        public long TotalBytes { get; set; }
    }
}
