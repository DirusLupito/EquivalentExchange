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

namespace EquivalentExchange.UI.States
{
    public class PhilosophersStoneUIState : UIState
    {
        // UI elements
        private UIPanel mainPanel;
        private UIText emcStorageText;
        private UIImage burnSlotPanel;
        private UIImageButton burnButton;
        private Item burnSlot = new Item();
        private bool hoveringBurnSlot = false;

        // Constants for UI layout
        private const float PANEL_WIDTH = 500f;
        private const float PANEL_HEIGHT = 260f;
        private const float BURN_SLOT_SIZE = 50f;

        public override void OnInitialize()
        {
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
            var titleText = new UIText("Philosopher's Stone - Transmutation", 1.2f);
            titleText.HAlign = 0.5f;
            titleText.Top.Set(10f, 0f);
            mainPanel.Append(titleText);

            // EMC display
            emcStorageText = new UIText("EMC: 0");
            emcStorageText.HAlign = 0.5f;
            emcStorageText.Top.Set(40f, 0f);
            mainPanel.Append(emcStorageText);

            // Burn slot
            burnSlotPanel = new UIImage(TextureAssets.InventoryBack);
            burnSlotPanel.Width.Set(BURN_SLOT_SIZE, 0f);
            burnSlotPanel.Height.Set(BURN_SLOT_SIZE, 0f);
            burnSlotPanel.HAlign = 0.5f;
            burnSlotPanel.Top.Set(80f, 0f);
            mainPanel.Append(burnSlotPanel);

            // Burn button
            burnButton = new UIImageButton(ModContent.Request<Texture2D>("Terraria/Images/UI/ButtonPlay"));
            burnButton.Width.Set(30f, 0f);
            burnButton.Height.Set(30f, 0f);
            burnButton.HAlign = 0.5f;
            burnButton.Top.Set(140f, 0f);
            burnButton.OnLeftClick += BurnButton_OnClick;
            mainPanel.Append(burnButton);

            // Burn slot label
            var burnSlotLabel = new UIText("Burn Slot");
            burnSlotLabel.HAlign = 0.5f;
            burnSlotLabel.Top.Set(180f, 0f);
            mainPanel.Append(burnSlotLabel);

            // Instructions
            var instructionsText = new UIText("Place items here to convert them to EMC", 0.8f);
            instructionsText.HAlign = 0.5f;
            instructionsText.Top.Set(200f, 0f);
            mainPanel.Append(instructionsText);
        }

        private void BurnButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            BurnCurrentItem();
        }

        private void BurnCurrentItem()
        {
            if (!burnSlot.IsAir && Main.LocalPlayer.TryGetModPlayer(out EMCPlayer emcPlayer))
            {
                // Calculate EMC value of the item
                long emcValue = EMCHelper.ConvertItemToEMC(burnSlot);

                // Add EMC to player's stored EMC
                emcPlayer.AddEMC(emcValue);

                // Show a message to the player
                Main.NewText($"Burned {burnSlot.stack} {burnSlot.Name} for {emcValue} EMC.", Color.Orange);

                // Remove the item
                burnSlot = new Item();

                // Update the burn slot image
                burnSlotPanel.SetImage(TextureAssets.InventoryBack);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Update EMC display
            if (Main.LocalPlayer.TryGetModPlayer(out EMCPlayer emcPlayer))
            {
                emcStorageText.SetText($"EMC: {emcPlayer.storedEMC}");
            }

            // Handle dragging items to the burn slot
            HandleBurnSlot();

            // Show player inventory
            Main.playerInventory = true;
        }

        private void HandleBurnSlot()
        {
            // Get the rectangle for the burn slot in screen coordinates
            var burnSlotRect = new Rectangle(
                (int)(burnSlotPanel.GetDimensions().X),
                (int)(burnSlotPanel.GetDimensions().Y),
                (int)BURN_SLOT_SIZE, 
                (int)BURN_SLOT_SIZE);

            // Check if mouse is hovering the burn slot
            hoveringBurnSlot = burnSlotRect.Contains(Main.mouseX, Main.mouseY);

            // Handle item interactions
            if (hoveringBurnSlot)
            {
                // Handle item dropping into burn slot
                if (Main.mouseItem != null && !Main.mouseItem.IsAir && Main.mouseLeft)
                {
                    // If burn slot is empty, move the item there
                    if (burnSlot.IsAir)
                    {
                        burnSlot = Main.mouseItem.Clone();
                        Main.mouseItem = new Item();
                    }
                    // If burn slot has an item, swap them
                    else
                    {
                        var temp = burnSlot.Clone();
                        burnSlot = Main.mouseItem.Clone();
                        Main.mouseItem = temp;
                    }

                    // Update the burn slot image
                    burnSlotPanel.SetImage(TextureAssets.Item[burnSlot.type]);
                }

                // Handle right-click to retrieve item from burn slot
                if (!burnSlot.IsAir && Main.mouseRight && Main.mouseRightRelease)
                {
                    // Put burn slot item into player's hand if hand is empty
                    if (Main.mouseItem.IsAir)
                    {
                        Main.mouseItem = burnSlot.Clone();
                        burnSlot = new Item();
                    }
                    // Otherwise, try to add to player's inventory
                    else
                    {
                        var item = burnSlot.Clone();
                        burnSlot = new Item();
                        // if (!Main.LocalPlayer.GetItem(Main.myPlayer, item, GetItemSettings.InventoryUIToInventorySettings))
                        // {
                        //     // If inventory is full, put it back in the burn slot
                        //     burnSlot = item;
                        // }
                    }

                    // Update the burn slot image
                    burnSlotPanel.SetImage(TextureAssets.Item[burnSlot.type]);
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            // Draw item name in burn slot only if it is not empty
            if (!burnSlot.IsAir)
            {
                // Draw the item name on the mouse if hovering over the burn slot
                if (burnSlotPanel.ContainsPoint(Main.MouseScreen))
                {
                    Main.hoverItemName = burnSlot.Name;
                }
            }
        }
    }
}