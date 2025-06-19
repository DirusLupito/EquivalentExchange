using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Linq;
using System.IO;
using System.Text;
using Terraria.Localization;

namespace EquivalentExchange.Common.Utilities
{
    /// <summary>
    /// Calculates and applies EMC values based on crafting recipes
    /// </summary>
    public static class EMCCalculator
    {
        // List of all items with their EMC values
        private static List<Item> allItems = new List<Item>();

        // Log file path
        private const string LogFilePath = "EMCCalculator.log";

        // If a log file will be created and written to
        private static bool IsLoggingEnabled = false;

        /// <summary>
        /// Log a message to the EMC calculation log file
        /// </summary>
        private static void LogMessage(string message)
        {
            if (!IsLoggingEnabled)
                return;
            try
            {
                // Get timestamp
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Format message with timestamp
                string formattedMessage = $"[{timestamp}] {message}";

                // Append to log file
                using (StreamWriter writer = new StreamWriter(LogFilePath, true))
                {
                    writer.WriteLine(formattedMessage);
                }
            }
            catch (Exception ex)
            {
                // If logging fails, try to write to error log
                try
                {
                    using (StreamWriter writer = new StreamWriter("EMCCalculator_error.log", true))
                    {
                        writer.WriteLine($"[{DateTime.Now}] Error logging to main log file: {ex.Message}");
                    }
                }
                catch
                {
                    // At this point we can't do much else
                }
            }
        }

        /// <summary>
        /// Clear the log file to start fresh
        /// </summary>
        private static void ClearLogFile()
        {
            if (!IsLoggingEnabled)
                return;
            try
            {
                // Create a new file or overwrite existing file
                using (StreamWriter writer = new StreamWriter(LogFilePath, false))
                {
                    writer.WriteLine($"=== EMC Calculator Log Started at {DateTime.Now} ===");
                    writer.WriteLine();
                }
            }
            catch
            {
                // If clearing fails, there's not much we can do
                // except continue and append to the existing file
            }
        }

        /// <summary>
        /// Gets a list of all valid items in the game (vanilla and modded)
        /// </summary>
        private static List<Item> GetAllItems()
        {
            List<Item> allItems = new List<Item>();

            // Iterate through all vanilla items
            for (int i = 1; i < ItemID.Count; i++)
            {
                Item item = new Item();
                item.SetDefaults(i);

                // Skip items with no name or empty names
                if (string.IsNullOrEmpty(item.Name))
                    continue;

                allItems.Add(item);
            }

            // Also iterate through all modded items
            foreach (var mod in ModLoader.Mods)
            {
                if (mod == null) continue;

                if (mod.Name == "ModLoader") continue; // Skip the ModLoader mod

                LogMessage($"Loading {mod.GetContent<ModItem>().Count()} items from mod: {mod.Name}");

                for (int i = 0; i < mod.GetContent<ModItem>().Count(); i++)
                {
                    int type = mod.GetContent<ModItem>().ElementAt(i).Type;
                    Item item = new Item();
                    item.SetDefaults(type);

                    var localizationKey = mod.GetContent<ModItem>().ElementAt(i).GetLocalizationKey("");

                    // Skip items with no name or empty names
                    if (string.IsNullOrEmpty(Language.GetText(localizationKey).ToString()))
                        continue;

                    LogMessage($"Loaded item: {Language.GetText(localizationKey)} (Type: {type}) from mod: {mod.Name}");

                    allItems.Add(item);
                }
            }

            return allItems;
        }

