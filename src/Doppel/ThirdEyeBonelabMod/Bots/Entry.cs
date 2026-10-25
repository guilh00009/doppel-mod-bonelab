using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using BoneLib.BoneMenu;
using HarmonyLib;
using MelonLoader;

namespace ThirdEyeBonelabMod.Bots;

public static class Entry
{
	private const string StarterVersion = "1.4.0";

	private static bool netOn;

	private static void Starter()
	{
		try
		{
			string path = AssetFiles.SaveFolder("");
			string text = Path.Combine(path, "starter");
			string path2 = Path.Combine(path, "starter_done.txt");
			if (!Directory.Exists(text) || (File.Exists(path2) && File.ReadAllText(path2).Contains("[1.4.0]")))
			{
				return;
			}
			int num = 0;
			string[] files = Directory.GetFiles(text, "*", SearchOption.AllDirectories);
			foreach (string text2 in files)
			{
				string text3 = Path.Combine(path, text2.Substring(text.Length).TrimStart('\\', '/'));
				if (!File.Exists(text3))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(text3));
					File.Copy(text2, text3);
					num++;
				}
			}
			File.WriteAllText(path2, "[1.4.0] The recordings the mod came with were put in place (" + num + " files). Delete this file to have any that are missing put back.");
			MelonLogger.Msg("Bot Player: the recordings it came with were put in place (" + num + " files)");
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: could not put its starter recordings in place: " + ex.Message);
		}
	}

	public static void Start(Page page)
	{
		AssetFiles.Root = "Bot Player";
		Starter();
		HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("RudyHill.BotBrain." + Guid.NewGuid().ToString("N"));
		int num = 0;
		try
		{
			Assembly assembly = typeof(Entry).Assembly;
			List<Assembly> list = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
			foreach (AssemblyLoadContext item in AssemblyLoadContext.All)
			{
				foreach (Assembly assembly2 in item.Assemblies)
				{
					if (!list.Contains(assembly2))
					{
						list.Add(assembly2);
					}
				}
			}
			foreach (Assembly item2 in list)
			{
				string name;
				try
				{
					name = item2.GetName().Name;
				}
				catch (Exception)
				{
					continue;
				}
				if (item2 == assembly || !name.StartsWith("BotBrain", StringComparison.Ordinal))
				{
					continue;
				}
				Type type = item2.GetType(typeof(BotManager).FullName);
				if (type == null)
				{
					continue;
				}
				num++;
				FieldInfo fieldInfo = ((type != null) ? type.GetField("shutDown", BindingFlags.Static | BindingFlags.NonPublic) : null);
				if (fieldInfo == null || (bool)fieldInfo.GetValue(null))
				{
					continue;
				}
				try
				{
					type.GetMethod("Shutdown").Invoke(null, null);
					MelonLogger.Msg("Bot brain: an earlier build that had not been stopped was stopped");
				}
				catch (Exception ex2)
				{
					MelonLogger.Msg("Bot brain: an earlier build could not be stopped: " + (ex2.InnerException ?? ex2).Message);
					try
					{
						item2.GetType(typeof(Lab).FullName).GetMethod("Shutdown").Invoke(null, null);
					}
					catch (Exception)
					{
					}
				}
			}
		}
		catch (Exception ex4)
		{
			MelonLogger.Msg("Bot brain: looking for earlier builds failed: " + ex4.Message);
		}
		MelonLogger.Msg("Bot brain: " + num + " earlier builds in memory");
		try
		{
			BotManager.Initialize(page, harmony);
		}
		catch (Exception)
		{
			try
			{
				BotManager.Shutdown();
			}
			catch (Exception)
			{
			}
			throw;
		}
		MelonLogger.Msg("Bot brain started");
	}

	public static void Update()
	{
		BotManager.Update();
	}

	public static void FixedUpdate()
	{
		BotManager.FixedUpdate();
	}

	public static void Stop()
	{
		BotManager.Shutdown();
	}

	public static void NetStart()
	{
		try
		{
			bool flag = false;
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				if (assemblies[i].GetName().Name == "LabFusion")
				{
					flag = true;
				}
			}
			if (flag)
			{
				NetLink(on: true);
				netOn = true;
				MelonLogger.Msg("Bot Player: Fusion is here: bots are shown to the other players");
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: could not join up with Fusion (bots stay your own): " + (ex.InnerException ?? ex).Message);
		}
	}

	public static void NetStop()
	{
		try
		{
			NetBots.Forget();
			if (netOn)
			{
				NetLink(on: false);
			}
			netOn = false;
		}
		catch (Exception)
		{
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void NetLink(bool on)
	{
		if (on)
		{
			FusionLink.Start();
		}
		else
		{
			FusionLink.Stop();
		}
	}
}
