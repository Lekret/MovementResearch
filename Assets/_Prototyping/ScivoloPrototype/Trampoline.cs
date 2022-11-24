﻿using MenteBacata.ScivoloCharacterControllerDemo;
using UnityEngine;

namespace _Prototyping.ScivoloPrototype
{
    public class Trampoline : MonoBehaviour
    {
        public Vector3 power;
        private float _cooldown;

        private void OnCollisionEnter(Collision collision)
        {
            if (_cooldown > 0)
                return;
            
            if (collision.collider.TryGetComponent(out ScivoloKinematicMovement movement))
            {
                movement.SetVelocity(power);
                _cooldown = 0.3f;
            }
        }

        private void Update()
        {
            if (_cooldown > 0)
                _cooldown -= Time.deltaTime;
        }
    }
}