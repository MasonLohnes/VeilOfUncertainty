// =============================================================================
// AIAdvisor.cs — Rational Agent / AI Advisor for Veil of Uncertainty
// Ties together the belief state, decision network, and VPI calculator
// to produce intelligent advisory recommendations each turn.
// The advisor is a rational agent selecting actions via MEU principle:
//   a* = argmax_a EU(a|e)
// with VPI-based meta-reasoning for information gathering.
// Corresponds to Implementation Step 6.
// =============================================================================

using UnityEngine;
using System.Collections.Generic;

namespace VeilOfUncertainty
{
    /// <summary>
    /// The AIAdvisor class implements a rational agent that recommends actions
    /// to the player. Each turn, the advisor:
    ///   (a) Queries the belief state for current evidence
    ///   (b) Runs MEU via the decision network to find the best action
    ///   (c) Evaluates VPI for adjacent cells via the VPI calculator
    ///   (d) Produces a recommendation: act immediately or scout first
    ///
    /// The player can follow or override the advice. This demonstrates
    /// rational agency with meta-reasoning about information gathering,
    /// directly implementing the concepts from the Topic 8 lecture.
    /// </summary>
    public class AIAdvisor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameConfig config;

        // AI subsystem references
        private DecisionNetwork decisionNetwork;
        private VPICalculator vpiCalculator;
        private BeliefState beliefState;
        private GridWorld gridWorld;
        private LinearRegressionModel difficultyPredictor;
        private KNNClassifier enemyClassifier;

        // Current recommendation
        private AdvisorRecommendation currentRecommendation;
        private List<VPIResult> currentVPIResults;
        private float currentDifficultyModifier = 1.0f;

        // Properties for UI access
        public AdvisorRecommendation CurrentRecommendation => currentRecommendation;
        public List<VPIResult> CurrentVPIResults => currentVPIResults;
        public float DifficultyModifier => currentDifficultyModifier;

        /// <summary>
        /// Initializes all AI subsystems and wires them together.
        /// </summary>
        public void Initialize(GridWorld world, GameConfig gameConfig)
        {
            gridWorld = world;
            config = gameConfig;

            // Initialize belief state
            beliefState = gameObject.AddComponent<BeliefState>();
            beliefState.Initialize(world);

            // Initialize decision network
            decisionNetwork = gameObject.AddComponent<DecisionNetwork>();
            decisionNetwork.Initialize(world, beliefState, config);

            // Initialize VPI calculator
            vpiCalculator = gameObject.AddComponent<VPICalculator>();
            vpiCalculator.Initialize(decisionNetwork, beliefState, world, config);

            // Initialize statistical learning models
            difficultyPredictor = gameObject.AddComponent<LinearRegressionModel>();
            difficultyPredictor.Initialize();

            enemyClassifier = gameObject.AddComponent<KNNClassifier>();
            enemyClassifier.Initialize(config.knnK);

            currentVPIResults = new List<VPIResult>();
        }

        /// <summary>
        /// Generates a complete advisory recommendation for the current turn.
        /// This is the main method called by the GameManager each turn.
        ///
        /// The advisor follows this decision loop:
        ///   1. Update difficulty prediction via linear regression
        ///   2. Classify any detected enemy behaviors via k-NN
        ///   3. Compute MEU for candidate actions via decision network
        ///   4. Compute VPI for all unobserved adjacent cells
        ///   5. Compare best VPI against scouting cost
        ///   6. Recommend: SCOUT (if VPI > cost) or ACT (if VPI <= cost)
        /// </summary>
        public AdvisorRecommendation GenerateRecommendation(
            int playerX, int playerY, int currentTurn,
            int playerHP, int scoutsRemaining, int score)
        {
            // ---- Step 1: Difficulty Prediction via Linear Regression ----
            float mapRevealed = gridWorld.GetRevealedFraction();
            float enemyRatio = gridWorld.TotalEnemies > 0
                ? (float)gridWorld.EnemiesRemaining / gridWorld.TotalEnemies
                : 0f;

            float predictedDifficulty = difficultyPredictor.Predict(
                currentTurn, mapRevealed, score, enemyRatio);

            // Convert prediction to a risk modifier for the utility function
            currentDifficultyModifier = config.baseDifficulty +
                predictedDifficulty * config.riskWeightMultiplier;
            currentDifficultyModifier = Mathf.Clamp(currentDifficultyModifier, 0.5f, 2.0f);

            // ---- Step 2: Enemy Behavior Classification via k-NN ----
            // Feed classification results as evidence into the decision network
            decisionNetwork.ClearEvidence();
            if (enemyClassifier.HasTrainingData())
            {
                EnemyBehaviorType? classifiedBehavior =
                    enemyClassifier.ClassifyNearestEnemy(playerX, playerY);
                if (classifiedBehavior.HasValue)
                {
                    decisionNetwork.SetEnemyBehaviorEvidence(classifiedBehavior.Value);
                }
            }

            // ---- Step 3: Compute MEU via Decision Network ----
            var (bestAction, meu) = decisionNetwork.ComputeMEU(
                playerX, playerY,
                difficultyModifier: currentDifficultyModifier);

            // ---- Step 4: Compute VPI for Adjacent Cells ----
            vpiCalculator.InvalidateCache();
            currentVPIResults = vpiCalculator.ComputeVPIForAdjacentCells(
                playerX, playerY, currentDifficultyModifier);

            // ---- Step 5: Compare VPI against Scouting Cost ----
            VPIResult? bestScoutTarget = vpiCalculator.GetBestScoutTarget();
            bool shouldScout = bestScoutTarget.HasValue && scoutsRemaining > 0;

            // ---- Step 6: Build Recommendation ----
            currentRecommendation = new AdvisorRecommendation
            {
                RecommendedAction = shouldScout ? PlayerAction.Scout : bestAction,
                MEU = meu,
                ShouldScout = shouldScout,
                ScoutTargetX = shouldScout ? bestScoutTarget.Value.CellX : -1,
                ScoutTargetY = shouldScout ? bestScoutTarget.Value.CellY : -1,
                ScoutVPI = shouldScout ? bestScoutTarget.Value.VPI : 0f,
                ScoutCost = config.scoutCost,
                Explanation = BuildExplanation(bestAction, meu, shouldScout,
                    bestScoutTarget, currentDifficultyModifier)
            };

            // ---- Log training data for difficulty predictor ----
            // Each turn becomes a training example for future predictions
            float actualDifficulty = ComputeActualDifficulty(playerHP, scoutsRemaining, enemyRatio);
            difficultyPredictor.AddTrainingExample(
                currentTurn, mapRevealed, score, enemyRatio, actualDifficulty);

            return currentRecommendation;
        }

