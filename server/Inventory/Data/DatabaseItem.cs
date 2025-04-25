using System;
using System.Collections.Generic;
using System.Linq;

namespace GridInventory.Inventory.Data
{
    public class DatabaseItem : Item
    {
        public string ItemUuid { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public float Weight { get; set; }
        public int MaxStack { get; set; }
        public (int, int) RowsColumns { get; set; }
    }
}