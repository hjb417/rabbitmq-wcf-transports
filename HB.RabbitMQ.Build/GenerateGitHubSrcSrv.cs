using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace HB.RabbitMQ.Build
{
    public class GenerateGitHubSrcSrv : Task
    {
        [Required]
        public string[] SourceFiles { get; set; }

        [Required]
        public string ProjectDir { get; set; }

        [Required]
        public string GitHubUserName { get; private set; }

        [Required]
        public string GitHubPorjectName { get; private set; }

        [Required]
        public string GitCommitId { get; private set; }

        [Output]
        public string SrcSrv { get; set; }

        public override bool Execute()
        {
            var httpAlias = @"https://raw.github.com/" + string.Join("/", GitHubUserName, GitHubPorjectName, GitCommitId) + "/";
            var srcSrv = new StringBuilder();
            srcSrv.AppendFormat(@"SRCSRV: ini ------------------------------------------------
VERSION=1
SRCSRV: variables ------------------------------------------
SRCSRVVERCTRL=https
SRCSRVTRG={0}%var2%
SRCSRV: source files ---------------------------------------", httpAlias);
            foreach (var srcFile in SourceFiles)
            {
                srcSrv.AppendLine();
                var relactiveSourceFilePath = srcFile.Substring(ProjectDir.Length).Replace('\\', '/');
                srcSrv.AppendFormat("{0}*{1}", srcFile, relactiveSourceFilePath);
            }
            srcSrv.Append(@"SRCSRV: end ------------------------------------------------");
            SrcSrv = srcSrv.ToString();
            Log.LogMessage(SrcSrv);
            return true;
        }
    }
}
