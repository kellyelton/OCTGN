/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using log4net;

namespace Octgn.Extentions
{
    public static partial class StringExtensionMethods
    {
        public static void SetLastPythonFunction(this ILog log, string function)
        {
            GlobalContext.Properties["lastpythonfunction"] = function;
        }

        public static void SetUserName(this ILog log, string username)
        {
            GlobalContext.Properties["username"] = username;
        }

        public static void SetRunningGame(this ILog log, string gameName, Guid gameId, Version gameVersion)
        {
            GlobalContext.Properties["gameName"] = gameName;
            GlobalContext.Properties["gameId"] = gameId;
            GlobalContext.Properties["gameVersion"] = gameVersion;
        }
    }
}
