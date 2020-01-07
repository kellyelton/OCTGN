/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Data;
using Microsoft.Win32;
using Octgn.Core.DataExtensionMethods;
using log4net;
using Octgn.Core.Play;
using Octgn.Extentions;
using Octgn.Library;
using Octgn.Utils;
using System.Collections.Generic;

namespace Octgn.Play.Gui
{
    partial class ChatControl : INotifyPropertyChanged
    {
        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private bool hideErrors;

        private bool hideDebug;

        public bool IgnoreMute { get; set; }

        public bool ShowInput {
            get {
                bool result;

                if (GameEngine == null) {
                    result = false;
                } else if (GameEngine.Spectator) {
                    result = GameEngine.Settings.MuteSpectators == false;
                } else {
                    result = GameEngine.IsReplay == false;
                }

                return result;
            }
        }

        public bool DevMode => GameEngine?.IsDeveloperMode ?? false;

        public bool HideErrors
        {
            get
            {
                return this.hideErrors;
            }
            set
            {
                if (value.Equals(this.hideErrors))
                {
                    return;
                }
                this.output.Document.Blocks.Clear();
                _gameLogReader?.Reset();
                this.hideErrors = value;
                this.OnPropertyChanged("HideErrors");
            }
        }

        public bool AutoScroll
        {
            get
            {
                return this.autoScroll;
            }
            set
            {
                if (value == this.autoScroll) return;
                this.autoScroll = value;
                OnPropertyChanged("AutoScroll");
            }
        }


        public bool HideDebug
        {
            get
            {
                return this.hideDebug;
            }
            set
            {
                if (value.Equals(this.hideDebug))
                {
                    return;
                }
                this.output.Document.Blocks.Clear();
                _gameLogReader?.Reset();
                this.hideDebug = value;
                OnPropertyChanged("HideDebug");
            }
        }

        public Action<IGameMessage> NewMessage;

        public double ChatFontSize
        {
            get
            {
                var ret = Core.Prefs.ChatFontSize;
                return ret;
            }
        }

        public GameEngine GameEngine {
            get { return (GameEngine)GetValue(GameEngineProperty); }
            set { SetValue(GameEngineProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GameEngine.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GameEngineProperty =
            DependencyProperty.Register(nameof(GameEngine), typeof(GameEngine), typeof(ChatControl), new PropertyMetadata(GameEngine_Changed));

        private static void GameEngine_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chatControl = (ChatControl)d;

            chatControl.Configure((GameEngine)e.OldValue, (GameEngine)e.NewValue);
            chatControl.OnPropertyChanged(nameof(ShowInput));
            chatControl.OnPropertyChanged(nameof(DevMode));
        }

        private GameLogReader _gameLogReader;

        public ChatControl() {
            AutoScroll = true;

            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this)) return;

            (output.Document.Blocks.FirstBlock).Margin = new Thickness();
        }

        private void Configure(GameEngine previousGameEngine, GameEngine gameEngine) {
            _gameLogReader?.Dispose();

            if (previousGameEngine != null) {
                previousGameEngine.Settings.PropertyChanged -= GameSettings_PropertyChanged;
            }

            if (gameEngine != null) {
                _gameLogReader = new GameLogReader(gameEngine.GameLog);
                _gameLogReader.Start(GameLogsAdded);
                gameEngine.Settings.PropertyChanged += GameSettings_PropertyChanged;
            }

        }

        private void GameSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            OnPropertyChanged(nameof(ShowInput));
        }

