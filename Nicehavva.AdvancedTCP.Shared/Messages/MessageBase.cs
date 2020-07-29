using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nicehavva.AdvancedTCP.Shared.Messages
{
    [Serializable]
    public class MessageBase
    {
        public Guid CallbackID { get; set; }
        public bool HasError { get; set; }
        public Exception Exception { get; set; }
        public string ReceiverClient { get; set; }
        public string SenderClient { get; set; }

        public MessageBase()
        {
            Exception = new Exception();
        }
    }
}
