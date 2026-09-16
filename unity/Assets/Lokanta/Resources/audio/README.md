# Audio files go here

`Sfx.Init` looks in this folder **first**: if `tik.ogg` is here it plays that,
otherwise it falls back to the synthesised tone in `Sfx.cs`. So adding a file
requires no code change, and while the folder is empty the game runs complete.

The file **name** matters, not the extension. The thirteen expected names
(Turkish, because they are the literal strings `Sfx.cs` looks for):

```text
tik  onay  iptal  para  kapi-zili
cizirti  dokme  kizgin  seviye  gun-donumu
uyari  bitti  kombo
```

`tools/check_licenses.py` reads those names out of `Sfx.cs` and fails if this
list or the ledger table has fallen behind - the first two of the three above
were added to the code with both lists left saying "ten".

Which sound each one should be, and the licence rule: the "Audio" section of
`Art/ATTRIBUTION.md`. In short: only **CC0** or a source open to commercial use
with a clear licence, and the licence text goes both to
`Art/<folder>/License.txt` and under `Resources/licenses/`.

This file does not go into the build (a `.md` is not an `AudioClip`); leaving it
here is harmless.
