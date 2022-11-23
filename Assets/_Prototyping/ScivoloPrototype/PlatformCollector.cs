using System;
using System.Collections.Generic;
using UnityEngine;
using MovingPlatform = MenteBacata.ScivoloCharacterControllerDemo.MovingPlatform;

namespace _Prototyping.ScivoloPrototype
{
    public class PlatformCollector : MonoBehaviour
    {
        [SerializeField] private MovingPlatform _platform;

        private readonly HashSet<Transform> _movables = new HashSet<Transform>();

        private void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb && !rb.isKinematic)
            {
                _movables.Add(rb.transform);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb && !rb.isKinematic)
            {
                _movables.Remove(rb.transform);
            }
        }

        private void LateUpdate()
        {
            _platform.GetDeltaPositionAndRotation(out var deltaPosition, out var deltaRotation);
            var position = _platform.transform.position;
            
            foreach (var target in _movables)
            {
                MovingPlatformUtils.GetMovementFromMovingPlatform(
                    target, 
                    position, 
                    deltaPosition,
                    deltaRotation);
            }
        }
    }
}