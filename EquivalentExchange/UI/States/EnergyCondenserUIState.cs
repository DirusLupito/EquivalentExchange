using EquivalentExchange.Common.Systems;
using EquivalentExchange.Common.Players;
using EquivalentExchange.TileEntities;
using EquivalentExchange.UI.Elements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.ID;
using Terraria.DataStructures;
using System.Collections.Generic;
using Terraria.Audio;
namespace EquivalentExchange.UI.States
{
    public class EnergyCondenserUIState : UIState
    {
        // UI elements
        private UIPanel mainPanel;
        private UIText emcText;
        private UIText titleText;
        private UIPanel lootButton; // Add this line
        private UIText lootButtonText; // Add this line
        
        // Template slot
        private UIImage templateSlotContainer;
        private UIItemImage templateSlotImage;
        
        // Inventory slots (7x8 grid = 56 slots)
        private UIImage[] inventorySlotContainers;
        private UIItemImage[] inventorySlotImages;

        // Constants
        
        // Amount of item slots in the inventory
        private const int INVENTORY_WIDTH = 8; 
        private const int INVENTORY_HEIGHT = 7;
        private const int INVENTORY_SIZE = INVENTORY_WIDTH * INVENTORY_HEIGHT; // Now 56 instead of 91

        // UI dimensions of the item slots
        private const float SLOT_SIZE = 52f;
        private const float SLOT_SPACING = 54f;

        // UI dimensions of the main panel
        private const float PANEL_WIDTH = 550f;
        private const float PANEL_HEIGHT = 480f;
        
        // Reference to the tile entity
        private EnergyCondenserTileEntity tileEntity;
        
        // Tracking variables
        private bool hoveringTemplateSlot = false;
        private int hoveringInventorySlot = -1;

        public void SetTileEntity(EnergyCondenserTileEntity entity)
        {
            tileEntity = entity;
        }

        public override void OnInitialize()
        {
            // Main panel
            mainPanel = new UIPanel();
            mainPanel.Width.Set(PANEL_WIDTH, 0f);
            mainPanel.Height.Set(PANEL_HEIGHT, 0f);
            // Position at bottom left middle
            mainPanel.HAlign = 0.2f; // Left middle
            mainPanel.VAlign = 1f; // Bottom
            mainPanel.Left.Set(20f, 0f); // Give some margin from the left edge
            mainPanel.Top.Set(-20f, 0f); // Move up slightly from bottom
            mainPanel.SetPadding(10f);
            mainPanel.BackgroundColor = new Color(73, 94, 171);
            Append(mainPanel);

            // Title
            titleText = new UIText("Energy Condenser", 1.2f);
            titleText.HAlign = 0.5f;
            titleText.Top.Set(5f, 0f);
            mainPanel.Append(titleText);

            // EMC display
            emcText = new UIText("EMC: 0", 1f);
            emcText.Left.Set(10f, 0f);
            emcText.Top.Set(35f, 0f);
            mainPanel.Append(emcText);

            // Add loot button
            CreateLootButton();

            // Template slot
            CreateTemplateSlot();
            
            // Inventory slots
            CreateInventorySlots();
        }

        private void CreateLootButton()
        {
            // Loot button
            lootButton = new UIPanel();
            lootButton.Width.Set(100f, 0f);
            lootButton.Height.Set(30f, 0f);
            lootButton.Left.Set(PANEL_WIDTH - 120f, 0f); // Position at top right
            lootButton.Top.Set(35f, 0f);
            lootButton.BackgroundColor = new Color(100, 150, 100);
            lootButton.BorderColor = new Color(80, 120, 80);
            lootButton.OnLeftClick += LootButton_OnLeftClick;
            lootButton.OnMouseOver += LootButton_OnMouseOver;
            lootButton.OnMouseOut += LootButton_OnMouseOut;
            mainPanel.Append(lootButton);

            // Loot button text
            lootButtonText = new UIText("Loot All", 0.8f);
            lootButtonText.HAlign = 0.5f;
            lootButtonText.VAlign = 0.5f;
            lootButton.Append(lootButtonText);
        }

        private void LootButton_OnLeftClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (tileEntity == null) return;

            // Collect all items from inventory (excluding template)
            List<Item> itemsToLoot = new List<Item>();
            
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                if (!tileEntity.inventory[i].IsAir)
                {
                    itemsToLoot.Add(tileEntity.inventory[i].Clone());
                    tileEntity.inventory[i] = new Item();
                }
            }

