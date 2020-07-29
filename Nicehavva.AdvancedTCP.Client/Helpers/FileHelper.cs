using Nicehavva.AdvancedTCP.Shared.Messages;
using Nicehavva.AdvancedTCP.Shared.Models;
using Nicehavva.AdvancedTCP.Shared.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nicehavva.AdvancedTCP.Client.Helpers
{
    public class FileHelper
    {
        public static byte[] SampleBytesFromFile(String filePath, int currentPosition, int bufferSize)
        {
            int length = bufferSize;
            FileStream fs = new FileStream(filePath, FileMode.Open);
            fs.Position = currentPosition;

            if (currentPosition + length > fs.Length)
            {
                length = (int)(fs.Length - currentPosition);
            }

            byte[] b = new byte[length];
            fs.Read(b, 0, length);
            fs.Dispose();
            return b;
        }

        public static long GetFileLength(String filePath)
        {
            FileInfo info = new FileInfo(filePath);
            return info.Length;
        }

        public static void AppendAllBytes(String filePath, byte[] bytes)
        {
            FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write);
            fs.Write(bytes, 0, bytes.Length);
            fs.Dispose();
        }
        public static void AppendAllBytes(Client client,List<FileUploadRequest> fileUploadRequests, Dictionary<string, EncryptionkeyObject> clientPublicEncryptionkeys)
        {
            if (fileUploadRequests.Count > 0)
            {
                var item0 = fileUploadRequests[0];

                var encryptionkey = clientPublicEncryptionkeys[item0.SenderClient];
                var dycriptdata = UtilityFunction.EncryptByte(item0.BytesToWrite, encryptionkey.FirstX, encryptionkey.U, encryptionkey.SelectChoas);

                FileStream fs = new FileStream(item0.DestinationFilePath, FileMode.Append, FileAccess.Write);
                
                FileUploadResponse response = new FileUploadResponse(item0);
                if (fs.Position != item0.CurrentPosition - item0.DataLength)
                {
                    response.HasError = true;
                    response.Exception = new Exception("The file sequence not correct!");
                    client.SendMessage(response);
                    fileUploadRequests.RemoveAll(x => x.DestinationFilePath == item0.DestinationFilePath);
                    //fileUploadRequests.RemoveAt(0);
                    fs.Dispose();
                    File.Delete(item0.DestinationFilePath);
                    return;
                }
                else
                {
                    client.SendMessage(response);
                    client.OnUploadFileProgress(new EventArguments.FileUploadProgressEventArguments() { CurrentPosition = item0.CurrentPosition, FileName = item0.FileName, TotalBytes = item0.TotalBytes, DestinationPath = item0.DestinationFilePath });
                }
                fs.Write(dycriptdata, 0, item0.DataLength);
                fileUploadRequests.RemoveAt(0);
                while (fileUploadRequests.Count > 0 && fileUploadRequests[0].DestinationFilePath== item0.DestinationFilePath)
                {
                    item0 = fileUploadRequests[0];
                    dycriptdata = UtilityFunction.EncryptByte(item0.BytesToWrite, encryptionkey.FirstX, encryptionkey.U, encryptionkey.SelectChoas);
                    
                    response = new FileUploadResponse(item0);
                    if (fs.Position != item0.CurrentPosition - item0.DataLength)
                    {
                        response.HasError = true;
                        response.Exception = new Exception("The file sequence not correct!");
                        client.SendMessage(response);
                        fileUploadRequests.RemoveAll(x => x.DestinationFilePath == item0.DestinationFilePath);
                        //fileUploadRequests.RemoveAt(0);
                        fs.Dispose();
                        File.Delete(item0.DestinationFilePath);
                        return;
                    }
                    else
                    {
                        client.SendMessage(response);
                        client.OnUploadFileProgress(new EventArguments.FileUploadProgressEventArguments() { CurrentPosition = item0.CurrentPosition, FileName = item0.FileName, TotalBytes = item0.TotalBytes, DestinationPath = item0.DestinationFilePath });
                    }

                    fs.Write(dycriptdata, 0, item0.DataLength);
                    fileUploadRequests.RemoveAt(0);
                }
                fs.Dispose();
            }
        }
    }
}
