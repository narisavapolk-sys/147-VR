using System;
using System.Globalization;

namespace PoolPhysics.Sim
{
    /// <summary>
    /// Canonical name-to-rules identity parsing for imported billiards ball assets.
    /// Names are presentation data; game rules use the numeric identity returned here.
    /// </summary>
    public static class PoolBallIdentity
    {
        /// <summary>
        /// Returns the snooker value (cue=0, red=1, yellow=2 ... black=7),
        /// or -1 when the name does not identify a snooker ball.
        /// Handles both legacy names and imported FBX names such as
        /// "SM_Red.012" and "SM_147VR_Bay1_Ball_Red_011".
        /// </summary>
        public static int SnookerPoints(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return -1;

            if (Contains(name, "cue") || Contains(name, "white"))
                return 0;
            if (Contains(name, "red"))
                return 1;
            if (Contains(name, "yellow"))
                return 2;
            if (Contains(name, "green"))
                return 3;
            if (Contains(name, "brown"))
                return 4;
            if (Contains(name, "blue"))
                return 5;
            if (Contains(name, "pink"))
                return 6;
            if (Contains(name, "black"))
                return 7;

            return -1;
        }

        /// <summary>
        /// Extracts the printed number from numbered pool-ball FBX names.
        /// Accepts names such as "Ball_08", "SM_Bay3_Ball_03_Solid" and "Ball-9".
        /// </summary>
        public static bool TryParsePoolNumber(string name, int maxNumber, out int number)
        {
            number = 0;
            if (string.IsNullOrWhiteSpace(name) || maxNumber < 1)
                return false;

            string[] tokens = name.Split(
                new[] { '_', ' ', '-' },
                StringSplitOptions.RemoveEmptyEntries);

            for (int i = tokens.Length - 1; i >= 0; i--)
            {
                if (!int.TryParse(tokens[i], NumberStyles.None, CultureInfo.InvariantCulture, out int candidate))
                    continue;

                if (candidate < 1 || candidate > maxNumber)
                    return false;

                number = candidate;
                return true;
            }

            return false;
        }

        private static bool Contains(string value, string token)
            => value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
