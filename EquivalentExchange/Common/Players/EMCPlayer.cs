using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using EquivalentExchange.Common.Utilities;

namespace EquivalentExchange.Common.Players
{
    public class EMCPlayer : ModPlayer
    {
        public RationalNumber storedEMC = RationalNumber.Zero;

        // SortedDictionary to store learned items
        // This data structure prioritizes two operations:
        // 1. Fast insertion of new items O(log n) complexity (if this doesn't make sense, look up red-black trees https://en.wikipedia.org/wiki/Red%E2%80%93black_tree)
        // 2. For some c in the set {1, 2, 3, ...} (natural numbers), it allows us to retrieve the c most expensive items learned in O(log n + c) time complexity
        // As justification, the SortedDictionary uses a custom comparer that sorts items by EMC value in descending order. 
        // The underlying data structure is a self-balancing binary search tree (typically a Red-Black Tree).
        // To reach the starting position (the first/highest item), the data structure needs to navigate to the leftmost node in the tree, 
        // which takes O(log n) operations where n is the total number of items.
        // Once we have the starting position, we simply need to iterate through c consecutive items. 
        // In a binary search tree, traversing to the next in-order element is an amortized O(1) operation, 
        // as each edge in the tree is traversed at most once during a complete traversal.

        // Using a composite key of (EMC value, item type) ensures uniqueness while sorting by EMC
        // as two different items can in theory have the same EMC value
        private SortedDictionary<(RationalNumber EMC, int ItemType), LearnedItemInfo> learnedItems = new SortedDictionary<(RationalNumber EMC, int ItemType), LearnedItemInfo>(
            Comparer<(RationalNumber EMC, int ItemType)>.Create((a, b) =>
            {
                // Sort primarily by EMC (descending)
                int emcCompare = b.EMC.CompareTo(a.EMC); // Note: Reversed for descending order
                if (emcCompare != 0) return emcCompare;
                // If EMC is equal, sort by item type (ascending) to ensure uniqueness
                return a.ItemType.CompareTo(b.ItemType);
            })
        );

        // Save EMC when the player is saved
        public override void SaveData(TagCompound tag)
        {
            // Save storedEMC as numerator/denominator
            tag.Add("StoredEMCNumerator", storedEMC.Numerator);
            tag.Add("StoredEMCDenominator", storedEMC.Denominator);
            
            // Save learned items
            List<int> learnedItemTypes = new List<int>();
            List<long> learnedItemEMCNumerators = new List<long>();
            List<long> learnedItemEMCDenominators = new List<long>();
            List<string> learnedItemNames = new List<string>();
            
            foreach (var item in learnedItems.Values)
            {
                // Skip items that have already been learned
                if (learnedItemTypes.Contains(item.ItemType))
                    continue;
                learnedItemTypes.Add(item.ItemType);
                learnedItemEMCNumerators.Add(item.EMCValue.Numerator);
                learnedItemEMCDenominators.Add(item.EMCValue.Denominator);
                learnedItemNames.Add(item.Name);
            }
            
            tag.Add("LearnedItemTypes", learnedItemTypes);
            tag.Add("LearnedItemEMCNumerators", learnedItemEMCNumerators);
            tag.Add("LearnedItemEMCDenominators", learnedItemEMCDenominators);
            tag.Add("LearnedItemNames", learnedItemNames);
        }

