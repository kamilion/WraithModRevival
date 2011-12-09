using System;
using System.Collections.Generic;
using System.IO;

namespace Wraith.API
{
    public class Commands
    {
        #region README
        public const string README = @"To create a command, you can open another command script file and study the syntax. 

It's all LUA except for the first comment lines. Those first comment lines define metadata about your command. If setup improperly, your command script may not function properly. 

The first comment line contains the command name and description. Like this, but without the brackets: 
-- [commandName], [A description of what your command does]

Any following lines contain data for the arguments your command can take. For example, if you have a teleport command, you need to able to say who you're teleporting to. This is why these arguments come in handy. Here's the structure of the argument metadata, minus the barckets of course: 
-- [argumentName], [what the argument should be], [what the argument is for]

Then, when the command is loaded, it will create a LUA variable with the name of the argument specified for you to use in your script. So if we had this argument set up:
-- arg1, money, Gives you money

We could use the variable " + "\"arg1\"" + @" later in the script as the paramater for our command. However, these arguments cannot be parsed properly if they are meant to be strings, that is, text. If your intention is for the argument to be text, like a player's name, you must put a $ in front of the name: 
-- $arg1, money, Gives you money

This way, WraithMod can give you the proper input for your script. 

It's also useful to have optional arguments. Perhaps you want people to have to option of specifying a true or false, but if something isn't specified, then it is toggled. For an optional argument, just put an asterisk (*) in front of the name like so:
-- *arg1, money, Gives you money

Now, when you use optional arguments, you will want to be able to detect if they were specified in your script. To do this, you would simply do this:
if arg1 == nil 

Then you know that it wasn't set. Also, at the very end of your script, you will want to reset it to nil.
arg1 = nil

So that it doesn't confuse future runs of the command. 

Note: You can have both a $ and * marking your arguments.

For further reading, see the README.txt file in the Scripts folder. ";
        #endregion

        public const string DEFAULT_COMMANDS_PATH = "Commands/";

        public static string CommandsPath;
        public List<LuaFunctionDescriptor> CommandList = new List<LuaFunctionDescriptor>();

        public Commands()
        {

        }

        public void Initialize(string commandsPath = DEFAULT_COMMANDS_PATH)
        {
            CommandsPath = commandsPath;
            CommandList.Clear();
            if (!Directory.Exists(CommandsPath))
                Directory.CreateDirectory(CommandsPath);

            if (!File.Exists(CommandsPath + "README.txt"))
            {
                StreamWriter sw = new StreamWriter(CommandsPath + "README.txt");
                sw.Write(README);
                sw.Close();
            }

            LoadCommands();
            Console.WriteLine("Commands: Initialized");
        }

        public void CommandHelp()
        {
            string s = "";
            System.Collections.IEnumerator cmds = CommandList.GetEnumerator();
            bool found = false;
            while (cmds.MoveNext())
            {
                if (!found)
                    found = true;
                s += ((LuaFunctionDescriptor)cmds.Current).Documentation;
            }
            if (found)
                Console.WriteLine("Commands: Available commands: (* = Optional, $ = Text/String)\n{0}", s);
            else
                Console.WriteLine("Commands: No commands available - check your " + CommandsPath + " directory");
        }

