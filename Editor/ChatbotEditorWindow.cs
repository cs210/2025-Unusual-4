using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using System;
using System.Text;
using System.Threading.Tasks;

public class ChatbotEditorWindow : EditorWindow
{
    private string openAiApiKey = "<OPENAI API KEY>";  

    private const string PLACEHOLDER_TEXT = "Type your message...";

    private ScrollView conversationScrollView;
    private TextField queryField;
    private Button sendButton;

    [MenuItem("Window/Chatbox")]
    public static void ShowWindow()
    {
        // This overload attempts to dock it next to the Inspector
        var editorAssembly = typeof(UnityEditor.Editor).Assembly;
        var inspectorType = editorAssembly.GetType("UnityEditor.InspectorWindow");

        if (inspectorType == null)
        {
            // If for some reason we can’t find InspectorWindow, just open normally
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

        // Input row
        var inputRow = new VisualElement
        {
            style =
            {
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
        queryField.SetValueWithoutNotify(PLACEHOLDER_TEXT);
        queryField.AddToClassList("placeholder-text");
        inputRow.Add(queryField);

        // Focus events for placeholder simulation
        queryField.RegisterCallback<FocusInEvent>(OnFocusInQueryField);
        queryField.RegisterCallback<FocusOutEvent>(OnFocusOutQueryField);

        // Send button
        sendButton = new Button(OnSendButtonClicked) { text = "Send" };
        inputRow.Add(sendButton);

        rootVisualElement.Add(inputRow);
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

        // Send to OpenAI
        SendQueryToOpenAI(userText, (assistantReply) =>
        {
            AddMessageToHistory("XeleR", assistantReply);

            // Re-enable input
            queryField.SetEnabled(true);
            sendButton.SetEnabled(true);

            if (string.IsNullOrEmpty(queryField.value))
            {
                queryField.SetValueWithoutNotify(PLACEHOLDER_TEXT);
                queryField.AddToClassList("placeholder-text");
            }
        });
    }

    private async void SendQueryToOpenAI(string userMessage, Action<string> onResponse)
    {
        const string url = "https://api.openai.com/v1/chat/completions";
        string jsonPayload = $"{{\"model\":\"gpt-3.5-turbo\",\"messages\":[{{\"role\":\"user\",\"content\":\"{EscapeJson(userMessage)}\"}}]}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + openAiApiKey);

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("OpenAI API Error: " + request.error);
                onResponse?.Invoke("<error: could not get response>");
                return;
            }

            string responseJson = request.downloadHandler.text;
            string assistantText = ParseAssistantReply(responseJson);
            onResponse?.Invoke(assistantText);
        }
    }

    private void AddMessageToHistory(string sender, string message)
    {
        var msgLabel = new Label
        {
            style =
            {
                whiteSpace = WhiteSpace.Normal,
                marginBottom = 4
            },
            enableRichText = true,
            text = $"<b>{sender}:</b> {message}"
        };

        conversationScrollView.Add(msgLabel);

        // Scroll to bottom
        EditorApplication.delayCall += () =>
        {
            float fullHeight = conversationScrollView.contentContainer.layout.height;
            conversationScrollView.scrollOffset = new Vector2(0, fullHeight);
        };
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

    private string ParseAssistantReply(string json)
    {
        const string contentTag = "\"content\":";
        int startIndex = json.IndexOf(contentTag);
        if (startIndex == -1) return "<No content found in response>";

        startIndex = json.IndexOf('"', startIndex + contentTag.Length) + 1;
        if (startIndex < 0) return "<Invalid JSON format>";

        int endIndex = json.IndexOf('"', startIndex);
        if (endIndex < 0) return "<Invalid JSON format>";

        string extracted = json.Substring(startIndex, endIndex - startIndex);
        extracted = extracted.Replace("\\n", "\n").Replace("\\r", "\r");
        return extracted;
    }

    private string EscapeJson(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }
}

[InitializeOnLoad]
public static class ChatbotAutoOpen
{
    // This static constructor is called when the editor finishes loading
    static ChatbotAutoOpen()
    {
        EditorApplication.update += OpenOnce;
    }

    private static void OpenOnce()
    {
        // Avoid repeating on every update
        EditorApplication.update -= OpenOnce;

        // Show and dock next to Inspector
        ChatbotEditorWindow.ShowWindow();
    }
}