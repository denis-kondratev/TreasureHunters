using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TreasureHunters.Gameplay
{
    public class CharacterController : MonoBehaviour
    {
        [SerializeField] private CharacterBody _characterBody;
        
        [Min(0)]
        [SerializeField] private float _speed = 5;
        
        [Min(0)]
        [SerializeField] private float _jumpHeight = 2.5f;
        
        [Min(1)]
        [SerializeField] private float _stopJumpFactor = 2.5f;
        
        [Min(0)]
        [SerializeField] private float _jumpActionTime = 0.1f;

        [Min(0)]
        [SerializeField] private float _rememberGroundTime = 0.1f;
        
        private float _locomotionVelocity;
        private float _jumpSpeed;
        private bool _isJumping;
        private float _jumpActionEndTime;
        private float _lostGroundTime;

        public void OnMove(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
            _locomotionVelocity = value.x * _speed;
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
