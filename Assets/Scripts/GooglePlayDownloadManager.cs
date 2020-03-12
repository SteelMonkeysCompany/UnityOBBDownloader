using UnityEngine;
using System;

public interface IGooglePlayObbDownloader
{
    string PublicKey { set; }
    string MainOBBPath { get; }

	void FetchOBB();
}

public static class GooglePlayObbDownloadManager
{
    private static IGooglePlayObbDownloader _instance;
    private static AndroidJavaClass _androidOsBuild;

	private static bool IsDownloaderAvailable => (_androidOsBuild ?? ( _androidOsBuild = new AndroidJavaClass("android.os.Build"))).GetRawClass() != IntPtr.Zero;

    public static IGooglePlayObbDownloader Downloader => _instance ?? (IsDownloaderAvailable ? _instance = new GooglePlayObbDownloader() : null);
}