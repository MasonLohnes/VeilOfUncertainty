// =============================================================================
// BeliefStatePanel.cs — POMDP Belief State Visualization Panel
// Shows the current POMDP belief-state probabilities for key variables:
// probability of enemy in each adjacent region, probability of traps,
// and resource likelihood. Updates after each observation via Bayesian filtering.
// Uses IMGUI (OnGUI) for zero-dependency rendering.
// Corresponds to Implementation Step 9.
// =============================================================================

using UnityEngine;
using System.Collections.Generic;

namespace VeilOfUncertainty
{
    /// <summary>
    /// BeliefStatePanel displays the player's current POMDP belief state using
    /// Unity IMGUI. Shows per-cell probability distributions for adjacent cells,
    /// color-coded for Enemy, Trap, Resource, Empty, with entropy values.
    /// </summary>
    public class BeliefStatePanel : MonoBehaviour
    {
        private BeliefState beliefState;
        private GridWorld gridWorld;
        private bool isVisible;

        // Cached player position for OnGUI
        private int cachedPlayerX;
        private int cachedPlayerY;

        private GUIStyle boxStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private bool stylesInitialized;

        public void Initialize(BeliefState belief, GridWorld world)
        {
            beliefState = belief;
            gridWorld = world;
            isVisible = false; // Hidden by default, toggled with TAB
        }

        public void Refresh(int playerX, int playerY)
        {
            cachedPlayerX = playerX;
            cachedPlayerY = playerY;
        }

        public void ToggleVisibility()
        {
            isVisible = !isVisible;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.08f, 0.1f, 0.15f, 0.85f));

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 14;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 11;
            labelStyle.richText = true;

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!isVisible || beliefState == null) return;
            InitStyles();

            float panelW = 300f;
            float panelH = 340f;
            float panelX = Screen.width - panelW - 10f;
            float panelY = Screen.height - panelH - 10f;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "", boxStyle);

            float y = panelY + 5f;
            GUI.Label(new Rect(panelX, y, panelW, 22f), "BELIEF STATE (POMDP)", titleStyle);
            y += 26f;

            // Show beliefs for neighboring cells
            List<Vector2Int> neighbors = gridWorld.GetNeighbors(cachedPlayerX, cachedPlayerY, 2);
            int count = 0;

            foreach (Vector2Int cell in neighbors)
            {
                if (gridWorld.IsCellRevealed(cell.x, cell.y)) continue;
                if (count >= 8) break;

                CellBelief belief = beliefState.GetBelief(cell.x, cell.y);
                float entropy = belief.Entropy();

                // Cell header
                labelStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 16f),
                    $"Cell({cell.x},{cell.y})  H={entropy:F2}", labelStyle);
                y += 16f;

                // Probability values with color coding
                string line =
                    $"<color=#FF3333>E:{belief.ProbEnemy:F2}</color>  " +
                    $"<color=#FF9900>T:{belief.ProbTrap:F2}</color>  " +
                    $"<color=#33CCFF>R:{belief.ProbResource:F2}</color>  " +
                    $"<color=#80FF80>_:{belief.ProbEmpty:F2}</color>";

                GUI.Label(new Rect(panelX + 15f, y, panelW - 25f, 16f), line, labelStyle);
                y += 20f;

                count++;
            }

            if (count == 0)
            {
                labelStyle.normal.textColor = Color.gray;
                GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 18f),
                    "All adjacent cells revealed.", labelStyle);
            }
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}
