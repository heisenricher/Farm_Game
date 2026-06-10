using System.Collections.Generic;
using UnityEngine;

namespace FarmEmpire.Core
{
    public class Player : MonoBehaviour
    {
        [Header("Identity")]
        public string playerName;
        public int playerID;
        public bool isBot;
        public int tokenIndex;
        public Color playerColor;

        [Header("State")]
        public int cash;
        public int currentPosition;
        public int auditTurnsRemaining; // Audit/Jail turns
        public bool isBankrupt;

        [Header("AI Configuration")]
        public string aiDifficulty = "Normal"; // Easy, Normal, Hard, Expert

        // Runtime list of property instances owned by this player
        private List<string> ownedPropertyNames = new List<string>();

        public void Initialize(int id, string name, int startingCash, bool bot, string difficulty, int token, Color color)
        {
            playerID = id;
            playerName = name;
            cash = startingCash;
            isBot = bot;
            aiDifficulty = difficulty;
            tokenIndex = token;
            playerColor = color;
            currentPosition = 0;
            auditTurnsRemaining = 0;
            isBankrupt = false;
            ownedPropertyNames.Clear();
        }

        public void AddProperty(string tileName)
        {
            if (!ownedPropertyNames.Contains(tileName))
            {
                ownedPropertyNames.Add(tileName);
            }
        }

        public void RemoveProperty(string tileName)
        {
            if (ownedPropertyNames.Contains(tileName))
            {
                ownedPropertyNames.Remove(tileName);
            }
        }

        public List<string> GetOwnedProperties()
        {
            return ownedPropertyNames;
        }

        public bool IsInAuditHold()
        {
            return auditTurnsRemaining > 0;
        }
    }
}
