/*
 * PlannerGPT.cs
 *
 * This script defines the PlannerGPT class that extends the ChatBot class to manage an interactive conversation
 * with the user for planning Unity scenes using OpenAI's GPT-4 API. It performs several key functions:
 *
 * 1. Initialization and Prompt Loading:
 *    - The script attempts to load a dedicated planning prompt from a file ("PlannerGPT.txt") located in a specific
 *      directory. If the file is not found, it falls back to a default, hardcoded prompt.
 *    - The prompt instructs the assistant to gather detailed user requirements and generate a comprehensive plan
 *      for constructing a Unity scene.
 *
 * 2. Conversation Handling:
 *    - It maintains an internal conversation history (stored as a list of messages and as a concatenated string 'history')
 *      to preserve context across multiple exchanges.
 *    - Each new user input and assistant response is appended to this history.
 *    - When sending a new request to GPT-4, the entire conversation context is built and prepended as a system message,
 *      ensuring that the assistant has full context for its responses.
 *
 * 3. Streaming GPT-4 Responses:
 *    - The class constructs a chat request using the full conversation context and sends it to the GPT-4 API.
 *    - The assistant's response is streamed in chunks, with each chunk appended to the conversation history and
 *      displayed in real time on the chat UI.
 *
 * 4. Scene Processing Trigger:
 *    - The script monitors the conversation history for the marker "[Conversation finished]". When detected,
 *      and only once per session (ensured by a flag), it automatically prompts GPT-4 for the final plan.
 *    - Subsequently, it calls a SceneParser component to parse the current Unity scene hierarchy, producing a compact
 *      JSON representation of the scene.
 *    - This parsed scene JSON is then appended to the conversation history so that it is displayed on the chat UI.
 *
 * 5. UI Updates:
 *    - TextMeshPro UI components are used to display user inputs, assistant responses, and the final parsed scene JSON.
 *    - The UpdateHistoryUI() method updates the UI element to reflect the latest conversation history.
 *
  * This script is designed for runtime use in Unity's Play Mode and integrates with UI components like TMP_InputField
 * and TMP_Text to facilitate interactive user input and conversation display.
 */

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


    // References to scene processing components (assign via Inspector)
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
        base.Awake();
        // Attempt to load the planner prompt from file.
        string filePath = Path.Combine(Application.dataPath, "Scripts", "XeleR Scripts", "MetaPrompt", "PlannerGPT.txt");
        if (File.Exists(filePath))
        {
            string loadedPrompt = File.ReadAllText(filePath);
            SetMetapromptAndClearHistory(loadedPrompt);
            Debug.Log("Planner prompt loaded from file: " + filePath);
        }
        else
        {
            Debug.LogError("Planner prompt file not found at: " + filePath + ". Using fallback prompt.");
            SetMetapromptAndClearHistory(plannerPrompt);
        }

        // Try to find an existing SceneParser in the scene
        SceneParser sceneParser = FindObjectOfType<SceneParser>();
        if (sceneParser == null)
        {
            GameObject parserGO = new GameObject("SceneParser");
            sceneParser = parserGO.AddComponent<SceneParser>();
            Debug.Log("SceneParser was not found. Created new SceneParser GameObject.");
        }

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
        ChatHistory.Add(new Message(Role.User, input_str));
        history += "User: " + input_str + "\n\n";
        UpdateHistoryUI();

        string fullContext = BuildFullContext();
        List<Message> promptMessages = new List<Message>
        {
            new Message(Role.System, fullContext)
        };

        promptMessages.AddRange(ChatHistory);

        ChatRequest request = new ChatRequest(promptMessages, Model.GPT4, temperature: Temperature, maxTokens: MaxTokens);
        string fullResult = "";
        history += "Assistant: \n";
        if (output_TMP != null)
            output_TMP.text = "";
        OpenAIClient api = new OpenAIClient();
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
