/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Octgn.Play;
using Octgn.Play.Gui;
using Octgn.Scripting.Controls;
using Octgn.Utils;
using log4net;

using Octgn.Core;
using Octgn.Core.DataExtensionMethods;
using Octgn.Core.Util;
using Octgn.DataNew.Entities;
using Octgn.Extentions;
using Octgn.Library;
using Octgn.Library.Exceptions;
using Octgn.Scripting;

using Card = Octgn.Play.Card;
using Marker = Octgn.Play.Marker;
using Player = Octgn.Play.Player;
using System.Collections.ObjectModel;
using Octgn.DataNew;
using Octgn.Play.Save;
using Octgn.Core.Play;
using System.Threading.Tasks;
using System.Net;
using Octgn.Networking;
using Octgn.Library.Utils;
using Octgn.Online.Hosting;
using Octgn.Communication;
using Octgn.Online;
using Octgn.Data;

namespace Octgn
{
    [Serializable]
    public class GameEngine : INotifyPropertyChanged
    {
        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Engine ScriptEngine { get; }

        public ScriptApi ScriptApi { get; set; }

        public ObservableDeck LoadedCards { get; }

        public DeckStatsViewModel DeckStats { get; }

        private const int MaxRecentMarkers = 10;
        private const int MaxRecentCards = 10;

        private readonly SortedList<Guid, DataNew.Entities.Marker> _markersById = new SortedList<Guid, DataNew.Entities.Marker>();

        private readonly List<DataNew.Entities.Card> _recentCards = new List<DataNew.Entities.Card>(MaxRecentCards);
        private readonly List<DataNew.Entities.Marker> _recentMarkers = new List<DataNew.Entities.Marker>(MaxRecentMarkers);
        private readonly Dictionary<string, Tuple<BitmapImage, BitmapImage>> _cardFrontsBacksCache = new Dictionary<string, Tuple<BitmapImage, BitmapImage>>();
        private readonly Table _table;
        internal readonly string Password;

        //wouldn't a heap be best for these caches?
        private bool _stopTurn;
        private Play.Player _activePlayer;
        private int _turnNumber;
        private readonly List<Phase> _allPhases = new List<Phase>();
        private Phase _currentPhase;
        //private ushort _uniqueId;
        private bool _BeginCalled;
        private bool _spectator;

        private string boardImage;

        internal string Nickname;

        public bool IsLocal { get; private set; }

        public bool IsDone { get; private set; }

        public bool Spectator {
            get { return _spectator; }
            set {
                if (value == _spectator) return;
                _spectator = value;
                OnPropertyChanged("Spectator");
            }
        }

        public bool MuteSpectators {
            get { return _muteSpectators; }
            set {
                if (_muteSpectators == value) return;
                _muteSpectators = value;
                OnPropertyChanged("MuteSpectators");
            }
        }

        public bool IsMuted {
            get {
                if (Client == null) return false;
                return Client.Muted != 0;
            }
        }

        public bool InPreGame { get; private set; }

        public History History { get; }

        public bool IsReplay { get; }

        public ReplayWriter ReplayWriter { get; }
        public ReplayEngine ReplayEngine { get; }

        public IClient Client { get; private set; }
        public HostedGame HostedGame { get; }
        public string HostedGameName { get; }
        public bool IsHost { get; }
        public bool IsDeveloperMode { get; }

        public GameLog GameLog { get; }

        public GameSettings Settings { get; }

        public Dispatcher Dispatcher { get; }

        public string LocalUserId { get; }

        public ushort CurrentUniqueId;

        public event EventHandler<string> LaunchUrl;

        /// <summary>
        /// For Testing
        /// </summary>
        [Obsolete("This is only to be used for mocking")]
        internal GameEngine() {

        }

