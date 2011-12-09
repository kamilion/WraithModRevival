using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Terraria;

namespace Wraith.API
{
    public class LuaEvent
    {
        public string EventString { get; set; }
        public string FunctionName { get; set; }

        public LuaEvent()
        {
            EventString = "";
            FunctionName = "";
        }
    }

    /// <summary>
    /// Contains API functions that always work properly. (Lies!)
    /// </summary>
    public class Core
    {
        List<string> EventStrings = new List<string>();
        List<LuaEvent> Events = new List<LuaEvent>();
        BindingFlags bFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        public Core()
        {
            EventStrings.Clear();
            Events.Clear();
        }

        #region Client/Singleplayer API

        /// <summary>
        /// Sends a specified chat message to the server with the specified color. 
        /// Useless in singleplayer (Terraria.Main.netMode == 0).
        /// </summary>
        /// <param name="message">The chat message to send.</param>
        /// <param name="r">The red value of the color. 0-255</param>
        /// <param name="g">The green value of the color. 0-255</param>
        /// <param name="b">The blue value of the color. 0-255</param>
        [LuaFunction("sendChat", "Sends a chat message", "Message", "Red", "Green", "Blue")]
        public void SendChat(string message, int r = 255, int g = 255, int b = 255)
        {
            if (message == null)
            {
                Console.WriteLine("sendChat() failed (message was null)");
                return;
            }
            NetMessage.SendData(PacketType.ChatMessage, -1, -1, message, 8, r, g, b);
        }

        /// <summary>
        /// Clears the current Player's armor and accessories.
        /// </summary>
        [LuaFunction("clearArmor", "Clears all armor and accessories")]
        public void ClearArmor()
        {
            foreach (Item i in GetPlayer().armor)
            {
                ClearItem(i);
            }
            Console.WriteLine("API: Cleared armor of {0}", GetPlayer().name);
        }

        /// <summary>
        /// Clears the current Player's inventory. 
        /// </summary>
        [LuaFunction("clearInventory", "Clears inventory")]
        public void ClearInventory()
        {
            foreach (Item i in GetPlayer().inventory)
            {
                ClearItem(i);
            }
            Console.WriteLine("API: Cleared inventory of {0}", GetPlayer().name);
        }

        /// <summary>
        /// Calls both ClearArmor() and ClearInventory().
        /// </summary>
        [LuaFunction("clearAll", "Clears all items")]
        public void ClearAll()
        {
            ClearArmor();
            ClearInventory();
        }
        #endregion

        #region Core API

        public static List<Item[]> Inventories = new List<Item[]>(); // The list of inventories

        [LuaFunction("loadInventory", "Loads the Inventory with the specified ID - saveInventory(id) first!", "Inventory ID")]
        public int LoadInventory(int id)
        {
            if (id < 0)
            {
                id = Inventories.Count - 1;
            }
            else if (id > Inventories.Count - 1)
            {
                id = 0;
            }

            for (int i = 0; i < Inventories[id].Length; i++)
            {
                if (i < GetPlayer().inventory.Length)
                    GetInventory()[i].SetDefaults(Inventories[id][i].type);
                else
                    GetPlayer().armor[i - GetPlayer().inventory.Length].SetDefaults(Inventories[id][i].type);
            }

            return id;
        }

        [LuaFunction("saveInventory", "Saves the Inventory at the specified ID - see addInventory", "Inventory ID")]
        public int SaveInventory(int id)
        {
            if (id < 0)
            {
                id = Inventories.Count - 1;
            }
            else if (id > Inventories.Count - 1)
            {
                id = 0;
            }

            for (int i = 0; i < Inventories[id].Length; i++)
            {
                if (i < GetPlayer().inventory.Length)
                    Inventories[id][i].SetDefaults(GetPlayer().inventory[i].type);
                else
                    Inventories[id][i].SetDefaults(GetPlayer().armor[i - GetPlayer().inventory.Length].type);
            }

            return id;
        }

        [LuaFunction("addInventory", "Adds the Inventory to the list of Inventories - returns the newly added Inventory's index")]
        public int AddInventory()
        {
            Item[] inv = new Item[GetPlayer().inventory.Length];
            for (int i = 0; i < GetPlayer().inventory.Length; i++)
            {
                inv[i] = new Item();
            }
            for (int i = 0; i < GetPlayer().inventory.Length; i++)
            {
                if (i < GetPlayer().inventory.Length)
                {
                    inv[i].SetDefaults(GetPlayer().inventory[i].type);
                }
                else
                {
                    inv[i].SetDefaults(GetPlayer().armor[i - GetPlayer().inventory.Length].type);
                }
            }
            Inventories.Add(inv);

            return Inventories.Count - 1;
        }

        /*[LuaFunction("event", "Fires the functions tied to the Event with the specified Event String", "Event String")]
        public void TriggerEvent(string eventString)
        {
            foreach (LuaEvent le in Events)
            {
                try
                {
                    if (le.EventString == eventString)
                    {
                        WraithMod.Lua.GetFunction(le.FunctionName).Call(new object[] { });
                    }
                }
                catch
                {

                }
            }
        }

        [LuaFunction("registerEvent", "Registers the specified event (use getEvents()!) to the specified function name", "Event string (use getEvents()!)", "Lua Function name")]
        public void RegisterEvent(string eventString, string functionName)
        {
            LuaEvent le = new LuaEvent();
            le.EventString = eventString;
            le.FunctionName = functionName;
            Events.Add(le);
        }

        [LuaFunction("getEvents", "Prints a list of the events")]
        public void GetEvents()
        {
            string s = "Lua Events: {1}";
            int count = 0;
            foreach (LuaEvent e in Events)
            {
                count++;
                s += "\n" + e.EventString;
            }
            Console.WriteLine("API: {0}", s);
        }

        [LuaFunction("addEvent", "Creates an event with the specified name", "Event name")]
        public void AddEvent(string name)
        {
            EventStrings.Add(name);
        }*/

