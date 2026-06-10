using System;
using UnityEngine;

namespace FarmEmpire.Core
{
    public class DiceManager : MonoBehaviour
    {
        public static DiceManager Instance { get; private set; }

        public event Action<int, int, bool> OnDiceRolled; // dice1, dice2, isDouble

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

        public void RollDice()
        {
            int d1 = UnityEngine.Random.Range(1, 7);
            int d2 = UnityEngine.Random.Range(1, 7);
            bool isDouble = (d1 == d2);

            Debug.Log($"[DiceManager] Rolled {d1} and {d2} (Double: {isDouble})");
            OnDiceRolled?.Invoke(d1, d2, isDouble);
        }
    }
}
