using System;
using System.Collections.Generic;
using UnityEngine;
using FarmEmpire.Board;
using FarmEmpire.Data;

namespace FarmEmpire.Core
{
    public enum GameState
    {
        Setup,
        TurnStart,
        Rolling,
        Moving,
        TileResolution,
        PlayerDecisions,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        public GameState currentState = GameState.Setup;
        public List<Player> players = new List<Player>();
        public int activePlayerIndex = -1;
        public int targetNetWorth = 10000;
        public int startingCash = 1500;
        public bool turnTimerEnabled = false;
        public float turnTimerDuration = 30f;
        public float currentTurnTimer = 0f;

        [Header("Prefabs")]
        [SerializeField] private GameObject playerPrefab; // Simple prefab that has the Player component

        // Events
        public event Action<GameState> OnStateChanged;
        public event Action<Player> OnActivePlayerChanged;
        public event Action<string> OnGameLogAdded;
        public event Action<Player, string> OnVictory; // Winner, reason

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

        private void Update()
        {
            if (currentState == GameState.PlayerDecisions && turnTimerEnabled)
            {
                currentTurnTimer -= Time.deltaTime;
                if (currentTurnTimer <= 0)
                {
                    LogGameMessage($"[Turn Timer] {GetActivePlayer().playerName}'s time expired. Ending turn.");
                    EndTurn();
                }
            }
        }

        public void StartNewOfflineGame(int totalPlayers, int botCount, string aiDifficulty, int winTarget, int startCash, bool timerEnabled, float timerDuration)
        {
            LogGameMessage("Starting a new Offline game...");
            
            // Clean up existing players
            foreach (var p in players)
            {
                if (p != null) Destroy(p.gameObject);
            }
            players.Clear();

            targetNetWorth = winTarget;
            startingCash = startCash;
            turnTimerEnabled = timerEnabled;
            turnTimerDuration = timerDuration;

            Color[] colors = { Color.emerald, Color.yellow, Color.red, Color.blue, Color.magenta, Color.cyan };
            
            // Spawn Human Players
            int humanCount = totalPlayers - botCount;
            for (int i = 0; i < humanCount; i++)
            {
                GameObject pObj = new GameObject($"Player_{i + 1}");
                pObj.transform.SetParent(transform);
                Player p = pObj.AddComponent<Player>();
                p.Initialize(i, $"Executive {i + 1}", startingCash, false, "Normal", i, colors[i % colors.Length]);
                players.Add(p);
            }

            // Spawn Bot Players
            for (int i = 0; i < botCount; i++)
            {
                int botID = humanCount + i;
                GameObject pObj = new GameObject($"Bot_{i + 1}");
                pObj.transform.SetParent(transform);
                Player p = pObj.AddComponent<Player>();
                p.Initialize(botID, $"AgriBot {i + 1}", startingCash, true, aiDifficulty, botID, colors[botID % colors.Length]);
                players.Add(p);
            }

            // Reset Board states
            if (BoardManager.Instance != null)
            {
                BoardManager.Instance.InitializePropertyStates();
            }

            activePlayerIndex = 0;
            SetState(GameState.TurnStart);
        }

        public void SetState(GameState newState)
        {
            currentState = newState;
            Debug.Log($"[GameManager] Game State changed to: {currentState}");
            OnStateChanged?.Invoke(currentState);

            switch (currentState)
            {
                case GameState.TurnStart:
                    ProcessTurnStart();
                    break;
                case GameState.Rolling:
                    // Waiting for roll dice trigger
                    break;
                case GameState.Moving:
                    // Handled by animation/movement routines
                    break;
                case GameState.TileResolution:
                    ResolveLandedTile();
                    break;
                case GameState.PlayerDecisions:
                    currentTurnTimer = turnTimerDuration;
                    ProcessPlayerDecisions();
                    break;
                case GameState.GameOver:
                    break;
            }
        }

