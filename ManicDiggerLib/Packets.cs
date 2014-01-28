﻿using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using ManicDigger;

namespace GameModeFortress
{
    [ProtoContract]
    public class PacketServerInventory
    {
        /*
        [ProtoMember(1, IsRequired = false)]
        public bool IsFinite;
        [ProtoMember(2, IsRequired = false)]
        public Dictionary<int, int> BlockTypeAmount = new Dictionary<int, int>();
        [ProtoMember(3, IsRequired = false)]
        public int Max = 200;
        */
        [ProtoMember(4, IsRequired = false)]
        public ManicDigger.Inventory Inventory;
    }
    [ProtoContract]
    public class PacketServerPlayerStats
    {
        [ProtoMember(1, IsRequired = false)]
        public int CurrentHealth = 20;
        [ProtoMember(2, IsRequired = false)]
        public int MaxHealth = 20;
    }
}
