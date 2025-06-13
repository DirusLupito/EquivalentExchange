using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace EquivalentExchange.Common.Players
{
    public class EMCPlayer : ModPlayer
    {
        public long storedEMC = 0;

        // Save EMC when the player is saved
        public override void SaveData(TagCompound tag)
        {
            tag.Add("StoredEMC", storedEMC);
        }

        // Load EMC when the player is loaded
        public override void LoadData(TagCompound tag)
        {
            storedEMC = tag.GetLong("StoredEMC");
        }

        // Add EMC to the player's stored amount
        public void AddEMC(long value)
        {
            storedEMC += value;
        }

        // Remove EMC from the player's stored amount (with check to prevent negative values)
        public bool TryRemoveEMC(long value)
        {
            if (storedEMC >= value)
            {
                storedEMC -= value;
                return true;
            }
            return false;
        }
    }
}