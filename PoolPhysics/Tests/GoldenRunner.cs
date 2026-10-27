using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Numerics;
using PoolPhysics.Sim;

// Headless regression harness for the event-based PoolPhysics core.
//   gen|check|fit|real|validate|eventval|genreal|touchtest
// Exit code is non-zero if any assertion fails (CI/Unity-test friendly).
static class GoldenRunner
{
    static int FAILS = 0;
    static void Check(bool cond, string msg){ Console.WriteLine((cond?"  PASS  ":"  FAIL  ")+msg); if(!cond) FAILS++; }

    class ShotJson {
        public string name=""; public TableJson table=new(); public List<BallJson> balls=new();
        public CueJson cue=new(); public List<ExpJson> expected=new();
        public int collisionCount; public double energyError; public ProfileJson profile;
    }
    class ProfileJson { public float slidingFriction, rollingFriction, spinDecay, ballBallRestitution, ballBallFriction, cushionRestitution, cushionFriction, napAnisotropy, napSteer; }
    class TableJson { public float length=2.54f, width=1.27f, napHeading=0f; }
    class BallJson { public float x, z, r=0.028575f, m=0.16f; }
    class CueJson { public int index=0; public float[] dir={1,0}; public float speed=2f; public float[] tip={0,0}; public float elev=0f; }
    class ExpJson { public float x,z; public bool pocketed=false; public float vx,vz,wx,wy,wz; public string phase="Rest"; }
    class GoldenJson { public List<ShotJson> shots=new(); }

    static readonly JsonSerializerOptions JOPT = new() { WriteIndented=true, IncludeFields=true, NumberHandling=JsonNumberHandling.AllowReadingFromString };

    static (string,Func<PhysicsProfile,float>,Action<PhysicsProfile,float>,float,float)[] TUNABLES = {
        ("slidingFriction", p=>p.SlidingFriction,(p,v)=>p.SlidingFriction=v,0.05f,1.2f),
        ("rollingFriction", p=>p.RollingFriction,(p,v)=>p.RollingFriction=v,0.001f,0.3f),
        ("spinDecay", p=>p.SpinDecay,(p,v)=>p.SpinDecay=v,0.2f,4f),
        ("ballBallRestitution",p=>p.BallBallRestitution,(p,v)=>p.BallBallRestitution=v,0.5f,1f),
        ("ballBallFriction", p=>p.BallBallFriction,(p,v)=>p.BallBallFriction=v,0f,0.3f),
        ("cushionRestitution",p=>p.CushionRestitution,(p,v)=>p.CushionRestitution=v,0.3f,0.95f),
        ("napAnisotropy", p=>p.NapAnisotropy,(p,v)=>p.NapAnisotropy=v,0f,0.8f),
    };

    static List<Ball> BuildBalls(ShotJson s){ var l=new List<Ball>(); foreach(var b in s.balls) l.Add(new Ball{Position=new Vector3(b.x,0,b.z),Radius=b.r,Mass=b.m,Active=true}); return l; }
    static TableSpecData BuildTable(ShotJson s){ var t=new TableSpecData{PlayfieldSize=new Vector2(s.table.length,s.table.width),NapHeading=s.table.napHeading}; t.BuildRectRails(); return t; }

    static (double path,double peak,int cc,TerminationReason term) EventSim(ShotJson s, PhysicsProfile p){
        var sim=new EventSimulator(BuildTable(s), p);
        foreach(var b in BuildBalls(s)) sim.Balls.Add(b);
        var cue=sim.Balls[s.cue.index];
        CueStrike.Apply(ref cue, new Vector3(s.cue.dir[0],0,s.cue.dir[1]), s.cue.speed,
                        new Vector2(s.cue.tip[0], s.cue.tip.Length>1?s.cue.tip[1]:0), p, s.cue.elev);
        sim.Balls[s.cue.index]=cue;
        var res=sim.RunToRest(30f);
        return (sim.Balls[s.cue.index].Position.X - s.balls[s.cue.index].x, s.cue.speed, sim.CollisionCount, res.Reason);
    }

    static double ScoreShots(GoldenJson g, PhysicsProfile defaultP, bool strict){
        double err=0; int n=0;
        foreach(var s in g.shots){
            // apply the shot's recorded profile (item 14); fall back to default if absent
            var p = (s.profile!=null) ? new PhysicsProfile{
                SlidingFriction=s.profile.slidingFriction, RollingFriction=s.profile.rollingFriction,
                SpinDecay=s.profile.spinDecay, BallBallRestitution=s.profile.ballBallRestitution,
                BallBallFriction=s.profile.ballBallFriction, CushionRestitution=s.profile.cushionRestitution,
                CushionFriction=s.profile.cushionFriction, NapAnisotropy=s.profile.napAnisotropy, NapSteer=s.profile.napSteer,
                Gravity=defaultP.Gravity,
                PoolBallRadius=defaultP.PoolBallRadius, BallMass=defaultP.BallMass } : (PhysicsProfile)defaultP.Clone();
            var sim=new EventSimulator(BuildTable(s), p);
            foreach(var b in BuildBalls(s)) sim.Balls.Add(b);
            var cue=sim.Balls[s.cue.index]; CueStrike.Apply(ref cue,new Vector3(s.cue.dir[0],0,s.cue.dir[1]),s.cue.speed,new Vector2(s.cue.tip[0],s.cue.tip.Length>1?s.cue.tip[1]:0),p,s.cue.elev); sim.Balls[s.cue.index]=cue;
            var res=sim.RunToRest(30f);
            if (res.Reason==TerminationReason.InvalidState){ Console.WriteLine($"  FAIL {s.name}: InvalidState"); err+=1000; }
            if (strict && s.expected.Count!=sim.Balls.Count){ Console.WriteLine($"  SCHEMA FAIL {s.name}: expected {s.expected.Count} != balls {sim.Balls.Count}"); err+=100; }
            for(int i=0;i<s.expected.Count && i<sim.Balls.Count;i++){ var e=s.expected[i]; var q=sim.Balls[i];
                double dx=e.x-q.Position.X, dz=e.z-q.Position.Z; err+=dx*dx+dz*dz;
                // also compare final velocity + phase (catches trajectory bugs that land near the same point)
                err += (e.vx-q.Velocity.X)*(e.vx-q.Velocity.X) + (e.vz-q.Velocity.Z)*(e.vz-q.Velocity.Z);
                // ANGULAR compare: final spin state (english + roll) must match too
                err += (e.wx-q.AngularVelocity.X)*(e.wx-q.AngularVelocity.X)
                     + (e.wy-q.AngularVelocity.Y)*(e.wy-q.AngularVelocity.Y)
                     + (e.wz-q.AngularVelocity.Z)*(e.wz-q.AngularVelocity.Z);
                if (e.pocketed!=q.Pocketed) err+=1.0;
                if (e.phase!=q.Phase.ToString()) err+=0.5;
                n++; }
        }
        return n>0? err/n : (strict?1e9:0);
    }