            // Get the player who is accessing the Energy Condenser
            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead)
            {
                return; // No active player to give items to
            }

            // Search through the player's inventory and build a map of item types to their indices
            Dictionary<int, int> itemIndexMap = new Dictionary<int, int>();
            for (int i = 0; i < player.inventory.Length; i++)
            {
                if (player.inventory[i].IsAir) continue;
                if (!itemIndexMap.ContainsKey(player.inventory[i].type))
                {
                    itemIndexMap[player.inventory[i].type] = i;
                }
            }

            // Give items to player
            foreach (Item item in itemsToLoot)
            {
                // Check and see if we can add items to existing stacks
                if (itemIndexMap.TryGetValue(item.type, out int existingIndex))
                {
                    // If we have an existing stack, try to add to it
                    Item existingItem = player.inventory[existingIndex];
                    int transferAmount = Math.Min(item.stack, existingItem.maxStack - existingItem.stack);
                    if (transferAmount > 0)
                    {
                        existingItem.stack += transferAmount;
                        item.stack -= transferAmount;

                        // If the item is fully transferred, we can skip adding it again
                        if (item.stack <= 0) continue;
                    }
                }

                // If we still have items left, try to find an empty slot
                bool addedToInventory = false;
                for (int i = 0; i < player.inventory.Length; i++)
                {
                    if (player.inventory[i].IsAir)
                    {
                        player.inventory[i] = item;
                        addedToInventory = true;
                        break;
                    }
                }

                // If the item still has stack/wasn't fully transferred, try to put it back into the first empty slot in the condenser
                bool addedToCondenserInventory = false;
                if (!addedToInventory && item.stack > 0)
                {
                    for (int i = 0; i < INVENTORY_SIZE; i++)
                    {
                        if (tileEntity.inventory[i].IsAir)
                        {
                            tileEntity.inventory[i] = item;
                            addedToCondenserInventory = true;
                            item.stack = 0; // Mark as transferred
                            break;
                        }
                    }
                }

                // If we still have items left, drop them on the ground
                if (!addedToInventory && !addedToCondenserInventory && item.stack > 0)
                {
                    player.QuickSpawnItem(player.GetSource_Misc("EnergyCondenserLoot"), item);
                }

            }

            // Sync changes to server if in multiplayer
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // Send clear inventory message to server
                for (int i = 0; i < INVENTORY_SIZE; i++)
                {
                    EMCNetCodeSystem.SendModifyInventory(
                        tileEntity.Position.X,
                        tileEntity.Position.Y,
                        i,
                        new Item());
                }
            }

            UpdateDisplay();
            
            // Play sound effect
            SoundEngine.PlaySound(SoundID.Grab);
        }

        private void LootButton_OnMouseOver(UIMouseEvent evt, UIElement listeningElement)
        {
            lootButton.BackgroundColor = new Color(120, 170, 120);
            Main.hoverItemName = "Loot all items from the Energy Condenser\n(Template item will remain)";
        }

        private void LootButton_OnMouseOut(UIMouseEvent evt, UIElement listeningElement)
        {
            lootButton.BackgroundColor = new Color(100, 150, 100);
        }

        private void CreateTemplateSlot()
        {
            // Template slot container
            templateSlotContainer = new UIImage(TextureAssets.InventoryBack);
            templateSlotContainer.Left.Set(10f, 0f);
            templateSlotContainer.Top.Set(65f, 0f);
            templateSlotContainer.Width.Set(SLOT_SIZE, 0f);
            templateSlotContainer.Height.Set(SLOT_SIZE, 0f);
            templateSlotContainer.OnLeftClick += TemplateSlot_OnLeftClick;
            templateSlotContainer.OnRightClick += TemplateSlot_OnRightClick;
            mainPanel.Append(templateSlotContainer);

            // Template slot image
            templateSlotImage = new UIItemImage();
            templateSlotImage.Left.Set(10f, 0f);
            templateSlotImage.Top.Set(10f, 0f);
            templateSlotImage.Width.Set(32f, 0f);
            templateSlotImage.Height.Set(32f, 0f);
            templateSlotContainer.Append(templateSlotImage);
        }

        private void CreateInventorySlots()
        {
            inventorySlotContainers = new UIImage[INVENTORY_SIZE];
            inventorySlotImages = new UIItemImage[INVENTORY_SIZE];

            float startX = 80f; // Start after template slot
            float startY = 65f;

            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                int row = i / INVENTORY_WIDTH;
                int col = i % INVENTORY_WIDTH;

                float x = startX + col * SLOT_SPACING;
                float y = startY + row * SLOT_SPACING;

                // Container
                inventorySlotContainers[i] = new UIImage(TextureAssets.InventoryBack);
                inventorySlotContainers[i].Left.Set(x, 0f);
                inventorySlotContainers[i].Top.Set(y, 0f);
                inventorySlotContainers[i].Width.Set(SLOT_SIZE, 0f);
                inventorySlotContainers[i].Height.Set(SLOT_SIZE, 0f);
                
                int slotIndex = i; // Capture for closure
                inventorySlotContainers[i].OnLeftClick += (evt, element) => InventorySlot_OnLeftClick(slotIndex);
                inventorySlotContainers[i].OnRightClick += (evt, element) => InventorySlot_OnRightClick(slotIndex);
                
                mainPanel.Append(inventorySlotContainers[i]);

                // Item image
                inventorySlotImages[i] = new UIItemImage();
                inventorySlotImages[i].Left.Set(10f, 0f);
                inventorySlotImages[i].Top.Set(10f, 0f);
                inventorySlotImages[i].Width.Set(32f, 0f);
                inventorySlotImages[i].Height.Set(32f, 0f);
                inventorySlotContainers[i].Append(inventorySlotImages[i]);
            }
        }

        private void TemplateSlot_OnLeftClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (tileEntity == null) return;

            // Left click sets template (without consuming the item)
            if (!Main.mouseItem.IsAir)
            {
                Item newTemplateItem = Main.mouseItem.IsAir ? new Item() : Main.mouseItem.Clone();
                newTemplateItem.stack = 1;
                
                // In multiplayer, send change to server first
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    EMCNetCodeSystem.SendModifyTemplate(
                        tileEntity.Position.X,
                        tileEntity.Position.Y,
                        newTemplateItem);
                }
                else
                {
                    // In singleplayer, update directly
                    if (tileEntity.TrySetTemplate(Main.mouseItem))
                    {
                        // Success
                    }
                    else
                    {
                        // Item has no EMC value
                        Main.NewText("This item has no EMC value and cannot be used as a template.", Color.Red);
                    }
                }
            }
            else if (!tileEntity.templateItem.IsAir)
            {
                // If the user clicks with an empty hand, clear the template
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    EMCNetCodeSystem.SendModifyTemplate(
                        tileEntity.Position.X,
                        tileEntity.Position.Y,
                        new Item());
                }
                else
                {
                    tileEntity.TrySetTemplate(new Item());
                }
            }
            
            UpdateDisplay();
        }

        private void TemplateSlot_OnRightClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (tileEntity == null) return;
        
            // Right click clears template
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                EMCNetCodeSystem.SendModifyTemplate(
                    tileEntity.Position.X,
                    tileEntity.Position.Y,
                    new Item());
            }
            else
            {
                tileEntity.TrySetTemplate(new Item());
            }
            
            UpdateDisplay();
        }

        private void InventorySlot_OnLeftClick(int slotIndex)
        {
            if (tileEntity == null || slotIndex < 0 || slotIndex >= INVENTORY_SIZE) return;

            Item slotItem = tileEntity.inventory[slotIndex];
            
            if (!Main.mouseItem.IsAir && !slotItem.IsAir)
            {
                // Both mouse and slot have items - try to stack or swap
                if (Main.mouseItem.type == slotItem.type && slotItem.stack < slotItem.maxStack)
                {
                    // Stack items
                    int transferAmount = Math.Min(Main.mouseItem.stack, slotItem.maxStack - slotItem.stack);
                    slotItem.stack += transferAmount;
                    Main.mouseItem.stack -= transferAmount;
                    
                    if (Main.mouseItem.stack <= 0)
                        Main.mouseItem = new Item();
                        
                    // Sync change to server
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        EMCNetCodeSystem.SendModifyInventory(
                            tileEntity.Position.X,
                            tileEntity.Position.Y,
                            slotIndex,
                            tileEntity.inventory[slotIndex]);
                    }
                }
                else
                {
                    // Swap items - direct reference swap
                    Item temp = Main.mouseItem;
                    Main.mouseItem = slotItem;
                    
                    // Send both changes to server
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        // First, update the slot with the new item
                        EMCNetCodeSystem.SendModifyInventory(
                            tileEntity.Position.X,
                            tileEntity.Position.Y,
                            slotIndex,
                            temp);
                    }
                    
                    tileEntity.inventory[slotIndex] = temp;
                }
            }
            else if (!Main.mouseItem.IsAir && slotItem.IsAir)
            {
                // Put mouse item in empty slot
                tileEntity.inventory[slotIndex] = Main.mouseItem;
                
                // Sync to server
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    EMCNetCodeSystem.SendModifyInventory(
                        tileEntity.Position.X,
                        tileEntity.Position.Y,
                        slotIndex,
                        tileEntity.inventory[slotIndex]);
                }
                
                Main.mouseItem = new Item();
            }
            else if (Main.mouseItem.IsAir && !slotItem.IsAir)
            {
                // Take item from slot - direct reference
                Main.mouseItem = slotItem;
                tileEntity.inventory[slotIndex] = new Item();
                
                // Sync to server
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    EMCNetCodeSystem.SendModifyInventory(
                        tileEntity.Position.X,
                        tileEntity.Position.Y,
                        slotIndex,
                        new Item());
                }
            }
            
            UpdateDisplay();
        }

        private void InventorySlot_OnRightClick(int slotIndex)
        {
            if (tileEntity == null || slotIndex < 0 || slotIndex >= INVENTORY_SIZE) return;

            Item slotItem = tileEntity.inventory[slotIndex];
            
            if (!slotItem.IsAir)
            {
                // Right click takes half the stack (or 1 if stack is 1)
                int takeAmount = Math.Max(1, slotItem.stack / 2);
                
                if (Main.mouseItem.IsAir)
                {
                    Main.mouseItem = slotItem.Clone();
                    Main.mouseItem.stack = takeAmount;
                    slotItem.stack -= takeAmount;
                    
                    if (slotItem.stack <= 0)
                        tileEntity.inventory[slotIndex] = new Item();
                }
                else if (Main.mouseItem.type == slotItem.type && Main.mouseItem.stack < Main.mouseItem.maxStack)
                {
                    // Add to existing stack in mouse
                    int addAmount = Math.Min(takeAmount, Main.mouseItem.maxStack - Main.mouseItem.stack);
                    Main.mouseItem.stack += addAmount;
                    slotItem.stack -= addAmount;
                    
                    if (slotItem.stack <= 0)
                        tileEntity.inventory[slotIndex] = new Item();
                }
            }
            else if (!Main.mouseItem.IsAir)
            {
                // Right click places 1 item from mouse into empty slot
                tileEntity.inventory[slotIndex] = Main.mouseItem.Clone();
                tileEntity.inventory[slotIndex].stack = 1;
                Main.mouseItem.stack--;
                
                if (Main.mouseItem.stack <= 0)
                    Main.mouseItem = new Item();
            }
            
            UpdateDisplay();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Check if we should close the UI
            if (Main.LocalPlayer.TryGetModPlayer(out EMCPlayer emcPlayer))
            {
                // If the player's inventory is not open, close the energy condenser UI
                if (!Main.playerInventory && emcPlayer != null)
                {
                    EMCUI.CloseEnergyCondenserUI();
                    return;
                }
            }

            if (tileEntity == null)
            {
                // Close UI if tile entity is null
                EMCUI.CloseEnergyCondenserUI();
                return;
            }

            // Request a sync to keep up-to-date with server
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                EMCNetCodeSystem.RequestCondenserSync(tileEntity.Position.X, tileEntity.Position.Y);
                // Reset the tile entity to ensure we have the latest data
                TileEntity.TryGet(tileEntity.Position.X, tileEntity.Position.Y, out tileEntity);
                // If we can't get the tile entity, close the UI
                if (tileEntity == null)
                {
                    EMCUI.CloseEnergyCondenserUI();
                    return;
                }
            }


            // If the player is interacting with the UI, set mouseInterface to true
            if (!Main.LocalPlayer.mouseInterface && mainPanel.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }

            // Update hover states
            UpdateHoverStates();
            
            // Update display periodically
            UpdateDisplay();
        }

        private void UpdateHoverStates()
        {
            hoveringTemplateSlot = templateSlotContainer.ContainsPoint(Main.MouseScreen);
            hoveringInventorySlot = -1;
            
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                if (inventorySlotContainers[i].ContainsPoint(Main.MouseScreen))
                {
                    hoveringInventorySlot = i;
                    break;
                }
            }
        }

        private void UpdateDisplay()
        {
            if (tileEntity == null) return;

            // Update EMC text
            emcText.SetText($"EMC: {tileEntity.storedEMC}");

            // Update template slot
            templateSlotImage.Item = tileEntity.templateItem;

            // Update inventory slots
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                inventorySlotImages[i].Item = tileEntity.inventory[i];
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            // Draw hover tooltips
            if (hoveringTemplateSlot)
            {
                if (tileEntity.templateItem.IsAir)
                    Main.hoverItemName = "Left-click: Set template item\nRight-click: Clear template";
                else
                    Main.hoverItemName = $"Template: {tileEntity.templateItem.Name}\nLeft-click: Replace\nRight-click: Clear";
            }
            else if (hoveringInventorySlot >= 0 && hoveringInventorySlot < INVENTORY_SIZE)
            {
                Item item = tileEntity.inventory[hoveringInventorySlot];
                if (!item.IsAir)
                {
                    Main.hoverItemName = $"{item.Name} ({item.stack})";
                }
            }
        }
    }
}