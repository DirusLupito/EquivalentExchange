using EquivalentExchange.UI.States;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace EquivalentExchange.Common.Systems
{
    public class EMCUI : ModSystem
    {
        // UI state instances
        public static TransmutationTabletUIState transmutationTabletUI;
        public static TransmutationTabletItemsUIState transmutationTabletItemsUI;
        
        // UserInterface instances that hold the UI states
        public static UserInterface transmutationTabletInterface;
        public static UserInterface transmutationTabletItemsInterface;
        
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
                    {
                        transmutationTabletInterface?.SetState(transmutationTabletUI);
                        transmutationTabletItemsInterface?.SetState(transmutationTabletItemsUI);
                    }
                    else
                    {
                        transmutationTabletInterface?.SetState(null);
                        transmutationTabletItemsInterface?.SetState(null);
                    }
                }
            }
        }

        public override void Load()
        {
            if (!Main.dedServ)
            {
                // Initialize the main UI
                transmutationTabletUI = new TransmutationTabletUIState();
                transmutationTabletUI.Activate();
                
                // Initialize the items UI (passing the main UI as reference)
                transmutationTabletItemsUI = new TransmutationTabletItemsUIState(transmutationTabletUI);
                transmutationTabletItemsUI.Activate();
                
                // Initialize the UserInterfaces
                transmutationTabletInterface = new UserInterface();
                transmutationTabletItemsInterface = new UserInterface();
                
                // Initially hide the UIs
                TransmutationTabletVisible = false;
            }
        }

        public override void Unload()
        {
            // Clean up references
            transmutationTabletUI = null;
            transmutationTabletItemsUI = null;
            transmutationTabletInterface = null;
            transmutationTabletItemsInterface = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // Update the UIs if they're visible
            if (_transmutationTabletVisible)
            {
                transmutationTabletInterface?.Update(gameTime);
                transmutationTabletItemsInterface?.Update(gameTime);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryIndex != -1)
            {
                // Insert the main UI layer right after the inventory
                layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
                    "EquivalentExchange: Transmutation Tablet UI",
                    delegate {
                        if (TransmutationTabletVisible)
                            transmutationTabletInterface?.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
                
                // Insert the items UI layer above the main UI
                layers.Insert(inventoryIndex + 2, new LegacyGameInterfaceLayer(
                    "EquivalentExchange: Transmutation Tablet Items",
                    delegate {
                        if (TransmutationTabletVisible)
                            transmutationTabletItemsInterface?.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}