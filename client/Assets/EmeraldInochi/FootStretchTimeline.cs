using System;

namespace EmeraldInochi {
    public readonly struct FootStretchPose {
        public readonly float Lift, Spread;
        public FootStretchPose(float lift, float spread) { Lift=lift; Spread=spread; }
    }

    // A pure, time-based sequence. X lifts the foreground foot; Y opens all five toes.
    public static class FootStretchTimeline {
        public const double Duration=4.4;
        public const double EntryDelay=1.0;
        public static FootStretchPose Evaluate(double seconds) {
            if(double.IsNaN(seconds)||seconds<=0||seconds>=Duration)return new FootStretchPose(0,0);
            float lift=seconds<1.1?Ease(seconds/1.1):seconds<=3.55?1:1-Ease((seconds-3.55)/.85);
            float spread=seconds<1.35?0:seconds<2.1?Ease((seconds-1.35)/.75):seconds<=2.65?1:1-Ease((seconds-2.65)/.7);
            return new FootStretchPose(lift,spread);
        }
        static float Ease(double value) {
            value=Math.Max(0,Math.Min(1,value));
            return (float)(value*value*(3-2*value));
        }

        // Also runs in capture builds so a broken sequence cannot produce a completion marker.
        public static void SelfCheck() {
            CheckPose(-1,0,0); CheckPose(0,0,0);
            CheckPose(1.1,1,0); CheckPose(1.35,1,0);
            CheckPose(2.1,1,1); CheckPose(2.65,1,1);
            CheckPose(3.35,1,0); CheckPose(3.55,1,0);
            CheckPose(Duration,0,0); CheckPose(100,0,0);
            CheckPose(double.NaN,0,0); CheckPose(double.PositiveInfinity,0,0);
            CheckPose(double.NegativeInfinity,0,0);
            var previous=Evaluate(0);
            for(int i=1;i<=6000;i++) {
                double time=i/1000.0;
                var pose=Evaluate(time);
                Require(!float.IsNaN(pose.Lift)&&!float.IsNaN(pose.Spread),"non-finite pose");
                Require(pose.Lift>=0&&pose.Lift<=1&&pose.Spread>=0&&pose.Spread<=1,"out-of-range pose");
                Require(pose.Spread==0||pose.Lift==1,"toes open before foot is raised");
                Require(Math.Abs(pose.Lift-previous.Lift)<.003&&Math.Abs(pose.Spread-previous.Spread)<.003,"discontinuous pose");
                if(time<=1.1)Require(pose.Lift>=previous.Lift,"lift moves backwards");
                if(time>=1.351&&time<=2.1)Require(pose.Spread>=previous.Spread,"spread moves backwards");
                if(time>=2.651&&time<=3.35)Require(pose.Spread<=previous.Spread,"close moves backwards");
                if(time>=3.551&&time<=Duration)Require(pose.Lift<=previous.Lift,"lower moves backwards");
                previous=pose;
            }
        }
        static void CheckPose(double seconds,float lift,float spread) {
            var pose=Evaluate(seconds);
            Require(Math.Abs(pose.Lift-lift)<.00001&&Math.Abs(pose.Spread-spread)<.00001,"incorrect key pose at "+seconds);
        }
        static void Require(bool condition,string message) {
            if(!condition)throw new InvalidOperationException("FootStretch timeline: "+message);
        }
    }
}
