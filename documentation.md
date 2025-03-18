# XeleR Codebase Documentation

## Overview

XeleR is a Unity Editor extension that provides AI-assisted XR prototyping capabilities. This document provides a technical overview of the codebase structure, implementation details, and key components to help new developers understand and contribute to the project. If you are looking for a more product-focused overview, please refer to the [README](README.md).

## Technical Architecture

XeleR follows a modular architecture with clear separation between:
- Editor UI components (Unity Editor integration)
- AI communication services
- Scene analysis utilities

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
- Implements a caching system with configurable timeout to improve performance
- Uses reflection to extract component properties dynamically
- Performs raycasting for spatial relationship detection
- Captures scene screenshots for visual context

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

### 2. AI Communication

#### API Integration
The system communicates with multiple AI providers through REST APIs.

**OpenAI Integration:**
- Supports models: gpt-3.5-turbo, gpt-4, gpt-4-turbo, gpt-4o
- Implements streaming API for real-time responses
- Handles token counting and context management
- Processes chunked responses for incremental UI updates

**Claude Integration:**
- Supports models: claude-3-opus, claude-3-5-sonnet, claude-3-7-sonnet
- Uses Anthropic's API with appropriate headers and authentication
- Implements streaming response handling
- Processes event-stream formatted responses

**Technical Implementation:**
- Uses UnityWebRequest for HTTP communication
- Implements proper header management for authentication
- Handles streaming responses with chunked processing
- Manages connection timeouts and error handling
- Implements backoff strategies for rate limiting

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

#### PlannerGPT.cs
Extends ChatBot to provide planning capabilities for Unity scenes.

**Key Functions:**
- `LoadPlanningPrompt()`: Loads specialized prompts for scene planning
- `ProcessUserInput()`: Handles user input and maintains conversation
- `StreamGPT4Response()`: Streams AI responses in real-time
- `CheckForConversationFinished()`: Detects conversation completion
- `UpdateHistoryUI()`: Updates UI with conversation history

**Technical Implementation:**
- Loads planning-specific prompts from external files
- Maintains conversation context across multiple exchanges
- Triggers scene processing when conversation is complete
- Updates UI in real-time with streaming responses

## Data Flow

1. **User Input Flow:**
   - User enters query in ChatbotEditorWindow
   - System optionally adds scene context from SceneAnalysisIntegration
   - Query is sent to AI service via SendQueryToOpenAIStreaming/SendQueryToClaudeStreaming
   - Response is streamed back and displayed incrementally
   - Code blocks are extracted via ProcessCodeBlocksInMessage
   - Apply buttons are added for each code block

2. **Code Application Flow:**
   - User clicks Apply button for a code block
   - System extracts file path and code content
   - ApplyCodeEdit validates file path and handles file I/O
   - Unity's AssetDatabase is refreshed to recognize changes
   - Success/error message is displayed to user

3. **Scene Analysis Flow:**
   - User requests scene analysis via context menu
   - System calls appropriate SceneAnalysisIntegration methods
   - Analysis results are formatted as markdown
   - Results are added to chat history and displayed
   - Results may be included in subsequent AI queries

## Extension Points

### 1. Adding New AI Models

To add a new AI model:

1. Add the model to the `availableModels` list in `ChatbotEditorWindow.cs`
2. If using a new provider, implement a new sending method similar to `SendQueryToOpenAIStreaming()`
3. Update the model selector dropdown to include the new model
4. Implement appropriate token counting for the new model

### 2. Adding New Scene Analysis Features

To add new scene analysis capabilities:

1. Add a new analysis method to `SceneAnalysisIntegration.cs`
2. Implement the analysis logic using Unity's scene querying APIs
3. Format the results as markdown for display
4. Add a UI option in the scene analysis context menu
5. Consider caching for performance optimization

## Performance Considerations

1. **Scene Analysis Optimization**
   - Scene analysis results are cached with a 30-second timeout
   - Large scenes use selective analysis to avoid performance issues
   - Hierarchy traversal is optimized to minimize GameObject.Find calls
   - Screenshot capture is only performed when specifically requested

2. **Token Management**
   - The system implements token counting to stay within API limits
   - FIFO strategy removes oldest messages first when context window is full
   - Full_Reset strategy clears all history except system prompt when context is full
   - Token counting uses the TikToken library for accurate estimates

3. **UI Performance**
   - The chat UI uses virtualization for large message histories
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
   - Directory traversal attacks are prevented by path normalization
   - File operations are wrapped in try/catch blocks for error handling
   - User confirmation is required before overwriting existing files

## Debugging Tips

1. **API Communication Issues**
   - Check Unity console for HTTP status codes and error messages
   - Verify API keys are correctly configured
   - Check for network connectivity issues
   - Examine request/response headers for API-specific errors

2. **Code Application Problems**
   - Check file paths for correctness (case sensitivity matters on some platforms)
   - Verify file permissions allow writing to the target location
   - Look for syntax errors in the generated code
   - Check Unity console for compilation errors after code application

3. **Scene Analysis Errors**
   - Use `SceneAnalysisIntegration.Cleanup()` to clear cached results
   - Check for null references in scene objects
   - Verify scene is loaded and saved before analysis
   - Look for missing components that might cause analysis failures