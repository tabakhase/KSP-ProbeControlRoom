using UnityEngine;

namespace ProbeControlRoom
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class ProbeControlRoomUI : MonoBehaviour
	{
		public static ProbeControlRoomUI Instance { get; protected set; }
		private bool initStylesDone = false;
		private bool toolbarIsActive = false;
		private bool hideUIState = false;
		private Rect ivaButtonPosition;
		private GUIStyle windowIVAButtStyle;
		private GUIStyle windowIVAButtButtonStyle;

		private bool COMPONENT_OnGUI_DEBUG = false;
		
		public void Start()
		{
			if (Instance != null) {
				ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom][ProbeControlRoomUI] Start() - InstanceKill");
				Destroy (this);
				return;
			}
			Instance = this;
			if(!initStylesDone) InitStyles();
			GameEvents.onHideUI.Add(onHideUI);
			GameEvents.onShowUI.Add(onShowUI);
			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom][ProbeControlRoomUI] OnStart()");
		}

		void onHideUI()
		{
			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom][ProbeControlRoomUI] onHideUI()");
			hideUIState = true;
		}

		void onShowUI()
		{
			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom][ProbeControlRoomUI] onShowUI()");
			hideUIState = false;
		}

		public void OnDestroy()
		{
			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom][ProbeControlRoomUI] OnDestroy()");
			GameEvents.onHideUI.Remove(onHideUI);
			GameEvents.onShowUI.Remove(onShowUI);
		}

		public static void messageThatToolbarIsActive() 
		{
			//ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom][ProbeControlRoomUI] messageThatToolbarIsActive()");
			ProbeControlRoomUI.Instance.toolbarIsActive = true;
		}

		private  void InitStyles()
		{
			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom][ProbeControlRoomUI] InitStyles()");
			initStylesDone = true;

			windowIVAButtStyle = new GUIStyle ();

			windowIVAButtButtonStyle = new GUIStyle (HighLogic.Skin.button);

			if (GameSettings.UI_SIZE >= 990) {
				ivaButtonPosition = new Rect (Screen.width-2 - 120, Screen.height-4-30 - 142, 111, 30);
			}
			if (GameSettings.UI_SIZE == 900) {
				ivaButtonPosition = new Rect (Screen.width-2 - 115, Screen.height-4-30 - 133, 107, 30);
			}
			if (GameSettings.UI_SIZE == 840) {
				ivaButtonPosition = new Rect (Screen.width-2 - 106, Screen.height-4-30 - 124, 96, 30);
			}
			if (GameSettings.UI_SIZE == 768) {
				ivaButtonPosition = new Rect (Screen.width-2 - 96, Screen.height-4-30 - 114, 89, 30);
			}
			if (GameSettings.UI_SIZE == 720) {
				ivaButtonPosition = new Rect (Screen.width-2 - 91, Screen.height-4-30 - 108, 84, 30);
			}
			if (GameSettings.UI_SIZE <= 680) {
				ivaButtonPosition = new Rect (Screen.width-2 - 86, Screen.height-4-30 - 102, 79, 30);
			}
			if(ivaButtonPosition==null)
				ivaButtonPosition = new Rect (Screen.width-2 - 40, Screen.height-4 - 100, 111, 30);

		}
		public void OnGUI()
		{
			if(COMPONENT_OnGUI_DEBUG)
				ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom][ProbeControlRoomUI] OnGUI()");
			var scene = HighLogic.LoadedScene;
			if (scene == GameScenes.FLIGHT && !toolbarIsActive) {
				if (ProbeControlRoom.vesselCanIVA && !hideUIState && !MapView.MapIsEnabled) {
					GUILayout.BeginArea (ivaButtonPosition, windowIVAButtStyle);
					// ignore ProbeControlRoomSettings..ForcePCROnly on purpose
					if (ProbeControlRoom.isActive) {
						if (GUILayout.Button ("End IVA", windowIVAButtButtonStyle)) {
							ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom][ProbeControlRoomUI] OnGUI().Button(End IVA)");
							ProbeControlRoom.Instance.stopIVA ();
						}
					} else {
						if (GUILayout.Button ("IVA", windowIVAButtButtonStyle)) {
							ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom][ProbeControlRoomUI] OnGUI().Button(Start IVA)");
							ProbeControlRoom.Instance.startIVA ();
						}
					}
					GUILayout.EndArea ();
				}

				// TODO when IVA is active, also draw button to switch to other rooms

				// TODO when IVA is active, also draw button to switch to other IVA kerbals
			}
		}
	}
}

