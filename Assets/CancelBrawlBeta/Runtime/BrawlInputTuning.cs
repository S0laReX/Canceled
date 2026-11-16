using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Stick shaping for SYS-026: radial deadzone, rescale to full range and response curve.</summary>
    public static class BrawlInputTuning
    {
        public const float MinDeadzone = .05f, MaxDeadzone = .5f;
        public const float MinResponse = .5f, MaxResponse = 2f;
        /// <summary>Physical sticks rarely reach exactly 1; anything past this counts as full deflection.</summary>
        public const float OuterLimit = .95f;

        /// <param name="raw">Unprocessed stick value.</param>
        /// <param name="deadzone">Radius ignored as noise/drift.</param>
        /// <param name="response">1 = linear; above 1 reaches high speed with less travel; below 1 gives finer slow control.</param>
        public static Vector2 Shape(Vector2 raw, float deadzone, float response)
        {
            float magnitude = raw.magnitude;
            if (magnitude <= deadzone || magnitude < 1e-5f) return Vector2.zero;
            float scaled = Mathf.Clamp01((magnitude - deadzone) / Mathf.Max(.01f, OuterLimit - deadzone));
            scaled = Mathf.Pow(scaled, 1f / Mathf.Clamp(response, MinResponse, MaxResponse));
            return raw / magnitude * scaled;
        }
    }
}
