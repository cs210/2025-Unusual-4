using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// Modified API Settings Window to use the key manager
public class ApiSettingsWindow : EditorWindow
{
    private TextField openAiKeyField;
    private TextField claudeKeyField;
    private ChatbotEditorWindow parentWindow;
    
    // These fields are used to toggle visibility of API keys
    private bool showClaudeKey = false;
    
    private Vector2 scrollPosition;

    public void Initialize(ChatbotEditorWindow parent, string openAiKey, string claudeKey)
    {
        parentWindow = parent;
        titleContent = new GUIContent("API Settings");
        
        // Create UI
        var root = rootVisualElement;
        
        // Fix: Use proper padding syntax for UIElements
        root.style.paddingLeft = 10;
        root.style.paddingRight = 10;
        root.style.paddingTop = 10;
        root.style.paddingBottom = 10;
        
        // Title
        var titleLabel = new Label("API Key Settings");
        titleLabel.style.fontSize = 16;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 10;
        root.Add(titleLabel);
        
        // OpenAI Key
        var openAiContainer = new VisualElement();
        openAiContainer.style.marginBottom = 10;
        
        var openAiLabel = new Label("OpenAI API Key:");
        openAiContainer.Add(openAiLabel);
        
        openAiKeyField = new TextField();
        openAiKeyField.style.width = Length.Percent(100);
        openAiKeyField.value = openAiKey;
        openAiKeyField.isPasswordField = true; // Always hide by default for security
        openAiContainer.Add(openAiKeyField);
        
        root.Add(openAiContainer);
        
        // Claude Key
        var claudeContainer = new VisualElement();
        claudeContainer.style.marginBottom = 10;
        
        var claudeLabel = new Label("Claude API Key:");
        claudeContainer.Add(claudeLabel);
        
        claudeKeyField = new TextField();
        claudeKeyField.style.width = Length.Percent(100);
        claudeKeyField.value = claudeKey;
        claudeKeyField.isPasswordField = true; // Always hide by default for security
        claudeContainer.Add(claudeKeyField);
        
        root.Add(claudeContainer);
        
        // Buttons
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.justifyContent = Justify.SpaceBetween;
        
        var saveButton = new Button(SaveKeys) { text = "Save" };
        saveButton.style.width = 80;
        buttonContainer.Add(saveButton);
        
        var cancelButton = new Button(Close) { text = "Cancel" };
        cancelButton.style.width = 80;
        buttonContainer.Add(cancelButton);
        
        root.Add(buttonContainer);
    }
    
    private void SaveKeys()
    {
        // Update the parent window with the new keys
        parentWindow.SetApiKeys(openAiKeyField.value, claudeKeyField.value);
        
        // Show a confirmation
        Debug.Log("API keys saved successfully");
        
        // Close the window
        Close();
    }

    // This makes the window draggable by allowing click+drag anywhere
    void OnEnable()
    {
        // Set window to be draggable by default
        this.wantsMouseMove = true;
    }
}