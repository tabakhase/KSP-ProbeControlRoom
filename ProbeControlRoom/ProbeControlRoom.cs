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
        //Instance reference
        public static ProbeControlRoom Instance { get; protected set; }

        //Is IVA currently active and visible
        public static bool isActive = false;
        
        //Vessel has IVA with crew onboard
        private bool canStockIVA;
        
        //Vessel has a probe control room available
        private bool canPCRIVA;
        
        //Current PCR module
        private ProbeControlRoomPart aModule;

        //Current PCR part with internal module
        private Part aPart;
        
        //Storage for original game settings
        float shipVolumeBackup = GameSettings.SHIP_VOLUME;
        float ambianceVolumeBackup = GameSettings.AMBIENCE_VOLUME;
        float cameraWobbleBackup = GameSettings.FLT_CAMERA_WOBBLE;
        float cameraFXInternalBackup = GameSettings.CAMERA_FX_INTERNAL;
        float cameraFXExternalBackup = GameSettings.CAMERA_FX_EXTERNAL;

        //Application 
        private static ApplicationLauncherButton appLauncherButton = null;
        //App launcher in use
        private bool AppLauncher = false;

        //App launcher icons
        private Texture2D IconActivate = null;
        private Texture2D IconDeactivate = null;

        /// <summary>
        /// Startup and initialization
        /// </summary>
        public void Start()
        {
            ProbeControlRoomUtils.Logger.debug("Start()");
            Instance = this;

            refreshVesselRooms();

            //Register game events
            GameEvents.onVesselWasModified.Fire(FlightGlobals.ActiveVessel);
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onVesselWasModified.Add(OnVesselModified);
            GameEvents.onGUIApplicationLauncherReady.Add(onGUIApplicationLauncherReady);

            //If Manely mode is set true, force straight into IVA
            if (ProbeControlRoomSettings.Instance.ForcePCROnly)
            {
                ProbeControlRoomUtils.Logger.message("Start() - ForcePCROnly Enabled.");
                startIVA();
            }
        }

        /// <summary>
        /// Setup app launcher button when the GUI is ready
        /// </summary>
        private void onGUIApplicationLauncherReady()
        {
            if (!AppLauncher)
            {
                appLauncherButton = InitializeApplicationButton();
                AppLauncher = true;
                if(!canPCRIVA)
                {
                    appLauncherButton.Disable();
                }
            }
           
        }

        /// <summary>
        /// Sets up the App launcher button to activate and deactivate PCR
        /// </summary>
        /// <returns>Reference to created button</returns>
        ApplicationLauncherButton InitializeApplicationButton()
        {
            ApplicationLauncherButton Button = null;

            IconActivate = GameDatabase.Instance.GetTexture("ProbeControlRoom/Icons/ProbeControlRoomToolbarDisabled", false);
            IconDeactivate = GameDatabase.Instance.GetTexture("ProbeControlRoom/Icons/ProbeControlRoomToolbarEnabled", false);


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

        /// <summary>
        /// Static function for Toolbar integration
        /// </summary>
        public static bool vesselCanIVA
        {
            get {
                return Instance.canPCRIVA;
            }
        }

        /// <summary>
        /// Toggles PCR mode on
        /// </summary>
        void OnAppLauncherTrue()
        {
            toggleIVA();
        }

        /// <summary>
        /// Toggles PCR mode off
        /// </summary>
        void OnAppLauncherFalse()
        {
            toggleIVA();
        }

        /// <summary>
        /// Activates and deactivates PCR as well as changes icon in the app launcher
        /// </summary>
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

        /// <summary>
        /// Shuts down IVA and restores all game settings
        /// </summary>
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

        /// <summary>
        /// Sets up IVA and activtes it
        /// </summary>
        /// <returns>True if successful, False on error</returns>
        public bool startIVA()
        {
            ProbeControlRoomUtils.Logger.debug("startIVA()");
            Transform actualTransform;

            //Verify active room available
            if (!canPCRIVA)
            {
                ProbeControlRoomUtils.Logger.message("startIVA() - Refresh rooms said there were no IVAs available! Can't start.");
                return false;
            }

            //Ensure part still exists
            if (aPart == null)
            {
                ProbeControlRoomUtils.Logger.message("startIVA() Lost our part, refreshing");
                refreshVesselRooms();
            }

            //Setup module for transforms
            if (aPart.FindModulesImplementing<ProbeControlRoomPart>().Count == 0)
            {
                ProbeControlRoomUtils.Logger.error("startIVA() a module was not found on the part now, exiting");
                return false;
            }
            aModule = aPart.FindModulesImplementing<ProbeControlRoomPart>()[0];

            //Make the PCR part the focused part
            aPart.MakeReferencePart();

            //Setup camera transform for internal seat
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


            //Unsure of this purpose at the moment
            FlightCamera.fetch.EnableCamera();
            FlightCamera.fetch.DeactivateUpdate();
            FlightCamera.fetch.gameObject.SetActive(true);

            //Move internal camera to the correct position and enable
            InternalCamera.Instance.SetTransform(actualTransform, true);
            InternalCamera.Instance.EnableCamera();

            //Disable sun effects inside of IVA
            IVASun sunBehaviour;
            sunBehaviour = (IVASun)FindObjectOfType(typeof(IVASun));
            sunBehaviour.enabled = false;

            
            if (UIPartActionController.Instance != null)
                UIPartActionController.Instance.Deactivate();

            //Activate internal camera
            CameraManager.Instance.SetCameraInternal(aPart.internalModel, actualTransform);

            ProbeControlRoomUtils.Logger.debug("startIVA() - DONE");

            //Change app launcher button icon
            appLauncherButton.SetTexture(IconDeactivate);

            isActive = true;

            return true;

        }

        /// <summary>
        /// Shuts down current PCR IVA
        /// </summary>
        public void stopIVA()
        {

            ProbeControlRoomUtils.Logger.debug("stopIVA()");
            isActive = false;
            
            //Restore settings to levels prior to entering IVA
            if (ProbeControlRoomSettings.Instance.DisableSounds)
            {
                ProbeControlRoomUtils.Logger.message("stopIVA() - DisableSounds - RESTORE");
                //re-enable sound
                GameSettings.SHIP_VOLUME = shipVolumeBackup;
                GameSettings.AMBIENCE_VOLUME = ambianceVolumeBackup;
            }

            if (ProbeControlRoomSettings.Instance.DisableWobble)
            {
                ProbeControlRoomUtils.Logger.message("stopIVA() - DisableWobble - RESTORE");
                //re-enable camera wobble
                GameSettings.FLT_CAMERA_WOBBLE = cameraWobbleBackup;
                GameSettings.CAMERA_FX_INTERNAL = cameraFXInternalBackup;
                GameSettings.CAMERA_FX_EXTERNAL = cameraFXExternalBackup;
            }

            //Switch back to normal cameras
            CameraManager.ICameras_DeactivateAll();
            CameraManager.Instance.SetCameraFlight();

            if (UIPartActionController.Instance != null)
                UIPartActionController.Instance.Activate();

            ProbeControlRoomUtils.Logger.debug("stopIVA() - CHECKMARK");

            //Change app launcher button
            appLauncherButton.SetTexture(IconActivate);
        }

        /// <summary>
        /// Check for invalid PCR states and controls inputs for acivation/deactivation
        /// </summary>
        public void Update()
        {
            // PCR should only be active during Flight
            var scene = HighLogic.LoadedScene;
            if (scene == GameScenes.FLIGHT)
            {
                if (isActive)
                {
                    //If IVA Camera is active, there is a valid and operational stock IVA and PCR needs to be shutdown
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


                    // PCR should be active but the InternalCamera is not, something broke, restart PCR
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

                    //Check for imput to stop IVA
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
                    // Listen for keyboard input to start PCR unless a valid IVA exists.  PCR can still be started via AppLauncher or toolbar
                    if (!canStockIVA && canPCRIVA && Input.GetKeyDown(GameSettings.CAMERA_MODE.primary))
                    {
                        ProbeControlRoomUtils.Logger.message("OnUpdate() - CAMERA_MODE.key seen, startIVA()");
                        startIVA();
                    }
                }
            }
            else
            {
                //Shutdown PCR if not in flight
                if (isActive)
                {
                    ProbeControlRoomUtils.Logger.error("OnUpdate() - stopping, active while not in FLIGHT");
                    stopIVA();
                }
            }
        }

        /// <summary>
        /// Vessel has changed to a new one, check IVA status (docking, vessel switching, etc.)
        /// </summary>
        /// <param name="v">New Vessel</param>
        private void OnVesselChange(Vessel v)
        {
            ProbeControlRoomUtils.Logger.message("OnVesselChange(Vessel)");
            vesselModified();
        }

        /// <summary>
        /// Vessel has been modified, check IVA status (Staging, RUD, etc.)
        /// </summary>
        /// <param name="v">New Vessel</param>
        private void OnVesselModified(Vessel v)
        {
            ProbeControlRoomUtils.Logger.message("onVesselWasModified(Vessel)");
            vesselModified();
        }

        /// <summary>
        /// Rebuild room and IVA information and reset PCR as needed
        /// </summary>
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

            // If our current vessel still has the old PCR part, keep it active
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

            //No current active PCR found, time to create a new one
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
            }
            else if(pcrNoModel.Count > 0)
            {
                // No parts with an available internal model were found, attempting to create one
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
            }

            pcrNoModel.Clear();
            pcrNoModel = null;

            // Set app launcher availability based on current layout.
            if(AppLauncher)
            {
                if (canPCRIVA)
                {
                    appLauncherButton.Enable();
                }
                else
                {
                    appLauncherButton.Disable();
                }
            }
            return;
        }
    }
}

