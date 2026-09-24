# From .NET Engineer to AI-Powered Product Engineer

### A complete course, designed around your CV

Andre — you are not starting from zero. You have the part that takes years to build: production backends in C#/.NET, Azure cloud services, microservices, Docker, messaging (RabbitMQ/Azure Queues), Elasticsearch, ASP.NET Core APIs, and a security/GRC background. The "SWE leveraging AI" role is mostly *your current job plus one new layer*: treating a language model as an unreliable, probabilistic external dependency and engineering reliable software around it.

This course teaches only that new layer. Wherever your existing skills give you a shortcut, the module says so explicitly.

The native path for a .NET developer in 2026 is `Microsoft.Extensions.AI` (the unified provider-agnostic abstraction — `IChatClient`, `IEmbeddingGenerator`) plus the **Microsoft Agent Framework** (GA v1.0, the official successor to Semantic Kernel). Your Azure, dependency-injection, and middleware experience transfers almost directly into both.

---

## How to use this course

Eleven modules, ordered so each builds on the last. Every module has the same shape:

- **Goal** — what you can do after it
- **Concepts** — the new ideas (the genuinely new material)
- **Your shortcut** — where your CV already covers this
- **Build** — a hands-on deliverable; the course is project-driven, not video-driven
- **Resources** — a small, curated set (no link dumps)

Suggested pace: **10–14 weeks part-time** (about 6–8 hours/week). The timeline at the end maps modules to weeks. Do not skip the *Build* steps — reading about LLMs and shipping against them are completely different skills, and only the second one gets you hired.

---

## Module 0 — The mindset shift (½ week)

**Goal:** Internalize why building with AI feels different from everything on your CV so far.

**Concepts.** Every system you have shipped is deterministic: same input, same output, testable with exact assertions. An LLM is the opposite — non-deterministic, occasionally wrong with total confidence ("hallucination"), sensitive to phrasing, and bounded by a context window and a per-call cost. Your job shifts from "make the function correct" to "make the system reliable *despite* an unreliable component." That reframing is the whole role. The patterns you will learn — validation, fallbacks, retrieval, evals, guardrails — all exist to tame that unreliability.

**Your shortcut.** You already do this for *other* unreliable dependencies. A third-party payment API can time out, return garbage, or rate-limit you; you already write retries, circuit breakers, and validation around it. An LLM is the same category of problem with a probabilistic twist. Lean on that instinct.

**Build.** Write one page (for yourself) listing, for a feature you shipped at NETGROUP, what would change if one step were handled by an LLM instead of code. Where would you add validation? What would you do when it returns nonsense?

**Resources.** Anthropic's "Building effective agents" essay; OpenAI's prompt engineering guide intro. Read for mental model, not syntax.

---

## Module 1 — LLM fundamentals, the engineer's subset (½ week)

**Goal:** Know exactly as much about how models work as you need to build with them — and no more. You are not training models (that is the *other* role we compared earlier).

**Concepts.**
- **Tokens** — models read/write in tokens (~¾ of a word in English). Pricing and limits are per-token. You will budget in tokens the way you budget memory.
- **Context window** — the maximum tokens a model can "see" at once (input + output). This is your hard constraint; most engineering problems trace back to it.
- **Temperature / top-p** — knobs controlling randomness. Low for extraction and classification, higher for creative text.
- **System vs. user vs. assistant messages** — the role structure of a chat request. The system message is where you set behavior; treat it like configuration.
- **Model families and tiers** — frontier vs. small/fast/cheap models, and reasoning vs. standard models. Picking the right tier per task is a core cost/latency decision.

**Your shortcut.** Nothing here is mathematically deep. Treat it like learning the request/response contract and rate-limit semantics of a new API — which you have done many times.

**Build.** Use any provider's tokenizer tool to tokenize a few of your own paragraphs. Estimate the cost of sending a 5,000-word document to a model. This number will surprise you and shape every later design decision.

**Resources.** Provider docs for OpenAI, Anthropic, and Azure OpenAI (concepts pages only). Microsoft Learn "Generative AI for Beginners .NET" v2 — Lesson 1.

---

## Module 2 — Your first integration in C# (1 week)

**Goal:** Call an LLM from a real ASP.NET Core service using the .NET-native abstraction, swap providers without changing your code, and stream a response to a UI.

