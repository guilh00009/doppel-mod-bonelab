using System;
using System.Collections.Generic;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class HeadsetGuard
{
	public static float SwappedAt = -100f;

	private const float AfterSwap = 8f;

	private static readonly HashSet<IntPtr> said = new HashSet<IntPtr>();

	public static void Update()
	{
		if (Time.time - SwappedAt > 8f)
		{
			return;
		}
		RigManager rigManager = Player.RigManager;
		if ((Object)(object)rigManager == (Object)null || !((Component)rigManager).gameObject.activeInHierarchy)
		{
			return;
		}
		try
		{
			foreach (Camera allCamera in Camera.allCameras)
			{
				if ((Object)(object)allCamera == (Object)null || !((Behaviour)allCamera).enabled || (Object)(object)allCamera.targetTexture != (Object)null || (int)allCamera.stereoTargetEye == 0)
				{
					continue;
				}
				RigManager componentInParent = ((Component)allCamera).GetComponentInParent<RigManager>();
				if (!((Object)(object)componentInParent == (Object)null) && !((Object)(object)componentInParent == (Object)(object)rigManager))
				{
					((Behaviour)allCamera).enabled = false;
					AudioListener component = ((Component)allCamera).GetComponent<AudioListener>();
					if ((Object)(object)component != (Object)null)
					{
						((Behaviour)component).enabled = false;
					}
					if (said.Add(((Il2CppObjectBase)(object)allCamera).Pointer))
					{
						MelonLogger.Warning("Bot Player: a second player body (" + ((Object)componentInParent).name + ") had a headset camera on: switched off");
					}
					if (!BotRig.IsBot(componentInParent))
					{
						Remove(componentInParent);
					}
				}
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player headset guard: " + ex.Message);
		}
	}

	private static void Remove(RigManager rig)
	{
		try
		{
			Poolee componentInParent = ((Component)rig).GetComponentInParent<Poolee>();
			if ((Object)(object)componentInParent != (Object)null)
			{
				componentInParent.Despawn();
			}
			else
			{
				Object.Destroy((Object)(object)((Component)rig).gameObject);
			}
			MelonLogger.Msg("Bot Player: the second player body was taken away");
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: the blank body could not be taken away: " + ex.Message);
		}
	}

	public static void ForgetLevel()
	{
		SwappedAt = -100f;
		said.Clear();
	}

	public static void OnSpawn(Poolee poolee)
	{
		try
		{
			if (!((Object)(object)poolee == (Object)null) && !((Object)(object)poolee.SpawnableCrate == (Object)null))
			{
				string iD = ((Scannable)poolee.SpawnableCrate).Barcode.ID;
				if (Clone.IsBody(iD, ((Object)((Component)poolee).gameObject).name) || !((Object)(object)((Component)poolee).GetComponentInChildren<RigManager>(true) == (Object)null))
				{
					MelonLogger.Msg("Bot Player: a player body was spawned (" + iD + ")");
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
