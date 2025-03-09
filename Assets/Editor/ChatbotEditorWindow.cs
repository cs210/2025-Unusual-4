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
using UnityEditor.Compilation;

public class ChatbotEditorWindow : EditorWindow
{
    // Add serialization for conversation history
    [SerializeField] private List<ChatMessage> conversationHistory = new List<ChatMessage>();
    
    // Serializable class to store chat messages
    [Serializable]
    private class ChatMessage
    {
        public string Sender;
        public string Content;
        public bool IsFileContent;
        public string FileName;
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

    private ScrollView conversationScrollView;
    private TextField queryField;
    private Button sendButton;
    private Button browseScriptsButton;
    private PopupField<ModelInfo> modelSelector;

    // Add these fields to store the last loaded script information
    [SerializeField] private string lastLoadedScriptPath;
    [SerializeField] private string lastLoadedScriptContent;

    [MenuItem("Window/Chatbox")]
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

        // Browse scripts button
        browseScriptsButton = new Button(OnBrowseScriptsClicked) { text = "Browse Scripts" };
        browseScriptsButton.style.height = 22; // Fixed height
        toolbar.Add(browseScriptsButton);

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

        // Restore conversation history from serialized data
        RestoreConversationHistory();
        
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
        
        // Save to serialized history
        conversationHistory.Add(new ChatMessage 
        { 
            Sender = "File", 
            Content = content,
            IsFileContent = true,
            FileName = fileName
        });
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
        
        // Send to the appropriate API based on the selected model's provider
        if (provider == "OpenAI")
        {
            SendQueryToOpenAI(userText, selectedModel.Name, OnResponseReceived);
        }
        else if (provider == "Claude")
        {
            SendQueryToClaude(userText, selectedModel.Name, OnResponseReceived);
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

    private async void SendQueryToOpenAI(string userMessage, string model, Action<string, string> onResponse)
    {
        const string url = "https://api.openai.com/v1/chat/completions";
        
        // Get API key from manager instead of using the field directly
        string apiKey = ApiKeyManager.GetKey(ApiKeyManager.OPENAI_KEY);
        if (string.IsNullOrEmpty(apiKey))
        {
            onResponse?.Invoke("<error: OpenAI API key not set. Click the API Settings button to configure it.>", "OpenAI");
            return;
        }
        
        // Properly escape the user message to avoid JSON formatting issues
        string escapedMessage = EscapeJson(userMessage);
        
        // Add script context if available
        string systemPrompt = "You are a Unity development assistant that can help with code. When suggesting code changes, use the format ```csharp:Assets/Scripts/FileName.cs\\n// code here\\n``` so the changes can be automatically applied.";
        
        string contextMessage = "";
        if (!string.IsNullOrEmpty(lastLoadedScriptPath) && !string.IsNullOrEmpty(lastLoadedScriptContent))
        {
            contextMessage = $"I'm working with this file: {lastLoadedScriptPath}\\n```csharp\\n{EscapeJson(lastLoadedScriptContent)}\\n```\\n\\nMy question is: ";
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

    private async void SendQueryToClaude(string userMessage, string model, Action<string, string> onResponse)
    {
        const string url = "https://api.anthropic.com/v1/messages";
        
        // Get API key from manager instead of using the field directly
        string apiKey = ApiKeyManager.GetKey(ApiKeyManager.CLAUDE_KEY);
        if (string.IsNullOrEmpty(apiKey))
        {
            onResponse?.Invoke("<error: Claude API key not set. Click the API Settings button to configure it.>", "Claude");
            return;
        }
        
        // Properly escape the user message to avoid JSON formatting issues
        string escapedMessage = EscapeJson(userMessage);
        
        // Add script context if available
        string systemPrompt = "You are a Unity development assistant that can help with code. When suggesting code changes, use the format ```csharp:Assets/Scripts/FileName.cs\\n// code here\\n``` so the changes can be automatically applied.";
        
        string contextMessage = "";
        if (!string.IsNullOrEmpty(lastLoadedScriptPath) && !string.IsNullOrEmpty(lastLoadedScriptContent))
        {
            contextMessage = $"I'm working with this file: {lastLoadedScriptPath}\\n```csharp\\n{EscapeJson(lastLoadedScriptContent)}\\n```\\n\\nMy question is: ";
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
            var formattedContent = CreateMarkdownFormattedContent(message);
            messageContainer.Add(formattedContent);
            
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

        // Save to serialized history
        conversationHistory.Add(new ChatMessage 
        { 
            Sender = sender, 
            Content = message,
            IsFileContent = false
        });

        // Scroll to bottom using the helper method
        EditorApplication.delayCall += ScrollToBottom;
    }

    // Create formatted content with markdown-like styling
    private VisualElement CreateMarkdownFormattedContent(string message)
    {
        var container = new VisualElement();
        
        // Remove code blocks first to process them separately
        string textWithoutCodeBlocks = RemoveCodeBlocks(message);
        
        // Split by newlines to process paragraph by paragraph
        string[] paragraphs = textWithoutCodeBlocks.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.None);
        
        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph)) continue;
            
            string trimmedParagraph = paragraph.Trim();
            
            // Check for headers
            if (trimmedParagraph.StartsWith("# "))
            {
                AddHeader(container, trimmedParagraph.Substring(2), 18, FontStyle.Bold);
            }
            else if (trimmedParagraph.StartsWith("## "))
            {
                AddHeader(container, trimmedParagraph.Substring(3), 16, FontStyle.Bold);
            }
            else if (trimmedParagraph.StartsWith("### "))
            {
                AddHeader(container, trimmedParagraph.Substring(4), 14, FontStyle.Bold);
            }
            // Check for bullet lists
            else if (trimmedParagraph.StartsWith("- ") || trimmedParagraph.StartsWith("* "))
            {
                AddBulletList(container, trimmedParagraph);
            }
            // Check for numbered lists
            else if (Regex.IsMatch(trimmedParagraph, @"^\d+\.\s"))
            {
                AddNumberedList(container, trimmedParagraph);
            }
            // Regular paragraph
            else
            {
                AddFormattedParagraph(container, trimmedParagraph);
            }
        }
        
        return container;
    }

