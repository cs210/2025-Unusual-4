# XeleR Codebase Documentation

## Overview

XeleR is a Unity Editor extension that provides AI-assisted XR prototyping capabilities. This document provides a technical overview of the codebase structure, implementation details, and key components to help new developers understand and contribute to the project. 

If you want a more product-focused overview, please refer to the [README](README.md).

## Technical Architecture

XeleR follows a modular architecture with a clear separation between:
- Editor UI components (Unity Editor integration)
- AI communication services
- Context Gathering 
- Scene analysis utilities

The system uses a combination of Unity's Editor extension APIs, UIElements for modern UI, and external AI services (OpenAI and Claude) to provide an integrated development experience.

## Core Components - Technical Details

### 1. Editor Integration (Assets/Editor/)

#### ChatbotEditorWindow.cs
The main UI component that inherits from `EditorWindow` to create a dockable window in Unity Editor.

**Key Classes:**
- `ChatMessage`: Stores individual messages with sender, content, and timestamp
- `ChatSession`: Manages a conversation thread with message history and context
- `ModelInfo`: Represents an AI model with name and provider information

**Key Functions:**
- `ShowWindow()`: Entry point that creates and displays the chat window
- `SendQueryToOpenAIStreaming()`: Handles streaming API communication with OpenAI
- `SendQueryToClaudeStreaming()`: Handles streaming API communication with Claude
- `ProcessCodeBlocksInMessage()`: Parses code blocks from AI responses using regex
- `ApplyCodeEdit()`: Applies code changes to project files with error handling
- `OnContextButtonClicked()`: Manages context menu for file/scene selection
- `CreateNewChatSession()`: Manages multiple conversation threads

**Technical Implementation:**
- Uses Unity's UIElements framework for modern, responsive UI
- Implements asynchronous streaming responses for real-time feedback
- Uses regex pattern matching to extract code blocks with file paths
- Maintains session persistence using EditorPrefs serialization
- Implements a token-aware context management system to stay within API limits

**Implementation Details:**
- The window is registered with Unity's Editor extension system via `[MenuItem("Window/Chatbox")]`
- Uses `UnityWebRequest` with chunked processing for streaming API responses
- Implements custom `VisualElement` creation for chat messages with markdown support
- Uses `EditorPrefs.SetString()` with JSON serialization for session persistence
- Implements file I/O operations with proper error handling for code application
- Uses `EditorApplication.delayCall` for UI updates to avoid threading issues
- Implements custom dropdown menus with `GenericMenu` for context selection

#### SceneAnalysisIntegration.cs
Static utility class that provides scene analysis capabilities by extracting information about Unity scenes.

**Key Functions:**
- `GetSceneStructureSummary()`: Analyzes scene hierarchy and component structure
- `GetSpatialInformation()`: Analyzes spatial relationships between objects
- `AppendGameObjectInfo()`: Recursively extracts GameObject properties
- `LoadMetaprompt()`: Loads analysis-specific system prompts
- `Cleanup()`: Clears cached analysis results

**Technical Implementation:**
- Uses recursive scene traversal to build complete hierarchy representations
- Implements a caching system with a configurable timeout to improve performance
- Uses reflection to extract component properties dynamically
- Performs raycasting for spatial relationship detection
- Captures scene screenshots for visual context

**Implementation Details:**
- Uses `SceneManager.GetActiveScene().GetRootGameObjects()` to access scene hierarchy
- Implements depth-first traversal with `transform.GetChildren()` for hierarchy analysis
- Uses `GameObject.GetComponents<Component>()` with reflection to extract properties
- Implements spatial analysis with `Physics.Raycast()` for object relationships
- Uses `EditorWindow.GetWindow<SceneView>()` for scene view capture
- Implements caching with `DateTime.Now` comparison for performance optimization
- Uses `StringBuilder` for efficient string concatenation in analysis results

#### ApiKeyManager.cs
Static utility class for secure API key management.

**Key Functions:**
- `GetKey()`: Retrieves and decrypts stored API keys
- `SetKey()`: Encrypts and stores API keys
- `Encrypt()/Decrypt()`: Handles AES encryption/decryption

**Technical Implementation:**
- Uses AES encryption with CBC mode for basic security
- Stores encrypted keys in EditorPrefs for cross-session persistence
- Implements key validation before API calls
- Provides constants for consistent key naming

