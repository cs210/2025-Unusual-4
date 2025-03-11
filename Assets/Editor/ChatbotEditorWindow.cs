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

    public PlannerGPT plannerGPT;
    
    // Track if we need to create PlannerGPT
    private bool needsSceneSetup = false;

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
            needsSceneSetup = true;
            return;
        }

        SetupPlannerGPT();
    }

    private void OnFocus()
    {
        // Check again for PlannerGPT when the window gets focus
        if (EditorApplication.isPlaying && (plannerGPT == null || !plannerGPT))
        {
            SetupPlannerGPT();
        }
    }

    private void Update()
    {
        // Check if we've entered play mode and need to set up the scene
        if (needsSceneSetup && EditorApplication.isPlaying)
        {
            needsSceneSetup = false;
            SetupPlannerGPT();
            CreateGUI(); // Refresh the UI when entering play mode
        }
    }

    private void SetupPlannerGPT()
    {
        // Try to find an existing PlannerGPT instance.
        plannerGPT = GameObject.FindObjectOfType<PlannerGPT>();
        if (plannerGPT == null)
        {
            GameObject plannerGO = new GameObject("PlannerGPTInstance");
            plannerGPT = plannerGO.AddComponent<PlannerGPT>();
            Debug.Log("No PlannerGPT found. Created a new instance: " + plannerGPT);
            
            // Add a SceneParser component if needed by PlannerGPT
            if (plannerGO.GetComponent<SceneParser>() == null)
            {
                plannerGO.AddComponent<SceneParser>();
                Debug.Log("Added SceneParser component to PlannerGPTInstance");
            }
        }
        else
        {
            Debug.Log("Found existing PlannerGPT instance: " + plannerGPT);
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
        
        // Display current status in UI
        if (!EditorApplication.isPlaying)
        {
            AddMessageToHistory("System", "Enter Play Mode to use the chat functionality.");
        }
        else if (plannerGPT == null)
        {
            AddMessageToHistory("System", "PlannerGPT not assigned. Trying to create one...");
            SetupPlannerGPT();
            if (plannerGPT != null)
            {
                AddMessageToHistory("System", "PlannerGPT created successfully. Ready to chat!");
            }
            else
            {
                AddMessageToHistory("System", "Failed to create PlannerGPT. Please check the console for errors.");
            }
        }
        else
        {
            AddMessageToHistory("System", "Ready to chat. Type your message and click Send.");
        }
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

        // Check if we're in play mode
        if (!EditorApplication.isPlaying)
        {
            AddMessageToHistory("System", "Please enter Play Mode to use chat functionality.");
            queryField.SetEnabled(true);
            sendButton.SetEnabled(true);
            return;
        }

        // Check PlannerGPT reference again
        if (plannerGPT == null)
        {
            Debug.LogError("PlannerGPT reference is not set in ChatbotEditorWindow.");
            SetupPlannerGPT();
            
            if (plannerGPT == null)
            {
                AddMessageToHistory("System", "<error: PlannerGPT could not be created>");
                queryField.SetEnabled(true);
                sendButton.SetEnabled(true);
                return;
            }
        }

        // Send the user input to PlannerGPT for processing.
        try
        {
            string assistantReply = await plannerGPT.ConverseWithUser(userText);
            AddMessageToHistory("XeleR", assistantReply);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error while conversing with PlannerGPT: " + ex.Message);
            AddMessageToHistory("System", "<error: " + ex.Message + ">");
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