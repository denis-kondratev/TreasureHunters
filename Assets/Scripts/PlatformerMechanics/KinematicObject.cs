using System;
using UnityEngine;

namespace TreasureHunters.PlatformerMechanics
{
    /// <summary>
    /// Foundational script for controlling the movement and state of game entities
    /// such as characters, enemies, and objects within a 2D platformer environment.
    /// This script provides a framework for handling different movement states
    /// (e.g., grounded, airborne) and  enables the controlled application of
    /// kinematic principles for movement, ensuring that entities can move smoothly
    /// along surfaces, jump, and fall, adhering to custom-defined physics rather
    /// than relying solely on the engine's physics system. Use this script as a
    /// base for any entity that requires precise movement control, including
    /// walking, jumping, and interacting with the game world's physics in a
    /// controlled manner.
    /// </summary>
    public class KinematicObject : MonoBehaviour
    {
        /// <summary>
        /// The Rigidbody2D of this object. It should be in Kinematic mode.
        /// </summary>
        [SerializeField] private Rigidbody2D _rigidbody2D;
        
        /// <summary>
        /// Coefficient that affects how gravity impacts the object.
        /// </summary>
        [SerializeField] private float _gravityFactor = 1f;

        /// <summary>
        /// Filter for casting against surfaces.
        /// </summary>
        [SerializeField] private ContactFilter2D _groundFilter;

        /// <summary>
        /// Maximum speed the object can move.
        /// </summary>
        [SerializeField] private float _maxSpeed = 30;

        /// <summary>
        /// Buffer size for casts.
        /// </summary>
        [SerializeField] private int _castBufferSize = 4;

        /// <summary>
        /// The minimum distance for which movement will be processed.
        /// </summary>
        [SerializeField] private float _minMoveDistance = 0.005f;
        
        /// <summary>
        /// Distance to maintain from surfaces to prevent sticking.
        /// </summary>
        [SerializeField] private float _safeDistance = 0.01f;

        /// <summary>
        /// The velocity's role may slightly vary depending on the object's
        /// <see cref="KinematicState"/>. For instance, when in the
        /// <see cref="KinematicState.Grounded"/> state, the <c>x</c> value
        /// represents the speed of movement along the surface. Conversely, in the
        /// <see cref="KinematicState.Airborne"/> state, the velocity corresponds
        /// to the object's speed in 2D space.
        /// </summary> 
        [SerializeField] private Vector2 _velocity;

        /// <summary>
        /// The minimum dot product value of the surface normal on the opposite
        /// of the normalized gravity vector required for an object to be considered
        /// standing on a surface. If the value is greater than 0 but less than this
        /// threshold, the character should slide along the surface.
        /// </summary>
        [SerializeField] private float _minGroundNormalVertical = 0.5f;

        /// <summary>
        /// Defines the states in which the object can exist. The object's movement
        /// type changes based on its current state.
        /// </summary>
        [field: SerializeField]
        public KinematicState KinematicState { get; private set; }

        /// <summary>
        /// The velocity of the object. Limits the magnitude of velocity to the
        /// maximum speed if it exceeds it.
        /// </summary>
        public Vector2 Velocity
        {
            get => _velocity;

            private set => _velocity = value.sqrMagnitude > _sqrMaxSpeed
                ? value.normalized * _maxSpeed
                : value;
        }

        /// <summary>
        /// Stores cast result for this object. Avoids memory allocation on every
        /// cast.
        /// </summary>
        private RaycastHit2D[] _castBuffer;
        
        /// <summary>
        /// The square of the minimum movement distance. Used to avoid taking the
        /// square root when calculating the magnitude of the movement vector,
        /// thereby avoiding unnecessary calculations. We will simply compare the
        /// squares of distances.
        /// </summary>
        private float _sqrMinMoveDistance;

        /// <summary>
        /// The square of the maximum speed the object can move at.
        /// </summary>
        private float _sqrMaxSpeed;

        /// <summary>
        /// The normal of the surface the object is on.
        /// </summary>
        private Vector2 _groundNormal;

        /// <summary>
        /// The normalized gravity vector.
        /// </summary>
        private Vector2 _normalizedGravity;
        
        private void Awake()
        {
            _castBuffer = new RaycastHit2D[_castBufferSize];
            _sqrMinMoveDistance = _minMoveDistance * _minMoveDistance;
            _sqrMaxSpeed = _maxSpeed * _maxSpeed;
        }

        private void FixedUpdate()
        {
            // Recalculate the normalized value of the gravity vector in case it
            // has changed.
            _normalizedGravity = Physics2D.gravity.normalized;
            
            // Attempt to ground the object if possible.
            GroundIfPossible();
            
            if (KinematicState == KinematicState.Airborne)
            {
                // If the object is in the Airborne state, account for the impact
                // of gravity.
                var gravityImpaction =
                    Time.fixedDeltaTime * _gravityFactor * Physics2D.gravity;
                Velocity = _velocity + gravityImpaction;
            }
            
            // Calculate the desired displacement vector for this update cycle.
            var displacement = _velocity * Time.fixedDeltaTime;

            if (displacement.sqrMagnitude >= _sqrMinMoveDistance)
            {
                // If the displacement is too small, it won't be executed.
                // Otherwise, proceed with moving the object.
                PerformMotion(displacement);
            }
        }
        