        private GameEngine(Game def, Dispatcher dispatcher, string nickname, string localUserId, ReplayReader replayReader, bool isDeveloperMode) {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (replayReader == null) throw new ArgumentNullException(nameof(replayReader));
            if (string.IsNullOrWhiteSpace(nickname)) throw new ArgumentNullException(nameof(nickname));

            Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            LocalUserId = localUserId;

            Settings = new GameSettings();
            Settings.UseTwoSidedTable = def.UseTwoSidedTable;
            Settings.ChangeTwoSidedTable = def.ChangeTwoSidedTable;
            Settings.PropertyChanged += Settings_PropertyChanged;

            IsDeveloperMode = isDeveloperMode;

            IsReplay = true;
            IsHost = true;

            Client = new ReplayClient(this);

            ReplayEngine = new ReplayEngine(this, replayReader, (ReplayClient)Client);

            HostedGameName = replayReader.Replay.Name;

            LoadedCards = new ObservableDeck();
            LoadedCards.Sections = new ObservableCollection<ObservableSection>();

            DeckStats = new DeckStatsViewModel();

            Spectator = false;

            GameLog = new GameLog(() => IsMuted);
            GameLog.LogAdded += GameLog_LogAdded;

            if (def.ScriptVersion.Equals(new Version(0, 0, 0, 0))) {
                GameLog.Warning("This game doesn't have a Script Version specified. Please contact the game developer.\n\n\nYou can get in contact of the game developer here {0}", def.GameUrl);
                def.ScriptVersion = new Version(3, 1, 0, 0);
            }
            if (Versioned.ValidVersion(def.ScriptVersion) == false) {
                GameLog.Warning(
                    "Can't find API v{0}. Loading the latest version.\n\nIf you have problems, get in contact of the developer of the game to get an update.\nYou can get in contact of them here {1}",
                    def.ScriptVersion, def.GameUrl);
                def.ScriptVersion = Versioned.LowestVersion;
            } else {
                var vmeta = Versioned.GetVersion(def.ScriptVersion);
                if (vmeta.DeleteDate <= DateTime.Now) {
                    GameLog.Warning("This game requires an API version {0} which is no longer supported by OCTGN.\nYou can still play, however some aspects of the game may no longer function as expected, and it may be removed at any time.\nYou may want to contact the developer of this game and ask for an update.\n\nYou can find more information about this game at {1}."
                        , def.ScriptVersion, def.GameUrl);
                }
            }
            //Program.ChatLog.ClearEvents();
            IsLocal = true;
            Definition = def;
            Password = string.Empty;
            _table = new Table(this, def.Table);
            if (def.Phases != null) {
                byte PhaseId = 1;
                _allPhases = def.Phases.Select(x => new Phase(PhaseId++, x)).ToList();
            }
            GlobalVariables = new Dictionary<string, string>();
            foreach (var varDef in def.GlobalVariables)
                GlobalVariables.Add(varDef.Name, varDef.DefaultValue);
            ScriptApi = Versioned.Get<ScriptApi>(Definition.ScriptVersion, this);
            this.Nickname = nickname;

            // Load all game markers
            foreach (DataNew.Entities.Marker m in Definition.GetAllMarkers()) {
                if (!_markersById.ContainsKey(m.Id)) {
                    _markersById.Add(m.Id, m);
                }
            }

            // Init fields
            CurrentUniqueId = 1;
            TurnNumber = 0;
            GameBoard = Definition.GameBoards[""];
            ActivePlayer = null;

            foreach (var size in Definition.CardSizes) {
                var front = ImageUtils.CreateFrozenBitmap(this, new Uri(size.Value.Front));
                var back = ImageUtils.CreateFrozenBitmap(this, new Uri(size.Value.Back));
                _cardFrontsBacksCache.Add(size.Key, new Tuple<BitmapImage, BitmapImage>(front, back));
            }

            ScriptEngine = new Engine(this);
            EventProxy = new GameEventProxy(ScriptEngine, this);

            Application.Current.Dispatcher.Invoke(new Action(() => {
                // clear any existing players
                Play.Player.All.Clear();
                Player.Spectators.Clear();
                // Create the global player, if any
                if (Definition.GlobalPlayer != null)
                    Play.Player.GlobalPlayer = new Play.Player(this, Definition, IsReplay);
                // Create the local player
                Play.Player.LocalPlayer = new Player(this, Definition, this.Nickname, LocalUserId, 255, Crypto.ModExp(Prefs.PrivateKey), false, true, IsReplay);

                IsConnected = true;
            }));
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (!IsHost) return;

            if (IsDone) return;

            Client.Rpc.Settings(Settings.UseTwoSidedTable, Settings.AllowSpectators, Settings.MuteSpectators);
        }

