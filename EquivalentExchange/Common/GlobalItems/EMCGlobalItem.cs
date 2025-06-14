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
            // Base EMC on sell value as a starting point
            emc = item.value;
            // Todo: Base EMC on crafting components and fundamental item EMC values
        }

        // Add EMC value to tooltips
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            // Add EMC tooltip after other tooltips
            tooltips.Add(new TooltipLine(Mod, "EMCValue", $"EMC: {emc}")
            {
                OverrideColor = new Color(255, 255, 255)
            });
            // Add stack EMC value if applicable
            if (item.stack > 1) {
                tooltips.Add(new TooltipLine(Mod, "StackEMCValue", $"Stack EMC: {emc * item.stack}")
                {
                    OverrideColor = new Color(200, 200, 200)
                });
            }
        }

        // Save EMC value when the game is saved
        public override void SaveData(Item item, TagCompound tag) {
            tag.Add("EMC", emc);
        }

        // Load EMC value when the game is loaded
        public override void LoadData(Item item, TagCompound tag) {
            emc = tag.GetLong("EMC");
        }
    }
}
