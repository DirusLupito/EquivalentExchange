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
        public static TransmutationTabletUIState transmutationTabletUI;
        
        // UserInterface instance that holds the UI state
        public static UserInterface transmutationTabletInterface;
        
        // Flag to track if the UI is visible
        private static bool _transmutationTabletVisible = false;
        
        public static bool TransmutationTabletVisible
        {
            get => _transmutationTabletVisible;
            set
            {
                if (_transmutationTabletVisible != value)
                {
                    _transmutationTabletVisible = value;
                    
                    if (_transmutationTabletVisible)
                        transmutationTabletInterface?.SetState(transmutationTabletUI);
                    else
                        transmutationTabletInterface?.SetState(null);
                }
            }
        }

        public override void Load()
        {
            if (!Main.dedServ)
            {
                // Initialize the UI
                transmutationTabletUI = new TransmutationTabletUIState();
                transmutationTabletUI.Activate();
                
                // Initialize the UserInterface
                transmutationTabletInterface = new UserInterface();
                
                // Initially hide the UI
                TransmutationTabletVisible = false;
            }
        }

        public override void Unload()
        {
            // Clean up references
            transmutationTabletUI = null;
            transmutationTabletInterface = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // Update the UI if it's visible
            if (_transmutationTabletVisible && transmutationTabletInterface != null)
                transmutationTabletInterface.Update(gameTime);
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
                        if (TransmutationTabletVisible && transmutationTabletInterface != null)
                            transmutationTabletInterface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}