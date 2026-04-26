// =============================================================================
// BeliefState.cs — POMDP Belief-State Manager for Veil of Uncertainty
// Maintains a probability distribution b(s) over cell contents for each grid
// cell. Implements Bayesian belief update:
//   b'(s') = alpha * P(o|s') * Sum_s P(s'|s,a) * b(s)
// where alpha is a normalizing constant.
// Corresponds to Implementation Step 3.
// =============================================================================

using UnityEngine;
using System.Collections.Generic;

namespace VeilOfUncertainty
{
    /// <summary>
    /// Per-cell belief: a probability distribution over the four possible
    /// hidden states {Enemy, Trap, Resource, Empty}. The POMDP operates
    /// over these belief states rather than the true hidden states.
    /// </summary>
    [System.Serializable]
    public class CellBelief
    {
        public float ProbEnemy;
        public float ProbTrap;
        public float ProbResource;
        public float ProbEmpty;

        /// <summary>
        /// Creates a uniform prior over all cell states (maximum uncertainty).
        /// </summary>
        public CellBelief(float pEnemy = 0.15f, float pTrap = 0.10f,
                          float pResource = 0.10f)
        {
            ProbEnemy = pEnemy;
            ProbTrap = pTrap;
            ProbResource = pResource;
            ProbEmpty = 1f - pEnemy - pTrap - pResource;
        }

        /// <summary>
        /// Returns the probability assigned to a given state.
        /// </summary>
        public float GetProbability(CellState state)
        {
            switch (state)
            {
                case CellState.Enemy:    return ProbEnemy;
                case CellState.Trap:     return ProbTrap;
                case CellState.Resource: return ProbResource;
                case CellState.Empty:    return ProbEmpty;
                default:                 return 0f;
            }
        }

        /// <summary>
        /// Sets the probability for a given state (used during belief update).
        /// </summary>
        public void SetProbability(CellState state, float value)
        {
            switch (state)
            {
                case CellState.Enemy:    ProbEnemy = value; break;
                case CellState.Trap:     ProbTrap = value; break;
                case CellState.Resource: ProbResource = value; break;
                case CellState.Empty:    ProbEmpty = value; break;
            }
        }

        /// <summary>
        /// Returns the most likely state (MAP estimate).
        /// </summary>
        public CellState MostLikelyState()
        {
            float max = ProbEmpty;
            CellState best = CellState.Empty;

            if (ProbEnemy > max) { max = ProbEnemy; best = CellState.Enemy; }
            if (ProbTrap > max)  { max = ProbTrap;  best = CellState.Trap; }
            if (ProbResource > max) { best = CellState.Resource; }

            return best;
        }

        /// <summary>
        /// Normalizes the belief distribution so probabilities sum to 1.
        /// This is the normalization constant alpha in the Bayesian update.
        /// </summary>
        public void Normalize()
        {
            float sum = ProbEnemy + ProbTrap + ProbResource + ProbEmpty;
            if (sum > 0f)
            {
                ProbEnemy /= sum;
                ProbTrap /= sum;
                ProbResource /= sum;
                ProbEmpty /= sum;
            }
            else
            {
                // Fallback to uniform if all zero (shouldn't happen)
                ProbEnemy = ProbTrap = ProbResource = ProbEmpty = 0.25f;
            }
        }

        /// <summary>
        /// Sets this cell to a known state (probability 1.0 for the revealed state).
        /// Called when a cell is fully revealed.
        /// </summary>
        public void SetKnown(CellState knownState)
        {
            ProbEnemy = 0f;
            ProbTrap = 0f;
            ProbResource = 0f;
            ProbEmpty = 0f;
            SetProbability(knownState, 1f);
        }

        /// <summary>
        /// Shannon entropy of the belief distribution. Higher entropy means more
        /// uncertainty. Used to visualize uncertainty on the fog-of-war overlay.
        /// </summary>
        public float Entropy()
        {
            float h = 0f;
            float[] probs = { ProbEnemy, ProbTrap, ProbResource, ProbEmpty };
            foreach (float p in probs)
            {
                if (p > 0f)
                    h -= p * Mathf.Log(p, 2f);
            }
            return h;
        }

        public override string ToString()
        {
            return $"E:{ProbEnemy:F2} T:{ProbTrap:F2} R:{ProbResource:F2} _:{ProbEmpty:F2}";
        }
    }

    /// <summary>
    /// BeliefState maintains the full POMDP belief state: a per-cell probability
    /// distribution over possible cell contents. After each observation, the belief
    /// is updated via Bayesian filtering:
    ///   b'(s') = alpha * P(o|s') * Sum_s P(s'|s,a) * b(s)
    /// This is the standard POMDP belief update discussed in the lecture.
    /// </summary>
    public class BeliefState : MonoBehaviour
    {
        private CellBelief[,] beliefs;
        private GridWorld gridWorld;
        private int width;
        private int height;

        // Prior probabilities (matching GridWorld generation parameters)
        private float priorEnemy = 0.15f;
        private float priorTrap = 0.10f;
        private float priorResource = 0.10f;

        // History of observations for learning
        private List<ObservationRecord> observationHistory = new List<ObservationRecord>();

        public struct ObservationRecord
        {
            public int X, Y;
            public SensorReading Reading;
            public int Turn;
        }

