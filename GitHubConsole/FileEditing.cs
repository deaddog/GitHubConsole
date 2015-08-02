using System;
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

            if (application.Contains("%f"))
                using (var p = System.Diagnostics.Process.Start(application.Replace("%f", filepath)))
                    p.WaitForExit();
            else
                using (var p = System.Diagnostics.Process.Start(application, filepath))
                    p.WaitForExit();
        }
    }
}
