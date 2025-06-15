using EquivalentExchange.Common.Players;
using EquivalentExchange.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.GameContent;
using System.Collections.Generic;
using EquivalentExchange.Common.GlobalItems;
using EquivalentExchange.Common.Systems;
using EquivalentExchange.UI.Elements;

namespace EquivalentExchange.UI.States
{
    public class TransmutationTabletUIState : UIState
    {
        // UI elements
        private UIPanel mainPanel;
        private UIText emcStorageText;
        private UIImageButton nextPageButton;
        private UIImageButton prevPageButton;
        private UIText pageInfoText;

        // Current page for transmutation items
        private int currentPage = 0;

        // Transmutation circle slots
        private UIImage[] transmutationSlotContainers; // Container images
        private UIItemImage[] transmutationSlots; // Item display elements
        private Item[] transmutationItems;

        // Burn slot at the bottom
        private UIImage burnSlotContainer;
        private UIImage burnSlotPanel;
        private Item burnSlot = new Item();

        // Unlearn slot (bottom middle right)
        private UIImage unlearnSlotPanel;
        private Item unlearnSlot = new Item();

        // Unlearn slot container picture
        private UIImage unlearnSlotPanelContainerPicture;

        // Tracking variables
        private bool hoveringBurnSlot = false;
        private bool hoveringUnlearnSlot = false;
        private int hoveringTransmutationSlot = -1;
        
        // Right-click tracking
        private bool isRightMouseDown = false;
        private int rightClickHoldTime = 0;
        private int activeRightClickSlot = -1;

        // Constants for UI layout
        private const float PANEL_WIDTH = 500f;
        private const float PANEL_HEIGHT = 500f;
        private float SLOT_WIDTH = TextureAssets.InventoryBack.Width(); 
        private float SLOT_HEIGHT = TextureAssets.InventoryBack.Height();
        private const int TRANSMUTATION_SLOT_COUNT = 12;
        private const float CIRCLE_RADIUS = 150f;

        // Constants for right-click item creation timing
        private const int INITIAL_HOLD_DELAY = 30; // Initial delay (frames)
        private const int CONTINUOUS_ITEM_DELAY = 5; // Delay between continuous item creation (frames)

        private const double EXPONENTIAL_BASE_ITEM_INCREASE = 1.1; // Base for exponential growth of item creation amount

