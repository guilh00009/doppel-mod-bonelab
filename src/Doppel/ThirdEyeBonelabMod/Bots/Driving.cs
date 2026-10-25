using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class Driving
{
	public enum Channel
	{
		LeftStickY,
		RightStickY,
		LeftTrigger,
		RightTrigger,
		LeftStickX,
		RightStickX
	}

	public class Model
	{
		public bool ok;

		public string why = "";

		public string barcode = "";

		public float seatAt;

		public float driveFrom;

		public float driveTo;

		public Clone.Event leftGrab;

		public Clone.Event rightGrab;

		public Clone.Event sit;

		public Channel go;

		public Channel back;

		public Channel steer;

		public float goSign = 1f;

		public float backSign = 1f;

		public float goPull;

		public bool hasBack;

		public bool stickSteer;

		public float steerGain;

		public float top;

		public float turnLeft;

		public float turnRight;

		public float side;

		public float forward = 1f;

		public bool inSeat;

		public Bot.Pose[] left = new Bot.Pose[11];

		public Bot.Pose[] right = new Bot.Pose[11];

		public Quaternion head = Quaternion.identity;

		public float fit;

		public float answers = 1f;

		public float trim;

		public List<Vector3> road = new List<Vector3>();

		public List<float> roadSpeed = new List<float>();

		public string mirrored = "";

		public void Hands(float turning, out Bot.Pose l, out Bot.Pose r)
		{
			float num = turnRight - turnLeft;
			float num2 = (Mathf.Clamp(turning, turnLeft - num * Tuning.F("drive.past", 0.25f), turnRight + num * Tuning.F("drive.past", 0.25f)) - turnLeft) / num * 10f;
			int num3 = Mathf.Clamp(Mathf.FloorToInt(num2), 0, 9);
			float num4 = num2 - (float)num3;
			l = new Bot.Pose(Vector3.LerpUnclamped(left[num3].position, left[num3 + 1].position, num4), Quaternion.SlerpUnclamped(left[num3].rotation, left[num3 + 1].rotation, num4));
			r = new Bot.Pose(Vector3.LerpUnclamped(right[num3].position, right[num3 + 1].position, num4), Quaternion.SlerpUnclamped(right[num3].rotation, right[num3 + 1].rotation, num4));
		}

		public string Said()
		{
			if (!ok)
			{
				return "cannot be driven from this lesson: " + why;
			}
			return "goes by " + Name(go, goSign) + " (" + goPull.ToString("0.0") + " m/s2)" + (hasBack ? (", back by " + Name(back, backSign)) : ", no going back shown") + ", steered by " + (stickSteer ? Name(steer, 1f) : ("its " + ((leftGrab != null && rightGrab != null) ? "two hands" : ((leftGrab != null) ? "left hand" : "right hand")) + " (fit " + fit.ToString("0.00") + ")")) + ", " + top.ToString("0.0") + " m/s at most, turns as tight as " + (1f / Mathf.Max(0.01f, Mathf.Max(0f - turnLeft, turnRight))).ToString("0.0") + " m, driven from " + driveFrom.ToString("0.0") + " to " + driveTo.ToString("0.0") + " s" + ((mirrored.Length > 0) ? (", turning " + mirrored + " hardly shown (made up from the other way)") : "") + ((road.Count > 1) ? (", its way kept (" + road.Count + " points)") : "") + (inSeat ? "" : " (an older recording: hands placed roughly)");
		}
	}

	private const int Bins = 11;

	private static readonly Dictionary<string, Model> models = new Dictionary<string, Model>();

	public static Model Live;

	private static string Name(Channel c, float sign)
	{
		return c switch
		{
			Channel.LeftStickY => "the left stick " + ((sign > 0f) ? "forward" : "back"), 
			Channel.RightStickY => "the right stick " + ((sign > 0f) ? "forward" : "back"), 
			Channel.LeftTrigger => "the left trigger", 
			Channel.RightTrigger => "the right trigger", 
			Channel.LeftStickX => "the left stick, side to side", 
			_ => "the right stick, side to side", 
		};
	}

	public static void Forget()
	{
		models.Clear();
	}

	public static Model LearnNow(Clone.Take take)
	{
		Model model = new Model();
		try
		{
			Learn(take, model);
		}
		catch (Exception ex)
		{
			model.ok = false;
			model.why = ex.Message;
		}
		return model;
	}

	public static Model Of(Lessons.Lesson lesson)
	{
		if (lesson == null || lesson.take == null)
		{
			return null;
		}
		string key = lesson.file ?? lesson.name;
		if (!models.TryGetValue(key, out var value))
		{
			value = new Model();
			try
			{
				Learn(lesson.take, value);
			}
			catch (Exception ex)
			{
				value.ok = false;
				value.why = ex.Message;
			}
			models[key] = value;
			Lab.Event("driving: the " + lesson.name + " " + value.Said());
		}
		return value;
	}

	public static float Read(Clone.Frame f, Channel c)
	{
		return c switch
		{
			Channel.LeftStickY => f.leftIn.stick.y, 
			Channel.RightStickY => f.rightIn.stick.y, 
			Channel.LeftTrigger => f.leftIn.trigger, 
			Channel.RightTrigger => f.rightIn.trigger, 
			Channel.LeftStickX => f.leftIn.stick.x, 
			_ => f.rightIn.stick.x, 
		};
	}

	private static Bot.Pose From(Clone.Frame f, bool inSeat)
	{
		if (inSeat)
		{
			return f.seat;
		}
		Vector3 val = f.pelvisTurn * Vector3.forward;
		val.y = 0f;
		return new Bot.Pose(f.pelvis, (((Vector3)(ref val)).sqrMagnitude > 0.001f) ? Quaternion.LookRotation(((Vector3)(ref val)).normalized, Vector3.up) : Quaternion.identity);
	}

	private static Vector3 Root(Clone.Frame f, bool inSeat)
	{
		if (!inSeat)
		{
			return f.pelvis + Vector3.up * 0.55f - f.playspace * f.head.position;
		}
		return f.root;
	}

	private static Bot.Pose World(Clone.Frame f, Bot.Pose local, bool inSeat)
	{
		return new Bot.Pose(Root(f, inSeat) + f.playspace * local.position, f.playspace * local.rotation);
	}

	private static void Learn(Clone.Take take, Model m)
	{
		List<Clone.Frame> frames = take.frames;
		float num = -1f;
		float num2 = float.MaxValue;
		foreach (Clone.Event @event in take.events)
		{
			if (@event.kind == Clone.Kind.Seat)
			{
				if (@event.hand == 1 && num < 0f)
				{
					num = @event.t;
					m.sit = @event;
					m.barcode = @event.barcode;
				}
				else if (@event.hand == 0 && num >= 0f && num2 == float.MaxValue)
				{
					num2 = @event.t;
				}
			}
		}
		if (num < 0f)
		{
			m.why = "you did not sit in anything";
			return;
		}
		m.seatAt = num;
		int i;
		for (i = 0; i < frames.Count && frames[i].t < num + 0.4f; i++)
		{
		}
		int num3 = frames.Count - 1;
		while (num3 > i && frames[num3].t > num2 - 0.1f)
		{
			num3--;
		}
		int num4 = num3 - i + 1;
		if (num4 < 150)
		{
			m.why = "you were hardly in it";
			return;
		}
		m.inSeat = frames[i].seated && frames[num3].seated;
		Vector3[] array = (Vector3[])(object)new Vector3[num4];
		Vector3[] array2 = (Vector3[])(object)new Vector3[num4];
		float[] array3 = new float[num4];
		float[] array4 = new float[num4];
		float[] array5 = new float[num4];
		float[] array6 = new float[num4];
		float[] array7 = new float[num4];
		bool[] array8 = new bool[num4];
		for (int j = 0; j < num4; j++)
		{
			Bot.Pose pose = From(frames[i + j], m.inSeat);
			array[j] = pose.position;
			Vector3 val = pose.rotation * Vector3.forward;
			val.y = 0f;
			array2[j] = ((((Vector3)(ref val)).sqrMagnitude > 0.001f) ? ((Vector3)(ref val)).normalized : Vector3.forward);
			array3[j] = Mathf.Atan2(array2[j].x, array2[j].z) * 57.29578f;
		}
		float num5 = 0f;
		for (int k = 6; k < num4 - 6; k++)
		{
			float num6 = frames[i + k + 6].t - frames[i + k - 6].t;
			if (!(num6 < 0.01f))
			{
				Vector3 val2 = (array[k + 6] - array[k - 6]) / num6;
				val2.y = 0f;
				array4[k] = Vector3.Dot(val2, array2[k]);
				array5[k] = Mathf.DeltaAngle(array3[k - 6], array3[k + 6]) * ((float)Math.PI / 180f) / num6;
				num5 += array4[k];
			}
		}
		if (num5 < 0f)
		{
			m.forward = -1f;
			for (int l = 0; l < num4; l++)
			{
				array4[l] = 0f - array4[l];
			}
		}
		int num7 = 0;
		int num8 = -1;
		int num9 = -1;
		for (int n = 18; n < num4 - 12 - 6; n++)
		{
			float num10 = frames[i + n + 12].t - frames[i + n - 12].t;
			array6[n] = ((num10 > 0.01f) ? ((array4[n + 12] - array4[n - 12]) / num10) : 0f);
			if (Mathf.Abs(array4[n]) > 0.7f)
			{
				if (num8 < 0)
				{
					num8 = n;
				}
				num9 = n;
			}
			array8[n] = Mathf.Abs(array4[n]) > 1.2f;
			if (array8[n])
			{
				num7++;
				array7[n] = array5[n] / array4[n];
			}
		}
		if (num7 < 90)
		{
			m.why = "you did not drive it anywhere (drive it about for a while: forward, turning both ways, stopping, backing)";
			return;
		}
		m.driveFrom = frames[i + num8].t;
		m.driveTo = frames[i + num9].t + 0.3f;
		if (m.inSeat)
		{
			for (int num11 = num8; num11 <= num9; num11++)
			{
				if (array4[num11] < 1.8f)
				{
					continue;
				}
				if (m.road.Count > 0)
				{
					Vector3 val3 = array[num11] - m.road[m.road.Count - 1];
					if (((Vector3)(ref val3)).magnitude < 1.5f)
					{
						continue;
					}
				}
				m.road.Add(array[num11]);
				m.roadSpeed.Add(Mathf.Abs(array4[num11]));
			}
		}
		Channel[] array9 = new Channel[4]
		{
			Channel.LeftStickY,
			Channel.RightStickY,
			Channel.LeftTrigger,
			Channel.RightTrigger
		};
		double[,] array10 = new double[6, 6];
		double[] array11 = new double[6];
		double[] array12 = new double[6];
		int num12 = 0;
		for (int num13 = 18; num13 < num4 - 12 - 6; num13++)
		{
			Clone.Frame f = frames[i + num13];
			for (int num14 = 0; num14 < 4; num14++)
			{
				array12[num14] = Read(f, array9[num14]);
			}
			array12[4] = array4[num13];
			array12[5] = 1.0;
			for (int num15 = 0; num15 < 6; num15++)
			{
				array11[num15] += array12[num15] * (double)array6[num13];
				for (int num16 = 0; num16 < 6; num16++)
				{
					array10[num15, num16] += array12[num15] * array12[num16];
				}
			}
			num12++;
		}
		for (int num17 = 0; num17 < 6; num17++)
		{
			array10[num17, num17] += 0.02 * (double)num12;
		}
		double[] array13 = Solve(array10, array11, 6);
		float num18 = 0.4f;
		float num19 = -0.4f;
		bool flag = false;
		for (int num20 = 0; num20 < 4; num20++)
		{
			List<float> list = new List<float>();
			List<float> list2 = new List<float>();
			for (int num21 = 0; num21 < num4; num21++)
			{
				float num22 = Read(frames[i + num21], array9[num20]);
				list.Add(Mathf.Max(0f, num22));
				list2.Add(Mathf.Max(0f, 0f - num22));
			}
			float num23 = Part(list, 0.97f);
			float num24 = Part(list2, 0.97f);
			for (int num25 = 0; num25 < 2; num25++)
			{
				float num26 = ((num25 == 0) ? num23 : num24);
				if (!(num26 < 0.3f))
				{
					float num27 = (float)array13[num20] * ((num25 == 0) ? 1f : (-1f));
					if (num27 * num26 > num18)
					{
						num18 = num27 * num26;
						m.go = array9[num20];
						m.goSign = ((num25 == 0) ? 1f : (-1f));
						m.goPull = num27;
						flag = true;
					}
					if (num27 * num26 < num19)
					{
						num19 = num27 * num26;
						m.back = array9[num20];
						m.backSign = ((num25 == 0) ? 1f : (-1f));
						m.hasBack = true;
					}
				}
			}
		}
		if (!flag)
		{
			m.why = "could not tell what makes it go";
			return;
		}
		List<float> list3 = new List<float>();
		List<float> list4 = new List<float>();
		List<float> list5 = new List<float>();
		for (int num28 = 0; num28 < num4; num28++)
		{
			if (array8[num28])
			{
				list3.Add(array4[num28]);
				list4.Add(Mathf.Abs(array7[num28]) * array4[num28] * array4[num28]);
				list5.Add(array7[num28]);
			}
		}
		m.top = Mathf.Max(2f, Part(list3, 0.95f));
		m.side = Mathf.Clamp(Part(list4, 0.9f), 2f, 9f);
		Channel[] array14 = new Channel[2]
		{
			Channel.LeftStickX,
			Channel.RightStickX
		};
		foreach (Channel channel in array14)
		{
			List<float> list6 = new List<float>();
			List<float> list7 = new List<float>();
			List<float> list8 = new List<float>();
			for (int num30 = 0; num30 < num4; num30++)
			{
				if (array8[num30])
				{
					list6.Add(Read(frames[i + num30], channel));
					list7.Add(array7[num30]);
					list8.Add(Mathf.Abs(list6[list6.Count - 1]));
				}
			}
			float num31 = Together(list6, list7);
			if (Mathf.Abs(num31) > 0.6f && Part(list8, 0.95f) > 0.3f && Mathf.Abs(num31) > m.fit)
			{
				float num32 = 0f;
				float num33 = 0f;
				for (int num34 = 0; num34 < list6.Count; num34++)
				{
					num32 += list6[num34] * list6[num34];
					num33 += list6[num34] * list7[num34];
				}
				m.stickSteer = true;
				m.steer = channel;
				m.steerGain = num33 / Mathf.Max(0.001f, num32);
				m.fit = Mathf.Abs(num31);
			}
		}
		m.leftGrab = HeldMost(take, 0, m.barcode, m.driveFrom, m.driveTo);
		m.rightGrab = HeldMost(take, 1, m.barcode, m.driveFrom, m.driveTo);
		Bot.Pose[] array15 = new Bot.Pose[num4];
		Bot.Pose[] array16 = new Bot.Pose[num4];
		Vector4 sum = Vector4.zero;
		for (int num35 = 0; num35 < num4; num35++)
		{
			Clone.Frame f2 = frames[i + num35];
			Bot.Pose pose2 = From(f2, m.inSeat).Inverse();
			array15[num35] = pose2 * World(f2, f2.left, m.inSeat);
			array16[num35] = pose2 * World(f2, f2.right, m.inSeat);
			Quaternion rotation = (pose2 * World(f2, f2.head, m.inSeat)).rotation;
			Add(ref sum, rotation);
		}
		m.head = Mean(sum);
		float[] array17 = new float[num4];
		if (m.stickSteer)
		{
			m.turnRight = Mathf.Abs(m.steerGain);
			m.turnLeft = 0f - m.turnRight;
			for (int num36 = 0; num36 < num4; num36++)
			{
				array17[num36] = m.steerGain * Read(frames[i + num36], m.steer);
			}
		}
		else
		{
			if (m.leftGrab == null && m.rightGrab == null)
			{
				m.why = "could not tell how you steer it (no stick turned it, and your hands were not on it)";
				return;
			}
			double[] array18 = new double[6];
			int num37 = 0;
			for (int num38 = 0; num38 < num4; num38++)
			{
				if (array8[num38])
				{
					double[] array19 = Feature(array15[num38], array16[num38], m);
					for (int num39 = 0; num39 < 6; num39++)
					{
						array18[num39] += array19[num39];
					}
					num37++;
				}
			}
			for (int num40 = 0; num40 < 6; num40++)
			{
				array18[num40] /= num37;
			}
			double[,] array20 = new double[6, 6];
			double[] array21 = new double[6];
			double num41 = 0.0;
			for (int num42 = 0; num42 < num4; num42++)
			{
				if (!array8[num42])
				{
					continue;
				}
				double[] array22 = Feature(array15[num42], array16[num42], m);
				for (int num43 = 0; num43 < 6; num43++)
				{
					array21[num43] += (array22[num43] - array18[num43]) * (double)array7[num42];
					for (int num44 = 0; num44 < 6; num44++)
					{
						array20[num43, num44] += (array22[num43] - array18[num43]) * (array22[num44] - array18[num44]);
					}
				}
			}
			for (int num45 = 0; num45 < 6; num45++)
			{
				num41 += array20[num45, num45];
			}
			for (int num46 = 0; num46 < 6; num46++)
			{
				array20[num46, num46] += 0.01 * num41 / 6.0 + 1E-06;
			}
			double[] array23 = Solve(array20, array21, 6);
			List<float> list9 = new List<float>();
			List<float> list10 = new List<float>();
			List<float> list11 = new List<float>();
			for (int num47 = 0; num47 < num4; num47++)
			{
				double[] array24 = Feature(array15[num47], array16[num47], m);
				double num48 = 0.0;
				for (int num49 = 0; num49 < 6; num49++)
				{
					num48 += array23[num49] * (array24[num49] - array18[num49]);
				}
				array17[num47] = (float)num48;
				if (frames[i + num47].t >= m.driveFrom && frames[i + num47].t <= m.driveTo)
				{
					list11.Add(array17[num47]);
				}
				if (array8[num47])
				{
					list9.Add(array17[num47]);
					list10.Add(array7[num47]);
				}
			}
			m.fit = Together(list9, list10);
			if (m.fit < 0.45f)
			{
				m.why = "could not tell how you steer it (drive it about longer, turning both ways)";
				return;
			}
			m.turnLeft = Mathf.Min(-0.02f, Part(list11, 0.03f));
			m.turnRight = Mathf.Max(0.02f, Part(list11, 0.97f));
		}
		Vector3[] array25 = (Vector3[])(object)new Vector3[11];
		Vector3[] array26 = (Vector3[])(object)new Vector3[11];
		Vector4[] array27 = (Vector4[])(object)new Vector4[11];
		Vector4[] array28 = (Vector4[])(object)new Vector4[11];
		int[] array29 = new int[11];
		for (int num50 = 0; num50 < num4; num50++)
		{
			float t = frames[i + num50].t;
			if (!(t < m.driveFrom) && !(t > m.driveTo))
			{
				int num51 = Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(m.turnLeft, m.turnRight, array17[num50]) * 10f), 0, 10);
				ref Vector3 reference = ref array25[num51];
				reference += array15[num50].position;
				ref Vector3 reference2 = ref array26[num51];
				reference2 += array16[num50].position;
				Add(ref array27[num51], array15[num50].rotation);
				Add(ref array28[num51], array16[num50].rotation);
				array29[num51]++;
			}
		}
		int num52 = 0;
		for (int num53 = 0; num53 < 11; num53++)
		{
			if (array29[num53] < 3)
			{
				array29[num53] = 0;
				continue;
			}
			m.left[num53] = new Bot.Pose(array25[num53] / (float)array29[num53], Mean(array27[num53]));
			m.right[num53] = new Bot.Pose(array26[num53] / (float)array29[num53], Mean(array28[num53]));
			num52++;
		}
		if (num52 == 0)
		{
			m.why = "nothing kept of where your hands were";
			return;
		}
		for (int num54 = 0; num54 < 11; num54++)
		{
			if (array29[num54] <= 0)
			{
				int num55 = num54;
				int num56 = num54;
				while (num55 >= 0 && array29[num55] == 0)
				{
					num55--;
				}
				for (; num56 < 11 && array29[num56] == 0; num56++)
				{
				}
				if (num55 >= 0 && num56 < 11)
				{
					float k2 = (float)(num54 - num55) / (float)(num56 - num55);
					m.left[num54] = Bot.Pose.Lerp(m.left[num55], m.left[num56], k2);
					m.right[num54] = Bot.Pose.Lerp(m.right[num55], m.right[num56], k2);
				}
				else
				{
					int num57 = ((num55 >= 0) ? num55 : num56);
					m.left[num54] = m.left[num57];
					m.right[num54] = m.right[num57];
				}
			}
		}
		if (!m.stickSteer)
		{
			int num58 = 0;
			int num59 = 10;
			for (; num58 < 10 && array29[num58] == 0; num58++)
			{
			}
			while (num59 > 0 && array29[num59] == 0)
			{
				num59--;
			}
			float num60 = Mathf.Lerp(m.turnLeft, m.turnRight, (float)num58 / 10f);
			float num61 = Mathf.Lerp(m.turnLeft, m.turnRight, (float)num59 / 10f);
			if (num58 > 0 || num59 < 10)
			{
				Bot.Pose[] array30 = new Bot.Pose[11];
				Bot.Pose[] array31 = new Bot.Pose[11];
				for (int num62 = 0; num62 < 11; num62++)
				{
					float num63 = Mathf.Lerp((float)num58, (float)num59, (float)num62 / 10f);
					int num64 = Mathf.Clamp(Mathf.FloorToInt(num63), 0, 9);
					array30[num62] = Bot.Pose.Lerp(m.left[num64], m.left[num64 + 1], num63 - (float)num64);
					array31[num62] = Bot.Pose.Lerp(m.right[num64], m.right[num64 + 1], num63 - (float)num64);
				}
				m.left = array30;
				m.right = array31;
				m.turnLeft = Mathf.Min(-0.02f, num60);
				m.turnRight = Mathf.Max(0.02f, num61);
			}
		}
		float num65 = Mathf.Max(0f - m.turnLeft, m.turnRight);
		float num66 = Mathf.Min(0f - m.turnLeft, m.turnRight);
		if (!m.stickSteer && num66 < num65 * 0.75f)
		{
			bool flag2 = 0f - m.turnLeft >= m.turnRight;
			Bot.Pose[] array32 = new Bot.Pose[11];
			Bot.Pose[] array33 = new Bot.Pose[11];
			for (int num67 = 0; num67 < 11; num67++)
			{
				float num68 = Mathf.Lerp(0f - num65, num65, (float)num67 / 10f);
				m.Hands(num68, out var l2, out var r);
				if (!(flag2 ? (num68 <= 0f) : (num68 >= 0f)))
				{
					m.Hands(0f - num68, out var l3, out var r2);
					float k3 = Mathf.Clamp01(Mathf.Abs(num68) / Mathf.Max(0.02f, num66));
					l2 = Bot.Pose.Lerp(l2, Mirror(r2), k3);
					r = Bot.Pose.Lerp(r, Mirror(l3), k3);
				}
				array32[num67] = l2;
				array33[num67] = r;
			}
			m.left = array32;
			m.right = array33;
			m.turnLeft = 0f - num65;
			m.turnRight = num65;
			m.mirrored = (flag2 ? "right" : "left");
		}
		m.ok = true;
	}

	private static Bot.Pose Mirror(Bot.Pose p)
	{
		return new Bot.Pose(new Vector3(0f - p.position.x, p.position.y, p.position.z), new Quaternion(p.rotation.x, 0f - p.rotation.y, 0f - p.rotation.z, p.rotation.w));
	}

	private static double[] Feature(Bot.Pose l, Bot.Pose r, Model m)
	{
		Vector3 val = ((m.leftGrab != null) ? l.position : Vector3.zero);
		Vector3 val2 = ((m.rightGrab != null) ? r.position : Vector3.zero);
		return new double[6] { val.x, val.y, val.z, val2.x, val2.y, val2.z };
	}

	private static Clone.Event HeldMost(Clone.Take take, int hand, string barcode, float from, float to)
	{
		Clone.Event holding = null;
		Clone.Event most = null;
		float since = 0f;
		float longest = 0f;
		foreach (Clone.Event @event in take.events)
		{
			if (@event.hand != hand)
			{
				continue;
			}
			if (@event.kind == Clone.Kind.Grab)
			{
				Close(@event.t);
				if (@event.barcode == barcode && barcode.Length > 0)
				{
					holding = @event;
					since = @event.t;
				}
			}
			else if (@event.kind == Clone.Kind.Release)
			{
				Close(@event.t);
			}
		}
		Close(take.Length);
		if (!(longest > (to - from) * 0.4f))
		{
			return null;
		}
		return most;
		void Close(float at)
		{
			if (holding != null)
			{
				float num = Mathf.Min(at, to) - Mathf.Max(since, from);
				if (num > longest)
				{
					longest = num;
					most = holding;
				}
				holding = null;
			}
		}
	}

	private static void Add(ref Vector4 sum, Quaternion q)
	{
		Vector4 val = default(Vector4);
		((Vector4)(ref val))._002Ector(q.x, q.y, q.z, q.w);
		if (Vector4.Dot(sum, val) < 0f)
		{
			val = -val;
		}
		sum += val;
	}

	private static Quaternion Mean(Vector4 sum)
	{
		if (((Vector4)(ref sum)).sqrMagnitude < 1E-08f)
		{
			return Quaternion.identity;
		}
		((Vector4)(ref sum)).Normalize();
		return new Quaternion(sum.x, sum.y, sum.z, sum.w);
	}

	private static float Part(List<float> values, float part)
	{
		if (values.Count == 0)
		{
			return 0f;
		}
		List<float> list = new List<float>(values);
		list.Sort();
		return list[Mathf.Clamp(Mathf.RoundToInt(part * (float)(list.Count - 1)), 0, list.Count - 1)];
	}

	private static float Together(List<float> a, List<float> b)
	{
		int num = Mathf.Min(a.Count, b.Count);
		if (num < 10)
		{
			return 0f;
		}
		double num2 = 0.0;
		double num3 = 0.0;
		for (int i = 0; i < num; i++)
		{
			num2 += (double)a[i];
			num3 += (double)b[i];
		}
		num2 /= (double)num;
		num3 /= (double)num;
		double num4 = 0.0;
		double num5 = 0.0;
		double num6 = 0.0;
		for (int j = 0; j < num; j++)
		{
			num4 += ((double)a[j] - num2) * ((double)b[j] - num3);
			num5 += ((double)a[j] - num2) * ((double)a[j] - num2);
			num6 += ((double)b[j] - num3) * ((double)b[j] - num3);
		}
		if (!(num5 < 1E-09) && !(num6 < 1E-09))
		{
			return (float)(num4 / Math.Sqrt(num5 * num6));
		}
		return 0f;
	}

	private static double[] Solve(double[,] a, double[] b, int n)
	{
		double[,] array = (double[,])a.Clone();
		double[] array2 = (double[])b.Clone();
		for (int i = 0; i < n; i++)
		{
			int num = i;
			for (int j = i + 1; j < n; j++)
			{
				if (Math.Abs(array[j, i]) > Math.Abs(array[num, i]))
				{
					num = j;
				}
			}
			if (Math.Abs(array[num, i]) < 1E-12)
			{
				continue;
			}
			if (num != i)
			{
				ref double reference;
				double num3;
				double num2;
				for (int k = 0; k < n; k++)
				{
					reference = ref array[i, k];
					ref double reference2 = ref array[num, k];
					num2 = array[num, k];
					num3 = array[i, k];
					reference = num2;
					reference2 = num3;
				}
				reference = ref array2[i];
				ref double reference3 = ref array2[num];
				num3 = array2[num];
				num2 = array2[i];
				reference = num3;
				reference3 = num2;
			}
			for (int l = 0; l < n; l++)
			{
				if (l == i)
				{
					continue;
				}
				double num4 = array[l, i] / array[i, i];
				if (num4 != 0.0)
				{
					for (int m = i; m < n; m++)
					{
						array[l, m] -= num4 * array[i, m];
					}
					array2[l] -= num4 * array2[i];
				}
			}
		}
		double[] array3 = new double[n];
		for (int num5 = 0; num5 < n; num5++)
		{
			array3[num5] = ((Math.Abs(array[num5, num5]) < 1E-12) ? 0.0 : (array2[num5] / array[num5, num5]));
		}
		return array3;
	}
}
