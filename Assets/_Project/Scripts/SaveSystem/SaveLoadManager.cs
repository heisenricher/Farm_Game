using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using FarmEmpire.Core;
using FarmEmpire.Board;

namespace FarmEmpire.SaveSystem
{
    public class SaveLoadManager : MonoBehaviour
    {
        public static SaveLoadManager Instance { get; private set; }

        private List<string> activeLogs = new List<string>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Track game logs
                GameManager.Instance.OnGameLogAdded += LogTracker;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameLogAdded -= LogTracker;
            }
        }

        private void LogTracker(string msg)
        {
            if (activeLogs.Count > 100) activeLogs.RemoveAt(0); // Cap logs
            activeLogs.Add(msg);
        }

        public string GetSaveFilePath(int slot)
        {
            return Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");
        }

        public bool HasSaveInSlot(int slot)
        {
            return File.Exists(GetSaveFilePath(slot));
        }

        public void SaveGame(int slot, string saveName = "AutoSave")
        {
            try
            {
                GameSaveData data = new GameSaveData();
                data.saveName = saveName;
                data.saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                data.activePlayerIndex = GameManager.Instance.activePlayerIndex;
                data.targetNetWorth = GameManager.Instance.targetNetWorth;
                data.startingCash = GameManager.Instance.startingCash;
                data.turnTimerEnabled = GameManager.Instance.turnTimerEnabled;
                data.turnTimerDuration = GameManager.Instance.turnTimerDuration;
                data.gameStateValue = (int)GameManager.Instance.currentState;
                data.gameLogs = new List<string>(activeLogs);

                // Save Player States
                foreach (var p in GameManager.Instance.players)
                {
                    PlayerSaveData pData = new PlayerSaveData();
                    pData.playerName = p.playerName;
                    pData.playerID = p.playerID;
                    pData.isBot = p.isBot;
                    pData.tokenIndex = p.tokenIndex;
                    pData.colorHex = "#" + ColorUtility.ToHtmlStringRGBA(p.playerColor);
                    pData.cash = p.cash;
                    pData.currentPosition = p.currentPosition;
                    pData.auditTurnsRemaining = p.auditTurnsRemaining;
                    pData.isBankrupt = p.isBankrupt;
                    pData.aiDifficulty = p.aiDifficulty;
                    pData.ownedProperties = new List<string>(p.GetOwnedProperties());
                    data.players.Add(pData);
                }

                // Save Board Property States
                foreach (var tile in BoardManager.Instance.GetTiles())
                {
                    if (tile is Data.PropertyData prop)
                    {
                        var state = BoardManager.Instance.GetPropertyState(prop.tileName);
                        if (state != null)
                        {
                            PropertySaveData prSave = new PropertySaveData();
                            prSave.propertyName = state.propertyName;
                            prSave.ownerID = state.ownerID;
                            prSave.upgradeLevel = state.upgradeLevel;
                            prSave.isMortgaged = state.isMortgaged;
                            data.properties.Add(prSave);
                        }
                    }
                }

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(GetSaveFilePath(slot), json);
                GameManager.Instance.LogGameMessage($"[Save] Game saved successfully in Slot {slot} ({saveName}).");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to save game: {e.Message}");
                GameManager.Instance.LogGameMessage($"[Error] Save failed: {e.Message}");
            }
        }

        public bool LoadGame(int slot)
        {
            if (!HasSaveInSlot(slot))
            {
                GameManager.Instance.LogGameMessage($"[Error] Load failed: No save file found in Slot {slot}.");
                return false;
            }

            try
            {
                string path = GetSaveFilePath(slot);
                string json = File.ReadAllText(path);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

                // Re-initialize GameManager properties
                GameManager.Instance.targetNetWorth = data.targetNetWorth;
                GameManager.Instance.startingCash = data.startingCash;
                GameManager.Instance.turnTimerEnabled = data.turnTimerEnabled;
                GameManager.Instance.turnTimerDuration = data.turnTimerDuration;

                // Recreate players
                foreach (var p in GameManager.Instance.players)
                {
                    if (p != null) Destroy(p.gameObject);
                }
                GameManager.Instance.players.Clear();

                foreach (var pData in data.players)
                {
                    GameObject pObj = new GameObject($"Player_{pData.playerID}");
                    pObj.transform.SetParent(GameManager.Instance.transform);
                    Player p = pObj.AddComponent<Player>();
                    
                    Color color = Color.white;
                    ColorUtility.TryParseHtmlString(pData.colorHex, out color);

                    p.Initialize(pData.playerID, pData.playerName, pData.cash, pData.isBot, pData.aiDifficulty, pData.tokenIndex, color);
                    p.currentPosition = pData.currentPosition;
                    p.auditTurnsRemaining = pData.auditTurnsRemaining;
                    p.isBankrupt = pData.isBankrupt;
                    
                    foreach (var prop in pData.ownedProperties)
                    {
                        p.AddProperty(prop);
                    }

                    GameManager.Instance.players.Add(p);
                }

                // Restore Board Property States
                BoardManager.Instance.InitializePropertyStates();
                foreach (var prSave in data.properties)
                {
                    var state = BoardManager.Instance.GetPropertyState(prSave.propertyName);
                    if (state != null)
                    {
                        state.ownerID = prSave.ownerID;
                        state.upgradeLevel = prSave.upgradeLevel;
                        state.isMortgaged = prSave.isMortgaged;
                    }
                }

                // Restore Logs
                activeLogs.Clear();
                foreach (var log in data.gameLogs)
                {
                    GameManager.Instance.LogGameMessage(log);
                }

                GameManager.Instance.activePlayerIndex = data.activePlayerIndex;
                GameManager.Instance.LogGameMessage($"[Save] Game loaded successfully from Slot {slot} ({data.saveName}).");
                
                // Transition GameManager state
                GameManager.Instance.SetState((GameState)data.gameStateValue);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to load game: {e.Message}");
                GameManager.Instance.LogGameMessage($"[Error] Load failed: {e.Message}");
                return false;
            }
        }
    }
}