    private string RemoveCodeBlocks(string message)
    {
        // Remove code blocks with file paths
        string withoutCodeBlocks = Regex.Replace(message, @"```(?:csharp|cs):([^\n]+)\n([\s\S]*?)```", "");
        
        // Remove regular code blocks
        withoutCodeBlocks = Regex.Replace(withoutCodeBlocks, @"```(?:csharp|cs)?\n([\s\S]*?)```", "");
        
        return withoutCodeBlocks;
    }

    private void AddHeader(VisualElement container, string text, int fontSize, FontStyle style)
    {
        var header = new Label
        {
            style =
            {
                fontSize = fontSize,
                unityFontStyleAndWeight = style,
                marginTop = 4,
                marginBottom = 4,
                color = new Color(0.9f, 0.9f, 0.9f)
            },
            text = text
        };
        container.Add(header);
    }

    private void AddBulletList(VisualElement container, string paragraph)
    {
        string[] items = paragraph.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var item in items)
        {
            string trimmedItem = item.Trim();
            if (trimmedItem.StartsWith("- ") || trimmedItem.StartsWith("* "))
            {
                string bulletText = trimmedItem.Substring(2);
                
                var bulletContainer = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        marginLeft = 8,
                        marginBottom = 2
                    }
                };
                
                var bullet = new Label
                {
                    style = { marginRight = 4 },
                    text = "•"
                };
                
                var bulletContent = new Label
                {
                    style = { whiteSpace = WhiteSpace.Normal, flexGrow = 1 },
                    text = FormatInlineMarkdown(bulletText)
                };
                
