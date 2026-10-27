using System;
using System.Collections.Generic;
using System.Numerics;

namespace PoolPhysics.Sim
{
    public enum TerminationReason { Rest, TimeLimit, IterationLimit, InvalidState }

    public struct StepResult
    {
        public float SimulatedTime;
        public TerminationReason Reason;
        public int EventCount;
        public float MaxPenetration;
        public bool Valid => Reason != TerminationReason.InvalidState;
    }

    /// <summary>
    /// EVENT-BASED billiard simulator (Leckie & Greenspan). Motion advanced
    /// ANALYTICALLY within each phase; loop jumps event-to-event (phase transitions,
    /// ball-ball & ball-cushion time-of-impact, pockets). Collisions use LINEAR swept
    /// TOI: exact for constant-velocity motion within an interval; during sliding,
    /// acceleration is approximated (documented limitation). A separate positional
    /// depenetration pass keeps overlaps from lingering. Table plane = XZ, up = +Y.
    /// </summary>
    public sealed class EventSimulator
    {
        public readonly List<Ball> Balls = new List<Ball>();
        public TableSpecData Table;
        public PhysicsProfile Profile;
        public int CueBallIndex = 0;

        public int CollisionCount;                 // impact events (not pure depenetration)
        public double InitialEnergy, PeakTotalEnergy, FinalEnergy;

        const float EPS = 1e-4f;
        const float CONTACT_TOL = 1e-5f;
        const int MAX_ZERO_TIME = 64;
        const int DEPENETRATE_ITERS = 8;

        public EventSimulator(TableSpecData table, PhysicsProfile profile) { Table = table; Profile = profile; }

        public bool AllAtRest
        {
            get { for (int i=0;i<Balls.Count;i++) if (Balls[i].Active && !Balls[i].Pocketed && Balls[i].Phase!=BallPhase.Rest) return false; return true; }
        }

        double TotalEnergy()
        {
            double e=0; for(int i=0;i<Balls.Count;i++){ var b=Balls[i]; if(!b.Active||b.Pocketed) continue;
                double lin=0.5*b.Mass*b.Velocity.LengthSquared();
                double I=0.4*b.Mass*b.Radius*b.Radius;
                e+=lin+0.5*I*b.AngularVelocity.LengthSquared(); } return e;
        }

        Vector2 NapU => new Vector2(MathF.Cos(Table.NapHeading), MathF.Sin(Table.NapHeading));

        void Classify(ref Ball b)
        {
            b.Phase = EventMotion.Classify(ref b);
            if (b.Phase == BallPhase.Rest) { b.Velocity = Vector3.Zero; b.AngularVelocity = Vector3.Zero; }
        }
        float PhaseDuration(ref Ball b)
            => EventMotion.SegmentDuration(ref b, Profile.Gravity, Profile.SlidingFriction, Profile.RollingFriction,
                                           Profile.SpinDecay, NapU, Profile.NapAnisotropy);
        void Advance(ref Ball b, float dt)
            => EventMotion.Advance(ref b, dt, Profile.Gravity, Profile.SlidingFriction, Profile.RollingFriction,
                                   Profile.SpinDecay, NapU, Profile.NapAnisotropy, Profile.NapSteer);

        private int _zeroTime;

        /// <summary>
        /// Reset per-shot telemetry and CANONICALIZE ball order by initial (X,Z).
        /// Sorting by physical position (not input list order) makes the whole
        /// event loop permutation-invariant; CueBallIndex is remapped to follow
        /// the cue ball by its position.
        /// </summary>
        public void BeginShot()
        {
            CollisionCount=0; InitialEnergy=TotalEnergy(); PeakTotalEnergy=InitialEnergy; _zeroTime=0;
            if (Trace) TraceLog.Clear();
            if (Balls.Count>1)
            {
                var cuePos = (CueBallIndex>=0 && CueBallIndex<Balls.Count) ? Balls[CueBallIndex].Position : Vector3.Zero;
                bool hasCue = CueBallIndex>=0 && CueBallIndex<Balls.Count;
                Balls.Sort((p,q)=>{ int c=p.Position.X.CompareTo(q.Position.X); if(Math.Abs(c)!=0&&p.Position.X!=q.Position.X) return c; return p.Position.Z.CompareTo(q.Position.Z); });
                if (hasCue) { int best=0; float bd=float.MaxValue; for(int i=0;i<Balls.Count;i++){ float dd=(Balls[i].Position-cuePos).LengthSquared(); if(dd<bd){bd=dd;best=i;} } CueBallIndex=best; }
            }
        }

