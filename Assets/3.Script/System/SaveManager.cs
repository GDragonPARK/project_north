using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class SaveData
{
    // Player Data
    public Vector3 playerPosition;
    public List<ItemSaveData> playerInventory = new List<ItemSaveData>();

    // World Data
    public float currentTime;
    public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
    public List<ContainerSaveData> containers = new List<ContainerSaveData>();
    public byte[] fogData;
}

[System.Serializable]
public class ItemSaveData
{
    public string itemName;
    public int amount;
}

[System.Serializable]
public class BuildingSaveData
{
    public string prefabName;
    public Vector3 position;
    public Quaternion rotation;
}

[System.Serializable]
public class ContainerSaveData
{
    public Vector3 position; // To identify which container
    public List<ItemSaveData> items = new List<ItemSaveData>();
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string m_savePath;
    public float autoSaveInterval = 300f; // 5 minutes
    private float m_autoSaveTimer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        m_savePath = Path.Combine(Application.persistentDataPath, "valheim_save.json");
    }

    private void Update()
    {
        m_autoSaveTimer += Time.deltaTime;
        if (m_autoSaveTimer >= autoSaveInterval)
        {
            SaveGame();
            m_autoSaveTimer = 0;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.f5Key.wasPressedThisFrame) // Quick Save
            {
                SaveGame();
            }

            if (Keyboard.current.f9Key.wasPressedThisFrame) // Quick Load
            {
                LoadGame();
            }
        }
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();

        // 1. Player Data
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            data.playerPosition = player.transform.position;
            foreach (var item in InventorySystem.Instance.items)
            {
                data.playerInventory.Add(new ItemSaveData { itemName = item.Key.itemName, amount = item.Value });
            }
        }

        // 2. Time Data
        if (TimeManager.Instance != null)
        {
            data.currentTime = TimeManager.Instance.currentTime;
        }

        // 3. Building Data
        // We find all objects on the "Building" layer or with a specific tag
        StructureStability[] buildings = Object.FindObjectsByType<StructureStability>(FindObjectsSortMode.None);
        foreach (var b in buildings)
        {
            data.buildings.Add(new BuildingSaveData {
                prefabName = b.gameObject.name.Replace("(Clone)", "").Trim(),
                position = b.transform.position,
                rotation = b.transform.rotation
            });
        }

        // 4. Container Data
        StorageContainer[] containers = Object.FindObjectsByType<StorageContainer>(FindObjectsSortMode.None);
        foreach (var c in containers)
        {
            ContainerSaveData cData = new ContainerSaveData { position = c.transform.position };
            foreach (var stack in c.GetItems())
            {
                cData.items.Add(new ItemSaveData { itemName = stack.item.itemName, amount = stack.amount });
            }
            data.containers.Add(cData);
        }

        // 5. Fog Data
        FogOfWar fog = Object.FindFirstObjectByType<FogOfWar>();
        if (fog != null) data.fogData = fog.GetFogData();

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(m_savePath, json);
        Debug.Log($"<color=green>Game Saved to {m_savePath}</color>");
    }

    public void LoadGame()
    {
        if (!File.Exists(m_savePath))
        {
            Debug.LogWarning("No save file found.");
            return;
        }

        string json = File.ReadAllText(m_savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // 1. Player Position & Inventory
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = data.playerPosition;
            // Clear and reload inventory
            InventorySystem.Instance.items.Clear();
            foreach (var iData in data.playerInventory)
            {
                ItemData item = FindItemByName(iData.itemName);
                if (item != null) InventorySystem.Instance.AddItem(item, iData.amount);
            }
        }

        // 2. Time
        if (TimeManager.Instance != null) TimeManager.Instance.currentTime = data.currentTime;

        // 3. Buildings
        // Clear existing buildings first
        StructureStability[] existingBuildings = Object.FindObjectsByType<StructureStability>(FindObjectsSortMode.None);
        foreach (var eb in existingBuildings) Destroy(eb.gameObject);

        foreach (var bData in data.buildings)
        {
            ItemData prefabData = FindBuildingPieceByName(bData.prefabName);
            if (prefabData != null && prefabData.itemPrefab != null)
            {
                Instantiate(prefabData.itemPrefab, bData.position, bData.rotation).name = bData.prefabName;
            }
        }

        // 4. Containers (The buildings above already instantiated the containers, now we fill them)
        // Wait a frame or find them immediately since Instantiate happens now
        StorageContainer[] loadedContainers = Object.FindObjectsByType<StorageContainer>(FindObjectsSortMode.None);
        foreach (var cData in data.containers)
        {
            foreach (var lc in loadedContainers)
            {
                if (Vector3.Distance(lc.transform.position, cData.position) < 0.1f)
                {
                    lc.GetItems().Clear();
                    foreach (var iData in cData.items)
                    {
                        ItemData item = FindItemByName(iData.itemName);
                        if (item != null) lc.AddItem(item, iData.amount);
                    }
                }
            }
        }

        // 5. Fog
        FogOfWar loadedFog = Object.FindFirstObjectByType<FogOfWar>();
        if (loadedFog != null && data.fogData != null) loadedFog.LoadFogData(data.fogData);

        Debug.Log("<color=cyan>Game Loaded Successfully.</color>");
    }

    private ItemData FindItemByName(string name)
    {
        // This is a helper. In a real project, you'd have an ItemDatabase SO.
        // For now, we search in the BuildManager's list or resources.
        if (BuildManager.Instance != null)
        {
            return BuildManager.Instance.buildablePieces.Find(p => p.itemName == name);
        }
        return null;
    }

    private ItemData FindBuildingPieceByName(string name)
    {
        if (BuildManager.Instance != null)
        {
            return BuildManager.Instance.buildablePieces.Find(p => p.itemName == name);
        }
        return null;
    }
}
