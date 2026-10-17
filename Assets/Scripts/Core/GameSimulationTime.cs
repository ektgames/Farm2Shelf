using System;
using UnityEngine;

namespace Farm2Shelf.Core
{
    /// <summary>Single owner of simulation speed and overlapping pause requests.</summary>
    public static class GameSimulationTime
    {
        [Flags]
        public enum PauseReason { Intro = 1, MainMenu = 2, PauseMenu = 4, GameOver = 8 }

        private static PauseReason pauses;
        public static int Speed { get; private set; } = 1;
        public static bool IsPaused => pauses != 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            pauses = 0;
            Speed = 1;
            Apply();
        }

        public static void SelectSpeed(int speed)
        {
            // A click must never resume a paused game, including a pause in the same frame.
            if (IsPaused || Time.timeScale <= 0f) return;
            Speed = Mathf.Clamp(speed, 1, 3);
            Apply();
        }

        public static void SetPaused(PauseReason reason, bool paused)
        {
            if (paused) pauses |= reason;
            else pauses &= ~reason;
            Apply();
        }

        public static void BeginSession()
        {
            Speed = 1;
            pauses &= PauseReason.MainMenu | PauseReason.Intro;
            Apply();
        }

        private static void Apply()
        {
            Time.timeScale = IsPaused ? 0f : Speed;
            // Keep the existing fixed timestep: physics retains its simulation accuracy.
            // deltaTime, Time.time, WaitForSeconds and normal animations share this clock.
        }
    }
}