        public override void OnInitialize()
        {
            // Initialize arrays
            transmutationSlotContainers = new UIImage[TRANSMUTATION_SLOT_COUNT];
            transmutationSlots = new UIItemImage[TRANSMUTATION_SLOT_COUNT];
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

            // Add navigation buttons
            AddNavigationButtons();

            // Create transmutation slots in a circle
            float centerX = PANEL_WIDTH / 2;
            float centerY = PANEL_HEIGHT / 2;

            for (int i = 0; i < TRANSMUTATION_SLOT_COUNT; i++)
            {
                // Calculate position in the circle (starting from top position)
                float angle = (float)(Math.PI * 2 * i / TRANSMUTATION_SLOT_COUNT - Math.PI / 2);
                float x = centerX + CIRCLE_RADIUS * (float)Math.Cos(angle) - SLOT_WIDTH / 2f - 10;
                float y = centerY + CIRCLE_RADIUS * (float)Math.Sin(angle) - SLOT_HEIGHT / 2f;

                // Create the slot container (background)
                transmutationSlotContainers[i] = new UIImage(TextureAssets.InventoryBack);
                transmutationSlotContainers[i].Left.Set(x, 0f);
                transmutationSlotContainers[i].Top.Set(y, 0f);
                transmutationSlotContainers[i].Width.Set(SLOT_WIDTH, 0f);
                transmutationSlotContainers[i].Height.Set(SLOT_HEIGHT, 0f);

                // Store the index for event handlers
                int index = i;
                transmutationSlotContainers[i].OnLeftClick += (evt, element) => TransmutationSlot_OnLeftClick(index);
                transmutationSlotContainers[i].OnRightClick += (evt, element) => TransmutationSlot_OnRightClick(index);
                transmutationSlotContainers[i].OnRightMouseDown += (evt, element) => TransmutationSlot_OnRightMouseDown(index);
                transmutationSlotContainers[i].OnRightMouseUp += (evt, element) => TransmutationSlot_OnRightMouseUp(index);

                mainPanel.Append(transmutationSlotContainers[i]);

                // Create the item display element
                transmutationSlots[i] = new UIItemImage();
                transmutationSlots[i].Left.Set(x, 0f);
                transmutationSlots[i].Top.Set(y, 0f);
                transmutationSlots[i].Width.Set(SLOT_WIDTH, 0f);
                transmutationSlots[i].Height.Set(SLOT_HEIGHT, 0f);
                
                // Add event handlers to the item image as well
                transmutationSlots[i].OnLeftClick += (evt, element) => TransmutationSlot_OnLeftClick(index);
                transmutationSlots[i].OnRightClick += (evt, element) => TransmutationSlot_OnRightClick(index);
                transmutationSlots[i].OnRightMouseDown += (evt, element) => TransmutationSlot_OnRightMouseDown(index);
                transmutationSlots[i].OnRightMouseUp += (evt, element) => TransmutationSlot_OnRightMouseUp(index);
                
                mainPanel.Append(transmutationSlots[i]);
            }

            // EMC display (bottom left)
            emcStorageText = new UIText("EMC: 0");
            emcStorageText.Left.Set(0f, 0f);
            emcStorageText.Top.Set(PANEL_HEIGHT - 50f, 0f);
            mainPanel.Append(emcStorageText);

            // Page info text (center bottom)
            pageInfoText = new UIText("Page 1");
            pageInfoText.HAlign = 0.5f;
            pageInfoText.Top.Set(PANEL_HEIGHT - 50f, 0f);
            mainPanel.Append(pageInfoText);

            // Burn slot container (bottom right)
            burnSlotContainer = new UIImage(TextureAssets.InventoryBack);
            burnSlotContainer.Left.Set(PANEL_WIDTH - SLOT_WIDTH - 20f, 0f);
            burnSlotContainer.Top.Set(PANEL_HEIGHT - 70f, 0f);
            burnSlotContainer.Width.Set(SLOT_WIDTH, 0f);
            burnSlotContainer.Height.Set(SLOT_HEIGHT, 0f);
            burnSlotContainer.OnLeftClick += (evt, element) => BurnSlot_OnLeftClick();
            mainPanel.Append(burnSlotContainer);

            // Calculate center of burn slot container
            float burnSlotCenterX = burnSlotContainer.Left.Pixels + SLOT_WIDTH / 2f;
            float burnSlotCenterY = burnSlotContainer.Top.Pixels + SLOT_HEIGHT / 2f;

            ReLogic.Content.Asset<Texture2D> burnSlotTexture = TextureAssets.SunOrb;

            // Figure out how to position burn slot image inside burn slot container
            float burnSlotImageLeft = burnSlotCenterX - burnSlotTexture.Width() / 2f;
            float burnSlotImageTop = burnSlotCenterY - burnSlotTexture.Height() / 2f;

            // The picture of the burn slot
            burnSlotPanel = new UIImage(burnSlotTexture);
            burnSlotPanel.Left.Set(burnSlotImageLeft, 0f);
            burnSlotPanel.Top.Set(burnSlotImageTop, 0f);
            burnSlotPanel.Width.Set(burnSlotTexture.Width(), 0f);
            burnSlotPanel.Height.Set(burnSlotTexture.Height(), 0f);
            // Also add the event listeners for the burn slot itself, since otherwise the space that is occupied by the icon will not trigger the click events
            burnSlotPanel.OnLeftClick += (evt, element) => BurnSlot_OnLeftClick();
            mainPanel.Append(burnSlotPanel);

            // Box image containing the unlearn slot
            unlearnSlotPanelContainerPicture = new UIImage(TextureAssets.InventoryBack);
            unlearnSlotPanelContainerPicture.Left.Set(PANEL_WIDTH - SLOT_WIDTH - 20f - SLOT_WIDTH - 10f, 0f);
            unlearnSlotPanelContainerPicture.Top.Set(PANEL_HEIGHT - 70f, 0f);
            unlearnSlotPanelContainerPicture.Width.Set(SLOT_WIDTH, 0f);
            unlearnSlotPanelContainerPicture.Height.Set(SLOT_HEIGHT, 0f);
            unlearnSlotPanelContainerPicture.OnLeftClick += (evt, element) => UnlearnSlot_OnLeftClick();
            mainPanel.Append(unlearnSlotPanelContainerPicture);

            // Figure out the center position of the unlearnSlotPanelContainerPicture (.left will set the left edge while .Top will set the top edge)
            // Moreover, the actual width and height as seen by the player depend on the texture
            float unlearnSlotPanelContainerPictureCenterX = unlearnSlotPanelContainerPicture.Left.Pixels + SLOT_WIDTH / 2f;
            float unlearnSlotPanelContainerPictureCenterY = unlearnSlotPanelContainerPicture.Top.Pixels + SLOT_HEIGHT / 2f;

            ReLogic.Content.Asset<Texture2D> unlearnSlotTexture = TextureAssets.MapDeath;

            // Since this is also the intended center position of the unlearnSlotPanel, we will use it to derive the left and top positions of the unlearnSlotPanel
            float unlearnSlotPanelLeft = unlearnSlotPanelContainerPictureCenterX - unlearnSlotTexture.Width() / 2f;
            float unlearnSlotPanelTop = unlearnSlotPanelContainerPictureCenterY - unlearnSlotTexture.Height() / 2f;

            // Unlearn slot (bottom middle right, centered inside the unlearn slot container)
            unlearnSlotPanel = new UIImage(unlearnSlotTexture);
            unlearnSlotPanel.Left.Set(unlearnSlotPanelLeft, 0f);
            unlearnSlotPanel.Top.Set(unlearnSlotPanelTop, 0f);
            unlearnSlotPanel.Width.Set(unlearnSlotTexture.Width(), 0f);
            unlearnSlotPanel.Height.Set(unlearnSlotTexture.Height(), 0f);
            // Also add the event listeners for the unlearn slot itself, since otherwise the space that is occupied by the icon will not trigger the click events
            unlearnSlotPanel.OnLeftClick += (evt, element) => UnlearnSlot_OnLeftClick();
            mainPanel.Append(unlearnSlotPanel);
        }

