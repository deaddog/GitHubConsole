using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubConsole.Commands
{
    public class IssuesAssigner : Command
    {
        private bool isFirst = true;
        private bool isTake = false;
        private bool isDrop = false;

        private List<int> take = new List<int>();
        private List<int> drop = new List<int>();

        public override void Execute()
        {
            string username, project;
            GitHubClient client = CreateClient(out username, out project);
            if (client == null)
                return;

            if (isTake)
            {
                var issue = client.Issue.Get(username, project, take[0]).Result;
                var user = client.User.Current().Result;
                
                var update = issue.ToUpdate();
                update.Assignee = user.Login;
                var updated = client.Issue.Update(username, project, issue.Number, update).Result;
            }
            else if (isDrop)
            {

            }
        }

        public override bool HandleArgument(ArgumentStack.Argument argument)
        {
            if (isFirst)
            {
                isFirst = false;
                return handleFirst(argument);
            }
            else
                return handleRest(argument);
        }

        private bool handleFirst(ArgumentStack.Argument argument)
        {
            switch (argument.Key)
            {
                case "take":
                    isTake = true;
                    return true;

                case "drop":
                    isDrop = true;
                    return true;

                default:
                    return base.HandleArgument(argument);
            }
        }
        private bool handleRest(ArgumentStack.Argument argument)
        {
            int id;
            if (!int.TryParse(argument.Key, out id))
            {
                Console.WriteLine("GitHub issue # must be an integer. \"{0}\" is not valid.", argument.Key);
                return false;
            }

            if (isTake)
                take.Add(id);
            else if (isDrop)
                drop.Add(id);
            else
            {
                Console.WriteLine("take or drop must be specified before attempting to handle assignment,.");
                return false;
            }
            return true;
        }
    }
}
