﻿using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    public static class Const
    {
        public const float MASS_MIN = 0.1f;
        public const float MASS_MAX = 2.0f;

        public const float SOFTNESS_MIN = 0f;
        public const float SOFTNESS_MAX = 100f;

        public const float GRAVITY_MIN = 0f;
        public const float GRAVITY_MAX = 100f;
    }

    public static class Globals
    {
        public static AdjustJoints BREAST_CONTROL { get; set; }
        public static DAZPhysicsMesh BREAST_PHYSICS_MESH { get; set; }
        public static DAZCharacterSelector GEOMETRY { get; set; }
    }

    public static class Mode
    {
        public const string ANIM_OPTIMIZED = "Animation optimized";
        public const string BALANCED = "Balanced";
        public const string TOUCH_OPTIMIZED = "Touch optimized";
    }

    public static class RefreshStatus
    {
        public const int DONE = 0;
        public const int WAITING = 1;
        public const int MASS_STARTED = 2;
        public const int MASS_OK = 3;
        public const int NEUTRALPOS_STARTED = 4;
        public const int NEUTRALPOS_OK = 5;
    }
}
