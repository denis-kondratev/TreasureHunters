using System;
using UnityEngine;

namespace TreasureHunters.PlatformerMechanics
{
    /// <summary>
    /// Foundational script for controlling the movement and state of game
    /// entities such as characters, enemies, and objects within a 2D
    /// platformer environment. This script provides a framework for
    /// handling different movement states (e.g., grounded, airborne) and
    /// enables the controlled application of kinematic principles for movement,
    /// ensuring that entities can move smoothly along surfaces, jump,
    /// and fall, adhering to custom-defined physics rather than relying
    /// solely on the engine's physics system. Use this script as a base
    /// for any entity that requires precise movement control, including
    /// walking, jumping, and interacting with the game world's physics
    /// in a controlled manner.
    /// </summary>
    public class KinematicObject : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody2D;
        
        /// <summary>
        /// Коэффициет воздействия гравитации на объект.
        /// </summary>
        [SerializeField] private float _gravityFactor = 1f;

        /// <summary>
        /// Слои, которые расцениваются как земля. Если объекты находятся на
        /// коллайжерах принадлежащим этому слою, что объект будет считаться
        /// <see cref="KinematicState.Grounded"/>.
        /// </summary>
        [SerializeField] private ContactFilter2D _groundFilter;

        /// <summary>
        /// Максимальная скорость с которой может двигаться объект.
        /// </summary>
        [SerializeField] private float _maxSpeed = 30;

        /// <summary>
        /// Максимальное количество хитов, которое можно получить при raycast
        /// данного объекта.
        /// </summary>
        [SerializeField] private int _raycastBufferSize = 4;

        /// <summary>
        /// Минимальное расстояние, для которого будет осуществляться движение.
        /// </summary>
        [SerializeField] private float _minMoveDistance = 0.005f;

        /// <summary>
        /// The velocity's role may slightly vary depending on the object's
        /// <see cref="KinematicState"/>. For instance, when in the
        /// <see cref="KinematicState.Grounded"/> state, the <c>x</c> value
        /// represents the speed of movement along the surface. Conversely,
        /// in the <see cref="KinematicState.Airborne"/> state, the velocity
        /// corresponds to the object's speed in 2D space.
        /// </summary> 
        [SerializeField] private Vector2 _velocity;

        /// <summary>
        /// Defines the states in which the object can exist. The object's
        /// movement type changes based on its current state.
        /// </summary>
        [field: SerializeField]
        public KinematicState KinematicState { get; private set; }

        public Vector2 Velocity
        {
            get => _velocity;

            private set => _velocity = value.sqrMagnitude > _sqrMaxSpeed
                ? value.normalized * _maxSpeed
                : value;
        }

        /// <summary>
        /// Сюда будут складываться рейкасты данного объекта. Позволяет избежать
        /// эллокации памяти при каждом рейкасте.
        /// </summary>
        private RaycastHit2D[] _raycastBuffer;
        
        /// <summary>
        /// Минимальное расстояние, для которого будет осуществляться движение
        /// возведенное в квадрат. Необходимо для того, чтобы не извлекать
        /// квадратный корень при расчете модуля вектора движения, и тем
        /// самым избежать лишник вычислений. Будем просто сравнимать
        /// квадраты расстояний.
        /// </summary>
        private float _sqrMinMoveDistance;

        private float _sqrMaxSpeed;

        /// <summary>
        /// Нормаль поверхности, на которой находится объект.
        /// </summary>
        private Vector2 _groundNormal;
        
        private void Awake()
        {
            _raycastBuffer = new RaycastHit2D[_raycastBufferSize];
            _sqrMinMoveDistance = _minMoveDistance * _minMoveDistance;
            _sqrMaxSpeed = _maxSpeed * _maxSpeed;
        }