        [LuaFunction("killSelf", "Kills the current player")]
        public void KillSelf()
        {
            try
            {
                if (Main.player[Main.myPlayer] == null)
                {
                    int x = 1;
                }
                else
                    Main.player[Main.myPlayer].KillMe(GetPlayer().statLifeMax, 0, false);
            }
            catch
            {
                Console.WriteLine("API: killSelf() failed - unknown reason");
            }
        }

        [LuaFunction("getKey", "Gets the Key with the specified text", "The text of the Key")]
        public Keys GetKey(string keyName)
        {
            Keys key = Keys.F1;
            if (keyName == null || keyName == "")
            {
                Console.WriteLine("API: getKey() failed - keyName was null");
                return key;
            }
            foreach (Keys k in Enum.GetValues(typeof(Keys)))
            {
                if (Input.CommandMatch(k.ToString(), keyName, false, false, false, false))
                    return k;
            }
            Console.WriteLine("API: Could not find key {0} - returned F1", keyName);
            return key;
        }

        [LuaFunction("getKeys", "Gets the Xna.Input.Keys enum")]
        public void GetKeys()
        {
            string toPrint = "Keys:";
            foreach (Keys k in Enum.GetValues(typeof(Keys)))
            {
                toPrint += "  " + k.ToString() + "\n";
            }
            Print(toPrint);
        }

        [LuaFunction("#removeOverlay", "Removes the Overlay with the specified id", "The id of the Overlay to remove")]
        public void RemoveOverlay(int id)
        {
            if (id < WraithMod.Overlay.Count && id > 0)
            {
                try
                {
                    WraithMod.Overlay.RemoveAt(id);
                    WraithMod.OverlayColor.RemoveAt(id);
                    WraithMod.OverlayPosition.RemoveAt(id);
                    return;
                }
                catch
                {

                }
            }
            Console.WriteLine(MessageType.Warning, "API: removeOverlay() failed (invalid id)");
            return;
        }

        [LuaFunction("#addOverlay", "Adds an Overlay - for formatting overlays, use LUA block comments in overlay to parse LUA - example Overlay: 'HP: --[[ getPlayer().statLife .. \"/\" .. getPlayer().statLifeMax ]]--'", "Text of Overlay in Overlay format (see above)", "Color - use createColor()", "Position - use createPosition()")]
        public void AddOverlay(string text, Color color, Vector2 position)
        {
            if (color == null)
            {
                Console.WriteLine(MessageType.Warning, "API: addOverlay() using default color");
                color = Color.White;
            }
            if (text == null)
            {
                Console.WriteLine(MessageType.Warning, "API: addOverlay() failed (text was null)");
                return;
            }
            WraithMod.Overlay.Add(text);
            WraithMod.OverlayColor.Add(color);
            WraithMod.OverlayPosition.Add(position);
        }

        [LuaFunction("createColor", "Creates a color usable in the LUA engine", "Red value", "Green value", "Blue value", "Alpha value")]
        public Color CreateColor(int r, int g, int b, int a)
        {
            return new Color(r, g, b, a);
        }

        [LuaFunction("getNpcByName", "Gets the NPC type with the specified name", "NPC name")]
        public NPC GetNpcByName(string name)
        {
            if (name == null)
            {
                Console.WriteLine(MessageType.Warning, "API: getNpcByName() failed (name was null");
                return null;
            }
            NPC n = new NPC();
            for (int i = 100; i > 0; i--)
            {
                try
                {
                    n.SetDefaults(i);
                    if (n.name == "") continue;
                    if (n.name == null) continue;
                }
                catch
                {
                    continue;
                }
                if (Input.CommandMatch(name, n.name))
                {
                    return n;
                }
            }
            n = null;
            return n;
        }

        [LuaFunction("getNpcs", "Gets all current NPCs")]
        public NPC[] GetNpcs()
        {
            return Main.npc;
        }

        [LuaFunction("sendPacket", "Sends a packet with the specified data", "Message type", "num1", "num2", "num3", "Message", "float1", "float2", "float3")]
        public void SendPacket(int type, int a, int b, int c, string msg, float d, float e, float f)
        {
            NetMessage.SendData(type, a, b, msg, c, d, e, f);
        }

        [LuaFunction("fillTile", "Fills the specified tile with the specified liquid", "X index", "Y index", "Bool lava (water if false)")]
        public void FillTile(int x, int y, bool lava)
        {
            GetTiles()[x, y].lava = lava;
            GetTiles()[x, y].liquid = 255;
            NetMessage.SendData(0x30, -1, -1, "", x, (float)y, 0f, 0f);
            Liquid.AddWater(x, y);
        }

        [LuaFunction("setTile", "Sets the tile's type", "X index", "Y index", "Tile type")]
        public void SetTile(int x, int y, int type)
        {
            NetMessage.SendData(PacketType.ClientTileData, -1, -1, "", 1, (float)x, (float)y, (float)type);
            GetTiles()[x, y].type = (byte)type;
            GetTiles()[x, y].active = true;
        }

        /// <summary>
        /// Resets the tile at the specified coordinates. 
        /// </summary>
        /// <param name="x">The x-coordinate of the tile to reset.</param>
        /// <param name="y">The y-coordinate of the tile to reset.</param>
        [LuaFunction("resetTile", "Erases a tile", "X index", "Y index")]
        public void ResetTile(int x, int y)
        {
            NetMessage.SendData(PacketType.ClientTileData, -1, -1, "", 0, x, y, 0);
            NetMessage.SendData(PacketType.ClientTileData, -1, -1, "", 2, (float)x, (float)y, 0f);
            GetTiles()[x, y] = new Tile();
            GetTiles()[x, y].type = 0;
            GetTiles()[x, y].wall = 0;
        }

