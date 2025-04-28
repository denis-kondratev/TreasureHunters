using System;
using Bitpatch.CharacterController;
using UnityEngine;

namespace TreasureHunters.Gameplay
{
    public class CharacterAnimatorController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private CharacterBody _body;
        [SerializeField] private CharacterController _controller;

        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int VerticalVelocityParam = Animator.StringToHash("VerticalVelocity");
        private static readonly int IsAirborneParam = Animator.StringToHash("IsAirborne");
        private static readonly int JumpParam = Animator.StringToHash("Jump");

        private void OnEnable()
        {
            _controller.Jumped += OnJump;
        }

        private void OnDisable()
        {
            _controller.Jumped -= OnJump;
        }

        private void Update()
        {
            _animator.SetFloat(SpeedParam, Mathf.Abs(_body.Velocity.x));
            _animator.SetFloat(VerticalVelocityParam, _body.Velocity.y);
            _animator.SetBool(IsAirborneParam, _body.State == CharacterState.Airborne);
        }
        
        private void OnJump()
        {
            _animator.SetTrigger(JumpParam);
            _animator.SetBool(IsAirborneParam, true);
        }
    }
}
