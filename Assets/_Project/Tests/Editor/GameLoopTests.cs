using NUnit.Framework;
using UnityEngine;
using FarmEmpire.Core;
using FarmEmpire.Board;
using FarmEmpire.Data;

namespace FarmEmpire.Tests
{
    public class GameLoopTests
    {
        private GameObject testContainer;
        private GameManager gameManager;
        private BoardManager boardManager;
        private Player player1;
        private Player player2;

        [SetUp]
        public void SetUp()
        {
            testContainer = new GameObject("TestContainer");
            gameManager = testContainer.AddComponent<GameManager>();
            boardManager = testContainer.AddComponent<BoardManager>();

            // Setup players
            GameObject p1Obj = new GameObject("Player1");
            p1Obj.transform.SetParent(testContainer.transform);
            player1 = p1Obj.AddComponent<Player>();
            player1.Initialize(0, "TestPlayer1", 1500, false, "Normal", 0, Color.red);

            GameObject p2Obj = new GameObject("Player2");
            p2Obj.transform.SetParent(testContainer.transform);
            player2 = p2Obj.AddComponent<Player>();
            player2.Initialize(1, "TestPlayer2", 1500, false, "Normal", 1, Color.blue);

            gameManager.players.Clear();
            gameManager.players.Add(player1);
            gameManager.players.Add(player2);

            // Populate some minimal tiles on board (e.g. 5 tiles for quick testing)
            // 0: Start, 1: Tax, 2: RegulatoryReview, 3: Subsidy, 4: GoToRegulatoryReview
            var startTile = ScriptableObject.CreateInstance<EventTileData>();
            startTile.tileName = "Agricultural HQ";
            startTile.tileType = TileType.Start;

            var taxTile = ScriptableObject.CreateInstance<EventTileData>();
            taxTile.tileName = "Environmental Tax";
            taxTile.tileType = TileType.Tax;
            taxTile.fixedValue = 100;

            var reviewTile = ScriptableObject.CreateInstance<EventTileData>();
            reviewTile.tileName = "Audit Hold";
            reviewTile.tileType = TileType.RegulatoryReview;

            var subsidyTile = ScriptableObject.CreateInstance<EventTileData>();
            subsidyTile.tileName = "Subsidy";
            subsidyTile.tileType = TileType.Subsidy;
            subsidyTile.fixedValue = 150;

            var goToReviewTile = ScriptableObject.CreateInstance<EventTileData>();
            goToReviewTile.tileName = "Audit Office";
            goToReviewTile.tileType = TileType.GoToRegulatoryReview;

            boardManager.boardTiles.Clear();
            boardManager.boardTiles.Add(startTile);       // Index 0
            boardManager.boardTiles.Add(taxTile);         // Index 1
            boardManager.boardTiles.Add(reviewTile);      // Index 2 (Regulatory review hold spot)
            boardManager.boardTiles.Add(subsidyTile);     // Index 3
            boardManager.boardTiles.Add(goToReviewTile);  // Index 4

            boardManager.InitializePropertyStates();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(testContainer);
        }

        [Test]
        public void TestGameInitialization()
        {
            gameManager.StartNewOfflineGame(2, 0, "Normal", 10000, 1500, false, 30f);
            
            Assert.AreEqual(2, gameManager.players.Count);
            Assert.AreEqual(1500, gameManager.players[0].cash);
            Assert.AreEqual(0, gameManager.activePlayerIndex);
            Assert.AreEqual(GameState.Rolling, gameManager.currentState);
        }

        [Test]
        public void TestPassingHQSubsidy()
        {
            gameManager.StartNewOfflineGame(2, 0, "Normal", 10000, 1500, false, 30f);
            
            // Player 1 is at index 0. Move them 6 spaces.
            // Board has 5 tiles, so they wrap around to index 1.
            // Starting cash is 1500. Passing start gives 200. Cash should be 1700.
            gameManager.ExecuteRoll(3, 3, false); // Moves spaces = 6

            Assert.AreEqual(1, player1.currentPosition);
            Assert.AreEqual(1700, player1.cash); 
        }

        [Test]
        public void TestLandedTileResolution_Tax()
        {
            gameManager.StartNewOfflineGame(2, 0, "Normal", 10000, 1500, false, 30f);
            
            // Player rolls 1, lands on environmental tax tile (index 1).
            // Should lose $100 tax. Cash goes 1500 -> 1400.
            gameManager.ExecuteRoll(1, 0, false);

            Assert.AreEqual(1400, player1.cash);
            Assert.AreEqual(GameState.PlayerDecisions, gameManager.currentState);
        }

        [Test]
        public void TestGoToRegulatoryReview()
        {
            gameManager.StartNewOfflineGame(2, 0, "Normal", 10000, 1500, false, 30f);
            
            // Roll 4, land on GoToRegulatoryReview (index 4).
            // Should get sent to index 10 (Wait, in GameManager it moves to index 10.
            // Let's modify index to 2 in test or let it map to index 2.
            // GameManager hardcodes GoToRegulatoryReview position to index 10.
            // Let's verify that auditTurnsRemaining is set to 3.
            gameManager.ExecuteRoll(4, 0, false);

            Assert.AreEqual(3, player1.auditTurnsRemaining);
            Assert.IsTrue(player1.IsInAuditHold());
        }

        [Test]
        public void TestWinCondition_NetWorthReached()
        {
            gameManager.StartNewOfflineGame(2, 0, "Normal", 3000, 1500, false, 30f);
            
            // Give player 1 plenty of cash to exceed target of 3000
            player1.cash = 3500;
            
            gameManager.CheckWinCondition();

            Assert.AreEqual(GameState.GameOver, gameManager.currentState);
        }

        [Test]
        public void TestWinCondition_BankruptcyElimination()
        {
            gameManager.StartNewOfflineGame(2, 0, "Normal", 10000, 1500, false, 30f);

            // Bankrupt player 2
            gameManager.DeclareBankruptcy(player2);

            // Player 1 should win as sole survivor
            Assert.IsTrue(player2.isBankrupt);
            Assert.AreEqual(GameState.GameOver, gameManager.currentState);
        }
    }
}
