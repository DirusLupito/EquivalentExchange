using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace EquivalentExchange.UI.Elements
{
    // A UIElement that displays a Terraria item using Main.DrawItemIcon 
    // to preserve the vanilla item icon drawing logic
    public class UIItemImage : UIElement
    {
        // The item to display
        public Item Item { get; set; } = new Item();

        // Constructor for UIItemImage without parameters
        public UIItemImage()
        {
        }

        // Creates a new UIItemImage displaying the specified item
        public UIItemImage(Item item)
        {
            Item = item;
        }

        // Overrides the DrawSelf method of a UIElement to draw the item
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            // Don't draw if item is empty
            if (Item == null || Item.IsAir)
                return;

            // Make sure the item texture is loaded
            Main.instance.LoadItem(Item.type);

            // Calculate the center position for the item
            Vector2 position = new Vector2(
                GetDimensions().X + GetDimensions().Width / 2,
                GetDimensions().Y + GetDimensions().Height / 2
            );

            // Draw the item
            Main.DrawItemIcon(
                spriteBatch,
                Item,
                position,
                Color.White,
                32f
            );
        }
    }
}