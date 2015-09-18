using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GitHubConsole
{
    public static class FileEditing
    {
        public static string[] Edit(string initialText, string editorConfigKey = null)
        {
            string filepath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            File.WriteAllText(filepath, initialText);

            OpenAndEdit(filepath, editorConfigKey);

            string[] content = File.ReadAllLines(filepath)
                .Where(x => !x.StartsWith("#"))
                .SkipWhile(x => x.Trim().Length == 0)
                .Reverse()
                .SkipWhile(x => x.Trim().Length == 0)
                .Reverse()
                .ToArray();
            File.Delete(filepath);

            return content;
        }

        public static void OpenAndEdit(string filepath, string editorConfigKey = null)
        {
            string application = editorConfigKey ?? Config.Default["generic.editor"] ?? "%f";
            filepath = "\"" + filepath + "\"";

            if (application.Contains("%f"))
                application = application.Replace("%f", filepath);
            else
                application = application + " " + filepath;

            var parts = CommandLineParsing.Command.SimulateParse(application);
            using (var p = Process.Start(parts[0], string.Join(" ", parts.Skip(1))))
                p.WaitForExit();
        }
    }
}
