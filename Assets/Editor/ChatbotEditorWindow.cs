using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Compilation;

[InitializeOnLoad]
public class ChatbotEditorWindow : EditorWindow
{
    // Static constructor that will be called when Unity starts or scripts are recompiled
    static ChatbotEditorWindow()
    {
        // Instead of doing work here, register for the first editor update
        EditorApplication.delayCall += OnFirstEditorUpdate;
    }

    // This method will be called once after Unity is fully initialized
    private static void OnFirstEditorUpdate()
    {
        // Clear any cached state about open windows
        EditorPrefs.DeleteKey("ChatbotEditorWindowOpen");

        // Use a delayed call to ensure Unity is fully initialized
        EditorApplication.delayCall += ForceOpenWindow;

        // Also subscribe to the projectOpened event to ensure it opens on project launch
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
    }

    // This is a one-time check that runs when the project window is first drawn
    private static bool hasOpenedOnLaunch = false;
    private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
    {
        if (!hasOpenedOnLaunch)
        {
            hasOpenedOnLaunch = true;
            // Unsubscribe to prevent this from running again
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
            // Open the window with a slight delay to ensure Unity is fully initialized
            EditorApplication.delayCall += ForceOpenWindow;
        }
    }

    // Force the window to open, regardless of any cached state
    private static void ForceOpenWindow()
    {
        // Close any existing instances first
        var existingWindows = Resources.FindObjectsOfTypeAll<ChatbotEditorWindow>();
        foreach (var window in existingWindows)
        {
            window.Close();
        }

        // Create a fresh window instance, docked next to the Inspector if possible
        var editorAssembly = typeof(UnityEditor.Editor).Assembly;
        var inspectorType = editorAssembly.GetType("UnityEditor.InspectorWindow");

        ChatbotEditorWindow newWindow;
        if (inspectorType == null)
        {
            // If Inspector isn't found at all, just open normally
            newWindow = GetWindow<ChatbotEditorWindow>("Chat x0", true);
        }
        else
        {
            // Attempt to dock next to the Inspector
            newWindow = GetWindow<ChatbotEditorWindow>("Chat x0", true, inspectorType);
        }

        newWindow.Show();
        newWindow.Focus();

        Debug.LogWarning("ChatbotEditorWindow: Forcing window to open");
        EditorPrefs.SetBool("ChatbotEditorWindowOpen", true);
    }

    // Add serialization for conversation history
    [SerializeField] private List<ChatMessage> conversationHistory = new List<ChatMessage>();

    // Add a list to store multiple chat sessions
    [SerializeField] private List<ChatSession> chatSessions = new List<ChatSession>();
    [SerializeField] private int currentSessionIndex = 0;

    // Add a key for EditorPrefs to store serialized chat sessions
    private const string CHAT_SESSIONS_KEY = "ChatbotEditorWindow_ChatSessions";
    private const string CURRENT_SESSION_INDEX_KEY = "ChatbotEditorWindow_CurrentSessionIndex";

    // Serializable class to store chat messages
    [Serializable]
    private class ChatMessage
    {
        public string Sender;
        public string Content;
        public bool IsFileContent;
        public string FileName;
    }

    // Serializable class to store a chat session
    [Serializable]
    private class ChatSession
    {
        public string Name;
        public List<ChatMessage> Messages = new List<ChatMessage>();
        public string LastLoadedScriptPath;
        public string LastLoadedScriptContent;
        public string LastLoadedScenePath;
        public bool IsSceneLoaded;
        public DateTime CreatedAt;

        public ChatSession(string name)
        {
            Name = name;
            CreatedAt = DateTime.Now;
        }
    }

