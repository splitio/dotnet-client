using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System.Linq;

namespace Splitio.Services.SemverImp
{
    public class Semver
    {
        private static readonly char MetadataDelimiter = '+';
        private static readonly char PreReleaseDelimiter = '-';
        private static readonly char ValueDelimiter = '.';

        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(Semver));

        public ulong Major { get; private set; }
        public ulong Minor { get; private set; }
        public ulong Patch { get; private set; }
        public string[] PreRelease { get; private set; }
        public string Metadata { get; private set; }
        public bool IsStable { get; private set; }
        public string Version { get; private set; }

        #region Public Methods
        public static Semver Build(string version)
        {
            if (string.IsNullOrEmpty(version)) return null;

            try
            {
                return new Semver(version);
            }
            catch
            {
                return null;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Semver))
            {
                return false;
            }

            return Version.Equals(((Semver)obj).Version);
        }
        #endregion

        #region Private Methods
        private Semver(string version)
        {
            var vWithoutMetadata = SetAndRemoveMetadataIfExists(version);
            var vWithoutPreRelease = SetAndRemovePreReleaseIfExists(vWithoutMetadata);
            SetMajorMinorAndPatch(vWithoutPreRelease);
            Version = SetVersion();
        }

        private string SetAndRemoveMetadataIfExists(string version)
        {
            var index = version.IndexOf(MetadataDelimiter);

            if (index == -1) return version;

            Metadata = version.Substring(index + 1);
            if (string.IsNullOrEmpty(Metadata))
            {
                _log.Error("Unable to convert to Semver, incorrect pre release data.");
                throw new SemverParseException("Unable to convert to Semver, incorrect pre release data");
            }

            return version.Substring(0, index);
        }

        private string SetAndRemovePreReleaseIfExists(string vWithoutMetadata)
        {
            var index = vWithoutMetadata.IndexOf(PreReleaseDelimiter);

            if (index == -1)
            {
                IsStable = true;
                return vWithoutMetadata;
            }

            var preReleaseData = vWithoutMetadata.Substring(index + 1);
            PreRelease = preReleaseData.Split(ValueDelimiter);

            if (PreRelease.Any(p => string.IsNullOrEmpty(p)))
            {
                _log.Error("Unable to convert to Semver, incorrect pre release data.");
                throw new SemverParseException("Unable to convert to Semver, incorrect pre release data.");
            }

            return vWithoutMetadata.Substring(0, index);
        }

        private void SetMajorMinorAndPatch(string version)
        {
            var vParts = version.Split(ValueDelimiter);
            if (vParts.Length != 3 ||
                !ulong.TryParse(vParts[0], out ulong major) ||
                !ulong.TryParse(vParts[1], out ulong minor) ||
                !ulong.TryParse(vParts[2], out ulong patch))
            {
                _log.Error($"Unable to convert to Semver, incorrect format: {version}");
                throw new SemverParseException($"Unable to convert to Semver, incorrect format: {version}");
            }

            Major = major;
            Minor = minor;
            Patch = patch;
        }

        private string SetVersion()
        {
            var toReturn = $"{Major}{ValueDelimiter}{Minor}{ValueDelimiter}{Patch}";

            if (PreRelease != null && PreRelease.Length != 0)
            {
                toReturn += $"{PreReleaseDelimiter}{string.Join(ValueDelimiter.ToString(), PreRelease)}";
            }

            if (!string.IsNullOrEmpty(Metadata))
            {
                toReturn += $"{MetadataDelimiter}{Metadata}";
            }

            return toReturn;
        }
        #endregion
    }
}
