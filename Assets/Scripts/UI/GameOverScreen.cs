// =============================================================================
// GameOverScreen.cs — Game Over / Victory Overlay for Veil of Uncertainty
// Full-screen IMGUI overlay that appears when the game ends, showing
// victory/defeat status, gameplay statistics, and restart/menu buttons.
// =============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;

namespace VeilOfUncertainty
{
    /// <summary>
    /// Full-screen overlay that appears when the game ends. Shows victory or
    /// defeat status with gameplay stats and provides restart/menu options.
    /// Uses IMGUI consistent with the rest of the project.
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        private bool isVisible = false;
        private bool isVictory;

        // Stats
        private int finalScore;
        private int turnsSurvived;
        private int enemiesNeutralized;
        private int totalEnemies;
        private int resourcesCollected;
        private int hpRemaining;
        private float mapExplored;

        // Fade effect
        private float fadeAlpha = 0f;
        private float fadeTarget = 0f;

        // Callback
        private System.Action onRestart;

        // Styles
        private GUIStyle overlayStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle statLabelStyle;
        private GUIStyle statValueStyle;
        private GUIStyle buttonStyle;
        private bool stylesInitialized;

        /// <summary>
        /// Shows the game over screen with the given stats.
        /// </summary>
        public void Show(bool victory, int score, int turns,
                         int neutralized, int total, int resources,
                         int hp, float explored, System.Action restartCallback)
        {
            isVisible = true;
            isVictory = victory;
            finalScore = score;
            turnsSurvived = turns;
            enemiesNeutralized = neutralized;
            totalEnemies = total;
            resourcesCollected = resources;
            hpRemaining = hp;
            mapExplored = explored;
            onRestart = restartCallback;

            fadeAlpha = 0f;
            fadeTarget = 0.85f;
        }

        /// <summary>
        /// Hides the game over screen.
        /// </summary>
        public void Hide()
        {
            isVisible = false;
            fadeAlpha = 0f;
            fadeTarget = 0f;
        }

        private void Update()
        {
            // Fade in/out
            fadeAlpha = Mathf.MoveTowards(fadeAlpha, fadeTarget, Time.deltaTime * 2f);
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            overlayStyle = new GUIStyle(GUI.skin.box);
            overlayStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 1f));

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 36;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;

            subtitleStyle = new GUIStyle(GUI.skin.label);
            subtitleStyle.fontSize = 22;
            subtitleStyle.fontStyle = FontStyle.Italic;
            subtitleStyle.alignment = TextAnchor.MiddleCenter;

            statLabelStyle = new GUIStyle(GUI.skin.label);
            statLabelStyle.fontSize = 16;
            statLabelStyle.alignment = TextAnchor.MiddleLeft;
            statLabelStyle.normal.textColor = new Color(0.7f, 0.7f, 0.8f);

            statValueStyle = new GUIStyle(GUI.skin.label);
            statValueStyle.fontSize = 16;
            statValueStyle.fontStyle = FontStyle.Bold;
            statValueStyle.alignment = TextAnchor.MiddleRight;
            statValueStyle.normal.textColor = Color.white;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 18;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.fixedHeight = 45f;

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!isVisible || fadeAlpha < 0.01f) return;

            InitStyles();

            float sw = Screen.width;
            float sh = Screen.height;

            // Semi-transparent dark overlay
            Color overlayColor = new Color(0.02f, 0.02f, 0.05f, fadeAlpha);
            GUI.color = new Color(1f, 1f, 1f, fadeAlpha);
            overlayStyle.normal.background = MakeTex(2, 2, overlayColor);
            GUI.Box(new Rect(0, 0, sw, sh), "", overlayStyle);

            // Only show content when mostly faded in
            if (fadeAlpha < 0.3f)
            {
                GUI.color = Color.white;
                return;
            }

            float contentAlpha = Mathf.InverseLerp(0.3f, 0.85f, fadeAlpha);
            GUI.color = new Color(1f, 1f, 1f, contentAlpha);

            float panelW = 420f;
            float panelX = (sw - panelW) / 2f;
            float y = sh * 0.15f;

            // Title
            titleStyle.normal.textColor = isVictory
                ? new Color(0.3f, 1f, 0.3f)
                : new Color(1f, 0.3f, 0.3f);
            string title = isVictory ? "ALL ENEMIES NEUTRALIZED" : "YOU HAVE FALLEN";
            GUI.Label(new Rect(0, y, sw, 50f), title, titleStyle);
            y += 55f;

            // Subtitle
            subtitleStyle.normal.textColor = isVictory
                ? new Color(0.5f, 0.9f, 0.5f)
                : new Color(0.9f, 0.5f, 0.5f);
            GUI.Label(new Rect(0, y, sw, 35f), isVictory ? "Victory!" : "Defeated...", subtitleStyle);
            y += 60f;

            // Stats panel
            DrawStatRow(panelX, ref y, panelW, "Final Score", finalScore.ToString());
            DrawStatRow(panelX, ref y, panelW, "Turns Survived", turnsSurvived.ToString());
            DrawStatRow(panelX, ref y, panelW, "Enemies Neutralized",
                $"{enemiesNeutralized} / {totalEnemies}");
            DrawStatRow(panelX, ref y, panelW, "Resources Collected", resourcesCollected.ToString());
            DrawStatRow(panelX, ref y, panelW, "HP Remaining", hpRemaining.ToString());
            DrawStatRow(panelX, ref y, panelW, "Map Explored", $"{mapExplored:F0}%");

            y += 40f;

            // Buttons
            float btnW = 200f;
            float btnX = (sw - btnW) / 2f;

            if (GUI.Button(new Rect(btnX, y, btnW, 45f), "Play Again", buttonStyle))
            {
                Hide();
                onRestart?.Invoke();
            }
            y += 60f;

            if (GUI.Button(new Rect(btnX, y, btnW, 45f), "Main Menu", buttonStyle))
            {
                if (Application.CanStreamedLevelBeLoaded("MainMenu"))
                    SceneManager.LoadScene("MainMenu");
                else
                {
                    Hide();
                    onRestart?.Invoke();
                }
            }

            GUI.color = Color.white;
        }

        private void DrawStatRow(float x, ref float y, float width, string label, string value)
        {
            GUI.Label(new Rect(x, y, width * 0.6f, 28f), label, statLabelStyle);
            GUI.Label(new Rect(x + width * 0.6f, y, width * 0.4f, 28f), value, statValueStyle);
            y += 32f;
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
