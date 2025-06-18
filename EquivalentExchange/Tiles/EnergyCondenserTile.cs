using EquivalentExchange.TileEntities;
using EquivalentExchange.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using EquivalentExchange.Common.Systems;

namespace EquivalentExchange.Tiles
{
    public class EnergyCondenserTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            // Properties
            Main.tileSpelunker[Type] = true;
            Main.tileContainer[Type] = true;
            Main.tileShine2[Type] = true;
            Main.tileShine[Type] = 1200;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileOreFinderPriority[Type] = 500;
            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileID.Sets.AvoidedByNPCs[Type] = true;
            TileID.Sets.InteractibleByNPCs[Type] = true;
            TileID.Sets.IsAContainer[Type] = true;
            TileID.Sets.FriendlyFairyCanLureTo[Type] = true;
            TileID.Sets.GeneralPlacementTiles[Type] = false;

            DustType = DustID.Electric;
            AdjTiles = [TileID.Containers];
            VanillaFallbackOnModDeletion = TileID.Containers;

            AddMapEntry(new Color(100, 100, 200), CreateMapEntryName());

            // Placement
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.CoordinateHeights = [16, 18];
            TileObjectData.newTile.HookPostPlaceMyPlayer = ModContent.GetInstance<EnergyCondenserTileEntity>().Generic_HookPostPlaceMyPlayer;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            ModContent.GetInstance<EnergyCondenserTileEntity>().Kill(i, j);
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
        {
            return true;
        }

        public override bool RightClick(int i, int j)
        {
            Player player = Main.LocalPlayer;
            Tile tile = Main.tile[i, j];
            Main.mouseRightRelease = false;
            
            int left = i;
            int top = j;
            if (tile.TileFrameX % 36 != 0)
                left--;
            if (tile.TileFrameY != 0)
                top--;

            player.CloseSign();
            player.SetTalkNPC(-1);
            Main.npcChatCornerItem = 0;
            Main.npcChatText = "";

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // In multiplayer, send request to server
                EMCNetCodeSystem.SendRequestAccess(left, top);
                return true;
            }
            else
            {
                // Single player, just open UI
                if (TileEntity.TryGet(left, top, out EnergyCondenserTileEntity tileEntity))
                {
                    Main.playerInventory = true;
                    tileEntity.RequestAccess(Main.myPlayer); // For consistency
                    if (EMCUI.ToggleEnergyCondenserUI(tileEntity))
                    {
                        SoundEngine.PlaySound(SoundID.MenuOpen);
                    }
                    else
                    {
                        SoundEngine.PlaySound(SoundID.MenuClose);
                    }
                }
            }

            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            Tile tile = Main.tile[i, j];
            
            int left = i;
            int top = j;
            if (tile.TileFrameX % 36 != 0)
                left--;
            if (tile.TileFrameY != 0)
                top--;

            if (TileEntity.TryGet(left, top, out EnergyCondenserTileEntity tileEntity))
            {
                player.cursorItemIconText = "Energy Condenser";
                if (tileEntity.storedEMC > RationalNumber.Zero)
                {
                    player.cursorItemIconText += $"\nStored EMC: {tileEntity.storedEMC}";
                }
            }

            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
        }

        public override void MouseOverFar(int i, int j)
        {
            MouseOver(i, j);
            Player player = Main.LocalPlayer;
            if (player.cursorItemIconText == "")
            {
                player.cursorItemIconEnabled = false;
                player.cursorItemIconID = 0;
            }
        }
    }
}