        private void AddNavigationButtons()
        {
            var realButtonWidth = Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Button_Back").Value.Width;
            var realButtonHeight = Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Button_Back").Value.Height;
            // Previous page button (left arrow)
            prevPageButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Button_Back"));
            prevPageButton.Left.Set(realButtonWidth / 2, 0f);
            prevPageButton.Top.Set(PANEL_HEIGHT / 2 - realButtonHeight / 2, 0f);
            prevPageButton.Width.Set(realButtonWidth, 0f);
            prevPageButton.Height.Set(realButtonHeight, 0f);
            prevPageButton.OnLeftClick += (evt, element) => NavigatePrevPage();
            mainPanel.Append(prevPageButton);

            // Next page button (right arrow)
            nextPageButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Button_Forward"));
            nextPageButton.Left.Set(PANEL_WIDTH - 2 * realButtonWidth, 0f);
            nextPageButton.Top.Set(PANEL_HEIGHT / 2 - realButtonHeight / 2, 0f);
            nextPageButton.Width.Set(realButtonWidth, 0f);
            nextPageButton.Height.Set(realButtonHeight, 0f);
            nextPageButton.OnLeftClick += (evt, element) => NavigateNextPage();
            mainPanel.Append(nextPageButton);
        }

        private void NavigatePrevPage()
        {
            if (currentPage > 0)
            {
                currentPage--;
                UpdateTransmutationSlots(Main.LocalPlayer.GetModPlayer<EMCPlayer>());
                UpdatePageInfo();
            }
        }

