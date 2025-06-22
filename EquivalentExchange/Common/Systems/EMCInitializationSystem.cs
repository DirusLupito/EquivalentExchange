using EquivalentExchange.Common.GlobalItems;
using EquivalentExchange.Common.Utilities;
using Terraria.ModLoader;

namespace EquivalentExchange.Common.Systems
{
    public class EMCInitializationSystem : ModSystem
    {

        // Calls the EMC calculator and make it set up all the item EMC values for all the items (hopefully) across not
        // just the vanilla game, but also all the mods that are loaded.
        public override void PostSetupRecipes()
        {
            base.PostSetupRecipes();
            EMCGlobalItem.ItemEMCValues = EMCCalculator.CalculateEMCValues();
            EMCGlobalItem.EMCAlgorithmInitialized = true;
        }
    }
}

