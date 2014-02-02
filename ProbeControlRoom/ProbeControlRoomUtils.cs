using System;
using UnityEngine;

namespace ProbeControlRoom
{
	public static class ProbeControlRoomUtils
	{
		public static void MakeReferencePart(this Part thatPart)
		{
			if (thatPart != null) {
				foreach (PartModule thatModule in thatPart.Modules) {
					var thatNode = thatModule as ModuleDockingNode;
					var thatPod = thatModule as ModuleCommand;
					if (thatNode != null) {
						thatNode.MakeReferenceTransform();
						break;
					}
					if (thatPod != null) {
						thatPod.MakeReference();
						break;
					}
				}
			}
		}

		public static class Logger
		{
			public enum modes {
				DEBUG,
				TESTING,
				RELEASE,
				OFF
			}
			#if DEBUG
			public static modes mode = modes.DEBUG;
			public static bool limiter = true;
			#else
			public static modes mode = modes.TESTING;
			public static bool limiter = true;
			#endif

			private static string lastMessage = "";


			public static void debug(String str)
			{
				if(modes.DEBUG.Equals(mode))
					logMessage (str);
			}
			public static void message(String str)
			{
				if(modes.DEBUG.Equals(mode) || modes.TESTING.Equals(mode) )
					logMessage (str);
			}
			public static void error(String str)
			{
				if(modes.DEBUG.Equals(mode) || modes.TESTING.Equals(mode) || modes.RELEASE.Equals(mode))
					logMessage (str);
			}


			private static void logMessage(String str)
			{
				if (limiter && str != lastMessage) {
					lastMessage = str;
					Debug.Log (str);
				} else {
					Debug.Log (str);
				}
			}


		}
	}
}

