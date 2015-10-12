using UnityEngine;
using ProbeControlRoom;
using Toolbar;

namespace ProbeControlRoomToolbar 
{
	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	class ProbeControlRoomToolbar : MonoBehaviour {
		private string enabledTexture = "ProbeControlRoom/ProbeControlRoomToolbarEnabled";
		private string disabledTexture = "ProbeControlRoom/ProbeControlRoomToolbarDisabled";
		private IButton button;


		private void Start()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				this.button = ToolbarManager.Instance.add("PCR", "ProbeControlRoomButton");
				this.button.ToolTip = "ProbeControlRoom";

				this.button.OnClick += (e) =>
				{
					if (ProbeControlRoom.ProbeControlRoom.isActive) {
						// ignore ProbeControlRoomSettings..ForcePCROnly on purpose
						ProbeControlRoom.ProbeControlRoom.Instance.stopIVA ();
					} else {
						ProbeControlRoom.ProbeControlRoom.Instance.startIVA ();
					}
				};
			}
		}

		private void LateUpdate()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				if (ProbeControlRoom.ProbeControlRoom.Instance != null) {
					ButtonVisibility (ProbeControlRoom.ProbeControlRoom.vesselCanIVA && !MapView.MapIsEnabled);
					ButtonState (ProbeControlRoom.ProbeControlRoom.isActive);
					if (ProbeControlRoom.ProbeControlRoomUI.Instance != null) {
						ProbeControlRoom.ProbeControlRoomUI.messageThatToolbarIsActive ();
					}
				}
			}
		}

		private void OnDestroy()
		{
			if (button != null)
			{
				button.Destroy();
			}
		}

		private void ButtonVisibility(bool visible)
		{
			if (this.button.Visible != visible)
			{
				this.button.Visible = visible;
			}
		}

		private void ButtonState(bool state)
		{
			if (state)
			{
				this.button.TexturePath = this.enabledTexture;
			}
			else
			{
				this.button.TexturePath = this.disabledTexture;
			}
		}
	}
}