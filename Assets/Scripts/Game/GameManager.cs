// =============================================================================
// GameManager.cs — Main Game Loop and Integration for Veil of Uncertainty
// Integrates all AI modules (decision network, VPI, belief state, linear
// regression, k-NN) into the turn-based game loop. Manages game state,
// player actions, scoring, and win/loss conditions.
// Corresponds to Implementation Step 10.
// =============================================================================

using UnityEngine;

namespace VeilOfUncertainty
{
    /// <summary>
    /// Possible phases of the turn-based game.
    /// </summary>
    public enum GamePhase
    {
        PlayerTurn,     // Waiting for player input
        ProcessAction,  // Executing the player's chosen action
        EnemyTurn,      // Enemies act (simplified)
        TurnEnd,        // End-of-turn bookkeeping
        GameOver        // Game has ended (win or loss)
    }

    /// <summary>
    /// GameManager is the central coordinator for the turn-based game loop.
    /// Each turn:
    ///   1. Generate sensor observations for adjacent cells
    ///   2. Run the AI Advisor (decision network + VPI + belief update)
    ///   3. Display recommendation and wait for player input
    ///   4. Execute the player's action
    ///   5. Update game state (HP, score, belief state)
    ///   6. Check win/loss conditions
    ///   7. Advance to next turn
    ///
    /// This integrates all modules from Steps 1-9 into a cohesive game.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameConfig config;

        [Header("Grid")]
        [SerializeField] private GridWorld gridWorld;

        [Header("UI")]
        [SerializeField] private GameUIManager uiManager;

        [Header("Camera")]
        [SerializeField] private CameraController cameraController;

        // Player visual controller
        private PlayerController playerController;

        // Game Over screen
        private GameOverScreen gameOverScreen;

        // AI Advisor (contains all AI subsystems)
        private AIAdvisor aiAdvisor;

        // Game state
        private int currentTurn = 1;
        private int playerHP;
        private int playerScouts;
        private int playerResources;
        private int playerScore;
        private int playerX;
        private int playerY;
        private Direction playerFacing = Direction.North;
        private GamePhase currentPhase;

        // Stat tracking
        private int enemiesNeutralized = 0;
        private int resourcesCollected = 0;

        // Turn state
        private bool actionTakenThisTurn;
        private int scoutsUsedThisTurn;

        // Whether Setup() was called (SceneBuilder path)
        private bool setupCalled;

        // Properties
        public int CurrentTurn => currentTurn;
        public int PlayerHP => playerHP;
        public int PlayerScore => playerScore;
        public GamePhase Phase => currentPhase;

        /// <summary>
        /// Public setup method called by SceneBuilder to wire references
        /// that are normally set via [SerializeField] in the Inspector.
        /// </summary>
        public void Setup(GameConfig cfg, GridWorld grid, GameUIManager ui,
                          CameraController cam, PlayerController player,
                          GameOverScreen goScreen)
        {
            config = cfg;
            gridWorld = grid;
            uiManager = ui;
            cameraController = cam;
            playerController = player;
            gameOverScreen = goScreen;
            setupCalled = true;
        }

        private void Start()
        {
            InitializeGame();
        }

        /// <summary>
        /// Initializes the entire game: grid world, AI systems, UI, and starting state.
        /// </summary>
        private void InitializeGame()
        {
            // Load config if not assigned
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
            }

            // Initialize grid world
            if (gridWorld == null)
            {
                gridWorld = gameObject.AddComponent<GridWorld>();
            }
            gridWorld.Initialize();

            // Initialize AI advisor (creates all AI subsystems)
            aiAdvisor = gameObject.AddComponent<AIAdvisor>();
            aiAdvisor.Initialize(gridWorld, config);

            // Initialize UI
            if (uiManager == null)
            {
                uiManager = gameObject.AddComponent<GameUIManager>();
            }
            uiManager.Initialize(aiAdvisor, gridWorld, config);

            // Set starting state
            playerHP = config.startingHP;
            playerScouts = config.startingScouts;
            playerResources = config.startingResources;
            playerScore = 0;
            playerX = 0;
            playerY = 0;
            playerFacing = Direction.North;
            currentTurn = 1;
            enemiesNeutralized = 0;
            resourcesCollected = 0;

            // Set player position
            if (playerController != null)
            {
                playerController.SetPosition(gridWorld.CellToWorldPosition(0, 0));
            }

            // Center camera on player start
            if (cameraController != null)
            {
                cameraController.FollowPlayer(gridWorld.CellToWorldPosition(0, 0));
            }

            // Reveal starting position
            aiAdvisor.ProcessCellReveal(0, 0, CellState.Empty);

