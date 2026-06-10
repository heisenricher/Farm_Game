using UnityEngine;

namespace FarmEmpire.Data
{
    public enum CropSector
    {
        RiceValley,          // Brown equivalent
        WheatPlains,         // Light Blue equivalent
        CornBelt,            // Pink equivalent
        DairyRanch,          // Orange equivalent
        FruitOrchard,        // Red equivalent
        CoffeeHighlands,     // Yellow equivalent
        TeaGardens,          // Green equivalent
        CottonFields,        // Blue equivalent
        AgritechCampus,      // Utilities / special equivalent
        ExportHarbors        // Stations / special equivalent
    }

    [CreateAssetMenu(fileName = "NewPropertyData", menuName = "Farm Empire/Property Data")]
    public class PropertyData : TileData
    {
        public CropSector sector;
        public int buyCost;
        public int baseRent;
        public int upgradeCost;
        public int[] rentTiers = new int[6]; // Index 0: Empty land, 1: Small Farm, 2: Advanced, 3: Processing, 4: Export, 5: HQ
        public int mortgageValue;
        public int unmortgageCost;
        public int sellValue; // Value when selling back to bank

        private void OnValidate()
        {
            tileType = TileType.Property;
            if (rentTiers == null || rentTiers.Length != 6)
            {
                System.Array.Resize(ref rentTiers, 6);
            }
        }
    }
}