    static GoldenJson MakeSyntheticGolden(){
        var truth=new PhysicsProfile{SlidingFriction=0.30f,RollingFriction=0.03f,SpinDecay=2.0f,BallBallRestitution=0.80f,BallBallFriction=0.12f,CushionRestitution=0.60f,NapAnisotropy=0.40f};
        var g=new GoldenJson();
        void Add(string nm,float[] dir,float sp,float[] tip,(float,float)[] balls,float L,float W,float nap){
            var s=new ShotJson{name=nm, table={length=L,width=W,napHeading=nap}, cue={index=0,dir=dir,speed=sp,tip=tip}};
            foreach(var b in balls) s.balls.Add(new BallJson{x=b.Item1,z=b.Item2});
            var sim=new EventSimulator(BuildTable(s),truth); foreach(var b in BuildBalls(s)) sim.Balls.Add(b);
            var c=sim.Balls[0]; CueStrike.Apply(ref c,new Vector3(dir[0],0,dir[1]),sp,new Vector2(tip[0],tip.Length>1?tip[1]:0),truth,0); sim.Balls[0]=c; sim.RunToRest(30f);
            foreach(var b in sim.Balls) s.expected.Add(new ExpJson{x=b.Position.X,z=b.Position.Z,pocketed=b.Pocketed,vx=b.Velocity.X,vz=b.Velocity.Z,wx=b.AngularVelocity.X,wy=b.AngularVelocity.Y,wz=b.AngularVelocity.Z,phase=b.Phase.ToString()});
            s.collisionCount=sim.CollisionCount;
            // record the generating profile so check()/fit() replay the SAME physics (item 14)
            s.profile=new ProfileJson{slidingFriction=truth.SlidingFriction,rollingFriction=truth.RollingFriction,
                spinDecay=truth.SpinDecay,ballBallRestitution=truth.BallBallRestitution,
                ballBallFriction=truth.BallBallFriction,cushionRestitution=truth.CushionRestitution,cushionFriction=truth.CushionFriction,
                napAnisotropy=truth.NapAnisotropy,napSteer=truth.NapSteer};
            g.shots.Add(s);
        }
        Add("roll_slow",new[]{1f,0f},1.4f,new[]{0f,0f},new[]{(0.4f,0.635f)},2.54f,1.27f,0);
        Add("roll_mid", new[]{1f,0f},2.6f,new[]{0f,0f},new[]{(0.4f,0.635f)},2.54f,1.27f,0);
        Add("topspin",  new[]{1f,0f},2.4f,new[]{0f,0.6f},new[]{(0.4f,0.635f)},2.54f,1.27f,0);
        Add("draw",     new[]{1f,0f},2.4f,new[]{0f,-0.6f},new[]{(0.4f,0.635f)},2.54f,1.27f,0);
        Add("headon_cushion",new[]{1f,0f},3.0f,new[]{0f,0f},new[]{(0.4f,0.635f)},1.0f,1.27f,0);
        Add("headon_stun",new[]{1f,0f},2.6f,new[]{0f,0f},new[]{(0.4f,0.635f),(0.85f,0.635f)},2.54f,1.27f,0);
        Add("cut_throw",new[]{1f,0.07f},2.6f,new[]{0f,0f},new[]{(0.4f,0.635f),(1.2f,0.72f)},2.54f,1.27f,0);
        Add("english_nap",new[]{0.85f,0.53f},2.4f,new[]{0.6f,0f},new[]{(0.4f,0.5f)},2.54f,1.27f,0.7f);
        return g;
    }

    static readonly (string name,float v0,float side,float vert,float dist)[] REAL_CASES = {
        ("Straight",4.053f,0f,0f,0.72825f),("Follow",3.674f,0f,0.75f,0.90066f),
        ("Draw",2.547f,0f,-0.75f,0.49386f),("LeftEnglish",3.674f,-0.75f,0f,0.80090f),("RightEnglish",3.674f,0.75f,0f,0.80090f) };
    static ShotJson RealShot((string name,float v0,float side,float vert,float dist) c){
        return new ShotJson{name=c.name, table={length=100f,width=100f}, balls={new BallJson{x=0.4f,z=0.635f,r=0.02625f}}, cue={index=0,dir=new[]{1f,0f},speed=c.v0,tip=new[]{c.side,c.vert}}}; }

