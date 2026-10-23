#!/bin/sh
# Test for scripts/zero-test-guard (spec 0002). POSIX sh, sed and grep only — no .NET, database or
# browser. Run from the repository root: sh tests/scripts/zero-test-guard.test.sh (the guard-test
# step in scripts/check.conf does). Exit 0 = all cases pass, exit 1 = at least one failed.
set -u
ESC=$(printf '\033')
passed=0
failed=0
ok()   { passed=$((passed + 1)); echo "ok    $1"; }
fail() { failed=$((failed + 1)); echo "FAIL  $1"; }

# guard <summary line>: runs the guard on the line; sets $rc and $err (its stderr).
guard() { err=$(printf '%s\n' "$1" | sh scripts/zero-test-guard 2>&1 >/dev/null); rc=$?; }
passes() { guard "$1"; if [ "$rc" -eq 0 ]; then ok "$2"; else fail "$2 (exit $rc)"; fi; }
fails()  {
  guard "$1"
  if [ "$rc" -eq 1 ] && [ "$err" = "zero tests ran (docs/testing.md, Two speeds rule 4)" ]; then ok "$2"
  else fail "$2 (exit $rc, stderr: $err)"; fi
}

# CB-1, CB-2: colour-coded summaries as CI prints them.
passes "${ESC}[m  total: 94"              "CB-1 coloured 'total: 94' passes"
fails  "${ESC}[m  total: 0"               "CB-2 coloured 'total: 0' fails with the zero-test message"
fails  "${ESC}[90m  total: ${ESC}[m0${ESC}[m" "CB-2 colour codes inside the line cannot hide a zero"
# CB-3: plain summaries as a local run prints them.
passes "  total: 94"                      "CB-3 plain 'total: 94' passes"
fails  "  total: 0"                       "CB-3 plain 'total: 0' fails"
fails  ""                                 "CB-3 no summary at all fails"

# CB-4: every test step in check.conf delegates to the guard (one source of truth).
for step in test-unit test-integration test-e2e; do
  if grep -E "^$step( \[fast\])?:" scripts/check.conf | grep -qF "| sh scripts/zero-test-guard || exit 1"; then
    ok "CB-4 $step calls scripts/zero-test-guard"
  else
    fail "CB-4 $step calls scripts/zero-test-guard"
  fi
done

echo "== zero-test-guard: $passed passed, $failed failed =="
[ "$failed" -eq 0 ]
