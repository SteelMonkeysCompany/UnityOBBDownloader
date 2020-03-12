using UnityEngine;
using System.IO;
using System;

internal class GooglePlayObbDownloader : IGooglePlayObbDownloader
{
	private const string EnvironmentMediaMounted = "mounted";
	private const string ObbPath = "Android/obb";
	private const int IntentFlagActivityNoAnimation = 0x10000;

	private static readonly AndroidJavaClass _environmentClass = new AndroidJavaClass("android.os.Environment");
	private static string _obbPackage;
	private static int _obbVersion;
	private string _expansionFilePath;

	private static string ObbPackage
	{
		get
		{
			if (_obbPackage == null)
			{
				PopulateOBBProperties();
			}
			return _obbPackage;
		}
	}

	private static int ObbVersion
	{
		get
		{
			if (_obbVersion == 0)
			{
				PopulateOBBProperties();
			}
			return _obbVersion;
		}
	}

	public string PublicKey { get; set; }

	public string MainOBBPath => GetOBBPackagePath(GetExpansionFilePath(), "main");

	public void FetchOBB()
	{
		ApplyPublicKey();

		using (var unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
		{
			var currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
			var intent = new AndroidJavaObject("android.content.Intent", currentActivity, new AndroidJavaClass("com.unity3d.plugin.downloader.UnityDownloaderActivity"));

			intent.Call<AndroidJavaObject>("addFlags", IntentFlagActivityNoAnimation);
			intent.Call<AndroidJavaObject>("putExtra", "unityplayer.Activity", currentActivity.Call<AndroidJavaObject>("getClass").Call<string>("getName"));

			try
			{
				currentActivity.Call("startActivity", intent);
			}
			catch (Exception ex)
			{
				Debug.LogError("GooglePlayObbDownloader: Exception occurred while attempting to start DownloaderActivity - is the AndroidManifest.xml incorrect?\n" + ex.Message);
			}
		}
	}

	private static string GetOBBPackagePath(string expansionFilePath, string prefix)
	{
		if (string.IsNullOrEmpty(expansionFilePath))
			return null;

		string filePath = $"{expansionFilePath}/{prefix}.{ObbVersion}.{ObbPackage}.obb";
		return File.Exists(filePath) ? filePath : null;
	}
	
	private static void PopulateOBBProperties()
	{
		using (var unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
		{
			var currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
			_obbPackage = currentActivity.Call<string>("getPackageName");
			var packageInfo = currentActivity.Call<AndroidJavaObject>("getPackageManager").Call<AndroidJavaObject>("getPackageInfo", _obbPackage, 0);
			_obbVersion = packageInfo.Get<int>("versionCode");
		}
	}

	private void ApplyPublicKey()
	{
		if (string.IsNullOrEmpty(PublicKey))
		{
			Debug.LogError("GooglePlayObbDownloader: The public key is not set - did you forget to set it in the script?\n");
		}

		using (var downloaderServiceClass = new AndroidJavaClass("com.unity3d.plugin.downloader.UnityDownloaderService"))
		{
			downloaderServiceClass.CallStatic("setPublicKey", PublicKey);
			downloaderServiceClass.CallStatic("setSalt", new byte[] { 1, 43, 256 - 12, 256 - 1, 54, 98, 256 - 100, 256 - 12, 43, 2, 256 - 8, 256 - 4, 9, 5, 256 - 106, 256 - 108, 256 - 33, 45, 256 - 1, 84 });
		}
	}

	private string GetExpansionFilePath()
	{
		if (_environmentClass.CallStatic<string>("getExternalStorageState") != EnvironmentMediaMounted)
		{
			_expansionFilePath = null;
			return _expansionFilePath;
		}

		if (string.IsNullOrEmpty(_expansionFilePath))
		{
			using (var externalStorageDirectory = _environmentClass.CallStatic<AndroidJavaObject>("getExternalStorageDirectory"))
			{
				string externalRoot = externalStorageDirectory.Call<string>("getPath");
				_expansionFilePath = $"{externalRoot}/{ObbPath}/{ObbPackage}";
			}
		}

		return _expansionFilePath;
	}

}