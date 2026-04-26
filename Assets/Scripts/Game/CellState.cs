// =============================================================================
// CellState.cs — Core enumerations and data types for Veil of Uncertainty
// Defines the POMDP state space, action space, and observation types.
// =============================================================================

namespace VeilOfUncertainty
{
    /// <summary>
    /// Hidden state of each grid cell in the POMDP model.
    /// Corresponds to the state space S = {Enemy, Trap, Resource, Empty}.
    /// </summary>
    public enum CellState
    {
        Empty    = 0,
        Enemy    = 1,
        Trap     = 2,
        Resource = 3
    }

    /// <summary>
    /// Player action space A = {Scout, Move, Attack, Defend}.
    /// These are the action node values in the decision network.
    /// </summary>
    public enum PlayerAction
    {
        Scout   = 0,
        Move    = 1,
        Attack  = 2,
        Defend  = 3
    }

    /// <summary>
    /// Noisy sensor observation types returned by the observation function P(o | s).
    /// </summary>
    public enum SensorReading
    {
        Safe    = 0,  // Suggests the cell is empty or has a resource
        Danger  = 1,  // Suggests the cell contains an enemy or trap
        Unknown = 2   // No useful information (noise)
    }

    /// <summary>
    /// Enemy behavior classification labels used by the k-NN classifier.
    /// </summary>
    public enum EnemyBehaviorType
    {
        Aggressive = 0,
        Defensive  = 1,
        Patrol     = 2
    }

    /// <summary>
    /// Direction enumeration for player movement on the grid.
    /// </summary>
    public enum Direction
    {
        North = 0,
        South = 1,
        East  = 2,
        West  = 3
    }

    /// <summary>
    /// Result of the AI Advisor's analysis each turn, combining MEU,
    /// VPI recommendations, and belief-state summaries.
    /// </summary>
    public struct AdvisorRecommendation
    {
        public PlayerAction RecommendedAction;
        public float MEU;
        public bool ShouldScout;
        public int ScoutTargetX;
        public int ScoutTargetY;
        public float ScoutVPI;
        public float ScoutCost;
        public string Explanation;
    }

    /// <summary>
    /// Stores per-cell VPI computation results for the VPI panel display.
    /// </summary>
    public struct VPIResult
    {
        public int CellX;
        public int CellY;
        public float VPI;
        public float NetVPI; // VPI minus scouting cost
    }

    /// <summary>
    /// Feature vector for enemy behavior classification via k-NN.
    /// </summary>
    public struct EnemyFeatureVector
    {
        public float MoveFrequency;      // How often the enemy moves per turn
        public float AggressionScore;     // Ratio of attacks to total actions
        public float PatrolRegularity;    // Regularity of movement pattern
        public float DistanceToPlayer;    // Average distance maintained from player
        public EnemyBehaviorType Label;   // Ground-truth label (for training examples)
    }
}
