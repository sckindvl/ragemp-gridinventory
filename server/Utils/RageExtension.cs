using System;
using System.Collections.Generic;
using System.Linq;
using GTANetworkAPI;
using GTANetworkMethods;
using Entity = GTANetworkAPI.Entity;
using Player = GTANetworkAPI.Player;

namespace GridInventory.Utils
{
    public static partial class RageExtension
    {
        public static bool Found(this Entity entity)
        {
            if (entity == null || !entity.Exists) return false;
            return true;
        }
        
        public static bool IsInRange(this Vector3 currentPosition, Vector3 otherPosition, float distance)
            => currentPosition.DistanceTo(otherPosition) <= distance;
    }
}