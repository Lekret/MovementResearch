//#define MB_DEBUG

using MenteBacata.ScivoloCharacterController;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace MenteBacata.ScivoloCharacterControllerDemo
{
    public enum MovementState
    {
        Moving,
        Dashing
    }
    
    public class ScivoloKinematicMovement : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float jumpSpeed = 8f;
        public float rotationSpeed = 720f;
        public float gravity = -25f;
        public float minVerticalSpeed = -12f;
        public float groundSpeedChangeRate = 15;
        public float airSpeedChangeRate = 15;
        public CharacterMover mover;
        public GroundDetector groundDetector;
        public MeshRenderer groundedIndicator;

        private const float timeBeforeUngrounded = 0.02f;
        private float nextUngroundedTime = -1f;
        private float verticalSpeed = 0;
        private Vector3 horizontalVelocity;
        private Transform cameraTransform;
        private MoveContact[] moveContacts = CharacterMover.NewMoveContactArray;
        private int contactCount;
        private bool isOnMovingPlatform = false;
        private MovingPlatform movingPlatform;
        private float dashTime = 0;
        private Vector3 dashDirection;
        private bool shouldMoveUp;
        
        private void Start()
        {
            cameraTransform = Camera.main.transform;
            mover.canClimbSteepSlope = true;
        }
        
        public void SetVelocity(Vector3 velocity)
        {
            verticalSpeed = velocity.y;
            velocity.y = 0;
            horizontalVelocity = velocity;
            shouldMoveUp = verticalSpeed > 0;
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            var movementInput = GetMovementInput();
            
            // Dash
            if (Input.GetKeyDown(KeyCode.LeftShift) &&
                dashTime <= 0 &&
                movementInput != Vector3.zero)
            {
                dashTime = 0.3f;
                dashDirection = movementInput.normalized;
            }

            var groundDetected = DetectGroundAndCheckIfGrounded(out bool isGrounded, out GroundInfo groundInfo);
            if (isGrounded)
            {
                horizontalVelocity = Vector3.Lerp(
                    horizontalVelocity,
                    movementInput * moveSpeed, 
                    Time.deltaTime * groundSpeedChangeRate);
            }
            else
            {
                var airMotion = movementInput * (deltaTime * airSpeedChangeRate);
                var airHorizontalVelocity = horizontalVelocity + airMotion;
                if (airHorizontalVelocity.sqrMagnitude <= moveSpeed * moveSpeed)
                {
                    horizontalVelocity = airHorizontalVelocity;
                }
                else
                {
                    if (Mathf.Abs(airHorizontalVelocity.x) < Mathf.Abs(horizontalVelocity.x))
                        horizontalVelocity.x = airHorizontalVelocity.x;
                    if (Mathf.Abs(airHorizontalVelocity.z) < Mathf.Abs(horizontalVelocity.z))
                        horizontalVelocity.z = airHorizontalVelocity.z;
                }
            }

            SetGroundedIndicatorColor(isGrounded);

            isOnMovingPlatform = false;

            if (isGrounded && Input.GetButtonDown("Jump"))
            {
                verticalSpeed = jumpSpeed;
                nextUngroundedTime = -1f;
                isGrounded = false;
            }
            
            var resultVelocity = horizontalVelocity;
            
            if (isGrounded)
            {
                mover.isInWalkMode = true;
                verticalSpeed = 0f;

                if (groundDetected)
                    isOnMovingPlatform = groundInfo.collider.TryGetComponent(out movingPlatform);
            }
            else
            {
                mover.isInWalkMode = false;

                BounceDownIfTouchedCeiling();

                verticalSpeed += gravity * deltaTime;

                if (verticalSpeed < minVerticalSpeed)
                    verticalSpeed = minVerticalSpeed;

                resultVelocity += verticalSpeed * transform.up;
            }

            if (dashTime > 0)
            {
                dashTime -= Time.deltaTime;
                resultVelocity = dashDirection * 15;
                horizontalVelocity = Vector3.zero;
                verticalSpeed = 0;
                mover.isInWalkMode = false;
            }
            
            mover.Move(resultVelocity * deltaTime, moveContacts, out contactCount);
            shouldMoveUp = false;
        }

        private void LateUpdate()
        {
            if (isOnMovingPlatform)
                ApplyPlatformMovement(movingPlatform);
        }

        private Vector3 GetMovementInput()
        {
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");

            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, transform.up).normalized;
            Vector3 right = Vector3.Cross(transform.up, forward);

            var result = x * right + y * forward;
            result = Vector3.ClampMagnitude(result, 1);
            return result;
        }

        private bool DetectGroundAndCheckIfGrounded(out bool isGrounded, out GroundInfo groundInfo)
        {
            if (shouldMoveUp)
            {
                groundInfo = default;
                isGrounded = false;
                return false;
            }
            
            bool groundDetected = groundDetector.DetectGround(out groundInfo);

            if (groundDetected)
            {
                if (groundInfo.isOnFloor && verticalSpeed < 0.1f)
                    nextUngroundedTime = Time.time + timeBeforeUngrounded;
            }
            else
                nextUngroundedTime = -1f;

            isGrounded = Time.time < nextUngroundedTime;
            return groundDetected;
        }

        private void SetGroundedIndicatorColor(bool isGrounded)
        {
            if (groundedIndicator != null)
                groundedIndicator.material.color = isGrounded ? Color.green : Color.blue;
        }

        private void ApplyPlatformMovement(MovingPlatform movingPlatform)
        {
            GetMovementFromMovingPlatform(movingPlatform, out Vector3 movement, out float upRotation);

            transform.Translate(movement, Space.World);
            transform.Rotate(0f, upRotation, 0f, Space.Self);
        }

        private void GetMovementFromMovingPlatform(MovingPlatform movingPlatform, out Vector3 movement, out float deltaAngleUp)
        {
            movingPlatform.GetDeltaPositionAndRotation(out Vector3 platformDeltaPosition, out Quaternion platformDeltaRotation);
            Vector3 localPosition = transform.position - movingPlatform.transform.position;
            movement = platformDeltaPosition + platformDeltaRotation * localPosition - localPosition;

            platformDeltaRotation.ToAngleAxis(out float platformDeltaAngle, out Vector3 axis);
            float axisDotUp = Vector3.Dot(axis, transform.up);

            if (-0.1f < axisDotUp && axisDotUp < 0.1f)
                deltaAngleUp = 0f;
            else
                deltaAngleUp = platformDeltaAngle * Mathf.Sign(axisDotUp);
        }
        
        private void BounceDownIfTouchedCeiling()
        {
            for (int i = 0; i < contactCount; i++)
            {
                if (Vector3.Dot(moveContacts[i].normal, transform.up) < -0.7f)
                {
                    verticalSpeed = -0.25f * verticalSpeed;
                    break;
                }
            }
        }
    }
}
