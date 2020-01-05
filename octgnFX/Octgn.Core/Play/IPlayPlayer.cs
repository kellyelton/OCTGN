/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Windows.Media;

namespace Octgn.Core.Play
{
    public interface IPlayPlayer
    {
		/// <summary>
		/// Identifier
		/// </summary>
        byte Id{ get; }

		/// <summary>
		/// Nickname
		/// </summary>
        string Name{ get; }

		/// <summary>
		/// Player Color
		/// </summary>
        Color Color { get; }

		/// <summary>
		/// Player State
		/// </summary>
        PlayerState State { get; }
    }
}