        public bool Trace;                             // per-iteration debug log (detdbg)
        public readonly List<string> TraceLog = new List<string>();

        public StepResult Step(float maxTime)
        {
            var res = new StepResult();
            float t=0; int iter=0; float maxPen=0;
            if (Trace) TraceLog.Add($"== STEP window t0={t:R} maxTime={maxTime:R}");
            while (true)
            {
                for (int i=0;i<Balls.Count;i++){ var b=Balls[i]; if(b.Active&&!b.Pocketed) Classify(ref b); Balls[i]=b; }
                maxPen = MathF.Max(maxPen, Depenetrate());          // positional pass, independent of events
                if (Trace) TraceLog.Add($"i{iter} t={t:R} depen={maxPen:R}");
                if (AllAtRest) { res.Reason = TerminationReason.Rest; break; }
                if (t>=maxTime) { res.Reason = TerminationReason.TimeLimit; break; }
                if (++iter>200000) { res.Reason = TerminationReason.IterationLimit; break; }

                float dtNext = maxTime - t;
                for (int i=0;i<Balls.Count;i++){ var b=Balls[i]; if(!b.Active||b.Pocketed||b.Phase==BallPhase.Rest)continue;
                    float d=PhaseDuration(ref b); if(d<dtNext) dtNext=d; }
                if (Trace) {
                    string dd = string.Join(";", System.Linq.Enumerable.Select(Balls, bb =>
                        !bb.Active || bb.Pocketed || bb.Phase==BallPhase.Rest ? "r" :
                        $"{bb.Phase} dur={PhaseDuration(ref bb):R} u={EventMotion.ContactULen(ref bb):R} w=({bb.AngularVelocity.X:R},{bb.AngularVelocity.Z:R})"));
                    TraceLog.Add($"  durs[{dd}] cap={maxTime - t:R}");
                }

                // earliest event over ALL unordered pairs (order-independent; rest ball can be struck)
                float toi=dtNext; int ai=-1,bi=-1; bool isCushion=false; Rail rail=default;
                for (int i=0;i<Balls.Count;i++){ var A=Balls[i]; if(!A.Active||A.Pocketed)continue;
                    for (int j=i+1;j<Balls.Count;j++){ var B=Balls[j]; if(!B.Active||B.Pocketed)continue;
                        if (A.Phase==BallPhase.Rest && B.Phase==BallPhase.Rest) continue;
                        float tt=SweptSphereTOI(A,B,dtNext); if(tt<toi){toi=tt;ai=i;bi=j;isCushion=false;} }
                    if (A.Phase!=BallPhase.Rest)
                        for (int r=0;r<Table.Rails.Count;r++){ float tt=SweptRailTOI(A,Table.Rails[r],dtNext); if(tt<toi){toi=tt;ai=i;bi=-1;isCushion=true;rail=Table.Rails[r];} }
                }

                float dt = toi;
                if (Trace) TraceLog.Add($"  ev ai={ai} bi={bi} cush={isCushion} toi={toi:R} dtNext={dtNext:R}");
                // Zero-time treatment applies to CONTACT events only. A phase-end event
                // (ai=-1, no rail) below CONTACT_TOL is a real sub-μs end of a segment:
                // advancing it explains forward progress (otherwise a ball with slip u just
                // over EPS can end up in an eternal zero-advance loop → InvalidState).
                bool isContactEvent = (ai >= 0) || isCushion;
                if (isContactEvent && dt <= CONTACT_TOL) { if (++_zeroTime > MAX_ZERO_TIME) { res.Reason=TerminationReason.InvalidState; break; } dt=0f; }
                else { _zeroTime=0; if (dt>dtNext) dt=dtNext; }

                // snapshot for swept pocket capture
                var prevPos = new Vector3[Balls.Count];
                for (int i=0;i<Balls.Count;i++) prevPos[i]=Balls[i].Position;

                if (dt > 0f) { for (int i=0;i<Balls.Count;i++){ var b=Balls[i]; if(!b.Active||b.Pocketed)continue; Advance(ref b,dt); Balls[i]=b; } t+=dt; }
                { double e=TotalEnergy(); if(e>PeakTotalEnergy) PeakTotalEnergy=e; }   // before resolve

                // Resolve the single earliest event this iteration (clusters converge over
                // successive iterations via the depenetration pass + repeated events).
                if (isCushion && bi<0 && ai>=0){ var b=Balls[ai]; ResolveCushion(ref b, rail); Balls[ai]=b; CollisionCount++; }
                else if (ai>=0 && bi>=0){ var A=Balls[ai]; var B=Balls[bi]; ResolveBallBall(ref A,ref B); Balls[ai]=A; Balls[bi]=B; CollisionCount++; }
                { double e=TotalEnergy(); if(e>PeakTotalEnergy) PeakTotalEnergy=e; }   // after resolve

                // SWEPT pocket capture: segment prev->new crossing a pocket circle
                for (int i=0;i<Balls.Count;i++){ var b=Balls[i]; if(!b.Active||b.Pocketed)continue;
                    foreach(var pk in Table.Pockets){
                        if (SegmentHitsCircle(prevPos[i], b.Position, new Vector2(pk.Center.X, pk.Center.Z), pk.Radius))
                        { b.Pocketed=true; b.Velocity=Vector3.Zero; b.AngularVelocity=Vector3.Zero; b.Phase=BallPhase.Rest; break; } }
                    Balls[i]=b; }
                if (Trace) TraceLog.Add($"  st t={t:R} " + string.Join(";", System.Linq.Enumerable.Select(Balls, b => b.Position.X.ToString("R") + "," + b.Position.Z.ToString("R") + "," + b.Phase + "@" + MathF.Sqrt(b.Velocity.X*b.Velocity.X+b.Velocity.Z*b.Velocity.Z).ToString("R") + "," + b.AngularVelocity.Y.ToString("R"))));
            }
            FinalEnergy=TotalEnergy();
            res.SimulatedTime=t; res.EventCount=iter; res.MaxPenetration=maxPen;
            return res;
        }

