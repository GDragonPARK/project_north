using UnityEngine;

public class StartupFloor : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnBeforeSceneLoadRuntimeMethod()
    {
        // This runs automatically when the game starts
        GameObject floor = GameObject.Find("Floor");
        if (floor == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(100, 1, 100);
            
            // Give it a dark color so it's visible
            Renderer rend = floor.GetComponent<Renderer>();
            if (rend != null)
            {
                // In URP, we need to assign a URP-compatible shader to avoid magenta color (pink map)
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit != null)
                {
                    rend.material = new Material(urpLit);
                }
                rend.material.color = new Color(0.2f, 0.2f, 0.2f);
            }
            
            Debug.Log("Auto-generated floor created at startup.");
        }
    }
}
