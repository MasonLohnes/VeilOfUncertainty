// =============================================================================
// DecisionNetwork.cs — Decision Network (Influence Diagram) for Veil of Uncertainty
// Implements a Bayesian network augmented with action and utility nodes.
// Computes Maximum Expected Utility (MEU) using the five-step procedure
// from the Topic 8 lecture:
//   1. Instantiate all observed evidence
//   2. Set action node to each possible value
//   3. Compute posterior distribution over utility parents via Bayesian inference
//   4. Calculate EU(a|e) = Sum_s P(s|e) * U(s,a) for each action
//   5. Select a* = argmax_a EU(a|e)
// Corresponds to Implementation Step 4.
// =============================================================================

using UnityEngine;
using System.Collections.Generic;

namespace VeilOfUncertainty
{
    /// <summary>
    /// Represents a node in the decision network.
    /// The network contains chance nodes (EnemyLocation, TerrainType),
    /// observation nodes (SensorReading, ScoutReport), an action node
    /// (PlayerAction), and a utility node.
    /// </summary>
    public enum NodeType
    {
        Chance,      // Random variable (e.g., EnemyLocation, TerrainType)
        Observation, // Evidence node (e.g., SensorReading)
        Action,      // Decision node (PlayerAction)
        Utility      // Utility/payoff node
    }

    /// <summary>
    /// A single node in the decision network, storing its type, name,
    /// possible values, and current evidence (if observed).
    /// </summary>
    [System.Serializable]
    public class DecisionNode
    {
        public string Name;
        public NodeType Type;
        public int NumValues;     // Number of discrete values this node can take
        public bool IsObserved;
        public int ObservedValue; // Index of the observed value (if IsObserved)

        public DecisionNode(string name, NodeType type, int numValues)
        {
            Name = name;
            Type = type;
            NumValues = numValues;
            IsObserved = false;
            ObservedValue = -1;
        }
    }

    /// <summary>
    /// The DecisionNetwork class models the player's tactical decision problem
    /// as an influence diagram (decision network). Following the lecture's
    /// umbrella-weather example structure:
    ///
    /// Chance Nodes:
    ///   - EnemyLocation: probability distribution over cell states
    ///   - TerrainType: probability of terrain modifiers
    ///   - EnemyBehavior: enemy behavior classification (from k-NN)
    ///
    /// Observation Node:
    ///   - SensorReading: noisy observation from POMDP observation function
    ///
    /// Action Node:
    ///   - PlayerAction: {Scout, Move, Attack, Defend}
    ///
    /// Utility Node:
    ///   - Encodes payoffs based on the outcome of (state, action) pairs
    ///
    /// The MEU action-selection algorithm is implemented as the five-step
    /// procedure from the lecture.
    /// </summary>
    public class DecisionNetwork : MonoBehaviour
    {
        private GameConfig config;
        private BeliefState beliefState;
        private GridWorld gridWorld;

        // Network structure — nodes
        private DecisionNode enemyLocationNode;
        private DecisionNode terrainTypeNode;
        private DecisionNode enemyBehaviorNode;
        private DecisionNode sensorReadingNode;
        private DecisionNode actionNode;

        // Current evaluation state
        private float currentMEU;
        private PlayerAction currentBestAction;
        private Dictionary<PlayerAction, float> actionUtilities;

        // Properties for UI display
        public float CurrentMEU => currentMEU;
        public PlayerAction BestAction => currentBestAction;
        public Dictionary<PlayerAction, float> ActionUtilities => actionUtilities;

