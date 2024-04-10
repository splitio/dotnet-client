using System;
using System.Globalization;

namespace Splitio.Services.SemverImp
{
    public static class SemverComparer
    {

        /*
         * Precedence comparision between 2 Semver objects.
         * 
         * return the value 0 if version == toCompare;
         *          a value less than 0 if version < toCompare; and
         *          a value greater than 0 if version > toCompare
         */
        public static int Compare(this Semver semver, Semver toCompare)
        {
            if (semver.Version.Equals(toCompare.Version))
            {
                return 0;
            }

            // Compare major, minor, and patch versions numerically
            var result = semver.Major.CompareTo(toCompare.Major);
            if (result != 0)
            {
                return result;
            }

            result = semver.Minor.CompareTo(toCompare.Minor);
            if (result != 0)
            {
                return result;
            }

            result = semver.Patch.CompareTo(toCompare.Patch);
            if (result != 0)
            {
                return result;
            }

            if (!semver.IsStable && toCompare.IsStable)
            {
                return -1;
            }
            else if (semver.IsStable && !toCompare.IsStable)
            {
                return 1;
            }

            // Compare pre-release versions lexically
            int minLength = Math.Min(semver.PreRelease.Length, toCompare.PreRelease.Length);
            for (int i = 0; i < minLength; i++)
            {
                if (semver.PreRelease[i].Equals(toCompare.PreRelease[i])) continue;

                if (int.TryParse(semver.PreRelease[i], out int num1) && int.TryParse(toCompare.PreRelease[i], out int num2))
                {
                    return num1.CompareTo(num2);
                }

                return CompareString(semver.PreRelease[i], toCompare.PreRelease[i], CultureInfo.CurrentCulture);
            }

            // Compare lengths of pre-release versions
            return semver.PreRelease.Length.CompareTo(toCompare.PreRelease.Length);
        }

        private static int CompareString(string value1, string value2, CultureInfo culture)
        {
            if (culture == null)
            {
                return value1.CompareTo(value2);
            }

            return culture.CompareInfo.Compare(value1, value2, CompareOptions.StringSort);
        }
    }
}
