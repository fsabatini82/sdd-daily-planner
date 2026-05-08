# lab — SDD Daily Planner (token-pricing-aware)

> allineato al **nuovo modello di billing GHCP token-based attivo dal 1° giugno 2026**.
>
> **Una sola strategia, coerente con la separation of concerns**:
> - **3 agenti specializzati**, uno per ruolo (spec-reviewer, plan-reviewer, code-reviewer)
> - **Modello giusto per il lavoro giusto** — `gpt-5-mini` (Included = €0) per i due deterministici, `gpt-5.4` (Versatile, metered) solo per quello semantico
> - **Prompt targettati** — ogni agente legge SOLO i file che gli servono
> - **Orchestrator C# completo** (no TODO) — `CopilotCliRunner` già implementato
>
> **NIENTE** dual-mode, **niente** meta-agent, **niente** caching tricks: la simplicity è la feature.

---

## Struttura

```
lab-repo/
├── library-system/                      ← TARGET di analisi (5 drift inseriti)
│   ├── .github/agents/
│   │   ├── spec-reviewer.agent.md       ← model: gpt-5.4 (Versatile, metered)
│   │   ├── plan-reviewer.agent.md       ← model: gpt-5-mini (Included)
│   │   └── code-reviewer.agent.md       ← model: gpt-5-mini (Included)
│   ├── specs/                            ← SPEC-01..03
│   ├── plan.md                           ← stale rispetto al codice
│   └── src/                              ← C# Web API
│
├── orchestrator/                         ← C# .NET 8 — completo, no TODO
│   ├── Program.cs                        ← bootstrap host + DI
│   ├── appsettings.json                  ← 3 agenti con prompt targettati
│   ├── Models/Options.cs                 ← DTOs minimi
│   └── Services/
│       ├── CopilotCliRunner.cs           ← Process wrapper (no TODO)
│       ├── JsonExtractor.cs              ← regex per fenced ```json
│       ├── OrchestratorService.cs        ← loop secco sui 3 agenti
│       └── ReportRenderer.cs             ← unified markdown
│
├── scheduling/morning-sdd-triangulation.xml  ← Task Scheduler (StartBoundary 2026-06-01)
└── reports/                              ← output (morning-report-{yyyyMMdd-HHmm}.md)
```

---

## Le tre ottimizzazioni 

### 1. Modelli per agente — il lever più grosso

Nel frontmatter di ogni `.agent.md` abbiamo messo l'identifier *esplicito* del nuovo listino, scelto in base al lavoro che l'agente fa:

| Agente | Modello | Categoria | Costo per 1M token (input/output) | Perché |
|--------|---------|-----------|----------------------------------|--------|
| `spec-reviewer` | `gpt-5.4`     | Versatile · metered | $2.50 / $15.00 | Drift semantico richiede reasoning vero |
| `plan-reviewer` | `gpt-5-mini`  | Lightweight · **Included** | gratis nel piano | Matching checkbox → codice, deterministico |
| `code-reviewer` | `gpt-5-mini`  | Lightweight · **Included** | gratis nel piano | Pattern code-quality, deterministico |

`gpt-5-mini` è **Included** nel piano: zero AI Credits consumati. La spesa effettiva del run è quasi tutta sullo `spec-reviewer`.

> Identifier verificati su [Models and pricing for GitHub Copilot](https://docs.github.com/en/copilot/reference/copilot-billing/models-and-pricing). I nomi modello si aggiornano periodicamente — controllare prima di applicare.

### 2. Prompt targettati — ogni agente legge SOLO ciò che gli serve

In `orchestrator/appsettings.json` ogni prompt **restringe esplicitamente** l'ambito di lettura dell'agente:

| Agente | Cosa LEGGE | Cosa NON LEGGE |
|--------|------------|----------------|
| `spec-reviewer` | `specs/*.md` + `src/**/*.cs` | `plan.md` |
| `plan-reviewer` | `plan.md` + `Program.cs` (endpoint) + signatures pubbliche | corpo dei service, contenuto delle spec (solo gli ID BR/AC servono) |
| `code-reviewer` | `src/**/*.cs` | `specs/`, `plan.md` |

**Effetto**: input tokens ridotti rispetto a "ogni agente legge tutto". Ogni agente è anche più focalizzato → output più preciso (meno rumore in cui perdersi).

### 3. Tool perimeter minimo

Solo `--allow-tool read --allow-tool search` in `CopilotCliRunner.cs`. Nessun `shell` o `edit`. Più tool = più step autonomi dell'agente = più token.

---

## La math del costo per un run

Per un repo come `library-system` (~600 LOC + 3 specs + plan):

| Agente | Modello | Input letti | Cost input | Cost output | Totale |
|--------|---------|-------------|------------|-------------|--------|
| `spec-reviewer` | `gpt-5.4` (metered) | ~12K (specs+src) | 12K × $2.50/M = $0.030 = 3 cr | ~3K × $15/M = ~5 cr | **~8 cr** |
| `plan-reviewer` | `gpt-5-mini` (**Included**) | ~8K (plan + signatures) | **0 cr** | **0 cr** | **0 cr** |
| `code-reviewer` | `gpt-5-mini` (**Included**) | ~7K (src) | **0 cr** | **0 cr** | **0 cr** |

**Totale per run: ~8 AI Credits.** Su 20 giorni feriali = **~160 credits/mese per repo monitorato**. Con piano Pro+ (3.900 credits/mese) sostieni **20+ repo continuativi** senza extra-fees.

> Stime basate sul `library-system` di esempio. Numeri reali variano col modello e con la dimensione del codebase. Per il numero autoritativo: dashboard *Billing → Usage* dell'organizzazione.

### Confronto rapido

| Configurazione | Cost/run | Mensile (20 gg) | Repo monitorabili Pro+ |
|----------------|---------:|----------------:|-----------------------:|
| MultiCall naive (tutti `gpt-5.4`, prompt non targettati)   | ~25 cr | ~500 cr | 7-8 |
| **MultiCall ottimizzato (questo repo)**                     | **~8 cr** | **~160 cr** | **20+** |

Il guadagno viene da DUE leve combinate: 2 agenti su 3 vanno su modello Included (zero costo), e i prompt targettati eliminano letture ridondanti.

---

## Quickstart

### 0. Prerequisiti
Identici a `lab-repo/`: Node 22+, .NET 8 SDK, GHCP CLI, fine-grained PAT con `Copilot Requests: Read`.

### 1. PAT
```powershell
$env:COPILOT_GITHUB_TOKEN = "github_pat_..."
```

### 2. Build
```powershell
dotnet build .\orchestrator\SddOrchestrator.csproj
```

### 3. Run
```powershell
dotnet run --project .\orchestrator\SddOrchestrator.csproj
```

Output:
```
HH:MM:SS info: Target repo: ...\lab-repo2\library-system
HH:MM:SS info: --- Invoking agent 'spec-reviewer' ---
HH:MM:SS info: Agent 'spec-reviewer' done in 00:00:42 — success: True
HH:MM:SS info: --- Invoking agent 'plan-reviewer' ---
...
✓ Report: ..\reports\morning-report-20260601-0900.md
```

### 4. Apri il report
Il file `morning-report-{yyyyMMdd-HHmm}.md` ha 3 sezioni JSON, una per agente, con i finding citati `file:line`.

---

## Schedulazione

`scheduling/morning-sdd-triangulation.xml` ha `StartBoundary: 2026-06-01` (allineato al rollout del nuovo billing). Importa con:

```powershell
schtasks /Create /TN "SDD-Triangulation-Lab2-Daily" /XML "$PWD\scheduling\morning-sdd-triangulation.xml"
```

Per il pattern multi-repo (un task per repo, agenti centralizzati), vedi [`../agents-pack/README.md`](../agents-pack/README.md): basta installare gli agenti via `INSTALL.ps1` e i 3 agenti diventano user-wide, riusabili su qualsiasi target.

---

## Estensioni possibili

1. **Override `TargetRepoPath` via env var** per il pattern multi-repo: `Microsoft.Extensions.Configuration` legge `Orchestrator__TargetRepoPath` come override di `appsettings.json`. Vedi `agents-pack/run-on-repo.ps1`.
2. **Aggiungere un 4° agente** (es. `security-reviewer`) con `model: gpt-5-mini` (Included = €0): drop di un nuovo `.agent.md` + entry in `appsettings.json`, niente ricompilazione.
3. **Output → Teams** invece di file markdown: post-run hook in `Program.cs` che fa `curl` su un webhook con il contenuto del report.
4. **Cost reporter inline**: estensione del `ReportRenderer` per aggiungere una tabella di stima token consumati (basata su lunghezza output e prompt).

---

## Setup Guide

### 1 Prerequisiti

| Requisito | Versione minima | Verifica |
|-----------|-----------------|----------|
| Node.js | 22+ | `node --version` |
| .NET SDK | 8.0+ | `dotnet --version` |
| GitHub Copilot CLI | latest | `copilot --version` |
| Licenza GHCP | Pro / Pro+ / Business / Enterprise | <https://github.com/settings/copilot> |
| OS | Windows 10/11, macOS 13+, Linux | — |
| Terminale | PowerShell 7+ (Win), bash/zsh (mac/linux) | — |

### 2 Installazioni rapide

**Windows**:

```powershell
winget install OpenJS.NodeJS.LTS
winget install Microsoft.DotNet.SDK.8
npm install -g @github/copilot
copilot --version
```

**macOS**:

```bash
brew install node@22 dotnet-sdk
npm install -g @github/copilot
copilot --version
```

**Linux** (Ubuntu/Debian):

```bash
curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash -
sudo apt-get install -y nodejs dotnet-sdk-8.0
npm install -g @github/copilot
copilot --version
```

### 3 PAT fine-grained con scope `Copilot Requests`

⚠️ **Critical**: deve essere fine-grained, NON classic. I token classic (`ghp_*`) vengono **silenziosamente ignorati** dalla CLI.

1. Vai su <https://github.com/settings/personal-access-tokens>
2. **Generate new token** → user-owned (NON organization)
3. Token name: `copilot-cli-lab2` · Expiration: 30-90 giorni
4. Repository access: *Public Repositories (read-only)* è sufficiente
5. **Permissions → Account permissions → Copilot Requests → Read-only**
6. Genera, copia il token (inizia con `github_pat_...`)

### 4 Configura `COPILOT_GITHUB_TOKEN`

**Sessione corrente**:
```powershell
$env:COPILOT_GITHUB_TOKEN = "github_pat_..."   # PowerShell
```
```bash
export COPILOT_GITHUB_TOKEN="github_pat_..."   # bash/zsh
```

**Persistente** (per scheduling):
```powershell
[Environment]::SetEnvironmentVariable("COPILOT_GITHUB_TOKEN", "github_pat_...", "User")
```

### 5 Clone & smoke test

```powershell
git clone <URL-DEL-REPO> sdd-cli-lab
cd sdd-cli-lab\lab-repo2

dotnet build .\orchestrator\SddOrchestrator.csproj
# atteso: Build succeeded. 0 Warning(s) 0 Error(s)

copilot --agent spec-reviewer -p 'Reply with JSON {"ok": true}' --allow-tool read --no-color
```

Se i comandi passano, sei pronto.
