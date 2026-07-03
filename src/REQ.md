D

Role Assignment:
Act as a Senior Principal Software Engineer and System Architect. Your task is to read the following Software Requirements Specification (SRS) and implement the complete codebase for a production-ready Desktop Application.

SOFTWARE REQUIREMENTS SPECIFICATION (SRS)
Project Name: SortingVisualizerSolution
Tech Stack: C# (.NET Framework 4.8.1 WinForms) for UI & C++ (Dynamic Link Library) for Core Logic.

1. INTRODUCTION & SCOPE
The objective is to build a high-performance desktop application that visualizes sorting algorithms in real-time. The heavy lifting (sorting algorithms) must be separated into an unmanaged C++ DLL for maximum performance, while the presentation layer is managed by a C# WinForms application.

2. STRICT TECHNICAL CONSTRAINTS
Linguistic Rules: ALL code structures (variables, functions, classes, config files) MUST be written in English. ALL in-line code comments MUST be written in Vietnamese (Ti?ng Vi?t) to explain the logic.

Completeness: Provide complete, compilable code. Do NOT use placeholders like // implementation here. If you reach the output token limit, pause and prompt me to type "Continue".

Threading & UI Safety: The C++ DLL functions MUST be called from C# on a background thread (e.g., Task.Run) to prevent UI freezing. Callbacks from C++ to C# MUST utilize Control.Invoke() for thread-safe UI updates.

Memory & Interop: Use extern "C" __declspec(dllexport) in C++. Define explicit calling conventions (e.g., __stdcall). Keep the C# callback delegate alive using GCHandle or class-level references to prevent Garbage Collection during execution.

3. PROJECT DIRECTORY STRUCTURE
Implement the code assuming this exact file architecture:

Plaintext
/SortingVisualizerSolution
    /SortingLogic_CPP        (C++ DLL Project)
        - SortingAPI.h
        - SortingAlgorithms.cpp
    /SortingApp_CS           (C# .NET 4.8.1 WinForms Project)
        - MainForm.cs
        - InteropHelper.cs   
    /Resources
        /Icons               (flag_vi.png, flag_en.png, flag_jp.png)
        /Backgrounds         (default_bg.jpg)
    /Languages
        - localization.xml   
4. FUNCTIONAL REQUIREMENTS (FR)
FR1: C++ Sorting Engine (SortingLogic_CPP)
Algorithms: Implement exactly 30 algorithms categorized into 5 groups:

Basic: Bubble Sort, Insertion Sort, Selection Sort, Cocktail Shaker Sort, Comb Sort, Gnome Sort, Odd-Even Sort, Shell Sort, Double Selection Sort, Cycle Sort, Pancake Sort, Exchange Sort, Binary Insertion Sort

Divide & Conquer: Merge Sort, Quick Sort, Heap Sort, Bitonic Sort, Stooge Sort, Tournament Sort

Distribution: Radix Sort, Counting Sort, Bucket Sort, Pigeonhole Sort, Bead Sort, Flash Sort

Hybrid: IntroSort, TimSort

Advanced: Tree Sort, Patience Sort, Strand Sort

Callback Mechanism: Define a function pointer typedef void (__stdcall *UpdateCallback)(const int* arr, int size, int activeIdx1, int activeIdx2);. Trigger this on every significant comparison/swap.

Cancellation Flow: Implement a volatile boolean pointer or by-reference flag passed from C# so the C++ loops can be safely aborted mid-execution.

FR2: Multi-Language Support
Create a single localization.xml file containing key-value pairs for Vietnamese, English, and Japanese.

Implement a ComboBox (Top-Right UI) with custom DrawItem logic to display flag_vi.png, flag_en.png, and flag_jp.png next to the language names.

Dynamically reload all UI text strings when the language changes.

FR3: User Controls & Input (Region 1)
Data Entry: A TextBox for comma-separated integer input. On submit, dynamically generate Button controls in the Visualization Area.

Execution Control: A "Start" button. When clicked: Text changes to "Stop", Color changes to Red, and clicking it again halts the sorting via the C++ cancellation flag.

Speed Control: A NumericUpDown to adjust the delay (in milliseconds) applied to the visualization callbacks.

Algorithm Selection: Two cascading ComboBoxes. ComboBox 1 selects the Category (from FR1). ComboBox 2 dynamically populates with specific algorithms based on ComboBox 1.

Pseudo-code Viewer: A button in Region 1 opens a child window showing readable pseudo-code for the currently selected algorithm.

FR4: Visualization Area (Region 2)
Layout: A container Panel. Support loading a BackgroundImage dynamically. Elements are represented by Button controls.

Interactivity: Clicking an idle Button pauses the system and allows the user to change its integer value via a small prompt.

Dynamic Styling (Callback Driven):

activeIdx1 & activeIdx2 (Moving): Red background.

Idle elements: Blue background.

Swap Animation: Every valid swap between two visualized elements must animate the involved buttons moving between their positions.

Completion Effect: When sorting concludes successfully, asynchronously blink all element buttons 3 times, then settle them into a Green background with Black text.

FR5: Reporting & Export (Region 3)
Output Display: A read-only TextBox showing the final sorted array.

History Display: A read-only multi-line TextBox showing every sorting step as text. The history area must provide a vertical scrollbar when the log grows.

Export Feature: A Button to export a .txt report using the currently selected UI language. The report must include the original input, step-by-step process history, final result, algorithm used, and timestamp.

5. NON-FUNCTIONAL REQUIREMENTS (NFR)
NFR1 (Configurability): Expose UI settings in App.config (MovingColor, IdleColor, CompletedColor, BackgroundImagePath). Fetch these on application load. Add comments on how a user can manually edit this XML file.

NFR2 (Rendering Performance): Use SuspendLayout(), ResumeLayout(), and enable DoubleBuffered on the main form and panels to prevent visual flickering during rapid array updates.

6. DELIVERABLES & EXECUTION PLAN
Please output the source code block by block in the following strict order:

localization.xml (Full multi-language template).

SortingAPI.h & SortingAlgorithms.cpp (C++ Engine).

InteropHelper.cs (C# P/Invoke definitions and Delegate setups).

MainForm.cs (C# UI, threading, and rendering logic).
