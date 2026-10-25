using System;
using System.Collections.Generic;
using System.IO;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.SceneStreaming;
using Il2CppSLZ.Marrow.Warehouse;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class Lessons
{
	public class Lesson
	{
		public string file;

		public string item = "";

		public string name = "";

		public string level = "";

		public float length;

		public float savedAt;

		public Clone.Take take;

		public string purpose = "use";

		public float rise;

		public float riseAt;

		public float loopFrom;

		public float loopTo;

		public float loopRise;

		public float topAt;

		public float overRise;

		public float afterLoop;

		public int moved;

		public int harm;
	}

	private static readonly List<(Rigidbody rb, Vector3 at)> about = new List<(Rigidbody, Vector3)>();

	private static int harmed;

	private const int KeptOfEach = 6;

	private const float Longest = 90f;

	private static List<Lesson> all;

	private static IntPtr watched;

	private static Transform watchedThing;

	private static float lastHeldAt;

	private static bool wallLesson;

	private static float liveAt;

	private static bool liveSaid;

	private static readonly List<(Poolee thing, float at)> litter = new List<(Poolee, float)>();

	private static string Folder => AssetFiles.SaveFolder("lessons");

	public static string Level
	{
		get
		{
			try
			{
				return ((Scannable)SceneStreamer.Session.Level).Barcode.ID;
			}
			catch (Exception)
			{
				return "";
			}
		}
	}

	private static List<Lesson> All
	{
		get
		{
			if (all != null)
			{
				return all;
			}
			all = new List<Lesson>();
			try
			{
				if (Directory.Exists(Folder))
				{
					string[] files = Directory.GetFiles(Folder, "*.bin");
					Array.Sort(files, (IComparer<string>?)StringComparer.Ordinal);
					string[] array = files;
					foreach (string text in array)
					{
						try
						{
							Clone.Take take = Clone.Load(text);
							if (take.frames.Count > 10 && !string.IsNullOrEmpty(take.item))
							{
								Lesson lesson = new Lesson
								{
									file = text,
									item = take.item,
									name = take.itemName,
									level = take.level,
									length = take.Length,
									take = take,
									savedAt = -1000f
								};
								Analyse(lesson);
								all.Add(lesson);
							}
						}
						catch (Exception ex)
						{
							MelonLogger.Warning("Bot Player: a lesson could not be read (" + Path.GetFileName(text) + "): " + ex.Message);
						}
					}
				}
			}
			catch (Exception ex2)
			{
				MelonLogger.Warning("Bot Player: the lessons could not be read: " + ex2.Message);
			}
			return all;
		}
	}

	public static int Count => All.Count;

	public static Lesson For(string purpose)
	{
		List<Lesson> list = All.FindAll((Lesson l) => l.purpose == purpose);
		if (list.Count == 0)
		{
			return null;
		}
		return list[(Random.value < 0.7f) ? (list.Count - 1) : Random.Range(0, list.Count)];
	}

	public static Lesson With(string barcode)
	{
		if (string.IsNullOrEmpty(barcode))
		{
			return null;
		}
		for (int num = All.Count - 1; num >= 0; num--)
		{
			Lesson lesson = All[num];
			if (!(lesson.purpose == "drive") && !(lesson.purpose == "climb") && !(lesson.item == "wall") && lesson.take != null)
			{
				foreach (Clone.Event @event in lesson.take.events)
				{
					if (@event.barcode == barcode)
					{
						return lesson;
					}
				}
			}
		}
		return null;
	}

	public static List<Lesson> Drives()
	{
		List<Lesson> list = new List<Lesson>();
		HashSet<string> hashSet = new HashSet<string>();
		for (int num = All.Count - 1; num >= 0; num--)
		{
			if (!(All[num].purpose != "drive"))
			{
				Driving.Model model = Driving.Of(All[num]);
				if (model != null && model.ok && hashSet.Add(model.barcode))
				{
					list.Add(All[num]);
				}
			}
		}
		return list;
	}

	public static Lesson Drive()
	{
		for (int num = All.Count - 1; num >= 0; num--)
		{
			if (!(All[num].purpose != "drive"))
			{
				Driving.Model model = Driving.Of(All[num]);
				if (model != null && model.ok)
				{
					return All[num];
				}
			}
		}
		return null;
	}

	public static Driving.Model Road()
	{
		List<Driving.Model> list = new List<Driving.Model>();
		foreach (Lesson item in All)
		{
			if (!(item.purpose != "drive") && !(item.level != Level))
			{
				Driving.Model model = Driving.Of(item);
				if (model != null && model.road.Count > 8)
				{
					list.Add(model);
				}
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[(Random.value < 0.6f) ? (list.Count - 1) : Random.Range(0, list.Count)];
	}

	public static string DriveSaid()
	{
		int num = 0;
		foreach (Lesson item in All)
		{
			if (item.purpose == "drive" && Driving.Of(item) != null)
			{
				num++;
			}
		}
		return num + " lesson(s) with a seat";
	}

	public static string Purposes()
	{
		List<string> list = new List<string>();
		foreach (Lesson item in All)
		{
			list.Add(item.name + ": " + item.purpose + ((item.purpose == "climb") ? (" (" + item.rise.ToString("0.0") + " m up" + ((item.loopRise > 0f) ? (", " + item.loopRise.ToString("0.0") + " m a go, again and again") : "") + ")") : ""));
		}
		if (list.Count != 0)
		{
			return string.Join("; ", list);
		}
		return "no lessons";
	}

	private static void Analyse(Lesson l)
	{
		List<Clone.Frame> frames = l.take.frames;
		if (frames.Count < 10)
		{
			return;
		}
		ReadInfo(l);
		float y = frames[0].pelvis.y;
		float num = y;
		int num2 = 0;
		for (int i = 1; i < frames.Count; i++)
		{
			num = Mathf.Max(num, frames[i].pelvis.y);
			Vector3 val = frames[i].pelvis - frames[i - 1].pelvis;
			if (((Vector3)(ref val)).magnitude > 1.5f)
			{
				num2++;
			}
		}
		l.rise = num - y;
		float num3 = frames[frames.Count - 1].pelvis.y - y;
		int count = l.take.events.FindAll((Clone.Event e) => e.kind == Clone.Kind.Fire).Count;
		int count2 = l.take.events.FindAll((Clone.Event e) => e.kind == Clone.Kind.Spawn).Count;
		bool flag = l.take.events.Exists((Clone.Event e) => e.kind == Clone.Kind.Seat && e.hand == 1);
		l.purpose = (flag ? "drive" : ((num2 > 0) ? "portal" : ((l.rise > 1.5f && num3 > 1.2f) ? "climb" : ((l.harm > 0) ? "fight" : ((count > 0) ? "shoot" : ((count2 > 0) ? "make" : ((l.moved > 0) ? "move" : "use")))))));
		l.loopRise = 0f;
		if (l.purpose != "climb")
		{
			return;
		}
		int num4 = 0;
		for (int j = 0; j < frames.Count && frames[j].pelvis.y < y + l.rise * 0.9f; j++)
		{
			if (frames[j].pelvis.y <= y + 0.15f)
			{
				num4 = j;
			}
		}
		l.riseAt = frames[num4].t;
		l.topAt = l.take.Length;
		l.overRise = 0f;
		for (int k = num4; k < frames.Count; k++)
		{
			if (frames[k].pelvis.y >= y + l.rise * 0.9f)
			{
				l.topAt = frames[k].t;
				l.overRise = num - frames[k].pelvis.y;
				break;
			}
		}
		int m;
		for (m = num4; m < frames.Count - 1 && frames[m].pelvis.y < y + l.rise * 0.2f; m++)
		{
		}
		int num5 = -1;
		float num6 = float.MaxValue;
		for (int n = m + 1; n < frames.Count && frames[n].pelvis.y < y + l.rise * 0.88f; n++)
		{
			if (!(frames[n].t - frames[m].t < 1f) && !(frames[n].pelvis.y - frames[m].pelvis.y < 0.4f))
			{
				float num7 = Apart(frames[m], frames[n]);
				if (num7 < num6)
				{
					num6 = num7;
					num5 = n;
				}
			}
		}
		if (num5 > m)
		{
			l.loopFrom = frames[m].t;
			l.loopTo = frames[num5].t;
			l.loopRise = frames[num5].pelvis.y - frames[m].pelvis.y;
			l.afterLoop = num - frames[num5].pelvis.y;
		}
	}

	private static float Apart(Clone.Frame a, Clone.Frame b)
	{
		Vector3 val = a.left.position - a.head.position;
		Vector3 val2 = b.left.position - b.head.position;
		Vector3 val3 = a.right.position - a.head.position;
		Vector3 val4 = b.right.position - b.head.position;
		Vector3 val5 = val - val2;
		float magnitude = ((Vector3)(ref val5)).magnitude;
		val5 = val3 - val4;
		return magnitude + ((Vector3)(ref val5)).magnitude + 0.25f * Mathf.Abs(a.leftIn.grip - b.leftIn.grip) + 0.25f * Mathf.Abs(a.rightIn.grip - b.rightIn.grip) + 0.15f * ((a.leftIn.primary != b.leftIn.primary) ? 1f : 0f) + 0.15f * ((a.rightIn.primary != b.rightIn.primary) ? 1f : 0f);
	}

	private static void ReadInfo(Lesson l)
	{
		try
		{
			string path = l.file + ".info";
			if (!File.Exists(path))
			{
				return;
			}
			string[] array = File.ReadAllLines(path);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split('=');
				if (array2.Length == 2 && int.TryParse(array2[1], out var result))
				{
					if (array2[0] == "moved")
					{
						l.moved = result;
					}
					else if (array2[0] == "harm")
					{
						l.harm = result;
					}
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static void NoteHarm()
	{
		if (Clone.Lesson.On)
		{
			harmed++;
		}
	}

	private static void LookAbout()
	{
		about.Clear();
		harmed = 0;
		Transform head = Player.Head;
		if ((Object)(object)head == (Object)null)
		{
			return;
		}
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(head.position, 12f, -1, (QueryTriggerInteraction)1))
		{
			Rigidbody val = (((Object)(object)item != (Object)null) ? item.attachedRigidbody : null);
			if ((Object)(object)val == (Object)null || val.isKinematic || !hashSet.Add(((Il2CppObjectBase)(object)val).Pointer) || (Object)(object)((Component)val).GetComponentInParent<RigManager>() != (Object)null)
			{
				continue;
			}
			Vector3 velocity = val.velocity;
			if (!(((Vector3)(ref velocity)).magnitude > 0.3f) && (!((Object)(object)watchedThing != (Object)null) || !((Component)val).transform.IsChildOf(watchedThing)))
			{
				about.Add((val, val.worldCenterOfMass));
				if (about.Count >= 40)
				{
					break;
				}
			}
		}
	}

	public static void Reset()
	{
		Cancel();
		all = null;
	}

	public static void TellSeen()
	{
		foreach (Lesson item in All)
		{
			SelfTaught.Seen(item.item, item.name);
		}
	}

	public static string Summary()
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (Lesson item in All)
		{
			hashSet.Add(item.item);
		}
		return All.Count + " lessons with " + hashSet.Count + " tools";
	}

	public static string Names()
	{
		List<string> list = new List<string>();
		foreach (Lesson item in All)
		{
			if (!list.Contains(item.name))
			{
				list.Add(item.name);
			}
		}
		if (list.Count != 0)
		{
			return string.Join(", ", list);
		}
		return "nothing yet";
	}

	public static Lesson Pick(string notItem, string named = null)
	{
		List<string> list = new List<string>();
		foreach (Lesson i in All)
		{
			if ((named == null || i.name.IndexOf(named, StringComparison.OrdinalIgnoreCase) >= 0 || i.item.IndexOf(named, StringComparison.OrdinalIgnoreCase) >= 0) && !(i.purpose == "climb") && !(i.item == "wall") && (!(i.purpose == "drive") || named != null) && (named != null || !All.Exists((Lesson d) => d.purpose == "drive" && d.item == i.item)) && !list.Contains(i.item))
			{
				list.Add(i.item);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		if (list.Count > 1 && notItem != null)
		{
			list.Remove(notItem);
		}
		string thing = list[Random.Range(0, list.Count)];
		List<Lesson> list2 = All.FindAll((Lesson l) => l.item == thing && l.purpose != "climb" && (l.purpose != "drive" || named != null));
		float num = 0f;
		for (int j = 0; j < list2.Count; j++)
		{
			num += 1f + ((list2.Count > 1) ? ((float)j / (float)(list2.Count - 1)) : 0f);
		}
		float num2 = Random.value * num;
		for (int k = 0; k < list2.Count; k++)
		{
			num2 -= 1f + ((list2.Count > 1) ? ((float)k / (float)(list2.Count - 1)) : 0f);
			if (num2 <= 0f)
			{
				return list2[k];
			}
		}
		return list2[list2.Count - 1];
	}

	public static void OnGrip(Grip grip, Hand hand, bool on)
	{
		if (!on || (Object)(object)grip == (Object)null || (Object)(object)hand == (Object)null || ((Object)(object)hand != (Object)(object)Player.LeftHand && (Object)(object)hand != (Object)(object)Player.RightHand))
		{
			return;
		}
		NoteSeen(grip);
		if (!Recorder.Recording || Clone.Lesson.On)
		{
			return;
		}
		try
		{
			if ((Object)(object)((Component)grip).GetComponentInParent<RigManager>() != (Object)null)
			{
				return;
			}
			Poolee componentInParent = ((Component)grip).GetComponentInParent<Poolee>();
			if ((Object)(object)componentInParent == (Object)null || (Object)(object)componentInParent.SpawnableCrate == (Object)null)
			{
				return;
			}
			string iD = ((Scannable)componentInParent.SpawnableCrate).Barcode.ID;
			string name = ((Object)((Component)componentInParent).gameObject).name;
			string text = name.ToUpperInvariant();
			if (Clone.IsBody(iD, name) || text.Contains("SPAWN") || text.Contains("NIMBUS") || (Object)(object)((Component)componentInParent).GetComponent<Magazine>() != (Object)null)
			{
				return;
			}
			Gun componentInChildren = ((Component)componentInParent).GetComponentInChildren<Gun>();
			if ((!((Object)(object)componentInChildren != (Object)null) || !((Object)(object)componentInChildren.defaultMagazine != (Object)null)) && !text.Contains("AMMO") && !iD.ToUpperInvariant().Contains("AMMO") && Clone.Begin(Clone.Lesson))
			{
				Clone.Take taking = Clone.Lesson.taking;
				taking.item = iD;
				string text2 = null;
				try
				{
					text2 = ((Scannable)componentInParent.SpawnableCrate).Title;
				}
				catch (Exception)
				{
				}
				taking.itemName = (string.IsNullOrEmpty(text2) ? Clean(name) : text2);
				taking.level = Level;
				watched = ((Il2CppObjectBase)(object)componentInParent).Pointer;
				watchedThing = ((Component)componentInParent).transform;
				lastHeldAt = Time.time;
				LookAbout();
				Lab.Event("lesson: watching you with the " + taking.itemName);
			}
		}
		catch (Exception ex2)
		{
			MelonLogger.Warning("Bot Player: a lesson could not begin: " + ex2.Message);
		}
	}

	public static string NameOf(Poolee poolee)
	{
		string text = null;
		try
		{
			text = ((Scannable)poolee.SpawnableCrate).Title;
		}
		catch (Exception)
		{
		}
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return Clean(((Object)((Component)poolee).gameObject).name);
	}

	private static void NoteSeen(Grip grip)
	{
		try
		{
			if ((Object)(object)((Component)grip).GetComponentInParent<RigManager>() != (Object)null)
			{
				return;
			}
			Poolee componentInParent = ((Component)grip).GetComponentInParent<Poolee>();
			if (!((Object)(object)componentInParent == (Object)null) && !((Object)(object)componentInParent.SpawnableCrate == (Object)null))
			{
				string iD = ((Scannable)componentInParent.SpawnableCrate).Barcode.ID;
				string text = ((Object)((Component)componentInParent).gameObject).name.ToUpperInvariant();
				if (!Clone.IsBody(iD, ((Object)((Component)componentInParent).gameObject).name) && !text.Contains("SPAWN") && !text.Contains("NIMBUS") && !((Object)(object)((Component)componentInParent).GetComponentInChildren<Gun>() != (Object)null) && !((Object)(object)((Component)componentInParent).GetComponentInChildren<Magazine>() != (Object)null))
				{
					SelfTaught.Seen(iD, NameOf(componentInParent));
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private static string Clean(string name)
	{
		int num = name.IndexOf(" [", StringComparison.Ordinal);
		return ((num > 0) ? name.Substring(0, num) : name).Replace("(Clone)", "").Trim();
	}

	public static void Update()
	{
		Tidy();
		if (!Clone.Lesson.On)
		{
			Driving.Live = null;
			liveSaid = false;
			return;
		}
		if (Time.time >= liveAt)
		{
			liveAt = Time.time + Tuning.F("drive.live", 4f);
			Clone.Take taking = Clone.Lesson.taking;
			if (taking != null && taking.frames.Count > 300 && taking.events.Exists((Clone.Event e) => e.kind == Clone.Kind.Seat && e.hand == 1))
			{
				Driving.Model model = Driving.LearnNow(taking);
				if (model.ok)
				{
					Driving.Live = model;
					if (!liveSaid)
					{
						liveSaid = true;
						Lab.Event("driving, live: from how you are driving now, it " + model.Said());
					}
				}
			}
		}
		if (wallLesson)
		{
			if (WallGrab.Holding(Player.LeftHand) || WallGrab.Holding(Player.RightHand))
			{
				lastHeldAt = Time.time;
			}
			if (!Recorder.Recording || Time.time - lastHeldAt > 2.5f || Clone.Lesson.Now > 90f)
			{
				Finish(lastHeldAt - Clone.Lesson.startedAt + 1.5f);
			}
			return;
		}
		bool num = (Object)(object)watchedThing == (Object)null;
		if (!num && (Holds(Player.LeftHand) || Holds(Player.RightHand)))
		{
			lastHeldAt = Time.time;
		}
		if (num || !Recorder.Recording || Time.time - lastHeldAt > 1.5f || Clone.Lesson.Now > 90f)
		{
			Finish((!Recorder.Recording && Time.time - lastHeldAt < 0.2f) ? Clone.Lesson.Now : (lastHeldAt - Clone.Lesson.startedAt + 0.35f));
		}
	}

	public static void OnWall(int hand, bool on)
	{
		try
		{
			if (on && Recorder.Recording && !Clone.Lesson.On && Clone.Begin(Clone.Lesson))
			{
				Clone.Take taking = Clone.Lesson.taking;
				taking.item = "wall";
				taking.itemName = "Bare hands";
				taking.level = Level;
				wallLesson = true;
				watchedThing = null;
				lastHeldAt = Time.time;
				Lab.Event("lesson: watching you climb by your hands");
			}
			if (Clone.Lesson.On && wallLesson)
			{
				Clone.Lesson.taking.events.Add(new Clone.Event
				{
					t = Clone.Lesson.Now,
					kind = (on ? Clone.Kind.Grab : Clone.Kind.Release),
					hand = hand,
					name = "@wall"
				});
			}
		}
		catch (Exception)
		{
		}
	}

	private static bool Holds(Hand hand)
	{
		GameObject val = (((Object)(object)hand != (Object)null) ? hand.m_CurrentAttachedGO : null);
		if ((Object)(object)val != (Object)null && (Object)(object)watchedThing != (Object)null)
		{
			return val.transform.IsChildOf(watchedThing);
		}
		return false;
	}

	public static void Cancel()
	{
		Clone.End(Clone.Lesson);
		watchedThing = null;
	}

	private static void Finish(float until)
	{
		Clone.Take take = Clone.End(Clone.Lesson);
		watchedThing = null;
		bool flag = wallLesson;
		wallLesson = false;
		if (take == null)
		{
			return;
		}
		take.frames.RemoveAll((Clone.Frame f) => f.t > until);
		take.events.RemoveAll((Clone.Event e) => e.t > until);
		int num = 0;
		float num2 = 0f;
		Vector3 val;
		for (int i = 1; i < take.frames.Count; i++)
		{
			Clone.Frame frame = take.frames[i - 1];
			Clone.Frame frame2 = take.frames[i];
			num += Presses(frame.leftIn, frame2.leftIn) + Presses(frame.rightIn, frame2.rightIn);
			float num3 = frame2.t - frame.t;
			if (num3 > 0f)
			{
				float[] obj = new float[3] { num2, 0f, 0f };
				val = frame2.left.position - frame.left.position;
				obj[1] = ((Vector3)(ref val)).magnitude / num3;
				val = frame2.right.position - frame.right.position;
				obj[2] = ((Vector3)(ref val)).magnitude / num3;
				num2 = Mathf.Max(obj);
			}
		}
		int count = take.events.FindAll((Clone.Event e) => e.kind == Clone.Kind.Fire).Count;
		bool flag2 = take.events.Exists((Clone.Event e) => e.kind == Clone.Kind.Seat);
		if (flag)
		{
			float num4 = float.MaxValue;
			float num5 = float.MinValue;
			foreach (Clone.Frame frame3 in take.frames)
			{
				num4 = Mathf.Min(num4, frame3.pelvis.y);
				num5 = Mathf.Max(num5, frame3.pelvis.y);
			}
			flag2 = take.frames.Count > 30 && num5 - take.frames[0].pelvis.y > 1.2f;
			if (!flag2)
			{
				Lab.Event("lesson: your hands on a wall, but not up it (" + ((take.frames.Count > 0) ? (num5 - take.frames[0].pelvis.y).ToString("0.0") : "0") + " m), not kept");
				return;
			}
		}
		if (take.frames.Count < 2 || take.Length < 2f || (num == 0 && count == 0 && num2 < 2.5f && !flag2))
		{
			Lab.Event("lesson: nothing done with the " + take.itemName + " (" + take.Length.ToString("0.0") + " s), not kept");
			return;
		}
		try
		{
			Directory.CreateDirectory(Folder);
			string text = Path.Combine(Folder, Safe(take.item) + "__" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bin");
			Clone.Save(take, text);
			List<Lesson> list = All;
			Lesson lesson = new Lesson
			{
				file = text,
				item = take.item,
				name = take.itemName,
				level = take.level,
				length = take.Length,
				take = take,
				savedAt = Time.time
			};
			using (List<(Rigidbody, Vector3)>.Enumerator enumerator2 = about.GetEnumerator())
			{
				int moved;
				int num6;
				for (; enumerator2.MoveNext(); lesson.moved = moved + num6)
				{
					(Rigidbody, Vector3) current2 = enumerator2.Current;
					Rigidbody item = current2.Item1;
					Vector3 item2 = current2.Item2;
					moved = lesson.moved;
					if ((Object)(object)item != (Object)null)
					{
						val = item.worldCenterOfMass - item2;
						if (((Vector3)(ref val)).magnitude > 0.5f)
						{
							num6 = 1;
							continue;
						}
					}
					num6 = 0;
				}
			}
			lesson.harm = harmed;
			try
			{
				File.WriteAllLines(text + ".info", new string[2]
				{
					"moved=" + lesson.moved,
					"harm=" + lesson.harm
				});
			}
			catch (Exception)
			{
			}
			Analyse(lesson);
			list.Add(lesson);
			Lab.Event("lesson: the " + lesson.name + " is for: " + lesson.purpose + ((lesson.purpose == "climb") ? (" (" + lesson.rise.ToString("0.0") + " m up; the part to do again and again: " + ((lesson.loopRise > 0f) ? (lesson.loopFrom.ToString("0.0") + " to " + lesson.loopTo.ToString("0.0") + " s, " + lesson.loopRise.ToString("0.0") + " m each") : "none found") + ")") : "") + ((lesson.moved > 0) ? (", moved " + lesson.moved + " things") : "") + ((lesson.harm > 0) ? (", hurt something " + lesson.harm + " times") : ""));
			List<Lesson> list2 = list.FindAll((Lesson l) => l.item == take.item);
			for (int j = 0; j < list2.Count - 6; j++)
			{
				Remove(list2[j]);
			}
			foreach (Bot item3 in BotManager.All)
			{
				item3.LessonSoon();
			}
			string text2 = ((lesson.purpose == "climb") ? " to climb" : ((lesson.purpose == "portal") ? " to get somewhere" : ((lesson.purpose == "fight") ? " to fight" : ((lesson.purpose == "move") ? " to move things" : ""))));
			Spawnables.Notify("Learned how you use the " + take.itemName + text2 + " (" + take.Length.ToString("0") + " s" + ((num > 0) ? (", " + num + " presses") : "") + ")");
			Lab.Event("lesson: kept " + take.itemName + ", " + take.Length.ToString("0.0") + " s, " + num + " presses, " + count + " shots, " + take.events.Count + " events");
		}
		catch (Exception ex2)
		{
			MelonLogger.Warning("Bot Player: the lesson could not be saved: " + ex2.Message);
		}
	}

	private static int Presses(Clone.HandInput a, Clone.HandInput b)
	{
		return 0 + (((b.primary && !a.primary) || (b.trigger > 0.5f && a.trigger <= 0.5f && !b.primary)) ? 1 : 0) + ((b.a && !a.a) ? 1 : 0) + ((b.b && !a.b) ? 1 : 0) + ((b.menu && !a.menu) ? 1 : 0) + ((b.click && !a.click) ? 1 : 0) + ((b.pad && !a.pad) ? 1 : 0);
	}

	private static void Remove(Lesson lesson)
	{
		All.Remove(lesson);
		try
		{
			File.Delete(lesson.file);
		}
		catch (Exception)
		{
		}
	}

	public static int ForgetSince(float since)
	{
		int num = 0;
		if (Clone.Lesson.On)
		{
			Cancel();
			num++;
		}
		foreach (Lesson item in All.FindAll((Lesson l) => l.savedAt >= since))
		{
			Remove(item);
			num++;
		}
		return num;
	}

	private static string Safe(string barcode)
	{
		char[] array = barcode.ToCharArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (!char.IsLetterOrDigit(array[i]) && array[i] != '.' && array[i] != '-')
			{
				array[i] = '_';
			}
		}
		return new string(array);
	}

	public static void Litter(Poolee thing)
	{
		if (BotManager.OwnSpawns && (Object)(object)thing != (Object)null)
		{
			litter.Add((thing, Time.time));
		}
	}

	public static void Litter(Poolee thing, float after)
	{
		if (BotManager.OwnSpawns && (Object)(object)thing != (Object)null)
		{
			litter.Add((thing, Time.time - Tuning.F("lesson.litter", 30f) + after));
		}
	}

	public static void ForgetLevel()
	{
		litter.Clear();
		Cancel();
	}

	private static void Tidy()
	{
		for (int num = litter.Count - 1; num >= 0; num--)
		{
			var (val, num2) = litter[num];
			if ((Object)(object)val == (Object)null)
			{
				litter.RemoveAt(num);
			}
			else if (!(Time.time - num2 < Tuning.F("lesson.litter", 30f)))
			{
				litter.RemoveAt(num);
				try
				{
					bool flag = false;
					foreach (Grip componentsInChild in ((Component)val).GetComponentsInChildren<Grip>())
					{
						flag |= (Object)(object)componentsInChild != (Object)null && componentsInChild.attachedHands != null && componentsInChild.attachedHands.Count > 0;
					}
					foreach (Seat componentsInChild2 in ((Component)val).GetComponentsInChildren<Seat>(true))
					{
						flag |= (Object)(object)componentsInChild2 != (Object)null && (Object)(object)componentsInChild2.rigManager != (Object)null;
					}
					if (!flag)
					{
						val.Despawn();
					}
				}
				catch (Exception)
				{
				}
			}
		}
	}
}
