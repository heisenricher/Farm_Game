using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FarmEmpire.Core;
using FarmEmpire.Board;
using FarmEmpire.Economy;
using FarmEmpire.Data;

namespace FarmEmpire.AI
{
    public class AIManager : MonoBehaviour
    {
        public static AIManager Instance { get; private set; }

        [Header("AI Profiles")]
        public AIProfile easyProfile;
        public AIProfile normalProfile;
        public AIProfile hardProfile;
        public AIProfile expertProfile;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private AIProfile GetProfile(string difficulty)
        {
            switch (difficulty.ToLower())
            {
                case "easy":
                    return easyProfile ?? CreateDefaultProfile("Easy", 50, 0.5f, 0.3f, 0.7f);
                case "hard":
                    return hardProfile ?? CreateDefaultProfile("Hard", 200, 0.95f, 0.85f, 1.1f);
                case "expert":
                    return expertProfile ?? CreateDefaultProfile("Expert", 250, 1.0f, 0.95f, 1.2f);
                case "normal":
                default:
                    return normalProfile ?? CreateDefaultProfile("Normal", 150, 0.85f, 0.65f, 1.0f);
            }
        }

        private AIProfile CreateDefaultProfile(string name, int cashLimit, float buy, float upgrade, float tradeVal)
        {
            var p = ScriptableObject.CreateInstance<AIProfile>();
            p.difficultyName = name;
            p.cashReserveLimit = cashLimit;
            p.buyChance = buy;
            p.upgradeChance = upgrade;
            p.minTradeValuationRatio = tradeVal;
            return p;
        }

        public void ExecuteBotTurn(Player bot)
        {
            StartCoroutine(BotTurnCoroutine(bot));
        }

