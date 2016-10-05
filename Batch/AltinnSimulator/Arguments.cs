using System.Collections.Generic;

namespace AltinnSimulator
{
    /// <summary>
    /// Class to decompose and hold the arguments. 
    /// Arguments must be on format: command -name value -name value source, 
    /// the first word must be a command, then a number of name value pair, where name starts with -, 
    /// The value is optional. The names are always converted to lowercase.
    /// The source is any main parameter which applies to the command such as a folder name or file name.
    /// </summary>
    public class Arguments
    {
        private Dictionary<string, string> _arguments = new Dictionary<string, string>();

        /// <summary>
        /// The command is the first argument, always fetched as lowercase
        /// </summary>
        public string Command { get; private set; }

        /// <summary>
        /// The main parameter (not initiated with -nm), always fetched as lowercase
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="args">The arguments to analyze</param>
        public Arguments(string[] args)
        {
            Decompose(args);
        }

        /// <summary>
        /// Fetches one argument given the name
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The found argument or null if not found</returns>
        public string this[string name]
        {
            get
            {
                string val = null;
                try
                {
                    val = _arguments[name];
                }
                catch
                { }
                return val;
            }
        }


        private void Decompose(string[] args)
        {
            if (args.Length > 0)
            {
                Command = args[0].ToLower();
                int inx = 1;
                while (inx < args.Length)
                {
                    if (args[inx].StartsWith("-"))
                    {
                        string nm = args[inx].Substring(1).ToLower(); ;
                        inx++;
                        if (inx < args.Length && !args[inx].StartsWith("-"))
                        {
                            _arguments.Add(nm, args[inx++]);
                        }
                        else
                        {
                            _arguments.Add(nm, "");
                        }
                    }
                    else
                    {
                        Source = args[inx].ToLower();
                        inx++;
                    }
                }
            }
        }

    }
}
