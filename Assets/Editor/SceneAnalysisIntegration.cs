using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEditor;
using System.IO;
using System.Threading.Tasks;
using System;

/// <summary>
/// Integrates scene analysis functionality with the ChatbotEditorWindow
/// </summary>
public static class SceneAnalysisIntegration
{
    // Path for storing temporary scene captures
    private static readonly string TempCapturePath = "Temp/SceneCapture.png";
    
    // Path for storing metaprompts
    private static readonly string MetapromptsPath = "Assets/Editor/Metaprompts";
    
    // Cached scene analysis results
    private static string cachedSceneStructure = null;
    private static string cachedSpatialInfo = null;
    private static DateTime lastSceneAnalysisTime = DateTime.MinValue;
    
    // Cache timeout (5 minutes)
    private static readonly TimeSpan CacheTimeout = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Gets a summary of the current scene structure
    /// </summary>
    public static string GetSceneStructureSummary()
    {
        // Check if we have a recent cached result
        if (cachedSceneStructure != null && 
            (DateTime.Now - lastSceneAnalysisTime) < CacheTimeout)
        {
            return cachedSceneStructure;
        }
        
        var sceneInfo = new StringBuilder();
        sceneInfo.AppendLine("# Scene Structure Analysis");
        
        // Get all GameObjects in the scene
        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        
        // Count total objects
        int totalObjectCount = 0;
        CountObjectsRecursively(rootObjects, ref totalObjectCount);
        sceneInfo.AppendLine($"Total objects in scene: {totalObjectCount}");
        
        // Add hierarchy information
        sceneInfo.AppendLine("\n## Hierarchy:");
        foreach (var rootObj in rootObjects)
        {
            AppendGameObjectInfo(rootObj, sceneInfo, 0);
        }
        
        // Add camera information
        var cameras = GameObject.FindObjectsOfType<Camera>();
        if (cameras.Length > 0)
        {
            sceneInfo.AppendLine("\n## Cameras:");
            foreach (var camera in cameras)
            {
                sceneInfo.AppendLine($"- {camera.name}: Position {camera.transform.position}, Rotation {camera.transform.eulerAngles}");
            }
        }
        
        // Add light information
        var lights = GameObject.FindObjectsOfType<Light>();
        if (lights.Length > 0)
        {
            sceneInfo.AppendLine("\n## Lights:");
            foreach (var light in lights)
            {
                sceneInfo.AppendLine($"- {light.name}: Type {light.type}, Position {light.transform.position}");
            }
        }
        
        // Cache the result
        cachedSceneStructure = sceneInfo.ToString();
        lastSceneAnalysisTime = DateTime.Now;
        
        return cachedSceneStructure;
    }
    
    /// <summary>
    /// Gets spatial information about the scene
    /// </summary>
    public static string GetSpatialInformation()
    {
        // Check if we have a recent cached result
        if (cachedSpatialInfo != null && 
            (DateTime.Now - lastSceneAnalysisTime) < CacheTimeout)
        {
            return cachedSpatialInfo;
        }
        
        var spatialInfo = new StringBuilder();
        spatialInfo.AppendLine("# Spatial Analysis");
        
        // Capture a screenshot of the scene view
        CaptureSceneView();
        
        // Get all objects with colliders for spatial analysis
        var colliders = GameObject.FindObjectsOfType<Collider>();
        
        if (colliders.Length > 0)
        {
            spatialInfo.AppendLine("\n## Object Positions and Bounds:");
            
            foreach (var collider in colliders)
            {
                if (collider.gameObject.activeInHierarchy)
                {
                    Vector3 position = collider.transform.position;
                    Vector3 size = GetColliderSize(collider);
                    
                    spatialInfo.AppendLine($"- {collider.gameObject.name}: Position {position}, Size {size}");
                    
                    // Perform raycasts to detect spatial relationships
                    DetectSpatialRelationships(collider, spatialInfo);
                }
            }
        }
        
        // Add information about the scene bounds
        CalculateSceneBounds(spatialInfo);
        
        // Cache the result
        cachedSpatialInfo = spatialInfo.ToString();
        
        return cachedSpatialInfo;
    }
    
    /// <summary>
    /// Loads a metaprompt from file
    /// </summary>
    public static string LoadMetaprompt(string promptName)
    {
        string path = Path.Combine(MetapromptsPath, $"{promptName}.txt");
        
        if (File.Exists(path))
        {
            return File.ReadAllText(path);
        }
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(MetapromptsPath))
        {
            Directory.CreateDirectory(MetapromptsPath);
            
            // Create a default metaprompt
            string defaultPrompt = "You are a Unity scene analyzer. When analyzing scenes, focus on the spatial relationships between objects and their components.";
            File.WriteAllText(Path.Combine(MetapromptsPath, "SceneAnalyzer_RequestAware.txt"), defaultPrompt);
        }
        
