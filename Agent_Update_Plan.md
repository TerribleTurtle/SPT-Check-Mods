# Agent Update Plan: The 2026 Disk Brain Architecture

This document serves as the comprehensive execution guide and architectural manifesto for upgrading the AI agent orchestration layer to the industry-standard 2026 "Disk Brain" architecture. It incorporates Manus-style File-Based Planning, Context Engineering, and Anchored Iterative Summarization.

---

## Part 1: Architectural Theory & Protocols
*Why are we doing this, and what are the new rules of engagement?*

### 1. The Core Paradigm: RAM vs. Disk
Context windows are treated as **RAM** (volatile, easily bloated, and quickly degraded). The local filesystem is treated as **Disk** (persistent, unlimited, and shared). AI agents must push their thoughts, plans, and rules to Disk to prevent amnesia across sessions or sub-agent handoffs.

### 2. The CRTSE Framework
All agents will be governed by a strict prompt architecture:
- **[C] Context:** The environmental setup (Disk Brain paradigm).
- **[R] Role:** The persona and scope of authority.
- **[T] Task:** The specific bounded objective.
- **[S] Standards:** Strict rules of engagement.
- **[E] Examples:** Reference patterns for input/output.

### 3. Anchored Iterative Summarization (AIS)
To prevent `findings.md` from bloating into a 200-line chronological diary, agents must use AIS. 
- **The AIS Execution Prompt:** When a file gets too long, an agent does not just "summarize" it. The agent runs a specific sub-task with the following prompt:
  > "You are an expert context manager. You have an existing Session Anchor and a set of new conversational interactions. Read the current Session Anchor (`findings.md`). Read the new raw logs. Merge the new insights into the Anchor's structured headers without deleting old architectural rules."
- **Explicit Checklists:** The file uses strict markdown headers to force categorization (e.g., Global Invariants vs. Architectural Decisions).

### 4. Behavioral Protocols
- **The 2-Action Rule:** After every two view/browser/search operations, agents MUST save key findings to text files to prevent context/multimodal loss.
- **The 3-Strike Error Protocol:** 
  - **Attempt 1:** Diagnose & Fix.
  - **Attempt 2:** Alternative Approach (never repeat the exact same failing action).
  - **Attempt 3:** Broader Rethink. 
  - **After 3 failures:** Escalate to User.
- **The 5-Question Reboot Test:** Found in `progress.md`. If an agent can answer these 5 questions by reading the memory files, the context is solid.

### 5. Swarm Concurrency (File Locking)
When multiple sub-agents are spawned simultaneously, they risk overwriting each other's memory. To solve this in 2026:
- **Git Worktree Isolation:** Parallel sub-agents must be spawned using the `using-git-worktrees` skill (Workspace: `branch`). This namespaces their work.
- **Atomic Merging:** Sub-agents must not directly write to the master `findings.md` while in parallel. The Lead Orchestrator reviews their output and runs the AIS merge manually into the master branch.

---

## Part 2: Phased Execution Plan
*How we will establish this system globally and locally.*

### Stage 1: Establish the Global Architecture (`AGENTS.md`)
*Objective: Guarantee that any agent spawned on this machine natively inherits the Disk Brain paradigm.*

- **Target:** `C:\Users\evanw\.gemini\config\AGENTS.md` (Global Customization Root).
- **Action:** Write the global CRTSE template. 

