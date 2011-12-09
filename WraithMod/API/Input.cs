using System;
using System.Threading;
using System.Collections.Generic;

using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace Wraith.API
{
    public class Input
    {
        public const char CommandSymbol = '/';
        static bool commandLock = false;
        static string lockedCommand = "";
        static Thread inputThread;

        public delegate void ConsoleEventHandler(object sender, ConsoleInputEventArgs e);
        public static event ConsoleEventHandler InputGiven;

        public static KeyboardState Keyboard;
        public static KeyboardState OldKeyboard;

        /// <summary>
        /// Be sure to hook InputGiven!
        /// </summary>
        public static void Start()
        {
            Keyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            //inputThread = new Thread(new ThreadStart(inputLoop));
            //inputThread.Start();

            Console.WriteLine("Input: Thread started");
        }

        static void inputLoop()
        {
            /*while (true)
            {
                string input = System.Console.ReadLine().Trim();
                if (input != "")
                {
                    if (commandLock)
                    {
                        lockedCommand = input;
                    }
                    if (InputGiven != null)
                    {
                        InputGiven(null, new ConsoleInputEventArgs(input));
                    }
                }
            }*/
        }

        public static bool CommandMatch(string command, string input, bool useCommandSymbol = false, bool caseSensitive = false, bool includeSpaces = false, bool useSubstrings = true)
        {
            if (command == null || input == null || command == "" || input == "")
                return false;
            if (useCommandSymbol)
            {
                if (input[0] == CommandSymbol)
                    input = input.Substring(1);
                else
                    return false;
            }
            if (!caseSensitive)
            {
                command = command.ToLower();
                input = input.ToLower();
            }
            if (!includeSpaces)
            {
                command = command.Replace(" ", "");
                input = input.Replace(" ", "");
            }
            if (useSubstrings)
            {
                int min = Math.Min(command.Length, input.Length);
                command = command.Substring(0, min);
                input = input.Substring(0, min);
            }
            return command == input;
        }

        public static string[] ParseArguments(string command, bool useCommandSymbol = false)
        {
            if (command == "")
                return new string[]{ "" };
            if (command[0] != CommandSymbol && useCommandSymbol)
            {
                return new string[1] { command };
            }
            List<string> arguments = new List<string>();
            {
                string[] argumentsQ = command.Split(new char[] { '"', '\'', '`' });
                for (int i = 0; i < argumentsQ.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        arguments.AddRange(argumentsQ[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                    else
                    {
                        arguments.Add(argumentsQ[i]);
                    }
                }
                if (useCommandSymbol)
                {
                    arguments[0] = arguments[0].Substring(1);
                }
            }

            for (int i = 0; i < arguments.Count; i++)
            {
                arguments[i] = arguments[i].Trim();
            }
            return arguments.ToArray();
        }

        public static void Update(GameTime gameTime)
        {
            Keyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState();

            Console.Update(gameTime);

            OldKeyboard = Keyboard;
        }
    }

    public class ConsoleInputEventArgs : EventArgs
    {
        public string Message = "";

        public ConsoleInputEventArgs(string message)
        {
            Message = message;
        }
    }
}
