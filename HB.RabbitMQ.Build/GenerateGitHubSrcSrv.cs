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
            var srcSrv = new StringBuilder();
            srcSrv.AppendFormat(@"SRCSRV: ini ------------------------------------------------
VERSION=1
INDEXVERSION=2
VERCTL=Archive
DATETIME={0}
SRCSRV: variables ------------------------------------------
SRCSRVVERCTRL=http
SRCSRVTRG=%var2%
SRCSRVCMD=
SRCSRV: source files ---------------------------------------", DateTime.Now);
            foreach(var srcFile in SourceFiles)
            {
                srcSrv.AppendLine();
                var relactiveSourceFilePath = srcFile.Substring(ProjectDir.Length).Replace('\\', '/');
                var url = string.Format(@"https://raw.githubusercontent.com/{0}/{1}/{2}/{3}", GitHubUserName, GitHubPorjectName, GitCommitId, relactiveSourceFilePath);
                srcSrv.AppendFormat("{0}*{1}", srcFile, url);
            }
            srcSrv.Append(@"SRCSRV: end ------------------------------------------------");
            SrcSrv = srcSrv.ToString();
            Log.LogMessage(SrcSrv);
            return true;
        }
    }
}
