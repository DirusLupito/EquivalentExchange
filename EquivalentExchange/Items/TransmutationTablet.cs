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
            // Price of its constituent items:
            // Obsidian (4) = 0
            // Stone Block (4) = 0
            // Philosopher's Stone (1) = 1 gold 50 silver
            Item.value = Item.sellPrice(gold: 1, silver: 50);
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
            recipe.AddIngredient(ItemID.Obsidian, 4);
            recipe.AddIngredient(ItemID.StoneBlock, 4);
            recipe.AddIngredient(ItemID.PhilosophersStone, 1);
            recipe.Register();
        }
    }
}