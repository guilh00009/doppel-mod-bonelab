using System;
using BoneLib.BoneMenu;
using MelonLoader;
using ThirdEyeBonelabMod;
using ThirdEyeBonelabMod.Bots;
using UnityEngine;

namespace DoppelMod;

public class Mod : MelonMod
{
	private static bool started;

	private static float errorSaidAt = -10f;

	public override void OnInitializeMelon()
	{
		AssetFiles.Root = "Bot Player";
		try
		{
			Entry.Start(Page.Root.CreatePage("Doppel", Color.cyan, 0, true));
			started = true;
			MelonLogger.Msg("Doppel ready (BoneMenu > Doppel)");
		}
		catch (Exception ex)
		{
			MelonLogger.Error("Doppel could not start: " + ex);
		}
	}

	public override void OnUpdate()
	{
		Run(Entry.Update);
	}

	public override void OnFixedUpdate()
	{
		Run(Entry.FixedUpdate);
	}

	public override void OnDeinitializeMelon()
	{
		if (started)
		{
			Run(Entry.Stop);
		}
	}

	private static void Run(Action step)
	{
		if (!started)
		{
			return;
		}
		try
		{
			step();
		}
		catch (Exception ex)
		{
			if (Time.realtimeSinceStartup - errorSaidAt > 5f)
			{
				errorSaidAt = Time.realtimeSinceStartup;
				MelonLogger.Error("Doppel: " + ex);
			}
		}
	}
}