        [LuaFunction("runCommand", "Runs the specified command", "The command to run")]
        public void RunCommand(string command)
        {
            WraithMod.Commands.RunCommand(command);
        }

        [LuaFunction("reloadCommands", "Reloads commands")]
        public void RunCommand()
        {
            WraithMod.Commands.Initialize();
        }

        /// <summary>
        /// Deals the specified amount of damage to the Player with the specified id.
        /// </summary>
        /// <param name="pid">The ID of the Player to deal damage to.</param>
        /// <param name="damage">The amount of damage to deal.</param>
        [LuaFunction("damagePlayerById", "Damages a Player", "The Player's ID", "The amount of damage")]
        public void DamagePlayerById(int id, float damage)
        {
            if (Main.netMode == 0 || id == GetPlayer().whoAmi)
                GetPlayer().statLife -= (int)damage;
            else
                NetMessage.SendData(0x1a, -1, -1, "", id, 1f, damage, 1f);
            Console.WriteLine("API: Damaging {0} for {1}", GetPlayerById(id).name, damage);
        }

        /// <summary>
        /// Deals the specified amount of damage to the specified Player.
        /// </summary>
        /// <param name="p">The Player to deal damage to.</param>
        /// <param name="damage">The amount of damage to deal.</param>
        [LuaFunction("damagePlayer", "Damages a Player", "Player object", "The amount of damage")]
        public void DamagePlayer(Player p, float damage)
        {
            DamagePlayerById(p.whoAmi, damage);
        }

        /// <summary>
        /// Deals the specified amount of damage to the specified Player.
        /// </summary>
        /// <param name="name">The name of the Player to deal damage to.</param>
        /// <param name="damage">The amount of damage to deal.</param>
        [LuaFunction("damagePlayerByName", "Damages a Player", "Player's name", "The amount of damage")]
        public void DamagePlayerByName(string name, float damage)
        {
            if (name == null)
            {
                Console.WriteLine(MessageType.Warning, "API: damagePlayerByName() failed (name was null)");
                return;
            }
            int id = GetPlayerIdByName(name);
            if (id == -1)
            {
                Console.WriteLine(MessageType.Warning, "API: damagePlayerByName() failed (Couldn't find player with name {0})", name);
                return;
            }
            DamagePlayerById(id, damage);
        }

        /// <summary>
        /// Deals 200,000 damage to the Player with the specified ID.
        /// </summary>
        /// <param name="pid">The ID of the Player to deal damage to.</param>
        [LuaFunction("kill", "Kills a Player", "Player's ID")]
        public void KillPlayer(int pid)
        {
            DamagePlayerById(pid, 200000);
        }

        /// <summary>
        /// Deals 200,000 damage to the specified Player. 
        /// </summary>
        /// <param name="p">The Player to deal damage to.</param>
        [LuaFunction("killPlayer", "Kills a Player", "Player object")]
        public void KillPlayer(Player p)
        {
            DamagePlayerById(p.whoAmi, 200000);
        }

        /// <summary>
        /// Deals 200,000 damage to the Player with the specified name.
        /// </summary>
        /// <param name="name">The name of the Player to deal damage to.</param>
        [LuaFunction("killPlayerByName", "Kills a Player", "Player's name")]
        public void KillPlayer(string name)
        {
            if (name == null)
            {
                Console.WriteLine(MessageType.Warning, "API: killPlayerByName() failed (name was null)");
                return;
            }
            int id = GetPlayerIdByName(name);
            if (id == -1)
            {
                Console.WriteLine(MessageType.Warning, "API: killPlayerByName() failed (Couldn't find player with name {0})", name);
                return;
            }
            DamagePlayerById(id, 200000);
        }

        [LuaFunction("getMain", "Gets the main WraithMod class")]
        public WraithMod GetMain()
        {
            return Wraith.Program.game;
        }

        [LuaFunction("runScript", "Runs the specified script", "The relative path to the script")]
        public void RunScript(string file)
        {
            try
            {
                try
                {
                    if (file.Substring(file.Length - 4) != ".lua")
                        file += ".lua";
                    if (file.Substring(0, Lua.ScriptsPath.Length) != Lua.ScriptsPath)
                        file = file.Insert(0, Lua.ScriptsPath);
                }
                catch
                {

                }
                Console.WriteLine("Lua: Running {0}", file);
                try
                {
                    ParameterizedThreadStart pts = new ParameterizedThreadStart(WraithMod.Lua.RunFile);
                    Thread t = new Thread(pts);
                    Lua.ScriptThreads.Add(t);
                    t.Start(file);
                }
                catch
                {
                }
                // WraithMod.Lua.DoFile(Lua.ScriptsPath + file + ".lua");
                return;
            }
            catch (System.IO.FileNotFoundException e)
            {
                Console.WriteLine(MessageType.Warning, "Lua: Could not load {0} - error in path\n  Message: {1}", file, e.Message);
            }
            catch (LuaInterface.LuaException e)
            {
                Console.WriteLine(MessageType.Warning, "Lua: Could not load {0}", file);
                Console.WriteLine(MessageType.Error, "Lua: Error: {0}", e.Message);
            }
            /*catch (Exception e)
            {
                Console.WriteLine(MessageType.Warning, "Lua: Could not load {0}", file);
                Console.WriteLine(MessageType.Error, "Lua: Error: {0}", e.Message);
            }*/
            Console.WriteLine(MessageType.Info, "Lua: Skipped loading script {0}", file);
        }

        [LuaFunction("help", "List available functions")]
        public void Help()
        {
            string s = "";
            IDictionaryEnumerator funcs = WraithMod.Lua.LuaFunctions.GetEnumerator();
            while (funcs.MoveNext())
            {
                s += ((LuaFunctionDescriptor)funcs.Value).Documentation;
            }
            s += "Looking for something more specific? Try the helpcmd() function";
            Console.WriteLine("API: Available functions: \n{0}", s);
        }

