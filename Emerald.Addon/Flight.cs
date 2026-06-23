using System;
using System.Linq;
using Emerald.Runtime;
using UnityEngine;

namespace Emerald.Addon
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class Flight : MonoBehaviour
    {
        private ScriptHost _program;

        private void Start()
        {
            TryReloadProgram();
        }

        private void OnDestroy()
        {
            _program?.Dispose();
        }

        private void FixedUpdate()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                ScreenMessages.PostScreenMessage("[Emerald] Recompiling program... ");
                TryReloadProgram();
            }

            TryTickProgram();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void TryReloadProgram()
        {
            try
            {
                // Build the replacement first; only tear the running program down once the new one
                // has compiled, so a compile error leaves the current program running.
                var next = CreateProgram();
                _program?.Dispose();
                _program = next;
            }
            catch (Exception e)
            {
                ScreenMessages.PostScreenMessage("[Emerald] Load failed: " + e);
                Debug.Log("[Emerald] Load failed: " + e);
            }
        }

        private static ScriptHost CreateProgram()
        {
            // Discover formatters, commands and services across every loaded Emerald.Runtime.*
            // assembly. KSP loads all GameData DLLs at startup, so they are already in the AppDomain —
            // any new Emerald.Runtime.* assembly is picked up automatically, no list to maintain here.
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("Emerald.Runtime", StringComparison.Ordinal))
                .ToArray();

            var commands = CommandRegistry.FromAssemblies(assemblies);
            var services = ServiceRegistry.FromAssemblies(assemblies);

            var path = ExpandHome("~/work/ksp-script/main.rb");
            return new ScriptHost(path, commands, services);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void TryTickProgram()
        {
            try
            {
                _program?.Tick();
            }
            catch (Exception e)
            {
                ScreenMessages.PostScreenMessage("[Emerald] Execution error: " + e);
                Debug.Log("[Emerald] Execution error: " + e);
            }
        }

        private static string ExpandHome(string path)
        {
            if (string.IsNullOrEmpty(path) || path[0] != '~')
                return path;

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return home + path.Substring(1);
        }
    }
}