        private IEnumerator BotTurnCoroutine(Player bot)
        {
            AIProfile profile = GetProfile(bot.aiDifficulty);
            GameManager.Instance.LogGameMessage($"[AI] {bot.playerName} ({profile.difficultyName}) is thinking...");
            yield return new WaitForSeconds(1.0f);

            // 1. Evaluate Property Purchase
            TileData landedTile = BoardManager.Instance.GetTileAt(bot.currentPosition);
            if (landedTile is PropertyData prop)
            {
                PropertyState state = BoardManager.Instance.GetPropertyState(prop.tileName);
                if (state != null && state.ownerID == -1) // Unowned
                {
                    // Evaluate buying
                    bool willBuy = false;
                    int remainingCash = bot.cash - prop.buyCost;
                    
                    if (remainingCash >= profile.cashReserveLimit)
                    {
                        // Check random factor
                        if (Random.value <= profile.buyChance)
                        {
                            willBuy = true;
                        }
                    }
                    else if (profile.difficultyName == "Easy" && bot.cash >= prop.buyCost)
                    {
                        // Easy bot is careless and doesn't maintain cash reserve
                        willBuy = (Random.value < 0.5f);
                    }
                    else if (profile.difficultyName == "Expert")
                    {
                        // Expert might buy even if it breaks reserve if they complete a monopoly
                        bool completesMonopoly = WillCompleteMonopoly(bot, prop);
                        if (completesMonopoly && bot.cash >= prop.buyCost)
                        {
                            willBuy = true;
                        }
                    }

                    if (willBuy)
                    {
                        EconomyManager.Instance.BuyProperty(bot, prop);
                        yield return new WaitForSeconds(1.0f);
                    }
                    else
                    {
                        GameManager.Instance.LogGameMessage($"[AI] {bot.playerName} decided not to buy {prop.tileName}.");
                    }
                }
                else if (state != null && state.ownerID != bot.playerID && state.ownerID != -1 && !state.isMortgaged)
                {
                    // Must pay rent
                    Player landlord = GetPlayerByID(state.ownerID);
                    if (landlord != null)
                    {
                        EconomyManager.Instance.PayRent(bot, landlord, prop);
                        yield return new WaitForSeconds(1.0f);
                    }
                }
            }

            if (bot.isBankrupt) yield break;

            // 2. Evaluate Upgrades
            // Loop through owned properties, check if we can upgrade
            List<string> ownedProps = new List<string>(bot.GetOwnedProperties());
            foreach (var propName in ownedProps)
            {
                if (BoardManager.Instance.TryGetPropertyData(propName, out var pData))
                {
                    while (EconomyManager.Instance.CanUpgradeProperty(bot, pData))
                    {
                        // Check if we stay above reserve
                        int remainingCash = bot.cash - pData.upgradeCost;
                        if (remainingCash >= profile.cashReserveLimit)
                        {
                            if (Random.value <= profile.upgradeChance)
                            {
                                EconomyManager.Instance.UpgradeProperty(bot, pData);
                                yield return new WaitForSeconds(0.8f);
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            // 3. Evaluate Unmortgaging properties
            foreach (var propName in ownedProps)
            {
                var state = BoardManager.Instance.GetPropertyState(propName);
                if (state != null && state.isMortgaged)
                {
                    if (BoardManager.Instance.TryGetPropertyData(propName, out var pData))
                    {
                        if (EconomyManager.Instance.CanUnmortgageProperty(bot, pData))
                        {
                            int remainingCash = bot.cash - pData.unmortgageCost;
                            if (remainingCash >= profile.cashReserveLimit)
                            {
                                EconomyManager.Instance.UnmortgageProperty(bot, pData);
                                yield return new WaitForSeconds(0.8f);
                            }
                        }
                    }
                }
            }

            yield return new WaitForSeconds(0.5f);
            GameManager.Instance.LogGameMessage($"[AI] {bot.playerName} finished decisions.");
            GameManager.Instance.EndTurn();
        }

        public bool EvaluateTradeOffer(Player bot, TradeOffer offer)
        {
            AIProfile profile = GetProfile(bot.aiDifficulty);

            // Simple valuation: sum of purchase costs + upgrades
            int valueGiven = GetOfferAssetValuation(offer.receiverProperties) + (offer.senderCashOffer < 0 ? Mathf.Abs(offer.senderCashOffer) : 0);
            int valueReceived = GetOfferAssetValuation(offer.senderProperties) + (offer.senderCashOffer > 0 ? offer.senderCashOffer : 0);

            // AI evaluates if valueReceived is sufficient compared to valueGiven
            float ratio = (float)valueReceived / (valueGiven == 0 ? 1 : valueGiven);
            
            // Hard/Expert bots might also value monopoly potential
            if (profile.difficultyName == "Hard" || profile.difficultyName == "Expert")
            {
                // check if the incoming properties help complete a sector
                foreach (var propName in offer.senderProperties)
                {
                    if (BoardManager.Instance.TryGetPropertyData(propName, out var pData))
                    {
                        int owned = BoardManager.Instance.GetSectorOwnedCount(pData.sector, bot.playerID);
                        int total = BoardManager.Instance.GetSectorCount(pData.sector);
                        if (owned == total - 1)
                        {
                            // completing monopoly! highly valuable!
                            ratio *= 1.3f;
                        }
                    }
                }
                
                // check if we are giving away properties that break our own monopoly
                foreach (var propName in offer.receiverProperties)
                {
                    if (BoardManager.Instance.TryGetPropertyData(propName, out var pData))
                    {
                        if (BoardManager.Instance.HasSectorMonopoly(pData.sector, bot.playerID))
                        {
                            // breaking our own monopoly! very bad!
                            ratio *= 0.6f;
                        }
                    }
                }
            }

            return ratio >= profile.minTradeValuationRatio;
        }

        private int GetOfferAssetValuation(List<string> properties)
        {
            int sum = 0;
            foreach (var propName in properties)
            {
                if (BoardManager.Instance.TryGetPropertyData(propName, out var pData))
                {
                    var state = BoardManager.Instance.GetPropertyState(propName);
                    sum += pData.buyCost;
                    if (state != null)
                    {
                        sum += state.upgradeLevel * pData.upgradeCost;
                    }
                }
            }
            return sum;
        }

        private bool WillCompleteMonopoly(Player bot, PropertyData prop)
        {
            int owned = BoardManager.Instance.GetSectorOwnedCount(prop.sector, bot.playerID);
            int total = BoardManager.Instance.GetSectorCount(prop.sector);
            return (owned == total - 1);
        }

        private Player GetPlayerByID(int id)
        {
            foreach (var p in GameManager.Instance.players)
            {
                if (p.playerID == id) return p;
            }
            return null;
        }
    }
}
