using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nicehavva.AdvancedTCP.Shared.Enums
{
    [Serializable]
    public enum StatusEnum
    {
        Connected,
        Disconnected,
        Validated,
        InSession
    }
}
