using System;
using System.Collections.Generic;
using System.Linq;
using GTANetworkAPI;

namespace GridInventory.Inventory.Data
{
    public class ItemService : Script
    {
        private static List<DatabaseItem> ServerItems_ = new List<DatabaseItem>();

        public static void LoadServerItems()
        {
            ServerItems_.Add(new DatabaseItem() {
                ItemUuid = "1009ef15050e70a1d4057554", Name = "Can of Soda", ShortName = "Soda", Slug = "canofsoda", Description = "This is a can of soda, 0.33l", Icon = "soda", 
                Weight = 0.5f, MaxStack = 10, RowsColumns = (1, 3) });
            ServerItems_.Add(new DatabaseItem() {
                ItemUuid = "9cb3a47c668ef2426b0111c9", Name = "Filled drug box", ShortName = "Drug Box", Slug = "drugbox_filled", Description = "Filled drug box", Icon = "drugbox", 
                Weight = 5f, MaxStack = 1, RowsColumns = (2, 2) });
            ServerItems_.Add(new DatabaseItem() {
                ItemUuid = "1632c672b08b1f8cca1fa693", Name = "The heaviest item for testing", ShortName = "Heavy", Slug = "heavy", Description = "Heavy", Icon = "medkit", 
                Weight = 10f, MaxStack = 10, RowsColumns = (1, 1) });
        }

        public static DatabaseItem SearchItem(string slug)
        {
            return ServerItems_.FirstOrDefault(x => x != null && x.Slug == slug);
        }
        
        public static DatabaseItem SearchItemByUuid(string uuid)
        {
            return ServerItems_.FirstOrDefault(x => x != null && x.ItemUuid == uuid);
        }
    }
}