// =============================================================================
// HUDManager.cs — Heads-Up Display Manager for Veil of Uncertainty
// Displays the turn counter, player HP, scout count, resources, score,
// keyboard controls, and the AI advisor's recommendation.
// Uses IMGUI (OnGUI) for zero-dependency rendering.
// Corresponds to Implementation Step 9.
// =============================================================================

using UnityEngine;

namespace VeilOfUncertainty
{
    /// <summary>
    /// HUDManager controls the bottom HUD bar and advisor recommendation display.
    /// Uses Unity IMGUI so no external UI packages are required.
    /// Also renders a damage flash overlay when the player takes damage.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color healthyHPColor = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color warningHPColor = new Color(1f, 1f, 0.3f);
        [SerializeField] private Color criticalHPColor = new Color(1f, 0.3f, 0.3f);

        // Cached HUD state for OnGUI rendering
        private int displayTurn = 1;
        private int displayHP;
        private int displayMaxHP;
        private int displayScouts;
        private int displayResources;
        private int displayScore;
        private string displayStatus = "";
        private Color statusColor = Color.white;
        private string advisorAction = "";
        private Color advisorColor = Color.white;
        private string advisorExplanation = "";
        private float displayDifficulty = 1.0f;

        // Damage flash
        private float damageFlashAlpha = 0f;

        private GUIStyle boxStyle;
        private GUIStyle statStyle;
        private GUIStyle advisorStyle;
        private GUIStyle statusStyle;
        private GUIStyle controlsStyle;
        private bool stylesInitialized;

        public void Initialize(GameConfig config)
        {
            displayHP = config.startingHP;
            displayMaxHP = config.startingHP;
            displayScouts = config.startingScouts;
            displayResources = config.startingResources;
            displayScore = 0;
            displayTurn = 1;
            SetStatus("Your turn. Choose an action.");
        }

        public void UpdateTurn(int turn) => displayTurn = turn;
        public void UpdateHP(int currentHP, int maxHP) { displayHP = currentHP; displayMaxHP = maxHP; }
        public void UpdateScouts(int scoutsRemaining) => displayScouts = scoutsRemaining;
        public void UpdateResources(int resources) => displayResources = resources;
        public void UpdateScore(int score) => displayScore = score;
        public void UpdateDifficulty(float mod) => displayDifficulty = mod;

        public void UpdateAdvisorRecommendation(AdvisorRecommendation rec)
        {
            advisorAction = rec.ShouldScout
                ? $"SCOUT ({rec.ScoutTargetX},{rec.ScoutTargetY})"
                : rec.RecommendedAction.ToString().ToUpper();

            advisorColor = rec.RecommendedAction switch
            {
                PlayerAction.Scout => new Color(0.3f, 0.7f, 1f),
                PlayerAction.Move => new Color(0.3f, 1f, 0.3f),
                PlayerAction.Attack => new Color(1f, 0.3f, 0.3f),
                PlayerAction.Defend => new Color(1f, 1f, 0.3f),
                _ => Color.white
            };

            advisorExplanation = $"MEU={rec.MEU:F1}  {rec.Explanation}";
        }

        public void SetStatus(string message)
        {
            displayStatus = message;
            statusColor = Color.white;
        }

        public void ShowTurnResult(string result, bool isPositive)
        {
            displayStatus = result;
            statusColor = isPositive ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);
        }

        /// <summary>
        /// Triggers a red damage flash overlay effect.
        /// </summary>
        public void TriggerDamageFlash()
        {
            damageFlashAlpha = 0.3f;
        }

        private void Update()
        {
            // Decay damage flash
            if (damageFlashAlpha > 0f)
            {
                damageFlashAlpha = Mathf.Max(0f, damageFlashAlpha - Time.deltaTime * 2f);
            }
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.08f, 0.08f, 0.12f, 0.9f));

            statStyle = new GUIStyle(GUI.skin.label);
            statStyle.fontSize = 14;
            statStyle.fontStyle = FontStyle.Bold;

            advisorStyle = new GUIStyle(GUI.skin.label);
            advisorStyle.fontSize = 13;
            advisorStyle.fontStyle = FontStyle.Bold;

            statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.fontSize = 13;
            statusStyle.alignment = TextAnchor.MiddleCenter;

            controlsStyle = new GUIStyle(GUI.skin.label);
            controlsStyle.fontSize = 11;
            controlsStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            controlsStyle.alignment = TextAnchor.MiddleCenter;

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            // Damage flash overlay
            if (damageFlashAlpha > 0.01f)
            {
                Texture2D flashTex = MakeTex(2, 2, new Color(1f, 0f, 0f, damageFlashAlpha));
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), flashTex);
            }

            float hudH = 110f;
            float hudW = Screen.width;
            float hudY = Screen.height - hudH;

            GUI.Box(new Rect(0f, hudY, hudW, hudH), "", boxStyle);

            float y = hudY + 5f;

            // Stats bar: Turn | HP | Scouts | Resources | Score | Difficulty
            float hpPct = displayMaxHP > 0 ? (float)displayHP / displayMaxHP : 0f;
            Color hpColor = hpPct > 0.5f ? healthyHPColor : (hpPct > 0.25f ? warningHPColor : criticalHPColor);
            Color scoutColor = displayScouts > 0 ? Color.white : Color.gray;

            string diffLevel = displayDifficulty < 0.8f ? "LOW" : (displayDifficulty < 1.2f ? "MED" : "HIGH");

            float x = 15f;
            statStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y, 100f, 20f), $"Turn: {displayTurn}", statStyle); x += 100f;

            statStyle.normal.textColor = hpColor;
            GUI.Label(new Rect(x, y, 120f, 20f), $"HP: {displayHP}/{displayMaxHP}", statStyle); x += 120f;

            statStyle.normal.textColor = scoutColor;
            GUI.Label(new Rect(x, y, 110f, 20f), $"Scouts: {displayScouts}", statStyle); x += 110f;

            statStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y, 130f, 20f), $"Resources: {displayResources}", statStyle); x += 130f;

            GUI.Label(new Rect(x, y, 120f, 20f), $"Score: {displayScore}", statStyle); x += 120f;

            Color diffColor = displayDifficulty < 0.8f ? new Color(0.3f, 1f, 0.3f)
                : (displayDifficulty < 1.2f ? new Color(1f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f));
            statStyle.normal.textColor = diffColor;
            GUI.Label(new Rect(x, y, 160f, 20f), $"Diff: {diffLevel} ({displayDifficulty:F2}x)", statStyle);

            y += 24f;

            // Advisor recommendation
            advisorStyle.normal.textColor = advisorColor;
            GUI.Label(new Rect(15f, y, hudW - 30f, 20f),
                $"AI Advisor: {advisorAction}", advisorStyle);
            y += 20f;

            advisorStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            advisorStyle.fontStyle = FontStyle.Normal;
            advisorStyle.fontSize = 11;
            GUI.Label(new Rect(15f, y, hudW - 30f, 18f), advisorExplanation, advisorStyle);
            advisorStyle.fontStyle = FontStyle.Bold;
            advisorStyle.fontSize = 13;
            y += 20f;

            // Status message
            statusStyle.normal.textColor = statusColor;
            GUI.Label(new Rect(0f, y, hudW, 20f), displayStatus, statusStyle);
            y += 20f;

            // Controls
            GUI.Label(new Rect(0f, y, hudW, 18f),
                "[S] Scout  [A] Attack  [W/Arrows] Move  [F] Defend  " +
                "[D] DecisionNet  [V] VPI  [TAB] Belief  [C] Camera",
                controlsStyle);
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