        public Player GetActivePlayer()
        {
            if (activePlayerIndex >= 0 && activePlayerIndex < players.Count)
            {
                return players[activePlayerIndex];
            }
            return null;
        }

        private void ProcessTurnStart()
        {
            Player activePlayer = GetActivePlayer();
            if (activePlayer == null || activePlayer.isBankrupt)
            {
                MoveToNextPlayer();
                return;
            }

            OnActivePlayerChanged?.Invoke(activePlayer);
            LogGameMessage($"--- {activePlayer.playerName}'s Turn Start ---");

            if (activePlayer.IsInAuditHold())
            {
                activePlayer.auditTurnsRemaining--;
                LogGameMessage($"{activePlayer.playerName} is under Regulatory Audit. (Turns remaining: {activePlayer.auditTurnsRemaining})");
                
                // If they pay a fine or wait
                if (activePlayer.cash >= 150)
                {
                    // For now, let's auto-pay or wait. Let's say bots always pay if they have enough cash, humans can decide or wait.
                    // To keep implementation simple, pay 150 to get out immediately
                    activePlayer.cash -= 150;
                    activePlayer.auditTurnsRemaining = 0;
                    LogGameMessage($"{activePlayer.playerName} paid $150 regulatory clearance fine and is released.");
                }
                else
                {
                    LogGameMessage($"{activePlayer.playerName} stays in audit review and skips rolling.");
                    EndTurn();
                    return;
                }
            }

            // Move to Roll phase
            SetState(GameState.Rolling);

            // If bot, auto roll dice
            if (activePlayer.isBot)
            {
                Invoke(nameof(TriggerBotDiceRoll), 1.0f);
            }
        }

        private void TriggerBotDiceRoll()
        {
            if (currentState == GameState.Rolling && GetActivePlayer().isBot)
            {
                DiceManager.Instance.RollDice();
            }
        }

        // Triggered by UI or Dice roll event
        public void ExecuteRoll(int d1, int d2, bool isDouble)
        {
            if (currentState != GameState.Rolling) return;

            Player activePlayer = GetActivePlayer();
            int totalRoll = d1 + d2;
            LogGameMessage($"{activePlayer.playerName} rolled a {d1} and {d2} (Total: {totalRoll})");

            // Move token
            SetState(GameState.Moving);
            MovePlayer(activePlayer, totalRoll);
        }

        private void MovePlayer(Player player, int spaces)
        {
            int oldPos = player.currentPosition;
            int newPos = (oldPos + spaces) % BoardManager.Instance.GetTileCount();

            // Pass GO/HQ check
            if (newPos < oldPos)
            {
                // Passed Start
                player.cash += 200;
                LogGameMessage($"{player.playerName} passed Agricultural HQ, receiving $200 subsidy.");
            }

            player.currentPosition = newPos;

            // Simple direct movement for now. We can animate this in UI.
            // Move directly to tile resolution state
            SetState(GameState.TileResolution);
        }

