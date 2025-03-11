using OpenAI.Chat;
using OpenAI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using OpenAI.Models;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.IO;
using TiktokenSharp;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using System.Net;
using System;

public class ChatBot : MonoBehaviour
{
    [Header("API Configuration")]
    [Tooltip("Your OpenAI API Key. IMPORTANT: Do not check this into source control!")]
    [SerializeField] protected string apiKey = "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";
    
    [Tooltip("Load API Key from a local file outside of project (more secure than Inspector)")]
    [SerializeField] private bool loadApiKeyFromFile = true;
    
    [Tooltip("Path to the file containing just the API key (relative to project folder)")]
    [SerializeField] private string apiKeyFilePath = "../2025-Unusual-4/openapi_key.txt";

    [Header("Prompt Configuration")]
    [TextArea(2, 20)]
    public string metaprompt_file_name;

    [TextArea(10, 100)]
    public string input;

    [TextArea(50, 100)]
    public string output;

    [TextArea(20, 100)]
    public string history;

    [TextArea(5, 20)]
    public string metaprompt;

    [TextArea(2, 20)]
    public string processing_status_text;
    [TextArea(2, 20)]
    public string processing_finished_status_text;

    public string model_name = "gpt-4";
    public int MaxTokens = 512; // max number of tokens per response
    public int context_length = 8000; // model context length
    public TokenManagementOption token_management_option = TokenManagementOption.None;
    private List<string> stopValues = new List<string>();

    [Tooltip("Temperature value has to be between 0 and 1.")]
    public double Temperature = 0.7;

    [Tooltip("Frequency penalty value has to be between 0 and 2.")]
    public double FrequencyPenalty;

    protected List<Message> ChatHistory = new List<Message>();
    
    protected TikToken tokenizer;

    public enum TokenManagementOption
    {
        // None: Do nothing. Throw error if the max token size is exceeded.
        // FIFO: Eliminate chat history from earliest to latest until enough tokens have been freed up for a new chat
        // Full_Reset: Clear all chat history (except metaprompt) when context window is full
        None, FIFO, Full_Reset
    }

    protected virtual void Awake()
    {
        stopValues.Add("/*");
        stopValues.Add("</");

        // Try to load API key from file if needed
        if (loadApiKeyFromFile)
        {
            LoadApiKeyFromFile();
        }

        if (metaprompt_file_name != "")
        {
            LoadMetapromptFromFile();
        }

        ChatHistory.Add(new Message(Role.System, metaprompt));

        // for counting tokens
        tokenizer = TikToken.EncodingForModel(model_name);
    }