        public StepResult RunToRest(float maxTime=30f) { BeginShot(); return Step(maxTime); }

        float Depenetrate()
        {
            float residual=0;
            for (int k=0;k<DEPENETRATE_ITERS;k++)
            {
                for (int i=0;i<Balls.Count;i++){ var A=Balls[i]; if(!A.Active||A.Pocketed)continue;
                    for (int j=i+1;j<Balls.Count;j++){ var B=Balls[j]; if(!B.Active||B.Pocketed)continue;
                        Vector2 d=new Vector2(B.Position.X-A.Position.X, B.Position.Z-A.Position.Z);
                        float dist=d.Length(); float minD=A.Radius+B.Radius;
                        if (dist>=minD) continue;
                        float pen=minD-dist;
                        Vector2 n = dist>1e-7f ? d/dist : FallbackNormal(A,B);
                        float im=1f/A.Mass, jm=1f/B.Mass, tot=im+jm; if(tot<=0f) continue;
                        A.Position-=new Vector3(n.X,0,n.Y)*(pen*im/tot);
                        B.Position+=new Vector3(n.X,0,n.Y)*(pen*jm/tot);
                        Balls[j]=B;                       // write back B (struct!) — fixes lost correction
                    }
                    Balls[i]=A;
                }
                // measure TRUE residual AFTER correction (not pre-correction maxPen)
                residual=0;
                for (int i=0;i<Balls.Count;i++){ var A=Balls[i]; if(!A.Active||A.Pocketed)continue;
                    for (int j=i+1;j<Balls.Count;j++){ var B=Balls[j]; if(!B.Active||B.Pocketed)continue;
                        float dd=(new Vector2(B.Position.X-A.Position.X,B.Position.Z-A.Position.Z)).Length();
                        float pen=MathF.Max(0,A.Radius+B.Radius-dd); if(pen>residual) residual=pen; } }
                if (residual<1e-6f) break;
            }
            return residual;
        }

        static Vector2 FallbackNormal(Ball A, Ball B)
        {
            Vector2 dv=new Vector2(B.Velocity.X-A.Velocity.X, B.Velocity.Z-A.Velocity.Z);
            if (dv.LengthSquared()>1e-12f) return Vector2.Normalize(dv);
            return (A.Position.X+B.Position.X) >= 0 ? new Vector2(1,0) : new Vector2(-1,0);
        }

        static Vector2 ClosestPointOnSeg(Vector2 p, Vector2 a, Vector2 b)
        { Vector2 ab=b-a; float t=Vector2.Dot(p-a,ab)/Math.Max(1e-9f,ab.LengthSquared()); t=t<0f?0f:(t>1f?1f:t); return a+ab*t; }