        /// <summary>
        /// Processes a sensor observation and updates the belief state.
        /// Called when the player receives a noisy sensor reading.
        /// </summary>
        public void ProcessObservation(int cellX, int cellY,
                                        SensorReading reading, int currentTurn)
        {
            beliefState.UpdateBelief(cellX, cellY, reading, currentTurn);
            vpiCalculator.InvalidateCache();
        }

        /// <summary>
        /// Processes a scout result (full reveal of a cell).
        /// </summary>
        public void ProcessScoutResult(int cellX, int cellY, CellState revealedState)
        {
            beliefState.RevealCell(cellX, cellY, revealedState);
            gridWorld.RevealCell(cellX, cellY, fullReveal: false);
            vpiCalculator.InvalidateCache();
        }

        /// <summary>
        /// Processes the player moving onto a cell (full reveal).
        /// </summary>
        public void ProcessCellReveal(int cellX, int cellY, CellState revealedState)
        {
            beliefState.RevealCell(cellX, cellY, revealedState);
            gridWorld.RevealCell(cellX, cellY, fullReveal: true);
            vpiCalculator.InvalidateCache();
        }

        /// <summary>
        /// Records an enemy behavior observation for the k-NN classifier.
        /// </summary>
        public void RecordEnemyBehavior(EnemyFeatureVector features)
        {
            enemyClassifier.AddTrainingExample(features);
        }

        /// <summary>
        /// Provides auto-generated sensor observations for cells adjacent to
        /// the player's position. Called at the start of each turn.
        /// </summary>
        public void GenerateAdjacentObservations(int playerX, int playerY, int currentTurn)
        {
            List<Vector2Int> neighbors = gridWorld.GetNeighbors(playerX, playerY, 1);
            foreach (Vector2Int neighbor in neighbors)
            {
                if (!gridWorld.IsCellRevealed(neighbor.x, neighbor.y))
                {
                    SensorReading reading = gridWorld.GenerateObservation(neighbor.x, neighbor.y);
                    beliefState.UpdateBelief(neighbor.x, neighbor.y, reading, currentTurn);
                }
            }
        }

        // =====================================================================
        // Helper Methods
        // =====================================================================

        /// <summary>
        /// Builds a human-readable explanation of the recommendation.
        /// </summary>
        private string BuildExplanation(PlayerAction bestAction, float meu,
            bool shouldScout, VPIResult? scoutTarget, float diffMod)
        {
            string explanation = "";

            if (shouldScout && scoutTarget.HasValue)
            {
                explanation = $"SCOUT cell ({scoutTarget.Value.CellX},{scoutTarget.Value.CellY}). " +
                    $"VPI={scoutTarget.Value.VPI:F1} exceeds cost={config.scoutCost:F1}. " +
                    $"Net gain={scoutTarget.Value.NetVPI:F1}. " +
                    $"Information is more valuable than acting now.";
            }
            else
            {
                explanation = $"{bestAction} recommended. MEU={meu:F1}. ";

                switch (bestAction)
                {
                    case PlayerAction.Attack:
                        explanation += "High probability of enemy in target area.";
                        break;
                    case PlayerAction.Move:
                        explanation += "Path appears safe — expected positive utility.";
                        break;
                    case PlayerAction.Defend:
                        explanation += "High uncertainty or danger — defend to minimize risk.";
                        break;
                    case PlayerAction.Scout:
                        explanation += "Gathering information is optimal.";
                        break;
                }

                if (diffMod > 1.3f)
                    explanation += " [Caution: difficulty is predicted to increase.]";
            }

            return explanation;
        }

        /// <summary>
        /// Computes an empirical difficulty measure from current game state.
        /// Used as the target variable for linear regression training.
        /// </summary>
        private float ComputeActualDifficulty(int playerHP, int scoutsRemaining,
                                               float enemyRatio)
        {
            // Difficulty increases with fewer HP, fewer scouts, more enemies remaining
            float hpFactor = 1f - (playerHP / (float)config.startingHP);
            float scoutFactor = 1f - (scoutsRemaining / (float)config.startingScouts);
            float enemyFactor = enemyRatio;

            return (hpFactor + scoutFactor + enemyFactor) / 3f;
        }

        /// <summary>
        /// Provides access to the belief state for UI rendering.
        /// </summary>
        public BeliefState GetBeliefState() => beliefState;

        /// <summary>
        /// Provides access to the decision network for UI rendering.
        /// </summary>
        public DecisionNetwork GetDecisionNetwork() => decisionNetwork;

        /// <summary>
        /// Provides access to the VPI calculator for UI rendering.
        /// </summary>
        public VPICalculator GetVPICalculator() => vpiCalculator;
    }
}