        public static Block GameMessageToBlock(IGameMessage m)
        {
            if (m == null)
                return null;

			if (m is PlayerEventMessage)
            {
                if (m.IsMuted) return null;
                var b = new GameMessageBlock(m);
                var p = new Paragraph();
                var prun = new Run(m.From + " ");
                prun.Foreground = m.From.Color.CacheToBrush();
                prun.FontWeight = FontWeights.Bold;
                p.Inlines.Add(prun);

                var chatRun = MergeArgsv2(m.Message, m.Arguments);
                chatRun.Foreground = new SolidColorBrush(m.From.Color);
                p.Inlines.Add(chatRun);

                b.Blocks.Add(p);

                return b;
            }
            else if (m is ChatMessage)
            {
                if (m.IsMuted) return null;

                var p = new Paragraph();
                var b = new GameMessageBlock(m);

                var inline = new Span();

                inline.Foreground = m.From.Color.CacheToBrush();
				var chatRun = new Run("<" + m.From + "> ");
                chatRun.Foreground = m.From.Color.CacheToBrush();
				chatRun.FontWeight = FontWeights.Bold;
                inline.Inlines.Add(chatRun);

                inline.Inlines.Add(MergeArgsv2(m.Message, m.Arguments));

                p.Inlines.Add(inline);

                b.Blocks.Add(p);

                return b;
            }
            else if (m is WarningMessage)
            {
                if (m.IsMuted) return null;

                var b = new GameMessageBlock(m);
                b.Background = Brushes.LightGray;
                b.Padding = new Thickness(5);
                b.BorderBrush = Brushes.LightGray;
                b.Foreground = m.From.Color.CacheToBrush();
				var par = new Paragraph(MergeArgsv2(m.Message, m.Arguments));
                par.Margin = new Thickness(0);
                b.Blocks.Add(par);

                return b;
            }
            else if (m is SystemMessage)
            {
                if (m.IsMuted) return null;

                var p = new Paragraph();
                var b = new GameMessageBlock(m);
                var chatRun = MergeArgsv2(m.Message, m.Arguments);
                chatRun.Foreground = m.From.Color.CacheToBrush();
				p.Inlines.Add(chatRun);
                b.Blocks.Add(p);
                return b;
            }
            else if (m is NotifyMessage)
            {
                if (m.IsMuted) return null;

                var p = new Paragraph();
                var b = new GameMessageBlock(m);
                var chatRun = MergeArgsv2(m.Message, m.Arguments);
                chatRun.Foreground = m.From.Color.CacheToBrush();
				b.Blocks.Add(p);
                p.Inlines.Add(chatRun);
                return b;
            }
            else if (m is PhaseMessage)
            {
                if (m.IsMuted) return null;

                var brush = m.From.Color.CacheToBrush();

                var p = new Paragraph();
                var b = new GameMessageBlock(m);
                b.TextAlignment = TextAlignment.Center;
                b.Margin = new Thickness(2);

                var chatRun = new Run(string.Format(m.Message, m.Arguments));
                chatRun.Foreground = brush;
                chatRun.FontWeight = FontWeights.Bold;
                p.Inlines.Add(chatRun);

                var turnPlayer = (m as PhaseMessage).ActivePlayer;
                var prun = new Run(" " + turnPlayer + " ");
                prun.Foreground = turnPlayer == null ? m.From.Color.CacheToBrush() : turnPlayer.Color.CacheToBrush();
                prun.FontWeight = FontWeights.Bold;
                p.Inlines.Add(prun);

                b.Blocks.Add(p);

                return b;
            }
            else if (m is TurnMessage)
            {
                if (m.IsMuted) return null;

                var brush = m.From.Color.CacheToBrush();

				var p = new Paragraph();
                var b = new GameMessageBlock(m);
                b.TextAlignment = TextAlignment.Center;
                b.Margin = new Thickness(2);

                p.Inlines.Add(
                    new Line
                    {
                        X1 = 0,
                        X2 = 40,
                        Y1 = -4,
                        Y2 = -4,
                        StrokeThickness = 2,
                        Stroke = brush
                    });

                var chatRun = new Run(string.Format(m.Message, m.Arguments));
                chatRun.Foreground = brush;
                chatRun.FontWeight = FontWeights.Bold;
                p.Inlines.Add(chatRun);

                var turnPlayer = (m as TurnMessage).ActivePlayer;
                var prun = new Run(" " + turnPlayer + " ");
                prun.Foreground = turnPlayer == null ? m.From.Color.CacheToBrush() : turnPlayer.Color.CacheToBrush();
                prun.FontWeight = FontWeights.Bold;
                p.Inlines.Add(prun);

                p.Inlines.Add(
                    new Line
                    {
                        X1 = 0,
                        X2 = 40,
                        Y1 = -4,
                        Y2 = -4,
                        StrokeThickness = 2,
                        Stroke = brush
                    });

                b.Blocks.Add(p);

                return b;
            }
            else if (m is DebugMessage)
            {
                if (m.IsMuted) return null;
                var p = new Paragraph();
                var b = new GameMessageBlock(m);
                var chatRun = MergeArgsv2(m.Message, m.Arguments);
                chatRun.Foreground = m.From.Color.CacheToBrush();
				p.Inlines.Add(chatRun);
                b.Blocks.Add(p);
                return b;
            }
            else if (m is NotifyBarMessage)
            {
                if (m.IsMuted) return null;
                var p = new Paragraph();
                var b = new GameMessageBlock(m);
                var chatRun = MergeArgsv2(m.Message, m.Arguments);
                chatRun.Foreground = (m as NotifyBarMessage).MessageColor.CacheToBrush();
                p.Inlines.Add(chatRun);
                b.Blocks.Add(p);
                return b;
            }
            return null;
        }

