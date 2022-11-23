using UnityEngine;

namespace _Prototyping.ScivoloPrototype
{
    public static class MovingPlatformUtils
    {
        public static void GetMovementFromMovingPlatform(
            Transform target,
            Vector3 platformPosition,
            Vector3 platformDeltaPosition, 
            Quaternion platformDeltaRotation)
        {
            var localPosition = target.position - platformPosition;
            var movement = platformDeltaPosition + platformDeltaRotation * localPosition - localPosition;

            platformDeltaRotation.ToAngleAxis(out var platformDeltaAngle, out var axis);
            var axisDotUp = Vector3.Dot(axis, target.up);

            float deltaAngleUp;
            if (-0.1f < axisDotUp && axisDotUp < 0.1f)
                deltaAngleUp = 0f;
            else
                deltaAngleUp = platformDeltaAngle * Mathf.Sign(axisDotUp);
            
            target.Translate(movement, Space.World);
            target.Rotate(0f, deltaAngleUp, 0f, Space.Self);
        }
    }
}