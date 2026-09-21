using System;
using System.Collections.Generic;
using UnityEngine;

namespace VR147.AAA.Diagnostics
{
    /// <summary>Lightweight runtime sampler: XR pose source -> consumer -> shot sampling.</summary>
    public sealed class M7_4RuntimeLatencySampler : MonoBehaviour
    {
        [Serializable]
        public struct Sample
        {
            public int frame;
            public double poseTimestamp;
            public double consumerTimestamp;
            public double shotTimestamp;
            public double PoseToConsumerMs => (consumerTimestamp - poseTimestamp) * 1000.0;
            public double ConsumerToShotMs => (shotTimestamp - consumerTimestamp) * 1000.0;
            public double PoseToShotMs => (shotTimestamp - poseTimestamp) * 1000.0;
        }

        [SerializeField, Min(16)] private int capacity = 512;
        [SerializeField] private bool captureInEditor = true;
        private readonly List<Sample> samples = new();
        private double poseTimestamp;
        private double consumerTimestamp;
        public IReadOnlyList<Sample> Samples => samples;
        public double LatestPoseToShotMs => samples.Count == 0 ? 0.0 : samples[^1].PoseToShotMs;
        public void MarkPoseSample() => poseTimestamp = Time.realtimeSinceStartupAsDouble;
        public void MarkConsumerSample() => consumerTimestamp = Time.realtimeSinceStartupAsDouble;
        public void MarkShotSample()
        {
            var shot = Time.realtimeSinceStartupAsDouble;
            if (consumerTimestamp < poseTimestamp || shot < consumerTimestamp) return;
            samples.Add(new Sample { frame = Time.frameCount, poseTimestamp = poseTimestamp, consumerTimestamp = consumerTimestamp, shotTimestamp = shot });
            var overflow = samples.Count - Mathf.Max(16, capacity);
            if (overflow > 0) samples.RemoveRange(0, overflow);
        }

        public void Clear() => samples.Clear();
    }
}