        /// <summary>
        /// Calculate EMC values for all items
        /// The overall idea: Consider the set of all items, and the set of recipes.
        /// Recipes define essentially an operation that takes a set of items and produces another item.
        /// So for a recipe R and items A, B, ... C, we have R: (A, B, ...) -> D.
        /// If for some item A we have a function EMC : A -> \mathbb{N}, then EMC() is the function that gives the EMC value of an item.
        /// Now, the goal is to define this EMC function such that it tries to satisfy the following property for all recipes
        /// with inputs A, B, ..., and output C,
        /// EMC(A) + EMC(B) + ... = EMC(C).
        /// Thus, we can think of recipes as linear equations in the space of items,
        /// and we want to find a solution for the EMC values that satisfies all these equations.
        /// There are a few issues to consider:
        /// 1. Some items may not have a recipe, so we need to assign them a default EMC value.
        /// 2. Some items may have multiple recipes, so we should try to prevent infinite positive EMC feedback loops. This can be done by chosing the solution that minimizes the EMC values.
        /// 3. We need to ensure that the EMC values are positive integers.
        /// </summary>
        public static Dictionary<int, RationalNumber> CalculateEMCValues()
        {
            // Start timer
            long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Clear the log file at the start
            ClearLogFile();
            LogMessage("Starting EMC calculation...");

            // Get all items
            allItems = GetAllItems();
            LogMessage($"Found {allItems.Count} items in total.");

            // Create a dictionary to hold EMC values (key: item type, value: EMC value as RationalNumber)
            Dictionary<int, RationalNumber> emcValues = new Dictionary<int, RationalNumber>();

            // Initialize all items with a default EMC value of -1 (-1 will be treated as "not calculated")
            foreach (var item in allItems)
            {
                emcValues[item.type] = new RationalNumber(-1, 1);
            }
            LogMessage("Initialized EMC values for all items.");

            // Figure out what items are the most basic ones, i.e. those that have no recipes
            List<int> basicItems = new List<int>();
            foreach (var item in allItems)
            {
                // If the item has no recipe, we consider it a basic item
                if (Main.recipe.Any(r => r.createItem.type == item.type) == false)
                {
                    basicItems.Add(item.type);
                    // Assign a default EMC value of the item's price in copper coins divided by 5, or 1 if it would have been 0
                    emcValues[item.type] = item.value > 0 ? new RationalNumber(item.value, 5) : RationalNumber.One;
                }
            }
            LogMessage($"Found {basicItems.Count} basic items with no recipes.");

            // Use some hardcoded initial values for some more common vanilla items
            setCommonEMCValues(emcValues);

            // Now lets see how much we can solve for from this set of basic items
            bool changed;
            int numIterations = 0;
            long loopTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            do
            {
                changed = false;
                foreach (var recipe in Main.recipe)
                {
                    // If recipe.createItem.type is not in emcValues, we can't calculate its EMC value yet
                    if (!emcValues.ContainsKey(recipe.createItem.type))
                    {
                        continue;
                    }

                    // Check the number of unknowns in the recipe emc equation
                    // First lets see if we know the EMC value of the output item
                    int numUnknowns = emcValues[recipe.createItem.type].Numerator == -1 ? 1 : 0;

                    // Now check the ingredients
                    foreach (var ingredient in recipe.requiredItem)
                    {
                        if (emcValues.ContainsKey(ingredient.type) && emcValues[ingredient.type].Numerator == -1)
                        {
                            numUnknowns++;
                        }
                        if (numUnknowns >= 2)
                        {
                            break; // No need to check further, we can't solve this recipe yet
                        }
                    }

                    // If we have two or more unknowns, we can't solve this recipe yet
                    if (numUnknowns >= 2)
                    {
                        continue;
                    }

                    // If we have exactly one unknown, we can solve for it

                    // In the case where the output item was the unknown:
                    if (emcValues[recipe.createItem.type].Numerator == -1)
                    {
                        RationalNumber totalInputEMC = RationalNumber.Zero;
                        foreach (var ingredient in recipe.requiredItem)
                        {
                            if (emcValues.ContainsKey(ingredient.type) && emcValues[ingredient.type].Numerator != -1)
                            {
                                totalInputEMC += emcValues[ingredient.type] * ingredient.stack;
                            }
                        }

                        // Now we have the equation of the form input = unknown, no rearranging needed
                        // Set the EMC value of the output item to the total input EMC,
                        // and divide by the stack size to get the EMC per item while ensuring it's at least 1
                        RationalNumber result = totalInputEMC / recipe.createItem.stack;
                        emcValues[recipe.createItem.type] = result > RationalNumber.Zero ? result : RationalNumber.One;
                        changed = true;
                    }
                    else
                    {
                        // In the case where one of the ingredients was the unknown
                        foreach (var ingredient in recipe.requiredItem)
                        {
                            if (emcValues.ContainsKey(ingredient.type) && emcValues[ingredient.type].Numerator == -1)
                            {
                                // Calculate the EMC value for this ingredient
                                RationalNumber totalInputEMC = RationalNumber.Zero;
                                foreach (var otherIngredient in recipe.requiredItem)
                                {
                                    if (otherIngredient.type != ingredient.type &&
                                        emcValues.ContainsKey(otherIngredient.type) &&
                                        emcValues[otherIngredient.type].Numerator != -1)
                                    {
                                        totalInputEMC += emcValues[otherIngredient.type] * otherIngredient.stack;
                                    }
                                }
                                // Now we have the equation of the form input + unknown = output
                                // Rearranging gives us unknown = output - input
                                // We can now calculate the EMC value for this ingredient, ensuring it's at least 1
                                RationalNumber outputEMC = emcValues[recipe.createItem.type];
                                RationalNumber result = (outputEMC - totalInputEMC) / ingredient.stack;
                                emcValues[ingredient.type] = result > RationalNumber.Zero ? result : RationalNumber.One;
                                changed = true;
                            }
                        }
                    }
                    numIterations++;
                }
            } while (changed);

            // Log the number of iterations it took to stabilize
            LogMessage($"EMC calculation stabilized after {numIterations} iterations in {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - loopTime} ms.");

            // Now we have a set of EMC values, but some items may still have -1 (not calculated)
            // Assign default EMC values to any items that still have -1
            foreach (var item in allItems)
            {
                if (emcValues[item.type].Numerator == -1)
                {
                    // Assign the item's value in copper coins, or 1 if it would have been 0
                    emcValues[item.type] = item.value > 0 ? new RationalNumber(item.value, 5) : RationalNumber.One;
                }
            }
            // Log the final EMC values
            LogMessage("Final EMC values calculated:");
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in emcValues.OrderBy(k => k.Key))
            {
                sb.AppendLine($"Item: {allItems.FirstOrDefault(i => i.type == kvp.Key)?.Name ?? "Unknown"} (Type: {kvp.Key}) - EMC: {kvp.Value}");
            }
            LogMessage(sb.ToString());

