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

// 1) A small class to help parse the JSON from the streaming chunks.
[Serializable]
public class OpenAIStreamChunk
{
    public string id;
    public string @object;
    public long created;
    public string model;
    public Choice[] choices;

    [Serializable]
    public class Choice
    {
        public Delta delta;
        public int index;
        public string finish_reason;
    }

    [Serializable]
    public class Delta
    {
        public string role;
        public string content;
    }
}


[InitializeOnLoad]
public class ChatbotEditorWindow : EditorWindow
{
    // Add a field to store the streaming message label
    private Label streamingMessageLabel;

    private void AddStreamingPlaceholderMessage()
    {
        // Create a container for the streaming message
        var messageContainer = new VisualElement
        {
            style =
            {
                marginBottom = 8,
                paddingLeft = 4,
                paddingRight = 4
            }
        };

        // Add the sender label
        var senderLabel = new Label("XeleR:")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                marginBottom = 2
            }
        };
        messageContainer.Add(senderLabel);

        // Create and store the streaming message label
        streamingMessageLabel = new Label("")
        {
            style =
            {
                whiteSpace = WhiteSpace.Normal,
                marginLeft = 4
            }
        };
        messageContainer.Add(streamingMessageLabel);

        // Add the container to the conversation view
        conversationScrollView.Add(messageContainer);
    }

    private void UpdateStreamingMessage(string newText)
    {
        if (streamingMessageLabel != null)
        {
            streamingMessageLabel.text += newText;
            // Optionally scroll to bottom
            EditorApplication.delayCall += ScrollToBottom;
        }
        else
        {
            Debug.LogWarning("Streaming message label is null. Unable to update streaming text.");
        }
    }


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

    // Add this as a class member
    private Button contextMenuButton;
    private VisualElement contextMenuDropdown;
    private bool isContextMenuOpen = false;

    private string[] selectedFiles = new string[0];

    // Add a reference to the sessionContainer
    private VisualElement sessionContainer;

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
        sessionContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                flexWrap = Wrap.Wrap, // Add this to allow wrapping if there are many options
                minHeight = 22,
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

        // Delete chat button
        var deleteChatButton = new Button(() =>
        {
            if (EditorUtility.DisplayDialog("Delete Chat", 
                "Are you sure you want to delete this chat session?", "Yes", "No"))
            {
                DeleteCurrentSession();
            }
        }) { text = "🗑" };

        deleteChatButton.style.width = 24;
        deleteChatButton.style.height = 22;
        sessionContainer.Add(deleteChatButton);

        // Add the @ context button for file browsing
        contextMenuButton = new Button(OnContextMenuButtonClicked) { text = "@ Context" };
        contextMenuButton.style.height = 22;
        contextMenuButton.style.width = 80;
        contextMenuButton.style.backgroundColor = new Color(0.2f, 0.4f, 0.6f);
        contextMenuButton.style.color = Color.white;
        contextMenuButton.style.borderTopLeftRadius = 4;
        contextMenuButton.style.borderTopRightRadius = 4;
        contextMenuButton.style.borderBottomLeftRadius = 4;
        contextMenuButton.style.borderBottomRightRadius = 4;
        contextMenuButton.style.marginLeft = 4; // Small margin to separate from the + button
        sessionContainer.Add(contextMenuButton); // Add to the sessionContainer, right after the newChatButton

        // Add a single Scene Analysis button
        var sceneAnalysisButton = new Button(OnSceneAnalysisClicked) { text = "Scene Analysis" };
        sceneAnalysisButton.style.height = 22;
        sceneAnalysisButton.style.marginLeft = 4;
        sceneAnalysisButton.style.paddingLeft = 8;
        sceneAnalysisButton.style.paddingRight = 8;
        sceneAnalysisButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.3f); // Green for scene analysis
        sceneAnalysisButton.style.color = Color.white;
        sceneAnalysisButton.style.borderTopLeftRadius = 4;
        sceneAnalysisButton.style.borderTopRightRadius = 4;
        sceneAnalysisButton.style.borderBottomLeftRadius = 4;
        sceneAnalysisButton.style.borderBottomRightRadius = 4;
        sceneAnalysisButton.style.borderTopWidth = 1;
        sceneAnalysisButton.style.borderBottomWidth = 1;
        sceneAnalysisButton.style.borderLeftWidth = 1;
        sceneAnalysisButton.style.borderRightWidth = 1;
        sceneAnalysisButton.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
        sceneAnalysisButton.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
        sceneAnalysisButton.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
        sceneAnalysisButton.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
        sessionContainer.Add(sceneAnalysisButton);

        // Add Scene Context toggle with a visible box
        var contextContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                marginLeft = 8
            }
        };

        includeSceneContextToggle = new Toggle
        {
            value = includeSceneContext
        };
        
        // Style the toggle to ensure the box is visible
        includeSceneContextToggle.style.marginRight = 4;
        
        // Make the checkmark box more visible
        var toggleCheckmark = includeSceneContextToggle.Q(className: "unity-toggle__checkmark");
        if (toggleCheckmark != null)
        {
            toggleCheckmark.style.width = 16;
            toggleCheckmark.style.height = 16;
            toggleCheckmark.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            toggleCheckmark.style.borderTopWidth = 1;
            toggleCheckmark.style.borderBottomWidth = 1;
            toggleCheckmark.style.borderLeftWidth = 1;
            toggleCheckmark.style.borderRightWidth = 1;
            toggleCheckmark.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f);
            toggleCheckmark.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f);
            toggleCheckmark.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f);
            toggleCheckmark.style.borderRightColor = new Color(0.5f, 0.5f, 0.5f);
        }
        
        includeSceneContextToggle.RegisterValueChangedCallback(evt => {
            includeSceneContext = evt.newValue;
            if (includeSceneContext)
                AddMessageToHistory("System", "Your current scene's context will be included in your queries.");
            else
                AddMessageToHistory("System", "Scene context disabled.");
        });
        
        contextContainer.Add(includeSceneContextToggle);
        
        var contextLabel = new Label("Quick Context");
        contextLabel.style.marginTop = 2; // Align with the toggle
        contextContainer.Add(contextLabel);

        // Add the context container to the session container
        sessionContainer.Add(contextContainer);

        // Create the context menu dropdown (initially hidden)
        contextMenuDropdown = new VisualElement();
        contextMenuDropdown.style.position = Position.Absolute;
        contextMenuDropdown.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        contextMenuDropdown.style.borderTopWidth = 1;
        contextMenuDropdown.style.borderBottomWidth = 1;
        contextMenuDropdown.style.borderLeftWidth = 1;
        contextMenuDropdown.style.borderRightWidth = 1;
        contextMenuDropdown.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
        contextMenuDropdown.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
        contextMenuDropdown.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
        contextMenuDropdown.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
        contextMenuDropdown.style.paddingTop = 5;
        contextMenuDropdown.style.paddingBottom = 5;
        contextMenuDropdown.style.display = DisplayStyle.None; // Hidden by default
        contextMenuDropdown.pickingMode = PickingMode.Position;
        rootVisualElement.Add(contextMenuDropdown);

        toolbar.Add(sessionContainer);

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

        // After adding all the buttons to sessionContainer, update the selected files display
        UpdateSelectedFilesDisplay();
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
        // No longer directly storing keys in class fields
        // Instead, update manager and perform any needed UI updates
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
            
            // Add the file path to the selected files array if not already in the array
            if (Array.IndexOf(selectedFiles, filePath) < 0) {
                Array.Resize(ref selectedFiles, selectedFiles.Length + 1);
                selectedFiles[selectedFiles.Length - 1] = filePath;
            }
            
            // Update the display of selected files
            UpdateSelectedFilesDisplay();
            
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
        // Add to UI
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
            
            // Save to EditorPrefs after adding file content
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
        
        // Scroll to bottom using the helper method
        EditorApplication.delayCall += ScrollToBottom;
    }

    private void OnSendButtonClicked()
    {
        // If placeholder, clear
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
        
        // Build context from selected files
        string filesContext = "";
        if (selectedFiles.Length > 0)
        {
            StringBuilder contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("[Selected Files Context]");
            
            foreach (string filePath in selectedFiles)
            {
                if (filePath.EndsWith(".unity"))
                {
                    // Load the scene if it's not already loaded
                    if (lastLoadedScenePath != filePath)
                    {
                        contextBuilder.AppendLine($"Scene: {Path.GetFileName(filePath)}");
                        contextBuilder.AppendLine("[Scene Structure]");
                        contextBuilder.AppendLine(SceneAnalysisIntegration.GetSceneStructureSummary());
                        contextBuilder.AppendLine("[Spatial Information]");
                        contextBuilder.AppendLine(SceneAnalysisIntegration.GetSpatialInformation());
                    }
                    else if (isSceneLoaded)
                    {
                        contextBuilder.AppendLine($"Scene: {Path.GetFileName(filePath)}");
                        contextBuilder.AppendLine("[Scene Structure]");
                        contextBuilder.AppendLine(SceneAnalysisIntegration.GetSceneStructureSummary());
                        contextBuilder.AppendLine("[Spatial Information]");
                        contextBuilder.AppendLine(SceneAnalysisIntegration.GetSpatialInformation());
                    }
                }
                else if (filePath.EndsWith(".cs")) // Script file
                {
                    try
                    {
                        string fileContent = File.ReadAllText(filePath);
                        contextBuilder.AppendLine($"Script: {Path.GetFileName(filePath)}");
                        contextBuilder.AppendLine("```csharp");
                        contextBuilder.AppendLine(fileContent);
                        contextBuilder.AppendLine("```");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error reading file {filePath}: {ex.Message}");
                    }
                }
            }
            
            filesContext = contextBuilder.ToString();
        }
        
        // Include scene context if the toggle is enabled
        string contextEnhancedPrompt = userText;
        if (includeSceneContext) {
            string sceneStructure = SceneAnalysisIntegration.GetSceneStructureSummary();
            string spatialInfo = SceneAnalysisIntegration.GetSpatialInformation();
            
            // Combine both types of information
            string combinedContext = $"[Scene Context]\n{sceneStructure}\n\n[Spatial Information]\n{spatialInfo}";
            
            // Add files context if available
            if (!string.IsNullOrEmpty(filesContext)) {
                contextEnhancedPrompt = $"{filesContext}\n\n{combinedContext}\n\n[User Query]\n{userText}";
            }
            else
            {
                contextEnhancedPrompt = $"{combinedContext}\n\n[User Query]\n{userText}";
            }
                
            // Add a system message to show the user we're including scene context
            AddMessageToHistory("System", "Including current scene context, spatial analysis, and selected files in this query.");
        }
        else if (!string.IsNullOrEmpty(filesContext))
        {
            // Only include files context
            contextEnhancedPrompt = $"{filesContext}\n\n[User Query]\n{userText}";
            AddMessageToHistory("System", "Including selected files in this query.");
        }
        
        // Send to the appropriate API based on the selected model's provider
        if (provider == "OpenAI")
        {
            SendQueryToOpenAIStreaming(contextEnhancedPrompt, selectedModel.Name, OnResponseReceived);
        }
        else if (provider == "Claude")
        {
            SendQueryToClaude(contextEnhancedPrompt, selectedModel.Name, OnResponseReceived);
        }
    }

    private void OnResponseReceived(string assistantReply, string providerName)
    {
        string displayName = $"XeleR";
        AddMessageToHistory(displayName, assistantReply);
        
        // Check if the response contains code edits and apply them
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
        // Pattern to match code blocks with file paths
        // Format: ```csharp:Assets/Scripts/SomeFile.cs ... ```
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
        
        // Add scene edit processing
        ProcessSceneEdits(assistantReply);
    }

    private void ProcessSceneEdits(string assistantReply)
    {
        // Only process scene edits if a scene is loaded
        if (!isSceneLoaded) return;
        
        // Pattern to match scene edit instructions
        // Format: ```scene:ObjectPath/Component/Property=Value```
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
                // Mark the scene as dirty so Unity knows it needs to be saved
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                
                AddMessageToHistory("System", "Scene modifications applied. Remember to save your scene.");
            }
        }
    }

    private bool ApplySceneEdit(string editInstruction)
    {
        // Parse the edit instruction
        // Format: ObjectPath/Component/Property=Value
        // Example: Main Camera/Camera/fieldOfView=60
        
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
        
        // The first part is the GameObject path
        string objectPath = pathParts[0];
        for (int i = 1; i < pathParts.Length - 1; i++)
        {
            objectPath += "/" + pathParts[i];
        }
        
        // The last part is the property name
        string propertyName = pathParts[pathParts.Length - 1];
        
        // The second-to-last part is the component name
        string componentName = pathParts[pathParts.Length - 2];
        
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
        
        // Set the property value using reflection
        try
        {
            var property = targetComponent.GetType().GetProperty(propertyName);
            if (property != null)
            {
                // Convert the value to the appropriate type
                object convertedValue = ConvertValue(value, property.PropertyType);
                property.SetValue(targetComponent, convertedValue);
                AddMessageToHistory("System", $"Set {objectPath}/{componentName}/{propertyName} = {value}");
                return true;
            }
            
            var field = targetComponent.GetType().GetField(propertyName);
            if (field != null)
            {
                // Convert the value to the appropriate type
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
        // Handle common Unity types
        if (targetType == typeof(float))
        {
            return float.Parse(value);
        }
        else if (targetType == typeof(int))
        {
            return int.Parse(value);
        }
        else if (targetType == typeof(bool))
        {
            return bool.Parse(value);
        }
        else if (targetType == typeof(string))
        {
            return value;
        }
        else if (targetType == typeof(Vector3))
        {
            // Format: (x,y,z)
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
            // Format: (x,y)
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
            // Format: (r,g,b,a) or (r,g,b)
            value = value.Trim('(', ')');
            string[] components = value.Split(',');
            if (components.Length >= 3)
            {
                if (components.Length == 4)
                {
                    return new Color(
                        float.Parse(components[0]),
                        float.Parse(components[1]),
                        float.Parse(components[2]),
                        float.Parse(components[3])
                    );
                }
                else
                {
                    return new Color(
                        float.Parse(components[0]),
                        float.Parse(components[1]),
                        float.Parse(components[2])
                    );
                }
            }
        }
        
        // For other types, try a general conversion
        return Convert.ChangeType(value, targetType);
    }

    private void ApplyEditToFile(string filePath, string newContent)
    {
        // Check if this is a full file replacement or a partial edit
        bool isPartialEdit = newContent.Contains("// … existing code …") || 
                             newContent.Contains("// existing code...") ||
                             newContent.Contains("// ...");
        
        if (isPartialEdit)
        {
            // For partial edits, we need to be more careful
            ApplyPartialEdit(filePath, newContent);
        }
        else
        {
            // For full file replacement, just write the new content
            File.WriteAllText(filePath, newContent);
            AssetDatabase.Refresh();
        }
    }

    private void ApplyPartialEdit(string filePath, string editContent)
    {
        // This is a simplified implementation - a real one would need more robust parsing
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }
        
        string originalContent = File.ReadAllText(filePath);
        
        // Remove comment markers that indicate unchanged code
        var cleanedEdit = Regex.Replace(editContent, 
            @"//\s*(?:…|\.\.\.)\s*existing code\s*(?:…|\.\.\.)", 
            "");
        
        // For this simple implementation, we'll just replace the entire file
        // A more robust implementation would identify specific functions or sections to edit
        File.WriteAllText(filePath, cleanedEdit);
        AssetDatabase.Refresh();
    }

    private async void SendQueryToOpenAIStreaming(string userMessage, string model, Action<string, string> onResponse)
    {
        const string url = "https://api.openai.com/v1/chat/completions";
        string apiKey = ApiKeyManager.GetKey(ApiKeyManager.OPENAI_KEY);
        if (string.IsNullOrEmpty(apiKey))
        {
            AddMessageToHistory("System", "<error: OpenAI API key not set. Click the API Settings button to configure it.>");
            return;
        }
        
        if (userMessage.ToLower().Contains("more example") ||
            userMessage.ToLower().Contains("more prompt") ||
            userMessage.ToLower().Contains("give example") ||
            userMessage.ToLower().Contains("show example"))
        {
            List<string> moreExamples = PromptRecommender.GetRandomPrompts(3);
            string examplesMessage = "Here are some more example prompts you can try:\n\n" +
                                    $"• {moreExamples[0]}\n" +
                                    $"• {moreExamples[1]}\n" +
                                    $"• {moreExamples[2]}";
            onResponse?.Invoke(examplesMessage, "OpenAI");
            return;
        }
        
        string escapedMessage = EscapeJson(userMessage);
        string systemPrompt = "You are a Unity development assistant that can help with code. When suggesting code changes, use the format ```csharp:Assets/Scripts/FileName.cs\\n// code here\\n``` so the changes can be automatically applied.";
        string sceneAnalyzerPrompt = SceneAnalysisIntegration.LoadMetaprompt("SceneAnalyzer_RequestAware");
        if (!string.IsNullOrEmpty(sceneAnalyzerPrompt))
        {
            systemPrompt += "\n\n" + sceneAnalyzerPrompt;
        }
        
        string contextMessage = "";
        if (!string.IsNullOrEmpty(lastLoadedScriptPath) && !string.IsNullOrEmpty(lastLoadedScriptContent))
        {
            contextMessage = $"I'm working with this file: {lastLoadedScriptPath}\\n```csharp\\n{EscapeJson(lastLoadedScriptContent)}\\n```\\n\\nMy question is: ";
        }
        if (isSceneLoaded && !string.IsNullOrEmpty(lastLoadedScenePath))
        {
            string sceneName = Path.GetFileName(lastLoadedScenePath);
            string sceneContext = SceneAnalysisIntegration.GetSceneStructureSummary();
            contextMessage += $"I'm working with the Unity scene: {sceneName}\n{sceneContext}\n\nMy question is: ";
        }
        
        string jsonPayload = @"{
            ""model"": """ + model + @""",
            ""stream"": true,
            ""messages"": [
                {
                    ""role"": ""system"",
                    ""content"": """ + systemPrompt + @"""
                },";
        
        if (!string.IsNullOrEmpty(contextMessage))
        {
            jsonPayload += @"
                {
                    ""role"": ""user"",
                    ""content"": """ + contextMessage + escapedMessage + @"""
                }";
        }
        else
        {
            jsonPayload += @"
                {
                    ""role"": ""user"",
                    ""content"": """ + escapedMessage + @"""
                }";
        }
        
        jsonPayload += @"
            ]
        }";
        
        jsonPayload = Regex.Replace(jsonPayload, @"\s+", " ").Replace(" \"", "\"").Replace("\" ", "\"");
        
        AddStreamingPlaceholderMessage();
        
        void OnChunkReceived(string chunk)
        {
            string processed = chunk.StartsWith("data:") ? chunk.Substring(5).Trim() : chunk;
            if (processed == "[DONE]")
                return;
        
            OpenAIStreamChunk chunkObj = null;
            try
            {
                chunkObj = JsonUtility.FromJson<OpenAIStreamChunk>(processed);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to parse streaming JSON chunk: " + e.Message);
                return;
            }
        
            if (chunkObj?.choices != null && chunkObj.choices.Length > 0)
            {
                // // Text Streaming CodeBlock Error FIX: Instead of splitting on spaces:
                string content = chunkObj.choices[0].delta?.content;
                if (!string.IsNullOrEmpty(content))
                {
                    // Just append the raw chunk:
                    UpdateStreamingMessage(content);
                }
            }
        }
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            var streamingHandler = new StreamingDownloadHandler(OnChunkReceived);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = streamingHandler;
        
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        
            var operation = request.SendWebRequest();
        
            Debug.Log("Sending streaming request to OpenAI with payload: " + jsonPayload);
        
            while (!operation.isDone)
                await Task.Yield();
        
            queryField.SetEnabled(true);
            sendButton.SetEnabled(true);
        
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("OpenAI Streaming API Error: " + request.error);
                Debug.LogError("Response body: " + request.downloadHandler.text);
                AddMessageToHistory("System", "<error: could not get response>");
                return;
            }

            if (streamingMessageLabel != null)
            {
                string finalResponse = streamingMessageLabel.text;
                if (streamingMessageLabel.parent != null)
                    streamingMessageLabel.parent.RemoveFromHierarchy();
                streamingMessageLabel = null;

                // Then pass the actual text so ProcessAndApplyCodeEdits can detect code blocks
                onResponse?.Invoke(finalResponse, "OpenAI");
            }
            else
            {
                // If for some reason streamingMessageLabel was null, just pass empty
                onResponse?.Invoke("", "OpenAI");
            }
        }
        // Extra closing brace added to fix missing }
    }

    private async void SendQueryToOpenAI(string userMessage, string model, Action<string, string> onResponse)
    {
        const string url = "https://api.openai.com/v1/chat/completions";
        
        // Get API key from manager
        string apiKey = ApiKeyManager.GetKey(ApiKeyManager.OPENAI_KEY);
        if (string.IsNullOrEmpty(apiKey))
        {
            onResponse?.Invoke("<error: OpenAI API key not set. Click the API Settings button to configure it.>", "OpenAI");
            return;
        }

        // Check if the user is asking for more examples
        if (userMessage.ToLower().Contains("more example") || 
            userMessage.ToLower().Contains("more prompt") || 
            userMessage.ToLower().Contains("give example") ||
            userMessage.ToLower().Contains("show example"))
        {
            // Get more example prompts
            List<string> moreExamples = PromptRecommender.GetRandomPrompts(3);
            string examplesMessage = "Here are some more example prompts you can try:\n\n" +
                                     $"• {moreExamples[0]}\n" +
                                     $"• {moreExamples[1]}\n" +
                                     $"• {moreExamples[2]}";
            
            onResponse?.Invoke(examplesMessage, "OpenAI");
            return;
        }
        
        // Properly escape the user message to avoid JSON formatting issues
        string escapedMessage = EscapeJson(userMessage);
        
        // Load scene analyzer metaprompt if available
        string systemPrompt = "You are a Unity development assistant that can help with code. When suggesting code changes, use the format ```csharp:Assets/Scripts/FileName.cs\\n// code here\\n``` so the changes can be automatically applied.";
        
        string sceneAnalyzerPrompt = SceneAnalysisIntegration.LoadMetaprompt("SceneAnalyzer_RequestAware");
        if (!string.IsNullOrEmpty(sceneAnalyzerPrompt))
        {
            systemPrompt += "\n\n" + sceneAnalyzerPrompt;
        }
        
        // Add script context if available
        string contextMessage = "";
        if (!string.IsNullOrEmpty(lastLoadedScriptPath) && !string.IsNullOrEmpty(lastLoadedScriptContent))
        {
            contextMessage = $"I'm working with this file: {lastLoadedScriptPath}\\n```csharp\\n{EscapeJson(lastLoadedScriptContent)}\\n```\\n\\nMy question is: ";
        }
        
        // Add scene context if available
        if (isSceneLoaded && !string.IsNullOrEmpty(lastLoadedScenePath))
        {
            string sceneName = Path.GetFileName(lastLoadedScenePath);
            string sceneContext = SceneAnalysisIntegration.GetSceneStructureSummary();
            
            contextMessage += $"I'm working with the Unity scene: {sceneName}\n{sceneContext}\n\nMy question is: ";
        }
        
        // Simplify the prompt to reduce potential formatting issues
        string jsonPayload = @"{
            ""model"": """ + model + @""",
            ""messages"": [
                {
                    ""role"": ""system"",
                    ""content"": """ + systemPrompt + @"""
                },";
        
        // Add context message if available
        if (!string.IsNullOrEmpty(contextMessage))
        {
            jsonPayload += @"
                {
                    ""role"": ""user"",
                    ""content"": """ + contextMessage + escapedMessage + @"""
                }";
        }
        else
        {
            jsonPayload += @"
                {
                    ""role"": ""user"",
                    ""content"": """ + escapedMessage + @"""
                }";
        }
        
        jsonPayload += @"
            ]
        }";
        
        // Remove whitespace from the JSON to ensure proper formatting
        jsonPayload = Regex.Replace(jsonPayload, @"\s+", " ");
        jsonPayload = jsonPayload.Replace(" \"", "\"").Replace("\" ", "\"");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            Debug.Log("Sending request to OpenAI with payload: " + jsonPayload);
            
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("OpenAI API Error: " + request.error);
                Debug.LogError("Response body: " + request.downloadHandler.text);
                onResponse?.Invoke("<error: could not get response>", "OpenAI");
                return;
            }

            string responseJson = request.downloadHandler.text;
            string assistantText = ParseOpenAIReply(responseJson);
            onResponse?.Invoke(assistantText, "OpenAI");
        }
    }

    private async void SendQueryToClaudeStreaming(string userMessage, string model, Action<string, string> onResponse)
    {
        const string url = "https://api.anthropic.com/v1/messages";
        string apiKey = ApiKeyManager.GetKey(ApiKeyManager.CLAUDE_KEY);
        if (string.IsNullOrEmpty(apiKey))
        {
            AddMessageToHistory("System", "<error: Claude API key not set. Click the API Settings button to configure it.>");
            return;
        }

        // Check if the user is asking for more examples
        if (userMessage.ToLower().Contains("more example") ||
            userMessage.ToLower().Contains("more prompt") ||
            userMessage.ToLower().Contains("give example") ||
            userMessage.ToLower().Contains("show example"))
        {
            List<string> moreExamples = PromptRecommender.GetRandomPrompts(3);
            string examplesMessage = "Here are some more example prompts you can try:\n\n" +
                                    $"• {moreExamples[0]}\n" +
                                    $"• {moreExamples[1]}\n" +
                                    $"• {moreExamples[2]}";
            onResponse?.Invoke(examplesMessage, "Claude");
            return;
        }

        // Escape the user message to avoid JSON formatting issues
        string escapedMessage = EscapeJson(userMessage);

        // Load scene analyzer metaprompt if available
        string systemPrompt = "You are a Unity development assistant that can help with code. When suggesting code changes, use the format ```csharp:Assets/Scripts/FileName.cs\\n// code here\\n``` so the changes can be automatically applied.";
        string sceneAnalyzerPrompt = SceneAnalysisIntegration.LoadMetaprompt("SceneAnalyzer_RequestAware");
        if (!string.IsNullOrEmpty(sceneAnalyzerPrompt))
        {
            systemPrompt += "\n\n" + sceneAnalyzerPrompt;
        }

        // Add script context if available
        string contextMessage = "";
        if (!string.IsNullOrEmpty(lastLoadedScriptPath) && !string.IsNullOrEmpty(lastLoadedScriptContent))
        {
            contextMessage = $"I'm working with this file: {lastLoadedScriptPath}\\n```csharp\\n{EscapeJson(lastLoadedScriptContent)}\\n```\\n\\nMy question is: ";
        }

        // Add scene context if available
        if (isSceneLoaded && !string.IsNullOrEmpty(lastLoadedScenePath))
        {
            string sceneName = Path.GetFileName(lastLoadedScenePath);
            string sceneContext = SceneAnalysisIntegration.GetSceneStructureSummary();
            contextMessage += $"I'm working with the Unity scene: {sceneName}\n{sceneContext}\n\nMy question is: ";
        }

        // Construct JSON payload with streaming enabled
        string jsonPayload = @"{
            ""model"": """ + model + @""",
            ""stream"": true,
            ""max_tokens"": 1024,
            ""messages"": [
                {
                    ""role"": ""system"",
                    ""content"": """ + systemPrompt + @"""
                },";
        if (!string.IsNullOrEmpty(contextMessage))
        {
            jsonPayload += @"
                {
                    ""role"": ""user"",
                    ""content"": """ + contextMessage + escapedMessage + @"""
                }";
        }
        else
        {
            jsonPayload += @"
                {
                    ""role"": ""user"",
                    ""content"": """ + escapedMessage + @"""
                }";
        }
        jsonPayload += @"
            ]
        }";
        jsonPayload = Regex.Replace(jsonPayload, @"\s+", " ").Replace(" \"", "\"").Replace("\" ", "\"");

        // Create a placeholder message for streaming and store the label reference.
        AddStreamingPlaceholderMessage();

        // Define a callback to update the UI as chunks arrive.
        void OnChunkReceived(string chunk)
        {
            string processed = chunk.StartsWith("data:") ? chunk.Substring(5).Trim() : chunk;
            if (processed == "[DONE]")
                return;
            OpenAIStreamChunk chunkObj = null;
            try
            {
                chunkObj = JsonUtility.FromJson<OpenAIStreamChunk>(processed);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to parse streaming JSON chunk from Claude: " + e.Message);
                return;
            }
            if (chunkObj?.choices != null && chunkObj.choices.Length > 0)
            {
                // Text Streaming CodeBlock Error FIX: Instead of splitting on spaces:
                string content = chunkObj.choices[0].delta?.content;
                if (!string.IsNullOrEmpty(content))
                {
                    // Just append the raw chunk:
                    UpdateStreamingMessage(content);
                }
            }
        }

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            var streamingHandler = new StreamingDownloadHandler(OnChunkReceived);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = streamingHandler;

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-api-key", apiKey);
            request.SetRequestHeader("anthropic-version", "2023-06-01");

            Debug.Log("Sending streaming request to Claude with payload: " + jsonPayload);
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            queryField.SetEnabled(true);
            sendButton.SetEnabled(true);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Claude Streaming API Error: " + request.error);
                Debug.LogError("Response body: " + request.downloadHandler.text);
                AddMessageToHistory("System", "<error: could not get response from Claude>");
                return;
            }
        }
    }

    private async void SendQueryToClaude(string userMessage, string model, Action<string, string> onResponse)
    {
        const string url = "https://api.anthropic.com/v1/messages";
        
        // Get API key from manager
        string apiKey = ApiKeyManager.GetKey(ApiKeyManager.CLAUDE_KEY);
        if (string.IsNullOrEmpty(apiKey))
        {
            onResponse?.Invoke("<error: Claude API key not set. Click the API Settings button to configure it.>", "Claude");
            return;
        }

        // Check if the user is asking for more examples
        if (userMessage.ToLower().Contains("more example") || 
            userMessage.ToLower().Contains("more prompt") || 
            userMessage.ToLower().Contains("give example") ||
            userMessage.ToLower().Contains("show example"))
        {
            // Get more example prompts
            List<string> moreExamples = PromptRecommender.GetRandomPrompts(3);
            string examplesMessage = "Here are some more example prompts you can try:\n\n" +
                                     $"• {moreExamples[0]}\n" +
                                     $"• {moreExamples[1]}\n" +
                                     $"• {moreExamples[2]}";
            
            onResponse?.Invoke(examplesMessage, "Claude");
            return;
        }
        
        // Properly escape the user message to avoid JSON formatting issues
        string escapedMessage = EscapeJson(userMessage);
        
        // Load scene analyzer metaprompt if available
        string systemPrompt = "You are a Unity development assistant that can help with code. When suggesting code changes, use the format ```csharp:Assets/Scripts/FileName.cs\\n// code here\\n``` so the changes can be automatically applied.";
        
        string sceneAnalyzerPrompt = SceneAnalysisIntegration.LoadMetaprompt("SceneAnalyzer_RequestAware");
        if (!string.IsNullOrEmpty(sceneAnalyzerPrompt))
        {
            systemPrompt += "\n\n" + sceneAnalyzerPrompt;
        }
        
        // Add script context if available
        string contextMessage = "";
        if (!string.IsNullOrEmpty(lastLoadedScriptPath) && !string.IsNullOrEmpty(lastLoadedScriptContent))
        {
            contextMessage = $"I'm working with this file: {lastLoadedScriptPath}\\n```csharp\\n{EscapeJson(lastLoadedScriptContent)}\\n```\\n\\nMy question is: ";
        }
        
        // Add scene context if available
        if (isSceneLoaded && !string.IsNullOrEmpty(lastLoadedScenePath))
        {
            string sceneName = Path.GetFileName(lastLoadedScenePath);
            string sceneContext = SceneAnalysisIntegration.GetSceneStructureSummary();
            
            contextMessage += $"I'm working with the Unity scene: {sceneName}\n{sceneContext}\n\nMy question is: ";
        }
        
        // Construct Claude API request
        string jsonPayload = @"{
            ""model"": """ + model + @""",
            ""max_tokens"": 1024,
            ""messages"": [
                {
                    ""role"": ""system"",
                    ""content"": """ + systemPrompt + @"""
                },";
        
        // Add context message if available
        if (!string.IsNullOrEmpty(contextMessage))
        {
            jsonPayload += @"
                {
                    ""role"": ""user"",
                    ""content"": """ + contextMessage + escapedMessage + @"""
                }";
        }
        else
        {
            jsonPayload += @"
                {
                    ""role"": ""user"",
                    ""content"": """ + escapedMessage + @"""
                }";
        }
        
        jsonPayload += @"
            ]
        }";
        
        // Remove whitespace from the JSON to ensure proper formatting
        jsonPayload = Regex.Replace(jsonPayload, @"\s+", " ");
        jsonPayload = jsonPayload.Replace(" \"", "\"").Replace("\" ", "\"");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Headers for Claude API
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-api-key", apiKey);
            request.SetRequestHeader("anthropic-version", "2023-06-01");

            Debug.Log("Sending request to Claude with payload: " + jsonPayload);
            
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Claude API Error: " + request.error);
                Debug.LogError("Response body: " + request.downloadHandler.text);
                onResponse?.Invoke("<error: could not get response>", "Claude");
                return;
            }

            string responseJson = request.downloadHandler.text;
            string assistantText = ParseClaudeReply(responseJson);
            onResponse?.Invoke(assistantText, "Claude");
        }
    }

    private void AddMessageToHistory(string sender, string message)
    {
        // Create a container for the message
        var messageContainer = new VisualElement
        {
            style =
            {
                marginBottom = 8,
                paddingLeft = 4,
                paddingRight = 4
            }
        };

        // Add sender name with bold styling
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

        // Process message for markdown if it's from the AI
        if (sender == "XeleR")
        {
            // Create a content container for the message
            var contentContainer = new VisualElement
            {
                style =
                {
                    marginLeft = 4,
                    marginRight = 4
                }
            };
            
            // Use the markdown renderer to format the message
            var formattedContent = MarkdownRenderer.RenderMarkdown(message);
            contentContainer.Add(formattedContent);
            messageContainer.Add(contentContainer);
            
            // Process code blocks separately (these will be added directly to the conversation)
            ProcessCodeBlocksInMessage(message);
        }
        else
        {
            // For non-AI messages, just use a simple label
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

        // Save to current session's history
        if (currentSessionIndex >= 0 && currentSessionIndex < chatSessions.Count)
        {
            chatSessions[currentSessionIndex].Messages.Add(new ChatMessage 
            { 
                Sender = sender, 
                Content = message,
                IsFileContent = false
            });
            
            // Save to EditorPrefs after adding a message
            SaveChatSessionsToEditorPrefs();
        }

        // Scroll to bottom using the helper method
        EditorApplication.delayCall += ScrollToBottom;
    }
    private void ProcessCodeBlocksInMessage(string message)
    {
        var codeBlockPattern = new Regex(@"```(?:csharp|cs):([^\n]+)\n([\s\S]*?)```");
        var matches = codeBlockPattern.Matches(message);
        
        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                string filePath = match.Groups[1].Value.Trim();
                string codeContent = match.Groups[2].Value;
                VisualElement codeBlockElement = MarkdownRenderer.RenderCodeBlock(filePath, codeContent);
                conversationScrollView.Add(codeBlockElement);
            }
        }
    }


    // Add a formatted code block to the conversation
    private void AddCodeBlockToHistory(string filePath, string content)
    {
        var fileHeader = new Label
        {
            style =
            {
                whiteSpace = WhiteSpace.Normal,
                marginBottom = 4,
                marginTop = 8,
                unityFontStyleAndWeight = FontStyle.Bold,
                color = new Color(0.4f, 0.7f, 1.0f) // Light blue for code headers
            },
            text = $"Code: {Path.GetFileName(filePath)} ({filePath})"
        };
        
        var codeBlock = new TextField
        {
            multiline = true,
            style =
            {
                whiteSpace = WhiteSpace.Normal,
                marginBottom = 8,
                backgroundColor = new Color(0.15f, 0.15f, 0.15f), // Darker background for code
                color = new Color(0.9f, 0.9f, 0.9f), // Lighter text for code
                paddingLeft = 8,
                paddingRight = 8,
                paddingTop = 4,
                paddingBottom = 4,
                borderTopWidth = 1,
                borderBottomWidth = 1,
                borderLeftWidth = 1,
                borderRightWidth = 1,
                borderTopColor = new Color(0.3f, 0.3f, 0.3f),
                borderBottomColor = new Color(0.3f, 0.3f, 0.3f),
                borderLeftColor = new Color(0.3f, 0.3f, 0.3f),
                borderRightColor = new Color(0.3f, 0.3f, 0.3f)
            }
        };
        
        codeBlock.SetValueWithoutNotify(content);
        codeBlock.isReadOnly = true;
        
        conversationScrollView.Add(fileHeader);
        conversationScrollView.Add(codeBlock);
        
        // Scroll to bottom
        EditorApplication.delayCall += ScrollToBottom;
    }

    private void OnFocusInQueryField(FocusInEvent evt)
    {
        if (queryField.value == PLACEHOLDER_TEXT)
        {
            queryField.SetValueWithoutNotify(string.Empty);
            queryField.RemoveFromClassList("placeholder-text");
        }
    }

    private void OnFocusOutQueryField(FocusOutEvent evt)
    {
        if (string.IsNullOrEmpty(queryField.value))
        {
            queryField.SetValueWithoutNotify(PLACEHOLDER_TEXT);
            queryField.AddToClassList("placeholder-text");
        }
    }

    private string ParseOpenAIReply(string json)
    {
        try
        {
            // More robust parsing
            int contentStartIndex = json.IndexOf("\"content\":");
            if (contentStartIndex == -1)
            {
                Debug.LogError("Could not find content in response: " + json);
                return "<No content found in response>";
            }

            // Find the opening quote after "content":
            int openQuoteIndex = json.IndexOf('"', contentStartIndex + 10);
            if (openQuoteIndex == -1) return "<Invalid JSON format>";

            // Find the closing quote (accounting for escaped quotes)
            int closeQuoteIndex = openQuoteIndex + 1;
            bool foundClosingQuote = false;
            
            while (closeQuoteIndex < json.Length)
            {
                if (json[closeQuoteIndex] == '"' && json[closeQuoteIndex - 1] != '\\')
                {
                    foundClosingQuote = true;
                    break;
                }
                closeQuoteIndex++;
            }
            
            if (!foundClosingQuote) return "<Invalid JSON format>";

            string extracted = json.Substring(openQuoteIndex + 1, closeQuoteIndex - openQuoteIndex - 1);
            extracted = extracted
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
                
            return extracted;
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing OpenAI response: " + ex.Message);
            Debug.LogError("JSON: " + json);
            return "<Error parsing response>";
        }
    }

    private string ParseClaudeReply(string json)
    {
        try
        {
            // Claude's response format is different from OpenAI
            int contentStartIndex = json.IndexOf("\"content\":");
            if (contentStartIndex == -1)
            {
                Debug.LogError("Could not find content in Claude response: " + json);
                return "<No content found in response>";
            }

            // The content is within the messages array in Claude's response
            int openBracketIndex = json.IndexOf('[', contentStartIndex);
            if (openBracketIndex == -1) return "<Invalid JSON format>";

            // Find the opening quote for content
            int openQuoteIndex = json.IndexOf('"', openBracketIndex + 10);
            if (openQuoteIndex == -1) return "<Invalid JSON format>";

            // Find the closing quote (accounting for escaped quotes)
            int closeQuoteIndex = openQuoteIndex + 1;
            bool foundClosingQuote = false;
            
            while (closeQuoteIndex < json.Length)
            {
                if (json[closeQuoteIndex] == '"' && json[closeQuoteIndex - 1] != '\\')
                {
                    foundClosingQuote = true;
                    break;
                }
                closeQuoteIndex++;
            }
            
            if (!foundClosingQuote) return "<Invalid JSON format>";

            string extracted = json.Substring(openQuoteIndex + 1, closeQuoteIndex - openQuoteIndex - 1);
            extracted = extracted
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
                
            return extracted;
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing Claude response: " + ex.Message);
            Debug.LogError("JSON: " + json);
            return "<Error parsing response>";
        }
    }

    private string EscapeJson(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t")
            .Replace("\b", "\\b")
            .Replace("\f", "\\f");
    }

    // Add a method to clear the script context
    private void ClearScriptContext()
    {
        lastLoadedScriptPath = null;
        lastLoadedScriptContent = null;
        AddMessageToHistory("System", "Script context cleared.");
    }

    // Add a method to clear conversation history
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
                    // Restore file content display
                    AddFileContentToHistoryWithoutSaving(message.FileName, message.Content);
                }
                else
                {
                    // Restore regular message
                    AddMessageToHistoryWithoutSaving(message.Sender, message.Content);
                }
            }

            // If this is a new session with no messages, add the welcome message
            if (currentSession.Messages.Count == 0)
            {
                AddWelcomeMessage();
            }
           
            
            // Scroll to bottom
            EditorApplication.delayCall += ScrollToBottom;
        }
    }

    // Handle Enter key in the query field
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

    // Helper method to scroll to the bottom of the conversation
    private void ScrollToBottom()
    {
        if (conversationScrollView != null)
        {
            float fullHeight = conversationScrollView.contentContainer.layout.height;
            conversationScrollView.scrollOffset = new Vector2(0, fullHeight);
        }
    }

    // Override OnEnable to ensure we restore state when the window is enabled
    private void OnEnable()
    {
        // Load chat sessions from EditorPrefs
        LoadChatSessionsFromEditorPrefs();
        
        // If we already have a UI built, restore the conversation
        if (rootVisualElement != null && conversationScrollView != null)
        {
            // Restore the current session
            RestoreCurrentSession();
            
            // Always scroll to bottom when window is enabled (after compilation)
            EditorApplication.delayCall += ScrollToBottom;
        }
        
        // Register for compilation events
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.compilationFinished += OnCompilationFinished;
    }
    
    // Override OnDisable to save any state before the window is disabled
    private void OnDisable()
    {
        // Save the current session state and all sessions to EditorPrefs
        SaveChatSessionsToEditorPrefs();
        
        // Unregister from compilation events
        CompilationPipeline.compilationStarted -= OnCompilationStarted;
        CompilationPipeline.compilationFinished -= OnCompilationFinished;
        
        // Clean up scene analysis components
        SceneAnalysisIntegration.Cleanup();
    }

    private void OnCompilationStarted(object obj)
    {
        // Save state before compilation starts
        SaveChatSessionsToEditorPrefs();
    }

    private void OnCompilationFinished(object obj)
    {
        // Load state after compilation finishes
        LoadChatSessionsFromEditorPrefs();
        
        // Restore UI if needed
        if (rootVisualElement != null && conversationScrollView != null)
        {
            RestoreCurrentSession();
        }
        
        // Scroll to bottom after compilation finishes
        EditorApplication.delayCall += ScrollToBottom;
    }

    // Combined scene analysis method
    private void OnSceneAnalysisClicked()
    {
        // Create a dropdown menu with scene analysis options
        var menu = new GenericMenu();
        
        menu.AddItem(new GUIContent("Scene Structure"), false, () => {
            string sceneStructure = SceneAnalysisIntegration.GetSceneStructureSummary();
            AddMessageToHistory("You", "Analyze the current scene structure");
            AddMessageToHistory("System", sceneStructure);
        });
        
        menu.AddItem(new GUIContent("Spatial Analysis"), false, () => {
            string spatialInfo = SceneAnalysisIntegration.GetSpatialInformation();
            AddMessageToHistory("You", "Perform spatial analysis on the scene");
            AddMessageToHistory("System", spatialInfo);
        });
        
        menu.AddItem(new GUIContent("Complete Analysis"), false, () => {
            string sceneStructure = SceneAnalysisIntegration.GetSceneStructureSummary();
            string spatialInfo = SceneAnalysisIntegration.GetSpatialInformation();
            AddMessageToHistory("You", "Perform complete scene analysis");
            AddMessageToHistory("System", "Scene Structure:\n" + sceneStructure + "\n\nSpatial Analysis:\n" + spatialInfo);
        });
        
        menu.ShowAsContext();
    }

    // Add a method to handle scene context in queries
    public string AddSceneContextToQuery(string query)
    {
        if (!includeSceneContext)
            return query;
        
        string sceneContext = SceneAnalysisIntegration.GetSceneStructureSummary();
        return $"[Scene Context]\n{sceneContext}\n\n[User Query]\n{query}";
    }

    private void OnBrowseScenesClicked()
    {
        // Create a dropdown menu with scene files
        var menu = new GenericMenu();
        
        // Ensure the Scenes folder exists
        if (!Directory.Exists(SCENES_FOLDER))
        {
            Directory.CreateDirectory(SCENES_FOLDER);
            AssetDatabase.Refresh();
        }
        
        // Get all Unity scene files in the Scenes folder
        string[] sceneFiles = Directory.GetFiles(SCENES_FOLDER, "*.unity", SearchOption.AllDirectories);
        
        foreach (string filePath in sceneFiles)
        {
            string relativePath = filePath.Replace("\\", "/"); // Normalize path for Unity
            menu.AddItem(new GUIContent(relativePath), false, () => LoadSceneFile(relativePath));
        }
        
        menu.ShowAsContext();
    }

    private void LoadSceneFile(string scenePath)
    {
        try
        {
            // Check if there are unsaved changes in the current scene
            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty)
            {
                if (!EditorUtility.DisplayDialog("Unsaved Changes", 
                    "The current scene has unsaved changes. Do you want to proceed and lose those changes?", 
                    "Yes", "No"))
                {
                    return;
                }
            }
            
            // Add the scene path to the selected files array if not already in the array
            if (Array.IndexOf(selectedFiles, scenePath) < 0) {
                Array.Resize(ref selectedFiles, selectedFiles.Length + 1);
                selectedFiles[selectedFiles.Length - 1] = scenePath;
            }

            // Update the display of selected files
            UpdateSelectedFilesDisplay();
            
            // Load the scene
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            string sceneName = Path.GetFileName(scenePath);
            
            // Add the scene info to the conversation
            AddMessageToHistory("You", $"Open scene {sceneName}");
            AddMessageToHistory("System", $"Scene {sceneName} loaded successfully.");
            
            // Store the scene path for context in the next API call
            lastLoadedScenePath = scenePath;
            isSceneLoaded = true;
            
            // Optionally, ask the AI about the scene
            string prompt = $"I've opened the scene {sceneName}. Can you analyze this scene and suggest improvements?";
            queryField.SetValueWithoutNotify(prompt);
            queryField.RemoveFromClassList("placeholder-text");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading scene file: {ex.Message}");
            AddMessageToHistory("System", $"Error loading scene: {ex.Message}");
        }
    }

    // Add a method to create a new GameObject in the scene
    private GameObject CreateGameObject(string name, Vector3 position)
    {
        if (!isSceneLoaded)
        {
            AddMessageToHistory("System", "No scene is currently loaded.");
            return null;
        }
        
        GameObject newObject = new GameObject(name);
        newObject.transform.position = position;
        
        AddMessageToHistory("System", $"Created new GameObject '{name}' at position {position}");
        
        // Mark the scene as dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        return newObject;
    }

    // Add a method to save the current scene
    private void SaveCurrentScene()
    {
        if (!isSceneLoaded)
        {
            AddMessageToHistory("System", "No scene is currently loaded.");
            return;
        }
        
        var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        
        if (string.IsNullOrEmpty(currentScene.path))
        {
            // This is a new scene that hasn't been saved yet
            string newPath = EditorUtility.SaveFilePanel(
                "Save Scene",
                SCENES_FOLDER,
                "NewScene.unity",
                "unity");
                
            if (string.IsNullOrEmpty(newPath))
            {
                // User cancelled the save dialog
                return;
            }
            
            // Convert to a project-relative path
            if (newPath.StartsWith(Application.dataPath))
            {
                newPath = "Assets" + newPath.Substring(Application.dataPath.Length);
            }
            
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(currentScene, newPath);
            lastLoadedScenePath = newPath;
        }
        else
        {
            // Save the existing scene
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(currentScene);
        }
        
        AddMessageToHistory("System", $"Scene saved to {currentScene.path}");
    }

    // Add a method to create a new scene
    private void CreateNewScene()
    {
        // Check if there are unsaved changes in the current scene
        if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty)
        {
            if (!EditorUtility.DisplayDialog("Unsaved Changes", 
                "The current scene has unsaved changes. Do you want to proceed and lose those changes?", 
                "Yes", "No"))
            {
                return;
            }
        }
        
        // Create a new empty scene
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
            UnityEditor.SceneManagement.NewSceneMode.Single);
        
        isSceneLoaded = true;
        lastLoadedScenePath = "";
        
        AddMessageToHistory("System", "Created a new scene with default game objects.");
    }

    private void OnSessionChanged(ChangeEvent<string> evt)
    {
        // Find the index of the selected session
        int newIndex = chatSessions.FindIndex(s => s.Name == evt.newValue);
        if (newIndex >= 0 && newIndex < chatSessions.Count)
        {
            // Save current session state
            SaveCurrentSessionState();
            
            // Switch to the new session
            currentSessionIndex = newIndex;
            
            // Restore the selected session
            RestoreCurrentSession();
            
            // Save the updated state
            SaveChatSessionsToEditorPrefs();
        }
    }

    private void OnNewChatClicked()
    {
        // Save current session state
        SaveCurrentSessionState();
        
        // Create a new chat session
        int newChatNumber = chatSessions.Count + 1;
        var newSession = new ChatSession($"Chat {newChatNumber}");
        chatSessions.Add(newSession);
        
        // Update the session selector
        var sessionNames = chatSessions.Select(s => s.Name).ToList();
        sessionSelector.choices = sessionNames;
        
        // Switch to the new session
        currentSessionIndex = chatSessions.Count - 1;
        sessionSelector.index = currentSessionIndex;
        
        // Clear the conversation view and restore (which will be empty for a new chat)
        RestoreCurrentSession();
        
        // Add a welcome message
        AddMessageToHistory("System", $"Started new chat session: {newSession.Name}");
        
        // Save the updated state
        SaveChatSessionsToEditorPrefs();
    }

    // Add a method to display the welcome message with example prompts
    private void AddWelcomeMessage()
    {
        string welcomeMessage = PromptRecommender.GetWelcomeMessage();
        AddMessageToHistory("XeleR", welcomeMessage);
    }

    private void SaveCurrentSessionState()
    {
        if (currentSessionIndex >= 0 && currentSessionIndex < chatSessions.Count)
        {
            var currentSession = chatSessions[currentSessionIndex];
            
            // Update the session with current state
            currentSession.LastLoadedScriptPath = lastLoadedScriptPath;
            currentSession.LastLoadedScriptContent = lastLoadedScriptContent;
            currentSession.LastLoadedScenePath = lastLoadedScenePath;
            currentSession.IsSceneLoaded = isSceneLoaded;
            
            // Messages are already saved as they're added
        }
    }

    // Add a method to rename the current chat session
    private void RenameCurrentSession(string newName)
    {
        if (currentSessionIndex >= 0 && currentSessionIndex < chatSessions.Count)
        {
            chatSessions[currentSessionIndex].Name = newName;
            
            // Update the session selector
            var sessionNames = chatSessions.Select(s => s.Name).ToList();
            sessionSelector.choices = sessionNames;
            sessionSelector.index = currentSessionIndex;
            
            AddMessageToHistory("System", $"Renamed session to: {newName}");
            
            // Save the updated state
            SaveChatSessionsToEditorPrefs();
        }
    }

    // Add a method to delete the current chat session
    private void DeleteCurrentSession()
    {
        if (chatSessions.Count <= 1)
        {
            // Don't delete the last session, just clear it
            ClearConversationHistory();
            return;
        }
        
        if (currentSessionIndex >= 0 && currentSessionIndex < chatSessions.Count)
        {
            // Remove the current session
            chatSessions.RemoveAt(currentSessionIndex);
            
            // Adjust the current index if needed
            if (currentSessionIndex >= chatSessions.Count)
            {
                currentSessionIndex = chatSessions.Count - 1;
            }
            
            // Update the session selector
            var sessionNames = chatSessions.Select(s => s.Name).ToList();
            sessionSelector.choices = sessionNames;
            sessionSelector.index = currentSessionIndex;
            
            // Restore the new current session
            RestoreCurrentSession();
            
            AddMessageToHistory("System", "Chat session deleted");
            
            // Save the updated state
            SaveChatSessionsToEditorPrefs();
        }
    }

    // Helper method to restore messages without adding them to history again
    private void AddMessageToHistoryWithoutSaving(string sender, string message)
    {
        // Create a container for the message
        var messageContainer = new VisualElement
        {
            style =
            {
                marginBottom = 8,
                paddingLeft = 4,
                paddingRight = 4
            }
        };

        // Add sender name with bold styling
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

        // Process message for markdown if it's from the AI
        if (sender == "XeleR")
        {
            // Create a content container for the message
            var contentContainer = new VisualElement
            {
                style =
                {
                    marginLeft = 4,
                    marginRight = 4
                }
            };
            
            // Use the markdown renderer to format the message
            var formattedContent = MarkdownRenderer.RenderMarkdown(message);
            contentContainer.Add(formattedContent);
            messageContainer.Add(contentContainer);
            
            // Process code blocks separately
            ProcessCodeBlocksInMessage(message);
        }
        else
        {
            // For non-AI messages, just use a simple label
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

    // Add methods to save and load chat sessions from EditorPrefs
    private void SaveChatSessionsToEditorPrefs()
    {
        try
        {
            // Save current session state first
            SaveCurrentSessionState();
            
            // Serialize the chat sessions to JSON
            string sessionsJson = JsonUtility.ToJson(new ChatSessionsWrapper { Sessions = chatSessions });
            
            // Store in EditorPrefs
            EditorPrefs.SetString(CHAT_SESSIONS_KEY, sessionsJson);
            EditorPrefs.SetInt(CURRENT_SESSION_INDEX_KEY, currentSessionIndex);
            
            // Log success
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
            // Check if we have saved sessions
            if (EditorPrefs.HasKey(CHAT_SESSIONS_KEY))
            {
                string sessionsJson = EditorPrefs.GetString(CHAT_SESSIONS_KEY);
                
                // Deserialize the chat sessions
                var wrapper = JsonUtility.FromJson<ChatSessionsWrapper>(sessionsJson);
                if (wrapper != null && wrapper.Sessions != null && wrapper.Sessions.Count > 0)
                {
                    chatSessions = wrapper.Sessions;
                    
                    // Load current session index
                    if (EditorPrefs.HasKey(CURRENT_SESSION_INDEX_KEY))
                    {
                        currentSessionIndex = EditorPrefs.GetInt(CURRENT_SESSION_INDEX_KEY);
                        
                        // Ensure index is valid
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
            
            // If loading fails, start with a fresh session
            chatSessions = new List<ChatSession> { new ChatSession("Chat 1") };
            currentSessionIndex = 0;
        }
    }
    
    // Wrapper class for serialization
    [Serializable]
    private class ChatSessionsWrapper
    {
        public List<ChatSession> Sessions;
    }

    // Add this method to handle the button click
    private void OnContextMenuButtonClicked()
    {
        if (isContextMenuOpen)
        {
            CloseContextMenu();
        }
        else
        {
            OpenContextMenu();
        }
    }

    private void OpenContextMenu()
    {
        // Position the dropdown above the @ button
        var buttonRect = contextMenuButton.worldBound;
        
        // Calculate the height of the dropdown based on the number of items
        // Each item is 24px high plus margins (4px total), and we have padding (10px total)
        float dropdownHeight = (2 * (24 + 4)) + 10; // Only 2 items now: Browse Scripts and Browse Scenes
        
        // Add additional offset to position it higher
        float additionalOffset = 24;
        
        contextMenuDropdown.style.left = buttonRect.x;
        contextMenuDropdown.style.top = buttonRect.y - dropdownHeight - additionalOffset;
        contextMenuDropdown.style.width = 200;
        
        // Clear existing items
        contextMenuDropdown.Clear();
        
        // Add only the file browsing options
        AddContextMenuItem("Browse Scripts", OnBrowseScriptsClicked);
        AddContextMenuItem("Browse Scenes", OnBrowseScenesClicked);
        
        // Show the dropdown
        contextMenuDropdown.style.display = DisplayStyle.Flex;
        isContextMenuOpen = true;
        
        // Add a click event handler to the root to close the menu when clicking outside
        rootVisualElement.RegisterCallback<MouseDownEvent>(OnClickOutsideContextMenu);
    }

    private void CloseContextMenu()
    {
        contextMenuDropdown.style.display = DisplayStyle.None;
        isContextMenuOpen = false;
        rootVisualElement.UnregisterCallback<MouseDownEvent>(OnClickOutsideContextMenu);
    }

    private void OnClickOutsideContextMenu(MouseDownEvent evt)
    {
        // Check if the click is outside the dropdown
        if (!contextMenuDropdown.worldBound.Contains(evt.mousePosition))
        {
            CloseContextMenu();
        }
    }

    // Modify the AddContextMenuItem method to update the display after tracking clicks
    private void AddContextMenuItem(string label, Action clickAction)
    {
        var item = new Button(() => {
            // Just execute the original action without tracking menu clicks
            clickAction();
        }) { text = label };
        
        item.style.height = 24;
        item.style.width = 190;
        item.style.marginLeft = 5;
        item.style.marginRight = 5;
        item.style.marginTop = 2;
        item.style.marginBottom = 2;
        item.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
        
        contextMenuDropdown.Add(item);
    }

    // Modify the UpdateSelectedFilesDisplay method to add close buttons to each file box
    private void UpdateSelectedFilesDisplay()
    {
        // First, clear any existing option boxes
        var existingBoxes = sessionContainer.Query(className: "selected-file-box").ToList();
        foreach (var box in existingBoxes)
        {
            sessionContainer.Remove(box);
        }
        
        // Display all selected files
        for (int i = 0; i < selectedFiles.Length; i++)
        {
            string filePath = selectedFiles[i];
            string fileName = Path.GetFileName(filePath);
            bool isScene = filePath.EndsWith(".unity");
            
            var fileBox = new VisualElement();
            fileBox.AddToClassList("selected-file-box");
            fileBox.style.flexDirection = FlexDirection.Row; // Make it horizontal to fit the close button
            
            // Use different colors for scripts vs scenes
            if (isScene)
                fileBox.style.backgroundColor = new Color(0.5f, 0.3f, 0.7f); // Purple for scenes
            else
                fileBox.style.backgroundColor = new Color(0.3f, 0.5f, 0.7f); // Blue for scripts
            
            fileBox.style.borderTopLeftRadius = 3;
            fileBox.style.borderTopRightRadius = 3;
            fileBox.style.borderBottomLeftRadius = 3;
            fileBox.style.borderBottomRightRadius = 3;
            fileBox.style.paddingLeft = 4;
            fileBox.style.paddingRight = 2; // Reduced right padding to fit close button
            fileBox.style.paddingTop = 2;
            fileBox.style.paddingBottom = 2;
            fileBox.style.marginLeft = 4;
            fileBox.style.height = 18;
            
            // Create a container for the file name label
            var labelContainer = new VisualElement();
            labelContainer.style.flexGrow = 1; // Take up available space
            
            // Make the file box clickable to reload the file
            int index = i; // Capture the index for the click handler
            labelContainer.RegisterCallback<ClickEvent>((evt) => {
                if (isScene)
                    LoadSceneFile(selectedFiles[index]);
                else
                    LoadScriptFile(selectedFiles[index]);
            });
            
            var fileLabel = new Label(fileName);
            fileLabel.style.fontSize = 10;
            fileLabel.style.color = Color.white;
            fileLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            
            labelContainer.Add(fileLabel);
            fileBox.Add(labelContainer);
            
            // Add a close button
            var closeButton = new Button(() => RemoveSelectedFile(index)) { text = "×" };
            closeButton.AddToClassList("file-close-button");
            closeButton.style.width = 14;
            closeButton.style.height = 14;
            closeButton.style.fontSize = 10;
            closeButton.style.paddingLeft = 0;
            closeButton.style.paddingRight = 0;
            closeButton.style.paddingTop = 0;
            closeButton.style.paddingBottom = 0;
            closeButton.style.marginLeft = 2;
            closeButton.style.marginRight = 0;
            closeButton.style.marginTop = 0;
            closeButton.style.marginBottom = 0;
            closeButton.style.backgroundColor = new Color(0.7f, 0.3f, 0.3f); // Red for close button
            closeButton.style.color = Color.white;
            closeButton.style.borderTopLeftRadius = 2;
            closeButton.style.borderTopRightRadius = 2;
            closeButton.style.borderBottomLeftRadius = 2;
            closeButton.style.borderBottomRightRadius = 2;
            
            fileBox.Add(closeButton);
            sessionContainer.Add(fileBox);
        }
    }

    // Add a method to remove a file from the selectedFiles array
    private void RemoveSelectedFile(int index)
    {
        if (index < 0 || index >= selectedFiles.Length)
            return;
        
        // Create a new array without the selected file
        string[] newSelectedFiles = new string[selectedFiles.Length - 1];
        
        // Copy all elements except the one at the specified index
        for (int i = 0, j = 0; i < selectedFiles.Length; i++)
        {
            if (i != index)
            {
                newSelectedFiles[j++] = selectedFiles[i];
            }
        }
        
        // Update the array
        selectedFiles = newSelectedFiles;
        
        // Update the UI
        UpdateSelectedFilesDisplay();
    }

    // Helper method to standardize button styling
    private void StyleButton(Button button)
    {
        button.style.height = 22;
        button.style.marginRight = 4;
        button.style.paddingLeft = 8;
        button.style.paddingRight = 8;
        button.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
        button.style.color = Color.white;
        button.style.borderTopLeftRadius = 4;
        button.style.borderTopRightRadius = 4;
        button.style.borderBottomLeftRadius = 4;
        button.style.borderBottomRightRadius = 4;
        button.style.borderTopWidth = 1;
        button.style.borderBottomWidth = 1;
        button.style.borderLeftWidth = 1;
        button.style.borderRightWidth = 1;
        button.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
        button.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
        button.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
        button.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
    }
}
