# 46 — The binary we shipped never ran

*14 September 2026.* The question started as "should we install an Android
emulator to simulate the game". The answer came out no, but the gap
**underneath** the question was real, and bigger than the one the emulator
would have closed.

---

## 1. Why the emulator does not help

Today's APK **cannot be installed** on an emulator. Emulator system images are
x86_64; installation fails with `INSTALL_FAILED_NO_MATCHING_ABIS`.

This was read off **the package itself**, not off what the build script says —
intent and output have come apart in this project before:

```
$ aapt2 dump badging build/android/Lokanta.apk
package: name='com.ahmetakar.lokanta' versionCode='1' versionName='0.1.0'
minSdkVersion:'29'  targetSdkVersion:'36'
native-code: 'arm64-v8a'
```

There is a single ABI under `lib/`: `arm64-v8a`. The same dump confirms two
more things: minSdk 29 (the floor [19](19-technical-setup.md) writes down) and
**no INTERNET** in the permission list — so the sentence in
[44](44-store-texts.md)'s privacy text, "it does not even ask for internet
permission", is true of today's package too.

So using an emulator means compiling **a second build we do not ship** and
testing that one. This is exactly what this project's recurring lesson forbids:
*a green tick may be measuring the wrong thing.* If the binary under test is not
the binary that ships, the tick was measuring the wrong thing from the start.

There are three more things an emulator structurally cannot give, and all three
land squarely on this game:

| | emulator | real phone |
|---|---|---|
| ARM64 binary | will not install | runs |
| frame time / GPU | meaningless | real |
| heat (a 60-day campaign) | none | real |
| two-finger camera | mouse imitation | real |

`adb` is already installed — it comes with Unity's Android module
(`…/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe`). So the real
device path **requires no download at all**, where the emulator path wanted
~3 GB.

[tools/android/device.ps1](../tools/android/device.ps1) was written: it finds adb
independently of the version, checks the device's ABI **before** installing
(that error message does not say why), installs the APK, runs it, and takes the
log and a screenshot.

Two traps were written into the script:

- The log is **cleared** (`logcat -c`); otherwise the previous run's crash looks
  like this run's.
- If there is no Unity line at all the result is `COULD NOT MEASURE` — "no
  errors" and "the app never opened" look the same from outside. The screenshot
  is for the same reason: a process stays alive on a black screen too.

---

## 2. The real gap: not touch, the stripper

While researching the emulator question, `unity/Assets/link.xml` surfaced. Its
own comment said this:

> Nothing shows up in the editor or in a Windows build (Mono, no stripping) —
> so the bug this file protects against lives in the one configuration **NO
> TEST** in the project reaches.

What it protects is this: Android is compiled with
`ManagedStrippingLevel.High`, while content loading is entirely reflection
(`JsonConvert.DeserializeObject<T>`). High stripping can decide that members
only ever called through reflection are "unused" and delete them.

The comment's **prediction** was: what gets stripped is the DTO setters, and the
symptom is not a crash but a silent default — every field comes back zero/null,
validation rejects it, and the game drops to an error screen at startup. (§4
measured it and both were wrong; the danger was real but the mechanism was a
different one.)

The comment was right, and that is exactly why it was a problem: the protection
was **written by reasoning and never run.** On top of that, nobody had opened
the APK taken on 13 September until that day either. *A check that does not run
looks exactly like one that passes.*

---

## 3. Running the stripper without a device

The stripper can be run without a device too: a Windows build, with Android's
**compiler and stripper** — IL2CPP + High.

```
.\tools\unity\tour.ps1 -Build windows-il2cpp
```

`BuildPlayer.WindowsIl2cpp` sets this up and puts the setting back inside a
`finally`. Putting it back is not optional: if the setting stayed in the
project, every daily tour would get more expensive and we could pay that for
months without anyone noticing.

What is the same and matters: the same `link.xml`, the same `Lokanta.Content`
and `Newtonsoft.Json` assemblies, the same reflective loading — that is,
**managed stripping of our own assemblies.** What is not the same: stripping of
the engine modules varies by platform. This is not an Android test, it is a
**stripping test**.

### It was confirmed that the build really is IL2CPP

105 seconds was fast for IL2CPP, and "did it actually run" is precisely this
document's subject. The evidence is in the files:

| | windows (Mono) | windows-il2cpp |
|---|---|---|
| `GameAssembly.dll` | none | **43 MB** |
| `Lokanta_Data/il2cpp_data` | none | **present** |
| `Lokanta_Data/Managed/` | full | **empty** |
| `MonoBleedingEdge/` | present | none |

That stripping ran at High is also read out of `ProjectSettings.asset`:
`managedStrippingLevel: Android: 3` (= High), and after the restore
`Standalone: 0` (= off) — so the `finally` really did run.

---

## 4. It was measured that the protection really carries load

The tour passed — but on its own that proves nothing. If stripping caused no
trouble without the protection, the tour would still pass and `link.xml` would
be a pointless file. The two look the same from outside.

The only honest exam is **mutation**: `link.xml` was removed temporarily, the
same build was run through the same tour, and it was **expected** to break.

| | `link.xml` present | `link.xml` absent |
|---|---|---|
| tour | **129 passed, 0 failed** | could not even produce a summary |
| exit | 0 | crash |

The root cause is on line 31 of the player log:

```
could not load the content: JsonSerializationException: Unable to find a constructor
to use for type Lokanta.Content.EconomyDto
```

So `link.xml` really does carry load. Without that file the Android build would
have gone to the store as a game that **cannot load its own content**, and no
test would have caught it.

### The measurement corrected the comment

The expected symptom was "a silent default: every field comes back zero/null".
The real symptom is harsher: what gets stripped is not the property **setters**
but the DTOs' **constructors** — Newtonsoft cannot construct the object at all.

The consequence is worse than predicted too: the game **never reaches** the
error screen. When content fails to load it carries on in a half state, prints
"did not work" across eight checks, and dies with a `NullReferenceException`. So
what the player would see is not an explanatory error but an app that freezes at
startup.

The danger had been described correctly, the mechanism wrongly. The comment was
made to fit the measurement — not the other way round.

### Side finding: the exit crash may be specific to Mono

Mono tours have for a long time been closing with `0xC0000005` **after**
completing (outside managed code, not affecting the tour's result). Both full
runs of the IL2CPP build gave **exit code 0**.

Two runs are not proof, but for the first time there is a clue: the fault may be
in the Mono runtime's shutdown — that is, it may not concern the binary we ship
(IL2CPP) at all. Claiming more than that needs more runs.

---

## 5. What this does not close

Stripping is measured now, but **the ARM64 binary has still never run**. What is
left, and only a real device can give:

- ARM64 code generation (IL2CPP's x86_64 and ARM64 output are not the same)
- frame time and thermal throttling — a 60-day campaign is a long session
- touch: two-finger camera, 48 dp targets, real DPI and the notch
- the Android lifecycle: save integrity in an app backgrounded and then killed

All of these are one command with `tools/android/device.ps1` and a phone.