            // End timer and log the total time taken
            long endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long totalTime = endTime - startTime;
            LogMessage($"EMC calculation completed in {totalTime} ms.");

            // Return the final EMC values
            return emcValues;
        }

        /// <summary>
        /// This method will initialize certain items to have certain EMC values.
        /// </summary>
        private static void setCommonEMCValues(Dictionary<int, RationalNumber> emcValues)
        {
            // Set the four fragments to their store price divided by 5
            emcValues[ItemID.FragmentVortex] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.FragmentVortex)?.value ?? 0, 5);
            emcValues[ItemID.FragmentNebula] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.FragmentNebula)?.value ?? 0, 5);
            emcValues[ItemID.FragmentSolar] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.FragmentSolar)?.value ?? 0, 5);
            emcValues[ItemID.FragmentStardust] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.FragmentStardust)?.value ?? 0, 5);

            // Dirt, stone, sand, cobweb and wood are all very common, so their EMC is 1
            emcValues[ItemID.DirtBlock] = RationalNumber.One;
            emcValues[ItemID.StoneBlock] = RationalNumber.One;
            emcValues[ItemID.SandBlock] = RationalNumber.One;
            emcValues[ItemID.Wood] = RationalNumber.One;
            emcValues[ItemID.BorealWood] = RationalNumber.One;
            emcValues[ItemID.RichMahogany] = RationalNumber.One;
            emcValues[ItemID.PalmWood] = RationalNumber.One;
            emcValues[ItemID.Ebonwood] = RationalNumber.One;
            emcValues[ItemID.Shadewood] = RationalNumber.One;
            emcValues[ItemID.Cactus] = RationalNumber.One;
            emcValues[ItemID.AshWood] = RationalNumber.One;
            emcValues[ItemID.Cobweb] = RationalNumber.One;

            // Silk is 7 since even though its worth quite a bit, it only takes 7 cobwebs to craft
            emcValues[ItemID.Silk] = new RationalNumber(7, 1);

            // Obsidian is 1000 because it felt right
            emcValues[ItemID.Obsidian] = new RationalNumber(1000, 1);

            // Water and lava buckets the same as the bucket
            emcValues[ItemID.WaterBucket] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.EmptyBucket)?.value ?? 0, 5);
            emcValues[ItemID.LavaBucket] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.EmptyBucket)?.value ?? 0, 5);

            // The major ores should be set to their store price (except for obsidian since that has a store price of 0)
            emcValues[ItemID.Hellstone] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.Hellstone)?.value ?? 0, 5);
            emcValues[ItemID.CopperOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.CopperOre)?.value ?? 0, 5);
            emcValues[ItemID.TinOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.TinOre)?.value ?? 0, 5);
            emcValues[ItemID.IronOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.IronOre)?.value ?? 0, 5);
            emcValues[ItemID.LeadOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.LeadOre)?.value ?? 0, 5);
            emcValues[ItemID.SilverOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.SilverOre)?.value ?? 0, 5);
            emcValues[ItemID.TungstenOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.TungstenOre)?.value ?? 0, 5);
            emcValues[ItemID.GoldOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.GoldOre)?.value ?? 0, 5);
            emcValues[ItemID.PlatinumOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.PlatinumOre)?.value ?? 0, 5);
            emcValues[ItemID.DemoniteOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.DemoniteOre)?.value ?? 0, 5);
            emcValues[ItemID.CrimtaneOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.CrimtaneOre)?.value ?? 0, 5);
            emcValues[ItemID.Meteorite] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.Meteorite)?.value ?? 0, 5);
            emcValues[ItemID.CobaltOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.CobaltOre)?.value ?? 0, 5);
            emcValues[ItemID.PalladiumOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.PalladiumOre)?.value ?? 0, 5);
            emcValues[ItemID.MythrilOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.MythrilOre)?.value ?? 0, 5);
            emcValues[ItemID.OrichalcumOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.OrichalcumOre)?.value ?? 0, 5);
            emcValues[ItemID.AdamantiteOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.AdamantiteOre)?.value ?? 0, 5);
            emcValues[ItemID.TitaniumOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.TitaniumOre)?.value ?? 0, 5);
            emcValues[ItemID.ChlorophyteOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.ChlorophyteOre)?.value ?? 0, 5);
            emcValues[ItemID.LunarOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.LunarOre)?.value ?? 0, 5);

            // Golden keys for unlocking dungeon chests are way too cheap at 1, hence the increase to 30000
            emcValues[ItemID.GoldenKey] = new RationalNumber(30000, 1);

            // Bones should be set at their store price
            emcValues[ItemID.Bone] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.Bone)?.value ?? 0, 5);

            // Biome keys should be set at 1000000 to reflect their rarity
            emcValues[ItemID.CrimsonKey] = new RationalNumber(1000000, 1);
            emcValues[ItemID.CorruptionKey] = new RationalNumber(1000000, 1);
            emcValues[ItemID.JungleKey] = new RationalNumber(1000000, 1);
            emcValues[ItemID.HallowedKey] = new RationalNumber(1000000, 1);
            emcValues[ItemID.FrozenKey] = new RationalNumber(1000000, 1);
            emcValues[ItemID.DungeonDesertKey] = new RationalNumber(1000000, 1);

            // Spooky wood should be set at the store price of the spooky breastplate, divided by the amount of wood it takes to craft it (300)
            emcValues[ItemID.SpookyWood] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.SpookyBreastplate)?.value ?? 0, 5) / 300;

            // Pumpkins should be set at their store price
            emcValues[ItemID.Pumpkin] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.Pumpkin)?.value ?? 0, 5);

            // Rotten eggs should be set at 100 rather than 1 since they are more rare than say, wood
            emcValues[ItemID.RottenEgg] = new RationalNumber(100, 1);

            // Presents should be set at a high value (10000) since they can potentially be unboxed into very valuable items
            emcValues[ItemID.Present] = new RationalNumber(10000, 1);

            // Dog whistle is a rare drop, so 10000 seems reasonable
            emcValues[ItemID.DogWhistle] = new RationalNumber(10000, 1);

            // All the quest fish should be set to 1000 due to their rarity
            emcValues[ItemID.Batfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.BumblebeeTuna] = new RationalNumber(1000, 1);
            emcValues[ItemID.Catfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.Cloudfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.Cursedfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.Dirtfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.DynamiteFish] = new RationalNumber(1000, 1);
            emcValues[ItemID.EaterofPlankton] = new RationalNumber(1000, 1);
            emcValues[ItemID.FallenStarfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.TheFishofCthulu] = new RationalNumber(1000, 1);
            emcValues[ItemID.Fishotron] = new RationalNumber(1000, 1);
            emcValues[ItemID.Harpyfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.Hungerfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.Ichorfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.Jewelfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.MirageFish] = new RationalNumber(1000, 1);
            emcValues[ItemID.MutantFlinxfin] = new RationalNumber(1000, 1);
            emcValues[ItemID.Pengfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.Pixiefish] = new RationalNumber(1000, 1);
            emcValues[ItemID.Spiderfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.TundraTrout] = new RationalNumber(1000, 1);
            emcValues[ItemID.UnicornFish] = new RationalNumber(1000, 1);
            emcValues[ItemID.GuideVoodooFish] = new RationalNumber(1000, 1);
            emcValues[ItemID.Wyverntail] = new RationalNumber(1000, 1);
            emcValues[ItemID.ZombieFish] = new RationalNumber(1000, 1);
            emcValues[ItemID.AmanitaFungifin] = new RationalNumber(1000, 1);
            emcValues[ItemID.Angelfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.BloodyManowar] = new RationalNumber(1000, 1);
            emcValues[ItemID.Bonefish] = new RationalNumber(1000, 1);
            emcValues[ItemID.Bunnyfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.CapnTunabeard] = new RationalNumber(1000, 1);
            emcValues[ItemID.Clownfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.DemonicHellfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.Derpfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.Fishron] = new RationalNumber(1000, 1);
            emcValues[ItemID.InfectedScabbardfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.Mudfish] = new RationalNumber(1000, 1);
            emcValues[ItemID.Slimefish] = new RationalNumber(1000, 1);
            emcValues[ItemID.TropicalBarracuda] = new RationalNumber(1000, 1);
            emcValues[ItemID.ScorpioFish] = new RationalNumber(1000, 1);
            emcValues[ItemID.ScarabFish] = new RationalNumber(1000, 1);

            // Solar tablet fragements should be 50000 due to their rarity
            // For some reason they are called "Lunar Tablet Fragment"
            emcValues[ItemID.LunarTabletFragment] = new RationalNumber(50000, 1);

            // Moon and sun mask vanity items should be set to 1000 since they are rare drops
            emcValues[ItemID.MoonMask] = new RationalNumber(1000, 1);
            emcValues[ItemID.SunMask] = new RationalNumber(1000, 1);

            // Treasure bags should not be learnable, so we set their EMC to 0
            emcValues[ItemID.KingSlimeBossBag] = RationalNumber.Zero;
            emcValues[ItemID.EyeOfCthulhuBossBag] = RationalNumber.Zero;
            emcValues[ItemID.EaterOfWorldsBossBag] = RationalNumber.Zero;
            emcValues[ItemID.BrainOfCthulhuBossBag] = RationalNumber.Zero;
            emcValues[ItemID.QueenBeeBossBag] = RationalNumber.Zero;
            emcValues[ItemID.SkeletronBossBag] = RationalNumber.Zero;
            emcValues[ItemID.WallOfFleshBossBag] = RationalNumber.Zero;
            emcValues[ItemID.DestroyerBossBag] = RationalNumber.Zero;
            emcValues[ItemID.TwinsBossBag] = RationalNumber.Zero;
            emcValues[ItemID.SkeletronPrimeBossBag] = RationalNumber.Zero;
            emcValues[ItemID.PlanteraBossBag] = RationalNumber.Zero;
            emcValues[ItemID.GolemBossBag] = RationalNumber.Zero;
            emcValues[ItemID.FishronBossBag] = RationalNumber.Zero;
            emcValues[ItemID.CultistBossBag] = RationalNumber.Zero;
            emcValues[ItemID.MoonLordBossBag] = RationalNumber.Zero;
            emcValues[ItemID.DeerclopsBossBag] = RationalNumber.Zero;
            emcValues[ItemID.QueenSlimeBossBag] = RationalNumber.Zero;
            emcValues[ItemID.FairyQueenBossBag] = RationalNumber.Zero;
            emcValues[ItemID.BossBagBetsy] = RationalNumber.Zero;

            // Desert fossils should be set to 200 since they could be transformed into very valuable items, albeit with a low chance
            emcValues[ItemID.DesertFossil] = new RationalNumber(200, 1);

            // Sturdy fossils should be set to the emc value of tungsten since it makes a similar tier of equipment
            emcValues[ItemID.FossilOre] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.TungstenOre)?.value ?? 0, 5);

            // Defender medals should be worth 1/5 gold coin
            emcValues[ItemID.DefenderMedal] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.GoldCoin)?.value ?? 0, 5);

            // The bloody tear should be worth 10000 due to its rarity and the fact that it can summon an event
            emcValues[ItemID.BloodMoonStarter] = new RationalNumber(10000, 1);

            // Advanced combat techniques and its second volume should be worth 5000 since it is rare and can empower the town npcs
            emcValues[ItemID.CombatBook] = new RationalNumber(5000, 1);
            emcValues[ItemID.CombatBookVolumeTwo] = new RationalNumber(5000, 1);
        }
    }
}
