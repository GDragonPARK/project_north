using UnityEngine;
using UnityEditor;

public class SpawnerAttacher : EditorWindow
{
    [MenuItem("Tools/Setup Spawner")]
    public static void Setup()
    {
        GameObject managers = GameObject.Find("GameManagers");
        if (!managers) managers = new GameObject("GameManagers");

        PlayerSpawner spawner = managers.GetComponent<PlayerSpawner>();
        if (!spawner) spawner = managers.AddComponent<PlayerSpawner>();

        spawner.spawnPointName = "Spawn_Point";
        Debug.Log("Spawner System Ready.");
    }
}