**Implementation Details:**
- Uses `System.Security.Cryptography.AesManaged` for encryption
- Implements PKCS7 padding for AES encryption
- Uses Base64 encoding for encrypted string storage
- Stores keys in `EditorPrefs` with consistent key names
- Implements proper disposal of cryptographic resources
- Uses static initialization to ensure keys are loaded at startup

#### MarkdownRenderer.cs
Utility class that renders markdown-formatted text in the Unity UI.

**Key Functions:**
- `RenderMarkdown()`: Converts markdown text to styled visual elements
- `RenderHeading()`: Creates heading elements with appropriate styling
- `RenderCodeBlock()`: Renders code blocks with syntax highlighting
- `RenderList()`: Handles ordered and unordered lists

**Technical Implementation:**
- Uses regex pattern matching to identify markdown elements
- Creates UIElements with appropriate styling for each markdown component
- Implements basic syntax highlighting for code blocks
- Supports nested elements like lists within lists

**Implementation Details:**
- Uses `System.Text.RegularExpressions.Regex` for pattern matching
- Creates `VisualElement` hierarchies with proper styling
- Implements custom USS (Unity Style Sheets) for markdown styling
- Uses `Label`, `Box`, and custom elements for different markdown components
- Implements recursive parsing for nested markdown elements
- Handles code blocks with language-specific formatting

### 2. AI Communication

#### API Integration
The system communicates with multiple AI providers through REST APIs.

**OpenAI Integration:**
- Supports models: gpt-3.5-turbo, gpt-4, gpt-4-turbo, gpt-4o
- Implements streaming API for real-time responses
- Handles token counting and context management
- Processes chunked responses for incremental UI updates (text streaming)

**Claude Integration:**
- Supports models: claude-3-opus, claude-3-5-sonnet, claude-3-7-sonnet
- Uses Anthropic's API with appropriate headers and authentication

**Technical Implementation:**
- Uses UnityWebRequest for HTTP communication
- Implements proper header management for authentication
- Handles streaming responses with chunked processing
- Manages connection timeouts and error handling
- Implements backoff strategies for rate limiting

**Implementation Details:**
- Uses `UnityWebRequest.Post()` with JSON body for API requests
- Sets appropriate content-type headers for each API provider
- Implements custom download handlers for streaming responses
- Uses `DownloadHandlerBuffer.data` for chunked response processing
- Implements proper error handling with HTTP status codes
- Uses `EditorUtility.DisplayProgressBar()` for request progress indication

### 3. Runtime Components

#### ChatBot.cs
Base class for AI chat functionality that can be used in runtime applications.

**Key Properties:**
- `model_name`: Specifies which AI model to use
- `MaxTokens`: Controls maximum tokens per response
- `context_length`: Defines model context window size
- `Temperature`: Controls response randomness (0-1)
- `FrequencyPenalty`: Reduces repetition in responses

**Key Functions:**
- `SendMessage()`: Sends a message to the AI and receives a response
- `AddMessageToHistory()`: Manages conversation history
- `ManageTokens()`: Implements token counting and context management
- `LoadMetaprompt()`: Loads system prompts from files

**Technical Implementation:**
- Uses TikToken library for accurate token counting
- Implements multiple token management strategies (FIFO, Full_Reset)
- Handles conversation history as a list of message objects
- Supports temperature and frequency penalty adjustments

**Implementation Details:**
- Uses `TikToken.Encode()` for token counting
- Implements token management with configurable strategies
- Uses `Resources.Load<TextAsset>()` for prompt loading
- Implements proper message role assignment (system, user, assistant)
- Uses `JsonUtility.ToJson()` for API request serialization
- Implements proper error handling with try/catch blocks

## Data Flow

1. **User Input Flow:**
   - User enters query in ChatbotEditorWindow
   - System optionally adds scene context from SceneAnalysisIntegration
   - Query is sent to AI service via SendQueryToOpenAIStreaming/SendQueryToClaudeStreaming
   - Response is streamed back and displayed incrementally
   - Code blocks are extracted via ProcessCodeBlocksInMessage

2. **Code Application Flow:**
   - System extracts file path and code content through ProcessAndApplyCodeEdits
   - ApplyEditToFile applies or creates the code edits for a Unity file  
   - Unity's AssetDatabase is refreshed to recognize changes
   - Success/error message is displayed to user
  
