using UnityEngine;

namespace SailingGame
{
    /// <summary>
    /// Rotates a small flag so that it streams downwind, according to the wind felt on the ship.
    /// </summary>
    public class WindVane : MonoBehaviour
    {
        [SerializeField] private ShipController _ship;

        private void LateUpdate()
        {
            Vector3 apparentWind = _ship.ApparentWind;
            if (apparentWind.sqrMagnitude < 0.0001f)
            {
                return;
            }

            // The flag's local +Z axis points in the direction the air is moving.
            transform.rotation = Quaternion.LookRotation(apparentWind, Vector3.up);
        }
    }
}