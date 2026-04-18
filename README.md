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
| `sharpbot setup` | Interactive first-run wizard. |
| `sharpbot models list` | Show curated + installed GGUF models. |
| `sharpbot models download [name]` | Download a model; prompts interactively if no name. |
| `sharpbot hf login` | Save a HuggingFace token (needed for gated models — see below). |
| `sharpbot hf logout` | Remove the saved token. |
| `sharpbot hf status` | Show whether a token is saved. |
| `sharpbot pair` | Pair a Telegram user — first message to the bot wins. |
| `sharpbot doctor` | Sanity-check config, model file, tokens, MCP servers. |

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

## Requirements

- .NET 10 SDK ([download](https://dotnet.microsoft.com/download))
- ~2 GB free RAM per active model (Qwen 2.5 3B Q4_K_M uses ~3 GB)
- ~2–5 GB disk per model

## License

MIT. See [LICENSE](LICENSE).
