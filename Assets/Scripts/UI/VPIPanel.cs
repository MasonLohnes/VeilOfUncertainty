// =============================================================================
// VPIPanel.cs — Value of Perfect Information Display Panel
// Lists the computed VPI for each unobserved variable adjacent to the player,
// the scouting cost, and the system's recommendation (SCOUT or ACT).
// When VPI exceeds the scout cost, the recommendation is highlighted in green.
// This is the most distinctive UI element — real-time VPI from the lecture.
// Uses IMGUI (OnGUI) for zero-dependency rendering.
// Corresponds to Implementation Step 9.
// =============================================================================

using UnityEngine;
using System.Collections.Generic;

namespace VeilOfUncertainty
{
    /// <summary>
    /// VPIPanel displays a real-time VPI dashboard using Unity IMGUI.
    /// Shows per-cell VPI values, scouting cost, net VPI, and a
    /// clear SCOUT or ACT recommendation.
    /// </summary>
    public class VPIPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color scoutRecommendColor = new Color(0.2f, 1f, 0.2f);
        [SerializeField] private Color actRecommendColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color positiveVPIColor = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color negativeVPIColor = new Color(1f, 0.3f, 0.3f);

        private VPICalculator vpiCalculator;
        private GameConfig config;
        private bool isVisible = true;

        // Cached data for OnGUI rendering
        private List<VPIResult> cachedResults;
        private bool cachedShouldScout;
        private int cachedScoutsRemaining;

        private GUIStyle boxStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private bool stylesInitialized;

        public void Initialize(VPICalculator calculator, GameConfig gameConfig)
        {
            vpiCalculator = calculator;
            config = gameConfig;
        }

        public void Refresh(List<VPIResult> results, bool shouldScout, int scoutsRemaining)
        {
            cachedResults = results;
            cachedShouldScout = shouldScout;
            cachedScoutsRemaining = scoutsRemaining;
        }

        public void ToggleVisibility()
        {
            isVisible = !isVisible;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.12f, 0.1f, 0.85f));

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 14;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 12;

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!isVisible || vpiCalculator == null) return;
            InitStyles();

            float panelW = 280f;
            float panelH = 300f;
            float panelX = Screen.width - panelW - 10f;
            float panelY = 330f; // Below the Decision Network panel

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "", boxStyle);

            float y = panelY + 5f;
            GUI.Label(new Rect(panelX, y, panelW, 22f),
                "VALUE OF PERFECT INFORMATION", titleStyle);
            y += 24f;

            // Base MEU and scout cost
            labelStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 18f),
                $"Base MEU: {vpiCalculator.BaseMEU:F1}    Scout Cost: {config.scoutCost:F1}",
                labelStyle);
            y += 22f;

            // Recommendation
            var recStyle = new GUIStyle(titleStyle);
            if (cachedShouldScout && cachedScoutsRemaining > 0)
            {
                VPIResult? best = vpiCalculator.GetBestScoutTarget();
                if (best.HasValue)
                {
                    recStyle.normal.textColor = scoutRecommendColor;
                    GUI.Label(new Rect(panelX + 5f, y, panelW - 10f, 20f),
                        $">> SCOUT ({best.Value.CellX},{best.Value.CellY}) <<", recStyle);
                    y += 20f;
                    labelStyle.normal.textColor = scoutRecommendColor;
                    GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 18f),
                        $"VPI={best.Value.VPI:F1} > Cost={config.scoutCost:F1}  Net: +{best.Value.NetVPI:F1}",
                        labelStyle);
                    y += 22f;
                }
            }
            else
            {
                recStyle.normal.textColor = actRecommendColor;
                string reason = cachedScoutsRemaining <= 0
                    ? "No scouts remaining."
                    : "VPI does not justify scouting.";
                GUI.Label(new Rect(panelX + 5f, y, panelW - 10f, 20f),
                    ">> ACT NOW <<", recStyle);
                y += 20f;
                labelStyle.normal.textColor = actRecommendColor;
                GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 18f), reason, labelStyle);
                y += 22f;
            }

            // VPI list
            y += 4f;
            labelStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 18f),
                "--- Per-Cell VPI ---", labelStyle);
            y += 20f;

            if (cachedResults != null)
            {
                int count = Mathf.Min(cachedResults.Count, 8);
                for (int i = 0; i < count; i++)
                {
                    VPIResult r = cachedResults[i];
                    string prefix = r.NetVPI > 0 ? "+" : "";
                    labelStyle.normal.textColor = r.NetVPI > 0 ? positiveVPIColor : negativeVPIColor;
                    GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 18f),
                        $"  ({r.CellX},{r.CellY})  VPI={r.VPI:F1}  Net={prefix}{r.NetVPI:F1}",
                        labelStyle);
                    y += 18f;
                }
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