        static bool SegmentHitsCircle(Vector3 a, Vector3 b, Vector2 center, float radius)
        {
            Vector2 p=new Vector2(a.X,a.Z), q=new Vector2(b.X,b.Z);
            Vector2 c=ClosestPointOnSeg(center, p, q);
            return (c-center).LengthSquared() <= radius*radius;
        }

        Vector2 Accel(ref Ball b)
            => EventMotion.Acceleration(ref b, Profile.Gravity, Profile.SlidingFriction, Profile.RollingFriction,
                                        NapU, Profile.NapAnisotropy, Profile.NapSteer);

        // gap between two spheres under QUADRATIC relative motion at time t
        static float Gap(Vector2 dp, Vector2 dv, Vector2 da, float rr, float t)
        { Vector2 d = dp + dv*t + 0.5f*da*t*t; return d.Length() - rr; }

        /// <summary>Trajectory-consistent sphere-sphere TOI: solves |dp+dv t+0.5 da t^2|=rr
        /// (quadratic motion, not linear) via bracket+bisect → deterministic under time-chunking.</summary>
        float SweptSphereTOI(Ball A, Ball B, float tHigh)
        {
            Vector2 dp=new Vector2(B.Position.X-A.Position.X, B.Position.Z-A.Position.Z);
            Vector2 dv=new Vector2(B.Velocity.X-A.Velocity.X, B.Velocity.Z-A.Velocity.Z);
            var aA=A; var aB=B; Vector2 da=Accel(ref aB)-Accel(ref aA);
            float rr=A.Radius+B.Radius;
            float c=dp.LengthSquared()-rr*rr;
            // TOUCH tolerance: floats stored at coordinates ~50 lose ~4e-6 m of absolute
            // precision, so "exactly touching" can land just above zero (c>0). Treat a
            // surface gap <= GAP_EPS as contact; ResolveBallBall's depenetration pass
            // finishes the rest.
            if (c <= 2f*rr*GAP_EPS + GAP_EPS*GAP_EPS) { float b0=2*Vector2.Dot(dp,dv); return (b0 < 0f) ? 0f : float.PositiveInfinity; }
            if (tHigh<=0f) return float.PositiveInfinity;
            // EXACT first-contact CCD: q(t)=|d(t)|^2-rr^2 is a quartic; q'(t)/2 is a cubic.
            // Between successive critical points q is monotone, so no interior dip can be
            // missed by sampling; bisection inside a monotone piece converges to THE root.
            // Purely analytic => bit-deterministic and independent of window chunking.
            float c3=0.5f*(da.X*da.X+da.Y*da.Y);
            float c2=1.5f*(da.X*dv.X+da.Y*dv.Y);
            float c1=dp.X*da.X+dv.X*dv.X+dp.Y*da.Y+dv.Y*dv.Y;
            float c0c=dp.X*dv.X+dp.Y*dv.Y;
            Span<float> crit=stackalloc float[3];
            int nc=CubicCriticalPoints(c3,c2,c1,c0c,crit,tHigh);
            float lo=0f;
            for (int k=0;k<=nc;k++)
            {
                float hi = k<nc ? crit[k] : tHigh;
                if (Gap(dp,dv,da,rr,lo)>0f && Gap(dp,dv,da,rr,hi)<=0f)
                {   // decreasing piece crossing zero: bisect to the root
                    float a2=lo, b2=hi;
                    for(int it=0; it<40; it++){ float mid=(a2+b2)*0.5f; if(Gap(dp,dv,da,rr,mid)<=0f) b2=mid; else a2=mid; }
                    return (a2+b2)*0.5f;
                }
                lo=hi;
            }
            return float.PositiveInfinity;
        }

        const float GAP_EPS = 1e-5f;   // touching tolerance in metres (see SweptSphereTOI)

