using UnityEngine;

namespace SailingGame
{
    /// <summary>
    /// Simulates the ship's motion without Unity physics: the wind pushes the sail,
    /// the water resists the hull and the keel, and the rudder turns the ship.
    /// </summary>
    [RequireComponent(typeof(ShipInput))]
    public class ShipController : MonoBehaviour
    {
        private const float AirDensity = 1.225f; // kg/m^3.
        private const float MinWindSpeed = 0.01f; // m/s, avoids dividing by zero.
        private const float MaxDeltaTime = 1f / 30f; // Keeps the integration stable on frame spikes (in current state it is probably an overkill though).

        [Header("References")]
        [SerializeField] private Sail _sail;
        [SerializeField] private WindSource _wind;

        [Header("Sail controls")]
        [Tooltip("Opening speed, in percent per second.")]
        [SerializeField, Min(0f)] private float _openingSpeed = 25f;

        [Tooltip("Rotation speed, in degrees per second.")]
        [SerializeField, Min(0f)] private float _rotationSpeed = 30f;

        [Header("Ship")]
        [Tooltip("Ship mass, in kilograms.")]
        [SerializeField, Min(1f)] private float _mass = 2000f;

        [Header("Sail aerodynamics")]
        [Tooltip("Sail area at 100% opening, in square meters.")]
        [SerializeField, Min(0f)] private float _maxSailArea = 30f;

        [Tooltip("Peak lift coefficient of the sail.")]
        [SerializeField, Min(0f)] private float _maxLiftCoefficient = 1.4f;

        [Tooltip("Drag coefficient when the wind blows along the sail.")]
        [SerializeField, Min(0f)] private float _baseDragCoefficient = 0.05f;

        [Tooltip("Drag coefficient when the wind hits the sail head-on")]
        [SerializeField, Min(0f)] private float _maxDragCoefficient = 2.8f;

        [Header("Water resistance (N per m/s and N per (m/s)^2)")]
        [SerializeField, Min(0f)] private float _forwardLinearDrag = 150f;  // important for low velocity
        [SerializeField, Min(0f)] private float _forwardQuadraticDrag = 30f;    // important for higher velocity

        [Tooltip("The keel")]
        // Values are much higher than the forward drag, so the ship does not slide (mostly) sideways in certain cases.
        [SerializeField, Min(0f)] private float _lateralLinearDrag = 1500f;
        [SerializeField, Min(0f)] private float _lateralQuadraticDrag = 6000f;

        [Header("Rudder")]
        [Tooltip("Maximum turn rate, in degrees per second.")]
        [SerializeField, Min(0f)] private float _maxTurnRate = 25f; // the rate depends on the ship's velocity - we can't turn if we don't move.

        [Tooltip("How quickly the turn rate changes, in degrees per second squared.")]
        [SerializeField, Min(0.01f)] private float _yawAcceleration = 25f;

        [Tooltip("Forward speed (m/s) at which the rudder works at full strength.")]
        [SerializeField, Min(0.1f)] private float _rudderFullAuthoritySpeed = 2.5f;

        private ShipInput _input;
        private Vector3 _velocity;
        private float _headingDegrees;
        private float _yawRate;

        // Gets the ship velocity relative to the ground, in meters per second.
        public Vector3 Velocity => _velocity;

        // Gets the wind velocity as felt on the moving ship, in meters per second.
        public Vector3 ApparentWind { get; private set; }

        private void Awake()
        {
            _input = GetComponent<ShipInput>();
            _headingDegrees = transform.eulerAngles.y;
        }

        private void Update()
        {
            float deltaTime = Mathf.Min(Time.deltaTime, MaxDeltaTime);

            // Player input changes the sail first, so the forces below use the current adjustment.
            _sail.AdjustOpening(_input.SailOpening * _openingSpeed * deltaTime);
            _sail.AdjustAngle(_input.SailRotation * _rotationSpeed * deltaTime);

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            // The sail reacts to the wind as felt on the moving ship.
            ApparentWind = _wind.Velocity - _velocity;

            // Newton's second law.
            Vector3 force = CalculateSailForce() + CalculateWaterForce(forward, right); // Overall force vector.
            _velocity += force / _mass * deltaTime; // v += a * t

            UpdateHeading(Vector3.Dot(_velocity, forward), deltaTime);
            transform.position += _velocity * deltaTime;
        }

        private Vector3 CalculateSailForce()
        {
            float windSpeed = ApparentWind.magnitude;
            float sailArea = _maxSailArea * _sail.OpeningNormalized;
            if (windSpeed < MinWindSpeed || sailArea <= 0f)
            {
                return Vector3.zero;
            }

            // Direction in which the air moves relative to the ship.
            Vector3 flowDirection = ApparentWind / windSpeed;

            // The sail plane is vertical: the chord is the boom, the normal is perpendicular to it.
            Vector3 chord = transform.TransformDirection(_sail.LocalChordDirection);
            Vector3 normal = new(chord.z, 0f, -chord.x);

            // Sine of the angle of attack. Its sign tells which side of the sail the wind hits.
            float sinAttack = Vector3.Dot(flowDirection, normal);

            // Lift is perpendicular to the flow (magnitude: LiftCoefficient * sin(2 * alpha)), drag is parallel to the flow.
            // In () projection on the plane perpendicular to the flow. Magnitude of this vector is cos(alpha).
            Vector3 lift = 2f * _maxLiftCoefficient * sinAttack * (normal - sinAttack * flowDirection);
            float dragCoefficient = _baseDragCoefficient + _maxDragCoefficient * sinAttack * sinAttack;
            Vector3 drag = dragCoefficient * flowDirection;

            float dynamicPressure = 0.5f * AirDensity * windSpeed * windSpeed;
            return dynamicPressure * sailArea * (lift + drag);
        }

        private Vector3 CalculateWaterForce(Vector3 forward, Vector3 right)
        {
            float forwardSpeed = Vector3.Dot(_velocity, forward);
            float lateralSpeed = Vector3.Dot(_velocity, right);

            // Drag always opposes the motion and grows linearly and quadratically with speed.
            float forwardDrag = (_forwardLinearDrag + _forwardQuadraticDrag * Mathf.Abs(forwardSpeed)) * forwardSpeed;
            float lateralDrag = (_lateralLinearDrag + _lateralQuadraticDrag * Mathf.Abs(lateralSpeed)) * lateralSpeed;

            return -(forwardDrag * forward + lateralDrag * right);
        }

        private void UpdateHeading(float forwardSpeed, float deltaTime)
        {
            // The rudder needs water flowing past it - no speed means no steering,
            // and moving backwards reverses the steering.
            float rudderAuthority = Mathf.Clamp(forwardSpeed / _rudderFullAuthoritySpeed, -1f, 1f);
            float targetYawRate = _input.Steer * _maxTurnRate * rudderAuthority;

            // Limited angular acceleration simulates the rotational inertia of the hull.
            _yawRate = Mathf.MoveTowards(_yawRate, targetYawRate, _yawAcceleration * deltaTime);

            _headingDegrees = Mathf.Repeat(_headingDegrees + _yawRate * deltaTime, 360f);
            transform.rotation = Quaternion.Euler(0f, _headingDegrees, 0f);
        }
    }
}