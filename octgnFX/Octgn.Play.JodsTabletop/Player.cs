/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using log4net;
using Octgn.Core;
using Octgn.Core.DataExtensionMethods;
using Octgn.Core.Play;
using Octgn.DataNew.Entities;
using Octgn.Site.Api;

namespace Octgn.Play
{
    public sealed class Player : INotifyPropertyChanged, IPlayPlayer
    {
        private static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Static members

        private static readonly Color _black = Color.FromRgb(0x00, 0x00, 0x00);

        private static readonly Color[] _playerColors =
        {
            Color.FromRgb(0x00, 0x80, 0x00),
            Color.FromRgb(0xcc, 0x00, 0x00),
            Color.FromRgb(0x00, 0x00, 0x80),
            Color.FromRgb(0x80, 0x00, 0x80),
            Color.FromRgb(0xcc, 0x66, 0x00),
            Color.FromRgb(0x00, 0x80, 0x80),
            Color.FromRgb(0x66, 0x4b, 0x32),
            Color.FromRgb(0x50, 0x20, 0x60),
            Color.FromRgb(0x80, 0x80, 0x00),
            Color.FromRgb(0xFF, 0x00, 0x00),
            Color.FromRgb(0x80, 0x80, 0x80),
            Color.FromRgb(0x20, 0x60, 0x20),
            Color.FromRgb(0xFF, 0x00, 0xFF),
            Color.FromRgb(0x00, 0x00, 0xFF)
        };

        public static Player LocalPlayer;

        // May be null if there's no global lPlayer in the game definition
        public static Player GlobalPlayer;

        // Contains all players in this game (TODO: Rename to All, then cleanup all the dependancies)

        // Get all players in the game
        public static ObservableCollection<Player> All { get; } = new ObservableCollection<Player>();

        // Get all players in the game, except a possible Global lPlayer
        public static ObservableCollection<Player> AllExceptGlobal { get; } = new ObservableCollection<Player>();

        public static ObservableCollection<Player> Spectators { get; } = new ObservableCollection<Player>();

        // Number of players
        internal static int Count
        {
            get { return GlobalPlayer == null ? All.Count : All.Count - 1; }
        }

        // Find a lPlayer with his id
        internal static Player Find(GameEngine gameEngine, byte id)
        {
            return All.FirstOrDefault(p => p.Id == id);
        }

        internal static Player FindIncludingSpectators(GameEngine gameEngine, byte id)
        {
            return All.Union(Spectators).FirstOrDefault(p => p.Id == id);
        }

        // Resets the lPlayer list
        internal static void Reset()
        {
            lock (All)
            {
                All.Clear();
                Spectators.Clear();
                LocalPlayer = GlobalPlayer = null;
            }
        }



        public static event Action OnLocalPlayerWelcomed;
        public static void FireLocalPlayerWelcomed()
        {
            if (OnLocalPlayerWelcomed != null)
                OnLocalPlayerWelcomed.Invoke();
        }

        // May be null if we're in pure server mode

        internal static event EventHandler<PlayerEventArgs> PlayerAdded;
        internal static event EventHandler<PlayerEventArgs> PlayerRemoved;

        static Player()
        {
            All.CollectionChanged += (sender, args) =>
            {
                AllExceptGlobal.Clear();
                foreach (var p in All.ToArray().Where(x => !x.IsGlobal))
                {
                    AllExceptGlobal.Add(p);
                }
                foreach (var p in All.Union(Spectators))
                {
                    p.OnPropertyChanged("All");
                    p.OnPropertyChanged("AllExceptGlobal");
                    p.OnPropertyChanged("Count");
                    p.OnPropertyChanged("Spectators");
                    p.OnPropertyChanged("WaitingOnPlayers");
                    p.OnPropertyChanged("Ready");
                }

            };
        }

        #endregion

        #region Public fields and properties

        internal readonly ulong PublicKey; // Public cryptographic key
        internal readonly double minHandSize;
        private bool _invertedTable;
        private string _name;
        private byte _id;
        private bool _ready;
        private bool _spectator;
        private int _disconnectPercent;
        private string _userIcon;

        private PlayerState state;

        public bool WaitingOnPlayers
        {
            get
            {
                var ret = AllExceptGlobal.Any(x => !x.Ready);
                Log.Debug("WaitingOnPlayers Checking Players Ready Status");
                foreach (var p in AllExceptGlobal)
                {
                    Log.DebugFormat("Player {0} Ready={1}", p.Name, p.Ready);
                }
                Log.DebugFormat("WaitingOnPlayers = {0}", ret);
                Log.Debug("WaitingOnPlayers Done Checking Players Ready Status");
                return ret;
            }
        }

