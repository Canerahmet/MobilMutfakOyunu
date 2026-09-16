# -*- coding: utf-8 -*-
"""Is everything where it should be.

This script runs the checkers on **both the Python and the .NET side**
in order and gives one answer.

Why it is needed: each of these checkers was written to catch a real
bug, and each of them missed one at least once **because it was not
run**. Half an hour after the font-coverage checker was written, a new
string was added and that string contained an arrow that was not in the
font; it was only seen when the checker was run by hand.

THE DOCSTRING ONCE SAID "there are six checkers and it runs all of
them". Both halves were wrong: there were thirteen steps, and **none**
of the checkers on the Unity side (ArtCheck, PlacementAudit, RoomLayout,
GameShot and the smoke tour) were in the list. So the docstring's own
justification ("each of them missed one at least once because it was not
run") applied exactly to those checkers.

The Unity steps are separate, because each one opens an editor session
and takes minutes; they are skipped in the default run but they **appear
in the list**.

Usage:
    python tools/check.py            Python + .NET checks
    python tools/check.py --fast    skip the tests (takes seconds)
    python tools/check.py --unity    run the Unity checkers too (slow)

Exit code 0 clean, 1 at least one check is red.
"""
from __future__ import print_function

import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PY = sys.executable


def run(name, args, cwd=ROOT):
    """A single check. Returns (name, passed, last line).

    If a `dotnet` call is blocked it is retried **with a changed hash**:
    on this machine Smart App Control can block an unsigned assembly
    from loading, and the block is tied to the file's hash. Because the
    core is built with `<Deterministic>true</Deterministic>` the same
    source always produces the same hash — so a blocked assembly is
    **not fixed** by rebuilding, it stays blocked forever.
    `-p:Deterministic=false` produces a new identity on every build and
    the block falls away by itself. Details: docs/19.
    """
    if args and args[0] == "dotnet":
        r = _once(name, args + ["-c", "Release"], cwd)
        # THE BLOCK IS LOOKED FOR IN THE WHOLE OUTPUT, not the last line.
        #
        # Only the `tail` was examined at first, and xUnit writes the
        # block message at the TOP ("Skipping: ... An Application Control
        # policy has blocked this file") and then ends with information
        # lines. So the block was never seen and the retry never fired.
        # THE RETRY DEPENDS ON EVIDENCE, NOT ON A REASON.
        #
        # At first it only retried when 0x800711C7 appeared. But the
        # block does not always write that code: the test host sometimes
        # silently finds ZERO tests and exits 0 ("A total of 1 test files
        # matched" and no summary line at all). So the code is not looked
        # for, the check is counted as failed and the retry never fired.
        if r[1]:
            return r
        # Blocked: BY CHANGING THE HASH **and rebuilding**.
        # -p:Deterministic=false alone is not enough - if the source has
        # not changed MSBuild skips the build and the same blocked binary
        # is used again.
        #
        # `dotnet test` IS HANDLED SEPARATELY: it does not accept
        # --no-incremental (MSB1001 "Unknown switch"), so the old path
        # NEVER worked for the tests. Build first, then run with
        # --no-build. A fresh hash can be blocked too, hence several
        # attempts.
        if len(args) > 1 and args[1] == "test":
            proj = args[2]
            for _ in range(5):
                # THE RESULT OF THE BUILD MATTERS.
                #
                # It used to be ignored, and that broke one more thing:
                # --no-incremental CLEANS first, so when the build fails
                # the test assembly DISAPPEARS and the following
                # "--no-build" run said "test source file ... was not
                # found". A temporary block was turning into a different
                # error that looked permanent.
                b = _once(name, ["dotnet", "build", proj, "-c", "Release",
                                 "-v", "q", "--nologo", "-p:Deterministic=false",
                                 "--no-incremental"], cwd)
                if not b[1]:
                    continue
                r = _once(name, args + ["-c", "Release", "--no-build"], cwd)
                if r[1]:
                    return r

            # Five attempts and it still could not run. One last normal
            # build so the repository is not left without a test
            # assembly.
            _once(name, ["dotnet", "build", proj, "-c", "Release",
                         "-v", "q", "--nologo"], cwd)
            return r
        return _once(name, args + ["-c", "Release", "-p:Deterministic=false",
                                   "--no-incremental"], cwd)
    return _once(name, args, cwd)


def _once(name, args, cwd=ROOT):
    try:
        p = subprocess.run(args, cwd=cwd, capture_output=True, text=True,
                           encoding="utf-8", errors="replace")
    except OSError as e:
        return name, False, "could not be started: %s" % e, ""

    out = (p.stdout or "") + (p.stderr or "")
    lines = [l.strip() for l in out.splitlines() if l.strip()]
    tail = lines[-1] if lines else "(no output)"

    ok = p.returncode == 0

    # PROOF THAT THE TESTS RAN.
    #
    # When the test assembly cannot be loaded, `dotnet test` EXITS 0 and
    # only writes "Skipping: ... blocked". The check counted that as a
    # pass: zero of 239 tests ran and the table lit up green. This
    # project's most frequent class of bug, this time inside the CHECKER
    # ITSELF.
    #
    # Evidence is now required: if xUnit's summary line ("Passed!" /
    # "Failed!") does not appear in the output, the check is red.
    if ok and len(args) > 1 and args[0] == "dotnet" and args[1] == "test":
        if "Passed!" not in out and "Failed!" not in out:
            ok = False
            tail = "THE TESTS DID NOT RUN (no summary line): " + tail

    return name, ok, tail, out


