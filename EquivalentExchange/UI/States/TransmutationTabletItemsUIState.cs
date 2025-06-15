using EquivalentExchange.Common.GlobalItems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace EquivalentExchange.UI.States
{
    // This UI state is responsible for drawing items above the main UI panel
    public class TransmutationTabletItemsUIState : UIState
    {
        // Reference to the main UI state to access transmutation items
        private TransmutationTabletUIState mainUIState;

        public TransmutationTabletItemsUIState(TransmutationTabletUIState mainUIState)
        {
            this.mainUIState = mainUIState;
        }

        // We override Draw instead of DrawSelf to have complete control over drawing
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            // Don't draw if main UI isn't initialized
            if (mainUIState == null) {
                return;
            }


            // Draw the items in the transmutation slots
            for (int i = 0; i < mainUIState.TransmutationItems.Length; i++)
            {
                Item item = mainUIState.TransmutationItems[i];
                if (item == null || item.IsAir)
                    continue;

                // Get the position relative to each transmutation slot of each item
                Vector2 position = mainUIState.GetTransmutationSlotCenter(i);

                // Figure out where the UI is on the screen based off of the HAlign and VAlign and the dimensions of the screen and the panel dimensions
                // Then adjust the position to be in the center of the slot rather than the top-left corner of the slot
                position.X += Main.screenWidth * mainUIState.mainPanel.HAlign - mainUIState.mainPanel.Width.Pixels * mainUIState.mainPanel.HAlign + mainUIState.SLOT_WIDTH / 5;
                position.Y += Main.screenHeight * mainUIState.mainPanel.VAlign - mainUIState.mainPanel.Height.Pixels * mainUIState.mainPanel.VAlign + mainUIState.SLOT_HEIGHT / 5;

                // Draw the item at the position
                Main.instance.LoadItem(item.type);
                Main.DrawItemIcon(
                    spriteBatch,
                    item,
                    position,
                    Color.White,
                    32f
                );
            }

            // Draw hover text if needed
            if (mainUIState.HoveringTransmutationSlot >= 0)
            {
                Item item = mainUIState.TransmutationItems[mainUIState.HoveringTransmutationSlot];
                if (!item.IsAir)
                {
                    long emcCost = item.GetGlobalItem<EMCGlobalItem>().emc;
                    Main.hoverItemName = $"{item.Name} ({emcCost} EMC)";
                }
            }
        }
    }
}