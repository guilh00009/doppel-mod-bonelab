using LabFusion.Network;
using LabFusion.SDK.Modules;

namespace ThirdEyeBonelabMod.Bots;

public class DoppelMessage : ModuleMessageHandler
{
	protected override void OnHandleMessage(ReceivedMessage received)
	{
		if (((ReceivedMessage)(ref received)).Sender.HasValue)
		{
			DoppelData doppelData = ((ReceivedMessage)(ref received)).ReadData<DoppelData>();
			NetBots.Receive(((ReceivedMessage)(ref received)).Sender.Value, doppelData.bytes);
		}
	}
}
