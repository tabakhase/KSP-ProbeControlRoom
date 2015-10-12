using System;
using UnityEngine;

namespace ProbeControlRoom
{
	public class ProbeControlRoomSettings
	{
		private static Settings mInstance;
		public static Settings Instance
		{
			get
			{
				return mInstance = mInstance ?? Settings.Load();
			}
		}
	}
	public class Settings
	{
		// UI and Toolbar can overrule this!
		[Persistent] public bool ForcePCROnly = false;
		[Persistent] public bool DisableWobble = true;
		[Persistent] public bool DisableSounds = false;
		
		private static String File { 
			get { return KSPUtil.ApplicationRootPath + "/GameData/ProbeControlRoom/Settings.cfg"; }
		}
		
		public void Save()
		{
			try
			{
				ConfigNode save = new ConfigNode();
				ConfigNode.CreateConfigFromObject(this, 0, save);
				save.Save(File);
			}
			catch (Exception e) { Debug.Log("An error occurred while attempting to save: " + e.Message); }
		}
		
		public static Settings Load()
		{
			ConfigNode load = ConfigNode.Load(File);
			Settings settings = new Settings();
			if (load == null)
			{
				settings.Save();
				return settings;
			}
			ConfigNode.LoadObjectFromConfig(settings, load);
			settings.Save();
			return settings;
		}
	}
}