        private void GameLogsAdded(IReadOnlyList<IGameMessage> obj) {
            Dispatcher.Invoke(new Action(() =>
            {
                var gotone = false;

                foreach (var gameLog in obj)
                {
                    if (gameLog is NotifyBarMessage)
                    {
                        continue;
                    }
                    if (gameLog is WarningMessage)
                    {
                        if (this.HideErrors)
                            continue;
                    }
                    if (gameLog is DebugMessage)
                    {
                        if (DevMode == false || this.HideDebug)
                            continue;
                    }

                    if (NewMessage != null)
                        NewMessage(gameLog);

                    var b = GameMessageToBlock(gameLog);
                    if (b != null)
                    {
                        gotone = true;
                        this.output.Document.Blocks.Add(b);
                    }
                }

                if (gotone && AutoScroll)
                {
                    X.Instance.Try(this.output.ScrollToEnd);
                }
            }));
        }

        private bool autoScroll;

        //Like a boss
        public static Inline MergeArgsv2(string format, object[] arguments)
        {
            var args = arguments.ToList();
            var ret = new Span();
            bool foundLeft = false;
            int tStart = 0;
            var sb = new StringBuilder();
            var numString = "";
            var i = 0;

            // Replace any instances of any players name with the goods.

            foreach (var p in Player.AllExceptGlobal.OrderByDescending(x=>x.ToString().Length))
            {
                if (format.Contains(p.Name))
                {
                    var ind = -1;
                    for (var a = 0; a < args.Count; a++)
                    {
                        if (args[a] == p)
                        {
                            ind = a;
                            break;
                        }
                    }
                    if (ind == -1)
                    {
                        ind = args.Count;
                        args.Add(p);
                    }
                    format = format.Replace(p.Name, "{" + ind + "}");
                }
            }

            // Now we replace the format shit with objects like a boss.
            foreach (var c in format)
            {
                sb.Append(c);
                if (c == '{')
                {
                    numString = "";
                    if (foundLeft)
                    {
                        foundLeft = false;
                    }
                    else
                    {
                        foundLeft = true;
                        tStart = 0;
                    }
                }
                else if (c.IsANumber() && foundLeft)
                {
                    numString += c;
                    tStart++;
                }
                else if (c == '}')
                {
                    if (foundLeft && numString.IsANumber())
                    {
                        // Add our current string to the ret inline
                        if (sb.Length > 0)
                        {
                            var str = sb.ToString();
                            str = str.Substring(0, str.Length - (tStart + 2));
                            if (str.Length > 0)
                            {
                                var il = new Run(str);
                                ret.Inlines.Add(il);
                            }
                            sb.Clear();
                            i = -1;
                        }
                        int num = int.Parse(numString);

                        var arg = args[num];

                        var card = arg as ChatCard;

                        if ((card != null && card.Card != null) || arg is IPlayPlayer)
                        {
                            if (arg is IPlayPlayer)
                            {
                                ret.Inlines.Add(new PlayerRun((arg as IPlayPlayer)));
                            }
                            else
                            {
                                ret.Inlines.Add(new CardRun(card));
                            }
                        }
                        else
                            sb.Append(arg == null ? "[?]" : arg.ToString());
                    }
                    foundLeft = false;
                }
                else
                {
                    foundLeft = false;
                }
                i++;
            }
            if (sb.Length > 0)
            {
                var str = sb.ToString();
                //if(tStart > 0)
                //	str = str.Substring(0, tStart);
                var il = new Run(str);
                ret.Inlines.Add(il);
                sb.Clear();
            }
            return ret;
        }