        private GameEngine(Game def, Dispatcher dispatcher, string hostedGameName, bool isHost, string nickname, string localUserId, bool specator, bool isDevMode, string password = "", bool isLocal = false, HostedGame hostedGame = null) {
            Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            LocalUserId = localUserId;

            Settings = new GameSettings();
            Settings.UseTwoSidedTable = def.UseTwoSidedTable;
            Settings.ChangeTwoSidedTable = def.ChangeTwoSidedTable;
            Settings.PropertyChanged += Settings_PropertyChanged;

            HostedGame = hostedGame;
            HostedGameName = hostedGameName;
            IsHost = isHost;
            IsDeveloperMode = isDevMode;

            History = new History(def.Id);
            if (IsHost) {
                History.Name = HostedGameName;
            }

            ReplayWriter = new ReplayWriter();

            LoadedCards = new ObservableDeck();
            LoadedCards.Sections = new ObservableCollection<ObservableSection>();

            DeckStats = new DeckStatsViewModel();

            Spectator = specator;

            GameLog = new GameLog(() => IsMuted);
            GameLog.LogAdded += GameLog_LogAdded;

            if (def.ScriptVersion.Equals(new Version(0, 0, 0, 0))) {
                GameLog.Warning("This game doesn't have a Script Version specified. Please contact the game developer.\n\n\nYou can get in contact of the game developer here {0}", def.GameUrl);
                def.ScriptVersion = new Version(3, 1, 0, 0);
            }
            if (Versioned.ValidVersion(def.ScriptVersion) == false) {
                GameLog.Warning(
                    "Can't find API v{0}. Loading the latest version.\n\nIf you have problems, get in contact of the developer of the game to get an update.\nYou can get in contact of them here {1}",
                    def.ScriptVersion, def.GameUrl);
                def.ScriptVersion = Versioned.LowestVersion;
            } else {
                var vmeta = Versioned.GetVersion(def.ScriptVersion);
                if (vmeta.DeleteDate <= DateTime.Now) {
                    GameLog.Warning("This game requires an API version {0} which is no longer supported by OCTGN.\nYou can still play, however some aspects of the game may no longer function as expected, and it may be removed at any time.\nYou may want to contact the developer of this game and ask for an update.\n\nYou can find more information about this game at {1}."
                        , def.ScriptVersion, def.GameUrl);
                }
            }

            IsLocal = isLocal;
            this.Password = password;
            Definition = def;
            _table = new Table(this, def.Table);
            if (def.Phases != null) {
                byte PhaseId = 1;
                _allPhases = def.Phases.Select(x => new Phase(PhaseId++, x)).ToList();
            }
            GlobalVariables = new Dictionary<string, string>();
            foreach (var varDef in def.GlobalVariables)
                GlobalVariables.Add(varDef.Name, varDef.DefaultValue);
            ScriptApi = Versioned.Get<ScriptApi>(Definition.ScriptVersion, this);
            this.Nickname = nickname;
            while (String.IsNullOrWhiteSpace(this.Nickname)) {
                this.Nickname = Prefs.Nickname;
                if (string.IsNullOrWhiteSpace(this.Nickname)) this.Nickname = Randomness.GrabRandomNounWord() + new Random().Next(30);
                var retNick = this.Nickname;
                Dispatcher.Invoke(new Action(() => {
                    var i = new InputDlg("Choose a nickname", "Choose a nickname", this.Nickname);
                    retNick = i.GetString();
                }));
                this.Nickname = retNick;
            }
            // Load all game markers
            foreach (DataNew.Entities.Marker m in Definition.GetAllMarkers()) {
                if (!_markersById.ContainsKey(m.Id)) {
                    _markersById.Add(m.Id, m);
                }
            }
            // Init fields
            CurrentUniqueId = 1;
            TurnNumber = 0;
            GameBoard = Definition.GameBoards[""];
            ActivePlayer = null;

            foreach (var size in Definition.CardSizes) {
                var front = ImageUtils.CreateFrozenBitmap(this, new Uri(size.Value.Front));
                var back = ImageUtils.CreateFrozenBitmap(this, new Uri(size.Value.Back));
                _cardFrontsBacksCache.Add(size.Key, new Tuple<BitmapImage, BitmapImage>(front, back));
            }

            ScriptEngine = new Engine(this);
            EventProxy = new GameEventProxy(ScriptEngine, this);

            Application.Current.Dispatcher.Invoke(new Action(() => {
                // clear any existing players
                Play.Player.All.Clear();
                Player.Spectators.Clear();
                // Create the local player
                Play.Player.LocalPlayer = new Player(this, Definition, this.Nickname, LocalUserId, 255, Crypto.ModExp(Prefs.PrivateKey), specator, true, IsReplay);
                // Create the global player, if any
                if (Definition.GlobalPlayer != null)
                    Play.Player.GlobalPlayer = new Play.Player(this, Definition, IsReplay);
            }));
        }

        public GameBoard GameBoard { get; set; }

        public int TurnNumber {
            get { return _turnNumber; }
            set {
                if (_turnNumber == value) return;
                _turnNumber = value;
                OnPropertyChanged("TurnNumber");
            }
        }

        public Octgn.Play.Player ActivePlayer {
            get { return _activePlayer; }
            set {
                if (_activePlayer == value) return;
                _activePlayer = value;
                OnPropertyChanged("ActivePlayer");
            }
        }

        public bool StopTurn {
            get { return _stopTurn; }
            set {
                if (_stopTurn == value) return;
                _stopTurn = value;
                OnPropertyChanged("StopTurn");
            }
        }

        public List<Phase> AllPhases {
            get { return _allPhases; }
        }

        public Phase CurrentPhase {
            get { return _currentPhase; }
            set {
                if (_currentPhase == value) return;
                _currentPhase = value;
                foreach (var p in _allPhases) {
                    p.IsActive = p == value ? true : false;
                }
                OnPropertyChanged("CurrentPhase");
            }
        }

        public Table Table {
            get { return _table; }
        }

        public Game Definition { get; set; }

        private object _isConnectedLocker = new object();

        public bool IsConnected {
            get {
                return this.isConnected;
            }
            set {
                lock (_isConnectedLocker) {
                    if (value == this.isConnected) {
                        return;
                    }
                    Log.DebugFormat("IsConnected = {0}", value);
                    this.isConnected = value;
                }
                this.OnPropertyChanged("IsConnected");
            }
        }

        public IList<DataNew.Entities.Marker> Markers {
            get { return _markersById.Values; }
        }

        public IList<DataNew.Entities.Marker> RecentMarkers {
            get { return _recentMarkers; }
        }

        public IList<DataNew.Entities.Card> RecentCards {
            get { return _recentCards; }
        }

        public Dictionary<string, int> Variables { get; private set; }
        public Dictionary<string, string> GlobalVariables { get; private set; }

