using UnityEngine;
using System;
namespace VR147.AAA.Physics.Golden
{
 [CreateAssetMenu(menuName="147VR/Physics/M4 Ball Collision Golden")]
 public sealed class M4BallCollisionGolden : ScriptableObject
 {
  public string caseId,family,sourceJsonPath,sourceTimestampUtc,sourceUnityVersion;
  public float referenceAngleDeg,measuredNormalMean,measuredNormalStd,targetSpeedMean,targetSpeedStd,strikerSpeedMean,strikerSpeedStd,energyRatioMean,energyRatioStd,deltaVelocityMean,deltaVelocityStd,preRelNormalMean,postRelNormalMean,tolerancePercent=.5f;
  public int repetitions;
  public float[] measuredNormalSamples,targetSpeedSamples,strikerSpeedSamples,energyRatioSamples,deltaVelocitySamples,preRelNormalSamples,postRelNormalSamples;
  public bool IsValid(){return !string.IsNullOrWhiteSpace(caseId)&&!string.IsNullOrWhiteSpace(sourceJsonPath)&&repetitions==5&&measuredNormalSamples?.Length==5&&targetSpeedSamples?.Length==5&&strikerSpeedSamples?.Length==5&&energyRatioSamples?.Length==5&&deltaVelocitySamples?.Length==5&&measuredNormalMean>=0f&&targetSpeedMean>0f&&deltaVelocityMean>0f&&tolerancePercent>0f;}
 }
}
