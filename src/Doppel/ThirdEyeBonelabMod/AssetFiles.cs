using System.IO;
using MelonLoader.Utils;

namespace ThirdEyeBonelabMod;

public static class AssetFiles
{
	public static string Root = "Mod";

	public static string SaveFolder(string name)
	{
		return Path.Combine(MelonEnvironment.UserDataDirectory, Root, name);
	}
}
