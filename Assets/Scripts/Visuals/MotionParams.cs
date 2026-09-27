namespace HashiraChronicles
{
    /// <summary>Numbers behind each <see cref="MotionStyle"/>: how a character bobs, leans, breathes and fidgets.</summary>
    public struct MotionParams
    {
        public float walkFreq, walkAmp, lean, idleFreq, idleAmp, idleLean, roll, jitter, headLook;
        public bool stomp;

        public static MotionParams For(MotionStyle m)
        {
            switch (m)
            {
                case MotionStyle.Nervous: return new MotionParams { walkFreq = 16f, walkAmp = 0.08f, lean = 8f, idleFreq = 6f, idleAmp = 0.02f, jitter = 7f, headLook = 35f };
                case MotionStyle.Aggressive: return new MotionParams { walkFreq = 9f, walkAmp = 0.16f, lean = 20f, idleFreq = 2.4f, idleAmp = 0.045f, idleLean = 8f, stomp = true, headLook = 8f };
                case MotionStyle.Graceful: return new MotionParams { walkFreq = 9f, walkAmp = 0.06f, lean = 6f, idleFreq = 1.8f, idleAmp = 0.05f, roll = 5f, headLook = 15f };
                case MotionStyle.Stoic: return new MotionParams { walkFreq = 8f, walkAmp = 0.05f, lean = 4f, idleFreq = 1.4f, idleAmp = 0.012f, headLook = 5f, stomp = true };
                case MotionStyle.Confident: return new MotionParams { walkFreq = 11f, walkAmp = 0.1f, lean = 9f, idleFreq = 2.4f, idleAmp = 0.03f, idleLean = -5f, roll = 3f, headLook = 12f };
                case MotionStyle.Sly: return new MotionParams { walkFreq = 12f, walkAmp = 0.06f, lean = 16f, idleFreq = 2.2f, idleAmp = 0.025f, roll = 4f, headLook = 20f };
                case MotionStyle.Light: return new MotionParams { walkFreq = 15f, walkAmp = 0.15f, lean = 13f, idleFreq = 4f, idleAmp = 0.04f, headLook = 18f };
                default: return new MotionParams { walkFreq = 12f, walkAmp = 0.1f, lean = 12f, idleFreq = 3f, idleAmp = 0.025f, headLook = 12f };
            }
        }
    }
}
