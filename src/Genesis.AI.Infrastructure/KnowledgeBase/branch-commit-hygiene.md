# Skill: Branch, Commit & Session Hygiene

**Apply whenever:** starting implementation work, coming off a long design or debugging session, preparing commits, or managing the exp/PR branch split. Apply the audit BEFORE the first line of new code, not after something breaks.

---

## The audit rule — clean baseline before building

After any complex session (long design discussion, debugging, failed revert, merge conflict, agent mishap), run the audit before new implementation:

```bash
git branch --show-current   # right branch? (agents have run on the wrong one)
git status --short          # stray uncommitted changes? untracked files that should be committed?
git log --oneline -5        # duplicates? unexpected commits? how far ahead of origin?
```

What the audit catches in practice: an uncommitted guardrail file (the instructions protecting the very work about to start), a duplicate commit from a double-run, and confirmation that a reverted shortcut actually stayed reverted (`Directory.Build.props` absent from status).

## Commit discipline

- **Small, staged, logical units.** Tests + implementation of one feature = one commit (separate commits leave a RED build in history). Docs on their own. Instructions/guardrails on their own. Explicit `git add <files>` over `git add -A` so nothing stray is swept in — and so a file that *should* have zero changes (build props) is conspicuously absent from the list.
- **Commit messages carry the reasoning**, not just the change: what was broken, why this shape, the design reference, the test counts. A year later the message is the archaeology.
- **Duplicate commits**: verify identity before dropping — `git diff <sha1> <sha2>` must be empty; only then `git rebase -i` and `drop` the second. Never drop on message-similarity alone.
- **zsh trap**: `!` inside double-quoted commit messages triggers history expansion ("event not found"). Single-quote commit messages.

## Branch model discipline (exp / PR split)

- **exp branches** (`*-exp`): experiments, full history, wip allowed. All agent implementation happens here.
- **PR branches**: clean, cherry-picked commits only. Nothing lands there except deliberately curated changes.
- **Before every agent prompt**: confirm the agent's working branch matches the intended one. An agent prompt executed on the PR branch instead of exp caused a stash→checkout→conflict cleanup. State the branch in the prompt AND verify with `git branch --show-current` in the agent's own command log.
- **Branches can diverge in signatures.** A fix applied to the PR branch (e.g. an interface collapse) may not exist on exp yet — never assume parity; check the actual file on the actual branch before writing prompts against it.

## Session-end discipline

Before stopping: everything committed or deliberately parked (named, with a reason), status clean, and a short written record of (a) what landed, (b) exact next step, (c) open decisions. The next session's quality depends on this one's landing. Unpushed local commits are fine on exp — but know how many and say so.

## Pager awareness

`git log`/`diff` output ending in `(END)` means the human is inside `less` and subsequent commands didn't run — tell them to press `q`, then re-issue only the missing command.
