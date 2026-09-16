# -*- coding: utf-8 -*-
"""Do ProjectSetup.cs and LokantaURP.asset say the same thing?

Why it exists: the render settings are written in **two places**.
`LokantaURP.asset` is the file Unity reads; `ProjectSetup.ConfigureUrp`
is the code that rewrites the same fields during setup. When the two
drift apart no test breaks — the game quietly builds with the old
setting.

And they did drift: the performance pass fixed the `.asset` file by hand
(MSAA 4→1, render scale 1→0.8) while `ProjectSetup` went on writing the
old values. Anyone who ran `ApplyAll` undid the whole performance effort
and there was no way to see it.

Writing the same number in two places has drifted five times in this
project; this script is here to catch the sixth.

Exit code 0 clean, 1 they have drifted.
"""
from __future__ import print_function

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SETUP = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Editor", "ProjectSetup.cs")
ASSET = os.path.join(ROOT, "unity", "Assets", "Settings", "LokantaURP.asset")

# SetUrpField(so, "m_X", p => p.<kind>Value = <value>, "...")
CALL = re.compile(
    r'SetUrpField\(\s*so\s*,\s*"(?P<field>\w+)"\s*,\s*'
    r'p\s*=>\s*p\.(?P<kind>\w+)\s*=\s*(?P<value>[^,]+?)\s*,',
    re.S)


def expected(kind, value):
    """Turns the value on the C# side into the number in the .asset file."""
    value = value.strip()
    if kind == "boolValue":
        return 1.0 if value == "true" else 0.0
    if kind in ("intValue", "enumValueIndex"):
        return float(int(value))
    if kind == "floatValue":
        return float(value.rstrip("fF"))
    return None                       # a kind we do not know: skip it


def main():
    for p in (SETUP, ASSET):
        if not os.path.exists(p):
            print("file not found: %s" % p)
            return 1

    code = io.open(SETUP, encoding="utf-8").read()
    asset = io.open(ASSET, encoding="utf-8").read()

    # .asset lines: "  m_MSAA: 1"
    in_asset = {}
    for line in asset.splitlines():
        m = re.match(r"\s*(m_\w+):\s*([-\d.]+)\s*$", line)
        if m:
            in_asset[m.group(1)] = float(m.group(2))

    broken, examined, skipped = [], 0, []
    for m in CALL.finditer(code):
        field, kind, value = m.group("field"), m.group("kind"), m.group("value")
        want = expected(kind, value)
        if want is None:
            skipped.append("%s (%s)" % (field, kind))
            continue
        if field not in in_asset:
            # The field is not a scalar in the .asset (object, array) or
            # does not exist in this version.
            skipped.append("%s (not a scalar in the asset)" % field)
            continue
        examined += 1
        if abs(in_asset[field] - want) > 1e-6:
            broken.append("  %-36s code %-8g asset %-8g"
                          % (field, want, in_asset[field]))

    if broken:
        print("URP SETTINGS HAVE DRIFTED (%d fields):" % len(broken))
        for k in broken:
            print(k)
        print("\n  ProjectSetup.ConfigureUrp and Assets/Settings/LokantaURP.asset")
        print("  must say the same number. Otherwise running ApplyAll")
        print("  silently reverts the value in the asset.")
        return 1

    print("result    : %d URP fields match%s" % (
        examined, (", %d skipped" % len(skipped)) if skipped else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main())
