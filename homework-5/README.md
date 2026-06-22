# 🔌 Homework 5 — Configure MCP Servers

> **Student Name**: Dmytro Samartsov
> **Date Submitted**: 2026-06-23
> **AI Tools Used**: Claude Code, Claude Opus 4.8

This homework configures three external MCP servers (**GitHub**, **Filesystem**,
**Notion**) and implements one **custom MCP server** with FastMCP that serves
word-limited text from `lorem-ipsum.md`.

---

## 📂 Contents

```
homework-5/
├── README.md                     ← you are here
├── HOWTORUN.md                   ← install / run / connect / test instructions
├── .mcp.json                     ← all four MCP servers registered
├── custom-mcp-server/
│   ├── server.py                 ← custom FastMCP server (Resource + `read` tool)
│   ├── lorem-ipsum.md            ← source text the resource/tool reads from
│   ├── requirements.txt          ← pip dependency list (includes fastmcp)
│   └── pyproject.toml            ← project metadata + fastmcp dependency
└── docs/
    └── screenshots/              ← MCP call result screenshots
```

---

## 🧩 Custom MCP server

The custom server (`custom-mcp-server/server.py`) is named **`custom-lorem-server`**
and exposes the contents of `lorem-ipsum.md` two ways:

| Kind | Name / URI | Behaviour |
|------|------------|-----------|
| **Resource** | `lorem://lorem-ipsum` | Returns the default **30** words |
| **Resource (template)** | `lorem://lorem-ipsum/{word_count}` | Returns exactly `word_count` words |
| **Tool** | `read(word_count=30)` | Returns the first `word_count` words from the file |

The word count is clamped to the file's length, so requesting more words than the
file contains returns the whole file, and `0` (or less) returns an empty string.

### Resources vs. Tools

- **Resources** are URIs that Claude can **read** from — like files or API
  endpoints. Reading a resource is a passive lookup; here `lorem://lorem-ipsum`
  hands Claude the file content.
- **Tools** are actions Claude can **call** to perform an operation — like
  reading a file, querying a database, or running a command. Here the `read`
  tool actively fetches a word-limited slice of `lorem-ipsum.md` on demand,
  taking an optional `word_count` argument.

---

## ▶️ Quick start

```bash
cd custom-mcp-server
uv run --with fastmcp python server.py        # start the server (stdio)
```

Full install / connect / test instructions are in **[HOWTORUN.md](./HOWTORUN.md)**.

---

## 🌐 External MCP servers

`.mcp.json` also registers the three external servers required by the assignment:

| Server | Transport | Notes |
|--------|-----------|-------|
| `github` | stdio (`npx @modelcontextprotocol/server-github`) | needs `GITHUB_PERSONAL_ACCESS_TOKEN` |
| `filesystem` | stdio (`npx @modelcontextprotocol/server-filesystem`) | scoped to this repository path |
| `notion` | http (`https://mcp.notion.com/mcp`) | OAuth on first connect |

Screenshots of each server's MCP call results live in `docs/screenshots/`:

| Screenshot | Server | What it shows |
|------------|--------|---------------|
| [`01-github-mcp-list-repos.png`](./docs/screenshots/01-github-mcp-list-repos.png) | `github` | Listing GitHub repositories |
| [`02-filesystem-mcp-find-docx.png`](./docs/screenshots/02-filesystem-mcp-find-docx.png) | `filesystem` | Finding `.docx` files in a folder |
| [`03-notion-mcp-list-documents-calls.png`](./docs/screenshots/03-notion-mcp-list-documents-calls.png) | `notion` | Querying the "Інформатика" database |
| [`04-notion-mcp-list-documents-results.png`](./docs/screenshots/04-notion-mcp-list-documents-results.png) | `notion` | The resulting documents table |
| [`05-custom-lorem-mcp-read-words.png`](./docs/screenshots/05-custom-lorem-mcp-read-words.png) | `custom-lorem` | Reading 10 words from the custom server |