    // Combined model selections
    private class ModelInfo
    {
        public string Name { get; set; }
        public string Provider { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    private List<ModelInfo> availableModels = new List<ModelInfo>
    {
        new ModelInfo { Name = "gpt-3.5-turbo", Provider = "OpenAI" },
        new ModelInfo { Name = "gpt-4", Provider = "OpenAI" },
        new ModelInfo { Name = "gpt-4-turbo", Provider = "OpenAI" },
        new ModelInfo { Name = "gpt-4o", Provider = "OpenAI" },
        new ModelInfo { Name = "claude-3-opus", Provider = "Claude" },
        new ModelInfo { Name = "claude-3-5-sonnet", Provider = "Claude" },
        new ModelInfo { Name = "claude-3-7-sonnet", Provider = "Claude" }
    };

    // Store selected model index for persistence
    [SerializeField] private int selectedModelIndex = 0;

    private const string PLACEHOLDER_TEXT = "Type your message...";
    private const string SCRIPTS_FOLDER = "Assets/Scripts";
    private const string SCENES_FOLDER = "Assets/Scenes";

    private ScrollView conversationScrollView;
    private TextField queryField;
    private Button sendButton;
    private Button browseScriptsButton;
    private Button browseScenesButton;
    private PopupField<ModelInfo> modelSelector;
    private PopupField<string> sessionSelector;
    private Button newChatButton;

    // Add these fields to store the last loaded script and scene information
    [SerializeField] private string lastLoadedScriptPath;
    [SerializeField] private string lastLoadedScriptContent;
    [SerializeField] private string lastLoadedScenePath;
    [SerializeField] private bool isSceneLoaded = false;

    private Button analyzeSceneButton;
    private Button spatialAnalysisButton;
    private Toggle includeSceneContextToggle;
    private bool includeSceneContext = false;

    [MenuItem("Window/Chatbox %i")]
    public static void ShowWindow()
    {
        // This overload attempts to dock it next to the Inspector
        var editorAssembly = typeof(UnityEditor.Editor).Assembly;
        var inspectorType = editorAssembly.GetType("UnityEditor.InspectorWindow");

        if (inspectorType == null)
        {
            // If for some reason we can't find InspectorWindow, just open normally
            Debug.LogWarning("InspectorWindow type not found; opening Chatbot without docking.");
            var wndFallback = GetWindow<ChatbotEditorWindow>("Chat v0", true);
            wndFallback.Show();
        }
        else
        {
            // Dock next to Inspector using the reflected type
            var wnd = GetWindow<ChatbotEditorWindow>(
                "Chat v0",
                true,
                inspectorType
            );
            wnd.Show();
        }
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        rootVisualElement.style.flexDirection = FlexDirection.Column;

        // Load chat sessions from EditorPrefs
        LoadChatSessionsFromEditorPrefs();

        // Initialize chat sessions if empty
        if (chatSessions.Count == 0)
        {
            // Migrate existing conversation to a session if needed
            if (conversationHistory.Count > 0)
            {
                var initialSession = new ChatSession("Chat 1");
                initialSession.Messages = new List<ChatMessage>(conversationHistory);
                initialSession.LastLoadedScriptPath = lastLoadedScriptPath;
                initialSession.LastLoadedScriptContent = lastLoadedScriptContent;
                initialSession.LastLoadedScenePath = lastLoadedScenePath;
                initialSession.IsSceneLoaded = isSceneLoaded;
                chatSessions.Add(initialSession);
            }
            else
            {
                chatSessions.Add(new ChatSession("Chat 1"));
            }
            currentSessionIndex = 0;

            // Save the initial state
            SaveChatSessionsToEditorPrefs();
        }

        // Scrollview for conversation
        conversationScrollView = new ScrollView(ScrollViewMode.Vertical);
        conversationScrollView.style.flexGrow = 1;
        conversationScrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
        rootVisualElement.Add(conversationScrollView);

        // Add a toolbar with buttons and model controls
        var toolbar = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                marginBottom = 4,
                marginLeft = 4,
                marginRight = 4,
                flexWrap = Wrap.Wrap,
                height = 22 // Set fixed height for toolbar
            }
        };

