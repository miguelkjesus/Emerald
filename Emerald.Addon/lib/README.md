# Vendored runtime facades

These DLLs are bundled because a **net472** build does not emit them into this
project's output, yet KSP's Mono runtime needs them at load time to run the
**netstandard2.0** ChibiRuby assemblies. They are copied to `GameData/Emerald`
by the `DeployToGameData` target in `Emerald.Addon.csproj`.

## netstandard.dll

Must be the **runtime** facade, identity `netstandard, Version=2.0.0.0,
PublicKeyToken=cc7b13ffcd2ddd51`.

- Do NOT use the copy from `Microsoft.NETFramework.ReferenceAssemblies.*` or any
  `*-api/Facades` / `ref/` folder — those carry `[ReferenceAssembly]` and Mono
  refuses to load them for execution (`FileNotFoundException: Could not load ...
  netstandard, Version=2.0.0.0`).
- Do NOT use Mono's `lib/mono/4.5/Facades/netstandard.dll` — it is runtime but
  version **2.1.0.0**, which won't satisfy the strong-named 2.0.0.0 bind.
- Correct source (the shim MSBuild ships for net4x apps consuming netstandard2.0):
  `Microsoft.NET.Build.Extensions/net461/lib/netstandard.dll`
  (on this machine: `/Library/Frameworks/Mono.framework/Versions/<ver>/lib/mono/
  xbuild/Microsoft/Microsoft.NET.Build.Extensions/net461/lib/netstandard.dll`).

Verify: `monodis --assembly netstandard.dll` → Version 2.0.0.0, and
`monodis --customattr netstandard.dll | grep ReferenceAssembly` → no match.

## System.Runtime.CompilerServices.Unsafe.dll

Resolved as a framework assembly across the `ProjectReference` from
Emerald.Runtime, so it is dropped from this project's output. Vendored at the
version the build resolves (assembly 6.0.3.0, package 6.0.0).

## System.ValueTuple.dll

The matching companion to the net461 `netstandard.dll` above. ChibiRuby
(netstandard2.0) uses `System.ValueTuple`, and the net461 `netstandard.dll`
shim **type-forwards** `System.ValueTuple`1..n` to a standalone
`System.ValueTuple` assembly. A **net472** build keeps `ValueTuple` in
`mscorlib`, so it never emits this DLL — and KSP's Mono has no standalone
`System.ValueTuple` either. Without it, loading ChibiRuby fails with:

    System.Reflection.ReflectionTypeLoadException ...
    System.TypeLoadException: Could not resolve type with token ...
    (from typeref, class/assembly System.ValueTuple`1, netstandard, Version=2.0.0.0,
     PublicKeyToken=cc7b13ffcd2ddd51)

and every `new Program(...)` throws before MRuby is even touched.

- Use the **runtime** assembly from the SAME folder as `netstandard.dll`:
  `Microsoft.NET.Build.Extensions/net461/lib/System.ValueTuple.dll`
  (identity `System.ValueTuple, Version=4.0.2.0, PublicKeyToken=cc7b13ffcd2ddd51`).
- Do NOT use a `*-api/Facades/` copy — those carry `[ReferenceAssembly]` and
  Mono refuses to load them for execution.

Verify: `monodis --customattr System.ValueTuple.dll | grep ReferenceAssembly`
→ no match, and `monodis --typedef ... | grep -c ValueTuple` → non-zero (it
contains the real types, not just forwards).

## libmruby.dylib (NOT deployed to GameData)

Native MRuby compiler used by `ChibiRuby.Compiler` via `[DllImport("libmruby")]`
(`mrb_open`, `mrc_*`). It is **not** an MSBuild output of a net472 project (the
NuGet `runtimes/<rid>/native/` asset is ignored by non-SDK projects), so without
it `MRubyCompiler.Create()` fails to load the library.

- Source: `MiguelJesus.ChibiRuby.Compiler` package,
  `runtimes/osx-x64/native/libmruby.dylib` (KSP runs **x86_64**).
- `DeployToGameData` copies this to
  `KSP.app/Contents/Frameworks/MonoEmbedRuntime/osx/`, **not** GameData — the
  Player.log shows Mono's fallback handler probing exactly that path for
  `libmruby`. (Note: that path lives inside the app bundle and is wiped by Steam
  "Verify integrity of game files" / KSP updates; re-run the build to restore.)
- Self-contained: `otool -L` shows only `libSystem`. If a future macOS blocks it,
  clear quarantine with `xattr -d com.apple.quarantine libmruby.dylib`.
