using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nicehavva.AdvancedTCP.Shared.Messages
{
    [Serializable]
    public class SessionResponse : ResponseMessageBase
    {
        public SessionResponse(RequestMessageBase request)
            : base(request)
        {

        }

        public bool IsConfirmed { get; set; }
        public string ClientName { get; set; }
        public string PublicKey { get; set; }
    }
}
