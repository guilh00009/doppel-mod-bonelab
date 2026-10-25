using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class GripBook
{
	public class Entry
	{
		public string item { get; set; }

		public string name { get; set; }

		public string grip { get; set; }

		public string gripName { get; set; }

		public string side { get; set; }

		public bool main { get; set; }

		public float[] pose { get; set; }

		public int n { get; set; }

		public float held { get; set; }

		public float[] fingers { get; set; }
	}

	public class Store
	{
		public List<Entry> grips { get; set; } = new List<Entry>();

	}

	private class Watch
	{
		public IntPtr grip;

		public float since;

		public float sampleAt;

		public float heldFrom;

		public readonly List<Bot.Pose> samples = new List<Bot.Pose>();

		public readonly float[] fingers = new float[5];

		public bool done;

		public Grip g;

		public Transform thing;

		public string side;
	}

	private static Store store;

	private static bool dirty;

	private static float savedAt;

	private static readonly Watch[] watching = new Watch[2]
	{
		new Watch(),
		new Watch()
	};

	private const int Samples = 12;

	private static StreamWriter items;

	private static float itemsAt;

	private static string File => Path.Combine(AssetFiles.SaveFolder(""), "grips.json");

	private static Store Data
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
				MelonLogger.Warning("Bot Player: the grip book could not be read: " + ex.Message);
			}
			return store;
		}
	}

	public static int Count => Data.grips.Count;

	public static void Save()
	{
		if (!dirty || store == null)
		{
			return;
		}
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(File));
			System.IO.File.WriteAllText(File, JsonSerializer.Serialize(store));
			dirty = false;
			savedAt = Time.time;
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: the grip book could not be saved: " + ex.Message);
		}
	}

	public static Transform ThingOf(Grip g)
	{
		if ((Object)(object)g == (Object)null)
		{
			return null;
		}
		MarrowEntity componentInParent = ((Component)g).GetComponentInParent<MarrowEntity>();
		if ((Object)(object)componentInParent != (Object)null)
		{
			return ((Component)componentInParent).transform;
		}
		Rigidbody componentInParent2 = ((Component)g).GetComponentInParent<Rigidbody>();
		if (!((Object)(object)componentInParent2 != (Object)null))
		{
			return null;
		}
		return ((Component)componentInParent2).transform;
	}

	public static string KeyOf(Transform thing)
	{
		if ((Object)(object)thing == (Object)null)
		{
			return "";
		}
		try
		{
			Poolee componentInParent = ((Component)thing).GetComponentInParent<Poolee>();
			if ((Object)(object)componentInParent != (Object)null && (Object)(object)componentInParent.SpawnableCrate != (Object)null)
			{
				return ((Scannable)componentInParent.SpawnableCrate).Barcode.ID;
			}
		}
		catch (Exception)
		{
		}
		return Plain(((Object)thing).name);
	}

	public static string NameOf(Transform thing)
	{
		if ((Object)(object)thing == (Object)null)
		{
			return "?";
		}
		try
		{
			Poolee componentInParent = ((Component)thing).GetComponentInParent<Poolee>();
			if ((Object)(object)componentInParent != (Object)null && (Object)(object)componentInParent.SpawnableCrate != (Object)null && !string.IsNullOrEmpty(((Scannable)componentInParent.SpawnableCrate).Title))
			{
				return ((Scannable)componentInParent.SpawnableCrate).Title;
			}
		}
		catch (Exception)
		{
		}
		return Plain(((Object)thing).name);
	}

	private static string Plain(string name)
	{
		int num = name.IndexOf(" [", StringComparison.Ordinal);
		if (num > 0)
		{
			name = name.Substring(0, num);
		}
		return name.Replace("(Clone)", "").Trim();
	}

	public static string PathOf(Grip g, Transform thing)
	{
		StringBuilder stringBuilder = new StringBuilder();
		Transform val = ((Component)g).transform;
		while ((Object)(object)val != (Object)null && (Object)(object)val != (Object)(object)thing)
		{
			stringBuilder.Insert(0, (stringBuilder.Length > 0) ? (Plain(((Object)val).name) + "/") : Plain(((Object)val).name));
			val = val.parent;
		}
		return stringBuilder.ToString();
	}

	private static Transform BodyOf(Grip g)
	{
		Rigidbody componentInParent = ((Component)g).GetComponentInParent<Rigidbody>();
		if (!((Object)(object)componentInParent != (Object)null))
		{
			return ((Component)g).transform;
		}
		return ((Component)componentInParent).transform;
	}

	private static bool MainGrip(Grip g, Transform thing)
	{
		Gun val = (((Object)(object)thing != (Object)null) ? ((Component)thing).GetComponentInChildren<Gun>() : null);
		if ((Object)(object)val != (Object)null)
		{
			return (Object)(object)val.triggerGrip == (Object)(object)g;
		}
		return false;
	}

	public static void Update()
	{
		if (store != null && dirty && Time.time - savedAt > 10f)
		{
			Save();
		}
		if (!Recorder.Recording)
		{
			for (int i = 0; i < 2; i++)
			{
				Let(watching[i]);
			}
			CloseItems();
			return;
		}
		try
		{
			Look(watching[0], Player.LeftHand, "L");
			Look(watching[1], Player.RightHand, "R");
			WriteItems();
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: watching your hands: " + ex.Message);
		}
	}

	private static Grip Held(Hand hand)
	{
		if ((Object)(object)hand == (Object)null || (Object)(object)hand.m_CurrentAttachedGO == (Object)null)
		{
			return null;
		}
		Grip val = null;
		try
		{
			val = (((Object)(object)hand.AttachedReceiver != (Object)null) ? ((Il2CppObjectBase)(object)hand.AttachedReceiver).TryCast<Grip>() : null);
		}
		catch (Exception)
		{
		}
		if ((Object)(object)val == (Object)null)
		{
			val = hand.m_CurrentAttachedGO.GetComponent<Grip>();
		}
		if ((Object)(object)val == (Object)null || (Object)(object)((Il2CppObjectBase)(object)val).TryCast<WorldGrip>() != (Object)null || (Object)(object)((Component)val).GetComponentInParent<RigManager>() != (Object)null || (Object)(object)((Component)val).GetComponentInParent<Rigidbody>() == (Object)null)
		{
			return null;
		}
		return val;
	}

	private static void Look(Watch w, Hand hand, string side)
	{
		Grip val = Held(hand);
		IntPtr intPtr = (((Object)(object)val != (Object)null) ? ((Il2CppObjectBase)(object)val).Pointer : IntPtr.Zero);
		if (intPtr != w.grip)
		{
			Let(w);
			w.grip = intPtr;
			w.g = val;
			w.thing = ThingOf(val);
			w.side = side;
			w.since = Time.time;
			w.heldFrom = Time.time;
			w.samples.Clear();
			Array.Clear(w.fingers, 0, 5);
			w.done = false;
		}
		if (!((Object)(object)val == (Object)null) && !w.done && !(Time.time - w.since < Tuning.F("grips.settle", 0.4f)) && !(Time.time < w.sampleAt))
		{
			w.sampleAt = Time.time + 0.08f;
			Bot.Pose item = Bot.Pose.Of(BodyOf(val)).Inverse() * Bot.Pose.Of(((Component)hand).transform);
			w.samples.Add(item);
			BaseController val2 = ((side == "L") ? Player.LeftController : Player.RightController);
			if ((Object)(object)val2 != (Object)null)
			{
				w.fingers[0] += val2._processedIndex;
				w.fingers[1] += val2._processedMiddle;
				w.fingers[2] += val2._processedRing;
				w.fingers[3] += val2._processedPinky;
				w.fingers[4] += val2._processedThumb;
			}
			if (w.samples.Count >= 12)
			{
				Commit(w, side);
				w.done = true;
			}
		}
	}

	private static void Let(Watch w)
	{
		if (w.grip != IntPtr.Zero && w.done && (Object)(object)w.g != (Object)null)
		{
			Entry entry = Find(KeyOf(w.thing), PathOf(w.g, w.thing), w.side);
			if (entry != null)
			{
				entry.held += Time.time - w.heldFrom;
				dirty = true;
			}
		}
		w.grip = IntPtr.Zero;
		w.g = null;
		w.thing = null;
		w.samples.Clear();
		w.done = false;
	}

	private static void Commit(Watch w, string side)
	{
		if ((Object)(object)w.g == (Object)null || (Object)(object)w.thing == (Object)null)
		{
			return;
		}
		Vector3 val = Vector3.zero;
		Quaternion val2 = w.samples[0].rotation;
		for (int i = 0; i < w.samples.Count; i++)
		{
			val += w.samples[i].position;
			if (i > 0)
			{
				val2 = Quaternion.Slerp(val2, w.samples[i].rotation, 1f / (float)(i + 1));
			}
		}
		val /= (float)w.samples.Count;
		Bot.Pose pose = new Bot.Pose(val, val2);
		float[] array = new float[5];
		for (int j = 0; j < 5; j++)
		{
			array[j] = Mathf.Clamp01(w.fingers[j] / (float)w.samples.Count);
		}
		string text = KeyOf(w.thing);
		string text2 = PathOf(w.g, w.thing);
		Entry entry = Find(text, text2, side);
		bool flag = entry == null;
		if (flag)
		{
			entry = new Entry
			{
				item = text,
				name = NameOf(w.thing),
				grip = text2,
				gripName = Plain(((Object)w.g).name),
				side = side,
				main = MainGrip(w.g, w.thing),
				pose = GunMoves.P(pose),
				n = 0,
				fingers = array
			};
			Data.grips.Add(entry);
		}
		else
		{
			Bot.Pose a = GunMoves.U(entry.pose, pose);
			float num = Mathf.Max(1f / (float)(entry.n + 1), 0.25f);
			entry.pose = GunMoves.P(Bot.Pose.Lerp(a, pose, num));
			if (entry.fingers == null || entry.fingers.Length < 5)
			{
				entry.fingers = array;
			}
			else
			{
				for (int k = 0; k < 5; k++)
				{
					entry.fingers[k] = Mathf.Lerp(entry.fingers[k], array[k], num);
				}
			}
		}
		entry.n = Mathf.Min(entry.n + 1, 50);
		dirty = true;
		string text3 = NameOf(w.thing) + ": " + ((side == "L") ? "left" : "right") + " hand on " + Plain(((Object)w.g).name) + (flag ? "" : (" (" + entry.n + ")"));
		Lab.Event("grips: learned " + text3);
		MoveSlots.Note("<size=60%>learned " + text3 + "</size>", 2.5f);
	}

	private static Entry Find(string key, string path, string side)
	{
		foreach (Entry grip in Data.grips)
		{
			if (grip.item == key && grip.grip == path && (side == null || grip.side == side))
			{
				return grip;
			}
		}
		return null;
	}

	public static bool HandOn(Grip g, bool leftHand, out Bot.Pose hand, out bool mirrored)
	{
		hand = default(Bot.Pose);
		mirrored = false;
		if ((Object)(object)g == (Object)null || Tuning.F("grips.use", 1f) < 0.5f)
		{
			return false;
		}
		Transform val = ThingOf(g);
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		string key = KeyOf(val);
		string path = PathOf(g, val);
		Entry entry = Find(key, path, leftHand ? "L" : "R");
		if (entry == null)
		{
			entry = Find(key, path, leftHand ? "R" : "L");
			mirrored = entry != null;
		}
		if (entry == null)
		{
			return false;
		}
		Bot.Pose pose = GunMoves.U(entry.pose, Bot.Pose.identity);
		if (mirrored)
		{
			Transform t = BodyOf(g);
			Transform t2 = val;
			Bot.Pose pose2 = Bot.Pose.Of(t2).Inverse() * Bot.Pose.Of(t) * pose;
			pose2 = new Bot.Pose(new Vector3(0f - pose2.position.x, pose2.position.y, pose2.position.z), new Quaternion(pose2.rotation.x, 0f - pose2.rotation.y, 0f - pose2.rotation.z, pose2.rotation.w));
			hand = Bot.Pose.Of(t2) * pose2;
			return true;
		}
		hand = Bot.Pose.Of(BodyOf(g)) * pose;
		return true;
	}

	public static float[] FingersOn(Grip g, bool leftHand)
	{
		if ((Object)(object)g == (Object)null)
		{
			return null;
		}
		Transform val = ThingOf(g);
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		string key = KeyOf(val);
		string path = PathOf(g, val);
		Entry entry = Find(key, path, leftHand ? "L" : "R") ?? Find(key, path, leftHand ? "R" : "L");
		if (entry == null || entry.fingers == null || entry.fingers.Length < 5)
		{
			return null;
		}
		return entry.fingers;
	}

	public static Grip Steadying(Transform thing, Grip main)
	{
		return Favourite(thing, main, mainWanted: false);
	}

	public static Grip Holding(Transform thing)
	{
		return Favourite(thing, null, mainWanted: true);
	}

	private static Grip Favourite(Transform thing, Grip not, bool mainWanted)
	{
		if ((Object)(object)thing == (Object)null || Tuning.F("grips.use", 1f) < 0.5f)
		{
			return null;
		}
		string text = KeyOf(thing);
		Grip result = null;
		float num = -1f;
		Grip[] array = ((Component)thing).GetComponentsInChildren<Grip>(false);
		foreach (Entry grip in Data.grips)
		{
			if (grip.item != text)
			{
				continue;
			}
			bool main = grip.main;
			bool num2;
			if (!mainWanted)
			{
				num2 = main;
			}
			else
			{
				if (main)
				{
					goto IL_008d;
				}
				num2 = HasMain(text);
			}
			if (num2)
			{
				continue;
			}
			goto IL_008d;
			IL_008d:
			float num3 = grip.held + (float)grip.n;
			if (num3 <= num)
			{
				continue;
			}
			Grip[] array2 = array;
			foreach (Grip val in array2)
			{
				if (!((Object)(object)val == (Object)null) && !((Object)(object)val == (Object)(object)not) && !val.HasAttachedHands() && ((Component)val).gameObject.activeInHierarchy && PathOf(val, thing) == grip.grip)
				{
					result = val;
					num = num3;
					break;
				}
			}
		}
		return result;
	}

	private static bool HasMain(string key)
	{
		foreach (Entry grip in Data.grips)
		{
			if (grip.item == key && grip.main)
			{
				return true;
			}
		}
		return false;
	}

	public static string Summary()
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (Entry grip in Data.grips)
		{
			hashSet.Add(grip.item);
		}
		return Data.grips.Count + " grips on " + hashSet.Count + " things";
	}

	private static void WriteItems()
	{
		if (Time.time < itemsAt)
		{
			return;
		}
		itemsAt = Time.time + 1f / 30f;
		OpenControllerRig controllerRig = Player.ControllerRig;
		if (!((Object)(object)controllerRig == (Object)null) && !((Object)(object)controllerRig.vrRoot == (Object)null) && !((Object)(object)controllerRig.headset == (Object)null))
		{
			if (items == null)
			{
				string text = AssetFiles.SaveFolder("learn");
				Directory.CreateDirectory(text);
				items = new StreamWriter(Path.Combine(text, "items_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv"), append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				items.WriteLine("t,head,left,right,lItem,lGrip,lThing,lHandInThing,lTrigger,rItem,rGrip,rThing,rHandInThing,rTrigger,shot");
			}
			Transform vrRoot = controllerRig.vrRoot;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(Time.time.ToString("0.000", CultureInfo.InvariantCulture));
			stringBuilder.Append(',').Append(P(vrRoot, controllerRig.headset));
			stringBuilder.Append(',').Append(P(vrRoot, ((Component)((ControllerRig)controllerRig).leftController).transform));
			stringBuilder.Append(',').Append(P(vrRoot, ((Component)((ControllerRig)controllerRig).rightController).transform));
			Hand(stringBuilder, vrRoot, Player.LeftHand, ((ControllerRig)controllerRig).leftController);
			Hand(stringBuilder, vrRoot, Player.RightHand, ((ControllerRig)controllerRig).rightController);
			stringBuilder.Append(',').Append((Time.time - PlayerWatch.FiredAt < 1f / 30f) ? "1" : "0");
			items.WriteLine(stringBuilder.ToString());
		}
	}

	private static void Hand(StringBuilder line, Transform root, Hand hand, BaseController controller)
	{
		Grip val = Held(hand);
		Transform val2 = ThingOf(val);
		if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null)
		{
			line.Append(",,,,,").Append(((Object)(object)controller != (Object)null) ? controller._primaryAxis.ToString("0.00", CultureInfo.InvariantCulture) : "0");
			return;
		}
		line.Append(',').Append(KeyOf(val2).Replace(',', ' '));
		line.Append(',').Append(PathOf(val, val2).Replace(',', ' '));
		line.Append(',').Append(P(root, val2));
		Bot.Pose p = Bot.Pose.Of(val2).Inverse() * Bot.Pose.Of(((Component)hand).transform);
		line.Append(',').Append(Join(GunMoves.P(p)));
		line.Append(',').Append(((Object)(object)controller != (Object)null) ? controller._primaryAxis.ToString("0.00", CultureInfo.InvariantCulture) : "0");
	}

	private static string P(Transform root, Transform t)
	{
		return Join(GunMoves.P(new Bot.Pose(root.InverseTransformPoint(t.position), Quaternion.Inverse(root.rotation) * t.rotation)));
	}

	private static string Join(float[] v)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < v.Length; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append(v[i].ToString("0.####", CultureInfo.InvariantCulture));
		}
		return stringBuilder.ToString();
	}

	private static void CloseItems()
	{
		if (items != null)
		{
			try
			{
				items.Flush();
				items.Dispose();
			}
			catch (Exception)
			{
			}
			items = null;
		}
	}

	public static void Shutdown()
	{
		CloseItems();
		Save();
	}
}
