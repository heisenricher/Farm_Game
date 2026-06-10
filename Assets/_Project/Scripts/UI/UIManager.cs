using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmEmpire.Core;
using FarmEmpire.Board;
using FarmEmpire.Economy;
using FarmEmpire.Data;
using FarmEmpire.SaveSystem;

namespace FarmEmpire.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject setupPanel;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject tradePanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject saveLoadPanel;
        [SerializeField] private GameObject pausePanel;

        [Header("Main Menu Controls")]
        [SerializeField] private Button playBtn;
        [SerializeField] private Button loadBtn;
        [SerializeField] private Button exitBtn;

        [Header("Game Setup Controls")]
        [SerializeField] private TMP_InputField totalPlayersInput;
        [SerializeField] private TMP_InputField botCountInput;
        [SerializeField] private TMP_Dropdown aiDifficultyDropdown;
        [SerializeField] private TMP_InputField targetNetWorthInput;
        [SerializeField] private TMP_InputField startingCashInput;
        [SerializeField] private Toggle turnTimerToggle;
        [SerializeField] private TMP_InputField turnTimerDurationInput;
        [SerializeField] private Button startMatchBtn;
        [SerializeField] private Button backToMenuBtn;

        [Header("Gameplay HUD")]
        [SerializeField] private TextMeshProUGUI activePlayerNameText;
        [SerializeField] private TextMeshProUGUI turnTimerText;
        [SerializeField] private Button rollDiceBtn;
        [SerializeField] private Button endTurnBtn;
        [SerializeField] private Button openTradeBtn;
        [SerializeField] private Button pauseBtn;
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerHUDPrefab; // Shows player name, cash, net worth
        [SerializeField] private TextMeshProUGUI logConsoleText;
        [SerializeField] private ScrollRect logScrollRect;

        [Header("Landed Tile Purchase Dialog")]
        [SerializeField] private GameObject purchaseDialog;
        [SerializeField] private TextMeshProUGUI propertyNameText;
        [SerializeField] private TextMeshProUGUI propertyCostText;
        [SerializeField] private TextMeshProUGUI propertyRentText;
        [SerializeField] private Button buyPropertyBtn;
        [SerializeField] private Button declinePropertyBtn;

        [Header("Property Operations Dialog")]
        [SerializeField] private GameObject propertyOpsDialog;
        [SerializeField] private TextMeshProUGUI opsPropertyNameText;
        [SerializeField] private TextMeshProUGUI opsDetailsText; // Current level, rent, mortgage status
        [SerializeField] private Button upgradeBtn;
        [SerializeField] private Button downgradeBtn;
        [SerializeField] private Button mortgageBtn;
        [SerializeField] private Button unmortgageBtn;
        [SerializeField] private Button sellPropertyBtn;
        [SerializeField] private Button closeOpsBtn;

        [Header("Trade Offering Controls")]
        [SerializeField] private TMP_Dropdown tradeTargetDropdown;
        [SerializeField] private TMP_InputField offerCashInput;
        [SerializeField] private TMP_Dropdown myPropertiesDropdown;
        [SerializeField] private TMP_Dropdown targetPropertiesDropdown;
        [SerializeField] private Button addMyPropBtn;
        [SerializeField] private Button addTargetPropBtn;
        [SerializeField] private TextMeshProUGUI myOfferListText;
        [SerializeField] private TextMeshProUGUI targetOfferListText;
        [SerializeField] private Button proposeTradeBtn;
        [SerializeField] private Button cancelTradeBtn;

        [Header("Bot Trade Proposal Dialog")]
        [SerializeField] private GameObject tradeProposedDialog;
        [SerializeField] private TextMeshProUGUI tradeProposalDescText;
        [SerializeField] private Button acceptTradeBtn;
        [SerializeField] private Button declineTradeBtn;

        [Header("Victory Controls")]
        [SerializeField] private TextMeshProUGUI winnerNameText;
        [SerializeField] private TextMeshProUGUI victoryReasonText;
        [SerializeField] private Button victoryExitBtn;

        [Header("Save & Load Panel")]
        [SerializeField] private Button[] saveSlotBtns; // Slot buttons
        [SerializeField] private Button[] loadSlotBtns;
        [SerializeField] private Button closeSaveLoadBtn;
        
        // Internal state for trade draft
        private TradeOffer currentDraftOffer;
        private string selectedOpsProperty;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // Subscribe to game events
            GameManager.Instance.OnStateChanged += HandleGameStateChanged;
            GameManager.Instance.OnActivePlayerChanged += HandleActivePlayerChanged;
            GameManager.Instance.OnGameLogAdded += AddLogMessage;
            GameManager.Instance.OnVictory += HandleVictory;
            DiceManager.Instance.OnDiceRolled += HandleDiceRolled;

            // Setup buttons
            playBtn.onClick.AddListener(() => ShowPanel(setupPanel));
            loadBtn.onClick.AddListener(() => OpenSaveLoad(false));
            exitBtn.onClick.AddListener(Application.Quit);

            startMatchBtn.onClick.AddListener(StartMatchFromSetup);
            backToMenuBtn.onClick.AddListener(() => ShowPanel(mainMenuPanel));

            rollDiceBtn.onClick.AddListener(() => DiceManager.Instance.RollDice());
            endTurnBtn.onClick.AddListener(() => GameManager.Instance.EndTurn());
            openTradeBtn.onClick.AddListener(OpenTradeOfferWindow);
            pauseBtn.onClick.AddListener(() => pausePanel.SetActive(true));
            victoryExitBtn.onClick.AddListener(() => ShowPanel(mainMenuPanel));

            buyPropertyBtn.onClick.AddListener(BuyLandedProperty);
            declinePropertyBtn.onClick.AddListener(DeclineLandedProperty);

            // Ops buttons
            upgradeBtn.onClick.AddListener(UpgradeOpsProperty);
            downgradeBtn.onClick.AddListener(DowngradeOpsProperty);
            mortgageBtn.onClick.AddListener(MortgageOpsProperty);
            unmortgageBtn.onClick.AddListener(UnmortgageOpsProperty);
            sellPropertyBtn.onClick.AddListener(SellOpsProperty);
            closeOpsBtn.onClick.AddListener(() => propertyOpsDialog.SetActive(false));

            // Trade offering setup
            addMyPropBtn.onClick.AddListener(AddMyPropToTrade);
            addTargetPropBtn.onClick.AddListener(AddTargetPropToTrade);
            proposeTradeBtn.onClick.AddListener(SubmitTradeOffer);
            cancelTradeBtn.onClick.AddListener(() => tradePanel.SetActive(false));

            closeSaveLoadBtn.onClick.AddListener(() => saveLoadPanel.SetActive(false));

            SetupSaveLoadButtons();
            
            ShowPanel(mainMenuPanel);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnActivePlayerChanged -= HandleActivePlayerChanged;
                GameManager.Instance.OnGameLogAdded -= AddLogMessage;
                GameManager.Instance.OnVictory -= HandleVictory;
            }
            if (DiceManager.Instance != null)
            {
                DiceManager.Instance.OnDiceRolled -= HandleDiceRolled;
            }
        }

        private void Update()
        {
            if (gameplayPanel.activeSelf && GameManager.Instance.currentState == GameState.PlayerDecisions && GameManager.Instance.turnTimerEnabled)
            {
                turnTimerText.text = $"Time Left: {Mathf.CeilToInt(GameManager.Instance.currentTurnTimer)}s";
            }
            else
            {
                turnTimerText.text = "";
            }
        }

        private void ShowPanel(GameObject panelToShow)
        {
            mainMenuPanel.SetActive(panelToShow == mainMenuPanel);
            setupPanel.SetActive(panelToShow == setupPanel);
            gameplayPanel.SetActive(panelToShow == gameplayPanel);
            victoryPanel.SetActive(panelToShow == victoryPanel);
            
            tradePanel.SetActive(false);
            saveLoadPanel.SetActive(false);
            pausePanel.SetActive(false);
            purchaseDialog.SetActive(false);
            propertyOpsDialog.SetActive(false);
            tradeProposedDialog.SetActive(false);
        }

        #region Setup Match

        private void StartMatchFromSetup()
        {
            int total = int.Parse(string.IsNullOrEmpty(totalPlayersInput.text) ? "4" : totalPlayersInput.text);
            int bots = int.Parse(string.IsNullOrEmpty(botCountInput.text) ? "3" : botCountInput.text);
            string diff = aiDifficultyDropdown.options[aiDifficultyDropdown.value].text;
            int winVal = int.Parse(string.IsNullOrEmpty(targetNetWorthInput.text) ? "10000" : targetNetWorthInput.text);
            int startCash = int.Parse(string.IsNullOrEmpty(startingCashInput.text) ? "1500" : startingCashInput.text);
            bool timerOn = turnTimerToggle.isOn;
            float duration = float.Parse(string.IsNullOrEmpty(turnTimerDurationInput.text) ? "30" : turnTimerDurationInput.text);

            ShowPanel(gameplayPanel);
            
            // Clear old logs in text UI
            logConsoleText.text = "";

            GameManager.Instance.StartNewOfflineGame(total, bots, diff, winVal, startCash, timerOn, duration);
        }

        #endregion

        #region Game HUD

        private void HandleGameStateChanged(GameState state)
        {
            UpdateHUDButtons(state);

            if (state == GameState.TileResolution)
            {
                // Landed. Check if unowned property
                Player active = GameManager.Instance.GetActivePlayer();
                if (active != null && !active.isBot)
                {
                    TileData tile = BoardManager.Instance.GetTileAt(active.currentPosition);
                    if (tile is PropertyData prop)
                    {
                        var pState = BoardManager.Instance.GetPropertyState(prop.tileName);
                        if (pState != null && pState.ownerID == -1)
                        {
                            // Prompt purchase dialog
                            propertyNameText.text = prop.tileName;
                            propertyCostText.text = $"Cost: ${prop.buyCost}";
                            propertyRentText.text = $"Rent: ${prop.rentTiers[0]} (Upgrade Tier 0)";
                            purchaseDialog.SetActive(true);
                        }
                    }
                }
            }
            
            // Refresh player stats in HUD
            UpdatePlayerListHUD();
        }

        private void HandleActivePlayerChanged(Player player)
        {
            activePlayerNameText.text = $"{player.playerName}'s Turn";
            activePlayerNameText.color = player.playerColor;
            
            // Close purchase dialog and ops dialog from previous turn
            purchaseDialog.SetActive(false);
            propertyOpsDialog.SetActive(false);
        }

        private void UpdateHUDButtons(GameState state)
        {
            Player active = GameManager.Instance.GetActivePlayer();
            bool isMyTurn = (active != null && !active.isBot);

            rollDiceBtn.gameObject.SetActive(isMyTurn && state == GameState.Rolling);
            endTurnBtn.gameObject.SetActive(isMyTurn && state == GameState.PlayerDecisions);
            openTradeBtn.gameObject.SetActive(isMyTurn && state == GameState.PlayerDecisions);
        }

        private void HandleDiceRolled(int d1, int d2, bool isDouble)
        {
            // Dice rolling anim placeholder trigger
            GameManager.Instance.ExecuteRoll(d1, d2, isDouble);
        }

        private void AddLogMessage(string msg)
        {
            if (logConsoleText != null)
            {
                logConsoleText.text += msg + "\n";
                // Auto scroll to bottom
                Canvas.ForceUpdateCanvases();
                if (logScrollRect != null) logScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void UpdatePlayerListHUD()
        {
            // Destroy old entries
            foreach (Transform child in playerListContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var p in GameManager.Instance.players)
            {
                // Instantiate HUD items
                // For MVP, create simple texts inside container
                GameObject item = new GameObject($"HUD_{p.playerName}");
                item.transform.SetParent(playerListContainer, false);
                var text = item.AddComponent<TextMeshProUGUI>();
                text.font = activePlayerNameText.font;
                text.fontSize = 18;
                
                int netWorth = GameManager.Instance.CalculateNetWorth(p);
                string status = p.isBankrupt ? "[BANKRUPT]" : p.IsInAuditHold() ? "[AUDIT]" : "";
                
                text.text = $"{p.playerName} ({(p.isBot ? "Bot" : "Player")}): ${p.cash} | Net Worth: ${netWorth} {status}";
                text.color = p.isBankrupt ? Color.gray : p.playerColor;

                // Add button to inspect property list of this player
                if (!p.isBankrupt)
                {
                    var btn = item.AddComponent<Button>();
                    btn.onClick.AddListener(() => OpenPlayerPropertyOps(p));
                }
            }
        }

        #endregion

        #region Landed Property Purchase

        private void BuyLandedProperty()
        {
            Player active = GameManager.Instance.GetActivePlayer();
            if (active != null && !active.isBot)
            {
                TileData tile = BoardManager.Instance.GetTileAt(active.currentPosition);
                if (tile is PropertyData prop)
                {
                    if (EconomyManager.Instance.CanBuyProperty(active, prop))
                    {
                        EconomyManager.Instance.BuyProperty(active, prop);
                    }
                    else
                    {
                        GameManager.Instance.LogGameMessage($"[UI] Not enough cash to purchase {prop.tileName}.");
                    }
                }
            }
            purchaseDialog.SetActive(false);
            GameManager.Instance.SetState(GameState.PlayerDecisions);
        }

        private void DeclineLandedProperty()
        {
            purchaseDialog.SetActive(false);
            GameManager.Instance.SetState(GameState.PlayerDecisions);
        }

        #endregion

        #region Property Upgrades / Operations

        private void OpenPlayerPropertyOps(Player owner)
        {
            // Allows player to inspect and manage their properties (or look at bot's properties without modifying)
            Player active = GameManager.Instance.GetActivePlayer();
            bool isMe = (active != null && active.playerID == owner.playerID && !active.isBot);

            // Open property ops window if there is an owned property
            List<string> owned = owner.GetOwnedProperties();
            if (owned.Count == 0) return;

            // Pick first property to show operations for
            selectedOpsProperty = owned[0];
            ShowPropertyOpsDialog(owner, isMe);
        }

        private void ShowPropertyOpsDialog(Player owner, bool allowModifications)
        {
            if (string.IsNullOrEmpty(selectedOpsProperty)) return;

            if (BoardManager.Instance.TryGetPropertyData(selectedOpsProperty, out var prop))
            {
                var state = BoardManager.Instance.GetPropertyState(selectedOpsProperty);
                opsPropertyNameText.text = prop.tileName;
                
                string status = state.isMortgaged ? "Mortgaged" : $"Level {state.upgradeLevel}";
                int currentRent = EconomyManager.Instance.CalculateRent(prop);
                opsDetailsText.text = $"Status: {status}\nRent: ${currentRent}\nSector: {prop.sector}\nMortgage Val: ${prop.mortgageValue}";

                // Enable buttons if allowed
                upgradeBtn.gameObject.SetActive(allowModifications && EconomyManager.Instance.CanUpgradeProperty(owner, prop));
                downgradeBtn.gameObject.SetActive(allowModifications && EconomyManager.Instance.CanDowngradeProperty(owner, prop));
                mortgageBtn.gameObject.SetActive(allowModifications && EconomyManager.Instance.CanMortgageProperty(owner, prop));
                unmortgageBtn.gameObject.SetActive(allowModifications && EconomyManager.Instance.CanUnmortgageProperty(owner, prop));
                sellPropertyBtn.gameObject.SetActive(allowModifications && EconomyManager.Instance.CanSellProperty(owner, prop));

                propertyOpsDialog.SetActive(true);
            }
        }

        private void UpgradeOpsProperty()
        {
            Player active = GameManager.Instance.GetActivePlayer();
            if (active != null && BoardManager.Instance.TryGetPropertyData(selectedOpsProperty, out var prop))
            {
                EconomyManager.Instance.UpgradeProperty(active, prop);
                ShowPropertyOpsDialog(active, true);
                UpdatePlayerListHUD();
            }
        }

        private void DowngradeOpsProperty()
        {
            Player active = GameManager.Instance.GetActivePlayer();
            if (active != null && BoardManager.Instance.TryGetPropertyData(selectedOpsProperty, out var prop))
            {
                EconomyManager.Instance.DowngradeProperty(active, prop);
                ShowPropertyOpsDialog(active, true);
                UpdatePlayerListHUD();
            }
        }

        private void MortgageOpsProperty()
        {
            Player active = GameManager.Instance.GetActivePlayer();
            if (active != null && BoardManager.Instance.TryGetPropertyData(selectedOpsProperty, out var prop))
            {
                EconomyManager.Instance.MortgageProperty(active, prop);
                ShowPropertyOpsDialog(active, true);
                UpdatePlayerListHUD();
            }
        }

        private void UnmortgageOpsProperty()
        {
            Player active = GameManager.Instance.GetActivePlayer();
            if (active != null && BoardManager.Instance.TryGetPropertyData(selectedOpsProperty, out var prop))
            {
                EconomyManager.Instance.UnmortgageProperty(active, prop);
                ShowPropertyOpsDialog(active, true);
                UpdatePlayerListHUD();
            }
        }

        private void SellOpsProperty()
        {
            Player active = GameManager.Instance.GetActivePlayer();
            if (active != null && BoardManager.Instance.TryGetPropertyData(selectedOpsProperty, out var prop))
            {
                EconomyManager.Instance.SellProperty(active, prop);
                propertyOpsDialog.SetActive(false);
                UpdatePlayerListHUD();
            }
        }

        #endregion

        #region Trading UI

        private void OpenTradeOfferWindow()
        {
            Player active = GameManager.Instance.GetActivePlayer();
            if (active == null || active.isBot) return;

            currentDraftOffer = new TradeOffer();
            currentDraftOffer.senderID = active.playerID;
            currentDraftOffer.senderCashOffer = 0;

            // Populate Targets (other players not bankrupt)
            tradeTargetDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (var p in GameManager.Instance.players)
            {
                if (p.playerID != active.playerID && !p.isBankrupt)
                {
                    options.Add(new TMP_Dropdown.OptionData($"{p.playerName}"));
                }
            }
            tradeTargetDropdown.AddOptions(options);

            // Populate My Properties
            myPropertiesDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> myProps = new List<TMP_Dropdown.OptionData>();
            foreach (var propName in active.GetOwnedProperties())
            {
                // Only properties with level 0 can be traded
                var state = BoardManager.Instance.GetPropertyState(propName);
                if (state != null && state.upgradeLevel == 0)
                {
                    myProps.Add(new TMP_Dropdown.OptionData(propName));
                }
            }
            myPropertiesDropdown.AddOptions(myProps);

            RefreshTradeDraftUI();
            tradePanel.SetActive(true);
        }

        private void RefreshTradeDraftUI()
        {
            // Read target player ID
            if (tradeTargetDropdown.options.Count == 0) return;
            string targetName = tradeTargetDropdown.options[tradeTargetDropdown.value].text;
            Player target = GameManager.Instance.players.Find(p => p.playerName == targetName);
            if (target != null)
            {
                currentDraftOffer.receiverID = target.playerID;
                
                // Populate Target Properties
                targetPropertiesDropdown.ClearOptions();
                List<TMP_Dropdown.OptionData> targetProps = new List<TMP_Dropdown.OptionData>();
                foreach (var propName in target.GetOwnedProperties())
                {
                    var state = BoardManager.Instance.GetPropertyState(propName);
                    if (state != null && state.upgradeLevel == 0)
                    {
                        targetProps.Add(new TMP_Dropdown.OptionData(propName));
                    }
                }
                targetPropertiesDropdown.AddOptions(targetProps);
            }

            myOfferListText.text = "My Assets Offered:\n" + string.Join("\n", currentDraftOffer.senderProperties);
            targetOfferListText.text = "Target Assets Demanded:\n" + string.Join("\n", currentDraftOffer.receiverProperties);
        }

        private void AddMyPropToTrade()
        {
            if (myPropertiesDropdown.options.Count == 0) return;
            string prop = myPropertiesDropdown.options[myPropertiesDropdown.value].text;
            if (!currentDraftOffer.senderProperties.Contains(prop))
            {
                currentDraftOffer.senderProperties.Add(prop);
            }
            RefreshTradeDraftUI();
        }

        private void AddTargetPropToTrade()
        {
            if (targetPropertiesDropdown.options.Count == 0) return;
            string prop = targetPropertiesDropdown.options[targetPropertiesDropdown.value].text;
            if (!currentDraftOffer.receiverProperties.Contains(prop))
            {
                currentDraftOffer.receiverProperties.Add(prop);
            }
            RefreshTradeDraftUI();
        }

        private void SubmitTradeOffer()
        {
            int cash = 0;
            int.TryParse(offerCashInput.text, out cash);
            currentDraftOffer.senderCashOffer = cash;

            if (TradeManager.Instance.ValidateOffer(currentDraftOffer))
            {
                tradePanel.SetActive(false);
                TradeManager.Instance.ProposeTrade(currentDraftOffer);
            }
            else
            {
                GameManager.Instance.LogGameMessage("[UI] Trade validation failed. Check owned properties or cash levels.");
            }
        }

        #endregion

        #region Save / Load HUD

        private void OpenSaveLoad(bool isSaving)
        {
            saveLoadPanel.SetActive(true);
            for (int i = 0; i < saveSlotBtns.Length; i++)
            {
                int slot = i + 1;
                bool hasSave = SaveLoadManager.Instance.HasSaveInSlot(slot);
                
                saveSlotBtns[i].gameObject.SetActive(isSaving);
                loadSlotBtns[i].gameObject.SetActive(!isSaving && hasSave);

                // Add listeners
                saveSlotBtns[i].onClick.RemoveAllListeners();
                saveSlotBtns[i].onClick.AddListener(() => {
                    SaveLoadManager.Instance.SaveGame(slot, $"Match_{DateTime.Now:MMdd_HHmm}");
                    saveLoadPanel.SetActive(false);
                });

                loadSlotBtns[i].onClick.RemoveAllListeners();
                loadSlotBtns[i].onClick.AddListener(() => {
                    if (SaveLoadManager.Instance.LoadGame(slot))
                    {
                        ShowPanel(gameplayPanel);
                    }
                });
            }
        }

        private void SetupSaveLoadButtons()
        {
            // Set button count
            if (saveSlotBtns == null || saveSlotBtns.Length == 0)
            {
                saveSlotBtns = new Button[3];
                loadSlotBtns = new Button[3];
            }
        }

        #endregion

        #region Victory & Pause

        private void HandleVictory(Player winner, string reason)
        {
            winnerNameText.text = $"{winner.playerName} Wins!";
            winnerNameText.color = winner.playerColor;
            victoryReasonText.text = reason;
            ShowPanel(victoryPanel);
        }

        public void ResumeGame()
        {
            pausePanel.SetActive(false);
        }

        public void OpenSaveMenuFromPause()
        {
            pausePanel.SetActive(false);
            OpenSaveLoad(true);
        }

        public void ExitToMainMenuFromPause()
        {
            ShowPanel(mainMenuPanel);
        }

        #endregion
    }
}