        private void NavigateNextPage()
        {
            EMCPlayer emcPlayer = Main.LocalPlayer.GetModPlayer<EMCPlayer>();
            int affordableItems = emcPlayer.GetAffordableLearnedItemCount();
            int totalPages = (int)Math.Ceiling(affordableItems / (float)TRANSMUTATION_SLOT_COUNT);
            
            if (currentPage < totalPages - 1)
            {
                currentPage++;
                UpdateTransmutationSlots(emcPlayer);
                UpdatePageInfo();
            }
        }

        private void UpdatePageInfo()
        {
            EMCPlayer emcPlayer = Main.LocalPlayer.GetModPlayer<EMCPlayer>();
            int affordableItems = emcPlayer.GetAffordableLearnedItemCount();
            int totalPages = Math.Max(1, (int)Math.Ceiling(affordableItems / (float)TRANSMUTATION_SLOT_COUNT));
            
            pageInfoText.SetText($"Page {currentPage + 1}/{totalPages}");
            
            // Enable/disable buttons based on current page
            prevPageButton.SetVisibility(1f, currentPage > 0 ? 1f : 0.5f);
            nextPageButton.SetVisibility(1f, currentPage < totalPages - 1 ? 1f : 0.5f);
        }

        // Handles updating the displayed items in the transmutation slot as well as the displayed EMC value
        private void updateDisplay()
        {
            if (Main.LocalPlayer.TryGetModPlayer(out EMCPlayer emcPlayer))
            {
                // Update EMC display
                emcStorageText.SetText($"EMC: {emcPlayer.storedEMC}");
                
                // Update transmutation slots with the current page of items
                UpdateTransmutationSlots(emcPlayer);
                
                // Update page info
                UpdatePageInfo();
            }
        }

        private void BurnCurrentItem()
        {
            if (!burnSlot.IsAir && Main.LocalPlayer.TryGetModPlayer(out EMCPlayer emcPlayer))
            {
                // Calculate EMC value of the item
                RationalNumber emcValue = EMCHelper.ConvertItemToEMC(burnSlot);

                // Add EMC to player's stored EMC
                emcPlayer.AddEMC(emcValue);

                // Learn the item
                emcPlayer.LearnItem(burnSlot, EMCHelper.GetEMC(burnSlot));

                // Show a message to the player
                Main.NewText($"Burned {burnSlot.stack} x {burnSlot.Name} for {emcValue} EMC.", Color.LightGreen);

                // Remove the item
                burnSlot = new Item();
            }
        }

        // Handle click on a transmutation slot
        private void TransmutationSlot_OnLeftClick(int slotIndex)
        {
            // Check if there's an item in this slot and player's hands are empty
            if (!transmutationItems[slotIndex].IsAir && Main.mouseItem.IsAir &&
                Main.LocalPlayer.TryGetModPlayer(out EMCPlayer emcPlayer))
            {
                Item selectedItem = transmutationItems[slotIndex];
                RationalNumber emcCostPerItem = selectedItem.GetGlobalItem<EMCGlobalItem>().emc;

                // Calculate how many items the player can afford
                int maxStackSize = selectedItem.maxStack;
                // Handle the case where emcCostPerItem is zero
                int affordableCount = emcCostPerItem > RationalNumber.Zero ?
                    (int)Math.Min(maxStackSize, (double)(emcPlayer.storedEMC / emcCostPerItem)) : maxStackSize;

                // Make sure we can create at least one
                if (affordableCount > 0)
                {
                    // Calculate total EMC cost
                    RationalNumber totalEmcCost = emcCostPerItem * affordableCount;

                    // Create a new item with appropriate stack size
                    Item newItem = new Item();
                    newItem.SetDefaults(selectedItem.type);
                    newItem.stack = affordableCount;

                    // Give the item to the player
                    Main.mouseItem = newItem;

                    // Remove the EMC
                    emcPlayer.TryRemoveEMC(totalEmcCost);

                    // Show success message
                    Main.NewText($"Successfully transmuted {affordableCount}x {newItem.Name} for {totalEmcCost} EMC.", Color.LightGreen);
                }
                else
                {
                    // Show error message if not enough EMC
                    Main.NewText($"Not enough EMC! Need {emcCostPerItem} EMC.", Color.Red);
                }
            }
        }

