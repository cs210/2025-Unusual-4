using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages and provides contextually appropriate prompt recommendations
/// </summary>
public static class PromptRecommender
{
    // Store example prompts categorized by context type
    private static Dictionary<string, List<string>> promptCategories = new Dictionary<string, List<string>>
    {
        { "SceneStructure", new List<string> {
            "Can you analyze the hierarchy of my scene?",
            "What are the key GameObjects I should focus on in this scene?",
            "How is this scene organized? Are there any improvements you'd suggest?",
            "Are there any performance concerns with my current scene setup?",
            "What design patterns do you notice in my scene structure?",
        }},
        
        { "ObjectRelationships", new List<string> {
            "How are objects spatially related in this scene?",
            "Can you identify any potential collision issues?",
            "Are there any objects that seem oddly positioned?",
            "Could you suggest better positioning for the key objects?",
            "How would you improve the layout of this scene?",
        }},
        
        { "ComponentAnalysis", new List<string> {
            "What components are most used in this scene?",
            "Are there any missing components I should add?",
            "Could you suggest optimizations for my component usage?",
            "Are there any GameObject's with too many components?",
            "Can you identify any component configurations that could cause issues?",
        }},
        
        { "SceneOptimization", new List<string> {
            "How can I optimize this scene for better performance?",
            "Are there any lighting issues I should address?",
            "What changes would you make to improve framerate?",
            "How could I improve the scene loading time?",
            "Can you help me identify performance bottlenecks in this scene?",
        }}
    };
    
    // Track previously suggested prompts to avoid repetition
    private static HashSet<string> suggestedPrompts = new HashSet<string>();
    
    /// <summary>
    /// Gets recommended prompts based on scene context and avoids repeating previous suggestions
    /// </summary>
    /// <returns>List of recommended prompts</returns>
    public static List<string> GetRecommendedPrompts(string sceneInfo = null, int count = 3)
    {
        var recommendations = new List<string>();
        var categoryKeys = promptCategories.Keys.ToList();
        ShuffleList(categoryKeys);
        
        // If scene info is provided, we could analyze it to prioritize certain categories
        // randomly select prompt
        int attempts = 0;
        while (recommendations.Count < count && attempts < 50)
        {
            string category = categoryKeys[attempts % categoryKeys.Count];
            var categoryPrompts = promptCategories[category];
            
            string prompt = categoryPrompts
                .Where(p => !suggestedPrompts.Contains(p))
                .OrderBy(_ => Guid.NewGuid()) // Shuffle remaining options
                .FirstOrDefault();
            
            if (string.IsNullOrEmpty(prompt) && categoryPrompts.Count > 0)
            {
                prompt = categoryPrompts[UnityEngine.Random.Range(0, categoryPrompts.Count)];
            }
            
            if (!string.IsNullOrEmpty(prompt) && !recommendations.Contains(prompt))
            {
                recommendations.Add(prompt);
                suggestedPrompts.Add(prompt);
            }
            
            attempts++;
        }
        if (suggestedPrompts.Count > promptCategories.Values.SelectMany(v => v).Count() * 0.7)
        {
            suggestedPrompts.Clear();
        }
        
        return recommendations;
    }
    
    /// <summary>
    /// Adds a new prompt to the suggestion system
    /// </summary>
    public static void AddPrompt(string category, string prompt)
    {
        if (!promptCategories.ContainsKey(category))
        {
            promptCategories[category] = new List<string>();
        }
        
        if (!promptCategories[category].Contains(prompt))
        {
            promptCategories[category].Add(prompt);
        }
    }
    
    /// <summary>
    /// Marks a prompt as used when the user selects it
    /// </summary>
    public static void MarkPromptAsUsed(string prompt)
    {
        suggestedPrompts.Add(prompt);
    }
    
    /// <summary>
    /// Helper method to shuffle a list
    /// </summary>
    private static void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}