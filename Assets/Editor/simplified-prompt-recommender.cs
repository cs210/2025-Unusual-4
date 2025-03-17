using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages and provides example prompts for the chat interface
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
        }},
        
        { "CodeHelp", new List<string> {
            "How can I improve this script's performance?",
            "Can you explain how to implement a singleton pattern in Unity?",
            "What's the best way to handle object pooling in my game?",
            "How should I structure my code for a state machine?",
            "Can you help me understand coroutines vs async/await in Unity?",
        }},
        
        { "UnityFeatures", new List<string> {
            "What's the best way to use the new Input System?",
            "How can I set up cinemachine for a third-person camera?",
            "What are the advantages of using Scriptable Objects for my game data?",
            "Can you explain how to use Unity's UI Builder effectively?",
            "How should I approach implementing a save system in my game?",
        }}
    };
    
    // Track previously suggested prompts to avoid repetition
    private static HashSet<string> usedPrompts = new HashSet<string>();
    
    /// <summary>
    /// Gets the welcome message with example prompts
    /// </summary>
    /// <returns>Welcome message with example prompts</returns>
    public static string GetWelcomeMessage()
    {
        List<string> examplePrompts = GetRandomPrompts(3);
        
        return $"Hello, how can I help you today? Feel free to try out these example prompts:\n\n" +
               $"• {examplePrompts[0]}\n" +
               $"• {examplePrompts[1]}\n" +
               $"• {examplePrompts[2]}\n\n" +
               $"You can also ask for more example prompts anytime.";
    }
    
    /// <summary>
    /// Gets additional example prompts when requested
    /// </summary>
    /// <param name="count">Number of prompts to return</param>
    /// <returns>List of example prompts</returns>
    public static List<string> GetRandomPrompts(int count = 3)
    {
        var result = new List<string>();
        var allPrompts = promptCategories.Values.SelectMany(v => v).ToList();
        
        // If we've used most of the prompts, reset the used prompts tracking
        if (usedPrompts.Count > allPrompts.Count * 0.7)
        {
            usedPrompts.Clear();
        }
        
        // Get available prompts that haven't been used yet
        var availablePrompts = allPrompts.Where(p => !usedPrompts.Contains(p)).ToList();
        
        // If we're running low on unused prompts, just use what's left
        if (availablePrompts.Count < count)
        {
            availablePrompts = allPrompts;
            usedPrompts.Clear();
        }
        
        // Shuffle available prompts and take the requested count
        ShuffleList(availablePrompts);
        
        for (int i = 0; i < Math.Min(count, availablePrompts.Count); i++)
        {
            result.Add(availablePrompts[i]);
            usedPrompts.Add(availablePrompts[i]);
        }
        
        return result;
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