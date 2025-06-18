using EquivalentExchange.Common.Utilities;
using EquivalentExchange.Common.Systems;
using EquivalentExchange.Tiles;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace EquivalentExchange.TileEntities
{
    public class EnergyCondenserTileEntity : ModTileEntity
    {
        // Add tracking for which player is using this condenser
        public int CurrentUser { get; private set; } = -1; // -1 means no user

        // Constants
        private const int INVENTORY_SIZE = 56; // 8 rows * 7 columns = 56 slots
        private const int ITEMS_PER_TICK = 5; // How many items can be processed per tick
        
        // Data to store and sync
        public RationalNumber storedEMC = RationalNumber.Zero;
        public Item[] inventory;
        public Item templateItem = new Item();
        
        // Cached values for performance
        private int lastEMCSync = 0;
        private const int EMC_SYNC_INTERVAL = 10; // Reduced from 60 to 10 for more frequent syncing

        public EnergyCondenserTileEntity()
        {
            // Initialize inventory
            inventory = new Item[INVENTORY_SIZE];
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                inventory[i] = new Item();
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag[nameof(storedEMC) + "Numerator"] = storedEMC.Numerator;
            tag[nameof(storedEMC) + "Denominator"] = storedEMC.Denominator;
            
            // Save template item
            tag[nameof(templateItem)] = templateItem;
            
            // Save inventory
            var inventoryList = new TagCompound[INVENTORY_SIZE];
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                inventoryList[i] = ItemIO.Save(inventory[i]);
            }
            tag[nameof(inventory)] = inventoryList;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey(nameof(storedEMC) + "Numerator") && tag.ContainsKey(nameof(storedEMC) + "Denominator"))
            {
                long numerator = tag.GetLong(nameof(storedEMC) + "Numerator");
                long denominator = tag.GetLong(nameof(storedEMC) + "Denominator");
                storedEMC = new RationalNumber(numerator, denominator);
            }
            
            // Load template item
            if (tag.ContainsKey(nameof(templateItem)))
            {
                templateItem = ItemIO.Load(tag.GetCompound(nameof(templateItem)));
            }
            
            // Load inventory
            if (tag.ContainsKey(nameof(inventory)))
            {
                var inventoryList = tag.Get<TagCompound[]>(nameof(inventory));
                for (int i = 0; i < Math.Min(INVENTORY_SIZE, inventoryList.Length); i++)
                {
                    inventory[i] = ItemIO.Load(inventoryList[i]);
                }
            }
        }

        public override void NetSend(BinaryWriter writer)
        {
            // Add CurrentUser to the data being sent
            writer.Write(CurrentUser);
            
            writer.Write(storedEMC.Numerator);
            writer.Write(storedEMC.Denominator);
            
            // Send template item
            ItemIO.Send(templateItem, writer, writeStack: true, writeFavorite: false);
            
            // Send inventory
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                ItemIO.Send(inventory[i], writer, writeStack: true, writeFavorite: false);
            }
        }

        public override void NetReceive(BinaryReader reader)
        {
            // Read CurrentUser first
            CurrentUser = reader.ReadInt32();
            
            long numerator = reader.ReadInt64();
            long denominator = reader.ReadInt64();
            storedEMC = new RationalNumber(numerator, denominator);
            
            // Receive template item
            templateItem = ItemIO.Receive(reader, readStack: true, readFavorite: false);
            
            // Receive inventory
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                inventory[i] = ItemIO.Receive(reader, readStack: true, readFavorite: false);
            }
        }

        // Add this method to handle player access requests
        public bool RequestAccess(int playerIndex)
        {
            // If condenser is not in use or the requesting player is already using it
            if (CurrentUser == -1 || CurrentUser == playerIndex)
            {
                CurrentUser = playerIndex;
                
                // Request accepted, tell everyone
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
                }
                return true;
            }
            return false;
        }
        
        // Add this method to handle releasing access
        public void ReleaseAccess(int playerIndex)
        {
            if (CurrentUser == playerIndex)
            {
                CurrentUser = -1;
                
                // Access released, tell everyone
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
                }
            }
        }
        
        // Add method to handle item placement from client
        public bool TryPlaceItem(int slotIndex, Item item, int playerIndex)
        {
            // Only allow current user to modify inventory
            if (CurrentUser != playerIndex)
                return false;
                
            // Logic for placing the item
            if (slotIndex >= 0 && slotIndex < INVENTORY_SIZE)
            {
                inventory[slotIndex] = item;
                
                // Sync changes
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
                }
                return true;
            }
            return false;
        }
        
        // Add method to handle item removal from client
        public bool TryTakeItem(int slotIndex, int playerIndex, out Item item)
        {
            item = new Item();
            
            // Only allow current user to modify inventory
            if (CurrentUser != playerIndex)
                return false;
                
            // Logic for taking the item
            if (slotIndex >= 0 && slotIndex < INVENTORY_SIZE && !inventory[slotIndex].IsAir)
            {
                item = inventory[slotIndex];
                inventory[slotIndex] = new Item();
                
                // Sync changes
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
                }
                return true;
            }
            return false;
        }

        public override void Update()
        {
            // Only process on server or single player
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Existing processing logic
            ProcessItems();
            
            // Sync periodically
            lastEMCSync++;
            if (lastEMCSync >= EMC_SYNC_INTERVAL)
            {
                lastEMCSync = 0;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
                }
            }
        }

        private void ProcessItems()
        {
            // If no template is set, energy condenser acts as a regular chest
            if (templateItem.IsAir)
                return;

            // Get EMC value of template item
            RationalNumber templateEMC = EMCHelper.GetEMC(templateItem);
            if (templateEMC <= RationalNumber.Zero)
                return; // Template item has no EMC value

            int itemsProcessed = 0;
            
            // Convert non-template items to EMC
            for (int i = 0; i < INVENTORY_SIZE && itemsProcessed < ITEMS_PER_TICK; i++)
            {
                Item item = inventory[i];
                if (item.IsAir || item.type == templateItem.type)
                    continue;

                RationalNumber itemEMCPerUnit = EMCHelper.GetEMC(item);
                if (itemEMCPerUnit <= RationalNumber.Zero)
                    continue;

                // Calculate how many items we can process from this stack
                int itemsToProcess = Math.Min(item.stack, ITEMS_PER_TICK - itemsProcessed);
                
                if (itemsToProcess > 0)
                {
                    // Convert only part of the stack
                    RationalNumber totalEMCGained = itemEMCPerUnit * itemsToProcess;
                    storedEMC += totalEMCGained;
                    
                    // Reduce the stack
                    item.stack -= itemsToProcess;
                    if (item.stack <= 0)
                        inventory[i] = new Item();
                        
                    itemsProcessed += itemsToProcess;
                }
            }

            // Create template items if we have enough EMC
            while (storedEMC >= templateEMC && itemsProcessed < ITEMS_PER_TICK)
            {
                // First try to stack with existing items of the same type
                bool stacked = false;
                for (int i = 0; i < INVENTORY_SIZE; i++)
                {
                    Item item = inventory[i];
                    if (!item.IsAir && item.type == templateItem.type && item.stack < item.maxStack)
                    {
                        // Stack with existing item
                        item.stack++;
                        storedEMC -= templateEMC;
                        itemsProcessed++;
                        stacked = true;
                        break;
                    }
                }

                // If couldn't stack, try to find an empty slot
                if (!stacked)
                {
                    int emptySlot = FindEmptySlot();
                    if (emptySlot == -1)
                        break; // No empty slots
                        
                    // Create the item
                    inventory[emptySlot] = templateItem.Clone();
                    inventory[emptySlot].stack = 1;
                    storedEMC -= templateEMC;
                    itemsProcessed++;
                }
            }
        }

        private int FindEmptySlot()
        {
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                if (inventory[i].IsAir)
                    return i;
            }
            return -1;
        }

        public bool TrySetTemplate(Item item)
        {
            if (item.IsAir)
            {
                templateItem.TurnToAir();
                return true;
            }

            RationalNumber emc = EMCHelper.GetEMC(item);
            if (emc > RationalNumber.Zero)
            {
                templateItem = item.Clone();
                templateItem.stack = 1;
                return true;
            }
            return false;
        }
        
        public bool ToggleUI()
        {
            return EMCUI.ToggleEnergyCondenserUI(this);
        }

        public override bool IsTileValidForEntity(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            return tile.HasTile && tile.TileType == ModContent.TileType<EnergyCondenserTile>();
        }
    }
}
