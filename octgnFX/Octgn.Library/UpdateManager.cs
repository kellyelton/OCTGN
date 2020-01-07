/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;
using Octgn.Library.Util;

namespace Octgn.Library
{
    public sealed class UpdateManager
    {
        private static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static UpdateManager Current { get; set; }

        public event EventHandler<UpdateDetails> UpdateAvailable;
        public event EventHandler<UpdateDetails> RestartForUpdate;

        public UpdateDetails LatestVersion { get; }

        private readonly bool _isReleaseTest;
        private Timer _timer;

        public UpdateManager(bool isReleaseTest) {
            _isReleaseTest = isReleaseTest;
            LatestVersion = new UpdateDetails(Const.OctgnVersion, _isReleaseTest);
        }

        public void Start() {
            _timer = new Timer(Tick, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        public void Restart() {
            if (_timer != null) throw new InvalidOperationException("Already started");

            _timer = new Timer(Tick, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        public void Stop() {
            var timer = _timer;

            _timer = null;

            timer?.Dispose();
        }

        private void Tick(object state) {
            lock (_timer) {
                LatestVersion.UpdateInfo();
                if (LatestVersion.CanUpdate) {
                    DownloadLatestVersion();
                    if (LatestVersion.UpdateDownloaded) {
                        FireOnUpdateAvailable(LatestVersion);
                    }
                }
            }
        }

        public void DownloadLatestVersion() {
            lock (LatestVersion) {
                if (LatestVersion.CanUpdate && !LatestVersion.UpdateDownloaded) {
                    var downloadUri = new Uri(LatestVersion.InstallUrl);
                    string filename = System.IO.Path.GetFileName(downloadUri.LocalPath);
                    var filePath = Path.Combine(Config.Instance.Paths.UpdatesPath, filename);
                    var fd = new FileDownloader(downloadUri, filePath);
                    var dtask = fd.Download();
                    dtask.Start();
                    dtask.Wait();

                }
            }
        }

        public bool UpdateAndRestart() {
            lock (LatestVersion) {
                if (LatestVersion.CanUpdate && LatestVersion.UpdateDownloaded) {
                    var downloadUri = new Uri(LatestVersion.InstallUrl);
                    var filename = System.IO.Path.GetFileName(downloadUri.LocalPath);
                    var fi = new FileInfo(Path.Combine(Config.Instance.Paths.UpdatesPath, filename));

                    FireRestartForUpdate(LatestVersion);
                    return true;
                }
            }
            return false;
        }

        private void FireOnUpdateAvailable(UpdateDetails updateDetails) {
            UpdateAvailable?.Invoke(this, updateDetails);
        }

        private void FireRestartForUpdate(UpdateDetails updateDetails) {
            RestartForUpdate?.Invoke(this, updateDetails);
        }
    }

    public class UpdateDetails
    {
        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Version Version { get; set; }
        public string InstallUrl { get; set; }
        public DateTime LastCheckTime { get; set; }
        public bool IsFaulted { get; set; }
        public FileInfo UpdateFile {
            get {
                var downloadUri = new Uri(InstallUrl);
                var filename = System.IO.Path.GetFileName(downloadUri.LocalPath);
                return new FileInfo(Path.Combine(Config.Instance.Paths.UpdatesPath, filename));
            }
        }

        public bool UpdateDownloaded {
            get {
                if (!CanUpdate) return false;

                var updateFile = UpdateFile;

                if (updateFile.Exists) {
                    var updateUri = new Uri(InstallUrl);

                    var remoteLength = new FileDownloader(updateUri, updateFile.Name).GetRemoteFileSize();

                    for (var i = 0; i < 3; i++) {
                        if (remoteLength != -1) break;
                        remoteLength = new FileDownloader(updateUri, updateFile.Name).GetRemoteFileSize();
                        Thread.Sleep(1000);
                    }

                    if (remoteLength == -1) return false;

                    if (updateFile.Length >= remoteLength) {
                        return true;
                    }
                }
                return false;
            }
        }

        private readonly Version _currentVerison;
        private readonly bool _isReleaseTest;

        public UpdateDetails(Version currentVersion, bool isReleaseTest) {
            _currentVerison = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
            _isReleaseTest = isReleaseTest;
            IsFaulted = true;
            LastCheckTime = DateTime.MinValue;
        }

        public bool? IsUpToDate {
            get {
                if (Version == null)
                    return null;
                var thisVersion = GetType().Assembly.GetName().Version;

                if (Version.Minor != thisVersion.Minor) return true;

                return Version.Equals(thisVersion);
            }
        }

        public bool CanUpdate {
            get {
                var iu = IsUpToDate ?? true;
                if (IsFaulted) return false;
                return iu == false;
            }
        }

        public UpdateDetails UpdateInfo() {
            lock (this) {
                Version = null;
                InstallUrl = null;
                this.LastCheckTime = DateTime.Now;
                IsFaulted = true;
                try {
                    var c = new Octgn.Site.Api.ApiClient();
                    var info = c.GetLatestRelease(_currentVerison);
                    if (_isReleaseTest == false) {
                        Version = Version.Parse(info.LiveVersion);
                        this.InstallUrl = info.LiveVersionDownloadLocation;
                    } else {
                        Version = Version.Parse(info.TestVersion);
                        this.InstallUrl = info.TestVersionDownloadLocation;
                    }
                    if (!String.IsNullOrWhiteSpace(InstallUrl) && Version != null) {
                        IsFaulted = false;
                    }
                } catch (WebException e) {
                    Log.Warn("", e);
                    IsFaulted = true;
                } catch (Exception e) {
                    Log.Warn("", e);
                    IsFaulted = true;
                }
                return this;
            }
        }
    }
}