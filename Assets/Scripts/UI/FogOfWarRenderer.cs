// =============================================================================
// FogOfWarRenderer.cs — Fog of War Rendering with Probability Overlays
// Renders the fog-of-war overlay using shader-based cell masking and
// displays probability annotations from the POMDP belief state on
// unrevealed cells. Probability overlays (e.g., '0.45') appear on fog
// cells to visualize the belief-state probability of enemy presence.
// Uses only core Unity APIs (TextMesh for world-space labels).
// Corresponds to Implementation Step 9.
// =============================================================================

using UnityEngine;
using System.Collections.Generic;

namespace VeilOfUncertainty
{
    /// <summary>
    /// Manages the fog-of-war visual system. Revealed cells show terrain
    /// and entities; unrevealed cells appear darkened. Partially revealed
    /// (scouted) cells display a '?' indicator. Probability overlays show
    /// the belief-state probability directly on fog cells, visualizing
    /// the POMDP belief state as described in the proposal's screen design.
    /// </summary>
    public class FogOfWarRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridWorld gridWorld;

        [Header("Fog Colors")]
        [SerializeField] private Color fogColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        [SerializeField] private Color partialFogColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
        [SerializeField] private Color dangerHighlightColor = new Color(1f, 0.3f, 0.3f, 0.6f);
        [SerializeField] private Color safeHighlightColor = new Color(0.3f, 1f, 0.3f, 0.4f);

        [Header("Overlay Settings")]
        [SerializeField] private bool showProbabilityOverlays = true;
        [SerializeField] private float overlayUpdateInterval = 0.5f;

        // Internal tracking
        private Dictionary<Vector2Int, GameObject> probabilityLabels;
        private Dictionary<Vector2Int, GameObject> fogOverlays;
        private BeliefState beliefState;
        private float lastUpdateTime;

        /// <summary>
        /// Initializes the fog renderer with reference to the belief state.
        /// Creates overlay objects for all grid cells.
        /// </summary>
        public void Initialize(GridWorld world, BeliefState belief)
        {
            gridWorld = world;
            beliefState = belief;
            probabilityLabels = new Dictionary<Vector2Int, GameObject>();
            fogOverlays = new Dictionary<Vector2Int, GameObject>();

            CreateOverlays();
        }

        /// <summary>
        /// Creates fog overlay objects and probability label texts for all cells.
        /// Uses TextMesh (3D text) for world-space probability labels.
        /// </summary>
        private void CreateOverlays()
        {
            for (int x = 0; x < gridWorld.Width; x++)
            {
                for (int y = 0; y < gridWorld.Height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    Vector3 worldPos = gridWorld.CellToWorldPosition(x, y);

                    // Create fog overlay quad
                    GameObject fogQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    fogQuad.name = $"FogOverlay_{x}_{y}";
                    fogQuad.transform.SetParent(transform);
                    fogQuad.transform.position = worldPos + Vector3.up * 0.15f;
                    fogQuad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    fogQuad.transform.localScale = new Vector3(1.9f, 1.9f, 1f);

                    // Remove collider (visual only)
                    var collider = fogQuad.GetComponent<Collider>();
                    if (collider != null) Destroy(collider);

                    // Apply fog material
                    var renderer = fogQuad.GetComponent<Renderer>();
                    renderer.material = new Material(Shader.Find("Sprites/Default"));
                    renderer.material.color = fogColor;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;

                    fogOverlays[pos] = fogQuad;

                    // Create probability label using TextMesh (3D world-space text)
                    if (showProbabilityOverlays)
                    {
                        GameObject labelObj = new GameObject($"ProbLabel_{x}_{y}");
                        labelObj.transform.SetParent(transform);
                        labelObj.transform.position = worldPos + Vector3.up * 0.25f;
                        labelObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                        TextMesh label = labelObj.AddComponent<TextMesh>();
                        label.text = "";
                        label.characterSize = 0.15f;
                        label.fontSize = 48;
                        label.anchor = TextAnchor.MiddleCenter;
                        label.alignment = TextAlignment.Center;
                        label.color = Color.white;

                        // Ensure the text renders above the fog quad
                        var meshRenderer = labelObj.GetComponent<MeshRenderer>();
                        meshRenderer.sortingOrder = 10;

                        probabilityLabels[pos] = labelObj;
                    }

                    // Hide for revealed cells
                    GridCell cell = gridWorld.GetCell(x, y);
                    if (cell != null && cell.IsRevealed)
                    {
                        fogQuad.SetActive(false);
                        if (probabilityLabels.ContainsKey(pos))
                            probabilityLabels[pos].SetActive(false);
                    }
                }
            }
        }