        public bool Ready
        {
            get
            {
                return _ready;
            }
            set
            {
                _ready = value;
                Log.DebugFormat("Player {0} Ready = {1}", this.Name, value);
                this.OnPropertyChanged("Ready");
                foreach (var p in All)
                    p.OnPropertyChanged("WaitingOnPlayers");
                foreach (var p in Spectators)
                    p.OnPropertyChanged("WaitingOnPlayers");
            }
        }

        public Counter[] Counters { get; }

        public bool Subscriber { get; }

        public Group[] IndexedGroups { get; }

        public IEnumerable<Group> Groups
        {
            get { return IndexedGroups.Where(g => g != null); }
        }

        public IEnumerable<Group> BottomGroups
        {
            get
            {
                return Groups.Where(x => x.Name.Equals("library", StringComparison.InvariantCultureIgnoreCase) == false);
            }
        }

        public IEnumerable<Group> TableGroups
        {
            get
            {
                return IndexedGroups.Where(x => x.Name.Equals("library", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public bool CanKick { get; private set; }

        public bool IsHostOrLocal
        {
            get
            {
                return (this == LocalPlayer) || GameEngine.IsHost;
            }
        }

        public bool Spectator
        {
            get { return _spectator; }
            set
            {
                if (GameEngine.Client == null) return;
                Log.InfoFormat("[Spectator]{0} {1}", this, value);
                if (_spectator == value) return;
                this.UpdateSettings(InvertedTable, value, true);
            }
        }

        public Dictionary<string, string> GlobalVariables { get; private set; }

        public Hand Hand { get; }

        public byte Id // Identifier
        {
            get { return _id; }
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged("Id");
            }
        }

        public string Name // Nickname
        {
            get { return _name; }
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        public string UserId { get; }

        public int DisconnectPercent
        {
            get { return _disconnectPercent; }
            set
            {
                if (_disconnectPercent == value) return;
                _disconnectPercent = value;
                OnPropertyChanged("DisconnectPercent");
            }
        }

        public string UserIcon
        {
            get { return _userIcon; }
            set
            {
                if (_userIcon == value) return;
                _userIcon = value;
                OnPropertyChanged("UserIcon");
            }
        }

        public bool IsGlobalPlayer
        {
            get { return Id == 0; }
        }

        /// <summary>
        /// True if the lPlayer plays on the opposite side of the table (for two-sided table only)
        /// </summary>
        public bool InvertedTable
        {
            get { return _invertedTable; }
            set
            {
                Log.InfoFormat("[InvertedTable]{0} {1}", this, value);
                this.UpdateSettings(value, Spectator, true);
            }
        }

        //Color for the chat.
        // Associated color
        public Color Color { get; set; }
        public Color ActualColor { get; set; }

        // Work around a WPF binding bug ? Borders don't seem to bind correctly to Color!
        public Brush Brush { get; set; }

        public Brush TransparentBrush { get; set; }

        public PlayerState State
        {
            get
            {
                return this.state;
            }
            set
            {
                if (value == this.state) return;
                Log.DebugFormat("Player {0} State = {1}", this.Name, value);
                this.state = value;
                this.OnPropertyChanged("State");
                this.OnPropertyChanged("Ready");
                foreach (var p in All)
                    p.OnPropertyChanged("WaitingOnPlayers");
            }
        }

        public BitmapImage SleeveImage {
            get => _sleeveImage;
            set {
                if(value != _sleeveImage) {
                    _sleeveImage = value;
                    OnPropertyChanged(nameof(SleeveImage));
                }
            }
        }
        private BitmapImage _sleeveImage;

        public void SetSleeve(ISleeve sleeve) {
            SleeveImage = sleeve.GetImage();
        }

        //Set the player's color based on their id.
        public void SetPlayerColor(int idx)
        {
            // Create the Player's Color
            Color playerColor;
            if (idx == 0 || idx == 255)
                playerColor = _black;
            else
                playerColor = _playerColors[(idx - 1) % _playerColors.Length];

            ActualColor = playerColor;
            if (this == LocalPlayer)
            {
                return;
            }
            Color = playerColor;
            Brush = new SolidColorBrush(Color);
            Brush.Freeze();
            TransparentBrush = new SolidColorBrush(Color) { Opacity = 0.4 };
            TransparentBrush.Freeze();

            //Notify clients that this has changed
            OnPropertyChanged("Color");
            OnPropertyChanged("Brush");
            OnPropertyChanged("TransparentBrush");
        }

        public void SetPlayerColor(string colorHex)
        {
            var convertFromString = ColorConverter.ConvertFromString(colorHex);
            if (convertFromString != null)
            {
                ActualColor = (Color)convertFromString;

                if (this == LocalPlayer)
                {
                    return;
                }

                Color = (Color) convertFromString;

                Brush = new SolidColorBrush(Color);
                TransparentBrush = new SolidColorBrush(Color) {Opacity = 0.4};

                OnPropertyChanged("Color");
                OnPropertyChanged("Brush");
                OnPropertyChanged("TransparentBrush");
            }
        }

        #endregion

        #region Public interface

        internal void SetupPlayer(bool spectator)
        {
            State = PlayerState.Connected;
        }

        public bool IsLocal { get; }

        // C'tor
        internal Player(GameEngine gameEngine, DataNew.Entities.Game g, string name, string userId, byte id, ulong pkey, bool spectator, bool local, bool isReplay)
        {
            GameEngine = gameEngine ?? throw new ArgumentNullException(nameof(gameEngine));

            Id = id;
            _name = name;
            IsLocal = local;

            if (!string.IsNullOrWhiteSpace(userId)) {
                UserId = userId;

                if (!userId.StartsWith("##LOCAL##")) {
                    Task.Factory.StartNew(async () => {
                        try {
                            var c = new ApiClient();

                            var apiUser = await c.UserFromUserId(userId);
                            if (apiUser != null) {
                                this.DisconnectPercent = apiUser.DisconnectPercent;
                                this.UserIcon = apiUser.IconUrl;
                            }
                        } catch (Exception e) {
                            Log.Warn("Player() Error getting api stuff", e);
                        }
                    });
                }
            } else {
                UserId = $"##LOCAL##{name}:{id}";
            }
            _spectator = spectator;
            SetupPlayer(Spectator);
            PublicKey = pkey;
            if (Spectator == false)
            {
                All.Add(this);
            }
            else
            {
                Spectators.Add(this);
            }
            // Assign subscriber status
            Subscriber = Subscription.IsActive;
            //Create the color brushes
            SetPlayerColor(id);
            // Create counters
            Counters = new Counter[0];
            if (g.Player.Counters != null)
                Counters = g.Player.Counters.Select(x => new Counter(GameEngine, this, x, isReplay)).ToArray();
            // Create global variables
            GlobalVariables = new Dictionary<string, string>();
            foreach (var varD in g.Player.GlobalVariables)
                GlobalVariables.Add(varD.Name, varD.Value);
            // Create a hand, if any
            if (g.Player.Hand != null)
                Hand = new Hand(this, g.Player.Hand);
            // Create groups
            IndexedGroups = new Group[0];
            if (g.Player.Groups != null)
            {
                var tempGroups = g.Player.Groups.ToArray();
                IndexedGroups = new Group[tempGroups.Length + 1];
                IndexedGroups[0] = Hand;
                for (int i = 1; i < IndexedGroups.Length; i++)
                    IndexedGroups[i] = new Pile(this, tempGroups[i - 1]);
            }
            minHandSize = 250;
            if (Spectator == false)
            {
                // Raise the event
                if (PlayerAdded != null) PlayerAdded(null, new PlayerEventArgs(this));
                Ready = false;
                OnPropertyChanged("All");
                OnPropertyChanged("AllExceptGlobal");
                OnPropertyChanged("Count");
            }
            else
            {
                OnPropertyChanged("Spectators");
                Ready = true;
            }
            CanKick = local == false && GameEngine.IsHost;
        }

        public bool IsGlobal { get; }
        public GameEngine GameEngine { get; }

        // C'tor for global items
        internal Player(GameEngine gameEngine, DataNew.Entities.Game g, bool isReplay)
        {
            GameEngine = gameEngine ?? throw new ArgumentNullException(nameof(gameEngine));

            IsGlobal = true;
            _spectator = false;
            SetupPlayer(false);
            var globalDef = g.GlobalPlayer;
            // Register the lPlayer
            lock (All)
                All.Add(this);
            // Init fields
            _name = "Global";
            Id = 0;
            PublicKey = 0;
            if (GlobalVariables == null)
            {
                // Create global variables
                GlobalVariables = new Dictionary<string, string>();
                foreach (var varD in g.Player.GlobalVariables)
                    GlobalVariables.Add(varD.Name, varD.Value);
            }
            // Create counters
            Counters = new Counter[0];
            if (globalDef.Counters != null)
                Counters = globalDef.Counters.Select(x => new Counter(GameEngine, this, x, isReplay)).ToArray();
            // Create global's lPlayer groups
            // TODO: This could fail with a run-time exception on write, make it safe
            // I don't know if the above todo is still relevent - Kelly Elton - 3/18/2013
            if (globalDef.Groups != null)
            {
                var tempGroups = globalDef.Groups.ToArray();
                IndexedGroups = new Group[tempGroups.Length + 1];
                IndexedGroups[0] = Hand;
                for (int i = 1; i < IndexedGroups.Length; i++)
                    IndexedGroups[i] = new Pile(this, tempGroups[i - 1]);
            }
            OnPropertyChanged("All");
            OnPropertyChanged("AllExceptGlobal");
            OnPropertyChanged("Count");
            minHandSize = 0;
            CanKick = false;
        }

        internal void UpdateSettings(bool invertedTable, bool spectator, bool notify)
        {
            Log.InfoFormat("[UpdateSettings]{0} {1} {2}", this, invertedTable, spectator);
            if (GameEngine.InPreGame == false) return;
            _invertedTable = invertedTable;
            _spectator = spectator;
            if (_spectator)
                _invertedTable = false;
            if (this == Player.LocalPlayer)
                GameEngine.Spectator = _spectator;
            OnPropertyChanged("InvertedTable");
            OnPropertyChanged("Spectator");
            OnPropertyChanged("All");
            OnPropertyChanged("AllExceptGlobal");
            OnPropertyChanged("Count");

            if(notify) // used to prevent feedback loops
                GameEngine.Client.Rpc.PlayerSettings(this, _invertedTable, _spectator);
        }

        public static void RefreshSpectators()
        {
            lock (All)
            {
                foreach (var p in All.Where(x => x.Spectator).ToArray())
                {
                    All.Remove(p);
                    Spectators.Add(p);
                }
                foreach (var s in Spectators.Where(x => x.Spectator == false).ToArray())
                {
                    Spectators.Remove(s);
                    All.Add(s);
                }
                foreach (var p in All.Union(Spectators))
                {
                    p.OnPropertyChanged("All");
                    p.OnPropertyChanged("AllExceptGlobal");
                    p.OnPropertyChanged("Count");
                    p.OnPropertyChanged("Spectators");
                    p.OnPropertyChanged("WaitingOnPlayers");
                    p.OnPropertyChanged("Ready");
                }
            }
        }

        // Remove the lPlayer from the game
        internal void Delete()
        {

            // Remove from the list
            lock (All)
            {
                All.Remove(this);
                Spectators.Remove(this);
            }
            if (GameEngine.ActivePlayer == this)
            {
                GameEngine.ActivePlayer = null;
            }
            this.OnPropertyChanged("Ready");
            foreach (var p in All)
                p.OnPropertyChanged("WaitingOnPlayers");
            // Raise the event
            if (PlayerRemoved != null) PlayerRemoved(null, new PlayerEventArgs(this));
        }

        public void Print(string text, string color = null)
        {
            var p = Parse(this, text);
            if (color == null)
            {
                GameEngine.GameLog.Notify(p.Item1, p.Item2);
            }
            else
            {
                Color? c = null;
                if (String.IsNullOrWhiteSpace(color))
                {
                    c = Colors.Black;
                }
                if (c == null)
                {
                    try
                    {
                        if (color.StartsWith("#") == false)
                        {
                            color = color.Insert(0, "#");
                        }
                        if (color.Length == 7)
                        {
                            color = color.Insert(1, "F");
                            color = color.Insert(1, "F");
                        }
                        c = (Color)ColorConverter.ConvertFromString(color);
                    }
                    catch
                    {
                        c = Colors.Black;
                    }
                }
                GameEngine.GameLog.NotifyBar(c.Value, p.Item1, p.Item2);
            }
        }

        private static Tuple<string, object[]> Parse(Player player, string text)
        {
            string finalText = text;
            int i = 0;
            var args = new List<object>(2);
            Match match = Regex.Match(text, "{([^}]*)}");
            while (match.Success)
            {
                string token = match.Groups[1].Value;
                finalText = finalText.Replace(match.Groups[0].Value, "##$$%%^^LEFTBRACKET^^%%$$##" + i + "##$$%%^^RIGHTBRACKET^^%%$$##");
                i++;
                object tokenValue = token;
                switch (token)
                {
                    case "me":
                        tokenValue = player;
                        break;
                    default:
                        if (token.StartsWith("#"))
                        {
                            int id;
                            if (!int.TryParse(token.Substring(1), out id)) break;
                            ControllableObject obj = ControllableObject.Find(player.GameEngine, id);
                            if (obj == null) break;
                            tokenValue = obj;
                            break;
                        }
                        break;
                }
                args.Add(tokenValue);
                match = match.NextMatch();
            }
            args.Add(player);
            finalText = finalText.Replace("{", "").Replace("}", "");
            finalText = finalText.Replace("##$$%%^^LEFTBRACKET^^%%$$##", "{").Replace(
                "##$$%%^^RIGHTBRACKET^^%%$$##", "}");
            return new Tuple<string, object[]>(finalText, args.ToArray());
        }


        public override string ToString()
        {
            return _name;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }

    public class PlayerEventArgs : EventArgs
    {
        public readonly Player Player;

        public PlayerEventArgs(Player p)
        {
            Player = p;
        }
    }
}