# Attention & RAG Strategy for Large Codebase Analysis

To effectively manage a "huge" Unity project like Fishing Planet without getting overwhelmed or exceeding context limits, we will simulate an Agentic Attention/RAG mechanism.

## 1. The "Knowledge Map" (Long-Term Memory)
We will maintain a central document (`documents/knowledge_map.md`) that serves as the index of the project. It will contain:
- **Directory Structure Tree**: High-level view of folders.
- **Key Concepts Index**:
    - **Physics**: Links to `*Physics*`, `*Buoyancy*`, `*Rod*` classes.
    - **Fish AI**: Links to `*Fish*`, `*AI*`, `*Behavior*` classes.
    - **Game Loop**: Links to `GameManager`, `Controllers`.
- **Status Tags**: 🔴 (Unexplored), 🟡 (Partially Understood), 🟢 (Fully Documented).

## 2. Attention Mechanics (Context Management)
Instead of reading all files at once, we use a "Zoom-In/Zoom-Out" approach:

### Phase 1: Scanning (Low Attention)
- Use `list_dir` to get file names.
- Use `grep_search` to find "definition" lines (class declarations) without reading bodies.
- **Output**: A "Skeleton" of the architecture.

### Phase 2: Drilling (High Attention)
- When focusing on a module (e.g., Fish AI), we `view_file` relevant scripts.
- **Compass Check**: Before deep diving, we write down *specific questions* we want to answer (e.g., "How does the fish decide to bite?").
- **Summarization**: After reading, we write a *condensed summary* into `knowledge_map.md` and unload the raw code content from context.

## 3. RAG Simulation (Retrieval)
When we need to answer a question or implement a feature:
1. **Query**: Define the problem (e.g., "Change fish swim speed").
2. **Retrieve**: Check `knowledge_map.md` for related classes.
3. **Expand**: Read *only* the interfaces/headers of those classes first.
4. **Target**: Read the implementation of the specific function needed.

## 4. Implementation Plan for "Fishing Planet"
1. **Root Analysis**: List root directories. Identification of `Assembly-CSharp` vs plugins.
2. **Core Systems ID**: Find `GameController`, `FishController`, `PhysicsController`.
3. **Deep Dive - Physics**: Focus on rod/line tension logic.
4. **Deep Dive - AI**: Focus on state machines (FSM) or behavior trees used for fish.
