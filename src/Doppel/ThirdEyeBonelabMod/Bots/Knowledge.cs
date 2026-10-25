using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class Knowledge
{
	public class GunRecord
	{
		public float seconds { get; set; }

		public int kills { get; set; }

		public int times { get; set; }
	}

	public class PlayerStyle
	{
		public List<float> gaps { get; set; } = new List<float>();


		public List<float> bursts { get; set; } = new List<float>();


		public List<float> pauses { get; set; } = new List<float>();


		public List<float> fightDistance { get; set; } = new List<float>();


		public List<float> giveGround { get; set; } = new List<float>();


		public List<float> reloadAt { get; set; } = new List<float>();


		public List<float> burstRounds { get; set; } = new List<float>();


		public List<float> strafe { get; set; } = new List<float>();

	}

	public class Store
	{
		public int version { get; set; } = 2;


		public Dictionary<string, float[]> weights { get; set; } = new Dictionary<string, float[]>();


		public Dictionary<string, int> tries { get; set; } = new Dictionary<string, int>();


		public Dictionary<string, GunRecord> guns { get; set; } = new Dictionary<string, GunRecord>();


		public PlayerStyle player { get; set; } = new PlayerStyle();


		public float fightDistance { get; set; } = -1f;


		public int kills { get; set; }

		public int closeCalls { get; set; }

		public float minutes { get; set; }
	}

	public static class Style
	{
		public static List<float> gaps => Data.player.gaps;

		public static List<float> bursts => Data.player.bursts;

		public static List<float> pauses => Data.player.pauses;

		public static List<float> fightDistance => Data.player.fightDistance;

		public static List<float> giveGround => Data.player.giveGround;

		public static List<float> reloadAt => Data.player.reloadAt;

		public static List<float> burstRounds
		{
			get
			{
				PlayerStyle player = Data.player;
				return player.burstRounds ?? (player.burstRounds = new List<float>());
			}
		}

		public static List<float> strafe
		{
			get
			{
				PlayerStyle player = Data.player;
				return player.strafe ?? (player.strafe = new List<float>());
			}
		}

		public static void Add(List<float> list, float value)
		{
			list.Add(value);
			if (list.Count > 300)
			{
				list.RemoveAt(0);
			}
			dirty = true;
		}

		public static float Sample(List<float> list, float otherwise)
		{
			if (list.Count < 8)
			{
				return otherwise;
			}
			return list[UnityEngine.Random.Range(0, list.Count)] * UnityEngine.Random.Range(0.88f, 1.12f);
		}

		public static float Median(List<float> list, float otherwise)
		{
			if (list.Count < 8)
			{
				return otherwise;
			}
			List<float> list2 = new List<float>(list);
			list2.Sort();
			return list2[list2.Count / 2];
		}

		public static float Mean(List<float> list, float otherwise)
		{
			if (list.Count < 8)
			{
				return otherwise;
			}
			float num = 0f;
			foreach (float item in list)
			{
				num += item;
			}
			return num / (float)list.Count;
		}
	}

	public static readonly string[] FeatureNames = new string[10] { "always", "enemy seen", "enemy close", "enemies many", "armed", "ammo low", "player far", "enemy lost", "close call lately", "player fighting" };

	private const int Kept = 300;

	private static Store store;

	private static bool dirty;

	private static float savedAt;

	public static int Features => FeatureNames.Length;

	private static string File => Path.Combine(AssetFiles.SaveFolder(""), "brain.json");

	public static Store Data
	{
		get
		{
			if (store == null)
			{
				Load();
			}
			return store;
		}
	}

	public static float FightDistance
	{
		get
		{
			if (Data.fightDistance > 0f)
			{
				return Data.fightDistance;
			}
			float num = Style.Median(Style.fightDistance, -1f);
			if (num > 0f)
			{
				return Mathf.Clamp(num, 3f, 25f);
			}
			if (!(MotionLibrary.PlayerFightDistance > 0f))
			{
				return 9f;
			}
			return MotionLibrary.PlayerFightDistance;
		}
	}

	private static void Load()
	{
		Knowledge.store = new Store();
		try
		{
			if (System.IO.File.Exists(File))
			{
				Store store = JsonSerializer.Deserialize<Store>(System.IO.File.ReadAllText(File));
				if (store != null && store.version >= 2)
				{
					Knowledge.store = store;
				}
				else if (store != null)
				{
					Knowledge.store.guns = store.guns ?? Knowledge.store.guns;
					Knowledge.store.fightDistance = store.fightDistance;
					Knowledge.store.kills = store.kills;
					Knowledge.store.closeCalls = store.closeCalls;
					Knowledge.store.minutes = store.minutes;
				}
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: what it learned could not be read (starting afresh): " + ex.Message);
			Knowledge.store = new Store();
		}
		Store store2 = Knowledge.store;
		if (store2.player == null)
		{
			PlayerStyle playerStyle2 = (store2.player = new PlayerStyle());
		}
		store2 = Knowledge.store;
		if (store2.weights == null)
		{
			Dictionary<string, float[]> dictionary2 = (store2.weights = new Dictionary<string, float[]>());
		}
	}

	public static void Save()
	{
		if (store == null || !dirty)
		{
			return;
		}
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(File));
			System.IO.File.WriteAllText(File, JsonSerializer.Serialize(store, new JsonSerializerOptions
			{
				WriteIndented = true
			}));
			dirty = false;
			savedAt = Time.time;
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: what it learned could not be saved: " + ex.Message);
		}
	}

	public static void Tick(float dt, bool anyBot)
	{
		if (anyBot && store != null)
		{
			store.minutes += dt / 60f;
			if (Time.time - savedAt > 30f)
			{
				dirty = true;
				Save();
			}
		}
	}

	public static void Forget()
	{
		store = new Store();
		dirty = true;
		Save();
	}

	private static float[] Weights(string choice)
	{
		if (!Data.weights.TryGetValue(choice, out var value) || value == null || value.Length != Features)
		{
			value = (Data.weights[choice] = new float[Features]);
		}
		return value;
	}

	public static float Worth(string choice, float[] seen)
	{
		float[] array = Weights(choice);
		float num = 0f;
		for (int i = 0; i < Features; i++)
		{
			num += array[i] * seen[i];
		}
		return Mathf.Clamp(num, -1f, 1f);
	}

	public static int Tries(string choice)
	{
		if (!Data.tries.TryGetValue(choice, out var value))
		{
			return 0;
		}
		return value;
	}

	public static void Learn(string choice, float[] seen, float outcome)
	{
		float[] array = Weights(choice);
		float num = Mathf.Clamp(outcome, -1f, 1f) - Worth(choice, seen);
		float num2 = 0f;
		for (int i = 0; i < Features; i++)
		{
			num2 += seen[i] * seen[i];
		}
		Data.tries.TryGetValue(choice, out var value);
		Data.tries[choice] = value + 1;
		float num3 = Mathf.Lerp(0.5f, 0.15f, Mathf.Clamp01((float)value / 60f)) / Mathf.Max(num2, 1f);
		for (int j = 0; j < Features; j++)
		{
			array[j] = Mathf.Clamp(array[j] + num3 * num * seen[j], -2f, 2f);
		}
		dirty = true;
	}

	public static void HeldGun(string barcode, float seconds)
	{
		if (!string.IsNullOrEmpty(barcode))
		{
			Gun(barcode).seconds += seconds;
			dirty = true;
		}
	}

	public static void KilledWith(string barcode, float distance)
	{
		Data.kills++;
		if (!string.IsNullOrEmpty(barcode))
		{
			Gun(barcode).kills++;
		}
		float fightDistance = FightDistance;
		Data.fightDistance = Mathf.Clamp(fightDistance + 0.1f * (distance - fightDistance), 3f, 25f);
		dirty = true;
	}

	public static void CloseCall()
	{
		Data.closeCalls++;
		Data.fightDistance = Mathf.Clamp(FightDistance + 0.3f, 3f, 25f);
		dirty = true;
	}

	private static GunRecord Gun(string barcode)
	{
		if (!Data.guns.TryGetValue(barcode, out var value))
		{
			value = (Data.guns[barcode] = new GunRecord());
		}
		return value;
	}

	public static string PickGun(List<string> have)
	{
		if (have.Count == 0)
		{
			return null;
		}
		int num = 1;
		foreach (string item in have)
		{
			num += (Data.guns.TryGetValue(item, out var value) ? value.times : 0);
		}
		string text = have[0];
		float num2 = float.MinValue;
		foreach (string item2 in have)
		{
			Data.guns.TryGetValue(item2, out var value2);
			float num3 = ((value2 != null) ? ((float)value2.kills) : 0f);
			float num4 = ((value2 != null) ? (value2.seconds / 60f) : 0f);
			int num5 = value2?.times ?? 0;
			float num6 = (num3 + 1f) / (num4 + 1f);
			float num7 = 1.2f * Mathf.Sqrt(Mathf.Log((float)num + 1f) / ((float)num5 + 1f));
			float num8 = num6 + num7 + UnityEngine.Random.value * 0.3f;
			if (num8 > num2)
			{
				num2 = num8;
				text = item2;
			}
		}
		Gun(text).times++;
		dirty = true;
		return text;
	}

	public static string Summary()
	{
		Store data = Data;
		string text = "none yet";
		float num = -1f;
		foreach (KeyValuePair<string, GunRecord> gun in data.guns)
		{
			if (gun.Value.kills != 0)
			{
				float num2 = (float)gun.Value.kills / Mathf.Max(gun.Value.seconds / 60f, 0.5f);
				if (num2 > num)
				{
					num = num2;
					int num3 = gun.Key.LastIndexOf('.');
					text = ((num3 >= 0) ? gun.Key.Substring(num3 + 1) : gun.Key);
				}
			}
		}
		int num4 = data.player.gaps.Count + data.player.bursts.Count;
		return data.kills + " kills, " + data.closeCalls + " close calls, " + data.minutes.ToString("0", CultureInfo.InvariantCulture) + " min; fights at " + FightDistance.ToString("0", CultureInfo.InvariantCulture) + " m; best gun " + text + "; watched " + num4 + " of your shots";
	}
}