        public void RunCommand(object o)
        {
            string input = o.ToString();
            string[] args = Input.ParseArguments(input, true);
            if (args[0].ToLower().Trim() == "help")
            {
                CommandHelp();
                return;
            }
            LuaFunctionDescriptor command = null;
            foreach (LuaFunctionDescriptor lfd in CommandList)
            {
                if (Input.CommandMatch(lfd.Name, args[0], false, false, false, false))
                {
                    command = lfd;
                    break;
                }
            }
            if (command == null)
            {
                foreach (LuaFunctionDescriptor lfd in CommandList)
                {
                    if (Input.CommandMatch(lfd.Name, args[0]))
                    {
                        command = lfd;
                        break;
                    }
                }
            }
            if (command == null)
            {
                Console.WriteLine(MessageType.Warning, "Commands: Could not find command {0}", args[0]);
                return;
            }
            string file = CommandsPath + command.Command + ".lua";
            try
            {
                string toRun = "";
                int optionalCommands = 0;
                foreach (string s in command.Args)
                {
                    try
                    {
                        if (s[0] == '*' || s[1] == '*')
                        {
                            optionalCommands++;
                        }
                    }
                    catch
                    {

                    }
                }
                if (command.Args.Length - (args.Length - 1 - optionalCommands) < optionalCommands)
                {
                    Console.WriteLine(MessageType.Warning, "Commands: Invalid number of arguments given: {0} ({1} needed and {2} optional)", args.Length - 1, command.Args.Length - optionalCommands, optionalCommands);
                    return;
                }
                for (int i = 0; i < command.Args.Length; i++)
                {
                    string argName = command.Args[i];
                    int offset = 0;
                    if (argName[0] == '*' || argName[1] == '*')
                    {
                        offset++;
                    }
                    string argVal = "";
                    try
                    {
                        argVal = args[i + 1];
                    }
                    catch
                    {
                        break;
                    }
                    bool isString = false;
                    if (argName[0] == '$' || argName[1] == '$')
                    {
                        isString = true;
                        offset++;
                    }
                    argName = argName.Substring(offset);
                    try
                    {
                        if (!isString)
                            toRun += argName + " = " + argVal + "\n";
                        else
                            toRun += argName + " = \"" + argVal +"\"\n";
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        // Must have been optional...?
                    }
                }
                StreamReader sr = new StreamReader(file);
                toRun += sr.ReadToEnd();
                sr.Close();
                WraithMod.Lua.RunCommand(toRun, !WraithMod.Debug);
            }
            catch (Exception e) // 65 - 122
            {
                Console.WriteLine(MessageType.Error, "Commands: Could not run the script for command {0}: {1}\n  {2}", command.Name, file, e.Message);
                return;
            }
        }

        public void LoadCommands()
        {
            foreach (string str in WraithMod.Lua.GetScripts(CommandsPath))
            {
                LuaFunctionDescriptor lfd = new LuaFunctionDescriptor();
                List<string> args = new List<string>();
                List<string> argDocs = new List<string>();
                StreamReader sr = new StreamReader(str);
                string s = "";
                bool first = true;
                while ((s = sr.ReadLine()).Substring(0, 2) == "--")
                {
                    string[] headerData = s.Substring(2).Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (headerData.Length < 1) continue;
                    if (first)
                    {
                        if (headerData.Length == 1)
                        {
                            lfd.Name = headerData[0];
                        }
                        else
                        {
                            for (int i = 1; i < headerData.Length; i++)
                            {
                                lfd.Description += headerData[i];
                                if (i != headerData.Length - 1)
                                {
                                    lfd.Description += ',';
                                }
                            }
                            lfd.Name = headerData[0].Trim();
                        }
                        first = false;
                    }
                    else
                    {
                        if (headerData.Length == 1)
                        {
                            args.Add(headerData[0].Trim());
                            argDocs.Add(headerData[0].Trim());
                        }
                        else if (headerData.Length == 2)
                        {
                            string docs = "";
                            for (int i = 1; i < headerData.Length; i++)
                            {
                                docs += headerData[i];
                                if (i == headerData.Length - 1)
                                {
                                    docs += ',';
                                }
                            }
                            args.Add(headerData[0].Trim());
                            argDocs.Add(headerData[0].Trim() + " " + docs.Trim());
                        }
                        else if (headerData.Length == 3)
                        {
                            string docs = headerData[1] + " - ";
                            for (int i = 2; i < headerData.Length; i++)
                            {
                                docs += headerData[i];
                                if (i != headerData.Length - 1)
                                {
                                    docs += ',';
                                }
                            }
                            args.Add(headerData[0].Trim());
                            argDocs.Add(headerData[0].Trim() + " " + docs.Trim());
                        }
                    }
                }
                sr.Close();
                string[] splitter = str.Split(new char[] { '/', '\\' });
                string[] splitter2 = splitter[splitter.Length - 1].Split('.');
                for (int i = 0; i < splitter2.Length - 1; i++)
                {
                    lfd.Command += splitter2[i];
                    if (i != splitter2.Length - 2)
                        lfd.Command += '.';
                }
                if (lfd.Name == "")
                    lfd.Name = lfd.Command;
                lfd.Args = args.ToArray();
                lfd.ArgDocs = argDocs.ToArray();
                lfd.MakeDocumentation(true);
                CommandList.Add(lfd);
                Console.WriteLine("Commands: Loaded command {0}", lfd.Header);
            }
        }
    }
}
