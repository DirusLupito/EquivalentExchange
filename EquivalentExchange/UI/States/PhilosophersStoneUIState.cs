using EquivalentExchange.Common.Players;
using EquivalentExchange.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.GameContent;
using Terraria.ModLoader.UI;
using System.Collections.Generic;
using EquivalentExchange.Common.GlobalItems;

namespace EquivalentExchange.UI.States
{
    public class PhilosophersStoneUIState : UIState
    {
        // UI elements
        private UIPanel mainPanel;
        private UIText emcStorageText;

        // Transmutation circle slots
        private UIImage[] transmutationSlots;
        private Item[] transmutationItems;

        // Burn slot at the bottom
        private UIImage burnSlotPanel;
        private Item burnSlot = new Item();

        // Tracking variables
        private bool hoveringBurnSlot = false;
        private int hoveringTransmutationSlot = -1;

        // Constants for UI layout
        private const float PANEL_WIDTH = 500f;
        private const float PANEL_HEIGHT = 500f;
        private const float SLOT_SIZE = 50f;
        private const int TRANSMUTATION_SLOT_COUNT = 12;
        private const float CIRCLE_RADIUS = 150f;

        public override void OnInitialize()
        {
            // Initialize arrays
            transmutationSlots = new UIImage[TRANSMUTATION_SLOT_COUNT];
            transmutationItems = new Item[TRANSMUTATION_SLOT_COUNT];
            for (int i = 0; i < TRANSMUTATION_SLOT_COUNT; i++)
            {
                transmutationItems[i] = new Item();
            }

            // Main panel that holds everything
            mainPanel = new UIPanel();
            mainPanel.Width.Set(PANEL_WIDTH, 0f);
            mainPanel.Height.Set(PANEL_HEIGHT, 0f);
            mainPanel.HAlign = 0.5f; // Center horizontally
            mainPanel.VAlign = 0.4f; // Position vertically
            mainPanel.SetPadding(10f);
            mainPanel.BackgroundColor = new Color(73, 94, 171);
            Append(mainPanel);

            // Title text
            var titleText = new UIText("Transmutation", 1.2f);
            titleText.HAlign = 0.5f;
            titleText.Top.Set(10f, 0f);
            mainPanel.Append(titleText);

            // Create transmutation slots in a circle
            float centerX = PANEL_WIDTH / 2;
            float centerY = PANEL_HEIGHT / 2 - 20f; // Slightly above center to make room for bottom elements

            for (int i = 0; i < TRANSMUTATION_SLOT_COUNT; i++)
            {
                // Calculate position in the circle (starting from top position)
                float angle = (float)(Math.PI * 2 * i / TRANSMUTATION_SLOT_COUNT - Math.PI / 2);
                float x = centerX + CIRCLE_RADIUS * (float)Math.Cos(angle) - SLOT_SIZE / 2;
                float y = centerY + CIRCLE_RADIUS * (float)Math.Sin(angle) - SLOT_SIZE / 2;

                // Create the slot
                transmutationSlots[i] = new UIImage(TextureAssets.InventoryBack);
                transmutationSlots[i].Left.Set(x, 0f);
                transmutationSlots[i].Top.Set(y, 0f);
                transmutationSlots[i].Width.Set(SLOT_SIZE, 0f);
                transmutationSlots[i].Height.Set(SLOT_SIZE, 0f);
                
                // Store the index to use in the click handler
                int index = i;
                transmutationSlots[i].OnLeftClick += (evt, element) => TransmutationSlot_OnClick(index);
                
                mainPanel.Append(transmutationSlots[i]);
            }

            // EMC display (bottom left)
            emcStorageText = new UIText("EMC: 0");
            emcStorageText.Left.Set(0f, 0f);
            emcStorageText.Top.Set(PANEL_HEIGHT - 50f, 0f);
            mainPanel.Append(emcStorageText);
            
            // Burn slot (bottom right)
            burnSlotPanel = new UIImage(TextureAssets.InventoryBack);
            burnSlotPanel.Left.Set(PANEL_WIDTH - SLOT_SIZE - 20f, 0f); // Position it near right edge
            burnSlotPanel.Top.Set(PANEL_HEIGHT - 70f, 0f);
            burnSlotPanel.Width.Set(SLOT_SIZE, 0f);
            burnSlotPanel.Height.Set(SLOT_SIZE, 0f);
            mainPanel.Append(burnSlotPanel);
        }

        private void BurnCurrentItem()
        {
            if (!burnSlot.IsAir && Main.LocalPlayer.TryGetModPlayer(out EMCPlayer emcPlayer))
            {
                // Calculate EMC value of the item
                long emcValue = EMCHelper.ConvertItemToEMC(burnSlot);

                // Add EMC to player's stored EMC
                emcPlayer.AddEMC(emcValue);
                
                // Learn the item
                emcPlayer.LearnItem(burnSlot, EMCHelper.GetEMC(burnSlot));

                // Show a message to the player
                Main.NewText($"Learned {burnSlot.Name} ({emcValue} EMC).", Color.LightGreen);

                // Remove the item
                burnSlot = new Item();

                // Update the burn slot image
                burnSlotPanel.SetImage(TextureAssets.InventoryBack);
            }
        }

