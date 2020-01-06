/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
namespace Octgn.Core.Play
{
    public interface IGameMessage
    {
        bool IsClientMuted { get; }
        bool IsMuted { get; }
        bool CanMute { get; }
        int Id { get; }
        DateTime Timestamp { get; }
        IPlayPlayer From { get; }
        string Message { get; }
        object[] Arguments { get; }
    }
}