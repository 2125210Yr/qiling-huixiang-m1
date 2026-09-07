using System;
namespace Resonance.App
{
    public static class PuppetLegIK
    {
        public struct Pose { public double KneeX, KneeY, AnkleX, AnkleY; }
        public static Pose Solve(double hipX,double hipY,double targetX,double targetY,double upper,double lower,int bendSign)
        {
            if(upper<=0 || lower<=0 || double.IsNaN(upper+lower) || double.IsInfinity(upper+lower))
                throw new ArgumentOutOfRangeException("upper", "Bone lengths must be finite and positive.");
            double dx=targetX-hipX,dy=targetY-hipY;
            double distance=Math.Sqrt(dx*dx+dy*dy);
            double ux=distance>1e-10 ? dx/distance : 0;
            double uy=distance>1e-10 ? dy/distance : -1;
            distance=Math.Max(Math.Abs(upper-lower)+1e-9,Math.Min(upper+lower-1e-9,distance));
            double along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            double height=Math.Sqrt(Math.Max(0,upper*upper-along*along))*(bendSign<0 ? -1 : 1);
            return new Pose {
                KneeX=hipX+ux*along-uy*height,
                KneeY=hipY+uy*along+ux*height,
                AnkleX=hipX+ux*distance,
                AnkleY=hipY+uy*distance
            };
        }
    }
}
