//#define MB_DEBUG

using _Prototyping.ScivoloPrototype;
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
        public float gravity = -25f;
        public float minVerticalSpeed = -12f;
        public float groundSpeedChangeRate = 12;
        public float airSpeedChangeRate = 15;
        public float speedFallDuringCollision = 8;
        public CharacterMover mover;
        public GroundDetector groundDetector;
        public MeshRenderer groundedIndicator;

        private const float timeBeforeUngrounded = 0.02f;
        private float nextUngroundedTime = -1f;
        private float verticalSpeed = 0;
        private Vector3 horizontalVelocity;
        private Vector3 additionalHorizontalVelocity;
        private Transform cameraTransform;
        private MoveContact[] moveContacts = CharacterMover.NewMoveContactArray;
        private int contactCount;
        private MovingPlatform movingPlatform;
        private float dashTime = 0;
        private Vector3 dashDirection;
        
        private void Start()
        {
            cameraTransform = Camera.main.transform;
            mover.canClimbSteepSlope = true;
        }
        
        public void SetVelocity(Vector3 velocity)
        {
            verticalSpeed = velocity.y;
            velocity.y = 0;
            additionalHorizontalVelocity = velocity;
            nextUngroundedTime = -1;
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
                var targetHorizontalVelocity = movementInput * moveSpeed;
                horizontalVelocity = Vector3.Lerp(
                    horizontalVelocity, 
                    targetHorizontalVelocity,
                    groundSpeedChangeRate * deltaTime);
                additionalHorizontalVelocity = Vector3.zero;
            }
            else
            {
                var airMotion = movementInput * (deltaTime * airSpeedChangeRate);
                horizontalVelocity += airMotion;
                horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, moveSpeed);

                if (additionalHorizontalVelocity != Vector3.zero)
                {
                    var newAdditionalVelocity = additionalHorizontalVelocity + airMotion;
                    if (Mathf.Abs(newAdditionalVelocity.x) < Mathf.Abs(additionalHorizontalVelocity.x))
                        additionalHorizontalVelocity.x = newAdditionalVelocity.x;
                    if (Mathf.Abs(newAdditionalVelocity.z) < Mathf.Abs(additionalHorizontalVelocity.z))
                        additionalHorizontalVelocity.z = newAdditionalVelocity.z;
                }
            }

            SetGroundedIndicatorColor(isGrounded);

            var previousMovingPlatform = movingPlatform;
            movingPlatform = null;

            if (isGrounded && Input.GetButtonDown("Jump"))
            {
                verticalSpeed = jumpSpeed;
                nextUngroundedTime = -1f;
                isGrounded = false;
            }
            
            var resultVelocity = horizontalVelocity + additionalHorizontalVelocity;
            
            if (isGrounded)
            {
                mover.isInWalkMode = true;
                verticalSpeed = 0f;

                if (groundDetected)
                {
                    groundInfo.collider.TryGetComponent(out movingPlatform);
                }
            }
            else
            {
                mover.isInWalkMode = false;

                if (verticalSpeed > 0)
                    BounceDownIfTouchedCeiling();

                verticalSpeed += gravity * deltaTime;

                if (verticalSpeed < minVerticalSpeed)
                    verticalSpeed = minVerticalSpeed;

                resultVelocity += verticalSpeed * transform.up;
            }

            if (previousMovingPlatform && !movingPlatform)
            {
                Debug.Log("Exited");
                previousMovingPlatform.GetDeltaPositionAndRotation(out var deltaPosition, out var deltaRotation);
                deltaPosition /= Time.deltaTime;
                horizontalVelocity += deltaPosition;
                horizontalVelocity.y = 0;
                verticalSpeed += deltaPosition.y;
                resultVelocity += deltaPosition;
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
            if (!isGrounded)
                ClampAirVelocity();
        }

        private void ClampAirVelocity()
        {
            for (var i = 0; i < contactCount; i++)
            {
                var contact = moveContacts[i];
                var combinedVelocity = horizontalVelocity + new Vector3(0, verticalSpeed, 0);
                combinedVelocity = Vector3.ProjectOnPlane(combinedVelocity, contact.normal);
                if (verticalSpeed < 0)
                    verticalSpeed = combinedVelocity.y;
                horizontalVelocity = combinedVelocity;
                horizontalVelocity.y = 0;
            }
        }

        private void LateUpdate()
        {
            TryApplyPlatformMovement();
        }

        private Vector3 GetMovementInput()
        {
            var x = Input.GetAxisRaw("Horizontal");
            var y = Input.GetAxisRaw("Vertical");

            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, transform.up).normalized;
            var right = Vector3.Cross(transform.up, forward);

            var result = x * right + y * forward;
            result = Vector3.ClampMagnitude(result, 1);
            return result;
        }

        private bool DetectGroundAndCheckIfGrounded(out bool isGrounded, out GroundInfo groundInfo)
        {
            var groundDetected = groundDetector.DetectGround(out groundInfo);

            if (groundDetected)
            {
                if (groundInfo.isOnFloor && verticalSpeed < 0.1f)
                    nextUngroundedTime = Time.time + timeBeforeUngrounded;
            }
            else
            {
                nextUngroundedTime = -1f;
            }

            isGrounded = Time.time < nextUngroundedTime;
            return groundDetected;
        }

        private void SetGroundedIndicatorColor(bool isGrounded)
        {
            if (groundedIndicator != null)
                groundedIndicator.material.color = isGrounded ? Color.green : Color.blue;
        }

        private void TryApplyPlatformMovement()
        {
            if (!movingPlatform)
                return;
            
            movingPlatform.GetDeltaPositionAndRotation(out var deltaPosition, out var deltaRotation);
            MovingPlatformUtils.GetMovementFromMovingPlatform(
                transform, 
                movingPlatform.transform.position,
                deltaPosition,
                deltaRotation);
        }

        private void BounceDownIfTouchedCeiling()
        {
            for (var i = 0; i < contactCount; i++)
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