        public bool DisplayKeyboardShortcut
        {
            set { if (value) watermark.Text += "  (Ctrl+T)"; }
        }

        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    {
                        e.Handled = true;

                        string msg = input.Text;
                        input.Clear();
                        if (string.IsNullOrEmpty(msg)) return;

                        GameEngine.Client.Rpc.ChatReq(msg);
                    }
                    break;
                case Key.Escape:
                    {
                        e.Handled = true;
                        input.Clear();
                        Window window = Window.GetWindow(this);
                        if (window != null)
                            ((UIElement)window.Content).MoveFocus(
                                new TraversalRequest(FocusNavigationDirection.First));
                    }
                    break;
            }
        }

        private void InputGotFocus(object sender, RoutedEventArgs e)
        {
            watermark.Visibility = Visibility.Hidden;
        }

        private void InputLostFocus(object sender, RoutedEventArgs e)
        {
            if (input.Text == "") watermark.Visibility = Visibility.Visible;
        }

        public void FocusInput()
        {
            input.Focus();
        }

        public void Save()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(this.Save));
                return;
            }
            try
            {
                var sfd = new SaveFileDialog { Filter = "Octgn Game Log (*.txt) | *.txt" };
                if (sfd.ShowDialog().GetValueOrDefault(false))
                {
                    var tr = new TextRange(output.Document.ContentStart, output.Document.ContentEnd);
                    using (var stream = sfd.OpenFile())
                    {
                        tr.Save(stream, DataFormats.Text);
                        stream.Flush();
                    }
                }

            }
            catch (Exception e)
            {
                Log.Warn("Save log error", e);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    internal class CardModelEventArgs : RoutedEventArgs
    {
        public readonly ChatCard CardModel;

        public CardModelEventArgs(ChatCard model, RoutedEvent routedEvent, object source)
            : base(routedEvent, source)
        {
            CardModel = model;
        }
    }

    internal class ChatCard
    {
        public DataNew.Entities.Card Card { get; private set; }
        public Card GameCard { get; private set; }

        private Action<DataNew.Entities.Card, Card> _updateAction;

        public ChatCard(CardIdentity ci)
        {
            this.Card = ci.Model;
        }

        public ChatCard(DataNew.Entities.Card model)
        {
            this.Card = model;
        }

        public void SetGameCard(Card card)
        {
            GameCard = card;
            GameCard.PropertyChanged += (x, y) =>
            {
                if (_updateAction != null)
                {
                    _updateAction(Card, GameCard);
                }
            };
        }

        public void SetCardModel(DataNew.Entities.Card model)
        {
            Debug.Assert(this.Card == null, "Cannot set the CardModel of a CardRun if it is already defined");
            this.Card = model;
            if (_updateAction != null)
                _updateAction.Invoke(model, GameCard);
        }

        public void UpdateCardText(Action<DataNew.Entities.Card, Card> action)
        {
            _updateAction = action;
        }

        public override string ToString()
        {
            if (this.Card == null)
                return "[?}";
            return this.Card.PropertyName();
        }
    }

    internal class CardRun : Underline
    {
        public static readonly RoutedEvent ViewCardModelEvent = EventManager.RegisterRoutedEvent("ViewCardIdentity",
                                                                                                 RoutingStrategy.Bubble,
                                                                                                 typeof(
                                                                                                     EventHandler
                                                                                                     <CardModelEventArgs
                                                                                                     >),
                                                                                                 typeof(CardRun));

        private readonly ChatCard _card;

        public CardRun(ChatCard card)
            : base(new Run(card.ToString()))
        {
            this.FontWeight = FontWeights.Bold;
            this.Foreground = Brushes.DarkSlateGray;
            this.Cursor = Cursors.Hand;
            _card = card;
            _card.UpdateCardText((model, gamecard) =>
                {
                    (this.Inlines.FirstInline as Run).Text = model.PropertyName();
                });
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (_card != null)
                RaiseEvent(new CardModelEventArgs(_card, ViewCardModelEvent, this));
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (_card != null)
                RaiseEvent(new CardModelEventArgs(null, ViewCardModelEvent, this));
        }
    }

    internal class PlayerRun : Run
    {
        private IPlayPlayer _player;

        public PlayerRun(IPlayPlayer player)
            : base(player.Name)
        {
            _player = player;
            Foreground = _player.Color.CacheToBrush();
            FontWeight = FontWeights.Bold;
        }
    }

    public class GameMessageBlock : Section
    {
        public IGameMessage Message { get; private set; }

        public GameMessageBlock(IGameMessage mess)
        {
            Message = mess;
        }
    }

    public class GameMessageToBlockConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var mess = value as IGameMessage;
            if (mess == null) return null;
            return ChatControl.GameMessageToBlock(mess);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value is GameMessageBlock) == false) return null;
            return (value as GameMessageBlock).Message;
        }
    }
}