**Concepts.**
- `Microsoft.Extensions.AI` and its central interface `IChatClient`. You register a client in DI exactly like `ILogger` or `IConfiguration`, then inject it. Because it is provider-agnostic, you can point it at Azure OpenAI today and OpenAI or a local model tomorrow with no logic change.
- The request/response message shape, and **streaming** responses token-by-token (so the UI shows text as it generates instead of freezing for ten seconds).
- Configuration and secret handling for API keys (you already do this with Azure).

**Your shortcut.** This is *entirely* in your wheelhouse: ASP.NET Core Web API, DI, Azure config, and a Blazor or Angular front end. The only new thing is the `IChatClient` call itself. If you use Azure OpenAI as the provider, you are not even learning a new cloud — it sits inside the Azure you already operate.

**Build.** A minimal ASP.NET Core Web API endpoint `POST /chat` that takes a prompt, calls an `IChatClient`, and **streams** the answer back. Wire it to a tiny Blazor page with a chat box. Then prove the abstraction: switch the registered provider (e.g., Azure OpenAI ↔ a local model via Ollama) and confirm the endpoint is unchanged. Containerize it with Docker, which you already do daily.

**Resources.** Microsoft Learn: *Microsoft.Extensions.AI libraries* doc (the `IChatClient` quickstart). The `Azure.AI.OpenAI` SDK readme. Microsoft's "Generative AI for Beginners .NET" v2 repo — clone it and run the samples.

---

## Module 3 — Prompt engineering (1 week)

**Goal:** Reliably get the output you want by how you structure instructions — the highest-leverage, lowest-cost skill in the whole role.

**Concepts.**
- Clear instructions, **few-shot examples** (showing 2–3 input/output pairs), role/persona setting, and explicit output-format requests.
- **Chain-of-thought / "think step by step"** for tasks needing reasoning, and when *not* to use it (latency/cost).
- Delimiters and structure (XML-ish tags, headings) to separate instructions from data — which also matters for security (Module 10).
- Treating prompts as **versioned artifacts**: store them in source control, not inline string literals scattered across the codebase. Build a small prompt-loading abstraction.

**Your shortcut.** Your habit of writing maintainable, reviewable code applies directly. Most developers treat prompts as throwaway strings; if you treat them like the configuration/templates you already manage, your systems will be far more maintainable than average.

**Build.** Take one concrete task — say, "summarize a support ticket and extract priority, category, and customer sentiment." Write a prompt, test it on 10 real-ish examples, then iterate the wording until it is consistent. Keep a changelog of what each edit improved. You now have a reusable, version-controlled prompt asset.

**Resources.** Anthropic and OpenAI prompt-engineering guides (read both; they emphasize different things). Skip "prompt hack" listicles.

---

## Module 4 — Structured outputs & tool calling (1 week)

**Goal:** Make a model return data your code can trust (typed JSON), and let it *call your functions* — the bridge from "chatbot" to "feature inside a product."

**Concepts.**
- **Structured / JSON output**: constraining a model to return JSON matching a schema, then deserializing into C# types. This is what turns free text into something a typed language can consume safely.
- **Function calling / tool use**: you describe functions (name, description, parameters); the model decides when to "call" one and returns the arguments; your code executes it and feeds the result back. This is how an AI feature looks up an order, queries your SQL Server, or hits an internal API.
- `Microsoft.Extensions.AI` supports **automatic function invocation** — you annotate methods and the middleware wires the call loop for you.
- Defensive parsing: the model can still return malformed data. Validate every time.

**Your shortcut.** Tool calling is, mechanically, you exposing methods and the model picking which to invoke — conceptually close to controller routing and your existing API design. Your RBAC/claims background matters here too: a tool the model can call should be authorized exactly like any other entry point.

**Build.** Extend your Module 2 service: give the model two tools — `GetOrderStatus(orderId)` and `SearchProducts(query)` — backed by a real (or fake) SQL Server. Ask it natural-language questions ("where's order 4471 and do we have a replacement in stock?") and watch it call your functions and compose an answer. Add JSON-schema-constrained output for the final structured response.

**Resources.** Microsoft Learn: function calling / tool invocation with `Microsoft.Extensions.AI`. Provider docs on structured outputs.

---

## Module 5 — Retrieval-Augmented Generation, RAG (1.5 weeks)

**Goal:** Make a model answer questions about *your* data — documents, knowledge bases, internal wikis — that it was never trained on. RAG is the single most common AI feature in production, and you have an unfair advantage here.

