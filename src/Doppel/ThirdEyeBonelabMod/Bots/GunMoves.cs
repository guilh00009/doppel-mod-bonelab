using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BoneLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class GunMoves
{
	public class Hold
	{
		public int n { get; set; }

		public float[] right { get; set; }

		public float[] left { get; set; }

		public float[] muzzle { get; set; }
	}

	public class ReloadFrame
	{
		public float t { get; set; }

		public float[] head { get; set; }

		public float[] right { get; set; }

		public float[] left { get; set; }
	}

	public class Reload
	{
		public float eject { get; set; } = -1f;


		public float grab { get; set; } = -1f;


		public float insert { get; set; } = -1f;


		public float rack { get; set; } = -1f;


		public float end { get; set; }

		public List<ReloadFrame> frames { get; set; } = new List<ReloadFrame>();

	}

	public class GunRecord
	{
		public Hold aim { get; set; }

		public Hold low { get; set; }

		public List<Reload> reloads { get; set; } = new List<Reload>();

	}

	public class Punch
	{
		public bool right { get; set; }

		public List<ReloadFrame> frames { get; set; } = new List<ReloadFrame>();

	}

	public class Store
	{
		public Dictionary<string, GunRecord> guns { get; set; } = new Dictionary<string, GunRecord>();


		public List<Punch> punches { get; set; } = new List<Punch>();


		public Dictionary<string, string> kinds { get; set; } = new Dictionary<string, string>();

	}

	private struct Moment
	{
		public float t;

		public Bot.Pose head;

		public Bot.Pose right;

		public Bot.Pose left;
	}

	private static Store store;

	private static bool dirty;

	private static float savedAt;

	public static bool Teaching;

	private static readonly List<(float at, Action undo)> learned = new List<(float, Action)>();

	private static float scannedAt = -100f;

	public static float PlayerPunchedAt = -10f;

	private static readonly List<Moment> moments = new List<Moment>();

	private static float punchPeak = -1f;

	private static bool punchRight;

	private static float momentAt;

	private static float sampleAt;

	private static Gun watched;

	private static bool hadMagazine;

	private static Reload taking;

	private static float takingSince;

	private static string takingKey;

	private static bool takingTwoHanded;

	private static float takeFrameAt;

	private static bool slotFresh;

	private static readonly HashSet<string> freshened = new HashSet<string>();

	private static string File => Path.Combine(AssetFiles.SaveFolder(""), "gunmoves.json");

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
				MelonLogger.Warning("Bot Player: the gun moves could not be read: " + ex.Message);
			}
			return store;
		}
	}

	private static void Learned(Action undo)
	{
		learned.Add((Time.time, undo));
		if (learned.Count > 200)
		{
			learned.RemoveAt(0);
		}
	}

	public static int ForgetSince(float since)
	{
		int num = 0;
		int num2 = learned.Count - 1;
		while (num2 >= 0 && learned[num2].at >= since)
		{
			try
			{
				learned[num2].undo();
			}
			catch (Exception)
			{
			}
			learned.RemoveAt(num2);
			num++;
			num2--;
		}
		if (num > 0)
		{
			dirty = true;
			Save();
		}
		return num;
	}

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
			MelonLogger.Warning("Bot Player: the gun moves could not be saved: " + ex.Message);
		}
	}

	public static string KeyOf(Gun gun, bool twoHanded)
	{
		try
		{
			Poolee componentInParent = ((Component)gun).GetComponentInParent<Poolee>();
			if ((Object)(object)componentInParent != (Object)null && (Object)(object)componentInParent.SpawnableCrate != (Object)null)
			{
				string iD = ((Scannable)componentInParent.SpawnableCrate).Barcode.ID;
				NoteKind(iD, gun);
				return iD;
			}
		}
		catch (Exception)
		{
		}
		if (!twoHanded)
		{
			return "pistol";
		}
		return "long";
	}

	public static string KindOf(Gun gun)
	{
		float num = 0.2f;
		try
		{
			if ((Object)(object)gun.triggerGrip != (Object)null && (Object)(object)gun.firePointTransform != (Object)null)
			{
				Vector3 val = gun.firePointTransform.position - ((Component)gun.triggerGrip).transform.position;
				num = ((Vector3)(ref val)).magnitude;
			}
		}
		catch (Exception)
		{
		}
		if (!(num < Tuning.F("gun.kind.smg", 0.3f)))
		{
			if (!(num < Tuning.F("gun.kind.long", 0.46f)))
			{
				return "long";
			}
			return "smg";
		}
		return "pistol";
	}

	private static void NoteKind(string barcode, Gun gun)
	{
		if (Data.kinds == null)
		{
			Data.kinds = new Dictionary<string, string>();
		}
		if (!Data.kinds.ContainsKey(barcode))
		{
			string text = KindOf(gun);
			Data.kinds[barcode] = text;
			dirty = true;
			float num;
			if (!((Object)(object)gun.triggerGrip != (Object)null) || !((Object)(object)gun.firePointTransform != (Object)null))
			{
				num = 0f;
			}
			else
			{
				Vector3 val = gun.firePointTransform.position - ((Component)gun.triggerGrip).transform.position;
				num = ((Vector3)(ref val)).magnitude;
			}
			float num2 = num;
			Lab.Event("gun kind: " + ((Object)gun).name + " is a " + text + " (" + num2.ToString("0.00") + " m from handle to muzzle)");
		}
	}

	private static void ScanGuns()
	{
		if (Time.time - scannedAt < 15f)
		{
			return;
		}
		scannedAt = Time.time;
		try
		{
			foreach (Gun item in Object.FindObjectsOfType<Gun>())
			{
				if ((Object)(object)item != (Object)null && (Object)(object)item.triggerGrip != (Object)null)
				{
					KeyOf(item, twoHanded: false);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private static GunRecord OfItsKind(string key, Func<GunRecord, int> worth)
	{
		ScanGuns();
		if (Data.kinds == null || !Data.kinds.TryGetValue(key, out var value))
		{
			return null;
		}
		GunRecord result = null;
		int num = 0;
		foreach (KeyValuePair<string, GunRecord> gun in Data.guns)
		{
			if (!(gun.Key == key) && Data.kinds.TryGetValue(gun.Key, out var value2) && !(value2 != value))
			{
				int num2 = worth(gun.Value);
				if (num2 > num)
				{
					num = num2;
					result = gun.Value;
				}
			}
		}
		return result;
	}

	private static GunRecord Record(string key)
	{
		if (!Data.guns.TryGetValue(key, out var value))
		{
			value = (Data.guns[key] = new GunRecord());
		}
		return value;
	}

	public static Hold Own(string key, bool aimed, int least = 15)
	{
		if (!Data.guns.TryGetValue(key, out var value))
		{
			return null;
		}
		Hold hold = (aimed ? value.aim : value.low);
		if (hold == null || hold.n < least)
		{
			return null;
		}
		return hold;
	}

	public static Hold Held(string key, bool twoHanded, bool aimed, int least = 15)
	{
		Hold hold = null;
		if (Data.guns.TryGetValue(key, out var value))
		{
			hold = (aimed ? value.aim : value.low);
		}
		if (hold == null || hold.n < least)
		{
			GunRecord gunRecord = OfItsKind(key, (GunRecord g) => ((aimed ? g.aim : g.low) != null) ? (aimed ? g.aim : g.low).n : 0);
			Hold hold2 = ((gunRecord == null) ? null : (aimed ? gunRecord.aim : gunRecord.low));
			if (hold2 != null && hold2.n >= least)
			{
				hold = hold2;
			}
		}
		if (hold != null && (hold.n < least || (twoHanded && hold.left == null)))
		{
			return null;
		}
		return hold;
	}

	public static Reload ReloadFor(string key, bool twoHanded)
	{
		List<Reload> list = null;
		if (Data.guns.TryGetValue(key, out var value) && value.reloads.Count > 0)
		{
			list = value.reloads;
		}
		else
		{
			GunRecord gunRecord = OfItsKind(key, (GunRecord g) => g.reloads.Count);
			if (gunRecord != null && gunRecord.reloads.Count > 0)
			{
				list = gunRecord.reloads;
			}
		}
		return list?[Random.Range(0, list.Count)];
	}

	public static Punch PunchFor(bool right)
	{
		List<Punch> list = new List<Punch>();
		foreach (Punch item in Data.punches ?? new List<Punch>())
		{
			if (item.right == right && item.frames.Count > 3)
			{
				list.Add(item);
			}
		}
		if (list.Count <= 0)
		{
			return null;
		}
		return list[Random.Range(0, list.Count)];
	}

	private static void WatchFists(OpenControllerRig controls)
	{
		float time = Time.time;
		if (time - momentAt < 1f / 60f)
		{
			return;
		}
		momentAt = time;
		Transform vrRoot = controls.vrRoot;
		Moment moment = default(Moment);
		moment.t = time;
		moment.head = Local(vrRoot, controls.headset);
		moment.right = Local(vrRoot, ((Component)((ControllerRig)controls).rightController).transform);
		moment.left = Local(vrRoot, ((Component)((ControllerRig)controls).leftController).transform);
		Moment item = moment;
		moments.Add(item);
		while (moments.Count > 0 && time - moments[0].t > 1f)
		{
			moments.RemoveAt(0);
		}
		if (moments.Count < 3)
		{
			return;
		}
		Moment moment2 = moments[moments.Count - 2];
		float num = Mathf.Max(item.t - moment2.t, 0.001f);
		Vector3 val = item.head.rotation * Vector3.forward;
		if (punchPeak < 0f && time - PlayerPunchedAt > 0.5f)
		{
			bool[] array = new bool[2] { true, false };
			foreach (bool flag in array)
			{
				Hand val2 = (flag ? Player.RightHand : Player.LeftHand);
				if (!((Object)(object)val2 == (Object)null) && !((Object)(object)val2.m_CurrentAttachedGO != (Object)null))
				{
					Vector3 val3 = ((flag ? item.right.position : item.left.position) - (flag ? moment2.right.position : moment2.left.position)) / num;
					if (((Vector3)(ref val3)).magnitude > Tuning.F("teach.punch.speed", 3f) && Vector3.Dot(val3, val) > Tuning.F("teach.punch.forward", 1.8f))
					{
						punchPeak = time;
						punchRight = flag;
						break;
					}
				}
			}
		}
		if (!(punchPeak > 0f) || !(time - punchPeak > 0.3f))
		{
			return;
		}
		Punch punch = new Punch
		{
			right = punchRight
		};
		float num2 = punchPeak - 0.3f;
		foreach (Moment moment3 in moments)
		{
			if (!(moment3.t < num2))
			{
				Bot.Pose head = moment3.head;
				Bot.Pose pose = head.Inverse();
				punch.frames.Add(new ReloadFrame
				{
					t = moment3.t - num2,
					right = P(pose * moment3.right),
					left = P(pose * moment3.left)
				});
			}
		}
		if (Data.punches != null && Data.punches.Count > 0)
		{
			Data.punches.Clear();
			Lab.Event("teach: the punches learned by watching are forgotten (its punches are the Punch slots now)");
		}
		_ = punch.frames.Count;
		_ = 5;
		PlayerPunchedAt = punchPeak;
		punchPeak = -1f;
	}

	public static int AimMoments()
	{
		int num = 0;
		foreach (GunRecord value in Data.guns.Values)
		{
			num += ((value.aim != null) ? value.aim.n : 0);
		}
		return num;
	}

	public static void ForgetGun(string barcode, bool reloads, bool holds)
	{
		if (Data.guns.TryGetValue(barcode, out var value))
		{
			if (reloads)
			{
				value.reloads.Clear();
			}
			if (holds)
			{
				GunRecord gunRecord = value;
				Hold aim = (value.low = null);
				gunRecord.aim = aim;
			}
			dirty = true;
			Save();
		}
	}

	public static void Reset()
	{
		store = new Store();
		learned.Clear();
		dirty = false;
		Teaching = false;
	}

	public static void Lessons(out int guns, out int reloads, out int punches)
	{
		guns = (reloads = 0);
		foreach (KeyValuePair<string, GunRecord> gun in Data.guns)
		{
			if (!(gun.Key == "long") && !(gun.Key == "pistol"))
			{
				if (gun.Value.aim != null && gun.Value.aim.n >= 20)
				{
					guns++;
				}
				reloads += gun.Value.reloads.Count;
			}
		}
		punches = ((Data.punches != null) ? Data.punches.Count : 0);
	}

	public static string Summary()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (KeyValuePair<string, GunRecord> gun in Data.guns)
		{
			if (!(gun.Key == "long") && !(gun.Key == "pistol"))
			{
				num += ((gun.Value.aim != null) ? gun.Value.aim.n : 0);
				num2 += ((gun.Value.low != null) ? gun.Value.low.n : 0);
				num3 += gun.Value.reloads.Count;
			}
		}
		return "watched " + num + " moments aiming, " + num2 + " holding low, " + num3 + " reloads, " + ((Data.punches != null) ? Data.punches.Count : 0) + " punches, " + PlayerWatch.Imitated + " choices of yours";
	}

	public static void Update()
	{
		if (store != null && dirty && Time.time - savedAt > 10f)
		{
			Save();
		}
		if (!Teaching)
		{
			taking = null;
			return;
		}
		OpenControllerRig controllerRig = Player.ControllerRig;
		if ((Object)(object)controllerRig != (Object)null && (Object)(object)controllerRig.vrRoot != (Object)null && (Object)(object)controllerRig.headset != (Object)null && (Object)(object)((ControllerRig)controllerRig).rightController != (Object)null && (Object)(object)((ControllerRig)controllerRig).leftController != (Object)null)
		{
			WatchFists(controllerRig);
		}
		Gun componentInHand = Player.GetComponentInHand<Gun>(Player.RightHand);
		Gun componentInHand2 = Player.GetComponentInHand<Gun>(Player.LeftHand);
		Gun gun = componentInHand ?? componentInHand2;
		if ((Object)(object)gun != (Object)null && (((Object)gun).name.ToUpperInvariant().Contains("SPAWN") || ((Object)gun).name.ToUpperInvariant().Contains("NIMBUS")))
		{
			gun = null;
		}
		bool flag = (Object)(object)componentInHand == (Object)null && (Object)(object)componentInHand2 != (Object)null;
		if ((Object)(object)gun != (Object)null && (Object)(object)gun.triggerGrip != (Object)null && gun.triggerGrip.attachedHands != null)
		{
			bool flag2 = gun.triggerGrip.attachedHands.Contains(Player.LeftHand);
			bool flag3 = gun.triggerGrip.attachedHands.Contains(Player.RightHand);
			if (flag2 != flag3)
			{
				flag = flag2;
			}
		}
		Hand val = (flag ? Player.RightHand : Player.LeftHand);
		if ((Object)(object)controllerRig == (Object)null || (Object)(object)controllerRig.vrRoot == (Object)null || (Object)(object)controllerRig.headset == (Object)null || (Object)(object)gun == (Object)null || (Object)(object)gun.firePointTransform == (Object)null)
		{
			watched = null;
			taking = null;
			return;
		}
		bool flag4 = OtherHandOn(gun, val);
		string text = KeyOf(gun, flag4);
		Transform vrRoot = controllerRig.vrRoot;
		Bot.Pose head = Local(vrRoot, controllerRig.headset);
		Bot.Pose pose = Local(vrRoot, ((Component)(flag ? ((ControllerRig)controllerRig).leftController : ((ControllerRig)controllerRig).rightController)).transform);
		Bot.Pose pose2 = Local(vrRoot, ((Component)(flag ? ((ControllerRig)controllerRig).rightController : ((ControllerRig)controllerRig).leftController)).transform);
		Bot.Pose pose3 = Local(vrRoot, gun.firePointTransform);
		bool flag5 = Safe(() => gun.HasMagazine(), otherwise: true);
		if ((Object)(object)gun != (Object)(object)watched)
		{
			watched = gun;
			hadMagazine = flag5;
			taking = null;
		}
		if (hadMagazine && !flag5 && taking == null)
		{
			taking = new Reload
			{
				eject = 0f
			};
			takingSince = Time.time;
			takingKey = text;
			takingTwoHanded = flag4 || Safe(() => gun.otherGrips != null && gun.otherGrips.Length > 1, otherwise: false);
			takeFrameAt = 0f;
			Lab.Event("teach: reload started (" + text + ")");
		}
		hadMagazine = flag5;
		if (taking != null)
		{
			Take(gun, head, pose, pose2, flag5, val, flag);
		}
		else
		{
			if (Time.time < sampleAt)
			{
				return;
			}
			sampleAt = Time.time + 0.1f;
			bool flag6 = (flag ? ((ControllerRig)controllerRig).leftController : ((ControllerRig)controllerRig).rightController)._primaryAxis > 0.5f || Time.time - PlayerWatch.FiredAt < 0.6f;
			Rigidbody componentInParent = ((Component)gun).GetComponentInParent<Rigidbody>();
			if (!flag6 && (Object)(object)componentInParent != (Object)null)
			{
				Vector3 angularVelocity = componentInParent.angularVelocity;
				if (((Vector3)(ref angularVelocity)).magnitude > 1.2f)
				{
					return;
				}
			}
			float num = Vector3.Angle(head.rotation * Vector3.forward, pose3.rotation * Vector3.forward);
			Bot.Pose pose4 = head.Inverse() * pose;
			Bot.Pose pose5 = head.Inverse() * pose2;
			Quaternion val2 = Quaternion.Inverse(head.rotation) * pose3.rotation;
			if (flag)
			{
				pose4 = Mirrored(pose4);
				pose5 = Mirrored(pose5);
				val2 = Mirrored(new Bot.Pose(Vector3.zero, val2)).rotation;
			}
			if (num < 12f || (flag6 && num < 50f))
			{
				if (slotFresh && freshened.Add(text))
				{
					Record(text).aim = null;
					Lab.Event("teach: the aimed hold of " + text + " begun afresh" + (flag ? " (left-handed, learned mirrored)" : "") + (flag4 ? ", two hands" : ", one hand"));
				}
				Add(Record(text), aimed: true, pose4, flag4 ? new Bot.Pose?(pose5) : null, val2);
			}
			else if (num > 30f && !flag6)
			{
				Add(Record(text), aimed: false, pose4, flag4 ? new Bot.Pose?(pose5) : null, val2);
			}
		}
	}

	public static void SlotFresh(bool on)
	{
		slotFresh = on;
		freshened.Clear();
	}

	private static Bot.Pose Mirrored(Bot.Pose p)
	{
		return new Bot.Pose(new Vector3(0f - p.position.x, p.position.y, p.position.z), new Quaternion(p.rotation.x, 0f - p.rotation.y, 0f - p.rotation.z, p.rotation.w));
	}

	private static void Take(Gun gun, Bot.Pose head, Bot.Pose right, Bot.Pose left, bool has, Hand otherHand, bool lefty)
	{
		float num = Time.time - takingSince;
		if (num >= takeFrameAt)
		{
			takeFrameAt = num + 1f / 30f;
			Quaternion val = Quaternion.Euler(0f, ((Quaternion)(ref head.rotation)).eulerAngles.y, 0f);
			Bot.Pose pose = new Bot.Pose(head.position, val);
			Bot.Pose p = new Bot.Pose(Vector3.zero, Quaternion.Inverse(val) * head.rotation);
			Bot.Pose p2 = pose.Inverse() * right;
			Bot.Pose p3 = pose.Inverse() * left;
			if (lefty)
			{
				p = Mirrored(p);
				p2 = Mirrored(p2);
				p3 = Mirrored(p3);
			}
			taking.frames.Add(new ReloadFrame
			{
				t = num,
				head = P(p),
				right = P(p2),
				left = P(p3)
			});
		}
		GameObject val2 = (((Object)(object)otherHand != (Object)null) ? otherHand.m_CurrentAttachedGO : null);
		if (taking.grab < 0f && (Object)(object)val2 != (Object)null && (Object)(object)val2.GetComponentInParent<Magazine>() != (Object)null)
		{
			taking.grab = num;
		}
		if (taking.insert < 0f && has)
		{
			taking.insert = num;
		}
		bool flag = Safe(() => (int)gun.cartridgeState == 1, otherwise: false);
		if (taking.insert >= 0f && taking.rack < 0f && flag && num - taking.insert > 0.15f)
		{
			taking.rack = num;
		}
		if ((!(taking.insert >= 0f) || ((!(taking.rack >= 0f) || !(num - taking.rack > 0.4f)) && (!flag || !(num - taking.insert > 1.2f)))) && !(num > 10f))
		{
			return;
		}
		taking.end = num;
		if (taking.insert >= 0f && taking.frames.Count > 10)
		{
			string[] array = new string[1] { takingKey };
			foreach (string key in array)
			{
				List<Reload> list = Record(key).reloads;
				list.Add(taking);
				Reload added = taking;
				Learned(delegate
				{
					list.Remove(added);
				});
				if (list.Count > 8)
				{
					list.RemoveAt(0);
				}
			}
			dirty = true;
			Lab.Event("teach: reload learned (" + num.ToString("0.0") + " s, grab " + taking.grab.ToString("0.0") + ", in " + taking.insert.ToString("0.0") + ", rack " + taking.rack.ToString("0.0") + ")");
			Spawnables.Notify("Learned your reload");
		}
		taking = null;
	}

	private static void Add(GunRecord record, bool aimed, Bot.Pose right, Bot.Pose? left, Quaternion muzzle)
	{
		Hold hold = (aimed ? record.aim : record.low);
		if (hold == null)
		{
			hold = new Hold();
			if (aimed)
			{
				record.aim = hold;
			}
			else
			{
				record.low = hold;
			}
		}
		float num = 1f / (float)Mathf.Min(hold.n + 1, 200);
		hold.right = P(Bot.Pose.Lerp(U(hold.right, right), right, num));
		hold.muzzle = Q(Quaternion.Slerp(UQ(hold.muzzle, muzzle), muzzle, num));
		if (left.HasValue)
		{
			hold.left = P(Bot.Pose.Lerp(U(hold.left, left.Value), left.Value, num));
		}
		hold.n++;
		dirty = true;
		if (hold.n == 15 || hold.n == 100)
		{
			Lab.Event("teach: " + (aimed ? "aimed" : "low") + " hold learned (" + hold.n + " moments)" + (left.HasValue ? ", two hands" : ""));
		}
	}

	private static bool OtherHandOn(Gun gun, Hand other)
	{
		GameObject val = (((Object)(object)other != (Object)null) ? other.m_CurrentAttachedGO : null);
		if ((Object)(object)val == (Object)null || (Object)(object)val.GetComponentInParent<Magazine>() != (Object)null)
		{
			return false;
		}
		MarrowEntity componentInParent = ((Component)gun).GetComponentInParent<MarrowEntity>();
		if ((Object)(object)componentInParent != (Object)null)
		{
			return (Object)(object)val.GetComponentInParent<MarrowEntity>() == (Object)(object)componentInParent;
		}
		return false;
	}

	private static Bot.Pose Local(Transform root, Transform t)
	{
		return new Bot.Pose(root.InverseTransformPoint(t.position), Quaternion.Inverse(root.rotation) * t.rotation);
	}

	public static float[] P(Bot.Pose p)
	{
		return new float[7]
		{
			p.position.x,
			p.position.y,
			p.position.z,
			p.rotation.x,
			p.rotation.y,
			p.rotation.z,
			p.rotation.w
		};
	}

	public static Bot.Pose U(float[] a, Bot.Pose otherwise)
	{
		if (a == null || a.Length < 7)
		{
			return otherwise;
		}
		return new Bot.Pose(new Vector3(a[0], a[1], a[2]), new Quaternion(a[3], a[4], a[5], a[6]));
	}

	public static float[] Q(Quaternion q)
	{
		return new float[4] { q.x, q.y, q.z, q.w };
	}

	public static Quaternion UQ(float[] a, Quaternion otherwise)
	{
		if (a != null && a.Length >= 4)
		{
			return new Quaternion(a[0], a[1], a[2], a[3]);
		}
		return otherwise;
	}

	private static T Safe<T>(Func<T> f, T otherwise)
	{
		try
		{
			return f();
		}
		catch (Exception)
		{
			return otherwise;
		}
	}
}
