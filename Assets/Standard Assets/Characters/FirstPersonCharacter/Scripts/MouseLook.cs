using System;
using UnityEngine;

namespace UnitySampleAssets.Characters.FirstPerson
{
    [Serializable]
    public class MouseLook
    {
        public float XSensitivity = 15f;
        public float YSensitivity = 15f;
        public float MinimumX = -360F;
        public float MaximumX = 360F;
        public float MinimumY = -90F;
        public float MaximumY = 90F;

        private float xvel;
        private float yvel;
        public bool smooth;
        public float smoothtime;

        public Vector2 UnClamped(float x, float y)
        {
            Vector2 value;
            value.x = y + Input.GetAxis("Mouse X")*XSensitivity;
            value.y = x + Input.GetAxis("Mouse Y")*YSensitivity;

            if (smooth)
            {
                value.x = Mathf.SmoothDamp(y, value.x, ref xvel, smoothtime);
                value.y = Mathf.SmoothDamp(x, value.y, ref yvel, smoothtime);
            }

            return value;
        }

        public Vector2 Clamped(float x, float y)
        {
            Vector2 value;
            value.x = y + Input.GetAxis("Mouse X")*XSensitivity;
            value.y = x + Input.GetAxis("Mouse Y")*YSensitivity;

            value.x = Mathf.Clamp(value.x, MinimumX, MaximumX);
            value.y = Mathf.Clamp(value.y, MinimumY, MaximumY);

            if (smooth)
            {
                value.x = Mathf.SmoothDamp(y, value.x, ref xvel, smoothtime);
                value.y = Mathf.SmoothDamp(x, value.y, ref yvel, smoothtime);
            }

            return value;
        }
    }
}