        private void Update()
        {
            if (beliefState == null) return;

            // Throttle updates for performance
            if (Time.time - lastUpdateTime < overlayUpdateInterval) return;
            lastUpdateTime = Time.time;

            UpdateOverlays();
        }

        /// <summary>
        /// Updates all fog overlays and probability labels based on current
        /// belief state. This is the visualization of the POMDP belief state
        /// described in the proposal's screen design.
        /// </summary>
        public void UpdateOverlays()
        {
            for (int x = 0; x < gridWorld.Width; x++)
            {
                for (int y = 0; y < gridWorld.Height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    GridCell cell = gridWorld.GetCell(x, y);

                    if (cell == null) continue;

                    bool hasFog = fogOverlays.ContainsKey(pos);
                    bool hasLabel = probabilityLabels.ContainsKey(pos);

                    if (cell.IsRevealed)
                    {
                        if (hasFog) fogOverlays[pos].SetActive(false);
                        if (hasLabel) probabilityLabels[pos].SetActive(false);
                    }
                    else if (cell.IsPartiallyRevealed)
                    {
                        if (hasFog)
                        {
                            fogOverlays[pos].SetActive(true);
                            var renderer = fogOverlays[pos].GetComponent<Renderer>();
                            renderer.material.color = partialFogColor;
                        }
                        if (hasLabel)
                        {
                            probabilityLabels[pos].SetActive(true);
                            UpdateProbabilityLabel(pos, x, y);
                        }
                    }
                    else
                    {
                        if (hasFog)
                        {
                            fogOverlays[pos].SetActive(true);
                            CellBelief belief = beliefState.GetBelief(x, y);
                            float danger = belief.ProbEnemy + belief.ProbTrap;

                            Color tint = Color.Lerp(fogColor, dangerHighlightColor, danger * 0.5f);
                            var renderer = fogOverlays[pos].GetComponent<Renderer>();
                            renderer.material.color = tint;
                        }
                        if (hasLabel)
                        {
                            probabilityLabels[pos].SetActive(showProbabilityOverlays);
                            if (showProbabilityOverlays)
                                UpdateProbabilityLabel(pos, x, y);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates a single probability label with the current belief values.
        /// Shows the probability of danger (enemy + trap) as the primary number.
        /// </summary>
        private void UpdateProbabilityLabel(Vector2Int pos, int x, int y)
        {
            if (!probabilityLabels.ContainsKey(pos)) return;

            CellBelief belief = beliefState.GetBelief(x, y);
            float dangerProb = belief.ProbEnemy + belief.ProbTrap;

            TextMesh label = probabilityLabels[pos].GetComponent<TextMesh>();
            if (label != null)
            {
                label.text = dangerProb.ToString("F2");

                if (dangerProb > 0.5f)
                    label.color = new Color(1f, 0.4f, 0.4f);
                else if (dangerProb > 0.25f)
                    label.color = new Color(1f, 1f, 0.4f);
                else
                    label.color = new Color(0.4f, 1f, 0.4f);
            }
        }

        /// <summary>
        /// Toggles the probability overlay display on/off.
        /// </summary>
        public void ToggleProbabilityOverlays()
        {
            showProbabilityOverlays = !showProbabilityOverlays;
            foreach (var label in probabilityLabels.Values)
            {
                label.SetActive(showProbabilityOverlays);
            }
        }
    }
}