        public bool IsTableBackgroundFlipped {
            get {
                return isTableBackgroundFlipped;
            }
            set {
                isTableBackgroundFlipped = value;
                this.OnPropertyChanged("IsTableBackgroundFlipped");
            }
        }

        public void ChangeGameBoard(string name) {
            if (name == null) return;
            if (!Definition.GameBoards.ContainsKey(name)) return;
            GameBoard = Definition.GameBoards[name];
            BoardImage = GameBoard.Source;
            this.OnPropertyChanged("GameBoard");
            this.OnPropertyChanged("BoardMargin");
        }

        public string BoardImage {
            get {
                return boardImage;
            }
            set {
                if (value == boardImage) return;
                var nw = value;
                if (!File.Exists(nw)) {
                    var workingDirectory = Path.Combine(Config.Instance.Paths.DatabasePath, Definition.Id.ToString());
                    if (File.Exists(Path.Combine(workingDirectory, nw))) {
                        nw = Path.Combine(workingDirectory, nw);
                    } else {
                        throw new Exception(string.Format("Cannot find file {0} or {1}", nw, Path.Combine(workingDirectory, nw)));
                    }
                }

                boardImage = nw;
                this.OnPropertyChanged("BoardImage");
            }
        }

        private Thickness? boardMargin;
        public Thickness BoardMargin {
            get {
                var pos = new Rect(GameBoard.XPos, GameBoard.YPos, GameBoard.Width, GameBoard.Height);
                boardMargin = new Thickness(pos.Left, pos.Top, 0, 0);
                return boardMargin.Value;
            }
        }

        public GameEventProxy EventProxy { get; set; }

        public bool WaitForGameState {
            get {
                return this.waitForGameState;
            }
            set {
                if (value == this.waitForGameState) return;
                Log.DebugFormat("WaitForGameState = {0}", value);
                this.waitForGameState = value;
                this.OnPropertyChanged("WaitForGameState");
            }
        }

        public Guid SessionId { get; set; }
        public bool TableLoaded { get; set; }

        public bool CardsRevertToOriginalOnGroupChange = false;//As opposed to staying SwitchedWithAlternate

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public bool IsWelcomed { get; private set; }

        private string _historyPath = null;
        private string _replayPath = null;
        private string _logPath = null;

        private StreamWriter _logStream = null;

        private string _gameName;

        public async Task Connect(string host, int port) {
            if (Client != null) throw new InvalidOperationException("Already connected.");

            Client = await GameEngine.ConnectToHost(this, host, port);
        }

        public async Task Connect(IPAddress host, int port) {
            if (Client != null) throw new InvalidOperationException("Already connected.");

            Client = await GameEngine.ConnectToHost(this, host, port);
        }

        public void OnWelcomed(Guid gameSessionId, string gameName, bool waitForGameState) {
            InPreGame = true;
            IsWelcomed = true;

            SessionId = gameSessionId;
            WaitForGameState = waitForGameState;
            _gameName = gameName;
        }

        public void OnStart() {
            InPreGame = false;

            if (IsReplay) {
                return;
            }

            History.Name = DateTime.Now.Ticks.ToString();

            if (_historyPath == null) {
                var dir = new DirectoryInfo(Config.Instance.Paths.GameHistoryPath);

                if (!dir.Exists) {
                    dir.Create();
                }

                for (var i = 0; i < Int32.MaxValue; i++) {
                    var historyFileName = History.Name;
                    var replayFileName = History.Name;
                    var logFileName = History.Name;

                    if (i > 0) {
                        historyFileName = replayFileName = logFileName = historyFileName + "_" + i;
                    }

                    historyFileName = Path.Combine(dir.FullName, historyFileName + ".o8h");
                    replayFileName = Path.Combine(dir.FullName, replayFileName + ".o8r");
                    logFileName = Path.Combine(dir.FullName, logFileName + ".o8l");

                    if (!File.Exists(historyFileName) && !File.Exists(replayFileName)) {
                        _historyPath = historyFileName;
                        _replayPath = replayFileName;
                        _logPath = logFileName;
                        break;
                    }
                }

                SaveHistory();
                var replay = new Replay {
                    Name = _gameName,
                    GameId = Definition.Id,
                    User = Player.LocalPlayer.Name
                };

                var stream = File.Open(_replayPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);

                ReplayWriter.Start(replay, stream);

                var logStream = File.Open(_logPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);

                _logStream = new StreamWriter(logStream);
            }
        }

        public void OnKicked(string reason) {
            InPreGame = false;

            GameLog.Warning("You have been kicked: {0}", reason);

            Client.Shutdown();
        }

        public void FireLaunchUrl(string url) {
            LaunchUrl?.Invoke(this, url);
        }

