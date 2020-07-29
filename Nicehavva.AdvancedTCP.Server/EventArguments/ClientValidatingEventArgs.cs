using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nicehavva.AdvancedTCP.Shared.Messages;

namespace Nicehavva.AdvancedTCP.Server.EventArguments
{
    public class ClientValidatingEventArgs
    {
        public Receiver Receiver { get; set; }
        public ValidationRequest Request { get; set; }
        private Action ConfirmAction;
        private Action RefuseAction;

        public ClientValidatingEventArgs(Action confirmAction,Action refuseAction)
        {
            ConfirmAction = confirmAction;
            RefuseAction = refuseAction;
        }

        public void Confirm()
        {
            ConfirmAction();
        }

        public void Refuse()
        {
            RefuseAction();
        }
    }
}
