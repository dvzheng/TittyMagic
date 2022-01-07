﻿using UnityEngine;

namespace TittyMagic
{
    public static class Calc
    {
        // value between -1 and +1
        // +1 = leaning 90 degrees left
        // -1 = leaning 90 degrees right
        public static float Roll(Quaternion q)
        {
            return 2 * InverseLerpToPi(Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w));
        }

        // value between -2 and 2
        // +2 = upright
        // +1 = horizontal, on stomach
        // -1 = horizontal, on back
        // -2 = upside down
        public static float Pitch(Quaternion q)
        {
            return 2 * InverseLerpToPi(Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z));
        }

        private static float InverseLerpToPi(float val)
        {
            if(val > 0)
            {
                return Mathf.InverseLerp(0, Mathf.PI, val);
            }

            return -Mathf.InverseLerp(0, -Mathf.PI, val);
        }

        public static float RoundToDecimals(float value, float roundFactor)
        {
            return Mathf.Round(value * roundFactor) / roundFactor;
        }

        public static Vector3 RelativePosition(Transform origin, Vector3 position)
        {
            Vector3 distance = position - origin.position;
            return new Vector3(
                Vector3.Dot(distance, origin.right.normalized),
                Vector3.Dot(distance, origin.up.normalized),
                Vector3.Dot(distance, origin.forward.normalized)
            );
        }

        public static bool EqualWithin(float roundFactor, float v1, float v2)
        {
            return Mathf.Round(v1 * roundFactor) / roundFactor == Mathf.Round(v2 * roundFactor) / roundFactor;
        }

        public static bool VectorEqualWithin(float roundFactor, Vector3 v1, Vector3 v2)
        {
            return Mathf.Round(v1.x * roundFactor) / roundFactor == Mathf.Round(v2.x * roundFactor) / roundFactor
                && Mathf.Round(v1.y * roundFactor) / roundFactor == Mathf.Round(v2.y * roundFactor) / roundFactor
                && Mathf.Round(v1.z * roundFactor) / roundFactor == Mathf.Round(v2.z * roundFactor) / roundFactor;
        }

        public static float ScaledSmoothMax(float value, float logMaxX)
        {
            if(logMaxX < 0)
            {
                return -Mathf.Log(value * Mathf.Abs(logMaxX) + 1);
            }

            return Mathf.Log(value * logMaxX + 1);
        }
    }
}
