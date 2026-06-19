---
date: 2026-05-26
description: "Claude Code adopted for AI-assisted development, GitHub repository initialized with context engineering infrastructure"
---
# Key decisions

- **Claude Code adopted** as the AI-assisted development tool for the project.
- **GitHub repository initialized** with context engineering infrastructure.
  - Repo: https://github.com/nicolasgrosfort/head-md-care
  - Context files live in `process/robot/` — structured prompts and agent context to keep Claude Code aligned with the project's design intent across sessions.

# Key ideas

- **Context engineering as project practice:** Rather than relying on conversational memory, the team is building a structured context layer (this `Agents.md` file, logs, etc.) so that any AI-assisted session starts with full project awareness.

# Status

- Infrastructure in place. Development sessions going forward can be started from the GitHub repo with consistent context.
