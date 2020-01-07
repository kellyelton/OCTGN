/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Octgn.Data;
using Octgn.Extentions;
using Octgn.Play.Dialogs;
using Octgn.Play.Gui;
using Octgn.Scripting;
using Octgn.Utils;
using System.Collections.ObjectModel;
using System.Windows.Navigation;

using Octgn.Core;
using Octgn.Core.DataExtensionMethods;
using Octgn.Core.DataManagers;
using Octgn.Core.Play;
using Octgn.DataNew.Entities;
using Octgn.Library;
using Octgn.Library.Exceptions;

using log4net;
using Octgn.Controls;
using Octgn.Play.Save;

namespace Octgn.Play
{
    public partial class PlayWindow : INotifyPropertyChanged
    {
        private bool _isLocal;

        protected readonly Engine ScriptEngine;
        internal new static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Dependency Properties

        public bool IsFullScreen
        {
            get { return (bool)GetValue(IsFullScreenProperty); }
            set { SetValue(IsFullScreenProperty, value); }
        }

        public static readonly DependencyProperty IsFullScreenProperty =
            DependencyProperty.Register("IsFullScreen", typeof(bool), typeof(PlayWindow),
                                        new UIPropertyMetadata(false));

        public bool ShowSubscribeMessage
        {
            get { return (bool)GetValue(ShowSubscribeMessageProperty); }
            set { SetValue(ShowSubscribeMessageProperty, value); }
        }

        public static readonly DependencyProperty ShowSubscribeMessageProperty =
            DependencyProperty.Register("ShowSubscribeMessage", typeof(bool), typeof(PlayWindow),
                                        new UIPropertyMetadata(false));

        #endregion

        private SolidColorBrush _backBrush = new SolidColorBrush(Color.FromArgb(210, 33, 33, 33));
        private SolidColorBrush _offBackBrush = new SolidColorBrush(Color.FromArgb(55, 33, 33, 33));
        private Storyboard _fadeIn, _fadeOut;
        private static System.Collections.ArrayList fontName = new System.Collections.ArrayList();
        private GameLogReader _gameMessageReader;

        private Card _currentCard;
        private bool _currentCardUpStatus;
        private bool _newCard;

        private Storyboard _showBottomBar;

        private TableControl table;

        private DateTime lastMessageSoundTime = DateTime.MinValue;

        public ObservableCollection<IGameMessage> GameMessages { get; set; }

        public bool IsHost { get; set; }

        public bool ChatVisible
        {
            get
            {
                return this.chatVisible;
            }
            set
            {
                if (value == this.chatVisible) return;
                this.chatVisible = value;
                OnPropertyChanged("ChatVisible");
            }
        }

        public bool EnableGameScripts
        {
            get
            {
                return Prefs.EnableGameScripts;
            }
            set
            {
                Prefs.EnableGameScripts = value;
                OnPropertyChanged("EnableGameScripts");
            }
        }

        public ReplayEngine ReplayEngine { get; }

        public GameEngine GameEngine {
            get { return (GameEngine)GetValue(GameEngineProperty); }
            set { SetValue(GameEngineProperty, value); }
        }

        public static readonly DependencyProperty GameEngineProperty =
            DependencyProperty.Register(nameof(GameEngine), typeof(GameEngine), typeof(PlayWindow), new PropertyMetadata(GameEngine_Changed));

        private static void GameEngine_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var playWindow = (PlayWindow)d;

            playWindow.OnPropertyChanged(nameof(GameSettings));
        }

        [Obsolete("Used only when in design mode")]
        public PlayWindow()
            : base() {
            InitializeComponent();
        }

