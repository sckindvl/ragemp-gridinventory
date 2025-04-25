using System;
using System.Collections.Generic;
using System.Linq;
using GridInventory.Inventory.Data;
using GridInventory.Inventory.Extensions;
using GTANetworkAPI;
using Type = GridInventory.Inventory.Type;

namespace GridInventory
{
    public class Main : Script
    {
        [ServerEvent(Event.ResourceStart)]
        public void OnResourceStart()
        {
            ItemService.LoadServerItems();
        }
        
        [ServerEvent(Event.PlayerConnected)]
        public void OnPlayerConnect(Player player)
        {
            player.InitializeInventory(Type.PlayerInventory, 10, 10, 40f);
            player.AddItem("canofsoda", 3);
            player.AddItem("drugbox_filled", 1);

            Ped randomPed = NAPI.Ped.CreatePed(PedHash.AviSchwartzman, new Vector3(-429, 1110, 327), 0, 0);
            randomPed.InitializeInventory(Type.PedInventory, 8, 8, 25f);

            DummyEntity dummyEntity = NAPI.DummyEntity.CreateDummyEntity(0, new Dictionary<string, object>(), player.Dimension);
            dummyEntity.InitializeInventory(Type.PlayerInventory, 4, 4, 5f);
        }
    }
}