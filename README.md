# Emerald

A Ruby scripting runtime for **Kerbal Space Program**. You write vessel automation in
Ruby (mruby, embedded via [ChibiRuby](https://www.nuget.org/packages/MiguelJesus.ChibiRuby)),
and Emerald exposes the game to your script through **commands** written in C#.

The command set is **extensible**: anyone can ship a DLL of extra commands, services and
formatters, drop it into KSP's `GameData`, and have it auto-register — no change to Emerald
itself. This document focuses on writing those extensions.

## Solution layout

| Project | Role |
|---|---|
| **Emerald.Runtime** | The engine/framework. Command framework, mruby host, CLR⇄Ruby marshalling. **KSP-agnostic** — it does not reference KSP's `Assembly-CSharp`. |
| **Emerald.Builtins** | The built-in commands/services/formatters. This is just an extension that ships in the box. |
| **Emerald.Addon** | The KSP bootstrap (`KSPAddon` `MonoBehaviour`): loads the script, discovers extensions, ticks the program each frame. |

An **extension** is any assembly that references `Emerald.Runtime` and is marked with
`[assembly: EmeraldExtension]`. `Emerald.Builtins` is the reference example.

## How scripting works

The addon compiles a single Ruby entrypoint (currently hardcoded to
`~/work/ksp-script/main.rb` in `Emerald.Addon/Flight.cs`) into one mruby VM and resumes it
as a fiber every `FixedUpdate`. Press **`` ` `` (backquote)** in flight to recompile and
reload; a compile error leaves the previous program running.

Each command is exposed as a method on the `Emerald` module, called with keyword arguments:

```ruby
Emerald.command_slug(keyword: value, other: value)
```

All command arguments are **keyword arguments**. Calling an undefined command raises a normal
Ruby `NoMethodError`. In practice you wrap the raw calls in friendly Ruby methods (a prelude), e.g.:

```ruby
def altitude(vessel = 0)
  Emerald.vessel_altitude(vessel_index: vessel)
end

def set_throttle(value)
  Emerald.set_throttle(throttle: value)
end
```

## Writing an extension

### 1. Create a class library

Target `net472`, reference `Emerald.Runtime`, and reference whatever KSP assemblies your
commands need (`Assembly-CSharp`, `UnityEngine`, …). The simplest start is to copy
`Emerald.Builtins/Emerald.Builtins.csproj` and adjust the name, GUID and `<Compile>` items.

### 2. Mark the assembly

In `Properties/AssemblyInfo.cs` (or any file):

```csharp
using Emerald.Runtime;

[assembly: EmeraldExtension]
```

This is what makes the assembly discoverable — see [How discovery works](#how-discovery-works).

### 3. Add commands, services and/or formatters

#### Commands

A command is a method marked `[Command]` on a `CommandController` subclass. The class needs a
public parameterless constructor; **a fresh instance is created for every invocation**, so
the controller is its own per-call context (it exposes `State`, `Slug`, `Service<T>()` and
`Raise()`).

```csharp
using Emerald.Runtime;

public sealed class VesselTelemetry : CommandController
{
    // Slug defaults to the snake_case of the method name; pass one to override.
    [Command("vessel_altitude")]
    private double GetAltitude(int vesselIndex)
        => FlightGlobals.Vessels[vesselIndex].altitude;
}
```

- **The slug is the method name** on `Emerald.Commands`, so it must be a valid Ruby method name and must
  not shadow a module built-in (e.g. `class`, `name`, `send`). Duplicate slugs are rejected at startup.
- **Arguments bind by keyword.** Each parameter's Ruby keyword is the **snake_case** of its
  name (`vesselIndex` → `vessel_index`). Call it as `Emerald.Commands.vessel_altitude(vessel_index: 0)`.
- **Defaults are honored.** A C# default value makes the keyword optional; a missing required
  keyword raises a Ruby error, as does an unknown keyword.
- **Return values are marshalled back to Ruby** (`void`/`null` → `nil`). Custom types need a
  formatter (below).
- Resolve a long-lived service with `Service<T>()`; raise a Ruby `StandardError` with
  `Raise("message")`.

#### Services

Controllers are transient, so anything that must persist across calls (event subscriptions,
accumulated state) lives in a **service**: a class marked `[CommandService]` with a public
parameterless constructor. It is constructed once at startup, resolved via `Service<T>()`,
and disposed when the program reloads (implement `IDisposable` to clean up).

```csharp
using System;
using Emerald.Runtime;

[CommandService]
public sealed class FlyByWireService : IDisposable
{
    public float? Throttle;
    public void Start() => FlightGlobals.ActiveVessel.OnFlyByWire += OnFlyByWire;
    public void Dispose() => FlightGlobals.ActiveVessel.OnFlyByWire -= OnFlyByWire;
    private void OnFlyByWire(FlightCtrlState s) { if (Throttle != null) s.mainThrottle = Throttle.Value; }
}

public sealed class VesselControl : CommandController
{
    private FlyByWireService FlyByWire => Service<FlyByWireService>();

    [Command("set_throttle")]
    private void SetThrottle(float throttle) => FlyByWire.Throttle = throttle;
}
```

#### Formatters

To pass a custom CLR type to/from Ruby, derive `MRubyObjectFormatter<T>` and mark it
`[MRubyFormatter]`. You declare the Ruby class name and how to map the type to keyword
fields and back. **The Ruby class itself must already be defined by your script/prelude** —
serialization raises if it is missing.

```csharp
using System.Collections.Generic;
using ChibiRuby;
using ChibiRuby.Serializer;
using Emerald.Runtime;

[MRubyFormatter]
public sealed class Vector3DFormatter : MRubyObjectFormatter<Vector3d>
{
    protected override string ClassName => "Vector3"; // built as Vector3.new(x:, y:, z:)

    protected override IReadOnlyDictionary<string, MRubyValue> ToFields(Vector3d v, MRubyState mrb)
        => new Dictionary<string, MRubyValue> { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z };

    protected override Vector3d FromFields(MRubyState mrb, MRubyValue value)
        => new Vector3d(
            mrb.AsFloat(mrb.Send(value, mrb.Intern("x"))),
            mrb.AsFloat(mrb.Send(value, mrb.Intern("y"))),
            mrb.AsFloat(mrb.Send(value, mrb.Intern("z"))));
}
```

### 4. Build into GameData

Build the DLL into `GameData/Emerald/` (alongside `Emerald.Runtime.dll`). KSP loads it at
startup and the addon picks it up automatically. `Emerald.Builtins` is deployed this way by
the `DeployToGameData` target in `Emerald.Addon.csproj`.

## How discovery works

At program load, `Emerald.Addon/Flight.cs` scans every assembly already loaded into the
AppDomain for the marker attribute:

```csharp
var assemblies = AppDomain.CurrentDomain.GetAssemblies()
    .Where(a => a.IsDefined(typeof(EmeraldExtensionAttribute), false))
    .ToArray();

var commands = CommandRegistry.FromAssemblies(assemblies);
var services = ServiceRegistry.FromAssemblies(assemblies);
```

`CommandRegistry`/`ServiceRegistry` then reflect over those assemblies and register every
`CommandController` (its `[Command]` methods), every `[CommandService]`, and every
`[MRubyFormatter]`. Because KSP's `AssemblyLoader` loads **all** DLLs under `GameData` at
startup, a new extension DLL is in the AppDomain before the scan runs — so it registers with
no code change and no recompile of Emerald.

## Building

Built with Mono `msbuild` (KSP runs Unity's Mono):

```sh
msbuild Emerald.sln /restore /t:Build /p:Configuration=Debug
```

Building `Emerald.Addon` runs the `DeployToGameData` target, which copies the Emerald
assemblies and their third-party dependencies into KSP's `GameData/Emerald`, and the native
mruby library into KSP's Mono embed-runtime path.