        public PlayWindow(GameEngine gameEngine) {
            GameEngine = gameEngine ?? throw new ArgumentNullException(nameof(gameEngine));

            IsHost = GameEngine.IsHost;
            GameMessages = new ObservableCollection<IGameMessage>();
            _gameMessageReader = new GameLogReader(GameEngine.GameLog);
            var isLocal = GameEngine.IsLocal;
            DataContext = GameEngine;

            ReplayEngine = GameEngine.ReplayEngine;

            InitializeComponent();


            if (GameEngine.IsReplay) {
                foreach (var eve in ReplayEngine.AllEvents) {
                    if (eve.Type == ReplayEventType.NextTurn) {
                        ReplaySlider.Ticks.Add(eve.Time.Ticks);
                    }
                }

                ReplayControls.Visibility = Visibility.Visible;
            } else {
                ReplayControls.Visibility = Visibility.Collapsed;
            }

            _isLocal = isLocal;
            //Application.Current.MainWindow = this;
            Version oversion = Assembly.GetExecutingAssembly().GetName().Version;
            Title = "Octgn  version : " + oversion + " : " + GameEngine.Definition.Name;
            ScriptEngine = GameEngine.ScriptEngine;
            if (GameEngine.AllPhases.Count() < 1) PhaseControl.Visibility = Visibility.Collapsed;
            this.Loaded += OnLoaded;
            this.chat.MouseEnter += ChatOnMouseEnter;
            this.chat.MouseLeave += ChatOnMouseLeave;
            this.playerTabs.MouseEnter += PlayerTabsOnMouseEnter;
            this.playerTabs.MouseLeave += PlayerTabsOnMouseLeave;
            this.PreGameLobby.OnClose += delegate
            {
                if (this.PreGameLobby.StartingGame)
                {
                    PreGameLobby.Visibility = Visibility.Collapsed;
                    if (Player.LocalPlayer.Spectator == false && GameEngine.IsReplay == false)
                        GameEngine.ScriptEngine.SetupEngine(false);


                    table = new TableControl(GameEngine) { DataContext = GameEngine.Table, IsTabStop = true };
                    KeyboardNavigation.SetIsTabStop(table, true);
                    TableHolder.Child = table;

                    table.UpdateSided();
                    Keyboard.Focus(table);

					Dispatcher.BeginInvoke(new Action(GameEngine.Ready), DispatcherPriority.ContextIdle);

                    //GameEngine.Ready();
                    if (GameEngine.IsDeveloperMode && Player.LocalPlayer.Spectator == false && GameEngine.IsReplay == false)
                    {
                        MenuConsole.Visibility = Visibility.Visible;
                        var wnd = new DeveloperWindow(GameEngine) { Owner = this };
                        wnd.Show();
                    }
                    // Select proper player tab
                    Dispatcher.BeginInvoke(new Action(() =>
                        {
                            playerTabs.SelectedIndex = 0;
                        }));
                }
                else
                {
                    IsRealClosing = true;
                    this.TryClose();
                }
            };

            this.Loaded += delegate
            {
                _gameMessageReader.Start(
                    x =>
                    {
                        Dispatcher.Invoke(new Action(
                            () =>
                            {
                                bool gotOne = false;
                                foreach (var m in x)
                                {
                                    var b = Octgn.Play.Gui.ChatControl.GameMessageToBlock(m);
                                    if (b == null) continue;

                                    if (m is NotifyBarMessage)
                                    {
                                        GameMessages.Insert(0, m);
                                        gotOne = true;
                                        while (GameMessages.Count > 60)
                                        {
                                            GameMessages.Remove(GameMessages.Last());
                                        }
                                    }
                                }
                                if (!gotOne) return;

                                if (_showBottomBar != null && _showBottomBar.GetCurrentProgress(BottomBar) > 0)
                                {
                                    _showBottomBar.Seek(BottomBar, TimeSpan.FromMilliseconds(500), TimeSeekOrigin.BeginTime);
                                }
                                else
                                {
                                    if (_showBottomBar == null)
                                    {
                                        _showBottomBar = BottomBar.Resources["ShowBottomBar"] as Storyboard;
                                    }
                                    _showBottomBar.Begin(BottomBar, HandoffBehavior.Compose, true);
                                }
                                if (this.IsActive == false)
                                {
                                    this.FlashWindow();
                                }
                                if (this.IsActive == false && Prefs.EnableGameSound && DateTime.Now > lastMessageSoundTime.AddSeconds(10))
                                {
                                    Octgn.Utils.Sounds.PlayGameMessageSound();
                                    lastMessageSoundTime = DateTime.Now;
                                }
                            }));
                    });
            };
            this.Activated += delegate
            {
                this.StopFlashingWindow();
            };
            this.Unloaded += delegate
            {
                _gameMessageReader.Stop();
            };


            //this.chat.NewMessage = x =>
            //{
            //    GameMessages.Insert(0, x);
            //};
        }

        public override void OnPreferencesChanged() {
            OnPropertyChanged("EnableGameScripts");

            base.OnPreferencesChanged();
        }

        private void PlayerTabsOnMouseLeave(object sender, MouseEventArgs mouseEventArgs)
        {
            playerTabs.Background = _offBackBrush;
        }

        private void PlayerTabsOnMouseEnter(object sender, MouseEventArgs mouseEventArgs)
        {
            playerTabs.Background = _backBrush;
        }

        private void ChatOnMouseLeave(object sender, MouseEventArgs mouseEventArgs)
        {
            chat.Background = _offBackBrush;
        }

        private void ChatOnMouseEnter(object sender, MouseEventArgs mouseEventArgs)
        {
            chat.Background = _backBrush;
        }