        return string.Empty;
    }
    
    /// <summary>
    /// Cleans up any temporary resources
    /// </summary>
    public static void Cleanup()
    {
        // Delete temporary capture file if it exists
        if (File.Exists(TempCapturePath))
        {
            try
            {
                File.Delete(TempCapturePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to delete temporary scene capture: {ex.Message}");
            }
        }
        
        // Clear cached data
        cachedSceneStructure = null;
        cachedSpatialInfo = null;
    }
    
    #region Helper Methods
    
    private static void CountObjectsRecursively(GameObject[] objects, ref int count)
    {
        foreach (var obj in objects)
        {
            count++;
            
            int childCount = obj.transform.childCount;
            if (childCount > 0)
            {
                var childObjects = new GameObject[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    childObjects[i] = obj.transform.GetChild(i).gameObject;
                }
                
                CountObjectsRecursively(childObjects, ref count);
            }
        }
    }
    
    private static void AppendGameObjectInfo(GameObject obj, StringBuilder sb, int depth)
    {
        // Create indentation based on depth
        string indent = new string(' ', depth * 2);
        
        // Append object name and active state
        sb.AppendLine($"{indent}- {obj.name} {(obj.activeInHierarchy ? "(Active)" : "(Inactive)")}");
        
        // Append component information
        var components = obj.GetComponents<Component>();
        if (components.Length > 1) // Always has at least Transform
        {
            foreach (var component in components)
            {
                if (component != null && !(component is Transform)) // Skip Transform as it's on every GameObject
                {
                    sb.AppendLine($"{indent}  * {component.GetType().Name}");
                }
            }
        }
        
        // Recursively process children
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            AppendGameObjectInfo(obj.transform.GetChild(i).gameObject, sb, depth + 1);
        }
    }
    
    private static void CaptureSceneView()
    {
        // Get the active scene view
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            Debug.LogWarning("No active scene view found for capture");
            return;
        }
        
        // Capture the scene view
        RenderTexture rt = new RenderTexture(1024, 768, 24);
        sceneView.camera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(1024, 768, TextureFormat.RGB24, false);
        sceneView.camera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, 1024, 768), 0, 0);
        sceneView.camera.targetTexture = null;
        RenderTexture.active = null;
        
        // Save to file
        byte[] bytes = screenShot.EncodeToPNG();
        File.WriteAllBytes(TempCapturePath, bytes);
        
        // Clean up
        UnityEngine.Object.DestroyImmediate(screenShot);
        rt.Release();
    }
    
    private static Vector3 GetColliderSize(Collider collider)
    {
        if (collider is BoxCollider boxCollider)
        {
            return Vector3.Scale(boxCollider.size, boxCollider.transform.lossyScale);
        }
        else if (collider is SphereCollider sphereCollider)
        {
            float radius = sphereCollider.radius * Mathf.Max(
                collider.transform.lossyScale.x,
                collider.transform.lossyScale.y,
                collider.transform.lossyScale.z);
            return new Vector3(radius * 2, radius * 2, radius * 2);
        }
        else if (collider is CapsuleCollider capsuleCollider)
        {
            float radius = capsuleCollider.radius * Mathf.Max(
                collider.transform.lossyScale.x,
                collider.transform.lossyScale.z);
            float height = capsuleCollider.height * collider.transform.lossyScale.y;
            return new Vector3(radius * 2, height, radius * 2);
        }
        else
        {
            // For other collider types, use bounds
            return collider.bounds.size;
        }
    }
    
    private static void DetectSpatialRelationships(Collider collider, StringBuilder sb)
    {
        GameObject obj = collider.gameObject;
        Vector3 position = obj.transform.position;
        
        // Cast rays in 6 directions to detect nearby objects
        Vector3[] directions = new Vector3[]
        {
            Vector3.up,
            Vector3.down,
            Vector3.left,
            Vector3.right,
            Vector3.forward,
            Vector3.back
        };
        
        string[] directionNames = new string[]
        {
            "above",
            "below",
            "to the left of",
            "to the right of",
            "in front of",
            "behind"
        };
        
        for (int i = 0; i < directions.Length; i++)
        {
            RaycastHit hit;
            // Use the collider's bounds to determine ray origin and distance
            float distance = 10f; // Maximum detection distance
            Vector3 rayOrigin = position + Vector3.Scale(directions[i], collider.bounds.extents);
            
            if (Physics.Raycast(rayOrigin, directions[i], out hit, distance))
            {
                if (hit.collider != collider && hit.collider.gameObject != obj)
                {
                    sb.AppendLine($"  * {hit.collider.gameObject.name} is {directionNames[i]} {obj.name} (distance: {hit.distance:F2})");
                }
            }
        }
    }
    
    private static void CalculateSceneBounds(StringBuilder sb)
    {
        // Find all renderers in the scene
        var renderers = GameObject.FindObjectsOfType<Renderer>();
        
        if (renderers.Length == 0)
        {
            sb.AppendLine("\n## Scene Bounds: No renderers found in scene");
            return;
        }
        
        // Calculate combined bounds
        Bounds sceneBounds = new Bounds(renderers[0].bounds.center, renderers[0].bounds.size);
        foreach (var renderer in renderers)
        {
            if (renderer.gameObject.activeInHierarchy)
            {
                sceneBounds.Encapsulate(renderer.bounds);
            }
        }
        
        sb.AppendLine($"\n## Scene Bounds: Center {sceneBounds.center}, Size {sceneBounds.size}, Min {sceneBounds.min}, Max {sceneBounds.max}");
    }
    
    #endregion
} 