**[Global Template to be Written]**
```markdown
# Global Memory & Context Engineering Standards

## [CONTEXT]
You are operating under the 2026 Context Engineering paradigm, interacting with a "Disk Brain" (persistent file-based memory) to prevent context window degradation. You share memory with all other sub-agents via three files in the workspace root.

## [ROLE]
You are an advanced AI coding assistant and Lead Orchestrator. Your primary role is to manage and maintain the project's memory system without relying on ephemeral context.

## [TASK]
You must actively maintain the 3-file memory system (`task_plan.md`, `findings.md`, `progress.md`), track active work, and preserve architectural rules across sessions.

## [STANDARDS: The 3-File Memory System]
1. **`task_plan.md` (The Blueprint):** 
   - Must be created before any complex multi-step task.
   - Use checklists (`[ ]`, `[x]`) to track explicit phases. 

2. **`findings.md` (Persistent File-Based Memory):**
   - The authoritative rulebook for the project's architecture.
   - **CRITICAL:** Do NOT treat this as a chronological changelog. Use *Anchored Iterative Summarization* to condense discoveries into structured, domain-specific sections. Overwrite outdated facts.

3. **`progress.md` (The Ephemeral Scratchpad):**
   - The working set for the *current active session only*.
   - Wipe this file clean when transitioning to a completely new Stage.

## [EXECUTION: Protocols & Concurrency]
- **Bootstrapping (Auto-Init/Auto-Update):** On invocation, read the Core Triad. If it is missing (New Project), create it. If it is bloated with logs (Old Project), compress it using AIS.
- **Swarm Concurrency:** Parallel sub-agents MUST be spawned into isolated Git Worktrees (Workspace: `branch`). Do not allow parallel sub-agents to blindly overwrite the master `findings.md`.
- **Read Before Decide:** Always read `task_plan.md` and `findings.md` before making architectural decisions.
- **The 2-Action Rule:** After every two view/browser/search operations, you MUST write key findings to `findings.md`.
- **The 3-Strike Error Protocol:** Escalate to the human user after 3 unique failures on the same step.
```

---

### Stage 2: Initialize the Master Blueprint (`task_plan.md`)
*Objective: Create the generic project-agnostic template for tracking active work.*

- **Target:** `task_plan.md` in the project root.

**[Standardized Template Structure]**
```markdown
# Task Plan: [Current Feature Name]

## Goal
[High level description of what the agent is currently building]

## Current Stage
[Stage X]

## Stages
### Stage 1: [Stage Name]
- [ ] [Sub-task 1]
- **Status:** in_progress

### Stage 2: [Stage Name]
- [ ] [Sub-task 1]
- **Status:** pending

## Decisions Made
| Decision | Rationale |
|----------|-----------|
|          |           |

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
|       | 1       |            |
```

---

### Stage 3: Implement Anchored Iterative Summarization (`findings.md`)
*Objective: Establish the permanent, structured rulebook template to replace chronological logging.*

- **Target:** `findings.md` in the project root.
- **Action on Existing Projects:** Delete all chronological "Session Conclusion" logs. Extract only the hard, permanent rules.

**[Standardized Template Structure]**
```markdown
# Swarm Memory: findings.md

## 1. Global Invariants (Anchored)
*Hard rules discovered during execution. Merge redundant items. Remove obsolete ones.*
- [List of permanent architectural rules, e.g., error handling standards]

## 2. Architectural Decisions (Iterative Merge)
*Record the "Why" and permanent shifts, discarding the trial-and-error.*
- [List of major pivots and why they were made]

## 3. Artifact & State Trail (Explicit Checklist)
*Only track currently active files. Remove deleted/irrelevant files.*
- [List of currently active files being modified]

## 4. Handoffs & Next Steps (Transient)
*MUST BE COMPLETELY OVERWRITTEN by the departing agent. NEVER APPEND.*
- [Next step directive]
```

---

### Stage 4: Implement Ephemeral Session Tracking (`progress.md`)
*Objective: Reset the progress file to act strictly as a volatile session scratchpad.*

- **Target:** `progress.md` in the project root.

**[Standardized Template Structure]**
```markdown
# Progress Log

## Session: [Current Date/Task]
- **Status:** [Current Status]

## Error Log
| Timestamp | Error | Attempt | Resolution |
|-----------|-------|---------|------------|
|           |       | 1       |            |

## 5-Question Reboot Check
| Question | Answer |
|----------|--------|
| Where am I? | [Current step] |
| Where am I going? | [Remaining stages] |
| What's the goal? | [Overall goal] |
| What have I learned? | See findings.md |
| What have I done? | See above |
```

---

### Stage 5: Sub-Agent Swarm Test
*Objective: Prove the new architecture autonomously governs sub-agents without relying on project-specific prompts.*

- **Deployment:** Invoke a generic sub-agent using `invoke_subagent`.
- **Verification Task:** The sub-agent's prompt will simply be: *"Read the project's memory and report back what we are doing."*
- **Success Criteria:** The sub-agent automatically reads the 3 files established by the global `AGENTS.md`, utilizes the 5-Question Reboot Check, and correctly identifies the active state of the repository.
