using System;
using System.Collections.Generic;
using System.Linq;
using GridInventory.Inventory.Data;
using GridInventory.Inventory.Extensions;
using GridInventory.Utils;
using GTANetworkAPI;

namespace GridInventory.Inventory
{
    class InventoryHandler : Script
    {
        [RemoteEvent("Inventory:onMoveItem")]
        public void OnMoveItem(Player player, string instanceId, int sourceInvIdx, int destInvIdx, string newPos, bool isRotated)
        {
            try
            {
                int[] newPosition = newPos.Trim('[', ']').Split(',').Select(int.Parse).ToArray();

                Inventory playerInv = player.GetInventory();
                if (playerInv == null)
                    return;

                Inventory secondaryInv = player.GetClosestInventoryInRange(3f);
                Inventory sourceInv = (sourceInvIdx == 0) ? playerInv : secondaryInv;
                Inventory destInv = (destInvIdx == 0) ? playerInv : secondaryInv;

                if (sourceInv == null || destInv == null)
                    return;

                Item movingItem = sourceInv.Items.FirstOrDefault(i => i.InstanceId == instanceId);
                if (movingItem == null)
                    return;

                movingItem.IsRotated = isRotated;

                bool removed = sourceInv.Items.Remove(movingItem);
                if (!removed)
                    return;

                DatabaseItem dbItem = ItemService.SearchItemByUuid(movingItem.ItemUuid);
                if (dbItem == null)
                {
                    sourceInv.Items.Add(movingItem);
                    return;
                }

                float destWeight = destInv.Items.Sum(i =>
                {
                    var otherDb = ItemService.SearchItemByUuid(i.ItemUuid);
                    return otherDb?.Weight ?? 0;
                });
                if (destWeight + dbItem.Weight > destInv.MaxWeight)
                {
                    sourceInv.Items.Add(movingItem);
                    return;
                }

                bool collision = IsCollision(destInv, movingItem, newPosition[0], newPosition[1]);
                if (collision)
                {
                    sourceInv.Items.Add(movingItem);
                    return;
                }

                movingItem.Row = newPosition[0];
                movingItem.Column = newPosition[1];
                destInv.Items.Add(movingItem);

                switch (sourceInv.Type)
                {
                    case Type.PlayerInventory:
                        player.SetInventory(sourceInv);
                        break;
                    case Type.VehicleInventory:
                        Vehicle vehSrc = NAPI.Pools.GetAllVehicles()
                            .FirstOrDefault(x => x.Found() && x.Position.IsInRange(player.Position, 3f));
                        vehSrc?.SetInventory(sourceInv);
                        break;
                    case Type.PedInventory:
                        Ped pedSrc = NAPI.Pools.GetAllPeds()
                            .FirstOrDefault(x => x.Found() && x.Position.IsInRange(player.Position, 3f));
                        pedSrc?.SetInventory(sourceInv);
                        break;
                }

                switch (destInv.Type)
                {
                    case Type.PlayerInventory:
                        player.SetInventory(destInv);
                        break;
                    case Type.VehicleInventory:
                        Vehicle vehDst = NAPI.Pools.GetAllVehicles()
                            .FirstOrDefault(x => x.Found() && x.Position.IsInRange(player.Position, 3f));
                        vehDst?.SetInventory(destInv);
                        break;
                    case Type.PedInventory:
                        Ped pedDst = NAPI.Pools.GetAllPeds()
                            .FirstOrDefault(x => x.Found() && x.Position.IsInRange(player.Position, 3f));
                        pedDst?.SetInventory(destInv);
                        break;
                }
            }
            catch (Exception ex)
            {
                NAPI.Util.ConsoleOutput(ex.ToString());
            }
        }

        private bool IsCollision(Inventory inv, Item movingItem, int newRow, int newCol)
        {
            DatabaseItem dbItem = ItemService.SearchItemByUuid(movingItem.ItemUuid);
            if (dbItem == null)
                return true;

            int itemRows = dbItem.RowsColumns.Item1;
            int itemCols = dbItem.RowsColumns.Item2;

            if (movingItem.IsRotated)
            {
                int tmp = itemRows;
                itemRows = itemCols;
                itemCols = tmp;
            }

            if (newRow < 0 || newCol < 0 || newRow + itemRows > inv.Rows || newCol + itemCols > inv.Columns)
                return true;

            foreach (var i in inv.Items)
            {
                if (ReferenceEquals(i, movingItem))
                    continue;

                DatabaseItem otherDb = ItemService.SearchItemByUuid(i.ItemUuid);
                if (otherDb == null) continue;

                int otherRows = otherDb.RowsColumns.Item1;
                int otherCols = otherDb.RowsColumns.Item2;

                if (i.IsRotated)
                {
                    int tmp = otherRows;
                    otherRows = otherCols;
                    otherCols = tmp;
                }

                bool overlap = !(
                    (newRow + itemRows <= i.Row) ||
                    (newRow >= i.Row + otherRows) ||
                    (newCol + itemCols <= i.Column) ||
                    (newCol >= i.Column + otherCols)
                );

                if (overlap)
                    return true;
            }
            
            return false;
        }

        [RemoteProc("Inventory:fetchData")]
        public string FetchInventoryData(Player player)
        {
            if (player.Found())
            {
                List<string> inventories = new List<string>();
                inventories.Add(player.GetInventory().Parse());
                Inventory secondaryInventory = player.GetClosestInventoryInRange(3f);
                if (secondaryInventory != null)
                    inventories.Add(secondaryInventory.Parse());

                return NAPI.Util.ToJson(inventories);
            }

            return null;
        }
    }
}