        private string BlockToString(System.Windows.Documents.Block block) {
            if (block == null) return string.Empty;

            switch (block) {
                case System.Windows.Documents.List list: {
                        var result = string.Empty;

                        foreach (var item in list.ListItems) {
                            foreach (var itemBlock in item.Blocks) {
                                result += BlockToString(itemBlock);
                            }
                            result += Environment.NewLine;
                        }

                        return result;
                    }
                case System.Windows.Documents.Paragraph pg: {
                        var result = string.Empty;

                        foreach (var inline in pg.Inlines) {
                            result += RunToString(inline);
                        }

                        return result;
                    }
                case System.Windows.Documents.Section sec: {
                        var result = string.Empty;

                        foreach (var secBlock in sec.Blocks) {
                            result += BlockToString(secBlock);
                        }
                        result += Environment.NewLine;

                        return result;
                    }
                default: {
                        throw new InvalidOperationException($"Block {block.GetType().Name} not implemented.");
                    }
            }
        }

        private string RunToString(System.Windows.Documents.Inline inline) {
            switch (inline) {
                case System.Windows.Documents.Run run: {
                        return run.Text;
                    }

                case System.Windows.Documents.Span span: {
                        var ret = string.Empty;
                        foreach (var sinline in span.Inlines) {
                            ret += RunToString(sinline);
                        }
                        return ret;
                    }

                case System.Windows.Documents.LineBreak lb: {
                        return Environment.NewLine;
                    }

                case System.Windows.Documents.InlineUIContainer uicontainer: {
                        return string.Empty;
                    }

                case System.Windows.Documents.AnchoredBlock ab: {
                        return string.Empty;
                    }

                default:
                    throw new InvalidOperationException($"Inline {inline.GetType().Name} not implemented.");
            }
        }

        private void ValidateGameName(string gameName) {
            if (string.IsNullOrWhiteSpace(gameName)) throw new ArgumentNullException(nameof(gameName));

            if (gameName.Length > 32) throw new ArgumentOutOfRangeException(nameof(gameName), $"Game name max length is 32");
        }

        private static async Task<IClient> ConnectToHost(GameEngine gameEngine, IPAddress host, int port) {
            for (var i = 0; i < 5; i++) {
                try {
                    var client = new ClientSocket(gameEngine, host, port);

                    await client.Connect().ConfigureAwait(false);

                    return client;
                } catch (Exception e) {
                    Log.Warn("Start local game error", e);
                    if (i == 4) throw;
                }

                await Task.Delay(2000).ConfigureAwait(false);
            }

            throw new NotImplementedException("Unreachable branch hit.");
        }

        private static async Task<IClient> ConnectToHost(GameEngine gameEngine, string host, int port) {
            foreach (var address in Dns.GetHostAddresses(host)) {
                if (address == IPAddress.IPv6Loopback) continue;

                // Should use gameData.IpAddress sometime.
                Log.Info($"{nameof(HostOnline)}: Trying to connect to {address}:{port}");

                try {
                    return await ConnectToHost(gameEngine, address, port).ConfigureAwait(false);
                } catch (Exception ex) {
                    Log.Error($"{nameof(HostOnline)}: Couldn't connect to address {address}:{port}", ex);
                }
            }

            throw new InvalidOperationException($"Unable to connect to {AppConfig.GameServerPath}.{port}");
        }

        public static async Task<GameEngine> HostLocal(Dispatcher dispatcher, Game game, string name, string password, User octgnUser, string nickname, bool allowSpectators, bool isDeveloperMode) {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(nickname)) throw new ArgumentNullException(nameof(nickname));

            var hostport = new Random().Next(5000, 6000);
            while (!NetworkHelper.IsPortAvailable(hostport)) hostport++;

            var result = new HostedGame() {
                Id = Guid.NewGuid(),
                Name = name,
                HostUser = octgnUser ?? new User(hostport.ToString(), nickname),
                GameName = game.Name,
                GameId = game.Id,
                GameVersion = game.Version.ToString(),
                HostAddress = $"0.0.0.0:{hostport}",
                Password = password,
                GameIconUrl = game.IconUrl,
                Spectators = allowSpectators,
            };
            if (octgnUser != null) {
                result.HostUserIconUrl = ApiUserCache.Instance.ApiUser(octgnUser)?.IconUrl;
            }

            // Since it's a local game, we want to use the username instead of a userid, since that won't exist.
            var hs = new HostedGameProcess(result, X.Instance.Debug, true);
            hs.Start();

            Prefs.Nickname = nickname;

            var engine = new GameEngine(game, dispatcher, name, true, nickname, octgnUser?.Id, false, isDeveloperMode, password, true);

            var ip = IPAddress.Parse("127.0.0.1");

            await engine.Connect(ip, hostport).ConfigureAwait(false);

            return engine;
        }

