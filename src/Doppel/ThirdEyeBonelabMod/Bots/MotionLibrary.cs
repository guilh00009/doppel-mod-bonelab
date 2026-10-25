using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class MotionLibrary
{
	public enum Gait
	{
		Idle,
		Walk,
		Run
	}

	private const float WalkFrom = 0.12f;

	private const float RunFrom = 0.8f;

	private const float ShortestClip = 1.2f;

	private static readonly List<Frame[]>[] clips = new List<Frame[]>[3]
	{
		new List<Frame[]>(),
		new List<Frame[]>(),
		new List<Frame[]>()
	};

	private static int filesSeen = -1;

	private static readonly Dictionary<Frame[], (string name, float fresh)> about = new Dictionary<Frame[], (string, float)>();

	private const float Piece = 4f;

	private const float Step = 3f;

	private static readonly Dictionary<Frame[], (bool tense, float lively)> tags = new Dictionary<Frame[], (bool, float)>();

	private static readonly int[] tenseCount = new int[3];

	private static int rejected;

	private static int glancesSeen;

	private static float lastGlance = -10f;

	private const float SprintFrom = 2.9f;

	private static bool pickTense;

	public static float WalkPush { get; private set; } = 0.6f;


	public static float RunPush { get; private set; } = 1f;


	public static float PlayerFightDistance { get; private set; } = -1f;


	public static float GlanceEvery { get; private set; } = 2.5f;


	public static float StartTime { get; private set; } = 0.5f;


	public static float StopTime { get; private set; } = 0.35f;


	public static bool ClicksToRun { get; private set; }

	public static void Reload()
	{
		filesSeen = -1;
		Refresh();
	}

	public static string Summary()
	{
		return MoveSlots.Summary() + "; from watching: " + clips[0].Count + " standing, " + clips[1].Count + " walking, " + clips[2].Count + " running moves";
	}

	public static int Count(Gait gait)
	{
		return clips[(int)gait].Count + ((MoveSlots.For(gait, slow: false, null, null) != null) ? 5 : 0);
	}

	public static Gait For(float push)
	{
		if (!(push >= 0.8f))
		{
			if (!(push >= 0.12f))
			{
				return Gait.Idle;
			}
			return Gait.Walk;
		}
		return Gait.Run;
	}

	public static void Refresh()
	{
		try
		{
			string[] array = (Directory.Exists(Recorder.Folder) ? Directory.GetFiles(Recorder.Folder, "rec_*.csv") : new string[0]);
			if (array.Length == filesSeen)
			{
				return;
			}
			filesSeen = array.Length;
			List<Frame[]>[] array2 = clips;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].Clear();
			}
			about.Clear();
			tags.Clear();
			Array.Clear(tenseCount, 0, 3);
			rejected = 0;
			Array.Sort(array, (IComparer<string>?)StringComparer.Ordinal);
			int num = 0;
			List<float> list = new List<float>();
			List<float> list2 = new List<float>();
			List<float> list3 = new List<float>();
			int num2 = 0;
			int num3 = 0;
			string[] array3 = array;
			foreach (string text in array3)
			{
				if (Recorder.Recording && text == Recorder.CurrentFile)
				{
					continue;
				}
				List<Frame> list4;
				try
				{
					list4 = Recorder.Load(text);
				}
				catch (Exception)
				{
					continue;
				}
				float fresh = ((array.Length > 1) ? (0.5f + 1f * (float)num / (float)(array.Length - 1)) : 1f);
				num++;
				Cut(list4, Path.GetFileNameWithoutExtension(text), fresh);
				for (int j = 0; j < list4.Count; j++)
				{
					Frame frame = list4[j];
					if (frame.armed && frame.enemyDistance > 0f && frame.rightTrigger > 0.5f)
					{
						list3.Add(frame.enemyDistance);
					}
					float magnitude = frame.stick.magnitude;
					float num4 = Speed(list4, j);
					switch ((!(magnitude < 0.12f)) ? ((num4 >= 2.9f) ? Gait.Run : ((num4 > 0.3f) ? Gait.Walk : For(magnitude))) : Gait.Idle)
					{
					case Gait.Walk:
						list.Add(magnitude);
						break;
					case Gait.Run:
						list2.Add(magnitude);
						num2++;
						if (frame.primaryStick || frame.secondaryStick)
						{
							num3++;
						}
						break;
					}
				}
			}
			Habits(array);
			Rank();
			WalkPush = Median(list, 0.6f);
			RunPush = Median(list2, 1f);
			ClicksToRun = num2 > 30 && num3 > num2 / 2;
			PlayerFightDistance = ((list3.Count >= 20) ? Mathf.Clamp(Median(list3, 9f, 25f), 3f, 25f) : (-1f));
			MelonLogger.Msg("Bot Player learned from " + array.Length + " recordings: " + Count(Gait.Idle) + " standing, " + Count(Gait.Walk) + " walking, " + Count(Gait.Run) + " running clips (walks at " + WalkPush.ToString("0.00") + ", runs at " + RunPush.ToString("0.00") + (ClicksToRun ? ", clicking the stick" : "") + "); " + rejected + " stretches left out for bad tracking; with enemies about: " + tenseCount[0] + " standing, " + tenseCount[1] + " walking, " + tenseCount[2] + " running");
			MelonLogger.Msg("Bot Player, to teach next: " + TeachNext());
		}
		catch (Exception ex2)
		{
			MelonLogger.Warning("Bot Player could not read the recordings: " + ex2.Message);
		}
	}

	private static void Cut(List<Frame> frames, string file, float fresh)
	{
		int[] array = new int[frames.Count];
		for (int i = 0; i < frames.Count; i++)
		{
			array[i] = Kind(frames[i], Speed(frames, i));
		}
		int num = 0;
		for (int j = 1; j <= frames.Count; j++)
		{
			if (j != frames.Count && array[j] == array[num] && !(frames[j].time - frames[j - 1].time > 0.2f))
			{
				continue;
			}
			int num2 = array[num];
			if (num2 >= 0 && frames[j - 1].time - frames[num].time >= 1.2f)
			{
				int num3 = num;
				while (num3 < j)
				{
					int k;
					for (k = num3; k < j && frames[k].time - frames[num3].time < 4f; k++)
					{
					}
					if (k < j && frames[j - 1].time - frames[k].time < 1.2f)
					{
						k = j;
					}
					if (frames[k - 1].time - frames[num3].time >= 1.2f)
					{
						Add(num2, frames, num3, k, file, fresh);
					}
					if (k >= j)
					{
						break;
					}
					int l;
					for (l = num3; l < j && frames[l].time - frames[num3].time < 3f; l++)
					{
					}
					num3 = l;
				}
			}
			num = j;
		}
	}

	private static bool Good(List<Frame> frames, int from, int to)
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = from + 1; i < to; i++)
		{
			float num3 = frames[i].time - frames[i - 1].time;
			if (!(num3 <= 0f))
			{
				float magnitude = (frames[i].leftPos - frames[i - 1].leftPos).magnitude;
				float magnitude2 = (frames[i].rightPos - frames[i - 1].rightPos).magnitude;
				if (magnitude / num3 > 7f || magnitude2 / num3 > 7f || Mathf.Abs(frames[i].height - frames[i - 1].height) / num3 > 4f)
				{
					return false;
				}
				num = ((magnitude < 0.00015f) ? (num + num3) : 0f);
				num2 = ((magnitude2 < 0.00015f) ? (num2 + num3) : 0f);
				if (num > 0.6f || num2 > 0.6f)
				{
					return false;
				}
			}
		}
		return true;
	}

	private static void Add(int kind, List<Frame> frames, int from, int to, string file, float fresh)
	{
		if (!Good(frames, from, to))
		{
			rejected++;
			return;
		}
		Frame[] array = frames.GetRange(from, to - from).ToArray();
		int num = 0;
		float num2 = 0f;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].enemies > 0 && array[i].enemyDistance > 0f && array[i].enemyDistance < 20f)
			{
				num++;
			}
			if (i > 0)
			{
				num2 += (array[i].leftPos - array[i - 1].leftPos).magnitude + (array[i].rightPos - array[i - 1].rightPos).magnitude;
			}
		}
		float num3 = Mathf.Max(0.5f, array[^1].time - array[0].time);
		tags[array] = (num * 2 > array.Length, num2 / num3);
		if (num * 2 > array.Length)
		{
			tenseCount[kind]++;
		}
		about[array] = (file + ":" + from, fresh);
		float time = array[0].time;
		for (int j = 0; j < array.Length; j++)
		{
			array[j].time -= time;
		}
		clips[kind].Add(array);
	}

	private static void Habits(string[] files)
	{
		float num = 0f;
		int num2 = 0;
		List<float> list = new List<float>();
		List<float> list2 = new List<float>();
		foreach (string file in files)
		{
			List<Frame> list3;
			try
			{
				list3 = Recorder.Load(file);
			}
			catch (Exception)
			{
				continue;
			}
			float num3 = -1f;
			float num4 = -1f;
			bool flag = false;
			lastGlance = -10f;
			for (int j = 1; j < list3.Count; j++)
			{
				float num5 = list3[j].time - list3[j - 1].time;
				if (num5 <= 0f || num5 > 0.2f)
				{
					continue;
				}
				float num6 = Speed(list3, j);
				if (num6 < 0.3f)
				{
					num += num5;
					int num7 = j;
					bool flag2 = Mathf.Abs(list3[j].turnStick.x) > 0.3f;
					while (num7 > 0 && list3[j].time - list3[num7 - 1].time <= 0.25f)
					{
						num7--;
						flag2 |= Mathf.Abs(list3[num7].turnStick.x) > 0.3f;
					}
					if ((flag2 ? 0f : Mathf.Abs(Mathf.DeltaAngle(list3[num7].yaw, list3[j].yaw))) > 15f && list3[j].time - lastGlance > 0.5f)
					{
						num2++;
						lastGlance = list3[j].time;
					}
				}
				if (num6 < 0.3f)
				{
					if (flag && num4 >= 0f && list3[j].time - num4 < 2f)
					{
						list2.Add(list3[j].time - num4);
					}
					num3 = list3[j].time;
					flag = false;
				}
				else if (num6 > 1.5f)
				{
					if (!flag && num3 >= 0f && list3[j].time - num3 < 2f)
					{
						list.Add(list3[j].time - num3);
					}
					num4 = list3[j].time;
					flag = true;
				}
			}
		}
		glancesSeen = num2;
		GlanceEvery = ((num2 >= 10) ? Mathf.Clamp(num / (float)num2, 0.8f, 6f) : 2.5f);
		StartTime = ((list.Count >= 5) ? Mathf.Clamp(MedianOf(list), 0.15f, 1.5f) : 0.5f);
		StopTime = ((list2.Count >= 5) ? Mathf.Clamp(MedianOf(list2), 0.15f, 1.5f) : 0.35f);
		MelonLogger.Msg("Bot Player learned your habits: a glance about every " + GlanceEvery.ToString("0.0") + " s standing, " + StartTime.ToString("0.00") + " s to get going, " + StopTime.ToString("0.00") + " s to stop");
	}

	private static float MedianOf(List<float> values)
	{
		values.Sort();
		return values[values.Count / 2];
	}

	private static float Speed(List<Frame> frames, int j)
	{
		int num = j;
		int i = j;
		while (num > 0 && frames[j].time - frames[num - 1].time <= 0.25f)
		{
			num--;
		}
		for (; i < frames.Count - 1 && frames[i + 1].time - frames[j].time <= 0.25f; i++)
		{
		}
		float num2 = frames[i].time - frames[num].time;
		if (num2 < 0.05f)
		{
			return 0f;
		}
		Vector3 vector = frames[i].headWorld - frames[num].headWorld;
		return new Vector2(vector.x, vector.z).magnitude / num2;
	}

	private static int Kind(Frame f, float speed)
	{
		if (!string.IsNullOrEmpty(f.leftHeld) || !string.IsNullOrEmpty(f.rightHeld) || f.jump || f.jumped)
		{
			return -1;
		}
		if (Mathf.Abs(f.turnStick.x) > 0.5f || f.crouch < -0.25f || f.height < 1.1f)
		{
			return -1;
		}
		if (f.leftPos.y > f.height || f.rightPos.y > f.height || new Vector2(f.leftPos.x, f.leftPos.z).magnitude > 0.65f || new Vector2(f.rightPos.x, f.rightPos.z).magnitude > 0.65f)
		{
			return -1;
		}
		if (f.stick.magnitude < 0.12f && speed < 0.5f)
		{
			return 0;
		}
		if (speed < 0.3f)
		{
			return (int)For(f.stick.magnitude);
		}
		if (!(speed >= 2.9f))
		{
			return 1;
		}
		return 2;
	}

	public static Frame Relax(Frame f, float amount)
	{
		if (amount <= 0f)
		{
			return f;
		}
		f.leftPos = Relax(f.leftPos, f.height, -1f, amount);
		f.rightPos = Relax(f.rightPos, f.height, 1f, amount);
		return f;
	}

	private static Vector3 Relax(Vector3 hand, float height, float side, float amount)
	{
		float num = height - hand.y;
		float magnitude = new Vector2(hand.x, hand.z).magnitude;
		float num2 = Mathf.Clamp01((num - 0.05f) / 0.2f) * Mathf.Clamp01((0.75f - num) / 0.2f) * Mathf.Clamp01((0.55f - magnitude) / 0.15f);
		num2 *= amount;
		if (num2 <= 0f)
		{
			return hand;
		}
		float num3 = Tuning.F("relax.drop", 0.22f);
		return hand + new Vector3(side * 0.05f * num2, (0f - num3) * num2, (0f - Mathf.Max(0f, hand.z - 0.06f)) * 0.5f * num2);
	}

	private static float Median(List<float> values, float otherwise, float most = 1f)
	{
		if (values.Count < 20)
		{
			return otherwise;
		}
		values.Sort();
		return Mathf.Clamp(values[values.Count / 2], 0.2f, most);
	}

	public static Frame[] Pick(Gait gait, List<Frame[]> lately, Personality who, bool tense = false, bool slow = false, string avatar = null)
	{
		Frame[] array = MoveSlots.For(gait, slow, lately, avatar);
		if (array != null)
		{
			return array;
		}
		pickTense = tense;
		int[] array2 = gait switch
		{
			Gait.Walk => new int[3] { 1, 2, 0 }, 
			Gait.Idle => new int[3] { 0, 1, 2 }, 
			_ => new int[3] { 2, 1, 0 }, 
		};
		foreach (int num in array2)
		{
			List<Frame[]> list = clips[num];
			if (list.Count == 0)
			{
				continue;
			}
			float num2 = 0f;
			foreach (Frame[] item in list)
			{
				if (!lately.Contains(item))
				{
					num2 += Worth(item, who);
				}
			}
			if (num2 <= 0f)
			{
				return list[UnityEngine.Random.Range(0, list.Count)];
			}
			float num3 = UnityEngine.Random.value * num2;
			foreach (Frame[] item2 in list)
			{
				if (!lately.Contains(item2))
				{
					num3 -= Worth(item2, who);
					if (num3 <= 0f)
					{
						return item2;
					}
				}
			}
			return list[list.Count - 1];
		}
		return null;
	}

	private static float Worth(Frame[] clip, Personality who)
	{
		if (!about.TryGetValue(clip, out (string, float) value))
		{
			return 1f;
		}
		float num = value.Item2 * (who?.Likes(value.Item1) ?? 1f);
		if (tags.TryGetValue(clip, out (bool, float) value2))
		{
			num *= ((value2.Item1 == pickTense) ? Tuning.F("learn.context", 3f) : (1f / Tuning.F("learn.context", 3f)));
			if (who != null)
			{
				num *= Mathf.Lerp(1.7f, 0.5f, Mathf.Abs(value2.Item2 - Mathf.InverseLerp(0.4f, 1.6f, who.energy)));
			}
		}
		return num;
	}

	private static void Rank()
	{
		List<Frame[]>[] array = clips;
		foreach (List<Frame[]> list in array)
		{
			List<float> list2 = new List<float>();
			foreach (Frame[] item in list)
			{
				list2.Add(tags[item].lively);
			}
			list2.Sort();
			foreach (Frame[] item2 in list)
			{
				(bool, float) tuple = tags[item2];
				tags[item2] = (tuple.Item1, (list2.Count > 1) ? ((float)list2.BinarySearch(tuple.Item2) / (float)(list2.Count - 1)) : 0.5f);
			}
		}
	}

	public static string TeachNext()
	{
		string text = MoveSlots.EmptyKinds();
		if (text.Length > 0)
		{
			return "record these in Moves: " + text;
		}
		GunMoves.Lessons(out var guns, out var reloads, out var punches);
		List<(float, string)> list = new List<(float, string)>
		{
			((float)Count(Gait.Idle) / 40f, "stand about, hands empty, looking round (" + Count(Gait.Idle) + " standing moves)"),
			((float)Count(Gait.Walk) / 40f, "walk about without sprinting (" + Count(Gait.Walk) + " walking moves)"),
			((float)Count(Gait.Run) / 30f, "sprint: click the stick and run (" + Count(Gait.Run) + " running moves)"),
			((float)glancesSeen / 30f, "stand still and look about you (" + glancesSeen + " glances seen)"),
			((float)ClimbMoves.Count / 5f, "climb up onto ledges (" + ClimbMoves.Count + " climbs)"),
			((float)punches / 20f, "throw punches, hands empty (" + punches + " punches)"),
			((float)guns / 3f, "aim and fire different guns (" + guns + " guns known)"),
			((float)reloads / 6f, "reload by hand (" + reloads + " reloads)"),
			((float)(tenseCount[0] + tenseCount[1] + tenseCount[2]) / 15f, "move about, hands empty, with enemies near (" + (tenseCount[0] + tenseCount[1] + tenseCount[2]) + " such moves)")
		};
		list.Sort(((float have, string what) a, (float have, string what) b) => a.have.CompareTo(b.have));
		if (list[0].Item1 >= 1f)
		{
			return "nothing lacking: just keep playing with Learn from me on";
		}
		return list[0].Item2 + ((list[1].Item1 < 1f) ? ("; then " + list[1].Item2) : "");
	}

	public static Frame Sample(Frame[] clip, float t)
	{
		float time = clip[^1].time;
		if (time <= 0f)
		{
			return clip[0];
		}
		t = Mathf.Clamp(t, 0f, time);
		int num = 0;
		int num2 = clip.Length - 1;
		while (num2 - num > 1)
		{
			int num3 = (num + num2) / 2;
			if (clip[num3].time <= t)
			{
				num = num3;
			}
			else
			{
				num2 = num3;
			}
		}
		float k = Mathf.InverseLerp(clip[num].time, clip[num2].time, t);
		return Bot.Blend(clip[num], clip[num2], k);
	}

	public static Frame Mirror(Frame f)
	{
		Frame result = f;
		result.head = Flip(f.head);
		result.leftPos = Flip(f.rightPos);
		result.rightPos = Flip(f.leftPos);
		result.leftRot = Flip(f.rightRot);
		result.rightRot = Flip(f.leftRot);
		result.leftGrip = f.rightGrip;
		result.rightGrip = f.leftGrip;
		return result;
	}

	private static Vector3 Flip(Vector3 v)
	{
		return new Vector3(0f - v.x, v.y, v.z);
	}

	private static Quaternion Flip(Quaternion q)
	{
		return new Quaternion(q.x, 0f - q.y, 0f - q.z, q.w);
	}
}
