using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading;
using System.IO;

using Wraith;
using Wraith.API;

using LuaInterface;

namespace Wraith.API
{
    public class Lua : LuaInterface.Lua
    {
        const string DEFAULT_SCRIPTS_PATH = "Scripts/";
        const string DEFAULT_STARTUP_SCRIPT = "startup.lua";
        const string DEFAULT_ONLOAD_SCRIPT = "onload.lua";

        public static string ScriptsPath;
        public static string StartupScript;
        public static string OnLoadScript;
        public Hashtable LuaFunctions = null;

        public static List<Thread> ScriptThreads = new List<Thread>();

        public void Initialize(string scriptsPath = DEFAULT_SCRIPTS_PATH, string startupScript = DEFAULT_STARTUP_SCRIPT, string onLoadScript = DEFAULT_ONLOAD_SCRIPT)
        {
            foreach (Thread t in ScriptThreads)
            {
                t.Abort();
            }
            ScriptsPath = scriptsPath;
            StartupScript = startupScript;
            OnLoadScript = onLoadScript;
            if (!Directory.Exists(ScriptsPath))
            {
                Directory.CreateDirectory(ScriptsPath);
            }
            if (!File.Exists(ScriptsPath + "README.txt"))
            {
                StreamWriter sw = new StreamWriter(ScriptsPath + "README.txt");
                sw.Write("Coming soon! A simple scripting guide... maybe.");
                sw.Close();
            }
            LuaFunctions = new Hashtable();
            LuaFunctions.Clear();
            LoadAPI(WraithMod.Core);
            Console.WriteLine("Lua: Initialized");
        }

        public void TryStartupScript()
        {
            if (File.Exists(ScriptsPath + StartupScript))
            {
                WraithMod.Core.RunScript(ScriptsPath + StartupScript);
            }
        }

        public void TryOnLoadScript()
        {
            if (File.Exists(ScriptsPath + OnLoadScript))
            {
                WraithMod.Core.RunScript(ScriptsPath + OnLoadScript);
            }
        }

        public void LoadAPI(object apiObject)
        {
            if (LuaFunctions == null)
                return;

            MethodInfo[] mi = apiObject.GetType().GetMethods();
            for (int i = 0; i < mi.Length; i++)
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(mi[i]))
                {
                    if (attr.GetType() == typeof(LuaFunction))
                    {
                        LuaFunction a = (LuaFunction)attr;
                        List<string> arguments = new List<string>();
                        List<string> argDocs = new List<string>();

                        string name = a.Name;
                        string desc = a.Description;
                        string[] docs = a.Args;
                        if (docs == null)
                            docs = new string[] { };

                        ParameterInfo[] argInfo = mi[i].GetParameters();

                        if (argInfo != null && (argInfo.Length != docs.Length))
                        {
                            Console.WriteLine(MessageType.Warning, "Lua: Warning: {0}() args.Length mismatch requires {1}, had {2}", name, docs.Length, argInfo.Length);
                            break;
                        }

                        for (int j = 0; j < argInfo.Length; j++)
                        {
                            argDocs.Add(argInfo[j].Name + ": " + docs[j]);
                            arguments.Add(argInfo[j].ParameterType.Name.ToLower() + " " + argInfo[j].Name);
                        }

                        LuaFunctionDescriptor lfd = new LuaFunctionDescriptor(name, desc, arguments.ToArray(), argDocs.ToArray());
                        try
                        {
                            LuaFunctions.Add(name, lfd);
                        }
                        catch
                        {
                            Console.WriteLine(MessageType.Warning, "Lua: Duplicate registered functions: {0}", name);
                        }

                        if (name[0] != '#' && !WraithMod.DedicatedServer)
                        {
                            RegisterFunction(name, apiObject, mi[i]);
                        }
                        Console.WriteLine("Lua: Registered function {0}", lfd.Header);
                    }
                }
            }
            Console.WriteLine("Lua: API loaded");
        }

        public string[] GetScripts(string path)
        {
            List<string> scripts = new List<string>();
            GetScripts(scripts, path);
            return scripts.ToArray();
        }

        public void GetScripts(List<string> scripts, string file)
        {
            foreach (string dir in Directory.GetDirectories(file))
                GetScripts(scripts, dir);

            scripts.AddRange(Directory.GetFiles(file, "*.lua"));
        }

        public void RunCommand(object lua, bool quiet = false)
        {
            try
            {
                if (quiet)
                    Console.WriteLine("Lua: Running");
                else
                    Console.WriteLine("Lua: Running: {0}", lua.ToString());
                DoString(lua.ToString());
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(MessageType.Error, "Lua: Error: {0}", e.Message);
            }
            if (quiet)
                Console.WriteLine(MessageType.Warning, "Lua: Command failed");
            else
                Console.WriteLine(MessageType.Warning, "Lua: Command failed: {0}", lua.ToString());
        }

        public void RunFile(object lua)
        {
            try
            {
                Console.WriteLine("Lua: Running: {0}", lua.ToString());
                DoFile(lua.ToString());
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(MessageType.Error, "Lua: Error: {0}", e.Message);
            }
        }

        public void RunString(object lua)
        {
            try
            {
                Console.WriteLine("Lua: Running: {0}", lua.ToString());
                DoString(lua.ToString());
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(MessageType.Error, "Lua: Error: {0}", e.Message);
            }
            Console.WriteLine(MessageType.Warning, "Lua: Command failed: {0}", lua.ToString());
            Console.WriteLine(MessageType.Info, "Lua: Did you mean to use a command? Include the '{0}' next time", Input.CommandSymbol);
        }

        void Engine_HookException(object sender, HookExceptionEventArgs e)
        {
            Console.WriteLine(MessageType.Error, "Lua: {0}", e.Exception.Message);
        }
    }
}