        public static async Task<GameEngine> HostOnline(Dispatcher dispatcher, Client lobbyClient, Game game, string name, string password, bool allowSpectators, bool isDeveloperMode) {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            var apiclient = new Octgn.Site.Api.ApiClient();
            if (!await apiclient.IsGameServerRunning(Prefs.Username, Prefs.Password.Decrypt())) {
                throw new UserMessageException("The game server is currently down. Please try again later.");
            }
            // TODO: Replace this with a server-side check
            password = Subscription.IsActive == true ? password : String.Empty;

            var octgnVersion = typeof(GameEngine).Assembly.GetName().Version;

            var req = new HostedGame {
                GameId = game.Id,
                GameVersion = game.Version.ToString(),
                Name = name,
                GameName = game.Name,
                GameIconUrl = game.IconUrl,
                Password = password,
                HasPassword = !string.IsNullOrWhiteSpace(password),
                OctgnVersion = octgnVersion.ToString(),
                Spectators = allowSpectators
            };

            HostedGame result;
            try {
                result = await lobbyClient.HostGame(req) ?? throw new InvalidOperationException("HostGame returned a null");
            } catch (ErrorResponseException ex) {
                if (ex.Code != ErrorResponseCodes.UserOffline) throw;
                throw new UserMessageException("The Game Service is currently offline. Please try again.");
            }

            var engine = new GameEngine(game, dispatcher, result.Name, true, result.HostUser.DisplayName, lobbyClient.User.Id, false, isDeveloperMode, password, false);

            await engine.Connect(AppConfig.GameServerPath, result.Port).ConfigureAwait(false);

            return engine;
        }

        public static async Task<GameEngine> Join(Dispatcher dispatcher, Game game, User octgnUser, string nickname, string password, bool spectator, IPAddress host, int port, bool isDeveloperMode) {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (string.IsNullOrWhiteSpace(nickname)) throw new ArgumentNullException(nameof(nickname));

            Log.InfoFormat("Creating client for {0}:{1}", host, port);

            var engine = new GameEngine(game, dispatcher, null, false, nickname, octgnUser?.Id, spectator, isDeveloperMode, password, true);

            await engine.Connect(host, port).ConfigureAwait(false);

            return engine;
        }

        public static GameEngine Replay(Dispatcher dispatcher, Game game, string replayFile, bool isDeveloperMode) {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (string.IsNullOrWhiteSpace(replayFile)) throw new ArgumentNullException(nameof(replayFile));

            ReplayReader reader = null;
            try {
                reader = ReplayReader.FromStream(File.OpenRead(replayFile));

                return new GameEngine(game, dispatcher, reader.Replay.User, null, reader, isDeveloperMode);
            } catch {
                reader?.Dispose();

                throw;
            }
        }

        public void Begin() {
            //TODO: Merge into Start if possible
            if (_BeginCalled) return;
            _BeginCalled = true;
            // Register oneself to the server
            Version oversion = Const.OctgnVersion;
            Client.Rpc.Hello(this.Nickname, Player.LocalPlayer.UserId, Player.LocalPlayer.PublicKey,
                                     Const.ClientName, oversion, oversion,
                                     Definition.Id, Definition.Version, this.Password
                                     , Spectator);

            if (IsReplay) {
                ReplayEngine.Start();
            }
        }

        public void Resume() {
            //throw new NotImplementedException();
            // Register oneself to the server
            this.gameStateCount = 0;
            Version oversion = Const.OctgnVersion;
            Client.Rpc.HelloAgain(Player.LocalPlayer.Id, this.Nickname, Player.LocalPlayer.UserId, Player.LocalPlayer.PublicKey,
                                     Const.ClientName, oversion, oversion,
                                     Definition.Id, Definition.Version, this.Password);
        }

        public void Reset() {
            TurnNumber = 0;
            ActivePlayer = null;
            foreach (var p in Player.All) {
                foreach (var g in p.Groups)
                    g.Reset();
                foreach (var c in p.Counters)
                    c.Reset();
                foreach (var g in Definition.Player.GlobalVariables)
                    p.GlobalVariables[g.Name] = g.DefaultValue;
            }
            foreach (var p in AllPhases) {
                p.Hold = false;
            }
            CurrentPhase = null;
            Table.Reset();
            Card.Reset();
            CardIdentity.Reset();
            Selection.Clear();

            foreach (var g in Definition.GlobalVariables)
                GlobalVariables[g.Name] = g.DefaultValue;

            DeckStats.Reset();

            //fix MAINWINDOW bug
            App.PlayWindow.RaiseEvent(new CardEventArgs(CardControl.CardHoveredEvent, App.PlayWindow));
            EventProxy.OnGameStart_3_1_0_0();
            EventProxy.OnGameStart_3_1_0_1();
            EventProxy.OnGameStarted_3_1_0_2();
        }

        public void End() {
            IsDone = true;
            SaveHistory();
            ReplayWriter?.Dispose();
            ReplayEngine?.Dispose();
            _logStream?.Dispose();

            Player.Reset();
            Card.Reset();
            CardIdentity.Reset();
            Selection.Clear();

            GameLog.LogAdded -= GameLog_LogAdded;

            OnPropertyChanged(nameof(IsDone));
        }

        public BitmapImage GetCardFront(string name) {
            return _cardFrontsBacksCache[name].Item1;
        }

        public BitmapImage GetCardBack(string name) {
            return _cardFrontsBacksCache[name].Item2;
        }

        public ushort GetUniqueId() {
            return CurrentUniqueId++;
        }

        public int GetUniqueCardId() {
            return (Player.LocalPlayer.Id) << 16 | GetUniqueId();
        }



