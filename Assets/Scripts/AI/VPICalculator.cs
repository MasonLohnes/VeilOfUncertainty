// =============================================================================
// VPICalculator.cs — Value of Perfect Information Engine for Veil of Uncertainty
// Implements VPI computation as defined in the Topic 8 lecture:
//   VPI(E'_j | e) = Sum_{e'_j} P(e'_j | e) * MEU(e, e'_j) - MEU(e)
// Key properties: VPI >= 0 (nonnegative), nonadditive, order-independent.
// Compares VPI against scouting cost to recommend information gathering.
// Corresponds to Implementation Step 5.
// =============================================================================

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VeilOfUncertainty
{
    /// <summary>
    /// VPICalculator computes the Value of Perfect Information for each unobserved
    /// variable adjacent to the player. This directly implements the lecture formula:
    ///
    ///   VPI(E'_j | e) = E_{e'_j}[MEU(e, e'_j)] - MEU(e)
    ///                  = Sum_{e'_j} P(e'_j | e) * MEU(e, e'_j) - MEU(e)
    ///
    /// The system compares VPI against the scouting cost and produces a ranked
    /// list of scouting recommendations. This mirrors the oil-drilling-rights
    /// and weather-forecast examples from the lecture.
    /// </summary>
    public class VPICalculator : MonoBehaviour
    {
        private DecisionNetwork decisionNetwork;
        private BeliefState beliefState;
        private GridWorld gridWorld;
        private GameConfig config;

        // Cached results for efficiency
        private List<VPIResult> cachedResults = new List<VPIResult>();
        private float cachedBaseMEU;
        private bool resultsValid;

        // Properties for UI display
        public List<VPIResult> LatestResults => cachedResults;
        public float BaseMEU => cachedBaseMEU;

        /// <summary>
        /// Initializes the VPI calculator with references to game systems.
        /// </summary>
        public void Initialize(DecisionNetwork network, BeliefState belief,
                               GridWorld world, GameConfig gameConfig)
        {
            decisionNetwork = network;
            beliefState = belief;
            gridWorld = world;
            config = gameConfig;
            resultsValid = false;
        }

        /// <summary>
        /// Computes VPI for all unobserved variables adjacent to the player.
        /// This is the main computation called each turn.
        ///
        /// For each unobserved cell E'_j within scouting range:
        ///   1. Get the current belief P(e'_j | e) from the belief state
        ///   2. For each possible value v of E'_j:
        ///      - Compute MEU(e, E'_j = v) — the MEU if we knew E'_j = v
        ///   3. VPI(E'_j) = Sum_v P(E'_j = v | e) * MEU(e, E'_j = v) - MEU(e)
        ///   4. NetVPI = VPI - scoutCost
        ///
        /// Returns results sorted by NetVPI (descending).
        /// </summary>
        public List<VPIResult> ComputeVPIForAdjacentCells(
            int playerX, int playerY, float difficultyModifier = 1.0f)
        {
            cachedResults.Clear();

            // Get the base MEU without any additional information
            var (_, baseMEU) = decisionNetwork.ComputeMEU(
                playerX, playerY, difficultyModifier: difficultyModifier);
            cachedBaseMEU = baseMEU;

            // Get all unrevealed cells within scouting range
            List<Vector2Int> candidates = gridWorld.GetUnrevealedNeighbors(
                playerX, playerY, config.scoutRange);

            // The four possible states an unobserved cell could have
            CellState[] possibleStates = {
                CellState.Empty, CellState.Enemy,
                CellState.Trap, CellState.Resource
            };

            foreach (Vector2Int cell in candidates)
            {
                float vpi = 0f;

                // VPI(E'_j | e) = Sum_{e'_j} P(e'_j | e) * MEU(e, e'_j) - MEU(e)
                foreach (CellState hypotheticalState in possibleStates)
                {
                    // P(E'_j = v | e) from the current belief state
                    float probOfState = beliefState.GetMarginalProbability(
                        cell.x, cell.y, hypotheticalState);

                    // MEU(e, E'_j = v) — the MEU if we perfectly knew this cell's state
                    float meuWithInfo = decisionNetwork.ComputeMEUWithAdditionalEvidence(
                        cell.x, cell.y, hypotheticalState, difficultyModifier);

                    vpi += probOfState * meuWithInfo;
                }

                // Subtract the base MEU: VPI = E[MEU with info] - MEU without info
                vpi -= baseMEU;

                // VPI is always nonnegative (lecture property)
                // Small negative values can occur due to floating point; clamp to 0
                vpi = Mathf.Max(0f, vpi);

                // Net VPI = VPI - scouting cost
                float netVPI = vpi - config.scoutCost;

                cachedResults.Add(new VPIResult
                {
                    CellX = cell.x,
                    CellY = cell.y,
                    VPI = vpi,
                    NetVPI = netVPI
                });
            }

            // Sort by Net VPI descending — highest information value first
            cachedResults.Sort((a, b) => b.NetVPI.CompareTo(a.NetVPI));
            resultsValid = true;

            return cachedResults;
        }

        /// <summary>
        /// Returns the best scouting recommendation: the cell with the highest
        /// positive Net VPI (VPI exceeds scouting cost).
        /// Returns null if no cell is worth scouting.
        /// </summary>
        public VPIResult? GetBestScoutTarget()
        {
            if (cachedResults.Count == 0 || !resultsValid) return null;

            VPIResult best = cachedResults[0]; // Already sorted by NetVPI
            if (best.NetVPI > 0f)
                return best;

            return null; // No cell worth scouting
        }

        /// <summary>
        /// Determines whether the player should scout before acting,
        /// based on whether any cell has positive Net VPI.
        /// This is the key decision from the lecture: gather information or act?
        /// </summary>
        public bool ShouldScout()
        {
            return GetBestScoutTarget().HasValue;
        }

        /// <summary>
        /// Returns all cells with positive Net VPI, ranked by value.
        /// These are all cells where scouting provides more expected value
        /// than it costs — multiple targets for players with multiple scouts.
        /// </summary>
        public List<VPIResult> GetWorthwhileScoutTargets()
        {
            return cachedResults.Where(r => r.NetVPI > 0f).ToList();
        }

        /// <summary>
        /// Computes VPI for a single specific cell. Used for targeted queries
        /// and for updating the VPI display when the player hovers over a cell.
        /// </summary>
        public float ComputeVPIForCell(int cellX, int cellY,
                                        int playerX, int playerY,
                                        float difficultyModifier = 1.0f)
        {
            if (gridWorld.IsCellRevealed(cellX, cellY)) return 0f;

            var (_, baseMEU) = decisionNetwork.ComputeMEU(
                playerX, playerY, difficultyModifier: difficultyModifier);

            CellState[] possibleStates = {
                CellState.Empty, CellState.Enemy,
                CellState.Trap, CellState.Resource
            };

            float vpi = 0f;
            foreach (CellState state in possibleStates)
            {
                float prob = beliefState.GetMarginalProbability(cellX, cellY, state);
                float meuWithInfo = decisionNetwork.ComputeMEUWithAdditionalEvidence(
                    cellX, cellY, state, difficultyModifier);
                vpi += prob * meuWithInfo;
            }
            vpi -= baseMEU;

            return Mathf.Max(0f, vpi);
        }

        /// <summary>
        /// Invalidates cached results (call when the game state changes).
        /// </summary>
        public void InvalidateCache()
        {
            resultsValid = false;
        }
    }
}
