# Calora Backend

## Memory

This project uses a shared memory system stored in `docs/memory/`. Memory here is committed to the repo and applies to **everyone** working on this codebase.

**Rules:**
- Read the memory index at `docs/memory/MEMORY.md` at the start of every conversation.
- Save new memories as individual `.md` files inside `docs/memory/` with the frontmatter format:
  ```
  ---
  name: <name>
  description: <one-line description>
  type: user | feedback | project | reference
  ---
  ```
- Keep `docs/memory/MEMORY.md` updated as the index (one line per file, under 150 chars).
- Do **not** write memories to the personal path (`~/.claude/projects/.../memory/`).

@docs/memory/MEMORY.md
