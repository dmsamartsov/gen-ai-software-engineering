# 🛠️ HOWTORUN — Custom MCP Server

This guide covers the **custom FastMCP server** (`custom-mcp-server/`):
how to install dependencies, run the server, connect it to an MCP client
(Claude Code), and use/test the `read` tool.

> Requires **Python ≥ 3.10** (FastMCP 2.x / 3.x). The instructions below use
> [`uv`](https://docs.astral.sh/uv/), which provisions a compatible Python and
> the dependencies automatically. A plain `pip` path is also given.

---

## 1. Install dependencies

### Option A — uv (recommended)

```bash
# Install uv once (macOS/Linux):
curl -LsSf https://astral.sh/uv/install.sh | sh

cd custom-mcp-server
uv sync            # creates a venv from pyproject.toml (installs fastmcp)
```

`uv` reads `pyproject.toml`, picks a Python ≥ 3.10, and installs `fastmcp`.
You can also skip `uv sync` and let `uv run --with fastmcp ...` install on the fly.

### Option B — pip + venv

```bash
cd custom-mcp-server
python3.10 -m venv .venv          # or any Python >= 3.10
source .venv/bin/activate
pip install -r requirements.txt   # installs fastmcp>=2.0.0
```

Verify the dependency is present:

```bash
uv pip list | grep fastmcp        # or: pip show fastmcp
```

---

## 2. Run the server

From the `custom-mcp-server/` folder:

```bash
# uv:
uv run --with fastmcp python server.py

# or, inside the activated venv:
python server.py

# or via the FastMCP CLI:
fastmcp run server.py
```

The server starts on **stdio** (the transport MCP clients expect). There is no
HTTP port — it communicates over stdin/stdout, so when launched manually it will
simply wait for a client. Stop it with `Ctrl+C`.

---

## 3. Connect the MCP configuration

### Option A — use the provided `.mcp.json`

`homework-5/.mcp.json` already registers the server as `custom-lorem`:

```json
{
  "mcpServers": {
    "custom-lorem": {
      "command": "uv",
      "args": ["run", "--directory", "custom-mcp-server", "--with", "fastmcp", "python", "server.py"]
    }
  }
}
```

The relative `--directory custom-mcp-server` path resolves from the directory you
launch Claude Code in. Launch Claude Code from **`homework-5/`** so the path
resolves, or copy the `custom-lorem` entry into the project-root `.mcp.json` and
adjust the path to `homework-5/custom-mcp-server`.

Claude Code auto-loads `.mcp.json` from the project root on startup. Run
`/mcp` to confirm `custom-lorem` is connected.

### Option B — register via the Claude Code CLI (most reliable)

```bash
claude mcp add custom-lorem -- \
  uv run --directory /ABS/PATH/TO/homework-5/custom-mcp-server --with fastmcp python server.py
```

Using an absolute path makes the registration independent of your launch
directory. List/verify with:

```bash
claude mcp list
```

---

## 4. Use & test the `read` tool

### A. Quick standalone smoke test (no client needed)

From `custom-mcp-server/`:

```bash
uv run --with fastmcp python - <<'PY'
import asyncio
from fastmcp import Client
from server import mcp

async def main():
    async with Client(mcp) as c:
        print("tools:", [t.name for t in await c.list_tools()])
        r = await c.call_tool("read", {"word_count": 5})
        print("read(5):", r.data)
        d = await c.call_tool("read", {})          # default 30
        print("read() default words:", len(d.data.split()))

asyncio.run(main())
PY
```

Expected output:

```
tools: ['read']
read(5): Lorem ipsum dolor sit amet,
read() default words: 30
```

### B. From Claude Code

Once connected (step 3), prompt Claude:

> "Use the **read** tool from the custom-lorem server to get 10 words."

Claude calls `read(word_count=10)` and returns the first 10 words of
`lorem-ipsum.md`. You can also ask it to read the resource directly:

> "Read the resource `lorem://lorem-ipsum/15` from custom-lorem."

Capture the result as `docs/screenshots/custom-mcp-read-tool-result.png`.

---

## 5. Verification checklist

- [x] Starting command works — `uv run --with fastmcp python server.py`
- [x] MCP config (`.mcp.json`) is valid JSON and points to `server.py`
- [x] `fastmcp` is an explicit dependency in `requirements.txt` **and** `pyproject.toml`
- [x] `read` tool and `lorem://lorem-ipsum` resource return word-limited content