        /// <summary>
        /// Initializes the decision network with references to the game systems.
        /// Constructs the network topology: chance nodes -> observation node,
        /// chance nodes -> utility node, action node -> utility node.
        /// </summary>
        public void Initialize(GridWorld world, BeliefState belief, GameConfig gameConfig)
        {
            gridWorld = world;
            beliefState = belief;
            config = gameConfig;
            actionUtilities = new Dictionary<PlayerAction, float>();

            // Construct network nodes
            // EnemyLocation: 4 values (Enemy, Trap, Resource, Empty)
            enemyLocationNode = new DecisionNode("EnemyLocation", NodeType.Chance, 4);

            // TerrainType: 2 values (Normal, Hazardous)
            terrainTypeNode = new DecisionNode("TerrainType", NodeType.Chance, 2);

            // EnemyBehavior: 3 values (Aggressive, Defensive, Patrol)
            enemyBehaviorNode = new DecisionNode("EnemyBehavior", NodeType.Chance, 3);

            // SensorReading: 3 values (Safe, Danger, Unknown)
            sensorReadingNode = new DecisionNode("SensorReading", NodeType.Observation, 3);

            // Action: 4 values (Scout, Move, Attack, Defend)
            actionNode = new DecisionNode("PlayerAction", NodeType.Action, 4);
        }

        /// <summary>
        /// Computes the Maximum Expected Utility (MEU) for a given target cell,
        /// following the five-step lecture procedure:
        ///
        /// Step 1: Instantiate all observed evidence into the network
        /// Step 2: Set the action node to each possible value
        /// Step 3: Compute posterior over parents of utility node via Bayes
        /// Step 4: Calculate EU(a|e) = Sum_s P(s|e) * U(s,a) for each action
        /// Step 5: Select a* = argmax_a EU(a|e)
        /// </summary>
        /// <param name="targetX">X coordinate of the cell being evaluated</param>
        /// <param name="targetY">Y coordinate of the cell being evaluated</param>
        /// <param name="sensorEvidence">Current sensor reading for the target cell, or null</param>
        /// <param name="enemyBehavior">Classified enemy behavior, or null</param>
        /// <param name="difficultyModifier">Risk modifier from linear regression</param>
        /// <returns>The MEU value and best action</returns>
        public (PlayerAction bestAction, float meu) ComputeMEU(
            int targetX, int targetY,
            SensorReading? sensorEvidence = null,
            EnemyBehaviorType? enemyBehavior = null,
            float difficultyModifier = 1.0f)
        {
            actionUtilities.Clear();

            // ---- Step 1: Instantiate observed evidence ----
            CellBelief belief = beliefState.GetBelief(targetX, targetY);

            // Posterior over cell state given current belief (which already
            // incorporates all past sensor observations via Bayesian filtering)
            float[] posterior = new float[4];
            posterior[(int)CellState.Empty]    = belief.ProbEmpty;
            posterior[(int)CellState.Enemy]    = belief.ProbEnemy;
            posterior[(int)CellState.Trap]     = belief.ProbTrap;
            posterior[(int)CellState.Resource] = belief.ProbResource;

            // If we have a fresh sensor reading, further update via Bayes' rule
            if (sensorEvidence.HasValue)
            {
                posterior = UpdatePosteriorWithSensor(posterior, sensorEvidence.Value);
            }

            // If k-NN classified enemy behavior, adjust enemy-related probabilities
            if (enemyBehavior.HasValue)
            {
                posterior = AdjustForEnemyBehavior(posterior, enemyBehavior.Value);
            }

            // ---- Steps 2-4: Evaluate each action ----
            PlayerAction[] actions = {
                PlayerAction.Scout, PlayerAction.Move,
                PlayerAction.Attack, PlayerAction.Defend
            };

            float bestEU = float.NegativeInfinity;
            PlayerAction bestAction = PlayerAction.Defend;

            foreach (PlayerAction action in actions)
            {
                // Step 2: Set action node to this value
                // Step 3: Posterior is already computed above
                // Step 4: EU(a|e) = Sum_s P(s|e) * U(s,a)
                float eu = ComputeExpectedUtility(posterior, action, difficultyModifier);
                actionUtilities[action] = eu;

                // Step 5: Track the maximizing action
                if (eu > bestEU)
                {
                    bestEU = eu;
                    bestAction = action;
                }
            }

            currentMEU = bestEU;
            currentBestAction = bestAction;

            return (bestAction, bestEU);
        }

