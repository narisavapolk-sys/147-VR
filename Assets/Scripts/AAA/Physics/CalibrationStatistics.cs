using System.Collections.Generic;
using UnityEngine;

namespace VR147.AAA.Physics
{
    public readonly struct CalibrationStatistics
    {
        public readonly int sampleCount;
        public readonly float meanError;
        public readonly float minError;
        public readonly float maxError;
        public readonly float standardDeviation;
        public readonly float meanPeakSpeedError;
        public readonly float peakSpeedStandardDeviation;
        public readonly int invalidSamples;
        public readonly int peakSpeedValidatedSamples;
        public readonly int peakSpeedInvalidSamples;

        public CalibrationStatistics(IReadOnlyList<ShotCalibrationResult> samples)
        {
            sampleCount = samples?.Count ?? 0;
            if (sampleCount == 0)
            {
                meanError = minError = maxError = standardDeviation = 0f;
                meanPeakSpeedError = peakSpeedStandardDeviation = 0f;
                invalidSamples = 0;
                peakSpeedValidatedSamples = 0;
                peakSpeedInvalidSamples = 0;
                return;
            }

            float sum = 0f;
            float peakSum = 0f;
            minError = float.PositiveInfinity;
            maxError = float.NegativeInfinity;
            int invalid = 0;
            int peakValidated = 0;
            int peakInvalid = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                float value = samples[i].error;
                bool distanceInvalid = float.IsNaN(value) || float.IsInfinity(value);
                bool peakInvalidForSample = samples[i].peakSpeedValidated &&
                    (float.IsNaN(samples[i].peakSpeedError) || float.IsInfinity(samples[i].peakSpeedError));

                if (distanceInvalid)
                    invalid++;

                if (distanceInvalid)
                    continue;

                sum += value;
                minError = Mathf.Min(minError, value);
                maxError = Mathf.Max(maxError, value);

                if (samples[i].peakSpeedValidated)
                {
                    peakValidated++;
                    if (peakInvalidForSample)
                    {
                        peakInvalid++;
                        continue;
                    }
                    peakSum += samples[i].peakSpeedError;
                }
            }

            invalidSamples = invalid;
            peakSpeedValidatedSamples = peakValidated;
            peakSpeedInvalidSamples = peakInvalid;
            int validCount = sampleCount - invalid;
            if (validCount == 0)
            {
                meanError = minError = maxError = standardDeviation = float.PositiveInfinity;
                meanPeakSpeedError = peakSpeedStandardDeviation = float.PositiveInfinity;
                return;
            }

            meanError = sum / validCount;
            int peakValidCount = peakValidated - peakInvalid;
            meanPeakSpeedError = peakValidCount > 0 ? peakSum / peakValidCount : 0f;
            float variance = 0f;
            float peakVariance = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float value = samples[i].error;
                if (float.IsNaN(value) || float.IsInfinity(value)) continue;
                float delta = value - meanError;
                variance += delta * delta;

                if (!samples[i].peakSpeedValidated ||
                    float.IsNaN(samples[i].peakSpeedError) || float.IsInfinity(samples[i].peakSpeedError))
                    continue;
                float peakDelta = samples[i].peakSpeedError - meanPeakSpeedError;
                peakVariance += peakDelta * peakDelta;
            }

            standardDeviation = Mathf.Sqrt(variance / validCount);
            peakSpeedStandardDeviation = peakValidCount > 0
                ? Mathf.Sqrt(peakVariance / peakValidCount)
                : 0f;
        }
    }
}
