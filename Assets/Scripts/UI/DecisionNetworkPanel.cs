// =============================================================================
// DecisionNetworkPanel.cs — Decision Network Visualization Panel
// Displays a compact visualization of the current decision network:
// chance nodes (EnemyLoc, Terrain), observation node (Sensor), action node
// (Action), and utility node (Utility). The current MEU value updates each turn.
// This panel directly mirrors the lecture's decision network diagrams.
// Uses IMGUI (OnGUI) for zero-dependency rendering.
// Corresponds to Implementation Step 9.
// =============================================================================

using UnityEngine;
using System.Collections.Generic;

namespace VeilOfUncertainty
{
    /// <summary>
    /// DecisionNetworkPanel renders a visual representation of the decision network
    /// structure in the top-right corner of the screen using Unity IMGUI.
    /// Shows chance nodes (ovals), observation node, action node (rectangle),
    /// utility node (diamond), current MEU, and expected utility per action.
    /// </summary>
    public class DecisionNetworkPanel : MonoBehaviour
    {
        [Header("Panel Configuration")]
        [SerializeField] private bool startVisible = true;

        private DecisionNetwork decisionNetwork;
        private bool isVisible;
        private GUIStyle boxStyle;
        private GUIStyle titleStyle;
        private GUIStyle nodeStyle;
        private GUIStyle valueStyle;
        private bool stylesInitialized;

        public void Initialize(DecisionNetwork network)
        {
            decisionNetwork = network;
            isVisible = startVisible;
        }

        public void Refresh()
        {
            // IMGUI redraws automatically; nothing to cache here.
        }

        public void ToggleVisibility()
        {
            isVisible = !isVisible;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.15f, 0.85f));

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 14;

            nodeStyle = new GUIStyle(GUI.skin.label);
            nodeStyle.fontSize = 12;
            nodeStyle.normal.textColor = Color.white;

            valueStyle = new GUIStyle(GUI.skin.label);
            valueStyle.fontSize = 12;

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!isVisible || decisionNetwork == null) return;
            InitStyles();

            float panelW = 260f;
            float panelH = 310f;
            float panelX = Screen.width - panelW - 10f;
            float panelY = 10f;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "", boxStyle);

            float y = panelY + 5f;
            GUI.Label(new Rect(panelX, y, panelW, 22f), "DECISION NETWORK", titleStyle);
            y += 24f;

            // MEU and best action
            GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 20f),
                $"MEU: {decisionNetwork.CurrentMEU:F1}", titleStyle);
            y += 20f;

            Color bestColor = GetActionColor(decisionNetwork.BestAction);
            var bestStyle = new GUIStyle(titleStyle) { normal = { textColor = bestColor } };
            GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 20f),
                $"Best: {decisionNetwork.BestAction}", bestStyle);
            y += 26f;

            // Network nodes
            var nodes = decisionNetwork.GetNetworkDescription();
            foreach (var (name, type, observed, value) in nodes)
            {
                string typeSymbol = type switch
                {
                    NodeType.Chance => "(O)",
                    NodeType.Observation => "(O)",
                    NodeType.Action => "[R]",
                    NodeType.Utility => "<D>",
                    _ => "(?)"
                };
                string status = observed ? value : "?";

                nodeStyle.normal.textColor = observed ? Color.white : Color.gray;
                GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 18f),
                    $"{typeSymbol} {name}: {status}", nodeStyle);
                y += 18f;
            }

            y += 6f;
            GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 18f),
                "--- Action Utilities ---", nodeStyle);
            y += 20f;

            // Per-action EU values
            var utilities = decisionNetwork.ActionUtilities;
            if (utilities != null)
            {
                foreach (var kvp in utilities)
                {
                    bool isBest = kvp.Key == decisionNetwork.BestAction;
                    string marker = isBest ? " <<" : "";
                    valueStyle.normal.textColor = isBest
                        ? new Color(0.3f, 0.7f, 1f)
                        : (kvp.Value > 0 ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f));

                    GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 18f),
                        $"  {kvp.Key}: {kvp.Value:F1}{marker}", valueStyle);
                    y += 18f;
                }
            }
        }

        private Color GetActionColor(PlayerAction action)
        {
            return action switch
            {
                PlayerAction.Scout => new Color(0.3f, 0.7f, 1f),
                PlayerAction.Move => new Color(0.3f, 1f, 0.3f),
                PlayerAction.Attack => new Color(1f, 0.3f, 0.3f),
                PlayerAction.Defend => new Color(1f, 1f, 0.3f),
                _ => Color.white
            };
        }

        /// <summary>
        /// Helper: creates a solid-color texture for GUIStyle backgrounds.
        /// </summary>
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
