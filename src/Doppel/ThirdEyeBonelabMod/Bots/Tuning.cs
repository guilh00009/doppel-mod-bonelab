using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class Tuning
{
	private static readonly Dictionary<string, float> values = new Dictionary<string, float>();

	private static readonly SortedDictionary<string, float> known = new SortedDictionary<string, float>();

	private static DateTime readAt;

	private static float checkAt;

	private static bool unwritten;

	private static string File => Path.Combine(Lab.Folder, "tuning.txt");

	public static float F(string name, float otherwise)
	{
		if (!known.ContainsKey(name))
		{
			known[name] = otherwise;
			unwritten = true;
		}
		if (!values.TryGetValue(name, out var value))
		{
			return otherwise;
		}
		return value;
	}

	public static Vector3 V(string name, Vector3 otherwise)
	{
		return new Vector3(F(name + ".x", otherwise.x), F(name + ".y", otherwise.y), F(name + ".z", otherwise.z));
	}

	public static void Update()
	{
		if (Time.time < checkAt)
		{
			return;
		}
		checkAt = Time.time + 1f;
		try
		{
			if (System.IO.File.Exists(File))
			{
				DateTime lastWriteTimeUtc = System.IO.File.GetLastWriteTimeUtc(File);
				if (lastWriteTimeUtc != readAt)
				{
					readAt = lastWriteTimeUtc;
					Read();
				}
			}
			if (unwritten)
			{
				Write();
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: tuning file: " + ex.Message);
		}
	}

	private static void Read()
	{
		values.Clear();
		string[] array = System.IO.File.ReadAllLines(File);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.Length == 0 || text.StartsWith("#", StringComparison.Ordinal))
			{
				continue;
			}
			int num = text.IndexOf('=');
			if (num <= 0)
			{
				continue;
			}
			string key = text.Substring(0, num).Trim();
			if (float.TryParse(text.Substring(num + 1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				values[key] = result;
				if (!known.ContainsKey(key))
				{
					known[key] = result;
				}
			}
		}
		Lab.Event("tuning read (" + values.Count + " values)");
	}

	private static void Write()
	{
		unwritten = false;
		Directory.CreateDirectory(Lab.Folder);
		StringBuilder stringBuilder = new StringBuilder("# Bot Player tuning: change a value and save; the game reads it within a second.\n");
		foreach (KeyValuePair<string, float> item in known)
		{
			float value;
			float num = (values.TryGetValue(item.Key, out value) ? value : item.Value);
			stringBuilder.Append(item.Key).Append(" = ").Append(num.ToString("0.####", CultureInfo.InvariantCulture))
				.Append('\n');
		}
		System.IO.File.WriteAllText(File, stringBuilder.ToString());
		readAt = System.IO.File.GetLastWriteTimeUtc(File);
	}
}
