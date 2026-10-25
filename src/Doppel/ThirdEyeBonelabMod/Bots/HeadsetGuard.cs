using System;
using System.Collections.Generic;
using BoneLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Pool;
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
		if (rigManager == null || !rigManager.gameObject.activeInHierarchy)
		{
			return;
		}
		try
		{
			foreach (Camera allCamera in Camera.allCameras)
			{
				if (allCamera == null || !allCamera.enabled || allCamera.targetTexture != null || allCamera.stereoTargetEye == StereoTargetEyeMask.None)
				{
					continue;
				}
				RigManager componentInParent = allCamera.GetComponentInParent<RigManager>();
				if (!(componentInParent == null) && !(componentInParent == rigManager))
				{
					allCamera.enabled = false;
					AudioListener component = allCamera.GetComponent<AudioListener>();
					if (component != null)
					{
						component.enabled = false;
					}
					if (said.Add(allCamera.Pointer))
					{
						MelonLogger.Warning("Bot Player: a second player body (" + componentInParent.name + ") had a headset camera on: switched off");
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
			Poolee componentInParent = rig.GetComponentInParent<Poolee>();
			if (componentInParent != null)
			{
				componentInParent.Despawn();
			}
			else
			{
				UnityEngine.Object.Destroy(rig.gameObject);
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
			if (!(poolee == null) && !(poolee.SpawnableCrate == null))
			{
				string iD = poolee.SpawnableCrate.Barcode.ID;
				if (Clone.IsBody(iD, poolee.gameObject.name) || !(poolee.GetComponentInChildren<RigManager>(includeInactive: true) == null))
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
