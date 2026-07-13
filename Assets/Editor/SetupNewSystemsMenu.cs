using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SpaceMaintenance.Tasks;
using SpaceMaintenance.Tasks.UI;
using SpaceMaintenance.Minigames;
using SpaceMaintenance.Minigames.Games;
using SpaceMaintenance.Missions;
using SpaceMaintenance.Core;
using SpaceMaintenance.UI;
using TMPro;
using UnityEngine.UI;

public static class SetupNewSystemsMenu
{
    [MenuItem("SCRAPSHIFT/Setup New Systems")]
    public static void SetupSystems()
    {
        // 1. Create ScriptableObjects for Tasks
        string tasksPath = "Assets/Data/Tasks";
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(tasksPath)) AssetDatabase.CreateFolder("Assets/Data", "Tasks");

        TaskData genTask = CreateTaskData("TASK_REPAIR_GEN", "Repair Backup Generator", "The backup generator must be restarted.", SpaceMaintenance.Core.TaskPriority.High, 0, "Assets/Data/Tasks/Task_RepairGenerator.asset");
        TaskData reactTask = CreateTaskData("TASK_START_REACTOR", "Start Reactor", "Find the reactor and initiate startup.", SpaceMaintenance.Core.TaskPriority.Critical, 120f, "Assets/Data/Tasks/Task_StartReactor.asset");

