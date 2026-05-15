using System.Diagnostics;
using todo.HelperClasses;

namespace todo;

public class Git (string repoPath){

    private void RunProcess(string argument) {
        Process process = new Process();
        
        process.StartInfo.FileName = "git";
        process.StartInfo.WorkingDirectory = repoPath;
        process.StartInfo.Arguments = argument;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.Start();
        StreamReader streamReader = process.StandardOutput;
        string output = streamReader.ReadToEnd();
        if (Message.InfoEnabled || Message.VerboseEnabled) {
            Console.WriteLine(output);
        }
        
        process.Close();
    }

    private void makeProcess() {
        
    }

    public void Init() {
        RunProcess("init");
    }

    public void Clone(string repoToClone) {
        RunProcess($"clone {repoToClone}");
    }

    public void Push(string commitMessage) {
        RunProcess($"commit -a -m \"{commitMessage}\"");
        RunProcess("push");
    }

    public void Pull() {
        RunProcess("pull");
    }

    public void Status() {
        RunProcess("status");
    }
    
}