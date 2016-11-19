using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using KSP.UI.Screens;


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

        private static ApplicationLauncherButton appLauncherButton = null;
        private bool AppLauncher = false;

        
        private Texture2D IconActivate = null;
        private Texture2D IconDeactivate = null;

        public void Start()
        {
            ProbeControlRoomUtils.Logger.debug("[ProbeControlRoom] Start()");
            if (Instance != null)
            {
                ProbeControlRoomUtils.Logger.debug("[ProbeControlRoom] Start() - InstanceKill");
                Destroy(this);
                return;
            }
            Instance = this;
            refreshVesselRooms();
            GameEvents.onVesselWasModified.Fire(FlightGlobals.ActiveVessel);
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onVesselWasModified.Add(OnVesselModified);
            
            GameEvents.onGUIApplicationLauncherReady.Add(onGUIApplicationLauncherReady);
            // TODO: check for cfg file with cached vars, if (after crash) load it and use those as defaults
            shipVolumeBackup = GameSettings.SHIP_VOLUME;
            ambianceVolumeBackup = GameSettings.AMBIENCE_VOLUME;
            //musicVolumeBackup = GameSettings.MUSIC_VOLUME;
            //uiVolumeBackup = GameSettings.UI_VOLUME;
            //voiceVolumeBackup = GameSettings.VOICE_VOLUME;
            
            cameraWobbleBackup = GameSettings.FLT_CAMERA_WOBBLE;
            cameraFXInternalBackup = GameSettings.CAMERA_FX_INTERNAL;
            cameraFXExternalBackup = GameSettings.CAMERA_FX_EXTERNAL;



            if (ProbeControlRoomSettings.Instance.ForcePCROnly)
            {
                ProbeControlRoomUtils.Logger.message("[ProbeControlRoom] Start() - ForcePCROnly Enabled.");
                startIVA();
            }
        }    
       

        private void onGUIApplicationLauncherReady()
        {
            if (!AppLauncher)
            {
                appLauncherButton = InitializeApplicationButton();
                AppLauncher = true;
            }
        }

        ApplicationLauncherButton InitializeApplicationButton()
        {
            ApplicationLauncherButton Button = null;

            IconActivate = GameDatabase.Instance.GetTexture("ProbeControlRoom/ProbeControlRoomToolbarDisabled", false);
            IconDeactivate = GameDatabase.Instance.GetTexture("ProbeControlRoom/ProbeControlRoomToolbarEnabled", false);


            Button = ApplicationLauncher.Instance.AddModApplication(
                OnAppLauncherTrue,
                OnAppLauncherFalse,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.FLIGHT,
                IconActivate);

            if (Button == null)
            {
                ProbeControlRoomUtils.Logger.debug("[Probe Control Room] InitializeApplicationButton(): Was unable to initialize button");
            }

            return Button;
        }

        void OnAppLauncherTrue()
        {
            if(!isActive)
            {
                startIVA();
                
            }
        }
        void OnAppLauncherFalse()
        {
            if (isActive)
            {
                stopIVA();
                
            }
        }
        public void OnDestroy()
		{
			//in case of revert to launch while in IVA, Update() won't detect it
			//and startIVA(p) will be called without prior stopIVA
			//which will cause settings to be lost forever
			//OnDestroy() will be called though

			if (ProbeControlRoomSettings.Instance.DisableSounds) {
				ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] OnDestroy() - DisableSounds - RESTORE");
				//re-enable sound
				GameSettings.SHIP_VOLUME = shipVolumeBackup;
				GameSettings.AMBIENCE_VOLUME = ambianceVolumeBackup;
				//GameSettings.MUSIC_VOLUME = musicVolumeBackup;
				//GameSettings.UI_VOLUME = uiVolumeBackup;
				//GameSettings.VOICE_VOLUME = voiceVolumeBackup;
			}

			if (ProbeControlRoomSettings.Instance.DisableWobble) {
				ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] OnDestroy() - DisableWobble - RESTORE");
				//re-enable camera wobble
				GameSettings.FLT_CAMERA_WOBBLE = cameraWobbleBackup;
				GameSettings.CAMERA_FX_INTERNAL = cameraFXInternalBackup;
				GameSettings.CAMERA_FX_EXTERNAL = cameraFXExternalBackup;
			}
			// TODO: remove cfg file with cached vars, no crash and reseted, no need to keep

			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] OnDestroy()");
			GameEvents.onVesselChange.Remove(OnVesselChange);
			GameEvents.onVesselWasModified.Remove(OnVesselModified);

            if (appLauncherButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
                appLauncherButton = null;
            }
                Instance = null;
		}


		public static bool vesselCanIVA
		{ get {
				if (Instance.vesselRooms.Count >= 1) {
					return true;
				} else {
					return false;
				}
		}}

		public static bool vesselCanStockIVA
		{ get {
				if (Instance.vesselStockIVAs.Count >= 1) {
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

            if (vesselRooms.Contains(p))
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

				if (ProbeControlRoomSettings.Instance.DisableSounds) {
					ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] startIVA(Part) - DisableSounds");
					GameSettings.SHIP_VOLUME = 0f;
					GameSettings.AMBIENCE_VOLUME = 0;
					GameSettings.MUSIC_VOLUME = 0;
					GameSettings.UI_VOLUME = 0;
					GameSettings.VOICE_VOLUME = 0;
				}

				//disable camera wobble
				cameraWobbleBackup = GameSettings.FLT_CAMERA_WOBBLE;
				cameraFXInternalBackup = GameSettings.CAMERA_FX_INTERNAL;
				cameraFXExternalBackup = GameSettings.CAMERA_FX_EXTERNAL;

				if (ProbeControlRoomSettings.Instance.DisableWobble) {
					ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] startIVA(Part) - DisableWobble");
					GameSettings.FLT_CAMERA_WOBBLE = 0;
					GameSettings.CAMERA_FX_INTERNAL = 0;
					GameSettings.CAMERA_FX_EXTERNAL = 0;
				}
				// TODO: create cfg file with cached vars, on crash to be restored

				
                
               FlightCamera.fetch.EnableCamera ();
	 			 FlightCamera.fetch.DeactivateUpdate ();
               FlightCamera.fetch.gameObject.SetActive(true);
 /*               InternalSeat CapComSeat = new InternalSeat();
                CapComSeat.seatTransform = actualTransform;
                foreach(InternalSeat i in p.internalModel.seats)
                {
                    Debug.Log("PCR InternalSeat: " + i.seatTransformName);
                }
                p.internalModel.seats.Add(CapComSeat);
                CapComSeat.allowCrewHelmet = false;
                ProtoCrewMember CapCom = new ProtoCrewMember(ProtoCrewMember.KerbalType.Crew);
                p.internalModel.SitKerbalAt(CapCom, CapComSeat);
                p.internalModel.SpawnCrew();
                
                CapCom.ChangeName("CapCom");
                CapCom.isBadass = true;
                CapCom.veteran = true;
*/             

                

				InternalCamera.Instance.SetTransform(actualTransform, true);

				InternalCamera.Instance.EnableCamera ();
                
                
				IVASun sunBehaviour;
				sunBehaviour = (IVASun)FindObjectOfType(typeof(IVASun));
				sunBehaviour.enabled = false;
                
           
				isActive = true;
                
				if(UIPartActionController.Instance != null)
					UIPartActionController.Instance.Deactivate ();
                p.internalModel.SpawnCrew();
				

                //                CameraManager.Instance.currentCameraMode = CameraManager.CameraMode.Internal;
                CameraManager.Instance.SetCameraInternal(p.internalModel, actualTransform);
                CameraManager.Instance.SetCameraMode(CameraManager.CameraMode.Internal);
                ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] startIVA(Part) - DONE");
                appLauncherButton.SetTexture(IconDeactivate);

                
                
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

			if (ProbeControlRoomSettings.Instance.DisableSounds) {
				ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] stopIVA() - DisableSounds - RESTORE");
				//re-enable sound
				GameSettings.SHIP_VOLUME = shipVolumeBackup;
				GameSettings.AMBIENCE_VOLUME = ambianceVolumeBackup;
				//GameSettings.MUSIC_VOLUME = musicVolumeBackup;
				//GameSettings.UI_VOLUME = uiVolumeBackup;
				//GameSettings.VOICE_VOLUME = voiceVolumeBackup;
			}

			if (ProbeControlRoomSettings.Instance.DisableWobble) {
				ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] stopIVA() - DisableWobble - RESTORE");
				//re-enable camera wobble
				GameSettings.FLT_CAMERA_WOBBLE = cameraWobbleBackup;
				GameSettings.CAMERA_FX_INTERNAL = cameraFXInternalBackup;
				GameSettings.CAMERA_FX_EXTERNAL = cameraFXExternalBackup;
			}

			CameraManager.ICameras_DeactivateAll ();

			CameraManager.Instance.SetCameraFlight();

            if (UIPartActionController.Instance != null)
                UIPartActionController.Instance.Activate();

			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] stopIVA() - CHECKMARK");
            appLauncherButton.SetTexture(IconActivate);
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
                    /*      				if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA) {
                                            ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] OnUpdate() - real IVA detected, ending...");
                                            stopIVA ();
                                            if (ProbeControlRoomSettings.Instance.ForcePCROnly) {
                                                ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom] OnUpdate() - real IVA detected, ending... KILLED - ForcePCROnly Enabled.");
                                                startIVA ();
                                            }
                                        }

                    */
                    
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
                        if (p.internalModel == null)
                        {
                            p.CreateInternalModel();
                            if (p.internalModel != null)
                            {
                                p.internalModel.Initialize(p);
                                p.internalModel.SetVisible(false);
                            }
                            ProbeControlRoomUtils.Logger.debug("[ProbeControlRoom] refreshVesselRooms() created ProbeControlRoomPart in: " + p.ToString());
                            
                        }
                        
						InternalModel model = p.internalModel;
                        model.enabled = true;

                        
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
                Instance.vesselRooms = rooms;
				vesselStockIVAs = stockIVAs;
			} else {
				ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom] refreshVesselRooms() - no valid vessel");
				vesselRooms = new List<Part> ();
				vesselStockIVAs = new List<Part> ();
			}
		}
	}
}

