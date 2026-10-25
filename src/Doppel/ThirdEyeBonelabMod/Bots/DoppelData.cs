using LabFusion.Network.Serialization;

namespace ThirdEyeBonelabMod.Bots;

public class DoppelData : INetSerializable
{
	public byte[] bytes;

	public int? GetSize()
	{
		return ((bytes != null) ? bytes.Length : 0) + 8;
	}

	public void Serialize(INetSerializer serializer)
	{
		serializer.SerializeValue(ref bytes);
	}
}
