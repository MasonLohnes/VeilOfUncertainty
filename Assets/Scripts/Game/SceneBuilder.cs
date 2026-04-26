// =============================================================================
// SceneBuilder.cs — Scene Bootstrap for Veil of Uncertainty
// MonoBehaviour that auto-bootstraps the entire game scene on Awake().
// Creates lighting, config, prefabs, grid, player, camera, UI, and wires
// all components together. This is the ONLY script that needs to be manually
// placed in the scene — everything else is created by it.
// =============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;

namespace VeilOfUncertainty
{
    /// <summary>
    /// SceneBuilder bootstraps the entire game scene programmatically.
    /// It auto-bootstraps via [RuntimeInitializeOnLoadMethod] so no manual
    /// scene setup is needed — just open main.unity and press Play.
    /// Everything else is created by this script.
    /// </summary>
    public class SceneBuilder : MonoBehaviour
    {
        /// <summary>
        /// Auto-bootstrap: registers for scene loads so the game scene
        /// is set up automatically without manual editor configuration.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "main") return;
            if (Object.FindFirstObjectByType<SceneBuilder>() != null) return;
            if (Object.FindFirstObjectByType<GameManager>() != null) return;

            var go = new GameObject("SceneBuilder");
            go.AddComponent<SceneBuilder>();
        }

        private void Awake()
        {
            BuildScene();
        }

        private void BuildScene()
        {
            // ---- 1. Lighting ----
            SetupLighting();

            // ---- 2. Create Runtime Config ----
            GameConfig config = ScriptableObject.CreateInstance<GameConfig>();

            // ---- 3. Generate Prefab Templates ----
            GameObject cellPrefab = RuntimePrefabFactory.CreateCellPrefab();
            GameObject fogPrefab = RuntimePrefabFactory.CreateFogPrefab();
            GameObject enemyPrefab = RuntimePrefabFactory.CreateEnemyPrefab();
            GameObject trapPrefab = RuntimePrefabFactory.CreateTrapPrefab();
            GameObject resourcePrefab = RuntimePrefabFactory.CreateResourcePrefab();
            GameObject playerPrefab = RuntimePrefabFactory.CreatePlayerPrefab();

            // ---- 4. Create GridWorld ----
            // Pass null for fogPrefab — FogOfWarRenderer handles all fog rendering
            // with belief-state coloring and probability labels
            GameObject gridObj = new GameObject("GridWorld");
            GridWorld gridWorld = gridObj.AddComponent<GridWorld>();
            gridWorld.SetPrefabs(cellPrefab, null, enemyPrefab, trapPrefab, resourcePrefab);

            // ---- 5. Create Player ----
            GameObject playerObj = Instantiate(playerPrefab);
            playerObj.name = "Player";
            playerObj.SetActive(true);
            playerObj.transform.position = new Vector3(0f, 0.5f, 0f);
            PlayerController playerController = playerObj.AddComponent<PlayerController>();

            // Add point light as "lantern" effect on player
            GameObject lantern = new GameObject("PlayerLantern");
            lantern.transform.SetParent(playerObj.transform);
            lantern.transform.localPosition = Vector3.up * 0.5f;
            Light lanternLight = lantern.AddComponent<Light>();
            lanternLight.type = LightType.Point;
            lanternLight.range = 8f;
            lanternLight.intensity = 1.5f;
            lanternLight.color = new Color(1f, 0.95f, 0.8f); // warm white

            // ---- 6. Setup Camera ----
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject camObj = new GameObject("MainCamera");
                camObj.tag = "MainCamera";
                mainCamera = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
            // Dark background
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
            // Position above grid center
            float gridCenterX = (10 - 1) * 2f / 2f;
            float gridCenterZ = (10 - 1) * 2f / 2f;
            mainCamera.transform.position = new Vector3(gridCenterX, 15f, gridCenterZ);
            mainCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            CameraController cameraController = mainCamera.gameObject.AddComponent<CameraController>();

            // ---- 7. Create UI Components ----
            GameObject uiObj = new GameObject("UIManager");
            GameUIManager uiManager = uiObj.AddComponent<GameUIManager>();
            HUDManager hudManager = uiObj.AddComponent<HUDManager>();
            DecisionNetworkPanel dnPanel = uiObj.AddComponent<DecisionNetworkPanel>();
            VPIPanel vpiPanel = uiObj.AddComponent<VPIPanel>();
            BeliefStatePanel bsPanel = uiObj.AddComponent<BeliefStatePanel>();

            GameObject fogRendererObj = new GameObject("FogOfWarRenderer");
            FogOfWarRenderer fogRenderer = fogRendererObj.AddComponent<FogOfWarRenderer>();

            // Wire UI panel references
            uiManager.SetPanels(dnPanel, vpiPanel, bsPanel, hudManager, fogRenderer);

            // ---- 8. Create GameOverScreen ----
            GameObject gameOverObj = new GameObject("GameOverScreen");
            GameOverScreen gameOverScreen = gameOverObj.AddComponent<GameOverScreen>();

            // ---- 9. Create AudioManager ----
            GameObject audioObj = new GameObject("AudioManager");
            audioObj.AddComponent<AudioManager>();

            // ---- 10. Create GameManager ----
            GameObject gmObj = new GameObject("GameManager");
            GameManager gameManager = gmObj.AddComponent<GameManager>();
            gameManager.Setup(config, gridWorld, uiManager, cameraController,
                              playerController, gameOverScreen);

            // ---- 11. Render Settings ----
            SetupAtmosphere();

            // Hide prefab templates (keep them for restart/re-instantiation)
            // They are already deactivated from the factory
            cellPrefab.transform.position = Vector3.one * -100f;
            enemyPrefab.transform.position = Vector3.one * -100f;
            trapPrefab.transform.position = Vector3.one * -100f;
            resourcePrefab.transform.position = Vector3.one * -100f;
            Destroy(fogPrefab); // FogOfWarRenderer handles fog, template not needed
            Destroy(playerPrefab); // Player was already instantiated, template not needed
        }

        private void SetupLighting()
        {
            // Remove existing directional lights to avoid duplicates
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                    Destroy(light.gameObject);
            }

            // Directional light
            GameObject lightObj = new GameObject("DirectionalLight");
            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1f, 0.97f, 0.9f); // warm white
            dirLight.intensity = 1.2f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            dirLight.shadows = LightShadows.Soft;

            // Ambient light
            RenderSettings.ambientLight = new Color(0.15f, 0.15f, 0.2f);
        }

        private void SetupAtmosphere()
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.1f, 0.1f, 0.15f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.015f;
        }
    }
}
