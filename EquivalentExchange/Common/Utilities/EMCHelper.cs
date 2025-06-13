using Terraria;
using Terraria.ModLoader;
using EquivalentExchange.Common.GlobalItems;
using EquivalentExchange.Common.Players;

namespace EquivalentExchange.Common.Utilities
{
    public static class EMCHelper
    {
        public static long GetEMC(Item item) {
            return item.GetGlobalItem<EMCGlobalItem>().emc;
        }

        public static void SetEMC(Item item, long value) {
            item.GetGlobalItem<EMCGlobalItem>().emc = value;
        }

        public static void AddEMC(Item item, long value) {
            item.GetGlobalItem<EMCGlobalItem>().emc += value;
        }

        // Helper method to convert an item to raw EMC
        public static long ConvertItemToEMC(Item item) {
            // Calculate based on stack size
            return item.GetGlobalItem<EMCGlobalItem>().emc * item.stack;
        }
        
        // Add EMC to player
        public static void AddEMCToPlayer(Player player, long value) {
            if (player.TryGetModPlayer(out EMCPlayer emcPlayer)) {
                emcPlayer.AddEMC(value);
            }
        }
        
        // Get player's stored EMC
        public static long GetPlayerEMC(Player player) {
            if (player.TryGetModPlayer(out EMCPlayer emcPlayer)) {
                return emcPlayer.storedEMC;
            }
            return 0;
        }
    }
}
