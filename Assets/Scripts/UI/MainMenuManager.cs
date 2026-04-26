// =============================================================================
// MainMenuManager.cs — Main Menu for Veil of Uncertainty
// IMGUI-based main menu with Play, How to Play, and Quit buttons.
// Consistent with the project's IMGUI-based UI approach.
// =============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;

namespace VeilOfUncertainty
{
    /// <summary>
    /// Controls the main menu scene. Uses IMGUI (OnGUI) to stay consistent
    /// with the rest of the project's UI approach.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        private bool showHowToPlay = false;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle overlayBoxStyle;
        private GUIStyle controlsHeaderStyle;
        private GUIStyle controlsTextStyle;
        private bool stylesInitialized;

        private void Start()
        {
            // Set dark background
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
            }
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 42;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = new Color(0.3f, 0.5f, 0.9f);

            subtitleStyle = new GUIStyle(GUI.skin.label);
            subtitleStyle.fontSize = 18;
            subtitleStyle.fontStyle = FontStyle.Italic;
            subtitleStyle.alignment = TextAnchor.MiddleCenter;
            subtitleStyle.normal.textColor = new Color(0.6f, 0.6f, 0.7f);

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 20;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.fixedHeight = 50f;
            buttonStyle.normal.textColor = Color.white;

            overlayBoxStyle = new GUIStyle(GUI.skin.box);
            overlayBoxStyle.normal.background = MakeTex(2, 2, new Color(0.06f, 0.06f, 0.1f, 0.95f));

            controlsHeaderStyle = new GUIStyle(GUI.skin.label);
            controlsHeaderStyle.fontSize = 20;
            controlsHeaderStyle.fontStyle = FontStyle.Bold;
            controlsHeaderStyle.alignment = TextAnchor.MiddleCenter;
            controlsHeaderStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);

            controlsTextStyle = new GUIStyle(GUI.skin.label);
            controlsTextStyle.fontSize = 15;
            controlsTextStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
            controlsTextStyle.richText = true;

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            float sw = Screen.width;
            float sh = Screen.height;

            // Dark background overlay
            GUI.Box(new Rect(0, 0, sw, sh), "", overlayBoxStyle);

            if (!showHowToPlay)
            {
                DrawMainMenu(sw, sh);
            }
            else
            {
                DrawHowToPlay(sw, sh);
            }
        }

        private void DrawMainMenu(float sw, float sh)
        {
            float centerX = sw / 2f;
            float startY = sh * 0.2f;

            // Title
            GUI.Label(new Rect(0, startY, sw, 60f), "VEIL OF UNCERTAINTY", titleStyle);
            startY += 70f;

            // Subtitle
            GUI.Label(new Rect(0, startY, sw, 30f), "A POMDP-Driven Exploration Game", subtitleStyle);
            startY += 80f;

            // Buttons
            float btnW = 250f;
            float btnX = centerX - btnW / 2f;

            if (GUI.Button(new Rect(btnX, startY, btnW, 50f), "Play", buttonStyle))
            {
                SceneManager.LoadScene("main");
            }
            startY += 70f;

            if (GUI.Button(new Rect(btnX, startY, btnW, 50f), "How to Play", buttonStyle))
            {
                showHowToPlay = true;
            }
            startY += 70f;

            if (GUI.Button(new Rect(btnX, startY, btnW, 50f), "Quit", buttonStyle))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }

            // Credits
            GUIStyle creditsStyle = new GUIStyle(GUI.skin.label);
            creditsStyle.fontSize = 12;
            creditsStyle.alignment = TextAnchor.MiddleCenter;
            creditsStyle.normal.textColor = new Color(0.4f, 0.4f, 0.5f);
            GUI.Label(new Rect(0, sh - 40f, sw, 30f),
                "CST-415: AI in Games and Simulations  |  Grand Canyon University", creditsStyle);
        }

        private void DrawHowToPlay(float sw, float sh)
        {
            float panelW = 500f;
            float panelH = 450f;
            float panelX = (sw - panelW) / 2f;
            float panelY = (sh - panelH) / 2f;

            GUI.Box(new Rect(panelX - 10, panelY - 10, panelW + 20, panelH + 20), "", overlayBoxStyle);

            float y = panelY;

            GUI.Label(new Rect(panelX, y, panelW, 35f), "HOW TO PLAY", controlsHeaderStyle);
            y += 45f;

            string[] controls = new string[]
            {
                "MOVEMENT",
                "  Arrow Keys / W    Move on the grid",
                "",
                "ACTIONS",
                "  S                 Scout (reveal a cell using VPI)",
                "  A                 Attack cell you are facing",
                "  F                 Defend (reduce incoming damage)",
                "",
                "AI PANELS",
                "  D                 Toggle Decision Network panel",
                "  V                 Toggle VPI panel",
                "  TAB               Toggle Belief State panel",
                "",
                "CAMERA",
                "  C                 Toggle top-down / isometric view",
                "  Scroll Wheel      Zoom in / out",
                "",
                "GOAL",
                "  Neutralize all enemies. Avoid traps.",
                "  Collect resources to restore HP.",
                "  Use the AI advisor to make informed decisions."
            };

            foreach (string line in controls)
            {
                bool isHeader = line.Length > 0 && line[0] != ' ';
                if (isHeader)
                {
                    controlsTextStyle.fontStyle = FontStyle.Bold;
                    controlsTextStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);
                }
                else
                {
                    controlsTextStyle.fontStyle = FontStyle.Normal;
                    controlsTextStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
                }
                GUI.Label(new Rect(panelX + 20f, y, panelW - 40f, 20f), line, controlsTextStyle);
                y += line.Length == 0 ? 10f : 20f;
            }

            y += 15f;

            float btnW = 150f;
            if (GUI.Button(new Rect((sw - btnW) / 2f, y, btnW, 40f), "Back", buttonStyle))
            {
                showHowToPlay = false;
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
