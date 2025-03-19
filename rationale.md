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