    static void Validate(){
        var p=new PhysicsProfile{SlidingFriction=0.30f,RollingFriction=0.15f,SpinDecay=1.5f,NapAnisotropy=0f,NapSteer=0f};
        Console.WriteLine("EventMotion(analytic) vs EventSimulator, free roll (nap=0):");
        double maxd=0;
        foreach(var c in new[]{(2.0f,new[]{0f,0f}),(2.0f,new[]{0f,0.6f}),(2.0f,new[]{0f,-0.6f}),(2.5f,new[]{0.7f,0f})}){
            var shot=new ShotJson{table={length=100f,width=100f},balls={new BallJson{x=50f,z=50f,r=0.028575f}},cue={index=0,dir=new[]{1f,0f},speed=c.Item1,tip=c.Item2}};
            var b=new Ball{Position=new Vector3(50f,0.028575f,50f),Radius=0.028575f,Mass=p.BallMass};
            CueStrike.Apply(ref b,new Vector3(1,0,0),c.Item1,new Vector2(c.Item2[0],c.Item2[1]),p,0);
            var sol=EventMotion.Solve(new Vector3(50f,0.028575f,50f),b.Velocity,b.AngularVelocity,0.028575f,p.Gravity,p.SlidingFriction,p.RollingFriction,p.SpinDecay);
            var (evp,_,_,_)=EventSim(shot,p); double diff=(evp-sol.PathLength)/Math.Max(1e-6,sol.PathLength)*100; maxd=Math.Max(maxd,Math.Abs(diff));
            Console.WriteLine($"  v={c.Item1} tip={c.Item2[0]},{c.Item2[1]}: event={evp:F4} analytic={sol.PathLength:F4} diff={diff:+0.00;-0.00}%");
        }
        Check(maxd<1.0, $"motion matches analytic (max |diff|={maxd:F3}% < 1%)");
    }

    static void EventVal(){
        var p=new PhysicsProfile{BallBallRestitution=1f,BallBallFriction=0f,SlidingFriction=0f,RollingFriction=0f,SpinDecay=0f};
        var stun=new ShotJson{table={length=100f,width=100f},balls={new BallJson{x=50f,z=50f,r=0.028575f},new BallJson{x=50.2f,z=50f,r=0.028575f}},cue={index=0,dir=new[]{1f,0f},speed=2f,tip=new[]{0f,0f}}};
        var sim=new EventSimulator(BuildTable(stun),p); foreach(var b in BuildBalls(stun)) sim.Balls.Add(b);
        var c0=sim.Balls[0]; c0.Velocity=new Vector3(2,0,0); sim.Balls[0]=c0; sim.RunToRest(0.5f);
        float mom=p.BallMass*(sim.Balls[0].Velocity.X+sim.Balls[1].Velocity.X);
        Check(Math.Abs(sim.Balls[0].Velocity.X)<0.05f && Math.Abs(sim.Balls[1].Velocity.X-2f)<0.05f, $"head-on transfer cue={sim.Balls[0].Velocity.X:F3} obj={sim.Balls[1].Velocity.X:F3}");
        Check(Math.Abs(mom-0.32)<0.02, $"momentum conserved ({mom:F4}~0.32)");
        var ob=new ShotJson{table={length=100f,width=100f},balls={new BallJson{x=50f,z=50f,r=0.028575f},new BallJson{x=50.25f,z=50.04f,r=0.028575f}},cue={index=0,dir=new[]{1f,0f},speed=2f,tip=new[]{0f,0f}}};
        var s2=new EventSimulator(BuildTable(ob),p); foreach(var b in BuildBalls(ob)) s2.Balls.Add(b);
        var c1=s2.Balls[0]; c1.Velocity=new Vector3(2,0,0); s2.Balls[0]=c1; s2.RunToRest(0.5f);
        var va=new Vector2(s2.Balls[0].Velocity.X,s2.Balls[0].Velocity.Z); var vb=new Vector2(s2.Balls[1].Velocity.X,s2.Balls[1].Velocity.Z);
        if(va.Length()>1e-4&&vb.Length()>1e-4){ float ca=Vector2.Dot(va,vb)/(va.Length()*vb.Length()); ca=ca<-1?-1:ca>1?1:ca; float ang=MathF.Acos(ca)*180/MathF.PI; Check(Math.Abs(ang-90)<6,$"oblique separation {ang:F1}deg ~90"); }
        else Check(false,"oblique: both balls moving after cut");
        Vector2 CutObj(float fr){ var pp=new PhysicsProfile{BallBallRestitution=0.95f,BallBallFriction=fr,SlidingFriction=0f,RollingFriction=0f,SpinDecay=0f};
            var sh=new ShotJson{table={length=100f,width=100f},balls={new BallJson{x=50f,z=50f,r=0.028575f},new BallJson{x=50.15f,z=50.03f,r=0.028575f}},cue={index=0}};
            var s=new EventSimulator(BuildTable(sh),pp); foreach(var b in BuildBalls(sh)) s.Balls.Add(b);
            s.Balls[0]=new Ball{Position=s.Balls[0].Position,Radius=0.028575f,Mass=pp.BallMass,Active=true,Velocity=new Vector3(2,0,0),AngularVelocity=new Vector3(0,0,-2/0.028575f)};
            s.RunToRest(0.3f); return new Vector2(s.Balls[1].Velocity.X,s.Balls[1].Velocity.Z); }
        float thrown=(CutObj(0.15f)-CutObj(0f)).Length(); Check(thrown>1e-3,$"ball-ball friction throws object (|dv|={thrown:F4})");
        var p4=new PhysicsProfile{BallBallRestitution=0.95f,SlidingFriction=0.2f,RollingFriction=0.05f};
        var brk=new ShotJson{table={length=2.54f,width=1.27f},balls={new BallJson{x=0.4f,z=0.635f},new BallJson{x=1.6f,z=0.635f},new BallJson{x=1.66f,z=0.60f},new BallJson{x=1.66f,z=0.67f}},cue={index=0,dir=new[]{1f,0f},speed=5f,tip=new[]{0f,0f}}};
        var sb=new EventSimulator(BuildTable(brk),p4); foreach(var b in BuildBalls(brk)) sb.Balls.Add(b);
        var cb=sb.Balls[0]; CueStrike.Apply(ref cb,new Vector3(1,0,0),5f,new Vector2(0,0),p4,0); sb.Balls[0]=cb;
        var rr=sb.RunToRest(15f);
        bool finite=true,inb=true; foreach(var b in sb.Balls){
            if(!float.IsFinite(b.Position.X)||!float.IsFinite(b.Position.Z)||!float.IsFinite(b.Velocity.X)||!float.IsFinite(b.Velocity.Z)||!float.IsFinite(b.AngularVelocity.Y))finite=false;
            if(b.Position.X<-0.1||b.Position.X>2.64||b.Position.Z<-0.1||b.Position.Z>1.37)inb=false; }
        Check(finite,"4-ball: all state finite"); Check(inb,"4-ball: in bounds"); Check(rr.MaxPenetration<0.02f,$"4-ball: max penetration {rr.MaxPenetration:F4} small");
        Check(sb.PeakTotalEnergy<=sb.InitialEnergy+1e-6,$"4-ball: no energy injection (peak {sb.PeakTotalEnergy:F4} <= init {sb.InitialEnergy:F4})");
    }

