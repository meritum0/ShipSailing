using UnityEngine;
using UnityEngine.InputSystem;

namespace SailingGame
{
    /// <summary>
    /// Receives player input from the generated ShipControls class
    /// and exposes it as simple axis values.
    /// </summary>
    public class ShipInput : MonoBehaviour, ShipControls.IShipActions
    {
        private ShipControls _controls;

        // Gets the rudder input in [-1, 1]: negative turns left, positive turns right.
        public float Steer { get; private set; }

        // Gets the sail opening input in [-1, 1]: positive opens the sail.
        public float SailOpening { get; private set; }

        // Gets the sail rotation input in [-1, 1]: positive rotates the sail to the right.
        public float SailRotation { get; private set; }

        private void Awake()
        {
            _controls = new ShipControls();
            _controls.Ship.AddCallbacks(this);
        }

        private void OnEnable()
        {
            _controls.Ship.Enable();
        }

        private void OnDisable()
        {
            _controls.Ship.Disable();
        }

        private void OnDestroy()
        {
            _controls.Dispose();
        }

        public void OnSteer(InputAction.CallbackContext context)
        {
            Steer = ReadAxis(context);
        }

        public void OnSailOpening(InputAction.CallbackContext context)
        {
            SailOpening = ReadAxis(context);
        }

        public void OnSailRotation(InputAction.CallbackContext context)
        {
            SailRotation = ReadAxis(context);
        }

        private static float ReadAxis(InputAction.CallbackContext context)
        {
            // Releasing the keys cancels the action, which means no input.
            return context.canceled ? 0f : context.ReadValue<float>();
        }
    }
}