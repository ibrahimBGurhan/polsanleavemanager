# Git

> Bootstrap'ta ANEW varsayılanları olduğu gibi kabul edildi (2026-10-21). Uzak depo: GitHub.
> Bootstrap çıktısı da kurala uyar: `chore/bootstrap` dalı + PR.

## Branching
- `feature/<spec-no>-<short-name>` — **no branch without a spec.** Also for CHANGE-lane mini-specs.
- Refactors: `refactor/<spec-no>-<short-name>` (mini-spec with Changed behavior = none).
- Fixes: `fix/<report-id>-<short-name>` (bug fixes carry a report, not a spec number);
  incidents: `incident/<date>-<short-name>`; trivial changes: `trivial/<short-name>` (see below).

## Commits
- Conventional Commits, with a plan reference: `feat(catalog): paging endpoint [plan 0001/3]`.
- Agent commits follow the same standard: the agent writes the message, the human approves.

## Forbidden
- Direct commits to the default branch.
- Force push, history rewriting on shared branches. Undo = `git revert` (see recovery R-11).

## Trivial changes
<!-- Set at bootstrap. Applies only to requests the change-request triage rubric
     (workflows/change-request.md) classifies as TRIVIAL: no acceptance criterion changes, no shared code. -->
- Policy: **(1) PR + one reviewer, no spec** — chosen at bootstrap (2026-10-21). Not chosen:
  (2) direct commit on a `trivial/<short-name>` branch · (3) everything through a mini-spec.
- Always: one narrow commit, `scripts/check` green. A second file or a new test means re-triage.

## Pull requests
- PR template checklist completed; `scripts/check` green in CI; squash-merge.
- En az bir onaycı (reviewer) zorunlu (ekip gelene kadar tek geliştirici kendi PR'ını birleştirir).
- **Zorlama durumu (2026-10-22):** `main` için `main-protection` ruleset'i tanımlı (silme ve force push
  yasak, PR zorunlu, yalnızca squash, `doctor` + `check` zorunlu), ama depo kişisel hesapta private olduğu
  için GitHub Free'de **uygulanmıyor**. Bu kurallara şimdilik süreçle uyulur. Depo bir Polsan
  organizasyonuna (Team) taşındığında ruleset zorlanır ve onay sayısı 1 yapılır.
- Commit mesajları ve branch adları İngilizce (`docs/conventions.md`, Language).
