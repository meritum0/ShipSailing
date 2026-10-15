using UnityEngine;

namespace SailingGame
{
    /// <summary>
    /// Provides the true wind (relative to the ground) for the whole scene.
    /// </summary>
    public class WindSource : MonoBehaviour
    {
        [Tooltip("Direction the wind blows FROM, in degrees (0 = north / +Z-axis, 90 = east / +X-axis).")]
        [SerializeField, Range(0f, 360f)] private float _fromDegrees = 225f;

        [Tooltip("True wind speed in meters per second.")]
        [SerializeField, Min(0f)] private float _speed = 12f;

        // Gets the velocity of the air relative to the ground, in meters per second.
        public Vector3 Velocity
        {
            get
            {
                // The wind blows FROM the given bearing, so the air moves in the opposite direction.
                float radians = _fromDegrees * Mathf.Deg2Rad;
                return -_speed * new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
            }
        }
    }
}