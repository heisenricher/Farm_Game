using UnityEngine;

namespace FarmEmpire.Data
{
    [CreateAssetMenu(fileName = "NewEventTileData", menuName = "Farm Empire/Event Tile Data")]
    public class EventTileData : TileData
    {
        public int fixedValue; // Tax or Subsidy value
    }
}
