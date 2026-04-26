// =============================================================================
// LinearRegressionModel.cs — Linear Regression Difficulty Predictor
// Implements ordinary least squares (OLS) linear regression with incremental
// updates to predict the expected difficulty of upcoming turns.
//
// Features: turn number, map revealed fraction, cumulative score, enemies remaining.
// Target: difficulty scalar that modifies the utility function's risk assessment.
//
// This satisfies the Topic 8 objective: "Simulate intelligent decision making
// using linear regression." The prediction directly modifies the utility function,
// causing the AI advisor to become more conservative when difficulty increases.
// Corresponds to Implementation Step 7.
// =============================================================================

using UnityEngine;
using System.Collections.Generic;

namespace VeilOfUncertainty
{
    /// <summary>
    /// A lightweight linear regression model that predicts game difficulty from
    /// gameplay features. Uses ordinary least squares with incremental updates.
    ///
    /// Model: difficulty = w0 + w1*turn + w2*mapRevealed + w3*score + w4*enemyRatio
    ///
    /// The prediction feeds into the utility function as a risk modifier:
    /// when predicted difficulty is high, the rational agent shifts toward
    /// conservative actions (Defend, Scout) over aggressive ones (Attack, Move).
    /// </summary>
    public class LinearRegressionModel : MonoBehaviour
    {
        // Number of features (excluding bias term)
        private const int NUM_FEATURES = 4;

        // Weight vector: [bias, turn, mapRevealed, score, enemyRatio]
        private float[] weights;

        // Training data accumulated over the game session
        private List<float[]> trainingFeatures;
        private List<float> trainingTargets;

        // Learning configuration
        private float learningRate = 0.01f;
        private int minSamplesForPrediction = 3;
        private bool isTrained;

        /// <summary>
        /// Initializes the model with zero weights and empty training data.
        /// </summary>
        public void Initialize()
        {
            weights = new float[NUM_FEATURES + 1]; // +1 for bias
            trainingFeatures = new List<float[]>();
            trainingTargets = new List<float>();
            isTrained = false;

            // Set reasonable initial weights as a prior
            // (so the model gives sensible outputs before training)
            weights[0] = 0.3f;  // bias — baseline difficulty
            weights[1] = 0.01f; // turn — difficulty increases over time
            weights[2] = -0.2f; // mapRevealed — more info = less difficulty
            weights[3] = -0.001f; // score — higher score = better position
            weights[4] = 0.5f;  // enemyRatio — more enemies = harder
        }

        /// <summary>
        /// Adds a training example from the current turn's game state.
        /// Features are normalized to [0, 1] range for numerical stability.
        /// </summary>
        public void AddTrainingExample(int turn, float mapRevealed,
                                        int score, float enemyRatio,
                                        float actualDifficulty)
        {
            float[] features = NormalizeFeatures(turn, mapRevealed, score, enemyRatio);
            trainingFeatures.Add(features);
            trainingTargets.Add(Mathf.Clamp01(actualDifficulty));

            // Retrain with all accumulated data using OLS
            if (trainingFeatures.Count >= minSamplesForPrediction)
            {
                TrainOLS();
            }
        }

        /// <summary>
        /// Predicts difficulty for the current game state.
        /// Returns a value in [0, 1] where 0 = easy, 1 = very difficult.
        /// </summary>
        public float Predict(int turn, float mapRevealed, int score, float enemyRatio)
        {
            float[] features = NormalizeFeatures(turn, mapRevealed, score, enemyRatio);

            // Linear prediction: y = w0 + w1*x1 + w2*x2 + ... + wn*xn
            float prediction = weights[0]; // Bias term
            for (int i = 0; i < NUM_FEATURES; i++)
            {
                prediction += weights[i + 1] * features[i];
            }

            return Mathf.Clamp01(prediction);
        }