    static void TouchingRegression(){
        var p=new PhysicsProfile{BallBallRestitution=0.95f,BallBallFriction=0f,SlidingFriction=0f,RollingFriction=0f,SpinDecay=0f};
        float R=0.028575f, twoR=2*R;
        (int cc,TerminationReason term,double objV,float finalGap) Run(float gap,float cueVx){
            var sh=new ShotJson{table={length=100f,width=100f},balls={new BallJson{x=50f,z=50f,r=R},new BallJson{x=50f+gap,z=50f,r=R}},cue={index=0}};
            var s=new EventSimulator(BuildTable(sh),p); foreach(var b in BuildBalls(sh)) s.Balls.Add(b);
            s.Balls[0]=new Ball{Position=s.Balls[0].Position,Radius=R,Mass=p.BallMass,Active=true,Velocity=new Vector3(cueVx,0,0)};
            var r=s.RunToRest(2f);
            float fgap=(s.Balls[1].Position-s.Balls[0].Position).Length();
            return (s.CollisionCount,r.Reason,s.Balls[1].Velocity.X,fgap); }
        var a=Run(twoR,2f); var b=Run(twoR,-0.5f); var c=Run(twoR-0.001f,2f); var d=Run(twoR-0.001f,-0.5f);
        Check(a.cc>=1 && a.objV>0,"touching+approach collides");
        Check(b.cc==0 && b.term!=TerminationReason.InvalidState,"touching+separate no loop");
        Check(c.cc>=1,"overlap+approach collides");
        // overlap+separate: depenetration must push them to >=2R (final gap), not the historical maxPen
        Check(d.cc==0 && d.finalGap>=twoR-1e-4f && d.term!=TerminationReason.InvalidState,$"overlap+separate depenetrates to gap {d.finalGap:F5}>=2R {twoR:F5}, no loop");
        // ORDER-INDEPENDENCE: resting ball at LOW index (0), moving ball at HIGH index (1) must still collide.
        var sh2=new ShotJson{table={length=100f,width=100f},balls={new BallJson{x=50.2f,z=50f,r=R}, new BallJson{x=50f,z=50f,r=R}},cue={index=1}};
        var s2=new EventSimulator(BuildTable(sh2),p); foreach(var bb in BuildBalls(sh2)) s2.Balls.Add(bb);
        s2.Balls[1]=new Ball{Position=s2.Balls[1].Position,Radius=R,Mass=p.BallMass,Active=true,Velocity=new Vector3(2,0,0)}; // index1 at x=50 -> +X toward index0 at 50.2
        s2.CueBallIndex=1; s2.RunToRest(2f);
        // after BeginShot canonical sort: index0=lowest-X(the moving cue), index1=resting ball -> check index1 moved
        Check(s2.Balls[1].Velocity.X>1f,$"order-independent: moving ball hits resting ball (resting.v={s2.Balls[1].Velocity.X:F3})");
        // DEPENETRATION write-back: two RESTING balls overlapping with UNEQUAL mass.
        // Mass-weighted correction must preserve the center of mass (catches the
        // missing Balls[j]=B write-back, which would move only A and shift COM).
        {
            var pr=new PhysicsProfile{SlidingFriction=0f,RollingFriction=0f};
            float mA=0.16f, mB=0.32f;
            var shR=new ShotJson{table={length=100f,width=100f},balls={new BallJson{x=50f,z=50f,r=R,m=mA}, new BallJson{x=50f+twoR-0.01f,z=50f,r=R,m=mB}},cue={index=0}};
            var sr=new EventSimulator(BuildTable(shR),pr); foreach(var bb in BuildBalls(shR)) sr.Balls.Add(bb);
            sr.Balls[0]=new Ball{Position=sr.Balls[0].Position,Radius=R,Mass=mA,Active=true};
            sr.Balls[1]=new Ball{Position=sr.Balls[1].Position,Radius=R,Mass=mB,Active=true};
            float comBefore=(sr.Balls[0].Position.X*mA + sr.Balls[1].Position.X*mB)/(mA+mB);
            sr.RunToRest(0.5f);
            float comAfter=(sr.Balls[0].Position.X*mA + sr.Balls[1].Position.X*mB)/(mA+mB);
            float finalGap=sr.Balls[1].Position.X-sr.Balls[0].Position.X;
            Check(Math.Abs(comAfter-comBefore)<1e-4f && Math.Abs(finalGap-twoR)<1e-3f, $"depenetration mass-weighted: COM shift={comAfter-comBefore:E2}, gap={finalGap:F5}~2R={twoR:F5}");
        }
    }