def main():
    quick = "--fast" in sys.argv
    unity = "--unity" in sys.argv

    checks = [
        ("content generation",
         [PY, os.path.join("tools", "balance", "export.py")]),
        ("dish balance",
         [PY, os.path.join("tools", "content", "gen_dishes.py")]),
        ("staff traits",
         [PY, os.path.join("tools", "content", "gen_traits.py")]),
        ("regulars",
         [PY, os.path.join("tools", "content", "gen_regulars.py")]),
        ("staff names",
         [PY, os.path.join("tools", "content", "gen_names.py")]),
        ("string table",
         [PY, os.path.join("tools", "content", "gen_loc.py")]),
        ("content-code contract",
         [PY, os.path.join("tools", "audit_content.py")]),
        ("font coverage",
         [PY, os.path.join("tools", "art", "check_font.py")]),
        ("store listing texts",
         [PY, os.path.join("tools", "content", "check_store_texts.py")]),
        ("document links",
         [PY, os.path.join("tools", "check_docs.py")]),
        # RULE 1 OF CLAUDE.md, MEASURED.
        #
        # "Everything a reader sees is in English" is the kind of rule
        # that decays one hurried variable name at a time. Running it
        # here is what makes it a rule rather than a preference.
        ("written in English",
         [PY, os.path.join("tools", "check_english.py")]),
        ("urp settings",
         [PY, os.path.join("tools", "check_urp.py")]),
        # LICENCES: the gate for a commercial release. If an asset folder
        # is left without a licence or without a row in the attribution
        # ledger, a release risk is created - and it is not Google Play
        # that catches it, it is the copyright holder.
        ("licence and attribution",
         [PY, os.path.join("tools", "check_licenses.py")]),
    ]

    if not quick:
        # THE NETSTANDARD GUARD. The core builds for two targets and the
        # tests only load the net10.0 one - so the netstandard2.1 target
        # can pass without ever being built. That target is the ONLY
        # check that the core stays inside Unity's API surface; if it
        # does not run it is not a guard, it is an ornament.
        checks.append(("netstandard guard",
                       ["dotnet", "build",
                        os.path.join("src", "Lokanta.Core", "Lokanta.Core.csproj"),
                        "-f", "netstandard2.1", "-v", "q", "--nologo"]))
        checks.append(("content netstandard",
                       ["dotnet", "build",
                        os.path.join("src", "Lokanta.Content", "Lokanta.Content.csproj"),
                        "-f", "netstandard2.1", "-v", "q", "--nologo"]))
        checks.append(("core tests",
                       ["dotnet", "test",
                        os.path.join("tests", "Lokanta.Core.Tests",
                                     "Lokanta.Core.Tests.csproj"),
                        "-v", "q", "--nologo"]))

    # THE UNITY SIDE. Each one opens an editor session; they are skipped
    # in the default run but they APPEAR IN THE LIST - the most dangerous
    # state for a tool that says "I run all of them" is not counting what
    # it did not run.
    unity_checks = [
        ("art check",
         ["powershell", "-NoProfile", "-File",
          os.path.join("tools", "unity", "run.ps1"),
          "-Method", "Lokanta.EditorTools.ArtCheck.Run"]),
        ("placement check",
         ["powershell", "-NoProfile", "-File",
          os.path.join("tools", "unity", "run.ps1"),
          "-Method", "Lokanta.EditorTools.PlacementAudit.Run"]),
        ("room layout",
         ["powershell", "-NoProfile", "-File",
          os.path.join("tools", "unity", "run.ps1"),
          "-Method", "Lokanta.EditorTools.RoomLayout.Capture"]),
        ("scene screenshot",
         ["powershell", "-NoProfile", "-File",
          os.path.join("tools", "unity", "shot.ps1")]),
        ("smoke tour",
         ["powershell", "-NoProfile", "-File",
          os.path.join("tools", "unity", "tour.ps1")]),
    ]

    if unity:
        checks += unity_checks
    else:
        print("(skipped, run with --unity: "
              + ", ".join(n for n, _ in unity_checks) + ")")

    print("=" * 70)
    results = []
    for name, args in checks:
        name, ok, tail, _ = run(name, args)
        results.append((name, ok, tail))
        print("%-24s %s  %s" % (name, "OK" if ok else "RED", tail[:60]))

    print("=" * 70)
    bad = [r for r in results if not r[1]]
    if not bad:
        print("ALL CLEAN (%d checks)" % len(results))
        return 0

    print("%d CHECKS RED:" % len(bad))
    for name, _ok, tail in bad:
        print("  %s: %s" % (name, tail))
    return 1


if __name__ == "__main__":
    sys.exit(main())
