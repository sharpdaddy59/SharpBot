# SharpBot

Always-on local AI assistant. One self-contained binary. No Ollama, no LM Studio, no cloud API, no subscription.

- **Fully local.** The LLM runs in-process via [LlamaSharp](https://github.com/SciSharp/LLamaSharp). Nothing phones home.
- **Free.** No subscription fees of any kind.
- **Cross-platform.** Windows and Linux from a single .NET 10 codebase.
- **Appliance-friendly.** Designed for cheap mini-PCs (Ryzen 5 / 16 GB / no GPU).
- **Tool-capable.** Talks to [MCP](https://modelcontextprotocol.io) servers for real work (filesystem, Telegram, whatever else you plug in).

## Quick start

```bash
# 1. Build
dotnet build

# 2. Pick and download a local model (Qwen 2.5 3B is the recommended default)
dotnet run --project src/SharpBot -- setup

# 3. Run the bot
dotnet run --project src/SharpBot -- run
```

On first run, `setup` walks you through model selection and downloads a GGUF to `models/`. Your chosen model path is saved to `data/user-config.json`.

## CLI

| Command | What it does |
| --- | --- |
| `sharpbot run` | Start the bot (default when no command given). |
| `sharpbot chat` | Local chat REPL — talk to the model from the console, with tool use. `--no-tools` disables tool calling. |
| `sharpbot setup` | Interactive first-run wizard. |
| `sharpbot models list` | Show curated + installed GGUF models. |
| `sharpbot models download [name]` | Download a model; prompts interactively if no name. |
| `sharpbot hf login` / `logout` / `status` | Manage HuggingFace token (needed for gated models — see below). |
| `sharpbot tg login` / `logout` / `status` | Manage the Telegram bot token. |
| `sharpbot pair` | Pair a Telegram user — first message to the bot wins. |
| `sharpbot tools list` | List every tool available to the LLM (built-in + MCP), marked by source. |
| `sharpbot tools test name '{"arg":"value"}'` | Invoke any tool directly (debug). |
| `sharpbot mcp list` / `test` | MCP-specific variants for debugging server connectivity. |
| `sharpbot doctor` | Sanity-check config, model file, tokens, MCP servers. |

## Tools

SharpBot exposes tools to the LLM from two sources, aggregated into one flat catalog:

1. **Built-in tools** (C#, in-process, zero dependencies) — shipped with every binary. Use `sharpbot tools list` to see them.
2. **MCP tools** (optional, external processes) — plug in any [Model Context Protocol](https://modelcontextprotocol.io) server for extra capabilities.

### Model compatibility for tool use

All tools work with any model via direct CLI (`sharpbot tools test ...`). When the **LLM** calls tools on your behalf during chat, model choice matters:

| Model | Tool calling |
| --- | --- |
| **Qwen 2.5 (3B, 7B)** | **Recommended.** Qwen is natively trained on the `<tool_call>` format SharpBot uses; tool calls are reliable and well-formed. |
| Gemma 3 | Best-effort. Gemma improvises with markdown ` ```json {...} ``` ` blocks; SharpBot parses these as a fallback. Works, but Gemma often chooses not to call tools, or calls the wrong one. Use Gemma 3 **4B+**; the 1B is too small for reliable tool selection. |
| Llama 3.2 | Best-effort, less reliable than Gemma. Often emits partial/malformed tool calls. Fine for plain chat without tool use. |

TL;DR — keep Qwen 2.5 3B as your default if you want the bot to actually use its tools.

### Built-in tools (always available)

| Name | Purpose |
| --- | --- |
| `core.current_time` | Get the current date/time, optionally in a specific IANA timezone. |
| `core.fetch_url` | GET any http/https URL, returns response body as text. |
| `core.read_file` | Read a text file inside the workspace directory. |
| `core.list_files` | List files/subdirs inside the workspace directory. |

File-system tools are sandboxed to `SharpBot:BuiltInTools:WorkspaceDirectory` (default `./workspace`) — paths escaping that root are rejected. Tune these (fetch size cap, fetch timeout, workspace path) in `appsettings.json`.

### MCP tool servers (optional)

Configure servers under `SharpBot:Mcp:Servers` in `appsettings.json` or `data/user-config.json`:

```json
{
  "SharpBot": {
    "Mcp": {
      "Servers": [
        {
          "Name": "fs",
          "Command": "npx",
          "Args": ["-y", "@modelcontextprotocol/server-filesystem", "./workspace"]
        },
        {
          "Name": "fetch",
          "Command": "uvx",
          "Args": ["mcp-server-fetch"]
        }
      ]
    }
  }
}
```

Each server gets a short `Name` used as a tool-name prefix (tool `read_file` on server `fs` appears to the LLM as `fs.read_file`). This avoids collisions when multiple servers expose similar tools.

**Verify connectivity:**
```bash
sharpbot mcp list           # spawn configured servers, list their tools
sharpbot mcp test fs.read_file '{"path":"./workspace/README.md"}'
```

**Runtime requirements:** MCP is **optional**. SharpBot itself stays zero-dependency — the built-in tools above cover the common cases with no extra runtime. MCP servers are only needed if you want to go beyond that, and they have their own runtime requirements:

- Most official servers are Node packages (`npx -y @modelcontextprotocol/server-*`) → need Node.js installed.
- Python servers use `uvx` → need Python + uv installed.
- Some community servers ship as Go/Rust binaries → no runtime needed.

You install a runtime only for the servers you choose to use.

## Gated models (Gemma, Llama, etc.)

Some model families — **Gemma** (Google) and **Llama** (Meta) — require accepting a license on HuggingFace before you can download them. If you try without this, you'll see a 401 or 404 error.

**Fix:**

1. **Accept the license.** Open the model's HuggingFace page and click **Agree and access**. For example: <https://huggingface.co/bartowski/google_gemma-3-4b-it-GGUF>. HuggingFace remembers this per-account.
2. **Create a Read token.** Go to <https://huggingface.co/settings/tokens> → *Create new token* → type **Read** → copy it.
3. **Save it locally.**
   ```bash
   sharpbot hf login
   # paste the token when prompted
   ```
4. **Retry the download.**
   ```bash
   sharpbot models download
   ```

The token is stored (unencrypted) in `data/secrets.json`, which is gitignored. Use `sharpbot hf logout` to remove it.

**Or just use an open model** — the Qwen 2.5 family (Alibaba) is fully open and works without any login. The default model in `appsettings.json` is Qwen 2.5 3B Instruct for exactly this reason.

## Layout

```
src/SharpBot/
├── Program.cs              — config + DI + CLI dispatch
├── appsettings.json        — defaults (committed)
├── Agent/                  — chat loop, conversation state
├── Llm/                    — ILlmClient + LlamaSharp backend
├── Transport/              — IChatTransport + Telegram
├── Tools/                  — IToolHost + MCP client
├── Commands/               — Spectre CLI commands
├── Setup/                  — model catalog + HF downloader
├── Secrets/                — file-backed secret store
├── Config/                 — options + user-config writer
└── Hosting/                — DI extensions, Spectre type registrar
data/                       — gitignored; secrets, logs, user config
models/                     — gitignored; GGUF files
```

## GPU acceleration (optional)

SharpBot's default build is CPU-only — small binary, runs anywhere, good enough for cheap mini-PC hardware. On a machine with an NVIDIA GPU (RTX-class) a CUDA build is dramatically faster — expect 10–50× speedup for inference.

**Build with CUDA support:**

```bash
dotnet build src/SharpBot -p:IncludeCuda=true -c Release
dotnet run  --project src/SharpBot -p:IncludeCuda=true -- chat
```

Or publish a standalone CUDA binary:
```bash
dotnet publish src/SharpBot -p:IncludeCuda=true -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

The CUDA build adds ~100 MB of native libraries to the binary. All layers auto-offload to GPU (`GpuLayerCount = 99` default). Requires NVIDIA drivers on the host — on a system without drivers the code gracefully falls back to CPU and logs a warning.

**Other backends** (not bundled by default; see `LLamaSharp.Backend.*` on NuGet if you want to hack them in): Vulkan (Intel Arc / AMD / NVIDIA), Metal (Apple Silicon). Contributions welcome.

## Requirements

- .NET 10 SDK ([download](https://dotnet.microsoft.com/download))
- ~2 GB free RAM per active model (Qwen 2.5 3B Q4_K_M uses ~3 GB)
- ~2–5 GB disk per model

## License

MIT. See [LICENSE](LICENSE).
