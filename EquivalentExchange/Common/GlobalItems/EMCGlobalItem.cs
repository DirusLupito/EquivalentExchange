using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;

namespace EquivalentExchange.Common.GlobalItems
{
    public class EMCGlobalItem : GlobalItem
    {
        // The EMC value for this item
        public long emc;
        
        // Static localized text for tooltip
        public static LocalizedText EMCValueText { get; private set; }

        public override void SetStaticDefaults() {
            // Set up localized text for the tooltip
            EMCValueText = Mod.GetLocalization($"{nameof(EMCGlobalItem)}.EMCValue");
        }

        // Makes each item have its own instance of this GlobalItem
        public override bool InstancePerEntity => true;

        // Set default EMC values based on item properties
        public override void SetDefaults(Item item) {
            // Example: Base EMC on sell value as a starting point
            emc = item.value;
            // etc.
        }

        // Add EMC value to tooltips
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
            // Add EMC tooltip after other tooltips - color formatting can be adjusted
            tooltips.Add(new TooltipLine(Mod, "EMCValue", $"EMC: {emc}") { 
                OverrideColor = new Color(255, 255, 255) 
            });
        }

        // Save EMC value when the game is saved
        public override void SaveData(Item item, TagCompound tag) {
            tag.Add("EMC", emc);
        }

        // Load EMC value when the game is loaded
        public override void LoadData(Item item, TagCompound tag) {
            emc = tag.GetLong("EMC");
        }

        // Handle stacking of items with EMC
        public override void OnStack(Item destination, Item source, int numTransferred) {
            // When items stack, make sure EMC transfers properly
            EMCGlobalItem sourceEMC = source.GetGlobalItem<EMCGlobalItem>();
            
            // Calculate proportional EMC from the source stack
            if (source.stack > 0) {
                long emcPerItem = sourceEMC.emc / source.stack;
                emc += emcPerItem * numTransferred;
            }
        }
    }
}