        /// <summary>
        /// Step 3 helper: Updates the posterior distribution with a new sensor reading
        /// using Bayes' rule. P(s|o) = alpha * P(o|s) * P(s)
        /// </summary>
        private float[] UpdatePosteriorWithSensor(float[] prior, SensorReading observation)
        {
            float[] updated = new float[4];
            float sum = 0f;

            CellState[] states = { CellState.Empty, CellState.Enemy,
                                   CellState.Trap, CellState.Resource };

            for (int i = 0; i < 4; i++)
            {
                float likelihood = gridWorld.GetObservationProbability(observation, states[i]);
                updated[i] = likelihood * prior[i];
                sum += updated[i];
            }

            // Normalize (alpha)
            if (sum > 0f)
            {
                for (int i = 0; i < 4; i++)
                    updated[i] /= sum;
            }

            return updated;
        }

        /// <summary>
        /// Adjusts posterior probabilities based on classified enemy behavior.
        /// Aggressive enemies are more likely to be in cells the player approaches;
        /// Defensive enemies cluster; Patrol enemies follow patterns.
        /// </summary>
        private float[] AdjustForEnemyBehavior(float[] posterior, EnemyBehaviorType behavior)
        {
            float[] adjusted = (float[])posterior.Clone();

            switch (behavior)
            {
                case EnemyBehaviorType.Aggressive:
                    // Aggressive enemies more likely to be nearby — boost enemy probability
                    adjusted[(int)CellState.Enemy] *= 1.3f;
                    break;
                case EnemyBehaviorType.Defensive:
                    // Defensive enemies less likely in open cells
                    adjusted[(int)CellState.Enemy] *= 0.8f;
                    break;
                case EnemyBehaviorType.Patrol:
                    // Patrol enemies — slight boost, they move predictably
                    adjusted[(int)CellState.Enemy] *= 1.1f;
                    break;
            }

            // Renormalize
            float sum = 0f;
            for (int i = 0; i < 4; i++) sum += adjusted[i];
            if (sum > 0f)
                for (int i = 0; i < 4; i++) adjusted[i] /= sum;

            return adjusted;
        }

        /// <summary>
        /// Step 4: Computes EU(a|e) = Sum_s P(s|e) * U(s,a) for a specific action.
        /// The utility function U(s,a) encodes payoffs from the GameConfig:
        ///   +50 for neutralizing an enemy, -30 for trap, -10 for scouting, etc.
        /// The difficultyModifier from linear regression adjusts risk weighting.
        /// </summary>
        private float ComputeExpectedUtility(float[] posterior, PlayerAction action,
                                              float difficultyModifier)
        {
            float eu = 0f;

            // U(s, a) for each (state, action) pair from the utility node
            switch (action)
            {
                case PlayerAction.Attack:
                    eu += posterior[(int)CellState.Enemy] * config.utilityNeutralizeEnemy;
                    eu += posterior[(int)CellState.Trap] * config.utilityTrapDamage * difficultyModifier;
                    eu += posterior[(int)CellState.Resource] * config.utilityWastedAttack;
                    eu += posterior[(int)CellState.Empty] * config.utilityWastedAttack;
                    break;

                case PlayerAction.Move:
                    eu += posterior[(int)CellState.Enemy] * config.utilityEnemyDamage * difficultyModifier;
                    eu += posterior[(int)CellState.Trap] * config.utilityTrapDamage * difficultyModifier;
                    eu += posterior[(int)CellState.Resource] * config.utilityCollectResource;
                    eu += posterior[(int)CellState.Empty] * config.utilitySafeMove;
                    break;

                case PlayerAction.Defend:
                    eu += posterior[(int)CellState.Enemy] * config.utilityDefendAgainstEnemy;
                    eu += posterior[(int)CellState.Trap] * config.utilityDefend;
                    eu += posterior[(int)CellState.Resource] * config.utilityDefend;
                    eu += posterior[(int)CellState.Empty] * config.utilityDefend;
                    break;

                case PlayerAction.Scout:
                    eu += config.utilityScoutCost;
                    break;
            }

            return eu;
        }

