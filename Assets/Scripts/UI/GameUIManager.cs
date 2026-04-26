// =============================================================================
// GameUIManager.cs — Central UI Manager for Veil of Uncertainty
// Coordinates all UI panels: fog-of-war renderer, decision network panel,
// VPI panel, belief state panel, and HUD. Handles panel toggle inputs
// and orchestrates UI refresh cycles.
// Uses only core Unity APIs — no external UI packages required.
// Corresponds to Implementation Step 9.
// =============================================================================

using UnityEngine;

namespace VeilOfUncertainty
{
    /// <summary>
    /// GameUIManager is the central coordinator for all UI elements.
    /// It initializes all panels, routes keyboard input for panel toggles,
    /// and triggers UI refreshes after each turn.
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        [Header("UI Panel References")]
        [SerializeField] private DecisionNetworkPanel decisionNetworkPanel;
        [SerializeField] private VPIPanel vpiPanel;
        [SerializeField] private BeliefStatePanel beliefStatePanel;
        [SerializeField] private HUDManager hudManager;
        [SerializeField] private FogOfWarRenderer fogOfWarRenderer;

        private AIAdvisor aiAdvisor;
        private GridWorld gridWorld;
        private GameConfig config;
        private bool isInitialized;

        /// <summary>
        /// Initializes all UI panels with references to the game systems.
        /// </summary>
        public void Initialize(AIAdvisor advisor, GridWorld world, GameConfig gameConfig)
        {
            aiAdvisor = advisor;
            gridWorld = world;
            config = gameConfig;

            // Initialize panels
            if (decisionNetworkPanel != null)
                decisionNetworkPanel.Initialize(advisor.GetDecisionNetwork());

            if (vpiPanel != null)
                vpiPanel.Initialize(advisor.GetVPICalculator(), config);

            if (beliefStatePanel != null)
                beliefStatePanel.Initialize(advisor.GetBeliefState(), world);

            if (hudManager != null)
                hudManager.Initialize(config);

            if (fogOfWarRenderer != null)
                fogOfWarRenderer.Initialize(world, advisor.GetBeliefState());

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized) return;

            HandlePanelToggleInput();
        }

        /// <summary>
        /// Handles keyboard shortcuts for toggling UI panels.
        ///   [D] — Toggle Decision Network panel
        ///   [V] — Toggle VPI panel
        ///   [TAB] — Toggle Belief State panel
        /// </summary>
        private void HandlePanelToggleInput()
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (decisionNetworkPanel != null)
                    decisionNetworkPanel.ToggleVisibility();
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                if (vpiPanel != null)
                    vpiPanel.ToggleVisibility();
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (beliefStatePanel != null)
                    beliefStatePanel.ToggleVisibility();
            }
        }

        /// <summary>
        /// Refreshes all UI panels after a turn is processed.
        /// Called by the GameManager at the end of each turn.
        /// </summary>
        public void RefreshAllPanels(int playerX, int playerY,
                                      int currentTurn, int playerHP, int maxHP,
                                      int scoutsRemaining, int resources, int score,
                                      AdvisorRecommendation recommendation)
        {
            if (hudManager != null)
            {
                hudManager.UpdateTurn(currentTurn);
                hudManager.UpdateHP(playerHP, maxHP);
                hudManager.UpdateScouts(scoutsRemaining);
                hudManager.UpdateResources(resources);
                hudManager.UpdateScore(score);
                hudManager.UpdateAdvisorRecommendation(recommendation);
                hudManager.UpdateDifficulty(aiAdvisor.DifficultyModifier);
            }

            if (decisionNetworkPanel != null)
                decisionNetworkPanel.Refresh();

            if (vpiPanel != null)
            {
                vpiPanel.Refresh(
                    aiAdvisor.CurrentVPIResults,
                    recommendation.ShouldScout,
                    scoutsRemaining);
            }

            if (beliefStatePanel != null)
                beliefStatePanel.Refresh(playerX, playerY);

            if (fogOfWarRenderer != null)
                fogOfWarRenderer.UpdateOverlays();
        }

        public void ShowTurnResult(string message, bool isPositive)
        {
            if (hudManager != null)
                hudManager.ShowTurnResult(message, isPositive);
        }

        public void SetStatus(string message)
        {
            if (hudManager != null)
                hudManager.SetStatus(message);
        }
    }
}
