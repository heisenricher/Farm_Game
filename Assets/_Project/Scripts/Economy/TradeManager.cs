using System;
using System.Collections.Generic;
using UnityEngine;
using FarmEmpire.Core;
using FarmEmpire.Board;
using FarmEmpire.Data;

namespace FarmEmpire.Economy
{
    [System.Serializable]
    public class TradeOffer
    {
        public int senderID;
        public int receiverID;
        public List<string> senderProperties = new List<string>();
        public int senderCashOffer; // Positive if sender pays receiver, negative if sender demands cash from receiver
        public List<string> receiverProperties = new List<string>();
    }

    public class TradeManager : MonoBehaviour
    {
        public static TradeManager Instance { get; private set; }

        public event Action<TradeOffer> OnTradeProposed;
        public event Action<TradeOffer, bool> OnTradeResolved; // Offer, accepted/declined

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

        public bool ValidateOffer(TradeOffer offer)
        {
            if (offer == null) return false;
            if (offer.senderID == offer.receiverID) return false;

            Player sender = GetPlayer(offer.senderID);
            Player receiver = GetPlayer(offer.receiverID);

            if (sender == null || receiver == null || sender.isBankrupt || receiver.isBankrupt) return false;

            // Cash checks
            if (offer.senderCashOffer > 0 && sender.cash < offer.senderCashOffer) return false;
            if (offer.senderCashOffer < 0 && receiver.cash < Mathf.Abs(offer.senderCashOffer)) return false;

            // Properties check: sender properties must be owned by sender, and receiver properties owned by receiver
            // And they must NOT have upgrades. Upgrades must be sold first before trading a property.
            foreach (var propName in offer.senderProperties)
            {
                var state = BoardManager.Instance.GetPropertyState(propName);
                if (state == null || state.ownerID != sender.playerID || state.upgradeLevel > 0) return false;
            }

            foreach (var propName in offer.receiverProperties)
            {
                var state = BoardManager.Instance.GetPropertyState(propName);
                if (state == null || state.ownerID != receiver.playerID || state.upgradeLevel > 0) return false;
            }

            return true;
        }

        public void ProposeTrade(TradeOffer offer)
        {
            if (!ValidateOffer(offer))
            {
                GameManager.Instance.LogGameMessage($"[Trade] Invalid trade proposal between Player {offer.senderID} and Player {offer.receiverID}.");
                return;
            }

            Player sender = GetPlayer(offer.senderID);
            Player receiver = GetPlayer(offer.receiverID);
            GameManager.Instance.LogGameMessage($"[Trade] {sender.playerName} proposed a trade to {receiver.playerName}.");

            OnTradeProposed?.Invoke(offer);

            // If receiver is a bot, let AI evaluate it
            if (receiver.isBot)
            {
                bool accept = AI.AIManager.Instance.EvaluateTradeOffer(receiver, offer);
                ResolveTrade(offer, accept);
            }
        }

        public void ResolveTrade(TradeOffer offer, bool accepted)
        {
            if (!ValidateOffer(offer))
            {
                OnTradeResolved?.Invoke(offer, false);
                return;
            }

            Player sender = GetPlayer(offer.senderID);
            Player receiver = GetPlayer(offer.receiverID);

            if (accepted)
            {
                // Execute cash transfer
                if (offer.senderCashOffer > 0)
                {
                    sender.cash -= offer.senderCashOffer;
                    receiver.cash += offer.senderCashOffer;
                }
                else if (offer.senderCashOffer < 0)
                {
                    int amt = Mathf.Abs(offer.senderCashOffer);
                    sender.cash += amt;
                    receiver.cash -= amt;
                }

                // Transfer sender properties
                foreach (var propName in offer.senderProperties)
                {
                    var state = BoardManager.Instance.GetPropertyState(propName);
                    state.ownerID = receiver.playerID;
                    sender.RemoveProperty(propName);
                    receiver.AddProperty(propName);
                }

                // Transfer receiver properties
                foreach (var propName in offer.receiverProperties)
                {
                    var state = BoardManager.Instance.GetPropertyState(propName);
                    state.ownerID = sender.playerID;
                    receiver.RemoveProperty(propName);
                    sender.AddProperty(propName);
                }

                GameManager.Instance.LogGameMessage($"[Trade] Trade ACCEPTED between {sender.playerName} and {receiver.playerName}!");
            }
            else
            {
                GameManager.Instance.LogGameMessage($"[Trade] Trade DECLINED between {sender.playerName} and {receiver.playerName}.");
            }

            OnTradeResolved?.Invoke(offer, accepted);
        }

        private Player GetPlayer(int id)
        {
            foreach (var p in GameManager.Instance.players)
            {
                if (p.playerID == id) return p;
            }
            return null;
        }
    }
}
