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
			float t = num2 - (float)num3;
			l = new Bot.Pose(Vector3.LerpUnclamped(left[num3].position, left[num3 + 1].position, t), Quaternion.SlerpUnclamped(left[num3].rotation, left[num3 + 1].rotation, t));
			r = new Bot.Pose(Vector3.LerpUnclamped(right[num3].position, right[num3 + 1].position, t), Quaternion.SlerpUnclamped(right[num3].rotation, right[num3 + 1].rotation, t));
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
		Vector3 vector = f.pelvisTurn * Vector3.forward;
		vector.y = 0f;
		return new Bot.Pose(f.pelvis, (vector.sqrMagnitude > 0.001f) ? Quaternion.LookRotation(vector.normalized, Vector3.up) : Quaternion.identity);
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
		Vector3[] array = new Vector3[num4];
		Vector3[] array2 = new Vector3[num4];
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
			Vector3 vector = pose.rotation * Vector3.forward;
			vector.y = 0f;
			array2[j] = ((vector.sqrMagnitude > 0.001f) ? vector.normalized : Vector3.forward);
			array3[j] = Mathf.Atan2(array2[j].x, array2[j].z) * 57.29578f;
		}
		float num5 = 0f;
		for (int k = 6; k < num4 - 6; k++)
		{
			float num6 = frames[i + k + 6].t - frames[i + k - 6].t;
			if (!(num6 < 0.01f))
			{
				Vector3 lhs = (array[k + 6] - array[k - 6]) / num6;
				lhs.y = 0f;
				array4[k] = Vector3.Dot(lhs, array2[k]);
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
				if (!(array4[num11] < 1.8f) && (m.road.Count <= 0 || !((array[num11] - m.road[m.road.Count - 1]).magnitude < 1.5f)))
				{
					m.road.Add(array[num11]);
					m.roadSpeed.Add(Mathf.Abs(array4[num11]));
				}
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
			float f2 = Together(list6, list7);
			if (Mathf.Abs(f2) > 0.6f && Part(list8, 0.95f) > 0.3f && Mathf.Abs(f2) > m.fit)
			{
				float num31 = 0f;
				float num32 = 0f;
				for (int num33 = 0; num33 < list6.Count; num33++)
				{
					num31 += list6[num33] * list6[num33];
					num32 += list6[num33] * list7[num33];
				}
				m.stickSteer = true;
				m.steer = channel;
				m.steerGain = num32 / Mathf.Max(0.001f, num31);
				m.fit = Mathf.Abs(f2);
			}
		}
		m.leftGrab = HeldMost(take, 0, m.barcode, m.driveFrom, m.driveTo);
		m.rightGrab = HeldMost(take, 1, m.barcode, m.driveFrom, m.driveTo);
		Bot.Pose[] array15 = new Bot.Pose[num4];
		Bot.Pose[] array16 = new Bot.Pose[num4];
		Vector4 sum = Vector4.zero;
		for (int num34 = 0; num34 < num4; num34++)
		{
			Clone.Frame f3 = frames[i + num34];
			Bot.Pose pose2 = From(f3, m.inSeat).Inverse();
			array15[num34] = pose2 * World(f3, f3.left, m.inSeat);
			array16[num34] = pose2 * World(f3, f3.right, m.inSeat);
			Quaternion rotation = (pose2 * World(f3, f3.head, m.inSeat)).rotation;
			Add(ref sum, rotation);
		}
		m.head = Mean(sum);
		float[] array17 = new float[num4];
		if (m.stickSteer)
		{
			m.turnRight = Mathf.Abs(m.steerGain);
			m.turnLeft = 0f - m.turnRight;
			for (int num35 = 0; num35 < num4; num35++)
			{
				array17[num35] = m.steerGain * Read(frames[i + num35], m.steer);
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
			int num36 = 0;
			for (int num37 = 0; num37 < num4; num37++)
			{
				if (array8[num37])
				{
					double[] array19 = Feature(array15[num37], array16[num37], m);
					for (int num38 = 0; num38 < 6; num38++)
					{
						array18[num38] += array19[num38];
					}
					num36++;
				}
			}
			for (int num39 = 0; num39 < 6; num39++)
			{
				array18[num39] /= num36;
			}
			double[,] array20 = new double[6, 6];
			double[] array21 = new double[6];
			double num40 = 0.0;
			for (int num41 = 0; num41 < num4; num41++)
			{
				if (!array8[num41])
				{
					continue;
				}
				double[] array22 = Feature(array15[num41], array16[num41], m);
				for (int num42 = 0; num42 < 6; num42++)
				{
					array21[num42] += (array22[num42] - array18[num42]) * (double)array7[num41];
					for (int num43 = 0; num43 < 6; num43++)
					{
						array20[num42, num43] += (array22[num42] - array18[num42]) * (array22[num43] - array18[num43]);
					}
				}
			}
			for (int num44 = 0; num44 < 6; num44++)
			{
				num40 += array20[num44, num44];
			}
			for (int num45 = 0; num45 < 6; num45++)
			{
				array20[num45, num45] += 0.01 * num40 / 6.0 + 1E-06;
			}
			double[] array23 = Solve(array20, array21, 6);
			List<float> list9 = new List<float>();
			List<float> list10 = new List<float>();
			List<float> list11 = new List<float>();
			for (int num46 = 0; num46 < num4; num46++)
			{
				double[] array24 = Feature(array15[num46], array16[num46], m);
				double num47 = 0.0;
				for (int num48 = 0; num48 < 6; num48++)
				{
					num47 += array23[num48] * (array24[num48] - array18[num48]);
				}
				array17[num46] = (float)num47;
				if (frames[i + num46].t >= m.driveFrom && frames[i + num46].t <= m.driveTo)
				{
					list11.Add(array17[num46]);
				}
				if (array8[num46])
				{
					list9.Add(array17[num46]);
					list10.Add(array7[num46]);
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
		Vector3[] array25 = new Vector3[11];
		Vector3[] array26 = new Vector3[11];
		Vector4[] array27 = new Vector4[11];
		Vector4[] array28 = new Vector4[11];
		int[] array29 = new int[11];
		for (int num49 = 0; num49 < num4; num49++)
		{
			float t = frames[i + num49].t;
			if (!(t < m.driveFrom) && !(t > m.driveTo))
			{
				int num50 = Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(m.turnLeft, m.turnRight, array17[num49]) * 10f), 0, 10);
				array25[num50] += array15[num49].position;
				array26[num50] += array16[num49].position;
				Add(ref array27[num50], array15[num49].rotation);
				Add(ref array28[num50], array16[num49].rotation);
				array29[num50]++;
			}
		}
		int num51 = 0;
		for (int num52 = 0; num52 < 11; num52++)
		{
			if (array29[num52] < 3)
			{
				array29[num52] = 0;
				continue;
			}
			m.left[num52] = new Bot.Pose(array25[num52] / array29[num52], Mean(array27[num52]));
			m.right[num52] = new Bot.Pose(array26[num52] / array29[num52], Mean(array28[num52]));
			num51++;
		}
		if (num51 == 0)
		{
			m.why = "nothing kept of where your hands were";
			return;
		}
		for (int num53 = 0; num53 < 11; num53++)
		{
			if (array29[num53] <= 0)
			{
				int num54 = num53;
				int num55 = num53;
				while (num54 >= 0 && array29[num54] == 0)
				{
					num54--;
				}
				for (; num55 < 11 && array29[num55] == 0; num55++)
				{
				}
				if (num54 >= 0 && num55 < 11)
				{
					float k2 = (float)(num53 - num54) / (float)(num55 - num54);
					m.left[num53] = Bot.Pose.Lerp(m.left[num54], m.left[num55], k2);
					m.right[num53] = Bot.Pose.Lerp(m.right[num54], m.right[num55], k2);
				}
				else
				{
					int num56 = ((num54 >= 0) ? num54 : num55);
					m.left[num53] = m.left[num56];
					m.right[num53] = m.right[num56];
				}
			}
		}
		if (!m.stickSteer)
		{
			int num57 = 0;
			int num58 = 10;
			for (; num57 < 10 && array29[num57] == 0; num57++)
			{
			}
			while (num58 > 0 && array29[num58] == 0)
			{
				num58--;
			}
			float b = Mathf.Lerp(m.turnLeft, m.turnRight, (float)num57 / 10f);
			float b2 = Mathf.Lerp(m.turnLeft, m.turnRight, (float)num58 / 10f);
			if (num57 > 0 || num58 < 10)
			{
				Bot.Pose[] array30 = new Bot.Pose[11];
				Bot.Pose[] array31 = new Bot.Pose[11];
				for (int num59 = 0; num59 < 11; num59++)
				{
					float num60 = Mathf.Lerp(num57, num58, (float)num59 / 10f);
					int num61 = Mathf.Clamp(Mathf.FloorToInt(num60), 0, 9);
					array30[num59] = Bot.Pose.Lerp(m.left[num61], m.left[num61 + 1], num60 - (float)num61);
					array31[num59] = Bot.Pose.Lerp(m.right[num61], m.right[num61 + 1], num60 - (float)num61);
				}
				m.left = array30;
				m.right = array31;
				m.turnLeft = Mathf.Min(-0.02f, b);
				m.turnRight = Mathf.Max(0.02f, b2);
			}
		}
		float num62 = Mathf.Max(0f - m.turnLeft, m.turnRight);
		float num63 = Mathf.Min(0f - m.turnLeft, m.turnRight);
		if (!m.stickSteer && num63 < num62 * 0.75f)
		{
			bool flag2 = 0f - m.turnLeft >= m.turnRight;
			Bot.Pose[] array32 = new Bot.Pose[11];
			Bot.Pose[] array33 = new Bot.Pose[11];
			for (int num64 = 0; num64 < 11; num64++)
			{
				float num65 = Mathf.Lerp(0f - num62, num62, (float)num64 / 10f);
				m.Hands(num65, out var l2, out var r);
				if (!(flag2 ? (num65 <= 0f) : (num65 >= 0f)))
				{
					m.Hands(0f - num65, out var l3, out var r2);
					float k3 = Mathf.Clamp01(Mathf.Abs(num65) / Mathf.Max(0.02f, num63));
					l2 = Bot.Pose.Lerp(l2, Mirror(r2), k3);
					r = Bot.Pose.Lerp(r, Mirror(l3), k3);
				}
				array32[num64] = l2;
				array33[num64] = r;
			}
			m.left = array32;
			m.right = array33;
			m.turnLeft = 0f - num62;
			m.turnRight = num62;
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
		Vector3 vector = ((m.leftGrab != null) ? l.position : Vector3.zero);
		Vector3 vector2 = ((m.rightGrab != null) ? r.position : Vector3.zero);
		return new double[6] { vector.x, vector.y, vector.z, vector2.x, vector2.y, vector2.z };
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
		Vector4 vector = new Vector4(q.x, q.y, q.z, q.w);
		if (Vector4.Dot(sum, vector) < 0f)
		{
			vector = -vector;
		}
		sum += vector;
	}

	private static Quaternion Mean(Vector4 sum)
	{
		if (sum.sqrMagnitude < 1E-08f)
		{
			return Quaternion.identity;
		}
		sum.Normalize();
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
				for (int k = 0; k < n; k++)
				{
					(array[i, k], array[num, k]) = (array[num, k], array[i, k]);
				}
				(array2[i], array2[num]) = (array2[num], array2[i]);
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
