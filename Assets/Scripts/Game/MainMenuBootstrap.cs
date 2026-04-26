// =============================================================================
// MainMenuBootstrap.cs — Main Menu Scene Bootstrap for Veil of Uncertainty
// Attach to the Main Camera in the MainMenu scene. Automatically adds the
// MainMenuManager component and configures the camera.
// =============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;

namespace VeilOfUncertainty
{
    /// <summary>
    /// Bootstraps the main menu scene. Auto-bootstraps via
    /// [RuntimeInitializeOnLoadMethod] so no manual setup is needed.
    /// </summary>
    public class MainMenuBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "MainMenu") return;
            if (Object.FindFirstObjectByType<MainMenuManager>() != null) return;

            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<MainMenuBootstrap>() == null)
            {
                cam.gameObject.AddComponent<MainMenuBootstrap>();
            }
        }

        private void Awake()
        {
            // Set dark background
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
            }

            // Add main menu manager if not already present
            if (GetComponent<MainMenuManager>() == null)
            {
                gameObject.AddComponent<MainMenuManager>();
            }
        }
    }
}