        /// <summary>
        /// If the object is on a surface, sets
        /// <see cref="KinematicState.Grounded"/> and the surface normal
        /// <see cref="_groundNormal"/> of the object.
        /// </summary>
        private void GroundIfPossible()
        {
            // Find the vertical component of the object's velocity. We reserve the
            // right to change the direction of gravity, for instance, to simulate
            // swaying on a ship. This can be achieved by rotating the camera and
            // tilting the gravity vector. Therefore, to obtain the vertical
            // component of velocity, we need to calculate the dot product of the
            // velocity vector with the normalized vector of gravity,
            // rather than simply taking the y-value of the vector.
            var verticalSpeed = Vector2.Dot(_normalizedGravity, _velocity);

            // If verticalSpeed is not zero, it means the object is either moving
            // upwards or downwards, and thus is in the Airborne state.
            if (Mathf.Approximately(verticalSpeed, 0))
            {
                // Even if the object currently lacks vertical speed, you must still
                // ensure it has a surface to stand on. For this, we cast a ray.
                // The cast distance is calculated as the distance the object can
                // travel in one update cycle under the influence of gravity,
                // plus the _safeDistance buffer.
                var groundCastDistance = 
                    _gravityFactor 
                    * Time.fixedDeltaTime 
                    * Physics2D.gravity.magnitude 
                    + _safeDistance;

                // Search for a hit with the surface. If a hit is found and the
                // upward vertical component of the surface is sufficient for the
                // surface to be considered stable, the object is deemed to be in
                // the Grounded state.
                if (TryGetHit(Physics2D.gravity, groundCastDistance, out var hit)
                    && Vector2.Dot(hit.normal, -_normalizedGravity) 
                        >= _minGroundNormalVertical)
                {
                    // Save the surface normal. It will be useful for correctly
                    // calculating the object's speed when moving along the surface.
                    _groundNormal = hit.normal;
                    KinematicState = KinematicState.Grounded;
                    return;
                }
            }

            // If the above conditions are not met, the object is in the Airborne
            // state. We also reset the surface normal.
            _groundNormal = Vector2.zero;
            KinematicState = KinematicState.Airborne;
        }

        /// <summary>
        /// Attempts to find a hit with the surface in the direction of movement.
        /// </summary>
        /// <param name="direction">The direction of the cast.</param>
        /// <param name="distance">The distance of the cast.</param>
        /// <param name="hit">Returns the hit if one exists.</param>
        private bool TryGetHit(Vector2 direction,
                               float distance,
                               out RaycastHit2D hit)
        {
            var hitCount = _rigidbody2D.Cast(direction, _groundFilter,
                _castBuffer, distance);
            var hasHit = hitCount > 0;
            hit = hasHit ? GetNearestHit(hitCount) : new RaycastHit2D();
            return hasHit;
        }

        /// <summary>
        /// Finds the nearest hit to this object from those stored in the buffer.
        /// </summary>
        /// <param name="hitCount">The number of hits from the last cast.</param>
        /// <exception cref="ArgumentException">The value of
        /// <paramref name="hitCount"/> must be greater than zero and not exceed
        /// the size of the <see cref="_castBuffer"/>.</exception>
        private RaycastHit2D GetNearestHit(int hitCount)
        {
            if (hitCount <= 0 || hitCount > _castBuffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(hitCount), hitCount,
                    "Value cannot be zero ot grater than buffer size.");
            }

            var result = _castBuffer[0];
            
            for (var i = 1; i < hitCount; i++)
            {
                if (_castBuffer[i].distance < result.distance)
                {
                    result = _castBuffer[i];
                }
            }

            return result;
        }

        /// <summary>
        /// Calculates the object's displacement along the
        /// <paramref name="displacement"/> vector. Modifies the object's velocity
        /// <see cref="Velocity"/> if a collision occurs.
        /// </summary>
        private void PerformMotion(Vector2 displacement)
        {
            // Search for obstacles in the object's path, taking into account
            // the value of _safeDistance.
            if (TryGetHit(displacement,
                    displacement.magnitude + _safeDistance, out var hit))
            {
                // If a hit is found, limit the object's displacement vector up to
                // the point of collision, subtracting _safeDistance to maintain
                // a buffer distance between the object's collider and the surface
                // collider.
                displacement = (hit.distance - _safeDistance)
                               * displacement.normalized;
                // Also, adjust the velocity based on the collision with the surface.
                Velocity = ClipCollisionVector(_velocity, hit.normal);
            }

            // Directly execute the object's displacement.
            var position = _rigidbody2D.position + displacement;
            _rigidbody2D.MovePosition(position);
        }

        /// <summary>
        /// Damps the vector component of the collision with a surface.
        /// </summary>
        /// <param name="vector">The vector to operate on.</param>
        /// <param name="surfaceNormal">The normal vector of the collision
        /// surface.</param>
        private Vector2 ClipCollisionVector(Vector2 vector, Vector2 surfaceNormal)
        {
            var dampenedValue = Vector2.Dot(vector, surfaceNormal) 
                                * surfaceNormal;
            return vector - dampenedValue;
        }
    }
}