**Concepts.**
- The RAG loop: **chunk** your documents → **embed** each chunk into a vector → **store** the vectors → at query time, embed the question, **retrieve** the most similar chunks, and **stuff** them into the prompt as context so the model answers from real sources.
- **Chunking strategy** (size, overlap, structure-aware splitting) — this quietly determines RAG quality more than the model choice does.
- **Hybrid search**: combining keyword (BM25) and vector (semantic) search for better retrieval than either alone.
- **Grounding & citations**: returning the source chunks so answers are verifiable, which also reduces hallucination.

**Your shortcut — this is your biggest one.** You already run **Elasticsearch** and built indexing/search workflows at NETGROUP. Modern Elasticsearch does dense vector search and hybrid retrieval natively — so the storage and retrieval half of RAG is *infrastructure you already operate*. Most people learning RAG are also learning a vector database from scratch; you are not. You only need to add the embedding step and the prompt-assembly step on top of search you already understand.

**Build.** A "chat with your docs" service: ingest a folder of PDFs/markdown (chunk + embed + index into Elasticsearch), then a `/ask` endpoint that retrieves the top chunks and answers *with citations* to the source. Use your real Elasticsearch skills for the retrieval layer. This single project is portfolio gold and maps onto countless real business needs.

**Resources.** Microsoft Learn: `Microsoft.Extensions.VectorData` and semantic search tutorials. Elasticsearch docs on dense vectors and hybrid search (you already know where to look). Anthropic's "Contextual Retrieval" write-up for advanced chunking.

---

## Module 6 — Embeddings & vector data, deeper (½ week)

**Goal:** Understand the representation layer underneath RAG so you can debug retrieval when it returns the wrong chunks.

**Concepts.**
- What an **embedding** is (a vector capturing semantic meaning), cosine similarity, and why "similar text → nearby vectors" works.
- `IEmbeddingGenerator` in `Microsoft.Extensions.AI`.
- Choosing an embedding model, dimension sizes, and the cost/quality trade-off.
- When a dedicated vector DB (Qdrant, Azure AI Search, pgvector) is worth it versus extending Elasticsearch — usually, for you, it is not worth adding a new system.

**Your shortcut.** You already think in indexes and relevance tuning from Elasticsearch. Embeddings are just a different similarity metric over the same retrieval mindset.

**Build.** Visualize it: embed 50 short sentences across 3 topics, compute pairwise cosine similarity, and confirm same-topic sentences cluster. Then deliberately break your Module 5 RAG by using a bad chunk size and observe retrieval quality drop — debugging skill you will use constantly.

**Resources.** Microsoft Learn embeddings doc; any "embeddings explained for engineers" primer.

---

## Module 7 — Agents & orchestration (1.5 weeks)

**Goal:** Build systems where the model plans multiple steps, uses several tools, keeps state across turns, and recovers from failure — using the .NET-native framework.

**Concepts.**
- What an **agent** actually is: a loop of *model decides → tool runs → result feeds back → repeat until done*, with memory and stopping conditions. Demystify the hype: it is an orchestration pattern, not magic.
- **Microsoft Agent Framework (MAF)** — GA v1.0, the successor to Semantic Kernel and AutoGen, built on the same `IChatClient` abstraction you already used. For new projects this is Microsoft's recommended path; Semantic Kernel remains supported for existing codebases. MAF adds session state, type safety, middleware, telemetry, graph-based multi-agent workflows, and support for the **MCP** (Model Context Protocol) and **A2A** (agent-to-agent) standards.
- Single-agent vs. multi-agent orchestration, and human-in-the-loop checkpoints for risky actions.
- Why agents are harder to make reliable — more steps means more places to fail — which is exactly why Modules 8–10 exist.

**Your shortcut.** MAF reuses `Microsoft.Extensions.AI`, DI, and middleware — your Module 2–4 work and your general .NET architecture sense carry straight over. Your messaging background (RabbitMQ/Azure Queues) is directly relevant for long-running or asynchronous agent jobs: kick off a multi-step agent task onto a queue, process it with a worker, exactly as you already do for background workloads.

**Build.** Convert your RAG service into an agent that can: search the docs, call the `GetOrderStatus` tool, and decide which to use based on the question — then escalate to a human (a logged "needs review" queue message) when confidence is low. Run the long-running version as a queue-backed worker.

**Resources.** Microsoft Learn: Agent Framework overview + the Semantic Kernel → MAF migration guide (useful even if you never used SK, because it explains the model clearly). Anthropic's "Building effective agents" (re-read now — it lands differently).

---

## Module 8 — Evaluation & testing (1 week)

