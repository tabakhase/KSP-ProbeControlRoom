using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;


namespace ProbeControlRoom
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class ProbeControlRoom : MonoBehaviour
	{
		public static ProbeControlRoom Instance { get; protected set; }
		public static bool isActive = false;
		public List<Part> vesselRooms = new List<Part>();
		public List<Part> vesselStockIVAs = new List<Part>();

		private ProbeControlRoomPart aModule;
		private Part aPart;
		private Part aPartRestartTo = null;

		float shipVolumeBackup = GameSettings.SHIP_VOLUME;
		float ambianceVolumeBackup = GameSettings.AMBIENCE_VOLUME;
		//float musicVolumeBackup = GameSettings.MUSIC_VOLUME;
		//float uiVolumeBackup = GameSettings.UI_VOLUME;
		//float voiceVolumeBackup = GameSettings.VOICE_VOLUME;

		float cameraWobbleBackup = GameSettings.FLT_CAMERA_WOBBLE;
		float cameraFXInternalBackup = GameSettings.CAMERA_FX_INTERNAL;
		float cameraFXExternalBackup = GameSettings.CAMERA_FX_EXTERNAL;

		public void Start()
		{
			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] Start()");
			if (Instance != null) {
				ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] Start() - InstanceKill");
				Destroy (this);
				return;
			}
			Instance = this;
			GameEvents.onVesselChange.Add(OnVesselChange);
			GameEvents.onVesselWasModified.Add(OnVesselModified);
			refreshVesselRooms ();
			if (ProbeControlRoomSettings.Instance.ForcePCROnly) {
				ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] Start() - ForcePCROnly Enabled.");
				startIVA ();
			}
		}

		public void OnDestroy()
		{
			//in case of revert to launch while in IVA, Update() won't detect it
			//and startIVA(p) will be called without prior stopIVA
			//which will cause settings to be lost forever
			//OnDestroy() will be called though

			//re-enable sound
			GameSettings.SHIP_VOLUME = shipVolumeBackup;
			GameSettings.AMBIENCE_VOLUME = ambianceVolumeBackup;
			//GameSettings.MUSIC_VOLUME = musicVolumeBackup;
			//GameSettings.UI_VOLUME = uiVolumeBackup;
			//GameSettings.VOICE_VOLUME = voiceVolumeBackup;

			//re-enable camera wobble
			GameSettings.FLT_CAMERA_WOBBLE = cameraWobbleBackup;
			GameSettings.CAMERA_FX_INTERNAL = cameraFXInternalBackup;
			GameSettings.CAMERA_FX_EXTERNAL = cameraFXExternalBackup;

			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] OnDestroy()");
			GameEvents.onVesselChange.Remove(OnVesselChange);
			GameEvents.onVesselWasModified.Remove(OnVesselModified);
			Instance = null;
		}


		public static bool vesselCanIVA
		{ get {
				if (ProbeControlRoom.Instance.vesselRooms.Count >= 1) {
					return true;
				} else {
					return false;
				}
		}}

		public static bool vesselCanStockIVA
		{ get {
				if (ProbeControlRoom.Instance.vesselStockIVAs.Count >= 1) {
					return true;
				} else {
					return false;
				}
		}}



		public bool startIVA() 
		{
			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] startIVA()");
			refreshVesselRooms ();
			if (vesselRooms.Count >= 1) {
				if(vesselRooms.Contains(FlightGlobals.ActiveVessel.GetReferenceTransformPart()))
				{
					ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] startIVA() - kick startIVA(Part) - ActiveReference");
					return startIVA (FlightGlobals.ActiveVessel.GetReferenceTransformPart ());
				}
				ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] startIVA() - kick startIVA(Part) - first valid room");
				return startIVA (vesselRooms.First());
			} else {
				ProbeControlRoomUtils.Logger.error ("[ProbeControlRoom] startIVA() - no room found END");
				return false;
			}
		}

		public bool startIVA(Part p) 
		{
			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] startIVA(Part)");
			Transform actualTransform;

			refreshVesselRooms ();
			if(vesselRooms.Contains(p))
			{
				aModule = p.FindModulesImplementing<ProbeControlRoomPart> ().First ();
				aPart = p;
				// TODO when currentReference its a dockingport, store and restore that instead?
				p.MakeReferencePart ();

				actualTransform = p.internalModel.FindModelTransform (aModule.seatTransformName);
				if (Transform.Equals (actualTransform, null)) {
					ProbeControlRoomUtils.Logger.error ("[ProbeControlRoom] startIVA(Part) - NULL on actualTransform-seatTransformName, using fallback...");
					actualTransform = p.internalModel.FindModelTransform ("Seat");
				} else {
					ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] startIVA(Part) - Seat: "+aModule.seatTransformName.ToString());
				}

				ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] startIVA(Part) - fire up IVA");

				//disable sound
				shipVolumeBackup = GameSettings.SHIP_VOLUME;
				ambianceVolumeBackup = GameSettings.AMBIENCE_VOLUME;
				//musicVolumeBackup = GameSettings.MUSIC_VOLUME;
				//uiVolumeBackup = GameSettings.UI_VOLUME;
				//voiceVolumeBackup = GameSettings.VOICE_VOLUME;

				GameSettings.SHIP_VOLUME = 0;
				GameSettings.AMBIENCE_VOLUME = 0;
				//GameSettings.MUSIC_VOLUME = 0;
				//GameSettings.UI_VOLUME = 0;
				//GameSettings.VOICE_VOLUME = 0;

				//disable camera wobble
				cameraWobbleBackup = GameSettings.FLT_CAMERA_WOBBLE;
				cameraFXInternalBackup = GameSettings.CAMERA_FX_INTERNAL;
				cameraFXExternalBackup = GameSettings.CAMERA_FX_EXTERNAL;

				GameSettings.FLT_CAMERA_WOBBLE = 0;
				GameSettings.CAMERA_FX_INTERNAL = 0;
				GameSettings.CAMERA_FX_EXTERNAL = 0;


				CameraManager.ICameras_DeactivateAll ();

				FlightCamera.fetch.EnableCamera ();
				FlightCamera.fetch.DeactivateUpdate ();
				FlightCamera.fetch.gameObject.SetActive (true);
				FlightEVA.fetch.DisableInterface ();

				InternalCamera.Instance.SetTransform(actualTransform, true);

				InternalCamera.Instance.EnableCamera ();
				FlightGlobals.ActiveVessel.SetActiveInternalPart (p.internalModel.part);

				IVASun sunBehaviour;
				sunBehaviour = (IVASun)FindObjectOfType(typeof(IVASun));
				sunBehaviour.enabled = false;

				isActive = true;

				if(UIPartActionController.Instance != null)
					UIPartActionController.Instance.Deactivate ();

				CameraManager.Instance.currentCameraMode = CameraManager.CameraMode.Internal;

				ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] startIVA(Part) - DONE");
				return true;
			} else {
				ProbeControlRoomUtils.Logger.error ("[ProbeControlRoom] startIVA(Part) - Cannot instantiate ProbeControlRoom in this location - Part/ModuleNotFound");
				throw new ArgumentException("[ProbeControlRoom] startIVA(Part) - Cannot instantiate ProbeControlRoom in this location - Part/ModuleNotFound");
				//return false;
			}
		}


		public void stopIVA() 
		{
			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] stopIVA()");
			isActive = false;
			aModule = null;
			aPart = null;

			//re-enable sound
			GameSettings.SHIP_VOLUME = shipVolumeBackup;
			GameSettings.AMBIENCE_VOLUME = ambianceVolumeBackup;
			//GameSettings.MUSIC_VOLUME = musicVolumeBackup;
			//GameSettings.UI_VOLUME = uiVolumeBackup;
			//GameSettings.VOICE_VOLUME = voiceVolumeBackup;

			//re-enable camera wobble
			GameSettings.FLT_CAMERA_WOBBLE = cameraWobbleBackup;
			GameSettings.CAMERA_FX_INTERNAL = cameraFXInternalBackup;
			GameSettings.CAMERA_FX_EXTERNAL = cameraFXExternalBackup;

			CameraManager.ICameras_DeactivateAll ();

			CameraManager.Instance.SetCameraFlight();

			if(UIPartActionController.Instance != null)
				UIPartActionController.Instance.Activate ();

			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] stopIVA() - CHECKMARK");

			if (aPartRestartTo != null) {
				ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] stopIVA() - RestartHook found, fire!");
				Part aPartRestartToCache = aPartRestartTo;
				aPartRestartTo = null;
				startIVA (aPartRestartToCache);
			}
		}

		public void Update()
		{
			var scene = HighLogic.LoadedScene;
			if (scene == GameScenes.FLIGHT) {
				if (isActive) {
					if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA) {
						ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] OnUpdate() - real IVA detected, ending...");
						stopIVA ();
						if (ProbeControlRoomSettings.Instance.ForcePCROnly) {
							ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] OnUpdate() - real IVA detected, ending... KILLED - ForcePCROnly Enabled.");
							startIVA ();
						}
					}
					if (!MapView.MapIsEnabled && !InternalCamera.Instance.isActive) {
						ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] OnUpdate() - IVA broke, kill and maybe restart...");
						// TODO directReturn from map is broken in case vessel has a "real IVA"
						// Has IVA and no kerbal     		== OK    (cockpit)
						// Has IVA and kerbal        		== ERROR (cockpit)
						// Has IVA and no kerbal     		== OK    (CrewContainer)
						// Has IVA and kerbal        		== ERROR (CrewContainer)
						// Has No IVA and no Kerbal  		== OK    (Lab)
						// has No IVA and kerbal     		== OK    (Lab)
						if (!vesselCanStockIVA)
							aPartRestartTo = aPart;
						if (ProbeControlRoomSettings.Instance.ForcePCROnly) {
							ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] OnUpdate() - IVA broke, kill and maybe restart... OVERWRITE - ForcePCROnly Enabled.");
							aPartRestartTo = aPart;
						}
						stopIVA ();
					}
					if (!MapView.MapIsEnabled && Input.GetKeyDown (GameSettings.CAMERA_MODE.primary)) {
						ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] OnUpdate() - CAMERA_MODE.key seen, stopIVA()");
						if (ProbeControlRoomSettings.Instance.ForcePCROnly) {
							ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] OnUpdate() - CAMERA_MODE.key seen, stopIVA() KILLED - ForcePCROnly Enabled.");
						} else {
							stopIVA ();
						}
					}
				} else {
					if (!vesselCanStockIVA && !MapView.MapIsEnabled && Input.GetKeyDown (GameSettings.CAMERA_MODE.primary)) {
						ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] OnUpdate() - CAMERA_MODE.key seen, startIVA()");
						startIVA ();
					}
				}
			} else {
				if (isActive) {
					ProbeControlRoomUtils.Logger.error ("[ProbeControlRoom] OnUpdate() - stopping, active while not in FLIGHT");
					stopIVA ();
				}
			}
		}

		private void OnVesselChange(Vessel v)
		{
			ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] OnVesselChange(Vessel)");
			refreshVesselRooms ();
			if (isActive) {
				ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] OnVesselChange(Vessel) - fire stopIVA()");
				stopIVA ();
			}
		}
		private void OnVesselModified(Vessel v)
		{
			ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] onVesselWasModified(Vessel)");
			refreshVesselRooms ();
			if (isActive) {
				if (vesselCanIVA) {
					if (!vesselRooms.Contains (aPart)) {
						ProbeControlRoomUtils.Logger.error ("[ProbeControlRoom] OnVesselModified(Vessel) - our part is gone, forceRestart");
						stopIVA ();
						startIVA ();
					} else {
						ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] OnVesselModified(Vessel) - seems fine, go on...");
					}
				} else {
					ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] OnVesselModified - noIVA left - fire stopIVA()");
					stopIVA ();
				}
			}
		}


		private void refreshVesselRooms () {
			Vessel vessel = FlightGlobals.ActiveVessel;
			List<Part> rooms = new List<Part> ();
			List<Part> stockIVAs = new List<Part> ();
			if (vessel != null) {
				ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] refreshVesselRooms() - scanning vessel: " + vessel.ToString ());
				foreach (Part p in vessel.parts) {
					ProbeControlRoomPart room = p.GetComponent<ProbeControlRoomPart> ();
					if (room != null) {
						ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] refreshVesselRooms() - found ProbeControlRoomPart in: " + p.ToString ());
						InternalModel model = p.internalModel;
						if (model != null) {
							ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] refreshVesselRooms() - found internalModel in: " + p.ToString ());
							rooms.Add (p);
						}
					}

					InternalModel imodel = p.internalModel;
					if (imodel != null) {
						ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] refreshVesselRooms() - found internalModel in: " + p.ToString ());
						if (p.protoModuleCrew.Count >= 1) {
							ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] refreshVesselRooms() - found internalModel in: " + p.ToString () + " - and it has crew!");
							stockIVAs.Add (p);
						}
					}
				}
				ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] refreshVesselRooms() - scanned vessel: " + vessel.ToString () + " - rooms: " + rooms.Count.ToString () + " - stockIVAs: " + stockIVAs.Count.ToString ());
				vesselRooms = rooms;
				vesselStockIVAs = stockIVAs;
			} else {
				ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] refreshVesselRooms() - no valid vessel");
				vesselRooms = new List<Part> ();
				vesselStockIVAs = new List<Part> ();
			}
		}
	}
}

