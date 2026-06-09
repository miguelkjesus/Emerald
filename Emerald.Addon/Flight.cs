using System;
using System.Linq;
using Emerald.Runtime;
using UnityEngine;

namespace Emerald.Addon
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class Flight : MonoBehaviour
    {
        private Program _program;

        private void Start()
        {
            try
            {
                var path = ExpandHome("~/work/ksp-script/main.rb");

                _program = new Program(path, Capabilities.DebugLog | Capabilities.ReadVesselTelemetry);

                _program.CommandRegistry.AddFromService(new Commands.Debug());
                _program.CommandRegistry.AddFromService(new Commands.Vessel.Telemetry());
            }
            catch (Exception e)
            {
                Debug.LogError("[Emerald] startup failed: " + e);
            }
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.F)) return;

            ScreenMessages.PostScreenMessage("Addon: Pressed F");

            if (_program == null)
            {
                ScreenMessages.PostScreenMessage("Addon: program not loaded.");
                return;
            }

            try
            {
                var commandIds = _program.CommandRegistry.Commands.Select(c => c.Id);
                ScreenMessages.PostScreenMessage(string.Join(", ", commandIds));
                _program.Execute();
            }
            catch (Exception e)
            {
                Debug.LogError("[Emerald] execute failed: " + e);
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