        //Temporarily store group visibility information for LoadDeck. //bug (google) #20

        public void LoadDeck(IDeck deck, bool limited) {
            var def = Definition;
            int nCards = deck.CardCount();
            var ids = new int[nCards];
            var keys = new Guid[nCards];
            var cards = new Card[nCards];
            var groups = new Play.Group[nCards];
            var sizes = new string[nCards];
            var gtmps = new List<GrpTmp>(); //for temp groups visibility
            int j = 0;
            foreach (var section in deck.Sections) {
                { // Add cards to LoadedCards deck
                    if (!LoadedCards.Sections.Any(x => x.Name == section.Name)) {
                        // Add section
                        ((ObservableCollection<ObservableSection>)LoadedCards.Sections).Add(new ObservableSection() {
                            Name = section.Name,
                            Shared = section.Shared,
                            Cards = new ObservableCollection<ObservableMultiCard>()
                        });

                    }

                    var loadedCardsSection = LoadedCards.Sections.Single(x => x.Name == section.Name);

                    foreach (var card in section.Cards) {
                        var existingCard = loadedCardsSection.Cards.FirstOrDefault(x => x.Id == card.Id);

                        if (existingCard != null) {
                            existingCard.Quantity++;
                        } else {
                            var newCard = new ObservableMultiCard(card);

                            loadedCardsSection.Cards.AddCard(newCard);
                        }
                    }
                }

                DeckSection sectionDef = null;
                sectionDef = section.Shared ? def.SharedDeckSections[section.Name] : def.DeckSections[section.Name];
                if (sectionDef == null)
                    throw new InvalidFileFormatException("Invalid section '" + section.Name + "' in deck file.");
                var player = section.Shared ? Player.GlobalPlayer : Player.LocalPlayer;
                Play.Group group = player.Groups.First(x => x.Name == sectionDef.Group);

                //In order to make the clients know what the card is (if visibility is set so that they can see it),
                //we have to set the visibility to Nobody, and then after the cards are sent, set the visibility back
                //to what it was. //bug (google) #20
                var gt = new GrpTmp(group, group.Visibility, group.Viewers.ToList());
                if (!gtmps.Contains(gt)) {
                    gtmps.Add(gt);
                    group.SetVisibility(false, false);
                }

                foreach (IMultiCard element in section.Cards) {
                    //DataNew.Entities.Card mod = Definition.GetCardById(element.Id);
                    for (int i = 0; i < element.Quantity; i++) {
                        //for every card in the deck, generate a unique key for it, ID for it
                        var card = element.ToPlayCard(player);
                        ids[j] = card.Id;
                        keys[j] = card.Type.Model.Id;
                        //keys[j] = card.GetEncryptedKey();
                        groups[j] = group;
                        sizes[j] = card.Size.Name;
                        cards[j++] = card;
                        group.AddAt(card, group.Count);

                        DeckStats.AddCard(card);
                    }

                    // Load images in the background
                    string pictureUri = element.GetPicture();
                    Dispatcher.CurrentDispatcher.BeginInvoke(
                        new Func<GameEngine, string, BitmapImage>(ImageUtils.CreateFrozenBitmap),
                        DispatcherPriority.Background, this, pictureUri);
                }
            }

            string sleeveString = null;

            if (deck.Sleeve != null) {
                try {
                    var loadSleeve = true;

                    if (!IsLocal) {
                        var isSubscriber = Subscription.IsActive;

                        if (isSubscriber == null) {
                            loadSleeve = false;

                            Log.Warn("Can't set deck sleeve, unable to determin if user is a subscriber.");

                            GameLog.Warning($"Deck sleeve can not be loaded, subscriber status is unknown.");

                        } else if (isSubscriber == false) {
                            loadSleeve = false;

                            Log.Warn("Not authorized to use deck sleeve.");

                            GameLog.Warning($"Deck sleeve can not be used, you're not a subscriber.");
                        }
                    }

                    if (loadSleeve) {
                        Player.LocalPlayer.SetSleeve(deck.Sleeve);

                        sleeveString = Sleeve.ToString(deck.Sleeve);
                    } else {
                        Log.Info("Sleeve will not be loaded.");
                    }
                } catch (Exception ex) {
                    Log.Warn(ex.Message, ex);

                    GameLog.Warning($"There was an error loading the decks sleeve.");
                }
            }

            Client.Rpc.LoadDeck(ids, keys, groups, sizes, sleeveString ?? string.Empty, limited);
            //reset the visibility to what it was before pushing the deck to everybody. //bug (google) #20
            foreach (GrpTmp g in gtmps) {
                switch (g.Visibility) {
                    case GroupVisibility.Everybody:
                        g.Group.SetVisibility(true, false);
                        break;
                    case GroupVisibility.Nobody:
                        g.Group.SetVisibility(false, false);
                        break;
                    default:
                        foreach (Player p in g.Viewers) {
                            g.Group.AddViewer(p, false);
                        }
                        break;
                }
            }
            gtmps.Clear();
            gtmps.TrimExcess();
        }

