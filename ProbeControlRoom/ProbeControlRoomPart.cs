using UnityEngine;

namespace ProbeControlRoom
{
	public class ProbeControlRoomPart : PartModule
	{
		[KSPField]
		public string seatTransformName = "Seat";

		[KSPEvent(guiActive = true, guiName = "IVA ProbeControl")]
		public void EventActivateProbeControlRoomPart()
		{
			ProbeControlRoomUtils.Logger.message ("[ProbeControlRoom][ProbeControlRoomPart] EventActivateProbeControlRoomPart()");
			bool ivaLancher = ProbeControlRoom.Instance.startIVA (this.part);
		}


		public override void OnAwake()
		{
			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom][ProbeControlRoomPart] OnAwake()");
		}

		public override void OnStart(StartState state)
		{
			ProbeControlRoomUtils.Logger.debug ("[ProbeControlRoom][ProbeControlRoomPart] OnStart()");
		}
	}
}

