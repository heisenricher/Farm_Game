using UnityEngine;

namespace FarmEmpire.Data
{
    public enum TileType
    {
        Start,
        Property,
        Tax,
        Subsidy,
        RegulatoryReview, // Audit/Jail equivalent
        GoToRegulatoryReview,
        MarketFluctuation,
        WeatherImpact,
        Opportunity,
        Neutral
    }

    public abstract class TileData : ScriptableObject
    {
        public string tileName;
        [TextArea]
        public string tileDescription;
        public TileType tileType;
    }
}