        [LuaFunction("helpcmd", "Show help for a given function or search for functions where the name contains the give string", "Function to get help for or string to search for in function names")]
        public void HelpCmd(string command)
        {
            IDictionaryEnumerator funcs = WraithMod.Lua.LuaFunctions.GetEnumerator();
            string s = "";
            while (funcs.MoveNext())
            {
                int min = Math.Min(command.Length, funcs.Key.ToString().Length);
                if (funcs.Key.ToString().ToLower().Contains(command.ToLower()))
                    s += ((LuaFunctionDescriptor)funcs.Value).Documentation;
            }
            if (s == "")
            {
                Console.WriteLine(MessageType.Warning, "API: No API function references found containing string {0}", command);
                return;
            }

            Console.WriteLine("API: Found following functions: \n{0}", s);
        }

        /// <summary>
        /// Writes the specified string to the console.
        /// </summary>
        /// <param name="str">The string to write to the console.</param>
        [LuaFunction("print", "Write a message to the console", "The string to write to the console")]
        public void Print(string str)
        {
            Console.WriteLine("API: " + str);
        }

        /// <summary>
        /// Gets an array containing all the projectiles currently in the world. 
        /// </summary>
        /// <returns>Terraria.Main.projectile</returns>
        [LuaFunction("getProjectiles", "Gets all Projectiles from the world")]
        public Projectile[] GetProjectiles()
        {
            return Main.projectile;
        }

        /// <summary>
        /// Can be used to spawn Items.
        /// </summary>
        [LuaFunction("setItem", "Sets the mouse Item", "Item object")]
        public void SetMouseItem(Item item)
        {
            Main.mouseItem = item;
        }

        /// <summary>
        /// Gets a Projectile type by name.
        /// </summary>
        /// <param name="name">The Projectile.name to check for.</param>
        /// <returns>A default Projectile with a matching name.</returns>
        [LuaFunction("getProjectileByName", "Get's a Projectile type by name", "Name of projectile")]
        public Projectile GetProjectile(string name)
        {
            Projectile proj = new Projectile();
            proj.SetDefaults(0);
            for (int i = 100; i > -1; i--) // Start at 100 to leave room to grow
            {
                try
                {
                    proj.SetDefaults(i);
                    if (proj.name == "" || proj.name == null) continue;
                    if (proj.name.ToLower() == name.ToLower())
                    {
                        break;
                    }
                    else if (proj.name.Replace(" ", "") == name.ToLower())
                    {
                        break;
                    }
                    else
                    {
                        proj.type = -1;
                    }
                }
                catch
                {
                    continue;
                }
            }
            if (proj.type == -1)
            {
                Console.WriteLine(MessageType.Warning, "API: Failed to find item {0}", name);
                proj.SetDefaults(0);
            }
            return proj;
        }

        /// <summary>
        /// Resets the item to a blank item/empty slot.
        /// </summary>
        /// <param name="i">The item to reset.</param>
        [LuaFunction("clearItem", "Destroys specified Item", "Item object")]
        public void ClearItem(Item i)
        {
            i.SetDefaults(0);
            i.name = "";
            i.stack = 0;
        }

        /// <summary>
        /// Gets a Projectile by type. 
        /// </summary>
        /// <param name="type">The type of the Projectile to get.</param>
        /// <returns>A default Projectile with the specified type.</returns>
        [LuaFunction("getProjectile", "Gets a Projectile by type", "The Projectile type")]
        public Projectile GetProjectile(int type)
        {
            Projectile p = new Projectile();
            p.SetDefaults(type);
            if (p.name == "" || p.name == null)
            {
                p.SetDefaults(0);
            }
            return p;
        }

        /// <summary>
        /// Returns all non-null, valid Projectiles through GetProjectile(int).
        /// <para>"Static" in the function name is probably inaccurate.</para>
        /// <seealso cref="GetProjectile(int)"/>
        /// </summary>
        /// <returns>An array of all Projectiles by type.</returns>
        [LuaFunction("getDefaultProjectiles", "Gets all available Projectile types")]
        public Projectile[] GetStaticProjectiles()
        {
            List<Projectile> projectiles = new List<Projectile>();
            for (int i = 100; i > -1; i--)
            {
                Projectile projectile = new Projectile();
                projectile.SetDefaults(i);
                try
                {
                    if (projectile.name.ToLower() != "")
                    {
                        projectiles.Add(projectile);
                    }
                }
                catch
                {
                    continue;
                }
            }
            return projectiles.ToArray();
        }

        /// <summary>
        /// Returns all non-null, valid Projectiles through GetProjectile(int).
        /// <para>"Static" in the function name is probably inaccurate.</para>
        /// </summary>
        /// <returns>A list of all non-null or blank default items.</returns>
        [LuaFunction("getDefaultItems", "Gets all available Item types")]
        public Item[] GetStaticItems()
        {
            List<Item> items = new List<Item>();
            for (int i = 500; i > -1; i--)
            {
                Item item = new Item();
                item.SetDefaults(i);
                try
                {
                    if (item.name != "" && item.name != null)
                    {
                        items.Add(item);
                    }
                }
                catch
                {

                }
            }
            return items.ToArray();
        }

        /// <summary>
        /// Returns the current Player.
        /// Best not called during menus as returns could produce unwanted results. 
        /// Should be mostly harmless though. 
        /// <para>Should be checked using this, just in case: </para>
        /// <para>if (name != "" || name != null)</para>
        /// </summary>
        /// <returns>The current Player. If there is no current Player, returns GetPlayers()[0]. </returns>
        [LuaFunction("getPlayer", "Gets the current Player")]
        public Player GetPlayer()
        {
            try
            {
                return GetPlayers()[Main.myPlayer];
            }
            catch
            {
                return GetPlayers()[0];
            }
        }

