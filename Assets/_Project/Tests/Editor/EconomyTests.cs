using NUnit.Framework;
using UnityEngine;
using FarmEmpire.Core;
using FarmEmpire.Board;
using FarmEmpire.Economy;
using FarmEmpire.Data;

namespace FarmEmpire.Tests
{
    public class EconomyTests
    {
        private GameObject testContainer;
        private GameManager gameManager;
        private BoardManager boardManager;
        private EconomyManager economyManager;
        private Player player1;
        private Player player2;

        private PropertyData mockProp1;
        private PropertyData mockProp2;

        [SetUp]
        public void SetUp()
        {
            testContainer = new GameObject("TestContainer");
            
            // Add managers
            gameManager = testContainer.AddComponent<GameManager>();
            boardManager = testContainer.AddComponent<BoardManager>();
            economyManager = testContainer.AddComponent<EconomyManager>();

            // Setup players
            GameObject p1Obj = new GameObject("Player1");
            p1Obj.transform.SetParent(testContainer.transform);
            player1 = p1Obj.AddComponent<Player>();
            player1.Initialize(0, "TestPlayer1", 1500, false, "Normal", 0, Color.red);

            GameObject p2Obj = new GameObject("Player2");
            p2Obj.transform.SetParent(testContainer.transform);
            player2 = p2Obj.AddComponent<Player>();
            player2.Initialize(1, "TestPlayer2", 1500, false, "Normal", 1, Color.blue);

            // Add players to game manager list
            gameManager.players.Clear();
            gameManager.players.Add(player1);
            gameManager.players.Add(player2);

            // Create mock property data
            mockProp1 = ScriptableObject.CreateInstance<PropertyData>();
            mockProp1.tileName = "Rice Fields A";
            mockProp1.sector = CropSector.RiceValley;
            mockProp1.buyCost = 100;
            mockProp1.baseRent = 10;
            mockProp1.upgradeCost = 50;
            mockProp1.rentTiers = new int[] { 10, 20, 40, 80, 150, 300 };
            mockProp1.mortgageValue = 50;
            mockProp1.unmortgageCost = 55;
            mockProp1.sellValue = 50;

            mockProp2 = ScriptableObject.CreateInstance<PropertyData>();
            mockProp2.tileName = "Rice Fields B";
            mockProp2.sector = CropSector.RiceValley;
            mockProp2.buyCost = 100;
            mockProp2.baseRent = 10;
            mockProp2.upgradeCost = 50;
            mockProp2.rentTiers = new int[] { 10, 20, 40, 80, 150, 300 };
            mockProp2.mortgageValue = 50;
            mockProp2.unmortgageCost = 55;
            mockProp2.sellValue = 50;

            // Setup BoardManager tiles list
            boardManager.boardTiles.Clear();
            boardManager.boardTiles.Add(mockProp1);
            boardManager.boardTiles.Add(mockProp2);
            boardManager.InitializePropertyStates();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(testContainer);
        }

        [Test]
        public void TestBuyPropertySuccess()
        {
            Assert.IsTrue(economyManager.CanBuyProperty(player1, mockProp1));
            
            economyManager.BuyProperty(player1, mockProp1);

            var state = boardManager.GetPropertyState(mockProp1.tileName);
            Assert.AreEqual(0, state.ownerID);
            Assert.AreEqual(1400, player1.cash);
            Assert.Contains(mockProp1.tileName, player1.GetOwnedProperties());
        }

        [Test]
        public void TestBuyPropertyFail_InsufficientCash()
        {
            player1.cash = 50; // Less than cost of 100
            Assert.IsFalse(economyManager.CanBuyProperty(player1, mockProp1));
        }

        [Test]
        public void TestBuyPropertyFail_AlreadyOwned()
        {
            economyManager.BuyProperty(player1, mockProp1);
            Assert.IsFalse(economyManager.CanBuyProperty(player2, mockProp1));
        }

        [Test]
        public void TestCalculateRent_NoMonopoly()
        {
            economyManager.BuyProperty(player1, mockProp1);
            int rent = economyManager.CalculateRent(mockProp1);
            Assert.AreEqual(10, rent); // Base rent
        }

        [Test]
        public void TestCalculateRent_WithMonopoly()
        {
            // Buy all RiceValley properties (mockProp1 & mockProp2)
            economyManager.BuyProperty(player1, mockProp1);
            economyManager.BuyProperty(player1, mockProp2);

            int rent = economyManager.CalculateRent(mockProp1);
            Assert.AreEqual(20, rent); // Double base rent due to monopoly
        }

        [Test]
        public void TestUpgradeProperty_NeedsMonopoly()
        {
            economyManager.BuyProperty(player1, mockProp1);
            
            // Try to upgrade without owning mockProp2
            Assert.IsFalse(economyManager.CanUpgradeProperty(player1, mockProp1));

            // Buy mockProp2 to get monopoly
            economyManager.BuyProperty(player1, mockProp2);
            Assert.IsTrue(economyManager.CanUpgradeProperty(player1, mockProp1));
        }

        [Test]
        public void TestUpgradeProperty_EvenBuildRule()
        {
            economyManager.BuyProperty(player1, mockProp1);
            economyManager.BuyProperty(player1, mockProp2);

            // First upgrade on prop1
            economyManager.UpgradeProperty(player1, mockProp1);
            Assert.AreEqual(1, boardManager.GetPropertyState(mockProp1.tileName).upgradeLevel);

            // Cannot upgrade prop1 again to level 2 until prop2 is upgraded to level 1 (even build rule)
            Assert.IsFalse(economyManager.CanUpgradeProperty(player1, mockProp1));
            
            // Upgrade prop2 to level 1
            economyManager.UpgradeProperty(player1, mockProp2);
            
            // Now we can upgrade prop1 to level 2
            Assert.IsTrue(economyManager.CanUpgradeProperty(player1, mockProp1));
        }

        [Test]
        public void TestMortgageProperty()
        {
            economyManager.BuyProperty(player1, mockProp1);

            Assert.IsTrue(economyManager.CanMortgageProperty(player1, mockProp1));
            economyManager.MortgageProperty(player1, mockProp1);

            var state = boardManager.GetPropertyState(mockProp1.tileName);
            Assert.IsTrue(state.isMortgaged);
            Assert.AreEqual(1450, player1.cash); // Cost: -100, Mortgage: +50 = 1450
        }

        [Test]
        public void TestPayRent()
        {
            economyManager.BuyProperty(player2, mockProp1); // P2 owns it
            player1.cash = 1000;
            player2.cash = 1000;

            economyManager.PayRent(player1, player2, mockProp1);

            Assert.AreEqual(990, player1.cash);
            Assert.AreEqual(1010, player2.cash);
        }
    }
}
