using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ApplyBasicTexturesMenu
{
    [MenuItem("Tools/Apply Basic Textures")]
    public static void DoTexture()
    {
        // Ensure materials exist
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        Material reactorMat = CreateMaterial("Assets/Materials/ReactorMat.mat", new Color(1f, 0.5f, 0f)); // Orange
        Material genMat = CreateMaterial("Assets/Materials/GeneratorMat.mat", new Color(0f, 0.5f, 1f));   // Blue
        Material doorMat = CreateMaterial("Assets/Materials/DoorMat.mat", new Color(0.3f, 0.3f, 0.3f));   // Dark Grey
        Material floorMat = CreateMaterial("Assets/Materials/FloorMat.mat", new Color(0.15f, 0.15f, 0.15f)); // Almost Black

        Material skyboxMat = new Material(Shader.Find("Skybox/Procedural") ?? Shader.Find("Standard"));
        skyboxMat.SetColor("_SkyTint", new Color(0.02f, 0.02f, 0.05f)); 
        skyboxMat.SetFloat("_SunSize", 0f); 
        AssetDatabase.CreateAsset(skyboxMat, "Assets/Materials/SkyboxMat.mat");

        RenderSettings.skybox = skyboxMat;

        int changedCount = 0;

        // Reactors
        var reactors = Object.FindObjectsOfType<SpaceMaintenance.ShipSystems.ReactorController>(true);
        foreach (var r in reactors) { ApplyToRenderers(r.gameObject, reactorMat); changedCount++; }

        // Generators
        var generators = Object.FindObjectsOfType<SpaceMaintenance.ShipSystems.GeneratorController>(true);
        foreach (var g in generators) { ApplyToRenderers(g.gameObject, genMat); changedCount++; }

        // Doors
        var doors = Object.FindObjectsOfType<SpaceMaintenance.ShipSystems.DoorController>(true);
        foreach (var d in doors) { ApplyToRenderers(d.gameObject, doorMat); changedCount++; }

        // Floor
        var allObjects = Object.FindObjectsOfType<GameObject>(true);
        foreach (var obj in allObjects)
        {
            if (obj.name.ToLower().Contains("floor") || obj.name.ToLower().Contains("ground"))
            {
                ApplyToRenderers(obj, floorMat);
                changedCount++;
            }
        }

        if (changedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"Successfully applied basic textures to {changedCount} objects!");
        }
        else
        {
            Debug.LogWarning("No objects found to texture! Make sure you are in main.unity");
        }
    }

    static Material CreateMaterial(string path, Color color)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Hidden/InternalErrorShader");
            mat = new Material(shader);
            
            if (mat.HasProperty("_Color")) mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            if (mat.HasProperty("_Color")) mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        }
        return mat;
    }

    static void ApplyToRenderers(GameObject root, Material mat)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
        }
    }
}