        /// <summary>
        /// Trains the model using Ordinary Least Squares (OLS).
        /// Solves the normal equation: w = (X^T X)^{-1} X^T y
        /// For small feature spaces (4 features), this is tractable and exact.
        /// </summary>
        private void TrainOLS()
        {
            int n = trainingFeatures.Count;
            int p = NUM_FEATURES + 1; // +1 for bias

            // Build design matrix X (n x p) with bias column
            float[,] X = new float[n, p];
            float[] y = new float[n];

            for (int i = 0; i < n; i++)
            {
                X[i, 0] = 1f; // Bias term
                for (int j = 0; j < NUM_FEATURES; j++)
                {
                    X[i, j + 1] = trainingFeatures[i][j];
                }
                y[i] = trainingTargets[i];
            }

            // Compute X^T X (p x p matrix)
            float[,] XtX = new float[p, p];
            for (int i = 0; i < p; i++)
            {
                for (int j = 0; j < p; j++)
                {
                    float sum = 0f;
                    for (int k = 0; k < n; k++)
                        sum += X[k, i] * X[k, j];
                    XtX[i, j] = sum;
                }
            }

            // Add ridge regularization for numerical stability: (X^T X + lambda*I)
            float lambda = 0.01f;
            for (int i = 0; i < p; i++)
                XtX[i, i] += lambda;

            // Compute X^T y (p-vector)
            float[] Xty = new float[p];
            for (int i = 0; i < p; i++)
            {
                float sum = 0f;
                for (int k = 0; k < n; k++)
                    sum += X[k, i] * y[k];
                Xty[i] = sum;
            }

            // Solve (X^T X) w = X^T y using Gaussian elimination
            float[] solution = SolveLinearSystem(XtX, Xty, p);
            if (solution != null)
            {
                weights = solution;
                isTrained = true;
            }
        }

        /// <summary>
        /// Solves Ax = b using Gaussian elimination with partial pivoting.
        /// </summary>
        private float[] SolveLinearSystem(float[,] A, float[] b, int n)
        {
            // Augmented matrix [A | b]
            float[,] aug = new float[n, n + 1];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    aug[i, j] = A[i, j];
                aug[i, n] = b[i];
            }

            // Forward elimination with partial pivoting
            for (int col = 0; col < n; col++)
            {
                // Find pivot
                int maxRow = col;
                float maxVal = Mathf.Abs(aug[col, col]);
                for (int row = col + 1; row < n; row++)
                {
                    if (Mathf.Abs(aug[row, col]) > maxVal)
                    {
                        maxVal = Mathf.Abs(aug[row, col]);
                        maxRow = row;
                    }
                }

                // Swap rows
                if (maxRow != col)
                {
                    for (int j = 0; j <= n; j++)
                    {
                        float temp = aug[col, j];
                        aug[col, j] = aug[maxRow, j];
                        aug[maxRow, j] = temp;
                    }
                }

                // Check for singular matrix
                if (Mathf.Abs(aug[col, col]) < 1e-10f)
                    return null;

                // Eliminate below
                for (int row = col + 1; row < n; row++)
                {
                    float factor = aug[row, col] / aug[col, col];
                    for (int j = col; j <= n; j++)
                        aug[row, j] -= factor * aug[col, j];
                }
            }

            // Back substitution
            float[] x = new float[n];
            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = aug[i, n];
                for (int j = i + 1; j < n; j++)
                    x[i] -= aug[i, j] * x[j];
                x[i] /= aug[i, i];
            }

            return x;
        }

        /// <summary>
        /// Normalizes raw features to [0, 1] range for stable regression.
        /// </summary>
        private float[] NormalizeFeatures(int turn, float mapRevealed,
                                           int score, float enemyRatio)
        {
            return new float[]
            {
                Mathf.Clamp01(turn / 50f),           // Normalize turn to ~50 turns
                Mathf.Clamp01(mapRevealed),            // Already in [0,1]
                Mathf.Clamp01(score / 500f),           // Normalize score to ~500 max
                Mathf.Clamp01(enemyRatio)              // Already in [0,1]
            };
        }

        /// <summary>
        /// Returns the current model weights for debugging/display.
        /// </summary>
        public float[] GetWeights() => (float[])weights.Clone();

        /// <summary>
        /// Returns whether the model has been trained on enough data.
        /// </summary>
        public bool IsTrained() => isTrained;

        /// <summary>
        /// Returns the number of training examples collected.
        /// </summary>
        public int TrainingExampleCount => trainingFeatures.Count;
    }
}
