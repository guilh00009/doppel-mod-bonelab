using System;
using System.Collections.Generic;
using System.IO;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class NetBots
{
	private struct Wanted
	{
		public byte owner;

		public byte id;

		public bool enemy;

		public bool dead;

		public string avatar;

		public Vector3 scale;

		public Vector3 feet;

		public float yaw;
	}

	public static Func<bool> Online = () => false;

	public static Action<byte[], bool> Send = delegate
	{
	};

	public static Func<byte, bool> WithUs = (byte who) => true;

	public static Action<Gun> GunShot = delegate
	{
	};

	public static Func<MarrowEntity, ushort> Own = (MarrowEntity thing) => ushort.MaxValue;

	public static Func<ushort, Transform> Shared = (ushort number) => (Transform)null;

	public static Func<byte> Me = () => 0;

	public static Func<List<RigManager>> Others = () => NoOthers;

	public static Func<RigManager, int> Hide = (RigManager rig) => 0;

	private static float hideAt;

	private static int hidden;

	private static readonly List<RigManager> NoOthers = new List<RigManager>();

	private static List<RigManager> others = NoOthers;

	private static float othersAt = -10f;

	private static int hitsSent;

	private static int hitsTaken;

	public const string PuppetMark = "ThirdEyeNetPuppet";

	private const byte RosterKind = 1;

	private const byte PoseKind = 2;

	private const byte HitKind = 3;

	private const float PoseEvery = 0.05f;

	private const float RosterEvery = 2f;

	public const float Behind = 0.12f;

	private const float Gone = 8f;

	private static readonly Dictionary<int, Bot> puppets = new Dictionary<int, Bot>();

	private static readonly HashSet<int> making = new HashSet<int>();

	private static readonly Dictionary<int, Wanted> wanted = new Dictionary<int, Wanted>();

	private static readonly Dictionary<byte, float> heardFrom = new Dictionary<byte, float>();

	private static float poseAt;

	private static float rosterAt;

	private static float makeAt;

	private static int nextId = 1;

	private static int ownCountSent = -1;

	private static int own_;

	private static int sent;

	private static int got;

	private static int rosters;

	private static float tallyAt;

	private static bool wasOnline;

	private static float complainedAt = -100f;

	public static float Now => Time.realtimeSinceStartup;

	public static int PuppetCount => puppets.Count;

	public static List<RigManager> OtherPlayers()
	{
		if (Time.time - othersAt > 0.5f)
		{
			othersAt = Time.time;
			try
			{
				others = (Online() ? Others() : NoOthers);
			}
			catch (Exception)
			{
				others = NoOthers;
			}
		}
		return others;
	}

	public static bool IsOther(RigManager rig)
	{
		if ((Object)(object)rig == (Object)null)
		{
			return false;
		}
		foreach (RigManager item in OtherPlayers())
		{
			if ((Object)(object)item == (Object)(object)rig)
			{
				return true;
			}
		}
		return false;
	}

	public static void SendHit(byte owner, byte id, float damage, int part, bool bullet)
	{
		try
		{
			using MemoryStream memoryStream = new MemoryStream(16);
			using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write((byte)3);
			binaryWriter.Write(owner);
			binaryWriter.Write(id);
			binaryWriter.Write(damage);
			binaryWriter.Write((byte)Mathf.Clamp(part, 0, 255));
			binaryWriter.Write(bullet);
			Send(memoryStream.ToArray(), arg2: true);
			hitsSent++;
		}
		catch (Exception)
		{
		}
	}

	private static void Say(string what)
	{
		Lab.Event("net: " + what);
		MelonLogger.Msg("Bot Player net: " + what);
	}

	private static int Key(byte owner, byte id)
	{
		return (owner << 8) | id;
	}

	public static void Update(IEnumerable<Bot> all)
	{
		if (Now >= hideAt)
		{
			hideAt = Now + 2f;
			try
			{
				foreach (Bot item in all)
				{
					if (item.Alive)
					{
						int num = Hide(item.rig);
						if (num > 0)
						{
							hidden += num;
							Say("the body of " + (item.Puppet ? "a puppet" : ("our bot " + item.Name)) + " had been taken as a thing of the level's by a player who took hold of it: given back (" + hidden + " times in all)");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Say("the bots' bodies could not be kept out of Fusion's sharing: " + ex.Message);
				hideAt = Now + 30f;
			}
		}
		bool flag = false;
		try
		{
			flag = Online();
		}
		catch (Exception)
		{
		}
		if (flag != wasOnline)
		{
			wasOnline = flag;
			Say(flag ? "with other players now: bots are shared" : "on our own again");
			tallyAt = Now + 15f;
		}
		if (!flag)
		{
			if (puppets.Count > 0 || wanted.Count > 0)
			{
				Clear("on our own again");
			}
			ownCountSent = -1;
			return;
		}
		int num2 = 0;
		own_ = 0;
		foreach (Bot item2 in all)
		{
			if (!item2.Puppet && item2.Alive)
			{
				num2 = (own_ = num2 + 1);
				if (item2.netId == 0)
				{
					item2.netId = nextId;
					nextId = ((nextId >= 250) ? 1 : (nextId + 1));
				}
			}
		}
		if (Now >= tallyAt)
		{
			tallyAt = Now + 30f;
			Say("in the last while sent " + sent + ", got " + got + " (" + rosters + " lists); " + own_ + " of ours, " + puppets.Count + " puppet(s), " + wanted.Count + " wanted, heard from " + heardFrom.Count + " player(s); hits told " + hitsSent + ", taken " + hitsTaken + "; other players here " + OtherPlayers().Count);
			sent = (got = (rosters = 0));
		}
		if (Now >= poseAt)
		{
			poseAt = Now + 0.05f;
			foreach (Bot item3 in all)
			{
				if (!item3.Puppet && item3.Alive && item3.netId != 0)
				{
					byte[] array = item3.NetPose();
					if (array != null)
					{
						Send(array, arg2: false);
						sent++;
					}
				}
			}
		}
		if (Now >= rosterAt || num2 != ownCountSent)
		{
			rosterAt = Now + 2f;
			ownCountSent = num2;
			Send(Roster(all, num2), arg2: true);
		}
		List<int> list = null;
		foreach (KeyValuePair<int, Bot> puppet in puppets)
		{
			byte key2 = (byte)(puppet.Key >> 8);
			float value;
			bool flag2 = !heardFrom.TryGetValue(key2, out value) || Now - value > 8f;
			if (!puppet.Value.Alive || flag2)
			{
				(list ?? (list = new List<int>())).Add(puppet.Key);
			}
		}
		if (list != null)
		{
			foreach (int item4 in list)
			{
				BotManager.NetRemove(puppets[item4]);
				puppets.Remove(item4);
				wanted.Remove(item4);
			}
		}
		if (!(Now >= makeAt))
		{
			return;
		}
		makeAt = Now + 0.3f;
		foreach (KeyValuePair<int, Wanted> item5 in wanted)
		{
			Wanted w = item5.Value;
			if (!w.dead && !puppets.ContainsKey(item5.Key) && !making.Contains(item5.Key) && heardFrom.TryGetValue(w.owner, out var value2) && !(Now - value2 > 8f))
			{
				int key = item5.Key;
				making.Add(key);
				if (!BotManager.NetSpawn(w.enemy, w.avatar, w.scale, w.feet, w.yaw, delegate(Bot bot)
				{
					making.Remove(key);
					if (bot != null)
					{
						if (!wanted.ContainsKey(key))
						{
							BotManager.NetRemove(bot);
						}
						else
						{
							bot.BecomePuppet(w.owner, w.id);
							puppets[key] = bot;
							Say("a puppet made of player " + w.owner + "'s bot " + w.id + (w.enemy ? " (an enemy)" : "") + ", " + puppets.Count + " in all");
						}
					}
				}))
				{
					making.Remove(key);
				}
				break;
			}
		}
	}

	private static void Clear(string why)
	{
		foreach (Bot value in puppets.Values)
		{
			BotManager.NetRemove(value);
		}
		if (puppets.Count > 0)
		{
			Say(puppets.Count + " puppet(s) taken away: " + why);
		}
		puppets.Clear();
		wanted.Clear();
		making.Clear();
		heardFrom.Clear();
	}

	public static void Forget()
	{
		Clear("the level is over");
	}

	private static byte[] Roster(IEnumerable<Bot> all, int own)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write((byte)1);
		binaryWriter.Write((byte)Mathf.Min(own, 255));
		int num = 0;
		foreach (Bot item in all)
		{
			if (!item.Puppet && item.Alive && item.netId != 0 && num < 255)
			{
				num++;
				binaryWriter.Write((byte)item.netId);
				binaryWriter.Write((byte)((item.enemy ? 1u : 0u) | (item.dead ? 2u : 0u)));
				binaryWriter.Write(BotRig.AvatarOf(item.rig) ?? "");
				Vector3 v = (((Object)(object)item.rig.avatar != (Object)null) ? ((Component)item.rig.avatar).transform.localScale : Vector3.one);
				Put(binaryWriter, v);
				Put(binaryWriter, item.NetFeet);
				binaryWriter.Write(item.NetYaw);
			}
		}
		return memoryStream.ToArray();
	}

	public static void Receive(byte from, byte[] bytes)
	{
		if (bytes == null || bytes.Length < 2)
		{
			return;
		}
		try
		{
			if (!WithUs(from))
			{
				return;
			}
			using BinaryReader binaryReader = new BinaryReader(new MemoryStream(bytes));
			byte b = binaryReader.ReadByte();
			got++;
			if (b == 1)
			{
				rosters++;
			}
			switch (b)
			{
			case 1:
				ReadRoster(from, binaryReader);
				break;
			case 2:
				ReadPose(from, binaryReader);
				break;
			case 3:
				ReadHit(from, binaryReader);
				break;
			}
		}
		catch (Exception ex)
		{
			if (Now - complainedAt > 5f)
			{
				complainedAt = Now;
				Say("could not read what player " + from + " sent (" + bytes.Length + " bytes): " + ex.Message);
			}
		}
	}

	private static void ReadRoster(byte from, BinaryReader r)
	{
		if (!heardFrom.ContainsKey(from))
		{
			Say("player " + from + " has the mod too");
		}
		heardFrom[from] = Now;
		int num = r.ReadByte();
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < num; i++)
		{
			Wanted wanted = default(Wanted);
			wanted.owner = from;
			wanted.id = r.ReadByte();
			Wanted value = wanted;
			byte b = r.ReadByte();
			value.enemy = (b & 1) != 0;
			value.dead = (b & 2) != 0;
			value.avatar = r.ReadString();
			value.scale = Vec(r);
			value.feet = Vec(r);
			value.yaw = r.ReadSingle();
			int num2 = Key(from, value.id);
			hashSet.Add(num2);
			NetBots.wanted[num2] = value;
			if (puppets.TryGetValue(num2, out var value2) && value2.Alive && !value2.dead)
			{
				value2.NetWear(value.avatar, value.scale);
			}
		}
		List<int> list = null;
		foreach (int key in NetBots.wanted.Keys)
		{
			if (key >> 8 == from && !hashSet.Contains(key))
			{
				(list ?? (list = new List<int>())).Add(key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			NetBots.wanted.Remove(item);
			if (puppets.TryGetValue(item, out var value3))
			{
				BotManager.NetRemove(value3);
				puppets.Remove(item);
			}
		}
	}

	private static void ReadHit(byte from, BinaryReader r)
	{
		byte num = r.ReadByte();
		byte b = r.ReadByte();
		float damage = r.ReadSingle();
		int part = r.ReadByte();
		bool bullet = r.ReadBoolean();
		if (num != Me())
		{
			return;
		}
		foreach (Bot item in BotManager.All)
		{
			if (!item.Puppet && item.netId == b && item.Alive)
			{
				hitsTaken++;
				item.NetHit(damage, part, bullet, from);
				break;
			}
		}
	}

	private static void ReadPose(byte from, BinaryReader r)
	{
		heardFrom[from] = Now;
		byte id = r.ReadByte();
		if (puppets.TryGetValue(Key(from, id), out var value) && value.Alive)
		{
			byte b = r.ReadByte();
			Clone.Frame f = default(Clone.Frame);
			f.t = r.ReadSingle();
			f.pelvis = Vec(r);
			f.pelvisVelocity = Small(r, 100f);
			f.pelvisTurn = Turn(r);
			f.playspace = Turn(r);
			f.head = new Bot.Pose(Small(r, 1000f), Turn(r));
			f.left = new Bot.Pose(Small(r, 1000f), Turn(r));
			f.right = new Bot.Pose(Small(r, 1000f), Turn(r));
			f.leftIn = Fingers(r);
			f.rightIn = Fingers(r);
			f.crouch = (float)r.ReadInt16() / 1000f;
			f.feet = (float)r.ReadInt16() / 1000f;
			ushort leftHolds = r.ReadUInt16();
			ushort rightHolds = r.ReadUInt16();
			value.NetFrame(f, (b & 1) != 0, (b & 2) != 0, leftHolds, rightHolds);
		}
	}

	public static byte[] Pack(byte id, bool dead, bool down, Clone.Frame f, ushort leftHolds, ushort rightHolds)
	{
		using MemoryStream memoryStream = new MemoryStream(128);
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write((byte)2);
		binaryWriter.Write(id);
		binaryWriter.Write((byte)((dead ? 1u : 0u) | (down ? 2u : 0u)));
		binaryWriter.Write(f.t);
		Put(binaryWriter, f.pelvis);
		PutSmall(binaryWriter, f.pelvisVelocity, 100f);
		Put(binaryWriter, f.pelvisTurn);
		Put(binaryWriter, f.playspace);
		PutSmall(binaryWriter, f.head.position, 1000f);
		Put(binaryWriter, f.head.rotation);
		PutSmall(binaryWriter, f.left.position, 1000f);
		Put(binaryWriter, f.left.rotation);
		PutSmall(binaryWriter, f.right.position, 1000f);
		Put(binaryWriter, f.right.rotation);
		Put(binaryWriter, f.leftIn);
		Put(binaryWriter, f.rightIn);
		binaryWriter.Write((short)Mathf.Clamp(f.crouch * 1000f, -32000f, 32000f));
		binaryWriter.Write((short)Mathf.Clamp(f.feet * 1000f, -32000f, 32000f));
		binaryWriter.Write(leftHolds);
		binaryWriter.Write(rightHolds);
		return memoryStream.ToArray();
	}

	private static void Put(BinaryWriter w, Vector3 v)
	{
		w.Write(v.x);
		w.Write(v.y);
		w.Write(v.z);
	}

	private static Vector3 Vec(BinaryReader r)
	{
		return new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
	}

	private static void PutSmall(BinaryWriter w, Vector3 v, float per)
	{
		w.Write((short)Mathf.Clamp(v.x * per, -32000f, 32000f));
		w.Write((short)Mathf.Clamp(v.y * per, -32000f, 32000f));
		w.Write((short)Mathf.Clamp(v.z * per, -32000f, 32000f));
	}

	private static Vector3 Small(BinaryReader r, float per)
	{
		return new Vector3((float)r.ReadInt16() / per, (float)r.ReadInt16() / per, (float)r.ReadInt16() / per);
	}

	private static void Put(BinaryWriter w, Quaternion q)
	{
		w.Write((short)(Mathf.Clamp(q.x, -1f, 1f) * 32000f));
		w.Write((short)(Mathf.Clamp(q.y, -1f, 1f) * 32000f));
		w.Write((short)(Mathf.Clamp(q.z, -1f, 1f) * 32000f));
		w.Write((short)(Mathf.Clamp(q.w, -1f, 1f) * 32000f));
	}

	private static Quaternion Turn(BinaryReader r)
	{
		Quaternion val = default(Quaternion);
		((Quaternion)(ref val))._002Ector((float)r.ReadInt16() / 32000f, (float)r.ReadInt16() / 32000f, (float)r.ReadInt16() / 32000f, (float)r.ReadInt16() / 32000f);
		float num = Mathf.Sqrt(val.x * val.x + val.y * val.y + val.z * val.z + val.w * val.w);
		if (!(num < 0.5f))
		{
			return new Quaternion(val.x / num, val.y / num, val.z / num, val.w / num);
		}
		return Quaternion.identity;
	}

	private static byte Share(float of)
	{
		return (byte)(Mathf.Clamp01(of) * 255f);
	}

	private static void Put(BinaryWriter w, Clone.HandInput h)
	{
		w.Write(Share(h.index));
		w.Write(Share(h.middle));
		w.Write(Share(h.ring));
		w.Write(Share(h.pinky));
		w.Write(Share(h.thumb));
		w.Write(Share(h.grip));
		w.Write(Share(h.trigger));
	}

	private static Clone.HandInput Fingers(BinaryReader r)
	{
		Clone.HandInput handInput = default(Clone.HandInput);
		handInput.index = (float)(int)r.ReadByte() / 255f;
		handInput.middle = (float)(int)r.ReadByte() / 255f;
		handInput.ring = (float)(int)r.ReadByte() / 255f;
		handInput.pinky = (float)(int)r.ReadByte() / 255f;
		handInput.thumb = (float)(int)r.ReadByte() / 255f;
		handInput.grip = (float)(int)r.ReadByte() / 255f;
		handInput.trigger = (float)(int)r.ReadByte() / 255f;
		Clone.HandInput result = handInput;
		result.primary = result.trigger > 0.5f;
		return result;
	}
}
