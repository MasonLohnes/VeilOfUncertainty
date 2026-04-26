// =============================================================================
// GridWorld.cs — POMDP Grid World for Veil of Uncertainty
// Implements the NxN game board with hidden states, procedural generation,
// transition function P(s'|s,a), and observation function P(o|s).
// Corresponds to Implementation Step 2.
// =============================================================================

using UnityEngine;
using System.Collections.Generic;

namespace VeilOfUncertainty
{
    /// <summary>
    /// Represents a single cell on the grid with both hidden (true) state
    /// and visibility information for the fog-of-war system.
    /// </summary>
    [System.Serializable]
    public class GridCell
    {
        public int X;
        public int Y;
        public CellState HiddenState;
        public bool IsRevealed;
        public bool IsPartiallyRevealed; // Scouted but not visited
        public int EnemyID; // -1 if no enemy

        public GridCell(int x, int y, CellState state)
        {
            X = x;
            Y = y;
            HiddenState = state;
            IsRevealed = false;
            IsPartiallyRevealed = false;
            EnemyID = -1;
        }
    }

    /// <summary>
    /// The GridWorld class represents the NxN game board. Each cell has a hidden
    /// state (Enemy, Trap, Resource, Empty) generated procedurally. This class
    /// defines the POMDP state space S, action space A, transition function
    /// P(s'|s,a), and observation function P(o|s).
    /// </summary>
    public class GridWorld : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 10;

        [Header("Procedural Generation Probabilities")]
        [SerializeField] private float enemyProbability = 0.15f;
        [SerializeField] private float trapProbability = 0.10f;
        [SerializeField] private float resourceProbability = 0.10f;

        [Header("Observation Function Parameters")]
        [Tooltip("P(sensor=danger | Enemy) — true positive rate for enemies")]
        [SerializeField] private float enemyDetectionRate = 0.85f;
        [Tooltip("P(sensor=danger | Trap) — true positive rate for traps")]
        [SerializeField] private float trapDetectionRate = 0.75f;
        [Tooltip("P(sensor=danger | Empty) — false positive rate")]
        [SerializeField] private float falsePositiveRate = 0.10f;
        [Tooltip("P(sensor=danger | Resource) — false positive for resources")]
        [SerializeField] private float resourceFalsePositiveRate = 0.05f;

        [Header("Prefabs")]
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject trapPrefab;
        [SerializeField] private GameObject resourcePrefab;
        [SerializeField] private GameObject fogPrefab;

        // Internal state
        private GridCell[,] grid;
        private GameObject[,] cellObjects;
        private GameObject[,] fogObjects;
        private int totalEnemies;
        private int totalResources;
        private int enemiesRemaining;

        // Properties
        public int Width => gridWidth;
        public int Height => gridHeight;
        public int TotalEnemies => totalEnemies;
        public int EnemiesRemaining => enemiesRemaining;
        public int TotalResources => totalResources;
        public float EnemyDetectionRate => enemyDetectionRate;
        public float TrapDetectionRate => trapDetectionRate;
        public float FalsePositiveRate => falsePositiveRate;
        public float ResourceFalsePositiveRate => resourceFalsePositiveRate;

        /// <summary>
        /// Sets prefab references programmatically (called by SceneBuilder).
        /// </summary>
        public void SetPrefabs(GameObject cell, GameObject fog, GameObject enemy,
                               GameObject trap, GameObject resource)
        {
            cellPrefab = cell;
            fogPrefab = fog;
            enemyPrefab = enemy;
            trapPrefab = trap;
            resourcePrefab = resource;
        }

        /// <summary>
        /// Initializes and procedurally generates the grid world.
        /// </summary>
        public void Initialize(int? seed = null)
        {
            if (seed.HasValue)
                Random.InitState(seed.Value);

            grid = new GridCell[gridWidth, gridHeight];
            cellObjects = new GameObject[gridWidth, gridHeight];
            fogObjects = new GameObject[gridWidth, gridHeight];
            totalEnemies = 0;
            totalResources = 0;

            GenerateGrid();
            SpawnVisuals();

            enemiesRemaining = totalEnemies;
        }

        /// <summary>
        /// Procedurally generates hidden states for each cell using configured probabilities.
        /// The player's starting cell (0,0) is always empty.
        /// </summary>
        private void GenerateGrid()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    CellState state = GenerateCellState(x, y);
                    grid[x, y] = new GridCell(x, y, state);

