using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Wraith.API;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Wraith
{
    public static class Console
    {
        public static Stack<ConsoleColor> ConsoleColors = new Stack<ConsoleColor>();
        public const int MAX_LINES = 27;

        public static bool UseConsole = false;
        public static bool KeyDownRecently = false; 
        public static List<string> Output = new List<string>(300);
        public static Stack<string> PastCommands = new Stack<string>(100);
        public static string Command = "";
        public static int CursorPosition = 0;
        public static int ScrollPosition = 0;
        public static int CurrentCommand = -1;
        public static int KeyDownRecentlyTimer = 0; 
        public static StreamWriter Writer;

        public static void Initialize()
        {
            Process p = Process.GetCurrentProcess();
            System.Console.ResetColor();
            string logFile = "WraithMod_" + GetLogTimeStamp().Replace('/', '-').Replace(':', '-').Replace(' ', '_') + ".log";
            if (File.Exists(logFile))
                File.Move(logFile, logFile + ".bak");
            Writer = new StreamWriter(new FileStream(logFile, FileMode.Create, FileAccess.Write));
            WriteLine("Console: Initialized");
        }

        public static void WriteLine(string str = "", params object[] args)
        {
            WriteLine(MessageType.Normal, str, args);
        }

        public static void WriteLine(MessageType type, string str = "", params object[] args)
        {
            ConsoleColor cc = GetConsoleColorFromType(type);
            ConsoleColors.Push(cc);
            string level = "Unknown";
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    try
                    {
                        str = str.Replace("{" + i + "}", args[i].ToString());
                    }
                    catch
                    {

                    }
                }
            }
            try
            {
                string[] s = str.Split(':');
                if (s.Length > 1)
                {
                    level = s[0];
                }
                str = str.Substring(level.Length + 1, str.Length - level.Length - 1).Trim();
            }
            catch
            {
                level = "Unknown";
            }
            level = level.ToUpper();
            switch (level.Substring(0, 2))
            {
                
            }
            System.Console.ForegroundColor = cc;
            try
            {
                Writer.WriteLine(GetLogTimeStamp() + " [" + level + "] " + str);
                Writer.Flush();
            }
            catch
            {

            }
            str = GetTimeStamp() + " [" + level + "] " + str;
            //System.Console.WriteLine(str);
            if (WraithMod.DedicatedServer)
            {
                System.Console.WriteLine(str);
            }
            else
            {
                foreach (string s in str.Split('\n'))
                {
                    Output.Add(GetCharFromType(type) + s);
                }
            }
            System.Console.ResetColor();
            ConsoleColors.Pop();
        }

        static int lastCurrentCommand = -1;
        public static void Update(GameTime gameTime)
        {
            if (Input.Keyboard.GetPressedKeys().Length > 0)
            {
                KeyDownRecently = true;
                KeyDownRecentlyTimer = 0;
            }
            else
            {
                KeyDownRecentlyTimer += gameTime.ElapsedGameTime.Milliseconds;
                if (KeyDownRecentlyTimer > 500)
                    KeyDownRecently = false;
            }

            string toInsert = "";

            if (Command.Length > 0)
            {
                if (CursorPosition > Command.Length)
                    CursorPosition = Command.Length;
                else if (CursorPosition < 0)
                    CursorPosition = 0;
            }
            else
            {
                CursorPosition = 0;
            }

            if (Input.Keyboard.IsKeyDown(Keys.Tab) && !Input.OldKeyboard.IsKeyDown(Keys.Tab))
            {
                UseConsole = !UseConsole;
            }
            else if (UseConsole)
            {
                toInsert = GetStringFromCurrentKeys(gameTime);
            }

            if (CurrentCommand > PastCommands.Count - 1)
                CurrentCommand = -1;
            else if (CurrentCommand < -1)
                CurrentCommand = PastCommands.Count - 1;
            if (CurrentCommand == -1 && lastCurrentCommand != -1)
                Command = "";
            else if (CurrentCommand != lastCurrentCommand)
            {
                Command = PastCommands.ToArray()[CurrentCommand];
                CursorPosition = Command.Length;
            }

            if (ScrollPosition > Output.Count - MAX_LINES)
                ScrollPosition = Output.Count - MAX_LINES;
            else if (ScrollPosition < 0)
                ScrollPosition = 0;
            try
            {
                Command = Command.Insert(CursorPosition, toInsert);
                CursorPosition += toInsert.Length;
            }
            catch
            {

            }
            lastCurrentCommand = CurrentCommand;
        }

        public static void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            if (!UseConsole)
                return;

            Rectangle consoleArea = new Rectangle(0, 0, Wraith.Program.game.Window.ClientBounds.Width, 300);
            WraithMod.SpriteBatch.Draw(WraithMod.ConsoleBackground, consoleArea, Color.White);

            int j = 0;
            for (int i = MAX_LINES + ScrollPosition; i > 0 + ScrollPosition;)
            {
                string str = Output[Output.Count - i];
                MessageType mt = GetTypeFromChar(str[0]);
                int lines = DrawConsoleString(str.Substring(1), new Vector2(10, (j * 10) + 10), mt);
                i -= lines;
                j += lines;
            }
            DrawConsoleString("> " + Command, new Vector2(10, 280), MessageType.Highlight, false);

            try
            {
                if (KeyDownRecentlyTimer % 1000 > 500 || KeyDownRecently)
                {
                    Rectangle rect = new Rectangle((int)(WraithMod.DefaultFont.MeasureString((Command + "> ").Substring(0, CursorPosition + 2)).X) + 9, 279, 1, 12);
                    WraithMod.SpriteBatch.Draw(WraithMod.WhitePixel, rect, Color.White);
                }
            }
            catch
            {

            }

            // WraithMod.SpriteBatch.DrawString(WraithMod.DefaultFont, str, new Vector2(10, 10), Color.White);
        }

        public static int DrawConsoleString(string str, Vector2 pos, MessageType mt, bool overflowLines = true)
        {
            if (str == "")
                return 1;
            int linesDrawn = 1;
            Rectangle consoleArea = new Rectangle(0, 0, Wraith.Program.game.Window.ClientBounds.Width, 300);
            WraithMod.SpriteBatch.DrawString(WraithMod.DefaultFont, str, Vector2.Add(pos, Vector2.One), Color.Black);
            WraithMod.SpriteBatch.DrawString(WraithMod.DefaultFont, str, pos, GetColorFromType(mt));
            return linesDrawn;
        }

        static double dTime = 0;
        static Dictionary<Keys, int> downTime = new Dictionary<Keys, int>();
        public static string GetStringFromCurrentKeys(GameTime gameTime)
        {
            bool oneDown = false;
            string toInsert = "";
            int eTime = gameTime.ElapsedGameTime.Milliseconds;
            Keys[] keys = Input.Keyboard.GetPressedKeys();
            foreach (Keys k in keys)
            {
                if (!downTime.ContainsKey(k))
                    downTime.Add(k, eTime);
                else
                    downTime[k] += eTime;

                try
                {
                    if (Input.OldKeyboard.IsKeyDown(k))
                        continue;
                }
                catch
                {
                    continue;
                }

                toInsert += GetStringFromKey(k);
            }

            bool broken = Input.Keyboard.GetPressedKeys().Length == Input.OldKeyboard.GetPressedKeys().Length;
            bool repeated = false;
            if (broken == false)
            {
                downTime.Clear();
            }
            while (broken)
            {
                broken = false;
                foreach (Keys k in downTime.Keys)
                {
                    if (!Input.Keyboard.IsKeyDown(k))
                    {
                        downTime.Remove(k);
                        broken = true;
                        break;
                    }
                    else
                    {
                        if (downTime[k] > 500)
                        {
                            broken = false;
                            toInsert += GetStringFromKey(k);
                            repeated = true;
                            break;
                        }
                    }
                }
                if (repeated)
                {
                    break;
                }
            }

            return toInsert;
        }

        public static string GetStringFromKey(Keys k)
        {
            string toInsert = "";

            bool control = false;
            if (Input.Keyboard.IsKeyDown(Keys.LeftControl) || Input.Keyboard.IsKeyDown(Keys.RightControl))
                control = true;

            bool alt = false;
            if (Input.Keyboard.IsKeyDown(Keys.LeftAlt) || Input.Keyboard.IsKeyDown(Keys.RightAlt))
                alt = true;

            bool shift = false;
            if (Input.Keyboard.IsKeyDown(Keys.LeftShift) || Input.Keyboard.IsKeyDown(Keys.RightShift))
                shift = true;

            switch (k)
            {
                case Keys.Left:
                    CursorPosition--;
                    break;
                case Keys.Right:
                    CursorPosition++;
                    break;
                case Keys.Up:
                    if (control)
                        ScrollPosition++;
                    else if (alt)
                        ScrollPosition += MAX_LINES - 1;
                    else
                        CurrentCommand++;
                    break;
                case Keys.Down:
                    if (control)
                        ScrollPosition--;
                    else if (alt)
                        ScrollPosition -= MAX_LINES - 1;
                    else
                        CurrentCommand--;
                    break;
                case Keys.PageUp:
                    if (control)
                        ScrollPosition += 300;
                    ScrollPosition += MAX_LINES - 1;
                    break;
                case Keys.PageDown:
                    if (control)
                        ScrollPosition -= 300;
                    ScrollPosition -= MAX_LINES - 1;
                    break;
                case Keys.Enter:
                    if (Command.Length < 1) break;
                    if (Input.CommandMatch("help", Command, false, false, false, false))
                    {
                        Console.WriteLine("Input: Core help: (/[command] or [lua])\n" +
                            "  Page Up/Down, Ctrl+Up/Down, and Alt+Up/Down are used to scroll through\n" +
                            "  reload - Reloads scripts and commands\n" +
                            "  /help - Commands help\n" +
                            "  help() - LUA help");
                    }
                    else if (Input.CommandMatch("reload", Command))
                    {
                        WraithMod.Core = new Core();
                        WraithMod.Lua = new Lua();
                        WraithMod.Lua.Initialize();
                        WraithMod.Commands.Initialize();
                        WraithMod.LoadBanlist();
                    }
                    else if (Command[0] == Input.CommandSymbol)
                    {
                        try
                        {
                            ParameterizedThreadStart pts = new ParameterizedThreadStart(WraithMod.Commands.RunCommand);
                            Thread t = new Thread(pts);
                            Lua.ScriptThreads.Add(t);
                            t.Start(Command);
                        }
                        catch
                        {
                            Console.WriteLine(MessageType.Error, "Input: Unknown error running command: {0}", Command);
                        }
                    }
                    else if (Command.Trim() != "")
                    {
                        ParameterizedThreadStart pts = new ParameterizedThreadStart(WraithMod.Lua.RunString);
                        Thread t = new Thread(pts);
                        Lua.ScriptThreads.Add(t);
                        t.Start(Command);
                    }
                    try
                    {
                        if (Command != PastCommands.Peek())
                            PastCommands.Push(Command);
                    }
                    catch
                    {
                        PastCommands.Push(Command);
                    }
                    CurrentCommand = -1;
                    Command = "";
                    break;
                case Keys.Escape:
                    Command = "";
                    CursorPosition = 0;
                    break;
                case Keys.Delete:
                    if (Command.Length > 0)
                    {
                        try
                        {
                            Command = Command.Remove(CursorPosition, 1);
                        }
                        catch
                        {

                        }
                    }
                    break;
                case Keys.Back:
                    if (Command.Length > 0)
                    {
                        try
                        {
                            Command = Command.Remove(CursorPosition - 1, 1);
                            CursorPosition--;
                        }
                        catch
                        {

                        }
                    }
                    break;
                case Keys.OemTilde:
                    if (shift)
                        toInsert += "~";
                    else
                        toInsert += "`";
                    break;
                case Keys.OemPlus:
                    if (shift)
                        toInsert += "+";
                    else
                        toInsert += "=";
                    break;
                case Keys.OemPipe:
                    if (shift)
                        toInsert += "|";
                    else
                        toInsert += "\\";
                    break;
                case Keys.OemPeriod:
                    if (shift)
                        toInsert += ">";
                    else
                        toInsert += ".";
                    break;
                case Keys.OemQuestion:
                    if (shift)
                        toInsert += "?";
                    else
                        toInsert += "/";
                    break;
                case Keys.OemQuotes:
                    if (shift)
                        toInsert += "\"";
                    else
                        toInsert += "'";
                    break;
                case Keys.OemSemicolon:
                    if (shift)
                        toInsert += ":";
                    else
                        toInsert += ";";
                    break;
                case Keys.OemOpenBrackets:
                    if (shift)
                        toInsert += "{";
                    else
                        toInsert += "[";
                    break;
                case Keys.OemComma:
                    if (shift)
                        toInsert += "<";
                    else
                        toInsert += ",";
                    break;
                case Keys.OemMinus:
                    if (shift)
                        toInsert += "_";
                    else
                        toInsert += "-";
                    break;
                case Keys.OemCloseBrackets:
                    if (shift)
                        toInsert += "}";
                    else
                        toInsert += "]";
                    break;
                case Keys.OemBackslash:
                    if (shift)
                        toInsert += "|";
                    else
                        toInsert += "\\";
                    break;
                case Keys.NumPad0:
                    toInsert += "0";
                    break;
                case Keys.NumPad1:
                    toInsert += "1";
                    break;
                case Keys.NumPad2:
                    toInsert += "2";
                    break;
                case Keys.NumPad3:
                    toInsert += "3";
                    break;
                case Keys.NumPad4:
                    toInsert += "4";
                    break;
                case Keys.NumPad5:
                    toInsert += "5";
                    break;
                case Keys.NumPad6:
                    toInsert += "6";
                    break;
                case Keys.NumPad7:
                    toInsert += "7";
                    break;
                case Keys.NumPad8:
                    toInsert += "8";
                    break;
                case Keys.NumPad9:
                    toInsert += "9";
                    break;
                case Keys.D0:
                    if (shift)
                        toInsert += ")";
                    else
                        toInsert += "0";
                    break;
                case Keys.D1:
                    if (shift)
                        toInsert += "!";
                    else
                        toInsert += "1";
                    break;
                case Keys.D2:
                    if (shift)
                        toInsert += "@";
                    else
                        toInsert += "2";
                    break;
                case Keys.D3:
                    if (shift)
                        toInsert += "#";
                    else
                        toInsert += "3";
                    break;
                case Keys.D4:
                    if (shift)
                        toInsert += "$";
                    else
                        toInsert += "4";
                    break;
                case Keys.D5:
                    if (shift)
                        toInsert += "%";
                    else
                        toInsert += "5";
                    break;
                case Keys.D6:
                    if (shift)
                        toInsert += "^";
                    else
                        toInsert += "6";
                    break;
                case Keys.D7:
                    if (shift)
                        toInsert += "&";
                    else
                        toInsert += "7";
                    break;
                case Keys.D8:
                    if (shift)
                        toInsert += "*";
                    else
                        toInsert += "8";
                    break;
                case Keys.D9:
                    if (shift)
                        toInsert += "(";
                    else
                        toInsert += "9";
                    break;
                case Keys.Decimal:
                    toInsert += ".";
                    break;
                case Keys.Divide:
                    toInsert += "/.";
                    break;
                case Keys.End:
                    CursorPosition += Command.Length;
                    break;
                case Keys.Home:
                    CursorPosition = 0;
                    break;
                case Keys.Multiply:
                    toInsert += "*";
                    break;
                case Keys.Space:
                    toInsert += " ";
                    break;
                case Keys.Subtract:
                    toInsert += "-";
                    break;
                default:
                    string kstr = k.ToString();
                    if (kstr.Length != 1) break;
                    if (shift)
                        toInsert += kstr.ToUpper();
                    else
                        toInsert += kstr.ToLower();
                    while (WraithMod.DefaultFont.MeasureString("> " + Command).X > Wraith.Program.game.Window.ClientBounds.Width - 20)
                    {
                        Command = Command.Substring(0, Command.Length - 1);
                        CursorPosition--;
                    }
                    break;
            }

            return toInsert;
        }

        public static string GetTimeStamp()
        {
            return DateTime.Now.ToString(WraithMod.DateTimeFmt);
        }

        public static string GetLogTimeStamp()
        {
            return DateTime.Now.ToString(WraithMod.LogDateTimeFmt);
        }

        public static char GetCharFromType(MessageType mt)
        {
            switch (mt)
            {
                case MessageType.Loading:
                    return 'l';
                case MessageType.Highlight:
                    return 'h';
                case MessageType.Error:
                    return 'e';
                case MessageType.Warning:
                    return 'w';
                case MessageType.Fatal:
                    return 'f';
                case MessageType.Special:
                    return 's';
                case MessageType.Info:
                    return 'i';
                default:
                    return 'd';
            }
        }

        public static MessageType GetTypeFromChar(char c)
        {
            switch (c)
            {
                case 'l':
                    return MessageType.Loading;
                case 'e':
                    return MessageType.Error;
                case 'w':
                    return MessageType.Warning;
                case 'f':
                    return MessageType.Fatal;
                case 's':
                    return MessageType.Special;
                case 'i':
                    return MessageType.Info;
                case 'h':
                    return MessageType.Highlight;
                default:
                    return MessageType.Normal;
            }
        }

        public static Color GetColorFromType(MessageType mt)
        {
            switch (mt)
            {
                case MessageType.Loading:
                    return Color.DarkBlue;
                case MessageType.Error:
                    return Color.Red;
                case MessageType.Warning:
                    return Color.Yellow;
                case MessageType.Fatal:
                    return Color.Magenta;
                case MessageType.Special:
                    return Color.Lime;
                case MessageType.Info:
                    return Color.Cyan;
                case MessageType.Highlight:
                    return Color.White;
                default:
                    return Color.Gray;
            }
        }

        public static ConsoleColor GetConsoleColorFromType(MessageType mt)
        {
            switch (mt)
            {
                case MessageType.Loading:
                    return ConsoleColor.DarkBlue;
                case MessageType.Error:
                    return ConsoleColor.Red;
                case MessageType.Warning:
                    return ConsoleColor.Yellow;
                case MessageType.Fatal:
                    return ConsoleColor.Magenta;
                case MessageType.Special:
                    return ConsoleColor.Green;
                case MessageType.Info:
                    return ConsoleColor.Cyan;
                case MessageType.Highlight:
                    return ConsoleColor.White;
                default:
                    return ConsoleColor.Gray;
            }
        }
    }

    public enum MessageType
    {
        Normal,
        Highlight,
        Loading,

        Info,
        Special,

        Warning,
        Error,
        Fatal,
    }
}