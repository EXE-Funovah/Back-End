---
description: |
  Use this skill when finishing a substantial Mascoteach feature that adds or changes a database schema,
  API/module, external integration, or business flow across multiple layers. Triggers: "feature complete",
  "finish feature", "large feature", "major feature", "update codex", "handoff", "tong hop feature".
---

# Mascoteach - Feature Completion Skill

## When this rule applies

Apply this rule after a substantial feature, such as:

- adding or changing database tables, columns, constraints, or indexes;
- adding an API module or a multi-layer business flow;
- changing contracts shared with frontend, mobile, AI, or an external service;
- changing authentication, ownership, soft-delete, deployment, or configuration behavior.

Do not require a documentation update for a typo, comment-only change, formatting change, or another trivial edit.

## Before implementation

1. Read the relevant files in `.codex/skills/` before changing code.
2. Treat `.codex` as the latest project rules, while the user's current request remains the highest priority.
3. Follow the backend dependency direction: `API -> Service -> Data`.
4. Check DB-first schema impact, authorization and ownership, soft-delete behavior, configuration and deployment,
   API contracts, and required tests.
5. State the intended implementation clearly before starting a substantial change.

## After implementation

1. Update the existing domain rule in `.codex/skills/`, or create `mascoteach-<domain>.md` when no suitable rule exists.
2. Record only durable, verified context: current architecture, schema, exact API routes and values, business
   invariants, ownership and soft-delete rules, rollout state, test commands, and frontend/AI contracts.
3. Update cross-cutting rules such as deployment, debugging, authentication, storage, or payment only when the
   feature actually changes them.
4. Remove or correct stale statements that conflict with the current code or schema.
5. Update `SUMMARY.md` when the feature materially changes project-wide status or team handoff information.
6. Never store passwords, tokens, connection strings, private keys, or other secrets in `.codex` or `SUMMARY.md`.
7. Updating documentation does not authorize Git operations. Never commit, push, open a PR, reset, merge, or
   rewrite history unless the user explicitly requests that exact action.

## Rule file structure

Keep project rules consistent with the current `.codex/skills/` structure:

- place the file directly under `.codex/skills/`;
- use YAML frontmatter with a concrete `description: |` and recognizable triggers;
- use the heading `# Mascoteach - <Domain> Skill`;
- describe the current shape, required rules or workflow, validation, and common mistakes;
- write concise instructions based on the current code and database, not a chronological work diary.

## Completion checklist

Before reporting a substantial feature as complete, verify that:

- code follows the existing `API -> Service -> Data` architecture;
- relevant tests and build have been run with fresh results;
- development and production database rollout states are stated accurately;
- API/DTO contracts, ownership, and soft-delete behavior are documented when applicable;
- relevant `.codex` rules have been updated;
- `SUMMARY.md` has been updated when the change affects project handoff;
- no unauthorized Git write operation was performed.

## Common mistakes

- Do not copy temporary implementation notes into permanent rules.
- Do not claim production schema rollout based only on development database results.
- Do not create duplicate rule files when an existing domain rule can be updated.
- Do not treat passing tests as proof that documentation and deployment state are current.
- Do not automatically commit documentation updates.
