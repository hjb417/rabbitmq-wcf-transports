using System;
using System.Linq;
using System.Text;
using LibGit2Sharp;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace HB.RabbitMQ.Build
{
    public class GenerateGitHubSrcSrv : Task
    {
        [Required]
        public string[] SourceFiles { get; set; }

        [Required]
        public string GitFolder { get; set; }

        [Output]
        public string SrcSrv { get; set; }

        public override bool Execute()
        {
            using (var repo = new Repository(GitFolder))
            {
                var remoteRepo = repo.Network.Remotes.Single();
                var httpAlias = new UriBuilder(remoteRepo.Url.Substring(0, remoteRepo.Url.Length - 4) + "/" + repo.ObjectDatabase.ShortenObjectId(repo.Head.Tip) + "/");
                httpAlias.Host = "raw.github.com";

                var srcSrv = new StringBuilder();
                srcSrv.AppendFormat(@"SRCSRV: ini ------------------------------------------------
VERSION=2
VERCTL=https
SRCSRV: variables ------------------------------------------
SRCSRVVERCTRL=https
SRCSRVTRG={0}%var2%
SRCSRV: source files ---------------------------------------", httpAlias.Uri);
                
                foreach (var srcFile in SourceFiles)
                {
                    srcSrv.AppendLine();
                    var relactiveSourceFilePath = srcFile.Substring(repo.Info.WorkingDirectory.Length).Replace('\\', '/');
                    srcSrv.AppendFormat("{0}*{1}", srcFile, relactiveSourceFilePath);
                }
                srcSrv.AppendLine();
                srcSrv.Append(@"SRCSRV: end ------------------------------------------------");
                SrcSrv = srcSrv.ToString();
                Log.LogMessage(SrcSrv);
                return true;
            }
        }
    }
}
