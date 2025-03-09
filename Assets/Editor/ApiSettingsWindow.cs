using UnityEditor;
using UnityEngine;

// Modified API Settings Window to use the key manager
public class ApiSettingsWindow : EditorWindow
{
    private ChatbotEditorWindow parentWindow;
    private string openAiKey;
    private string claudeKey;
    private bool showOpenAiKey = false;
    private bool showClaudeKey = false;
    private Vector2 scrollPosition;

    public void Initialize(ChatbotEditorWindow parent, string currentOpenAiKey, string currentClaudeKey)
    {
        parentWindow = parent;
        openAiKey = currentOpenAiKey;
        claudeKey = currentClaudeKey;
        titleContent = new GUIContent("API Settings");
        minSize = new Vector2(350, 180);
    }

    void OnGUI()
    {
        // Add draggable window title bar at the top
        GUI.Box(new Rect(0, 0, position.width, 20), "");
        GUI.Label(new Rect(10, 3, position.width - 20, 20), "API Settings");

        // Make the whole window scrollable
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(25); // Space after the title bar

        EditorGUILayout.LabelField("API Keys", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox("API keys are stored in a file outside of your project's Assets folder. Add ApiKeys/ to your .gitignore file to prevent them from being committed.", MessageType.Info);
        
        EditorGUILayout.Space();

        // OpenAI API Key
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("OpenAI API Key:", GUILayout.Width(120));
        
        // Toggle to show/hide the key
        showOpenAiKey = EditorGUILayout.ToggleLeft("Show", showOpenAiKey, GUILayout.Width(60));
        
        // If showing, display as normal text field, otherwise mask with password field
        if (showOpenAiKey)
            openAiKey = EditorGUILayout.TextField(openAiKey);
        else
            openAiKey = EditorGUILayout.PasswordField(openAiKey);
            
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Claude API Key
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Claude API Key:", GUILayout.Width(120));
        
        // Toggle to show/hide the key
        showClaudeKey = EditorGUILayout.ToggleLeft("Show", showClaudeKey, GUILayout.Width(60));
        
        // If showing, display as normal text field, otherwise mask with password field
        if (showClaudeKey)
            claudeKey = EditorGUILayout.TextField(claudeKey);
        else
            claudeKey = EditorGUILayout.PasswordField(claudeKey);
            
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(20);

        // Buttons at the bottom
        EditorGUILayout.BeginHorizontal();
        
        // Cancel button
        if (GUILayout.Button("Cancel", GUILayout.Width(100)))
        {
            this.Close();
        }
        
        GUILayout.FlexibleSpace();
        
        // Save button
        if (GUILayout.Button("Save", GUILayout.Width(100)))
        {
            // Save to key manager instead of directly to parent window
            ApiKeyManager.SetKey(ApiKeyManager.OPENAI_KEY, openAiKey);
            ApiKeyManager.SetKey(ApiKeyManager.CLAUDE_KEY, claudeKey);
            
            // Update parent window with new keys
            parentWindow.SetApiKeys(openAiKey, claudeKey);
            
            this.Close();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndScrollView();
    }

    // This makes the window draggable by allowing click+drag anywhere
    void OnEnable()
    {
        // Set window to be draggable by default
        this.wantsMouseMove = true;
    }
}