3. **Context:**
   - @Context button implements hierarchical context management that traverses the Unity scene graph and project asset database
   - Captures code files, GameObject properties, component configurations, and inheritance hierarchies
   - Serializes scene structure and code into a compact JSON representation to add to user queries
   - Maintains persistent file references across editor sessions, with async I/O operations
   - Quick Context toggle injects current scene and code data into prompts
     
5. **Scene Analysis Flow:**
   - User requests scene analysis via menu
   - System calls appropriate SceneAnalysisIntegration methods
   - Analysis results are formatted as markdown
   - Results are added to chat history and displayed

## Extension Points

### 1. Adding New AI Models

To add a new AI model:

1. Add the model to the `availableModels` list in `ChatbotEditorWindow.cs`
2. If using a new provider, implement a new sending method similar to `SendQueryToOpenAIStreaming()`
3. Update the model selector dropdown to include the new model
4. Implement appropriate token counting for the new model

**Implementation Example:**
```csharp
// Add new model to the list
availableModels.Add(new ModelInfo { Name = "new-model-name", Provider = "NewProvider" });

// Update dropdown
modelSelector.choices = availableModels.Select(m => m.Name + " (" + m.Provider + ")").ToList();

// Check provider in send method
if (selectedModel.Provider == "NewProvider") {
    SendQueryToNewProviderStreaming(userMessage, selectedModel.Name);
}
```

### 2. Adding New Scene Analysis Features

To add new scene analysis capabilities:

1. Add a new analysis method to `SceneAnalysisIntegration.cs`
2. Implement the analysis logic using Unity's scene querying APIs
3. Format the results as markdown for display
4. Add a UI option in the scene analysis context menu
5. Consider caching for performance optimization

**Implementation Example:**
```csharp
// Cache variables
private static string cachedPhysicsAnalysis;
private static DateTime lastPhysicsAnalysisTime;

// Analysis method
public static string GetPhysicsAnalysis() {
    // Check cache first
    if (cachedPhysicsAnalysis != null && 
        (DateTime.Now - lastPhysicsAnalysisTime) < CacheTimeout) {
        return cachedPhysicsAnalysis;
    }
    
    // Analysis implementation
    var rigidbodies = GameObject.FindObjectsOfType<Rigidbody>();
    // Process and format results...
    
    // Cache results
    cachedPhysicsAnalysis = result;
    lastPhysicsAnalysisTime = DateTime.Now;
    return result;
}

// UI menu option
menu.AddItem(new GUIContent("Physics Analysis"), false, () => {
    string result = SceneAnalysisIntegration.GetPhysicsAnalysis();
    AddMessageToHistory("System", result);
});
```

## Performance Considerations
1. **Scene Analysis Optimization**
   - Scene analysis results are cached with a 30-second timeout
   - Large scenes use selective analysis to avoid performance issues
   - Hierarchy traversal is optimized to minimize GameObject.Find calls

3. **Token Management**
   - The system implements token counting to stay within API limits
   - FIFO strategy removes the oldest messages first when the context window is full
   - Full_Reset strategy clears all history except the system prompt when the context is full
   - Token counting uses the TikToken library for accurate estimates

4. **UI Performance**
   - Markdown rendering is optimized for common patterns
   - Long responses are streamed to avoid UI freezing
   - EditorPrefs are used efficiently to minimize serialization overhead

## Security Considerations

1. **API Key Storage**
   - API keys are encrypted using AES before storage in EditorPrefs
   - Keys are never exposed in the UI or logs
   - The encryption implementation provides basic protection
   - Keys can be revoked and replaced easily through the UI

2. **File System Access**
   - The system validates file paths before reading/writing
   - File operations are wrapped in try/catch blocks for error handling

## Debugging Tips
1. **API Communication Issues**
   - Verify API keys are correctly configured
   - Check for network connectivity issues
   - Examine request/response headers for API-specific errors

2. **Code Application Problems**
   - Check file paths for correctness (case sensitivity matters)
   - Verify file permissions allow writing to the target location
   - Check Unity console for compilation errors after code application

3. **Scene Analysis Errors**
   - Use `SceneAnalysisIntegration.Cleanup()` to clear cached results
   - Check for null references in scene objects
   - Verify scene is loaded and saved before analysis
   - Look for missing components that might cause analysis failures
