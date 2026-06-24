using System;
using System.Linq;
using Emerald.Runtime;
using Emerald.Runtime.Commands;
using Emerald.Runtime.Services;
using Emerald.Runtime.Execution;
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
            // Discover command controllers and services from any assembly with [assembly: EmeraldExtension]
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.IsDefined(typeof(EmeraldExtensionAttribute), false))
                .ToArray();

            var commands = CommandRegistry.FromAssemblies(assemblies);
            var services = ServiceRegistry.FromAssemblies(assemblies);

            // TODO:
            // - Let user define script location
            // - Think about script model? e.g. kOS-like where scripts can live on the controller (stored in game data)
            //   or on "at the KSC" (stored in your file system)
            // - Placeable controller object in KSP
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
