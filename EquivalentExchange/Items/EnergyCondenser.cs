using EquivalentExchange.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Creative;

namespace EquivalentExchange.Items
{
    public class EnergyCondenser : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<EnergyCondenserTile>());
            Item.width = 26;
            Item.height = 22;
            // Diamond = 30 silver * 6 = 180 = 1 Gold 80 silver
            // Ruby = 22 silver 50 copper * 2 =  45 silver
            // Chest = 1 silver * 1 =  1 silver
            // Obsidian and stone = nothing
            // = 1 Gold 80 silver + 45 silver + 1 silver = 2 Gold 26 Silver
            Item.value = Item.sellPrice(gold:  2, silver: 26);
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Diamond, 6);
            recipe.AddIngredient(ItemID.Ruby, 2);
            recipe.AddIngredient(ItemID.Obsidian, 10);
            recipe.AddIngredient(ItemID.StoneBlock, 10);
            recipe.AddIngredient(ItemID.Chest, 1);
            recipe.AddTile(TileID.Anvils);
            recipe.Register();
        }
    }
}
