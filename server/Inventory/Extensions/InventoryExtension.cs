using System;
using System.Collections.Generic;
using System.Linq;
using GridInventory.Inventory.Data;
using GridInventory.Utils;
using GTANetworkAPI;

namespace GridInventory.Inventory.Extensions
{
    public static partial class InventoryExtension
    {
        private static readonly string InventoryKey = "inventory";

        public static void InitializeInventory(this Entity entity, Type inventoryType, int rows, int columns, float maxWeight)
        {
            Inventory inventory = new Inventory();
            
            inventory.Type = inventoryType;
            inventory.Identifier = entity.Id; // This can be replaced with Database values, if needed.
            
            inventory.Rows = rows;
            inventory.Columns = columns;
            inventory.MaxWeight = maxWeight;
            
            if (!entity.HasData(InventoryKey))
                entity.SetData(InventoryKey, NAPI.Util.ToJson(inventory));
        }

        public static Inventory GetInventory(this Entity entity)
        {
            try
            {
                return NAPI.Util.FromJson<Inventory>(entity.GetData<string>(InventoryKey));
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static bool SetInventory(this Entity entity, Inventory inventory)
        {
            try
            {
                string inventoryJson = NAPI.Util.ToJson(inventory);
                entity.SetData(InventoryKey, inventoryJson);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        
        public static bool HasItem(this Entity entity, string itemSlug)
        {
            Inventory inventory = entity.GetInventory();
            if (inventory != null)
            {
                return inventory.Items.Exists(x => x != null && x.ItemUuid == itemSlug);
            }

            return false;
        }
        
        public static Item GetItem(this Entity entity, string itemSlug)
        {
            Inventory inventory = entity.GetInventory();
            if (inventory != null)
            {
                Item searchedItem = inventory.Items.FirstOrDefault(x => x != null && x.ItemUuid == itemSlug);
                return searchedItem;
            }

            return null;
        }

        public static int GetItemAmount(this Entity entity, string itemSlug)
        {
            return entity.GetItem(itemSlug).Amount ?? 0;
        }

        public static bool AddItem(this Entity entity, string itemSlug, int amount)
        {
            Inventory inventory = entity.GetInventory();
            if (inventory != null)
            {
                DatabaseItem searchedItem = ItemService.SearchItem(itemSlug);
                if (searchedItem != null)
                {
                    inventory.Add(searchedItem, amount);
                    return entity.SetInventory(inventory);
                }
            }

            return false;
        }

        public static Inventory GetClosestInventoryInRange(this Entity entity, float distance)
        {
            List<Vehicle> vehicles = NAPI.Pools.GetAllVehicles().Where(x => x.Found() && x.Position.IsInRange(entity.Position, distance)).ToList();
            if (vehicles.Count > 0)
                return vehicles.FirstOrDefault().GetInventory();

            List<Ped> peds = NAPI.Pools.GetAllPeds().Where(x => x.Found() && x.Position.IsInRange(entity.Position, distance)).ToList();
            if (peds.Count > 0)
                return peds.FirstOrDefault().GetInventory();
            
            return null;
        }
    }
}