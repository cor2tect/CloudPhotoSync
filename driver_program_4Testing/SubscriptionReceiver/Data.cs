using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubscriptionReceiver
{
    [ProtoContract]
    public class Data
    {
        [ProtoMember(1)]
        public string command;
        [ProtoMember(2)]
        public string dir;
    }
}
