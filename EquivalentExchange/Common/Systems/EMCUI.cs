using EquivalentExchange.UI.States;
using EquivalentExchange.TileEntities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using EquivalentExchange.Common.Players;

namespace EquivalentExchange.Common.Systems
{
    public class EMCUI : ModSystem
    {
        // Transmutation Tablet UI
        public static TransmutationTabletUIState transmutationTabletUI;
        public static UserInterface transmutationTabletInterface;
        private static bool _transmutationTabletVisible = false;

        // Energy Condenser UI
        public static EnergyCondenserUIState energyCondenserUI;
        public static UserInterface energyCondenserInterface;
        private static bool _energyCondenserVisible = false;
        private static EnergyCondenserTileEntity _currentTileEntity = null;

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

        public static bool EnergyCondenserVisible
        {
            get => _energyCondenserVisible;
            set
            {
                if (_energyCondenserVisible != value)
                {
                    _energyCondenserVisible = value;

                    if (_energyCondenserVisible)
                        energyCondenserInterface?.SetState(energyCondenserUI);
                    else
                        energyCondenserInterface?.SetState(null);
                }
            }
        }

        public override void Load()
        {
            if (!Main.dedServ)
            {
                // Initialize the Transmutation Tablet UI
                transmutationTabletUI = new TransmutationTabletUIState();
                transmutationTabletUI.Activate();
                transmutationTabletInterface = new UserInterface();
                TransmutationTabletVisible = false;

                // Initialize the Energy Condenser UI
                energyCondenserUI = new EnergyCondenserUIState();
                energyCondenserUI.Activate();
                energyCondenserInterface = new UserInterface();
                EnergyCondenserVisible = false;
            }
        }

        public override void Unload()
        {
            // Clean up references
            transmutationTabletUI = null;
            transmutationTabletInterface = null;
            energyCondenserUI = null;
            energyCondenserInterface = null;
            _currentTileEntity = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // Update the UIs if they're visible
            if (_transmutationTabletVisible && transmutationTabletInterface != null)
                transmutationTabletInterface.Update(gameTime);

            if (_energyCondenserVisible && energyCondenserInterface != null)
                energyCondenserInterface.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryIndex != -1)
            {
                // Insert transmutation tablet UI layer
                layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
                    "EquivalentExchange: Philosophers Stone UI",
                    delegate
                    {
                        if (TransmutationTabletVisible && transmutationTabletInterface != null)
                            transmutationTabletInterface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                // Insert energy condenser UI layer
                layers.Insert(inventoryIndex + 2, new LegacyGameInterfaceLayer(
                    "EquivalentExchange: Energy Condenser UI",
                    delegate
                    {
                        if (EnergyCondenserVisible && energyCondenserInterface != null)
                            energyCondenserInterface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

        // Handles opening and closing the Energy Condenser UI
        // Returns true if the UI was opened from the closed state
        // Returns false if the UI was closed from the open state
        public static bool ToggleEnergyCondenserUI(EnergyCondenserTileEntity tileEntity)
        {
            if (_currentTileEntity == tileEntity)
            {
                // If the UI is already open for this tile entity, close it
                CloseEnergyCondenserUI();
                return false;
            }
            else
            {
                // If the player already has a different condenser open, the EMCPlayer.SetCurrentCondenser
                // method will handle releasing it

                // Update player's current condenser tracking
                if (Main.LocalPlayer.TryGetModPlayer(out EMCPlayer emcPlayer))
                {
                    emcPlayer.SetCurrentCondenser(tileEntity.Position.X, tileEntity.Position.Y);
                }

                // Set the new tile entity and open UI
                _currentTileEntity = tileEntity;
                if (energyCondenserUI != null)
                {
                    energyCondenserUI.SetTileEntity(tileEntity);
                    EnergyCondenserVisible = true;
                    return true;
                }
            }
            return false;
        }

        public static void CloseEnergyCondenserUI()
        {
            if (_currentTileEntity != null)
            {
                // Update player's current condenser tracking
                if (Main.LocalPlayer.TryGetModPlayer(out EMCPlayer emcPlayer))
                {
                    emcPlayer.ReleaseCurrentCondenser();
                }
            }

            EnergyCondenserVisible = false;
            _currentTileEntity = null;
        }

        public static EnergyCondenserTileEntity GetCurrentEnergyCondenserTileEntity()
        {
            return _currentTileEntity;
        }
    }
}
