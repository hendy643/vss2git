using System;
using System.Text;

namespace Hpdi.Vss2Git.Avalonia
{
    /// <summary>
    /// Maps between AppSettings (persisted JSON) and MigrationConfiguration.
    /// </summary>
    public static class SettingsMapper
    {
        public static MigrationConfiguration FromSettings(AppSettings settings, Encoding encoding)
        {
            return new MigrationConfiguration
            {
                VssDirectory = settings.VssDirectory,
                GitDirectory = settings.GitDirectory,
                VssProject = settings.VssProject,
                VssExcludePaths = settings.VssExcludePaths,
                DefaultEmailDomain = settings.DefaultEmailDomain,
                DefaultComment = settings.DefaultComment,
                LogFile = settings.LogFile,
                TranscodeComments = settings.TranscodeComments,
                ForceAnnotatedTags = settings.ForceAnnotatedTags,
                ExportProjectToGitRoot = settings.ExportProjectToGitRoot,
                AnyCommentSeconds = settings.AnyCommentSeconds,
                SameCommentSeconds = settings.SameCommentSeconds,
                VssEncoding = encoding,
                GitBackend = Enum.TryParse<GitBackend>(settings.GitBackend, out var backend)
                    ? backend : GitBackend.Process,
            };
        }
    }
}
