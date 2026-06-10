using System.Collections.Generic;
using UnityEngine;
using FarmEmpire.Data;

namespace FarmEmpire.Board
{
    [System.Serializable]
    public class PropertyState
    {
        public string propertyName;
        public int ownerID = -1; // -1 = unowned
        public int upgradeLevel = 0; // 0 to 5
        public bool isMortgaged = false;

        public PropertyState(string name)
        {
            propertyName = name;
            ownerID = -1;
            upgradeLevel = 0;
            isMortgaged = false;
        }
    }

    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance { get; private set; }

        [Header("Board Configuration")]
        [SerializeField] public List<TileData> boardTiles = new List<TileData>();

        private Dictionary<string, PropertyState> propertyStates = new Dictionary<string, PropertyState>();

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
                return;
            }

            InitializePropertyStates();
        }

        public void InitializePropertyStates()
        {
            propertyStates.Clear();
            foreach (var tile in boardTiles)
            {
                if (tile is PropertyData prop)
                {
                    propertyStates[prop.tileName] = new PropertyState(prop.tileName);
                }
            }
        }

        public List<TileData> GetTiles() => boardTiles;

        public TileData GetTileAt(int index)
        {
            if (boardTiles == null || boardTiles.Count == 0) return null;
            
            // Handle negative index or wrap around
            int safeIndex = index % boardTiles.Count;
            if (safeIndex < 0) safeIndex += boardTiles.Count;
            
            return boardTiles[safeIndex];
        }

        public int GetTileCount() => boardTiles != null ? boardTiles.Count : 0;

        public PropertyState GetPropertyState(string propertyName)
        {
            if (propertyStates.TryGetValue(propertyName, out var state))
            {
                return state;
            }
            return null;
        }

        public bool TryGetPropertyData(string propertyName, out PropertyData data)
        {
            data = null;
            foreach (var tile in boardTiles)
            {
                if (tile.tileName == propertyName && tile is PropertyData prop)
                {
                    data = prop;
                    return true;
                }
            }
            return false;
        }

        public int GetSectorCount(CropSector sector)
        {
            int count = 0;
            foreach (var tile in boardTiles)
            {
                if (tile is PropertyData prop && prop.sector == sector)
                {
                    count++;
                }
            }
            return count;
        }

        public int GetSectorOwnedCount(CropSector sector, int playerID)
        {
            int count = 0;
            foreach (var tile in boardTiles)
            {
                if (tile is PropertyData prop && prop.sector == sector)
                {
                    var state = GetPropertyState(prop.tileName);
                    if (state != null && state.ownerID == playerID && !state.isMortgaged)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public bool HasSectorMonopoly(CropSector sector, int playerID)
        {
            int total = GetSectorCount(sector);
            if (total == 0) return false;
            return GetSectorOwnedCount(sector, playerID) == total;
        }
    }
}
