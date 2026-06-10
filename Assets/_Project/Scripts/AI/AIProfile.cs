using UnityEngine;

namespace FarmEmpire.AI
{
    [CreateAssetMenu(fileName = "NewAIProfile", menuName = "Farm Empire/AI Profile")]
    public class AIProfile : ScriptableObject
    {
        public string difficultyName;
        
        [Tooltip("Minimum cash buffer to keep before buying properties or upgrades.")]
        public int cashReserveLimit = 150;
        
        [Tooltip("Percent chance to buy an unowned property when landed on it.")]
        [Range(0f, 1f)] public float buyChance = 0.9f;
        
        [Tooltip("Percent chance to upgrade an eligible property during decisions phase.")]
        [Range(0f, 1f)] public float upgradeChance = 0.7f;
        
        [Tooltip("Minimum return on investment ratio required to accept a trade offer.")]
        public float minTradeValuationRatio = 1.0f;
    }
}
