//ProbeControlRoom.cs
using System;
using UnityEngine;
using System.Collections.Generic;
using KSP.UI.Screens;


namespace ProbeControlRoom
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    /// <summary>
    /// Primary class for Probe Control room
    /// </summary>
	public class ProbeControlRoom : MonoBehaviour
    {

        public static ProbeControlRoom Instance { get; protected set; }
        public static bool isActive = false;


        private ProbeControlRoomPart aModule;
        private Part aPart;
        private bool canStockIVA;
        private bool canPCRIVA;

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
            ProbeControlRoomUtils.Logger.debug("Start()");
            Instance = this;
            refreshVesselRooms();
            GameEvents.onVesselWasModified.Fire(FlightGlobals.ActiveVessel);
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onVesselWasModified.Add(OnVesselModified);

            GameEvents.onGUIApplicationLauncherReady.Add(onGUIApplicationLauncherReady);

            if (ProbeControlRoomSettings.Instance.ForcePCROnly)
            {
                ProbeControlRoomUtils.Logger.message("Start() - ForcePCROnly Enabled.");
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
                ProbeControlRoomUtils.Logger.debug("InitializeApplicationButton(): Was unable to initialize button");
            }

            return Button;
        }

        void OnAppLauncherTrue()
        {
            toggleIVA();
        }
        void OnAppLauncherFalse()
        {
            toggleIVA();
        }
        private void toggleIVA()
        {
            if (isActive)
            {
                stopIVA();
            }
            else
            {
                startIVA();
            }
        }

        public void OnDestroy()
        {
            //in case of revert to launch while in IVA, Update() won't detect it
            //and startIVA(p) will be called without prior stopIVA
            //which will cause settings to be lost forever
            //OnDestroy() will be called though

            if (ProbeControlRoomSettings.Instance.DisableSounds)
            {
                ProbeControlRoomUtils.Logger.message("OnDestroy() - DisableSounds - RESTORE");
                //re-enable sound
                GameSettings.SHIP_VOLUME = shipVolumeBackup;
                GameSettings.AMBIENCE_VOLUME = ambianceVolumeBackup;
            }

            if (ProbeControlRoomSettings.Instance.DisableWobble)
            {
                ProbeControlRoomUtils.Logger.message("OnDestroy() - DisableWobble - RESTORE");
                //re-enable camera wobble
                GameSettings.FLT_CAMERA_WOBBLE = cameraWobbleBackup;
                GameSettings.CAMERA_FX_INTERNAL = cameraFXInternalBackup;
                GameSettings.CAMERA_FX_EXTERNAL = cameraFXExternalBackup;
            }

            ProbeControlRoomUtils.Logger.debug("OnDestroy()");
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
        {
            get
            {
                return Instance.canPCRIVA;
            }
        }

        public static bool vesselCanStockIVA
        {
            get
            {
                return Instance.canStockIVA;
            }
        }


        public bool startIVA()
        {
            ProbeControlRoomUtils.Logger.debug("startIVA()");
            Transform actualTransform;
            if (!canPCRIVA)
            {
                ProbeControlRoomUtils.Logger.message("startIVA() - Refresh rooms said there were no IVAs available! Can't start.");
                return false;
            }

            if (aPart == null)
            {
                ProbeControlRoomUtils.Logger.message("startIVA() Lost our part, refreshing");
                refreshVesselRooms();
            }
            if (aPart.FindModulesImplementing<ProbeControlRoomPart>().Count == 0)
            {
                ProbeControlRoomUtils.Logger.error("startIVA() a module was not found on the part now, exiting");
                return false;
            }
            aModule = aPart.FindModulesImplementing<ProbeControlRoomPart>()[0];
            aPart.MakeReferencePart();
            actualTransform = aPart.internalModel.FindModelTransform(aModule.seatTransformName);

            if (Transform.Equals(actualTransform, null))
            {
                ProbeControlRoomUtils.Logger.error("startIVA(Part) - NULL on actualTransform-seatTransformName, using fallback...");
                actualTransform = aPart.internalModel.FindModelTransform("Seat");
            }
            else
            {
                ProbeControlRoomUtils.Logger.message("startIVA(Part) - Seat: " + aModule.seatTransformName.ToString());
            }

            ProbeControlRoomUtils.Logger.debug("startIVA() - fire up IVA");


            //disable sound
            shipVolumeBackup = GameSettings.SHIP_VOLUME;
            ambianceVolumeBackup = GameSettings.AMBIENCE_VOLUME;
            if (ProbeControlRoomSettings.Instance.DisableSounds)
            {
                ProbeControlRoomUtils.Logger.message("startIVA() - DisableSounds");
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

            if (ProbeControlRoomSettings.Instance.DisableWobble)
            {
                ProbeControlRoomUtils.Logger.message("startIVA() - DisableWobble");
                GameSettings.FLT_CAMERA_WOBBLE = 0;
                GameSettings.CAMERA_FX_INTERNAL = 0;
                GameSettings.CAMERA_FX_EXTERNAL = 0;
            }
            // TODO: create cfg file with cached vars, on crash to be restored



            FlightCamera.fetch.EnableCamera();
            FlightCamera.fetch.DeactivateUpdate();
            FlightCamera.fetch.gameObject.SetActive(true);

            InternalCamera.Instance.SetTransform(actualTransform, true);
            InternalCamera.Instance.EnableCamera();


            IVASun sunBehaviour;
            sunBehaviour = (IVASun)FindObjectOfType(typeof(IVASun));
            sunBehaviour.enabled = false;




            if (UIPartActionController.Instance != null)
                UIPartActionController.Instance.Deactivate();


            CameraManager.Instance.SetCameraInternal(aPart.internalModel, actualTransform);
            //          CameraManager.Instance.SetCameraMode(CameraManager.CameraMode.Internal);
            ProbeControlRoomUtils.Logger.debug("startIVA() - DONE");

            appLauncherButton.SetTexture(IconDeactivate);
            isActive = true;

            return true;

        }


        public void stopIVA()
        {
            ProbeControlRoomUtils.Logger.debug("stopIVA()");
            isActive = false;


            if (ProbeControlRoomSettings.Instance.DisableSounds)
            {
                ProbeControlRoomUtils.Logger.message("stopIVA() - DisableSounds - RESTORE");
                //re-enable sound
                GameSettings.SHIP_VOLUME = shipVolumeBackup;
                GameSettings.AMBIENCE_VOLUME = ambianceVolumeBackup;
                //GameSettings.MUSIC_VOLUME = musicVolumeBackup;
                //GameSettings.UI_VOLUME = uiVolumeBackup;
                //GameSettings.VOICE_VOLUME = voiceVolumeBackup;
            }

            if (ProbeControlRoomSettings.Instance.DisableWobble)
            {
                ProbeControlRoomUtils.Logger.message("stopIVA() - DisableWobble - RESTORE");
                //re-enable camera wobble
                GameSettings.FLT_CAMERA_WOBBLE = cameraWobbleBackup;
                GameSettings.CAMERA_FX_INTERNAL = cameraFXInternalBackup;
                GameSettings.CAMERA_FX_EXTERNAL = cameraFXExternalBackup;
            }

            CameraManager.ICameras_DeactivateAll();

            CameraManager.Instance.SetCameraFlight();

            if (UIPartActionController.Instance != null)
                UIPartActionController.Instance.Activate();

            ProbeControlRoomUtils.Logger.debug("stopIVA() - CHECKMARK");
            appLauncherButton.SetTexture(IconActivate);
        }

        public void Update()
        {
            var scene = HighLogic.LoadedScene;
            if (scene == GameScenes.FLIGHT)
            {
                if (isActive)
                {
                    if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA)
                    {
                        ProbeControlRoomUtils.Logger.message("OnUpdate() - real IVA detected, ending...");
                        stopIVA();
                        if (ProbeControlRoomSettings.Instance.ForcePCROnly)
                        {
                            ProbeControlRoomUtils.Logger.message("OnUpdate() - real IVA detected, ending... KILLED - ForcePCROnly Enabled.");
                            startIVA();
                        }
                    }



                    if (!MapView.MapIsEnabled && !InternalCamera.Instance.isActive)
                    {
                        ProbeControlRoomUtils.Logger.message("OnUpdate() - IVA broke, kill and maybe restart...");
                        // TODO directReturn from map is broken in case vessel has a "real IVA"
                        // Has IVA and no kerbal     		== OK    (cockpit)
                        // Has IVA and kerbal        		== ERROR (cockpit)
                        // Has IVA and no kerbal     		== OK    (CrewContainer)
                        // Has IVA and kerbal        		== ERROR (CrewContainer)
                        // Has No IVA and no Kerbal  		== OK    (Lab)
                        // has No IVA and kerbal     		== OK    (Lab)

                        stopIVA();
                        startIVA();
                    }
                    if (!MapView.MapIsEnabled && Input.GetKeyDown(GameSettings.CAMERA_MODE.primary))
                    {
                        ProbeControlRoomUtils.Logger.message("OnUpdate() - CAMERA_MODE.key seen, stopIVA()");
                        if (ProbeControlRoomSettings.Instance.ForcePCROnly)
                        {
                            ProbeControlRoomUtils.Logger.message("OnUpdate() - CAMERA_MODE.key seen, stopIVA() KILLED - ForcePCROnly Enabled.");
                        }
                        else
                        {
                            stopIVA();
                        }
                    }
                }
                else
                {
                    if (!canStockIVA && canPCRIVA && Input.GetKeyDown(GameSettings.CAMERA_MODE.primary))
                    {
                        ProbeControlRoomUtils.Logger.message("OnUpdate() - CAMERA_MODE.key seen, startIVA()");
                        startIVA();
                    }
                }
            }
            else
            {
                if (isActive)
                {
                    ProbeControlRoomUtils.Logger.error("OnUpdate() - stopping, active while not in FLIGHT");
                    stopIVA();
                }
            }
        }

        private void OnVesselChange(Vessel v)
        {
            ProbeControlRoomUtils.Logger.message("OnVesselChange(Vessel)");
            vesselModified();
        }
        private void OnVesselModified(Vessel v)
        {
            ProbeControlRoomUtils.Logger.message("onVesselWasModified(Vessel)");
            vesselModified();
        }

        private void vesselModified()
        {
            ProbeControlRoomUtils.Logger.message("vesselModified()");
            Part oldPart = aPart;
            refreshVesselRooms();
            //Only stop the IVA if the part is missing, restart it otherwise
            if (isActive)
            {
                if (!canPCRIVA)
                {
                    ProbeControlRoomUtils.Logger.message("vesselModified() - Can no longer use PCR on this vessel");
                    stopIVA();
                }

                if (aPart != oldPart)
                {
                    //Can still PCR IVA but the part has changed, restart
                    stopIVA();
                    startIVA();
                }
            }
        }

        /// <summary>
        /// Scans vessel for usable IVA rooms and PCR rooms and initializes them as neccessary
        /// </summary>
		private void refreshVesselRooms()
        {
            ProbeControlRoomUtils.Logger.debug("refreshVesselRooms()");

            Vessel vessel = FlightGlobals.ActiveVessel;

            //If the vessel is null, there is something wrong and no reason to continue scan
            if (vessel == null)
            {
                canStockIVA = false;
                aPart = null;
                aModule = null;
                ProbeControlRoomUtils.Logger.error("refreshVesselRooms() - ERROR: FlightGlobals.activeVessel is NULL");
                return;
            }

            if (vessel.parts.Contains(aPart))
            {
                ProbeControlRoomUtils.Logger.debug("refreshVesselRooms() - Old part still there, cleaning up extra rooms and returning");
                //Our old part is still there and active. Clean up extras as needed and return
                for (int i = 0; i < vessel.parts.Count; i++)
                {
                    Part p = vessel.parts[i];
                    if (p.GetComponent<ProbeControlRoomPart>() != null && aPart != p && p.protoModuleCrew.Count == 0 && p.internalModel != null)
                    {
                        ProbeControlRoomUtils.Logger.debug("refreshRooms() Found and destroying old PCR in " + p.ToString());
                        p.internalModel.gameObject.DestroyGameObject();
                        p.internalModel = null;
                    }
                }
                return;
            }


            canStockIVA = false;
            canPCRIVA = false;
            List<Part> rooms = new List<Part>();
            List<Part> pcrNoModel = new List<Part>();

            ProbeControlRoomUtils.Logger.message("refreshVesselRooms() - scanning vessel: " + vessel.ToString());


            //Look throught the list of parts and save those that have probe control room modules on them based on available internal models
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                ProbeControlRoomPart room = p.GetComponent<ProbeControlRoomPart>();
                if (room != null)
                {
                    if (p.internalModel != null)
                    {
                        //Check for stock IVA
                        if (p.protoModuleCrew.Count > 0)
                        {
                            ProbeControlRoomUtils.Logger.message("refreshVesselRooms() - Found Stock IVA with crew: " + p.ToString());
                            canStockIVA = true;
                        }
                        else
                        {
                            //No stock IVA possible, PCR model found
                            ProbeControlRoomUtils.Logger.message("refreshVesselRooms() - Found part with PCR IVA model: " + p.ToString());
                            rooms.Add(p);
                        }
                    }
                    else
                    {
                        //PCR Module noted but no active internal model found
                        ProbeControlRoomUtils.Logger.message("refreshVesselrooms() - Found PCR part but it has no model: " + p.ToString());
                        pcrNoModel.Add(p);
                    }


                }
            }

            //Clean up and specifiy active rooms
            if (rooms.Count > 0)
            {
                ProbeControlRoomUtils.Logger.message("refreshVesselRooms() - Cleaning up pcrNoModel List");
                pcrNoModel.Clear();
                pcrNoModel = null;


                //Select primary part for use and verify it's initialized
                ProbeControlRoomUtils.Logger.message("refreshVesselRooms() - Initializing room in " + aPart.ToString());
                aPart = rooms[0];
                aPart.internalModel.Initialize(aPart);
                aPart.internalModel.SetVisible(false);

                //Remove Excess internal models
                if (rooms.Count > 1)
                {
                    ProbeControlRoomUtils.Logger.debug("refreshVesselRooms() - Removing " + (rooms.Count - 1) + " Rooms");
                    for (int i = 1; i < rooms.Count; i++)
                    {
                        rooms[i].internalModel.gameObject.DestroyGameObject();
                        rooms[i].internalModel = null;
                    }
                }
                canPCRIVA = true;
                rooms.Clear();
                rooms = null;
                return;
            }

            //No useable PCR rooms were found.  Time to create one
            if (pcrNoModel.Count > 0)
            {
                aPart = pcrNoModel[0];
                aPart.CreateInternalModel();
                ProbeControlRoomUtils.Logger.debug("refreshVesselRooms() - No active room with a model found, creating now in " + aPart.ToString());
                if (aPart.internalModel == null)
                {
                    //Something went wrong creating the model
                    ProbeControlRoomUtils.Logger.message("refreshVesselRooms() - ERROR creating internal model");
                    return;
                }
                aPart.internalModel.Initialize(aPart);
                aPart.internalModel.SetVisible(false);
                canPCRIVA = true;
                return;
            }

            pcrNoModel.Clear();
            pcrNoModel = null;
            return;
        }
    }
}

