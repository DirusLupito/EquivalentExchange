using EquivalentExchange.Common.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Creative;

namespace EquivalentExchange.Items
{
    public class TransmutationTablet : ModItem
    {
        public override void SetStaticDefaults()
        {   
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.maxStack = 1;
            Item.value = Item.sellPrice(gold: 10);
            Item.rare = ItemRarityID.Orange;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useTurn = true;
            Item.autoReuse = false;
        }

        public override bool CanRightClick() => true;

        public override void RightClick(Player player)
        {
            // Toggle the UI when right-clicked
            EMCUI.TransmutationTabletVisible = !EMCUI.TransmutationTabletVisible;
        }

        public override bool ConsumeItem(Player player)
        {
            return false; // Prevent item consumption on use
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.DirtBlock, 10);
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }
    }
}