using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nicehavva.AdvancedTCP.Shared.Messages;

namespace Nicehavva.AdvancedTCP.Server
{
    public class Delegates
    {
        public delegate void ClientValidatingDelegate(EventArguments.ClientValidatingEventArgs args);
        public delegate void ClientBasicDelegate(Receiver receiver);
    }
}