        private void FixedUpdate()
        {
            var gravityImpaction = 
                Time.fixedDeltaTime * _gravityFactor * Physics2D.gravity;
            Velocity = _velocity + gravityImpaction;
            var displacement = _velocity * Time.fixedDeltaTime;
            GroundIfPossible(ref displacement);
            
            switch (KinematicState)
            {
                case KinematicState.Grounded:
                    break;
                case KinematicState.Airborne:
                    PerformAirborneMotion(ref displacement);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Пытается установить объект на поверхность, как бы прижать к
        /// поверхности. Если по ходу движения <paramref name="displacement"/>
        /// действительно оказывается поверхность, то устанавливается состояние
        /// <see cref="KinematicState.Grounded"/> и выставляется нормаль
        /// поверхности. А вертикальная состовляющая значения
        /// <paramref name="displacement"/> сбрасывается. Также сбрасывает
        /// вертикальная составляющая скорости объекта.
        /// </summary>
        private void GroundIfPossible(ref Vector2 displacement)
        {
            KinematicState = KinematicState.Airborne;
            _groundNormal = Vector2.zero;
            var normalizedGravity = Physics2D.gravity.normalized;
            var downwardDistance = Vector2.Dot(displacement, normalizedGravity);

            if (downwardDistance < 0)
            {
                return;
            }

            var hitCount = _rigidbody2D.Cast(Physics2D.gravity,
                _groundFilter,
                _raycastBuffer,
                downwardDistance);

            if (hitCount == 0)
            {
                return;
            }
            
            var hit = GetNearestHit(hitCount);
            _groundNormal = hit.normal;
            var downwardDisplacement = normalizedGravity * hit.distance;
            Move(downwardDisplacement);
            Velocity = new Vector2(_velocity.x, 0);
            KinematicState = KinematicState.Grounded;
            displacement = new Vector2(displacement.x, 0);
        }

        /// <summary>
        /// Находит ближайший хит к данному объекту.
        /// </summary>
        /// <param name="hitCount">Колличество хитов полученное при последнем
        /// рейкасте.</param>
        /// <exception cref="ArgumentException">Значение
        /// <paramref name="hitCount"/> должно быть больше нуля, и не должно
        /// превышать размер <see cref="_raycastBuffer"/>.</exception>
        private RaycastHit2D GetNearestHit(int hitCount)
        {
            if (hitCount <= 0)
            {
                throw new ArgumentException("Value must be positive.",
                    nameof(hitCount));
            }

            if (hitCount > _raycastBuffer.Length)
            {
                throw new ArgumentException(
                    "Value cannot be grater than raycast buffer size.",
                    nameof(hitCount));
            }

            var result = _raycastBuffer[0];
            
            for (var i = 1; i < hitCount; i++)
            {
                if (_raycastBuffer[i].distance < result.distance)
                {
                    result = _raycastBuffer[i];
                }
            }

            return result;
        }

        private void PerformGroundedMotion(ref Vector2 displacement)
        {
            
        }

        /// <summary>
        /// Выполняет движение объекта, находящегося в свободном падении.
        /// </summary>
        /// <param name="displacement">Вектор, на который объект должен
        /// сместиться.</param>
        private void PerformAirborneMotion(ref Vector2 displacement)
        {
            var hitCount = _rigidbody2D.Cast(displacement,
                _groundFilter,
                _raycastBuffer,
                displacement.magnitude);

            if (hitCount > 0)
            {
                var hit = GetNearestHit(hitCount);
                DampenCollisionVelocity(hit.normal);
                displacement *= hit.fraction;
            }

            Move(displacement);
        }

        private void Move(Vector2 displacement)
        {
            if (displacement.sqrMagnitude < _sqrMinMoveDistance)
            {
                return;
            }
            
            var currentPosition = _rigidbody2D.position;
            var nextPosition = currentPosition + displacement;
            _rigidbody2D.MovePosition(nextPosition);
        }

        /// <summary>
        /// Гасит скорость движения объекта при столкновении с поверхностью.
        /// </summary>
        /// <param name="collisionSurfaceNormal">Вектор нормаль поверхности
        /// с которой произошло столкновение.</param>
        private void DampenCollisionVelocity(Vector2 collisionSurfaceNormal)
        {
            var neutralizedVelocity =
                Vector2.Dot(_velocity, collisionSurfaceNormal)
                * collisionSurfaceNormal;

            Velocity = _velocity - neutralizedVelocity;
        }
    }
}