        // Handle click on a transmutation slot
        private void TransmutationSlot_OnClick(int slotIndex)
        {
            // Check if there's an item in this slot and player's hands are empty
            if (!transmutationItems[slotIndex].IsAir && Main.mouseItem.IsAir && 
                Main.LocalPlayer.TryGetModPlayer(out EMCPlayer emcPlayer))
            {
                Item selectedItem = transmutationItems[slotIndex];
                long emcCost = selectedItem.GetGlobalItem<EMCGlobalItem>().emc;
                
                // Check if player has enough EMC
                if (emcPlayer.TryRemoveEMC(emcCost))
                {
                    // Create a new item with stack size of 1
                    Item newItem = new Item();
                    newItem.SetDefaults(selectedItem.type);
                    newItem.stack = 1;
                    
                    // Give the item to the player
                    Main.mouseItem = newItem;
                    
                    // Show success message
                    Main.NewText($"Successfully transmuted {newItem.Name} for {emcCost} EMC.", Color.LightGreen);
                }
                else
                {
                    // Show error message if not enough EMC
                    Main.NewText($"Not enough EMC! Need {emcCost} EMC.", Color.Red);
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // If the player is interacting with the UI, set mouseInterface to true
            if (!Main.LocalPlayer.mouseInterface && mainPanel.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }

            // Update EMC display
            if (Main.LocalPlayer.TryGetModPlayer(out EMCPlayer emcPlayer))
            {
                emcStorageText.SetText($"EMC: {emcPlayer.storedEMC}");
                
                // Update transmutation slots with the most expensive items
                UpdateTransmutationSlots(emcPlayer);
            }

            // Handle dragging items to the burn slot
            HandleBurnSlot();

            // Track which slot the mouse is hovering over
            UpdateHoveredSlot();

            // Show player inventory
            Main.playerInventory = true;
        }
        
        private void UpdateTransmutationSlots(EMCPlayer emcPlayer)
        {
            // Get the 12 most expensive learned items
            List<LearnedItemInfo> topItems = emcPlayer.GetMostExpensiveItems(TRANSMUTATION_SLOT_COUNT);
            
            // Clear previous items
            for (int i = 0; i < TRANSMUTATION_SLOT_COUNT; i++)
            {
                transmutationItems[i] = new Item();
                transmutationSlots[i].SetImage(TextureAssets.InventoryBack);
            }
            
            // Add new items to slots
            for (int i = 0; i < topItems.Count; i++)
            {
                // Create item instance
                Item item = new Item();
                item.SetDefaults(topItems[i].ItemType);
                
                // Store the item
                transmutationItems[i] = item;
                
                // Update the slot image
                transmutationSlots[i].SetImage(TextureAssets.Item[item.type]);
            }
        }
        
        private void UpdateHoveredSlot()
        {
            hoveringTransmutationSlot = -1;
            
            for (int i = 0; i < TRANSMUTATION_SLOT_COUNT; i++)
            {
                if (transmutationSlots[i].ContainsPoint(Main.MouseScreen) && !transmutationItems[i].IsAir)
                {
                    hoveringTransmutationSlot = i;
                    break;
                }
            }
        }

        private void HandleBurnSlot()
        {
            // Check if mouse is hovering the burn slot
            hoveringBurnSlot = burnSlotPanel.ContainsPoint(Main.MouseScreen);

            // Handle item interactions
            if (hoveringBurnSlot)
            {
                // Handle item dropping into burn slot
                if (Main.mouseItem != null && !Main.mouseItem.IsAir && Main.mouseLeft)
                {
                    burnSlot = Main.mouseItem.Clone();
                    Main.mouseItem = new Item();

                    // Update the burn slot image
                    transmutationSlots[0].SetImage(TextureAssets.Item[burnSlot.type]);
                    // Burn the item
                    BurnCurrentItem();
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            // Draw the item name on the mouse if hovering over the burn slot
            if (hoveringBurnSlot)
            {
                Main.hoverItemName = "Left-click to burn item held by mouse";
            }
            // Show item info when hovering over a transmutation slot
            else if (hoveringTransmutationSlot >= 0)
            {
                Item item = transmutationItems[hoveringTransmutationSlot];
                if (!item.IsAir)
                {
                    long emcCost = item.GetGlobalItem<EMCGlobalItem>().emc;
                    Main.hoverItemName = $"{item.Name} ({emcCost} EMC)";
                }
            }
        }
    }
}