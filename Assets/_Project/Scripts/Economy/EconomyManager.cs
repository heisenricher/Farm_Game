using UnityEngine;
using FarmEmpire.Core;
using FarmEmpire.Board;
using FarmEmpire.Data;

namespace FarmEmpire.Economy
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

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

        #region Purchase & Sales
        
        public bool CanBuyProperty(Player player, PropertyData property)
        {
            if (player == null || property == null) return false;
            
            PropertyState state = BoardManager.Instance.GetPropertyState(property.tileName);
            if (state == null) return false;

            // Must be unowned and player has enough cash
            return state.ownerID == -1 && player.cash >= property.buyCost;
        }

        public void BuyProperty(Player player, PropertyData property)
        {
            if (!CanBuyProperty(player, property))
            {
                GameManager.Instance.LogGameMessage($"[Error] {player.playerName} cannot purchase {property.tileName}.");
                return;
            }

            PropertyState state = BoardManager.Instance.GetPropertyState(property.tileName);
            player.cash -= property.buyCost;
            state.ownerID = player.playerID;
            player.AddProperty(property.tileName);

            GameManager.Instance.LogGameMessage($"{player.playerName} purchased {property.tileName} (Sector: {property.sector}) for ${property.buyCost}.");
        }

        public bool CanSellProperty(Player player, PropertyData property)
        {
            if (player == null || property == null) return false;

            PropertyState state = BoardManager.Instance.GetPropertyState(property.tileName);
            if (state == null) return false;

            // Player must own it, and it must not have upgrades built on it (sell upgrades first)
            return state.ownerID == player.playerID && state.upgradeLevel == 0;
        }

        public void SellProperty(Player player, PropertyData property)
        {
            if (!CanSellProperty(player, property))
            {
                GameManager.Instance.LogGameMessage($"[Error] {player.playerName} cannot sell {property.tileName}.");
                return;
            }

            PropertyState state = BoardManager.Instance.GetPropertyState(property.tileName);
            int refund = property.sellValue;
            if (state.isMortgaged)
            {
                // If mortgaged, the refund is lower or half of unmortgaged value? Let's say half of sellValue
                refund = property.sellValue / 2;
            }

            player.cash += refund;
            state.ownerID = -1;
            state.upgradeLevel = 0;
            state.isMortgaged = false;
            player.RemoveProperty(property.tileName);

            GameManager.Instance.LogGameMessage($"{player.playerName} sold {property.tileName} back to bank for ${refund}.");
        }

        #endregion

        #region Rent

        public int CalculateRent(PropertyData property)
        {
            PropertyState state = BoardManager.Instance.GetPropertyState(property.tileName);
            if (state == null || state.isMortgaged || state.ownerID == -1) return 0;

            // If there are upgrades, rent depends on level
            if (state.upgradeLevel > 0)
            {
                return property.rentTiers[state.upgradeLevel];
            }

            // If monopoly exists in the sector, base rent is doubled (standard property game rule)
            bool hasMonopoly = BoardManager.Instance.HasSectorMonopoly(property.sector, state.ownerID);
            if (hasMonopoly)
            {
                return property.rentTiers[0] * 2;
            }

            return property.rentTiers[0];
        }

        public void PayRent(Player tenant, Player landlord, PropertyData property)
        {
            if (tenant == null || landlord == null || property == null) return;

            PropertyState state = BoardManager.Instance.GetPropertyState(property.tileName);
            if (state == null || state.isMortgaged || state.ownerID != landlord.playerID) return;

            int rentAmount = CalculateRent(property);
            if (rentAmount <= 0) return;

            GameManager.Instance.LogGameMessage($"{tenant.playerName} landed on {property.tileName} owned by {landlord.playerName} and must pay rent of ${rentAmount}.");

            tenant.cash -= rentAmount;
            landlord.cash += rentAmount;

            GameManager.Instance.LogGameMessage($"{tenant.playerName} paid ${rentAmount} rent to {landlord.playerName}.");

            if (tenant.cash < 0)
            {
                GameManager.Instance.LogGameMessage($"[ALERT] {tenant.playerName} has negative cash after paying rent! Entering asset recovery.");
                GameManager.Instance.ResolveNegativeCash(tenant);
            }
        }

        #endregion

        #region Upgrades

        public bool CanUpgradeProperty(Player player, PropertyData property)
        {
            if (player == null || property == null) return false;

            PropertyState state = BoardManager.Instance.GetPropertyState(property.tileName);
            if (state == null) return false;

            // Rules:
            // 1. Must own it
            // 2. Must not be mortgaged
            // 3. Must have a monopoly in the sector
            // 4. Upgrade level must be < 5
            // 5. Must have enough cash
            // 6. Build evenly: cannot upgrade this if another property in the sector is more than 1 level behind.
            if (state.ownerID != player.playerID) return false;
            if (state.isMortgaged) return false;
            if (state.upgradeLevel >= 5) return false;
            if (player.cash < property.upgradeCost) return false;

            if (!BoardManager.Instance.HasSectorMonopoly(property.sector, player.playerID)) return false;

            // Check even build rule
            foreach (var tile in BoardManager.Instance.GetTiles())
            {
                if (tile is PropertyData sibling && sibling.sector == property.sector && sibling.tileName != property.tileName)
                {
                    PropertyState sibState = BoardManager.Instance.GetPropertyState(sibling.tileName);
                    if (sibState != null)
                    {
                        if (sibState.isMortgaged) return false;
                        // If sibling upgrade level is less than this property's level, we cannot upgrade further.
                        if (sibState.upgradeLevel < state.upgradeLevel)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public void UpgradeProperty(Player player, PropertyData property)
        {
            if (!CanUpgradeProperty(player, property))
            {
                GameManager.Instance.LogGameMessage($"[Error] {player.playerName} cannot upgrade {property.tileName}.");
                return;
            }

            PropertyState state = BoardManager.Instance.GetPropertyState(property.tileName);
            player.cash -= property.upgradeCost;
            state.upgradeLevel++;

            GameManager.Instance.LogGameMessage($"{player.playerName} upgraded {property.tileName} to Level {state.upgradeLevel} for ${property.upgradeCost}.");
        }

        public bool CanDowngradeProperty(Player player, PropertyData property)
        {
            if (player == null || property == null) return false;

            PropertyState state = BoardManager.Instance.GetPropertyState(property.tileName);
            if (state == null) return false;

            // Must own and have upgrades
            if (state.ownerID != player.playerID || state.upgradeLevel == 0) return false;

            // Downbuild evenly: cannot downgrade if sibling upgrade level is greater than this property's level.
            foreach (var tile in BoardManager.Instance.GetTiles())
            {
                if (tile is PropertyData sibling && sibling.sector == property.sector && sibling.tileName != property.tileName)
                {
                    PropertyState sibState = BoardManager.Instance.GetPropertyState(sibling.tileName);
                    if (sibState != null && sibState.upgradeLevel > state.upgradeLevel)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void DowngradeProperty(Player player, PropertyData property)
        {
            if (!CanDowngradeProperty(player, property))
            {
                GameManager.Instance.LogGameMessage($"[Error] {player.playerName} cannot sell upgrades on {property.tileName}.");
                return;
            }

            PropertyState state = BoardManager.Instance.GetPropertyState(property.tileName);
            int refund = property.upgradeCost / 2;
            state.upgradeLevel--;
            player.cash += refund;

            GameManager.Instance.LogGameMessage($"{player.playerName} sold upgrade on {property.tileName} for ${refund}. Cash: ${player.cash}");
        }

        #endregion

        #region Mortgages

        public bool CanMortgageProperty(Player player, PropertyData property)
        {
            if (player == null || property == null) return false;

            PropertyState state = BoardManager.Instance.GetPropertyState(property.tileName);
            if (state == null) return false;

            // Rules:
            // 1. Must own it
            // 2. Must not be mortgaged
            // 3. All properties in this sector must have no upgrades (must sell upgrades first)
            if (state.ownerID != player.playerID || state.isMortgaged) return false;

            // Check if any property in sector has upgrades
            foreach (var tile in BoardManager.Instance.GetTiles())
            {
                if (tile is PropertyData sibling && sibling.sector == property.sector)
                {
                    PropertyState sibState = BoardManager.Instance.GetPropertyState(sibling.tileName);
                    if (sibState != null && sibState.upgradeLevel > 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void MortgageProperty(Player player, PropertyData property)
        {
            if (!CanMortgageProperty(player, property))
            {
                GameManager.Instance.LogGameMessage($"[Error] {player.playerName} cannot mortgage {property.tileName}.");
                return;
            }

            PropertyState state = BoardManager.Instance.GetPropertyState(property.tileName);
            state.isMortgaged = true;
            player.cash += property.mortgageValue;

            GameManager.Instance.LogGameMessage($"{player.playerName} mortgaged {property.tileName} for ${property.mortgageValue}. Cash: ${player.cash}");
        }

        public bool CanUnmortgageProperty(Player player, PropertyData property)
        {
            if (player == null || property == null) return false;

            PropertyState state = BoardManager.Instance.GetPropertyState(property.tileName);
            if (state == null) return false;

            // Must own, be mortgaged, and have cash
            return state.ownerID == player.playerID && state.isMortgaged && player.cash >= property.unmortgageCost;
        }

        public void UnmortgageProperty(Player player, PropertyData property)
        {
            if (!CanUnmortgageProperty(player, property))
            {
                GameManager.Instance.LogGameMessage($"[Error] {player.playerName} cannot unmortgage {property.tileName}.");
                return;
            }

            PropertyState state = BoardManager.Instance.GetPropertyState(property.tileName);
            player.cash -= property.unmortgageCost;
            state.isMortgaged = false;

            GameManager.Instance.LogGameMessage($"{player.playerName} unmortgaged {property.tileName} for ${property.unmortgageCost}. Cash: ${player.cash}");
        }

        #endregion
    }
}