        /// <summary>
        /// Computes MEU for a hypothetical scenario where an additional variable
        /// is observed. Used by the VPI calculator.
        /// MEU(e, e'_j = v) for a specific value v of unobserved variable E'_j.
        /// </summary>
        public float ComputeMEUWithAdditionalEvidence(
            int targetX, int targetY,
            CellState hypotheticalState,
            float difficultyModifier = 1.0f)
        {
            // Create a belief where the hypothetical cell is known
            float[] hypotheticalPosterior = new float[4];
            hypotheticalPosterior[(int)hypotheticalState] = 1.0f;

            float bestEU = float.NegativeInfinity;
            PlayerAction[] actions = {
                PlayerAction.Scout, PlayerAction.Move,
                PlayerAction.Attack, PlayerAction.Defend
            };

            foreach (PlayerAction action in actions)
            {
                float eu = ComputeExpectedUtility(hypotheticalPosterior, action, difficultyModifier);
                if (eu > bestEU) bestEU = eu;
            }

            return bestEU;
        }

        /// <summary>
        /// Returns a description of the network structure for UI display.
        /// Lists all nodes, their types, and current evidence status.
        /// </summary>
        public List<(string name, NodeType type, bool observed, string value)> GetNetworkDescription()
        {
            var desc = new List<(string, NodeType, bool, string)>
            {
                ("EnemyLoc", NodeType.Chance, enemyLocationNode.IsObserved,
                    enemyLocationNode.IsObserved ? ((CellState)enemyLocationNode.ObservedValue).ToString() : "?"),
                ("Terrain", NodeType.Chance, terrainTypeNode.IsObserved,
                    terrainTypeNode.IsObserved ? (terrainTypeNode.ObservedValue == 0 ? "Normal" : "Hazardous") : "?"),
                ("EnemyBhv", NodeType.Chance, enemyBehaviorNode.IsObserved,
                    enemyBehaviorNode.IsObserved ? ((EnemyBehaviorType)enemyBehaviorNode.ObservedValue).ToString() : "?"),
                ("Sensor", NodeType.Observation, sensorReadingNode.IsObserved,
                    sensorReadingNode.IsObserved ? ((SensorReading)sensorReadingNode.ObservedValue).ToString() : "?"),
                ("Action", NodeType.Action, true, currentBestAction.ToString()),
                ("Utility", NodeType.Utility, true, $"MEU={currentMEU:F1}")
            };
            return desc;
        }

        /// <summary>
        /// Sets observed evidence for sensor reading (used when player receives a reading).
        /// </summary>
        public void SetSensorEvidence(SensorReading reading)
        {
            sensorReadingNode.IsObserved = true;
            sensorReadingNode.ObservedValue = (int)reading;
        }

        /// <summary>
        /// Sets observed evidence for enemy behavior (from k-NN classification).
        /// </summary>
        public void SetEnemyBehaviorEvidence(EnemyBehaviorType behavior)
        {
            enemyBehaviorNode.IsObserved = true;
            enemyBehaviorNode.ObservedValue = (int)behavior;
        }

        /// <summary>
        /// Clears all evidence for a fresh evaluation next turn.
        /// </summary>
        public void ClearEvidence()
        {
            enemyLocationNode.IsObserved = false;
            terrainTypeNode.IsObserved = false;
            enemyBehaviorNode.IsObserved = false;
            sensorReadingNode.IsObserved = false;
        }
    }
}
