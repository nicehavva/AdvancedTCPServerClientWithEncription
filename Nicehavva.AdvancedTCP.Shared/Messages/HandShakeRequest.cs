using Nicehavva.AdvancedTCP.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nicehavva.AdvancedTCP.Shared.Messages
{
    [Serializable]
    public class HandShakeRequest : RequestMessageBase
    {
        public byte[] FirstX { get; set; }
        public byte[] U { get; set; }
        public byte[] SelectChoas { get; set; }
    }
}