        // Single right-click handler (creates a single item)
        private void TransmutationSlot_OnRightClick(int slotIndex)
        {
            CreateSingleItem(slotIndex);
        }
        
        // Right mouse down handler (for continuous item creation)
        private void TransmutationSlot_OnRightMouseDown(int slotIndex)
        {
            isRightMouseDown = true;
            activeRightClickSlot = slotIndex;
            rightClickHoldTime = 0;
        }
        
        // Called when right mouse button is released
        private void TransmutationSlot_OnRightMouseUp(int slotIndex)
        {
            isRightMouseDown = false;
            activeRightClickSlot = -1;
            rightClickHoldTime = 0;
        }

        // Helper method to create a single item
        private bool CreateSingleItem(int slotIndex)
        {
            return CreateItem(slotIndex, 1);
        }

        // Helper method to create amount many items
        private bool CreateItem(int slotIndex, int amount)
        {
            if (!transmutationItems[slotIndex].IsAir && 
                Main.LocalPlayer.TryGetModPlayer(out EMCPlayer emcPlayer))
            {
                Item selectedItem = transmutationItems[slotIndex];
                RationalNumber emcCost = selectedItem.GetGlobalItem<EMCGlobalItem>().emc;
                
                // Check if player has enough EMC
                if (emcPlayer.storedEMC >= emcCost)
                {
                    if (Main.mouseItem.IsAir)
                    {
                        // Create a new item with stack size of 1
                        Item newItem = new Item();
                        newItem.SetDefaults(selectedItem.type);
                        newItem.stack = amount;
                        
                        // Give the item to the player
                        Main.mouseItem = newItem;
                    }
                    else if (Main.mouseItem.type == selectedItem.type && Main.mouseItem.stack < Main.mouseItem.maxStack)
                    {
                        // Add to existing stack
                        Main.mouseItem.stack += Math.Min(amount, Main.mouseItem.maxStack - Main.mouseItem.stack);
                    }
                    else
                    {
                        // Player is holding a different item or max stack reached
                        return false;
                    }
                    
                    // Remove EMC
                    emcPlayer.TryRemoveEMC(emcCost * amount);
                    return true;
                }
                else
                {
                    // Not enough EMC
                    if (emcPlayer.storedEMC < emcCost)
                    {
                        Main.NewText($"Not enough EMC! Need {emcCost} EMC.", Color.Red);
                    }
                    return false;
                }
            }
            return false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            Main.LocalPlayer.TryGetModPlayer(out EMCPlayer emcPlayer);

            // If the player's inventory is not open, also close the transmutation tablet UI
            if (!Main.playerInventory && emcPlayer != null)
            {
                // Hide the UI if inventory is closed
                EMCUI.TransmutationTabletVisible = false;
                return;
            }

            // If the player is interacting with the UI, set mouseInterface to true
            if (!Main.LocalPlayer.mouseInterface && mainPanel.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }

            // Handle right-click holding for continuous item creation
            if (isRightMouseDown && activeRightClickSlot >= 0)
            {
                rightClickHoldTime++;

                // After initial delay, start creating items regularly
                if (rightClickHoldTime > INITIAL_HOLD_DELAY &&
                    rightClickHoldTime % CONTINUOUS_ITEM_DELAY == 0)
                {
                    // Exponentially increase the amount created as the player holds the right mouse button for longer periods
                    int amount = (int)Math.Pow(EXPONENTIAL_BASE_ITEM_INCREASE, (rightClickHoldTime - INITIAL_HOLD_DELAY) / CONTINUOUS_ITEM_DELAY);
                    CreateItem(activeRightClickSlot, amount);
                }
            }

            // Track which slot the mouse is hovering over
            UpdateHoveredSlot();

            // Update the display of transmutation slots and EMC value
            updateDisplay();
        }
        
