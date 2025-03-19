# Rationale Documentation

## Overview

This document outlines the rationale behind the decisions that our team made while building XeleR. 

## Phase 1: Need-finding 

AI-assisted development for Unity was an idea that we intuitively came up with as members of our team had first-hand experience dealing with the frustrations of XR development. Due to the inspiration arising from dissatisfaction with the XR development workflow, we wanted to focus on building XeleR for an XR development use-case (as opposed to other applications of Unity). We started off our need-finding process by curating a post on LinkedIn with a linked Google Form. The Google Form helped us collect important information about the XR development space and what XR development looks like for creators from different backgrounds and of different skill levels. From the 48 responses, we conducted 6 depth interviews, which dove deeply into understanding the different types of needs in the process. From this need-finding process, we were able to recognize four essential problems: 

**1. AI-Assisted Development is Limited:** An interviewee stated that "ChatGPT only works 50% of the time" for them, which demonstrates the lack in utility of general solutions like ChatGPT for XR development. The Unity environment is much more complex than simply writing code: there is a lot more information that needs to be provided to understand the context of the scene like object sizes, positions, and orientations. 

**2. Version Control and Debugging:** XR software is rapidly developing, which makes it difficult to always integrate latest updates in the software. The software breaks often aswell. 

**3. Iteration and Testing:** Iterating and testing takes a lot of time in the XR development process by virtue of how the current workflow and tools are structured (eg. having to restart app/device for testing). 

**4. Cross-platform Development:** Developers are constrained to develop for a headset in mind. It took one interviewee 6 months to move a project from Quest to AVP, which highlights how slow the cross-platform development process in XR is.  

## Phase 2: Prototype Development

Our prototype was based on WebXR and generated Three.js code. The goal that we set out building the prototype with was to get the minimum functionality of being able to create and interact with objects work. To our surprise, the prototype proved the concept of our product even without the context that the Unity API provides our current model. The main feedback that we got on our prototype was building the functionality into Unity so that developers would not have to interact with a different tab and interrupt their workflow to use our product. At this point, we were considering building a Unity extension and forking VS Code as potential approaches to building out our product. After discussing the feedback among us and encountering multiple obstacles when trying out the VS Code fork route, we decided to pivot to a Unity extension for our product. 

## Phase 3: Functional Product Development: 

A major decision that we had to make was how the architecture for our product was going to be built. Initially, we considered an approach proposed by "LLMR: Real-time Prompting of Interactive Worlds using Large Language Models" (De La Torre et al., 2023). This framework facilitates the real-time creation and modification of interactive 3D environments using Large Language Models (LLMs) and the Unity game engine. At its core, LLMR orchestrates several specialized modules that work in tandem to create and edit immersive virtual worlds:

**1. Planner GPT:** This module breaks down high-level user prompts into manageable sub-tasks, guiding the framework through a structured approach to problem-solving.

**2. Scene Analyzer GPT:** It provides a semantic summary of the current scene, helping the system understand and manipulate the existing environment efficiently.

**3. Skill Library GPT:** Retrieves relevant skills for tasks, allowing the Builder GPT to generate necessary code based on user requests.

**4. Builder GPT:** This is the core code-generation module, which creates Unity C# scripts for 3D object and scene creation.

**5. Inspector GPT:** Ensures the quality of generated code by performing checks for errors before it is executed, preventing compilation and runtime failures.

This setup significantly reduces error rates in scene creation compared to using a general-purpose LLM like GPT-4. The framework also features real-time execution in Unity, allowing for the dynamic generation and modification of scenes, which can be saved and reloaded. All these factors made the approach an attractive solution; however, after attempting to build it out, we were running into several hurdles like the Unity project only running on Alienware. 

Therefore, we decided to switch to a much more simpler approach, which the current XeleR model describes as per the README.md and documentation.md documents. 
