using System;
using System.Collections.Generic;
using System.Linq;

namespace GridInventory.Inventory
{
    public class Item
    {
        public string ItemUuid { get; set; }
        public string InstanceId { get; set; }
        public int? Amount { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public bool IsRotated { get; set; }
    }
}