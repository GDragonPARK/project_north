using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainGenerator script = (TerrainGenerator)target;

        GUILayout.Space(20);
        GUI.backgroundColor = new Color(0.2f, 1f, 0.2f); // 녹색 버튼

        // 버튼: 함수 이름이 달라도 알아서 찾아내는 '스마트 버튼'
        if (GUILayout.Button("🌲 Generate Environment (Apply Density) 🌲", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("환경 재생성",
                "현재 환경을 모두 지우고 설정된 밀도(Density)대로 다시 심습니다.\n(플레이어는 안전합니다)",
                "실행", "취소"))
            {
                TriggerGeneration(script);
            }
        }
        GUI.backgroundColor = Color.white;
    }

    void TriggerGeneration(TerrainGenerator script)
    {
        // [수정됨] 마스터의 코드에 적힌 정확한 함수 이름 'GenerateTerrain'을 최우선으로 찾습니다.
        string[] potentialMethodNames = { "GenerateTerrain", "GenerateEnvironment", "Generate" };
        bool success = false;

        foreach (var methodName in potentialMethodNames)
        {
            MethodInfo method = script.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method != null)
            {
                Debug.Log($"✅ [Antigravity] '{methodName}' 함수를 포착! 환경 재구성을 시작합니다...");
                method.Invoke(script, null);
                success = true;
                break;
            }
        }

        if (!success) Debug.LogError("❌ 함수를 찾을 수 없습니다. (GenerateTerrain 등)");
    }
}