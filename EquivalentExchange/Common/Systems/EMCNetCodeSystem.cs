using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace EquivalentExchange.Common.Systems
{
    public class EMCNetCodeSystem : ModSystem
    {
        public enum MessageType : byte
        {
            RequestCondenserAccess,
            ReleaseCondenserAccess,
            ModifyCondenserInventory,
            ModifyCondenserTemplate,
            RequestCondenserSync,
        }
        
        public static void SendRequestAccess(int i, int j)
        {
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                ModPacket packet = ModContent.GetInstance<EquivalentExchange>().GetPacket();
                packet.Write((byte)MessageType.RequestCondenserAccess);
                packet.Write(i);
                packet.Write(j);
                packet.Send();
            }
        }
        
        public static void SendReleaseAccess(int i, int j)
        {
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                ModPacket packet = ModContent.GetInstance<EquivalentExchange>().GetPacket();
                packet.Write((byte)MessageType.ReleaseCondenserAccess);
                packet.Write(i);
                packet.Write(j);
                packet.Send();
            }
        }
        
        public static void SendModifyInventory(int i, int j, int slotIndex, Item item)
        {
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                ModPacket packet = ModContent.GetInstance<EquivalentExchange>().GetPacket();
                packet.Write((byte)MessageType.ModifyCondenserInventory);
                packet.Write(i);
                packet.Write(j);
                packet.Write(slotIndex);
                ItemIO.Send(item, packet, writeStack: true, writeFavorite: false);
                packet.Send();
            }
        }
        
        public static void SendModifyTemplate(int i, int j, Item item)
        {
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                ModPacket packet = ModContent.GetInstance<EquivalentExchange>().GetPacket();
                packet.Write((byte)MessageType.ModifyCondenserTemplate);
                packet.Write(i);
                packet.Write(j);
                ItemIO.Send(item, packet, writeStack: true, writeFavorite: false);
                packet.Send();
            }
        }

        public static void RequestCondenserSync(int i, int j)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = ModContent.GetInstance<EquivalentExchange>().GetPacket();
                packet.Write((byte)MessageType.RequestCondenserSync);
                packet.Write(i);
                packet.Write(j);
                packet.Send();
            }
        }
    }
}