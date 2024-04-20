using System;
using UnityEngine;

namespace TreasureHunters.Gameplay
{
    public class CharacterBody : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody;
        
        [Min(0)]
        [field: SerializeField] public float GravityFactor { get; private set; } = 1f;
        
        [SerializeField] private LayerMask _solidLayers;
        
        [Min(0)]
        [SerializeField] private float _maxSpeed = 30;
        
        [Min(0)]
        [SerializeField] private float _surfaceAnchor = 0.05f;
        
        [Range(0, 90)]
        [SerializeField] private float _maxSlop = 45f;
        
        [SerializeField] private Vector2 _velocity;

        [field: SerializeField] private CharacterState _state;
        
        public Vector2 Velocity
        {
            get => _velocity;
            
            set => _velocity = value.sqrMagnitude > _sqrMaxSpeed
                ? value.normalized * _maxSpeed
                : value;
        }

        public CharacterState State
        {
            get => _state;

            private set
            {
                if (_state != value)
                {
                    var previousState = _state;
                    _state = value;
                    StateChanged?.Invoke(previousState, value);
                }
            }
        }
        
        private float _sqrMaxSpeed;

        private Rigidbody2D.SlideMovement _slideMovement;

        private float _minGroundVertical;

        public event Action<CharacterState, CharacterState> StateChanged;

        public void Jump(float jumpSpeed)
        {
            Velocity = new Vector2(_velocity.x, jumpSpeed);
            State = CharacterState.Airborne;
        }

        public void SetLocomotionVelocity(float locomotionVelocity)
        {
            Velocity = new Vector2(locomotionVelocity, _velocity.y);
        }
        
        private void Awake()
        {
            _minGroundVertical = Mathf.Cos(_maxSlop * Mathf.PI / 180f);
            _sqrMaxSpeed = _maxSpeed * _maxSpeed;
            _slideMovement = CreateSlideMovement();
        }
        
        private void FixedUpdate()
        {
            Velocity += Time.fixedDeltaTime * GravityFactor * Physics2D.gravity;
            
            var slideResults = _rigidbody.Slide(
                _velocity,
                Time.fixedDeltaTime, 
                _slideMovement);

            if (slideResults.slideHit)
            {
                Velocity = ClipVector(_velocity, slideResults.slideHit.normal);
            }
            
            if (_velocity.y <= 0 && slideResults.surfaceHit)
            {
                var surfaceHit = slideResults.surfaceHit;
                Velocity = ClipVector(_velocity, surfaceHit.normal);

                if (surfaceHit.normal.y >= _minGroundVertical)
                {
                    State = CharacterState.Grounded;
                    return;
                }
            }
            
            State = CharacterState.Airborne;
        }

        private Rigidbody2D.SlideMovement CreateSlideMovement()
        {
            return new Rigidbody2D.SlideMovement
            {
                maxIterations = 3,
                surfaceSlideAngle = 90,
                gravitySlipAngle = 90,
                surfaceUp = Vector2.up,
                surfaceAnchor = Vector2.down * _surfaceAnchor,
                gravity = Vector2.zero,
                layerMask = _solidLayers,
                useLayerMask = true,
            };
        }
        
        private static Vector2 ClipVector(Vector2 vector, Vector2 hitNormal)
        {
            return vector - Vector2.Dot(vector, hitNormal) * hitNormal;
        }
    }
}
