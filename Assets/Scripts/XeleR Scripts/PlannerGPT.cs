using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;
using OpenAI.Chat;
using OpenAI;
using OpenAI.Models;
using System.Linq;
using System.IO;

public class PlannerGPT : ChatBot
{
    public TMP_InputField input_TMP;    // For input, if used in-scene
    public TMP_Text output_TMP;         // For current reply
    public TMP_Text historyText;        // For displaying full conversation history

    // References to scene processing components
    public SceneParser sceneParser;
    private bool sceneProcessed = false; // Flag to ensure processing happens only once.

    [TextArea(10, 30)]
    public string plannerPrompt = @"Your goal is to discuss with the user what they want and to make a plan for their request after gathering good information.
    The user will ask to make a scene in Unity.
    - You should pay attention to the user's requests and come up with a plan that covers everything they ask for.
    - Each step of your plan should be properly scoped so that the Builder can execute it successfully.
    - Be flexible in your discussion but assertive in each step—commit to a single approach.
    - When you want to stop the conversation, output: [Conversation finished].
    - Ask the user if the plan is good and end the conversation when they confirm.
    - Only ask crucial questions, one at a time.
    - After two conversation turns, present the final plan.";

    protected override void Awake()
    {
        Debug.Log("[PlannerGPT] Awake");
        
        // Initialize base ChatBot
        base.Awake();
        
        // Attempt to load the planner prompt from file.
        TryLoadPlannerPrompt();
        
        // Set up SceneParser reference
        SetupSceneParser();
    }

    private void Start()
    {
        Debug.Log("[PlannerGPT] Start");
    }

    private void TryLoadPlannerPrompt()
    {
        // Try multiple potential paths
        string[] potentialPaths = new string[]
        {
            Path.Combine(Application.dataPath, "Scripts", "XeleR Scripts", "MetaPrompt", "PlannerGPT.txt"),
            Path.Combine(Application.dataPath, "Scripts", "MetaPrompt", "PlannerGPT.txt"),
            Path.Combine(Application.dataPath, "XeleR Scripts", "MetaPrompt", "PlannerGPT.txt"),
            Path.Combine(Application.dataPath, "MetaPrompt", "PlannerGPT.txt")
        };

        foreach (string filePath in potentialPaths)
        {
            if (File.Exists(filePath))
            {
                string loadedPrompt = File.ReadAllText(filePath);
                SetMetapromptAndClearHistory(loadedPrompt);
                Debug.Log("[PlannerGPT] Planner prompt loaded from file: " + filePath);
                return;
            }
        }

        // If we get here, no file was found
        Debug.LogWarning("[PlannerGPT] Planner prompt file not found in any of the expected locations. Using fallback prompt.");
        SetMetapromptAndClearHistory(plannerPrompt);
    }

    private void SetupSceneParser()
    {
        // Use the existing SceneParser if available
        if (sceneParser != null)
        {
            Debug.Log("[PlannerGPT] Using existing SceneParser reference: " + sceneParser);
            return;
        }
        
        // Try to find an existing SceneParser in the scene
        sceneParser = FindObjectOfType<SceneParser>();
        if (sceneParser != null)
        {
            Debug.Log("[PlannerGPT] Found existing SceneParser: " + sceneParser);
            return;
        }
        
        // Create a new SceneParser if none exists
        GameObject parserGO = new GameObject("SceneParser");
        sceneParser = parserGO.AddComponent<SceneParser>();
        Debug.Log("[PlannerGPT] Created new SceneParser GameObject.");
    }

    private string BuildFullContext()
    {
        string context = "";
        foreach (var msg in ChatHistory)
        {
            context += $"{msg.Role}: {msg.Content}\n";
        }
        return context;
    }

    // Main conversation method. It appends the new user input to ChatHistory,
    // then builds a new prompt by prepending the full context as a system message.
    public async Task<string> ConverseWithUser(string input_str)
    {
        Debug.Log("[PlannerGPT] ConverseWithUser called with input: " + input_str);
        
        // Check for API key
        if (string.IsNullOrEmpty(apiKey))
        {
            string errorMessage = "Error: OpenAI API key is missing. Please set your API key.";
            Debug.LogError("[PlannerGPT] " + errorMessage);
            history += "Assistant: " + errorMessage + "\n\n";
            UpdateHistoryUI();
            return errorMessage;
        }
        
        if (ChatHistory == null)
        {
            Debug.LogError("[PlannerGPT] ChatHistory is null! Reinitializing...");
            ChatHistory = new List<Message>();
            ChatHistory.Add(new Message(Role.System, plannerPrompt));
        }
        
        ChatHistory.Add(new Message(Role.User, input_str));
        history += "User: " + input_str + "\n\n";
        UpdateHistoryUI();

        string fullContext = BuildFullContext();
        List<Message> promptMessages = new List<Message>
        {
            new Message(Role.System, fullContext)
        };

        string fullResult = "";
        history += "Assistant: \n";
        if (output_TMP != null)
            output_TMP.text = "";
        
        try
        {
            OpenAIClient api = new OpenAIClient(apiKey);
            ChatRequest request = new ChatRequest(promptMessages, Model.GPT4, temperature: Temperature, maxTokens: MaxTokens);
            
            await api.ChatEndpoint.StreamCompletionAsync(request, result =>
            {
                if(result.FirstChoice != null && result.FirstChoice.Message != null)
                {
                    string chunk = result.FirstChoice.Message.Content?.ToString();
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        fullResult += chunk;
                        history += chunk;
                        if (output_TMP != null)
                            output_TMP.text += chunk;
                        UpdateHistoryUI();
                    }
                }
            });
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[PlannerGPT] Error during API call: " + ex.Message);
            string errorMessage = "Error: " + ex.Message;
            fullResult = errorMessage;
            history += errorMessage;
            UpdateHistoryUI();
        }

        ChatHistory.Add(new Message(Role.Assistant, fullResult));
        history += "\n\n";
        UpdateHistoryUI();

        // If conversation is finished, e.g., output equals "[Conversation finished]"
        // When conversation is finished, call scene processing.
        if (!sceneProcessed && history.Contains("[Conversation finished]"))
        {
            sceneProcessed = true;
            Debug.Log("[PlannerGPT] Finished processing scene." + sceneProcessed);
            Debug.Log("[PlannerGPT] Detected Finished Conversation");
            
            // Remove the marker from the history so it doesn't trigger again.
            history = history.Replace("[Conversation finished]", "");

            // Present the final plan.
            string finalPlan = await ConverseWithUser("Present the final plan.");
            Debug.Log("[PlannerGPT] Final Plan: " + finalPlan);
            Debug.Log("[PlannerGPT] SceneParser reference: " + sceneParser);

            // Call SceneParser if references are set.
            if (sceneParser != null)
            {
                Debug.Log("[PlannerGPT] Calling SceneParser to parse scene hierarchy.");
                sceneParser.ParseSceneHierarchy();  // Parse scene hierarchy synchronously.
                string sceneJson = sceneParser.scene_parsing_compact;
                Debug.Log("[PlannerGPT] Scene JSON obtained:\n" + sceneJson);

                // Append the parsed scene JSON to the chat UI.
                history += "\nThis is the Parsed Scene JSON:\n" + sceneJson + "\n\n";
                Debug.Log("[Add Parsed Scene Output]Updated History: " + history);

                UpdateHistoryUI();
            }
            else
            {
                Debug.LogWarning("[PlannerGPT] SceneParser reference not set.");
            }
        }

        return fullResult;
    }
    
    private void UpdateHistoryUI()
    {
        if (historyText != null)
        {
            historyText.text = history;
        }
    }
}