        // Add chat session selector
        var sessionContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                height = 22,
                marginRight = 8
            }
        };

        var sessionNames = chatSessions.Select(s => s.Name).ToList();
        sessionSelector = new PopupField<string>(
            sessionNames,
            Mathf.Min(currentSessionIndex, sessionNames.Count - 1)
        );
        sessionSelector.style.width = 120;
        sessionSelector.style.height = 22;
        sessionSelector.RegisterValueChangedCallback(OnSessionChanged);
        sessionContainer.Add(sessionSelector);

        // New chat button
        newChatButton = new Button(OnNewChatClicked) { text = "+" };
        newChatButton.style.width = 24;
        newChatButton.style.height = 22;
        sessionContainer.Add(newChatButton);

        toolbar.Add(sessionContainer);

        // Browse scripts button
        browseScriptsButton = new Button(OnBrowseScriptsClicked) { text = "Browse Scripts" };
        browseScriptsButton.style.height = 22; // Fixed height
        toolbar.Add(browseScriptsButton);

        // Browse scenes button
        browseScenesButton = new Button(OnBrowseScenesClicked) { text = "Browse Scenes" };
        browseScenesButton.style.height = 22; // Fixed height
        browseScenesButton.style.marginLeft = 4;
        toolbar.Add(browseScenesButton);

        // Model selection dropdown
        var modelContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                marginLeft = 8,
                height = 22 // Fixed height
            }
        };

        var modelLabel = new Label("Model: ");
        modelLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        modelContainer.Add(modelLabel);

        modelSelector = new PopupField<ModelInfo>(
            availableModels,
            selectedModelIndex
        );
        modelSelector.style.height = 22; // Fixed height
        modelSelector.RegisterValueChangedCallback(OnModelChanged);
        modelContainer.Add(modelSelector);

        toolbar.Add(modelContainer);

        // Add scene analysis buttons to the toolbar
        analyzeSceneButton = new Button(OnAnalyzeSceneClicked) { text = "Analyze Scene" };
        analyzeSceneButton.style.height = 22; // Fixed height
        toolbar.Add(analyzeSceneButton);

        spatialAnalysisButton = new Button(OnSpatialAnalysisClicked) { text = "Spatial Analysis" };
        spatialAnalysisButton.style.height = 22; // Fixed height
        toolbar.Add(spatialAnalysisButton);

        // Add a separator
        var separator = new VisualElement();
        separator.style.width = 1;
        separator.style.height = 22;
        separator.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        separator.style.marginLeft = 8;
        separator.style.marginRight = 8;
        toolbar.Add(separator);

        // Add toggle for including scene context in all queries - make it more prominent
        includeSceneContextToggle = new Toggle("Scene Context");
        includeSceneContextToggle.value = includeSceneContext;
        includeSceneContextToggle.style.height = 22;
        includeSceneContextToggle.style.marginLeft = 4;
        includeSceneContextToggle.style.backgroundColor = new Color(0.2f, 0.3f, 0.4f, 0.2f);
        includeSceneContextToggle.style.paddingLeft = 4;
        includeSceneContextToggle.style.paddingRight = 4;
        includeSceneContextToggle.style.borderTopWidth = 1;
        includeSceneContextToggle.style.borderBottomWidth = 1;
        includeSceneContextToggle.style.borderLeftWidth = 1;
        includeSceneContextToggle.style.borderRightWidth = 1;
        includeSceneContextToggle.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
        includeSceneContextToggle.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
        includeSceneContextToggle.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
        includeSceneContextToggle.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
        includeSceneContextToggle.RegisterValueChangedCallback(evt =>
        {
            includeSceneContext = evt.newValue;
            if (evt.newValue)
                AddMessageToHistory("System", "Scene context will be included in your queries.");
            else
                AddMessageToHistory("System", "Scene context disabled.");
        });
        toolbar.Add(includeSceneContextToggle);

        rootVisualElement.Add(toolbar);

        // Settings button for API keys
        var settingsButton = new Button(ShowApiKeySettings) { text = "API Settings" };
        settingsButton.style.alignSelf = Align.FlexEnd;
        settingsButton.style.marginRight = 4;
        settingsButton.style.height = 22; // Fixed height
        rootVisualElement.Add(settingsButton);

        // Input row - make this a fixed height container at the bottom
        var inputContainer = new VisualElement
        {
            style =
            {
                position = Position.Relative,
                bottom = 0,
                left = 0,
                right = 0,
                height = 40, // Fixed height for input area
                minHeight = 40,
                flexDirection = FlexDirection.Row,
                marginBottom = 4,
                marginLeft = 4,
                marginRight = 4
            }
        };

        // Query field
        queryField = new TextField();
        queryField.style.flexGrow = 1;
        queryField.style.marginRight = 4;
        queryField.style.height = 30; // Fixed height
        queryField.style.minHeight = 30;
        queryField.SetValueWithoutNotify(PLACEHOLDER_TEXT);
        queryField.AddToClassList("placeholder-text");
        inputContainer.Add(queryField);

        // Register keydown event for Enter key
        queryField.RegisterCallback<KeyDownEvent>(OnQueryFieldKeyDown);

        // Focus events for placeholder simulation
        queryField.RegisterCallback<FocusInEvent>(OnFocusInQueryField);
        queryField.RegisterCallback<FocusOutEvent>(OnFocusOutQueryField);

        // Send button
        sendButton = new Button(OnSendButtonClicked) { text = "Send" };
        sendButton.style.height = 30; // Fixed height
        inputContainer.Add(sendButton);

        rootVisualElement.Add(inputContainer);

        // Load API keys on startup
        string openAiKey = ApiKeyManager.GetKey(ApiKeyManager.OPENAI_KEY);
        string claudeKey = ApiKeyManager.GetKey(ApiKeyManager.CLAUDE_KEY);

        // If keys are missing, prompt user to enter them
        if (string.IsNullOrEmpty(openAiKey) && string.IsNullOrEmpty(claudeKey))
        {
            EditorApplication.delayCall += () =>
            {
                var settingsWindow = CreateInstance<ApiSettingsWindow>();
                settingsWindow.Initialize(this, "", "");
                settingsWindow.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 200);
                settingsWindow.ShowModal();
                AddMessageToHistory("System", "Please set up your API keys to continue.");
            };
        }

        // Restore conversation history from the current session
        RestoreCurrentSession();

        // Ensure we scroll to the bottom after restoring history
        EditorApplication.delayCall += ScrollToBottom;
    }

    private void OnModelChanged(ChangeEvent<ModelInfo> evt)
    {
        AddMessageToHistory("System", $"Model changed to {evt.newValue.Name} ({evt.newValue.Provider})");
    }

    private void ShowApiKeySettings()
    {
        // Create a simple popup window for API key settings
        var settingsWindow = CreateInstance<ApiSettingsWindow>();
        settingsWindow.Initialize(this,
            ApiKeyManager.GetKey(ApiKeyManager.OPENAI_KEY),
            ApiKeyManager.GetKey(ApiKeyManager.CLAUDE_KEY));

        // Center it on screen
        Vector2 mousePosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
        settingsWindow.position = new Rect(mousePosition.x, mousePosition.y, 400, 200);

        // Show as a normal window instead of popup for draggability
        settingsWindow.Show();
    }

    public void SetApiKeys(string newOpenAiKey, string newClaudeKey)
    {
        if (!string.IsNullOrEmpty(newOpenAiKey))
        {
            ApiKeyManager.SetKey(ApiKeyManager.OPENAI_KEY, newOpenAiKey);
        }
        if (!string.IsNullOrEmpty(newClaudeKey))
        {
            ApiKeyManager.SetKey(ApiKeyManager.CLAUDE_KEY, newClaudeKey);
        }
    }

    private void OnBrowseScriptsClicked()
    {
        // Create a dropdown menu with script files
        var menu = new GenericMenu();
        if (!Directory.Exists(SCRIPTS_FOLDER))
        {
            Directory.CreateDirectory(SCRIPTS_FOLDER);
            AssetDatabase.Refresh();
        }

        // Get all C# script files in the Scripts folder
        string[] scriptFiles = Directory.GetFiles(SCRIPTS_FOLDER, "*.cs", SearchOption.AllDirectories);

        foreach (string filePath in scriptFiles)
        {
            string relativePath = filePath.Replace("\\", "/"); // Normalize path for Unity
            menu.AddItem(new GUIContent(relativePath), false, () => LoadScriptFile(relativePath));
        }

        menu.ShowAsContext();
    }

    private void LoadScriptFile(string filePath)
    {
        try
        {
            string fileContent = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);

            // Add the file content to the conversation
            AddMessageToHistory("You", $"Show me the contents of {fileName}");
            AddFileContentToHistory(fileName, fileContent);

            // Store the file content and path for context in the next API call
            lastLoadedScriptPath = filePath;
            lastLoadedScriptContent = fileContent;

            // Optionally, ask the AI about the file
            string prompt = $"I'm looking at {fileName}. Can you explain what this script does?";
            queryField.SetValueWithoutNotify(prompt);
            queryField.RemoveFromClassList("placeholder-text");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading script file: {ex.Message}");
            AddMessageToHistory("System", $"Error loading file: {ex.Message}");
        }
    }

    private void AddFileContentToHistory(string fileName, string content)
    {
        // Display the file content in the conversation
        AddFileContentToHistoryWithoutSaving(fileName, content);

        // Save to current session's history
        if (currentSessionIndex >= 0 && currentSessionIndex < chatSessions.Count)
        {
            chatSessions[currentSessionIndex].Messages.Add(new ChatMessage
            {
                Sender = "File",
                Content = content,
                IsFileContent = true,
                FileName = fileName
            });
            SaveChatSessionsToEditorPrefs();
        }
    }

    private void AddFileContentToHistoryWithoutSaving(string fileName, string content)
    {
        var fileHeader = new Label
        {
            style =
            {
                whiteSpace = WhiteSpace.Normal,
                marginBottom = 4,
                marginTop = 8,
                unityFontStyleAndWeight = FontStyle.Bold
            },
            text = $"File: {fileName}"
        };

        var codeBlock = new TextField
        {
            multiline = true,
            style =
            {
                whiteSpace = WhiteSpace.Normal,
                marginBottom = 8,
                backgroundColor = new Color(0.2f, 0.2f, 0.2f),
                color = new Color(0.8f, 0.8f, 0.8f),
                paddingLeft = 8,
                paddingRight = 8,
                paddingTop = 4,
                paddingBottom = 4
            }
        };
        codeBlock.SetValueWithoutNotify(content);
        codeBlock.isReadOnly = true;

        conversationScrollView.Add(fileHeader);
        conversationScrollView.Add(codeBlock);
        EditorApplication.delayCall += ScrollToBottom;
    }

    private void OnSendButtonClicked()
    {
        if (queryField.value == PLACEHOLDER_TEXT)
        {
            queryField.value = string.Empty;
        }
        var userText = queryField.value?.Trim();
        if (string.IsNullOrEmpty(userText)) return;

        AddMessageToHistory("You", userText);

        // Temporarily disable input
        queryField.SetEnabled(false);
        sendButton.SetEnabled(false);
        queryField.SetValueWithoutNotify(string.Empty);

        // Get the current model and provider
        var selectedModel = modelSelector.value;
        string provider = selectedModel.Provider;

        // If user wants scene context, add it
        string contextEnhancedPrompt = userText;
        if (includeSceneContext)
        {
            string sceneContext = SceneAnalysisIntegration.GetSceneStructureSummary();
            contextEnhancedPrompt = $"[Scene Context]\n{sceneContext}\n\n[User Query]\n{userText}";

            AddMessageToHistory("System", "Including scene context in this query.");
        }

        // Send to the provider
        if (provider == "OpenAI")
        {
            SendQueryToOpenAI(contextEnhancedPrompt, selectedModel.Name, OnResponseReceived);
        }
        else if (provider == "Claude")
        {
            SendQueryToClaude(contextEnhancedPrompt, selectedModel.Name, OnResponseReceived);
        }
    }

    private void OnResponseReceived(string assistantReply, string providerName)
    {
        // Just show the entire reply immediately (no streaming)
        AddMessageToHistory("XeleR", assistantReply);

        // Check for code edits
        ProcessAndApplyCodeEdits(assistantReply);

        // Re-enable input
        queryField.SetEnabled(true);
        sendButton.SetEnabled(true);
        if (string.IsNullOrEmpty(queryField.value))
        {
            queryField.SetValueWithoutNotify(PLACEHOLDER_TEXT);
            queryField.AddToClassList("placeholder-text");
        }
    }

    private void ProcessAndApplyCodeEdits(string assistantReply)
    {
        // Pattern to match code blocks with file paths, e.g. ```csharp:Assets/Scripts/SomeFile.cs\n...```
        var codeBlockPattern = new Regex(@"```(?:csharp|cs):([^\n]+)\n([\s\S]*?)```");
        var matches = codeBlockPattern.Matches(assistantReply);

        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                string filePath = match.Groups[1].Value.Trim();
                string codeContent = match.Groups[2].Value;

                // Apply the edit to the file
                try
                {
                    ApplyEditToFile(filePath, codeContent);
                    AddMessageToHistory("System", $"Applied changes to {filePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error applying edit to {filePath}: {ex.Message}");
                    AddMessageToHistory("System", $"Error applying changes to {filePath}: {ex.Message}");
                }
            }
        }

        // Also handle potential scene edits, e.g. ```scene:ObjectPath/Component/Property=Value```
        ProcessSceneEdits(assistantReply);
    }

    private void ProcessSceneEdits(string assistantReply)
    {
        if (!isSceneLoaded) return; // Only if we have a scene loaded

        // Pattern to match scene edit instructions, e.g. ```scene:Main Camera/Camera/fieldOfView=60```
        var sceneEditPattern = new Regex(@"```scene:([^\n]+)```");
        var matches = sceneEditPattern.Matches(assistantReply);

        if (matches.Count > 0)
        {
            bool sceneModified = false;

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 2)
                {
                    string editInstruction = match.Groups[1].Value.Trim();
                    try
                    {
                        bool success = ApplySceneEdit(editInstruction);
                        if (success) sceneModified = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error applying scene edit: {ex.Message}");
                        AddMessageToHistory("System", $"Error applying scene edit: {ex.Message}");
                    }
                }
            }

            if (sceneModified)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                AddMessageToHistory("System", "Scene modifications applied. Remember to save your scene.");
            }
        }
    }

    private bool ApplySceneEdit(string editInstruction)
    {
        // Format: "Main Camera/Camera/fieldOfView=60"
        string[] parts = editInstruction.Split('=');
        if (parts.Length != 2)
        {
            AddMessageToHistory("System", $"Invalid scene edit format: {editInstruction}");
            return false;
        }

        string path = parts[0];
        string value = parts[1];

        string[] pathParts = path.Split('/');
        if (pathParts.Length < 2)
        {
            AddMessageToHistory("System", $"Invalid object path: {path}");
            return false;
        }

        // The last part is the property name; the second-to-last is the component
        string propertyName = pathParts[pathParts.Length - 1];
        string componentName = pathParts[pathParts.Length - 2];

        // Build the GameObject name from the earlier parts
        string objectPath = pathParts[0];
        for (int i = 1; i < pathParts.Length - 2; i++)
        {
            objectPath += "/" + pathParts[i];
        }

        // Find the GameObject
        GameObject targetObject = GameObject.Find(objectPath);
        if (targetObject == null)
        {
            AddMessageToHistory("System", $"GameObject not found: {objectPath}");
            return false;
        }

        // Find the component
        Component targetComponent = targetObject.GetComponent(componentName);
        if (targetComponent == null)
        {
            AddMessageToHistory("System", $"Component not found: {componentName} on {objectPath}");
            return false;
        }

        // Reflective property/field assignment
        try
        {
            var property = targetComponent.GetType().GetProperty(propertyName);
            if (property != null)
            {
                object convertedValue = ConvertValue(value, property.PropertyType);
                property.SetValue(targetComponent, convertedValue);
                AddMessageToHistory("System", $"Set {objectPath}/{componentName}/{propertyName} = {value}");
                return true;
            }

            var field = targetComponent.GetType().GetField(propertyName);
            if (field != null)
            {
                object convertedValue = ConvertValue(value, field.FieldType);
                field.SetValue(targetComponent, convertedValue);
                AddMessageToHistory("System", $"Set {objectPath}/{componentName}/{propertyName} = {value}");
                return true;
            }

            AddMessageToHistory("System", $"Property or field not found: {propertyName} on {componentName}");
            return false;
        }
        catch (Exception ex)
        {
            AddMessageToHistory("System", $"Error setting property: {ex.Message}");
            return false;
        }
    }

    private object ConvertValue(string value, Type targetType)
    {
        // Simple conversions for built-in Unity types
        if (targetType == typeof(float))
            return float.Parse(value);
        if (targetType == typeof(int))
            return int.Parse(value);
        if (targetType == typeof(bool))
            return bool.Parse(value);
        if (targetType == typeof(string))
            return value;

        // Try Vector3 or Vector2
        if (targetType == typeof(Vector3))
        {
            value = value.Trim('(', ')');
            string[] components = value.Split(',');
            if (components.Length == 3)
            {
                return new Vector3(
                    float.Parse(components[0]),
                    float.Parse(components[1]),
                    float.Parse(components[2])
                );
            }
        }
        else if (targetType == typeof(Vector2))
        {
            value = value.Trim('(', ')');
            string[] components = value.Split(',');
            if (components.Length == 2)
            {
                return new Vector2(
                    float.Parse(components[0]),
                    float.Parse(components[1])
                );
            }
        }
        else if (targetType == typeof(Color))
        {
            // Format: (r,g,b,a)
            value = value.Trim('(', ')');
            string[] components = value.Split(',');
            if (components.Length == 4)
            {
                return new Color(
                    float.Parse(components[0]),
                    float.Parse(components[1]),
                    float.Parse(components[2]),
                    float.Parse(components[3])
                );
            }
            else if (components.Length == 3)
            {
                return new Color(
                    float.Parse(components[0]),
                    float.Parse(components[1]),
                    float.Parse(components[2])
                );
            }
        }

        // Fallback: general Convert.ChangeType
        return Convert.ChangeType(value, targetType);
    }

    private void ApplyEditToFile(string filePath, string newContent)
    {
        bool isPartialEdit =
            newContent.Contains("// … existing code …") ||
            newContent.Contains("// existing code...") ||
            newContent.Contains("// ...");

        if (isPartialEdit)
        {
            // Very simplistic partial-edit approach
            ApplyPartialEdit(filePath, newContent);
        }
        else
        {
            // Full-file replacement
            File.WriteAllText(filePath, newContent);
            AssetDatabase.Refresh();
        }
    }

    private void ApplyPartialEdit(string filePath, string editContent)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }
        string originalContent = File.ReadAllText(filePath);

        // Remove placeholders like "// ...existing code..."
        var cleanedEdit = Regex.Replace(editContent,
            @"//\s*(?:…|\.\.\.)\s*existing code\s*(?:…|\.\.\.)",
            "");

        // For this simple approach, just overwrite the entire file
        File.WriteAllText(filePath, cleanedEdit);
        AssetDatabase.Refresh();
    }

    private void OnAnalyzeSceneClicked()
    {
        string sceneStructure = SceneAnalysisIntegration.GetSceneStructureSummary();
        AddMessageToHistory("You", "Analyze the current scene structure");
        AddMessageToHistory("System", sceneStructure);
    }

    private void OnSpatialAnalysisClicked()
    {
        string spatialInfo = SceneAnalysisIntegration.GetSpatialInformation();
        AddMessageToHistory("You", "Perform spatial analysis on the scene");
        AddMessageToHistory("System", spatialInfo);
    }

    private string EscapeJson(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t")
            .Replace("\b", "\\b")
            .Replace("\f", "\\f");
    }

    private void ClearScriptContext()
    {
        lastLoadedScriptPath = null;
        lastLoadedScriptContent = null;
        AddMessageToHistory("System", "Script context cleared.");
    }

    private void ClearConversationHistory()
    {
        if (currentSessionIndex >= 0 && currentSessionIndex < chatSessions.Count)
        {
            chatSessions[currentSessionIndex].Messages.Clear();
            if (conversationScrollView != null)
            {
                conversationScrollView.Clear();
            }
            AddMessageToHistory("System", "Conversation history cleared.");
        }
    }

    private void RestoreCurrentSession()
    {
        if (currentSessionIndex >= 0 && currentSessionIndex < chatSessions.Count)
        {
            var currentSession = chatSessions[currentSessionIndex];

            // Clear the conversation view
            conversationScrollView.Clear();

            // Restore session state
            lastLoadedScriptPath = currentSession.LastLoadedScriptPath;
            lastLoadedScriptContent = currentSession.LastLoadedScriptContent;
            lastLoadedScenePath = currentSession.LastLoadedScenePath;
            isSceneLoaded = currentSession.IsSceneLoaded;

            // Restore messages
            foreach (var message in currentSession.Messages)
            {
                if (message.IsFileContent)
                {
                    AddFileContentToHistoryWithoutSaving(message.FileName, message.Content);
                }
                else
                {
                    AddMessageToHistoryWithoutSaving(message.Sender, message.Content);
                }
            }

            EditorApplication.delayCall += ScrollToBottom;
        }
    }

    private void OnQueryFieldKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            // Don't send if Shift is held (allows for newlines)
            if (!evt.shiftKey)
            {
                evt.StopPropagation();
                OnSendButtonClicked();
            }
        }
    }

    private void ScrollToBottom()
    {
        if (conversationScrollView != null)
        {
            float fullHeight = conversationScrollView.contentContainer.layout.height;
            conversationScrollView.scrollOffset = new Vector2(0, fullHeight);
        }
    }

    private void OnEnable()
    {
        LoadChatSessionsFromEditorPrefs();
        if (rootVisualElement != null && conversationScrollView != null)
        {
            RestoreCurrentSession();
            EditorApplication.delayCall += ScrollToBottom;
        }
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.compilationFinished += OnCompilationFinished;
    }

    private void OnDisable()
    {
        SaveChatSessionsToEditorPrefs();
        CompilationPipeline.compilationStarted -= OnCompilationStarted;
        CompilationPipeline.compilationFinished -= OnCompilationFinished;
        SceneAnalysisIntegration.Cleanup();
    }

    private void OnCompilationStarted(object obj)
    {
        SaveChatSessionsToEditorPrefs();
    }

    private void OnCompilationFinished(object obj)
    {
        LoadChatSessionsFromEditorPrefs();
        if (rootVisualElement != null && conversationScrollView != null)
        {
            RestoreCurrentSession();
        }
        EditorApplication.delayCall += ScrollToBottom;
    }

    private void OnSessionChanged(ChangeEvent<string> evt)
    {
        int newIndex = chatSessions.FindIndex(s => s.Name == evt.newValue);
        if (newIndex >= 0 && newIndex < chatSessions.Count)
        {
            SaveCurrentSessionState();
            currentSessionIndex = newIndex;
            RestoreCurrentSession();
            SaveChatSessionsToEditorPrefs();
        }
    }

    private void OnNewChatClicked()
    {
        SaveCurrentSessionState();

        int newChatNumber = chatSessions.Count + 1;
        var newSession = new ChatSession($"Chat {newChatNumber}");
        chatSessions.Add(newSession);

        var sessionNames = chatSessions.Select(s => s.Name).ToList();
        sessionSelector.choices = sessionNames;

        currentSessionIndex = chatSessions.Count - 1;
        sessionSelector.index = currentSessionIndex;

        RestoreCurrentSession();
        AddMessageToHistory("System", $"Started new chat session: {newSession.Name}");

        SaveChatSessionsToEditorPrefs();
    }

    private void SaveCurrentSessionState()
    {
        if (currentSessionIndex >= 0 && currentSessionIndex < chatSessions.Count)
        {
            var currentSession = chatSessions[currentSessionIndex];
            currentSession.LastLoadedScriptPath = lastLoadedScriptPath;
            currentSession.LastLoadedScriptContent = lastLoadedScriptContent;
            currentSession.LastLoadedScenePath = lastLoadedScenePath;
            currentSession.IsSceneLoaded = isSceneLoaded;
        }
    }

    private void RenameCurrentSession(string newName)
    {
        if (currentSessionIndex >= 0 && currentSessionIndex < chatSessions.Count)
        {
            chatSessions[currentSessionIndex].Name = newName;
            var sessionNames = chatSessions.Select(s => s.Name).ToList();
            sessionSelector.choices = sessionNames;
            sessionSelector.index = currentSessionIndex;

            AddMessageToHistory("System", $"Renamed session to: {newName}");
            SaveChatSessionsToEditorPrefs();
        }
    }

    private void DeleteCurrentSession()
    {
        if (chatSessions.Count <= 1)
        {
            ClearConversationHistory();
            return;
        }

        if (currentSessionIndex >= 0 && currentSessionIndex < chatSessions.Count)
        {
            chatSessions.RemoveAt(currentSessionIndex);
            if (currentSessionIndex >= chatSessions.Count)
            {
                currentSessionIndex = chatSessions.Count - 1;
            }
            var sessionNames = chatSessions.Select(s => s.Name).ToList();
            sessionSelector.choices = sessionNames;
            sessionSelector.index = currentSessionIndex;

            RestoreCurrentSession();
            AddMessageToHistory("System", "Chat session deleted");
            SaveChatSessionsToEditorPrefs();
        }
    }

    private void AddMessageToHistoryWithoutSaving(string sender, string message)
    {
        var messageContainer = new VisualElement
        {
            style =
            {
                marginBottom = 8,
                paddingLeft = 4,
                paddingRight = 4
            }
        };

        var senderLabel = new Label
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                marginBottom = 2
            },
            text = sender + ":"
        };
        messageContainer.Add(senderLabel);

        if (sender == "XeleR")
        {
            var contentContainer = new VisualElement
            {
                style =
                {
                    marginLeft = 4,
                    marginRight = 4
                }
            };
            var formattedContent = MarkdownRenderer.RenderMarkdown(message);
            contentContainer.Add(formattedContent);
            messageContainer.Add(contentContainer);

            ProcessCodeBlocksInMessage(message);
        }
        else
        {
            var contentLabel = new Label
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    marginLeft = 4
                },
                text = message
            };
            messageContainer.Add(contentLabel);
        }

        conversationScrollView.Add(messageContainer);
    }

    private void SaveChatSessionsToEditorPrefs()
    {
        try
        {
            SaveCurrentSessionState();
            string sessionsJson = JsonUtility.ToJson(new ChatSessionsWrapper { Sessions = chatSessions });
            EditorPrefs.SetString(CHAT_SESSIONS_KEY, sessionsJson);
            EditorPrefs.SetInt(CURRENT_SESSION_INDEX_KEY, currentSessionIndex);
            Debug.Log("Chat sessions saved to EditorPrefs");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving chat sessions: {ex.Message}");
        }
    }

    private void LoadChatSessionsFromEditorPrefs()
    {
        try
        {
            if (EditorPrefs.HasKey(CHAT_SESSIONS_KEY))
            {
                string sessionsJson = EditorPrefs.GetString(CHAT_SESSIONS_KEY);
                var wrapper = JsonUtility.FromJson<ChatSessionsWrapper>(sessionsJson);
                if (wrapper != null && wrapper.Sessions != null && wrapper.Sessions.Count > 0)
                {
                    chatSessions = wrapper.Sessions;
                    if (EditorPrefs.HasKey(CURRENT_SESSION_INDEX_KEY))
                    {
                        currentSessionIndex = EditorPrefs.GetInt(CURRENT_SESSION_INDEX_KEY);
                        if (currentSessionIndex >= chatSessions.Count)
                        {
                            currentSessionIndex = chatSessions.Count - 1;
                        }
                    }
                    Debug.Log($"Loaded {chatSessions.Count} chat sessions from EditorPrefs");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading chat sessions: {ex.Message}");
            chatSessions = new List<ChatSession> { new ChatSession("Chat 1") };
            currentSessionIndex = 0;
        }
    }

    [Serializable]
    private class ChatSessionsWrapper
    {
        public List<ChatSession> Sessions;
    }
}
