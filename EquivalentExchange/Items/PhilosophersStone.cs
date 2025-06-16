using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Creative;

namespace EquivalentExchange.Items
{
    public class PhilosophersStone : ModItem
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
            // Price of its constituent items:
            // Diamond (1) = 30 silver
            // Ruby (4) = 22 silver 50 copper * 4 = 90 silver
            // Topaz (4) = 7 silver 50 copper * 4 = 30 silver
            // Total = 30 + 90 + 30 = 150 silver = 1 gold 50 silver
            Item.value = Item.sellPrice(gold: 1, silver: 50);
            Item.rare = ItemRarityID.Orange;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useTurn = true;
            Item.autoReuse = false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Diamond, 1);
            recipe.AddIngredient(ItemID.Ruby, 4);
            recipe.AddIngredient(ItemID.Topaz, 4);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}