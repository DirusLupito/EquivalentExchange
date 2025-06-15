using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using EquivalentExchange.Common.Utilities;
using Terraria.ID;

namespace EquivalentExchange.Common.GlobalItems
{
    public class EMCGlobalItem : GlobalItem
    {
        // The EMC value for this item
        public RationalNumber emc = RationalNumber.Zero;

        // Dictionary which maps item IDs to their EMC values
        public static Dictionary<int, RationalNumber> ItemEMCValues { get; private set; } = new Dictionary<int, RationalNumber>();

        // Static flag to check if the EMC calculation algorithm has been run
        public static bool EMCAlgorithmInitialized { get; private set; } = false;
        
        // Static localized text for tooltip
        public static LocalizedText EMCValueText { get; private set; }

        public override void SetStaticDefaults()
        {
            // Set up localized text for the tooltip
            EMCValueText = Mod.GetLocalization($"{nameof(EMCGlobalItem)}.EMCValue");
            // Initialize the dictionary with default values
            ItemEMCValues = EMCCalculator.CalculateEMCValues();
            // Set the static flag to true to indicate the algorithm has been run
            EMCAlgorithmInitialized = true;
        }

        // Makes each item have its own instance of this GlobalItem
        public override bool InstancePerEntity => true;

        // Set default EMC values based on item properties
        public override void SetDefaults(Item item)
        {
            // Run the EMC calculation algorithm if it hasn't been initialized yet
            if (EMCAlgorithmInitialized)
            {
                // Skip item 0
                if (item.type == ItemID.None) return;
                // If the item has a predefined EMC value, set it
                if (ItemEMCValues.TryGetValue(item.type, out RationalNumber predefinedEMC))
                {
                    emc = predefinedEMC;
                }
                else
                {
                    // If no predefined value, set to zero
                    emc = RationalNumber.Zero;
                }
            }

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
            // Store numerator and denominator separately
            tag.Add("EMCNumerator", emc.Numerator);
            tag.Add("EMCDenominator", emc.Denominator);
        }

        // Load EMC value when the game is loaded
        public override void LoadData(Item item, TagCompound tag) {
            if (tag.ContainsKey("EMCNumerator") && tag.ContainsKey("EMCDenominator")) {
                long numerator = tag.GetLong("EMCNumerator");
                long denominator = tag.GetLong("EMCDenominator");
                emc = new RationalNumber(numerator, denominator);
            }
        }
    }
}
