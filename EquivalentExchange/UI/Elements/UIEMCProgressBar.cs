using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace EquivalentExchange.UI.Elements
{
    // Vertical progress bar that shows EMC progress toward creating a template item
    public class UIEMCProgressBar : UIElement
    {
        // Progress value from 0.0 to 1.0
        private double _progress;

        // Customizable appearance
        public Color BarColor { get; set; } = new Color(0, 200, 255); // Bright blue default
        public Color BackgroundColor { get; set; } = new Color(50, 50, 50, 200); // Dark gray, semi-transparent
        public Color BorderColor { get; set; } = new Color(100, 100, 100); // Medium gray
        public int BorderWidth { get; set; } = 2;
        
        public UIEMCProgressBar()
        {
            Width.Set(30f, 0f);
            Height.Set(350f, 0f);
            _progress = 0f;
        }
        
        // Set progress with value clamping
        public void SetProgress(double value)
        {
            _progress = Math.Min(Math.Max(value, 0.0), 1.0);
        }
        
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            // Get position and dimensions
            Vector2 position = GetDimensions().Position();
            Vector2 size = new Vector2(GetDimensions().Width, GetDimensions().Height);
            
            // Draw background
            spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y),
                BackgroundColor
            );
            
            // Draw the filled part of the bar (fills from bottom to top)
            if (_progress > 0f)
            {
                int fillHeight = (int)(size.Y * _progress);
                Rectangle fillRect = new Rectangle(
                    (int)position.X + BorderWidth, 
                    (int)position.Y + (int)size.Y - fillHeight - BorderWidth, 
                    (int)size.X - BorderWidth * 2, 
                    fillHeight
                );
                
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, fillRect, BarColor);
            }
            
            // Draw border
            if (BorderWidth > 0)
            {
                // Top border
                spriteBatch.Draw(
                    TextureAssets.MagicPixel.Value,
                    new Rectangle((int)position.X, (int)position.Y, (int)size.X, BorderWidth),
                    BorderColor
                );
                // Bottom border
                spriteBatch.Draw(
                    TextureAssets.MagicPixel.Value,
                    new Rectangle((int)position.X, (int)position.Y + (int)size.Y - BorderWidth, (int)size.X, BorderWidth),
                    BorderColor
                );
                // Left border
                spriteBatch.Draw(
                    TextureAssets.MagicPixel.Value,
                    new Rectangle((int)position.X, (int)position.Y, BorderWidth, (int)size.Y),
                    BorderColor
                );
                // Right border
                spriteBatch.Draw(
                    TextureAssets.MagicPixel.Value,
                    new Rectangle((int)position.X + (int)size.X - BorderWidth, (int)position.Y, BorderWidth, (int)size.Y),
                    BorderColor
                );
            }
        }
        
        // Show tooltip when hovering over the progress bar
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            if (ContainsPoint(Main.MouseScreen))
            {
                if (_progress > 0f)
                {
                    Main.hoverItemName = $"Progress: {(_progress * 100):F1}%";
                }
                else
                {
                    Main.hoverItemName = "Progress: 0%";
                }
            }
        }
    }
}