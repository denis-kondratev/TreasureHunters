using UnityEngine;

namespace TreasureHunters.Gameplay
{
    public class CaracterAnimatorController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private CharacterBody _body;

        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int VerticalVelocityParam = Animator.StringToHash("VerticalVelocity");
        private static readonly int IsAirborneParam = Animator.StringToHash("IsAirborne");

        private void Update()
        {
            _animator.SetFloat(SpeedParam, Mathf.Abs(_body.Velocity.x));
            _animator.SetFloat(VerticalVelocityParam, _body.Velocity.y);
            _animator.SetBool(IsAirborneParam, _body.State == CharacterState.Airborne);
        }
    }
}
