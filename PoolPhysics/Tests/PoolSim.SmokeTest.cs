using System;
using System.Numerics;
using PoolPhysics.Sim;
class Smoke {
  static void Main(){
    var table=new TableSpecData{PlayfieldSize=new Vector2(2.54f,1.27f)};
    table.BuildRectRails();
    var prof=new PhysicsProfile();
    var sim=new PoolSimulator(table,prof);
    var cue=new Ball{Position=new Vector3(0.4f,prof.PoolBallRadius,0.635f),Radius=prof.PoolBallRadius,Mass=prof.BallMass,Active=true};
    sim.Balls.Add(cue);
    // shoot straight along +X at 2 m/s with a bit of topspin
    var b=sim.Balls[0]; CueStrike.Apply(ref b,new Vector3(1,0,0),2.0f,new Vector2(0,0.3f),prof); sim.Balls[0]=b;
    float t=0; int steps=0;
    while(!sim.AllAtRest && t<20f){ sim.Substep(prof.Substep); t+=prof.Substep; steps++; }
    var end=sim.Balls[0];
    Console.WriteLine($"rest at t={t:F3}s steps={steps} pos=({end.Position.X:F3},{end.Position.Z:F3}) traveled={(end.Position.X-0.4f):F3}m");
    // determinism: same shot again should give identical result
    var sim2=new PoolSimulator(table,prof);
    var c2=new Ball{Position=new Vector3(0.4f,prof.PoolBallRadius,0.635f),Radius=prof.PoolBallRadius,Mass=prof.BallMass,Active=true};
    sim2.Balls.Add(c2); var d=sim2.Balls[0]; CueStrike.Apply(ref d,new Vector3(1,0,0),2.0f,new Vector2(0,0.3f),prof); sim2.Balls[0]=d;
    float t2=0; while(!sim2.AllAtRest && t2<20f){ sim2.Substep(prof.Substep); t2+=prof.Substep; }
    Console.WriteLine($"determinism match: {(Math.Abs(sim.Balls[0].Position.X-sim2.Balls[0].Position.X)<1e-6f && Math.Abs(sim.Balls[0].Position.Z-sim2.Balls[0].Position.Z)<1e-6f)}");
  }
}