                    if (state == CellState.Enemy) totalEnemies++;
                    if (state == CellState.Resource) totalResources++;
                }
            }

            // Ensure starting position is safe
            grid[0, 0].HiddenState = CellState.Empty;
            grid[0, 0].IsRevealed = true;
        }

        private CellState GenerateCellState(int x, int y)
        {
            // Starting area (0,0) and its neighbors are always safe
            if (x <= 1 && y <= 1) return CellState.Empty;

            float roll = Random.value;
            if (roll < enemyProbability) return CellState.Enemy;
            roll -= enemyProbability;
            if (roll < trapProbability) return CellState.Trap;
            roll -= trapProbability;
            if (roll < resourceProbability) return CellState.Resource;
            return CellState.Empty;
        }

        /// <summary>
        /// Instantiates 3D cell visuals and fog-of-war overlays.
        /// Uses a checkerboard pattern for visual clarity.
        /// </summary>
        private void SpawnVisuals()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 position = new Vector3(x * 2f, 0f, y * 2f);

                    // Spawn base cell tile
                    if (cellPrefab != null)
                    {
                        cellObjects[x, y] = Instantiate(cellPrefab, position, Quaternion.identity, transform);
                        cellObjects[x, y].name = $"Cell_{x}_{y}";
                        cellObjects[x, y].SetActive(true);

                        // Checkerboard coloring
                        var renderer = cellObjects[x, y].GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            bool isDark = (x + y) % 2 == 0;
                            Color cellColor = isDark
                                ? new Color(0.65f, 0.65f, 0.68f)
                                : new Color(0.78f, 0.78f, 0.82f);
                            renderer.material.color = cellColor;
                        }
                    }

                    // Spawn fog overlay (hidden initially for revealed cells)
                    if (fogPrefab != null)
                    {
                        Vector3 fogPos = position + Vector3.up * 0.1f;
                        fogObjects[x, y] = Instantiate(fogPrefab, fogPos,
                            Quaternion.Euler(90f, 0f, 0f), transform);
                        fogObjects[x, y].name = $"Fog_{x}_{y}";
                        fogObjects[x, y].SetActive(!grid[x, y].IsRevealed);
                    }
                }
            }
        }

        // =====================================================================
        // POMDP Functions
        // =====================================================================

        /// <summary>
        /// Observation function P(o | s): returns the probability of a sensor reading
        /// given the true hidden state of a cell. This is the core of the POMDP's
        /// observation model, directly implementing the noisy sensor from the proposal.
        /// </summary>
        public float GetObservationProbability(SensorReading observation, CellState trueState)
        {
            // P(sensor=danger | state)
            float pDanger;
            switch (trueState)
            {
                case CellState.Enemy:
                    pDanger = enemyDetectionRate;     // 0.85
                    break;
                case CellState.Trap:
                    pDanger = trapDetectionRate;       // 0.75
                    break;
                case CellState.Resource:
                    pDanger = resourceFalsePositiveRate; // 0.05
                    break;
                case CellState.Empty:
                default:
                    pDanger = falsePositiveRate;       // 0.10
                    break;
            }

            float pSafe = 1f - pDanger;

            switch (observation)
            {
                case SensorReading.Danger:
                    return pDanger;
                case SensorReading.Safe:
                    return pSafe;
                case SensorReading.Unknown:
                    return 0.5f; // Uninformative observation
                default:
                    return 0.25f;
            }
        }

        /// <summary>
        /// Generates a noisy sensor reading for a given cell position.
        /// Simulates the POMDP observation process.
        /// </summary>
        public SensorReading GenerateObservation(int x, int y)
        {
            if (!IsValidCell(x, y)) return SensorReading.Unknown;

            CellState trueState = grid[x, y].HiddenState;
            float pDanger = GetObservationProbability(SensorReading.Danger, trueState);

            float roll = Random.value;
            if (roll < pDanger)
                return SensorReading.Danger;
            else
                return SensorReading.Safe;
        }

        /// <summary>
        /// Transition function P(s'|s,a): for this turn-based game, cell states
        /// are mostly static (enemies don't move between cells in the basic model),
        /// but enemy elimination and resource collection change states.
        /// </summary>
        public CellState ApplyTransition(int x, int y, PlayerAction action)
        {
            if (!IsValidCell(x, y)) return CellState.Empty;

            CellState current = grid[x, y].HiddenState;

            switch (action)
            {
                case PlayerAction.Attack:
                    if (current == CellState.Enemy)
                    {
                        grid[x, y].HiddenState = CellState.Empty;
                        enemiesRemaining--;
                        return CellState.Empty;
                    }
                    return current;

                case PlayerAction.Move:
                    // Moving onto a resource collects it
                    if (current == CellState.Resource)
                    {
                        grid[x, y].HiddenState = CellState.Empty;
                        return CellState.Resource; // Return original so caller knows it was collected
                    }
                    return current;

                default:
                    return current;
            }
        }

        // =====================================================================
        // Grid Query Methods
        // =====================================================================

        public GridCell GetCell(int x, int y)
        {
            if (!IsValidCell(x, y)) return null;
            return grid[x, y];
        }

        public CellState GetHiddenState(int x, int y)
        {
            if (!IsValidCell(x, y)) return CellState.Empty;
            return grid[x, y].HiddenState;
        }

        public bool IsValidCell(int x, int y)
        {
            return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
        }

        public bool IsCellRevealed(int x, int y)
        {
            if (!IsValidCell(x, y)) return false;
            return grid[x, y].IsRevealed;
        }

        /// <summary>
        /// Reveals a cell (called when the player moves onto it or scouts it).
        /// Updates the fog-of-war visual and tints the cell based on content.
        /// </summary>
        public void RevealCell(int x, int y, bool fullReveal = true)
        {
            if (!IsValidCell(x, y)) return;

            if (fullReveal)
            {
                grid[x, y].IsRevealed = true;
                grid[x, y].IsPartiallyRevealed = false;
            }
            else
            {
                grid[x, y].IsPartiallyRevealed = true;
            }

            // Update fog visual
            if (fogObjects[x, y] != null)
            {
                if (fullReveal)
                {
                    fogObjects[x, y].SetActive(false);
                }
                else
                {
                    // Partially transparent for scouted cells
                    var renderer = fogObjects[x, y].GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Color c = renderer.material.color;
                        c.a = 0.4f;
                        renderer.material.color = c;
                    }
                }
            }

            // Tint the cell tile based on revealed content
            if (fullReveal && cellObjects[x, y] != null)
            {
                var cellRenderer = cellObjects[x, y].GetComponent<Renderer>();
                if (cellRenderer != null)
                {
                    CellState state = grid[x, y].HiddenState;
                    Color tint;
                    switch (state)
                    {
                        case CellState.Empty:
                            tint = new Color(0.6f, 0.8f, 0.6f); // slight green (safe)
                            break;
                        case CellState.Enemy:
                            tint = new Color(0.85f, 0.6f, 0.6f); // slight red
                            break;
                        case CellState.Trap:
                            tint = new Color(0.85f, 0.75f, 0.55f); // slight orange
                            break;
                        case CellState.Resource:
                            tint = new Color(0.6f, 0.7f, 0.85f); // slight blue
                            break;
                        default:
                            tint = Color.white;
                            break;
                    }
                    cellRenderer.material.color = tint;
                }
            }

            // Spawn content visuals for revealed cells
            if (fullReveal)
            {
                SpawnContentVisual(x, y);
            }
        }

        private void SpawnContentVisual(int x, int y)
        {
            Vector3 position = new Vector3(x * 2f, 0.5f, y * 2f);
            CellState state = grid[x, y].HiddenState;

            switch (state)
            {
                case CellState.Enemy:
                    if (enemyPrefab != null)
                    {
                        var obj = Instantiate(enemyPrefab, position, Quaternion.identity, transform);
                        obj.SetActive(true);
                    }
                    break;
                case CellState.Trap:
                    if (trapPrefab != null)
                    {
                        var obj = Instantiate(trapPrefab, position,
                            Quaternion.identity, transform);
                        obj.SetActive(true);
                        obj.transform.position = new Vector3(x * 2f, 0.15f, y * 2f);
                    }
                    break;
                case CellState.Resource:
                    if (resourcePrefab != null)
                    {
                        var obj = Instantiate(resourcePrefab, position, Quaternion.identity, transform);
                        obj.SetActive(true);
                    }
                    break;
            }
        }

        /// <summary>
        /// Returns all neighboring cell coordinates within a given radius.
        /// Used by VPI calculator to find adjacent unobserved cells.
        /// </summary>
        public List<Vector2Int> GetNeighbors(int x, int y, int radius = 1)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (IsValidCell(nx, ny))
                        neighbors.Add(new Vector2Int(nx, ny));
                }
            }
            return neighbors;
        }

        /// <summary>
        /// Returns all unrevealed neighbor cells (candidates for scouting/VPI).
        /// </summary>
        public List<Vector2Int> GetUnrevealedNeighbors(int x, int y, int radius = 2)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            foreach (var neighbor in GetNeighbors(x, y, radius))
            {
                if (!grid[neighbor.x, neighbor.y].IsRevealed)
                    result.Add(neighbor);
            }
            return result;
        }

        /// <summary>
        /// Computes the fraction of the map that has been revealed.
        /// Used as a feature for the linear regression difficulty predictor.
        /// </summary>
        public float GetRevealedFraction()
        {
            int revealed = 0;
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    if (grid[x, y].IsRevealed) revealed++;
            return (float)revealed / (gridWidth * gridHeight);
        }

        /// <summary>
        /// Returns the world position of a grid cell.
        /// </summary>
        public Vector3 CellToWorldPosition(int x, int y)
        {
            return new Vector3(x * 2f, 0f, y * 2f);
        }

        /// <summary>
        /// Converts a world position to grid coordinates.
        /// </summary>
        public Vector2Int WorldToCell(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / 2f),
                Mathf.RoundToInt(worldPos.z / 2f)
            );
        }
    }
}
