using System.Collections.Generic;

namespace FarmEmpire.SaveSystem
{
    [System.Serializable]
    public class PlayerSaveData
    {
        public string playerName;
        public int playerID;
        public bool isBot;
        public int tokenIndex;
        public string colorHex;
        public int cash;
        public int currentPosition;
        public int auditTurnsRemaining;
        public bool isBankrupt;
        public string aiDifficulty;
        public List<string> ownedProperties = new List<string>();
    }

    [System.Serializable]
    public class PropertySaveData
    {
        public string propertyName;
        public int ownerID;
        public int upgradeLevel;
        public bool isMortgaged;
    }

    [System.Serializable]
    public class GameSaveData
    {
        public string saveName;
        public string saveDate;
        public int activePlayerIndex;
        public int targetNetWorth;
        public int startingCash;
        public bool turnTimerEnabled;
        public float turnTimerDuration;
        public int gameStateValue; // Cast of GameState enum

        public List<PlayerSaveData> players = new List<PlayerSaveData>();
        public List<PropertySaveData> properties = new List<PropertySaveData>();
        public List<string> gameLogs = new List<string>();
    }
}