        /// <summary>Real critical points of the swept-distance quartic = real roots of a cubic
        /// a3 t^3 + a2 t^2 + a1 t + a0, kept in (0, tHigh], ascending. Degenerate leading
        /// coeff falls back to the quadratic / linear formulas so behaviour is identical
        /// for linear relative motion (da==0).</summary>
        static int CubicCriticalPoints(float a3, float a2, float a1, float a0, Span<float> roots, float tHigh)
        {
            if (MathF.Abs(a3) < 1e-18f)
            {
                if (MathF.Abs(a2) < 1e-18f)
                {   // linear a1 t + a0 = 0
                    if (MathF.Abs(a1) < 1e-18f) return 0;
                    float t=-a0/a1;
                    if (t>0f && t<=tHigh) { roots[0]=t; return 1; }
                    return 0;
                }
                float disc=a1*a1-4f*a2*a0;
                if (disc<0f) return 0;
                float sq=MathF.Sqrt(disc);
                float r1=(-a1-sq)/(2f*a2), r2=(-a1+sq)/(2f*a2);
                if (r1>r2) (r1,r2)=(r2,r1);
                int n=0;
                if (r1>0f && r1<=tHigh) roots[n++]=r1;
                if (r2>0f && r2<=tHigh) roots[n++]=r2;
                return n;
            }
            float bn=a2/a3, cn=a1/a3, dn=a0/a3;
            float q=(3f*cn-bn*bn)/9f, r=(9f*bn*cn-27f*dn-2f*bn*bn*bn)/54f;
            float disc3=q*q*q+r*r;
            int m=0;
            if (disc3 > 0f)
            {   // one real root
                float sq=MathF.Sqrt(disc3);
                float cu=RealCbrt(r+sq), cv=RealCbrt(r-sq);
                float t=cu+cv-bn/3f;
                if (t>0f && t<=tHigh) roots[m++]=t;
            }
            else
            {   // three real roots (trigonometric form; includes disc3==0 double/tiple cases)
                float rq=MathF.Sqrt(-q*q*q);
                if (rq < 1e-12f)
                {   // q==0: cubic degenerates to t^2(bn? ...) — linear factor: t = cbrt(-dn)-bn/3
                    float cu=RealCbrt(r); float t=cu-bn/3f;
                    if (t>0f && t<=tHigh) roots[m++]=t;
                }
                else
                {
                    float phi=MathF.Acos(Math.Clamp(r/rq,-1f,1f))/3f;
                    float cb=2f*MathF.Sqrt(-q);
                    for(int i=0;i<3;i++)
                    {
                        float t=cb*MathF.Cos(phi+2f*MathF.PI*i/3f)-bn/3f;
                        if (t>0f && t<=tHigh) roots[m++]=t;
                    }
                    for(int i=1;i<m;i++){ float v2=roots[i]; int j=i-1; while(j>=0&&roots[j]>v2){roots[j+1]=roots[j];j--;} roots[j+1]=v2; }
                }
            }
            return m;
        }

        static float RealCbrt(float x) => (x < 0f ? -1f : 1f) * MathF.Pow(MathF.Abs(x), 1f/3f);

        /// <summary>Trajectory-consistent ball-rail TOI: signed distance to the rail line is
        /// quadratic in t (position is quadratic); solve exactly for the smallest positive root.</summary>
        float SweptRailTOI(Ball ball, Rail rail, float tHigh)
        {
            Vector2 n=new Vector2(rail.Inward.X, rail.Inward.Z);
            Vector2 A=new Vector2(rail.A.X, rail.A.Z), P=new Vector2(ball.Position.X, ball.Position.Z);
            Vector2 v=new Vector2(ball.Velocity.X, ball.Velocity.Z);
            var bb=ball; Vector2 acc=Accel(ref bb);
            float c0=Vector2.Dot(P-A,n)-ball.Radius;   // >0 separated, <=0 touching/penetrating
            float b1=Vector2.Dot(v,n);
            float q2=0.5f*Vector2.Dot(acc,n);
            if (c0 <= 0f) return (b1<0f) ? 0f : float.PositiveInfinity;
            float t=float.PositiveInfinity;
            if (MathF.Abs(q2) < 1e-9f) { if (b1 < -1e-9f) t = -c0/b1; }
            else {
                float disc=b1*b1-4f*q2*c0;
                if (disc >= 0f){ float sq=MathF.Sqrt(disc); float r1=(-b1-sq)/(2f*q2), r2=(-b1+sq)/(2f*q2);
                    float best=float.PositiveInfinity; if(r1>1e-9f) best=r1; if(r2>1e-9f && r2<best) best=r2; t=best; }
            }
            if (!(t>=0f) || t>tHigh) return float.PositiveInfinity;
            Vector2 hit=P+v*t+0.5f*acc*t*t; Vector2 AB=new Vector2(rail.B.X,rail.B.Z)-A;
            float tt=Vector2.Dot(hit-A,AB)/Math.Max(1e-9f,AB.LengthSquared());
            return (tt>=0f&&tt<=1f)? t : float.PositiveInfinity;
        }

