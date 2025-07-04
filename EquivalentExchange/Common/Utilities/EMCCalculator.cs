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
            setPreCalculationEMCValues(emcValues);

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

            // Add in the final override EMC values
            SetPostCalculationEMCValues(emcValues);

            long stringBuilderTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // Log the final EMC values
            LogMessage("Final EMC values calculated:");
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in emcValues.OrderBy(k => k.Key))
            {
                sb.AppendLine($"Item: {allItems.FirstOrDefault(i => i.type == kvp.Key)?.Name ?? "Unknown"} (Type: {kvp.Key}) - EMC: {kvp.Value}");
            }
            LogMessage(sb.ToString());
            LogMessage($"StringBuilder took {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - stringBuilderTime} ms to build the final EMC values string.");

            // End timer and log the total time taken
            long endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long totalTime = endTime - startTime;
            LogMessage($"EMC calculation completed in {totalTime} ms.");

            // Return the final EMC values
            return emcValues;
        }

        /// <summary>
        /// This method will initialize certain items to have certain EMC values,
        /// which will then be used in the EMC calculation algorithm.
        /// </summary>
        private static void setPreCalculationEMCValues(Dictionary<int, RationalNumber> emcValues)
        {
            // Set the four fragments to their store price divided by 5 (which is their sell price)
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

            // Glowing mushrooms should be set to their sell price
            emcValues[ItemID.GlowingMushroom] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.GlowingMushroom)?.value ?? 0, 5);

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

            // Presents should not be learnable as they can contain either very valuable items or trash
            emcValues[ItemID.Present] = RationalNumber.Zero;

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
            bool[] treasureBagSetFactory = ItemID.Sets.BossBag;
            // Extract the ids of all treasure bags
            List<int> treasureBagIds = new List<int>();
            for (int i = 0; i < treasureBagSetFactory.Length; i++)
            {
                if (treasureBagSetFactory[i])
                {
                    treasureBagIds.Add(i);
                }
            }
            // Set the EMC value of all treasure bags to 0
            foreach (int bagId in treasureBagIds)
            {
                emcValues[bagId] = RationalNumber.Zero;
            }

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

            // Crates should not be learnable, so we set their EMC to 0
            bool[] fishingCrateSetFactory = ItemID.Sets.IsFishingCrate;
            // Extract the ids of all fishing crates
            List<int> fishingCrateIds = new List<int>();
            for (int i = 0; i < fishingCrateSetFactory.Length; i++)
            {
                if (fishingCrateSetFactory[i])
                {
                    fishingCrateIds.Add(i);
                }
            }
            // Set the EMC value of all fishing crates to 0
            foreach (int crateId in fishingCrateIds)
            {
                emcValues[crateId] = RationalNumber.Zero;
            }

            // For thorium mod compatibility:
            // The following are set here:
            // Base game:
            // Marble Block
            // Life Crystal -> affects life quartz in thorium
            // Encumbering stone(should be fixed by annoying mud fix, costs 1 gold)

            // Strider's Tear
            // Ancient Blade
            // Bronze Alloy Fragments 
            // Abyssal Shadow (both types)
            // Unstable Core
            // Shooting Star Fragment
            // Celestial Fragment
            // White Dwarf Fragment
            // Family Heirloom
            // Dormant Hammer (used to make mjolnir = 50 gold)
            // Smooth Coal
            // Aquatic Depths Key
            // Desert Key
            // Underworld Key
            // Marine Block
            // Deadwood
            // Lil' Guppy
            // Annoying Mud (makes encumbering stone??)
            // Evergreen Wood
            // Yew Wood
            // Solar Pebbles

            // Check if the thorium mod is loaded
            if (ModLoader.TryGetMod("ThoriumMod", out Mod thoriumMod))
            {
                // Marble is common, so its EMC is 1
                emcValues[ItemID.MarbleBlock] = RationalNumber.One;
                // Life crystals are worth their store price divided by 5
                emcValues[ItemID.LifeCrystal] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.LifeCrystal)?.value ?? 0, 5);
                emcValues[ItemID.EncumberingStone] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.EncumberingStone)?.value ?? 0, 5);
                // Relatively rare boss summoning item, so 1000 seems reasonable
                emcValues[thoriumMod.TryFind<ModItem>("StriderTear", out ModItem stridersTear) ? stridersTear.Type : -1] = new RationalNumber(1000, 1);
                // Ancient Blade is yet another boss summoning item, but it should be set by setting Bronze Alloy Fragments and marble block (marble block is set above)

                // First get the instance of the bronze alloy fragments so we can then get its value
                if (thoriumMod.TryFind<ModItem>("BronzeAlloyFragments", out ModItem BronzeAlloyFragments))
                {
                    // Actually set the EMC value of the Bronze Alloy Fragments
                    emcValues[BronzeAlloyFragments.Type] = new RationalNumber(allItems.FirstOrDefault(item => item.type == BronzeAlloyFragments.Type)?.value ?? 0, 5);
                }

                // Both AbyssalShadow and AbyssalShadow2 shall be set to 10 silver equivalent EMC (1000)
                if (thoriumMod.TryFind<ModItem>("AbyssalShadow", out ModItem abyssalShadow))
                {
                    emcValues[abyssalShadow.Type] = new RationalNumber(1000, 1);
                }
                if (thoriumMod.TryFind<ModItem>("AbyssalShadow2", out ModItem abyssalShadow2))
                {
                    emcValues[abyssalShadow2.Type] = new RationalNumber(1000, 1);
                }

                // UnstableCore is yet another boss summoning item, so 1000 seems reasonable
                if (thoriumMod.TryFind<ModItem>("UnstableCore", out ModItem unstableCore))
                {
                    emcValues[unstableCore.Type] = new RationalNumber(1000, 1);
                }

                // Shooting Star, Celestial and White Dwarf fragments shall all be treated like their vanilla counterparts (the solar, vortex, nebula and stardust fragments)
                // So they will be set to their store price divided by 5
                if (thoriumMod.TryFind<ModItem>("ShootingStarFragment", out ModItem shootingStarFragment))
                {
                    emcValues[shootingStarFragment.Type] = new RationalNumber(allItems.FirstOrDefault(item => item.type == shootingStarFragment.Type)?.value ?? 0, 5);
                }
                if (thoriumMod.TryFind<ModItem>("CelestialFragment", out ModItem celestialFragment))
                {
                    emcValues[celestialFragment.Type] = new RationalNumber(allItems.FirstOrDefault(item => item.type == celestialFragment.Type)?.value ?? 0, 5);
                }
                if (thoriumMod.TryFind<ModItem>("WhiteDwarfFragment", out ModItem whiteDwarfFragment))
                {
                    emcValues[whiteDwarfFragment.Type] = new RationalNumber(allItems.FirstOrDefault(item => item.type == whiteDwarfFragment.Type)?.value ?? 0, 5);
                }

                // Family heirloom is both a starting item, and for mediumcore characters specifically, will respawn in the inventory upon every death. Thus, they shall have emc 1
                if (thoriumMod.TryFind<ModItem>("FamilyHeirloom", out ModItem familyHeirloom))
                {
                    emcValues[familyHeirloom.Type] = RationalNumber.One;
                }

                // Dormant Hammer is used to craft Mjolnir, so it should be worth the same as Mjolnir's value divided by 5
                if (thoriumMod.TryFind<ModItem>("DormantHammer", out ModItem dormantHammer) && thoriumMod.TryFind<ModItem>("Mjolnir", out ModItem mjolnir))
                {
                    emcValues[dormantHammer.Type] = new RationalNumber(allItems.FirstOrDefault(item => item.type == mjolnir.Type)?.value ?? 0, 5);
                }

                // Smooth Coal shall be equivalent to gel (so 1)
                if (thoriumMod.TryFind<ModItem>("SmoothCoal", out ModItem smoothCoal))
                {
                    emcValues[smoothCoal.Type] = RationalNumber.One;
                }

                // Much like the vanilla keys, the thorium mod has its own keys for the aquatic depths, desert and underworld. They should be set to 1000000 due to their rarity
                if (thoriumMod.TryFind<ModItem>("AquaticDepthsBiomeKey", out ModItem AquaticDepthsBiomeKey))
                {
                    emcValues[AquaticDepthsBiomeKey.Type] = new RationalNumber(1000000, 1);
                }
                if (thoriumMod.TryFind<ModItem>("DesertBiomeKey", out ModItem DesertBiomeKey))
                {
                    emcValues[DesertBiomeKey.Type] = new RationalNumber(1000000, 1);
                }
                if (thoriumMod.TryFind<ModItem>("UnderworldBiomeKey", out ModItem UnderworldBiomeKey))
                {
                    emcValues[UnderworldBiomeKey.Type] = new RationalNumber(1000000, 1);
                }

                // Marine Block is a common block, so its EMC is 1
                if (thoriumMod.TryFind<ModItem>("MarineBlock", out ModItem marineBlock))
                {
                    emcValues[marineBlock.Type] = RationalNumber.One;
                }

                // Deadwood, Evergreen Wood and Yew Wood are all wood types, so their EMC is 1
                if (thoriumMod.TryFind<ModItem>("Deadwood", out ModItem deadwood))
                {
                    emcValues[deadwood.Type] = RationalNumber.One;
                }
                if (thoriumMod.TryFind<ModItem>("EvergreenBlock", out ModItem evergreenWood))
                {
                    emcValues[evergreenWood.Type] = RationalNumber.One;
                }
                if (thoriumMod.TryFind<ModItem>("YewWood", out ModItem yewWood))
                {
                    emcValues[yewWood.Type] = RationalNumber.One;
                }

                // Lil' Guppy is another fish, so it should be worth 1000 due to its rarity
                if (thoriumMod.TryFind<ModItem>("LilGuppy", out ModItem lilGuppy))
                {
                    emcValues[lilGuppy.Type] = new RationalNumber(1000, 1);
                }

                // 10 Annoying Mud makes the encumbering stone, and the encumbering stone is worth 1 gold, so we set the annoying mud to 1 tenth of the encumbering stone (so divide its values by 5 and then by 10)
                if (thoriumMod.TryFind<ModItem>("AnnoyingMud", out ModItem annoyingMud))
                {
                    emcValues[annoyingMud.Type] = new RationalNumber(allItems.FirstOrDefault(item => item.type == ItemID.EncumberingStone)?.value ?? 0, 50);
                }

                // Solar Pebbles should be set to their value divided by 5
                if (thoriumMod.TryFind<ModItem>("SolarPebble", out ModItem solarPebble))
                {
                    emcValues[solarPebble.Type] = new RationalNumber(allItems.FirstOrDefault(item => item.type == solarPebble.Type)?.value ?? 0, 5);
                }
            }
        }

        /// <summary>
        /// This method will set the EMC values of item after the EMC calculation algorithm has been run.
        /// As such, these values will not be used in the EMC calculation algorithm,
        /// but rather will override any values that were calculated for these items.
        /// </summary>
        public static void SetPostCalculationEMCValues(Dictionary<int, RationalNumber> emcValues)
        {
            // The shellphones that come from right clicking should be worth the same as the actual shellphone from crafting
            // since they are all essentially the same item
            emcValues[ItemID.Shellphone] = emcValues[ItemID.ShellphoneDummy];
            emcValues[ItemID.ShellphoneHell] = emcValues[ItemID.ShellphoneDummy];
            emcValues[ItemID.ShellphoneOcean] = emcValues[ItemID.ShellphoneDummy];
            emcValues[ItemID.ShellphoneSpawn] = emcValues[ItemID.ShellphoneDummy];
        }
    }
}
