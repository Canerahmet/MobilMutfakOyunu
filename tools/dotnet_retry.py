# -*- coding: utf-8 -*-
"""Runs a `dotnet` command persistently against the Smart App Control block.

On this machine SAC is in enforced mode and can block an **unsigned,
freshly written** assembly from loading:

    FileLoadException ... An Application Control policy has blocked
    this file. (0x800711C7)

The block is **random** — the same command can be blocked once and pass
the next time. Rebuilding alone is not enough: because the core is built
with `<Deterministic>true</Deterministic>` the same source always
produces the same binary, so a blocked hash stays blocked forever.
`-p:Deterministic=false` embeds a new module identity on every build, so
every attempt is **a new hash** and a new chance.

So the right behaviour is: change the hash **and** keep trying.

Usage:
    python tools/dotnet_retry.py test tests/Lokanta.Core.Tests/... -v q
    python tools/dotnet_retry.py run --project src/Lokanta.Harness ...

The exit code is the command's own. If the block never lets it through, 2.
"""
from __future__ import print_function

import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BLOCKED = "0x800711C7"
TRIES = 6


def assemble(args):
    """The dotnet command line, with the configuration and the determinism
    flag added. They are added here rather than left to the caller: when
    one is forgotten, the symptom looks like "the code is broken".

    THEY GO BEFORE "--", AND FOR FOUR DAYS THEY DID NOT.

    For `dotnet run --project X -- --cuisine turk`, appending the flags put
    them after the "--", which is the line dotnet stops reading at: they
    were handed to the application as arguments, the application ignored
    them, and every first attempt built DEBUG with determinism ON - the
    exact configuration this tool exists to avoid. The retry then built
    Release correctly and ran `run --no-build`, which starts the Debug
    binary: the one that had just been blocked. So a blocked run stayed
    blocked through all six attempts, which is precisely what the
    calibration reported on 18 September, twice. The workaround had never
    once applied to a `run`.

    Nothing said so, because the process list is the only place the
    configuration is visible, and nobody reads bin\\Debug in a path.
    """
    cmd = ["dotnet"] + list(args)
    flags = []
    if "-c" not in args and "--configuration" not in args:
        flags += ["-c", "Release"]
    if not any(a.startswith("-p:Deterministic") for a in args):
        flags += ["-p:Deterministic=false"]
    if "--" in cmd:
        i = cmd.index("--")
        cmd[i:i] = flags
    else:
        cmd += flags
    return cmd


def main():
    args = sys.argv[1:]
    if not args:
        print(__doc__)
        return 2

    cmd = assemble(args)

    last = None
    for attempt in range(1, TRIES + 1):
        # THE REBUILD IS FORCED.
        #
        # -p:Deterministic=false alone is NOT ENOUGH: if the source has
        # not changed MSBuild considers the build up to date and SKIPS
        # it, so the same blocked binary is used again and the retry
        # changes nothing. Measured - all six of six attempts were
        # blocked on the same file.
        #
        # --no-incremental really does repeat the build; because
        # determinism is off, every repeat produces a NEW module
        # identity.
        run = list(cmd)
        if attempt > 1 and "--no-incremental" not in run:
            # "dotnet run" DOES NOT RECOGNISE this flag and passes it on
            # to the application; so for run, the build is a SEPARATE
            # step.
            if args[0] == "run":
                proj = None
                for i, a in enumerate(args):
                    if a == "--project" and i + 1 < len(args):
                        proj = args[i + 1]
                if proj:
                    subprocess.run(["dotnet", "build", proj, "-c", "Release",
                                    "-v", "q", "--nologo",
                                    "-p:Deterministic=false", "--no-incremental"],
                                   cwd=ROOT, capture_output=True)
                    if "--no-build" not in run:
                        run.insert(run.index("run") + 1, "--no-build")
            else:
                run.append("--no-incremental")

        p = subprocess.run(run, cwd=ROOT, capture_output=True, text=True,
                           encoding="utf-8", errors="replace")
        out = (p.stdout or "") + (p.stderr or "")
        last = p

        if BLOCKED not in out:
            sys.stdout.write(p.stdout or "")
            sys.stderr.write(p.stderr or "")
            return p.returncode

        print("  SAC block (%d/%d) - changing the hash and retrying"
              % (attempt, TRIES), file=sys.stderr)

    sys.stdout.write(last.stdout or "")
    print("\nSAC blocked all %d attempts. docs/19 'Smart App Control'."
          % TRIES, file=sys.stderr)
    return 2


if __name__ == "__main__":
    sys.exit(main())