        [LuaFunction("getItem", "Gets an Item by type", "The type of the Item to get")]
        public Item GetItemByType(int type)
        {
            Item item = new Item();
            item.SetDefaults(type);
            return item;
        }

        [LuaFunction("getItemByName", "Gets an Item by name", "The name of the Item to get")]
        public Item GetItemByName(string name)
        {
            Item item = new Item();
            if (name == null)
            {
                Console.WriteLine(MessageType.Warning, "API: getItemByName() failed (name was null");
                item = new Item();
                ClearItem(item);
                return item;
            }
            foreach (Item i in GetStaticItems())
            {
                if (Input.CommandMatch(name, i.name, false, false, false, false))
                {
                    return i;
                }
            }
            foreach (Item i in GetStaticItems())
            {
                if (Input.CommandMatch(name, i.name, false, false, false, true))
                {
                    return i;
                }
            }
            ClearItem(item);
            return item;
        }

        [LuaFunction("getItemStackByName", "Gets an Item by name", "The name of the Item to get", "How many of the Item to get")]
        public Item GetItemStackByName(string name, int amount)
        {
            foreach (Item i in GetStaticItems())
            {
                if (Input.CommandMatch(name, i.name, false, false, false, false))
                {
                    i.stack = amount;
                    return i;
                }
            }
            foreach (Item i in GetStaticItems())
            {
                if (Input.CommandMatch(name, i.name, false, false, false, true))
                {
                    i.stack = amount;
                    return i;
                }
            }
            Item item = new Item();
            ClearItem(item);
            return item;
        }

        [LuaFunction("getItemStackByType", "Gets an Item by name", "The type of the Item to get", "How many of the Item to get")]
        public Item GetItemStackByType(int type, int amount)
        {
            foreach (Item i in GetStaticItems())
            {
                if (i.type == type)
                {
                    i.stack = amount;
                    return i;
                }
            }
            Item item = new Item();
            ClearItem(item);
            return item;
        }

        [LuaFunction("renamePlayer", "Renamed the Player with the specified id to the specified name", "Player ID", "Player name")]
        public void RenamePlayer(int id, string name)
        {
            if (name == null)
            {
                Console.WriteLine(MessageType.Warning, "API: renamePlayer() failed (name was null)");
                return;
            }
            if (id >= 0 && id <= 7)
            {
                NetMessage.SendData(4, -1, GetPlayers()[id].whoAmi, name, id, 0f, 0f, 0f);
                GetPlayers()[id].name = name;
            }
            else
            {
                Console.WriteLine(MessageType.Warning, "API: renamePlayer() failed (invalid id {0})", id);
            }
        }

        /// <summary>
        /// Gets a Player by name.
        /// <seealso cref="GetPlayer()"/>
        /// </summary>
        /// <param name="name">The name to look for. (Note: Case sensitive)</param>
        /// <returns>The Player with the specified name. If not found, returns GetPlayer()</returns>
        [LuaFunction("getPlayerByName", "Gets a Player by name", "Player name")]
        public Player GetPlayerByName(string name)
        {
            if (name == null)
            {
                Console.WriteLine(MessageType.Warning, "API: renamePlayer() failed (name was null)");
                return GetPlayer();
            }
            Player[] players = GetPlayers();
            for (int i = 0; i < players.Length; i++)
            {
                Player p = players[i];
                if (p.name == name)
                {
                    return p;
                }
            }
            return GetPlayers()[Main.myPlayer];
        }

