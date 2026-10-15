using UnityEngine;

namespace SailingGame
{
    /// <summary>
    /// Camera that follows its target without rotating: only the position changes.
    /// The viewing angle is set once, in the camera's Transform.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        [Tooltip("Distance from the target along the camera's view direction, in meters.")]
        [SerializeField, Min(1f)] private float _distance = 30f;

        [Tooltip("Approximate time the camera needs to catch up with the target, in seconds.")]
        [SerializeField, Min(0f)] private float _smoothTime = 0.3f;

        private Vector3 _velocity;

        private void Start()
        {
            // Begin exactly at the desired position, so the camera does not fly in at the start.
            if (_target != null)
            {
                transform.position = CalculateDesiredPosition();
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, CalculateDesiredPosition(), ref _velocity, _smoothTime);
        }

        // The camera sits behind the target along its own, fixed view direction.
        private Vector3 CalculateDesiredPosition()
        {
            return _target.position - transform.forward * _distance;
        }
    }
}