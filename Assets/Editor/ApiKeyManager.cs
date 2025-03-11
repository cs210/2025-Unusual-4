using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

/// <summary>
/// Manages API keys for the chatbot
/// </summary>
public static class ApiKeyManager
{
    // File path for the keys file (placed outside of Assets to avoid being part of the build)
    private static readonly string KEYS_DIRECTORY = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ApiKeys");
    private static readonly string KEYS_FILE_PATH = Path.Combine(KEYS_DIRECTORY, "api_keys.txt");
    
    // Dictionary to store key-value pairs
    private static Dictionary<string, string> apiKeys = new Dictionary<string, string>();
    
    // Key names for consistency
    public const string OPENAI_KEY = "OpenAI_API_Key";
    public const string CLAUDE_KEY = "Claude_API_Key";
    
    private static string keyFilePath = Path.Combine(Application.dataPath, "Editor", "api_keys.dat");
    
    // Encryption key (this is just a simple obfuscation, not true security)
    private static readonly string EncryptionKey = "XeleR_Unity_Chatbot_Key";
    
    // Initialize and load keys
    static ApiKeyManager()
    {
        LoadKeys();
    }
    
    /// <summary>
    /// Gets an API key from EditorPrefs
    /// </summary>
    public static string GetKey(string keyName)
    {
        string encryptedKey = EditorPrefs.GetString(keyName, "");
        
        if (string.IsNullOrEmpty(encryptedKey))
            return "";
            
        return Decrypt(encryptedKey);
    }
    
    /// <summary>
    /// Sets an API key in EditorPrefs
    /// </summary>
    public static void SetKey(string keyName, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            EditorPrefs.DeleteKey(keyName);
            return;
        }
        
        string encryptedKey = Encrypt(value);
        EditorPrefs.SetString(keyName, encryptedKey);
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
    
    /// <summary>
    /// Simple encryption for API keys (not truly secure, just basic obfuscation)
    /// </summary>
    private static string Encrypt(string text)
    {
        try
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey);
            
            // Create a simple XOR cipher
            byte[] result = new byte[textBytes.Length];
            for (int i = 0; i < textBytes.Length; i++)
            {
                result[i] = (byte)(textBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }
            
            // Convert to Base64 for storage
            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error encrypting API key: {ex.Message}");
            return "";
        }
    }
    
    /// <summary>
    /// Simple decryption for API keys
    /// </summary>
    private static string Decrypt(string encryptedText)
    {
        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey);
            
            // Reverse the XOR cipher
            byte[] result = new byte[encryptedBytes.Length];
            for (int i = 0; i < encryptedBytes.Length; i++)
            {
                result[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }
            
            return Encoding.UTF8.GetString(result);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error decrypting API key: {ex.Message}");
            return "";
        }
    }
}