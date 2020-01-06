/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Windows.Media;
namespace Octgn.Core.Play
{
    public class BuiltInPlayer : IPlayPlayer
    {
        public byte Id { get; private set; }
        public string Name { get; private set; }
        public Color Color { get; private set; }
        public PlayerState State { get; private set; }

        private static readonly IPlayPlayer warningPlayer = new BuiltInPlayer
                                                            {
                                                                Color = Colors.Crimson,
                                                                Name = "Warning",
                                                                Id = 254,
                                                                State = PlayerState.Connected
                                                            };

        private static readonly IPlayPlayer systemPlayer = new BuiltInPlayer
                                                            {
                                                                Color = Colors.BlueViolet,
                                                                Name = "System",
                                                                Id = 253,
                                                                State = PlayerState.Connected
                                                            };

        private static readonly IPlayPlayer activePlayer = new BuiltInPlayer
                   {
                       Color = Color.FromRgb(0x5A, 0x9A, 0xCF),
                       Name = "",
                       Id = 252,
                       State = PlayerState.Connected
                   };
        private static readonly IPlayPlayer debugPlayer = new BuiltInPlayer
                   {
                       Color = Colors.LightGray,
                       Name = "DEBUG",
                       Id = 250,
                       State = PlayerState.Connected
                   };
        private static readonly IPlayPlayer notifyPlayer = new BuiltInPlayer
                   {
                       Color = Colors.DimGray,
                       Name = "",
                       Id = 251,
                       State = PlayerState.Connected
                   };
        private static readonly IPlayPlayer notifyBarPlayer = new BuiltInPlayer
                   {
                       Color = Colors.Black,
                       Name = "",
                       Id = 251,
                       State = PlayerState.Connected
                   };

        public static IPlayPlayer Warning { get { return warningPlayer; } }
        public static IPlayPlayer System { get { return systemPlayer; } }
        public static IPlayPlayer Turn { get { return activePlayer; } }
        public static IPlayPlayer Debug { get { return debugPlayer; } }
        public static IPlayPlayer Notify{ get { return notifyPlayer; } }
        public static IPlayPlayer NotifyBar{ get { return notifyBarPlayer; } }
    }
}