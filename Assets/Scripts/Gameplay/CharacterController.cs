using UnityEngine;
using UnityEngine.InputSystem;

namespace TreasureHunters.Gameplay
{
    public class CharacterController : MonoBehaviour
    {
        [Tooltip("Specify the Character Body of the character.")]
        [SerializeField] private CharacterBody _characterBody;
        
        [Tooltip("Character's movement speed.")]
        [Min(0)]
        [SerializeField] private float _speed = 5;
        
        [Tooltip("The height of the character's jump.")]
        [Min(0)]
        [SerializeField] private float _jumpHeight = 2.5f;
        
        [Tooltip("Determines how many times the jump speed will be limited at the "
                 + "moment the jump button is released.")]
        [Min(1)]
        [SerializeField] private float _stopJumpFactor = 2.5f;
        
        [Tooltip("The time during which the character will be able to perform a jump,"
                 + " if possible. This allows the player to press the jump button"
                 + " slightly before the character lands on the surface.")]
        [Min(0)]
        [SerializeField] private float _jumpActionTime = 0.1f;

        [Tooltip("The time during which the character is still able to jump after losing"
                 + " the ground under their feet. This allows players to press the jump"
                 + " button slightly later after the character has lost the grounded "
                 + "state.")]
        [Min(0)]
        [SerializeField] private float _rememberGroundTime = 0.1f;
        
        private float _locomotionVelocity;
        private float _jumpSpeed;
        private bool _isJumping;
        private float _jumpActionEndTime;
        private float _lostGroundTime;
        private Vector3 _originalScale;

        public void OnMove(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
            _locomotionVelocity = value.x * _speed;
            
            // Change character's direction.
            if (value.x != 0)
            {
                var scale = _originalScale;
                scale.x = value.x > 0 ? _originalScale.x : -_originalScale.x;
                transform.localScale = scale;
            }
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                if (_characterBody.State == CharacterState.Grounded
                    || !_isJumping && _lostGroundTime > Time.unscaledTime)
                {
                    Jump();
                }
                else
                {
                    _jumpActionEndTime = Time.unscaledTime + _jumpActionTime;
                }
            }
            else if (context.canceled)
            {
                StopJumping();
            }
        }

        private void Awake()
        {
            _jumpSpeed = Mathf.Sqrt(2 * Physics2D.gravity.magnitude
                                      * _characterBody.GravityFactor
                                      * _jumpHeight);
            
            _originalScale = transform.localScale;
        }

        private void OnEnable()
        {
            _characterBody.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            _characterBody.StateChanged -= OnStateChanged;
        }

        private void Update()
        {
            _characterBody.SetLocomotionVelocity(_locomotionVelocity);
        }
        
        private void OnStateChanged(CharacterState previousState, CharacterState state)
        {
            if (state == CharacterState.Grounded)
            {
                OnGrounded();
            }
            else if (previousState == CharacterState.Grounded)
            {
                _lostGroundTime = Time.unscaledTime + _rememberGroundTime;
            }
        }

        private void OnGrounded()
        {
            _isJumping = false;

            if (_jumpActionEndTime > Time.unscaledTime) 
            {
                _jumpActionEndTime = 0;
                Jump();
            }
        }

        private void Jump()
        {
            _isJumping = true;
            _characterBody.Jump(_jumpSpeed);
        }

        private void StopJumping()
        {
            _jumpActionEndTime = 0;
            var velocity = _characterBody.Velocity;
            
            if (_isJumping && velocity.y > 0)
            {
                _characterBody.Velocity = new Vector2(
                    velocity.x, 
                    velocity.y / _stopJumpFactor);
            }
        }
    }
}