        private void OnLoaded(object sen, RoutedEventArgs routedEventArgs)
        {
            this.Loaded -= OnLoaded;
            _fadeIn = (Storyboard)Resources["ImageFadeIn"];
            _fadeOut = (Storyboard)Resources["ImageFadeOut"];

            // I think this is the thing that previews a card if you hover it.
            cardViewer.Source = Extentions.StringExtensionMethods.BitmapFromUri(new Uri(GameEngine.Definition.CardSize.Back));
            //if (GameEngine.Definition.CardCornerRadius > 0)
            cardViewer.Clip = new RectangleGeometry();
            AddHandler(CardControl.CardHoveredEvent, new CardEventHandler(CardHovered));
            AddHandler(CardRun.ViewCardModelEvent, new EventHandler<CardModelEventArgs>(ViewCardModel));

            Loaded += (sender, args) => Keyboard.Focus(table);
            // Solve various issues, like disabled menus or non-available keyboard shortcuts

            GroupControl.groupFont = new FontFamily("Segoe UI");
            GroupControl.fontsize = Prefs.ContextFontSize;
            chat.output.FontFamily = new FontFamily("Segoe UI");
            chat.output.FontSize = Prefs.ChatFontSize;
            chat.watermark.FontFamily = new FontFamily("Segoe UI");
            chat.watermark.FontSize = Prefs.ContextFontSize;

            // Apply game defined fonts
            if (Prefs.UseGameFonts)
            {
                chat.output.SetFont(GameEngine.Definition.ChatFont);
                chat.watermark.SetFont(GameEngine.Definition.ContextFont);

                GroupControl.groupFont = new FontFamily(chat.watermark.FontFamily.Source);
                if (GameEngine.Definition.ContextFont?.Size > 0)
                {
                    GroupControl.fontsize = GameEngine.Definition.ContextFont.Size;
                }
            }
            Log.Info(string.Format("Checking if the loaded game has boosters for limited play."));
            int setsWithBoosterCount = GameEngine.Definition.Sets().Where(x => x.Packs.Count() > 0).Count();
            Log.Info(string.Format("Found #{0} sets with boosters.", setsWithBoosterCount));
            if (setsWithBoosterCount == 0)
            {
                LimitedGameMenuItem.Visibility = Visibility.Collapsed;
                Log.Info("Hiding limited play in the menu.");
            }
            Log.Info("Checking if the loaded game has a Decks folder for prebuilt decks.");
            if (!Directory.Exists(Path.Combine(GameEngine.Definition.InstallPath, "Decks")))
            {
                Log.Info("No Decks folder found, hiding Load Prebuilt Decks in the menu.");
                PrebuiltDeckMenuItem.Visibility = Visibility.Collapsed;
            };
            //SubTimer.Start();

            if (!X.Instance.Debug)
            {
                // Show the Scripting console in dev only
                if (Application.Current.Properties["ArbitraryArgName"] == null) return;
                string fname = Application.Current.Properties["ArbitraryArgName"].ToString();
                if (fname != "/developer") return;
            }

        }

        private void InitializePlayerSummary(object sender, EventArgs e)
        {
            var textBlock = (TextBlock)sender;
            var player = textBlock.DataContext as Player;
//            if (player != null && player.IsGlobalPlayer)
//            {
//                textBlock.Visibility = Visibility.Collapsed;
//                return;
//            }
            string format;
            if (player != null && player.IsGlobalPlayer)
            {
                format = GameEngine.Definition.GlobalPlayer.IndicatorsFormat;
            }
            else
            {
                format = GameEngine.Definition.Player.IndicatorsFormat;
            }

            if (format == null)
            {
                textBlock.Visibility = Visibility.Collapsed;
                return;
            }

            var multi = new MultiBinding();
            int placeholder = 0;
            format = Regex.Replace(format, @"{#([^}]*)}", delegate(Match match)
                                                              {
                                                                  string name = match.Groups[1].Value;
                                                                  if (player != null)
                                                                  {
                                                                      Counter counter =
                                                                          player.Counters.FirstOrDefault(
                                                                              c => c.Name == name);
                                                                      if (counter != null)
                                                                      {
                                                                          multi.Bindings.Add(new Binding("Value") { Source = counter });
                                                                          return "{" + placeholder++ + "}";
                                                                      }
                                                                  }
                                                                  if (player != null)
                                                                  {
                                                                      Group group =
                                                                          player.IndexedGroups.FirstOrDefault(
                                                                              g => g != null && g.Name == name);
                                                                      if (@group != null)
                                                                      {
                                                                          multi.Bindings.Add(new Binding("Count") { Source = @group.Cards });
                                                                          return "{" + placeholder++ + "}";
                                                                      }
                                                                  }
                                                                  return "?";
                                                              });
            multi.StringFormat = format;
            textBlock.SetBinding(TextBlock.TextProperty, multi);
        }

        protected void Close(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (this.PreGameLobby.Visibility == Visibility.Visible) return;
            //GameLogWindow.RealClose();
            //SubTimer.Stop();
            //SubTimer.Elapsed -= this.SubTimerOnElapsed;
            if (IsRealClosing == false)
                Close();
        }

        public void ShowGameLog(object sender, RoutedEventArgs routedEventArgs)
        {
            if (this.PreGameLobby.Visibility == Visibility.Visible) return;
        }

        private bool IsRealClosing = false;

