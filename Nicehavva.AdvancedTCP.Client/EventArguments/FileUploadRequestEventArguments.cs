using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nicehavva.AdvancedTCP.Shared.Messages;

namespace Nicehavva.AdvancedTCP.Client.EventArguments
{
    public class FileUploadRequestEventArguments
    {
        public FileUploadRequest Request { get; set; }
        private Action ConfirmAction;
        private Action RefuseAction;

        public FileUploadRequestEventArguments(Action confirmAction, Action refuseAction)
        {
            ConfirmAction = confirmAction;
            RefuseAction = refuseAction;
        }

        public void Confirm(String fileName)
        {
            Request.DestinationFilePath = fileName;
            ConfirmAction();
        }

        public void Refuse()
        {
            RefuseAction();
        }
    }
}
