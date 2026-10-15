using System;
using UnityEngine;

namespace SailingGame
{
    /// <summary>
    /// Holds the sail state (opening and rotation) and keeps the sail visuals in sync with it.
    /// Attached to the sail pivot, which is placed on the mast axis.
    /// </summary>
    public class Sail : MonoBehaviour
    {
        // <summary> Maximum sail opening, in percent.
        public const float MaxOpening = 100f;

        // Maximum sail rotation to either side, in degrees.
        public const float MaxAngleDegrees = 45f;

        // Keeps the visual sail from collapsing to zero length.
        private const float MinVisualScale = 0.02f;

        private float _opening;
        private float _angleDegrees;

        // Raised when the opening changes. The argument is the new opening in percent.
        public event Action<float> OpeningChanged;

        // Raised when the rotation changes. The argument is the new angle in degrees.
        public event Action<float> AngleChanged;

        // Gets the sail opening in percent.
        public float Opening => _opening;

        // Gets the sail opening as a fraction.
        public float OpeningNormalized => _opening / MaxOpening;

        // Gets the sail rotation in degrees. Positive values swing the boom to the right.
        public float AngleDegrees => _angleDegrees;

        // Gets the direction of the boom (from the mast towards the trailing edge) in the ship's local space.
        public Vector3 LocalChordDirection => Quaternion.Euler(0f, -_angleDegrees, 0f) * Vector3.back;

        private void Awake()
        {
            ApplyVisuals();
        }

        // Changes the sail opening by the given amount (in percent).
        public void AdjustOpening(float delta)
        {
            float newOpening = Mathf.Clamp(_opening + delta, 0f, MaxOpening);
            if (Mathf.Approximately(newOpening, _opening))
            {
                return; // The sail opening hasn't changed.
            }

            _opening = newOpening;
            ApplyVisuals();
            OpeningChanged?.Invoke(_opening);
        }

        // Rotates the sail by the given amount (in degrees), clamped to +/- 45.
        public void AdjustAngle(float delta)
        {
            float newAngle = Mathf.Clamp(_angleDegrees + delta, -MaxAngleDegrees, MaxAngleDegrees);
            if (Mathf.Approximately(newAngle, _angleDegrees))
            {
                return; // The sail rotation hasn't changed.
            }

            _angleDegrees = newAngle;
            ApplyVisuals();
            AngleChanged?.Invoke(_angleDegrees);
        }

        private void ApplyVisuals()
        {
            // The boom points backwards, so a positive Unity Y-axis rotation would swing it to the left.
            // The negation makes positive angles mean "to the right". Just a cosmetic thing.
            transform.localRotation = Quaternion.Euler(0f, -_angleDegrees, 0f);

            // The sail unfurls along the boom (local Z), keeping its front edge at the mast.
            Vector3 scale = transform.localScale;
            scale.z = Mathf.Max(OpeningNormalized, MinVisualScale);
            transform.localScale = scale;
        }
    }
}