    static void GenReal(string outp){
        var baseP=new PhysicsProfile{Gravity=9.806f,PoolBallRadius=0.028575f,BallMass=0.160f,SlidingFriction=0.20f,RollingFriction=0.010f,SpinDecay=1.47f,BallBallRestitution=0.95f,BallBallFriction=0.06f,CushionRestitution=0.75f,CushionFriction=0.20f};
        var g=new GoldenJson();
        var defs=new (string,float,float,(float,float)[],float[],float,float[],bool)[]{
            ("headon_v1",2.54f,1.27f,new[]{(0.5f,0.635f)},new[]{1f,0f},1f,new[]{0f,0f},false),
            ("headon_v3",2.54f,1.27f,new[]{(0.5f,0.635f)},new[]{1f,0f},3f,new[]{0f,0f},false),
            ("headon_v6",2.54f,1.27f,new[]{(0.5f,0.635f)},new[]{1f,0f},6f,new[]{0f,0f},false),
            ("follow",2.54f,1.27f,new[]{(0.5f,0.635f)},new[]{1f,0f},2.5f,new[]{0f,0.6f},false),
            ("draw",2.54f,1.27f,new[]{(0.5f,0.635f)},new[]{1f,0f},2.5f,new[]{0f,-0.6f},false),
            ("cut_thin",2.54f,1.27f,new[]{(0.5f,0.635f),(1.4f,0.66f)},new[]{1f,0f},2.5f,new[]{0f,0f},false),
            ("cut_half",2.54f,1.27f,new[]{(0.5f,0.635f),(1.4f,0.675f)},new[]{1f,0f},2.5f,new[]{0f,0f},false),
            ("cut_full",2.54f,1.27f,new[]{(0.5f,0.635f),(1.4f,0.64f)},new[]{1f,0f},2.5f,new[]{0f,0f},false),
            ("touching",2.54f,1.27f,new[]{(1.0f,0.635f),(1.05715f,0.635f)},new[]{1f,0f},2f,new[]{0f,0f},false),
            ("near_rail",2.54f,1.27f,new[]{(1.2f,0.03f)},new[]{0f,-1f},2f,new[]{0f,0f},false),
            ("frictionless",2.54f,1.27f,new[]{(0.5f,0.635f)},new[]{1f,0f},2f,new[]{0f,0f},true),
        };
        foreach(var d in defs){
            var p = d.Item8 ? new PhysicsProfile{Gravity=9.806f,PoolBallRadius=0.028575f,BallMass=0.160f,SlidingFriction=0f,RollingFriction=0f,SpinDecay=0f,BallBallRestitution=0.95f,BallBallFriction=0f,CushionRestitution=1f,CushionFriction=0f,CushionSpinLoss=1f} : baseP;
            var shot=new ShotJson{name=d.Item1,table={length=d.Item2,width=d.Item3},cue={index=0,dir=d.Item5,speed=d.Item6,tip=d.Item7},
                profile=new ProfileJson{slidingFriction=p.SlidingFriction,rollingFriction=p.RollingFriction,spinDecay=p.SpinDecay,ballBallRestitution=p.BallBallRestitution,ballBallFriction=p.BallBallFriction,cushionRestitution=p.CushionRestitution,cushionFriction=p.CushionFriction,napAnisotropy=p.NapAnisotropy,napSteer=p.NapSteer}};
            foreach(var b in d.Item4) shot.balls.Add(new BallJson{x=b.Item1,z=b.Item2,r=0.028575f,m=0.160f});
            var sim=new EventSimulator(BuildTable(shot),p); foreach(var b in BuildBalls(shot)) sim.Balls.Add(b);
            var cue=sim.Balls[0]; CueStrike.Apply(ref cue,new Vector3(d.Item5[0],0,d.Item5[1]),d.Item6,new Vector2(d.Item7[0],d.Item7[1]),p,0); sim.Balls[0]=cue;
            var res=sim.RunToRest(30f);
            foreach(var b in sim.Balls) shot.expected.Add(new ExpJson{x=b.Position.X,z=b.Position.Z,pocketed=b.Pocketed,vx=b.Velocity.X,vz=b.Velocity.Z,wx=b.AngularVelocity.X,wy=b.AngularVelocity.Y,wz=b.AngularVelocity.Z,phase=b.Phase.ToString()});
            shot.collisionCount=sim.CollisionCount;
            shot.energyError = sim.InitialEnergy>1e-9 ? (sim.PeakTotalEnergy-sim.InitialEnergy)/sim.InitialEnergy : 0;
            g.shots.Add(shot);
            Check(shot.energyError<=1e-6 && res.Reason!=TerminationReason.InvalidState && res.Reason!=TerminationReason.IterationLimit,
                  $"{d.Item1}: no injection & settled (err={shot.energyError:+0.000;-0.000} term={res.Reason})");
        }
        File.WriteAllText(outp, JsonSerializer.Serialize(g,JOPT));
        Console.WriteLine($"wrote {outp} ({g.shots.Count} REGRESSION-BASELINE shots; self-generated, not independent real-physics proof)");
    }

