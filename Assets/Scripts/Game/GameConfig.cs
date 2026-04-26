// =============================================================================
// GameConfig.cs — ScriptableObject configuration for game balance parameters
// Exposes scouting costs, utility values, and observation noise levels as
// tunable parameters, enabling rapid iteration without recompilation.
// This supports the contingency plan for balancing information vs. action.
// =============================================================================

using UnityEngine;

namespace VeilOfUncertainty
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "VeilOfUncertainty/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Utility Values (Payoff Matrix)")]
        [Tooltip("Reward for neutralizing an enemy")]
        public float utilityNeutralizeEnemy = 50f;

        [Tooltip("Penalty for stepping on a trap")]
        public float utilityTrapDamage = -30f;

        [Tooltip("Cost of deploying a scout")]
        public float utilityScoutCost = -10f;

        [Tooltip("Reward for collecting a resource")]
        public float utilityCollectResource = 20f;

        [Tooltip("Reward for moving to an empty safe cell")]
        public float utilitySafeMove = 5f;

        [Tooltip("Penalty for attacking an empty cell (wasted action)")]
        public float utilityWastedAttack = -5f;

        [Tooltip("Utility for defending (small positive — preserved HP)")]
        public float utilityDefend = 2f;

        [Tooltip("Penalty for being hit by an enemy while moving")]
        public float utilityEnemyDamage = -40f;

        [Tooltip("Bonus for defending against an enemy attack")]
        public float utilityDefendAgainstEnemy = 10f;

        [Header("Scouting Configuration")]
        [Tooltip("Fixed resource cost for deploying one scout")]
        public float scoutCost = 10f;

        [Tooltip("Maximum number of scouts per turn")]
        public int maxScoutsPerTurn = 2;

        [Tooltip("Scouting range (radius in cells)")]
        public int scoutRange = 2;

        [Header("Player Configuration")]
        public int startingHP = 100;
        public int startingScouts = 6;
        public int startingResources = 0;
        public int hpPerResource = 15;

        [Header("Difficulty Scaling")]
        [Tooltip("Base difficulty modifier")]
        public float baseDifficulty = 1.0f;

        [Tooltip("Risk weight adjustment from linear regression")]
        public float riskWeightMultiplier = 0.5f;

        [Header("k-NN Configuration")]
        public int knnK = 3; // Number of neighbors for k-NN
    }
}
