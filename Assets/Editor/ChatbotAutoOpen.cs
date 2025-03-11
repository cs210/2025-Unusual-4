using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class ChatbotAutoOpen
{
    // Static constructor called when Unity starts
    static ChatbotAutoOpen()
    {
        // Check if we've already opened the window in this session
        if (!SessionState.GetBool("ChatbotWindowOpenedThisSession", false))
        {
            // Set the flag to prevent reopening in the same session
            SessionState.SetBool("ChatbotWindowOpenedThisSession", true);
            
            // Use a delayed call to ensure Unity is fully initialized
            EditorApplication.delayCall += OpenChatbotWindow;
        }
    }
    
    private static void OpenChatbotWindow()
    {
        // Open the window using the ShowWindow method
        ChatbotEditorWindow.ShowWindow();
        Debug.Log("ChatbotAutoOpen: Opening chatbot window on project launch");
    }
} 