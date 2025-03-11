/*
 * PlannerGPT.cs
 * 
 * This script defines the PlannerGPT class, which inherits from ChatBot, to handle an interactive 
 * conversation for planning Unity scene requests using OpenAI's GPT-4.
 *
 * Key features:
 *  - Loads a dedicated planner prompt from a file ("Planner.txt") located at 
 *    "Assets/Scripts/XeleR/MetaPrompt/Planner.txt". If the file isn’t found, it falls back to a hardcoded prompt.
 *  - Maintains an internal conversation history (ChatHistory and the concatenated string "history").
 *  - Before sending a new request, it builds a full context string (including all previous user and assistant messages)
 *    and prepends it as a system message so that GPT-4 sees the entire conversation.
 *  - Sends the chat request using OpenAI's API and streams the assistant’s reply, updating the conversation history.
 *  - Updates UI elements (if assigned) to display the current reply and full conversation history.
 *  - Detects when the conversation is finished (if the assistant replies with "[Conversation finished]") and automatically
 *    sends a prompt to "Present the final plan."
 *
 * This script is intended for runtime use in Play Mode and integrates with UI components like TMP_InputField 
 * and TMP_Text for interactive user input and display.
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