**Goal:** Answer "is this AI feature actually good, and did my last change make it better or worse?" — without manually eyeballing outputs forever. This separates hobby projects from production systems.

**Concepts.**
- Why you cannot use normal unit-test equality assertions on probabilistic output.
- **Eval sets**: curated input → expected-property pairs you run your system against (like a test suite for behavior).
- Scoring methods: exact/keyword checks, similarity scoring, and **LLM-as-judge** (using a model to grade outputs against criteria).
- Regression evaluation in **CI/CD**: run the eval set on every change and fail the build if quality drops.
- `Microsoft.Extensions.AI.Evaluation` libraries, including quality, NLP, and content-safety evaluators.

**Your shortcut.** This is testing and CI/CD — squarely your experience. You already build pipelines and gate releases on test results; you are adding a new *kind* of test, not a new discipline. This is also where many AI-curious developers are weakest, so doing it well is a strong differentiator.

**Build.** Build a 20-example eval set for your RAG/agent service (questions + the facts a correct answer must contain). Write a runner that scores each answer (mix exact-match and LLM-as-judge), prints a pass rate, and **fails if below a threshold**. Wire it into a CI pipeline like the ones you already maintain.

**Resources.** Microsoft Learn: `Microsoft.Extensions.AI.Evaluation` docs. Articles on LLM-as-judge methodology.

---

## Module 9 — Production engineering (1 week)

**Goal:** Run AI features at acceptable cost, latency, and reliability — the unglamorous work that decides whether a feature survives contact with real traffic.

