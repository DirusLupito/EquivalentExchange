using Terraria;
using EquivalentExchange.Common.GlobalItems;
using EquivalentExchange.Common.Players;

namespace EquivalentExchange.Common.Utilities
{
    public static class EMCHelper
    {
        public static RationalNumber GetEMC(Item item) {
            return item.GetGlobalItem<EMCGlobalItem>().emc;
        }

        public static void SetEMC(Item item, RationalNumber value) {
            item.GetGlobalItem<EMCGlobalItem>().emc = value;
        }

        public static void AddEMC(Item item, RationalNumber value) {
            item.GetGlobalItem<EMCGlobalItem>().emc += value;
        }

        // Helper method to convert an item to raw EMC
        public static RationalNumber ConvertItemToEMC(Item item) {
            // Calculate based on stack size
            return item.GetGlobalItem<EMCGlobalItem>().emc * item.stack;
        }
        
        // Add EMC to player
        public static void AddEMCToPlayer(Player player, RationalNumber value) {
            if (player.TryGetModPlayer(out EMCPlayer emcPlayer)) {
                emcPlayer.AddEMC(value);
            }
        }
        
        // Get player's stored EMC
        public static RationalNumber GetPlayerEMC(Player player) {
            if (player.TryGetModPlayer(out EMCPlayer emcPlayer)) {
                return emcPlayer.storedEMC;
            }
            return RationalNumber.Zero;
        }
    }
}
