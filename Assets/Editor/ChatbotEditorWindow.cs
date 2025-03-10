using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

public class ChatbotEditorWindow : EditorWindow
{
    private const string PLACEHOLDER_TEXT = "Type your message...";

    private ScrollView conversationScrollView;
    private TextField queryField;
    private Button sendButton;

    // Reference to your PlannerGPT instance.
    public PlannerGPT plannerGPT;

    // List to hold the session history (for UI display only).
    private List<string> sessionHistory = new List<string>();

    [MenuItem("Window/Chatbox")]
    public static void ShowWindow()
    {
        var editorAssembly = typeof(Editor).Assembly;
        var inspectorType = editorAssembly.GetType("UnityEditor.InspectorWindow");
        if (inspectorType == null)
        {
            Debug.LogWarning("InspectorWindow type not found; opening Chatbot without docking.");
            var wndFallback = GetWindow<ChatbotEditorWindow>("Chat v0", true);
            wndFallback.Show();
        }
        else
        {
            var wnd = GetWindow<ChatbotEditorWindow>("Chat v0", true, inspectorType);
            wnd.Show();
        }
    }

    private void OnEnable()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("PlannerGPT is not available in Edit Mode. Enter Play Mode to test chat functionality.");
            return;
        }

        // Try to find an existing PlannerGPT instance.
        if (plannerGPT == null)
        {
            plannerGPT = GameObject.FindObjectOfType<PlannerGPT>();
            if (plannerGPT == null)
            {
                GameObject plannerGO = new GameObject("PlannerGPTInstance");
                plannerGPT = plannerGO.AddComponent<PlannerGPT>();
                Debug.LogWarning("No PlannerGPT found. Created a new instance.");
            }
        }
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        rootVisualElement.style.flexDirection = FlexDirection.Column;

        conversationScrollView = new ScrollView(ScrollViewMode.Vertical)
        {
            style = { flexGrow = 1 },
            verticalScrollerVisibility = ScrollerVisibility.Auto
        };
        rootVisualElement.Add(conversationScrollView);

        var inputRow = new VisualElement
        {
            style = { flexDirection = FlexDirection.Row, marginBottom = 4, marginLeft = 4, marginRight = 4 }
        };

        queryField = new TextField
        {
            style = { flexGrow = 1, marginRight = 4 }
        };
        queryField.SetValueWithoutNotify(PLACEHOLDER_TEXT);
        queryField.AddToClassList("placeholder-text");
        inputRow.Add(queryField);

        queryField.RegisterCallback<FocusInEvent>(OnFocusInQueryField);
        queryField.RegisterCallback<FocusOutEvent>(OnFocusOutQueryField);

        sendButton = new Button(OnSendButtonClicked) { text = "Send" };
        inputRow.Add(sendButton);

        rootVisualElement.Add(inputRow);
    }

    private async void OnSendButtonClicked()
    {
        if (queryField.value == PLACEHOLDER_TEXT)
            queryField.value = string.Empty;

        var userText = queryField.value?.Trim();
        if (string.IsNullOrEmpty(userText)) return;

        // Add the user's message to the session history and UI.
        AddMessageToHistory("You", userText);

        queryField.SetEnabled(false);
        sendButton.SetEnabled(false);
        queryField.SetValueWithoutNotify(string.Empty);

        if (plannerGPT == null)
        {
            Debug.LogError("PlannerGPT reference is not set in ChatbotEditorWindow.");
            AddMessageToHistory("System", "<error: PlannerGPT not assigned>");
        }
        else
        {
            // Send the user input to PlannerGPT for processing.
            string assistantReply = await plannerGPT.ConverseWithUser(userText);
            AddMessageToHistory("XeleR", assistantReply);
        }

        queryField.SetEnabled(true);
        sendButton.SetEnabled(true);

        if (string.IsNullOrEmpty(queryField.value))
        {
            queryField.SetValueWithoutNotify(PLACEHOLDER_TEXT);
            queryField.AddToClassList("placeholder-text");
        }
    }

    private void AddMessageToHistory(string sender, string message)
    {
        string formatted = $"<b>{sender}:</b> {message}";
        sessionHistory.Add(formatted);
        var msgLabel = new Label
        {
            style = { whiteSpace = WhiteSpace.Normal, marginBottom = 4 },
            enableRichText = true,
            text = formatted
        };
        conversationScrollView.Add(msgLabel);
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
}

[InitializeOnLoad]
public static class ChatbotAutoOpen
{
    static ChatbotAutoOpen()
    {
        EditorApplication.update += OpenOnce;
    }
    private static void OpenOnce()
    {
        EditorApplication.update -= OpenOnce;
        ChatbotEditorWindow.ShowWindow();
    }
}
