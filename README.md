# XeleR : Accelerating XR Prototyping

## Overview

This Unity Editor extension is a core component of the XeleR platform—a Text2XR solution designed to transform the XR app development ecosystem. XeleR aims to provide rapid, AI-assisted XR prototyping that lowers development barriers, accelerates iteration cycles, and ensures cross-platform compatibility. This project delivers a real-time chatbot window integrated into the Unity Editor, enabling developers to receive streaming AI responses, automated code suggestions, scene analysis, and file browsing capabilities.

## Demo Video
Demo Video of how to use XeleR to rapidly iterate on your screen. Here you can see how we add a new character onto the kitchen scene.
[![Watch the video](https://img.youtube.com/vi/Zdj6ES_ETMg/0.jpg)](https://youtu.be/Zdj6ES_ETMg)


## Features

- **Real-Time Chat Interface:**  
  Provides a dockable chat window that streams AI responses in real time using Unity's UIElements.

- **AI API Integration:**  
  Communicates with OpenAI and Anthropic Claude APIs for code assistance, debugging, and context-aware suggestions.

- **Code Assistance & Editing:**  
  Detects and processes specially formatted code blocks in AI responses to automatically apply code changes to project files.

- **Scene Analysis & Debugging:**  
  Extracts scene context and applies modifications via reflection, helping developers quickly iterate on scene design and interactions.

- **File & Scene Browsing:**  
  Integrates file browsing functionality to load scripts or scenes, incorporating them into AI queries for context-aware responses.

- **Session Management:**  
  Manages multi-session chat histories by serializing conversation data with EditorPrefs, ensuring persistence across editor restarts and recompilations.

- **Streaming Responses:**  
  Utilizes streaming download handlers to progressively update the chat window as API responses arrive.

## Technical Details

- **Editor Extension:**  
  Built with Unity’s UIElements for a modern, flexible UI and seamless integration into the Unity Editor environment.

- **Networking:**  
  Uses `UnityWebRequest` for HTTP communication with AI APIs, supporting both standard and streaming response modes.

- **JSON Parsing:**  
  Leverages Unity’s `JsonUtility` for efficient parsing of API responses, including handling of streaming JSON chunks.

- **Dynamic Code & Scene Updates:**  
  Implements reflection to parse and apply code or scene changes from AI suggestions, allowing for partial or full file updates.

- **Persistent State Management:**  
  Chat sessions and conversation histories are stored in EditorPrefs, enabling smooth restoration of sessions after script recompilations.

## Usage

1. **Installation:**  
   Place the script files in an `Editor` folder within your Unity project.

2. **API Configuration:**  
   Open the chat window via **Window → Chatbox** and configure your API keys using the API Settings button.

3. **Interaction:**  
   - Type queries into the input field and send them to receive real-time, streaming responses.
   - Use the context menu to browse and load script files or scenes to include additional context in your queries.
   - View AI-generated code suggestions and scene modifications directly within the editor.

4. **Session Management:**  
   Switch between multiple chat sessions, save conversation histories, and restore previous states seamlessly.

## XeleR Product Context

**XeleR PRD - Version 0.1**

XeleR is designed to be the platform XR developers turn to for rapidly building XR products. By integrating AI-driven prototyping, XeleR aims to:

- **Accelerate Prototyping:**  
  Dramatically reduce the time and cost required to build and iterate XR prototypes, enabling rapid user feedback without significant hardware investments.

- **Lower Development Barriers:**  
  Provide a unified, cross-platform development environment that simplifies the complexities of fragmented XR SDKs and APIs.

- **Enhance Developer Experience:**  
  Utilize AI-powered debugging, asset discovery, and test case generation to streamline development and improve the overall quality of XR applications.

**Key Benefits:**

- **Text-to-XR Prototyping:**  
  Transform plain-text descriptions into interactive XR prototypes, making early-stage development faster and more accessible.

- **AI-Powered Debugging & Automation:**  
  Automate debugging and testing processes, reducing manual effort and accelerating iteration cycles.

- **Cross-Platform Compatibility:**  
  Develop once and deploy across multiple XR devices seamlessly, ensuring broad reach and flexibility.

## Contributing

Contributions are welcome! Please follow the standard GitHub contribution guidelines and ensure that any pull requests are thoroughly documented.

## License

This project is licensed under the MIT License.
