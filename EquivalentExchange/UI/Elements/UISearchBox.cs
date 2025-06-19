using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace EquivalentExchange.UI.Elements
{
    /// <summary>
    /// A text-only search input field that should be placed on top of a background element.
    /// This component only handles text drawing and input processing.
    /// </summary>
    public class UISearchBox : UIElement
    {
        // Text properties
        private string searchText = "";
        private bool focused = false;
        private int cursorPosition = 0;
        private int cursorBlinkTimer = 0;
        private int maxTextLength = 20; // Limit text length to prevent overflow

        // Properties
        public string Text => searchText;
        public bool IsFocused => focused;
        public Color TextColor { get; set; } = Color.White;
        public Color PlaceholderColor { get; set; } = new Color(150, 150, 150);
        public string PlaceholderText { get; set; } = "Search...";
        public int MaxTextLength
        {
            get => maxTextLength;
            set => maxTextLength = Math.Max(1, value);
        }

        // Event for when search text changes
        public event Action<string> OnTextChanged;

        public UISearchBox(float width = 200f, float height = 30f)
        {
            Width.Set(width, 0f);
            Height.Set(height, 0f);
        }

        public void Focus()
        {
            focused = true;
            Main.blockInput = true;
            // Reset cursor blink timer
            cursorBlinkTimer = 0;
            // Place cursor at end of text
            cursorPosition = searchText.Length;
        }

        public void Unfocus()
        {
            focused = false;
            Main.blockInput = false;
        }

        public void SetText(string text)
        {
            // Limit text length to prevent overflow
            string newText = text;
            if (newText.Length > MaxTextLength)
                newText = newText.Substring(0, MaxTextLength);

            if (searchText != newText)
            {
                searchText = newText;
                cursorPosition = Math.Min(cursorPosition, searchText.Length);
                OnTextChanged?.Invoke(searchText);
            }
        }

        public void Clear()
        {
            if (searchText.Length > 0)
            {
                searchText = "";
                cursorPosition = 0;
                OnTextChanged?.Invoke(searchText);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            // Unfocus if the game loses focus
            if (!Main.hasFocus && focused)
            {
                Unfocus();
                return;
            }

            // Update cursor blink timer
            cursorBlinkTimer++;
            if (cursorBlinkTimer > 60)
                cursorBlinkTimer = 0;

            if (focused)
            {
                HandleInput();
            }
        }

        private void HandleInput()
        {
            // Handle keys for search box
            if (Main.keyState.IsKeyDown(Keys.Escape))
            {
                Unfocus();
                return;
            }

            // Get new input
            var keyboardState = Main.keyState;
            var oldKeyboardState = Main.oldKeyState;

            // Process typed characters
            foreach (var key in keyboardState.GetPressedKeys())
            {
                if (!oldKeyboardState.IsKeyDown(key))
                {
                    // Handle backspace
                    if (key == Keys.Back && searchText.Length > 0 && cursorPosition > 0)
                    {
                        searchText = searchText.Remove(cursorPosition - 1, 1);
                        cursorPosition--;
                        OnTextChanged?.Invoke(searchText);
                        continue;
                    }

                    // Handle delete
                    if (key == Keys.Delete &&
                        searchText.Length > 0 &&
                        cursorPosition < searchText.Length)
                    {
                        searchText = searchText.Remove(cursorPosition, 1);
                        OnTextChanged?.Invoke(searchText);
                        continue;
                    }

                    // Handle arrow keys
                    if (key == Keys.Left && cursorPosition > 0)
                    {
                        cursorPosition--;
                        continue;
                    }

                    if (key == Keys.Right && cursorPosition < searchText.Length)
                    {
                        cursorPosition++;
                        continue;
                    }

                    // Handle Enter to unfocus
                    if (key == Keys.Enter)
                    {
                        Unfocus();
                        continue;
                    }

                    // Get text input
                    string keyString = GetKeyString(key);
                    if (!string.IsNullOrEmpty(keyString) && searchText.Length < MaxTextLength)
                    {
                        searchText = searchText.Insert(cursorPosition, keyString);
                        cursorPosition += keyString.Length;
                        OnTextChanged?.Invoke(searchText);
                    }
                }
            }
        }

        // Convert keyboard key to string
        private string GetKeyString(Keys key)
        {
            bool shift = Main.keyState.IsKeyDown(Keys.LeftShift) ||
                        Main.keyState.IsKeyDown(Keys.RightShift);

            // Handle letters
            if (key >= Keys.A && key <= Keys.Z)
            {
                return shift ? key.ToString() : key.ToString().ToLower();
            }

            // Handle numbers
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                if (!shift) return key.ToString().Substring(1);

                // Handle shift + number special characters
                switch (key)
                {
                    case Keys.D0: return ")";
                    case Keys.D1: return "!";
                    case Keys.D2: return "@";
                    case Keys.D3: return "#";
                    case Keys.D4: return "$";
                    case Keys.D5: return "%";
                    case Keys.D6: return "^";
                    case Keys.D7: return "&";
                    case Keys.D8: return "*";
                    case Keys.D9: return "(";
                }
            }

            // Handle numpad
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                return (key - Keys.NumPad0).ToString();
            }

            // Handle special keys
            switch (key)
            {
                case Keys.Space: return " ";
                case Keys.OemMinus: return shift ? "_" : "-";
                case Keys.OemPlus: return shift ? "+" : "=";
                case Keys.OemOpenBrackets: return shift ? "{" : "[";
                case Keys.OemCloseBrackets: return shift ? "}" : "]";
                case Keys.OemPipe: return shift ? "|" : "\\";
                case Keys.OemSemicolon: return shift ? ":" : ";";
                case Keys.OemQuotes: return shift ? "\"" : "'";
                case Keys.OemComma: return shift ? "<" : ",";
                case Keys.OemPeriod: return shift ? ">" : ".";
                case Keys.OemQuestion: return shift ? "?" : "/";
                case Keys.Multiply: return "*";
                case Keys.Add: return "+";
                case Keys.Subtract: return "-";
                case Keys.Decimal: return ".";
                case Keys.Divide: return "/";
            }

            return "";
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            // Calculate position for text
            CalculatedStyle dimensions = GetDimensions();
            Vector2 textPos = new Vector2(dimensions.X + 10, dimensions.Y + 8);

            // Draw the text
            string displayText = string.IsNullOrEmpty(searchText) && !focused
                ? PlaceholderText
                : searchText;

            Color color = string.IsNullOrEmpty(searchText) && !focused
                ? PlaceholderColor
                : TextColor;

            // Draw the text
            Utils.DrawBorderString(spriteBatch, displayText, textPos, color);

            // Draw cursor if focused
            if (focused && cursorBlinkTimer / 30 % 2 == 0)
            {
                // Measure text width up to cursor position
                string textUpToCursor = searchText.Substring(0, cursorPosition);
                Vector2 cursorPos = textPos + FontAssets.MouseText.Value.MeasureString(textUpToCursor);
                // Reset y position to align with text height, but only if text is not empty
                if (displayText.Length > 0)
                    cursorPos.Y = textPos.Y + (FontAssets.MouseText.Value.MeasureString(displayText).Y - 18) / 2;
                else
                    cursorPos.Y = textPos.Y;

                // Draw cursor line
                spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                new Rectangle((int)cursorPos.X, (int)cursorPos.Y, 1, 18),
                Color.White
            );
            }
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            Focus();
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            Main.LocalPlayer.mouseInterface = true;
        }
    }
}
