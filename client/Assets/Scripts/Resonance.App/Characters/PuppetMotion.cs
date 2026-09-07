using System;
namespace Resonance.App
{
    /// <summary>Deterministic secondary motion in canvas-relative units. No renderer dependency.</summary>
    public sealed class PuppetMotion
    {
        const double Tick = 1.0 / 120.0;
        double _pending, _time, _skillTime = -1, _blinkTime = -1, _nextBlink = 2.7;
        double _previousSwing, _previousSwingSpeed;
        readonly Spring _chestLeft = new Spring(), _chestRight = new Spring();
        readonly Spring _hairLeft = new Spring(), _hairLeftTip = new Spring();
        readonly Spring _hairRight = new Spring(), _hairRightTip = new Spring();
        readonly Spring _gaze = new Spring();
        double _gazeTarget;
        public float ChestLeft { get { return (float)_chestLeft.Position; } }
        public float ChestRight { get { return (float)_chestRight.Position; } }
        public float HairLeft { get { return (float)_hairLeft.Position; } }
        public float HairLeftTip { get { return (float)_hairLeftTip.Position; } }
        public float HairRight { get { return (float)_hairRight.Position; } }
        public float HairRightTip { get { return (float)_hairRightTip.Position; } }
        public float Gaze { get { return (float)_gaze.Position; } }
        public float Breath { get { return (float)Math.Sin(_time * 1.05); } }
        public float Swing { get; private set; }
        public float Blink { get; private set; }
        public bool SkillActive { get { return _skillTime >= 0; } }
        public void TapChest(float side)
        {
            _chestLeft.Impulse(side <= 0 ? -.035 : -.009, .075);
            _chestRight.Impulse(side > 0 ? -.035 : -.009, .075);
        }
        public void Look(float direction) { _gazeTarget = Clamp(direction, -1, 1); }
        public void BlinkNow() { if (_blinkTime < 0) _blinkTime = 0; }
        public bool StartSkill()
        {
            if (SkillActive) return false;
            _skillTime = 0;
            return true;
        }
        public void Step(double dt)
        {
            if (double.IsNaN(dt) || double.IsInfinity(dt) || dt <= 0) return;
            // Limit catch-up work on app resume; never integrate one unstable giant step.
            _pending += Math.Min(dt, .25);
            while (_pending + 1e-10 >= Tick)
            {
                _pending -= Tick; _time += Tick;
                if (SkillActive)
                {
                    _skillTime += Tick;
                    Swing = (float)SwingAt(_skillTime);
                    if (_skillTime >= 2.2) { _skillTime = -1; Swing = 0; }
                }
                var speed = (Swing - _previousSwing) / Tick;
                var acceleration = (speed - _previousSwingSpeed) / Tick;
                _previousSwing = Swing; _previousSwingSpeed = speed;
                _chestLeft.Step(Clamp(-acceleration * .00003, -.003, .003), Tick, 6.8, 14, .006);
                _chestRight.Step(Clamp(-acceleration * .000025, -.003, .003), Tick, 7.4, 15.5, .006);
                var wind = Math.Sin(_time * .8) * .65 + Math.Sin(_time * 1.3) * .2;
                _hairLeft.Step(wind - speed * .25, Tick, 4, 6, 2.5);
                _hairLeftTip.Step(_hairLeft.Position * 1.7 - speed * .3, Tick, 3.2, 4.5, 5);
                _hairRight.Step(-wind * .8 - speed * .2, Tick, 4.5, 6.8, 2.5);
                _hairRightTip.Step(_hairRight.Position * 1.6 - speed * .3, Tick, 3.5, 4.8, 5);
                _gaze.Step(_gazeTarget, Tick, 10, 12, 1);
                if (_blinkTime < 0 && _time >= _nextBlink) _blinkTime = 0;
                if (_blinkTime >= 0)
                {
                    _blinkTime += Tick;
                    Blink = (float)(_blinkTime < .085 ? Ease(_blinkTime / .085) : 1-Ease((_blinkTime-.085)/.13));
                    if (_blinkTime >= .215) { _blinkTime = -1; Blink = 0; _nextBlink = _time + 3.1 + .7*Math.Sin(_time*1.7); }
                }
            }
        }
        static double SwingAt(double t)
        {
            if(t<.5) return -.12*Ease(t/.5);
            if(t<.85) return -.12+1.12*Ease((t-.5)/.35);
            if(t<1.15) return 1-.15*Ease((t-.85)/.3);
            if(t<2.2) return .85*(1-Ease((t-1.15)/1.05));
            return 0;
        }
        static double Ease(double t) { t=Clamp(t,0,1); return t*t*t*(t*(t*6-15)+10); }
        static double Clamp(double x,double lo,double hi) { return Math.Max(lo,Math.Min(hi,x)); }
        sealed class Spring
        {
            public double Position, Velocity;
            public void Impulse(double value,double limit) { Velocity=Clamp(Velocity+value,-limit,limit); }
            public void Step(double target,double dt,double damping,double frequency,double limit)
            {
                var x=Position-target;
                var decay=Math.Exp(-damping*dt);var c=Math.Cos(frequency*dt);var s=Math.Sin(frequency*dt);
                Position=target+decay*(x*c+(Velocity+damping*x)/frequency*s);
                Velocity=decay*(Velocity*c-(damping*Velocity+(frequency*frequency+damping*damping)*x)/frequency*s);
                if(Position>limit) { Position=limit; Velocity=Math.Min(0,Velocity); }
                if(Position< -limit) { Position= -limit; Velocity=Math.Max(0,Velocity); }
            }
        }
    }
}
