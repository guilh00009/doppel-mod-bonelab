using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class HoldLibrary
{
	public class Frame
	{
		public Bot.Pose thingInHead;

		public bool two;

		public Bot.Pose leftInThing;
	}

	private static readonly Dictionary<string, List<Frame>> byItem = new Dictionary<string, List<Frame>>();

	private static int filesRead = -1;

	private static float checkAt;

	public static List<Frame> For(string key)
	{
		if (Time.time >= checkAt)
		{
			checkAt = Time.time + 20f;
			Load();
		}
		if (!byItem.TryGetValue(key, out var value) || value.Count < 20)
		{
			return null;
		}
		return value;
	}

	private static void Load()
	{
		try
		{
			string path = AssetFiles.SaveFolder("learn");
			if (!Directory.Exists(path))
			{
				return;
			}
			string[] files = Directory.GetFiles(path, "items_*.csv");
			if (files.Length == filesRead)
			{
				return;
			}
			filesRead = files.Length;
			byItem.Clear();
			int num = 0;
			string[] array = files;
			foreach (string path2 in array)
			{
				int num2 = 0;
				using FileStream stream = new FileStream(path2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				using StreamReader streamReader = new StreamReader(stream);
				streamReader.ReadLine();
				string text;
				while ((text = streamReader.ReadLine()) != null)
				{
					if (num2++ % 2 != 0)
					{
						continue;
					}
					string[] array2 = text.Split(',');
					if (array2.Length >= 15 && array2[9].Length != 0 && P(array2[1], out var p) && P(array2[11], out var p2))
					{
						Frame frame = new Frame
						{
							thingInHead = p.Inverse() * p2
						};
						if (array2[4] == array2[9] && P(array2[7], out var p3))
						{
							frame.two = true;
							frame.leftInThing = p3;
						}
						if (!byItem.TryGetValue(array2[9], out var value))
						{
							value = (byItem[array2[9]] = new List<Frame>());
						}
						value.Add(frame);
						num++;
					}
				}
			}
			MelonLogger.Msg("Bot Player: your holds read: " + num + " moments of " + byItem.Count + " things");
			Lab.Event("holds: read " + num + " moments of " + byItem.Count + " things from " + files.Length + " recording(s)");
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: your holds could not be read: " + ex.Message);
		}
	}

	private static bool P(string s, out Bot.Pose p)
	{
		p = default(Bot.Pose);
		string[] array = s.Split(' ');
		if (array.Length < 7)
		{
			return false;
		}
		float[] array2 = new float[7];
		for (int i = 0; i < 7; i++)
		{
			if (!float.TryParse(array[i], NumberStyles.Float, CultureInfo.InvariantCulture, out array2[i]))
			{
				return false;
			}
		}
		Vector3 position = new Vector3(array2[0], array2[1], array2[2]);
		Quaternion val = new Quaternion(array2[3], array2[4], array2[5], array2[6]);
		p = new Bot.Pose(position, ((Quaternion)(ref val)).normalized);
		return true;
	}
}