                bulletContainer.Add(bullet);
                bulletContainer.Add(bulletContent);
                container.Add(bulletContainer);
            }
        }
    }

    private void AddNumberedList(VisualElement container, string paragraph)
    {
        string[] items = paragraph.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var item in items)
        {
            string trimmedItem = item.Trim();
            Match match = Regex.Match(trimmedItem, @"^(\d+)\.(.*)$");
            
            if (match.Success)
            {
                string number = match.Groups[1].Value;
                string content = match.Groups[2].Value.Trim();
                
                var numberContainer = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        marginLeft = 8,
                        marginBottom = 2
                    }
                };
                
                var numberLabel = new Label
                {
                    style = { marginRight = 4, minWidth = 20 },
                    text = number + "."
                };
                
                var numberContent = new Label
                {
                    style = { whiteSpace = WhiteSpace.Normal, flexGrow = 1 },
                    text = FormatInlineMarkdown(content)
                };
                
                numberContainer.Add(numberLabel);
                numberContainer.Add(numberContent);
                container.Add(numberContainer);
            }
        }
    }

    private void AddFormattedParagraph(VisualElement container, string paragraph)
    {
        var formattedText = new Label
        {
            style =
            {
                whiteSpace = WhiteSpace.Normal,
                marginBottom = 4,
                marginLeft = 4
            },
            text = FormatInlineMarkdown(paragraph)
        };
        
        container.Add(formattedText);
    }

    private string FormatInlineMarkdown(string text)
    {
        // Replace **text** with <b>text</b> for bold
        text = Regex.Replace(text, @"\*\*(.*?)\*\*", "<b>$1</b>");
        
        // Replace *text* or _text_ with <i>text</i> for italic
        text = Regex.Replace(text, @"\*(.*?)\*|_(.*?)_", "<i>$1$2</i>");
        
        // Replace `text` with <color=#AADDFF>text</color> for inline code
        text = Regex.Replace(text, @"`(.*?)`", "<color=#AADDFF>$1</color>");
        
        return text;
    }

    // Process code blocks in AI responses for better display
    private void ProcessCodeBlocksInMessage(string message)
    {
        // Pattern to match code blocks with file paths
        // Format: ```csharp:Assets/Scripts/SomeFile.cs ... ```
        var codeBlockPattern = new Regex(@"```(?:csharp|cs):([^\n]+)\n([\s\S]*?)```");
        var matches = codeBlockPattern.Matches(message);
        
        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                string filePath = match.Groups[1].Value.Trim();
                string codeContent = match.Groups[2].Value;
                
                // Create a visual representation of the code block
                AddCodeBlockToHistory(filePath, codeContent);
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
        conversationHistory.Clear();
        if (conversationScrollView != null)
        {
            conversationScrollView.Clear();
        }
        AddMessageToHistory("System", "Conversation history cleared.");
    }

    private void RestoreConversationHistory()
    {
        foreach (var message in conversationHistory)
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
        
        // Ensure we scroll to the bottom after restoring
        EditorApplication.delayCall += ScrollToBottom;
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
            var formattedContent = CreateMarkdownFormattedContent(message);
            messageContainer.Add(formattedContent);
            
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

        // Scroll to bottom using the helper method
        EditorApplication.delayCall += ScrollToBottom;
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
        // If we already have a UI built, restore the conversation
        if (rootVisualElement != null && conversationScrollView != null)
        {
            // Even if there's no history, we should still set up the scroll view
            if (conversationHistory.Count > 0)
            {
                RestoreConversationHistory();
            }
            
            // Always scroll to bottom when window is enabled (after compilation)
            EditorApplication.delayCall += ScrollToBottom;
        }
        
        // Register for compilation events to scroll to bottom after compilation
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.compilationFinished += OnCompilationFinished;
    }
    
    // Override OnDisable to save any state before the window is disabled
    private void OnDisable()
    {
        // Unity will automatically serialize the fields marked with [SerializeField]
        
        // Unregister from compilation events
        CompilationPipeline.compilationStarted -= OnCompilationStarted;
        CompilationPipeline.compilationFinished -= OnCompilationFinished;
    }

    private void OnCompilationStarted(object obj)
    {
        // Nothing to do when compilation starts
    }

    private void OnCompilationFinished(object obj)
    {
        // Scroll to bottom after compilation finishes
        EditorApplication.delayCall += ScrollToBottom;
    }
}