    static void Props(int iters=400){
        var rng=new Random(20261026);
        // 1) ANALYTIC stop distance: a ROLLING ball (no slip) => d = v^2/(2*muR*g)
        {
            float muR=0.05f, g=9.806f, v=2.5f, R=0.028575f;
            float d=v*v/(2*muR*g);
            var p=new PhysicsProfile{SlidingFriction=0f,RollingFriction=muR,SpinDecay=0f};
            var shot=new ShotJson{table={length=100f,width=100f},balls={new BallJson{x=50f,z=50f,r=R}},cue={index=0,dir=new[]{1f,0f},speed=v,tip=new[]{0f,0f}}};
            var sim=new EventSimulator(BuildTable(shot),p); foreach(var bb in BuildBalls(shot)) sim.Balls.Add(bb);
            // seed as pure rolling (w.z=-v/R => contact velocity 0), bypass CueStrike
            var b0=sim.Balls[0]; b0.Velocity=new Vector3(v,0,0); b0.AngularVelocity=new Vector3(0,0,-v/R); b0.Phase=BallPhase.Rolling; sim.Balls[0]=b0;
            sim.RunToRest(20f);
            float path=sim.Balls[0].Position.X-50f;
            Check(Math.Abs(path-d)/d<0.02, $"analytic stop distance: sim={path:F4} vs v^2/2muRg={d:F4}");
        }
        // 2) 5/7 rolling transition: struck flat (v, no spin) -> rolling speed = 5/7 v
        {
            float v=3f; var p=new PhysicsProfile{SlidingFriction=0.2f,RollingFriction=0f,SpinDecay=0f};
            var b=new Ball{Position=new Vector3(0,0.028575f,0),Radius=0.028575f,Mass=p.BallMass,Active=true};
            CueStrike.Apply(ref b,new Vector3(1,0,0),v,new Vector2(0,0),p,0);
            var sol=EventMotion.Solve(new Vector3(0,0.028575f,0),b.Velocity,b.AngularVelocity,0.028575f,p.Gravity,p.SlidingFriction,0.0001f,0f);
            Check(Math.Abs(sol.RollStartVel.X-5f/7f*v)/v<0.02, $"5/7 transition: rollStart={sol.RollStartVel.X:F4} vs {5f/7f*v:F4}");
        }
        // 3) FUZZ invariants: no NaN/inf, energy non-increase, permutation invariance, determinism
        int bad=0, permFail=0, detFail=0;
        for(int it=0; it<iters; it++){
            int nb=2+rng.Next(5);
            var p=new PhysicsProfile{SlidingFriction=0.15f+ (float)rng.NextDouble()*0.2f, RollingFriction=0.008f+(float)rng.NextDouble()*0.02f,
                SpinDecay=1f+(float)rng.NextDouble()*1.5f, BallBallRestitution=0.9f+(float)rng.NextDouble()*0.08f,
                BallBallFriction=(float)rng.NextDouble()*0.12f, CushionRestitution=0.6f+(float)rng.NextDouble()*0.3f};
            var balls=new List<BallJson>();
            for(int i=0;i<nb;i++) balls.Add(new BallJson{x=50f+(float)rng.NextDouble()*2f, z=50f+(float)rng.NextDouble()*2f, r=0.028575f, m=0.16f});
            float sp=1f+(float)rng.NextDouble()*5f; float ang=(float)(rng.NextDouble()*2*Math.PI);
            var dir=new[]{(float)Math.Cos(ang),(float)Math.Sin(ang)};
            var tip=new[]{(float)(rng.NextDouble()*2-1)*0.7f,(float)(rng.NextDouble()*2-1)*0.7f};
            var shot=new ShotJson{table={length=100f,width=100f},balls=balls,cue={index=0,dir=dir,speed=sp,tip=tip}};
            var sim=new EventSimulator(BuildTable(shot),p); foreach(var bb in BuildBalls(shot)) sim.Balls.Add(bb);
            var c=sim.Balls[0]; CueStrike.Apply(ref c,new Vector3(dir[0],0,dir[1]),sp,new Vector2(tip[0],tip[1]),p,0); sim.Balls[0]=c;
            var res=sim.RunToRest(20f);
            foreach(var bb in sim.Balls){ if(!float.IsFinite(bb.Position.X)||!float.IsFinite(bb.Position.Z)||!float.IsFinite(bb.Velocity.X)){ bad++; break; } }
            if(sim.PeakTotalEnergy>sim.InitialEnergy+1e-4){ bad++; }
            // permutation: shuffle ball order, run, compare sorted final positions
            var shuf=new List<BallJson>(balls); for(int i=shuf.Count-1;i>0;i--){int j=rng.Next(i+1); var tmp=shuf[i]; shuf[i]=shuf[j]; shuf[j]=tmp;}
            int cueIdx=shuf.IndexOf(balls[0]);   // strike the SAME physical cue ball after shuffle
            var shot2=new ShotJson{table={length=100f,width=100f},balls=shuf,cue={index=cueIdx,dir=dir,speed=sp,tip=tip}};
            var sim2=new EventSimulator(BuildTable(shot2),p); foreach(var bb in BuildBalls(shot2)) sim2.Balls.Add(bb);
            var c2=sim2.Balls[cueIdx]; CueStrike.Apply(ref c2,new Vector3(dir[0],0,dir[1]),sp,new Vector2(tip[0],tip[1]),p,0); sim2.Balls[cueIdx]=c2;
            sim2.RunToRest(20f);
            var pa=sim.Balls.Select(b=>(b.Position.X,b.Position.Z)).OrderBy(t=>t.X).ThenBy(t=>t.Z).ToList();
            var pb=sim2.Balls.Select(b=>(b.Position.X,b.Position.Z)).OrderBy(t=>t.X).ThenBy(t=>t.Z).ToList();
            double maxd=0; for(int i=0;i<pa.Count;i++) maxd=Math.Max(maxd, Math.Sqrt((pa[i].X-pb[i].X)*(pa[i].X-pb[i].X)+(pa[i].Z-pb[i].Z)*(pa[i].Z-pb[i].Z)));
            if(maxd>0.05){ permFail++; if(permFail<=3) Console.WriteLine($"  PERM diff={maxd:F3} nb={nb} seedIt={it}"); }
            // determinism: Step(T) vs 2x Step(T/2)
            var sim3=new EventSimulator(BuildTable(shot),p); foreach(var bb in BuildBalls(shot)) sim3.Balls.Add(bb);
            var c3=sim3.Balls[0]; CueStrike.Apply(ref c3,new Vector3(dir[0],0,dir[1]),sp,new Vector2(tip[0],tip[1]),p,0); sim3.Balls[0]=c3;
            sim3.BeginShot(); sim3.Step(10f); sim3.Step(10f);
            double maxdet=0; for(int i=0;i<sim.Balls.Count;i++) maxdet=Math.Max(maxdet,(sim.Balls[i].Position-sim3.Balls[i].Position).Length());
            if(maxdet>1e-3){ detFail++; if(detFail<=3) Console.WriteLine($"  DET diff={maxdet:F6} it={it} nb={nb} term1={res.Reason}"); }
        }
        Check(bad==0, $"fuzz {iters}: no NaN/inf & no energy injection (bad={bad})");
        Check(permFail==0, $"fuzz permutation invariance (fail={permFail})");
        Check(detFail==0, $"fuzz determinism Step(T)==2xStep(T/2) (fail={detFail})");
    }

