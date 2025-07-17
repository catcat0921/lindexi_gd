﻿// See https://aka.ms/new-console-template for more information

using System.Collections;
using System.Diagnostics;

using LibGit2Sharp;

AppendLog($"进程启动 {Environment.ProcessId} 工作路径 {Environment.CurrentDirectory}");
//foreach (DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
//{
//    AppendLog($"{environmentVariable.Key}={environmentVariable.Value}");
//}

var codeFolder = new DirectoryInfo(@"C:\lindexi\Code");

var folder = @$"{codeFolder.EnumerateDirectories().First(t => t.Name.Contains("lindexi")).FullName}\lindexi\.git\";

AppendLog($"仓库地址 {folder}");

var repository = new Repository(folder);

foreach (var worktree in repository.Worktrees)
{
    if (worktree.Name.Contains("Text"))
    {
        var last = worktree.WorktreeRepository.Commits.First();
        var message = last.Message;

        var lastCommitFile = Path.Join(AppContext.BaseDirectory, "LastCommit.txt");
        var lastCommit = string.Empty;
        if (File.Exists(lastCommitFile))
        {
            lastCommit = File.ReadAllText(lastCommitFile);
        }

        AppendLog($"当前 {last.Sha}。文件 {lastCommit}");

        if (lastCommit != last.Sha)
        {
            File.WriteAllText(lastCommitFile, last.Sha);
            Update(message);
        }
    }
}

static void Update(string message)
{
    var gitFolder = @"C:\lindexi\Work\Code\.git\";
    var sourceFolder = @"C:\lindexi\Work\Source\";

    var repository = new Repository(gitFolder);
    foreach (Worktree? worktree in repository.Worktrees)
    {
        if (worktree is null)
        {
            continue;
        }

        if (worktree.Name.Contains("Text"))
        {
            Process.Start(new ProcessStartInfo(@"C:\lindexi\Application\同步文档代码.lnk")
            {
                UseShellExecute = true
            })!.WaitForExit();

            var worktreeRepository = worktree.WorktreeRepository;

            //worktreeRepository.Index.Add(".");

            var processStartInfo = new ProcessStartInfo("git")
            {
                ArgumentList =
                {
                    "add",
                    "."
                },
                WorkingDirectory = sourceFolder,
            };
            // 这是在 git 里面调用的，会被注入 git 的环境变量，从而被投毒，如 GIT_INDEX_FILE GIT_DIR 等，导致加入的文件不是在要求的路径
            processStartInfo.Environment.Clear();

            Process.Start(processStartInfo)!.WaitForExit();

            var second = TimeSpan.FromMinutes(15).TotalSeconds;
            var minute = TimeSpan.FromSeconds(Random.Shared.Next((int) second));

            var signature = new Signature("lindexi", "lindexi_gd@163.com", DateTimeOffset.Now.AddMinutes(15).Add(minute));
            worktreeRepository.Commit(message, signature, signature);

            return;
        }
    }
}

Console.WriteLine("Hello, World!");

static void AppendLog(string message)
{
    var logFile = Path.Join(AppContext.BaseDirectory, "Log.txt");
    File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff}] {message}\r\n");
}
