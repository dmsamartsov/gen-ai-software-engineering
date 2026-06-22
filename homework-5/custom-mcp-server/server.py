"""Custom MCP server (Homework 5, Task 4) built with FastMCP.

This server exposes the contents of ``lorem-ipsum.md`` in two complementary ways:

* as an MCP **Resource** — a URI Claude can *read* from, and
* as an MCP **Tool** named ``read`` — an action Claude can *call*.

Both return a word-limited slice of the file: the first ``word_count`` words,
defaulting to 30 when no count is supplied.
"""

from pathlib import Path

from fastmcp import FastMCP

# Default number of words returned when the caller does not specify one.
DEFAULT_WORD_COUNT = 30

# Resolve the source file relative to this script so the server works no matter
# which working directory the MCP client launches it from.
LOREM_FILE = Path(__file__).parent / "lorem-ipsum.md"

mcp = FastMCP("custom-lorem-server")


def _read_words(word_count: int = DEFAULT_WORD_COUNT) -> str:
    """Return the first ``word_count`` words from ``lorem-ipsum.md``.

    The count is clamped to ``[0, total_words]`` so out-of-range requests
    return an empty string (<= 0) or the whole file (> available) instead of
    raising an error.
    """
    text = LOREM_FILE.read_text(encoding="utf-8")
    words = text.split()
    count = max(0, min(word_count, len(words)))
    return " ".join(words[:count])


@mcp.resource("lorem://lorem-ipsum")
def lorem_default() -> str:
    """Resource URI returning the default number of words (30)."""
    return _read_words(DEFAULT_WORD_COUNT)


@mcp.resource("lorem://lorem-ipsum/{word_count}")
def lorem_words(word_count: int) -> str:
    """Resource template returning exactly ``word_count`` words from the file.

    Example URI: ``lorem://lorem-ipsum/10`` returns the first 10 words.
    """
    return _read_words(word_count)


@mcp.tool()
def read(word_count: int = DEFAULT_WORD_COUNT) -> str:
    """Read ``word_count`` words (default 30) from ``lorem-ipsum.md``.

    This is the action Claude calls to pull the resource content on demand.
    """
    return _read_words(word_count)


if __name__ == "__main__":
    # Runs over stdio by default — the transport MCP clients expect.
    mcp.run()