    private void LoadApiKeyFromFile()
    {
        try
        {
            string fullPath = Path.GetFullPath(apiKeyFilePath);
            Debug.Log($"Looking for API key at: {fullPath}");
            
            if (File.Exists(fullPath))
            {
                apiKey = File.ReadAllText(fullPath).Trim();
                Debug.Log("API key loaded successfully");
            }
            else
            {
                Debug.LogError($"API key file not found at: {fullPath}");
                
                // Create an empty file as a placeholder r
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                    File.WriteAllText(fullPath, "YOUR_OPENAI_API_KEY_HERE");
                    Debug.Log($"Created empty API key file at: {fullPath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create API key file: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading API key from file: {ex.Message}");
        }
    }

    protected void LoadMetapromptFromFile()
{
    if (string.IsNullOrEmpty(metaprompt_file_name))
    {
        Debug.LogWarning("No metaprompt file name specified");
        return;
    }
    string path = Path.Combine("Scripts", "XeleR Scripts", "MetaPrompt", metaprompt_file_name + ".txt");
    string fullPath = Path.Combine(Application.dataPath, path);
    Debug.Log("Looking for metaprompt at: " + fullPath);
    if (File.Exists(fullPath))
    {
        metaprompt = File.ReadAllText(fullPath);
        Debug.Log("Loaded metaprompt from: " + fullPath);
    }
    else
    {
        Debug.LogError("Metaprompt file not found at: " + fullPath);
    }
}

    public virtual async Task SendChatOld()
    {
        // Check for API key
        if (string.IsNullOrEmpty(apiKey))
        {
            string errorMessage = "Error: OpenAI API key is missing. Please set your API key.";
            Debug.LogError(errorMessage);
            output = errorMessage;
            history += "assistant: \n" + errorMessage + "\n\n";
            return;
        }
        
        if (GetNumTokensForHistoryAndNextChat() > context_length) 
        {
            ManageMemory();
        }

        // send a chat
        OpenAIClient api = new OpenAIClient(apiKey);
        ChatHistory.Add(new Message(Role.User, input));
        history += "user: \n" + input + "\n\n";
        ChatRequest chatRequest = new ChatRequest(ChatHistory, model_name, temperature: Temperature, maxTokens: MaxTokens);
        string fullResult = "";
        history += "assistant: \n";
        output = "";

        try 
        {
            // wait for the response
            await api.ChatEndpoint.StreamCompletionAsync(chatRequest, result =>
            {
                output += result.FirstChoice.ToString();
                fullResult += result.FirstChoice.ToString();
                history += result.FirstChoice.ToString();
            });

            ChatHistory.Add(new Message(Role.Assistant, fullResult));
            history += "\n\n";
        }
        catch (Exception ex)
        {
            string errorMessage = $"Error: {ex.Message}";
            Debug.LogError(errorMessage);
            output = errorMessage;
            history += errorMessage + "\n\n";
        }
    }

   
    public virtual async Task SendChat()
    {
        // Check for API key
        if (string.IsNullOrEmpty(apiKey))
        {
            string errorMessage = "Error: OpenAI API key is missing. Please set your API key.";
            Debug.LogError(errorMessage);
            output = errorMessage;
            history += "assistant: \n" + errorMessage + "\n\n";
            return;
        }
        
        OpenAIClient api = new OpenAIClient(apiKey);
        int retryDelaySeconds = 60; // The delay in seconds before retrying the request
        int maxRetries = 5; // Maximum number of retries

        ChatHistory.Add(new Message(Role.User, input));
        history += "user: \n" + input + "\n\n";
        ChatRequest chatRequest = new ChatRequest(ChatHistory, model_name, temperature: Temperature, maxTokens: MaxTokens);
        string fullResult = "";
        history += "assistant: \n";
        output = "";

        int retries = 0;

        while (retries < maxRetries)
        {
            try
            {
                // wait for the response
                var response = await api.ChatEndpoint.StreamCompletionAsync(chatRequest, partialresult =>
                {
                    //Debug.Log("Output of chatbot is: "+partialresult);
                });
                output += response.FirstChoice.Message;
                fullResult += output;
                history += output;

                ChatHistory.Add(new Message(Role.Assistant, fullResult));
                history += "\n\n";
                break; // Exit the loop if the request was successful
            }
            catch (Exception ex)
            {
                // Check if the exception message indicates a rate limit error
                if (ex.Message.Contains("rate limit") || ex.Message.Contains("429"))
                {
                    // Handle rate limit exception
                    Debug.LogWarning($"Rate limit exceeded. Retrying in {retryDelaySeconds} seconds...");
                    await Task.Delay(retryDelaySeconds * 1000); // Convert seconds to milliseconds
                    retries++;
                }
                else
                {
                    // Handle other exceptions
                    string errorMessage = $"An error occurred: {ex.Message}";
                    Debug.LogError(errorMessage);
                    output = errorMessage;
                    history += errorMessage + "\n\n";
                    break; // Break out of the loop on other types of exceptions
                }
            }
        }

        if (retries >= maxRetries)
        {
            string errorMessage = "Failed to get a response from GPT-4 after several retries.";
            Debug.LogError(errorMessage);
            output = errorMessage;
            history += errorMessage + "\n\n";
        }
    }


    public async Task SendChatWithInput(string chat_input)
    {
        input = chat_input;
        await SendChat();
    }

    public async Task SendNewChat()
    {
        ClearChatMemory();
        await SendChat();
    }


    public int GetNumTokensForHistoryAndNextChat()
    {
        // assumes the next request is stored in input
        int num_tokens_history = GetCurrentChatTokenSize();
        int num_tokens_in_request = tokenizer.GetNumTokens(input);
        int num_tokens_response = MaxTokens;

        return num_tokens_history + num_tokens_in_request + num_tokens_response;
    }

    public int GetCurrentChatTokenSize()
    {
        int num_tokens = 0;
        // loop through all historical chats, add up their token count
        foreach (Message chatPrompt in ChatHistory)
        {
            num_tokens += tokenizer.GetNumTokens(chatPrompt.Content.ToString());
        }
        return num_tokens;
    }

    public void ManageMemory()
    {
        print("Managing memory");
        if (token_management_option == TokenManagementOption.Full_Reset)
        {
            ClearChatHistory();
        }
        else if (token_management_option == TokenManagementOption.FIFO)
        {
            // ChatHistory should always contain the metaprompt, thus the base Count of 1.
            while (ChatHistory.Count > 1  && GetNumTokensForHistoryAndNextChat() > context_length)
            {
                // delete ChatHistory in pairs (user-assistant) from earliest to latest
                ChatHistory.RemoveAt(1);
                ChatHistory.RemoveAt(1);
            }
        }
    }

    // for debugging, replaces History
    public void DisplayCurrentContext()
    {
        history = "";
        foreach(Message chatPrompt in ChatHistory)
        {
            history += chatPrompt.Role + ":" + chatPrompt.Content + '\n';
        }
        print("Current token size: " + GetCurrentChatTokenSize());
    }

    public void SaveCurrentContext(string dir)
    {
        string context = "";
        foreach (Message chatPrompt in ChatHistory)
        {
            context += chatPrompt.Role + ":" + chatPrompt.Content + '\n';
        }

        File.WriteAllText(dir, context);
    }

    public async Task SendNewChatWithInput(string chat_input)
    {
        ClearChatMemory();
        input = chat_input;
        await SendChat();
    }

    public async void SendChatViaButton()
    {
        await SendChat();
    }

    public async void SendNewChatViaButton()
    {
        await SendNewChat();
    }

    public void SetMetapromptAndClearHistory(string metaprompt_new)
    {
        metaprompt = metaprompt_new;
        history = "";
        output = "";
        ChatHistory = new List<Message>();
        // add back the metaprompt
        ChatHistory.Add(new Message(Role.System, metaprompt));
    }

    public async Task ClearChatHistory()
    {
        history = "";
        output = "";
        ChatHistory = new List<Message>();
        // add back the metaprompt
        ChatHistory.Add(new Message(Role.System, metaprompt));
    }

    // clear GPT's memory, but the user can still inspect the history of all conversation that's happened.
    public async Task ClearChatMemory()
    {
        ChatHistory = new List<Message>();
        // add back the metaprompt
        ChatHistory.Add(new Message(Role.System, metaprompt));
    }

    public void ClearAllButLastChatMemory()
    {
        // the first element (metaprompt) and last element (most recent chat) remains
        ChatHistory = RemoveMiddleElements(ChatHistory);
    }

    public static List<T> RemoveMiddleElements<T>(List<T> list)
    {
        // If the list is null or has less than 3 elements, return it as it is
        if (list == null || list.Count < 3)
        {
            return list;
        }

        // Otherwise, create a new list with the first and the last elements of the original list
        List<T> result = new List<T>();
        result.Add(list[0]); // add the first element
        result.Add(list[list.Count - 1]); // add the last element

        // Return the new list
        return result;
    }
}