            // Start first turn
            BeginPlayerTurn();
        }

        /// <summary>
        /// Begins a new player turn: generates observations, runs the AI advisor,
        /// and updates the UI with the recommendation.
        /// </summary>
        private void BeginPlayerTurn()
        {
            currentPhase = GamePhase.PlayerTurn;
            actionTakenThisTurn = false;
            scoutsUsedThisTurn = 0;

            // Generate noisy sensor observations for adjacent cells
            aiAdvisor.GenerateAdjacentObservations(playerX, playerY, currentTurn);

            // Run the AI advisor to compute MEU, VPI, and recommendation
            AdvisorRecommendation recommendation = aiAdvisor.GenerateRecommendation(
                playerX, playerY, currentTurn,
                playerHP, playerScouts, playerScore);

            // Update all UI panels
            uiManager.RefreshAllPanels(
                playerX, playerY, currentTurn,
                playerHP, config.startingHP,
                playerScouts, playerResources, playerScore,
                recommendation);

            uiManager.SetStatus("Your turn. Choose an action.");
        }

        private void Update()
        {
            // Handle restart in game over state
            if (currentPhase == GamePhase.GameOver)
            {
                if (Input.GetKeyDown(KeyCode.R))
                    RestartGame();
                return;
            }

            if (currentPhase != GamePhase.PlayerTurn) return;

            HandlePlayerInput();
        }

        /// <summary>
        /// Processes keyboard input for player actions.
        /// Controls: [S] Scout, [A] Attack, [M/Arrow] Move, [F] Defend
        /// </summary>
        private void HandlePlayerInput()
        {
            // Scout action [S]
            if (Input.GetKeyDown(KeyCode.S) && !actionTakenThisTurn)
            {
                AttemptScout();
                return;
            }

            // Attack action [A]
            if (Input.GetKeyDown(KeyCode.A) && !actionTakenThisTurn)
            {
                AttemptAttack();
                return;
            }

            // Defend action [F]
            if (Input.GetKeyDown(KeyCode.F) && !actionTakenThisTurn)
            {
                ExecuteDefend();
                return;
            }

            // Move actions [Arrow keys / WASD for movement]
            if (!actionTakenThisTurn)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                    AttemptMove(Direction.North);
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                    AttemptMove(Direction.South);
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                    AttemptMove(Direction.East);
                else if (Input.GetKeyDown(KeyCode.LeftArrow))
                    AttemptMove(Direction.West);
            }
        }

        // =====================================================================
        // Action Execution Methods
        // =====================================================================

        /// <summary>
        /// Attempts to scout the best VPI target cell.
        /// Uses a scout resource and reveals the cell's true state.
        /// </summary>
        private void AttemptScout()
        {
            if (playerScouts <= 0)
            {
                uiManager.ShowTurnResult("No scouts remaining!", false);
                return;
            }

            if (scoutsUsedThisTurn >= config.maxScoutsPerTurn)
            {
                uiManager.ShowTurnResult("Max scouts per turn reached!", false);
                return;
            }

            // Get the best scout target from VPI analysis
            var bestTarget = aiAdvisor.GetVPICalculator().GetBestScoutTarget();
            int targetX, targetY;

            if (bestTarget.HasValue)
            {
                targetX = bestTarget.Value.CellX;
                targetY = bestTarget.Value.CellY;
            }
            else
            {
                // No high-VPI target; scout nearest unrevealed cell
                var unrevealed = gridWorld.GetUnrevealedNeighbors(playerX, playerY, config.scoutRange);
                if (unrevealed.Count == 0)
                {
                    uiManager.ShowTurnResult("No cells in range to scout!", false);
                    return;
                }
                targetX = unrevealed[0].x;
                targetY = unrevealed[0].y;
            }

            // Execute scout
            playerScouts--;
            scoutsUsedThisTurn++;

            // Play scout sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayScoutPing();

            CellState revealedState = gridWorld.GetHiddenState(targetX, targetY);
            aiAdvisor.ProcessScoutResult(targetX, targetY, revealedState);

            string stateStr = revealedState.ToString();
            uiManager.ShowTurnResult(
                $"Scouted ({targetX},{targetY}): {stateStr} found!", true);

            playerScore += 5; // Small score for information gathering

            // Scouting does not end the turn — player can still act
            // But update the UI with new information
            AdvisorRecommendation newRec = aiAdvisor.GenerateRecommendation(
                playerX, playerY, currentTurn,
                playerHP, playerScouts, playerScore);

            uiManager.RefreshAllPanels(
                playerX, playerY, currentTurn,
                playerHP, config.startingHP,
                playerScouts, playerResources, playerScore,
                newRec);
        }

        /// <summary>
        /// Attempts to attack the cell the player is facing.
        /// </summary>
        private void AttemptAttack()
        {
            (int tx, int ty) = GetTargetCell(playerFacing);

            if (!gridWorld.IsValidCell(tx, ty))
            {
                uiManager.ShowTurnResult("Nothing to attack there!", false);
                return;
            }

            CellState targetState = gridWorld.GetHiddenState(tx, ty);
            gridWorld.ApplyTransition(tx, ty, PlayerAction.Attack);
            aiAdvisor.ProcessCellReveal(tx, ty, CellState.Empty);

            // Play attack sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayAttack();

            if (targetState == CellState.Enemy)
            {
                playerScore += 50;
                enemiesNeutralized++;
                uiManager.ShowTurnResult(
                    $"Enemy at ({tx},{ty}) neutralized! +50 points", true);
            }
            else
            {
                playerScore -= 5;
                uiManager.ShowTurnResult(
                    $"Attacked ({tx},{ty}) but nothing was there. -5 points", false);
            }

            actionTakenThisTurn = true;
            EndPlayerTurn();
        }

        /// <summary>
        /// Attempts to move the player in the specified direction.
        /// Reveals the destination cell and applies consequences.
        /// </summary>
        private void AttemptMove(Direction direction)
        {
            playerFacing = direction;
            (int tx, int ty) = GetTargetCell(direction);

            if (!gridWorld.IsValidCell(tx, ty))
            {
                uiManager.ShowTurnResult("Can't move there — edge of map!", false);
                return;
            }

            CellState targetState = gridWorld.GetHiddenState(tx, ty);

            // Move to the cell
            playerX = tx;
            playerY = ty;
            gridWorld.ApplyTransition(tx, ty, PlayerAction.Move);
            aiAdvisor.ProcessCellReveal(tx, ty, targetState);

            // Play footstep sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayFootstep();

            // Animate player movement
            if (playerController != null)
            {
                playerController.MoveTo(gridWorld.CellToWorldPosition(playerX, playerY), direction);
            }

            // Apply consequences based on cell content
            switch (targetState)
            {
                case CellState.Empty:
                    playerScore += 5;
                    uiManager.ShowTurnResult("Moved safely. +5 points", true);
                    break;

                case CellState.Enemy:
                    int damage = 40;
                    playerHP -= damage;
                    uiManager.ShowTurnResult(
                        $"Walked into an enemy! Took {damage} damage!", false);
                    if (playerController != null)
                        playerController.PlayDamageEffect();
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayDamage();
                    // Trigger HUD damage flash
                    if (uiManager != null)
                        uiManager.TriggerDamageFlash();
                    break;

                case CellState.Trap:
                    int trapDmg = 30;
                    playerHP -= trapDmg;
                    uiManager.ShowTurnResult(
                        $"Stepped on a trap! Took {trapDmg} damage!", false);
                    if (playerController != null)
                        playerController.PlayDamageEffect();
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayDamage();
                    if (uiManager != null)
                        uiManager.TriggerDamageFlash();
                    break;

                case CellState.Resource:
                    playerResources++;
                    resourcesCollected++;
                    playerHP = Mathf.Min(playerHP + config.hpPerResource, config.startingHP);
                    playerScore += 20;
                    uiManager.ShowTurnResult(
                        $"Found a resource! +{config.hpPerResource}HP, +20 points", true);
                    if (playerController != null)
                        playerController.PlayHealEffect();
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayPickup();
                    break;
            }

            // Update camera to follow player
            if (cameraController != null)
                cameraController.FollowPlayer(gridWorld.CellToWorldPosition(playerX, playerY));

            actionTakenThisTurn = true;
            EndPlayerTurn();
        }

        /// <summary>
        /// Executes the defend action. Reduces damage from adjacent enemies.
        /// </summary>
        private void ExecuteDefend()
        {
            playerScore += 2;

            // Check adjacent cells for enemies that might attack
            bool enemyNearby = false;
            var neighbors = gridWorld.GetNeighbors(playerX, playerY, 1);
            foreach (var n in neighbors)
            {
                if (gridWorld.GetHiddenState(n.x, n.y) == CellState.Enemy)
                {
                    enemyNearby = true;
                    break;
                }
            }

            if (enemyNearby)
            {
                playerScore += 10;
                uiManager.ShowTurnResult(
                    "Defended against nearby enemy! +10 points", true);
            }
            else
            {
                uiManager.ShowTurnResult(
                    "Defended. No immediate threats. +2 points", true);
            }

            actionTakenThisTurn = true;
            EndPlayerTurn();
        }

        /// <summary>
        /// Ends the player's turn and advances the game.
        /// </summary>
        private void EndPlayerTurn()
        {
            currentPhase = GamePhase.TurnEnd;

            // Check for game over conditions
            if (playerHP <= 0)
            {
                GameOver(false);
                return;
            }

            if (gridWorld.EnemiesRemaining <= 0)
            {
                GameOver(true);
                return;
            }

            // Record any enemy behavior for k-NN training
            RecordEnemyBehaviors();

            // Advance turn
            currentTurn++;
            BeginPlayerTurn();
        }

        /// <summary>
        /// Records simulated enemy behavior observations for k-NN training.
        /// In a full implementation, this would track actual enemy movements.
        /// </summary>
        private void RecordEnemyBehaviors()
        {
            // Generate training examples from nearby enemy observations
            var neighbors = gridWorld.GetNeighbors(playerX, playerY, 3);
            foreach (var n in neighbors)
            {
                if (gridWorld.GetHiddenState(n.x, n.y) == CellState.Enemy &&
                    gridWorld.IsCellRevealed(n.x, n.y))
                {
                    float dist = Vector2Int.Distance(
                        new Vector2Int(playerX, playerY),
                        new Vector2Int(n.x, n.y));

                    // Determine behavior type based on distance pattern
                    EnemyBehaviorType type;
                    if (dist <= 1.5f)
                        type = EnemyBehaviorType.Aggressive;
                    else if (dist >= 3f)
                        type = EnemyBehaviorType.Defensive;
                    else
                        type = EnemyBehaviorType.Patrol;

                    aiAdvisor.RecordEnemyBehavior(new EnemyFeatureVector
                    {
                        MoveFrequency = Random.Range(0.1f, 0.9f),
                        AggressionScore = type == EnemyBehaviorType.Aggressive ? 0.8f : 0.2f,
                        PatrolRegularity = type == EnemyBehaviorType.Patrol ? 0.8f : 0.3f,
                        DistanceToPlayer = dist,
                        Label = type
                    });
                }
            }
        }

        /// <summary>
        /// Handles game over (win or loss).
        /// </summary>
        private void GameOver(bool victory)
        {
            currentPhase = GamePhase.GameOver;

            if (victory)
            {
                playerScore += 100; // Victory bonus
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayVictory();
            }
            else
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayDefeat();
            }

            // Show game over screen with stats
            if (gameOverScreen != null)
            {
                float explored = gridWorld.GetRevealedFraction() * 100f;
                gameOverScreen.Show(
                    victory, playerScore, currentTurn,
                    enemiesNeutralized, gridWorld.TotalEnemies,
                    resourcesCollected,
                    Mathf.Max(0, playerHP), explored,
                    RestartGame);
            }

            string resultMsg = victory
                ? $"VICTORY! All enemies neutralized! Final Score: {playerScore}"
                : $"DEFEAT! HP reached 0. Final Score: {playerScore}";
            uiManager.ShowTurnResult(resultMsg, victory);
            uiManager.SetStatus("Game Over. Press [R] to restart.");
        }

        /// <summary>
        /// Restarts the game by destroying and re-initializing everything.
        /// </summary>
        private void RestartGame()
        {
            // Hide game over screen
            if (gameOverScreen != null)
                gameOverScreen.Hide();

            // Destroy all child objects of gridWorld (cells, fog, content)
            foreach (Transform child in gridWorld.transform)
                Destroy(child.gameObject);

            // Destroy existing AI advisor components
            var existingAdvisor = GetComponent<AIAdvisor>();
            if (existingAdvisor != null) Destroy(existingAdvisor);

            var existingBelief = GetComponent<BeliefState>();
            if (existingBelief != null) Destroy(existingBelief);

            var existingDN = GetComponent<DecisionNetwork>();
            if (existingDN != null) Destroy(existingDN);

            var existingVPI = GetComponent<VPICalculator>();
            if (existingVPI != null) Destroy(existingVPI);

            var existingLR = GetComponent<LinearRegressionModel>();
            if (existingLR != null) Destroy(existingLR);

            var existingKNN = GetComponent<KNNClassifier>();
            if (existingKNN != null) Destroy(existingKNN);

            // Destroy fog overlay objects
            var fogRenderer = Object.FindFirstObjectByType<FogOfWarRenderer>();
            if (fogRenderer != null)
            {
                foreach (Transform child in fogRenderer.transform)
                    Destroy(child.gameObject);
            }

            // Re-initialize the game
            InitializeGame();
        }

        /// <summary>
        /// Returns the grid coordinates of the cell in the specified direction.
        /// </summary>
        private (int x, int y) GetTargetCell(Direction direction)
        {
            int tx = playerX;
            int ty = playerY;

            switch (direction)
            {
                case Direction.North: ty++; break;
                case Direction.South: ty--; break;
                case Direction.East:  tx++; break;
                case Direction.West:  tx--; break;
            }

            return (tx, ty);
        }
    }
}
