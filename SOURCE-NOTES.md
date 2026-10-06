# Building/remixing these sources

Source-only archive, MIT copyright 2026 roryk. Game assets and SDK excerpts are excluded.

Use the installed R.E.P.O. managed assemblies and BepInEx 5 reference DLLs, .NET SDK (tested 9.0.203) and Python 3. Adjust GamePath in UltraHaul.csproj for a different Steam library. Build.ps1 accepts -Python with your interpreter path and runs preflight before compilation.

The hook check also expects local ILSpy excerpts under ../reference, named by each row's source in Sheets/hooks.json. Regenerate only those targeted classes from your own Assembly-CSharp.dll; do not redistribute them. This lab used ILSpy 9.1.0.7988 and universal-modder from https://github.com/rehan-remade/universal-modder. Native input checks additionally use that toolkit's um.win module and the development bridge; players need neither.

Sheets are the source of truth: change them before source code. Tools/preflight.py validates every defined cell/reference and generates one C# struct per row. Definitions are fully populated; co-op and several playtest behaviors remain unverified, as README states. Controls and movement checks were passed separately; there is no claimed combined end-to-end playthrough.
