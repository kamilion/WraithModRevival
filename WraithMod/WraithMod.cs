using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.IO;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Wraith.API;

using Terraria;

namespace Wraith
{
    static class Program
    {
        public static WraithMod game;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (game = new WraithMod())
            {
                game.Run();
            }
        }
    }

    public class WraithMod : Main
    {
        public const string SCRIPTS_PATH = "Scripts/";
        public const string STARTUP_SCRIPT = "startup.lua";
        public const string ONLOAD_SCRIPT = "onload.lua";

        public const string COMMANDS_PATH = "Commands/";

        public const string BANLIST_FILE = "banlist.txt";
        public const string WHITELIST_FILE = "whitelist.txt";

        private static bool debug = false;
        private static bool useWhitelist = false;
        private static bool useBanlist = true;

        public static bool Debug { get { return debug; } set { debug = value; } }
        public static bool UseWhitelist { get { return useWhitelist; } set { useWhitelist = value; } }
        public static bool UseBanlist { get { return useBanlist; } set { useBanlist = value; } }
        public static bool Pause { get; set; }
        public static bool UseOverlay { get; set; }
        public static bool DedicatedServer { get { return dedServ; } set { dedServ = value; } }
        public static List<string> Overlay { get; set; }
        public static List<Vector2> OverlayPosition { get; set; }
        public static List<Color> OverlayColor { get; set; }

        public static List<BannedPlayer> Banlist { get; set; }
        public static Version Version;
        public static string VersionString { get; set; }
        public static Core Core = new Core();
        public static Lua Lua = new Lua();
        public static Commands Commands = new Commands();

        public static SpriteBatch SpriteBatch { get; set; }

        public static SpriteFont DefaultFont { get; set; }
        public static Texture2D ConsoleBackground;
        public static Texture2D WhitePixel { get; set; }
        
        public static string DateTimeFmt { get; set; }
        public static string LogDateTimeFmt { get; set; }

        public static GraphicsDeviceManager Graphics; 

        public static Texture2D DefaultTexture { get; set; } // A single white pixel texture
        public static Color SelectionOverlay { get; set; } // The color of the drawn selection overlay

        public static Type WorldGenWrapper { get; set; } // For accessing WorldGen functions
        public static Type MainWrapper { get; set; } // For accessing private Main members

        Vector2 sel1 = Vector2.Zero;
        Vector2 sel2 = Vector2.Zero;

        public Point SelectionSize = new Point(0, 0);
        public Point SelectionPosition = new Point(0, 0);
        public bool[,] SelectedTiles = new bool[1, 1];

        public Point CopiedSize = new Point(0, 0);
        public Tile[,] Copied = new Tile[1, 1];

        public Point UndoSize = new Point(0, 0);
        public Point UndoPosition = new Point(0, 0);
        public Tile[,] Undo = new Tile[1, 1];

        public bool buildMode { get; set; }
        public int inventoryIndex = 0;
        public int oldMenuMode = 0;
        public bool hover = false;
        public Vector2 lastPosition = Vector2.Zero;
        public bool itemHax = false;
        public bool npcsEnabled = true;
        public KeyboardState oldKeyState = new KeyboardState();
        
        public WraithMod()
            : base()
        {
            // Initialize Properties
            GodMode = false;
            DedicatedServer = false;
            Pause = false;
            Banlist = new List<BannedPlayer>();
            SelectionOverlay = new Color(255, 100, 0, 50);

            DateTimeFmt = "HH:mm:ss";
            LogDateTimeFmt = "dd/MM/yy HH:mm:ss";

            Overlay = new List<string>();
            OverlayPosition = new List<Vector2>();
            OverlayColor = new List<Color>();

            Version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionString = Version.Major + "." + Version.Minor + "." + Version.Build;
            Exiting += new EventHandler<EventArgs>(WraithMod_Exiting);
            Input.InputGiven += new Input.ConsoleEventHandler(Input_InputGiven);

            Console.Initialize();
            Console.WriteLine("Core: Initializing...");
            Core = new Core();
            Lua = new Lua();
            Commands = new Commands();

            if (DedicatedServer)
            {
                Lua.Initialize(SCRIPTS_PATH);
                Commands.Initialize();
                Lua.TryStartupScript();

                Console.WriteLine("Core: Initialized");
                Input.Start();
                Main.versionNumber = "Running on Terraria " + Main.versionNumber + " =)";

                if (UseBanlist)
                {
                    LoadBanlist();
                }

                Console.WriteLine("Core: WraithMod v{0} Ready!", VersionString);
                DedServ();
            }
            else
            {

            }
        }

        protected override void Initialize()
        {
            if (DedicatedServer)
                return;

            Lua.Initialize(SCRIPTS_PATH);
            Commands.Initialize();
            Lua.TryStartupScript();

            Console.WriteLine("Core: Initialized");
            Input.Start();
            Window.Title = "WraithMod (v" + VersionString + ")";
            Main.versionNumber = "Running on Terraria " + Main.versionNumber + " =)";

            if (UseBanlist)
            {
                LoadBanlist();
            }

            base.Initialize();

            Window.Title = "WraithMod (v" + VersionString + ")";

            MemoryStream stream = new MemoryStream();
            Assembly asm = Assembly.Load(new AssemblyName("Terraria"));
            WorldGenWrapper = asm.GetType("Terraria.WorldGen");
            MainWrapper = asm.GetType("Terraria.Main");

            Texture2D t = new Texture2D(base.GraphicsDevice, 1, 1);
            t.SetData<Color>(new Color[] { new Color(255, 255, 255, 255) });
            DefaultTexture = t;

            Console.WriteLine("Core: WraithMod v{0} Ready!", VersionString);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            SpriteBatch = new SpriteBatch(this.GraphicsDevice);
            DefaultFont = Content.Load<SpriteFont>(@"../wmContent/Fonts/Default");
            ConsoleBackground = Content.Load<Texture2D>(@"../wmContent/Images/ConsoleBackground");
            WhitePixel = Content.Load<Texture2D>(@"../wmContent/Images/White");
            Lua.TryOnLoadScript();
            Console.WriteLine("Core: Content loaded");
        }

        string lastText = "";
        bool goneServer = false;
        string[] lastIps = new string[8] { "", "", "", "", "", "", "", "" };
        protected override void Update(GameTime gameTime)
        {

            // Lighting.LightTiles(

            bool shift = keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift);
            bool alt = keyState.IsKeyDown(Keys.LeftAlt) || keyState.IsKeyDown(Keys.RightAlt);
            bool ctrl = keyState.IsKeyDown(Keys.LeftControl) || keyState.IsKeyDown(Keys.RightControl);

            bool[] lavaBuckets = new bool[40];
            bool[] waterBuckets = new bool[40];
            bool[] emptyBuckets = new bool[40];
            for (int i = 0; i < player[myPlayer].inventory.Length; i++)
            {
                if (player[myPlayer].inventory[i].type == 0xcf)
                {
                    lavaBuckets[i] = true;
                }
                else if (player[myPlayer].inventory[i].type == 0xce)
                {
                    waterBuckets[i] = true;
                }
                else if (player[myPlayer].inventory[i].type == 0xcd)
                {
                    emptyBuckets[i] = true;
                }
            }

            if (ChatLine[0].text != lastText)
            {
                Console.WriteLine("Chat: {0}" + ChatLine[0].text);
            }

            Input.Update(gameTime);

            // Core.TriggerEvent("DAMAGE_PLAYER");

            if (!Pause)
            {
                if (Console.UseConsole)
                {
                    if (menuMode == 10 || (netMode == 1 && menuMode == 14))
                    {

                    }
                    else
                    {
                        base.Update(gameTime);
                    }
                }
                else
                {
                    base.Update(gameTime);
                }
            }

            if (buildMode && itemHax)
            {
                if (gamePaused)
                    return;

                if (itemHax)
                {
                    for (int i = 0; i < player[myPlayer].inventory.Length; i++)
                    {
                        if (player[myPlayer].inventory[i].type == 0xcd)
                        {
                            if (lavaBuckets[i] == true)
                            {
                                player[myPlayer].inventory[i].type = 0xcf;
                            }
                            else if (waterBuckets[i] == true)
                            {
                                player[myPlayer].inventory[i].type = 0xce;
                            }
                            else if (emptyBuckets[i] == true)
                            {
                                player[myPlayer].inventory[i].type = 0xcd;
                            }
                        }
                    }
                }

                try
                {
                    FieldInfo tilex = player[myPlayer].GetType().GetField("tileRangeX");
                    FieldInfo tiley = player[myPlayer].GetType().GetField("tileRangeY");
                    tilex.SetValue(player[myPlayer], 1000);
                    tiley.SetValue(player[myPlayer], 1000);

                    for (int i = 0; i < player[myPlayer].inventory.Length; i++)
                    {
                        Item it = player[myPlayer].inventory[i];

                        if (i == 39)
                        {
                            player[myPlayer].inventory[i].SetDefaults(0);
                            player[myPlayer].inventory[i].name = "";
                            player[myPlayer].inventory[i].stack = 0;
                            player[myPlayer].inventory[i].UpdateItem(0);
                        }
                        else if (it.name != "Magic Mirror")
                        {
                            it.SetDefaults(it.type);
                            it.stack = 255;
                            if (itemHax)
                            {
                                it.autoReuse = true;
                                it.useTime = 0;
                            }
                            if (it.hammer > 0 || it.axe > 0)
                            {
                                it.hammer = 100;
                                it.axe = 100;
                            }
                            if (it.pick > 0)
                                it.pick = 100;
                        }
                        else
                        {
                            it.SetDefaults(50);
                        }

                        player[myPlayer].inventory[i] = it;
                    }
                }
                catch
                {

                }
            }
            else
            {
                FieldInfo tilex = player[myPlayer].GetType().GetField("tileRangeX");
                FieldInfo tiley = player[myPlayer].GetType().GetField("tileRangeY");
                tilex.SetValue(player[myPlayer], 5);
                tiley.SetValue(player[myPlayer], 4);
            }

            if (GodMode)
            {
                player[myPlayer].statLife = player[myPlayer].statLifeMax;
                player[myPlayer].breath = player[myPlayer].breathMax;
                player[myPlayer].statMana = player[myPlayer].statManaMax;
                player[myPlayer].dead = false;
            }

            if (!npcsEnabled && netMode != 2)
            {
                foreach (NPC n in npc)
                {
                    if (!n.friendly)
                    {
                        n.life = 0;
                        n.UpdateNPC(0);
                    }
                }
            }

            if (menuMode == 10) // if in-game ...
            {
                bool allowStuff = true; // Disallows most buildaria functionality in-game
                // Set to true if the user may not want certain functions to be happening
                // Detect if mouse is on a hotbar or inventory is open
                for (int i = 0; i < 11; i++)
                {
                    int x = (int)(20f + ((i * 0x38) * inventoryScale));
                    int y = (int)(20f + ((0 * 0x38) * inventoryScale));
                    int index = x;
                    if (((mouseState.X >= x) && (mouseState.X <= (x + (inventoryBackTexture.Width * inventoryScale)))) && ((mouseState.Y >= y) && (mouseState.Y <= (y + (inventoryBackTexture.Height * inventoryScale) + 2))))
                    {
                        i = 11;
                        allowStuff = false;
                        break;
                    }
                }
                if (playerInventory || !buildMode || editSign) // Inventory is open
                    allowStuff = false;
                else
                    UpdateSelection();
                
                if (hover)
                {
                    player[myPlayer].position = lastPosition;
                    float magnitude = 6f;
                    if (shift)
                    {
                        magnitude *= 4;
                    }
                    if (player[myPlayer].controlUp || player[myPlayer].controlJump)
                    {
                        player[myPlayer].position = new Vector2(player[myPlayer].position.X, player[myPlayer].position.Y - magnitude);
                    }
                    if (player[myPlayer].controlDown)
                    {
                        player[myPlayer].position = new Vector2(player[myPlayer].position.X, player[myPlayer].position.Y + magnitude);
                    }
                    if (player[myPlayer].controlLeft)
                    {
                        player[myPlayer].position = new Vector2(player[myPlayer].position.X - magnitude, player[myPlayer].position.Y);
                    }
                    if (player[myPlayer].controlRight)
                    {
                        player[myPlayer].position = new Vector2(player[myPlayer].position.X + magnitude, player[myPlayer].position.Y);
                    }
                }

                if (alt && mouseState.LeftButton == ButtonState.Released && allowStuff)
                {
                    for (int x = 0; x < SelectionSize.X; x++)
                    {
                        for (int y = 0; y < SelectionSize.Y; y++)
                        {
                            SelectedTiles[x, y] = false;
                        }
                    }
                    Vector2 center = new Vector2(SelectionSize.X / 2f, SelectionSize.Y / 2f);
                    for (int x = 0; x < SelectionSize.X; x++)
                    {
                        for (int y = 0; y < SelectionSize.Y; y++)
                        {
                            double dx = (x - center.X + 1) / center.X;
                            double dy = (y - center.Y + 1) / center.Y;
                            if (dx * dx + dy * dy < 1)
                            {
                                SelectedTiles[x, y] = true;
                            }
                        }
                    }
                }

                if (shift && mouseState.LeftButton == ButtonState.Released && allowStuff)
                {
                    bool[,] tempTiles = new bool[SelectionSize.X, SelectionSize.Y];
                    for (int x = 0; x < SelectionSize.X; x++)
                    {
                        for (int y = 0; y < SelectionSize.Y; y++)
                        {
                            tempTiles[x, y] = SelectedTiles[x, y];
                        }
                    }
                    for (int x = 0; x < SelectionSize.X; x++)
                    {
                        bool found1 = false;
                        bool found2 = false;
                        for (int y = 0; y < SelectionSize.Y; y++)
                        {
                            if (!found1)
                            {
                                found1 = SelectedTiles[x, y];
                                continue;
                            }
                            else if (!found2)
                            {
                                if (y + 1 > SelectionSize.Y - 1)
                                {
                                    found2 = SelectedTiles[x, y];
                                    break;
                                }
                                else if (!found2 && !SelectedTiles[x, y + 1])
                                {
                                    found2 = SelectedTiles[x, y];
                                    break;
                                }
                                else
                                {
                                    SelectedTiles[x, y] = false;
                                }
                            }
                            else if (found1 && found2)
                                break;
                        }
                    }
                    for (int y = 0; y < SelectionSize.Y; y++)
                    {
                        bool found1 = false;
                        bool found2 = false;
                        for (int x = 0; x < SelectionSize.X; x++)
                        {
                            if (!found1)
                            {
                                found1 = tempTiles[x, y];
                                continue;
                            }
                            else if (!found2)
                            {
                                if (x + 1 > SelectionSize.X - 1)
                                {
                                    found2 = tempTiles[x, y];
                                    break;
                                }
                                else if (!found2 && !tempTiles[x + 1, y])
                                {
                                    found2 = tempTiles[x, y];
                                    break;
                                }
                                else
                                {
                                    tempTiles[x, y] = false;
                                }
                            }
                            else if (found1 && found2)
                                break;
                        }
                    }
                    for (int x = 0; x < SelectionSize.X; x++)
                    {
                        for (int y = 0; y < SelectionSize.Y; y++)
                        {
                            SelectedTiles[x, y] = SelectedTiles[x, y] || tempTiles[x, y];
                        }
                    }
                }

                if (allowStuff)
                {


                    #region Place Anywhere

                    if (mouseState.LeftButton == ButtonState.Pressed && player[myPlayer].inventory[player[myPlayer].selectedItem].createTile >= 0 && itemHax)
                    {
                        int x = (int)((Main.mouseState.X + Main.screenPosition.X) / 16f);
                        int y = (int)((Main.mouseState.Y + Main.screenPosition.Y) / 16f);

                        if (Main.tile[x, y].active == false)
                        {
                            byte wall = Main.tile[x, y].wall;
                            Main.tile[x, y] = new Tile();
                            Main.tile[x, y].type = (byte)player[myPlayer].inventory[player[myPlayer].selectedItem].createTile;
                            Main.tile[x, y].wall = wall;
                            Main.tile[x, y].active = true;
                            TileFrame(x, y);
                            SquareWallFrame(x, y, true);
                        }
                    }
                    else if (mouseState.LeftButton == ButtonState.Pressed && player[myPlayer].inventory[player[myPlayer].selectedItem].createWall >= 0 && itemHax)
                    {
                        int x = (int)((Main.mouseState.X + Main.screenPosition.X) / 16f);
                        int y = (int)((Main.mouseState.Y + Main.screenPosition.Y) / 16f);

                        if (Main.tile[x, y].wall == 0)
                        {
                            if (Main.tile[x, y] == null)
                            {
                                Main.tile[x, y] = new Tile();
                                Main.tile[x, y].type = 0;
                            }

                            Main.tile[x, y].wall = (byte)player[myPlayer].inventory[player[myPlayer].selectedItem].createWall;
                            TileFrame(x, y);
                            SquareWallFrame(x, y, true);
                        }
                    }

                    #endregion

                    #region Selection Modifications

                    #region Copy/Paste

                    if (ctrl && keyState.IsKeyDown(Keys.C) && oldKeyState.IsKeyUp(Keys.C))
                    {
                        Copied = new Tile[SelectionSize.X, SelectionSize.Y];
                        CopiedSize = new Point(SelectionSize.X, SelectionSize.Y);
                        for (int x = 0; x < SelectionSize.X; x++)
                        {
                            for (int y = 0; y < SelectionSize.Y; y++)
                            {
                                Copied[x, y] = new Tile();
                                Copied[x, y].type = tile[x + SelectionPosition.X, y + SelectionPosition.Y].type;
                                Copied[x, y].active = tile[x + SelectionPosition.X, y + SelectionPosition.Y].active;
                                Copied[x, y].wall = tile[x + SelectionPosition.X, y + SelectionPosition.Y].wall;
                                Copied[x, y].liquid = tile[x + SelectionPosition.X, y + SelectionPosition.Y].liquid;
                                Copied[x, y].lava = tile[x + SelectionPosition.X, y + SelectionPosition.Y].lava;
                            }
                        }
                    }

                    if (ctrl && keyState.IsKeyDown(Keys.V) && oldKeyState.IsKeyUp(Keys.V))
                    {
                        if (sel1 != -Vector2.One && sel2 != -Vector2.One)
                        {
                            Undo = new Tile[CopiedSize.X, CopiedSize.Y];
                            UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                            UndoSize = new Point(CopiedSize.X, CopiedSize.Y);
                            for (int x = 0; x < CopiedSize.X; x++)
                            {
                                for (int y = 0; y < CopiedSize.Y; y++)
                                {
                                    try
                                    {
                                        if (Main.tile[x, y] == null)
                                        {
                                            Main.tile[x, y] = new Tile();
                                            Undo[x, y] = null;
                                        }
                                        else
                                        {
                                            Undo[x, y] = new Tile();
                                            Undo[x, y].type = Main.tile[x, y].type;
                                            Undo[x, y].liquid = Main.tile[x, y].liquid;
                                            Undo[x, y].lava = Main.tile[x, y].lava;
                                            Undo[x, y].wall = Main.tile[x, y].wall;
                                            Undo[x, y].active = Main.tile[x, y].active;
                                        }

                                        tile[(int)sel1.X + x, (int)sel1.Y + y] = new Tile();
                                        tile[(int)sel1.X + x, (int)sel1.Y + y].type = Copied[x, y].type;
                                        tile[(int)sel1.X + x, (int)sel1.Y + y].active = Copied[x, y].active;
                                        tile[(int)sel1.X + x, (int)sel1.Y + y].wall = Copied[x, y].wall;
                                        tile[(int)sel1.X + x, (int)sel1.Y + y].liquid = Copied[x, y].liquid;
                                        tile[(int)sel1.X + x, (int)sel1.Y + y].lava = Copied[x, y].lava;
                                        TileFrame((int)sel1.X + x, (int)sel1.Y + y);
                                        SquareWallFrame((int)sel1.X + x, (int)sel1.Y + y);
                                    }
                                    catch
                                    {

                                    }
                                }
                            }
                        }
                    }

                    #endregion

                    #region Erasers

                    else if (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].pick >= 55)
                    {
                        Undo = new Tile[SelectionSize.X, SelectionSize.Y];
                        UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                        UndoSize = new Point(SelectionSize.X, SelectionSize.Y);
                        for (int xp = 0; xp < SelectionSize.X; xp++)
                        {
                            for (int yp = 0; yp < SelectionSize.Y; yp++)
                            {
                                int x = xp + SelectionPosition.X;
                                int y = yp + SelectionPosition.Y;
                                if (SelectedTiles[xp, yp])
                                {
                                    if (Main.tile[x, y] == null)
                                    {
                                        Main.tile[x, y] = new Tile();
                                        Undo[xp, yp] = null;
                                    }
                                    else
                                    {
                                        Undo[xp, yp] = new Tile();
                                        Undo[xp, yp].type = Main.tile[x, y].type;
                                        Undo[xp, yp].liquid = Main.tile[x, y].liquid;
                                        Undo[xp, yp].lava = Main.tile[x, y].lava;
                                        Undo[xp, yp].wall = Main.tile[x, y].wall;
                                        Undo[xp, yp].active = Main.tile[x, y].active;
                                    }

                                    byte wall = Main.tile[x, y].wall;
                                    Main.tile[x, y].type = 0;
                                    Main.tile[x, y].active = false;
                                    Main.tile[x, y].wall = wall;
                                    TileFrame(x, y);
                                    TileFrame(x, y - 1);
                                    TileFrame(x, y + 1);
                                    TileFrame(x - 1, y);
                                    TileFrame(x + 1, y);
                                    SquareWallFrame(x, y, true);
                                }
                            }
                        }
                    }
                    else if (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].hammer >= 55)
                    {
                        Undo = new Tile[SelectionSize.X, SelectionSize.Y];
                        UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                        UndoSize = new Point(SelectionSize.X, SelectionSize.Y);
                        for (int xp = 0; xp < SelectionSize.X; xp++)
                        {
                            for (int yp = 0; yp < SelectionSize.Y; yp++)
                            {
                                int x = xp + SelectionPosition.X;
                                int y = yp + SelectionPosition.Y;
                                if (SelectedTiles[xp, yp])
                                {
                                    if (Main.tile[x, y] == null)
                                    {
                                        Main.tile[x, y] = new Tile();
                                        Undo[xp, yp] = null;
                                    }
                                    else
                                    {
                                        Undo[xp, yp] = new Tile();
                                        Undo[xp, yp].type = Main.tile[x, y].type;
                                        Undo[xp, yp].liquid = Main.tile[x, y].liquid;
                                        Undo[xp, yp].lava = Main.tile[x, y].lava;
                                        Undo[xp, yp].wall = Main.tile[x, y].wall;
                                        Undo[xp, yp].active = Main.tile[x, y].active;
                                    }

                                    Main.tile[x, y].wall = 0;
                                    TileFrame(x, y);
                                    TileFrame(x, y - 1);
                                    TileFrame(x, y + 1);
                                    TileFrame(x - 1, y);
                                    TileFrame(x + 1, y);
                                    SquareWallFrame(x, y, true);
                                }
                            }
                        }
                    }

                    #endregion

                    #region Liquid (Fill/Erase)

                    else if (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].type == 0xcf)
                    {
                        Undo = new Tile[SelectionSize.X, SelectionSize.Y];
                        UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                        UndoSize = new Point(SelectionSize.X, SelectionSize.Y);
                        for (int xp = 0; xp < SelectionSize.X; xp++)
                        {
                            for (int yp = 0; yp < SelectionSize.Y; yp++)
                            {
                                int x = xp + SelectionPosition.X;
                                int y = yp + SelectionPosition.Y;
                                if (SelectedTiles[xp, yp])
                                {
                                    if (Main.tile[x, y] == null)
                                    {
                                        Main.tile[x, y] = new Tile();
                                        Undo[xp, yp] = null;
                                    }
                                    else
                                    {
                                        Undo[xp, yp] = new Tile();
                                        Undo[xp, yp].type = Main.tile[x, y].type;
                                        Undo[xp, yp].liquid = Main.tile[x, y].liquid;
                                        Undo[xp, yp].lava = Main.tile[x, y].lava;
                                        Undo[xp, yp].wall = Main.tile[x, y].wall;
                                        Undo[xp, yp].active = Main.tile[x, y].active;
                                    }

                                    Main.tile[x, y].liquid = 255;
                                    Main.tile[x, y].lava = true;
                                    TileFrame(x, y);
                                    SquareWallFrame(x, y, true);
                                }
                            }
                        }
                    }
                    else if (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].type == 0xce)
                    {
                        Undo = new Tile[SelectionSize.X, SelectionSize.Y];
                        UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                        UndoSize = new Point(SelectionSize.X, SelectionSize.Y);
                        for (int xp = 0; xp < SelectionSize.X; xp++)
                        {
                            for (int yp = 0; yp < SelectionSize.Y; yp++)
                            {
                                int x = xp + SelectionPosition.X;
                                int y = yp + SelectionPosition.Y;
                                if (SelectedTiles[xp, yp])
                                {
                                    if (Main.tile[x, y] == null)
                                    {
                                        Main.tile[x, y] = new Tile();
                                        Undo[xp, yp] = null;
                                    }
                                    else
                                    {
                                        Undo[xp, yp] = new Tile();
                                        Undo[xp, yp].type = Main.tile[x, y].type;
                                        Undo[xp, yp].liquid = Main.tile[x, y].liquid;
                                        Undo[xp, yp].lava = Main.tile[x, y].lava;
                                        Undo[xp, yp].wall = Main.tile[x, y].wall;
                                        Undo[xp, yp].active = Main.tile[x, y].active;
                                    }

                                    Main.tile[x, y].liquid = 255;
                                    Main.tile[x, y].lava = false;
                                    TileFrame(x, y);
                                    SquareWallFrame(x, y, true);
                                }
                            }
                        }
                    }
                    else if (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].type == 0xcd)
                    {
                        Undo = new Tile[SelectionSize.X, SelectionSize.Y];
                        UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                        UndoSize = new Point(SelectionSize.X, SelectionSize.Y);
                        for (int xp = 0; xp < SelectionSize.X; xp++)
                        {
                            for (int yp = 0; yp < SelectionSize.Y; yp++)
                            {
                                int x = xp + SelectionPosition.X;
                                int y = yp + SelectionPosition.Y;
                                if (SelectedTiles[xp, yp])
                                {
                                    if (Main.tile[x, y] == null)
                                    {
                                        Main.tile[x, y] = new Tile();
                                        Undo[xp, yp] = null;
                                    }
                                    else
                                    {
                                        Undo[xp, yp] = new Tile();
                                        Undo[xp, yp].type = Main.tile[x, y].type;
                                        Undo[xp, yp].liquid = Main.tile[x, y].liquid;
                                        Undo[xp, yp].lava = Main.tile[x, y].lava;
                                        Undo[xp, yp].wall = Main.tile[x, y].wall;
                                        Undo[xp, yp].active = Main.tile[x, y].active;
                                    }

                                    Main.tile[x, y].liquid = 0;
                                    Main.tile[x, y].lava = false;
                                    TileFrame(x, y);
                                    SquareWallFrame(x, y, true);
                                }
                            }
                        }
                    }

                    #endregion

                    #region Fills

                    if (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].createTile >= 0)
                    {
                        Undo = new Tile[SelectionSize.X, SelectionSize.Y];
                        UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                        UndoSize = new Point(SelectionSize.X, SelectionSize.Y);
                        for (int xp = 0; xp < SelectionSize.X; xp++)
                        {
                            for (int yp = 0; yp < SelectionSize.Y; yp++)
                            {
                                int x = xp + SelectionPosition.X;
                                int y = yp + SelectionPosition.Y;
                                if (SelectedTiles[xp, yp])
                                {
                                    if (Main.tile[x, y] == null)
                                    {
                                        Undo[xp, yp] = null;
                                    }
                                    else
                                    {
                                        Undo[xp, yp] = new Tile();
                                        Undo[xp, yp].type = Main.tile[x, y].type;
                                        Undo[xp, yp].liquid = Main.tile[x, y].liquid;
                                        Undo[xp, yp].lava = Main.tile[x, y].lava;
                                        Undo[xp, yp].wall = Main.tile[x, y].wall;
                                        Undo[xp, yp].active = Main.tile[x, y].active;
                                    }

                                    byte wall = Main.tile[x, y].wall;
                                    Main.tile[x, y] = new Tile();
                                    Main.tile[x, y].type = (byte)player[myPlayer].inventory[player[myPlayer].selectedItem].createTile;
                                    Main.tile[x, y].wall = wall;
                                    Main.tile[x, y].active = true;
                                    TileFrame(x, y);
                                    SquareWallFrame(x, y, true);
                                }
                            }
                        }
                    }
                    else if (mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].createWall >= 0)
                    {
                        Undo = new Tile[SelectionSize.X, SelectionSize.Y];
                        UndoPosition = new Point((int)sel1.X, (int)sel1.Y);
                        UndoSize = new Point(SelectionSize.X, SelectionSize.Y);
                        for (int xp = 0; xp < SelectionSize.X; xp++)
                        {
                            for (int yp = 0; yp < SelectionSize.Y; yp++)
                            {
                                int x = xp + SelectionPosition.X;
                                int y = yp + SelectionPosition.Y;

                                if (Main.tile[x, y] == null)
                                {
                                    Undo[xp, yp] = null;
                                }
                                else
                                {
                                    Undo[xp, yp] = new Tile();
                                    Undo[xp, yp].type = Main.tile[x, y].type;
                                    Undo[xp, yp].liquid = Main.tile[x, y].liquid;
                                    Undo[xp, yp].lava = Main.tile[x, y].lava;
                                    Undo[xp, yp].wall = Main.tile[x, y].wall;
                                    Undo[xp, yp].active = Main.tile[x, y].active;
                                }

                                if (SelectedTiles[xp, yp])
                                {
                                    if (Main.tile[x, y] == null)
                                    {
                                        Main.tile[x, y] = new Tile();
                                        Main.tile[x, y].type = 0;
                                    }

                                    Main.tile[x, y].wall = (byte)player[myPlayer].inventory[player[myPlayer].selectedItem].createWall;
                                    TileFrame(x, y);
                                    SquareWallFrame(x, y, true);
                                }
                            }
                        }
                    }

                    #endregion

                    #region Undo

                    if (ctrl && keyState.IsKeyDown(Keys.Z) && oldKeyState.IsKeyUp(Keys.Z))
                    {
                        for (int xp = 0; xp < UndoSize.X; xp++)
                        {
                            for (int yp = 0; yp < UndoSize.Y; yp++)
                            {
                                int x = xp + UndoPosition.X;
                                int y = yp + UndoPosition.Y;

                                if (Undo[xp, yp] == null)
                                    tile[x, y] = null;
                                else
                                {
                                    tile[x, y] = new Tile();
                                    tile[x, y].type = Undo[xp, yp].type;
                                    tile[x, y].active = Undo[xp, yp].active;
                                    tile[x, y].wall = Undo[xp, yp].wall;
                                    tile[x, y].liquid = Undo[xp, yp].liquid;
                                    tile[x, y].lava = Undo[xp, yp].lava;
                                    TileFrame(x, y);
                                    SquareWallFrame(x, y);
                                }
                            }
                        }
                    }

                    #endregion

                    #endregion
                }
            }

            if (UseBanlist && NetMode == 2)
            {
                for (int i = 0; i < maxPlayers; i++)
                {
                    Player p = Core.GetPlayers()[i];
                    if (p.name == null || p.name == "")
                    {
                        continue;
                    }
                    try
                    {
                        string ip = Netplay.serverSock[p.whoAmi].statusText.Split(':')[0].Substring(1);
                        if (ip != lastIps[i])
                        {
                            Console.WriteLine(MessageType.Special, "Core: Player \"{0}\" connected with IP {1}", p.name, ip);
                        }
                        foreach (BannedPlayer bp in Banlist)
                        {
                            if (ip.Trim().ToLower() == bp.IP.Trim().ToLower())
                            {
                                Core.Kick(i, "Banned. Reason: " + bp.Reason);
                            }
                        }
                        lastIps[i] = ip;
                    }
                    catch
                    {

                    }
                }
            }

            string chatText = Core.GetPlayer().chatText;
            if (chatText != "" && chatText != null && chatText.Length > 0)
            {
                if (chatText[0] == Input.CommandSymbol)
                {
                    Commands.RunCommand(chatText);
                    Core.GetPlayer().chatText = "";
                }
            }

            //string s = "";
            /*messageBuffer mb = NetMessage.buffer[0];
            mb.messageLength = BitConverter.ToInt32(mb.readBuffer, 0) + 4;
            if (mb.messageLength > 4)
            {
                int msgType = mb.readBuffer[4];
                Console.WriteLine("Net: Packet Peek: \n  Type: {0}\n", msgType);
            }*/

            if (!goneServer && netMode == 2)
            {
                WraithMod.logoTexture = new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice, 1, 1);
                goneServer = true;
            }

            if (buildMode)
            {
                Core.GetInventory()[39].SetDefaults(0);
                Core.GetInventory()[39].name = "";
                Core.GetInventory()[39].stack = 0;
                Core.GetInventory()[39].UpdateItem(0);
            }

            oldMenuMode = menuMode;
            lastText = ChatLine[0].text;
            lastPosition = player[myPlayer].position;
            oldMouseState = mouseState;
            oldStatusText = statusText;
            oldKeyState = keyState;
        }

        protected override void Draw(GameTime gameTime)
        {
            try
            {
                base.Draw(gameTime);
            }
            catch
            {
                Console.WriteLine(MessageType.Fatal, "Core: base.Draw() failed (unknown error)");
            }
            SpriteBatch.Begin();
            if (buildMode)
                DrawSelectionOverlay();
            if (showSplash)
            {
                SpriteBatch.DrawString(fontDeathText, "WraithMod v" + VersionString + "\nTab for Console, \"help\" for Help", new Vector2(10, 400), new Color(255, 100, 0), 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            }
            for (int i = 0; i < Overlay.Count; i++)
            {
                SpriteBatch.DrawString(DefaultFont, Overlay[i], OverlayPosition[i] + new Vector2(1, 1), Color.Black);
                SpriteBatch.DrawString(DefaultFont, Overlay[i], OverlayPosition[i] + new Vector2(-1, 1), Color.Black);
                SpriteBatch.DrawString(DefaultFont, Overlay[i], OverlayPosition[i] + new Vector2(1, -1), Color.Black);
                SpriteBatch.DrawString(DefaultFont, Overlay[i], OverlayPosition[i] + new Vector2(-1, -1), Color.Black);
                SpriteBatch.DrawString(DefaultFont, Overlay[i], OverlayPosition[i], OverlayColor[i]);
            }
            Console.Draw(gameTime);
            SpriteBatch.End();
        }

        public void DrawSelectionOverlay()
        {
            // TODO: Properly cull the tiles so I'm not killing people trying to select massive areas
            // BROKEN: This code offsets the selection position as you move it off the screen to left - i.e, moving right
            
            if ((sel1 == -Vector2.One && sel2 == -Vector2.One) || (sel1 == Vector2.Zero && sel2 == Vector2.Zero && SelectionSize.X == 0 && SelectionSize.Y == 0))
                return;

            Vector2 offset = new Vector2(((int)(screenPosition.X)), ((int)(screenPosition.Y)));
            int minx = (int)Math.Max(SelectionPosition.X * 16, (SelectionPosition.X * 16) - ((int)(screenPosition.X / 16)) * 16);
            int diffx = (int)(SelectionPosition.X * 16) - minx;
            int maxx = minx + (int)Math.Max(SelectionSize.X * 16, screenWidth) + diffx;
            int miny = (int)Math.Max(SelectionPosition.Y * 16, screenPosition.Y);
            int diffy = (int)(SelectionPosition.Y * 16) - miny;
            int maxy = miny + (int)Math.Min(SelectionSize.Y * 16, screenHeight) + diffy;
            for (int x = minx; x < maxx; x += (int)16)
            {
                for (int y = miny; y < maxy; y += (int)16)
                {
                    int tx = (int)((x - minx) / 16);
                    int ty = (int)((y - miny) / 16);
                    if (ty >= SelectionSize.Y)
                        continue;
                    if (tx >= SelectionSize.X)
                        break;
                    if (SelectedTiles[tx, ty])
                    {
                        Vector2 cull = (new Vector2(tx + (minx / 16), ty + (miny / 16)) * new Vector2(16)) - offset;
                        SpriteBatch.Draw(DefaultTexture, cull, null, SelectionOverlay, 0, Vector2.Zero, new Vector2(16), SpriteEffects.None, 0);
                    }
                }
            }
        }

        public void UpdateSelection()
        {
            // Button clicked, set first selection point 
            if (mouseState.LeftButton == ButtonState.Pressed && oldMouseState.LeftButton == ButtonState.Released && player[myPlayer].inventory[player[myPlayer].selectedItem].name.ToLower().Contains("phaseblade"))
            {
                int x = (int)((Main.mouseState.X + Main.screenPosition.X) / 16f);
                int y = (int)((Main.mouseState.Y + Main.screenPosition.Y) / 16f);
                sel1 = new Vector2(x, y);
            }

            // Button is being held down, set second point and make sure the selection points are in the right order
            if (mouseState.LeftButton == ButtonState.Pressed && player[myPlayer].inventory[player[myPlayer].selectedItem].name.ToLower().Contains("phaseblade"))
            {
                int x = (int)((Main.mouseState.X + Main.screenPosition.X) / 16f);
                int y = (int)((Main.mouseState.Y + Main.screenPosition.Y) / 16f);
                sel2 = new Vector2(x, y) + Vector2.One;
                if (keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift))
                {
                    Vector2 size = sel1 - sel2;
                    if (Math.Abs(size.X) != Math.Abs(size.Y))
                    {
                        float min = Math.Min(Math.Abs(size.X), Math.Abs(size.Y));
                        if (sel2.X > sel1.X)
                        {
                            sel2 = new Vector2(sel1.X + min, sel2.Y);
                        }
                        else
                        {
                            sel2 = new Vector2(sel1.X - min, sel2.Y);
                        }
                        if (sel2.Y > sel1.Y)
                        {
                            sel2 = new Vector2(sel2.X, sel1.Y + min);
                        }
                        else
                        {
                            sel2 = new Vector2(sel2.X, sel1.Y - min);
                        }
                    }
                }
            }

            // Clear selection
            if (mouseState.RightButton == ButtonState.Pressed && player[myPlayer].inventory[player[myPlayer].selectedItem].name.ToLower().Contains("phaseblade"))
            {
                sel1 = -Vector2.One;
                sel2 = -Vector2.One;
            }

            // Check inside the selection and set SelectedTiles accordingly
            int minx = (int)Math.Min(sel1.X, sel2.X);
            int maxx = (int)Math.Max(sel1.X, sel2.X);
            int miny = (int)Math.Min(sel1.Y, sel2.Y);
            int maxy = (int)Math.Max(sel1.Y, sel2.Y);
            SelectedTiles = new bool[maxx - minx, maxy - miny];
            SelectionSize = new Point(maxx - minx, maxy - miny);
            SelectionPosition = new Point(minx, miny);
            for (int x = 0; x < SelectionSize.X; x++)
            {
                for (int y = 0; y < SelectionSize.Y; y++)
                {
                    SelectedTiles[x, y] = true;
                }
            }
        }

        public float inventoryScale
        {
            // Accesses Main's private field "inventoryScale" for checking if the mouse is in the hotbar
            get
            {
                object o = MainWrapper.GetField("inventoryScale", BindingFlags.Static | BindingFlags.NonPublic).GetValue(this);
                return (float)o;
            }
        }

        public void TileFrame(int x, int y, bool reset = false, bool breaks = true)
        {
            // Accesses the WorldGen's TileFrame() method for keeping tiles looking presentable when placed with hax
            WorldGenWrapper.GetMethod("TileFrame").Invoke(null, new object[] { x, y, reset, !breaks });
        }

        public void SquareWallFrame(int x, int y, bool reset = false)
        {
            // It's the above, but for walls
            WorldGenWrapper.GetMethod("SquareWallFrame").Invoke(null, new object[] { x, y, reset });
        }

        public static void LoadBanlist()
        {
            Banlist.Clear();
            if (!File.Exists(BANLIST_FILE))
                SaveBanlist();
            string[] banStrings = LoadList(BANLIST_FILE);
            foreach (string s in banStrings)
            {
                string[] data = s.Split(',');
                if (data.Length < 1) continue;
                BannedPlayer bp = new BannedPlayer();
                bp.IP = data[0];
                if (data.Length > 1)
                    bp.Name = data[1];
                if (data.Length > 2)
                {
                    string compiledReason = "";
                    for (int i = 2; i < data.Length; i++)
                    {
                        compiledReason += data[i];
                        if (i != data.Length - 1)
                            compiledReason += ",";
                    }
                    bp.Reason = compiledReason;
                }
                Banlist.Add(bp);
                Console.WriteLine(MessageType.Special, "Banlist: Loaded banned player: \n  IP: {0}\n  Name: {1}\n  Reason: {2}", bp.IP, bp.Name, bp.Reason);
            }
        }

        public static void SaveBanlist()
        {
            string toSave = "";
            if (File.Exists(BANLIST_FILE))
                File.Delete(BANLIST_FILE);
            foreach (BannedPlayer bp in Banlist)
            {
                toSave += bp.IP + "," + bp.Name + "," + bp.Reason + "\n";
            }
            FileStream fs = new FileStream(BANLIST_FILE, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(toSave);
            sw.Close();
            Console.WriteLine("Core: Saved banlist");
        }

        public static string[] LoadList(string path)
        {
            List<string> list = new List<string>();
            if (!File.Exists(path))
            {
                Console.WriteLine(MessageType.Error, "Core: LoadList() failed (file {0} not found)");
                return new string[] { };
            }
            StreamReader sr = new StreamReader(path);
            string str = sr.ReadToEnd();
            sr.Close();
            string[] readStr = str.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in readStr)
            {
                list.Add(s.Trim()); 
            }
            Console.WriteLine("Core: LoadList() loaded {0}", path);
            return list.ToArray();
        }

        void WraithMod_Exiting(object sender, EventArgs e)
        {
            SaveBanlist();
            Console.Writer.Close();
            Environment.Exit(0);
        }

        void Input_InputGiven(object sender, ConsoleInputEventArgs e)
        {
            string Command = e.Message;
            if (Command.Length < 1) return;
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
                WraithMod.Lua.Initialize();
                WraithMod.Commands.Initialize();
                WraithMod.LoadBanlist();
            }
            else if (Command[0] == Input.CommandSymbol)
            {
                try
                {
                    WraithMod.Commands.RunCommand(Command);
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
            /*if (Input.CommandMatch("help", e.Message, false, false, false, false))
            {
                Console.WriteLine("Input: Core help: (/[command] or [lua])\n" +
                    "  reload - Reloads scripts and commands\n" +
                    "  /help - Commands help\n" +
                    "  help() - LUA help");
            }
            else if (Input.CommandMatch("reload", e.Message))
            {
                Lua.Initialize();
                Commands.Initialize();
                LoadBanlist();
            }
            else if (e.Message[0] == Input.CommandSymbol)
            {
                try
                {
                    Commands.RunCommand(e.Message);
                }
                catch
                {
                    Console.WriteLine(MessageType.Error, "Input: Unknown error running command: {0}", e.Message);
                }
            }
            else if (e.Message.Trim() != "")
            {
                ParameterizedThreadStart pts = new ParameterizedThreadStart(Lua.RunString);
                Thread t = new Thread(pts);
                t.Start(e.Message);
            }*/
        }

        // Terraria.Main Properties for API
        public int Background { get { return background; } set { background = value;} }
        public float CottomWorld { get { return bottomWorld; } set { bottomWorld = value;} }
        public bool BloodMoon { get { return bloodMoon; } set { bloodMoon = value; } }
        public float CaveParrallax { get { return caveParrallax; } set { caveParrallax = value; } }
        public int ChatLength { get { return chatLength; } set { chatLength = value; } }
        public bool ChatMode { get { return chatMode; } set { chatMode = value; } }
        public bool ChatRelease { get { return chatRelease; } set { chatRelease = value; } }
        public string ChatText { get { return chatText; } set { chatText = value; } }
        public int CheckForSpawns { get { return checkForSpawns; } set { checkForSpawns = value; } }
        public Chest[] Chest { get { return chest; } set { chest = value; } }
        public ChatLine[] ChatLine { get { return chatLine; } set { chatLine = value; } }
        public Player ClientPlayer { get { return clientPlayer; } set { clientPlayer = value; } }
        public string CDown { get { return cDown; } set { cDown = value; } }
        public string CInv { get { return cInv; } set { cInv = value; } }
        public string CJump  { get { return cJump ; } set { cJump  = value; } }
        public string CLeft { get { return cLeft; } set { cLeft = value; } }
        public Cloud[] Cloud { get { return cloud; } set { cloud = value; } }
        public int CloudLimit { get { return cloudLimit; } set { cloudLimit = value; } }
        public CombatText[] CombatText { get { return combatText; } set { combatText = value; } }
        public string CRight { get { return cRight; } set { cRight = value; } }
        public string CThrowItem { get { return cThrowItem; } set { cThrowItem = value; } }
        public string CUp { get { return cUp; } set { cUp = value; } }
        public int CurMusic { get { return curMusic; } set { curMusic = value; } }
        public int CurRelease { get { return curRelease; } set { curRelease = value; } }
        public float CursorAlpha { get { return cursorAlpha; } set { cursorAlpha = value; } }
        new public Color CursorColor { get { return Main.cursorColor; } set { Main.cursorColor = value; } }
        public int CursorColorDirection { get { return cursorColorDirection; } set { cursorColorDirection = value; } }
        public float CursorScale { get { return cursorScale; } set { cursorScale = value; } }
        public bool DayTime { get { return dayTime; } set { dayTime = value; } }
        //public bool DebugMode { get { return debugMode; } set { debugMode = value; } }
        public int DrawTime { get { return drawTime; } set { drawTime = value; } }
        //public bool DumbAI { get { return dumbAI; } set { dumbAI = value; } }
        public int DungeonTiles { get { return dungeonTiles; } set { dungeonTiles = value; } }
        public int DungeonX { get { return dungeonX; } set { dungeonX = value; } }
        public int DungeonY { get { return dungeonY; } set { dungeonY = value; } }
        public Dust[] Dust { get { return dust; } set { dust = value; } }
        public bool EditSign { get { return editSign; } set { editSign = value; } }
        public int EvilTiles { get { return evilTiles; } set { evilTiles = value; } }
        public int FadeCounter { get { return fadeCounter; } set { fadeCounter = value; } }
        public bool FixedTiming { get { return fixedTiming; } set { fixedTiming = value; } }
        public int FocusRecipe { get { return focusRecipe; } set { focusRecipe = value; } }
        public int FrameRate { get { return frameRate; } set { frameRate = value; } }
        public bool FrameRelease { get { return frameRelease; } set { frameRelease = value; } }
        public bool GameMenu { get { return gameMenu; } set { gameMenu = value; } }
        public bool GodMode { get; set; }
        public string GetIP { get { return getIP; } set { getIP = value; } }
        public bool GrabSky { get { return grabSky; } set { grabSky = value; } }
        public bool HasFocus { get { return hasFocus; } set { hasFocus = value; } }
        public int HelpText { get { return helpText; } set { helpText = value; } }
        public bool HideUI { get { return hideUI; } set { hideUI = value; } }
        public float[] HotbarScale { get { return hotbarScale; } set { hotbarScale = value; } }
        public bool IgnoreErrors { get { return ignoreErrors; } set { ignoreErrors = value; } }
        public bool InputTextEnter { get { return inputTextEnter; } set { inputTextEnter = value; } }
        public int InvasionDelay { get { return invasionDelay; } set { invasionDelay = value; } }
        public int InvasionSize { get { return invasionSize; } set { invasionSize = value; } }
        public int InvasionType { get { return invasionType; } set { invasionType = value; } }
        public int InvasionWarn { get { return invasionWarn; } set { invasionWarn = value; } }
        public double InvasionX { get { return invasionX; } set { invasionX = value; } }
        public Item[] Item { get { return item; } set { item = value; } }
        public int JungleTiles { get { return jungleTiles; } set { jungleTiles = value; } }
        public Microsoft.Xna.Framework.Input.KeyboardState KeyState { get { return keyState; } set { keyState = value; } }
        public int LastItemUpdate { get { return lastItemUpdate; } set { lastItemUpdate = value; } }
        public int LastNPCUpdate { get { return lastNPCUpdate; } set { lastNPCUpdate = value; } }
        public float LeftWorld { get { return leftWorld; } set { leftWorld = value; } }
        public Liquid[] Liquid { get { return liquid; } set { liquid = value; } }
        public LiquidBuffer[] LiquidBuffer { get { return liquidBuffer; } set { liquidBuffer = value; } }
        public Player[] LoadPlayer { get { return loadPlayer; } set { loadPlayer = value; } }
        public string[] LoadPlayerPath { get { return loadPlayerPath; } set { loadPlayerPath = value; } }
        public string[] LoadWorld { get { return loadWorld; } set { loadWorld = value; } }
        public string[] LoadWorldPath { get { return loadWorldPath; } set { loadWorldPath = value; } }
        public int MaxTilesX { get { return maxTilesX; } set { maxTilesX = value; } }
        public int MaxTilesY { get { return maxTilesY; } set { maxTilesY = value; } }
        public Microsoft.Xna.Framework.Graphics.Texture2D LogoTexture { get { return logoTexture; } set { logoTexture = value; } }
        public int MagmaBGFrame { get { return magmaBGFrame; } set { magmaBGFrame = value; } }
        public int MagmaBGFrameCounter { get { return magmaBGFrameCounter; } set { magmaBGFrameCounter = value; } }
        public Microsoft.Xna.Framework.Graphics.Texture2D ManaTexture { get { return manaTexture; } set { manaTexture = value; } }
        public int MenuMode { get { return menuMode; } set { menuMode = value; } }
        public bool MenuMultiplayer { get { return menuMultiplayer; } set { menuMultiplayer = value; } }
        public int MeteorTiles { get { return meteorTiles; } set { meteorTiles = value; } }
        public short MoonModY { get { return moonModY; } set { moonModY = value; } }
        public int MoonPhase { get { return moonPhase; } set { moonPhase = value; } }
        public Color MouseColor { get { return mouseColor; } set { mouseColor = value; } }
        public Item MouseItem { get { return mouseItem; } set { mouseItem = value; } }
        public bool MouseLeftRelease { get { return mouseLeftRelease; } set { mouseLeftRelease = value; } }
        public bool MouseRightRelease { get { return mouseRightRelease; } set { mouseRightRelease = value; } }
        public Microsoft.Xna.Framework.Input.MouseState MouseState { get { return mouseState; } set { mouseState = value; } }
        public byte MouseTextColor { get { return mouseTextColor; } set { mouseTextColor = value; } }
        public int MouseTextColorChange { get { return mouseTextColorChange; } set { mouseTextColorChange = value; } }
        public float MusicVolume { get { return musicVolume; } set { musicVolume = value; } }
        public int MyPlayer { get { return myPlayer; } set { myPlayer = value; } }
        public int NetMode { get { return netMode; } set { netMode = value; } }
        public int NetPlayCounter { get { return netPlayCounter; } set { netPlayCounter = value; } }
        public int NewMusic { get { return newMusic; } set { newMusic = value; } }
        public string NewWorldName { get { return newWorldName; } set { newWorldName = value; } }
        public NPC[] Npc { get { return npc; } set { npc = value; } }
        public bool NpcChatFocus1 { get { return npcChatFocus1; } set { npcChatFocus1 = value; } }
        public bool NpcChatFocus2 { get { return npcChatFocus2; } set { npcChatFocus2 = value; } }
        public bool NpcChatRelease { get { return npcChatRelease; } set { npcChatRelease = value; } }
        public string NpcChatText { get { return npcChatText; } set { npcChatText = value; } }
        public int NpcShop { get { return npcShop; } set { npcShop = value; } }
        public int NumAvailableRecipes { get { return numAvailableRecipes; } set { numAvailableRecipes = value; } }
        public int NumChatLines { get { return numChatLines; } set { numChatLines = value; } }
        public int NumStars { get { return numStars; } set { numStars = value; } }
        public Player[] Player { get { return player; } set { player = value; } }
        public bool PlayerInventory { get { return playerInventory; } set { playerInventory = value; } }
        new public string PlayerPath { get { return Main.PlayerPath; } set { Main.PlayerPath = value; } }
        public string PlayerPathName { get { return playerPathName; } set { playerPathName = value; } }
        public Random Rand { get { return rand; } set { rand = value; } }
        public Recipe[] Recipe { get { return recipe; } set { recipe = value; } }
        public bool ReleaseUI { get { return releaseUI; } set { releaseUI = value; } }
        public bool ResetClouds { get { return resetClouds; } set { resetClouds = value; } }
        public float RightWorld { get { return rightWorld; } set { rightWorld = value; } }
        public double RockLayer { get { return rockLayer; } set { rockLayer = value; } }
        new public string SavePath { get { return Main.SavePath; } set { Main.SavePath = value; } }
        public int SaveTimer { get { return saveTimer; } set { saveTimer = value; } }
        public int ScreenHeight { get { return screenHeight; } set { screenHeight = value; } }
        public Vector2 ScreenLastPosition { get { return screenLastPosition; } set { screenLastPosition = value; } }
        public Vector2 ScreenPosition { get { return screenPosition; } set { screenPosition = value; } }
        public int ScreenWidth { get { return screenWidth; } set { screenWidth = value; } }
        public Chest[] Shop { get { return shop; } set { shop = value; } }
        public bool ShowFrameRate { get { return showFrameRate; } set { showFrameRate = value; } }
        public bool ShowItemOwner { get { return showItemOwner; } set { showItemOwner = value; } }
        public bool ShowSpam { get { return showSpam; } set { showSpam = value; } }
        public bool ShowSplash { get { return showSplash; } set { showSplash = value; } }
        public Sign[] Sign { get { return sign; } set { sign = value; } }
        public bool SignBubble { get { return signBubble; } set { signBubble = value; } }
        public string SignText { get { return signText; } set { signText = value; } }
        public int SignX { get { return signX; } set { signX = value; } }
        public int SignY { get { return signY; } set { signY = value; } }
        public bool SkipMenu { get { return skipMenu; } set { skipMenu = value; } }
        public float SoundVolume { get { return soundVolume; } set { soundVolume = value; } }
        public int SpawnTileX { get { return spawnTileX; } set { spawnTileX = value; } }
        public int SpawnTileY { get { return spawnTileY; } set { spawnTileY = value; } }
        public int StackCounter { get { return stackCounter; } set { stackCounter = value; } }
        public int StackDelay { get { return stackDelay; } set { stackDelay = value; } }
        public int StackSplit { get { return stackSplit; } set { stackSplit = value; } }
        public Star[] Star { get { return star; } set { star = value; } }
        public string StatusText { get { return statusText; } set { statusText = value; } }
        public bool StopTimeOuts { get { return stopTimeOuts; } set { stopTimeOuts = value; } }
        public short SunModY { get { return sunModY; } set { sunModY = value; } }
        public Color[] TeamColor { get { return teamColor; } set { teamColor = value; } }
        public Tile[,] Tile { get { return tile; } set { tile = value; } }
        public bool[] TileBlockLight { get { return tileBlockLight; } set { tileBlockLight = value; } }
        public Color TileColor { get { return tileColor; } set { tileColor = value; } }
        public bool[] TileDungeon { get { return tileDungeon; } set { tileDungeon = value; } }
        public bool[] TileFrameImportant { get { return tileFrameImportant; } set { tileFrameImportant = value; } }
        public bool[] TileLavaDeath { get { return tileLavaDeath; } set { tileLavaDeath = value; } }
        public bool[] TileNoAttach { get { return tileNoAttach; } set { tileNoAttach = value; } }
        public bool[] TileNoFail { get { return tileNoFail; } set { tileNoFail = value; } }
        public bool TilesLoaded { get { return tilesLoaded; } set { tilesLoaded = value; } }
        public bool[] TileSolid { get { return tileSolid; } set { tileSolid = value; } }
        public bool[] TileSolidTop { get { return tileSolidTop; } set { tileSolidTop = value; } }
        public bool[] TileStone { get { return tileStone; } set { tileStone = value; } }
        public bool[] TileTable { get { return tileTable; } set { tileTable = value; } }
        public bool[] TileWaterDeath { get { return tileWaterDeath; } set { tileWaterDeath = value; } }
        public double Time { get { return time; } set { time = value; } }
        public int TimeOut { get { return timeOut; } set { timeOut = value; } }
        public bool ToggleFullscreen { get { return toggleFullscreen; } set { toggleFullscreen = value; } }
        public float TopWorld { get { return topWorld; } set { topWorld = value; } }
        public int UpdateTime { get { return updateTime; } set { updateTime = value; } }
        public bool VerboseNetplay { get { return verboseNetplay; } set { verboseNetplay = value; } }
        public bool[] WallHouse { get { return wallHouse; } set { wallHouse = value; } }
        public float WindSpeed { get { return windSpeed; } set { windSpeed = value; } }
        public float WindSpeedSpeed { get { return windSpeedSpeed; } set { windSpeedSpeed = value; } }
        public int WorldID { get { return worldID; } set { worldID = value; } }
        public string WorldName { get { return worldName; } set { worldName = value; } }
        new public string WorldPath { get { return Main.WorldPath; } set { Main.WorldPath = value; } }
        public string WorldPathName { get { return worldPathName; } set { worldPathName = value; } }
        public double WorldSurface { get { return worldSurface; } set { worldSurface = value; } }
    }

    public class BannedPlayer
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public string Reason { get; set; }

        public BannedPlayer()
        {
            Name = "Unknown";
            IP = "";
            Reason = "Unknown";
        }
    }
}

