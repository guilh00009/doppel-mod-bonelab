using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BoneLib;
using Il2CppSLZ.Marrow;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class Recorder
{
	private const float Interval = 1f / 30f;

	private const string Header = "time,yaw,hx,hy,hz,height,hqx,hqy,hqz,hqw,lpx,lpy,lpz,lqx,lqy,lqz,lqw,rpx,rpy,rpz,rqx,rqy,rqz,rqw,sx,sy,tx,ty,lgrip,rgrip,ltrig,rtrig,la,lb,ra,rb,lheld,rheld,jump,pa,crouch,feet,jumped,pstick,sstick,enemies,edist,armed";

	private static StreamWriter writer;

	private static string path;

	private static float started;

	private static float next;

	private static float flushed;

	private static int frames;

	public static float PlayerJumpedAt = -10f;

	private static bool wasJumping;

	private static float recordedJumpAt = -10f;

	public static bool Recording => writer != null;

	public static string Folder => AssetFiles.SaveFolder("recordings");

	public static string CurrentFile
	{
		get
		{
			if (writer == null)
			{
				return null;
			}
			return path;
		}
	}

	public static void Watch()
	{
		RemapRig remapRig = Player.RemapRig;
		int num;
		if (remapRig != null)
		{
			num = (remapRig._jumping ? 1 : 0);
			if (num != 0 && !wasJumping)
			{
				PlayerJumpedAt = Time.time;
			}
		}
		else
		{
			num = 0;
		}
		wasJumping = (byte)num != 0;
	}

	public static void Set(bool on)
	{
		if (on)
		{
			Start();
		}
		else
		{
			Stop();
		}
	}

	private static void Start()
	{
		if (writer != null)
		{
			return;
		}
		try
		{
			Directory.CreateDirectory(Folder);
			path = Path.Combine(Folder, "rec_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv");
			writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			writer.WriteLine("time,yaw,hx,hy,hz,height,hqx,hqy,hqz,hqw,lpx,lpy,lpz,lqx,lqy,lqz,lqw,rpx,rpy,rpz,rqx,rqy,rqz,rqw,sx,sy,tx,ty,lgrip,rgrip,ltrig,rtrig,la,lb,ra,rb,lheld,rheld,jump,pa,crouch,feet,jumped,pstick,sstick,enemies,edist,armed");
			started = Time.time;
			next = 0f;
			recordedJumpAt = PlayerJumpedAt;
			flushed = Time.time;
			frames = 0;
			Spawnables.Notify("Recording your play");
		}
		catch (Exception ex)
		{
			writer = null;
			MelonLogger.Error("Recording could not start: " + ex.Message);
		}
	}

	public static void Stop()
	{
		if (writer != null)
		{
			try
			{
				writer.Dispose();
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("Recording could not be closed: " + ex.Message);
			}
			writer = null;
			Spawnables.Notify("Recording saved: " + (Time.time - started).ToString("0", CultureInfo.InvariantCulture) + " seconds");
		}
	}

	public static void Update()
	{
		if (writer == null)
		{
			return;
		}
		float num = Time.time - started;
		if (num < next)
		{
			return;
		}
		next = num + 1f / 30f;
		if (!Capture(out var frame))
		{
			return;
		}
		frame.time = num;
		Context(ref frame);
		frame.jumped = PlayerJumpedAt > recordedJumpAt;
		if (frame.jumped)
		{
			recordedJumpAt = PlayerJumpedAt;
		}
		frame.leftHeld = Held(Player.LeftHand);
		frame.rightHeld = Held(Player.RightHand);
		try
		{
			writer.WriteLine(Write(frame));
			frames++;
			if (Time.time - flushed > 2f)
			{
				writer.Flush();
				flushed = Time.time;
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Error("Recording stopped: " + ex.Message);
			writer = null;
		}
	}

	public static bool Capture(out Frame frame)
	{
		frame = default(Frame);
		OpenControllerRig controllerRig = Player.ControllerRig;
		if (controllerRig == null || controllerRig.vrRoot == null || controllerRig.headset == null || controllerRig.leftController == null || controllerRig.rightController == null)
		{
			return false;
		}
		Transform vrRoot = controllerRig.vrRoot;
		Transform headset = controllerRig.headset;
		Quaternion rotation = vrRoot.rotation;
		Quaternion quaternion = Quaternion.Inverse(rotation);
		Vector3 vector = vrRoot.InverseTransformPoint(headset.position);
		Quaternion quaternion2 = quaternion * headset.rotation;
		float y = Yaw(quaternion2 * Vector3.forward);
		Quaternion quaternion3 = Quaternion.Euler(0f, y, 0f);
		Quaternion quaternion4 = Quaternion.Inverse(quaternion3);
		Vector3 vector2 = new Vector3(vector.x, 0f, vector.z);
		Transform transform = controllerRig.leftController.transform;
		Transform transform2 = controllerRig.rightController.transform;
		frame.yaw = Yaw(rotation * quaternion3 * Vector3.forward);
		frame.headWorld = headset.position;
		frame.height = vector.y;
		frame.head = quaternion4 * quaternion2;
		frame.leftPos = quaternion4 * (vrRoot.InverseTransformPoint(transform.position) - vector2);
		frame.rightPos = quaternion4 * (vrRoot.InverseTransformPoint(transform2.position) - vector2);
		frame.leftRot = quaternion4 * (quaternion * transform.rotation);
		frame.rightRot = quaternion4 * (quaternion * transform2.rotation);
		BaseController leftController = controllerRig.leftController;
		BaseController rightController = controllerRig.rightController;
		frame.stick = leftController.GetThumbStickAxis();
		frame.turnStick = rightController.GetThumbStickAxis();
		frame.leftGrip = leftController._solvedGrip;
		frame.rightGrip = rightController._solvedGrip;
		frame.leftTrigger = leftController._primaryAxis;
		frame.rightTrigger = rightController._primaryAxis;
		frame.leftA = leftController._aButton;
		frame.leftB = leftController._bButton;
		frame.rightA = rightController._aButton;
		frame.rightB = rightController._bButton;
		frame.jump = controllerRig._secondaryAButton;
		frame.otherA = controllerRig._primaryAButton;
		frame.primaryStick = controllerRig._primaryStick;
		frame.secondaryStick = controllerRig._secondaryStick;
		RemapRig remapRig = Player.RemapRig;
		if (remapRig != null)
		{
			frame.crouch = remapRig._crouchTarget;
			frame.feet = remapRig._feetOffset;
		}
		return true;
	}

	private static void Context(ref Frame frame)
	{
		frame.enemyDistance = -1f;
		Transform head = Player.Head;
		if (head == null)
		{
			return;
		}
		float num = float.MaxValue;
		foreach (Targets.Target item in Targets.Hostiles())
		{
			frame.enemies++;
			num = Mathf.Min(num, (item.Point - head.position).magnitude);
		}
		if (frame.enemies > 0)
		{
			frame.enemyDistance = num;
		}
		frame.armed = Player.GetComponentInHand<Gun>(Player.RightHand) != null || Player.GetComponentInHand<Gun>(Player.LeftHand) != null;
	}

	public static float Yaw(Vector3 forward)
	{
		forward.y = 0f;
		if (!(forward.sqrMagnitude < 1E-06f))
		{
			return Mathf.Atan2(forward.x, forward.z) * 57.29578f;
		}
		return 0f;
	}

	private static string Held(Hand hand)
	{
		GameObject gameObject = ((hand != null) ? Player.GetObjectInHand(hand) : null);
		if (gameObject == null)
		{
			return "";
		}
		return gameObject.name.Replace(",", " ").Replace("\n", " ");
	}

	internal static string Write(Frame f)
	{
		StringBuilder stringBuilder = new StringBuilder(320);
		Add(stringBuilder, f.time);
		Add(stringBuilder, f.yaw);
		Add(stringBuilder, f.headWorld.x);
		Add(stringBuilder, f.headWorld.y);
		Add(stringBuilder, f.headWorld.z);
		Add(stringBuilder, f.height);
		Add(stringBuilder, f.head);
		Add(stringBuilder, f.leftPos);
		Add(stringBuilder, f.leftRot);
		Add(stringBuilder, f.rightPos);
		Add(stringBuilder, f.rightRot);
		Add(stringBuilder, f.stick.x);
		Add(stringBuilder, f.stick.y);
		Add(stringBuilder, f.turnStick.x);
		Add(stringBuilder, f.turnStick.y);
		Add(stringBuilder, f.leftGrip);
		Add(stringBuilder, f.rightGrip);
		Add(stringBuilder, f.leftTrigger);
		Add(stringBuilder, f.rightTrigger);
		Add(stringBuilder, f.leftA);
		Add(stringBuilder, f.leftB);
		Add(stringBuilder, f.rightA);
		Add(stringBuilder, f.rightB);
		stringBuilder.Append(f.leftHeld).Append(',').Append(f.rightHeld)
			.Append(',');
		Add(stringBuilder, f.jump);
		Add(stringBuilder, f.otherA);
		Add(stringBuilder, f.crouch);
		Add(stringBuilder, f.feet);
		Add(stringBuilder, f.jumped);
		Add(stringBuilder, f.primaryStick);
		Add(stringBuilder, f.secondaryStick);
		stringBuilder.Append(f.enemies).Append(',');
		Add(stringBuilder, f.enemyDistance);
		stringBuilder.Append(f.armed ? '1' : '0');
		return stringBuilder.ToString();
	}

	private static void Add(StringBuilder s, float v)
	{
		s.Append(v.ToString("0.####", CultureInfo.InvariantCulture)).Append(',');
	}

	private static void Add(StringBuilder s, bool v)
	{
		s.Append(v ? '1' : '0').Append(',');
	}

	private static void Add(StringBuilder s, Vector3 v)
	{
		Add(s, v.x);
		Add(s, v.y);
		Add(s, v.z);
	}

	private static void Add(StringBuilder s, Quaternion q)
	{
		Add(s, q.x);
		Add(s, q.y);
		Add(s, q.z);
		Add(s, q.w);
	}

	public static string Latest()
	{
		if (!Directory.Exists(Folder))
		{
			return null;
		}
		string result = null;
		DateTime dateTime = DateTime.MinValue;
		string[] files = Directory.GetFiles(Folder, "rec_*.csv");
		foreach (string text in files)
		{
			if (!(text == path) || writer == null)
			{
				DateTime lastWriteTime = File.GetLastWriteTime(text);
				if (lastWriteTime > dateTime)
				{
					dateTime = lastWriteTime;
					result = text;
				}
			}
		}
		return result;
	}

	public static bool ForgetLast(float seconds)
	{
		try
		{
			string text = ((writer != null) ? path : Latest());
			if (text == null)
			{
				return false;
			}
			float num;
			if (writer != null)
			{
				num = Time.time - started;
			}
			else
			{
				num = 0f;
				foreach (string item in File.ReadLines(text))
				{
					int num2 = item.IndexOf(',');
					if (num2 > 0 && float.TryParse(item.Substring(0, num2), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
					{
						num = result;
					}
				}
			}
			File.AppendAllText(text + ".forget", (num - seconds).ToString(CultureInfo.InvariantCulture) + "," + (num + 1f).ToString(CultureInfo.InvariantCulture) + "\n");
			return true;
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: could not forget the last part: " + ex.Message);
			return false;
		}
	}

	private static List<(float from, float to)> Forgotten(string file)
	{
		List<(float, float)> list = new List<(float, float)>();
		string text = file + ".forget";
		if (!File.Exists(text))
		{
			return list;
		}
		string[] array = File.ReadAllLines(text);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(',');
			if (array2.Length == 2 && float.TryParse(array2[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && float.TryParse(array2[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
			{
				list.Add((result, result2));
			}
		}
		return list;
	}

	public static List<Frame> Load(string file)
	{
		List<Frame> list = new List<Frame>();
		List<(float, float)> list2 = Forgotten(file);
		foreach (string item4 in File.ReadLines(file))
		{
			if (item4.StartsWith("time", StringComparison.Ordinal))
			{
				continue;
			}
			string[] c = item4.Split(',');
			if (c.Length < 38)
			{
				continue;
			}
			int i = 0;
			Frame item = default(Frame);
			item.time = F();
			item.yaw = F();
			item.headWorld = new Vector3(F(), F(), F());
			item.height = F();
			item.head = new Quaternion(F(), F(), F(), F());
			item.leftPos = new Vector3(F(), F(), F());
			item.leftRot = new Quaternion(F(), F(), F(), F());
			item.rightPos = new Vector3(F(), F(), F());
			item.rightRot = new Quaternion(F(), F(), F(), F());
			item.stick = new Vector2(F(), F());
			item.turnStick = new Vector2(F(), F());
			item.leftGrip = F();
			item.rightGrip = F();
			item.leftTrigger = F();
			item.rightTrigger = F();
			item.leftA = B();
			item.leftB = B();
			item.rightA = B();
			item.rightB = B();
			item.leftHeld = c[i++];
			item.rightHeld = c[i++];
			item.jump = item.rightA;
			if (c.Length >= 43)
			{
				item.jump = B();
				item.otherA = B();
				item.crouch = F();
				item.feet = F();
				item.jumped = B();
			}
			if (c.Length >= 45)
			{
				item.primaryStick = B();
				item.secondaryStick = B();
			}
			if (c.Length >= 48)
			{
				item.enemies = (int)F();
				item.enemyDistance = F();
				item.armed = B();
			}
			else
			{
				item.enemyDistance = -1f;
			}
			bool flag = false;
			foreach (var item5 in list2)
			{
				float item2 = item5.Item1;
				float item3 = item5.Item2;
				flag |= item.time >= item2 && item.time <= item3;
			}
			if (!flag)
			{
				list.Add(item);
			}
			bool B()
			{
				return c[i++] == "1";
			}
			float F()
			{
				return float.Parse(c[i++], CultureInfo.InvariantCulture);
			}
		}
		return list;
	}
}
