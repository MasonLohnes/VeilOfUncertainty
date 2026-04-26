// =============================================================================
// RuntimePrefabFactory.cs — Procedural Prefab Generator for Veil of Uncertainty
// Generates all game prefabs from code using Unity primitives and procedural
// materials. Eliminates the need for any manually-created assets.
// =============================================================================

using UnityEngine;

namespace VeilOfUncertainty
{
    /// <summary>
    /// Static utility class that creates all game prefabs at runtime using
    /// Unity primitives and procedural materials. Each Create method returns
    /// a deactivated GameObject template that can be instantiated by
    /// GridWorld and GameManager.
    /// </summary>
    public static class RuntimePrefabFactory
    {
        /// <summary>
        /// Creates a flat cube serving as a floor tile.
        /// Scale (1.9, 0.1, 1.9), light gray material.
        /// </summary>
        public static GameObject CreateCellPrefab()
        {
            GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cell.name = "CellPrefab";
            cell.transform.localScale = new Vector3(1.9f, 0.1f, 1.9f);

            var renderer = cell.GetComponent<Renderer>();
            Material mat = CreateStandardMaterial(new Color(0.75f, 0.75f, 0.78f));
            renderer.material = mat;

            cell.SetActive(false);
            return cell;
        }

        /// <summary>
        /// Creates a quad lying flat as a fog overlay.
        /// Scale (1.9, 1.9, 1), dark semi-transparent material.
        /// Rotated 90 degrees on X axis, positioned slightly above cell.
        /// </summary>
        public static GameObject CreateFogPrefab()
        {
            GameObject fog = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fog.name = "FogPrefab";
            fog.transform.localScale = new Vector3(1.9f, 1.9f, 1f);
            fog.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Remove collider (visual only)
            var collider = fog.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);

            var renderer = fog.GetComponent<Renderer>();
            Material mat = CreateTransparentMaterial(new Color(0.15f, 0.15f, 0.2f, 0.9f));
            renderer.material = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            fog.SetActive(false);
            return fog;
        }

        /// <summary>
        /// Creates a red sphere representing an enemy.
        /// Scale (0.6, 0.6, 0.6), red emissive material.
        /// </summary>
        public static GameObject CreateEnemyPrefab()
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            enemy.name = "EnemyPrefab";
            enemy.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var renderer = enemy.GetComponent<Renderer>();
            Color baseColor = new Color(0.9f, 0.2f, 0.2f);
            Material mat = CreateEmissiveMaterial(baseColor);
            renderer.material = mat;

            enemy.SetActive(false);
            return enemy;
        }

        /// <summary>
        /// Creates a thin cylinder representing a trap (pressure plate).
        /// Scale (0.5, 0.15, 0.5), orange material.
        /// </summary>
        public static GameObject CreateTrapPrefab()
        {
            GameObject trap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trap.name = "TrapPrefab";
            trap.transform.localScale = new Vector3(0.5f, 0.15f, 0.5f);

            var renderer = trap.GetComponent<Renderer>();
            Material mat = CreateStandardMaterial(new Color(1.0f, 0.6f, 0.1f));
            renderer.material = mat;

            trap.SetActive(false);
            return trap;
        }

        /// <summary>
        /// Creates a small rotated cube representing a resource (diamond shape).
        /// Scale (0.4, 0.4, 0.4), rotated 45 degrees on Y, blue-cyan emissive.
        /// </summary>
        public static GameObject CreateResourcePrefab()
        {
            GameObject resource = GameObject.CreatePrimitive(PrimitiveType.Cube);
            resource.name = "ResourcePrefab";
            resource.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            resource.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

            var renderer = resource.GetComponent<Renderer>();
            Color baseColor = new Color(0.2f, 0.7f, 1.0f);
            Material mat = CreateEmissiveMaterial(baseColor);
            renderer.material = mat;

            resource.SetActive(false);
            return resource;
        }

        /// <summary>
        /// Creates a green capsule representing the player.
        /// Scale (0.5, 0.5, 0.5), green emissive material.
        /// Includes a small child cube at front to indicate facing direction.
        /// </summary>
        public static GameObject CreatePlayerPrefab()
        {
            GameObject player = new GameObject("Player");

            // Main body — capsule
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "PlayerBody";
            body.transform.SetParent(player.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            var bodyRenderer = body.GetComponent<Renderer>();
            Color playerColor = new Color(0.2f, 0.9f, 0.3f);
            Material bodyMat = CreateEmissiveMaterial(playerColor);
            bodyRenderer.material = bodyMat;

            // Direction indicator — small cube at front
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.name = "DirectionIndicator";
            indicator.transform.SetParent(player.transform);
            indicator.transform.localPosition = new Vector3(0f, 0f, 0.3f);
            indicator.transform.localScale = new Vector3(0.15f, 0.15f, 0.3f);

            var indicatorRenderer = indicator.GetComponent<Renderer>();
            Color brightGreen = new Color(0.4f, 1f, 0.5f);
            Material indicatorMat = CreateEmissiveMaterial(brightGreen);
            indicatorRenderer.material = indicatorMat;

            player.SetActive(false);
            return player;
        }

        // =====================================================================
        // Material Creation Helpers
        // =====================================================================

        /// <summary>
        /// Creates a Standard shader material with the given color.
        /// </summary>
        private static Material CreateStandardMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Glossiness", 0.5f);
            return mat;
        }

        /// <summary>
        /// Creates a Standard shader material with emission enabled.
        /// </summary>
        private static Material CreateEmissiveMaterial(Color baseColor)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = baseColor;
            mat.SetFloat("_Glossiness", 0.5f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseColor * 0.3f);
            return mat;
        }

        /// <summary>
        /// Creates a Standard shader material in transparent rendering mode.
        /// </summary>
        public static Material CreateTransparentMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.SetFloat("_Glossiness", 0.0f);
            return mat;
        }
    }
}
