using EquivalentExchange.UI.States;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace EquivalentExchange.Common.Systems
{
    public class EMCUI : ModSystem
    {
        // UI state instance
        public static PhilosophersStoneUIState philosophersStoneUI;
        
        // UserInterface instance that holds the UI state
        public static UserInterface philosophersStoneInterface;
        
        // Flag to track if the UI is visible
        private static bool _philosophersStoneVisible = false;
        
        public static bool PhilosophersStoneVisible
        {
            get => _philosophersStoneVisible;
            set
            {
                if (_philosophersStoneVisible != value)
                {
                    _philosophersStoneVisible = value;
                    
                    if (_philosophersStoneVisible)
                        philosophersStoneInterface?.SetState(philosophersStoneUI);
                    else
                        philosophersStoneInterface?.SetState(null);
                }
            }
        }

        public override void Load()
        {
            if (!Main.dedServ)
            {
                // Initialize the UI
                philosophersStoneUI = new PhilosophersStoneUIState();
                philosophersStoneUI.Activate();
                
                // Initialize the UserInterface
                philosophersStoneInterface = new UserInterface();
                
                // Initially hide the UI
                PhilosophersStoneVisible = false;
            }
        }

        public override void Unload()
        {
            // Clean up references
            philosophersStoneUI = null;
            philosophersStoneInterface = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // Update the UI if it's visible
            if (_philosophersStoneVisible && philosophersStoneInterface != null)
                philosophersStoneInterface.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryIndex != -1)
            {
                // Insert our UI layer right after the inventory
                layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
                    "EquivalentExchange: Philosophers Stone UI",
                    delegate {
                        if (PhilosophersStoneVisible && philosophersStoneInterface != null)
                            philosophersStoneInterface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}