        /// <summary>
        /// Initializes belief state with prior distributions for all cells.
        /// Revealed cells get certainty beliefs.
        /// </summary>
        public void Initialize(GridWorld world)
        {
            gridWorld = world;
            width = world.Width;
            height = world.Height;
            beliefs = new CellBelief[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridCell cell = world.GetCell(x, y);
                    if (cell.IsRevealed)
                    {
                        // Known cell — belief is certain
                        beliefs[x, y] = new CellBelief();
                        beliefs[x, y].SetKnown(cell.HiddenState);
                    }
                    else
                    {
                        // Unknown cell — use prior distribution
                        beliefs[x, y] = new CellBelief(priorEnemy, priorTrap, priorResource);
                    }
                }
            }
        }

        /// <summary>
        /// Bayesian belief update after receiving a noisy sensor observation.
        /// Implements: b'(s') = alpha * P(o|s') * b(s)
        /// For static cells (no transition), P(s'|s,a) is identity, simplifying to:
        ///   b'(s) = alpha * P(o|s) * b(s)
        /// This is the standard POMDP belief update from the lecture.
        /// </summary>
        public void UpdateBelief(int x, int y, SensorReading observation, int currentTurn = 0)
        {
            if (!gridWorld.IsValidCell(x, y)) return;

            // If already fully revealed, no update needed
            if (gridWorld.IsCellRevealed(x, y)) return;

            CellBelief belief = beliefs[x, y];

            // Apply Bayes' rule for each possible state:
            // b'(s) = P(o|s) * b(s)  (unnormalized)
            CellState[] states = { CellState.Enemy, CellState.Trap,
                                   CellState.Resource, CellState.Empty };

            foreach (CellState state in states)
            {
                float likelihood = gridWorld.GetObservationProbability(observation, state);
                float prior = belief.GetProbability(state);
                float posterior = likelihood * prior;
                belief.SetProbability(state, posterior);
            }

            // Normalize: apply the alpha normalization constant
            belief.Normalize();

            // Record the observation for history/learning
            observationHistory.Add(new ObservationRecord
            {
                X = x, Y = y,
                Reading = observation,
                Turn = currentTurn
            });
        }

        /// <summary>
        /// Marks a cell as fully known after the player reveals it
        /// (by moving onto it or after it is scouted and confirmed).
        /// </summary>
        public void RevealCell(int x, int y, CellState trueState)
        {
            if (!gridWorld.IsValidCell(x, y)) return;
            beliefs[x, y].SetKnown(trueState);
        }

        /// <summary>
        /// Returns the belief distribution for a specific cell.
        /// </summary>
        public CellBelief GetBelief(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return new CellBelief(0.25f, 0.25f, 0.25f); // Uniform
            return beliefs[x, y];
        }

        /// <summary>
        /// Returns the marginal probability that a cell contains a specific state.
        /// Used by the decision network for inference.
        /// </summary>
        public float GetMarginalProbability(int x, int y, CellState state)
        {
            return GetBelief(x, y).GetProbability(state);
        }

        /// <summary>
        /// Returns the probability of danger (enemy or trap) for a cell.
        /// Convenience method for the VPI panel display.
        /// </summary>
        public float GetDangerProbability(int x, int y)
        {
            CellBelief b = GetBelief(x, y);
            return b.ProbEnemy + b.ProbTrap;
        }

        /// <summary>
        /// Returns whether a cell's belief is still at the prior (no observations).
        /// </summary>
        public bool IsAtPrior(int x, int y)
        {
            CellBelief b = GetBelief(x, y);
            return Mathf.Approximately(b.ProbEnemy, priorEnemy) &&
                   Mathf.Approximately(b.ProbTrap, priorTrap);
        }

        /// <summary>
        /// Computes the expected state of a cell (for utility calculation).
        /// Returns the expected value using the belief distribution.
        /// </summary>
        public float ComputeExpectedUtility(int x, int y, PlayerAction action, GameConfig config)
        {
            CellBelief b = GetBelief(x, y);

            float eu = 0f;

            switch (action)
            {
                case PlayerAction.Attack:
                    eu += b.ProbEnemy * config.utilityNeutralizeEnemy;
                    eu += b.ProbTrap * config.utilityTrapDamage;
                    eu += b.ProbResource * config.utilityWastedAttack;
                    eu += b.ProbEmpty * config.utilityWastedAttack;
                    break;

                case PlayerAction.Move:
                    eu += b.ProbEnemy * config.utilityEnemyDamage;
                    eu += b.ProbTrap * config.utilityTrapDamage;
                    eu += b.ProbResource * config.utilityCollectResource;
                    eu += b.ProbEmpty * config.utilitySafeMove;
                    break;

                case PlayerAction.Defend:
                    eu += b.ProbEnemy * config.utilityDefendAgainstEnemy;
                    eu += b.ProbTrap * config.utilityDefend;
                    eu += b.ProbResource * config.utilityDefend;
                    eu += b.ProbEmpty * config.utilityDefend;
                    break;

                case PlayerAction.Scout:
                    eu += config.utilityScoutCost;
                    break;
            }

            return eu;
        }

        /// <summary>
        /// Returns the observation history for statistical learning.
        /// </summary>
        public List<ObservationRecord> GetObservationHistory()
        {
            return observationHistory;
        }

        /// <summary>
        /// Returns the total number of unrevealed cells on the grid.
        /// </summary>
        public int GetUnrevealedCount()
        {
            int count = 0;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (!gridWorld.IsCellRevealed(x, y))
                        count++;
            return count;
        }
    }
}