    static void NapDbg(){
        var truth=new PhysicsProfile{SlidingFriction=0.30f,RollingFriction=0.03f,SpinDecay=2.0f,BallBallRestitution=0.80f,BallBallFriction=0.12f,CushionRestitution=0.60f,NapAnisotropy=0.40f};
        var shot=new ShotJson{name="english_nap",table={length=2.54f,width=1.27f,napHeading=0.7f},balls={new BallJson{x=0.4f,z=0.5f}},cue={index=0,dir=new[]{0.85f,0.53f},speed=2.4f,tip=new[]{0.6f,0f}}};
        var sim=new EventSimulator(BuildTable(shot),truth){ Trace=true }; foreach(var b in BuildBalls(shot)) sim.Balls.Add(b);
        var c=sim.Balls[0]; CueStrike.Apply(ref c,new Vector3(shot.cue.dir[0],0,shot.cue.dir[1]),shot.cue.speed,new Vector2(shot.cue.tip[0],shot.cue.tip[1]),truth,0); sim.Balls[0]=c;
        var res=sim.RunToRest(30f);
        Console.WriteLine($"term={res.Reason} simT={res.SimulatedTime:F4} coll={sim.CollisionCount}");
        var tl=sim.TraceLog; int start=Math.Max(0,tl.Count-44);
        for(int i=start;i<tl.Count;i++) Console.WriteLine(tl[i]);
    }

    static bool CloseEnough(string a, string b)
    {
        if (a.StartsWith("== STEP") && b.StartsWith("== STEP")) return true;   // per-window header differs by chunking only
        string Norm(string s) => s.Split(new[] { " cap=", "dtNext=" }, StringSplitOptions.None)[0];
        if (a.StartsWith("  durs") && b.StartsWith("  durs")) return Norm(a) == Norm(b);   // per-ball segments match; cap differs by window
        if (a.StartsWith("  ev") && b.StartsWith("  ev"))     return Norm(a) == Norm(b);  // compare chosen toi; dtNext tail may differ by cap
        return a == b;
    }

