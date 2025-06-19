using System.IO;
using EquivalentExchange.Common.Systems;
using EquivalentExchange.Common.Utilities;
using EquivalentExchange.TileEntities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace EquivalentExchange
{
	public class EquivalentExchange : Mod
	{
        // Routes packets to their handles or handles them directly
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            EMCNetCodeSystem.MessageType msgType = (EMCNetCodeSystem.MessageType)reader.ReadByte();
            
            switch (msgType)
            {
                case EMCNetCodeSystem.MessageType.RequestCondenserAccess:
                    HandleRequestAccess(reader, whoAmI);
                    break;

                case EMCNetCodeSystem.MessageType.ReleaseCondenserAccess:
                    HandleReleaseAccess(reader, whoAmI);
                    break;

                case EMCNetCodeSystem.MessageType.ModifyCondenserInventory:
                    HandleModifyInventory(reader, whoAmI);
                    break;

                case EMCNetCodeSystem.MessageType.ModifyCondenserTemplate:
                    HandleModifyTemplate(reader, whoAmI);
                    break;

                case EMCNetCodeSystem.MessageType.RequestCondenserSync:
                    int tileX = reader.ReadInt32();
                    int tileY = reader.ReadInt32();

                    // On server, handle sync request
                    if (Main.netMode == NetmodeID.Server)
                    {
                        if (TileEntity.TryGet(tileX, tileY, out EnergyCondenserTileEntity tileEntity))
                        {
                            // Force an immediate sync of this condenser to the requesting client
                            NetMessage.SendData(MessageID.TileEntitySharing, whoAmI, -1, null, tileEntity.ID, tileX, tileY);
                        }
                    }
                    break;
            }
        }

        private void HandleRequestAccess(BinaryReader reader, int whoAmI)
        {
            int i = reader.ReadInt32();
            int j = reader.ReadInt32();
            
            if (Main.netMode == NetmodeID.Server)
            {
                // On server, handle request and notify clients
                if (TileEntity.TryGet(i, j, out EnergyCondenserTileEntity tileEntity))
                {
                    bool success = tileEntity.RequestAccess(whoAmI);
                    
                    // Let the requesting client know the result
                    ModPacket packet = GetPacket();
                    packet.Write((byte)EMCNetCodeSystem.MessageType.RequestCondenserAccess);
                    packet.Write(i);
                    packet.Write(j);
                    packet.Write(success);
                    packet.Send(whoAmI);
                    
                    if (success)
                    {
                        // Sync TileEntity to all clients
                        NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, tileEntity.ID, i, j);
                    }
                }
            }
            else
            {
                // On client, receive result from server
                bool success = reader.ReadBoolean();
                if (success)
                {
                    // Open UI
                    if (TileEntity.TryGet(i, j, out EnergyCondenserTileEntity tileEntity))
                    {
                        Main.playerInventory = true;
                        EMCUI.ToggleEnergyCondenserUI(tileEntity);
                    }
                }
                else
                {
                    // Show message that condenser is in use
                    Main.NewText("This Energy Condenser is currently being used by another player.", Color.Red);
                }
            }
        }

        private void HandleReleaseAccess(BinaryReader reader, int whoAmI)
        {
            int i = reader.ReadInt32();
            int j = reader.ReadInt32();
            
            if (Main.netMode == NetmodeID.Server)
            {
                // On server, release access and notify clients
                if (TileEntity.TryGet(i, j, out EnergyCondenserTileEntity tileEntity))
                {
                    tileEntity.ReleaseAccess(whoAmI);
                    NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, tileEntity.ID, i, j);
                }
            }
        }

        private void HandleModifyInventory(BinaryReader reader, int whoAmI)
        {
            int i = reader.ReadInt32();
            int j = reader.ReadInt32();
            int slotIndex = reader.ReadInt32();
            Item item = ItemIO.Receive(reader, readStack: true, readFavorite: false);
            
            if (Main.netMode == NetmodeID.Server)
            {
                // On server, update inventory if player is allowed
                if (TileEntity.TryGet(i, j, out EnergyCondenserTileEntity tileEntity))
                {
                    if (tileEntity.TryPlaceItem(slotIndex, item, whoAmI))
                    {
                        // Sync TileEntity to all clients
                        NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, tileEntity.ID, i, j);
                    }
                }
            }
        }

        private void HandleModifyTemplate(BinaryReader reader, int whoAmI)
        {
            int i = reader.ReadInt32();
            int j = reader.ReadInt32();
            Item item = ItemIO.Receive(reader, readStack: true, readFavorite: false);
            
            if (Main.netMode == NetmodeID.Server)
            {
                // On server, update template if player is allowed
                if (TileEntity.TryGet(i, j, out EnergyCondenserTileEntity tileEntity))
                {
                    if (tileEntity.CurrentUser == whoAmI)
                    {
                        // Only allow current user to modify template
                        if (item.IsAir)
                        {
                            tileEntity.templateItem = new Item();
                        }
                        else
                        {
                            RationalNumber emc = EMCHelper.GetEMC(item);
                            if (emc > RationalNumber.Zero)
                            {
                                tileEntity.templateItem = item.Clone();
                                tileEntity.templateItem.stack = 1;
                            }
                        }
                        
                        // Sync TileEntity to all clients
                        NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, tileEntity.ID, i, j);
                    }
                }
            }
        }
    }
}