**Concepts.**
- **Cost control**: model-tier selection per task (don't use a frontier model for classification), prompt-size discipline, and **caching** (including prompt caching) for repeated or near-identical calls.
- **Latency**: streaming (Module 2), parallelizing independent calls, and choosing faster/smaller models where quality allows.
- **Reliability**: retries with backoff, timeouts, **fallback models** when a provider is down or rate-limits you, and graceful degradation.
- **Observability**: logging prompts, responses, token counts, latency, and cost per request; tracing multi-step agent runs. `Microsoft.Extensions.AI` exposes telemetry hooks (OpenTelemetry-friendly).
- **Async processing**: offloading slow LLM/agent work to background workers and queues.

**Your shortcut.** Almost all of this is *literally your CV* — incident resolution, performance/stability work (The Richmond Group), background processing with Azure Functions/WebJobs/Queues, CI/CD, and Docker microservices. You are adding token-cost and model-fallback as new dimensions to monitoring you already do. Your messaging experience makes the async/queue pattern second nature.

**Build.** Harden your service: add request-level logging of tokens + cost + latency, a retry-with-fallback policy (primary model → cheaper backup on failure), a cache for repeated questions, and a dashboard or structured logs showing cost per day. Move the heavy agent path onto an Azure Queue + worker.

**Resources.** Microsoft Learn: telemetry/caching middleware in `Microsoft.Extensions.AI`. Provider docs on prompt caching and rate limits.

---

## Module 10 — Security, safety & governance (1 week)

**Goal:** Stop AI features from being a new attack surface or a compliance liability. **This is your standout module** — your Information Security GRC specialization and your JWT/RBAC/claims work make you genuinely rare among AI-feature builders.

**Concepts.**
- **Prompt injection** — the defining new vulnerability: untrusted content (a web page, a document, a user message) carries hidden instructions that hijack the model. The core defense rule: *data from tools and documents is never trusted as instructions.* Separate instructions from data, and never let model output trigger privileged actions without authorization or a human check.
- **Data governance**: what data is allowed into prompts, PII handling, data residency (you operate in the EU — GDPR is directly relevant), and not leaking secrets or one customer's data into another's context.
- **Authorization for tools**: every function an agent can call must be access-controlled exactly like any API endpoint — your RBAC/policy-based-authorization experience applies one-to-one.
- **Output safety**: content filtering, and guardrails on actions the model can take.
- **Auditability**: logging for compliance (what was asked, what was retrieved, what action was taken) — a GRC instinct you already have.

**Your shortcut.** You hold a GRC certification and have built claims-based, policy-based authorization with custom handlers (your NETGROUP JWT system). Reframing those skills for AI — "treat the model as an untrusted actor that must be authorized and audited like any other" — is a short step, and it is exactly the framing security-conscious teams are desperate for. Most AI-feature developers have *no* security depth. You can lead here.

**Build.** Add guardrails to your agent: enforce that retrieved document content can never override the system instructions (test it by planting an injection in a document and confirming it is ignored), authorize each tool call against a claims/policy check, and log a full audit trail per request. Write a one-page threat model for the feature — a deliverable that will impress any interviewer.

**Resources.** OWASP "Top 10 for LLM Applications". Anthropic/OpenAI safety and prompt-injection guidance. Microsoft content-safety evaluator docs.

---

## Module 11 — Capstone (2 weeks)

**Goal:** Ship one polished, end-to-end AI product feature that uses every prior module, and present it like the production work it is.

Pick **one** capstone (each maps onto your background and onto real market demand):

1. **Internal knowledge assistant** — RAG over a company's documents (Elasticsearch retrieval), agentic tool use (look up live data via your APIs/SQL Server), streaming Blazor/Angular UI, full evals, cost/latency monitoring, prompt-injection guardrails, and an audit trail. *This is the most commercially common AI feature in 2026 and showcases your strongest advantages.*

2. **Support-ticket triage service** — ingests tickets, classifies/prioritizes/routes them (structured output + tool calling), drafts replies grounded in a knowledge base (RAG), runs async via queues, and is gated by an eval suite in CI. Directly echoes the production support work on your CV.

3. **Document-processing pipeline** — extracts structured data from messy documents (invoices, contracts) into typed C# objects with validation and human-in-the-loop review for low-confidence cases. Heavy on structured output, evals, and governance.

**Deliverables that make it count:** a clean GitHub repo with a real README and architecture diagram, the eval suite passing in CI, a short write-up of the threat model and cost profile, and ideally a containerized deploy. Record a 3-minute demo.

**This becomes the centerpiece of your CV's new section** — see below.

---

## Suggested timeline (≈12 weeks part-time)

| Weeks | Modules |
|------|---------|
| 1 | 0 + 1 (mindset + fundamentals) |
| 2 | 2 (first integration in C#) |
| 3 | 3 (prompt engineering) |
| 4 | 4 (structured output + tools) |
| 5–6 | 5 + 6 (RAG + embeddings) |
| 7–8 | 7 (agents + orchestration) |
| 9 | 8 (evaluation) |
| 10 | 9 (production engineering) |
| 11 | 10 (security & governance) |
| 12–13 | 11 (capstone) |

Compress freely where your CV already covers the ground (Modules 2, 9, and 10 will go faster for you than for most). Expand Modules 5 and 7 if they feel new — they are the heart of the role.

---

## Curated free resources (the short list)

- **Microsoft Learn — "Generative AI for Beginners .NET" v2** (rebuilt on .NET 10 + `Microsoft.Extensions.AI`). Your single best starting point; it speaks your language literally.
- **Microsoft Learn — `Microsoft.Extensions.AI`, `Microsoft.Extensions.VectorData`, and Agent Framework docs.** Primary reference for the native path.
- **Anthropic docs** — "Building effective agents," prompt engineering, contextual retrieval, prompt-injection guidance.
- **OpenAI docs** — prompt engineering and structured-outputs guides.
- **OWASP Top 10 for LLM Applications** — your Module 10 backbone.
- **Elasticsearch docs** — dense vector + hybrid search (you already know this site; now use it for RAG).

Avoid the firehose of YouTube "build an AI startup in 10 minutes" content. It teaches demos, not the reliability/eval/security engineering that distinguishes a software engineer from a tinkerer — and that engineering is your entire competitive edge.

---

## Updating your CV (do this as you go)

Once the capstone is done, add a section like:

> **AI-Powered Application Development** — Built production-grade AI features in C#/.NET using `Microsoft.Extensions.AI` and the Microsoft Agent Framework: retrieval-augmented generation over Elasticsearch, tool-calling agents, structured-output extraction, automated evaluation pipelines in CI/CD, and prompt-injection / authorization guardrails informed by GRC and claims-based security experience.

That sentence is credible *because* the rest of your CV already backs every claim in it. You are not pivoting careers — you are extending a strong one.

---

## The one-paragraph summary

You already are a software engineer. Becoming an "SWE leveraging AI" means learning to treat a language model as a powerful but untrusted, probabilistic dependency, and engineering reliable, safe, cost-aware software around it. The new skills are prompting, structured output and tool calling, RAG, agents, evaluation, and AI-specific security. Your `.NET`, Azure, microservices, messaging, Elasticsearch, and security background cover roughly half the work *before you start* — which is why this is a 12-week extension for you, not a multi-year reinvention. Build the projects, ship the capstone, and let your existing experience do the rest.