        // 2. Create Prefabs Folders
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Minigames")) AssetDatabase.CreateFolder("Assets/Prefabs", "Minigames");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI")) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        // 3. TaskEntryUI Prefab
        GameObject taskEntryObj = new GameObject("TaskEntryUI", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(TaskEntryUI));
        var entryRect = taskEntryObj.GetComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(300, 60);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(taskEntryObj.transform, false);

        GameObject textObj = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(taskEntryObj.transform, false);
        var textComp = textObj.GetComponent<TextMeshProUGUI>();
        textComp.text = "Task Name";

        GameObject timerObj = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
        timerObj.transform.SetParent(taskEntryObj.transform, false);
        var timerComp = timerObj.GetComponent<TextMeshProUGUI>();
        timerComp.text = "00:00";

        var taskEntryUI = taskEntryObj.GetComponent<TaskEntryUI>();
        var serializedTaskEntry = new SerializedObject(taskEntryUI);
        serializedTaskEntry.FindProperty("_priorityIcon").objectReferenceValue = iconObj.GetComponent<Image>();
        serializedTaskEntry.FindProperty("_taskNameText").objectReferenceValue = textComp;
        serializedTaskEntry.FindProperty("_timerText").objectReferenceValue = timerComp;
        serializedTaskEntry.FindProperty("_backgroundImage").objectReferenceValue = taskEntryObj.GetComponent<Image>();
        serializedTaskEntry.FindProperty("_canvasGroup").objectReferenceValue = taskEntryObj.GetComponent<CanvasGroup>();
        serializedTaskEntry.ApplyModifiedProperties();

        GameObject savedTaskEntry = PrefabUtility.SaveAsPrefabAsset(taskEntryObj, "Assets/Prefabs/UI/TaskEntryUI.prefab");
        GameObject.DestroyImmediate(taskEntryObj);

        // 4. WireConnectMinigame Prefab
        GameObject wireObj = new GameObject("WireConnectMinigame", typeof(RectTransform), typeof(WireConnectMinigame));
        GameObject wireBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        wireBg.transform.SetParent(wireObj.transform, false);
        var wireBgImage = wireBg.GetComponent<Image>();
        wireBgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        var wireBgRect = wireBg.GetComponent<RectTransform>();
        wireBgRect.anchorMin = Vector2.zero;
        wireBgRect.anchorMax = Vector2.one;
        wireBgRect.sizeDelta = Vector2.zero;

        GameObject savedWireMinigame = PrefabUtility.SaveAsPrefabAsset(wireObj, "Assets/Prefabs/Minigames/WireConnectMinigame.prefab");
        GameObject.DestroyImmediate(wireObj);

        // 5. Setup Active Scene Managers
        var scene = EditorSceneManager.GetActiveScene();
        GameObject gameManagerObj = GameObject.Find("GameManager");
        if (gameManagerObj == null) gameManagerObj = new GameObject("GameManager");

        GetOrAddComponent<TaskManager>(gameManagerObj);
        var mm = GetOrAddComponent<MinigameManager>(gameManagerObj);
        GetOrAddComponent<MissionFlowController>(gameManagerObj);
        GetOrAddComponent<EconomyManager>(gameManagerObj);

        // Wire MinigameManager
        var mmSO = new SerializedObject(mm);
        mmSO.FindProperty("_wireConnectPrefab").objectReferenceValue = savedWireMinigame.GetComponent<WireConnectMinigame>();
        
        GameObject mmCanvasObj = GameObject.Find("MinigameCanvas");
        if (mmCanvasObj == null) 
        {
            mmCanvasObj = new GameObject("MinigameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            mmCanvasObj.transform.SetParent(gameManagerObj.transform);
            var mmCanvas = mmCanvasObj.GetComponent<Canvas>();
            mmCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mmCanvas.sortingOrder = 100;
        }
        mmSO.FindProperty("_minigameCanvas").objectReferenceValue = mmCanvasObj.GetComponent<Canvas>();
        mmSO.ApplyModifiedProperties();

        // 6. MissionHUD
        var hudObj = GameObject.FindObjectOfType<MissionHUD>(true);
        if (hudObj != null)
        {
            var hudSO = new SerializedObject(hudObj);
            
            Transform darkPromptT = hudObj.transform.Find("DarkShipPrompt");
            GameObject darkPrompt = darkPromptT != null ? darkPromptT.gameObject : null;
            if (darkPrompt == null)
            {
                darkPrompt = new GameObject("DarkShipPrompt", typeof(RectTransform), typeof(TextMeshProUGUI));
                darkPrompt.transform.SetParent(hudObj.transform, false);
                var dpText = darkPrompt.GetComponent<TextMeshProUGUI>();
                dpText.text = "FIND THE REACTOR";
                dpText.fontSize = 50;
                dpText.alignment = TextAlignmentOptions.Center;
                var dpRect = darkPrompt.GetComponent<RectTransform>();
                dpRect.anchorMin = new Vector2(0, 0.5f);
                dpRect.anchorMax = new Vector2(1, 0.5f);
            }
            
            Transform startPromptT = hudObj.transform.Find("StartupPrompt");
            GameObject startPrompt = startPromptT != null ? startPromptT.gameObject : null;
            if (startPrompt == null)
            {
                startPrompt = new GameObject("StartupPrompt", typeof(RectTransform), typeof(TextMeshProUGUI));
                startPrompt.transform.SetParent(hudObj.transform, false);
                var spText = startPrompt.GetComponent<TextMeshProUGUI>();
                spText.text = "REACTOR STARTING...";
                spText.fontSize = 50;
                spText.alignment = TextAlignmentOptions.Center;
                var spRect = startPrompt.GetComponent<RectTransform>();
                spRect.anchorMin = new Vector2(0, 0.5f);
                spRect.anchorMax = new Vector2(1, 0.5f);
                startPrompt.SetActive(false);
            }

            hudSO.FindProperty("_darkShipPrompt").objectReferenceValue = darkPrompt;
            hudSO.FindProperty("_startupPrompt").objectReferenceValue = startPrompt;
            hudSO.ApplyModifiedProperties();
        }

        // 7. TaskListUI
        var taskList = GameObject.FindObjectOfType<TaskListUI>(true);
        if (taskList != null)
        {
            var tlSO = new SerializedObject(taskList);
            tlSO.FindProperty("_taskEntryPrefab").objectReferenceValue = savedTaskEntry;
            tlSO.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("SCRAPSHIFT Systems Setup Completed successfully!");
    }

    private static TaskData CreateTaskData(string id, string name, string desc, SpaceMaintenance.Core.TaskPriority priority, float timeLimit, string path)
    {
        TaskData data = AssetDatabase.LoadAssetAtPath<TaskData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<TaskData>();
            AssetDatabase.CreateAsset(data, path);
        }
        data.TaskId = id;
        data.DisplayName = name;
        data.Description = desc;
        data.Priority = priority;
        data.TimeLimit = timeLimit;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static T GetOrAddComponent<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();
        if (comp == null)
        {
            comp = obj.AddComponent<T>();
        }
        return comp;
    }
}
