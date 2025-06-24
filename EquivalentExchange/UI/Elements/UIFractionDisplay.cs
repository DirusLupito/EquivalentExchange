using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using EquivalentExchange.Common.Utilities;

namespace EquivalentExchange.UI.Elements
{
    // UI element that displays a fraction (numerator over denominator with a line between)
    public class UIFractionDisplay : UIElement
    {
        private string _numerator = "1";
        private string _denominator = "1";
        private float _scale = 1f;
        private Color _textColor = Color.White;
        private Color _lineColor = Color.White;
        private bool _showLabel = true;
        private string _label = "EMC:";
        private int _lineThickness = 1;
        private float _verticalSpacing = 2f;

        // Public properties for configuration
        public Color TextColor
        {
            get => _textColor;
            set => _textColor = value;
        }

        public Color LineColor
        {
            get => _lineColor;
            set => _lineColor = value;
        }

        public float Scale
        {
            get => _scale;
            set => _scale = value;
        }

        public bool ShowLabel
        {
            get => _showLabel;
            set => _showLabel = value;
        }

        public string Label
        {
            get => _label;
            set => _label = value;
        }

        public int LineThickness
        {
            get => _lineThickness;
            set => _lineThickness = Math.Max(1, value);
        }

        public float VerticalSpacing
        {
            get => _verticalSpacing;
            set => _verticalSpacing = value;
        }

        public UIFractionDisplay()
        {
            Width.Set(100f, 0f);
            Height.Set(60f, 0f);
        }

        // Set the fraction from strings
        public void SetFraction(string numerator, string denominator)
        {
            _numerator = numerator;
            _denominator = denominator;
        }

        // Set the fraction from integers
        public void SetFraction(int numerator, int denominator)
        {
            _numerator = numerator.ToString();
            _denominator = denominator.ToString();
        }

        // Set the fraction from a RationalNumber
        public void SetFraction(RationalNumber rational)
        {
            _numerator = rational.Numerator.ToString();
            _denominator = rational.Denominator.ToString();
            
            // If denominator is 1, just show the numerator
            if (_denominator == "1")
            {
                _denominator = "";
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            // Get dimensions of the element
            CalculatedStyle dimensions = GetDimensions();
            
            // Calculate text sizes
            Vector2 numSize = FontAssets.MouseText.Value.MeasureString(_numerator) * _scale;
            Vector2 denomSize = FontAssets.MouseText.Value.MeasureString(_denominator) * _scale;
            Vector2 labelSize = _showLabel ? FontAssets.MouseText.Value.MeasureString(_label) * _scale : Vector2.Zero;
            
            // Determine the line width based on the wider of numerator or denominator
            float lineWidth = Math.Max(numSize.X, denomSize.X);
            
            // If denominator is empty, don't show the line
            bool showLine = !string.IsNullOrEmpty(_denominator);
            
            // Calculate positions
            float startX = dimensions.X;
            if (_showLabel)
                startX += labelSize.X + 10; // Add spacing after the label
                
            float centerX = startX + lineWidth / 2;
            float centerY = dimensions.Y + dimensions.Height / 2;
            
            // Draw label if enabled
            if (_showLabel)
            {
                Utils.DrawBorderString(
                    spriteBatch,
                    _label,
                    new Vector2(dimensions.X, centerY - labelSize.Y / 2),
                    _textColor,
                    _scale
                );
            }
            
            // If denominator is empty, just draw the numerator centered
            if (string.IsNullOrEmpty(_denominator))
            {
                Utils.DrawBorderString(
                    spriteBatch,
                    _numerator,
                    new Vector2(centerX - numSize.X / 2, centerY - numSize.Y / 2),
                    _textColor,
                    _scale
                );
                return;
            }
            
            // Draw numerator
            Utils.DrawBorderString(
                spriteBatch,
                _numerator,
                new Vector2(centerX - numSize.X / 2, centerY - numSize.Y - _verticalSpacing),
                _textColor,
                _scale
            );
            
            // Draw the fraction line
            if (showLine)
            {
                spriteBatch.Draw(
                    TextureAssets.MagicPixel.Value,
                    new Rectangle(
                        (int)startX, 
                        (int)centerY, 
                        (int)lineWidth, 
                        _lineThickness),
                    _lineColor
                );
            }
            
            // Draw denominator
            Utils.DrawBorderString(
                spriteBatch,
                _denominator,
                new Vector2(centerX - denomSize.X / 2, centerY + _verticalSpacing),
                _textColor,
                _scale
            );
        }
    }
}