        private void ResolveLandedTile()
        {
            Player player = GetActivePlayer();
            TileData tile = BoardManager.Instance.GetTileAt(player.currentPosition);
            LogGameMessage($"{player.playerName} landed on {tile.tileName} ({tile.tileType})");

            switch (tile.tileType)
            {
                case TileType.Start:
                    // HQ tile landing bonus
                    player.cash += 100;
                    LogGameMessage($"{player.playerName} landed directly on Agricultural HQ and received extra $100.");
                    SetState(GameState.PlayerDecisions);
                    break;

                case TileType.Property:
                    // Handled in EconomyManager (Buy, Rent, upgrade checks)
                    // We transition to decisions, UI will handle buy prompts
                    SetState(GameState.PlayerDecisions);
                    break;

                case TileType.Tax:
                    // Pay tax
                    int taxAmount = 150;
                    if (tile is EventTileData evTile) taxAmount = evTile.fixedValue;
                    PayBank(player, taxAmount, "Import Tariffs / Environmental Tax");
                    SetState(GameState.PlayerDecisions);
                    break;

                case TileType.Subsidy:
                    // Receive subsidy
                    int subsidyAmount = 150;
                    if (tile is EventTileData evTile2) subsidyAmount = evTile2.fixedValue;
                    player.cash += subsidyAmount;
                    LogGameMessage($"{player.playerName} received ${subsidyAmount} Green Farm Subsidy.");
                    SetState(GameState.PlayerDecisions);
                    break;

                case TileType.GoToRegulatoryReview:
                    LogGameMessage($"{player.playerName} is sent to Audit Hold for regulatory inspection!");
                    
                    // Dynamically locate the first RegulatoryReview tile
                    int reviewTileIndex = 0;
                    if (BoardManager.Instance != null)
                    {
                        for (int i = 0; i < BoardManager.Instance.GetTileCount(); i++)
                        {
                            var t = BoardManager.Instance.GetTileAt(i);
                            if (t != null && t.tileType == TileType.RegulatoryReview)
                            {
                                reviewTileIndex = i;
                                break;
                            }
                        }
                    }
                    player.currentPosition = reviewTileIndex;
                    player.auditTurnsRemaining = 3;
                    SetState(GameState.PlayerDecisions);
                    break;

                case TileType.RegulatoryReview:
                case TileType.Neutral:
                default:
                    // Nothing happens
                    SetState(GameState.PlayerDecisions);
                    break;
            }
        }

        private void ProcessPlayerDecisions()
        {
            Player activePlayer = GetActivePlayer();
            
            // Check Net Worth win condition
            CheckWinCondition();

            if (currentState == GameState.GameOver) return;

            // If Bot, let AI take action automatically
            if (activePlayer.isBot)
            {
                // We'll call AI execution. Since AI execution is fast, it will buy, upgrade and end turn.
                // We will invoke it after a slight delay for readability.
                Invoke(nameof(ExecuteBotDecisions), 1.0f);
            }
        }

        private void ExecuteBotDecisions()
        {
            if (currentState != GameState.PlayerDecisions) return;
            // Let the bot evaluate choices and then end turn.
            // Delegate bot action to an AI controller or manager
            AIManager.Instance.ExecuteBotTurn(GetActivePlayer());
        }

        public void EndTurn()
        {
            if (currentState != GameState.PlayerDecisions) return;

            // Check bankruptcy or win conditions
            CheckWinCondition();

            if (currentState == GameState.GameOver) return;

            MoveToNextPlayer();
        }

        private void MoveToNextPlayer()
        {
            int attempts = 0;
            do
            {
                activePlayerIndex = (activePlayerIndex + 1) % players.Count;
                attempts++;
            } while (players[activePlayerIndex].isBankrupt && attempts < players.Count);

            if (attempts >= players.Count)
            {
                // Everyone is bankrupt? Should not happen normally unless only 1 player left.
                CheckWinCondition();
                return;
            }

            SetState(GameState.TurnStart);
        }

        public void PayBank(Player player, int amount, string reason)
        {
            player.cash -= amount;
            LogGameMessage($"{player.playerName} paid ${amount} for {reason}. Current Cash: ${player.cash}");

            if (player.cash < 0)
            {
                // Handle bankruptcy threat
                LogGameMessage($"[ALERT] {player.playerName} has negative cash! Must mortgage or sell assets.");
                // For simplified offline play, if a bot or human runs out, we will trigger bankruptcy flow
                // Or let them resolve it if they have assets.
                ResolveNegativeCash(player);
            }
        }

        public int CalculateNetWorth(Player player)
        {
            int netWorth = player.cash;
            foreach (var propName in player.GetOwnedProperties())
            {
                if (BoardManager.Instance.TryGetPropertyData(propName, out var propData))
                {
                    var state = BoardManager.Instance.GetPropertyState(propName);
                    if (state != null)
                    {
                        if (!state.isMortgaged)
                        {
                            netWorth += propData.buyCost;
                            netWorth += state.upgradeLevel * propData.upgradeCost;
                        }
                        else
                        {
                            netWorth += propData.mortgageValue;
                        }
                    }
                }
            }
            return netWorth;
        }

