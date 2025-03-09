using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

// This class handles loading and saving API keys from a local file
public class ApiKeyManager
{
    // File path for the keys file (placed outside of Assets to avoid being part of the build)
    private static readonly string KEYS_DIRECTORY = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ApiKeys");
    private static readonly string KEYS_FILE_PATH = Path.Combine(KEYS_DIRECTORY, "api_keys.txt");
    
    // Dictionary to store key-value pairs
    private static Dictionary<string, string> apiKeys = new Dictionary<string, string>();
    
    // Key names for consistency
    public const string OPENAI_KEY = "OPENAI_API_KEY";
    public const string CLAUDE_KEY = "CLAUDE_API_KEY";
    
    // Initialize and load keys
    static ApiKeyManager()
    {
        LoadKeys();
    }
    
    // Get a specific API key
    public static string GetKey(string keyName, string defaultValue = "")
    {
        if (apiKeys.TryGetValue(keyName, out string value))
        {
            return value;
        }
        return defaultValue;
    }
    
    // Set a specific API key
    public static void SetKey(string keyName, string value)
    {
        apiKeys[keyName] = value;
        SaveKeys();
    }
    
    // Load all keys from the file
    public static void LoadKeys()
    {
        apiKeys.Clear();
        
        if (!File.Exists(KEYS_FILE_PATH))
        {
            return;
        }
        
        try
        {
            string[] lines = File.ReadAllLines(KEYS_FILE_PATH);
            
            foreach (string line in lines)
            {
                // Skip comments and empty lines
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }
                
                int separator = line.IndexOf('=');
                if (separator > 0)
                {
                    string key = line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();
                    
                    // Remove quotes if they exist
                    if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    
                    apiKeys[key] = value;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading API keys: {ex.Message}");
        }
    }
    
    // Save all keys to the file
    public static void SaveKeys()
    {
        try
        {
            // Create directory if it doesn't exist
            Directory.CreateDirectory(KEYS_DIRECTORY);
            
            using (StreamWriter writer = new StreamWriter(KEYS_FILE_PATH))
            {
                writer.WriteLine("# API Keys - DO NOT COMMIT THIS FILE TO VERSION CONTROL");
                writer.WriteLine("# Format: KEY_NAME=value");
                writer.WriteLine();
                
                foreach (var pair in apiKeys)
                {
                    writer.WriteLine($"{pair.Key}={pair.Value}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving API keys: {ex.Message}");
        }
    }
}