        [LuaFunction("getPlayerIdByName", "Gets the id of the Player with the specified name", "Player name")]
        public int GetPlayerIdByName(string name)
        {
            if (name == null)
            {
                Console.WriteLine(MessageType.Warning, "API: renamePlayer() failed (name was null)");
                return -1;
            }
            Player[] players = GetPlayers();
            for (int i = 0; i < players.Length; i++)
            {
                Player p = players[i];
                if (p.name == name)
                {
                    return p.whoAmi;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the the current Player's inventory.
        /// </summary>
        /// <returns>GetPlayer().inventory</returns>
        [LuaFunction("getInventory", "Gets the current Player's inventory")]
        public Item[] GetInventory()
        {
            return GetPlayer().inventory;
        }

        /// <summary>
        /// Returns a Player by id.
        /// <para>Should be checked using this, just in case: </para>
        /// <para>if (name != "" || name != null)</para>
        /// </summary>
        /// <param name="id">The id of the Player to get.</param>
        /// <returns>GetPlayers()[id]</returns>
        [LuaFunction("getPlayerById", "Gets a Player with the specified id", "Player id")]
        public Player GetPlayerById(int id)
        {
            return Main.player[id];
        }

        /// <summary>
        /// Returns an array of all Players. 
        /// <para>When looping through, you should check using this, just in case: </para>
        /// <para>if (name != "" || name != null)</para>
        /// </summary>
        /// <returns>Main.player</returns>
        [LuaFunction("getPlayers", "Gets a list of current Players")]
        public Player[] GetPlayers()
        {
            return Main.player;
        }

        /// <summary>
        /// Returns the tile at indices (X, Y)
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <returns>GetTiles()[x, y]</returns>
        [LuaFunction("getTile", "Gets a Tile at the specified indeces", "X index", "Y index")]
        public Tile GetTile(int x, int y)
        {
            try
            {
                return GetTiles()[x, y];
            }
            catch
            {
                Console.WriteLine(MessageType.Warning, "API: Couldn't find Tile at ({0}, {1})", x, y);
                return null;
            }
        }

        /// <summary>
        /// Returns the entire map as a two-dimenstional Tile array.
        /// </summary>
        /// <returns>Main.tile</returns>
        [LuaFunction("getTiles", "Gets the whole map in tiles")]
        public Tile[,] GetTiles()
        {
            return Main.tile;
        }

        [LuaFunction("createPosition", "Creates a usable Vector2 for LUA", "X position", "Y position")]
        public Vector2 CreatePosition(float x, float y)
        {
            return new Vector2(x, y);
        }

        [LuaFunction("setField", "Sets the field with the specified name of the specified target to the specified value", "Object with fields", "Field to set", "The new value")]
        public void SetField(object o, string fieldName, object value)
        {
            if (o == null)
            {
                Console.WriteLine(MessageType.Warning, "API: setField() failed (target object \"o\" was null)");
                return;
            }
            if (fieldName == null)
            {
                Console.WriteLine(MessageType.Warning, "API: setField() failed (fieldName was null)");
                return;
            }
            if (value == null)
            {
                Console.WriteLine(MessageType.Warning, "API: setField() failed (value was null)");
                return;
            }
            try
            {
                FieldInfo[] fis = o.GetType().GetFields(bFlags);
                PropertyInfo[] pis = o.GetType().GetProperties(bFlags);
                string n = o.GetType().Name;
                FieldInfo fii = null;
                PropertyInfo pii = null;
                if (n == "RuntimeType")
                {
                    n = ((Type)o).Name;
                    fis = ((Type)o).GetFields();
                    pis = ((Type)o).GetProperties();
                }
                foreach (FieldInfo fi in fis)
                {
                    if (Input.CommandMatch(n, fieldName))
                    {
                        fii = fi;
                        break;  
                    }
                }
                foreach (PropertyInfo pi in pis)
                {
                    if (Input.CommandMatch(n, fieldName))
                    {
                        pii = pi;
                        break;
                    }
                }
                Type t = null;
                if (fii != null)
                {
                    t = fii.FieldType;
                }
                else if (pii != null)
                {
                    t = pii.PropertyType;
                }
                else
                {
                    Console.WriteLine(MessageType.Warning, "API: Could not find field {0}");
                    return;
                }

                if (t == null)
                {
                    // LOLWTF
                }
                else if (t == typeof(int))
                {
                    int val = 0;
                    int.TryParse(value.ToString(), out val);
                    value = val;
                }
                else if (t == typeof(double))
                {
                    double val = 0;
                    double.TryParse(value.ToString(), out val);
                    value = val;
                }
                else if (t == typeof(float))
                {
                    float val = 0;
                    float.TryParse(value.ToString(), out val);
                    value = val;
                }
                else if (t == typeof(bool))
                {
                    bool val = false;
                    bool.TryParse(value.ToString(), out val);
                    value = val;
                }
                else if (t == typeof(string))
                    value = value.ToString();

                if (fii != null)
                    fii.SetValue(o, value);
                else if (pii != null)
                    pii.SetValue(o, value, null);
                else
                {
                    // LOLWTF
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(MessageType.Warning, "API: setField() failed: {0}", e.Message);
            }
        }

        [LuaFunction("searchFields", "Prints a list of all the fields with names containing the specified filter", "Object with fields", "Field to search for")]
        public void SearchFields(object o, string filter)
        {
            if (o == null)
            {
                Console.WriteLine(MessageType.Warning, "API: searchFields() failed (object was null)");
                return;
            }
            if (filter == null)
            {
                Console.WriteLine(MessageType.Warning, "API: searchFields() failed (filter was null)");
                return;
            }
            string s = "";
            FieldInfo[] fis = o.GetType().GetFields(bFlags);
            PropertyInfo[] pis = o.GetType().GetProperties(bFlags);
            foreach (FieldInfo fi in fis)
            {
                if (fi.Name.ToLower().Contains(filter.ToLower()))
                    s += fi.FieldType.Name + " " + fi.Name + "\n";
            }
            Console.WriteLine("API: Field list of {0}: \n{1}", o.ToString(), s);
            foreach (PropertyInfo pi in pis)
            {
                if (pi.Name.ToLower().Contains(filter.ToLower()))
                    s += pi.PropertyType.Name + " " + pi.Name + "\n";
            }
            Console.WriteLine("API: Property list of {0}: \n{1}", o.ToString(), s);
        }

        [LuaFunction("printFields", "Prints a list of all the fields in the specified object", "Object with fields")]
        public void PrintFields(object o)
        {
            if (o == null)
            {
                Console.WriteLine(MessageType.Warning, "API: printFields() failed (object was null)");
                return;
            }
            string s = "";
            string n = o.GetType().Name;
            FieldInfo[] fis = o.GetType().GetFields(bFlags);
            PropertyInfo[] pis = o.GetType().GetProperties(bFlags);
            if (n == "RuntimeType")
            {
                n = ((Type)o).Name;
                fis = ((Type)o).GetFields();
                pis = ((Type)o).GetProperties();
            }
            foreach (FieldInfo fi in fis)
            {
                s += fi.FieldType.Name + " " + fi.Name + "\n";
            }
            Console.WriteLine("API: Field list of {0}: \n{1}", o.ToString(), s);
            foreach (PropertyInfo pi in pis)
            {
                s += pi.PropertyType.Name + " " + pi.Name + "\n";
            }
            Console.WriteLine("API: Property list of {0}: \n{1}", o.ToString(), s);
        }

        [LuaFunction("searchMethods", "Prints a list of all the fields with names containing the specified filter", "Object with fields", "Field to search for")]
        public void SearchMethods(object o, string filter)
        {
            if (o == null)
            {
                Console.WriteLine(MessageType.Warning, "API: searchMethods() failed (object was null)");
                return;
            }
            if (filter == null)
            {
                Console.WriteLine(MessageType.Warning, "API: searchMethods() failed (filter was null)");
                return;
            }
            string s = "";
            foreach (MethodInfo fi in o.GetType().GetMethods(bFlags))
            {
                if (fi.Name.ToLower().Contains(filter.ToLower()))
                {
                    s += fi.Name + " " + fi.Name + "\n";
                }
            }
            Console.WriteLine("API: Method list of {0}: \n{1}", o.ToString(), s);
        }

        [LuaFunction("printMethods", "Prints a list of all the methods (functions) in the specified object", "Object with fields")]
        public void PrintMethods(object o)
        {
            if (o == null)
            {
                Console.WriteLine(MessageType.Warning, "API: printMethods() failed (object was null)");
                return;
            }
            string s = "";
            string n = o.GetType().Name;
            foreach (MethodInfo fi in o.GetType().GetMethods(bFlags))
            {
                s += fi.Name + " " + fi.Name + "\n";
            }
            Console.WriteLine("API: Method list of {0}: \n{1}", o.ToString(), s);
        }

        /// <summary>
        /// Returns a FieldInfo array containing the fields of the specified Projectile. 
        /// </summary>
        /// <param name="p">The Projectile whose fields to return.</param>
        /// <returns>p.GetType().GetFields()</returns>
        [LuaFunction("getProjectileFields", "Returns the FieldInfo[] for the specified Projectile", "Projectile object")]
        public FieldInfo[] GetProjectileFields(Projectile p)
        {
            return p.GetType().GetFields();
        }

        /// <summary>
        /// Returns a FieldInfo array containing the fields of the specified Item. 
        /// </summary>
        /// <param name="item">The Item whose fields to return.</param>
        /// <returns>item.GetType().GetFields()</returns>
        [LuaFunction("getItemFields", "Returns the FieldInfo[] for the specified Item", "Item object")]
        public FieldInfo[] GetItemFields(Item item)
        {
            return item.GetType().GetFields();
        }

        /// <summary>
        /// Returns a FieldInfo array containing the fields of the current Player. 
        /// </summary>
        /// <returns>GetPlayer().GetType().GetFields()</returns>
        [LuaFunction("getPlayerFields", "Returns the FieldInfo[] for the current Player")]
        public FieldInfo[] GetPlayerFields()
        {
            return GetPlayer().GetType().GetFields();
        }

        /// <summary>
        /// Returns the posiition of the current Player.
        /// </summary>
        /// <returns>GetPlayer().position</returns>
        [LuaFunction("getLocation", "Gets the position for the current Player")]
        public Vector2 GetLocation()
        {
            return GetPlayer().position;
        }

        /// <summary>
        /// Returns the posiition of the Player with the specified id. 
        /// </summary>
        /// <param name="pid">The id of the Player whose position to return. </param>
        /// <returns>GetPlayer(pid).position</returns>
        [LuaFunction("getLocationById", "Gets the position for the Player with the specified id", "Player id")]
        public Vector2 GetLocation(int id)
        {
            return GetPlayerById(id).position;
        }

        /// <summary>
        /// Returns the posiition of the Player with the specified name. 
        /// </summary>
        /// <param name="name">The name the Player whose position to return. </param>
        /// <returns>GetPlayer(name).position</returns>
        [LuaFunction("getLocationByName", "Gets the position for the Player with the specified name", "Player name")]
        public Vector2 GetLocation(string name)
        {
            return GetPlayerByName(name).position;
        }
        #endregion

        #region Server API

        [LuaFunction("unban", "Unbans the specified IP", "IP address")]
        public void Unban(string ip)
        {
            for (int i = 0; i < WraithMod.Banlist.Count; i++)
            {
                if (WraithMod.Banlist[i].IP == ip.ToLower().Trim())
                {
                    WraithMod.Banlist.RemoveAt(i);
                    WraithMod.SaveBanlist();
                    return;
                }
            }
        }

        [LuaFunction("ban", "Bans the Player with the specified id", "Player id", "The reason for banning")]
        public void Ban(int id, string reason = "Noobery.")
        {
            string ip = GetIp(id);
            if (ip == "")
            {
                Console.WriteLine(MessageType.Warning, "API: ban() failed (getIP() returned null)");
                return;
            }
            BanIp(ip, reason, GetPlayerById(id).name);
        }

        [LuaFunction("banByName", "Bans the Player with the specified name", "Player name", "The reason for banning")]
        public void BanByName(string name, string reason = "Noobery.")
        {
            if (name == null || name == "")
            {
                Console.WriteLine(MessageType.Warning, "API: banByName() failed (name was null)");
                return;
            }
            int id = GetPlayerIdByName(name);
            if (id == -1)
            {
                Console.WriteLine(MessageType.Warning, "API: Could not find player {0}", name);
                return;
            }
            Ban(id);
        }

        [LuaFunction("banIp", "Bans the specified IP", "The IP to ban", "The reason for banning", "The Player's name")]
        public void BanIp(string ip, string reason = "Noobery.", string name = "Unknown")
        {
            BannedPlayer bp = new BannedPlayer();
            bp.IP = ip;
            bp.Name = name;
            bp.Reason = reason;
            WraithMod.Banlist.Add(bp);
            Console.WriteLine(MessageType.Special, "API: Banned {0}", ip);
            WraithMod.SaveBanlist();
        }

        [LuaFunction("getIp", "Gets the IP of the Player with the specified id", "Player id")]
        public string GetIp(int id)
        {
            if (Main.netMode == 2)
            {
                Player p = GetPlayers()[id];
                if (p.name == null || p.name == "")
                {
                    Console.WriteLine(MessageType.Warning, "API: getIp() failed (empty player at id {0})", id);
                    return "";
                }
                return Netplay.serverSock[p.whoAmi].statusText.Split(':')[0].Substring(1);
            }
            return "";
        }

        [LuaFunction("spawnNpc", "Spawns an NPC at the specified location of specified type", "X position", "Y position", "NPC type")]
        public void SpawnNpc(int x, int y, int type)
        {
            if (Main.netMode == 1)
            {
                Console.WriteLine("Client, you are not a server. You cannot even spawn NPCs.");
                return;
            }
            GetNpcs()[NPC.NewNPC(x, y, type, 1)].netUpdate = true;
        }

        /// <summary>
        /// Sends a specified chat message to the Player with the specified ID with the specified color. 
        /// Useless in singleplayer (Terraria.Main.netMode == 0).
        /// </summary>
        /// <param name="message">The chat message to send.</param>
        /// <param name="id">The ID of the Player</param>
        /// <param name="r">The red value of the color. 0-255</param>
        /// <param name="g">The green value of the color. 0-255</param>
        /// <param name="b">The blue value of the color. 0-255</param>
        [LuaFunction("sendChatToPlayer", "Sends a chat message", "Message", "Player id", "Red", "Green", "Blue")]
        public void SendChatToPlayer(string message, int id, int r = 255, int g = 255, int b = 255)
        {
            NetMessage.SendData(PacketType.ChatMessage, -1, -1, message, id, r, g, b);
        }

        [LuaFunction("broadcast", "Sends a chat message to all clients", "Message", "Red", "Green", "Blue")]
        public void Broadcast(string message, int r = 255, int g = 255, int b = 255)
        {
            if (Main.netMode == 2)
            {
                NetMessage.SendData(PacketType.ChatMessage, -1, -1, message, 7, r, g, b);
                NetMessage.SendData(PacketType.ChatMessage, -1, -1, message, 6, r, g, b);
                NetMessage.SendData(PacketType.ChatMessage, -1, -1, message, 5, r, g, b);
                NetMessage.SendData(PacketType.ChatMessage, -1, -1, message, 4, r, g, b);
                NetMessage.SendData(PacketType.ChatMessage, -1, -1, message, 3, r, g, b);
                NetMessage.SendData(PacketType.ChatMessage, -1, -1, message, 2, r, g, b);
                NetMessage.SendData(PacketType.ChatMessage, -1, -1, message, 1, r, g, b);
                NetMessage.SendData(PacketType.ChatMessage, -1, -1, message, 0, r, g, b);
            }
            else
            {
                SendChat(message, r, g, b);
            }
        }

        /// <summary>
        /// Kicks the Player with the specified ID. (Must be running server.)
        /// </summary>
        /// <param name="id">The ID of the connected Player to kick.</param>
        [LuaFunction("kick", "Kicks the Player with the specified id", "Player id")]
        public void Kick(int id)
        {
            Kick(id, "Kicked, n00b.");
        }

        /// <summary>
        /// Kicks the Player with the specified ID. (Must be running server.)
        /// </summary>
        /// <param name="id">The ID of the connected Player to kick.</param>
        [LuaFunction("kickWithReason", "Kicks the Player with the specified id", "Player id", "Message to player with reason for kicking")]
        public void Kick(int id, string reason)
        {
            if (Main.netMode == 2)
            {
                Console.WriteLine("API: Kicked {0}.", GetPlayerById(id).name);
                NetMessage.SendData(PacketType.ServerDisconnect, id, id, reason + " [WraithMod " + WraithMod.VersionString + "]", id, id, id, id);
                try
                {
                    Netplay.serverSock[id].kill = true;
                    Netplay.serverSock[id].Reset();
                    NetMessage.syncPlayers();
                }
                catch
                {

                }
            }
            else
            {
                Console.WriteLine("API: You cannot kick unless you are hosting, duh.");
            }
        }

        /// <summary>
        /// Kicks the Player with the specified ID. (Must be running server.)
        /// </summary>
        /// <param name="id">The ID of the connected Player to kick.</param>
        [LuaFunction("kickByName", "Kicks the Player with the specified name", "Player name", "Message to player with reason for kicking")]
        public void KickByName(string name, string reason)
        {
            int id = GetPlayerIdByName(name);
            if (id == -1)
            {
                Console.WriteLine("API: Could not find Player with name {0}", name);
                return;
            }
            Kick(id, reason);
        }

        #endregion
    }

    /// <summary>
    /// The attribute used to describe functions to be registered to the LUA engine. 
    /// </summary>
    public class LuaFunction : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Args { get; set; }

        public LuaFunction(string name, string description, params string[] args)
        {
            Name = name;
            Description = description;
            Args = args;
        }

        public LuaFunction(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Used to document LUA engine functions registered from the API. 
    /// </summary>
    public class LuaFunctionDescriptor
    {
        public string Command { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Args { get; set; }
        public string[] ArgDocs { get; set; }

        public string Documentation { get; set; }

        public string Header
        {
            get
            {
                try
                {
                    return Documentation.Split('\n')[0];
                }
                catch
                {
                    return "";
                }
            }
        }

        public LuaFunctionDescriptor()
        {
            Command = "";
            Name = "";
            Description = "";
            Args = new string[]{ "" };
            ArgDocs = new string[]{ "" };
            Documentation = "";
        }

        public LuaFunctionDescriptor(string name, string description, string[] args, string[] argDocs)
        {
            Name = name;
            Description = description;
            Args = args;
            ArgDocs = argDocs;

            MakeDocumentation();
        }

        public void MakeDocumentation(bool isCommand = false)
        {
            string header = Name + " %params% - " + Description;
            string body = "\n";
            string arguments = "";

            bool first = true;

            if (Args.Length > 0)
            {
                for (int i = 0; i < Args.Length; i++)
                {
                    if (!first)
                        arguments += ", ";

                    arguments += Args[i];
                    body += "  " + ArgDocs[i] + "\n";

                    first = false;
                }
            }
            else
            {
                arguments = "";
            }

            Documentation = header.Replace("%params%", arguments) + body;
        }
    }

    public static class PacketType
    {
        public static int ClientRequestPassword     { get { return 1    ; } }
        public static int ServerDisconnect          { get { return 2    ; } }
        public static int ServerRequestPlayerData   { get { return 3    ; } }
        public static int ClientPlayerData          { get { return 4    ; } }
        public static int ClientTileData            { get { return 17   ; } }
        public static int ChatMessage               { get { return 25   ; } }
        public static int DamagePlayer              { get { return 26   ; } }
    }
}
