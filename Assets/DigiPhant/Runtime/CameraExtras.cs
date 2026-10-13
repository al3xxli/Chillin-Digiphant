using System;

namespace DigiPhant
{
    // Optional packet fields; older pose-only bridges remain compatible.
    [Serializable] public sealed class BodyMotionFrame
    {
        public float hipY, shoulderY, leftFootY, rightFootY, scale, confidence;
    }
    [Serializable] public sealed class FaceMotionFrame { public float turn, confidence; }
    [Serializable] public sealed class ShoulderMotionFrame { public float turn, confidence; }
    [Serializable] public sealed class HandMotionFrame { public float pinch, fist, confidence; public bool pinchAllowed, peace; }
}
