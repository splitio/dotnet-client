using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Globalization;
using System.Threading;

namespace Splitio.Services.Parsing.Classes
{
    public class Semver
    {
        private static readonly char MetadataDelimiter = '+';
        private static readonly char PreReleaseDelimiter = '-';
        private static readonly char ValueDelimiter = '.';

        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitParser));

        private readonly string _oVersion;
        private long _major;
        private long _minor;
        private long _patch;
        private string[] _preRelease;
        private bool _isStable;

        #region Public Methods
        public static Semver Build(string version)
        {
            try
            {
                return new Semver(version);
            }
            catch
            {
                return null;
            }
        }

        public bool EqualTo(Semver toCompare)
        {
            bool result = _oVersion.Equals(toCompare._oVersion);
            _log.Debug($"{_oVersion} == {toCompare._oVersion} | Result: {result}");

            return result;
        }

        public bool GreaterThanOrEqualTo(Semver toCompare)
        {
            bool result = Compare(toCompare) >= 0;
            _log.Debug($"{_oVersion} >= {toCompare._oVersion} | Result: {result}");

            return result;
        }
        public bool LessThanOrEqualTo(Semver toCompare)
        {
            bool result = Compare(toCompare) <= 0;
            _log.Debug($"{_oVersion} <= {toCompare._oVersion} | Result: {result}");

            return result;
        }
        public bool Between(Semver start, Semver end)
        {
            bool result = GreaterThanOrEqualTo(start) && LessThanOrEqualTo(end);
            _log.Debug($"{start._oVersion} <= {_oVersion} <= {end._oVersion} | Result: {result}");

            return result;
        }
        #endregion

        #region Private Methods

        private Semver(string version)
        {
            Parse(version);
            _oVersion = version;
        }

        private void Parse(string version)
        {
            var vWithoutMetadata = RemoveMetadataIfExists(version);
            var index = vWithoutMetadata.IndexOf(PreReleaseDelimiter);

            if (index == -1)
            {
                _isStable = true;
            }
            else
            {
                var preReleaseData = vWithoutMetadata.Substring(index + 1);
                vWithoutMetadata = vWithoutMetadata.Substring(0, index);
                _preRelease = preReleaseData.Split(ValueDelimiter);
            }

            SetMajorMinorAndPatch(vWithoutMetadata);
        }

        private static string RemoveMetadataIfExists(string version)
        {
            var index = version.IndexOf(MetadataDelimiter);
            
            if (index == -1) return version;

            return version.Substring(0, index);
        }

        private void SetMajorMinorAndPatch(string version)
        {
            var vParts = version.Split(ValueDelimiter);
            if (vParts.Length != 3)
            {
                _log.Error("Unable to convert to Semver, incorrect format: " + version);
                throw new Exception("Unable to convert to Semver, incorrect format: " + version);
            }

            _major = long.Parse(vParts[0]);
            _minor = long.Parse(vParts[1]);
            _patch = long.Parse(vParts[2]);
        }

        /*
         * Precedence comparision between 2 Semver objects.
         * 
         * return the value 0 if this == toCompare;
         *          a value less than 0 if this < toCompare; and
         *          a value greater than 0 if this > toCompare
         */
        private int Compare(Semver toCompare)
        {
            if (EqualTo(toCompare))
            {
                return 0;
            }

            // Compare major, minor, and patch versions numerically
            var result = _major.CompareTo(toCompare._major);
            if (result != 0)
            {
                return result;
            }

            result = _minor.CompareTo(toCompare._minor);
            if (result != 0)
            {
                return result;
            }

            result = _patch.CompareTo(toCompare._patch);
            if (result != 0)
            {
                return result;
            }

            if (!_isStable && toCompare._isStable)
            {
                return -1;
            }
            else if (_isStable && !toCompare._isStable)
            {
                return 1;
            }

            // Compare pre-release versions lexically
            int minLength = Math.Min(_preRelease.Length, toCompare._preRelease.Length);
            for (int i = 0; i < minLength; i++)
            {
                if (_preRelease[i].Equals(toCompare._preRelease[i])) continue;

                if (int.TryParse(_preRelease[i], out int num1) && int.TryParse(toCompare._preRelease[i], out int num2))
                {
                    return num1.CompareTo(num2);
                }

                return CompareString(_preRelease[i], toCompare._preRelease[i], CultureInfo.CurrentCulture);
            }

            // Compare lengths of pre-release versions
            return _preRelease.Length.CompareTo(toCompare._preRelease.Length);
        }

        private static int CompareString(string value1, string value2, CultureInfo culture)
        {
            if (culture == null)
            {
                return value1.CompareTo(value2);
            }

            return culture.CompareInfo.Compare(value1, value2, CompareOptions.StringSort);
        }
        #endregion
    }
}