        public void ResolveNegativeCash(Player player)
        {
            // Simple bankruptcy check: Sell upgrades and mortgage properties until cash >= 0.
            // If they still cannot afford, declare bankrupt.
            List<string> props = new List<string>(player.GetOwnedProperties());
            
            // 1. Sell upgrades first
            foreach (var propName in props)
            {
                var state = BoardManager.Instance.GetPropertyState(propName);
                if (state != null && state.upgradeLevel > 0)
                {
                    if (BoardManager.Instance.TryGetPropertyData(propName, out var propData))
                    {
                        while (state.upgradeLevel > 0 && player.cash < 0)
                        {
                            state.upgradeLevel--;
                            player.cash += propData.upgradeCost / 2; // refund half
                            LogGameMessage($"{player.playerName} sold upgrade on {propName} for ${propData.upgradeCost / 2}. Cash: ${player.cash}");
                        }
                    }
                }
            }

            // 2. Mortgage properties
            if (player.cash < 0)
            {
                foreach (var propName in props)
                {
                    var state = BoardManager.Instance.GetPropertyState(propName);
                    if (state != null && !state.isMortgaged && state.upgradeLevel == 0)
                    {
                        if (BoardManager.Instance.TryGetPropertyData(propName, out var propData))
                        {
                            state.isMortgaged = true;
                            player.cash += propData.mortgageValue;
                            LogGameMessage($"{player.playerName} mortgaged {propName} for ${propData.mortgageValue}. Cash: ${player.cash}");
                            if (player.cash >= 0) break;
                        }
                    }
                }
            }

            // 3. Declare bankrupt
            if (player.cash < 0)
            {
                DeclareBankruptcy(player);
            }
        }

        public void DeclareBankruptcy(Player player)
        {
            player.isBankrupt = true;
            player.cash = 0;
            LogGameMessage($"[BANKRUPTCY] {player.playerName} is bankrupt and eliminated!");

            // Return properties to bank
            List<string> props = new List<string>(player.GetOwnedProperties());
            foreach (var propName in props)
            {
                var state = BoardManager.Instance.GetPropertyState(propName);
                if (state != null)
                {
                    state.ownerID = -1;
                    state.upgradeLevel = 0;
                    state.isMortgaged = false;
                }
                player.RemoveProperty(propName);
            }

            CheckWinCondition();
        }

        public void CheckWinCondition()
        {
            // 1. Check if anyone hit target net worth
            Player topPlayer = null;
            int maxNetWorth = -1;

            int activeCount = 0;
            Player soleSurvivor = null;

            foreach (var p in players)
            {
                if (!p.isBankrupt)
                {
                    activeCount++;
                    soleSurvivor = p;

                    int netWorth = CalculateNetWorth(p);
                    if (netWorth >= targetNetWorth && netWorth > maxNetWorth)
                    {
                        maxNetWorth = netWorth;
                        topPlayer = p;
                    }
                }
            }

            // Check if only one player remains
            if (activeCount == 1 && players.Count > 1)
            {
                LogGameMessage($"Game Over! {soleSurvivor.playerName} is the sole survivor!");
                SetState(GameState.GameOver);
                OnVictory?.Invoke(soleSurvivor, "Bankruptcy of all competitors");
                return;
            }

            if (topPlayer != null)
            {
                LogGameMessage($"Game Over! {topPlayer.playerName} reached the target net worth of ${targetNetWorth}!");
                SetState(GameState.GameOver);
                OnVictory?.Invoke(topPlayer, $"Reached target net worth (${maxNetWorth} >= ${targetNetWorth})");
            }
        }

        public void LogGameMessage(string message)
        {
            Debug.Log($"[LOG] {message}");
            OnGameLogAdded?.Invoke(message);
        }
    }
}