        private void UpdateTransmutationSlots(EMCPlayer emcPlayer)
        {
            // Get the current page of affordable items
            List<LearnedItemInfo> pageItems = emcPlayer.GetAffordableItemsPaginated(TRANSMUTATION_SLOT_COUNT, currentPage);
            
            // Clear previous items
            for (int i = 0; i < TRANSMUTATION_SLOT_COUNT; i++)
            {
                transmutationItems[i] = new Item();
                transmutationSlots[i].Item = new Item(); // Update UIItemImage with empty item
            }

            // Add new items to slots
            for (int i = 0; i < pageItems.Count; i++)
            {
                // Create item instance
                Item item = new Item();
                item.SetDefaults(pageItems[i].ItemType);

                // Store the item
                transmutationItems[i] = item;
                
                // Update the UIItemImage with the new item
                transmutationSlots[i].Item = item;
            }
        }
        
        private void UpdateHoveredSlot()
        {
            hoveringTransmutationSlot = -1;
            hoveringBurnSlot = false;
            hoveringUnlearnSlot = false;
            
            for (int i = 0; i < TRANSMUTATION_SLOT_COUNT; i++)
            {
                if (transmutationSlotContainers[i].ContainsPoint(Main.MouseScreen) && !transmutationItems[i].IsAir)
                {
                    hoveringTransmutationSlot = i;
                    return;
                }
            }
            
            if (burnSlotContainer.ContainsPoint(Main.MouseScreen))
            {
                hoveringBurnSlot = true;
                return;
            }
            
            if (unlearnSlotPanelContainerPicture.ContainsPoint(Main.MouseScreen))
            {
                hoveringUnlearnSlot = true;
                return;
            }
        }

        private void BurnSlot_OnLeftClick()
        {
            // Handle item dropping into burn slot
            if (Main.mouseItem != null && !Main.mouseItem.IsAir)
            {
                // If the item has a zero or negative EMC value, do not allow burning
                if (EMCHelper.GetEMC(Main.mouseItem) <= RationalNumber.Zero)
                {
                    Main.NewText("You cannot transmute this item.", Color.Red);
                    return;
                }
                burnSlot = Main.mouseItem.Clone();
                Main.mouseItem = new Item();

                // Burn the item
                BurnCurrentItem();
            }
        }

        private void UnlearnSlot_OnLeftClick()
        {
            // Handle item dropping into unlearn slot
            if (Main.mouseItem != null && !Main.mouseItem.IsAir)
            {
                // Store a copy of the item (we won't destroy it)
                unlearnSlot = Main.mouseItem.Clone();
                
                // Attempt to unlearn the item
                if (Main.LocalPlayer.TryGetModPlayer(out EMCPlayer emcPlayer))
                {
                    if (emcPlayer.UnlearnItem(unlearnSlot.type))
                    {
                        // Show success message
                        Main.NewText($"Unlearned {unlearnSlot.Name}. You can no longer transmute this item.", Color.Orange);
                    }
                    else
                    {
                        // Show message if item wasn't learned in the first place
                        Main.NewText($"You haven't learned {unlearnSlot.Name} yet.", Color.Red);
                    }
                }
                
                // Clear the slot but keep the player's item
                unlearnSlot = new Item();
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            // Draw the item name on the mouse if hovering over slots
            if (hoveringBurnSlot)
            {
                Main.hoverItemName = "Left-click to burn item held by mouse";
            }
            else if (hoveringUnlearnSlot)
            {
                Main.hoverItemName = "Left-click to unlearn item held by mouse";
            }
            else if (hoveringTransmutationSlot >= 0)
            {
                Item item = transmutationItems[hoveringTransmutationSlot];
                if (!item.IsAir)
                {
                    RationalNumber emcCost = item.GetGlobalItem<EMCGlobalItem>().emc;
                    Main.hoverItemName = $"{item.Name} ({emcCost} EMC)";
                }
            }
            // Show tooltip for navigation buttons
            else if (prevPageButton.IsMouseHovering)
            {
                Main.hoverItemName = "Previous Page";
            }
            else if (nextPageButton.IsMouseHovering)
            {
                Main.hoverItemName = "Next Page";
            }
        }
    }
}