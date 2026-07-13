# Skill: Verify Before Claim — The Grep-First Discipline

**Apply whenever:** about to state how code behaves, write a prompt referencing code, diagnose a failure, or choose between options whose costs depend on what exists. If a sentence contains "the code does X", "the method takes Y", or "this pattern already exists" — verify first or label it as unverified.

---

## The rule

Never state a signature, default, filter, convention, or version number from memory when it can be checked in seconds. Every unverified claim baked into a prompt or design propagates: an agent given a guessed method name hallucinates around it; a design leaning on an unwired pattern builds on vapour.

## What must always be verified before use

- **Method signatures and arity** before writing any agent prompt that mocks or calls them. (A collapsed overload on one branch and not another caused five test failures — the fix was checking the actual interface on the actual branch.)
- **Filter semantics** — a method named `GetByProjectAndFilePathAsync` might return latest-published, latest-draft, or latest-any. The name doesn't say; the `Where` clause does. When behaviour hinges on it (does the seed create *published* data?), read the implementation body.
- **Latest migration number** before creating a new one — check disk, not notes. A V24 existed that the project notes didn't mention; hardcoding from memory would have collided.
- **Whether a pattern exists** before a design reuses it. "Reuse the SESSION-CLOSE injection pattern" was a plan to reuse something never wired.
- **Which branch you are on and its state** before any agent prompt runs — `git branch --show-current`, `git status --short`. An agent run on the wrong branch cost a stash-conflict cleanup.
- **What actually constructs a class** before making a dependency required — `grep -rn "new ClassName("` tells you the real blast radius (often exactly one test call site, making the "compatibility" concern that motivated a shortcut imaginary).

## The escalating verification ladder

1. `grep -n` for the symbol — where does it live, how many overloads.
2. `sed -n 'X,Yp'` on the hit — read the actual body/signature.
3. If behaviour still ambiguous, read the caller or the test that exercises it.
4. Only then write the claim, the prompt, or the decision.

## When the check contradicts a prior belief — update loudly

If verification shows an earlier characterisation was wrong ("B is heavier" when the interception point already existed; "the injection pattern exists" when it didn't), correct it explicitly in the design/record, including *why* the earlier claim was wrong. Silent correction leaves the stale claim live in earlier documents and in other people's heads.

## Diagnose with the build, not with guesses

When something fails (e.g. an analyzer warning is being suppressed and you need its true origin): remove the suppression and let the build point at the exact line, rather than hunting by eye. The compiler's answer is authoritative; the grep-guess is not. Prefer mechanisms that make the system *tell you* over inference.

## Placeholder hygiene

When giving commands to a human to run, never leave `<line>`-style placeholders that will be pasted literally — substitute real values, or the round-trip is wasted. (Happened twice; both times cost a turn.)
