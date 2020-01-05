/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Octgn.Core.DataExtensionMethods;
using Octgn.DataNew.Entities;

namespace Octgn.Utils
{
    internal static class ImageUtils
    {
        private static readonly BitmapImage ReflectionBitmapImage;
        private static readonly Func<Uri, BitmapImage> GetImageFromCache;

        static ImageUtils()
        {
            ReflectionBitmapImage = new BitmapImage();
            MethodInfo methodInfo = typeof (BitmapImage).GetMethod("CheckCache",
                                                                   BindingFlags.NonPublic | BindingFlags.Instance);
            GetImageFromCache =
                (Func<Uri, BitmapImage>)
                Delegate.CreateDelegate(typeof (Func<Uri, BitmapImage>), ReflectionBitmapImage, methodInfo);
        }

        public static void GetCardImage(GameEngine gameEngine, ICard card, Action<BitmapImage> action, bool proxyOnly = false)
        {
			//var uri = new Uri(card.GetPicture());
            var uri = proxyOnly ?
                new Uri(card.GetProxyPicture()) : new Uri(card.GetPicture());
            BitmapImage bmp = GetImageFromCache(uri);
            if (bmp != null)
            {
                action(bmp);
                return;
            }
            action(CreateFrozenBitmap(gameEngine, uri));
        }

        public static BitmapImage CreateFrozenBitmap(GameEngine gameEngine, string source)
        {
            return CreateFrozenBitmap(gameEngine, new Uri(source));
        }

        public static BitmapImage CreateFrozenBitmap(GameEngine gameEngine, Uri uri)
        {
            var imgsrc = new BitmapImage();
            imgsrc.BeginInit();
            imgsrc.CacheOption = BitmapCacheOption.OnLoad;
            // catch bad Uri's and load Front Bitmap "?"
            try
            {
                imgsrc.UriSource = uri;
                imgsrc.EndInit();
            }
            catch (Exception)
            {
                imgsrc = new BitmapImage();
                imgsrc.BeginInit();
                imgsrc.CacheOption = BitmapCacheOption.None;
                imgsrc.UriSource = gameEngine.GetCardFront("Default").UriSource;
                imgsrc.EndInit();
            }
			if(imgsrc.CanFreeze)
				imgsrc.Freeze();
            return imgsrc;
        }
    }
}