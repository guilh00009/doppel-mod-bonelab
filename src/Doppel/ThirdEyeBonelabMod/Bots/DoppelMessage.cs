using LabFusion.Network;
using LabFusion.SDK.Modules;

namespace ThirdEyeBonelabMod.Bots;

public class DoppelMessage : ModuleMessageHandler
{
	protected override void OnHandleMessage(ReceivedMessage received)
	{
		if (received.Sender.HasValue)
		{
			DoppelData doppelData = received.ReadData<DoppelData>();
			NetBots.Receive(received.Sender.Value, doppelData.bytes);
		}
	}
}
