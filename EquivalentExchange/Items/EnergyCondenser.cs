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
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.DirtBlock, 1); // Placeholder recipe as requested
            recipe.Register();
        }
    }
}
