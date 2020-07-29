using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nicehavva.AdvancedTCP.Shared.Messages
{
    [Serializable]
    public class FileUploadRequest : RequestMessageBase
    {
        public FileUploadRequest()
        {
            BufferSize = 1024*500;
        }

        public FileUploadRequest(FileUploadResponse response)
            : this()
        {
            ReceiverClient = response.SenderClient;
            CallbackID = response.CallbackID;
            FileName = response.FileName;
            TotalBytes = response.TotalBytes;
            CurrentPosition = response.CurrentPosition;
            SourceFilePath = response.SourceFilePath;
            DestinationFilePath = response.DestinationFilePath;
        }

        public FileUploadRequest(FileUploadRequest request)
            : this()
        {
            ReceiverClient = request.ReceiverClient;
            CallbackID = request.CallbackID;
            FileName = request.FileName;
            TotalBytes = request.TotalBytes;
            CurrentPosition = request.CurrentPosition;
            SourceFilePath = request.SourceFilePath;
            DestinationFilePath = request.DestinationFilePath;
        }

        public String FileName { get; set; }
        public long TotalBytes { get; set; }
        public long CurrentPosition { get; set; }
        public String SourceFilePath { get; set; }
        public String DestinationFilePath { get; set; }
        public Byte[] BytesToWrite { get; set; }
        public int BufferSize { get; set; }
        public int DataLength { get; set; }
    }
}
