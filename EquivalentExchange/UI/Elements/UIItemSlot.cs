using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace EquivalentExchange.UI.Elements
{
    public class UIItemSlot : UIElement
    {
        private readonly Func<Item> _getItem;
        private readonly Action<Item> _setItem;
        private const float DefaultScale = 0.85f;

        public UIItemSlot(Func<Item> getItem, Action<Item> setItem)
        {
            _getItem = getItem;
            _setItem = setItem;
            Width.Set(44f, 0f);
            Height.Set(44f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            // Draw slot background
            CalculatedStyle dimensions = GetDimensions();
            Vector2 center = new Vector2(dimensions.X + dimensions.Width * 0.5f, dimensions.Y + dimensions.Height * 0.5f);
            
            // Draw inventory back texture
            Texture2D slotTexture = TextureAssets.InventoryBack.Value;
            spriteBatch.Draw(slotTexture, center, null, Color.White, 0f, slotTexture.Size() * 0.5f, DefaultScale, SpriteEffects.None, 0f);

            // Draw item if present
            Item item = _getItem();
            if (item != null && !item.IsAir)
            {
                // Draw the item
                Main.DrawItemIcon(
                    spriteBatch,
                    item,
                    center,
                    Color.White,
                    32f
                );
            }

            // Highlight on hover
            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                spriteBatch.Draw(TextureAssets.InventoryBack.Value, center, null, Color.White * 0.4f, 0f, slotTexture.Size() * 0.5f, DefaultScale, SpriteEffects.None, 0f);
                
                // Show tooltip
                Item hoverItem = _getItem();
                if (hoverItem != null && !hoverItem.IsAir)
                {
                    Main.HoverItem = hoverItem.Clone();
                    Main.hoverItemName = hoverItem.Name;
                }
            }
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            _setItem(_getItem());
        }

        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            _setItem(_getItem());
        }
    }
}
