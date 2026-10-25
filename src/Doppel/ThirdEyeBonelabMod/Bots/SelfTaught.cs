using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class SelfTaught
{
	public class Trial
	{
		public int tries { get; set; }

		public float effect { get; set; }

		public string what { get; set; } = "";

	}

	public class Item
	{
		public string name { get; set; } = "";


		public int sessions { get; set; }

		public Dictionary<string, Trial> actions { get; set; } = new Dictionary<string, Trial>();

	}

	public class Throw
	{
		public float speed { get; set; } = 8f;


		public float loft { get; set; } = 25f;


		public float spread { get; set; } = 1f;


		public float miss { get; set; } = -1f;


		public float firstMiss { get; set; } = -1f;


		public int tries { get; set; }
	}

	public class Level
	{
		public Dictionary<string, int> bad { get; set; } = new Dictionary<string, int>();


		public Dictionary<string, int> climbFails { get; set; } = new Dictionary<string, int>();


		public List<float[]> tops { get; set; } = new List<float[]>();

	}

	public class Store
	{
		public Dictionary<string, Item> items { get; set; } = new Dictionary<string, Item>();


		public Dictionary<string, string> seen { get; set; } = new Dictionary<string, string>();


		public List<string> never { get; set; } = new List<string>();


		public Dictionary<string, Throw> throws { get; set; } = new Dictionary<string, Throw>();


		public Dictionary<string, Level> levels { get; set; } = new Dictionary<string, Level>();

	}

	public static readonly string[] Actions = new string[12]
	{
		"trigger/ahead", "trigger/down", "hold trigger/ahead", "hold trigger/down", "A/ahead", "A/down", "B/ahead", "B/down", "menu/ahead", "click/ahead",
		"shake", "swing"
	};

	private static Store store;

	private static bool dirty;

	private static float savedAt;

	public static string NewsItem = "";

	public static string NewsName = "";

	public static Vector3 NewsAt;

	public static float NewsTime = -100f;

	private static string File => Path.Combine(AssetFiles.SaveFolder(""), "selftaught.json");

	public static Store Data
	{
		get
		{
			if (store != null)
			{
				return store;
			}
			store = new Store();
			try
			{
				if (System.IO.File.Exists(File))
				{
					store = JsonSerializer.Deserialize<Store>(System.IO.File.ReadAllText(File)) ?? new Store();
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("Bot Player: what it taught itself could not be read: " + ex.Message);
			}
			return store;
		}
	}

	private static Level Here
	{
		get
		{
			string level = Lessons.Level;
			if (!Data.levels.TryGetValue(level, out var value))
			{
				value = (Data.levels[level] = new Level());
			}
			return value;
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
			System.IO.File.WriteAllText(File, JsonSerializer.Serialize(store, new JsonSerializerOptions
			{
				WriteIndented = true
			}));
			dirty = false;
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: what it taught itself could not be saved: " + ex.Message);
		}
	}

	public static void Tick()
	{
		if (dirty && Time.time - savedAt > 20f)
		{
			savedAt = Time.time;
			Save();
		}
	}

	public static void Forget()
	{
		store = new Store();
		dirty = true;
		Save();
	}

	public static void Seen(string barcode, string name)
	{
		if (!string.IsNullOrEmpty(barcode) && !Data.seen.ContainsKey(barcode) && (Data.never == null || !Data.never.Contains(barcode)))
		{
			Data.seen[barcode] = name;
			dirty = true;
		}
	}

	public static void Never(string barcode)
	{
		if (!string.IsNullOrEmpty(barcode))
		{
			Store data = Data;
			if (data.never == null)
			{
				List<string> list2 = (data.never = new List<string>());
			}
			if (!Data.never.Contains(barcode))
			{
				Data.never.Add(barcode);
			}
			Data.seen.Remove(barcode);
			dirty = true;
		}
	}

	public static bool IsNever(string barcode)
	{
		if (Data.never != null)
		{
			return Data.never.Contains(barcode);
		}
		return false;
	}

	public static Item Of(string barcode, string name)
	{
		if (!Data.items.TryGetValue(barcode, out var value))
		{
			value = new Item
			{
				name = name
			};
			Data.items[barcode] = value;
			dirty = true;
		}
		return value;
	}

	public static float Known(string barcode)
	{
		if (!Data.items.TryGetValue(barcode, out var value))
		{
			return 0f;
		}
		int num = 0;
		string[] actions = Actions;
		foreach (string key in actions)
		{
			num += (value.actions.TryGetValue(key, out var value2) ? Mathf.Min(value2.tries, 3) : 0);
		}
		return (float)num / (3f * (float)Actions.Length);
	}

	public static float Best(string barcode, out string action, out string what)
	{
		action = (what = "");
		float num = 0f;
		if (!Data.items.TryGetValue(barcode, out var value))
		{
			return 0f;
		}
		foreach (KeyValuePair<string, Trial> action2 in value.actions)
		{
			if (action2.Value.tries > 0 && action2.Value.effect > num)
			{
				num = action2.Value.effect;
				action = action2.Key;
				what = action2.Value.what;
			}
		}
		return num;
	}

	public static bool Dull(string barcode)
	{
		string action;
		string what;
		if (Known(barcode) > 0.6f)
		{
			return Best(barcode, out action, out what) < 0.15f;
		}
		return false;
	}

	public static string Choose(string barcode, string name, string not)
	{
		Item item = Of(barcode, name);
		List<string> list = new List<string>();
		int num = 1;
		string[] actions = Actions;
		foreach (string text in actions)
		{
			if (!item.actions.TryGetValue(text, out var value) || value.tries == 0)
			{
				list.Add(text);
			}
			else
			{
				num += value.tries;
			}
		}
		list.Remove(not);
		if (list.Count > 0)
		{
			return list[UnityEngine.Random.Range(0, list.Count)];
		}
		string result = Actions[0];
		float num2 = float.MinValue;
		actions = Actions;
		foreach (string text2 in actions)
		{
			if (!(text2 == not))
			{
				Trial trial = item.actions[text2];
				float num3 = trial.effect + Tuning.F("curious.explore", 0.35f) * Mathf.Sqrt(Mathf.Log(num) / (float)trial.tries) + UnityEngine.Random.value * 0.1f;
				if (num3 > num2)
				{
					num2 = num3;
					result = text2;
				}
			}
		}
		return result;
	}

	public static bool Tried(string barcode, string name, string action, float effect, string what, Vector3 at)
	{
		Item item = Of(barcode, name);
		if (!item.actions.TryGetValue(action, out var value))
		{
			value = (item.actions[action] = new Trial());
		}
		string action2;
		string what2;
		bool flag = Best(barcode, out action2, out what2) >= 0.35f;
		bool num = effect >= 0.4f && (value.tries == 0 || value.effect < 0.2f);
		value.effect = ((value.tries == 0) ? effect : Mathf.Lerp(value.effect, effect, 1f / (float)Mathf.Min(value.tries + 1, 6)));
		value.tries++;
		if (effect >= 0.3f && what.Length > 0)
		{
			value.what = what;
		}
		dirty = true;
		if (num)
		{
			NewsItem = barcode;
			NewsName = name;
			NewsAt = at;
			NewsTime = Time.time;
		}
		if (num)
		{
			return !flag;
		}
		return false;
	}

	private static string Band(float far)
	{
		if (!(far < 6f))
		{
			if (!(far < 9f))
			{
				if (!(far < 13f))
				{
					return "15";
				}
				return "11";
			}
			return "8";
		}
		return "5";
	}

	public static void ThrowTry(float far, out float speed, out float loft)
	{
		string key = Band(far);
		if (!Data.throws.TryGetValue(key, out var value))
		{
			value = (Data.throws[key] = new Throw());
			dirty = true;
		}
		speed = Mathf.Clamp(value.speed + Gauss() * value.spread * 1.6f, 3f, 20f);
		loft = Mathf.Clamp(value.loft + Gauss() * value.spread * 7f, 3f, 65f);
	}

	public static string ThrowResult(float far, float speed, float loft, float miss)
	{
		Throw @throw = Data.throws[Band(far)];
		float num = ((@throw.miss < 0f) ? (miss * 1.2f) : @throw.miss);
		if (miss < num)
		{
			float t = Mathf.Clamp01(0.3f + 0.5f * (num - miss) / Mathf.Max(num, 0.5f));
			@throw.speed = Mathf.Lerp(@throw.speed, speed, t);
			@throw.loft = Mathf.Lerp(@throw.loft, loft, t);
			@throw.spread = Mathf.Max(0.12f, @throw.spread * 0.88f);
		}
		else
		{
			@throw.spread = Mathf.Min(1.2f, @throw.spread * 1.04f);
		}
		if (@throw.firstMiss < 0f)
		{
			@throw.firstMiss = miss;
		}
		@throw.miss = ((@throw.miss < 0f) ? miss : Mathf.Lerp(@throw.miss, miss, 0.2f));
		@throw.tries++;
		dirty = true;
		return "missed by " + miss.ToString("0.0") + " m (usually " + num.ToString("0.0") + "), throw " + @throw.tries + " at that range";
	}

	private static float Gauss()
	{
		float f = Mathf.Max(1E-05f, UnityEngine.Random.value);
		return Mathf.Sqrt(-2f * Mathf.Log(f)) * Mathf.Cos((float)Math.PI * 2f * UnityEngine.Random.value);
	}

	private static string Cell(Vector3 p)
	{
		return Mathf.RoundToInt(p.x / 2f) + "_" + Mathf.RoundToInt(p.y / 2f) + "_" + Mathf.RoundToInt(p.z / 2f);
	}

	public static void Bad(Vector3 at)
	{
		Level here = Here;
		string key = Cell(at);
		here.bad[key] = Mathf.Min(6, (here.bad.TryGetValue(key, out var value) ? value : 0) + 1);
		dirty = true;
	}

	public static int BadAt(Vector3 at)
	{
		if (!Here.bad.TryGetValue(Cell(at), out var value))
		{
			return 0;
		}
		return value;
	}

	public static void ClimbFailed(Vector3 edge)
	{
		Level here = Here;
		string key = Cell(edge);
		here.climbFails[key] = (here.climbFails.TryGetValue(key, out var value) ? value : 0) + 1;
		dirty = true;
	}

	public static int ClimbFails(Vector3 edge)
	{
		if (!Here.climbFails.TryGetValue(Cell(edge), out var value))
		{
			return 0;
		}
		return value;
	}

	public static void Top(Vector3 at)
	{
		Level here = Here;
		here.climbFails.Remove(Cell(at));
		foreach (float[] top in here.tops)
		{
			if ((new Vector3(top[0], top[1], top[2]) - at).magnitude < 3f)
			{
				return;
			}
		}
		here.tops.Add(new float[3] { at.x, at.y, at.z });
		if (here.tops.Count > 40)
		{
			here.tops.RemoveAt(0);
		}
		dirty = true;
	}

	public static bool KnownTop(Vector3 from, float within, out Vector3 top)
	{
		top = default(Vector3);
		List<Vector3> list = new List<Vector3>();
		foreach (float[] top2 in Here.tops)
		{
			Vector3 vector = new Vector3(top2[0], top2[1], top2[2]);
			float magnitude = (vector - from).magnitude;
			if (magnitude > 6f && magnitude < within)
			{
				list.Add(vector);
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		top = list[UnityEngine.Random.Range(0, list.Count)];
		return true;
	}

	public static string Summary()
	{
		int num = 0;
		int num2 = 0;
		foreach (Item value in Data.items.Values)
		{
			num2++;
			foreach (Trial value2 in value.actions.Values)
			{
				num += ((value2.tries > 0 && value2.effect >= 0.35f) ? 1 : 0);
			}
		}
		float num3 = 0f;
		float num4 = 0f;
		int num5 = 0;
		foreach (Throw value3 in Data.throws.Values)
		{
			if (value3.tries >= 2)
			{
				num3 += value3.miss;
				num4 += value3.firstMiss;
				num5++;
			}
		}
		Level here = Here;
		return "by itself: tried " + num2 + " things and found " + num + " that do something" + ((num5 > 0) ? ("; throws miss by " + (num3 / (float)num5).ToString("0.0") + " m (" + (num4 / (float)num5).ToString("0.0") + " at first)") : "") + "; here it knows " + here.bad.Count + " bad spots, " + here.tops.Count + " high places";
	}
}