        protected override void OnClosing(CancelEventArgs e)
        {
            if (TopMostMessageBox.Show(
                "Are you sure you want to quit the game?",
                "Octgn",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
                e.Cancel = true;
            if (e.Cancel == false)
            {
                IsRealClosing = true;
            }
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            WindowManager.PlayWindow = null;
			X.Instance.Try(()=>GameEngine.Client?.Rpc?.Leave(Player.LocalPlayer));

            GameEngine.Client.Shutdown();

            GameEngine.End();
        }

        public bool TryClose()
        {
            try
            {
                this.Close();
                return IsRealClosing;
            }
            catch(Exception ex)
            {
                Log.Warn(ex.Message, ex);
            }
            return false;
        }

        private void Open(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            LoadDeck(GameEngine.Definition.GetDefaultDeckPath());
        }

        private void OpenPrebuilt(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            LoadDeck(Path.Combine(GameEngine.Definition.InstallPath, "Decks"));
        }

        private void LoadDeck(string path)
        {

            if (this.PreGameLobby.Visibility == Visibility.Visible) return;
            if (Player.LocalPlayer.Spectator || GameEngine.IsReplay) return;

            // Show the dialog to choose the file

            var deckpath = new OpenFileDialog
            {
                Filter = "Octgn deck files (*.o8d) | *.o8d",
                InitialDirectory = path
            };
            //deckpath.InitialDirectory = Program.Game.Definition.DecksPath;
            if (deckpath.ShowDialog() != true) return;

            // Try to load the file contents
            try
            {
                var game = GameManager.Get().GetById(GameEngine.Definition.Id);
                var newDeck = new Deck().Load(game, deckpath.FileName);
                //DataNew.Entities.Deck newDeck = Deck.Load(ofd.FileName,
                //                         Program.GamesRepository.Games.First(g => g.Id == Program.Game.Definition.Id));
                // Load the deck into the game
                GameEngine.LoadDeck(newDeck, false);
                if (!String.IsNullOrWhiteSpace(newDeck.Notes))
                {
                    this.table.AddNote(100, 0, newDeck.Notes);
                }
            }
            catch (DeckException ex)
            {
                TopMostMessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.Show("Octgn couldn't load the deck.\r\nDetails:\r\n\r\n" + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LimitedGame(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (this.PreGameLobby.Visibility == Visibility.Visible) return;
            if (LimitedDialog.Singleton == null)
                new LimitedDialog(GameEngine) { Owner = this }.Show();
            else
                LimitedDialog.Singleton.Activate();
        }

        private void ToggleFullScreen(object sender, RoutedEventArgs e)
        {
            if (this.PreGameLobby.Visibility == Visibility.Visible) return;
            if (IsFullScreen)
            {
                Topmost = false;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Normal;
                //menuRow.Height = GridLength.Auto;
                this.TitleBarVisibility = Visibility.Visible;
            }
            else
            {
                //Topmost = true;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                //menuRow.Height = new GridLength(2);
                this.TitleBarVisibility = Visibility.Collapsed;
            }
            IsFullScreen = !IsFullScreen;
        }

        private void ResetScreen(object sender, RoutedEventArgs e)
        {
            table.ResetScreen();
        }

        private void ResetGame(object sender, RoutedEventArgs e)
        {
            if (this.PreGameLobby.Visibility == Visibility.Visible) return;
            if (GameEngine.Definition.Events.ContainsKey("OverrideGameReset") )
            {
                GameEngine.EventProxy.OverrideGameReset_3_1_0_2();
                return;
            }
            // Prompt for a confirmation
            if (MessageBoxResult.Yes ==
                TopMostMessageBox.Show("The current game will end. Are you sure you want to continue?",
                                "Confirmation", MessageBoxButton.YesNo))
            {
                GameEngine.Client.Rpc.ResetReq();
            }
        }

        protected void MouseEnteredMenu(object sender, RoutedEventArgs e)
        {
            if (!IsFullScreen) return;
            menuRow.Height = GridLength.Auto;
        }

        protected void MouseLeftMenu(object sender, RoutedEventArgs e)
        {
            if (!IsFullScreen) return;
            menuRow.Height = new GridLength(2);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (_currentCard != null && _currentCardUpStatus && (Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) > 0 && Prefs.ZoomOption == Prefs.ZoomType.ProxyOnKeypress && _newCard)
            {
                var img = _currentCard.GetBitmapImage(_currentCardUpStatus, true);
                ShowCardPicture(_currentCard, img);
                _newCard = false;
            }

            if (e.OriginalSource is TextBox)
                return; // Do not tinker with the keyboard events when the focus is inside a textbox

            if (e.IsRepeat)
                return;
            IInputElement mouseOver = Mouse.DirectlyOver;
            var te = new TableKeyEventArgs(this, e);
            if (mouseOver != null) mouseOver.RaiseEvent(te);
            if (te.Handled) return;

            // If the event was unhandled, check if there's a selection and try to apply a shortcut action to it
            if (!Selection.IsEmpty() && Selection.Source.CanManipulate())
            {
                ActionShortcut match =
                    Selection.Source.CardShortcuts.FirstOrDefault(
                        shortcut => shortcut.Key.Matches(this, te.KeyEventArgs));
                if (match != null)
                {
                    if (match.ActionDef.AsAction().Execute != null)
                        ScriptEngine.ExecuteOnCards(match.ActionDef.AsAction().Execute, Selection.Cards);
                    else if (match.ActionDef.AsAction().BatchExecute != null)
                        ScriptEngine.ExecuteOnBatch(match.ActionDef.AsAction().BatchExecute, Selection.Cards);
                    e.Handled = true;
                    return;
                }
            }

            // The event was still unhandled, try all groups, starting with the table
            if (table == null) return;
            table.RaiseEvent(te);
            if (te.Handled) return;
            foreach (Group g in Player.LocalPlayer.Groups.Where(g => g.CanManipulate()))
            {
                ActionShortcut a = g.GroupShortcuts.FirstOrDefault(shortcut => shortcut.Key.Matches(this, e));
                if (a == null) continue;
                if (a.ActionDef.AsAction().Execute != null)
                    ScriptEngine.ExecuteOnGroup(a.ActionDef.AsAction().Execute, g);
                e.Handled = true;
                return;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if(e.Key == Key.F9) {
                GameEngine.DeckStats.IsVisible = !GameEngine.DeckStats.IsVisible;

                e.Handled = true;

                return;
            }
            if (_currentCard != null && _currentCardUpStatus && (Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) == 0 && Prefs.ZoomOption == Prefs.ZoomType.ProxyOnKeypress)
            {
                var img = _currentCard.GetBitmapImage(_currentCardUpStatus);
                ShowCardPicture(_currentCard, img);
                _newCard = true;
            }
        }

        private void CardHovered(object sender, CardEventArgs e)
        {
            _currentCard = e.Card;
            _currentCardUpStatus = false;
            if (e.Card == null && e.CardModel == null)
            {
                _fadeOut.Begin(outerCardViewer, HandoffBehavior.SnapshotAndReplace);
                _fadeOut.Begin(outerCardViewer2, HandoffBehavior.SnapshotAndReplace);
            }
            else
            {
                Point mousePt = Mouse.GetPosition(table);
                if (mousePt.X < 0.4 * clientArea.ActualWidth)
                    outerCardViewer.HorizontalAlignment = cardViewer.HorizontalAlignment = outerCardViewer2.HorizontalAlignment = cardViewer2.HorizontalAlignment = HorizontalAlignment.Right;
                else if (mousePt.X > 0.6 * clientArea.ActualWidth)
                    outerCardViewer.HorizontalAlignment = cardViewer.HorizontalAlignment = outerCardViewer2.HorizontalAlignment = cardViewer2.HorizontalAlignment = HorizontalAlignment.Left;

                var ctrl = e.OriginalSource as CardControl;
                if (e.Card != null)
                {

                    bool up = ctrl != null && ctrl.IsAlwaysUp
                            || (e.Card.FaceUp || e.Card.PeekingPlayers.Contains(Player.LocalPlayer));

                    _currentCardUpStatus = up;

                    var img = e.Card.GetBitmapImage(up);
                    double width = ShowCardPicture(e.Card, img);
                    _newCard = true;

                    if (up && Prefs.ZoomOption == Prefs.ZoomType.OriginalAndProxy && !e.Card.IsProxy())
                    {
                        var proxyImg = e.Card.GetBitmapImage(true, true);
                        ShowSecondCardPicture(e.Card, proxyImg, width);
                    }
                }
                else
                {
                    var img = ImageUtils.CreateFrozenBitmap(GameEngine, new Uri(e.CardModel.GetPicture()));
                    this.ShowCardPicture(e.Card, img);
                }
            }
        }

        private void ShowSecondCardPicture(Card card, BitmapSource img, double requiredMargin)
        {
            var maxWidth = this.ActualWidth * 0.20;
            cardViewer2.Height = img.PixelHeight;
            cardViewer2.Width = img.PixelWidth > maxWidth ? maxWidth : img.PixelWidth;
            cardViewer2.Source = img;

            if (cardViewer2.HorizontalAlignment == HorizontalAlignment.Left)
            {
                outerCardViewer2.Margin = new Thickness(requiredMargin + 15, 10, 10, 10);
            }
            else
            {
                outerCardViewer2.Margin = new Thickness(10, 10, requiredMargin + 15, 10);
            }

            _fadeIn.Begin(outerCardViewer2, HandoffBehavior.SnapshotAndReplace);

            if (cardViewer2.Clip == null) return;
            var clipRect = ((RectangleGeometry)cardViewer2.Clip);
            double height = Math.Min(cardViewer2.MaxHeight, cardViewer2.Height);
            double width = cardViewer2.Width * height / cardViewer2.Height;
            clipRect.Rect = new Rect(new Size(width, height));
            //clipRect.RadiusX = clipRect.RadiusY = GameEngine.Definition.CardCornerRadius * height / card.Size.Height;
            clipRect.RadiusX = clipRect.RadiusY = card.RealCornerRadius * height / card.RealHeight;
        }

        private void ViewCardModel(object sender, CardModelEventArgs e)
        {
            if (e.CardModel == null)
                _fadeOut.Begin(outerCardViewer, HandoffBehavior.SnapshotAndReplace);
            else
                ShowCardPicture(e.CardModel.GameCard, ImageUtils.CreateFrozenBitmap(GameEngine, new Uri(e.CardModel.Card.GetPicture())));
        }

        private double ShowCardPicture(Card card, BitmapSource img)
        {
            //var maxWidth = this.ActualWidth*0.20;
            cardViewer.Height = img.PixelHeight;
            cardViewer.Width = img.PixelWidth;
            //cardViewer.Width = img.PixelWidth > maxWidth ? maxWidth : img.PixelWidth;
            cardViewer.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
            cardViewer.Source = img;

            _fadeIn.Begin(outerCardViewer, HandoffBehavior.SnapshotAndReplace);

            double height = Math.Min(cardViewer.MaxHeight, cardViewer.Height);
            double width = cardViewer.Width * height / cardViewer.Height;
            if (img.PixelWidth > img.PixelHeight)
            {
                width = Math.Min(cardViewer.MaxWidth, cardViewer.Width);
                height = cardViewer.Height * width / cardViewer.Width;
            }

            if (cardViewer.Clip == null) return width;

            var clipRect = ((RectangleGeometry)cardViewer.Clip);
            clipRect.Rect = new Rect(new Size(width, height));

            if (card == null)
                clipRect.RadiusX = clipRect.RadiusY = GameEngine.Definition.CardSize.CornerRadius * height / GameEngine.Definition.CardSize.Height;
            else
            {
                clipRect.RadiusX = clipRect.RadiusY = card.RealCornerRadius*height/card.RealHeight;
            }

            return width;
        }

        private void NextTurnClicked(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var targetPlayer = (Player)btn.DataContext;
            if (GameEngine.ActivePlayer == null || GameEngine.ActivePlayer == Player.LocalPlayer)
            {
                if (GameEngine.Definition.Events.ContainsKey("OverrideTurnPassed"))
                {
                    GameEngine.EventProxy.OverrideTurnPassed_3_1_0_2(targetPlayer);
                    return;
                }
                GameEngine.Client.Rpc.NextTurn(targetPlayer, true, false);
            }
            else
            {
                GameEngine.StopTurn = !GameEngine.StopTurn;
                GameEngine.Client.Rpc.StopTurnReq(GameEngine.TurnNumber, GameEngine.StopTurn);
            }
        }

        public void PhaseClicked(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var phase = (Phase)btn.DataContext;
            if (GameEngine.Definition.Events.ContainsKey("OverridePhaseClicked"))
            {
                GameEngine.EventProxy.OverridePhaseClicked_3_1_0_2(phase.Name, phase.Id);
                return;
            }
            phase.Hold = !phase.Hold;
            GameEngine.Client.Rpc.StopPhaseReq(phase.Id, phase.Hold);
        }

        private bool LockPhaseList = false;

        private void ShowPhaseStoryboard(object sender, MouseEventArgs e)
        {
            if (!LockPhaseList)
            {
                Storyboard sb = (Storyboard)PhaseControl.FindResource("ShowPhaseStoryboard");
                sb.Begin(PhaseControl);
            }
        }

        private void HidePhaseStoryboard(object sender, MouseEventArgs e)
        {
            if (!LockPhaseList)
            {
                Storyboard sb = (Storyboard)PhaseControl.FindResource("HidePhaseStoryboard");
                sb.Begin(PhaseControl);
            }
        }

        private void LockPhaseStoryboard(object sender, MouseEventArgs e)
        {
            LockPhaseList = !LockPhaseList;
        }

        private void ActivateChat(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            if (this.PreGameLobby.Visibility == Visibility.Visible) return;
            chat.FocusInput();
        }

        private void ShowAboutWindow(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (this.PreGameLobby.Visibility == Visibility.Visible) return;
            //var wnd = new AboutWindow() { Owner = this };
            //wnd.ShowDialog();
            GameEngine.FireLaunchUrl(AppConfig.WebsitePath);
        }

        private void ConsoleClicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (this.PreGameLobby.Visibility == Visibility.Visible) return;
            if (Player.LocalPlayer.Spectator == true || GameEngine.IsReplay) return;

            if (Program.DeveloperMode)
            {
                var wnd = new DeveloperWindow(GameEngine) { Owner = this };
                wnd.Show();
            }
        }

        internal void ShowBackstage(UIElement ui)
        {
            Dispatcher.Invoke(new Action(() =>
                {
                    this.table.Visibility = Visibility.Collapsed;
                    this.wndManager.Visibility = Visibility.Collapsed
                        ;
                    this.backstage.Child = ui;
					this.PhaseControl.Visibility = Visibility.Collapsed;
					this.DeckStats.Visibility = Visibility.Collapsed;
                    this.LimitedBackstage.Visibility = Visibility.Visible;
                    backstage.Visibility = Visibility.Visible;
                    this.Menu.IsEnabled = false;
                    this.Menu.Visibility = Visibility.Collapsed;
                }));
        }

        internal void HideBackstage()
        {

            table.Visibility = Visibility.Visible;
            wndManager.Visibility = Visibility.Visible;
            LimitedBackstage.Visibility = Visibility.Collapsed;
            backstage.Visibility = Visibility.Collapsed;
			this.PhaseControl.Visibility = Visibility.Visible;
			this.DeckStats.Visibility = Visibility.Visible;
			this.Menu.IsEnabled = true;
            this.Menu.Visibility = Visibility.Visible;
            backstage.Child = null;

            Keyboard.Focus(table); // Solve various issues, like disabled menus or non-available keyboard shortcuts
        }

        #region Limited

        protected void LimitedSaveClicked(object sender, EventArgs e)
        {
            if (this.PreGameLobby.Visibility == Visibility.Visible) return;
            var sfd = new SaveFileDialog
                          {
                              AddExtension = true,
                              Filter = "Octgn decks|*.o8d",
                              InitialDirectory = GameEngine.Definition.GetDefaultDeckPath()
                          };
            if (!sfd.ShowDialog().GetValueOrDefault()) return;

            var dlg = backstage.Child as PickCardsDialog;
            try
            {
                if (dlg != null)
                    dlg.LimitedDeck.Save(GameManager.Get().GetById(GameEngine.Definition.Id), sfd.FileName);
                else
                    GameEngine.LoadedCards.Save(GameManager.Get().GetById(GameEngine.Definition.Id), sfd.FileName);

            }
            catch (UserMessageException ex)
            {
                TopMostMessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void LimitedOkClicked(object sender, EventArgs e)
        {
            if (backstage.Child is PickCardsDialog dlg) GameEngine.LoadDeck(dlg.LimitedDeck, true);
            HideBackstage();
        }

        protected void LimitedCancelClicked(object sender, EventArgs e)
        {
            GameEngine.Client.Rpc.CancelLimitedReq();
            HideBackstage();
        }
        private void LimitedAddPacks(object sender, RoutedEventArgs e)
        {
            LimitedDialog ld;
            e.Handled = true;
            if (LimitedDialog.Singleton == null)
            {
                ld = new LimitedDialog(GameEngine) { Owner = this };
                ld.Show();
            }
            else
            {
                ld = LimitedDialog.Singleton;
                ld.Activate();
            }
            ld.showAddCardsCombo(true);
        }
        private void LimitedLoadCardPool(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var dlg = backstage.Child as PickCardsDialog;
            var loadDirectory = GameEngine.Definition.GetDefaultDeckPath();


            var ofd = new OpenFileDialog
            {
                Filter = "Octgn deck files (*.o8d) | *.o8d",
                InitialDirectory = loadDirectory
            };
            if (ofd.ShowDialog() != true) return;
            // Try to load the file contents
            try
            {
                var game = GameManager.Get().GetById(GameEngine.Definition.Id);
                var newDeck = new Deck().Load(game, ofd.FileName);
                dlg.OpenCardPool(newDeck);
            }
            catch (DeckException ex)
            {
                TopMostMessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.Show("Octgn couldn't load the deck.\r\nDetails:\r\n\r\n" + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private void LoadDocument(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (this.PreGameLobby.Visibility == Visibility.Visible) return;
            var s = sender as FrameworkElement;
            if (s == null) return;
            var document = s.DataContext as Document;
            if (document == null) return;
            var wnd = new RulesWindow(document) { Owner = this };
            wnd.Show();

        }

        private void KickPlayer(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var s = sender as FrameworkElement;
            if (s == null) return;
            var player = s.DataContext as Player;
            if (player == null) return;
            if (GameEngine.IsReplay) return;
            if (player == Player.LocalPlayer)
            {
                throw new UserMessageException("You cannot kick yourself.");
            }
            GameEngine.Client.Rpc.Boot(player, "The host has booted them from the game.");
        }

        private bool chatIsMaxed = false;

        private bool chatVisible;
        private void ChatSplitDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (chatIsMaxed)
            {
                ChatGridEmptyPart.Height = new GridLength(100, GridUnitType.Star);
                ChatGridChatPart.Height = new GridLength(playerTabs.ActualHeight);
                ChatSplit.DragIncrement = 1;
                chatIsMaxed = false;
            }
            else
            {
                ChatGridEmptyPart.Height = new GridLength(0, GridUnitType.Star);
                ChatGridChatPart.Height = new GridLength(100, GridUnitType.Star);
                ChatSplit.DragIncrement = 10000;
                chatIsMaxed = true;
            }
        }

        private void MenuChangeBackgroundFromFileClick(object sender, RoutedEventArgs e)
        {
            if (this.PreGameLobby.Visibility == Visibility.Visible) return;
            var sub = SubscriptionModule.Get().IsSubscribed;
            if (!sub)
            {
                TopMostMessageBox.Show("You must be subscribed to do that.", "OCTGN", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var fo = new OpenFileDialog
            {
                Filter = "All Images|*.BMP;*.JPG;*.JPEG;*.PNG|BMP Files: (*.BMP)|*.BMP|JPEG Files: (*.JPG;*.JPEG)|*.JPG;*.JPEG|PNG Files: (*.PNG)|*.PNG"
            };
            if ((bool)fo.ShowDialog())
            {
                if (File.Exists(fo.FileName))
                {
                    this.table.SetBackground(fo.FileName, "uniformToFill");
                    Prefs.DefaultGameBack = fo.FileName;
                }
            }
        }

        private void MenuChangeBackgroundReset(object sender, RoutedEventArgs e)
        {
            if (this.PreGameLobby.Visibility == Visibility.Visible) return;
            this.table.ResetBackground();
            Prefs.DefaultGameBack = "";
        }

        private void SubscribeNavigate(object sender, RequestNavigateEventArgs e)
        {
            var url = SubscriptionModule.Get().GetSubscribeUrl(new SubType() { Description = "", Name = "" });
            if (url != null)
            {
                GameEngine.FireLaunchUrl(url);
            }
        }

        private void ButtonWaitingForPlayersCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public new event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void GridSplitter_DragDelta(object sender, DragDeltaEventArgs e)
        {
            playerArea.MinHeight = playerTabs.DesiredSize.Height;
        }

        private void CardList_Click(object sender, RoutedEventArgs e) {
            GameEngine.DeckStats.IsVisible = !GameEngine.DeckStats.IsVisible;
        }

        private bool replayDragStarted = false;

        private void ReplaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (replayDragStarted) return;
            var val = (long)e.NewValue;

            if (!this.IsLoaded) return;

            ReplayEngine.FastForwardTo(TimeSpan.FromTicks(val));
        }

        private void ReplaySlider_DragStarted(object sender, DragStartedEventArgs e) {
            replayDragStarted = true;
        }

        private void ReplaySlider_DragCompleted(object sender, DragCompletedEventArgs e) {
            replayDragStarted = false;
            var val = (long)((Slider)sender).Value;

            ReplayEngine.FastForwardTo(TimeSpan.FromTicks(val));
        }

        private void ReplaySpeed_Click(object sender, RoutedEventArgs e) {
            ReplayEngine.ToggleSpeed();
        }

        private void ReplayPlayButton_Click(object sender, RoutedEventArgs e) {
            ReplayEngine.TogglePlay();
        }

        private void ReplayPreviousEventButton_Click(object sender, RoutedEventArgs e) {
            ReplayEngine.RewindToPreviousEvent();
        }

        private void ReplayNextEventButton_Click(object sender, RoutedEventArgs e) {
            ReplayEngine.FastForwardToNextEvent();
        }

        private void ReplayPreviousTurnButton_Click(object sender, RoutedEventArgs e) {
            ReplayEngine.RewindToPreviousTurn();
        }

        private void ReplayNextTurnButton_Click(object sender, RoutedEventArgs e) {
            ReplayEngine.FastForwardToNextTurn();
        }

        private void ChatSplit_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (ChatGridChatPart.ActualHeight <= ChatGridChatPart.MinHeight && e.VerticalChange >= 0) // + VerticalChange means shrinking chat box
            {
                ChatGridChatPart.Height = new GridLength(0);
                playerAreaGridSplitter.Margin = new Thickness(300,0,0,0);
            }
            else
            {
                playerAreaGridSplitter.Margin = new Thickness(0);
            }
        }
    }

    internal class CanPlayConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var turnPlayer = values[0] as Player;
            var player = values[1] as Player;
            var stopped = values[2] as bool?;

            string styleKey;
            if (player == Player.GlobalPlayer)
                styleKey = "InvisibleButton";
            else if (turnPlayer == null)
                styleKey = "PlayButton";
            else if (turnPlayer == Player.LocalPlayer)
                styleKey = "PlayButton";
            else if (turnPlayer != player)
                styleKey = "InvisibleButton";
            else if (stopped == true)
                styleKey = "HeldPauseButton";
            else
                styleKey = "PauseButton";
            return Application.Current.FindResource(styleKey);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class ScaleConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            var d = (double)value;
            double scale = double.Parse((string)parameter, CultureInfo.InvariantCulture);
            return d * scale;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class GameMessageTextBlock : TextBlock
    {
        public static readonly DependencyProperty GameMessageProperty =
            DependencyProperty.Register("GameMessage", typeof(IGameMessage), typeof(GameMessageTextBlock), new PropertyMetadata(default(IGameMessage), OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = sender as GameMessageTextBlock;
            if (textBlock == null) return;
            if (textBlock.Inlines.FirstInline != null) textBlock.Inlines.Remove(textBlock.Inlines.FirstInline);
            var b = Gui.ChatControl.GameMessageToBlock(textBlock.GameMessage) as System.Windows.Documents.Section;
            if (b == null) return;

            foreach (var block in b.Blocks.OfType<System.Windows.Documents.Paragraph>().ToArray())
            {
                foreach (var i in block.Inlines.ToArray())
                {
                    textBlock.Inlines.Add(i);
                }
            }

            textBlock.Margin = new Thickness(10, 0, 10, 0);
            textBlock.VerticalAlignment = VerticalAlignment.Center;
        }

        public IGameMessage GameMessage
        {
            get
            {
                return (IGameMessage)GetValue(GameMessageProperty);
            }
            set
            {
                SetValue(GameMessageProperty, value);
            }
        }
    }

    internal class ValueAdditionConveter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double.TryParse((string)parameter, out double temp);
            return (double)value + temp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}