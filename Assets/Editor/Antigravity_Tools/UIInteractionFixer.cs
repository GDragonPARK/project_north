using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;

public class UIInteractionFixer : EditorWindow
{
    [MenuItem("Antigravity/Fix UI Interaction")]
    public static void FixUIInteraction()
    {
        // 1. Check EventSystem
        var eventSystem = GameObject.Find("EventSystem");
        if (eventSystem != null)
        {
            var standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                DestroyImmediate(standalone);
                if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                {
                    eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
                Debug.Log("[UIInteractionFixer] Replaced StandaloneInputModule with InputSystemUIInputModule.");
            }
        }

        // 2. Fix Raycast Blocking
        var canvas = GameObject.Find("Canvas_Login");
        if (canvas != null)
        {
            // Disable Raycast Target for non-interactive elements
            DisableRaycastOn(canvas.transform, "BG_Overlay");
            DisableRaycastOn(canvas.transform, "TitleGroup/TitleText");
            DisableRaycastOn(canvas.transform, "ConnectPanel/Header");
            DisableRaycastOn(canvas.transform, "ConnectPanel/StatusText");
            DisableRaycastOn(canvas.transform, "Footer/VersionText");
            
            // Also disable on ConnectPanel itself if it has an Image
            var cpImg = canvas.transform.Find("ConnectPanel")?.GetComponent<Image>();
            if (cpImg != null) cpImg.raycastTarget = false;

            // 3. Button Navigation
            var connectBtn = canvas.transform.Find("ConnectPanel/ButtonArea/ConnectButton")?.GetComponent<Button>();
            if (connectBtn != null)
            {
                var nav = connectBtn.navigation;
                nav.mode = Navigation.Mode.None;
                connectBtn.navigation = nav;
                Debug.Log("[UIInteractionFixer] Set ConnectButton navigation to None.");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log("[UIInteractionFixer] ✅ UI Interaction fixes applied!");
    }

    private static void DisableRaycastOn(Transform root, string path)
    {
        var target = root.Find(path);
        if (target != null)
        {
            var img = target.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;

            var tmp = target.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.raycastTarget = false;
            
            Debug.Log($"[UIInteractionFixer] Disabled Raycast Target on: {path}");
        }
    }
}