        void ResolveBallBall(ref Ball a, ref Ball b)
        {
            Vector2 d=new Vector2(b.Position.X-a.Position.X, b.Position.Z-a.Position.Z);
            float dist=d.Length(); float minD=a.Radius+b.Radius;
            Vector2 n = dist>1e-7f ? d/dist : FallbackNormal(a,b);
            if (dist < minD) { float pen=minD-dist; float im=1/a.Mass, jm=1/b.Mass, tot=im+jm; a.Position-=new Vector3(n.X,0,n.Y)*(pen*im/tot); b.Position+=new Vector3(n.X,0,n.Y)*(pen*jm/tot); }
            Vector2 va=new Vector2(a.Velocity.X,a.Velocity.Z), vb=new Vector2(b.Velocity.X,b.Velocity.Z);
            float vn=Vector2.Dot(va-vb,n);
            if (vn<=0f) { Classify(ref a); Classify(ref b); return; }
            float e=Profile.BallBallRestitution; float invMa=1/a.Mass, invMb=1/b.Mass;
            float jn=(1+e)*vn/(invMa+invMb);
            va-=n*jn*invMa; vb+=n*jn*invMb;
            // PLANAR tangential throw with rotational effective mass kT=3.5*(invMa+invMb) (I=2/5 mR^2)
            if (Profile.BallBallFriction>0f){
                Vector2 t2=new Vector2(-n.Y, n.X);
                float relT=Vector2.Dot(va-vb,t2) - (a.AngularVelocity.Y*a.Radius + b.AngularVelocity.Y*b.Radius);
                if (MathF.Abs(relT)>1e-6f){
                    float kT=3.5f*(invMa+invMb);
                    float jt=MathF.Min(Profile.BallBallFriction*jn, MathF.Abs(relT)/kT);
                    Vector2 Jt=(relT>0f?-1f:1f)*t2*jt;
                    va+=Jt*invMa; vb-=Jt*invMb;
                    // angular (about Y): rA=+Ra*n & impulse +Jt ; rB=-Rb*n & impulse -Jt.
                    // tau_y = rz*Fx - rx*Fz  => both balls get the SAME torquePerRadius sign.
                    float torquePerRadius = n.Y*Jt.X - n.X*Jt.Y;
                    float tauA=a.Radius*torquePerRadius;
                    float tauB=b.Radius*torquePerRadius;
                    a.AngularVelocity=new Vector3(a.AngularVelocity.X, a.AngularVelocity.Y + tauA/(0.4f*a.Mass*a.Radius*a.Radius), a.AngularVelocity.Z);
                    b.AngularVelocity=new Vector3(b.AngularVelocity.X, b.AngularVelocity.Y + tauB/(0.4f*b.Mass*b.Radius*b.Radius), b.AngularVelocity.Z);
                }
            }
            a.Velocity=new Vector3(va.X,0,va.Y); b.Velocity=new Vector3(vb.X,0,vb.Y);
            Classify(ref a); Classify(ref b);
        }

        void ResolveCushion(ref Ball ball, Rail rail)
        {
            Vector3 inward=new Vector3(rail.Inward.X,0,rail.Inward.Z);
            Vector2 c=ClosestPointOnSeg(new Vector2(ball.Position.X,ball.Position.Z), new Vector2(rail.A.X,rail.A.Z), new Vector2(rail.B.X,rail.B.Z));
            ball.Position=new Vector3(c.X+inward.X*ball.Radius, ball.Radius, c.Y+inward.Z*ball.Radius);
            float vn=Vector3.Dot(ball.Velocity,inward);
            if (vn<0){
                float e=Profile.CushionRestitution; ball.Velocity-=(1+e)*vn*inward;
                Vector3 vt=ball.Velocity-Vector3.Dot(ball.Velocity,inward)*inward;
                ball.Velocity=Vector3.Dot(ball.Velocity,inward)*inward+vt*(1-Profile.CushionFriction*0.1f);
                Vector3 tangent=new Vector3(-inward.Z,0,inward.X);
                ball.Velocity+=tangent*(Profile.CushionGear*ball.AngularVelocity.Y*ball.Radius);
                ball.AngularVelocity*=Profile.CushionSpinLoss;
                if (Profile.CushionThrowCoeff!=0f){ float ts=vt.Length(); if(ts>1e-5f) ball.Velocity-=vt/ts*(Profile.CushionThrowCoeff*(-vn)); }
                Classify(ref ball);
            }
        }
    }
}