        internal void AddRecentCard(DataNew.Entities.Card card) {
            int idx = _recentCards.FindIndex(c => c.Id == card.Id);
            if (idx == 0) return;
            if (idx > 0) {
                _recentCards.RemoveAt(idx);
                _recentCards.Insert(0, card);
                return;
            }

            if (_recentCards.Count == MaxRecentCards)
                _recentCards.RemoveAt(MaxRecentCards - 1);
            _recentCards.Insert(0, card);
        }

        internal void AddRecentMarker(DataNew.Entities.Marker marker) {
            int idx = _recentMarkers.IndexOf(marker);
            if (idx == 0) return;
            if (idx > 0) {
                _recentMarkers.RemoveAt(idx);
                _recentMarkers.Insert(0, marker);
                return;
            }

            if (_recentMarkers.Count == MaxRecentMarkers)
                _recentMarkers.RemoveAt(MaxRecentMarkers - 1);
            _recentMarkers.Insert(0, marker);
        }

        internal DataNew.Entities.Marker GetMarkerModel(Guid id) {
            DataNew.Entities.Marker model;
            if (id.CompareTo(new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10)) < 0) {
                // Get a standard model
                DefaultMarkerModel defaultModel = Marker.DefaultMarkers.First(x => x.Id == id);
                model = (DefaultMarkerModel)defaultModel.Clone();
                model.Id = id;
                return model;
            }
            // Try to find the marker model
            if (!_markersById.TryGetValue(id, out model)) {
                GameLog.GameDebug("Marker model '{0}' not found, using default marker instead", id);
                DefaultMarkerModel defaultModel = Marker.DefaultMarkers[Crypto.Random(7)];
                model = (DefaultMarkerModel)defaultModel.Clone();
                model.Id = id;
                return model;
            }
            return model.Clone() as DataNew.Entities.Marker;
        }

        private void OnPropertyChanged(string propertyName) {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }



        private bool isTableBackgroundFlipped;

        private bool waitForGameState;

        #region Nested type: GrpTmp

        internal struct GrpTmp : IEquatable<GrpTmp>
        {
            public readonly Play.Group Group;
            public readonly List<Play.Player> Viewers;
            public readonly GroupVisibility Visibility;

            public GrpTmp(Play.Group g, GroupVisibility vis, List<Play.Player> v) {
                Group = g;
                Visibility = vis;
                Viewers = v;
            }

            #region IEquatable<GrpTmp> Members

            public bool Equals(GrpTmp gg) {
                return Group == gg.Group;
            }

            #endregion
        }

        #endregion

        public void PlaySoundReq(Player player, string name) {
            if (Definition.Sounds.ContainsKey(name.ToLowerInvariant())) {
                var sound = this.Definition.Sounds[name.ToLowerInvariant()];
                Sounds.PlayGameSound(this, sound);
            }
        }

        public void Ready() {
            Log.Debug("Ready");
            Client.Rpc.Ready(Player.LocalPlayer);
        }

        public void SaveHistory() {
            if (IsReplay) return;
            if (_historyPath == null) return;

            var serialized = History.GetSnapshot(this, Player.LocalPlayer);

            File.WriteAllBytes(_historyPath, serialized);
        }

        public void ExecuteRemoteCall(Player fromPlayer, string func, string args) {
            // Build args
            try {
                //var argo = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(args);
                ScriptEngine.ExecuteFunctionNoFormat(func, args);

            } catch (Exception) {

            }
        }

        private int gameStateCount = 0;

        private bool isConnected;
        private bool _muteSpectators;

        public void GotGameState(Player fromPlayer) {
            Log.DebugFormat("GotGameState {0} {1}", fromPlayer, gameStateCount);
            gameStateCount++;
            fromPlayer.Ready = true;
            if (gameStateCount == Player.Count - 1) {
                Log.DebugFormat("GotGameState Got all states");
                WaitForGameState = false;
                Ready();
            }
        }

        private void GameLog_LogAdded(object sender, IGameMessage message) {
            for (var i = 0; i < message.Arguments.Length; i++) {
                var arg = message.Arguments[i];
                var cardModel = arg as DataNew.Entities.Card;
                var cardId = arg as CardIdentity;
                var card = arg as Card;
                if (card != null && (card.FaceUp || card.MayBeConsideredFaceUp))
                    cardId = card.Type;

                if (cardId != null || cardModel != null) {
                    ChatCard chatCard = null;
                    if (cardId != null) {
                        chatCard = new ChatCard(cardId);
                    } else {
                        chatCard = new ChatCard(cardModel);
                    }
                    if (card != null)
                        chatCard.SetGameCard(card);
                    message.Arguments[i] = chatCard;
                } else {
                    message.Arguments[i] = arg == null ? "[?]" : arg.ToString();
                }
            }

            if (_logStream == null) return;

            if (message is DebugMessage) return;

            string blockString = null;

            Dispatcher.InvokeAsync(() => {
                var block = ChatControl.GameMessageToBlock(message);

                blockString = BlockToString(block);

                try {
                    _logStream.Write(blockString);

                    _logStream.Flush();
                } catch (ObjectDisposedException) {

                }
            });
        }
    }
}