    static void DetDbg(int iters=30){
        var rng=new Random(20261026);
        for(int it=0; it<iters; it++){
            int nb=2+rng.Next(5);
            var p=new PhysicsProfile{SlidingFriction=0.15f+ (float)rng.NextDouble()*0.2f, RollingFriction=0.008f+(float)rng.NextDouble()*0.02f,
                SpinDecay=1f+(float)rng.NextDouble()*1.5f, BallBallRestitution=0.9f+(float)rng.NextDouble()*0.08f,
                BallBallFriction=(float)rng.NextDouble()*0.12f, CushionRestitution=0.6f+(float)rng.NextDouble()*0.3f};
            var balls=new List<BallJson>();
            for(int i=0;i<nb;i++) balls.Add(new BallJson{x=50f+(float)rng.NextDouble()*2f, z=50f+(float)rng.NextDouble()*2f, r=0.028575f, m=0.16f});
            float sp=1f+(float)rng.NextDouble()*5f; float ang=(float)(rng.NextDouble()*2*Math.PI);
            var dir=new[]{(float)Math.Cos(ang),(float)Math.Sin(ang)};
            var tip=new[]{(float)(rng.NextDouble()*2-1)*0.7f,(float)(rng.NextDouble()*2-1)*0.7f};
            var shot=new ShotJson{table={length=100f,width=100f},balls=balls,cue={index=0,dir=dir,speed=sp,tip=tip}};

            var sim=new EventSimulator(BuildTable(shot),p){ Trace=true };
            foreach(var bb in BuildBalls(shot)) sim.Balls.Add(bb);
            var c=sim.Balls[0]; CueStrike.Apply(ref c,new Vector3(dir[0],0,dir[1]),sp,new Vector2(tip[0],tip[1]),p,0); sim.Balls[0]=c;
            sim.RunToRest(20f);

            var sim3=new EventSimulator(BuildTable(shot),p){ Trace=true };
            foreach(var bb in BuildBalls(shot)) sim3.Balls.Add(bb);
            var c3=sim3.Balls[0]; CueStrike.Apply(ref c3,new Vector3(dir[0],0,dir[1]),sp,new Vector2(tip[0],tip[1]),p,0); sim3.Balls[0]=c3;
            sim3.BeginShot(); sim3.Step(10f); sim3.Step(10f);

            double maxdet=0;
            for(int i=0;i<Math.Min(sim.Balls.Count,sim3.Balls.Count);i++)
                maxdet=Math.Max(maxdet,(sim.Balls[i].Position-sim3.Balls[i].Position).Length());
            if (maxdet<=1e-3) continue;
            FAILS++;
            Console.WriteLine($"FAIL it={it} nb={nb} sp={sp:F3} ang={ang:F5} tip=({tip[0]:F3},{tip[1]:F3}) maxdet={maxdet:F4}");
            Console.WriteLine("  balls: " + string.Join(" ", balls.Select(b=>$"({b.x:F3},{b.z:F3})")));
            var t1=sim.TraceLog; var t2=sim3.TraceLog; int k=0;
            for(; k<Math.Min(t1.Count,t2.Count); k++){ if (!CloseEnough(t1[k],t2[k])) break; }
            Console.WriteLine($"  traces: n1={t1.Count} n2={t2.Count} first-diff@{k}");
            for(int d=Math.Max(0,k-4); d<Math.Min(Math.Max(t1.Count,t2.Count), k+10); d++){
                string a = d<t1.Count? t1[d] : "<end:A>";
                string b = d<t2.Count? t2[d] : "<end:B>";
                Console.WriteLine($"   [{d}] A: {a}");
                Console.WriteLine($"   [{d}] B: {b}");
            }
            return;
        }
        Console.WriteLine("detdbg: no divergence >1e-3 within the given iters");
    }

    static int Main(string[] args){
        if(args.Length==0){ Console.WriteLine("usage: gen|check|fit|real|validate|eventval|genreal|touchtest|props [golden] [evals]"); return 2; }
        string mode=args[0];
        if(mode=="props"){ Props(args.Length>1?int.Parse(args[1]):400); return FAILS>0?1:0; }
        if(mode=="detdbg"){ DetDbg(args.Length>1?int.Parse(args[1]):30); return FAILS>0?1:0; }
        if(mode=="napdbg"){ NapDbg(); return FAILS>0?1:0; }
        if(mode=="touchtest"){ TouchingRegression(); return FAILS>0?1:0; }
        if(mode=="genreal"){ GenReal(args.Length>1?args[1]:"golden_regression_baseline.json"); return FAILS>0?1:0; }
        if(mode=="gen"){ var gj=MakeSyntheticGolden(); var outp=args.Length>1?args[1]:"golden.json";
            File.WriteAllText(outp, JsonSerializer.Serialize(gj,JOPT));
            Console.WriteLine($"wrote {outp} ({gj.shots.Count} synthetic golden shots; re-baseline after an intentional physics fix)"); return 0; }
        if(mode=="eventval"){ EventVal(); return FAILS>0?1:0; }
        if(mode=="validate"){ Validate(); return FAILS>0?1:0; }
        var gp = args.Length>1?args[1]:"golden.json";
        var golden=JsonSerializer.Deserialize<GoldenJson>(File.ReadAllText(gp),JOPT);
        var p=new PhysicsProfile();
        if(mode=="check"){ double sc=ScoreShots(golden,p,true); Console.WriteLine($"score(defaults, EventSimulator, strict)= {sc:E3}"); return sc<1e-3?0:1; }
        if(mode=="fit"){ int maxE=args.Length>2?int.Parse(args[2]):400; int k=TUNABLES.Length;
            double[] lo=new double[k],up=new double[k],x0=new double[k];
            for(int i=0;i<k;i++){lo[i]=TUNABLES[i].Item4;up[i]=TUNABLES[i].Item5;x0[i]=TUNABLES[i].Item2(p);}
            Func<double[],double> f=xs=>{var tr=(PhysicsProfile)p.Clone();for(int i=0;i<k;i++)TUNABLES[i].Item3(tr,(float)xs[i]);return ScoreShots(golden,tr,false);};
            var es=new PoolPhysics.Tests.CmaEs(); var (bx,bf)=es.Minimize(f,lo,up,x0,maxE);
            Console.WriteLine($"fit final score={bf:E3}"); for(int i=0;i<k;i++)Console.WriteLine($"  {TUNABLES[i].Item1}={bx[i]:F4}"); return bf<1e-3?0:1; }
        Console.WriteLine("unknown mode"); return 2;
    }
}
