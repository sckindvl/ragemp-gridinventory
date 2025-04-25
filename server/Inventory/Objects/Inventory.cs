using System;
using System.Collections.Generic;
using System.Linq;
using GridInventory.Inventory.Data;
using GTANetworkAPI;

namespace GridInventory.Inventory
{
    public class Inventory
    {
        public Type Type { get; set; }
        public int Identifier { get; set; }

        public string Title
        {
            get
            {
                switch (Type)
                {
                    case Type.PlayerInventory:
                        return "Player inventory";
                    case Type.VehicleInventory:
                        return "Vehicle";
                    case Type.PedInventory:
                        return "Ped";
                    default:
                        return "Inventory";
                }
            }
        }

        public int Rows { get; set; }
        public int Columns { get; set; }
        public float MaxWeight { get; set; }
        public List<Item> Items { get; set; } = new List<Item>();

        public void Add(DatabaseItem dbItem, int amount)
        {
            if (amount <= 0) return;

            float currentWeight = Items.Sum(item =>
            {
                var itemDb = ItemService.SearchItemByUuid(item.ItemUuid);
                return (itemDb?.Weight ?? 0f) * (item.Amount ?? 1);
            });

            float additionalWeight = dbItem.Weight * amount;

            if (currentWeight + additionalWeight > MaxWeight)
            {
                int allowedAmount = (int)((MaxWeight - currentWeight) / dbItem.Weight);
                if (allowedAmount <= 0)
                    return;

                amount = allowedAmount;
            }

            var tempItems = Items.Select(x => new Item
            {
                ItemUuid = x.ItemUuid,
                InstanceId = x.InstanceId,
                Amount = x.Amount,
                Row = x.Row,
                Column = x.Column,
                IsRotated = x.IsRotated
            }).ToList();

            var stacks = tempItems
                .Where(i => i.ItemUuid == dbItem.ItemUuid && i.Amount.HasValue && i.Amount.Value < dbItem.MaxStack)
                .ToList();

            int availableInExisting = stacks.Sum(i => dbItem.MaxStack - i.Amount.Value);
            int addToExisting = Math.Min(amount, availableInExisting);
            int remaining = amount - addToExisting;
            int toAdd = addToExisting;

            foreach (var item in stacks)
            {
                int space = dbItem.MaxStack - item.Amount.Value;
                int add = Math.Min(space, toAdd);
                item.Amount += add;
                toAdd -= add;
                if (toAdd <= 0) break;
            }

            int newStacksNeeded = dbItem.MaxStack > 0
                ? (int)Math.Ceiling((double)remaining / dbItem.MaxStack)
                : 0;

            if (newStacksNeeded <= 0)
            {
                Items = tempItems;
                return;
            }

            bool[,] occupancy = new bool[Rows, Columns];

            foreach (var item in tempItems)
            {
                var itemDb = ItemService.SearchItemByUuid(item.ItemUuid);
                if (itemDb == null) continue;
                (int rCount, int cCount) = itemDb.RowsColumns;
                if (item.IsRotated)
                {
                    var tmp = rCount;
                    rCount = cCount;
                    cCount = tmp;
                }

                for (int r = item.Row; r < item.Row + rCount && r < Rows; r++)
                {
                    for (int c = item.Column; c < item.Column + cCount && c < Columns; c++)
                    {
                        occupancy[r, c] = true;
                    }
                }
            }

            for (int s = 0; s < newStacksNeeded; s++)
            {
                int stackAmount = (s < newStacksNeeded - 1)
                    ? dbItem.MaxStack
                    : remaining - dbItem.MaxStack * (newStacksNeeded - 1);

                var standardPos = FindFirstFit(occupancy, dbItem.RowsColumns.Item1, dbItem.RowsColumns.Item2);
                if (standardPos != null)
                {
                    MarkOccupancy(occupancy, standardPos.Value, dbItem.RowsColumns.Item1, dbItem.RowsColumns.Item2, true);
                    tempItems.Add(new Item
                    {
                        ItemUuid = dbItem.ItemUuid,
                        InstanceId = Guid.NewGuid().ToString(),
                        Amount = stackAmount,
                        Row = standardPos.Value.row,
                        Column = standardPos.Value.col,
                        IsRotated = false
                    });
                }
                else
                {
                    var rotatedPos = FindFirstFit(occupancy, dbItem.RowsColumns.Item2, dbItem.RowsColumns.Item1);
                    if (rotatedPos != null)
                    {
                        MarkOccupancy(occupancy, rotatedPos.Value, dbItem.RowsColumns.Item2, dbItem.RowsColumns.Item1, true);
                        tempItems.Add(new Item
                        {
                            ItemUuid = dbItem.ItemUuid,
                            InstanceId = Guid.NewGuid().ToString(),
                            Amount = stackAmount,
                            Row = rotatedPos.Value.row,
                            Column = rotatedPos.Value.col,
                            IsRotated = true
                        });
                    }
                    else
                    {
                        return;
                    }
                }
            }

            Items = tempItems;
        }

        private (int row, int col)? FindFirstFit(bool[,] occupancy, int itemRows, int itemCols)
        {
            for (int r = 0; r <= Rows - itemRows; r++)
            {
                for (int c = 0; c <= Columns - itemCols; c++)
                {
                    bool canPlace = true;
                    for (int rr = r; rr < r + itemRows && canPlace; rr++)
                    {
                        for (int cc = c; cc < c + itemCols; cc++)
                        {
                            if (occupancy[rr, cc])
                            {
                                canPlace = false;
                                break;
                            }
                        }
                    }

                    if (canPlace) return (r, c);
                }
            }

            return null;
        }

        private void MarkOccupancy(bool[,] occupancy, (int row, int col) start, int itemRows, int itemCols, bool value)
        {
            for (int r = start.row; r < start.row + itemRows; r++)
            {
                for (int c = start.col; c < start.col + itemCols; c++)
                {
                    occupancy[r, c] = value;
                }
            }
        }

        public string Parse()
        {
            var data = new
            {
                Title,
                Rows,
                Columns,
                MaxWeight,
                Items = Items
                    .Where(x => x != null)
                    .Select(item =>
                    {
                        var dbItem = ItemService.SearchItemByUuid(item.ItemUuid);
                        return new
                        {
                            ItemUuid = item.ItemUuid,
                            InstanceId = item.InstanceId,
                            Amount = item.Amount,
                            Row = item.Row,
                            Column = item.Column,
                            RowsColumns = dbItem != null
                                ? new int[] { dbItem.RowsColumns.Item1, dbItem.RowsColumns.Item2 }
                                : new int[] { 1, 1 },
                            Weight = (dbItem?.Weight ?? 0.0f) * (item.Amount ?? 1),
                            IsRotated = item.IsRotated,
                            ShortName = dbItem?.ShortName ?? "Item"
                        };
                    })
                    .ToList()
            };
            return NAPI.Util.ToJson(data);
        }
    }

    public enum Type
    {
        PlayerInventory = 0,
        VehicleInventory = 1,
        PedInventory = 2,
    }
}