        // Load EMC when the player is loaded
        public override void LoadData(TagCompound tag)
        {
            // Load the stored EMC
            if (tag.ContainsKey("StoredEMCNumerator") && tag.ContainsKey("StoredEMCDenominator"))
            {
                long numerator = tag.GetLong("StoredEMCNumerator");
                long denominator = tag.GetLong("StoredEMCDenominator");
                storedEMC = new RationalNumber(numerator, denominator);
            }
            else if (tag.ContainsKey("StoredEMC"))
            {
                // Backward compatibility
                storedEMC = new RationalNumber(tag.GetLong("StoredEMC"), 1);
            }
            
            // Load learned items
            if (tag.ContainsKey("LearnedItemTypes"))
            {
                List<int> itemTypes = tag.Get<List<int>>("LearnedItemTypes");
                
                // Check which format of data we have
                if (tag.ContainsKey("LearnedItemEMCNumerators") && tag.ContainsKey("LearnedItemEMCDenominators"))
                {
                    // New format with RationalNumber data
                    List<long> itemEMCNumerators = tag.Get<List<long>>("LearnedItemEMCNumerators");
                    List<long> itemEMCDenominators = tag.Get<List<long>>("LearnedItemEMCDenominators");
                    List<string> itemNames = tag.Get<List<string>>("LearnedItemNames");

                    List<int> itemTypesAddedSoFar = new List<int>();
                    
                    learnedItems.Clear();
                    for (int i = 0; i < itemTypes.Count; i++)
                    {
                        // Skip item types that are already learned
                        if (itemTypesAddedSoFar.Contains(itemTypes[i]))
                            continue;

                        var emcValue = new RationalNumber(itemEMCNumerators[i], itemEMCDenominators[i]);
                        var key = (emcValue, itemTypes[i]);
                        var info = new LearnedItemInfo
                        {
                            ItemType = itemTypes[i],
                            EMCValue = emcValue,
                            Name = itemNames[i]
                        };
                        learnedItems[key] = info;
                        itemTypesAddedSoFar.Add(itemTypes[i]);
                    }
                }
            }
        }

        // Add EMC to the player's stored amount
        public void AddEMC(RationalNumber value)
        {
            storedEMC += value;
        }

        // Remove EMC from the player's stored amount (with check to prevent negative values)
        public bool TryRemoveEMC(RationalNumber value)
        {
            if (storedEMC >= value)
            {
                storedEMC -= value;
                return true;
            }
            return false;
        }
        
        // Learn a new item
        public void LearnItem(Item item, RationalNumber emcValue)
        {
            var key = (emcValue, item.type);

            // Only add if not already learned
            // Also don't learn unlearnable items (e.g. items 0 emc value)
            if (!learnedItems.ContainsKey(key) && emcValue > RationalNumber.Zero)
            {
                learnedItems[key] = new LearnedItemInfo
                {
                    ItemType = item.type,
                    EMCValue = emcValue,
                    Name = item.Name
                };
            }
        }
        
        // Check if player has learned an item
        public bool HasLearnedItem(int itemType)
        {
            return learnedItems.Values.Any(item => item.ItemType == itemType);
        }
        
        // Get the count most expensive items learned (in terms of EMC)
        public List<LearnedItemInfo> GetMostExpensiveItems(int count)
        {
            return learnedItems.Values.Take(count).ToList();
        }
        
        // Get all learned items
        public IEnumerable<LearnedItemInfo> GetAllLearnedItems()
        {
            return learnedItems.Values;
        }

        // Get a specific page of expensive items learned (in terms of EMC)
        public List<LearnedItemInfo> GetMostExpensiveItemsPaginated(int pageSize, int pageNumber)
        {
            // Skip (pageNumber * pageSize) items, then take pageSize items
            return learnedItems.Values.Skip(pageNumber * pageSize).Take(pageSize).ToList();
        }

        // Get total count of learned items for pagination purposes
        public int GetTotalLearnedItemCount()
        {
            return learnedItems.Count;
        }

        // Get a specific page of affordable items learned (items with EMC value ≤ player's current EMC)
        public List<LearnedItemInfo> GetAffordableItemsPaginated(int pageSize, int pageNumber)
        {
            // Filter to only include items that the player can afford
            return learnedItems.Values
                .Where(item => item.EMCValue <= storedEMC)
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .ToList();
        }

        // Get total count of affordable learned items
        public int GetAffordableLearnedItemCount()
        {
            return learnedItems.Values.Count(item => item.EMCValue <= storedEMC);
        }

        // Unlearn an item (remove it from learned items)
        public bool UnlearnItem(int itemType)
        {
            // Find the entry with matching item type
            var itemToRemove = learnedItems.FirstOrDefault(pair => pair.Value.ItemType == itemType);
            
            // If found, remove it
            if (itemToRemove.Key != default)
            {
                learnedItems.Remove(itemToRemove.Key);
                return true;
            }
            return false;
        }
    }

    // Class to store information about learned items
    public class LearnedItemInfo
    {
        public int ItemType { get; set; }
        public RationalNumber EMCValue { get; set; } = RationalNumber.Zero;
        public string Name { get; set; }
    }
}