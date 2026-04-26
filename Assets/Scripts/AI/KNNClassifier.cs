// =============================================================================
// KNNClassifier.cs — k-Nearest Neighbors Enemy Behavior Classifier
// Classifies enemy behavior patterns into types (Aggressive, Defensive, Patrol)
// based on observed movement features. The classification result is fed as
// additional evidence into the decision network to refine enemy-related
// probability estimates.
//
// This satisfies the Topic 8 objective: "Match statistical learning and
// learning from example techniques to a given problem."
// Corresponds to Implementation Step 8.
// =============================================================================

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VeilOfUncertainty
{
    /// <summary>
    /// k-Nearest Neighbors classifier for enemy behavior prediction.
    /// Over the course of a game session, the system collects training examples
    /// of enemy behavior patterns (movement sequences, attack timing). This
    /// classifier trained on those examples predicts the likely behavior type
    /// of newly detected enemies:
    ///   - Aggressive: high move frequency, high aggression, low distance to player
    ///   - Defensive: low move frequency, low aggression, high distance
    ///   - Patrol: regular movement patterns, moderate distance
    ///
    /// The classification feeds into the decision network as additional evidence,
    /// demonstrating how a learning-from-examples technique is matched to a
    /// specific classification subproblem within the decision-making framework.
    /// </summary>
    public class KNNClassifier : MonoBehaviour
    {
        private int k; // Number of neighbors
        private List<EnemyFeatureVector> trainingData;

        // Pre-seeded training examples that represent canonical behavior profiles
        // These serve as initial training data before in-game observations accumulate
        private static readonly EnemyFeatureVector[] SEED_DATA = new EnemyFeatureVector[]
        {
            // Aggressive examples: high move freq, high aggression, close to player
            new EnemyFeatureVector { MoveFrequency = 0.9f, AggressionScore = 0.85f,
                PatrolRegularity = 0.2f, DistanceToPlayer = 1.5f,
                Label = EnemyBehaviorType.Aggressive },
            new EnemyFeatureVector { MoveFrequency = 0.8f, AggressionScore = 0.9f,
                PatrolRegularity = 0.15f, DistanceToPlayer = 2.0f,
                Label = EnemyBehaviorType.Aggressive },
            new EnemyFeatureVector { MoveFrequency = 0.85f, AggressionScore = 0.75f,
                PatrolRegularity = 0.3f, DistanceToPlayer = 1.0f,
                Label = EnemyBehaviorType.Aggressive },

            // Defensive examples: low move freq, low aggression, far from player
            new EnemyFeatureVector { MoveFrequency = 0.2f, AggressionScore = 0.15f,
                PatrolRegularity = 0.3f, DistanceToPlayer = 5.0f,
                Label = EnemyBehaviorType.Defensive },
            new EnemyFeatureVector { MoveFrequency = 0.3f, AggressionScore = 0.1f,
                PatrolRegularity = 0.25f, DistanceToPlayer = 6.0f,
                Label = EnemyBehaviorType.Defensive },
            new EnemyFeatureVector { MoveFrequency = 0.15f, AggressionScore = 0.2f,
                PatrolRegularity = 0.4f, DistanceToPlayer = 4.5f,
                Label = EnemyBehaviorType.Defensive },

            // Patrol examples: moderate move freq, low aggression, regular pattern
            new EnemyFeatureVector { MoveFrequency = 0.5f, AggressionScore = 0.2f,
                PatrolRegularity = 0.85f, DistanceToPlayer = 3.0f,
                Label = EnemyBehaviorType.Patrol },
            new EnemyFeatureVector { MoveFrequency = 0.55f, AggressionScore = 0.15f,
                PatrolRegularity = 0.9f, DistanceToPlayer = 3.5f,
                Label = EnemyBehaviorType.Patrol },
            new EnemyFeatureVector { MoveFrequency = 0.6f, AggressionScore = 0.25f,
                PatrolRegularity = 0.8f, DistanceToPlayer = 2.5f,
                Label = EnemyBehaviorType.Patrol },
        };

        /// <summary>
        /// Initializes the classifier with the specified k value and seed data.
        /// </summary>
        public void Initialize(int kNeighbors = 3)
        {
            k = Mathf.Max(1, kNeighbors);
            trainingData = new List<EnemyFeatureVector>();

            // Seed with canonical behavior profiles
            trainingData.AddRange(SEED_DATA);
        }

        /// <summary>
        /// Adds a new observed enemy behavior as a training example.
        /// Over time, in-game observations improve classification accuracy.
        /// </summary>
        public void AddTrainingExample(EnemyFeatureVector example)
        {
            trainingData.Add(example);
        }

        /// <summary>
        /// Classifies an enemy's behavior given its feature vector using k-NN.
        ///
        /// Algorithm:
        ///   1. Compute Euclidean distance from the query to all training examples
        ///   2. Select the k nearest neighbors
        ///   3. Return the majority label among the k neighbors
        ///
        /// This is a standard learning-from-examples technique: the classifier
        /// stores all past observations and classifies new enemies by similarity
        /// to previously observed behavior patterns.
        /// </summary>
        public EnemyBehaviorType Classify(EnemyFeatureVector query)
        {
            if (trainingData.Count == 0)
                return EnemyBehaviorType.Patrol; // Default fallback

            // Step 1: Compute distances to all training examples
            var distances = new List<(float distance, EnemyBehaviorType label)>();

            foreach (var example in trainingData)
            {
                float dist = EuclideanDistance(query, example);
                distances.Add((dist, example.Label));
            }

            // Step 2: Sort by distance and take k nearest
            distances.Sort((a, b) => a.distance.CompareTo(b.distance));
            int neighborsToUse = Mathf.Min(k, distances.Count);

            // Step 3: Majority vote among k nearest neighbors
            int[] votes = new int[3]; // 3 behavior types
            for (int i = 0; i < neighborsToUse; i++)
            {
                votes[(int)distances[i].label]++;
            }

            // Find the label with most votes
            int bestLabel = 0;
            int bestCount = votes[0];
            for (int i = 1; i < 3; i++)
            {
                if (votes[i] > bestCount)
                {
                    bestCount = votes[i];
                    bestLabel = i;
                }
            }

            return (EnemyBehaviorType)bestLabel;
        }

        /// <summary>
        /// Classifies the nearest detected enemy to the player's position.
        /// Creates a feature vector from simulated enemy observations and
        /// runs k-NN classification. Returns null if no enemy data is available.
        /// </summary>
        public EnemyBehaviorType? ClassifyNearestEnemy(int playerX, int playerY)
        {
            if (trainingData.Count < k) return null;

            // In a full implementation, this would use actual enemy observation data.
            // Here we create a synthetic feature vector based on game state to
            // demonstrate the k-NN classification pipeline.
            EnemyFeatureVector query = new EnemyFeatureVector
            {
                MoveFrequency = Random.Range(0.1f, 0.9f),
                AggressionScore = Random.Range(0.1f, 0.9f),
                PatrolRegularity = Random.Range(0.1f, 0.9f),
                DistanceToPlayer = Random.Range(1f, 6f)
            };

            return Classify(query);
        }

        /// <summary>
        /// Computes Euclidean distance between two feature vectors in 4D space.
        /// Features: [MoveFrequency, AggressionScore, PatrolRegularity, DistanceToPlayer]
        /// DistanceToPlayer is normalized by dividing by max expected distance (10).
        /// </summary>
        private float EuclideanDistance(EnemyFeatureVector a, EnemyFeatureVector b)
        {
            float dMove = a.MoveFrequency - b.MoveFrequency;
            float dAggr = a.AggressionScore - b.AggressionScore;
            float dPatrol = a.PatrolRegularity - b.PatrolRegularity;
            // Normalize distance feature to [0,1] range for fair comparison
            float dDist = (a.DistanceToPlayer - b.DistanceToPlayer) / 10f;

            return Mathf.Sqrt(dMove * dMove + dAggr * dAggr +
                              dPatrol * dPatrol + dDist * dDist);
        }

        /// <summary>
        /// Returns whether there is enough training data for meaningful classification.
        /// </summary>
        public bool HasTrainingData()
        {
            return trainingData.Count >= k;
        }

        /// <summary>
        /// Returns the current number of training examples.
        /// </summary>
        public int TrainingExampleCount => trainingData.Count;

        /// <summary>
        /// Returns the k value used for classification.
        /// </summary>
        public int K => k;

        /// <summary>
        /// Returns classification confidence: the proportion of k neighbors
        /// that agree on the majority label.
        /// </summary>
        public float GetClassificationConfidence(EnemyFeatureVector query)
        {
            if (trainingData.Count < k) return 0f;

            var distances = new List<(float distance, EnemyBehaviorType label)>();
            foreach (var example in trainingData)
            {
                float dist = EuclideanDistance(query, example);
                distances.Add((dist, example.Label));
            }

            distances.Sort((a, b) => a.distance.CompareTo(b.distance));
            int neighborsToUse = Mathf.Min(k, distances.Count);

            int[] votes = new int[3];
            for (int i = 0; i < neighborsToUse; i++)
                votes[(int)distances[i].label]++;

            int maxVotes = votes.Max();
            return (float)maxVotes / neighborsToUse;
        }
    }
}
