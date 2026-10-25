using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using BoneLib;
using Il2CppSLZ.Data;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.AI;
using Il2CppSLZ.Marrow.Audio;
using Il2CppSLZ.Marrow.Combat;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Utilities;
using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.VRMK;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Il2CppTMPro;
using MelonLoader;
using Portals.MonoBehaviours;
using UnityEngine;
using UnityEngine.AI;

namespace ThirdEyeBonelabMod.Bots;

public class Bot
{
	public enum Mode
	{
		Stand,
		Follow,
		Wander,
		Gunplay,
		Brain,
		Fists,
		Clone,
		Exact,
		Copy,
		Replay
	}

	public enum Goal
	{
		Idle,
		Fight,
		Retreat,
		TakeCover,
		ArmUp,
		Investigate,
		Explore,
		Follow,
		Guard,
		Play,
		Practice,
		Brawl
	}

	private enum CoverPhase
	{
		None,
		Going,
		Hidden,
		Peeking
	}

	private enum Fid
	{
		None,
		Hand,
		Scratch,
		Neck,
		Stretch,
		Hips,
		Shake,
		Down,
		Behind
	}

	private enum Hold
	{
		Aimed,
		Low,
		Reload
	}

	private enum Step
	{
		OneHand,
		Reach,
		Settle,
		Ready
	}

	private enum Play
	{
		None,
		GoTo,
		Reach,
		Hold,
		WindUp,
		Throw,
		Drop,
		Shoot,
		Tinker,
		Melee,
		Shove,
		Carry,
		Catch
	}

	private enum Then
	{
		Throw,
		TossToYou,
		Drop,
		ToPartner
	}

	private enum Act
	{
		Idle,
		Stroll,
		Jog,
		Sprint,
		Climb,
		Play,
		Shoot,
		Visit,
		Explore,
		Use,
		Tinker,
		Go,
		Sit
	}

	private class Strike
	{
		public Frame[] clip;

		public float peak;

		public bool right;
	}

	private enum Reload
	{
		None,
		Eject,
		Fetch,
		Insert,
		Rack
	}

	public struct Pose
	{
		public Vector3 position;

		public Quaternion rotation;

		public static readonly Pose identity = new Pose(Vector3.zero, Quaternion.identity);

		public Pose(Vector3 position, Quaternion rotation)
		{
			this.position = position;
			this.rotation = rotation;
		}

		public static Pose Of(Transform t)
		{
			return new Pose(t.position, t.rotation);
		}

		public static Pose operator *(Pose a, Pose b)
		{
			return new Pose(a.position + a.rotation * b.position, a.rotation * b.rotation);
		}

		public Pose Inverse()
		{
			Quaternion quaternion = Quaternion.Inverse(rotation);
			return new Pose(quaternion * -position, quaternion);
		}

		public static Pose Lerp(Pose a, Pose b, float k)
		{
			return new Pose(Vector3.Lerp(a.position, b.position, k), Quaternion.Slerp(a.rotation, b.rotation, k));
		}
	}

	private enum Sight
	{
		Open,
		Thing,
		High,
		Bot,
		New,
		You,
		Tall,
		Drive
	}

	private struct Eased
	{
		public Vector3 from;

		public Quaternion turnFrom;

		public float at;

		public float turnAt;

		public bool turning;

		public bool moving;
	}

	public readonly RigManager rig;

	public readonly Personality personality;

	private readonly OpenControllerRig controls;

	public Mode mode;

	private Frame pose;

	private float bodyYaw;

	private Vector2 walk;

	private Vector2 stick;

	private bool jump;

	private bool otherA;

	private bool primaryClick;

	private bool secondaryClick;

	private bool running;

	private float runningSince;

	private bool crouching;

	private float crouch;

	private float feet;

	private float jumpDue = -1f;

	private int jumpsCleared;

	private float jumpClearSaidAt = -10f;

	private float jumpAsked;

	private bool wasJumping;

	private float jumpedAt = -10f;

	private float curl = 0.25f;

	private System.Collections.Generic.List<Frame> replay;

	private int replayAt;

	private int lastJumpFrame = -1;

	private float replayStart;

	private float replayYaw;

	private readonly Navigator nav = new Navigator();

	private float idleUntil;

	private float ledgeJumpAt = -10f;

	private float stride;

	private float headYaw;

	private bool following;

	private Frame[] clip;

	private Frame[] fromClip;

	private float clipT;

	private float fromT;

	private float clipLeft;

	private float fade = 1f;

	private bool clipMirror;

	private bool fromMirror;

	private float clipRate = 1f;

	private float fromRate = 1f;

	private readonly System.Collections.Generic.List<Frame[]> lately = new System.Collections.Generic.List<Frame[]>();

	private MotionLibrary.Gait gait;

	private MotionLibrary.Gait wantGait;

	private float wantSince;

	private readonly float born;

	private bool heard;

	private float metHeight = 1.65f;

	public bool enemy;

	public const string EnemyMark = "BotEnemyMark";

	private bool broken;

	private float partsOffSince = -1f;

	private int mended;

	private float mendedAt = -100f;

	private Vector2 gameStick;

	private float seatOutAt = -10f;

	private int driveCalls;

	private int driveStops;

	private int cloneDrives;

	private bool gameSprints;

	public int LastDriveFrame;

	private static int named;

	private string nameIs;

	private float walkMost;

	private float sprintClickAt = -10f;

	private float sprintSaidAt = -10f;

	private float runTraceUntil = -10f;

	private float runTraceAt;

	private float seatedLooseSince = -1f;

	private int seatedTries;

	private float waveUntil;

	private float waveStart;

	private Vector3 followSideWas;

	private float dropSaidAt = -10f;

	private string travelEnded = "";

	private float noGunSaidAt = -100f;

	private Vector3 steer;

	private float goYawWas;

	private float goAt = -10f;

	private float goSaidAt = -10f;

	private float goByOthers;

	private float goByWay;

	private float goByApart;

	private Vector3 goDirWas;

	private System.IntPtr eyesOf;

	private float headCutSaidAt = -10f;

	private float badSaidAt = -10f;

	private static float pointSince = -1f;

	private static float pointedAt = -100f;

	private static Vector3 pointPlace;

	private static bool pointHas;

	private static int pointFrame = -1;

	private float pointSeen = -100f;

	private float stareSince = -1f;

	private float staredAt = -100f;

	private static Vector3 youStoodAt;

	private static float youStoodSince;

	private static int youFrame = -1;

	private readonly System.Collections.Generic.Dictionary<string, float> calledAt = new System.Collections.Generic.Dictionary<string, float>();

	private const float ThinkEvery = 0.15f;

	private const float LearnEvery = 3f;

	private const float CloseCallRange = 1.6f;

	private const float CellSize = 5f;

	private readonly System.Collections.Generic.List<Targets.Target> hostiles = new System.Collections.Generic.List<Targets.Target>();

	private readonly System.Collections.Generic.List<Vector3> seenEnemies = new System.Collections.Generic.List<Vector3>();

	private Targets.Target nearestSeen;

	private Targets.Target mostDangerous;

	private float nearestSeenDistance = 999f;

	private float nearestAnyDistance = 999f;

	private Vector3 nearestAnyAt;

	private readonly float[] seen = new float[Knowledge.Features];

	private Vector3 lastEnemyAt;

	private Vector3 lastEnemyVelocity;

	private float lastEnemyTime = -100f;

	private readonly HashSet<long> visited = new HashSet<long>();

	private bool wasSeen;

	private Vector3 lookAt;

	private float lookUntil;

	public Goal goal;

	private float goalSince;

	private float goalReward;

	private float windowSince;

	private readonly float[] windowSeen = new float[Knowledge.Features];

	private float thinkAt;

	private float closeCallAt = -10f;

	private string why = "";

	private Vector3 goalPlace;

	private bool hasGoalPlace;

	private string heldBarcode;

	private float heldSince;

	private Targets.Target engaged;

	private float engagedAt = -10f;

	private Targets.Target brainTarget;

	private float coverReachedAt = -1f;

	private TextMeshPro thought;

	private static TMP_FontAsset font;

	private readonly System.Collections.Generic.Dictionary<System.IntPtr, float> sightAt = new System.Collections.Generic.Dictionary<System.IntPtr, float>();

	private float climbAfterAt;

	private float noGunUntil;

	private Vector3 huntedAt;

	private float coverWantAt;

	private float coverStay = 2.5f;

	private float coverRestUntil;

	private float gunThreatAt = -100f;

	private Bot catchWith;

	private int catchThrowsLeft;

	private float catchWaitSince;

	private float catchFlyingSince = -1f;

	private Rigidbody catchComing;

	private Grip catchComingGrip;

	private int catchesMade;

	private int catchesMissed;

	private bool catchNext;

	private bool climbing;

	private int climbPhase;

	private Vector3 climbEdge;

	private Vector3 climbIn;

	private float climbHeight;

	private Quaternion climbFacing;

	private ClimbMoves.Climb climbMove;

	private float climbT;

	private float climbReach;

	private float climbWait;

	private Pose climbStartHead;

	private Pose climbStartLeft;

	private Pose climbStartRight;

	private bool climbLifting;

	private Vector3 climbLiftFrom;

	private Vector3 climbFeetFrom;

	private Vector3 climbGlide;

	private bool climbGliding;

	private bool climbLegsUp;

	private float climbGlideSince;

	private readonly System.Collections.Generic.List<Rigidbody> climbBodies = new System.Collections.Generic.List<Rigidbody>();

	private Vector3 climbLastEdge;

	private float climbLiftSaidAt;

	private float climbUpAt = -100f;

	private int climbTries;

	private float climbSince;

	private float climbLookAt;

	private Frame climbFrom;

	private readonly bool[] climbHolding = new bool[2];

	private Pose climbHead;

	private Pose climbLeft;

	private Pose climbRight;

	private bool climbWantLeft;

	private bool climbWantRight;

	private float climbKeenAt = -10f;

	private float labClimbUntil = -10f;

	private bool climbHang;

	private Vector3 climbHangFeet;

	private readonly float[] climbSaidAt = new float[2];

	private Clone.Take cloneTake;

	private float cloneStart;

	private int cloneEvent;

	private bool cloneLoop;

	private int cloneFrame;

	private readonly System.Collections.Generic.Dictionary<int, Poolee> cloneSpawns = new System.Collections.Generic.Dictionary<int, Poolee>();

	private readonly System.Collections.Generic.List<(int id, string barcode, Vector3 at, float askedAt, HashSet<System.IntPtr> before)> clonePending = new System.Collections.Generic.List<(int, string, Vector3, float, HashSet<System.IntPtr>)>();

	private readonly System.Collections.Generic.List<(Clone.Event e, float until)> cloneGrabs = new System.Collections.Generic.List<(Clone.Event, float)>();

	private readonly Grip[] cloneHeld = new Grip[2];

	private float cloneFiredAt = -10f;

	private readonly float[] punchedAt = new float[2] { -10f, -10f };

	private bool cloneDressing;

	private float cloneDressedBy;

	private float noSpawnSaidAt = -10f;

	private string grabWhy = "";

	private readonly System.Collections.Generic.Dictionary<Clone.Event, float> grabWaitFrom = new System.Collections.Generic.Dictionary<Clone.Event, float>();

	private readonly System.Collections.Generic.List<(Hand hand, Rigidbody thing, float at, float handFastest, float thingFastest, string what)> grabWatch = new System.Collections.Generic.List<(Hand, Rigidbody, float, float, float, string)>();

	private int holdingPlayer = -1;

	private Vector3 heldOffset;

	private float playerHeldSince;

	private readonly System.Collections.Generic.List<Rigidbody> heldBodies = new System.Collections.Generic.List<Rigidbody>();

	private readonly float[] cloneGrip = new float[2];

	private CoverPhase coverPhase;

	private Vector3 coverAt;

	private Vector3 peekAt;

	private bool coverLow;

	private string coverBehind = "";

	private float coverPhaseUntil;

	private float coverLeaveAt;

	private float coverLookAt;

	private float coverRestUntil2;

	private float coverCheckAt;

	private float coverStepAt;

	private float coverInAt;

	private int coverStrikes;

	private Rigidbody coverThing;

	private readonly System.Collections.Generic.List<(Collider a, Collider b)> coverNoTouch = new System.Collections.Generic.List<(Collider, Collider)>();

	private static readonly System.Collections.Generic.Dictionary<Bot, Vector3> coverTaken = new System.Collections.Generic.Dictionary<Bot, Vector3>();

	private static readonly System.Collections.Generic.List<(Vector3 at, float t)> recentSpawns = new System.Collections.Generic.List<(Vector3, float)>();

	private bool tinkering;

	private string tinkerCode = "";

	private string tinkerName = "";

	private int trialN;

	private int trialsWanted;

	private string trialAction = "";

	private string lastTrialAction = "";

	private int trialPhase;

	private float trialAt;

	private readonly System.Collections.Generic.List<(Rigidbody rb, Vector3 at)> trialBodies = new System.Collections.Generic.List<(Rigidbody, Vector3)>();

	private int trialParts;

	private bool trialSound;

	private Vector3 trialHead;

	private int trialPortals;

	private float trialBegan;

	private bool pressA;

	private bool pressB;

	private bool pressMenu;

	private bool pressClick;

	private string tinkerWant;

	private float newsSeen;

	private string pendingCode;

	private Vector3 pendingAt;

	private float pendingUntil;

	private bool menuWas;

	private Rigidbody heldForLesson;

	private bool heldForLessonWasKinematic;

	private float heldForLessonUntil;

	private Mode pointBackTo;

	private float pointBackAt = -1f;

	private float throwFar;

	private Vector3 throwAt;

	private Rigidbody flying;

	private Vector3 flyTarget;

	private float flySpeed;

	private float flyLoft;

	private float flyFar;

	private float flySince;

	private float flyNearest;

	private Driving.Model driveModel;

	private bool driveMeant;

	private bool driving;

	private Vector3 driveGoal;

	private Transform driveAfter;

	private string driveWhy = "";

	private string driveEndWhy = "";

	private float driveWaitSince;

	private float driveTookAt;

	private float driveStuckFor;

	private float driveBackUntil;

	private float driveBackTurn;

	private float driveOverFor;

	private float driveOutAt;

	private float driveSaidAt;

	private float driveGrabAt;

	private float driveNextAt;

	private float driveFollowAt;

	private float driveYouOutFor;

	private float driveStopSince;

	private float driveFeelAt;

	private readonly float[] driveHandOff = new float[2];

	private int driveBacks;

	private Vector3 driveBackFrom;

	private bool driveStopping;

	private bool driveOut;

	private float driveGo;

	private float driveBack;

	private float driveTurn;

	private float driveTurnNow;

	private Vector3[] driveWay = new Vector3[0];

	private int driveWayNext;

	private float driveWayAt;

	private float driveWayFound = -100f;

	private NavMeshPath drivePath;

	private static readonly float[] feelAngles = new float[9] { 0f, 15f, -15f, 30f, -30f, 50f, -50f, 75f, -75f };

	private readonly float[] feelClear = new float[9];

	private float feelLength = 8f;

	private float feelBehind = 5f;

	private bool driveAbout;

	private float driveAboutAt;

	private float[] skill;

	private bool skillTrying;

	private bool skillScored;

	private float skillFar;

	private int skillBacks;

	private int skillAbouts;

	private int skillRouteFrom;

	private float driveNoRoomSaid = -100f;

	private float driveClockAt;

	private int posed;

	private string poseWhy = "";

	private bool driveLiveSaid;

	private System.Collections.Generic.List<Vector3> driveRoute;

	private System.Collections.Generic.List<float> driveRouteSpeed;

	private int driveRouteAt;

	private bool driveTrail;

	private Vector3 driveFacing;

	private static readonly System.Collections.Generic.Dictionary<System.IntPtr, System.Collections.Generic.KeyValuePair<Vector3, float>> driveSpots = new System.Collections.Generic.Dictionary<System.IntPtr, System.Collections.Generic.KeyValuePair<Vector3, float>>();

	private Vector3 driveAimLeft;

	private Vector3 driveAimRight;

	private Transform leftVehicle;

	private float leftVehicleAt = -100f;

	private float driveYawWas;

	private float driveSpin;

	private bool driveYawHas;

	private readonly System.Collections.Generic.List<Vector3> bendWay = new System.Collections.Generic.List<Vector3>();

	private float kartSeekAt;

	private Poolee kartSought;

	private Targets.Target rideTarget;

	private float rideLookAt;

	private float rideTargetSince;

	private float rideSaidAt = -10f;

	private float rideBlockedSaidAt = -10f;

	public int LabRideShots;

	private float rideStatusAt = -10f;

	private ExactClone.Take exactTake;

	private bool exactLoop;

	private bool exactReady;

	private float exactStart;

	private float exactReadyBy;

	private float exactBegunAt;

	private float exactPutBackAt = -10f;

	private int exactCursor;

	private int exactPulsedTo = -1;

	private Vector2 exactMove;

	private bool exactMoving;

	private readonly System.Collections.Generic.Dictionary<int, Poolee> exactItems = new System.Collections.Generic.Dictionary<int, Poolee>();

	private readonly HashSet<int> exactAsked = new HashSet<int>();

	private readonly HashSet<int> exactPouched = new HashSet<int>();

	private readonly System.Collections.Generic.List<(ExactClone.Item item, Pose at, HashSet<System.IntPtr> before, float askedAt)> exactPending = new System.Collections.Generic.List<(ExactClone.Item, Pose, HashSet<System.IntPtr>, float)>();

	private readonly System.Collections.Generic.List<ExactClone.Grab> exactDid = new System.Collections.Generic.List<ExactClone.Grab>();

	private int exactChecked;

	private float exBody;

	private float exBodyMost;

	private float exHands;

	private float exHandsMost;

	private float exHeldSame;

	private float exN;

	private float exAllBody;

	private float exAllBodyMost;

	private float exAllHands;

	private float exAllHandsMost;

	private float exAllHeld;

	private float exAllN;

	private int exMissed;

	private int exLate;

	private int exExtra;

	private int exOnTime;

	private float exNextReport;

	private Fid fid;

	private Fid lastFid;

	private float fidStart;

	private float fidLong;

	private float fidNext = -1f;

	private float fidLevel;

	private float fidSide = 1f;

	private Frame[] hitClip;

	private bool punchRight;

	private float punchAt = -10f;

	private float nextPunchAt;

	private float punchSaidAt = -10f;

	private int comboLeft;

	private readonly System.Collections.Generic.List<Frame[]> hitLately = new System.Collections.Generic.List<Frame[]>();

	private bool meleeWanted;

	private float meleeReach = 1.3f;

	private float meleeSeenAt;

	private float meleeSenseAt;

	private float chaseSetAt;

	private Vector3 hitRightWas;

	private Vector3 hitLeftWas;

	private Quaternion hitRightTurnWas = Quaternion.identity;

	private Quaternion hitLeftTurnWas = Quaternion.identity;

	private float hitPosedAt = -10f;

	private Vector3 hitRightBefore;

	private Vector3 hitLeftBefore;

	private float hitFast;

	private float hitSpin;

	private float hitBody;

	private Quaternion fistOffRight = Quaternion.identity;

	private Quaternion fistOffLeft = Quaternion.identity;

	private static readonly string[] meleeWords = new string[28]
	{
		"knife", "dagger", "sword", "katana", "machete", "cleaver", "axe", "hatchet", "bat", "crowbar",
		"hammer", "sledge", "pipe", "wrench", "shovel", "spear", "club", "baton", "mace", "pickaxe",
		"kunai", "blade", "pan", "bottle", "golf", "hook", "shiv", "chainsaw"
	};

	private Vector3 toolLocal = Vector3.back;

	private Quaternion toolTurnWas = Quaternion.identity;

	private bool toolTurnHas;

	private Vector3 gazeAt;

	private Transform gazeFollow;

	private string gazeOn = "";

	private float gazeUntil;

	private float gazeYaw;

	private float gazePitch;

	private float gazeYawSpeed;

	private float gazePitchSpeed;

	private float gazeFarSince = -1f;

	private Vector3 heardAt;

	private float heardTime = -100f;

	private Vector2 aimFix;

	private GunMoves.Reload learnedReload;

	private bool learnedInsertTried;

	private float aimSaidAt;

	private float lockSaidAt = -10f;

	private float learnedHoldAt = -10f;

	private float aimPitchNow;

	private float aimYawNow;

	private float lookPitchNow;

	private float lookYawNow;

	private bool lockSet;

	private Vector3 reachFix;

	private Pose gunInController;

	private bool leftOnGunSet;

	private Pose leftOnGun;

	private Pose leftHandOnController = Pose.identity;

	private System.IntPtr lockGun;

	private float lockSince;

	private float chargedAt = -10f;

	private static readonly string[] SpawnGuns = new string[10] { "c1534c5a-fcfc-4f43-8fb0-d29531393131", "c1534c5a-bcb7-4f02-a4f5-da9550333530", "c1534c5a-2a4f-481f-8542-cc9545646572", "c1534c5a-d00c-4aa8-adfd-3495534d474d", "c1534c5a-8d03-42de-93c7-f595534d4755", "c1534c5a-4c47-428d-b5a5-b05747756e56", "c1534c5a-40e5-40e0-8139-194347756e55", "c1534c5a-a6b5-4177-beb8-04d947756e41", "c1534c5a-9112-49e5-b022-9c955269666c", "c1534c5a-c061-4c5c-a5e2-3d955269666c" };

	private const float Range = 50f;

	private Gun gun;

	private Grip support;

	private bool supportHeld;

	private float supportTriedAt = -10f;

	private int supportTries;

	private float shootUntil;

	private bool spawningGun;

	private float spawnAskedAt;

	private Gun ownGun;

	private float lastSpawnAt = -100f;

	private Vector3 spawnAt;

	private readonly HashSet<System.IntPtr> gunsBefore = new HashSet<System.IntPtr>();

	private Targets.Target target;

	private float targetCheckAt;

	private float nearerCheckAt;

	private float targetSince;

	private float lostTargetAt;

	private float doneWithTargetAt = -10f;

	private int shotsAtTarget;

	private Vector2 sidestep;

	private float sidestepUntil;

	private Vector2 footMove;

	private float footUntil;

	private float footSide = 1f;

	private float dodgedAt = -10f;

	private float[] footLikes;

	private float footPace = 1f;

	private float footLong = 1f;

	private float duckUntil;

	private float duckDepth;

	private float duckNow;

	private float trigger;

	private float triggerSince;

	private bool triggerWorks;

	private float firedAt = -10f;

	private Vector3 lastAimPoint;

	private float teamSaidAt = -10f;

	private float lastTargetAt = -10f;

	private float noMagSince;

	private int burstRoundsLeft;

	private float firedByHandAt = -10f;

	private float nextPullAt;

	private float burstUntil;

	private int dryPulls;

	private float pullToJudge = -1f;

	private float judgeAt;

	private Pose rightOnController = Pose.identity;

	private float heldWrongSince = -1f;

	private Pose freeRight = Pose.identity;

	private bool freeRightSet;

	private Pose rightFix = Pose.identity;

	private Pose leftFix = Pose.identity;

	private bool handMeasured;

	private Pose gunWant;

	private bool gunWantSet;

	private Pose rightInGun;

	private bool rightInGunSet;

	private Pose leftInGun;

	private bool leftInGunSet;

	private float twoHandSince;

	private float reaction = 0.45f;

	private float tapLength = 0.08f;

	private float wantedFireSince = -1f;

	private Vector2 wobble;

	private static readonly System.Collections.Generic.Dictionary<System.IntPtr, (Bot bot, float at)> claims = new System.Collections.Generic.Dictionary<System.IntPtr, (Bot, float)>();

	private float unsafeSince = -1f;

	private float assistUntil = -10f;

	private float assistBegan = -10f;

	private float assistSawAt = -10f;

	private bool directAsked;

	private float forcedAt;

	private int forcedDry;

	private float forcedSeen;

	private static readonly HashSet<System.IntPtr> magless = new HashSet<System.IntPtr>();

	private float loadSettleUntil;

	private int loadFails;

	private static readonly System.Collections.Generic.Dictionary<System.IntPtr, float> wontLoad = new System.Collections.Generic.Dictionary<System.IntPtr, float>();

	private int unsticks;

	private float unstuckAt = -100f;

	private float duckAskedAt = -10f;

	private bool crouchPlan;

	private float crouchPlanUntil;

	private float crouchLookAt;

	private float crouchPlannedAt = -10f;

	private float crouchPlannedSince = -10f;

	private static MethodInfo zombieHit;

	private static bool zombieLooked;

	private static float zombieDamage = 25f;

	private float zombieSaidAt = -10f;

	private float[] shootSkill;

	private bool shootTrying;

	private int skillShots;

	private int skillHits;

	private float skillShooting;

	private float skillLastShot = -10f;

	private Vector2 aimSpot;

	private string aimPart = "chest";

	private float shotRange = 8f;

	private Pose aimedWant;

	private float aimLockSaidAt = -10f;

	private Quaternion aimLockFix = Quaternion.identity;

	private System.IntPtr aimLockGun;

	private bool aimedWantSet;

	private float aimedSince = -10f;

	private Pose freeLeft = Pose.identity;

	private bool freeLeftSet;

	private Step step = Step.Ready;

	private float stepAt;

	private Pose leftStart;

	private float frontClosing;

	private float frontClosingAt = -10f;

	private float frontArrivedAt = -1f;

	private float frontLetUntil = -10f;

	private float frontGap;

	private float frontOff;

	private float frontJolt;

	private bool frontForced;

	private bool frontJoltSaid = true;

	private float frontBefore;

	private float frontLeftOff;

	private float frontRightOff;

	private static int frontHolds;

	private bool frontKept;

	private bool frontCounted;

	private float steadyFrom = -1f;

	private float steadyMost;

	private float steadySum;

	private float settleCalmSince = -1f;

	private float frontRetryAt;

	private Grip frontRetry;

	private int steadyN;

	private Quaternion meterRel;

	private Quaternion meterCtl;

	private bool meterSet;

	private float meterFrom = -1f;

	private float meterRelSum;

	private float meterRelMost;

	private float meterCtlSum;

	private float meterShotSum;

	private int meterN;

	private int meterShotN;

	private Pose leftOnHeld;

	private bool leftOnHeldSet;

	private static readonly HashSet<string> boltGrips = new HashSet<string>();

	private float boltOffSince = -1f;

	private const float SeekRadius = 10f;

	private const float MaxMass = 15f;

	private Play play;

	private Then then;

	private Rigidbody item;

	private Grip grip;

	private float playAt;

	private float holdFor;

	private Vector3 throwDir;

	private bool released;

	private bool fists;

	private bool tossNext;

	private bool forceToss;

	private bool handOverride;

	private Vector3 handAt;

	private Quaternion handTurn;

	private float playHeight;

	private float playPitch;

	private float playYaw;

	private bool leftOverride;

	private Vector3 leftAt;

	private Quaternion leftTurn;

	private Frame[] pickClip;

	private System.IntPtr pickFor;

	private bool pickMirror;

	private float pickTakeAt;

	private float pickLow;

	private float pickChosenAt = -10f;

	private float hp = -1f;

	public static float LastShotAt = -10f;

	private float unCountedSaidAt = -10f;

	public bool dead;

	private float diedAt;

	private float bluntAt = -10f;

	private Rigidbody stowed;

	private Gun stowedGun;

	private Grip stowedGrip;

	private Transform stowOn;

	private Vector3 stowAt;

	private Quaternion stowTurn;

	private bool stowedKinematic;

	private readonly System.Collections.Generic.List<Collider> stowedOff = new System.Collections.Generic.List<Collider>();

	private float stowedSince;

	private float drawnAt = -10f;

	private MarrowEntity[] ownEntities;

	private float awakeAt;

	private int wakings;

	private float fastSaidAt = -10f;

	private readonly HashSet<System.IntPtr> noPushDone = new HashSet<System.IntPtr>();

	private float risingFor;

	private float liftedFor;

	private float liftedAt = -100f;

	private float noPushAgainAt;

	private System.IntPtr fleshOf;

	private float hurtAt = -100f;

	private float voiceAt = -10f;

	private static SurfaceData bloodData;

	private static bool bloodAsked;

	private float labAimError = -1f;

	private Vector3 labGunUp;

	private Vector3 labWantUp;

	private Lessons.Lesson cloneLesson;

	private Mode lessonReturn;

	private float cloneYaw;

	private Vector3 cloneFrom;

	private Vector3 cloneTo;

	private float cloneSize = 1f;

	private bool lessonRootSet;

	private float lessonRootYaw;

	private Vector3 lessonPelvisWas;

	private bool lessonPelvisHas;

	private int lessonJumpAt = -1;

	private readonly System.Collections.Generic.List<float> lessonJumps = new System.Collections.Generic.List<float>();

	private int lessonTurnsSeen;

	private int cloneDriven = -1;

	private float lessonSenseAt;

	private string lastLessonItem;

	private readonly System.Collections.Generic.Dictionary<Clone.Event, int> cloneStandIns = new System.Collections.Generic.Dictionary<Clone.Event, int>();

	private Lessons.Lesson lessonNext;

	private bool lessonAtSpot;

	private int lessonLoops;

	private float lessonSeamAt = -10f;

	private Pose seamHead;

	private Pose seamLeft;

	private Pose seamRight;

	private Pose lastHeadPose;

	private Pose lastLeftPose;

	private Pose lastRightPose;

	private bool lessonPurposeful;

	private bool lessonGliding;

	private float wallTopY = float.MinValue;

	private float lessonTopY = float.MinValue;

	private readonly System.Collections.Generic.Dictionary<string, Poolee> lessonThings = new System.Collections.Generic.Dictionary<string, Poolee>();

	private float regrabAt;

	private Vector3 lessonWallFoot;

	private Vector3 lessonWallIn;

	private Vector3 lessonLandAt;

	private bool lessonLanding;

	private float lessonLandSince;

	private int lessonLoopsDone;

	private readonly System.Collections.Generic.Dictionary<string, int> lessonStandIns = new System.Collections.Generic.Dictionary<string, int>();

	private readonly HashSet<int> lessonBig = new HashSet<int>();

	private readonly HashSet<int> lessonBigAsked = new HashSet<int>();

	private Vector3 placeAhead;

	private Vector3 placeAt;

	private bool placeAtSet;

	private Clone.Event seatWaiting;

	private float seatWaitUntil;

	private float seatSitAt;

	private float seatAskedAt;

	private int seatMoves;

	private int seatRetries;

	private Seat seatAsked;

	private Clone.Event seatAskedOf;

	private readonly bool[] wallHeld = new bool[2];

	private readonly bool[] wallPinned = new bool[2];

	private readonly Vector3[] wallPin = new Vector3[2];

	private readonly System.Collections.Generic.List<(Rigidbody body, Hand hand, Grip grip, Pose inHand, bool wasKinematic)> carried = new System.Collections.Generic.List<(Rigidbody, Hand, Grip, Pose, bool)>();

	private float glidedAt;

	private float lessonDueAt = -1f;

	private Act act;

	private Act lastAct;

	private float actUntil;

	public int netId;

	private int netOwner = -1;

	private readonly System.Collections.Generic.List<Clone.Frame> netFrames = new System.Collections.Generic.List<Clone.Frame>();

	private float netOffset;

	private bool netOffsetSet;

	private bool netDown;

	private int netAt;

	private readonly ushort[] netHolds = new ushort[2] { 65535, 65535 };

	private float netOwnAt;

	private string netWearing;

	private float netWoreAt = -100f;

	private float netStruckAt = -10f;

	private Targets.Target ordered;

	private float orderedUntil = -10f;

	private int shoveStage;

	private float shoveStageAt;

	private bool shoved;

	private float offerSince = -1f;

	private bool bringNext;

	private bool portalGoing;

	private PortalWays.Way portalWay;

	private float portalSince;

	private int portalPhase;

	private float portalPhaseAt;

	private float portalNotBefore;

	private float portalTookSeen;

	private Vector3 portalHeadWas;

	private bool portalHeadHas;

	private float portalThroughAt = -100f;

	private float droveYaw;

	private bool droveHas;

	private Vector3 droveHead;

	private bool droveHeadHas;

	private float droveJumpAt = -10f;

	private float droveTurn;

	private float droveTurnAt = -10f;

	private bool labPortal;

	private static readonly System.Collections.Generic.Dictionary<Frame[], System.Collections.Generic.List<Strike>> strikesOf = new System.Collections.Generic.Dictionary<Frame[], System.Collections.Generic.List<Strike>>();

	private float strikePace;

	private Strike lastStrike;

	private float defendUntil;

	private float defendSeenAt = -10f;

	private bool defendBlock;

	public int LabBlocks;

	public int LabSteps;

	public int LabTaken;

	private float purposeAt;

	private (Vector3 foot, Vector3 inward, float height) planWall;

	private bool raceLineUp;

	private bool raceReady;

	private Vector3 raceSpot;

	private float flinchAt = -10f;

	private float flinchSize;

	private float flinchSide;

	private float nerves;

	private float nervesAt;

	private Vector3 fallLast;

	private bool fallHas;

	private float fallSpeed;

	private bool falling;

	private float fallTop;

	private float fallSince;

	private float landAt = -10f;

	private float landSize;

	private float noLandUntil;

	private float waveSeen;

	private float waveDue = -1f;

	private int waveTimes;

	private bool waveLeft;

	private float lastNearYou;

	private bool nearYou;

	private Reload reload;

	private float reloadAt;

	private Magazine newMag;

	private bool magAsked;

	private float magAskedAt;

	private Vector3 magAt;

	private readonly HashSet<System.IntPtr> magsBefore = new HashSet<System.IntPtr>();

	private static readonly System.Collections.Generic.List<(Magazine mag, float at)> dropped = new System.Collections.Generic.List<(Magazine, float)>();

	private const float DroppedFor = 20f;

	private bool ejected;

	private Grip slide;

	private bool slideHeld;

	private Vector3 slideFrom;

	private Pose leftOnController = Pose.identity;

	private bool leftMeasured;

	private float reloadedAt = -10f;

	private bool downedForRescue;

	private Bot rescuing;

	private Bot rescuedBy;

	private float rescueSince;

	private float kneelSince = -1f;

	private float youOverSince = -1f;

	private Seat riding;

	private Transform ridingCar;

	private Seat boardingSeat;

	private Transform boardingCar;

	private float boardingSince;

	private float boardLookAt;

	private float noDriverSince = -1f;

	private Transform lessonCar;

	public bool labHost;

	private float cameOverAt = -1000f;

	private bool driveOnRoad;

	private string planWhy = "";

	private Sight planSight;

	private Vector3 planPlace;

	private Vector3 planLook;

	private float planPush = 0.6f;

	private bool planRun;

	private float thinkUntil;

	private bool thinking2;

	private int detours;

	private Vector3 detourFor;

	private bool onDetour;

	private Vector3 viaPlace;

	private bool viaLeft;

	private Vector3 roamHeading;

	private float pauseUntil;

	private float pauseNext;

	private float planSince;

	private Sight lastSight = Sight.New;

	private bool sitting;

	private float sitSince;

	private int sitLifeFrame = -10;

	private bool labSitting;

	private string avatarCode = "";

	private Frame[] waveClip;

	private Frame[] jumpClip;

	private Frame[] crouchClip;

	private float crouchStart;

	private bool crouchReset;

	private Frame[] previewClip;

	private bool previewing;

	private Mode previewBack;

	private Frame smoothOut;

	private bool smoothHas;

	private Eased easeLeft;

	private Eased easeRight;

	private Eased easeHead;

	private float easeHeightFrom;

	private float easeHeightAt = -10f;

	private float smoothSkipUntil;

	private int smoothDoing;

	private bool holdAimedNow;

	private int smoothFrame;

	private float peopleAt;

	private float rushedAt = -100f;

	private float runnerAt = -100f;

	private float braceFrom = -100f;

	private float braceUntil = -100f;

	private Vector3 rushStep;

	private float rushStepUntil;

	private float runnerLookAt = -1f;

	private Vector3 runnerPoint;

	private Vector3 runnerFrom;

	private bool runnerAlarm;

	private float squatUntil;

	private float squatDepth;

	private float squatNow;

	private Bot friend;

	private float meetFrom;

	private float meetUntil;

	private float metAt = -100f;

	private bool meetLead;

	private int meetStage;

	private float meetNearAt;

	private float meetLookAt;

	private float meetWaveDue = -1f;

	private float meetTryAt;

	private bool meetTalking;

	private float talkK;

	public bool surrendered;

	private bool surrenderRolled;

	private float surrenderedAt;

	private float spareNearSince = -1f;

	public static float FuriousUntil = -10f;

	private static float teamsAt = -10f;

	private static int ours;

	private static int theirs;

	private bool down;

	private float downAt;

	private float downFor;

	private bool rising;

	private float riseAt;

	private float upAt = -10f;

	private float bumpSaidAt = -10f;

	private bool limp;

	private float wakeAt = -1f;

	private Rigidbody[] bodyParts;

	private bool gunIsLong;

	private System.IntPtr lockedJoint;

	private string recKey;

	private System.Collections.Generic.List<HoldLibrary.Frame> recFrames;

	private readonly System.Collections.Generic.List<int> recAimed = new System.Collections.Generic.List<int>();

	private readonly System.Collections.Generic.List<int> recCarried = new System.Collections.Generic.List<int>();

	private float recCursor;

	private bool recWasAimed;

	private float standingHeight
	{
		get
		{
			float num = StandingEyes();
			if (!(num > 0.3f))
			{
				return metHeight;
			}
			return num * 0.985f;
		}
	}

	public bool Alive
	{
		get
		{
			if (rig != null && controls != null && controls.vrRoot != null)
			{
				return !broken;
			}
			return false;
		}
	}

	public bool Whole
	{
		get
		{
			if (rig != null && rig.physicsRig != null)
			{
				return rig.physicsRig.gameObject.activeInHierarchy;
			}
			return false;
		}
	}

	public Transform Head
	{
		get
		{
			if (!(rig.physicsRig != null))
			{
				return null;
			}
			return rig.physicsRig.m_head;
		}
	}

	private Vector3 Feet
	{
		get
		{
			if (!(Head != null))
			{
				return rig.transform.position;
			}
			return Head.position - Vector3.up * pose.height;
		}
	}

	public Vector2 Walk
	{
		get
		{
			if (!(stick.sqrMagnitude < 0.0001f))
			{
				return gameStick;
			}
			return Vector2.zero;
		}
	}

	public string Name => nameIs ?? (nameIs = (enemy ? "E" : "B") + ++named);

	public static float YouStoodFor => Time.time - youStoodSince;

	private bool Armed
	{
		get
		{
			if (gun != null)
			{
				return Holding();
			}
			return false;
		}
	}

	private static float ReloadBelow => Mathf.Clamp(Knowledge.Style.Median(Knowledge.Style.reloadAt, 0.25f), 0.05f, 0.6f);

	private Transform Hunted
	{
		get
		{
			if (!(Head != null))
			{
				return Player.Head;
			}
			return Targets.NearestPlayerHead(Head.position);
		}
	}

	private bool ClimbKeen
	{
		get
		{
			if (!(Time.time - climbKeenAt < 0.6f) && (!enemy || goal != Goal.Investigate))
			{
				return Time.time < labClimbUntil;
			}
			return true;
		}
	}

	public bool PlayingClone
	{
		get
		{
			if (mode == Mode.Clone)
			{
				return cloneTake != null;
			}
			return false;
		}
	}

	public bool InCover
	{
		get
		{
			if (coverPhase != CoverPhase.Hidden)
			{
				return coverPhase == CoverPhase.Peeking;
			}
			return true;
		}
	}

	private float Size => Mathf.Clamp(standingHeight / 1.6f, 0.5f, 2f);

	private float CrouchedEyes => standingHeight - Tuning.F("fight.crouch.drop", 0.55f) * Size;

	private float StandingEyesAt => standingHeight;

	public bool Driving2 => driving;

	public bool DrivingNow
	{
		get
		{
			if (InLesson)
			{
				return driveMeant;
			}
			return false;
		}
	}

	public bool DrivingLesson
	{
		get
		{
			if (InLesson && cloneLesson != null)
			{
				return cloneLesson.purpose == "drive";
			}
			return false;
		}
	}

	public bool Exact => mode == Mode.Exact;

	private float ExactTime
	{
		get
		{
			if (!exactReady)
			{
				return 0f;
			}
			return Time.time - exactStart;
		}
	}

	private bool MeleeArmed
	{
		get
		{
			if (play == Play.Melee)
			{
				return Holding();
			}
			return false;
		}
	}

	private Quaternion ToolFix => Quaternion.FromToRotation(toolLocal, Vector3.back);

	private string GunKey
	{
		get
		{
			if (!(gun != null))
			{
				return "";
			}
			return GunMoves.KeyOf(gun, LongGun);
		}
	}

	public Gun OwnGun => ownGun;

	private bool Assisting => Time.time < assistUntil;

	private float[] Skill
	{
		get
		{
			if (shootSkill == null)
			{
				shootSkill = ShootSkill.Begin(rig.Pointer, out shootTrying);
			}
			return shootSkill;
		}
	}

	private bool AimsAtABody
	{
		get
		{
			if (!target.player && target.bot == null)
			{
				return target.brain != null;
			}
			return true;
		}
	}

	public bool FrontLetGrab => Time.time < frontLetUntil;

	private bool LongGun
	{
		get
		{
			if (!gunIsLong && !(support != null))
			{
				return frontRetry != null;
			}
			return true;
		}
	}

	public bool Playing => play != Play.None;

	private Hand RightHand
	{
		get
		{
			if (!(rig.physicsRig != null))
			{
				return null;
			}
			return rig.physicsRig.rightHand;
		}
	}

	private float MaxHp => ((rig.health != null && rig.health.max_Health > 0f) ? rig.health.max_Health : 10f) * Tuning.F("bot.hp", 1.44f);

	public bool HasStowed
	{
		get
		{
			if (stowed != null)
			{
				return stowed.gameObject.activeInHierarchy;
			}
			return false;
		}
	}

	private bool WeaponInHand
	{
		get
		{
			if (Holding())
			{
				if (!(gun != null) || play != Play.Shoot)
				{
					return play == Play.Melee;
				}
				return true;
			}
			return false;
		}
	}

	public int LabShots { get; private set; }

	public int LabReloads { get; private set; }

	public Vector3 Facing => Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward;

	public Transform GunHandTransform
	{
		get
		{
			if (!(RightHand != null))
			{
				return Head;
			}
			return RightHand.transform;
		}
	}

	public Transform FrontHandTransform
	{
		get
		{
			if (!(LeftHand != null))
			{
				return null;
			}
			return LeftHand.transform;
		}
	}

	public bool InLesson
	{
		get
		{
			if (mode == Mode.Clone)
			{
				return cloneLesson != null;
			}
			return false;
		}
	}

	public Mode KeptMode
	{
		get
		{
			if (!InLesson)
			{
				return mode;
			}
			return lessonReturn;
		}
	}

	public bool Puppet => netOwner >= 0;

	public Vector3 NetFeet
	{
		get
		{
			if (!(rig.physicsRig != null) || !(rig.physicsRig.centerOfPressure != null))
			{
				return Feet;
			}
			return rig.physicsRig.centerOfPressure.position;
		}
	}

	public float NetYaw => bodyYaw;

	public Transform Chest
	{
		get
		{
			try
			{
				return (rig.physicsRig != null) ? rig.physicsRig.m_chest : null;
			}
			catch (System.Exception)
			{
				return null;
			}
		}
	}

	private float FistReach => Tuning.F("fist.reach", 0.8f);

	public bool RaceReady => raceReady;

	public Vector3? BodyAt
	{
		get
		{
			Rigidbody rigidbody = Pelvis();
			if (!(rigidbody != null))
			{
				return null;
			}
			return rigidbody.position;
		}
	}

	private bool HandsFree
	{
		get
		{
			if (!handOverride && !Playing && !climbing)
			{
				return !fists;
			}
			return false;
		}
	}

	public bool Reloading => reload != Reload.None;

	private Hand LeftHand
	{
		get
		{
			if (!(rig.physicsRig != null))
			{
				return null;
			}
			return rig.physicsRig.leftHand;
		}
	}

	private float RescueWait => Tuning.F("rescue.wait", 40f);

	public bool Riding => riding != null;

	public bool Sitting => sitting;

	private bool CalmNow
	{
		get
		{
			if (nearestSeen.transform == null && brainTarget.transform == null && !Reloading && !climbing && !InLesson && !Playing)
			{
				return !Down;
			}
			return false;
		}
	}

	private bool CanMeet
	{
		get
		{
			if (!dead && friend == null && CalmNow && Time.time - metAt > Tuning.F("meet.every", 40f) && Time.time - born > 8f && Time.time >= waveUntil && waveDue < 0f && Time.time > rushStepUntil)
			{
				if (mode != 0 && mode != Mode.Follow && mode != Mode.Wander)
				{
					return mode == Mode.Brain;
				}
				return true;
			}
			return false;
		}
	}

	public static bool Furious => Time.time < FuriousUntil;

	public bool Down
	{
		get
		{
			if (!down)
			{
				return rising;
			}
			return true;
		}
	}

	public Bot(RigManager rig, float yaw)
	{
		this.rig = rig;
		enemy = rig.transform.Find("BotEnemyMark") != null;
		personality = Personality.Of(rig);
		controls = rig.ControllerRig.TryCast<OpenControllerRig>();
		bodyYaw = yaw;
		born = Time.time;
		if (Recorder.Capture(out var frame))
		{
			metHeight = Mathf.Clamp(frame.height, 1.2f, 2.1f);
		}
		MotionLibrary.Refresh();
		pose = Standing(0f, 0f, 0f, 0f);
	}

	private void StayWhole()
	{
		if (Whole)
		{
			partsOffSince = -1f;
			return;
		}
		if (partsOffSince < 0f)
		{
			partsOffSince = Time.time;
		}
		if (Time.time - partsOffSince < 1f)
		{
			return;
		}
		partsOffSince = -1f;
		if (Time.time - mendedAt > 30f)
		{
			mended = 0;
		}
		mendedAt = Time.time;
		if (++mended > 3)
		{
			Lab.Event("whole: its body will not stay switched on: done away with");
			broken = true;
			return;
		}
		int num = 0;
		try
		{
			foreach (Rig componentsInChild in rig.GetComponentsInChildren<Rig>(includeInactive: true))
			{
				Transform transform = componentsInChild.transform;
				while (transform != null && transform != rig.transform)
				{
					if (!transform.gameObject.activeSelf)
					{
						transform.gameObject.SetActive(value: true);
						num++;
					}
					transform = transform.parent;
				}
				if (!componentsInChild.enabled)
				{
					componentsInChild.enabled = true;
				}
			}
		}
		catch (System.Exception ex)
		{
			Lab.Event("whole: could not switch its body on again: " + ex.Message);
		}
		Lab.Event("whole: its body had been switched off by the game: " + num + " part(s) switched on again (" + mended + ")");
	}

	private Vector2 ForItsHead(Vector2 forBody)
	{
		try
		{
			Transform head = Head;
			if (head == null || forBody.sqrMagnitude < 0.0001f || Tuning.F("move.byhead", 1f) < 0.5f)
			{
				return forBody;
			}
			Vector3 forward = Flat(head.forward);
			if (forward.sqrMagnitude < 0.04f)
			{
				return forBody;
			}
			float f = Mathf.DeltaAngle(Recorder.Yaw(forward), bodyYaw) * ((float)System.Math.PI / 180f);
			float num = Mathf.Cos(f);
			float num2 = Mathf.Sin(f);
			return new Vector2(forBody.x * num + forBody.y * num2, (0f - forBody.x) * num2 + forBody.y * num);
		}
		catch (System.Exception)
		{
			return forBody;
		}
	}

	public void SetMode(Mode next)
	{
		if (WeaponInHand && next != Mode.Clone && next != Mode.Exact && next != Mode.Copy && next != Mode.Replay)
		{
			Stow();
		}
		StopPlaying();
		if (mode == Mode.Clone && next != Mode.Clone)
		{
			LeaveClone();
		}
		if (mode == Mode.Exact && next != Mode.Exact)
		{
			LeaveExact();
		}
		MotionLibrary.Refresh();
		if (mode == Mode.Brain && next != Mode.Brain)
		{
			SwitchGoal(Goal.Idle);
		}
		HideThought();
		mode = next;
		walk = Vector2.zero;
		primaryClick = (secondaryClick = (running = false));
		jump = (otherA = false);
		crouching = false;
		jumpDue = -1f;
		nav.Clear();
		following = false;
		idleUntil = 0f;
		if (next == Mode.Copy)
		{
			jumpAsked = Recorder.PlayerJumpedAt;
		}
		if (next == Mode.Replay && !StartReplay())
		{
			mode = Mode.Stand;
		}
	}

	private bool StartReplay()
	{
		if (previewClip != null)
		{
			replay = new System.Collections.Generic.List<Frame>(previewClip);
			previewClip = null;
			replayAt = 0;
			lastJumpFrame = -1;
			replayStart = Time.time;
			replayYaw = bodyYaw - replay[0].yaw;
			return true;
		}
		string text = Recorder.Latest();
		if (text == null)
		{
			Spawnables.Notify("Nothing recorded yet (turn on Record my play)");
			return false;
		}
		try
		{
			replay = Recorder.Load(text);
		}
		catch (System.Exception ex)
		{
			Spawnables.Notify("That recording could not be read: " + ex.Message);
			return false;
		}
		if (replay.Count < 2)
		{
			Spawnables.Notify("That recording is too short");
			return false;
		}
		replayAt = 0;
		lastJumpFrame = -1;
		replayStart = Time.time;
		replayYaw = bodyYaw - replay[0].yaw;
		Spawnables.Notify("Replaying " + replay[replay.Count - 1].time.ToString("0") + " seconds of you");
		return true;
	}

	public void Update(float dt)
	{
		if (Dead())
		{
			return;
		}
		if (Puppet)
		{
			NetUpdate();
			return;
		}
		StayWhole();
		if (!Whole)
		{
			return;
		}
		KeepAwake();
		if (mode == Mode.Exact)
		{
			ExactUpdate();
			return;
		}
		StowFollow();
		try
		{
			if (riding != null && rig.activeSeat != riding)
			{
				riding = null;
			}
			if (mode != Mode.Clone && mode != Mode.Exact && rig.activeSeat != null && riding == null && Time.time - seatOutAt > 1.5f)
			{
				seatOutAt = Time.time;
				rig.activeSeat.EgressRig(immediate: false);
			}
		}
		catch (System.Exception)
		{
		}
		OnItsFeet();
		TheirWayStep();
		Flesh();
		Mend(dt);
		if (DownStep(dt))
		{
			return;
		}
		HurtPlayer();
		if (!heard && Time.time - born > 2f)
		{
			heard = true;
			BotRig.HearLate(rig);
		}
		Transform head = Head;
		Transform head2 = Player.Head;
		float t = Time.time - born;
		if (spawningGun && Time.time - spawnAskedAt > 0.2f && !TakeSpawnedGun() && Time.time - spawnAskedAt > 4f)
		{
			spawningGun = false;
			tossNext = false;
		}
		jump = (otherA = false);
		pressA = (pressB = (pressMenu = (pressClick = false)));
		primaryClick = (secondaryClick = false);
		crouching = false;
		NoteJumping();
		TrackThrow();
		MeasureFreeHands();
		if (SurrenderStep(head2, head, dt) || RideStep(head, dt) || RaceStep(head, dt) || RescueStep(head, dt))
		{
			return;
		}
		bool flag = mode == Mode.Stand || mode == Mode.Follow || mode == Mode.Wander || mode == Mode.Gunplay || mode == Mode.Brain || mode == Mode.Fists;
		if (flag)
		{
			Notice(head2, head);
			NoticePeople(head, dt);
			Aware(head2, head);
			HearNews();
		}
		bool flag2 = climbing && climbHang;
		if (flag2)
		{
			ClimbStep(dt);
		}
		switch (flag2 ? ((Mode)(-1)) : mode)
		{
		case Mode.Stand:
			Watch(head2, head, dt, (Time.time < waveUntil) ? 200f : 60f);
			walk = Vector2.zero;
			if (Playing)
			{
				PlayStep(head2, head, dt);
			}
			break;
		case Mode.Follow:
			Follow(head2, head, dt);
			break;
		case Mode.Wander:
			Wander(head2, head, dt);
			break;
		case Mode.Gunplay:
			Gunplay(head2, head, dt);
			break;
		case Mode.Clone:
			CloneUpdate(dt);
			break;
		case Mode.Fists:
			FistsOnly(head2, head, dt);
			break;
		case Mode.Brain:
			BrainStep(head2, head, dt);
			break;
		case Mode.Copy:
		{
			Watch(head2, head, dt, 150f);
			if (Recorder.Capture(out var frame))
			{
				pose = frame;
				Body(frame);
			}
			if (Recorder.PlayerJumpedAt > jumpAsked)
			{
				jumpAsked = Recorder.PlayerJumpedAt;
				AskJump();
			}
			walk = Vector2.zero;
			curl = -1f;
			break;
		}
		case Mode.Replay:
			Replay();
			break;
		}
		if (!flag2 && (mode == Mode.Follow || mode == Mode.Wander))
		{
			AsideForYou(dt);
		}
		if (!flag2 && flag)
		{
			SocialStep(dt);
		}
		if (!flag)
		{
			return;
		}
		curl = 0.25f;
		float magnitude = walk.magnitude;
		stride += dt * (2f + 5f * magnitude);
		bool flag3 = magnitude > 0.05f;
		primaryClick = SprintClick();
		Frame frame2 = Standing(t, flag3 ? 8f : LookPitch(head2, head), flag3 ? 0f : headYaw, flag3 ? magnitude : 0f);
		MotionLibrary.Gait want = (flag3 ? ((!running) ? MotionLibrary.Gait.Walk : MotionLibrary.Gait.Run) : MotionLibrary.Gait.Idle);
		if (Moves(want, dt, out var f))
		{
			if (!flag3)
			{
				f.head = Quaternion.Euler(LookPitch(head2, head), headYaw, 0f) * Quaternion.Slerp(Quaternion.identity, f.head, 0.35f);
			}
			frame2 = f;
		}
		pose = frame2;
		CrouchOfTheirs();
		JumpOfTheirs();
		OverHands();
		Wave(t);
		Fidget(head2, flag3, dt);
		Gaze(head2, flag3, dt);
		React(dt);
		SocialPose(dt);
		ClimbPose();
		Smooth(dt);
		if (!MaybePurpose(head))
		{
			MaybeLesson();
		}
	}

	private void RunTrace(string where)
	{
		if (!(Time.time > runTraceUntil) && !(Time.time - runTraceAt < 0.5f))
		{
			runTraceAt = Time.time;
			RemapRig remapHeptaRig = rig.remapHeptaRig;
			Rigidbody rigidbody = Pelvis();
			Lab.Event("run trace (" + where + "): running " + running + ", walk " + walk.magnitude.ToString("0.00") + ", stick " + stick.magnitude.ToString("0.00") + " (" + stick.x.ToString("0.0") + ", " + stick.y.ToString("0.0") + "), going " + ((rigidbody != null) ? Flat(rigidbody.velocity).magnitude.ToString("0.0") : "?") + " m/s" + ((remapHeptaRig != null) ? (", legs " + remapHeptaRig.travState.ToString() + ", top " + remapHeptaRig.maxVelocity.ToString("0.0") + ", its own " + remapHeptaRig._currentVelocity.magnitude.ToString("0.0")) : "") + ", game sprints " + gameSprints + ", clicked " + (Time.time - sprintClickAt).ToString("0.0") + " s ago");
		}
	}

	private bool SprintClick()
	{
		RemapRig remapHeptaRig = rig.remapHeptaRig;
		if (remapHeptaRig != null)
		{
			if (walkMost <= 0f)
			{
				for (walkMost = remapHeptaRig.maxVelocity; walkMost > Tuning.F("walk.most", 2.6f); walkMost /= Tuning.F("sprint.times", 2.1f))
				{
				}
			}
			float num = walkMost * Tuning.F("sprint.times", 2.1f);
			if (!running)
			{
				gameSprints = false;
			}
			else if (remapHeptaRig._currentVelocity.magnitude > num * 1.12f)
			{
				gameSprints = true;
			}
			float num2 = ((running && !gameSprints) ? num : walkMost);
			remapHeptaRig.maxVelocity = Mathf.MoveTowards(remapHeptaRig.maxVelocity, num2, Time.deltaTime * (gameSprints ? 30f : 6f));
		}
		try
		{
			Rigidbody rigidbody = Pelvis();
			if (remapHeptaRig != null && remapHeptaRig.travState == RemapRig.TraversalState.Run && (!running || rigidbody == null || Flat(rigidbody.velocity).magnitude < Tuning.F("sprint.gait", 2.6f) - 0.6f))
			{
				remapHeptaRig.travState = RemapRig.TraversalState.Walk;
			}
		}
		catch (System.Exception)
		{
		}
		if (!running || stick.magnitude < 0.85f || stick.y < 0.6f)
		{
			RunTrace("no click");
			return false;
		}
		RunTrace("clicking");
		Rigidbody rigidbody2 = Pelvis();
		float num3 = ((rigidbody2 != null) ? Flat(rigidbody2.velocity).magnitude : 0f);
		if (num3 < Tuning.F("sprint.speed", 2.9f) && Time.time - sprintClickAt > Tuning.F("sprint.again", 0.7f))
		{
			sprintClickAt = Time.time;
		}
		if (Time.time - sprintSaidAt > 3f)
		{
			sprintSaidAt = Time.time;
			RemapRig remapHeptaRig2 = rig.remapHeptaRig;
			Lab.Event("run: going " + num3.ToString("0.0") + " m/s, stick " + stick.magnitude.ToString("0.00") + " (" + stick.x.ToString("0.0") + ", " + stick.y.ToString("0.0") + ")" + ((remapHeptaRig2 != null) ? (", " + remapHeptaRig2.travState.ToString() + ", most " + remapHeptaRig2.currentMaxVelocity.ToString("0.0") + " of " + remapHeptaRig2.maxVelocity.ToString("0.0") + ", its own " + remapHeptaRig2._currentVelocity.magnitude.ToString("0.0")) : ""));
		}
		try
		{
			if ((num3 > Tuning.F("sprint.gait", 2.6f) && remapHeptaRig != null && remapHeptaRig.travState == RemapRig.TraversalState.Walk) || (remapHeptaRig != null && remapHeptaRig.travState == RemapRig.TraversalState.Jog && num3 > Tuning.F("sprint.gait", 2.6f)))
			{
				remapHeptaRig.travState = RemapRig.TraversalState.Run;
			}
		}
		catch (System.Exception)
		{
		}
		return Time.time - sprintClickAt < 0.15f;
	}

	public static string LegsOf(RigManager who)
	{
		try
		{
			RemapRig remapRig = ((who != null) ? who.remapHeptaRig : null);
			if (remapRig == null)
			{
				return "none";
			}
			OpenControllerRig openControllerRig = who.ControllerRig.TryCast<OpenControllerRig>();
			return remapRig.travState.ToString() + ", top speed " + remapRig.maxVelocity.ToString("0.00") + " (now " + remapRig.currentMaxVelocity.ToString("0.00") + "), accel " + remapRig.maxAcceleration.ToString("0.0") + ", jump " + remapRig.jumpVelocity.ToString("0.00") + ", jumping " + remapRig._jumping + ", crouch " + remapRig._crouchTarget.ToString("0.00") + ", feet " + remapRig._feetOffset.ToString("0.00") + ", in weight " + remapRig.inWeight.ToString("0.00") + ", enabled " + remapRig.enabled + ", ball loco " + (who.physicsRig != null && who.physicsRig.ballLocoEnabled) + ((openControllerRig != null) ? (", A " + openControllerRig._primaryAButton + "/" + openControllerRig._secondaryAButton + ", stick click " + openControllerRig._primaryStick + "/" + openControllerRig._secondaryStick) : "") + BodiesOf(who);
		}
		catch (System.Exception ex)
		{
			return "failed: " + ex.Message;
		}
	}

	private static string BodiesOf(RigManager who)
	{
		try
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			string text = "";
			foreach (Rigidbody componentsInChild in who.physicsRig.GetComponentsInChildren<Rigidbody>(includeInactive: true))
			{
				num++;
				bool isKinematic = componentsInChild.isKinematic;
				bool flag = !componentsInChild.gameObject.activeInHierarchy;
				if (isKinematic)
				{
					num2++;
				}
				if (flag)
				{
					num4++;
				}
				if (!isKinematic && !flag && componentsInChild.IsSleeping())
				{
					num3++;
				}
				if ((isKinematic || flag) && text.Length < 160)
				{
					text = text + " " + componentsInChild.name + (flag ? "(off)" : "");
				}
				foreach (Joint component in componentsInChild.GetComponents<Joint>())
				{
					if (component.connectedBody == null || component.connectedBody.GetComponentInParent<RigManager>() != who)
					{
						num5++;
						if (text.Length < 220)
						{
							text = text + " " + componentsInChild.name + "->" + ((component.connectedBody != null) ? component.connectedBody.name : "world");
						}
					}
				}
			}
			return ", seat " + ((who.activeSeat != null) ? who.activeSeat.name : "none") + ", bodies " + num + " (" + num2 + " held still, " + num3 + " asleep, " + num4 + " off, " + num5 + " tied to something else:" + text + "), physics rig on " + (who.physicsRig.enabled && who.physicsRig.gameObject.activeInHierarchy) + PartsOf(who);
		}
		catch (System.Exception ex)
		{
			return ", bodies failed: " + ex.Message;
		}
	}

	private void OnItsFeet()
	{
		try
		{
			if (rig.activeSeat != null || rig.bodyState != RigManager.BodyState.Seated || InLesson || sitting)
			{
				seatedLooseSince = -1f;
				seatedTries = 0;
				return;
			}
			if (seatedLooseSince < 0f)
			{
				seatedLooseSince = Time.time;
			}
			if (Time.time - seatedLooseSince < ((seatedTries == 0) ? 2.5f : 0.8f))
			{
				return;
			}
			seatedLooseSince = Time.time;
			seatedTries++;
			string text;
			if (seatedTries == 1)
			{
				rig.SwitchBodyState(RigManager.BodyState.OnFoot);
				text = "asked for";
			}
			else if (seatedTries == 2)
			{
				rig.bodyState = RigManager.BodyState.OnFoot;
				text = "set";
			}
			else
			{
				rig.physicsRig.SetBodyState(RigManager.BodyState.OnFoot, RigManager.BodyState.Seated);
				rig._bodyState = RigManager.BodyState.OnFoot;
				rig._bodyStateOnDeck = RigManager.BodyState.OnFoot;
				rig._bodyStateDirty = false;
				if (!rig.physicsRig.ballLocoEnabled)
				{
					rig.physicsRig.EnableBallLoco();
				}
				text = "forced";
			}
			if (seatedTries <= 4 || seatedTries % 20 == 0)
			{
				Lab.Event("seat: its body was still a sat one with no seat under it, back on its feet (" + text + ", try " + seatedTries + ")");
			}
		}
		catch (System.Exception ex)
		{
			if (seatedTries <= 4)
			{
				Lab.Event("seat: could not put it back on its feet: " + ex.Message);
			}
		}
	}

	private static string PartsOf(RigManager who)
	{
		try
		{
			PhysicsRig physicsRig = who.physicsRig;
			string text = ", rig root " + who.transform.position.ToString("0.00");
			Rigidbody rigidbody = ((physicsRig.m_pelvis != null) ? physicsRig.m_pelvis.GetComponent<Rigidbody>() : null);
			if (rigidbody != null)
			{
				text = text + ", hips at " + rigidbody.position.ToString("0.00") + " going " + rigidbody.velocity.ToString("0.00") + " drag " + rigidbody.drag.ToString("0.0") + " constraints " + rigidbody.constraints;
			}
			foreach (Rigidbody componentsInChild in physicsRig.GetComponentsInChildren<Rigidbody>(includeInactive: true))
			{
				string text2 = componentsInChild.name.ToLowerInvariant();
				if (!componentsInChild.isKinematic && (text2.Contains("knee") || text2.Contains("feet") || text2.Contains("foot") || text2.Contains("loco")))
				{
					text = text + ", " + componentsInChild.name + " y " + componentsInChild.position.y.ToString("0.00") + " going " + componentsInChild.velocity.magnitude.ToString("0.00") + " spin " + componentsInChild.angularVelocity.magnitude.ToString("0.0") + (componentsInChild.detectCollisions ? "" : " NO COLLISIONS") + " layer " + componentsInChild.gameObject.layer;
				}
			}
			foreach (Rig componentsInChild2 in who.GetComponentsInChildren<Rig>(includeInactive: true))
			{
				text = text + ", " + componentsInChild2.GetIl2CppType().Name + (componentsInChild2.enabled ? "" : " OFF") + (componentsInChild2.gameObject.activeInHierarchy ? "" : " (object off)");
			}
			return text + ", manager " + (who.enabled ? "on" : "OFF") + ", body state " + who.bodyState.ToString() + " (next " + who._bodyStateOnDeck.ToString() + (who._bodyStateDirty ? ", due" : "") + ")" + (who._leashed ? ", LEASHED" : "") + ", phys input " + ((who.GetPhysInputRig() != null) ? who.GetPhysInputRig().GetIl2CppType().Name : "none");
		}
		catch (System.Exception ex)
		{
			return ", parts failed: " + ex.Message;
		}
	}

	public void LabSprint()
	{
		if (mode != Mode.Wander)
		{
			SetMode(Mode.Wander);
		}
		StopPlaying();
		Vector3 vector = Feet;
		if (Begin(Act.Sprint, Player.Head, vector))
		{
			runTraceUntil = Time.time + 12f;
			Lab.Event("run: sent sprinting " + Flat(nav.Goal - vector).magnitude.ToString("0") + " m");
		}
	}

	public void LabWave(int times)
	{
		StopPlaying();
		if (mode != 0)
		{
			SetMode(Mode.Stand);
		}
		waveStart = Time.time;
		waveUntil = Time.time + 0.35f + (float)times * 0.7f;
		waveLeft = false;
		WaveOfTheirs();
	}

	private void Wave(float t)
	{
		if (Time.time >= waveUntil)
		{
			return;
		}
		if (waveClip != null)
		{
			OverClip(waveClip, Time.time - waveStart, 1f);
			return;
		}
		float num = waveUntil - Time.time;
		float t2 = Mathf.Clamp01(Mathf.Min((Time.time - waveStart) * 4f, num * 4f));
		float num2 = Mathf.Sin(t * (float)System.Math.PI * 2f / 0.7f);
		float num3 = pose.height / 1.7f;
		Vector3 b = new Vector3((0.32f + num2 * 0.12f) * num3, pose.height + 0.08f * num3, 0.12f * num3);
		Quaternion b2 = Quaternion.Euler(-75f, 0f, num2 * 25f);
		if (waveLeft)
		{
			pose.leftPos = Vector3.Lerp(pose.leftPos, new Vector3(0f - b.x, b.y, b.z), Mathf.SmoothStep(0f, 1f, t2));
			pose.leftRot = Quaternion.Slerp(pose.leftRot, b2, t2);
		}
		else
		{
			pose.rightPos = Vector3.Lerp(pose.rightPos, b, Mathf.SmoothStep(0f, 1f, t2));
			pose.rightRot = Quaternion.Slerp(pose.rightRot, b2, t2);
		}
		curl = 0.1f;
	}

	public static string RandomGun()
	{
		System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
		AssetWarehouse instance = AssetWarehouse.Instance;
		string[] spawnGuns = SpawnGuns;
		foreach (string newId in spawnGuns)
		{
			if (instance != null && instance.HasCrate(new Barcode(newId)))
			{
				list.Add(newId);
			}
		}
		if (list.Count <= 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	public void LabToss(string barcode)
	{
		StopPlaying();
		if (mode != 0)
		{
			SetMode(Mode.Stand);
		}
		tossNext = true;
		if (!SpawnGun(barcode))
		{
			tossNext = false;
			Lab.Event("could not spawn a gun to toss");
		}
	}

	private bool Moves(MotionLibrary.Gait want, float dt, out Frame f)
	{
		f = default(Frame);
		if (want != wantGait)
		{
			wantGait = want;
			wantSince = Time.time;
		}
		if (clip == null || (want != gait && Time.time - wantSince > 0.25f) || clipLeft <= 0f)
		{
			bool tense = (mode == Mode.Brain || mode == Mode.Fists || mode == Mode.Gunplay) && (nearestSeen.transform != null || Time.time - lastEnemyTime < 8f);
			Frame[] array = MotionLibrary.Pick((clip == null) ? want : wantGait, lately, personality, tense, walk.magnitude < 0.6f, avatarCode);
			if (array == null)
			{
				clip = null;
				return false;
			}
			if (clip != null)
			{
				fromClip = clip;
				fromT = clipT;
				fromMirror = clipMirror;
				fromRate = clipRate;
				fade = 0f;
			}
			clip = array;
			gait = wantGait;
			lately.Add(clip);
			while (lately.Count > Mathf.Max(1, MotionLibrary.Count(gait) * 2 / 3))
			{
				lately.RemoveAt(0);
			}
			clipMirror = UnityEngine.Random.value < personality.mirror;
			clipRate = UnityEngine.Random.Range(0.92f, 1.08f) * personality.tempo;
			float time = clip[clip.Length - 1].time;
			float num = Mathf.Min(time, UnityEngine.Random.Range(2.5f, 6f));
			clipT = UnityEngine.Random.Range(0f, time - num);
			if (MoveSlots.IsSlot(clip))
			{
				num = time;
				clipT = 0f;
			}
			clipLeft = num / clipRate - 0.35f;
		}
		float num2 = MoveSlots.SpeedOf(clip);
		if (num2 > 0.3f && gait != 0)
		{
			Rigidbody rigidbody = Pelvis();
			float num3 = ((rigidbody != null) ? Flat(rigidbody.velocity).magnitude : num2);
			clipRate = Mathf.Lerp(clipRate, Mathf.Clamp(num3 / num2, 0.7f, 1.4f), Mathf.Clamp01(dt * 4f));
		}
		clipT += dt * clipRate;
		fromT += dt * fromRate;
		clipLeft -= dt;
		f = Sample(clip, clipT, clipMirror);
		if (fade < 1f && fromClip != null)
		{
			fade = Mathf.Min(1f, fade + dt / 0.35f);
			f = Blend(Sample(fromClip, fromT, fromMirror), f, Mathf.SmoothStep(0f, 1f, fade));
		}
		if (!MoveSlots.IsSlot(clip))
		{
			f = MotionLibrary.Relax(f, Tuning.F("relax.arms", 0.85f));
		}
		else
		{
			float num4 = MoveSlots.Size(clip, StandingEyes());
			if (Mathf.Abs(num4 - 1f) > 0.03f)
			{
				f.height *= num4;
				f.leftPos *= num4;
				f.rightPos *= num4;
			}
		}
		return true;
	}

	private static Frame Sample(Frame[] from, float t, bool mirror)
	{
		Frame frame = MotionLibrary.Sample(from, t);
		if (!mirror)
		{
			return frame;
		}
		return MotionLibrary.Mirror(frame);
	}

	private void Follow(Transform you, Transform head, float dt)
	{
		headYaw = 0f;
		if (you == null || head == null)
		{
			walk = Vector2.zero;
			return;
		}
		Vector3 vector = Feet;
		Vector3 vector2 = you.position - Vector3.up * 1.4f;
		if (PortalStep(vector2, afterPlayer: true, dt))
		{
			following = true;
			return;
		}
		float magnitude = Flat(vector2 - vector).magnitude;
		if (DriveAfterYou(you, magnitude) || ToAVehicle(you, magnitude, dt))
		{
			return;
		}
		PhysicsRig physicsRig = rig.physicsRig;
		float num = ((physicsRig != null && physicsRig.centerOfPressure != null) ? physicsRig.centerOfPressure.position.y : vector.y);
		bool flag = vector2.y - num > Tuning.F("follow.above", 1f);
		bool flag2 = num - vector2.y > Tuning.F("follow.below", 1f);
		if (!following && (magnitude > BotManager.FollowDistance + 0.6f || flag || flag2))
		{
			following = true;
		}
		if (!following)
		{
			Watch(you, head, dt, 150f);
			walk = Vector2.zero;
			return;
		}
		Vector3 vector3 = vector2 + FollowSide(vector2, vector);
		if (!nav.HasGoal || Flat(nav.Goal - vector3).magnitude > Tuning.F("follow.regoal", 1.5f))
		{
			nav.SetGoal(vector3);
		}
		bool flag3 = running;
		bool flag4 = false;
		try
		{
			Rigidbody rigidbody = ((Player.PhysicsRig != null && Player.PhysicsRig.m_pelvis != null) ? Player.PhysicsRig.m_pelvis.GetComponent<Rigidbody>() : null);
			flag4 = rigidbody != null && Flat(rigidbody.velocity).magnitude > Tuning.F("follow.yourun", 2.8f);
		}
		catch (System.Exception)
		{
		}
		running = magnitude > BotManager.FollowDistance + Tuning.F("follow.run.after", 2f) || (flag4 && magnitude > BotManager.FollowDistance + 0.8f) || (running && (magnitude > BotManager.FollowDistance + 0.6f || Time.time - runningSince < 2.5f));
		if (running && !flag3)
		{
			runningSince = Time.time;
		}
		float speed = (running ? MotionLibrary.RunPush : Mathf.Clamp(MotionLibrary.WalkPush * (0.75f + (magnitude - BotManager.FollowDistance) * 0.15f), 0.3f, MotionLibrary.WalkPush));
		climbKeenAt = Time.time;
		if (flag && !climbing && magnitude > 0.2f)
		{
			TryStartClimb(Flat(vector2 - vector).normalized);
		}
		if (flag && !climbing && magnitude < 1.2f)
		{
			Watch(you, head, dt, 150f);
			walk = Vector2.zero;
		}
		else if (Travel(speed, (flag || flag2) ? 0.2f : BotManager.FollowDistance, dt) && !flag && !flag2)
		{
			following = false;
			running = false;
			nav.Clear();
		}
	}

	private Vector3 FollowSide(Vector3 yourFeet, Vector3 feet)
	{
		int num = 0;
		int num2 = 0;
		foreach (Bot item in BotManager.All)
		{
			if (!item.dead && item.enemy == enemy && item.mode == Mode.Follow)
			{
				if (item == this)
				{
					num = num2;
				}
				num2++;
			}
		}
		if (num2 < 2)
		{
			return Vector3.zero;
		}
		Vector3 vector = Flat(yourFeet - feet);
		if (vector.sqrMagnitude < 4f)
		{
			return followSideWas;
		}
		Vector3 vector2 = Vector3.Cross(Vector3.up, vector.normalized);
		float num3 = ((num % 2 == 0) ? (-1f) : 1f) * (0.9f + 0.9f * (float)(num / 2));
		followSideWas = Vector3.Lerp(followSideWas, vector2 * num3 * Tuning.F("follow.side", 1f), 0.05f);
		return followSideWas;
	}

	private void Gunplay(Transform you, Transform head, float dt)
	{
		running = false;
		if (Assisting && !Playing)
		{
			FistsOnly(you, head, dt);
			if (nearestSeen.transform != null)
			{
				assistSawAt = Time.time;
			}
			else if (Time.time - assistSawAt > 2.5f && Time.time - assistBegan > 2.5f)
			{
				assistUntil = 0f;
			}
			if (nearestSeen.transform != null && nearestSeenDistance < 3f)
			{
				assistUntil = Mathf.Max(assistUntil, Time.time + 3f);
			}
			return;
		}
		if (Playing)
		{
			PlayStep(you, head, dt);
			return;
		}
		walk = Vector2.zero;
		if (Time.time < idleUntil)
		{
			Watch(you, head, dt, 90f);
		}
		else if (!GetAGun())
		{
			if (!BotManager.OwnSpawns && Time.time - noGunSaidAt > 60f)
			{
				noGunSaidAt = Time.time;
				Spawnables.Notify("No gun near the bot (turn on Spawn guns for bots)");
			}
			idleUntil = Time.time + 3f;
		}
	}

	private void Wander(Transform you, Transform head, float dt)
	{
		Life(you, head, dt);
	}

	private bool Travel(float speed, float arrive, float dt)
	{
		if (climbing)
		{
			ClimbStep(dt);
			return false;
		}
		Vector3 vector = Feet;
		if (nav.Remaining(vector) < arrive)
		{
			walk = Vector2.zero;
			travelEnded = "there (" + nav.Remaining(vector).ToString("0.0") + " m left)";
			return true;
		}
		Transform head = Head;
		Vector3 chest = ((head != null) ? (head.position - Vector3.up * 0.45f) : (vector + Vector3.up * 1.2f));
		float rise;
		bool drop;
		Vector3 vector2 = nav.Steer(vector, chest, Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward, out rise, out drop);
		if (drop && vector2 != Vector3.zero && nav.HasGoal && nav.Goal.y < vector.y - 0.5f && nav.DropDepth <= Tuning.F("drop.max", 5f))
		{
			if (Time.time - dropSaidAt > 3f)
			{
				dropSaidAt = Time.time;
				Lab.Event("drop: down a " + nav.DropDepth.ToString("0.0") + " m edge");
			}
			Go(vector2, speed, dt);
			return false;
		}
		if (vector2 == Vector3.zero || drop)
		{
			walk = Vector2.zero;
			travelEnded = (drop ? ("an edge ahead, " + nav.DropDepth.ToString("0.0") + " m down") : "no way to turn") + (nav.OnMesh ? ", on the level's ways" : ", by feel") + ", feet at " + vector.y.ToString("0.00");
			return true;
		}
		if (TryStartClimb(vector2))
		{
			return false;
		}
		Go(vector2, speed, dt);
		if (rise > 0.4f && rise < 1.3f && Time.time - ledgeJumpAt > 1.5f)
		{
			ledgeJumpAt = Time.time;
			AskJump();
		}
		if (nav.CheckStuck(vector, walk.magnitude, dt))
		{
			AskJump();
			if (nav.StuckCount == 2)
			{
				SelfTaught.Bad(nav.HasGoal ? nav.Goal : vector);
			}
		}
		return false;
	}

	private void Go(Vector3 dir, float speed, float dt)
	{
		Vector3 vector = dir;
		if (Time.time - goAt < 0.2f)
		{
			goByOthers += Mathf.Abs(Mathf.DeltaAngle(goYawWas, bodyYaw));
			goByWay += ((goDirWas.sqrMagnitude > 0.01f) ? Vector3.Angle(goDirWas, vector) : 0f);
		}
		else
		{
			goByOthers = (goByWay = (goByApart = 0f));
			goSaidAt = Time.time;
		}
		goDirWas = vector;
		dir = KeepApart(dir, ref speed);
		goByApart += Vector3.Angle(vector, dir) * dt * 5f;
		if (Time.time - goSaidAt > 3f)
		{
			if (goByOthers > 120f || goByWay > 240f || goByApart > 60f)
			{
				Lab.Event("walk: in 3 s its body was turned " + goByOthers.ToString("0") + " deg by something other than its walking, its way swung " + goByWay.ToString("0") + " deg in all, kept off others by " + goByApart.ToString("0") + "; " + act.ToString() + "/" + play.ToString() + ((Time.time < gazeUntil) ? ", looking at something" : "") + (thinking2 ? ", thinking" : ""));
			}
			goByOthers = (goByWay = (goByApart = 0f));
			goSaidAt = Time.time;
		}
		steer = ((steer.sqrMagnitude < 0.01f) ? dir : Vector3.Slerp(steer, dir, 1f - Mathf.Exp((0f - dt) * Tuning.F("move.curve", 2.6f))));
		dir = ((Flat(steer).sqrMagnitude > 0.001f) ? Flat(steer).normalized : dir);
		float num = Recorder.Yaw(dir);
		float num2 = Mathf.Lerp(Tuning.F("move.turn.slow", 200f), Tuning.F("move.turn.fast", 110f), Mathf.Clamp01(stick.magnitude));
		bodyYaw = Mathf.MoveTowardsAngle(bodyYaw, num, num2 * dt);
		float num3 = Mathf.Abs(Mathf.DeltaAngle(bodyYaw, num));
		Vector3 vector2 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * dir;
		walk = new Vector2(vector2.x, vector2.z) * (speed * Mathf.Clamp01(1.3f - num3 / 90f));
		goYawWas = bodyYaw;
		goAt = Time.time;
	}

	public void PickSomethingUp()
	{
		SetMode(Mode.Wander);
		if (!FindSomethingToPlayWith())
		{
			Spawnables.Notify("Nothing it can pick up nearby");
		}
	}

	public void BringTo(Vector3 feet, Vector3 forward)
	{
		try
		{
			bodyYaw = Recorder.Yaw(forward);
			StopPlaying();
			nav.Clear();
			droveHas = false;
			droveHeadHas = false;
			portalHeadHas = false;
			portalGoing = false;
			SmoothReset();
			rig.Teleport(feet);
		}
		catch (System.Exception ex)
		{
			Spawnables.Notify("The bot could not be brought over: " + ex.Message);
		}
	}

	private float Watch(Transform you, Transform head, float dt, float speed)
	{
		if (you == null || head == null)
		{
			return 0f;
		}
		Vector3 forward = Flat(you.position - head.position);
		if (forward.sqrMagnitude < 0.01f)
		{
			return 0f;
		}
		float num = Recorder.Yaw(forward);
		if (Mathf.Abs(Mathf.DeltaAngle(bodyYaw, num)) > 15f || mode != 0)
		{
			bodyYaw = Mathf.MoveTowardsAngle(bodyYaw, num, speed * dt);
		}
		return Mathf.DeltaAngle(bodyYaw, num);
	}

	private float LookPitch(Transform you, Transform head)
	{
		if (you == null || head == null)
		{
			return 0f;
		}
		Vector3 v = you.position - head.position;
		return Mathf.Clamp((0f - Mathf.Atan2(x: Mathf.Max(Flat(v).magnitude, 0.1f), y: v.y)) * 57.29578f, -40f, 40f);
	}

	private Frame Standing(float t, float pitch, float lookYaw, float swing)
	{
		float num = Mathf.Sin(t * 1.7f);
		float num2 = Mathf.Sin(t * 0.6f);
		float num3 = Mathf.Sin(stride) * 0.13f * swing;
		float num4 = Mathf.Abs(Mathf.Cos(stride)) * 0.02f * swing;
		float num5 = standingHeight + num * 0.004f;
		Frame result = default(Frame);
		result.height = num5;
		result.head = Quaternion.Euler(pitch, lookYaw + num2 * 4f, 0f);
		result.leftPos = new Vector3(-0.2f, num5 - 0.78f + num * 0.006f + num4, 0.06f + num2 * 0.01f + num3);
		result.rightPos = new Vector3(0.2f, num5 - 0.78f + num * 0.006f + num4, 0.06f - num2 * 0.01f - num3);
		result.leftRot = Quaternion.Euler(65f, 10f, 0f);
		result.rightRot = Quaternion.Euler(65f, -10f, 0f);
		return result;
	}

	private void Replay()
	{
		float num = Time.time - replayStart;
		while (replayAt < replay.Count - 1 && replay[replayAt + 1].time <= num)
		{
			replayAt++;
			if (replay[replayAt].jumped && replayAt != lastJumpFrame)
			{
				lastJumpFrame = replayAt;
				AskJump();
			}
		}
		if (replayAt >= replay.Count - 1)
		{
			if (previewing)
			{
				previewing = false;
				crouchReset = true;
				SetMode(previewBack);
			}
			else
			{
				Spawnables.Notify("Replay finished");
				SetMode(Mode.Stand);
			}
		}
		else
		{
			Frame frame = replay[replayAt];
			Frame b = replay[replayAt + 1];
			float num2 = Mathf.InverseLerp(frame.time, b.time, num);
			pose = Blend(frame, b, num2);
			bodyYaw = replayYaw + Mathf.LerpAngle(frame.yaw, b.yaw, num2);
			walk = Vector2.Lerp(frame.stick, b.stick, num2);
			Body(frame);
			crouch = Mathf.Lerp(frame.crouch, b.crouch, num2);
			feet = Mathf.Lerp(frame.feet, b.feet, num2);
			curl = -1f;
		}
	}

	private void Body(Frame f)
	{
		jump = f.jump;
		otherA = f.otherA;
		primaryClick = f.primaryStick;
		secondaryClick = f.secondaryStick;
		crouching = true;
		crouch = f.crouch;
		feet = f.feet;
	}

	private void NoteJumping()
	{
		RemapRig remapHeptaRig = rig.remapHeptaRig;
		bool flag = remapHeptaRig != null && remapHeptaRig._jumping;
		if (flag && !wasJumping)
		{
			jumpedAt = Time.time;
			jumpClip = MoveSlots.Pick(MoveSlots.Kind.Jump, null, avatarCode);
		}
		wasJumping = flag;
		try
		{
			if (flag && remapHeptaRig != null && Time.time - jumpedAt > Tuning.F("jump.longest", 1.6f) && !climbing && !InLesson)
			{
				Rigidbody rigidbody = Pelvis();
				if (rigidbody != null && Mathf.Abs(rigidbody.velocity.y) < 0.6f && rig.physicsRig.centerOfPressure != null && FeetOnGround())
				{
					remapHeptaRig._jumping = false;
					remapHeptaRig._jumpCycle = 0f;
					remapHeptaRig._timeOfFlight = 0f;
					if (remapHeptaRig.travState != 0)
					{
						remapHeptaRig.travState = RemapRig.TraversalState.Walk;
					}
					wasJumping = false;
					jumpsCleared++;
					if (Time.time - jumpClearSaidAt > 5f)
					{
						jumpClearSaidAt = Time.time;
						Lab.Event("legs: a jump that never ended (" + (Time.time - jumpedAt).ToString("0.0") + " s, feet on the ground): ended, " + jumpsCleared + " so far");
					}
				}
			}
		}
		catch (System.Exception)
		{
		}
		if (jumpDue > 0f && Time.time >= jumpDue)
		{
			jumpDue = -1f;
			if (Time.time - jumpedAt > 0.3f && remapHeptaRig != null && !remapHeptaRig._jumping)
			{
				remapHeptaRig.Jump();
			}
		}
	}

	private void AskJump()
	{
		Call("effort");
		if (jumpDue < 0f)
		{
			jumpDue = Time.time + 0.12f;
		}
	}

	internal static Frame Blend(Frame a, Frame b, float k)
	{
		Frame result = a;
		result.height = Mathf.Lerp(a.height, b.height, k);
		result.head = Quaternion.Slerp(a.head, b.head, k);
		result.leftPos = Vector3.Lerp(a.leftPos, b.leftPos, k);
		result.rightPos = Vector3.Lerp(a.rightPos, b.rightPos, k);
		result.leftRot = Quaternion.Slerp(a.leftRot, b.leftRot, k);
		result.rightRot = Quaternion.Slerp(a.rightRot, b.rightRot, k);
		result.leftGrip = Mathf.Lerp(a.leftGrip, b.leftGrip, k);
		result.rightGrip = Mathf.Lerp(a.rightGrip, b.rightGrip, k);
		return result;
	}

	public void Drive()
	{
		driveCalls++;
		LastDriveFrame = Time.frameCount;
		if (mode == Mode.Exact && !dead)
		{
			ExactDrive();
			return;
		}
		if (dead || down)
		{
			driveStops++;
			return;
		}
		SitPose(Time.time);
		HandsUpPose();
		KneelPose();
		Transform vrRoot = controls.vrRoot;
		Transform headset = controls.headset;
		if (vrRoot == null || headset == null)
		{
			return;
		}
		if (Bad(pose.height) || Bad(pose.leftPos) || Bad(pose.rightPos) || Bad(pose.head) || Bad(pose.leftRot) || Bad(pose.rightRot))
		{
			if (Time.time - badSaidAt > 5f)
			{
				badSaidAt = Time.time;
				Lab.Event("bad pose (not numbers) in " + mode.ToString() + (climbing ? ", climbing" : "") + ": stood up again");
			}
			if (climbing)
			{
				StopClimb("its pose went bad");
			}
			pose = Standing(Time.time, 0f, 0f, 0f);
		}
		if (Puppet)
		{
			NetDrive();
			return;
		}
		if (mode == Mode.Clone)
		{
			CloneDrive();
			return;
		}
		rig.remapHeptaRig.inWeight = 1f;
		Vector2 vector = Vector2.ClampMagnitude(running ? walk : (walk * personality.pace), 1f);
		if (mode == Mode.Copy || mode == Mode.Replay)
		{
			stick = Vector2.MoveTowards(stick, vector, 30f * Time.deltaTime);
		}
		else
		{
			float magnitude = stick.magnitude;
			float magnitude2 = vector.magnitude;
			float num = ((magnitude2 > magnitude) ? (1f / Mathf.Max(0.1f, MotionLibrary.StartTime)) : (1f / Mathf.Max(0.1f, MotionLibrary.StopTime)));
			float num2 = Mathf.MoveTowards(magnitude, magnitude2, num * Time.deltaTime);
			Vector2 vector2 = ((magnitude2 > 0.01f) ? (vector / magnitude2) : ((magnitude > 0.01f) ? (stick / magnitude) : Vector2.up));
			Vector2 vector3 = ((magnitude > 0.01f) ? (stick / magnitude) : vector2);
			float z = Mathf.Clamp(Vector2.SignedAngle(vector3, vector2), (0f - Tuning.F("move.swing", 360f)) * Time.deltaTime, Tuning.F("move.swing", 360f) * Time.deltaTime);
			stick = ((Vector2)(Quaternion.Euler(0f, 0f, z) * vector3)).normalized * num2;
		}
		if (stick.sqrMagnitude < 0.0004f && walk == Vector2.zero)
		{
			stick = Vector2.zero;
		}
		gameStick = ((mode == Mode.Copy || mode == Mode.Replay) ? stick : ForItsHead(stick));
		if (controls.leftController != null)
		{
			controls.leftController._thumbstickAxis = gameStick;
		}
		controls._axisPrimary = gameStick;
		controls._axisPrimaryDirty = stick != Vector2.zero;
		KeepPortalTurn(vrRoot);
		Quaternion quaternion2 = (vrRoot.rotation = Quaternion.Euler(0f, bodyYaw, 0f));
		NotePortalTurn(vrRoot);
		headset.SetPositionAndRotation(vrRoot.TransformPoint(new Vector3(0f, pose.height + personality.posture, 0f)), quaternion2 * Norm(pose.head));
		KeepHeadDown(vrRoot, headset);
		Place(controls.leftController, vrRoot, quaternion2, pose.leftPos, pose.leftRot, pose.leftGrip);
		Place(controls.rightController, vrRoot, quaternion2, pose.rightPos, pose.rightRot, pose.rightGrip);
		bool secondaryAButton = controls._secondaryAButton;
		controls._secondaryAButtonDown = jump && !secondaryAButton;
		controls._secondaryAButtonUp = !jump && secondaryAButton;
		controls._secondaryAButton = jump;
		secondaryAButton = controls._primaryAButton;
		controls._primaryAButtonDown = otherA && !secondaryAButton;
		controls._primaryAButtonUp = !otherA && secondaryAButton;
		controls._primaryAButton = otherA;
		secondaryAButton = controls._primaryStick;
		controls._primaryStickDown = primaryClick && !secondaryAButton;
		controls._primaryStickUp = !primaryClick && secondaryAButton;
		controls._primaryStick = primaryClick;
		BaseController leftController = controls.leftController;
		if (leftController != null)
		{
			bool thumbstick = leftController._thumbstick;
			leftController._thumbstickDown = primaryClick && !thumbstick;
			leftController._thumbstickUp = !primaryClick && thumbstick;
			leftController._thumbstick = primaryClick;
		}
		secondaryAButton = controls._secondaryStick;
		controls._secondaryStickDown = secondaryClick && !secondaryAButton;
		controls._secondaryStickUp = !secondaryClick && secondaryAButton;
		controls._secondaryStick = secondaryClick;
		if (crouching)
		{
			rig.remapHeptaRig._crouchTarget = crouch;
			rig.remapHeptaRig._feetOffset = feet;
		}
		else if (crouchReset)
		{
			crouchReset = false;
			rig.remapHeptaRig._crouchTarget = 0f;
			rig.remapHeptaRig._feetOffset = 0f;
		}
	}

	private static Grip HeldGrip(Hand hand)
	{
		try
		{
			return ((hand.AttachedReceiver != null) ? hand.AttachedReceiver.TryCast<Grip>() : null) ?? ((hand.m_CurrentAttachedGO != null) ? hand.m_CurrentAttachedGO.GetComponent<Grip>() : null);
		}
		catch (System.Exception)
		{
			return null;
		}
	}

	private void Place(BaseController controller, Transform root, Quaternion body, Vector3 at, Quaternion turn, float grip)
	{
		if (controller == null)
		{
			return;
		}
		controller.transform.SetPositionAndRotation(root.TransformPoint(at), body * Norm(turn));
		float num = ((curl >= 0f) ? curl : Mathf.Clamp01(grip));
		Hand hand = ((controller == controls.leftController) ? LeftHand : RightHand);
		bool flag = controller == controls.leftController;
		float[] array = null;
		float num2 = -1f;
		if (hand != null && hand.m_CurrentAttachedGO != null)
		{
			num2 = 1f;
			array = GripBook.FingersOn(HeldGrip(hand), flag);
		}
		else if (flag && Time.time - frontClosingAt < 0.1f && support != null)
		{
			num2 = frontClosing;
			array = GripBook.FingersOn(support, leftHand: true);
		}
		float[] array2 = new float[5] { num, num, num, num, num };
		if (num2 >= 0f)
		{
			for (int i = 0; i < 5; i++)
			{
				array2[i] = Mathf.Lerp(num, (array != null) ? array[i] : 1f, num2);
			}
		}
		controller._processedIndex = array2[0];
		controller._processedMiddle = array2[1];
		controller._processedRing = array2[2];
		controller._processedPinky = array2[3];
		controller._processedThumb = array2[4];
		controller._solvedGrip = ((flag && FrontLetGrab && hand != null && hand.m_CurrentAttachedGO == null) ? 1f : Mathf.Min(num, OpenController.grabThreshold - 0.01f));
		float num3 = ((controller == controls.rightController) ? trigger : 0f);
		bool primaryInteractionButton = controller._primaryInteractionButton;
		bool flag2 = num3 > 0.5f;
		controller._primaryAxis = num3;
		controller._primaryInteractionButtonDown = flag2 && !primaryInteractionButton;
		controller._primaryInteractionButtonUp = !flag2 && primaryInteractionButton;
		controller._primaryInteractionButton = flag2;
		if (controller == controls.rightController)
		{
			PutPresses(controller);
		}
	}

	private float StandingEyes()
	{
		Il2CppSLZ.VRMK.Avatar avatar = rig.avatar;
		if (avatar == null)
		{
			return 0f;
		}
		float result = avatar.eyeHeight * avatar.transform.lossyScale.y;
		if (avatar.Pointer != eyesOf)
		{
			eyesOf = avatar.Pointer;
			Lab.Event("head limit: " + avatar.name + " eye height " + avatar.eyeHeight.ToString("0.000") + " x scale " + avatar.transform.lossyScale.y.ToString("0.000") + " = " + result.ToString("0.000") + " m");
		}
		return result;
	}

	private void KeepHeadDown(Transform root, Transform headset)
	{
		float num = StandingEyes() * Tuning.F("head.max", 0.99f);
		if (num <= 0.3f)
		{
			return;
		}
		Vector3 position = headset.position;
		float num2 = position.y - root.position.y;
		if (!(num2 <= num))
		{
			headset.position = new Vector3(position.x, root.position.y + num, position.z);
			if (Time.time - headCutSaidAt > 2f)
			{
				headCutSaidAt = Time.time;
				Lab.Event("head limit: head was " + num2.ToString("0.00") + " m up, kept at " + num.ToString("0.00") + " m (" + mode.ToString() + ")");
			}
		}
	}

	private static bool Bad(float v)
	{
		if (!float.IsNaN(v))
		{
			return float.IsInfinity(v);
		}
		return true;
	}

	private static bool Bad(Vector3 v)
	{
		if (!Bad(v.x) && !Bad(v.y))
		{
			return Bad(v.z);
		}
		return true;
	}

	private static bool Bad(Quaternion q)
	{
		if (!Bad(q.x) && !Bad(q.y) && !Bad(q.z) && !Bad(q.w))
		{
			return q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w < 0.5f;
		}
		return true;
	}

	private static Quaternion Norm(Quaternion q)
	{
		float num = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
		if (!(num < 0.0001f))
		{
			return new Quaternion(q.x / num, q.y / num, q.z / num, q.w / num);
		}
		return Quaternion.identity;
	}

	private static Vector3 Flat(Vector3 v)
	{
		v.y = 0f;
		return v;
	}

	public void Detach()
	{
		DropStowed();
		try
		{
			if (walkMost > 0f && rig != null && rig.remapHeptaRig != null)
			{
				rig.remapHeptaRig.maxVelocity = walkMost;
			}
		}
		catch (System.Exception)
		{
		}
		if (InLesson)
		{
			ReleaseCloneHands();
		}
		if (gun != null && Holding())
		{
			if (Reloading)
			{
				DropNewMag();
			}
			LetGoOfSupport();
			Pull(on: false);
		}
		else
		{
			StopPlaying();
		}
		HideThought();
	}

	public void Remove()
	{
		HideThought();
		if (rig == null)
		{
			return;
		}
		try
		{
			StopPlaying();
		}
		catch (System.Exception)
		{
		}
		foreach (Grip componentsInChild in rig.GetComponentsInChildren<Grip>(includeInactive: true))
		{
			try
			{
				if (componentsInChild != null)
				{
					componentsInChild.ForceDetach();
				}
			}
			catch (System.Exception)
			{
			}
		}
		UnityEngine.Object.Destroy(rig.gameObject);
	}

	private static void WatchPlayer(Transform you)
	{
		if (pointFrame == Time.frameCount)
		{
			return;
		}
		pointFrame = Time.frameCount;
		youFrame = Time.frameCount;
		try
		{
			if (Flat(you.position - youStoodAt).magnitude > 2.5f)
			{
				youStoodAt = you.position;
				youStoodSince = Time.time;
			}
			OpenControllerRig controllerRig = Player.ControllerRig;
			Hand[] array = new Hand[2]
			{
				Player.LeftHand,
				Player.RightHand
			};
			BaseController[] array2 = new BaseController[2]
			{
				(controllerRig != null) ? controllerRig.leftController : null,
				(controllerRig != null) ? controllerRig.rightController : null
			};
			bool flag = false;
			Vector3 vector = Vector3.zero;
			Vector3 vector2 = Vector3.zero;
			for (int i = 0; i < 2; i++)
			{
				if (flag)
				{
					break;
				}
				Hand hand = array[i];
				BaseController baseController = array2[i];
				if (!(hand == null) && !(baseController == null) && !(hand.m_CurrentAttachedGO != null) && !WallGrab.Holding(hand))
				{
					bool num = baseController._processedIndex < 0.3f && baseController._processedMiddle > 0.6f && baseController._processedRing > 0.6f;
					Vector3 vector3 = you.position - Vector3.up * 0.22f;
					Vector3 vector4 = hand.transform.position - vector3;
					if (num && !(vector4.magnitude < Tuning.F("point.arm", 0.45f)))
					{
						flag = true;
						vector = hand.transform.position;
						vector2 = vector4.normalized;
					}
				}
			}
			if (!flag)
			{
				pointSince = -1f;
				return;
			}
			if (pointSince < 0f)
			{
				pointSince = Time.time;
			}
			if (Time.time - pointSince < Tuning.F("point.hold", 0.6f) || Time.time - pointedAt < 2.5f)
			{
				return;
			}
			pointHas = false;
			foreach (RaycastHit item in Physics.RaycastAll(vector + vector2 * 0.3f, vector2, 45f, -1, QueryTriggerInteraction.Ignore))
			{
				if (!(item.collider == null) && !(item.collider.GetComponentInParent<RigManager>() == Player.RigManager) && (!pointHas || (item.point - vector).magnitude < (pointPlace - vector).magnitude))
				{
					pointPlace = item.point;
					pointHas = true;
				}
			}
			if (!pointHas)
			{
				pointPlace = vector + vector2 * 20f;
				pointHas = true;
			}
			pointedAt = Time.time;
			Lab.Event("you pointed (" + (pointPlace - you.position).magnitude.ToString("0") + " m off)");
			PointedThing(you);
		}
		catch (System.Exception)
		{
		}
	}

	private static void PointedThing(Transform you)
	{
		try
		{
			Rigidbody rigidbody = null;
			string code = "";
			string name = "";
			float num = Tuning.F("point.thing", 0.9f);
			foreach (Collider item in Physics.OverlapSphere(pointPlace, num, -1, QueryTriggerInteraction.Ignore))
			{
				Rigidbody rigidbody2 = ((item != null) ? item.attachedRigidbody : null);
				if (!(rigidbody2 == null) && !rigidbody2.isKinematic && !(rigidbody2.GetComponentInParent<RigManager>() != null) && !(rigidbody2.mass > 15f) && !NotForBots(rigidbody2) && Tryable(rigidbody2, out var code2, out var name2) && !(GripOf(rigidbody2) == null))
				{
					float magnitude = (item.ClosestPoint(pointPlace) - pointPlace).magnitude;
					if (magnitude < num)
					{
						num = magnitude;
						rigidbody = rigidbody2;
						code = code2;
						name = name2;
					}
				}
			}
			if (rigidbody == null)
			{
				return;
			}
			Bot bot = null;
			float num2 = Tuning.F("point.thing.far", 40f);
			foreach (Bot item2 in BotManager.All)
			{
				if (!item2.dead && !item2.enemy && !item2.InLesson && !item2.climbing && !item2.Reloading && !(item2.nearestSeen.transform != null) && !(item2.brainTarget.transform != null) && !(item2.rig.activeSeat != null))
				{
					float magnitude2 = (rigidbody.worldCenterOfMass - item2.Feet).magnitude;
					if (magnitude2 < num2)
					{
						num2 = magnitude2;
						bot = item2;
					}
				}
			}
			if (bot != null)
			{
				string who = Lab.Who;
				Lab.Who = bot.Name;
				bot.TryPointedAt(code, name);
				bot.pointSeen = pointedAt;
				Lab.Who = who;
			}
		}
		catch (System.Exception)
		{
		}
	}

	private void Aware(Transform you, Transform head)
	{
		if (you == null || head == null || enemy)
		{
			return;
		}
		WatchPlayer(you);
		Vector3 vector = you.position - head.position;
		float magnitude = vector.magnitude;
		bool flag = nearestSeen.transform == null && brainTarget.transform == null && !Reloading && !climbing && !InLesson && Time.time >= waveUntil;
		if (pointedAt > pointSeen && Time.time - pointedAt < 1f)
		{
			pointSeen = pointedAt;
			if (flag && magnitude < Tuning.F("point.far", 14f) && Targets.InSight(head.position, you.position, (Player.RigManager != null) ? Player.RigManager.transform : null, rig))
			{
				Look(pointPlace, null, "where you pointed", Random(2.2f, 3.5f));
				Lab.Event("looks where you pointed");
				if ((mode == Mode.Wander || (mode == Mode.Brain && (goal == Goal.Explore || goal == Goal.Idle || goal == Goal.Play))) && !Playing && Ground(pointPlace + Vector3.up * 0.5f, out var ground) && Flat(ground - Feet).magnitude > 3f && UnityEngine.Random.value < Tuning.F("point.go", 0.7f))
				{
					Vector3 vector2 = ground - Flat(ground - Feet).normalized * 1.2f;
					planPlace = (Ground(vector2 + Vector3.up * 0.5f, out var ground2) ? ground2 : ground);
					planLook = pointPlace;
					planSight = Sight.Open;
					planWhy = "going to see what you pointed at";
					planPush = MotionLibrary.WalkPush;
					planRun = false;
					viaLeft = false;
					onDetour = false;
					detours = 0;
					thinking2 = true;
					thinkUntil = Time.time + Random(1f, 1.8f);
					act = Act.Go;
					actUntil = Time.time + 60f;
					planSince = Time.time;
					pauseNext = Time.time + 30f;
					nav.Clear();
					Lab.Event("roam: " + planWhy + " (" + Flat(planPlace - Feet).magnitude.ToString("0") + " m)");
				}
			}
		}
		bool flag2 = magnitude < 9f && Vector3.Angle(you.forward, -vector) < Tuning.F("stare.cone", 9f);
		stareSince = ((!flag2) ? (-1f) : ((stareSince < 0f) ? Time.time : stareSince));
		if (flag2 && flag && Time.time - stareSince > Tuning.F("stare.hold", 1.3f) && Time.time - staredAt > Tuning.F("stare.every", 14f) && HandsFree)
		{
			staredAt = Time.time;
			Look(you.position, you, "you, looking at it", Random(1.8f, 3.2f));
		}
	}

	private void AsideForYou(float dt)
	{
		if (!enemy && !climbing && !InLesson && !down && (!Playing || play == Play.Shoot))
		{
			Transform head = Head;
			if (!(head == null) && Tactics.InPlayersLine(head.position - Vector3.up * 0.4f, out var outOfIt))
			{
				Vector3 vector = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * outOfIt;
				walk = new Vector2(vector.x, vector.z) * MotionLibrary.WalkPush;
			}
		}
	}

	private void Call(string what)
	{
		try
		{
			if (dead || Tuning.F("bot.calls", 1f) < 0.5f)
			{
				return;
			}
			float num = what switch
			{
				"effort" => 2.5f, 
				"clear" => 12f, 
				"spot" => 9f, 
				_ => 4f, 
			};
			if ((calledAt.TryGetValue(what, out var value) && Time.time - value < num) || Time.time - voiceAt < 0.8f)
			{
				return;
			}
			Il2CppSLZ.VRMK.Avatar avatar = rig.avatar;
			Transform head = Head;
			if (avatar == null || head == null)
			{
				return;
			}
			AudioVarianceData audioVarianceData = what switch
			{
				"heave" => (Clips(avatar.bigEffort) > 0) ? avatar.bigEffort : avatar.smallEffort, 
				"effort" => avatar.smallEffort, 
				"clear" => avatar.unAgro, 
				"spot" => avatar.agro, 
				_ => avatar.recovery, 
			};
			if (Clips(audioVarianceData) == 0)
			{
				HeadSFX headSFX = ((what == "spot" || what == "clear") ? null : rig.GetComponentInChildren<HeadSFX>(includeInactive: true));
				if (!(headSFX == null))
				{
					calledAt[what] = Time.time;
					if (what == "up")
					{
						headSFX.RecoveryVocal();
					}
					else
					{
						headSFX.JumpEffort();
					}
				}
			}
			else
			{
				calledAt[what] = Time.time;
				voiceAt = Time.time;
				Audio3dManager.PlayAtPoint(audioVarianceData.audioClips, head.position, Audio3dManager.hardInteraction, Tuning.F("bot.voice", 0.9f), 1f, new Il2CppSystem.Nullable<float>(0.06f), new Il2CppSystem.Nullable<float>(1f), new Il2CppSystem.Nullable<float>(1f));
			}
		}
		catch (System.Exception)
		{
		}
	}

	public void LabVoice(string what)
	{
		Il2CppSLZ.VRMK.Avatar avatar = rig.avatar;
		if (!(avatar == null))
		{
			Lab.Event("voice: " + avatar.name + " has " + Clips(avatar.agro) + " for spotting, " + Clips(avatar.unAgro) + " for all clear, " + Clips(avatar.smallEffort) + " small effort, " + Clips(avatar.bigEffort) + " big effort, " + Clips(avatar.recovery) + " recovery, " + Clips(avatar.spawn) + " spawn");
			if (what.Length > 0)
			{
				calledAt.Clear();
				voiceAt = -10f;
				Call(what);
			}
		}
	}

	private void Feel(string what)
	{
		switch (what)
		{
		case "kill":
			goalReward += 1f;
			break;
		case "prop":
			goalReward += 0.15f;
			break;
		case "armed":
			goalReward += 0.4f;
			break;
		case "reloaded":
			goalReward += 0.15f;
			break;
		case "found":
			goalReward += 0.4f;
			break;
		case "new place":
			goalReward += 0.08f;
			break;
		case "caught up":
			goalReward += 0.15f;
			break;
		case "in cover":
			goalReward += 0.3f;
			break;
		case "close call":
			goalReward -= 0.5f;
			break;
		case "in the way":
			goalReward -= 0.2f;
			break;
		}
	}

	private void Sense()
	{
		Transform head = Head;
		if (head == null)
		{
			return;
		}
		Vector3 position = head.position;
		hostiles.Clear();
		if (!enemy)
		{
			hostiles.AddRange(Targets.Hostiles());
		}
		else
		{
			if (Targets.PlayerTarget(out var t))
			{
				hostiles.Add(t);
			}
			hostiles.AddRange(Targets.OtherPlayers());
		}
		hostiles.AddRange(Targets.OtherSide(rig));
		seenEnemies.Clear();
		nearestSeen = default(Targets.Target);
		mostDangerous = default(Targets.Target);
		nearestSeenDistance = 999f;
		nearestAnyDistance = 999f;
		float num = -1f;
		foreach (Targets.Target hostile in hostiles)
		{
			Vector3 point = hostile.Point;
			float magnitude = (point - position).magnitude;
			if (magnitude < nearestAnyDistance)
			{
				nearestAnyDistance = magnitude;
				nearestAnyAt = point;
			}
			if (!(magnitude > Tuning.F("brain.see.far", 55f)))
			{
				System.IntPtr key = ((hostile.transform != null) ? hostile.transform.Pointer : System.IntPtr.Zero);
				float value;
				if (Targets.InSight(position, point, hostile.transform, rig))
				{
					sightAt[key] = Time.time;
				}
				else if (!sightAt.TryGetValue(key, out value) || Time.time - value > Tuning.F("brain.sight", 1.3f))
				{
					continue;
				}
				seenEnemies.Add(point);
				if (magnitude < nearestSeenDistance)
				{
					nearestSeenDistance = magnitude;
					nearestSeen = hostile;
				}
				float num2 = Tactics.Danger(hostile, position);
				if (num2 > num)
				{
					num = num2;
					mostDangerous = hostile;
				}
			}
		}
		ObeyOrder(position);
		if (nearestSeen.transform != null)
		{
			if (!wasSeen && goal == Goal.Investigate)
			{
				Feel("found");
			}
			lastEnemyAt = mostDangerous.Point;
			lastEnemyVelocity = Tactics.Velocity(mostDangerous);
			lastEnemyTime = Time.time;
		}
		if (nearestSeen.transform != null && !wasSeen)
		{
			Call("spot");
		}
		wasSeen = nearestSeen.transform != null;
		if (Time.time - PlayerWatch.FiredAt < 0.5f && nearestSeen.transform == null)
		{
			lookAt = PlayerWatch.ShotAt;
			lookUntil = Time.time + 2.5f;
		}
		if (nearestAnyDistance < 1.6f && Time.time - closeCallAt > 2.5f)
		{
			closeCallAt = Time.time;
			Feel("close call");
			Knowledge.CloseCall();
		}
		if (visited.Add(Cell(Feet)) && goal == Goal.Explore)
		{
			Feel("new place");
		}
		if (engaged.transform != null && !Targets.Alive(engaged))
		{
			if (Time.time - engagedAt < 2f)
			{
				if (engaged.prop != null)
				{
					Feel("prop");
				}
				else
				{
					Feel("kill");
					if (seenEnemies.Count <= 1)
					{
						Call("clear");
					}
					Knowledge.KilledWith(heldBarcode, (engaged.Point - position).magnitude);
				}
			}
			engaged = default(Targets.Target);
		}
		string text = (Armed ? BarcodeOf(gun) : null);
		if (text != heldBarcode)
		{
			if (heldBarcode != null)
			{
				Knowledge.HeldGun(heldBarcode, Time.time - heldSince);
			}
			if (text != null && heldBarcode == null)
			{
				Feel("armed");
			}
			heldBarcode = text;
			heldSince = Time.time;
		}
		float num3 = ((Player.Head != null) ? Flat(Player.Head.position - position).magnitude : 0f);
		seen[0] = 1f;
		seen[1] = ((nearestSeen.transform != null) ? 1f : 0f);
		seen[2] = ((nearestSeenDistance < 5f) ? 1f : 0f);
		seen[3] = Mathf.Clamp01((float)seenEnemies.Count / 4f);
		seen[4] = (Armed ? 1f : 0f);
		seen[5] = ((Armed && AmmoFraction() < 0.3f) ? 1f : 0f);
		seen[6] = ((num3 > 10f) ? 1f : 0f);
		seen[7] = ((nearestSeen.transform == null && Time.time - lastEnemyTime < 25f) ? 1f : 0f);
		seen[8] = ((Time.time - closeCallAt < 5f) ? 1f : 0f);
		seen[9] = (PlayerWatch.Fighting ? 1f : 0f);
	}

	private static long Cell(Vector3 p)
	{
		long num = Mathf.FloorToInt(p.x / 5f);
		long num2 = Mathf.FloorToInt(p.z / 5f);
		return (num << 32) ^ (num2 & 0xFFFFFFFFu);
	}

	private static string BarcodeOf(Gun g)
	{
		try
		{
			Poolee componentInParent = g.GetComponentInParent<Poolee>();
			return (componentInParent != null && componentInParent.SpawnableCrate != null) ? componentInParent.SpawnableCrate.Barcode.ID : g.name;
		}
		catch (System.Exception)
		{
			return g.name;
		}
	}

	private float AmmoFraction()
	{
		try
		{
			int num = ((gun.defaultMagazine != null) ? Mathf.Max(1, gun.defaultMagazine.rounds) : 15);
			return Mathf.Clamp01((float)gun.AmmoCount() / (float)num);
		}
		catch (System.Exception)
		{
			return 1f;
		}
	}

	private float Fit(Goal g, out string reason)
	{
		bool flag = nearestSeen.transform != null;
		bool flag2 = !flag && Time.time - lastEnemyTime < 25f;
		float num = ((Player.Head != null && Head != null) ? Flat(Player.Head.position - Head.position).magnitude : 0f);
		bool flag3 = Armed && AmmoFraction() < 0.3f;
		reason = "";
		switch (g)
		{
		case Goal.Fight:
		{
			reason = (flag ? ("enemy " + nearestSeenDistance.ToString("0") + " m" + ((seenEnemies.Count > 1) ? (", " + seenEnemies.Count + " seen") : "")) : "");
			if (flag && Armed)
			{
				return 0.9f;
			}
			float num2 = ((coverPhase != 0) ? Tuning.F("cover.keep", 15f) : Tuning.F("brain.keep", 4.5f));
			if (!Armed || !(Time.time - lastEnemyTime < num2))
			{
				return 0f;
			}
			return 0.72f;
		}
		case Goal.Retreat:
			reason = (Reloading ? "too close to reload" : "too many");
			if (nearestAnyDistance < 3f && Armed && Reloading)
			{
				return 0.95f;
			}
			if (nearestAnyDistance < 3f && !Armed && seenEnemies.Count >= 3)
			{
				return 0.9f;
			}
			if (!(nearestAnyDistance < 1.8f) || !Armed)
			{
				return 0f;
			}
			return 0.6f;
		case Goal.TakeCover:
		{
			if (Tuning.F("cover.on", 1f) > 0.5f)
			{
				reason = "";
				return 0f;
			}
			reason = (Reloading ? "reloading" : (flag3 ? "low on ammo" : "outnumbered"));
			bool flag4 = Reloading || flag3 || seenEnemies.Count >= 3 || Time.time - closeCallAt < 4f;
			if (!flag4 && WantsCover())
			{
				flag4 = true;
				reason = ((Time.time - hurtAt < 3f) ? "hit, getting behind something" : "getting behind something");
			}
			if (!(flag && Armed && flag4) || !(nearestSeenDistance > 3.5f))
			{
				return 0f;
			}
			return 0.8f;
		}
		case Goal.ArmUp:
			reason = ((flag || flag2) ? "needs a gun" : "wants a gun");
			if (meleeWanted && Playing)
			{
				return 0.92f;
			}
			if (Assisting)
			{
				return 0f;
			}
			if (Armed || MeleeArmed)
			{
				return 0f;
			}
			if (HasStowed)
			{
				if (!flag)
				{
					return 0f;
				}
				return 0.97f;
			}
			if (flag && nearestSeenDistance < Tuning.F("brain.tooclose", 2.2f) && seenEnemies.Count < 3)
			{
				return 0f;
			}
			if (Time.time < noGunUntil)
			{
				return 0f;
			}
			if (!(flag || flag2))
			{
				if (hostiles.Count <= 0)
				{
					return 0.25f;
				}
				return 0.6f;
			}
			return 0.85f;
		case Goal.Investigate:
			reason = (enemy ? "after you" : "where it saw one");
			if (enemy && Hunted != null && Head != null && (Hunted.position - Head.position).magnitude < Tuning.F("enemy.hunt", 90f))
			{
				return 0.62f;
			}
			if (!flag2)
			{
				return 0f;
			}
			return 0.6f;
		case Goal.Explore:
			reason = "somewhere new";
			return 0.3f;
		case Goal.Follow:
			reason = "keeping up with you";
			if (BotManager.StayNear)
			{
				if (!(num > 18f))
				{
					if (!(num > 10f))
					{
						return 0.05f;
					}
					return 0.4f;
				}
				return 0.7f;
			}
			return 0f;
		case Goal.Guard:
			reason = (PlayerWatch.Fighting ? "watching your back" : "staying by you");
			if (!Armed || !(num < 25f))
			{
				return 0f;
			}
			if (!PlayerWatch.Fighting)
			{
				if (BotManager.StayNear)
				{
					if (hostiles.Count <= 0)
					{
						return 0.12f;
					}
					return 0.35f;
				}
				return 0f;
			}
			return 0.65f;
		case Goal.Brawl:
			if (nearestSeen.transform == null)
			{
				reason = "fists, nobody in sight";
				return 0f;
			}
			reason = "fists, " + nearestSeenDistance.ToString("0.0") + " m";
			if (MeleeArmed)
			{
				if (!flag)
				{
					return 0.5f;
				}
				return 0.95f;
			}
			if (Assisting && flag)
			{
				return 0.98f;
			}
			if (flag && !Armed && nearestSeenDistance < Tuning.F("brain.tooclose", 2.2f) && seenEnemies.Count < 3)
			{
				return 0.95f;
			}
			if (!flag || Armed || !(Time.time < noGunUntil) || seenEnemies.Count >= 3 || !(nearestSeenDistance < 20f))
			{
				return 0f;
			}
			return 0.7f;
		case Goal.Play:
			reason = "bored";
			if (Armed || hostiles.Count != 0)
			{
				return 0f;
			}
			return 0.25f;
		case Goal.Practice:
			reason = "target practice";
			if (!Armed || flag || !Targets.Practice)
			{
				return 0f;
			}
			return 0.3f;
		default:
			reason = "looking around";
			return 0.15f;
		}
	}

	private bool ClimbAfter(float dt)
	{
		if (InLesson || down || Reloading || Time.time < climbAfterAt || rig.physicsRig == null || rig.physicsRig.centerOfPressure == null)
		{
			return false;
		}
		Vector3 vector;
		if (enemy && Hunted != null)
		{
			vector = Hunted.position - Vector3.up * 1.4f;
		}
		else if (nearestSeen.transform != null)
		{
			vector = nearestSeen.Point - Vector3.up * 1.3f;
		}
		else
		{
			if (!(Time.time - lastEnemyTime < 20f) || !(lastEnemyTime > 0f))
			{
				return false;
			}
			vector = lastEnemyAt - Vector3.up * 1.3f;
		}
		Vector3 position = rig.physicsRig.centerOfPressure.position;
		float num = vector.y - position.y;
		Vector3 vector2 = Flat(vector - position);
		if (num < Tuning.F("climbafter.up", 1.2f) || vector2.magnitude > Tuning.F("climbafter.near", 12f) || vector2.magnitude < 0.05f)
		{
			return false;
		}
		Vector3 normalized = vector2.normalized;
		int num2;
		if (FindEdge(position, normalized, out var edge, out var inward, out var height) && height >= Tuning.F("climb.min", 1.2f))
		{
			num2 = ((height <= Tuning.F("climb.max", 2.5f)) ? 1 : 0);
			if (num2 != 0)
			{
				goto IL_01d6;
			}
		}
		else
		{
			num2 = 0;
		}
		if (Lessons.For("climb") == null)
		{
			goto IL_01d6;
		}
		float height2;
		Vector3 top;
		int num3 = (FindTallWall(position, normalized, 1.8f, out inward, out edge, out height2, out top) ? 1 : 0);
		goto IL_01d7;
		IL_01d6:
		num3 = 0;
		goto IL_01d7;
		IL_01d7:
		bool flag = (byte)num3 != 0;
		if (num2 == 0 && !flag)
		{
			if (vector2.magnitude > 1.2f && !Playing)
			{
				running = vector2.magnitude > 2.5f;
				Go(normalized, running ? MotionLibrary.RunPush : MotionLibrary.WalkPush, dt);
				return true;
			}
			return false;
		}
		if (WeaponInHand)
		{
			Stow();
		}
		if (Playing)
		{
			StopPlaying();
		}
		nav.SetGoal(vector);
		walk = Vector2.zero;
		climbLookAt = 0f;
		climbKeenAt = Time.time;
		if (TryStartClimb(normalized))
		{
			Lab.Event("climb: up after " + (enemy ? "you" : "it") + " (" + num.ToString("0.0") + " m above, " + vector2.magnitude.ToString("0.0") + " m off)");
			return true;
		}
		climbAfterAt = Time.time + 1.5f;
		return false;
	}

	private bool WantsCover()
	{
		if (Tuning.F("fight.cover", 1f) < 0.5f || !GunThreat(out var _))
		{
			return false;
		}
		if (Time.time - gunThreatAt > 20f)
		{
			coverWantAt = Time.time + UnityEngine.Random.Range(2.5f, 6f);
		}
		gunThreatAt = Time.time;
		if (Time.time < coverRestUntil)
		{
			return false;
		}
		if (coverReachedAt > 0f && Time.time - coverReachedAt > coverStay)
		{
			CoverDone(UnityEngine.Random.Range(4f, 9f));
			return false;
		}
		if (!(Time.time - hurtAt < 3f))
		{
			return Time.time > coverWantAt;
		}
		return true;
	}

	private void CoverDone(float rest)
	{
		coverRestUntil = Time.time + rest;
		coverWantAt = coverRestUntil + UnityEngine.Random.Range(0f, 5f);
		coverStay = UnityEngine.Random.Range(1.5f, 3.5f);
	}

	private void Think()
	{
		Goal goal = this.goal;
		float num = float.MinValue;
		string text = "";
		foreach (Goal value in System.Enum.GetValues(typeof(Goal)))
		{
			string reason;
			float num2 = Fit(value, out reason);
			if (!(num2 <= 0f))
			{
				float num3 = Knowledge.Worth(value.ToString(), seen);
				float num4 = num2 * (1f + 0.6f * num3) + UnityEngine.Random.Range(-0.03f, 0.03f);
				if (value == this.goal)
				{
					num4 += ((Time.time - goalSince < 2.5f) ? 0.25f : 0.08f);
				}
				if (num4 > num)
				{
					num = num4;
					goal = value;
					text = reason;
				}
			}
		}
		bool flag = (this.goal == Goal.TakeCover || this.goal == Goal.Retreat) && Time.time - goalSince < Tuning.F("brain.settle", 1.4f);
		if (goal != this.goal && Time.time - goalSince < Tuning.F("brain.stick", 2.6f) && num < 0.93f && (Fit(this.goal, out var _) > 0f || flag))
		{
			goal = this.goal;
		}
		else
		{
			why = text;
		}
		if (goal != this.goal)
		{
			SwitchGoal(goal);
		}
		else if (Time.time - windowSince > 3f)
		{
			LearnWindow();
		}
	}

	private void LearnWindow()
	{
		float num = Mathf.Max(Time.time - windowSince, 0.5f);
		float outcome = goalReward / num * 10f - 0.02f * num / 10f;
		Knowledge.Learn(goal.ToString(), windowSeen, outcome);
		goalReward = 0f;
		windowSince = Time.time;
		System.Array.Copy(seen, windowSeen, seen.Length);
	}

	private void SwitchGoal(Goal next)
	{
		Lab.Event("goal " + goal.ToString() + " -> " + next);
		LearnWindow();
		if (goal == Goal.TakeCover && Time.time > coverRestUntil)
		{
			CoverDone(UnityEngine.Random.Range(6f, 12f));
		}
		goal = next;
		goalSince = Time.time;
		hasGoalPlace = false;
		coverReachedAt = -1f;
		nav.Clear();
		following = false;
		if (next != Goal.Play && Playing && !Armed && !MeleeArmed && !meleeWanted)
		{
			StopPlaying();
		}
	}

	private void BrainStep(Transform you, Transform head, float dt)
	{
		if (Time.time >= thinkAt)
		{
			thinkAt = Time.time + Tuning.F("brain.think", 0.15f);
			Sense();
			Think();
		}
		brainTarget = ((goal == Goal.Fight || goal == Goal.TakeCover || goal == Goal.Guard) ? mostDangerous : default(Targets.Target));
		running = false;
		if (climbing)
		{
			handOverride = false;
			leftOverride = false;
			ClimbStep(dt);
			Show();
			return;
		}
		if (ClimbAfter(dt))
		{
			Show();
			return;
		}
		if (!Playing)
		{
			handOverride = false;
			leftOverride = false;
		}
		fists = false;
		if (WeaponInHand && nearestSeen.transform == null && Time.time - lastEnemyTime > Tuning.F("stow.after", 7f) && Time.time - drawnAt > 3f && !Reloading && (!Targets.Practice || enemy))
		{
			Stow();
		}
		if (Armed && play == Play.Shoot)
		{
			if (hostiles.Count > 0)
			{
				shootUntil = float.MaxValue;
			}
			PlaceFor(goal);
			PlayStep(you, head, dt);
			StayOutOfTheWay(dt);
			Show();
			return;
		}
		if (Playing && (goal == Goal.Play || goal == Goal.ArmUp || play == Play.Shoot || play == Play.Reach || play == Play.Melee || meleeWanted))
		{
			PlayStep(you, head, dt);
			Show();
			return;
		}
		switch (goal)
		{
		case Goal.ArmUp:
			if (HasStowed)
			{
				if (!Draw())
				{
					DropStowed();
				}
			}
			else if (!Playing && !GetAWeapon())
			{
				noGunUntil = Time.time + Tuning.F("brain.nogun", 20f);
				Lab.Event("brain: no weapon to be had, its fists for a while");
				thinkAt = 0f;
			}
			else if (Playing)
			{
				PlayStep(you, head, dt);
			}
			break;
		case Goal.Explore:
		case Goal.Play:
			Life(you, head, dt);
			why = ActLabel();
			break;
		case Goal.Brawl:
			Brawl(head, dt);
			break;
		case Goal.Follow:
			Follow(you, head, dt);
			if (!following && Time.time - goalSince > 1f)
			{
				Feel("caught up");
			}
			break;
		case Goal.Retreat:
		case Goal.TakeCover:
		case Goal.Investigate:
		case Goal.Guard:
			if (goal == Goal.Investigate && enemy && hasGoalPlace && Hunted != null && (Hunted.position - huntedAt).magnitude > 2.5f && !climbing)
			{
				hasGoalPlace = false;
				nav.Clear();
			}
			PlaceFor(goal);
			if (hasGoalPlace)
			{
				if (!nav.HasGoal)
				{
					nav.SetGoal(goalPlace);
				}
				running = goal == Goal.Retreat || goal == Goal.TakeCover || ((goal == Goal.Investigate || goal == Goal.ArmUp || goal == Goal.Guard || goal == Goal.Fight) && nav.HasGoal && Flat(nav.Goal - Feet).magnitude > 6f);
				if (Travel(running ? MotionLibrary.RunPush : MotionLibrary.WalkPush, 1f, dt) || nav.StuckCount >= 3)
				{
					Arrived();
				}
			}
			else
			{
				walk = Vector2.zero;
			}
			break;
		default:
			Life(you, head, dt);
			why = ActLabel();
			break;
		}
		StayOutOfTheWay(dt);
		Show();
	}

	private void Arrived()
	{
		hasGoalPlace = false;
		nav.Clear();
		if (goal == Goal.Investigate)
		{
			lastEnemyTime = -100f;
		}
		if (goal == Goal.TakeCover && coverReachedAt < 0f)
		{
			coverReachedAt = Time.time;
			Squat(coverStay + 1f, 0.65f);
			Feel("in cover");
		}
	}

	private void PlaceFor(Goal g)
	{
		if (hasGoalPlace)
		{
			return;
		}
		Vector3 vector = Feet;
		switch (g)
		{
		case Goal.Retreat:
		{
			Vector3 vector5 = Flat(vector - nearestAnyAt);
			vector5 = ((vector5.sqrMagnitude < 0.01f) ? (Quaternion.Euler(0f, bodyYaw + 180f, 0f) * Vector3.forward) : vector5.normalized);
			hasGoalPlace = Navigator.RandomPlace(vector + vector5 * 7f, 0f, 2f, out goalPlace);
			break;
		}
		case Goal.TakeCover:
		{
			if (coverReachedAt < 0f && Tactics.FindCover(vector, seenEnemies, out var cover))
			{
				goalPlace = cover;
				hasGoalPlace = true;
			}
			else if (coverReachedAt < 0f && Time.time > coverRestUntil)
			{
				CoverDone(5f);
			}
			break;
		}
		case Goal.Investigate:
			if (enemy && nearestSeen.transform == null && Hunted != null)
			{
				goalPlace = Hunted.position - Vector3.up * 1.4f + OwnSide(1.4f);
				hasGoalPlace = true;
				huntedAt = Hunted.position;
			}
			else
			{
				goalPlace = lastEnemyAt + Vector3.ClampMagnitude(lastEnemyVelocity * 2f, 4f) - Vector3.up * 1.2f;
				hasGoalPlace = true;
			}
			break;
		case Goal.Guard:
		{
			Transform head = Player.Head;
			if (head != null)
			{
				Vector3 vector2 = Flat(PlayerWatch.Fighting ? PlayerWatch.AimAlong : head.forward);
				vector2 = ((vector2.sqrMagnitude < 0.01f) ? Vector3.forward : vector2.normalized);
				Vector3 vector3 = Vector3.Cross(Vector3.up, vector2) * ((Vector3.Dot(vector - head.position, Vector3.Cross(Vector3.up, vector2)) >= 0f) ? 1f : (-1f));
				Vector3 vector4 = head.position - Vector3.up * 1.4f + vector3 * 2.2f - vector2 * 1.2f;
				if (Flat(vector4 - vector).magnitude > 1.5f && Navigator.RandomPlace(vector4, 0f, 0.8f, out var place2))
				{
					goalPlace = place2;
					hasGoalPlace = true;
				}
			}
			break;
		}
		case Goal.Explore:
		case Goal.Practice:
		{
			for (int i = 0; i < 8; i++)
			{
				if (hasGoalPlace)
				{
					break;
				}
				if (Navigator.RandomPlace(vector, 5f, 18f, out var place) && (!visited.Contains(Cell(place)) || i == 7))
				{
					goalPlace = place;
					hasGoalPlace = true;
				}
			}
			break;
		}
		case Goal.ArmUp:
		case Goal.Follow:
		case Goal.Play:
			break;
		}
	}

	private void StayOutOfTheWay(float dt)
	{
		Transform head = Head;
		if (!(head == null) && Tactics.InPlayersLine(head.position - Vector3.up * 0.4f, out var outOfIt))
		{
			Vector3 vector = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * outOfIt;
			walk = new Vector2(vector.x, vector.z) * MotionLibrary.WalkPush;
			if (Time.time - closeCallAt > 1f && UnityEngine.Random.value < dt)
			{
				Feel("in the way");
			}
		}
	}

	private Vector3 Lookout(Transform head)
	{
		if (Time.time < lookUntil)
		{
			return lookAt;
		}
		if (goal == Goal.Guard && PlayerWatch.Fighting)
		{
			return head.position - Flat(PlayerWatch.AimAlong).normalized * 8f - Vector3.up * 0.8f;
		}
		float num = Mathf.Sin((Time.time - born) * 0.55f) * 40f;
		return head.position + Quaternion.Euler(0f, bodyYaw + num, 0f) * new Vector3(0f, -1.3f, 2.5f);
	}

	private void Show()
	{
		if (!BotManager.ShowThoughts)
		{
			if (thought != null)
			{
				thought.gameObject.SetActive(value: false);
			}
			return;
		}
		Transform head = Head;
		if (head == null)
		{
			return;
		}
		if (thought == null)
		{
			GameObject gameObject = new GameObject("ThirdEyeBotThought");
			thought = gameObject.AddComponent<TextMeshPro>();
			if (font == null)
			{
				foreach (TMP_FontAsset item in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
				{
					if (item != null)
					{
						font = item;
						break;
					}
				}
			}
			if (font != null)
			{
				thought.font = font;
			}
			thought.fontSize = 0.5f;
			thought.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
			thought.alignment = TextAlignmentOptions.Bottom;
			thought.enableWordWrapping = false;
			thought.richText = true;
			thought.color = new Color(0.75f, 0.95f, 1f, 0.95f);
		}
		thought.gameObject.SetActive(value: true);
		Transform transform = thought.transform;
		transform.position = head.position + Vector3.up * 0.55f;
		if (Player.Head != null)
		{
			transform.rotation = Quaternion.LookRotation(transform.position - Player.Head.position, Vector3.up);
		}
		string text = goal.ToString() + ((why.Length > 0) ? (": " + why) : "");
		if (Reloading)
		{
			text = "Reloading (" + reload.ToString() + ")";
		}
		float num = Knowledge.Worth(goal.ToString(), seen);
		thought.text = text + "\n<size=60%>expects " + num.ToString("+0.00;-0.00", CultureInfo.InvariantCulture) + (Armed ? ("  ammo " + Mathf.RoundToInt(AmmoFraction() * 100f) + "%") : "") + "</size>";
	}

	private void HideThought()
	{
		if (thought != null)
		{
			UnityEngine.Object.Destroy(thought.gameObject);
		}
		thought = null;
	}

	private bool FreeToCatch()
	{
		Hand rightHand = RightHand;
		if (!dead && !down && !surrendered && !Puppet && !climbing && !Playing && gun == null && rightHand != null && rightHand.m_CurrentAttachedGO == null && target.transform == null && !InLesson && !sitting && rescuing == null)
		{
			if (mode != Mode.Brain && mode != Mode.Wander && mode != 0)
			{
				return mode == Mode.Follow;
			}
			return true;
		}
		return false;
	}

	private Bot CatchPartner(Transform head)
	{
		if (head == null)
		{
			return null;
		}
		Bot result = null;
		float num = Tuning.F("catch.far", 14f);
		foreach (Bot item in BotManager.All)
		{
			if (item != this && item.enemy == enemy && item.FreeToCatch() && !(item.Head == null))
			{
				float magnitude = Flat(item.Head.position - head.position).magnitude;
				if (magnitude > Tuning.F("catch.near", 1.5f) && magnitude < num)
				{
					num = magnitude;
					result = item;
				}
			}
		}
		return result;
	}

	private string CatchWhyNot(Transform head)
	{
		string text = "";
		foreach (Bot item in BotManager.All)
		{
			if (item != this)
			{
				float num = ((item.Head != null && head != null) ? Flat(item.Head.position - head.position).magnitude : (-1f));
				text = text + item.Name + ": " + num.ToString("0.0") + " m, " + ((item.enemy != enemy) ? "other side" : ((!item.FreeToCatch()) ? ("busy (playing " + item.play.ToString() + ", gun " + (item.gun != null) + ", hand " + ((item.RightHand != null && item.RightHand.m_CurrentAttachedGO != null) ? "full" : "empty") + ", mode " + item.mode.ToString() + ")") : "free")) + "; ";
			}
		}
		if (text.Length <= 0)
		{
			return "no other bots";
		}
		return text;
	}

	private bool StartCatchThrow(Transform head, Bot partner, bool newGame)
	{
		if (partner == null || head == null || partner.Head == null)
		{
			return false;
		}
		if (newGame)
		{
			catchThrowsLeft = UnityEngine.Random.Range(3, 7);
			Lab.Event("catch: a game of catch with " + partner.Name + " (" + catchThrowsLeft + " throws)");
		}
		catchWith = partner;
		partner.catchWith = this;
		partner.catchThrowsLeft = catchThrowsLeft;
		partner.WaitToCatch();
		then = Then.ToPartner;
		throwFar = 0f;
		throwDir = Flat(partner.Head.position - head.position).normalized;
		Next(Play.WindUp);
		return true;
	}

	private void WaitToCatch()
	{
		StopPlaying();
		play = Play.Catch;
		playAt = Time.time;
		catchWaitSince = Time.time;
		catchComing = null;
		catchComingGrip = null;
		catchFlyingSince = -1f;
		nav.Clear();
	}

	private void ComingToYou(Rigidbody thing, Grip itsGrip)
	{
		catchComing = thing;
		catchComingGrip = itsGrip;
		catchFlyingSince = Time.time;
	}

	private Vector3 ThrowToPartner(Rigidbody thing)
	{
		Transform transform = ((catchWith != null) ? catchWith.Head : null);
		if (transform == null)
		{
			return throwDir * 6f + Vector3.up * 3f;
		}
		Vector3 vector = transform.position - Vector3.up * Tuning.F("catch.chest", 0.35f);
		float t = Mathf.Clamp((vector - thing.worldCenterOfMass).magnitude / Tuning.F("catch.pace", 6f), Tuning.F("catch.air", 0.75f), 1.3f);
		return Lob(thing.worldCenterOfMass, vector, t);
	}

	private bool CatchStep(Transform head, float dt)
	{
		Bot bot = catchWith;
		Hand rightHand = RightHand;
		Transform vrRoot = controls.vrRoot;
		if (bot == null || bot.dead || head == null || rightHand == null || vrRoot == null)
		{
			return EndCatch("the other one is gone");
		}
		walk = Vector2.zero;
		running = false;
		Transform head2 = bot.Head;
		if (head2 != null)
		{
			Face(head2.position, dt, 200f);
		}
		handOverride = true;
		playHeight = standingHeight;
		Vector3 vector = new Vector3(0.18f, standingHeight - 0.45f, 0.35f);
		if (catchComing == null)
		{
			handAt = vector;
			handTurn = Quaternion.Euler(-10f, -20f, 0f);
			playPitch = ((head2 != null) ? LookPitch(head2, head) : 0f);
			if (Time.time - catchWaitSince > Tuning.F("catch.wait", 15f))
			{
				return EndCatch("no throw came");
			}
			return true;
		}
		Vector3 worldCenterOfMass = catchComing.worldCenterOfMass;
		Vector3 vector2 = vrRoot.InverseTransformPoint(worldCenterOfMass);
		Vector3 vector3 = new Vector3(0.18f, standingHeight - 0.25f, 0f);
		Vector3 vector4 = vector2 - vector3;
		handAt = vector3 + Vector3.ClampMagnitude(vector4, Tuning.F("catch.arm", 0.65f));
		handTurn = Quaternion.Euler(-20f, -20f, 0f);
		playPitch = Mathf.Clamp((0f - Mathf.Atan2(worldCenterOfMass.y - head.position.y, Mathf.Max(0.3f, Flat(worldCenterOfMass - head.position).magnitude))) * 57.29578f, -50f, 60f);
		float magnitude = (rightHand.transform.position - worldCenterOfMass).magnitude;
		if (magnitude < Tuning.F("catch.reach", 0.45f) && catchComingGrip != null && !catchComingGrip.HasAttachedHands() && Grab(rightHand, catchComingGrip, null))
		{
			catchesMade++;
			item = catchComing;
			this.grip = catchComingGrip;
			catchComing = null;
			catchThrowsLeft--;
			Lab.Event("catch: caught it (" + magnitude.ToString("0.00") + " m from its hand; " + catchesMade + " caught, " + catchesMissed + " missed)");
			holdFor = UnityEngine.Random.Range(0.6f, 1.5f);
			Next(Play.Hold);
			return true;
		}
		if (Time.time - catchFlyingSince > Tuning.F("catch.flight", 2.5f) || (catchComing.velocity.magnitude < 0.5f && Time.time - catchFlyingSince > 0.5f))
		{
			catchesMissed++;
			catchThrowsLeft--;
			Lab.Event("catch: missed it (closest " + magnitude.ToString("0.00") + " m; " + catchesMade + " caught, " + catchesMissed + " missed)");
			Rigidbody rigidbody = catchComing;
			Grip grip = catchComingGrip;
			catchComing = null;
			if (catchThrowsLeft <= 0 || rigidbody == null || grip == null)
			{
				return EndCatch("that was the last throw");
			}
			item = rigidbody;
			this.grip = grip;
			play = Play.GoTo;
			playAt = Time.time;
			nav.SetGoal(rigidbody.worldCenterOfMass);
		}
		return true;
	}

	private bool EndCatch(string why)
	{
		Lab.Event("catch: over (" + why + ")");
		if (catchWith != null && catchWith.catchWith == this)
		{
			catchWith.catchWith = null;
		}
		catchWith = null;
		catchComing = null;
		catchThrowsLeft = 0;
		play = Play.None;
		handOverride = false;
		return false;
	}

	public void LabCatch()
	{
		StopPlaying();
		catchNext = true;
		if (!FindSomethingToPlayWith())
		{
			catchNext = false;
			Lab.Event("catch: nothing near to throw");
		}
	}

	private bool TryStartClimb(Vector3 dir)
	{
		if (climbing || Time.time < climbLookAt || Playing)
		{
			return false;
		}
		climbLookAt = Time.time + 0.4f;
		bool climbKeen = ClimbKeen;
		PhysicsRig physicsRig = rig.physicsRig;
		Vector3 vector = ((physicsRig != null && physicsRig.centerOfPressure != null) ? physicsRig.centerOfPressure.position : Feet);
		bool flag = nav.HasGoal && nav.Goal.y - vector.y > 0.8f;
		if (!climbKeen || !flag)
		{
			if (!climbKeen && nav.StuckCount >= 2)
			{
				climbLookAt = Time.time + 12f;
				nav.GiveUp();
			}
			return false;
		}
		Vector3 vector2 = (nav.HasGoal ? Flat(nav.Goal - vector) : Vector3.zero);
		Vector3 edge;
		Vector3 inward;
		float height;
		bool flag2 = FindEdge(vector, dir, out edge, out inward, out height);
		if (!flag2 && flag && vector2.magnitude > 0.3f && vector2.magnitude < 10f)
		{
			flag2 = FindEdge(vector, vector2.normalized, out edge, out inward, out height);
		}
		if ((!flag2 || height > Tuning.F("climb.max", 2.5f)) && flag && FindTallWall(vector, (vector2.sqrMagnitude > 0.09f) ? vector2.normalized : dir, 1.8f, out var foot, out var inward2, out var height2, out var _))
		{
			if (vector.y + height2 > nav.Goal.y + 1f)
			{
				climbLookAt = Time.time + 3f;
				return false;
			}
			if (!climbKeen && height2 >= Tuning.F("climb.roam.most", 5f))
			{
				Lab.Event("climb: a " + height2.ToString("0.0") + " m wall, too high to bother with while going about: somewhere else");
				climbLookAt = Time.time + 15f;
				nav.GiveUp();
				return false;
			}
			if (!climbKeen && UnityEngine.Random.value > Tuning.F("climb.roam.chance", 0.3f))
			{
				climbLookAt = Time.time + 12f;
				nav.GiveUp();
				return false;
			}
			if (ClimbWithTool(foot, inward2, height2))
			{
				return true;
			}
		}
		if (!flag2)
		{
			return false;
		}
		if (height < Tuning.F("climb.min", 1.2f) || height > Tuning.F("climb.max", 2.5f))
		{
			return false;
		}
		if (edge.y > nav.Goal.y + 1f)
		{
			Lab.Event("climb: that wall is " + (edge.y - nav.Goal.y).ToString("0.0") + " m higher than where it is going: not climbed");
			climbLookAt = Time.time + 3f;
			return false;
		}
		if (!climbKeen && UnityEngine.Random.value > Tuning.F("climb.roam.chance", 0.3f))
		{
			Lab.Event("climb: a " + height.ToString("0.0") + " m wall, not worth it while going about: somewhere else");
			climbLookAt = Time.time + 12f;
			nav.GiveUp();
			return false;
		}
		if (((LeftHand != null && LeftHand.m_CurrentAttachedGO != null) || (RightHand != null && RightHand.m_CurrentAttachedGO != null)) && (!WeaponInHand || !Stow()))
		{
			return false;
		}
		if ((edge - climbLastEdge).magnitude < 1.5f || Time.time - climbUpAt < 10f)
		{
			if (++climbTries > 3)
			{
				climbTries = 0;
				climbLookAt = Time.time + (climbKeen ? 1.5f : 20f);
				Lab.Event(climbKeen ? "climb: not up yet, at it again in a moment" : "climb: gave up on that wall for a while");
				if (!climbKeen)
				{
					nav.GiveUp();
				}
				return false;
			}
		}
		else
		{
			climbTries = 1;
		}
		if (!climbKeen && SelfTaught.ClimbFails(edge) >= 2 && UnityEngine.Random.value > 0.15f)
		{
			climbLookAt = Time.time + 6f;
			return false;
		}
		if (SomeoneClimbingAt(edge, Tuning.F("climb.apart", 1.8f)))
		{
			climbLookAt = Time.time + 1f;
			return false;
		}
		climbLastEdge = edge;
		climbing = true;
		climbLifting = false;
		climbPhase = 0;
		climbEdge = edge;
		climbIn = inward;
		climbHeight = height;
		climbFacing = Quaternion.LookRotation(inward, Vector3.up);
		climbSince = Time.time;
		climbMove = ClimbMoves.Pick(height);
		Lab.Event("climb: up a " + height.ToString("0.00") + " m wall (" + ((climbMove != null) ? "the player's way" : "its own way") + ")");
		return true;
	}

	public bool ClimbingNear(Vector3 at, float within)
	{
		if (climbing && Flat(climbEdge - at).magnitude < within)
		{
			return true;
		}
		if (InLesson && cloneLesson != null && cloneLesson.purpose == "climb" && lessonPurposeful)
		{
			return Flat(lessonWallFoot - at).magnitude < within;
		}
		return false;
	}

	private bool SomeoneClimbingAt(Vector3 at, float within)
	{
		foreach (Bot item in BotManager.All)
		{
			if (item != this && !item.dead && item.ClimbingNear(at, within))
			{
				return true;
			}
		}
		return false;
	}

	private bool FindEdge(Vector3 feet, Vector3 dir, out Vector3 edge, out Vector3 inward, out float height)
	{
		edge = (inward = Vector3.zero);
		height = 0f;
		if (!Physics.Raycast(feet + Vector3.up * 0.6f, dir, out var hitInfo, 1.6f, -1, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		if (hitInfo.collider.GetComponentInParent<RigManager>() != null || hitInfo.collider.attachedRigidbody != null)
		{
			return false;
		}
		inward = -Flat(hitInfo.normal);
		if (inward.sqrMagnitude < 0.01f)
		{
			return false;
		}
		inward.Normalize();
		if (!Physics.Raycast(hitInfo.point + inward * 0.3f + Vector3.up * 3.3f, Vector3.down, out var hitInfo2, 3.6f, -1, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		if (hitInfo2.normal.y < 0.7f || hitInfo2.point.y < feet.y + 0.5f)
		{
			return false;
		}
		if (Physics.Raycast(hitInfo2.point + Vector3.up * 0.05f, Vector3.up, 1.5f, -1, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		if (!Physics.Raycast(hitInfo.point + inward * 0.9f + Vector3.up * 3.3f, Vector3.down, out var hitInfo3, 3.6f, -1, QueryTriggerInteraction.Ignore) || Mathf.Abs(hitInfo3.point.y - hitInfo2.point.y) > 0.3f)
		{
			return false;
		}
		height = hitInfo2.point.y - feet.y;
		edge = new Vector3(hitInfo.point.x, hitInfo2.point.y, hitInfo.point.z) + inward * 0.04f;
		return true;
	}

	private void ClimbStep(float dt)
	{
		if (Time.time - climbSince > 16f)
		{
			StopClimb("took too long");
			return;
		}
		switch (climbPhase)
		{
		case 0:
		{
			Vector3 vector = ((rig.physicsRig != null && rig.physicsRig.centerOfPressure != null) ? rig.physicsRig.centerOfPressure.position : Feet);
			Vector3 vector2 = Flat(climbEdge - vector);
			if (vector2.magnitude > Tuning.F("climb.near", 0.45f) && Time.time - climbSince < 4f)
			{
				Go(vector2.normalized, MotionLibrary.WalkPush * 0.85f, dt);
				break;
			}
			walk = Vector2.zero;
			bodyYaw = Recorder.Yaw(climbIn);
			climbPhase = 1;
			climbT = 0f;
			climbReach = 0f;
			climbWait = 0f;
			climbFrom = pose;
			climbStartHead = ((Head != null) ? Pose.Of(Head) : new Pose(climbEdge, climbFacing));
			climbStartLeft = ((LeftHand != null) ? Pose.Of(LeftHand.transform) : new Pose(climbEdge, climbFacing));
			climbStartRight = ((RightHand != null) ? Pose.Of(RightHand.transform) : new Pose(climbEdge, climbFacing));
			break;
		}
		case 1:
		{
			walk = Vector2.zero;
			bodyYaw = Mathf.MoveTowardsAngle(bodyYaw, Recorder.Yaw(climbIn), 180f * dt);
			float num2 = ((climbMove != null) ? climbMove.Length : 2.6f);
			float num3 = Tuning.F("climb.reach", 0.5f);
			if (climbReach < num3)
			{
				climbReach += dt;
				ClimbAt(0f);
				float num4 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(climbReach / num3));
				climbHead = Pose.Lerp(climbStartHead, climbHead, num4);
				climbLeft = Pose.Lerp(climbStartLeft, climbLeft, num4);
				climbRight = Pose.Lerp(climbStartRight, climbRight, num4);
				bool flag = num4 > 0.8f;
				HoldWorld(0, flag && climbWantLeft, climbLeft.position);
				HoldWorld(1, flag && climbWantRight, climbRight.position);
				break;
			}
			ClimbAt(climbT);
			HoldWorld(0, climbWantLeft && !climbGliding, climbLeft.position);
			HoldWorld(1, climbWantRight && !climbGliding, climbRight.position);
			if (!climbGliding && (HandNearlyThere(0, climbWantLeft, climbLeft.position) || HandNearlyThere(1, climbWantRight, climbRight.position)))
			{
				climbWait += dt;
				if (climbWait > Tuning.F("climb.wait", 0.3f))
				{
					climbT += dt * Tuning.F("climb.speed", 0.7f);
				}
			}
			else
			{
				climbWait = 0f;
				climbT += dt * Tuning.F("climb.speed", 0.7f);
			}
			if (climbT >= num2)
			{
				HoldWorld(0, want: false, Vector3.zero);
				HoldWorld(1, want: false, Vector3.zero);
				climbPhase = 2;
				climbT = 0f;
				LegsUp(up: false);
				if (Head != null)
				{
					bodyYaw = Recorder.Yaw(climbIn);
				}
			}
			break;
		}
		case 2:
			climbT += dt;
			Go(climbIn, MotionLibrary.WalkPush * 0.7f, dt);
			if (climbT > 0.6f)
			{
				PhysicsRig physicsRig = rig.physicsRig;
				float num = ((physicsRig != null && physicsRig.centerOfPressure != null) ? (physicsRig.centerOfPressure.position.y - (climbEdge.y - climbHeight)) : 0f);
				StopClimb((num > climbHeight * 0.6f) ? "up" : "did not get up");
			}
			break;
		}
	}

	public void ClimbFixed(float dt)
	{
		if (climbing && climbHang && climbPhase == 1 && !climbGliding && rig.physicsRig != null && !Bad(climbHangFeet))
		{
			rig.Teleport(climbHangFeet);
		}
		if (!climbing || climbPhase != 1 || climbReach < Tuning.F("climb.reach", 0.5f))
		{
			return;
		}
		PhysicsRig physicsRig = rig.physicsRig;
		Rigidbody rigidbody = ((physicsRig != null && physicsRig.m_pelvis != null) ? physicsRig.m_pelvis.GetComponent<Rigidbody>() : null);
		if (rigidbody == null)
		{
			return;
		}
		if (!climbLifting)
		{
			climbLifting = true;
			climbGliding = false;
			climbLiftFrom = rigidbody.position;
			climbFeetFrom = (climbHang ? climbHangFeet : ((physicsRig.centerOfPressure != null) ? physicsRig.centerOfPressure.position : (rigidbody.position - Vector3.up * 0.95f)));
			climbGlide = climbFeetFrom;
			climbBodies.Clear();
			Rigidbody rigidbody2 = ((LeftHand != null) ? LeftHand.rb : null);
			Rigidbody rigidbody3 = ((RightHand != null) ? RightHand.rb : null);
			foreach (Rigidbody componentsInChild in physicsRig.GetComponentsInChildren<Rigidbody>())
			{
				if (componentsInChild != null && !componentsInChild.isKinematic && componentsInChild != rigidbody2 && componentsInChild != rigidbody3)
				{
					climbBodies.Add(componentsInChild);
				}
			}
		}
		LiftShape(climbT, out var up, out var over);
		float num = climbHeight + Tuning.F("climb.extra", 0.12f);
		float num2 = Flat(climbEdge - climbLiftFrom).magnitude + Tuning.F("climb.onto", 0.45f);
		Vector3 vector = climbLiftFrom + Vector3.up * (up * num) + climbIn * (over * num2);
		if (Bad(vector) || Bad(rigidbody.position))
		{
			StopClimb("its body went wild");
			return;
		}
		if (Time.time - climbLiftSaidAt > 0.5f)
		{
			climbLiftSaidAt = Time.time;
			Lab.Event("climb lift: hips " + (rigidbody.position.y - climbLiftFrom.y).ToString("0.00") + " m up of " + (up * num).ToString("0.00") + " wanted, " + Flat(rigidbody.position - climbLiftFrom).magnitude.ToString("0.00") + " m over of " + (over * num2).ToString("0.00") + ", hands holding " + climbHolding[0] + "/" + climbHolding[1] + ", from the wall " + Flat(climbEdge - rigidbody.position).magnitude.ToString("0.00") + " m");
		}
		if (Tuning.F("climb.glide", 1f) > 0.5f)
		{
			Vector3 vector2 = climbFeetFrom + Vector3.up * (up * num) + climbIn * (over * (Flat(climbEdge - climbFeetFrom).magnitude + Tuning.F("climb.onto", 0.45f)));
			if (!climbGliding && up > 0.02f)
			{
				climbGliding = true;
				Call("heave");
				climbGlideSince = Time.time;
				HoldWorld(0, want: false, Vector3.zero);
				HoldWorld(1, want: false, Vector3.zero);
			}
			if (climbGliding)
			{
				float num3 = Mathf.Clamp01((up - 0.25f) / 0.25f) * (1f - Mathf.Clamp01((over - 0.3f) / 0.4f));
				rig.remapHeptaRig._crouchTarget = Tuning.F("climb.tuck", -0.25f) * num3;
			}
			climbGlide = Vector3.MoveTowards(climbGlide, vector2, Tuning.F("climb.glide.step", 0.03f));
			if (climbGliding)
			{
				rig.Teleport(climbGlide);
			}
			return;
		}
		Vector3 force = Vector3.ClampMagnitude((vector - rigidbody.position) * Tuning.F("climb.pull", 8f) - rigidbody.velocity, Tuning.F("climb.step", 0.35f));
		foreach (Rigidbody climbBody in climbBodies)
		{
			if (climbBody != null)
			{
				climbBody.AddForce(force, ForceMode.VelocityChange);
			}
		}
	}

	private void LiftShape(float t, out float up, out float over)
	{
		if (climbMove == null || climbMove.frames.Count < 2)
		{
			up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.2f) / 1.3f));
			over = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 1.5f) / 0.9f));
			return;
		}
		System.Collections.Generic.List<ClimbMoves.Frame> frames = climbMove.frames;
		float length = climbMove.Length;
		float y = ClimbMoves.Read(frames[0].head).position.y;
		float num = y;
		float num2 = y;
		float num3 = length;
		foreach (ClimbMoves.Frame item in frames)
		{
			num = Mathf.Max(num, ClimbMoves.Read(item.head).position.y);
		}
		foreach (ClimbMoves.Frame item2 in frames)
		{
			float y2 = ClimbMoves.Read(item2.head).position.y;
			if (item2.t <= t)
			{
				num2 = Mathf.Max(num2, y2);
			}
			if (y2 >= y + (num - y) * 0.92f)
			{
				num3 = Mathf.Min(num3, item2.t);
			}
		}
		num3 = Mathf.Clamp(num3, Mathf.Min(0.6f, length * 0.4f), Mathf.Max(0.6f, length - Tuning.F("climb.over.time", 0.7f)));
		float num4 = ((num - y > 0.1f) ? Mathf.Clamp01((num2 - y) / ((num - y) * 0.92f)) : 0f);
		up = ((t >= num3) ? 1f : Mathf.Max(num4 * Mathf.Clamp01(t / num3 + 0.35f), Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / num3)) * 0.999f));
		up = Mathf.Clamp01(up);
		float num5 = num3 * Tuning.F("climb.over.lead", 0.22f);
		over = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - (num3 - num5)) / Mathf.Max(0.2f, length - num3 + num5)));
		over = Mathf.Min(over, Mathf.Clamp01((up - 0.9f) / 0.08f));
	}

	private bool HandNearlyThere(int which, bool want, Vector3 target)
	{
		Hand hand = ((which == 0) ? LeftHand : RightHand);
		if (want && !climbHolding[which] && hand != null)
		{
			return (hand.transform.position - target).magnitude < Tuning.F("climb.close", 0.45f);
		}
		return false;
	}

	public void LabClimb()
	{
		if (mode != Mode.Wander)
		{
			SetMode(Mode.Wander);
		}
		StopPlaying();
		Vector3 vector = ((rig.physicsRig != null && rig.physicsRig.centerOfPressure != null) ? rig.physicsRig.centerOfPressure.position : Feet);
		climbLookAt = 0f;
		climbTries = 0;
		if (FindHighGround(vector, out var top))
		{
			nav.Clear();
			nav.SetGoal(top);
			labClimbUntil = Time.time + 40f;
			act = Act.Climb;
			actUntil = Time.time + 40f;
			Lab.Event("climb: sent up to high ground " + Flat(top - vector).magnitude.ToString("0.0") + " m off, " + (top.y - vector.y).ToString("0.00") + " m up");
		}
		else
		{
			Lab.Event("climb: no wall it can climb within 15 m");
		}
	}

	private void LegsUp(bool up)
	{
		if (!up)
		{
			rig.remapHeptaRig._crouchTarget = 0f;
		}
	}

	private void ClimbAt(float t)
	{
		if (climbMove != null)
		{
			FromPlayer(t);
		}
		else
		{
			OwnClimb(t);
		}
	}

	private void StopClimb(string how)
	{
		climbHang = false;
		LegsUp(up: false);
		climbLifting = false;
		climbGliding = false;
		climbBodies.Clear();
		switch (how)
		{
		case "up":
			climbUpAt = Time.time;
			SelfTaught.Top(climbEdge);
			break;
		case "took too long":
		case "did not get up":
		case "its body went wild":
			SelfTaught.ClimbFailed(climbEdge);
			break;
		}
		HoldWorld(0, want: false, Vector3.zero);
		HoldWorld(1, want: false, Vector3.zero);
		climbing = false;
		walk = Vector2.zero;
		climbLookAt = Time.time + ((how == "up") ? 3f : 1.5f);
		nav.Clear();
		Lab.Event("climb: " + how);
	}

	private void FromPlayer(float t)
	{
		System.Collections.Generic.List<ClimbMoves.Frame> frames = climbMove.frames;
		int i;
		for (i = 0; i < frames.Count - 2 && frames[i + 1].t < t; i++)
		{
		}
		ClimbMoves.Frame frame = frames[i];
		ClimbMoves.Frame frame2 = frames[Mathf.Min(i + 1, frames.Count - 1)];
		float num = ((frame2.t > frame.t) ? Mathf.Clamp01((t - frame.t) / (frame2.t - frame.t)) : 0f);
		Pose pose = new Pose(climbEdge, climbFacing);
		climbHead = pose * Pose.Lerp(ClimbMoves.Read(frame.head), ClimbMoves.Read(frame2.head), num);
		climbLeft = pose * Pose.Lerp(ClimbMoves.Read(frame.left), ClimbMoves.Read(frame2.left), num);
		climbRight = pose * Pose.Lerp(ClimbMoves.Read(frame.right), ClimbMoves.Read(frame2.right), num);
		climbWantLeft = ((num < 0.5f) ? frame.lh : frame2.lh);
		climbWantRight = ((num < 0.5f) ? frame.rh : frame2.rh);
	}

	private void OwnClimb(float t)
	{
		Vector3 vector = Vector3.Cross(Vector3.up, climbIn);
		Vector3 vector2 = climbEdge - vector * 0.22f + Vector3.up * 0.03f;
		Vector3 vector3 = climbEdge + vector * 0.22f + Vector3.up * 0.03f;
		Quaternion rotation = climbFacing * Quaternion.Euler(70f, 0f, 0f);
		float num = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.6f));
		float num2 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.6f) / 1.2f));
		float num3 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 1.8f) / 0.6f));
		Vector3 vector4 = Vector3.down * (0.9f * num2 + 0.4f * num3) + climbIn * (0.25f * num3);
		climbLeft = new Pose(vector2 + vector4, rotation);
		climbRight = new Pose(vector3 + vector4, rotation);
		Vector3 position = climbEdge - climbIn * 0.3f + Vector3.up * Mathf.Lerp(-0.25f, 0.45f, num2) + climbIn * (0.5f * num3) + Vector3.up * (0.4f * num3);
		climbHead = new Pose(position, climbFacing * Quaternion.Euler(Mathf.Lerp(-25f, 15f, num2), 0f, 0f));
		climbWantRight = (climbWantLeft = t > 0.5f && t < 2.35f);
		if (num < 1f)
		{
			climbLeft.position = Vector3.Lerp((Head != null) ? (Head.position - Vector3.up * 0.4f - vector * 0.2f) : vector2, climbLeft.position, num);
			climbRight.position = Vector3.Lerp((Head != null) ? (Head.position - Vector3.up * 0.4f + vector * 0.2f) : vector3, climbRight.position, num);
		}
	}

	private void HoldWorld(int which, bool want, Vector3 target)
	{
		Hand hand = ((which == 0) ? LeftHand : RightHand);
		Grip worldGrip = rig.worldGrip;
		if (hand == null || worldGrip == null)
		{
			return;
		}
		if (want && !climbHolding[which])
		{
			float magnitude = (hand.transform.position - target).magnitude;
			bool flag = magnitude < Tuning.F("climb.grab", 0.3f) || (Touching(hand) && hand.transform.position.y > climbEdge.y - Tuning.F("climb.top", 0.3f));
			if (hand.m_CurrentAttachedGO == null && flag && TakeWorld(hand))
			{
				climbHolding[which] = true;
				Lab.Event("climb: " + ((which == 0) ? "left" : "right") + " hand took hold (" + magnitude.ToString("0.00") + " m from the player's spot)");
			}
			else if (Time.time - climbSaidAt[which] > 0.5f)
			{
				climbSaidAt[which] = Time.time;
				Lab.Event("climb: " + ((which == 0) ? "left" : "right") + " hand " + magnitude.ToString("0.00") + " m from the edge" + (flag ? ", near but the grab did not take" : "") + ((hand.m_CurrentAttachedGO != null) ? ", holding something" : ""));
			}
		}
		else if (!want && climbHolding[which])
		{
			LetGoOf(worldGrip, hand);
			climbHolding[which] = false;
		}
	}

	private bool Touching(Hand hand)
	{
		foreach (Collider item in Physics.OverlapSphere(hand.transform.position, Tuning.F("climb.touch", 0.1f), -1, QueryTriggerInteraction.Ignore))
		{
			if (item != null && item.attachedRigidbody == null && item.GetComponentInParent<RigManager>() == null)
			{
				return true;
			}
		}
		return false;
	}

	private bool TakeWorld(Hand hand)
	{
		WorldGrip worldGrip = rig.worldGrip;
		if (worldGrip == null)
		{
			return false;
		}
		try
		{
			hand.GrabLock = false;
			hand.HoverLock();
			SimpleTransform handTransform = SimpleTransform.Create(hand.transform);
			worldGrip.OnHandHoverBegin(hand, isOverride: true);
			worldGrip.OnHandHoverUpdate(hand);
			worldGrip.ValidateGripScore(hand, handTransform);
			hand._mHoveringReceiver = worldGrip;
			worldGrip.OnGrabConfirm(hand, isOverride: false);
			return hand.m_CurrentAttachedGO != null;
		}
		catch (System.Exception ex)
		{
			Lab.Event("climb: the world grab failed: " + ex.Message);
			return false;
		}
	}

	private void ClimbPose()
	{
		if (!climbing || climbPhase != 1)
		{
			return;
		}
		Transform root = controls.vrRoot;
		Quaternion toBody;
		float scale;
		if (!(root == null))
		{
			Quaternion rotation = Quaternion.Euler(0f, bodyYaw, 0f);
			toBody = Quaternion.Inverse(rotation);
			scale = Mathf.Max(0.01f, root.lossyScale.y);
			Frame frame = pose;
			frame.height = Local(climbHead.position).y;
			frame.head = toBody * climbHead.rotation;
			frame.leftPos = Local(climbLeft.position);
			frame.leftRot = toBody * climbLeft.rotation;
			frame.rightPos = Local(climbRight.position);
			frame.rightRot = toBody * climbRight.rotation;
			frame.leftGrip = (climbWantLeft ? 1f : 0.3f);
			frame.rightGrip = (climbWantRight ? 1f : 0.3f);
			if (climbGliding)
			{
				float num = ((StandingEyes() > 0.3f) ? StandingEyes() : standingHeight);
				frame.height = Mathf.Clamp(frame.height, num * Tuning.F("climb.head.least", 0.78f), num);
			}
			float num2 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(climbReach / 0.35f));
			pose = ((num2 < 1f) ? Blend(climbFrom, frame, num2) : frame);
			curl = -1f;
		}
		Vector3 Local(Vector3 world)
		{
			return toBody * (world - root.position) / scale;
		}
	}

	public void PlayClone(Clone.Take take, bool loop)
	{
		if (take == null || take.frames.Count < 2)
		{
			Spawnables.Notify("No clone recorded yet");
			return;
		}
		StopPlaying();
		ReleaseCloneHands();
		SetMode(Mode.Clone);
		cloneTake = take;
		cloneLesson = null;
		cloneLoop = loop;
		string text = BotRig.AvatarOf(rig);
		bool flag = rig.avatar != null && (rig.avatar.transform.localScale - take.avatarScale).sqrMagnitude < 0.0001f;
		if (!string.IsNullOrEmpty(take.avatar) && (take.avatar != text || !flag))
		{
			cloneDressing = true;
			cloneDressedBy = Time.time + 6f;
			Clone.Take playing = take;
			BotRig.UseAvatar(rig, take.avatar, take.avatarScale, delegate
			{
				if (cloneTake == playing && cloneDressing)
				{
					cloneDressing = false;
					BeginClone();
				}
			});
			Lab.Event("clone: putting on the recording's avatar");
		}
		else
		{
			BeginClone();
		}
	}

	private void BeginClone()
	{
		cloneStart = Time.time;
		cloneEvent = 0;
		cloneFrame = 0;
		foreach (Poolee value in cloneSpawns.Values)
		{
			try
			{
				if (value != null && value.gameObject.activeInHierarchy && !HeldByPlayer(value.transform))
				{
					value.Despawn();
				}
			}
			catch (System.Exception)
			{
			}
		}
		cloneSpawns.Clear();
		clonePending.Clear();
		cloneGrabs.Clear();
		cloneStandIns.Clear();
		cloneDriven = -1;
		ReleaseCloneHands();
		try
		{
			bodyYaw = cloneTake.startYaw;
			rig.Teleport(cloneTake.startFeet);
		}
		catch (System.Exception ex2)
		{
			Lab.Event("clone: could not start where you did: " + ex2.Message);
		}
		Lab.Event("clone: playing " + cloneTake.Length.ToString("0.0") + " s");
	}

	private void ReleaseCloneHands()
	{
		LetGoOfPlayer("the clone stopped");
		Hand[] array = new Hand[2] { LeftHand, RightHand };
		for (int i = 0; i < 2; i++)
		{
			if (cloneHeld[i] != null && array[i] != null)
			{
				LetGoOf(cloneHeld[i], array[i]);
			}
			if (cloneLesson != null)
			{
				FreeHand(array[i]);
			}
			cloneHeld[i] = null;
		}
	}

	private bool CloneAt(float t, out Clone.Frame a, out Clone.Frame b, out float k)
	{
		System.Collections.Generic.List<Clone.Frame> frames = cloneTake.frames;
		a = (b = default(Clone.Frame));
		k = 0f;
		if (frames.Count < 2)
		{
			return false;
		}
		if (cloneFrame >= frames.Count || frames[cloneFrame].t > t)
		{
			cloneFrame = 0;
		}
		while (cloneFrame < frames.Count - 2 && frames[cloneFrame + 1].t <= t)
		{
			cloneFrame++;
		}
		a = frames[cloneFrame];
		b = frames[cloneFrame + 1];
		k = ((b.t > a.t) ? Mathf.Clamp01((t - a.t) / (b.t - a.t)) : 0f);
		return true;
	}

	private void CloneUpdate(float dt)
	{
		if (cloneTake == null)
		{
			SetMode(Mode.Stand);
			return;
		}
		walk = Vector2.zero;
		if (cloneDressing)
		{
			if (Time.time < cloneDressedBy)
			{
				return;
			}
			cloneDressing = false;
			BeginClone();
		}
		LessonLoop();
		if ((cloneLesson != null && driveMeant && DriveStep(dt)) || cloneTake == null || mode != Mode.Clone)
		{
			return;
		}
		float num = Time.time - cloneStart;
		if (cloneLesson != null)
		{
			if (seatWaiting != null)
			{
				LessonSeat(seatWaiting);
			}
			SeatCheck();
			LessonKeepHold();
			LessonWatch();
			if (cloneTake == null || mode != Mode.Clone)
			{
				return;
			}
		}
		System.Collections.Generic.List<Clone.Event> events = cloneTake.events;
		while (cloneEvent < events.Count && events[cloneEvent].t <= num)
		{
			Do(events[cloneEvent]);
			cloneEvent++;
		}
		FindCloneSpawns();
		CloneGrabWatch();
		for (int num2 = cloneGrabs.Count - 1; num2 >= 0; num2--)
		{
			bool num3 = TryCloneGrab(cloneGrabs[num2].e);
			if (!num3 && Time.time > cloneGrabs[num2].until && cloneLesson != null)
			{
				Lab.Event("lesson: could not take the " + Clone.ThingName(cloneGrabs[num2].e) + ": " + grabWhy);
			}
			if (num3 || Time.time > cloneGrabs[num2].until)
			{
				cloneGrabs.RemoveAt(num2);
			}
		}
		if (cloneLesson == null && CloneAt(num, out var a, out var b, out var k))
		{
			GrabPlayerUpdate(a, b, k);
		}
		if (!(num > cloneTake.Length) || (cloneLesson != null && lessonPurposeful && cloneLesson.purpose == "climb" && !LandedOnTop()))
		{
			return;
		}
		if (cloneLesson != null)
		{
			EndLesson("done with the " + cloneLesson.name);
		}
		else if (cloneLoop)
		{
			if (!string.IsNullOrEmpty(cloneTake.avatar) && BotRig.AvatarOf(rig) != cloneTake.avatar)
			{
				BotRig.UseAvatar(rig, cloneTake.avatar, cloneTake.avatarScale, null);
			}
			BeginClone();
		}
		else
		{
			Lab.Event("clone: done");
			ReleaseCloneHands();
			cloneTake = null;
			SetMode(Mode.Stand);
		}
	}

	private void Do(Clone.Event e)
	{
		switch (e.kind)
		{
		case Clone.Kind.Spawn:
		{
			string text = (e.name ?? "").ToLowerInvariant();
			if (!text.Contains("spawn_gun") && !text.Contains("nimbus") && !text.StartsWith("mag_") && !text.StartsWith("vfx") && !Clone.IsBody(e.barcode, e.name))
			{
				CloneSpawn(e.spawn, e.barcode, LessonPlace(e.at), LessonTurn(e.turn));
			}
			break;
		}
		case Clone.Kind.Grab:
			if (e.name != null && e.name.StartsWith("@wall", System.StringComparison.Ordinal))
			{
				wallHeld[e.hand] = true;
				wallPinned[e.hand] = false;
			}
			else if (!TryCloneGrab(e))
			{
				cloneGrabs.Add((e, Time.time + 1.5f));
			}
			break;
		case Clone.Kind.Release:
		{
			wallHeld[e.hand] = false;
			Hand hand = ((e.hand == 0) ? LeftHand : RightHand);
			if (cloneHeld[e.hand] != null && hand != null)
			{
				LetGoOf(cloneHeld[e.hand], hand);
			}
			if (hand != null && hand.m_CurrentAttachedGO != null)
			{
				FreeHand(hand);
			}
			cloneHeld[e.hand] = null;
			break;
		}
		case Clone.Kind.Fire:
			if (cloneLesson == null || !LessonUnsafe((e.hand == 0) ? LeftHand : RightHand))
			{
				CloneFireCheck(e.hand);
			}
			break;
		case Clone.Kind.Seat:
			LessonSeat(e);
			break;
		case Clone.Kind.Avatar:
			if (cloneLesson == null && BotRig.AvatarOf(rig) != e.barcode)
			{
				BotRig.UseAvatar(rig, e.barcode, e.at, null);
				Lab.Event("clone: putting on " + e.barcode);
			}
			break;
		}
	}

	private static bool NeedsThings(Lessons.Lesson lesson)
	{
		try
		{
			foreach (Clone.Event @event in lesson.take.events)
			{
				if (@event.barcode != null && @event.barcode.Length > 0 && !Clone.IsBody(@event.barcode, Clone.ThingName(@event)))
				{
					return true;
				}
			}
		}
		catch (System.Exception)
		{
		}
		return false;
	}

	private void CloneSpawn(int id, string barcode, Vector3 at, Quaternion turn)
	{
		if (cloneLesson != null && !BotManager.OwnSpawns && !FromPouch(barcode))
		{
			if (Time.time - noSpawnSaidAt > 5f)
			{
				noSpawnSaidAt = Time.time;
				Lab.Event("clone: no " + barcode + " of its own (bots are not to spawn things)");
			}
			return;
		}
		try
		{
			AssetWarehouse instance = AssetWarehouse.Instance;
			if (instance == null || !instance.HasCrate(new Barcode(barcode)))
			{
				Lab.Event("clone: cannot spawn " + barcode);
				return;
			}
			HashSet<System.IntPtr> hashSet = new HashSet<System.IntPtr>();
			foreach (Collider item in Physics.OverlapSphere(at, 1.5f, -1, QueryTriggerInteraction.Ignore))
			{
				Poolee poolee = ((item != null) ? item.GetComponentInParent<Poolee>() : null);
				if (poolee != null)
				{
					hashSet.Add(poolee.Pointer);
				}
			}
			if (cloneLesson != null && cloneLesson.purpose == "drive")
			{
				foreach (Seat item2 in UnityEngine.Object.FindObjectsOfType<Seat>())
				{
					Poolee poolee2 = ((item2 != null) ? item2.GetComponentInParent<Poolee>() : null);
					if (poolee2 != null)
					{
						hashSet.Add(poolee2.Pointer);
					}
				}
			}
			Clone.Suppress();
			HelperMethods.SpawnCrate(barcode, at, turn, Vector3.one);
			clonePending.Add((id, barcode, at, Time.time, hashSet));
		}
		catch (System.Exception ex)
		{
			Lab.Event("clone: spawn failed: " + ex.Message);
		}
	}

	private void FindCloneSpawns()
	{
		for (int num = clonePending.Count - 1; num >= 0; num--)
		{
			(int, string, Vector3, float, HashSet<System.IntPtr>) tuple = clonePending[num];
			if (!(Time.time - tuple.Item4 < 0.05f))
			{
				foreach (Collider item in Physics.OverlapSphere(tuple.Item3, 1.5f, -1, QueryTriggerInteraction.Ignore))
				{
					Poolee poolee = ((item != null) ? item.GetComponentInParent<Poolee>() : null);
					if (!(poolee == null) && !tuple.Item5.Contains(poolee.Pointer) && !(poolee.SpawnableCrate == null) && !(poolee.SpawnableCrate.Barcode.ID != tuple.Item2) && !cloneSpawns.ContainsValue(poolee) && (cloneLesson == null || !(cloneLesson.purpose == "drive") || !Occupied(poolee)))
					{
						cloneSpawns[tuple.Item1] = poolee;
						if (lessonBigAsked.Contains(tuple.Item1))
						{
							lessonBig.Add(poolee.transform.GetInstanceID());
						}
						clonePending.RemoveAt(num);
						break;
					}
				}
				if (num < clonePending.Count && clonePending[num].id == tuple.Item1 && Time.time - tuple.Item4 > 4f)
				{
					clonePending.RemoveAt(num);
				}
			}
		}
	}

	private static bool Occupied(Poolee p)
	{
		try
		{
			foreach (Seat componentsInChild in p.GetComponentsInChildren<Seat>(includeInactive: true))
			{
				if (componentsInChild != null && componentsInChild.rigManager != null)
				{
					return true;
				}
			}
		}
		catch (System.Exception)
		{
		}
		return false;
	}

	private static bool NotItsToTake(Poolee p)
	{
		try
		{
			foreach (Seat componentsInChild in p.GetComponentsInChildren<Seat>(includeInactive: true))
			{
				if (componentsInChild != null && componentsInChild.rigManager != null)
				{
					return true;
				}
			}
			Transform head = Player.Head;
			return head != null && Flat(p.transform.position - head.position).magnitude < (BotManager.OwnSpawns ? Tuning.F("drive.yours", 5f) : Tuning.F("drive.yours.map", 1.6f));
		}
		catch (System.Exception)
		{
			return false;
		}
	}

	private static bool FromPouch(string barcode)
	{
		try
		{
			AssetWarehouse instance = AssetWarehouse.Instance;
			if (instance != null && instance.TryGetCrate(new Barcode(barcode), out SpawnableCrate crateT) && crateT != null)
			{
				string text = (crateT.Title ?? "").ToLowerInvariant();
				return text.Contains("mag") || text.Contains("12g") || text.Contains("shell") || text.Contains("ammo") || text.Contains("clip");
			}
		}
		catch (System.Exception)
		{
		}
		return false;
	}

	private void CloneGrabWatch()
	{
		for (int num = grabWatch.Count - 1; num >= 0; num--)
		{
			(Hand, Rigidbody, float, float, float, string) value = grabWatch[num];
			if (value.Item1 != null && value.Item1.rb != null)
			{
				value.Item4 = Mathf.Max(value.Item4, value.Item1.rb.velocity.magnitude);
			}
			if (value.Item2 != null)
			{
				value.Item5 = Mathf.Max(value.Item5, value.Item2.angularVelocity.magnitude * 57.29578f);
			}
			grabWatch[num] = value;
			if (Time.time - value.Item3 > 0.5f)
			{
				Lab.Event("clone grab: after " + value.Item6 + ": hand moved at most " + value.Item4.ToString("0.0") + " m/s, the thing spun at most " + value.Item5.ToString("0") + " deg/s");
				grabWatch.RemoveAt(num);
			}
		}
	}

	private bool TryCloneGrab(Clone.Event e)
	{
		Hand hand = ((e.hand == 0) ? LeftHand : RightHand);
		if (hand == null)
		{
			return true;
		}
		if (e.name != null && e.name.StartsWith("@wall", System.StringComparison.Ordinal))
		{
			return true;
		}
		Transform transform = null;
		int num = e.spawn;
		if (num == -1 && cloneStandIns.TryGetValue(e, out var value))
		{
			num = value;
		}
		Vector3 where = ((cloneLesson != null) ? hand.transform.position : e.at);
		if (num != -1)
		{
			if (!cloneSpawns.TryGetValue(num, out var value2) || value2 == null)
			{
				grabWhy = "its own one (" + num + ") has not appeared";
				return false;
			}
			transform = value2.transform;
		}
		else if (cloneLesson == null || !((transform = LessonOwn(e, hand)) != null))
		{
			float num2 = ((cloneLesson != null) ? 1.2f : 2f);
			bool flag = cloneLesson != null;
			foreach (Collider item in Physics.OverlapSphere(where, flag ? 0.001f : num2, (!flag) ? (-1) : 0, QueryTriggerInteraction.Ignore))
			{
				Poolee poolee = ((item != null) ? item.GetComponentInParent<Poolee>() : null);
				Transform transform2 = ((poolee != null) ? poolee.transform : null);
				if (transform2 == null)
				{
					continue;
				}
				bool num3;
				if (e.barcode.Length <= 0)
				{
					num3 = transform2.name == Clone.ThingName(e);
				}
				else
				{
					if (!(poolee.SpawnableCrate != null))
					{
						continue;
					}
					num3 = poolee.SpawnableCrate.Barcode.ID == e.barcode;
				}
				if (num3 && !HeldByPlayer(transform2) && !(transform2.GetComponentInChildren<RigManager>() != null) && (cloneLesson == null || (!HeldByAnyone(transform2, rig) && !HeldByAnyone(transform2, null) && !lessonThings.ContainsValue(poolee))))
				{
					float magnitude = (transform2.position - where).magnitude;
					if (magnitude < num2)
					{
						num2 = magnitude;
						transform = transform2;
					}
				}
			}
			if (transform == null && e.barcode.Length > 0 && !Clone.IsBody(e.barcode, Clone.ThingName(e)))
			{
				string text = ((cloneLesson != null) ? LessonThingKey(e, hand) : null);
				if (text != null && lessonStandIns.TryGetValue(text, out var value3))
				{
					cloneStandIns[e] = value3;
					return false;
				}
				int num4 = -1000 - cloneStandIns.Count;
				Quaternion faces = ((cloneLesson != null) ? hand.transform.rotation : Quaternion.identity);
				if (cloneLesson != null)
				{
					PlaceBigThing(e, ref where, ref faces);
				}
				CloneSpawn(num4, e.barcode, where, faces);
				cloneStandIns[e] = num4;
				if (text != null)
				{
					lessonStandIns[text] = num4;
				}
				if (cloneLesson != null)
				{
					Lab.Event("lesson: needs a " + Clone.ThingName(e) + " for its " + ((e.hand == 0) ? "left" : "right") + " hand, spawning one");
				}
				return false;
			}
			if (transform == null)
			{
				return true;
			}
		}
		try
		{
			Seat activeSeat = rig.activeSeat;
			Poolee poolee2 = ((activeSeat != null) ? activeSeat.GetComponentInParent<Poolee>() : null);
			if (poolee2 != null && e.barcode.Length > 0 && poolee2.SpawnableCrate != null && poolee2.SpawnableCrate.Barcode.ID == e.barcode && transform != poolee2.transform)
			{
				Lab.Event("lesson: the " + Clone.ThingName(e) + " it sits in, not " + transform.name);
				transform = poolee2.transform;
			}
		}
		catch (System.Exception)
		{
		}
		Grip[] array = Clone.GripsOf(transform);
		Grip grip = null;
		string text2 = Clone.GripName(e);
		if (text2 != null)
		{
			Grip[] array2 = array;
			foreach (Grip grip2 in array2)
			{
				if (grip2 != null && grip2.name == text2 && (grip == null || !grip2.HasAttachedHands()))
				{
					grip = grip2;
				}
			}
		}
		if (grip == null && cloneLesson != null && e.t < 0.3f)
		{
			Gun componentInChildren = transform.GetComponentInChildren<Gun>();
			if (componentInChildren != null && componentInChildren.triggerGrip != null)
			{
				grip = componentInChildren.triggerGrip;
			}
		}
		if (grip == null)
		{
			grip = ((e.grip >= 0 && e.grip < array.Length) ? array[e.grip] : null);
		}
		if (grip == null)
		{
			float num5 = float.MaxValue;
			Grip[] array2 = array;
			foreach (Grip grip3 in array2)
			{
				float magnitude2 = (grip3.transform.position - hand.transform.position).magnitude;
				if (magnitude2 < num5)
				{
					num5 = magnitude2;
					grip = grip3;
				}
			}
		}
		if (grip == null)
		{
			return true;
		}
		if (hand.m_CurrentAttachedGO != null)
		{
			FreeHand(hand);
		}
		Magazine componentInChildren2 = transform.GetComponentInChildren<Magazine>();
		if (componentInChildren2 != null && componentInChildren2.magazineState != null)
		{
			try
			{
				componentInChildren2.magazineState.Refill();
			}
			catch (System.Exception)
			{
			}
		}
		if (grip.attachedHands != null && grip.attachedHands.Contains(hand))
		{
			if (cloneHeld[e.hand] != grip)
			{
				Lab.Event("clone grab: " + ((e.hand == 0) ? "left" : "right") + " on " + Clone.ThingName(e) + "/" + grip.name + ": the game's own grab, from your squeeze");
			}
			cloneHeld[e.hand] = grip;
			return true;
		}
		if (cloneLesson != null && lessonBig.Contains(transform.GetInstanceID()) && (grip.transform.position - hand.transform.position).magnitude > Tuning.F("lesson.reach", 0.4f))
		{
			grabWhy = "its hand is " + (grip.transform.position - hand.transform.position).magnitude.ToString("0.0") + " m from the " + grip.name;
			return false;
		}
		Pose hand2;
		bool mirrored;
		Pose pose = (GripBook.HandOn(grip, e.hand == 0, out hand2, out mirrored) ? hand2 : HandOnGrip(grip, hand));
		float magnitude3 = (pose.position - hand.transform.position).magnitude;
		if (!grabWaitFrom.TryGetValue(e, out var value4))
		{
			value4 = (grabWaitFrom[e] = Time.time);
		}
		if (cloneLesson == null && Tuning.F("clone.grab.game", 1f) >= 0.5f && Time.time - value4 < Tuning.F("clone.grab.game.wait", 0.3f))
		{
			grabWhy = "the game's own grab given a moment";
			return false;
		}
		bool flag2 = magnitude3 <= Tuning.F("clone.grab.near", 0.06f) || (Time.time - value4 >= Tuning.F("clone.grab.wait", 0.5f) && magnitude3 <= Tuning.F("clone.grab.most", 0.15f));
		if (!flag2 && cloneLesson != null && rig.activeSeat != null && Time.time - value4 >= Tuning.F("lesson.seated.wait", 0.3f) && magnitude3 <= Tuning.F("lesson.seated.reach", 0.6f))
		{
			Poolee componentInParent = rig.activeSeat.GetComponentInParent<Poolee>();
			if (componentInParent != null && grip.transform.IsChildOf(componentInParent.transform))
			{
				flag2 = true;
				Lab.Event("lesson: takes the " + grip.name + " of the car it sits in from " + magnitude3.ToString("0.00") + " m");
			}
		}
		if (!flag2)
		{
			grabWhy = "its hand is " + magnitude3.ToString("0.00") + " m from where it goes on the " + grip.name;
			return false;
		}
		grabWaitFrom.Remove(e);
		try
		{
			BaseController baseController = ((e.hand == 0) ? controls.leftController : controls.rightController);
			Lab.Event("clone grab: " + ((e.hand == 0) ? "left" : "right") + " on " + transform.name + "/" + grip.name + ": hand " + (magnitude3 * 100f).ToString("0") + " cm off, " + Quaternion.Angle(hand.transform.rotation, pose.rotation).ToString("0") + " deg turned, waited " + (Time.time - value4).ToString("0.00") + " s, fingers " + ((baseController != null) ? (baseController._processedIndex.ToString("0.0") + "/" + baseController._processedMiddle.ToString("0.0") + "/" + baseController._processedThumb.ToString("0.0") + " grip " + baseController._solvedGrip.ToString("0.00")) : "?") + ((!GripBook.HandOn(grip, e.hand == 0, out var _, out var mirrored2)) ? ", the game's pose" : (mirrored2 ? ", your pose mirrored" : ", your pose")));
			grabWatch.Add((hand, transform.GetComponentInParent<Rigidbody>(), Time.time, 0f, 0f, transform.name + "/" + grip.name));
		}
		catch (System.Exception)
		{
		}
		grabWhy = "the " + transform.name + "'s " + grip.name + " would not be taken" + (grip.HasAttachedHands() ? " (a hand is on it)" : "") + ((hand.m_CurrentAttachedGO != null) ? " (its hand is full)" : "");
		if (Grab(hand, grip))
		{
			cloneHeld[e.hand] = grip;
			if (cloneLesson != null)
			{
				Poolee componentInParent2 = transform.GetComponentInParent<Poolee>();
				if (componentInParent2 != null)
				{
					lessonThings[LessonThingKey(e, hand)] = componentInParent2;
				}
			}
			if (cloneLesson != null)
			{
				Lab.Event("lesson: took the " + transform.name + " by its " + grip.name + " (" + ((e.hand == 0) ? "left" : "right") + " hand)");
			}
			return true;
		}
		return false;
	}

	private static bool HeldByAnyone(Transform thing, RigManager but)
	{
		foreach (Grip componentsInChild in thing.GetComponentsInChildren<Grip>())
		{
			if (componentsInChild == null || componentsInChild.attachedHands == null)
			{
				continue;
			}
			Il2CppSystem.Collections.Generic.List<Hand>.Enumerator enumerator2 = componentsInChild.attachedHands.GetEnumerator();
			while (enumerator2.MoveNext())
			{
				Hand current2 = enumerator2.Current;
				if (current2 != null && (but == null || current2.GetComponentInParent<RigManager>() != but))
				{
					return true;
				}
			}
		}
		return false;
	}

	private string LessonThingKey(Clone.Event e, Hand hand)
	{
		int num = Clone.ThingNumber(e);
		if (num >= 0)
		{
			return e.barcode + "#" + num;
		}
		Hand hand2 = ((hand == LeftHand) ? RightHand : LeftHand);
		GameObject gameObject = ((hand2 != null) ? hand2.m_CurrentAttachedGO : null);
		Poolee poolee = ((gameObject != null) ? gameObject.GetComponentInParent<Poolee>() : null);
		if (poolee != null && poolee.SpawnableCrate != null && poolee.SpawnableCrate.Barcode.ID == e.barcode)
		{
			Grip[] array = Clone.GripsOf(poolee.transform);
			Grip grip = ((e.grip >= 0 && e.grip < array.Length) ? array[e.grip] : null);
			string text = Clone.GripName(e);
			Grip grip2 = gameObject.GetComponent<Grip>() ?? gameObject.GetComponentInParent<Grip>();
			if (grip2 != null && ((grip != null && grip == grip2) || (text != null && grip2.name == text)))
			{
				return e.barcode + "#hand" + e.hand;
			}
		}
		return e.barcode + "#0";
	}

	private Transform LessonOwn(Clone.Event e, Hand hand)
	{
		if (string.IsNullOrEmpty(e.barcode))
		{
			return null;
		}
		string text = LessonThingKey(e, hand);
		if (lessonThings.TryGetValue(text, out var value) && value != null && value.gameObject.activeInHierarchy)
		{
			return value.transform;
		}
		if (text.EndsWith("#0", System.StringComparison.Ordinal) && Clone.ThingNumber(e) < 0)
		{
			Hand[] array = new Hand[2] { LeftHand, RightHand };
			foreach (Hand hand2 in array)
			{
				GameObject gameObject = ((hand2 != null) ? hand2.m_CurrentAttachedGO : null);
				Poolee poolee = ((gameObject != null) ? gameObject.GetComponentInParent<Poolee>() : null);
				if (poolee != null && poolee.SpawnableCrate != null && poolee.SpawnableCrate.Barcode.ID == e.barcode)
				{
					return poolee.transform;
				}
			}
		}
		return null;
	}

	private static bool HeldByPlayer(Transform thing)
	{
		Hand[] array = new Hand[2]
		{
			Player.LeftHand,
			Player.RightHand
		};
		foreach (Hand hand in array)
		{
			GameObject gameObject = ((hand != null) ? hand.m_CurrentAttachedGO : null);
			if (gameObject != null && gameObject.transform.IsChildOf(thing))
			{
				return true;
			}
		}
		return false;
	}

	private void CloneFireCheck(int which)
	{
		Hand hand = ((which == 0) ? LeftHand : RightHand);
		GameObject gameObject = ((hand != null) ? hand.m_CurrentAttachedGO : null);
		Gun gun = ((gameObject != null) ? gameObject.GetComponentInParent<Gun>() : null);
		if (gun == null || Time.time - cloneFiredAt < 0.12f)
		{
			return;
		}
		try
		{
			if (gun.cartridgeState != Gun.CartridgeStates.UNSPENT)
			{
				TryChargeGun(gun);
			}
			gun.Fire();
		}
		catch (System.Exception)
		{
		}
	}

	private static void TryChargeGun(Gun g)
	{
		try
		{
			if (g.slideState == Gun.SlideStates.LOCKED)
			{
				g.SlideRelease();
			}
			else
			{
				g.Charge();
			}
		}
		catch (System.Exception)
		{
		}
	}

	private bool FeetOnGround()
	{
		foreach (RaycastHit item in Physics.RaycastAll(rig.physicsRig.centerOfPressure.position + Vector3.up * 0.3f, Vector3.down, 0.55f, -1, QueryTriggerInteraction.Ignore))
		{
			if (item.collider != null && item.collider.GetComponentInParent<RigManager>() == null)
			{
				return true;
			}
		}
		return false;
	}

	public void OnCloneGunFired(Gun fired)
	{
		Hand[] array = new Hand[2] { LeftHand, RightHand };
		foreach (Hand hand in array)
		{
			GameObject gameObject = ((hand != null) ? hand.m_CurrentAttachedGO : null);
			if (gameObject != null && fired != null && gameObject.GetComponentInParent<Gun>() == fired)
			{
				cloneFiredAt = Time.time;
			}
		}
	}

	private void CloneDrive()
	{
		cloneDrives++;
		if (cloneLesson != null && driving)
		{
			DrivePose();
		}
		else
		{
			if (cloneTake == null || cloneDressing || !CloneAt(Time.time - cloneStart, out var a, out var b, out var k))
			{
				return;
			}
			Transform vrRoot = controls.vrRoot;
			rig.remapHeptaRig.inWeight = 1f;
			Quaternion quaternion = Quaternion.Slerp(a.playspace, b.playspace, k);
			Pose b2 = Pose.Lerp(a.head, b.head, k);
			Pose b3 = Pose.Lerp(a.left, b.left, k);
			Pose b4 = Pose.Lerp(a.right, b.right, k);
			if (cloneLesson != null)
			{
				cloneYaw += PortalTurn(vrRoot);
				if (LessonPortalTurns())
				{
					quaternion = b.playspace;
				}
				quaternion = Quaternion.Euler(0f, cloneYaw, 0f) * quaternion;
				if (Mathf.Abs(cloneSize - 1f) > 0.02f)
				{
					Vector3 vector = new Vector3(b2.position.x, 0f, b2.position.z);
					b2.position = new Vector3(b2.position.x, b2.position.y * cloneSize, b2.position.z);
					b3.position = vector + (b3.position - vector) * cloneSize;
					b4.position = vector + (b4.position - vector) * cloneSize;
				}
			}
			if (cloneLesson != null)
			{
				float num = Time.time - lessonSeamAt;
				if (num < 0.3f)
				{
					float num2 = num / 0.3f;
					float k2 = num2 * num2 * (3f - 2f * num2);
					b2 = Pose.Lerp(seamHead, b2, k2);
					b3 = Pose.Lerp(seamLeft, b3, k2);
					b4 = Pose.Lerp(seamRight, b4, k2);
				}
				lastHeadPose = b2;
				lastLeftPose = b3;
				lastRightPose = b4;
			}
			vrRoot.rotation = quaternion;
			NotePortalTurn(vrRoot);
			lessonRootYaw = vrRoot.eulerAngles.y;
			lessonRootSet = true;
			controls.headset.SetLocalPositionAndRotation(b2.position, b2.rotation);
			KeepHeadDown(vrRoot, controls.headset);
			controls.leftController.transform.SetLocalPositionAndRotation(b3.position, b3.rotation);
			controls.rightController.transform.SetLocalPositionAndRotation(b4.position, b4.rotation);
			WallHands();
			bool tapped;
			Clone.HandInput handInput = LessonInput(left: true, a, out tapped);
			bool tapped2;
			Clone.HandInput handInput2 = LessonInput(left: false, a, out tapped2);
			cloneDriven = cloneFrame;
			bool flag = cloneLesson == null || !LessonUnsafe(LeftHand);
			bool flag2 = cloneLesson == null || !LessonUnsafe(RightHand);
			if (cloneLesson != null && lessonPurposeful && cloneLesson.purpose == "climb" && Tuning.F("tool.climb.press", 0f) < 0.5f)
			{
				flag = (flag2 = false);
				handInput.secondary = (handInput2.secondary = false);
				handInput.a = (handInput.b = (handInput2.a = (handInput2.b = false)));
				handInput.click = (handInput2.click = (handInput.pad = (handInput2.pad = false)));
				tapped = (tapped2 = false);
			}
			bool gameGrabs = cloneLesson == null && Tuning.F("clone.grab.game", 1f) >= 0.5f;
			PutInput(controls.leftController, handInput, b.leftIn, k, flag, gameGrabs);
			PutInput(controls.rightController, handInput2, b.rightIn, k, flag2, gameGrabs);
			PutButtons(controls.leftController, handInput, tapped, cloneHeld[0] != null);
			PutButtons(controls.rightController, handInput2, tapped2, cloneHeld[1] != null);
			controls._primaryAButton = a.aPrimary;
			controls._secondaryAButton = a.aSecondary;
			rig.remapHeptaRig._crouchTarget = Mathf.Lerp(a.crouch, b.crouch, k);
			rig.remapHeptaRig._feetOffset = Mathf.Lerp(a.feet, b.feet, k);
			if (controls.leftController != null)
			{
				controls.leftController._thumbstickAxis = Vector2.zero;
			}
			controls._axisPrimary = Vector2.zero;
			if (cloneLesson != null && rig.activeSeat != null)
			{
				Vector2 vector2 = Vector2.Lerp(a.leftIn.stick, b.leftIn.stick, k);
				Vector2 thumbstickAxis = Vector2.Lerp(a.rightIn.stick, b.rightIn.stick, k);
				if (controls.leftController != null)
				{
					controls.leftController._thumbstickAxis = vector2;
				}
				if (controls.rightController != null)
				{
					controls.rightController._thumbstickAxis = thumbstickAxis;
				}
				controls._axisPrimary = vector2;
				controls._axisPrimaryDirty = vector2 != Vector2.zero;
			}
		}
	}

	private static void PutInput(BaseController c, Clone.HandInput a, Clone.HandInput b, float k, bool trigger, bool gameGrabs = false)
	{
		if (!(c == null))
		{
			if (!trigger)
			{
				a.trigger = (b.trigger = 0f);
				a.primary = false;
			}
			c._processedIndex = Mathf.Lerp(a.index, b.index, k);
			c._processedMiddle = Mathf.Lerp(a.middle, b.middle, k);
			c._processedRing = Mathf.Lerp(a.ring, b.ring, k);
			c._processedPinky = Mathf.Lerp(a.pinky, b.pinky, k);
			c._processedThumb = Mathf.Lerp(a.thumb, b.thumb, k);
			float num = Mathf.Lerp(a.grip, b.grip, k);
			c._solvedGrip = (gameGrabs ? num : Mathf.Min(num, OpenController.grabThreshold - 0.01f));
			bool primaryInteractionButton = c._primaryInteractionButton;
			c._primaryAxis = Mathf.Lerp(a.trigger, b.trigger, k);
			c._primaryInteractionButton = a.primary;
			c._primaryInteractionButtonDown = a.primary && !primaryInteractionButton;
			c._primaryInteractionButtonUp = !a.primary && primaryInteractionButton;
			c._secondaryInteractionButton = a.secondary;
		}
	}

	public void CloneFixed(float dt)
	{
		GrabPlayerFixed();
		if (!PlayingClone || cloneDressing || !CloneAt(Time.time - cloneStart, out var a, out var b, out var k))
		{
			return;
		}
		Rigidbody rigidbody = ((rig.physicsRig != null && rig.physicsRig.m_pelvis != null) ? rig.physicsRig.m_pelvis.GetComponent<Rigidbody>() : null);
		if (rigidbody == null)
		{
			return;
		}
		Vector3 vector = Vector3.Lerp(a.pelvis, b.pelvis, k);
		Vector3 vector2 = Vector3.Lerp(a.pelvisVelocity, b.pelvisVelocity, k);
		if (cloneLesson != null)
		{
			bool flag = lessonPelvisHas && (rigidbody.position - lessonPelvisWas).magnitude > 1.5f;
			bool num = (b.pelvis - a.pelvis).magnitude > 1.5f;
			if (num)
			{
				vector = b.pelvis;
				vector2 = b.pelvisVelocity;
			}
			bool flag2 = num && lessonJumpAt != cloneFrame;
			if (flag2)
			{
				lessonJumpAt = cloneFrame;
			}
			if (flag || flag2)
			{
				LessonFromHere(vector, rigidbody.position);
				Lab.Event("lesson: " + (flag ? "carried off somewhere (a portal?)" : "you were carried off here") + ", going on from where it is");
			}
			lessonPelvisWas = rigidbody.position;
			lessonPelvisHas = true;
			Vector3 there = vector;
			vector = LessonPlace(vector);
			vector2 = Quaternion.Euler(0f, cloneYaw, 0f) * vector2;
			if ((vector - rigidbody.position).magnitude > Tuning.F("clone.snap", 2.5f))
			{
				LessonFromHere(there, rigidbody.position);
				return;
			}
		}
		if (lessonLanding && rig.physicsRig.centerOfPressure != null)
		{
			Vector3 position = rig.physicsRig.centerOfPressure.position;
			rig.Teleport(Vector3.MoveTowards(position, lessonLandAt, Tuning.F("climb.glide.step", 0.03f)));
			lessonPelvisHas = false;
			CarriedFollow();
			return;
		}
		if (cloneLesson != null && rig.activeSeat != null)
		{
			lessonPelvisHas = false;
			return;
		}
		if (cloneLesson != null && lessonPurposeful && cloneLesson.purpose == "climb" && rig.physicsRig.centerOfPressure != null)
		{
			float num2 = vector.y - rigidbody.position.y;
			if (Time.time - cloneStart >= cloneLesson.riseAt - 0.1f && (num2 > 0.12f || (lessonGliding && num2 > -0.25f && !FeetOnGround())))
			{
				if (!lessonGliding)
				{
					CarryThings(on: true);
				}
				lessonGliding = true;
				glidedAt = Time.time;
				Vector3 position2 = rig.physicsRig.centerOfPressure.position;
				Vector3 vector3 = position2 + (vector - rigidbody.position);
				rig.Teleport(Vector3.MoveTowards(position2, vector3, Tuning.F("climb.glide.step", 0.03f)));
				lessonPelvisHas = false;
				CarriedFollow();
				return;
			}
			lessonGliding = false;
			if (carried.Count > 0 && Time.time - glidedAt > 0.6f)
			{
				CarryThings(on: false);
			}
		}
		CarriedFollow();
		Vector3 vector4 = vector - rigidbody.position;
		if (vector4.magnitude > Tuning.F("clone.snap", 2.5f))
		{
			try
			{
				Vector3 feetDestinationWorld = rig.physicsRig.centerOfPressure.position + vector4;
				rig.Teleport(feetDestinationWorld, zeroVelocity: false);
				return;
			}
			catch (System.Exception)
			{
				return;
			}
		}
		float num3 = Tuning.F("clone.pull", 14f);
		Vector3 vector5 = vector2 + new Vector3(vector4.x, 0f, vector4.z) * num3 - rigidbody.velocity;
		if (Mathf.Abs(vector2.y) < Tuning.F("clone.vertical", 1.2f))
		{
			vector5.y = 0f;
		}
		rigidbody.AddForce(Vector3.ClampMagnitude(vector5 * Tuning.F("clone.push", 18f), 120f), ForceMode.Acceleration);
		if (!rig.physicsRig.ballLocoEnabled)
		{
			(LessonTurn(Quaternion.Slerp(a.pelvisTurn, b.pelvisTurn, k)) * Quaternion.Inverse(rigidbody.rotation)).ToAngleAxis(out var angle, out var axis);
			if (angle > 180f)
			{
				angle -= 360f;
			}
			Vector3 vector6 = Vector3.Lerp(a.pelvisSpin, b.pelvisSpin, k) + axis * (angle * ((float)System.Math.PI / 180f) * num3);
			if (!float.IsNaN(vector6.x))
			{
				rigidbody.AddTorque(Vector3.ClampMagnitude((vector6 - rigidbody.angularVelocity) * Tuning.F("clone.push", 18f), 200f), ForceMode.Acceleration);
			}
		}
	}

	private void HurtPlayer()
	{
		Transform head = Player.Head;
		Health health = ((Player.RigManager != null) ? Player.RigManager.health : null);
		if (head == null || health == null)
		{
			return;
		}
		Hand[] array = new Hand[2] { LeftHand, RightHand };
		for (int i = 0; i < 2; i++)
		{
			Hand hand = array[i];
			if (hand == null || hand.rb == null || Time.time - punchedAt[i] < 0.5f)
			{
				continue;
			}
			Vector3 position = hand.transform.position;
			float magnitude = (position - head.position).magnitude;
			float magnitude2 = (position - (head.position - Vector3.up * 0.35f)).magnitude;
			float magnitude3 = hand.rb.velocity.magnitude;
			if ((magnitude < 0.2f || magnitude2 < 0.28f) && magnitude3 > Tuning.F("hurt.punch.speed", 2.5f))
			{
				punchedAt[i] = Time.time;
				float damage = health.max_Health * Tuning.F("hurt.punch", 0.06f) * Mathf.Clamp(magnitude3 / 3f, 0.5f, 2.5f);
				try
				{
					health.TAKEDAMAGE(damage);
				}
				catch (System.Exception)
				{
				}
				Lab.Event("clone: punched you (" + magnitude3.ToString("0.0") + " m/s, " + damage.ToString("0.00") + ")");
			}
		}
	}

	private void GrabPlayerUpdate(Clone.Frame a, Clone.Frame b, float k)
	{
		Hand[] array = new Hand[2] { LeftHand, RightHand };
		float[] array2 = new float[2]
		{
			Mathf.Lerp(a.leftIn.grip, b.leftIn.grip, k),
			Mathf.Lerp(a.rightIn.grip, b.rightIn.grip, k)
		};
		for (int i = 0; i < 2; i++)
		{
			float num = cloneGrip[i];
			cloneGrip[i] = array2[i];
			Hand hand = array[i];
			if (hand == null || hand.rb == null)
			{
				continue;
			}
			if (holdingPlayer == i)
			{
				if (array2[i] < Tuning.F("grab.open", 0.35f))
				{
					ThrowPlayer();
				}
				else if (Time.time - playerHeldSince > Tuning.F("grab.longest", 12f) || !PlayerStillNear(hand))
				{
					LetGoOfPlayer("slipped free");
				}
			}
			else
			{
				float num2 = Tuning.F("grab.shut", 0.6f);
				if (holdingPlayer < 0 && array2[i] >= num2 && num < num2 && hand.m_CurrentAttachedGO == null && PlayerWithin(hand))
				{
					TakePlayer(i, hand);
				}
			}
		}
	}

	private static bool PlayerWithin(Hand hand)
	{
		RigManager rigManager = Player.RigManager;
		if (rigManager == null)
		{
			return false;
		}
		float radius = Tuning.F("grab.reach", 0.25f);
		foreach (Collider item in Physics.OverlapSphere(hand.transform.position, radius, -1, QueryTriggerInteraction.Ignore))
		{
			if (item != null && item.GetComponentInParent<RigManager>() == rigManager)
			{
				return true;
			}
		}
		return false;
	}

	private static Rigidbody PlayerPelvis()
	{
		PhysicsRig physicsRig = Player.PhysicsRig;
		if (!(physicsRig != null) || !(physicsRig.m_pelvis != null))
		{
			return null;
		}
		return physicsRig.m_pelvis.GetComponent<Rigidbody>();
	}

	private bool PlayerStillNear(Hand hand)
	{
		Rigidbody rigidbody = PlayerPelvis();
		if (rigidbody == null)
		{
			return false;
		}
		Vector3 vector = hand.transform.position + hand.transform.rotation * heldOffset;
		return (rigidbody.position - vector).magnitude < Tuning.F("grab.slip", 1.5f);
	}

	private void TakePlayer(int which, Hand hand)
	{
		Rigidbody rigidbody = PlayerPelvis();
		if (rigidbody == null || Player.PhysicsRig == null)
		{
			return;
		}
		holdingPlayer = which;
		playerHeldSince = Time.time;
		heldOffset = Vector3.ClampMagnitude(Quaternion.Inverse(hand.transform.rotation) * (rigidbody.position - hand.transform.position), Tuning.F("grab.offset", 0.6f));
		heldBodies.Clear();
		foreach (Rigidbody componentsInChild in Player.PhysicsRig.GetComponentsInChildren<Rigidbody>())
		{
			if (componentsInChild != null && !componentsInChild.isKinematic)
			{
				heldBodies.Add(componentsInChild);
			}
		}
		Lab.Event("clone: grabbed you (" + ((which == 0) ? "left" : "right") + " hand)");
	}

	private void GrabPlayerFixed()
	{
		if (holdingPlayer < 0)
		{
			return;
		}
		Hand hand = ((holdingPlayer == 0) ? LeftHand : RightHand);
		Rigidbody rigidbody = PlayerPelvis();
		if (hand == null || hand.rb == null || rigidbody == null)
		{
			LetGoOfPlayer("lost hold");
			return;
		}
		Vector3 vector = hand.transform.position + hand.transform.rotation * heldOffset;
		Vector3 force = Vector3.ClampMagnitude(hand.rb.velocity + (vector - rigidbody.position) * Tuning.F("grab.pull", 12f) - rigidbody.velocity, Tuning.F("grab.step", 2.5f));
		foreach (Rigidbody heldBody in heldBodies)
		{
			if (heldBody != null)
			{
				heldBody.AddForce(force, ForceMode.VelocityChange);
			}
		}
	}

	private void ThrowPlayer()
	{
		Hand hand = ((holdingPlayer == 0) ? LeftHand : RightHand);
		Vector3 vector = ((hand != null && hand.rb != null) ? (hand.rb.velocity * Tuning.F("grab.throw", 1.4f)) : Vector3.zero);
		Rigidbody rigidbody = PlayerPelvis();
		if (rigidbody != null)
		{
			Vector3 force = vector - rigidbody.velocity;
			foreach (Rigidbody heldBody in heldBodies)
			{
				if (heldBody != null)
				{
					heldBody.AddForce(force, ForceMode.VelocityChange);
				}
			}
		}
		LetGoOfPlayer("threw you at " + vector.magnitude.ToString("0.0") + " m/s");
	}

	private void LetGoOfPlayer(string why)
	{
		if (holdingPlayer >= 0)
		{
			holdingPlayer = -1;
			heldBodies.Clear();
			Lab.Event("clone: let go of you (" + why + ")");
		}
	}

	private void CoverNoPush(bool on)
	{
		try
		{
			if (!on)
			{
				foreach (var item in coverNoTouch)
				{
					if (item.a != null && item.b != null)
					{
						Physics.IgnoreCollision(item.a, item.b, ignore: false);
					}
				}
				coverNoTouch.Clear();
			}
			else
			{
				if (coverThing == null || coverNoTouch.Count > 0)
				{
					return;
				}
				MarrowEntity componentInParent = coverThing.GetComponentInParent<MarrowEntity>();
				Transform obj = ((componentInParent != null) ? componentInParent.transform : coverThing.transform);
				System.Collections.Generic.List<Collider> list = new System.Collections.Generic.List<Collider>(rig.physicsRig.GetComponentsInChildren<Collider>(includeInactive: false));
				if (this.item != null)
				{
					MarrowEntity componentInParent2 = this.item.GetComponentInParent<MarrowEntity>();
					list.AddRange(((componentInParent2 != null) ? componentInParent2.transform : this.item.transform).GetComponentsInChildren<Collider>(includeInactive: false));
				}
				{
					foreach (Collider componentsInChild in obj.GetComponentsInChildren<Collider>(includeInactive: false))
					{
						if (componentsInChild == null || componentsInChild.isTrigger)
						{
							continue;
						}
						foreach (Collider item2 in list)
						{
							if (!(item2 == null) && !item2.isTrigger)
							{
								Physics.IgnoreCollision(componentsInChild, item2, ignore: true);
								coverNoTouch.Add((componentsInChild, item2));
							}
						}
					}
					return;
				}
			}
		}
		catch (System.Exception)
		{
			coverNoTouch.Clear();
		}
	}

	private void LeaveCover(float rest, string why)
	{
		if (coverPhase != 0)
		{
			Lab.Event("cover: leaving (" + why + ")");
		}
		coverPhase = CoverPhase.None;
		coverRestUntil2 = Time.time + rest;
		coverTaken.Remove(this);
		CoverNoPush(on: false);
		coverThing = null;
	}

	private bool CoverStep(Transform head, Vector3 point, ref Vector2 move, ref bool holdFire, float dt)
	{
		if (coverPhase != 0 && Time.time - coverStepAt > 2f)
		{
			LeaveCover(0f, "the fight moved on");
		}
		coverStepAt = Time.time;
		if (Tuning.F("cover.on", 1f) < 0.5f || head == null)
		{
			LeaveCover(0f, "off");
			return false;
		}
		Vector3 vector = Feet;
		if (Flat(point - head.position).magnitude < Tuning.F("cover.close", 5f))
		{
			LeaveCover(2f, "too close to them");
			return false;
		}
		if (coverPhase == CoverPhase.None)
		{
			if (Time.time < coverRestUntil2 || Time.time < coverLookAt)
			{
				return false;
			}
			coverLookAt = Time.time + Tuning.F("cover.look.every", 1.2f);
			System.Collections.Generic.List<Vector3> threats = Threats(point);
			if (!FindSpot(vector, threats, out var best, out var bestLow, out var bestPeek, out var bestBehind, out var bestThing))
			{
				return false;
			}
			coverThing = bestThing;
			coverAt = best;
			coverLow = bestLow;
			peekAt = bestPeek;
			coverBehind = bestBehind;
			coverPhase = CoverPhase.Going;
			coverPhaseUntil = Time.time + Tuning.F("cover.run.most", 6f);
			coverTaken[this] = best;
			Lab.Event("cover: making for " + (bestLow ? "low" : "tall") + " cover " + Flat(best - vector).magnitude.ToString("0.0") + " m off, behind " + bestBehind);
		}
		Vector3 vector2 = Flat(coverAt - vector);
		switch (coverPhase)
		{
		case CoverPhase.Going:
		{
			if (vector2.magnitude < 0.45f)
			{
				coverPhase = CoverPhase.Hidden;
				coverPhaseUntil = Time.time + HideFor();
				coverLeaveAt = Time.time + UnityEngine.Random.Range(Tuning.F("cover.stay.least", 10f), Tuning.F("cover.stay.most", 22f));
				coverInAt = Time.time;
				coverStrikes = 0;
				Lab.Event("cover: in, hidden");
				break;
			}
			if (Time.time > coverPhaseUntil)
			{
				LeaveCover(2f, "could not get there");
				return false;
			}
			float magnitude = vector2.magnitude;
			if (magnitude < 2.5f)
			{
				CoverNoPush(on: true);
			}
			move = Local(vector2.normalized) * ((magnitude > 2.5f) ? MotionLibrary.RunPush : (MotionLibrary.WalkPush * Mathf.Lerp(0.45f, 1f, magnitude / 2.5f)));
			running = magnitude > 2.5f;
			holdFire = false;
			CrouchTo(down: false);
			return true;
		}
		case CoverPhase.Hidden:
			if (!Reloading && gun != null && AmmoFraction() < Tuning.F("cover.reload.below", 0.5f))
			{
				StartReload();
			}
			if (Time.time >= coverPhaseUntil && !Reloading)
			{
				if (Time.time >= coverLeaveAt)
				{
					LeaveCover(UnityEngine.Random.Range(2f, 5f), "time to move on");
					return false;
				}
				coverPhase = CoverPhase.Peeking;
				coverPhaseUntil = Time.time + UnityEngine.Random.Range(Tuning.F("cover.peek.least", 1.3f), Tuning.F("cover.peek.most", 2.8f));
			}
			break;
		case CoverPhase.Peeking:
			if (Time.time >= coverPhaseUntil || Time.time - hurtAt < 0.3f)
			{
				coverPhase = CoverPhase.Hidden;
				coverPhaseUntil = Time.time + HideFor();
			}
			break;
		}
		if (coverPhase == CoverPhase.Hidden && Time.time >= coverCheckAt && Time.time - coverInAt > Tuning.F("cover.settle", 1.2f))
		{
			coverCheckAt = Time.time + 0.5f;
			Vector3 eye = coverAt + Vector3.up * (coverLow ? CrouchedEyes : (standingHeight - 0.1f * Size));
			bool flag = false;
			foreach (Vector3 item in Threats(point))
			{
				flag |= !Covered(eye, item);
			}
			coverStrikes = (flag ? (coverStrikes + 1) : 0);
			if (coverStrikes >= 2)
			{
				LeaveCover(0.5f, "seen where it hides");
				return false;
			}
		}
		bool flag2 = coverPhase == CoverPhase.Peeking;
		Vector3 vector3 = Flat(((flag2 && !coverLow) ? peekAt : coverAt) - vector);
		move = ((vector3.magnitude > 0.15f) ? (Local(vector3.normalized) * Mathf.Lerp(MotionLibrary.WalkPush * 0.4f, MotionLibrary.WalkPush, Mathf.Clamp01(vector3.magnitude / 1.2f))) : Vector2.zero);
		CoverNoPush(on: true);
		running = false;
		CrouchTo(coverLow && !flag2);
		holdFire = !flag2 || vector3.magnitude > 0.35f;
		return true;
	}

	private bool CoverWhileReloading(float dt)
	{
		if (coverPhase == CoverPhase.None || Tuning.F("cover.on", 1f) < 0.5f)
		{
			return false;
		}
		coverStepAt = Time.time;
		Vector3 vector = Flat(coverAt - Feet);
		if (coverPhase == CoverPhase.Going && vector.magnitude < 0.45f)
		{
			coverPhase = CoverPhase.Hidden;
			coverLeaveAt = Time.time + UnityEngine.Random.Range(Tuning.F("cover.stay.least", 10f), Tuning.F("cover.stay.most", 22f));
			coverInAt = Time.time;
			coverStrikes = 0;
			Lab.Event("cover: in, hidden (reloading)");
		}
		if (coverPhase == CoverPhase.Peeking)
		{
			coverPhase = CoverPhase.Hidden;
		}
		if (coverPhase == CoverPhase.Hidden)
		{
			coverPhaseUntil = Time.time + HideFor();
		}
		bool flag = coverPhase == CoverPhase.Going;
		walk = ((vector.magnitude > 0.15f) ? (Local(vector.normalized) * (flag ? MotionLibrary.WalkPush : (MotionLibrary.WalkPush * 0.7f))) : Vector2.zero);
		running = false;
		CrouchTo(coverLow && !flag);
		Ducking(dt);
		return true;
	}

	private float HideFor()
	{
		return UnityEngine.Random.Range(Tuning.F("cover.hide.least", 0.8f), Tuning.F("cover.hide.most", 2.2f)) * Mathf.Lerp(0.7f, 1.3f, personality.calm / 1.8f);
	}

	private void CrouchTo(bool down)
	{
		crouchPlannedAt = Time.time;
		if (down && !crouchPlan)
		{
			duckDepth = 0f - UnityEngine.Random.Range(0.6f, 0.85f);
		}
		crouchPlan = down;
		duckUntil = (down ? (Time.time + 0.5f) : 0f);
	}

	private Vector2 Local(Vector3 world)
	{
		Vector3 vector = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * world;
		return new Vector2(vector.x, vector.z);
	}

	private System.Collections.Generic.List<Vector3> Threats(Vector3 point)
	{
		System.Collections.Generic.List<Vector3> list = new System.Collections.Generic.List<Vector3> { point + Vector3.up * 0.25f };
		foreach (Vector3 seenEnemy in seenEnemies)
		{
			if (list.Count >= 4)
			{
				break;
			}
			if ((seenEnemy - point).magnitude > 1.5f)
			{
				list.Add(seenEnemy + Vector3.up * 0.25f);
			}
		}
		return list;
	}

	private bool Covered(Vector3 eye, Vector3 threat)
	{
		if (!Physics.Linecast(eye, threat, out var hitInfo, -1, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		if ((hitInfo.point - eye).magnitude > Tuning.F("cover.near", 2.5f))
		{
			return false;
		}
		string name;
		return SolidCover(hitInfo.collider, out name);
	}

	private bool SolidCover(Collider c, out string name)
	{
		name = ((c != null) ? c.name : "");
		if (c == null)
		{
			return false;
		}
		Rigidbody attachedRigidbody = c.attachedRigidbody;
		if (attachedRigidbody == null || attachedRigidbody.isKinematic)
		{
			return c.GetComponentInParent<RigManager>() == null;
		}
		if (attachedRigidbody.GetComponentInParent<RigManager>() != null || HeldByAnyone(attachedRigidbody.transform, null))
		{
			return false;
		}
		MarrowEntity componentInParent = attachedRigidbody.GetComponentInParent<MarrowEntity>();
		if (componentInParent != null)
		{
			name = componentInParent.name;
		}
		Vector3 size = c.bounds.size;
		if (!(attachedRigidbody.mass >= Tuning.F("cover.mass", 25f)))
		{
			if (size.y > 0.6f && Mathf.Max(size.x, size.z) > 0.7f)
			{
				return attachedRigidbody.mass >= 8f;
			}
			return false;
		}
		return true;
	}

	private bool FindSpot(Vector3 feet, System.Collections.Generic.List<Vector3> threats, out Vector3 best, out bool bestLow, out Vector3 bestPeek, out string bestBehind, out Rigidbody bestThing)
	{
		bestThing = null;
		best = feet;
		bestLow = false;
		bestPeek = feet;
		bestBehind = "";
		Vector3 vector = threats[0];
		Vector3 vector2 = Flat(feet - vector);
		vector2 = ((vector2.sqrMagnitude > 0.01f) ? vector2.normalized : (Quaternion.Euler(0f, bodyYaw + 180f, 0f) * Vector3.forward));
		float num = Tuning.F("cover.reach", 13f);
		System.Collections.Generic.List<Vector3> list = new System.Collections.Generic.List<Vector3>();
		System.Collections.Generic.List<(Collider, float)> list2 = new System.Collections.Generic.List<(Collider, float)>();
		foreach (Collider item2 in Physics.OverlapSphere(feet + Vector3.up, num, -1, QueryTriggerInteraction.Ignore))
		{
			if (!(item2 == null))
			{
				Bounds bounds = item2.bounds;
				if (!(bounds.size.y < 0.6f) && !(Mathf.Max(bounds.size.x, bounds.size.z) < 0.45f) && !(bounds.min.y > feet.y + 1f) && SolidCover(item2, out var _))
				{
					list2.Add((item2, Flat(item2.ClosestPoint(feet) - feet).magnitude));
				}
			}
		}
		list2.Sort(((Collider c, float d) a, (Collider c, float d) b) => a.d.CompareTo(b.d));
		for (int i = 0; i < list2.Count && i < 24; i++)
		{
			Collider item = list2[i].Item1;
			Bounds bounds2 = item.bounds;
			Vector3 vector3 = Flat(bounds2.center - vector);
			vector3 = ((vector3.sqrMagnitude > 0.01f) ? vector3.normalized : vector2);
			float num2 = Mathf.Abs(bounds2.extents.x * vector3.x) + Mathf.Abs(bounds2.extents.z * vector3.z);
			float num3 = ((item.attachedRigidbody != null && !item.attachedRigidbody.isKinematic) ? Tuning.F("cover.room.loose", 0.8f) : 0.55f);
			if (num2 < 4f)
			{
				list.Add(new Vector3(bounds2.center.x, feet.y, bounds2.center.z) + vector3 * (num2 + num3));
			}
			Vector3 vector4 = item.ClosestPoint(feet + Vector3.up * 0.8f);
			list.Add(new Vector3(vector4.x, feet.y, vector4.z) + vector3 * num3);
		}
		for (int j = 0; j < 16; j++)
		{
			list.Add(feet + Quaternion.Euler(0f, (float)j * 22.5f + UnityEngine.Random.Range(-8f, 8f), 0f) * Vector3.forward * UnityEngine.Random.Range(2f, num));
		}
		float size = Size;
		float num4 = float.MaxValue;
		foreach (Vector3 item3 in list)
		{
			if (!StandAt(item3, feet, out var vector5))
			{
				continue;
			}
			float magnitude = Flat(vector5 - feet).magnitude;
			if (magnitude > num)
			{
				continue;
			}
			float magnitude2 = Flat(vector - vector5).magnitude;
			if (magnitude2 < Tuning.F("cover.close", 5f) + 1f)
			{
				continue;
			}
			Vector3 vector6 = vector5 + Vector3.up * CrouchedEyes;
			Vector3 vector7 = vector5 + Vector3.up * (standingHeight - 0.1f * size);
			bool flag = true;
			bool flag2 = true;
			string name2 = "";
			Rigidbody rigidbody = null;
			foreach (Vector3 threat in threats)
			{
				if (!Covered(vector6, threat))
				{
					flag = false;
					break;
				}
				if (flag2 && !Covered(vector7, threat))
				{
					flag2 = false;
				}
			}
			if (!flag)
			{
				continue;
			}
			if (Physics.Linecast(vector6, vector, out var hitInfo, -1, QueryTriggerInteraction.Ignore))
			{
				SolidCover(hitInfo.collider, out name2);
				Rigidbody attachedRigidbody = hitInfo.collider.attachedRigidbody;
				rigidbody = ((attachedRigidbody != null && !attachedRigidbody.isKinematic) ? attachedRigidbody : null);
			}
			bool flag3 = !flag2;
			Vector3 vector8 = vector5;
			bool flag4;
			if (flag3)
			{
				flag4 = Seen(vector7 + Vector3.up * 0.1f * size, vector);
			}
			else
			{
				flag4 = false;
				Vector3 vector9 = Vector3.Cross(Vector3.up, Flat(vector - vector5).normalized);
				float[] array = new float[4] { 0.9f, -0.9f, 1.3f, -1.3f };
				foreach (float num5 in array)
				{
					if (StandAt(vector5 + vector9 * num5 * size, vector5, out var vector10) && Seen(vector10 + Vector3.up * (standingHeight - 0.1f * size), vector))
					{
						vector8 = vector10;
						flag4 = true;
						break;
					}
				}
			}
			if (Physics.Linecast(feet + Vector3.up * 0.5f, vector5 + Vector3.up * 0.5f, out var hitInfo2, -1, QueryTriggerInteraction.Ignore) && hitInfo2.collider.GetComponentInParent<RigManager>() == null)
			{
				continue;
			}
			float num6 = magnitude + (flag4 ? 0f : Tuning.F("cover.noshot.cost", 6f)) + (flag3 ? 0f : 0.5f) + Mathf.Max(0f, 9f - magnitude2) * 1.5f;
			foreach (System.Collections.Generic.KeyValuePair<Bot, Vector3> item4 in coverTaken)
			{
				if (item4.Key != this && item4.Key.Alive && Flat(item4.Value - vector5).magnitude < 1.4f)
				{
					num6 += 8f;
				}
			}
			if (num6 < num4)
			{
				num4 = num6;
				best = vector5;
				bestLow = flag3;
				bestPeek = vector8;
				bestBehind = (string.IsNullOrEmpty(name2) ? "something" : name2);
				bestThing = rigidbody;
			}
		}
		return num4 < float.MaxValue;
	}

	private static bool StandAt(Vector3 near, Vector3 from, out Vector3 feet)
	{
		feet = near;
		if (!Physics.Raycast(near + Vector3.up * 1.5f, Vector3.down, out var hitInfo, 3.5f, -1, QueryTriggerInteraction.Ignore) || hitInfo.normal.y < 0.75f)
		{
			return false;
		}
		if (hitInfo.collider.GetComponentInParent<RigManager>() != null || Mathf.Abs(hitInfo.point.y - from.y) > 1.2f)
		{
			return false;
		}
		feet = hitInfo.point;
		return !Physics.CheckCapsule(feet + Vector3.up * 0.45f, feet + Vector3.up * 1.5f, 0.24f, -1, QueryTriggerInteraction.Ignore);
	}

	private static bool Seen(Vector3 eye, Vector3 point)
	{
		if (!Physics.Linecast(eye, point, out var hitInfo, -1, QueryTriggerInteraction.Ignore))
		{
			return true;
		}
		if (!(hitInfo.collider.GetComponentInParent<RigManager>() != null))
		{
			return (hitInfo.point - point).magnitude < 0.6f;
		}
		return true;
	}

	public static void NoteSpawn(Poolee poolee)
	{
		if (!(poolee == null) && !(Time.time < Clone.SuppressUntil))
		{
			recentSpawns.Add((poolee.transform.position, Time.time));
			while (recentSpawns.Count > 0 && Time.time - recentSpawns[0].t > 6f)
			{
				recentSpawns.RemoveAt(0);
			}
		}
	}

	private void PutPresses(BaseController c)
	{
		bool aButton = c._aButton;
		c._aButton = pressA;
		c._aButtonDown = pressA && !aButton;
		c._aButtonUp = !pressA && aButton;
		aButton = c._bButton;
		c._bButton = pressB;
		c._bButtonDown = pressB && !aButton;
		c._bButtonUp = !pressB && aButton;
		aButton = c._thumbstick;
		c._thumbstick = pressClick;
		c._thumbstickDown = pressClick && !aButton;
		c._thumbstickUp = !pressClick && aButton;
		c._menuTap = pressMenu && !menuWas;
		menuWas = pressMenu;
	}

	private static bool Tryable(Rigidbody rb, out string code, out string name)
	{
		code = (name = "");
		Poolee componentInParent = rb.GetComponentInParent<Poolee>();
		if (componentInParent == null || componentInParent.SpawnableCrate == null)
		{
			return false;
		}
		code = componentInParent.SpawnableCrate.Barcode.ID;
		name = Lessons.NameOf(componentInParent);
		string text = componentInParent.gameObject.name.ToUpperInvariant();
		if (Clone.IsBody(code, componentInParent.gameObject.name) || text.Contains("SPAWN") || text.Contains("NIMBUS"))
		{
			return false;
		}
		if (componentInParent.GetComponentInChildren<Gun>() != null || componentInParent.GetComponentInChildren<Magazine>() != null || componentInParent.GetComponentInChildren<AIBrain>() != null)
		{
			return false;
		}
		return true;
	}

	private bool BeginTinker(Vector3 feet)
	{
		string text = (string.IsNullOrEmpty(tinkerWant) ? null : tinkerWant);
		tinkerWant = null;
		pendingCode = null;
		if (FindTinker(feet, text, (text != null) ? Tuning.F("point.thing.far", 40f) : (BotManager.OwnSpawns ? 10f : Tuning.F("tinker.seek", 25f))))
		{
			return true;
		}
		if (!BotManager.OwnSpawns)
		{
			return false;
		}
		Lessons.TellSeen();
		string text2 = text;
		if (text2 == null)
		{
			System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
			System.Collections.Generic.List<float> list2 = new System.Collections.Generic.List<float>();
			float num = 0f;
			foreach (string key in SelfTaught.Data.seen.Keys)
			{
				if (!SelfTaught.Dull(key) && !SelfTaught.IsNever(key) && Spawnable(key))
				{
					string action;
					string what;
					float num2 = 1.2f - SelfTaught.Known(key) + ((SelfTaught.Best(key, out action, out what) >= 0.35f) ? 0.6f : 0f);
					list.Add(key);
					list2.Add(num2);
					num += num2;
				}
			}
			if (list.Count == 0)
			{
				return false;
			}
			float num3 = UnityEngine.Random.value * num;
			text2 = list[list.Count - 1];
			for (int i = 0; i < list.Count; i++)
			{
				num3 -= list2[i];
				if (num3 <= 0f)
				{
					text2 = list[i];
					break;
				}
			}
		}
		try
		{
			AssetWarehouse instance = AssetWarehouse.Instance;
			Transform head = Head;
			if (instance == null || head == null || !instance.HasCrate(new Barcode(text2)))
			{
				return false;
			}
			Vector3 position = head.position + Quaternion.Euler(0f, bodyYaw, 0f) * new Vector3(0f, -0.5f, 1f);
			Clone.Suppress();
			HelperMethods.SpawnCrate(text2, position, Quaternion.identity, Vector3.one);
			pendingCode = text2;
			pendingAt = position;
			pendingUntil = Time.time + 4f;
			Lab.Event("curious: spawned a " + text2 + " to try");
			return true;
		}
		catch (System.Exception ex)
		{
			Lab.Event("curious: could not spawn " + text2 + ": " + ex.Message);
			return false;
		}
	}

	private static bool Spawnable(string code)
	{
		try
		{
			AssetWarehouse instance = AssetWarehouse.Instance;
			if (instance == null || !instance.TryGetCrate(new Barcode(code), out var crate) || crate == null)
			{
				return false;
			}
			return !crate.Redacted && code.IndexOf("FusionContent", System.StringComparison.OrdinalIgnoreCase) < 0;
		}
		catch (System.Exception)
		{
			return false;
		}
	}

	private bool TinkerPending()
	{
		if (pendingCode == null)
		{
			return false;
		}
		if (FindTinker(pendingAt - Vector3.up * 0.5f, pendingCode, 3.5f))
		{
			Lessons.Litter((item != null) ? item.GetComponentInParent<Poolee>() : null);
			pendingCode = null;
			return true;
		}
		if (Time.time > pendingUntil)
		{
			Lab.Event("curious: the " + pendingCode + " it spawned is not something it can pick up and try: cleared away, and not spawned again");
			SelfTaught.Never(pendingCode);
			try
			{
				HashSet<System.IntPtr> hashSet = new HashSet<System.IntPtr>();
				foreach (Collider item in Physics.OverlapSphere(pendingAt, 2.5f, -1, QueryTriggerInteraction.Collide))
				{
					Poolee poolee = ((item != null) ? item.GetComponentInParent<Poolee>() : null);
					if (poolee != null && hashSet.Add(poolee.Pointer) && poolee.SpawnableCrate != null && poolee.SpawnableCrate.Barcode.ID == pendingCode)
					{
						poolee.Despawn();
					}
				}
			}
			catch (System.Exception)
			{
			}
			pendingCode = null;
			return false;
		}
		return true;
	}

	private bool FindTinker(Vector3 feet, string wanted, float within)
	{
		Rigidbody rigidbody = null;
		Grip grip = null;
		string barcode = "";
		string text = "";
		float num = float.MaxValue;
		HashSet<System.IntPtr> hashSet = new HashSet<System.IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(feet + Vector3.up * 0.5f, within, -1, QueryTriggerInteraction.Ignore))
		{
			Rigidbody rigidbody2 = ((item != null) ? item.attachedRigidbody : null);
			if (rigidbody2 == null || rigidbody2.isKinematic || !hashSet.Add(rigidbody2.Pointer) || rigidbody2.mass < 0.05f || rigidbody2.mass > 15f || rigidbody2.GetComponentInParent<RigManager>() != null || !Tryable(rigidbody2, out var code, out var name) || ((wanted != null) ? (code != wanted) : SelfTaught.Dull(code)) || NotForBots(rigidbody2))
			{
				continue;
			}
			Grip grip2 = GripOf(rigidbody2);
			if (grip2 == null)
			{
				continue;
			}
			float num2 = rigidbody2.worldCenterOfMass.y - feet.y;
			if (!(num2 < -0.5f) && !(num2 > 1.8f))
			{
				float num3 = (rigidbody2.worldCenterOfMass - feet).magnitude + SelfTaught.Known(code) * 8f + UnityEngine.Random.value * 2f;
				if (num3 < num)
				{
					num = num3;
					rigidbody = rigidbody2;
					grip = grip2;
					barcode = code;
					text = name;
				}
			}
		}
		if (rigidbody == null)
		{
			return false;
		}
		this.item = rigidbody;
		this.grip = grip;
		gun = null;
		tinkering = true;
		tinkerCode = barcode;
		tinkerName = text;
		play = Play.GoTo;
		playAt = Time.time;
		nav.SetGoal(this.item.worldCenterOfMass);
		Lab.Event("curious: going to try the " + text + " (known " + (SelfTaught.Known(barcode) * 100f).ToString("0") + "%)");
		return true;
	}

	private void StartTrials()
	{
		SelfTaught.Of(tinkerCode, tinkerName).sessions++;
		trialN = 0;
		trialsWanted = Mathf.RoundToInt(Tuning.F("curious.tries", 5f));
		lastTrialAction = "";
		NextTrial();
		Next(Play.Tinker);
	}

	private void NextTrial()
	{
		trialAction = SelfTaught.Choose(tinkerCode, tinkerName, lastTrialAction);
		lastTrialAction = trialAction;
		trialPhase = 0;
		trialAt = Time.time;
	}

	private void TrialBefore()
	{
		Transform head = Head;
		trialBodies.Clear();
		HashSet<System.IntPtr> hashSet = new HashSet<System.IntPtr>();
		Transform transform = ((item != null) ? item.transform.root : null);
		foreach (Collider item in Physics.OverlapSphere(head.position, 12f, -1, QueryTriggerInteraction.Ignore))
		{
			Rigidbody rigidbody = ((item != null) ? item.attachedRigidbody : null);
			if (!(rigidbody == null) && !rigidbody.isKinematic && hashSet.Add(rigidbody.Pointer) && !(rigidbody.transform.root == transform) && !(rigidbody.GetComponentInParent<RigManager>() != null) && !(rigidbody.velocity.magnitude > 0.3f))
			{
				trialBodies.Add((rigidbody, rigidbody.worldCenterOfMass));
				if (trialBodies.Count >= 40)
				{
					break;
				}
			}
		}
		trialParts = Parts(out trialSound);
		trialHead = head.position;
		trialPortals = PortalWays.All.Count;
		trialBegan = Time.time;
	}

	private int Parts(out bool sound)
	{
		sound = false;
		if (item == null)
		{
			return 0;
		}
		Transform root = item.transform.root;
		foreach (AudioSource componentsInChild in root.GetComponentsInChildren<AudioSource>())
		{
			sound |= componentsInChild != null && componentsInChild.isPlaying;
		}
		return root.GetComponentsInChildren<Transform>(includeInactive: false).Length;
	}

	private float TrialAfter(out string what)
	{
		System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
		float num = 0f;
		Transform head = Head;
		int num2 = 0;
		int num3 = 0;
		Vector3 vector = ((this.item != null) ? this.item.worldCenterOfMass : head.position);
		foreach (var (vector2, num4) in recentSpawns)
		{
			if (!((vector2 - vector).magnitude > Tuning.F("curious.near", 3f)))
			{
				num2 += ((num4 >= trialBegan) ? 1 : 0);
				num3 += ((num4 < trialBegan && num4 > trialBegan - 2f) ? 1 : 0);
			}
		}
		if (num2 > 0 && num3 == 0)
		{
			num += 0.6f;
			list.Add("makes things appear");
		}
		int num5 = 0;
		foreach (var trialBody in trialBodies)
		{
			Rigidbody item = trialBody.rb;
			Vector3 item2 = trialBody.at;
			num5 += ((item != null && (item.worldCenterOfMass - item2).magnitude > 0.4f) ? 1 : 0);
		}
		if (num5 > 0)
		{
			num += 0.25f * (float)Mathf.Min(num5, 3);
			list.Add("moves things about");
		}
		bool sound;
		int num6 = Parts(out sound);
		if (num6 != trialParts || (sound && !trialSound))
		{
			num += 0.4f;
			list.Add((num6 != trialParts) ? "it changes" : "it makes a sound");
		}
		if (PortalWays.All.Count != trialPortals)
		{
			num += 0.8f;
			list.Add("opens a portal");
		}
		if ((head.position - trialHead).magnitude > 1.5f)
		{
			num += 0.6f;
			list.Add("carries it off");
		}
		what = string.Join(", ", list);
		return Mathf.Clamp01(num);
	}

	private bool TinkerStep(Transform you, Transform head, float dt)
	{
		walk = Vector2.zero;
		trigger = 0f;
		if (!Holding() || head == null)
		{
			return EndPlay();
		}
		float num = Time.time - trialAt;
		string text = trialAction.Split('/')[0];
		bool flag = trialAction.EndsWith("/down", System.StringComparison.Ordinal);
		float num2 = standingHeight;
		float num3 = num2 / 1.7f;
		Vector3 vector = new Vector3(0.2f * num3, num2 - 0.38f * num3, 0.42f * num3);
		Quaternion quaternion = Quaternion.Euler(flag ? 42f : 0f, 0f, 0f);
		handOverride = true;
		playHeight = num2;
		playPitch = (flag ? 32f : 10f);
		handAt = vector;
		handTurn = quaternion;
		switch (trialPhase)
		{
		case 0:
		{
			float num6 = ((trialN == 0) ? 1.1f : 0.45f);
			if (trialN == 0 && num < num6 - 0.3f)
			{
				handAt = new Vector3(0.16f * num3, num2 - 0.36f * num3, 0.34f * num3);
				handTurn = Quaternion.Euler(20f + Mathf.Sin(num * 5f) * 25f, -20f, Mathf.Sin(num * 4f) * 45f);
				playPitch = 34f;
			}
			if (num >= num6)
			{
				TrialBefore();
				trialPhase = 1;
				trialAt = Time.time;
			}
			return true;
		}
		case 1:
		{
			float num5 = text switch
			{
				"swing" => 0.55f, 
				"shake" => 0.9f, 
				"hold trigger" => 1.1f, 
				_ => 0.25f, 
			};
			switch (text)
			{
			case "trigger":
			case "hold trigger":
				trigger = 1f;
				break;
			case "A":
				pressA = true;
				break;
			case "B":
				pressB = true;
				break;
			case "menu":
				pressMenu = true;
				break;
			case "click":
				pressClick = true;
				break;
			case "shake":
				handAt = vector + new Vector3(Mathf.Sin(num * 38f) * 0.07f, Mathf.Cos(num * 33f) * 0.07f, 0f) * num3;
				break;
			case "swing":
			{
				float t = Mathf.Clamp01(num / num5);
				handAt = Vector3.Lerp(new Vector3(0.3f * num3, num2 - 0.1f * num3, -0.15f * num3), new Vector3(0.1f * num3, num2 - 0.7f * num3, 0.55f * num3), t);
				handTurn = Quaternion.Euler(Mathf.Lerp(-50f, 60f, t), 0f, 0f);
				break;
			}
			}
			if (num >= num5)
			{
				trialPhase = 2;
				trialAt = Time.time;
			}
			return true;
		}
		default:
		{
			if (num < Tuning.F("curious.watch", 1.3f))
			{
				return true;
			}
			string what;
			float num4 = TrialAfter(out what);
			bool flag2 = SelfTaught.Tried(tinkerCode, tinkerName, trialAction, num4, what, head.position);
			Lab.Event("curious: " + tinkerName + ", " + trialAction + ": " + ((num4 > 0.05f) ? (what + " (" + num4.ToString("0.00") + ")") : "nothing"));
			newsSeen = Mathf.Max(newsSeen, SelfTaught.NewsTime);
			if (flag2)
			{
				Spawnables.Notify("A bot worked out the " + tinkerName + " by itself: " + Said(trialAction) + " " + what);
			}
			trialN++;
			if (trialN >= trialsWanted)
			{
				if (TheirWayWith())
				{
					return true;
				}
				then = Then.Drop;
				Next(Play.Drop);
				return true;
			}
			NextTrial();
			return true;
		}
		}
	}

	private bool TheirWayWith()
	{
		try
		{
			Lessons.Lesson lesson = Lessons.With(tinkerCode);
			Hand rightHand = RightHand;
			GameObject gameObject = ((rightHand != null) ? rightHand.m_CurrentAttachedGO : null);
			Poolee poolee = ((gameObject != null) ? gameObject.GetComponentInParent<Poolee>() : null);
			if (lesson == null || poolee == null || enemy)
			{
				return false;
			}
			Rigidbody componentInChildren = poolee.GetComponentInChildren<Rigidbody>();
			string text = tinkerName;
			if (!UseLesson(lesson, atSpot: false))
			{
				return false;
			}
			foreach (Clone.Event @event in lesson.take.events)
			{
				if (@event.barcode == tinkerCode)
				{
					lessonThings[@event.barcode + "#" + Mathf.Max(0, Clone.ThingNumber(@event))] = poolee;
				}
			}
			if (componentInChildren != null)
			{
				heldForLesson = componentInChildren;
				heldForLessonWasKinematic = componentInChildren.isKinematic;
				heldForLessonUntil = Time.time + Tuning.F("curious.theirway.hold", 4f);
				componentInChildren.velocity = Vector3.zero;
				componentInChildren.angularVelocity = Vector3.zero;
				componentInChildren.isKinematic = true;
			}
			Lab.Event("curious: " + text + ", now as you did with it (" + lesson.take.frames[lesson.take.frames.Count - 1].t.ToString("0") + " s)");
			return true;
		}
		catch (System.Exception ex)
		{
			Lab.Event("curious: could not do it your way: " + ex.Message);
			return false;
		}
	}

	private void TheirWayStep()
	{
		try
		{
			if (heldForLesson != null)
			{
				Poolee componentInParent = heldForLesson.GetComponentInParent<Poolee>();
				if ((componentInParent != null && HeldByAnyone(componentInParent.transform, null)) || Time.time > heldForLessonUntil || !InLesson)
				{
					heldForLesson.isKinematic = heldForLessonWasKinematic;
					heldForLesson = null;
				}
			}
		}
		catch (System.Exception)
		{
			heldForLesson = null;
		}
		if (pointBackAt > 0f && Time.time > pointBackAt && !Playing && !InLesson && act != Act.Tinker && mode == Mode.Wander)
		{
			pointBackAt = -1f;
			SetMode(pointBackTo);
		}
	}

	public void TryPointedAt(string code, string name)
	{
		if (mode != Mode.Wander && mode != Mode.Brain)
		{
			pointBackTo = mode;
			pointBackAt = Time.time + 6f;
			SetMode(Mode.Wander);
		}
		StopPlaying();
		tinkerWant = code;
		actUntil = 0f;
		lessonDueAt = Mathf.Max(lessonDueAt, Time.time + 60f);
		Lab.Event("curious: you pointed at the " + name + ", going to try it");
	}

	private static string Said(string action)
	{
		string text = action.Split('/')[0];
		return text switch
		{
			"click" => "clicking the stick", 
			"menu" => "the menu button", 
			"swing" => "swinging it", 
			"shake" => "shaking it", 
			"hold trigger" => "holding the trigger", 
			"trigger" => "pulling the trigger", 
			_ => "the " + text + " button", 
		} + (action.EndsWith("/down", System.StringComparison.Ordinal) ? " (pointed at the ground)" : "");
	}

	private void HearNews()
	{
		if (SelfTaught.NewsTime <= newsSeen || enemy)
		{
			return;
		}
		newsSeen = SelfTaught.NewsTime;
		Transform head = Head;
		if (!(head == null) && !((SelfTaught.NewsAt - head.position).magnitude > Tuning.F("curious.news", 40f)))
		{
			Look(SelfTaught.NewsAt, null, "what it found", Random(1.5f, 3f));
			tinkerWant = SelfTaught.NewsItem;
			if ((mode == Mode.Wander || mode == Mode.Brain) && !Playing && !climbing)
			{
				actUntil = Mathf.Min(actUntil, Time.time + Random(3f, 10f));
			}
			Lab.Event("curious: heard about the " + SelfTaught.NewsName + ", wants to try it");
		}
	}

	public void LabCurious(string wanted)
	{
		if (mode != Mode.Wander && mode != Mode.Brain)
		{
			SetMode(Mode.Wander);
		}
		StopPlaying();
		tinkerWant = wanted;
		actUntil = 0f;
		if (tinkerWant == null)
		{
			tinkerWant = "";
		}
	}

	private Vector3 PickThrowTarget(Transform head)
	{
		Vector3 position = head.position;
		Vector3 vector = Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward;
		Vector3 vector2 = Vector3.zero;
		float num = float.MaxValue;
		foreach (Collider item in Physics.OverlapSphere(position + vector * 9f, 9f, -1, QueryTriggerInteraction.Ignore))
		{
			Rigidbody rigidbody = ((item != null) ? item.attachedRigidbody : null);
			if (rigidbody == null || rigidbody == this.item || rigidbody.isKinematic || rigidbody.GetComponentInParent<RigManager>() != null)
			{
				continue;
			}
			Vector3 worldCenterOfMass = rigidbody.worldCenterOfMass;
			float magnitude = Flat(worldCenterOfMass - position).magnitude;
			if (!(magnitude < 4f) && !(magnitude > 16f) && !Targets.NearPlayer(worldCenterOfMass, 2.5f) && !(Vector3.Angle(vector, Flat(worldCenterOfMass - position)) > 75f))
			{
				float value = UnityEngine.Random.value;
				if ((!Physics.Linecast(position, worldCenterOfMass, out var hitInfo, -1, QueryTriggerInteraction.Ignore) || hitInfo.rigidbody == rigidbody) && value < num)
				{
					num = value;
					vector2 = worldCenterOfMass;
				}
			}
		}
		if (num == float.MaxValue)
		{
			Vector3 vector3 = Quaternion.Euler(0f, bodyYaw + Random(-60f, 60f), 0f) * Vector3.forward;
			Vector3 vector4 = position + vector3 * Random(5f, 13f);
			vector2 = (Physics.Raycast(vector4 + Vector3.up * 2f, Vector3.down, out var hitInfo2, 8f, -1, QueryTriggerInteraction.Ignore) ? hitInfo2.point : (vector4 - Vector3.up * 1.5f));
		}
		throwAt = vector2;
		throwFar = Flat(vector2 - position).magnitude;
		return Flat(vector2 - position).normalized;
	}

	private Vector3 ThrowVelocity(Rigidbody thing)
	{
		SelfTaught.ThrowTry(throwFar, out var speed, out var loft);
		flying = thing;
		flyTarget = throwAt;
		flySpeed = speed;
		flyLoft = loft;
		flyFar = throwFar;
		flySince = Time.time;
		flyNearest = float.MaxValue;
		throwFar = 0f;
		float f = loft * ((float)System.Math.PI / 180f);
		Look(flyTarget, null, "its throw", 2.5f);
		return throwDir * (Mathf.Cos(f) * speed) + Vector3.up * (Mathf.Sin(f) * speed);
	}

	private void TrackThrow()
	{
		if (flying == null)
		{
			flySince = 0f;
			return;
		}
		float num = Time.time - flySince;
		flyNearest = Mathf.Min(flyNearest, (flying.worldCenterOfMass - flyTarget).magnitude);
		if ((num > 0.5f && flying.velocity.magnitude < 0.6f) || !(num < 4f))
		{
			string text = SelfTaught.ThrowResult(flyFar, flySpeed, flyLoft, flyNearest);
			Lab.Event("throw: at " + flyFar.ToString("0") + " m, " + flySpeed.ToString("0.0") + " m/s at " + flyLoft.ToString("0") + " deg: " + text);
			flying = null;
		}
	}

	public bool DriveTheirWay()
	{
		Driving.Model model = Lessons.Road();
		if (model == null)
		{
			return false;
		}
		Vector3 vector = Feet;
		int num = 0;
		for (int i = 1; i < model.road.Count; i++)
		{
			if (Flat(model.road[i] - vector).sqrMagnitude < Flat(model.road[num] - vector).sqrMagnitude)
			{
				num = i;
			}
		}
		System.Collections.Generic.List<Vector3> list = model.road;
		System.Collections.Generic.List<float> list2 = model.roadSpeed;
		bool flag = Flat(list[0] - list[list.Count - 1]).magnitude < 15f && list.Count > 30;
		if (flag)
		{
			System.Collections.Generic.List<Vector3> list3 = new System.Collections.Generic.List<Vector3>();
			System.Collections.Generic.List<float> list4 = new System.Collections.Generic.List<float>();
			for (int j = 0; j < list.Count; j++)
			{
				int index = (num + j) % list.Count;
				list3.Add(list[index]);
				list4.Add(list2[index]);
			}
			list = list3;
			list2 = list4;
			num = 0;
		}
		else if (num > list.Count - 8)
		{
			num = 0;
		}
		Vector3 vector2 = Flat(list[Mathf.Min(num + 4, list.Count - 1)] - list[num]);
		driveFacing = ((vector2.sqrMagnitude > 0.01f) ? vector2.normalized : Vector3.zero);
		if (!DriveTo(list[list.Count - 1], flag ? "a lap of the way you drove" : "driving the way you drove"))
		{
			return false;
		}
		driveRoute = list;
		driveRouteSpeed = list2;
		driveRouteAt = num;
		Vector3 vector3 = Flat(list[Mathf.Min(num + 4, list.Count - 1)] - list[num]);
		if (placeAhead.sqrMagnitude > 0.01f && vector3.sqrMagnitude > 0.01f)
		{
			int num2 = Mathf.Min(num + 1, list.Count - 1);
			for (int k = num2; k < list.Count - 6 && k < num2 + 14; k++)
			{
				bool flag2 = true;
				foreach (Collider item in Physics.OverlapSphere(list[k] + Vector3.up * 0.7f, 1.4f, -1, QueryTriggerInteraction.Ignore))
				{
					if (item != null && item.GetComponentInParent<RigManager>() != rig && (item.attachedRigidbody != null || item.GetComponentInParent<RigManager>() != null))
					{
						flag2 = false;
					}
				}
				foreach (System.Collections.Generic.KeyValuePair<System.IntPtr, System.Collections.Generic.KeyValuePair<Vector3, float>> driveSpot in driveSpots)
				{
					if (driveSpot.Key != rig.Pointer && Time.time < driveSpot.Value.Value && Flat(driveSpot.Value.Key - list[k]).magnitude < Tuning.F("drive.apart", 4f))
					{
						flag2 = false;
					}
				}
				if (flag2)
				{
					num2 = k;
					break;
				}
			}
			driveSpots[rig.Pointer] = new System.Collections.Generic.KeyValuePair<Vector3, float>(list[num2], Time.time + 10f);
			Vector3 vector4 = Flat(list[Mathf.Min(num2 + 4, list.Count - 1)] - list[num2]);
			placeAhead = ((vector4.sqrMagnitude > 0.01f) ? vector4.normalized : vector3.normalized);
			placeAt = list[num2];
			placeAtSet = true;
			driveRouteAt = num2;
		}
		return true;
	}

	public bool DriveTo(Vector3 goal, string why, Transform after = null)
	{
		Lessons.Lesson lesson = Lessons.Drive();
		Transform head = Head;
		if (lesson == null || InLesson || dead || enemy || climbing || head == null)
		{
			return false;
		}
		Driving.Model model = Driving.Of(lesson);
		Poolee poolee = FreeVehicle(model.barcode, Tuning.F("drive.spare", 5f));
		if (poolee == null)
		{
			foreach (Lessons.Lesson item in Lessons.Drives())
			{
				Driving.Model model2 = Driving.Of(item);
				Poolee poolee2 = ((item != lesson) ? FreeVehicle(model2.barcode, Tuning.F("drive.spare", 5f)) : null);
				if (poolee2 != null)
				{
					lesson = item;
					model = model2;
					poolee = poolee2;
					break;
				}
			}
		}
		if (BotManager.OwnSpawns && poolee != null && driveFacing.sqrMagnitude > 0.01f && Vector3.Dot(Flat(poolee.transform.forward).normalized * model.forward, driveFacing) < 0.5f)
		{
			poolee = null;
		}
		if (poolee == null && !BotManager.OwnSpawns)
		{
			driveFacing = Vector3.zero;
			Poolee poolee3 = FreeVehicle(model.barcode, 200f);
			Lab.Event("driving: no " + lesson.name + " standing by within " + Tuning.F("drive.spare", 5f).ToString("0") + " m (nearest free one " + ((poolee3 != null) ? (Flat(poolee3.transform.position - Feet).magnitude.ToString("0.0") + " m off") : "none") + "), and it may not bring one of its own");
			return false;
		}
		Vector3 vector = driveFacing;
		driveFacing = Vector3.zero;
		float num = Mathf.Max(0f, Mathf.Max(model.seatAt - 0.5f, model.driveFrom - 3f));
		Vector3 vector2 = ((poolee == null) ? DriveOffWay(goal) : Vector3.zero);
		if (poolee == null && vector.sqrMagnitude > 0.01f && vector2.sqrMagnitude > 0.01f)
		{
			vector2 = vector;
		}
		if (poolee == null && vector2.sqrMagnitude < 0.01f)
		{
			if (Time.time - driveNoRoomSaid > 20f)
			{
				driveNoRoomSaid = Time.time;
				Lab.Event("driving: no room here for a " + lesson.name + ", not from here");
			}
			driveNextAt = Time.time + 12f;
			return false;
		}
		if (poolee == null)
		{
			ClearEmptyVehicles(model.barcode, 7f);
		}
		if (!UseLesson(lesson, atSpot: false, num, 0, null))
		{
			return false;
		}
		Clone.Frame frame = FrameAt(lesson.take, num);
		cloneFrom = frame.pelvis;
		cloneYaw = Mathf.DeltaAngle(Recorder.Yaw(frame.playspace * frame.head.rotation * Vector3.forward), Recorder.Yaw(head.forward));
		driveModel = model;
		driveMeant = true;
		driving = (driveStopping = (driveOut = false));
		driveGoal = goal;
		driveAfter = after;
		driveWhy = why;
		driveLiveSaid = false;
		driveRoute = ((after != null) ? new System.Collections.Generic.List<Vector3>() : null);
		driveRouteSpeed = null;
		driveRouteAt = 0;
		driveTrail = after != null;
		driveWaitSince = -1f;
		driveNextAt = Time.time + (DriveSkill.Practice ? 4f : (Tuning.F("drive.every", 150f) * Random(0.7f, 1.5f)));
		placeAhead = vector2;
		if (poolee != null && model.sit != null)
		{
			lessonThings[model.sit.barcode + "#" + Mathf.Max(0, Clone.ThingNumber(model.sit))] = poolee;
		}
		Lab.Event("driving: " + why + " (" + Flat(goal - Feet).magnitude.ToString("0") + " m), in " + ((poolee != null) ? ("the " + lesson.name + " standing by") : ("a " + lesson.name + " of its own")));
		return true;
	}

	private Vector3 DriveOffWay(Vector3 goal)
	{
		Vector3 vector = ((rig.physicsRig != null && rig.physicsRig.centerOfPressure != null) ? rig.physicsRig.centerOfPressure.position : Feet);
		Vector3 vector2 = Flat(goal - vector);
		try
		{
			if (drivePath == null)
			{
				drivePath = new NavMeshPath();
			}
			if (NavMesh.SamplePosition(vector, out var hit, 2f, -1) && NavMesh.SamplePosition(goal, out var hit2, 4f, -1) && NavMesh.CalculatePath(hit.position, hit2.position, -1, drivePath) && drivePath.status != NavMeshPathStatus.PathInvalid)
			{
				Vector3[] array = drivePath.corners;
				if (array != null && array.Length >= 2 && Flat(array[1] - vector).magnitude > 1f)
				{
					vector2 = Flat(array[1] - vector);
				}
			}
		}
		catch (System.Exception)
		{
		}
		if (vector2.sqrMagnitude < 0.01f)
		{
			vector2 = Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward;
		}
		vector2.Normalize();
		Vector3 origin = vector + Vector3.up * 0.6f;
		Vector3 result = Vector3.zero;
		float num = float.MinValue;
		for (int i = 0; i < 16; i++)
		{
			Vector3 vector3 = Quaternion.Euler(0f, (float)i * 22.5f, 0f) * vector2;
			float num2 = 14f;
			foreach (RaycastHit item in Physics.SphereCastAll(origin, 0.45f, vector3, 14f, -1, QueryTriggerInteraction.Ignore))
			{
				if (!(item.collider == null) && !(item.distance <= 0f) && !(item.collider.GetComponentInParent<RigManager>() == rig) && !(item.normal.y > 0.55f))
				{
					num2 = Mathf.Min(num2, item.distance);
				}
			}
			if (!(num2 < 4.2f) && Physics.Raycast(vector + vector3 * 2.2f + Vector3.up * 1f, Vector3.down, 2.5f, -1, QueryTriggerInteraction.Ignore))
			{
				float num3 = num2 / 14f + 0.8f * Vector3.Dot(vector3, vector2);
				if (num3 > num)
				{
					num = num3;
					result = vector3;
				}
			}
		}
		return result;
	}

	private void ClearEmptyVehicles(string barcode, float within)
	{
		try
		{
			Transform head = Player.Head;
			HashSet<System.IntPtr> hashSet = new HashSet<System.IntPtr>();
			foreach (Collider item in Physics.OverlapSphere(Feet, within, -1, QueryTriggerInteraction.Ignore))
			{
				Poolee poolee = ((item != null) ? item.GetComponentInParent<Poolee>() : null);
				if (poolee == null || !hashSet.Add(poolee.Pointer) || poolee.SpawnableCrate == null || poolee.SpawnableCrate.Barcode.ID != barcode)
				{
					continue;
				}
				bool flag = HeldByAnyone(poolee.transform, null);
				foreach (Seat componentsInChild in poolee.GetComponentsInChildren<Seat>(includeInactive: true))
				{
					flag |= componentsInChild != null && componentsInChild.rigManager != null;
				}
				if (!flag && (!(head != null) || !(Flat(poolee.transform.position - head.position).magnitude < 4f)) && !AnotherDriverBy(poolee.transform.position, Tuning.F("drive.theirs", 5f)))
				{
					poolee.Despawn();
				}
			}
		}
		catch (System.Exception)
		{
		}
	}

	private bool AnotherDriverBy(Vector3 at, float within)
	{
		foreach (Bot item in BotManager.All)
		{
			if (item != this && !item.dead && item.DrivingLesson && Flat(item.Feet - at).magnitude < within)
			{
				return true;
			}
		}
		return false;
	}

	private Poolee FreeVehicle(string barcode, float within)
	{
		if (string.IsNullOrEmpty(barcode))
		{
			return null;
		}
		Poolee result = null;
		float num = within;
		HashSet<System.IntPtr> hashSet = new HashSet<System.IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(Feet, within, -1, QueryTriggerInteraction.Ignore))
		{
			Poolee poolee = ((item != null) ? item.GetComponentInParent<Poolee>() : null);
			if (poolee == null || !hashSet.Add(poolee.Pointer) || poolee.SpawnableCrate == null || poolee.SpawnableCrate.Barcode.ID != barcode)
			{
				continue;
			}
			bool flag = false;
			foreach (Seat componentsInChild in poolee.GetComponentsInChildren<Seat>(includeInactive: true))
			{
				flag |= componentsInChild != null && componentsInChild.rigManager != null;
			}
			if (!flag && !HeldByAnyone(poolee.transform, null) && !(poolee.transform.up.y < 0.9f) && !NotItsToTake(poolee) && !AnotherDriverBy(poolee.transform.position, 2.5f))
			{
				float magnitude = Flat(poolee.transform.position - Feet).magnitude;
				if (magnitude < num)
				{
					num = magnitude;
					result = poolee;
				}
			}
		}
		return result;
	}

	private Pose DriveFrame(Seat seat)
	{
		if (driveModel.inSeat)
		{
			return Pose.Of(seat.transform);
		}
		Rigidbody rigidbody = Pelvis();
		Vector3 vector = ((rigidbody != null) ? Flat(rigidbody.rotation * Vector3.forward) : Flat(seat.transform.forward));
		return new Pose((rigidbody != null) ? rigidbody.position : seat.transform.position, (vector.sqrMagnitude > 0.001f) ? Quaternion.LookRotation(vector.normalized, Vector3.up) : Quaternion.identity);
	}

	private bool DriveStep(float dt)
	{
		Driving.Model model = driveModel;
		if (model == null)
		{
			return false;
		}
		Seat activeSeat = rig.activeSeat;
		if (!driving)
		{
			driveClockAt = model.driveFrom;
			if (Time.time - cloneStart < model.driveFrom)
			{
				return false;
			}
			cloneStart = Time.time - driveClockAt;
			if (driveWaitSince < 0f)
			{
				driveWaitSince = Time.time;
			}
			if (activeSeat == null || !activeSeat.seated)
			{
				if (Time.time - driveWaitSince > 10f)
				{
					EndLesson("driving: could not get into it");
				}
				return false;
			}
			driving = true;
			driveTookAt = Time.time;
			skill = DriveSkill.Begin(rig.Pointer, out skillTrying);
			skillFar = Flat(driveGoal - activeSeat.transform.position).magnitude;
			skillScored = driveAfter == null && (driveRoute != null || skillFar > 18f);
			skillRouteFrom = driveRouteAt;
			skillBacks = (skillAbouts = 0);
			driveGo = (driveBack = (driveTurn = (driveTurnNow = 0f)));
			driveStuckFor = (driveOverFor = (driveYouOutFor = 0f));
			driveBackUntil = 0f;
			driveBacks = 0;
			driveWayAt = 0f;
			driveWay = new Vector3[0];
			driveWayFound = -100f;
			driveHandOff[0] = (driveHandOff[1] = 0f);
			driveBackFrom = activeSeat.transform.position;
			driveYawHas = false;
			driveAbout = false;
			driveAboutAt = Time.time - 2f;
			driveSpin = 0f;
			Lab.Event("driving: sat in it (" + activeSeat.name + "), off it goes");
		}
		cloneStart = Time.time - driveClockAt;
		Driving.Model live = Driving.Live;
		if (live != null && live != model && live.ok && live.barcode == model.barcode)
		{
			if (!driveLiveSaid)
			{
				Lab.Event("driving: taking up how you are driving now (" + (live.inSeat ? "hands exact" : "hands rough") + ", " + live.top.ToString("0.0") + " m/s, turns " + live.turnLeft.ToString("0.00") + " to " + live.turnRight.ToString("0.00") + ")");
			}
			driveLiveSaid = true;
			model = (driveModel = live);
		}
		if (driveOut)
		{
			if (Time.time >= driveOutAt)
			{
				EndLesson("driving: " + driveEndWhy);
			}
			return true;
		}
		if (activeSeat == null)
		{
			EndLesson("driving: out of its seat");
			return true;
		}
		Vector3 vector = Flat(activeSeat.transform.forward).normalized * model.forward;
		Rigidbody rigidbody = ((activeSeat.seatRb != null) ? activeSeat.seatRb : activeSeat.GetComponentInParent<Rigidbody>());
		Vector3 position = activeSeat.transform.position;
		float num = ((rigidbody != null) ? Vector3.Dot(rigidbody.velocity, vector) : 0f);
		DriveHold(activeSeat, dt);
		if (WaitForRiders(activeSeat))
		{
			driveGo = (driveBack = (driveTurn = 0f));
			return true;
		}
		float num2 = Recorder.Yaw(vector);
		if (driveYawHas && dt > 0.0001f)
		{
			float b = Mathf.DeltaAngle(driveYawWas, num2) * ((float)System.Math.PI / 180f) / dt;
			driveSpin = Mathf.Lerp(driveSpin, b, Mathf.Clamp01(dt * 4f));
			if (num > 2f && Mathf.Abs(driveTurnNow) > 0.05f && Mathf.Abs(driveTurnNow - driveTurn) < 0.05f && Time.time > driveBackUntil + 1f)
			{
				float b2 = Mathf.Clamp(driveSpin / num / driveTurnNow, 0.5f, 1.5f);
				model.answers = Mathf.Lerp(model.answers, b2, Mathf.Clamp01(dt * 0.4f));
			}
		}
		driveYawWas = num2;
		driveYawHas = true;
		_ = Player.Head;
		bool flag = Player.RigManager != null && Player.RigManager.activeSeat != null;
		if (driveAfter != null)
		{
			driveGoal = driveAfter.position - Vector3.up * 1.2f;
		}
		float magnitude = Flat(driveGoal - position).magnitude;
		if (driveTrail && driveRoute != null && (driveRoute.Count == 0 || Flat(driveGoal - driveRoute[driveRoute.Count - 1]).magnitude > 1.5f))
		{
			driveRoute.Add(driveGoal);
			if (driveRoute.Count > 400)
			{
				driveRoute.RemoveAt(0);
				driveRouteAt = Mathf.Max(0, driveRouteAt - 1);
			}
		}
		bool flag2 = driveRoute != null && !driveTrail && driveRouteAt < driveRoute.Count - 3;
		if (driveStopping)
		{
			driveGo = 0f;
			driveBack = ((model.hasBack && num > 1f) ? 0.7f : 0f);
			driveTurn = 0f;
			if (Mathf.Abs(num) < 0.8f || Time.time - driveStopSince > 3f)
			{
				DriveGetOut(activeSeat, atOnce: false);
			}
			return true;
		}
		driveOverFor = ((activeSeat.transform.up.y < 0.4f) ? (driveOverFor + dt) : 0f);
		if (driveOverFor > 1.5f)
		{
			driveEndWhy = "it turned over";
			DriveGetOut(activeSeat, atOnce: true);
			return true;
		}
		if (Time.time - driveTookAt > Tuning.F("drive.longest", 150f))
		{
			DriveStop("long enough");
			return true;
		}
		if (driveAfter == null && !flag2 && magnitude < Tuning.F("drive.arrive", 3.5f))
		{
			DriveStop("there (" + driveWhy + ")");
			return true;
		}
		if (driveAfter != null)
		{
			driveYouOutFor = (flag ? 0f : (driveYouOutFor + dt));
			float num3 = Tuning.F("drive.near", 6f);
			if (magnitude < num3)
			{
				driveGo = 0f;
				driveBack = ((model.hasBack && num > 1f) ? 0.7f : 0f);
				driveTurn = 0f;
				if (driveYouOutFor > 1.5f && Mathf.Abs(num) < 0.8f)
				{
					DriveStop("with you");
				}
				return true;
			}
		}
		float reach = Mathf.Clamp(2.5f + Mathf.Abs(num) * skill[4], 3f, 12f);
		Vector3 vector2 = Flat(DriveAim(position, reach) - position);
		float num4 = ((vector2.sqrMagnitude > 0.01f) ? Vector3.SignedAngle(vector, vector2.normalized, Vector3.up) : 0f);
		if (Time.time >= driveFeelAt)
		{
			driveFeelAt = Time.time + 0.1f;
			feelLength = Mathf.Clamp(4f + Mathf.Abs(num) * 1.6f, 5f, 22f);
			DriveFeel(activeSeat, position, vector, feelLength);
		}
		float num5 = Tuning.F("drive.turn", 1.45f);
		float num6 = model.turnLeft * num5;
		float num7 = model.turnRight * num5;
		float num8 = feelClear[0];
		if (Time.time < driveBackUntil)
		{
			driveGo = ((num < 0f - Tuning.F("drive.back.fast", 2.5f) - 1f) ? 0.4f : 0f);
			driveBack = ((num > 0f - Tuning.F("drive.back.fast", 2.5f)) ? 0.75f : 0f);
			driveTurn = driveBackTurn;
			driveStuckFor = ((Mathf.Abs(num) < 0.25f) ? (driveStuckFor + dt) : 0f);
			if (driveStuckFor > 1.2f || feelBehind < 1.3f || (driveAbout && Mathf.Abs(num4) < 25f))
			{
				driveBackUntil = 0f;
				driveAboutAt = Time.time;
				driveStuckFor = 0f;
			}
			DriveSay(magnitude, num, num8, num4, activeSeat);
			return true;
		}
		bool flag3 = driveGo > 0.45f && Mathf.Abs(num) < 0.35f;
		driveStuckFor = (flag3 ? (driveStuckFor + dt) : Mathf.Max(0f, driveStuckFor - dt * 2f));
		bool flag4 = num8 < Tuning.F("drive.wall", 1.5f) && num < 1.5f && Mathf.Abs(num4) < 60f && feelBehind > 1.5f;
		if (driveStuckFor > 1.3f || (flag4 && Time.time - driveTookAt > 1.5f && Time.time - driveAboutAt > 1.5f))
		{
			if (Flat(position - driveBackFrom).magnitude > 6f)
			{
				driveBacks = 0;
			}
			driveBackFrom = position;
			driveBacks++;
			skillBacks++;
			driveStuckFor = 0f;
			if (!model.hasBack || (float)driveBacks > Tuning.F("drive.backs", 5f))
			{
				SelfTaught.Bad(position);
				DriveStop(model.hasBack ? "it cannot get on" : "stopped by something (it was never shown how to back it)");
				return true;
			}
			float num9 = 0f;
			for (int i = 1; i < feelAngles.Length; i++)
			{
				num9 += feelClear[i] * Mathf.Sign(feelAngles[i]);
			}
			float num10 = ((Mathf.Abs(num9) > 2f) ? Mathf.Sign(num9) : ((num4 >= 0f) ? 1f : (-1f)));
			driveBackTurn = ((num10 > 0f) ? num6 : num7);
			driveAbout = false;
			driveBackUntil = Time.time + Tuning.F("drive.back", 1.7f);
			Lab.Event("driving: stopped by something, backing out (" + driveBacks + ")");
			return true;
		}
		float num11 = Mathf.Max(0.05f, ((num4 > 0f) ? num7 : (0f - num6)) * Mathf.Clamp(model.answers, 0.5f, 1f));
		float num12 = 2f * Mathf.Sin(Mathf.Min(90f, Mathf.Abs(num4)) * ((float)System.Math.PI / 180f)) / Mathf.Max(2f, vector2.magnitude);
		float num13 = ((driveRoute != null) ? Tuning.F("drive.about.road", 120f) : 75f);
		if (model.hasBack && Mathf.Abs(num4) > num13 && num12 > num11 * 1.2f && Time.time - driveAboutAt > 2.5f && feelBehind > 2.5f && num < 4f)
		{
			driveAbout = true;
			driveBackTurn = ((num4 > 0f) ? num6 : num7);
			driveBackUntil = Time.time + Tuning.F("drive.about", 3.5f);
			driveStuckFor = 0f;
			skillAbouts++;
			if ((float)skillAbouts > Tuning.F("drive.lost", 8f))
			{
				DriveStop("it cannot get on (lost its way)");
				return true;
			}
			Lab.Event("driving: its way is " + num4.ToString("0") + " deg off, backing round to face it");
			return true;
		}
		bool flag5 = model.hasBack && Mathf.Abs(num4) > 75f && num12 > num11 * 1.2f;
		float num14 = Mathf.Min(feelLength, Mathf.Max(3f, vector2.magnitude));
		float num15 = Mathf.Clamp(num4, -75f, 75f);
		int num16 = 0;
		float num17 = float.MinValue;
		for (int j = 0; j < feelAngles.Length; j++)
		{
			float num18 = Mathf.Min(feelClear[j], num14) / num14 * 1.6f - Mathf.Abs(feelAngles[j] - num15) / 75f;
			if (num18 > num17)
			{
				num17 = num18;
				num16 = j;
			}
		}
		float num19 = num4;
		float num20 = Mathf.Max(3f, vector2.magnitude);
		if (Mathf.Abs(feelAngles[num16] - num15) > 10f)
		{
			num19 = feelAngles[num16];
			num20 = Mathf.Max(2.5f, feelClear[num16] * 0.7f);
		}
		float num21 = ((Mathf.Abs(num19) > 100f) ? (Mathf.Sign(num19) * 10f) : (2f * Mathf.Sin(num19 * ((float)System.Math.PI / 180f)) / num20));
		float num22 = num21 * skill[3] / Mathf.Clamp(model.answers, 0.6f, 1.5f);
		float num23 = (model.inSeat ? 0.05f : 0.3f);
		if (num > 2.5f && Mathf.Abs(num21) < 0.05f && Mathf.Abs(driveTurnNow - driveTurn) < 0.02f && Time.time > driveBackUntil + 2f)
		{
			model.trim = Mathf.Clamp(model.trim + (num21 - driveSpin / num) * Tuning.F("drive.trim", 0.9f) * (model.inSeat ? 0.3f : 1f) * dt, 0f - num23, num23);
		}
		model.trim = Mathf.Clamp(model.trim, 0f - num23, num23);
		driveTurn = Mathf.Clamp(num22 + model.trim, num6, num7);
		float num24 = model.side * skill[5] / Mathf.Max(4f, num * num);
		driveTurn = Mathf.Clamp(driveTurn, 0f - num24, num24);
		float a = model.top * skill[0];
		float b3 = Mathf.Sqrt(model.side * skill[1] / Mathf.Max(0.02f, Mathf.Abs(driveTurnNow)));
		float num25 = ((num16 == 0) ? feelClear[0] : Mathf.Lerp(feelClear[0], feelClear[num16], 0.5f));
		float a2 = Mathf.Sqrt(2f * skill[2] * Mathf.Max(0.15f, num25 - 2.5f));
		float b4 = 1f + Mathf.Sqrt(5f * Mathf.Max(0f, magnitude - ((driveAfter != null) ? Tuning.F("drive.near", 6f) : Tuning.F("drive.arrive", 3.5f))));
		if (flag2)
		{
			b4 = 99f;
		}
		float a3 = Mathf.Min(Mathf.Min(a, b3), Mathf.Min(a2, b4));
		if (flag2 && driveRouteSpeed != null && driveRouteAt < driveRouteSpeed.Count)
		{
			float num26 = driveRouteSpeed[driveRouteAt];
			for (int k = driveRouteAt; k < driveRouteSpeed.Count && k < driveRouteAt + 5; k++)
			{
				num26 = Mathf.Min(num26, driveRouteSpeed[k]);
			}
			a3 = Mathf.Min(a3, Mathf.Max(2f, num26 + 0.5f));
		}
		if (flag5)
		{
			a3 = Mathf.Min(a3, 2.5f);
		}
		if (Time.time - driveAboutAt < Tuning.F("drive.careful", 3.5f) && driveBacks > 0)
		{
			a3 = Mathf.Min(a3, 2.8f);
		}
		a3 = Mathf.Min(a3, BendsAhead(position, num, model));
		float b5 = Mathf.Clamp((a3 - num) * 0.6f, -1f, 1f);
		float a4 = Mathf.Max(0f, b5);
		if (num < 0.6f && a3 > 0.6f)
		{
			a4 = Mathf.Max(a4, 0.6f);
		}
		driveGo = Mathf.MoveTowards(driveGo, a4, 3f * dt);
		driveBack = ((model.hasBack && num > a3 + 0.8f && num > 1f) ? Mathf.Clamp01((num - a3) * 0.5f) : 0f);
		if (driveBack > 0f)
		{
			driveGo = 0f;
		}
		DriveSay(magnitude, num, num8, num4, activeSeat);
		return true;
	}

	private void DriveSay(float far, float speed, float clear, float off, Seat seat)
	{
		if (!(Time.time - driveSaidAt < Tuning.F("drive.say", 2f)))
		{
			driveSaidAt = Time.time;
			Transform vrRoot = controls.vrRoot;
			Vector3 vector = Flat(seat.transform.forward).normalized * ((driveModel != null) ? driveModel.forward : 1f);
			Rigidbody componentInParent = seat.GetComponentInParent<Rigidbody>();
			float num = ((componentInParent != null) ? Vector3.Dot(componentInParent.velocity, vector) : 0f);
			Lab.Event("driving: yaw " + Recorder.Yaw(vector).ToString("0") + ", along " + num.ToString("0.0") + " m/s, " + far.ToString("0") + " m to go, " + speed.ToString("0.0") + " m/s, go " + driveGo.ToString("0.0") + " back " + driveBack.ToString("0.0") + ", turning " + driveTurnNow.ToString("0.00") + " (wants " + driveTurn.ToString("0.00") + "), its way " + off.ToString("0") + " deg off, clear " + clear.ToString("0.0") + " m, way of " + driveWay.Length + ", behind " + feelBehind.ToString("0.0") + ((BotManager.Missed > 0) ? (", put on by hand " + BotManager.Missed) : "") + ", answers " + driveModel.answers.ToString("0.00") + " trim " + driveModel.trim.ToString("0.00") + ((driveRoute != null) ? (", along a way " + driveRouteAt + "/" + driveRoute.Count) : "") + ((Time.time < driveBackUntil) ? ", backing" : "") + ", hands " + ((LeftHand != null && LeftHand.m_CurrentAttachedGO != null) ? "L" : "-") + ((RightHand != null && RightHand.m_CurrentAttachedGO != null) ? "R" : "-") + ", play space " + Mathf.DeltaAngle(seat.transform.eulerAngles.y, vrRoot.eulerAngles.y).ToString("0") + " deg from its seat");
		}
	}

	private void DriveStop(string why)
	{
		if (!driveStopping)
		{
			driveStopping = true;
			driveStopSince = Time.time;
			driveEndWhy = why;
		}
	}

	private void DriveGetOut(Seat seat, bool atOnce)
	{
		if (skillScored)
		{
			skillScored = false;
			bool flag = atOnce || driveEndWhy.Contains("cannot get on") || driveEndWhy.Contains("stopped by something");
			float num = ((driveRoute != null) ? ((float)Mathf.Max(0, driveRouteAt - skillRouteFrom) * 1.5f) : Mathf.Max(0f, skillFar - Flat(driveGoal - seat.transform.position).magnitude));
			if (num > 12f && (!flag || !(num < 60f)))
			{
				DriveSkill.End(rig.Pointer, skillTrying, num, Time.time - driveTookAt, skillBacks, skillAbouts, flag);
			}
		}
		driveGo = (driveBack = 0f);
		Hand[] array = new Hand[2] { LeftHand, RightHand };
		foreach (Hand hand in array)
		{
			if (hand != null && hand.m_CurrentAttachedGO != null)
			{
				FreeHand(hand);
			}
		}
		cloneHeld[0] = (cloneHeld[1] = null);
		try
		{
			seat.EgressRig(atOnce);
		}
		catch (System.Exception)
		{
		}
		Poolee componentInParent = seat.GetComponentInParent<Poolee>();
		leftVehicle = ((componentInParent != null) ? componentInParent.transform : seat.transform.root);
		if (componentInParent != null)
		{
			Lessons.Litter(componentInParent, Tuning.F("drive.litter", 5f));
		}
		leftVehicleAt = Time.time;
		driveOut = true;
		driveOutAt = Time.time + (atOnce ? 0.4f : 1.3f);
	}

	private void DriveHold(Seat seat, float dt)
	{
		if (Time.time < driveGrabAt)
		{
			return;
		}
		driveGrabAt = Time.time + 0.25f;
		Poolee componentInParent = seat.GetComponentInParent<Poolee>();
		Transform part = ((componentInParent != null) ? componentInParent.transform : seat.transform.root);
		Hand[] array = new Hand[2] { LeftHand, RightHand };
		Clone.Event[] array2 = new Clone.Event[2] { driveModel.leftGrab, driveModel.rightGrab };
		for (int i = 0; i < 2; i++)
		{
			Hand hand = array[i];
			if (hand == null || array2[i] == null)
			{
				continue;
			}
			if (hand.m_CurrentAttachedGO != null)
			{
				driveHandOff[i] = 0f;
				continue;
			}
			driveHandOff[i] += 0.25f;
			Grip grip = null;
			Grip[] array3 = Clone.GripsOf(part);
			string text = Clone.GripName(array2[i]);
			Grip[] array4 = array3;
			foreach (Grip grip2 in array4)
			{
				if (grip2 != null && text != null && grip2.name == text && (grip == null || !grip2.HasAttachedHands()))
				{
					grip = grip2;
				}
			}
			if (grip == null && array2[i].grip >= 0 && array2[i].grip < array3.Length)
			{
				grip = array3[array2[i].grip];
			}
			if (!(grip == null))
			{
				float magnitude = (grip.transform.position - hand.transform.position).magnitude;
				if ((!(magnitude > Tuning.F("lesson.reach", 0.4f)) || !(driveHandOff[i] < 1.5f)) && Grab(hand, grip))
				{
					cloneHeld[i] = grip;
					Lab.Event("driving: its " + ((i == 0) ? "left" : "right") + " hand on the " + grip.name + " (" + magnitude.ToString("0.00") + " m off)");
				}
			}
		}
	}

	private float BendsAhead(Vector3 at, float speed, Driving.Model m)
	{
		float num = 99f;
		float num2 = m.side * skill[1];
		float num3 = skill[2] / Mathf.Max(0.3f, skill[6]);
		float num4 = 6f + Mathf.Abs(speed) * 3f;
		System.Collections.Generic.List<Vector3> list = driveRoute;
		int num5 = driveRouteAt;
		if (list == null || list.Count < 3)
		{
			list = bendWay;
			list.Clear();
			list.Add(at);
			for (int i = driveWayNext; i < driveWay.Length; i++)
			{
				list.Add(driveWay[i]);
			}
			num5 = 0;
		}
		float num6 = Flat(list[Mathf.Min(num5, list.Count - 1)] - at).magnitude;
		for (int j = Mathf.Max(1, num5); j < list.Count - 1; j++)
		{
			if (!(num6 < num4))
			{
				break;
			}
			int index = Mathf.Max(0, j - 2);
			int index2 = Mathf.Min(list.Count - 1, j + 2);
			Vector3 from = Flat(list[j] - list[index]);
			Vector3 to = Flat(list[index2] - list[j]);
			if (j > num5)
			{
				num6 += Flat(list[j] - list[j - 1]).magnitude;
			}
			if (!(from.sqrMagnitude < 0.04f) && !(to.sqrMagnitude < 0.04f))
			{
				float num7 = Vector3.Angle(from, to) * ((float)System.Math.PI / 180f);
				float num8 = Mathf.Max(3f, (from.magnitude + to.magnitude) * 0.5f);
				float num9 = num7 / num8;
				if (!(num9 < 0.04f))
				{
					float num10 = Mathf.Sqrt(num2 / num9);
					float b = Mathf.Sqrt(num10 * num10 + 2f * num3 * Mathf.Max(0f, num6 - 1.5f));
					num = Mathf.Min(num, b);
				}
			}
		}
		return Mathf.Max(2f, num);
	}

	private Vector3 DriveAim(Vector3 at, float reach)
	{
		if (driveRoute != null && driveRoute.Count > 1)
		{
			while (driveRouteAt < driveRoute.Count - 1 && (Flat(driveRoute[driveRouteAt] - at).magnitude < 3f || Flat(driveRoute[driveRouteAt + 1] - at).magnitude < Flat(driveRoute[driveRouteAt] - at).magnitude || Vector3.Dot(Flat(driveRoute[driveRouteAt + 1] - driveRoute[driveRouteAt]), Flat(at - driveRoute[driveRouteAt])) > 0f))
			{
				driveRouteAt++;
			}
			Vector3 vector = at;
			float num = reach;
			for (int i = driveRouteAt; i < driveRoute.Count; i++)
			{
				Vector3 vector2 = Flat(driveRoute[i] - vector);
				float magnitude = vector2.magnitude;
				if (magnitude >= num && magnitude > 0.001f)
				{
					return vector + vector2 / magnitude * num;
				}
				num -= magnitude;
				vector = new Vector3(driveRoute[i].x, vector.y, driveRoute[i].z);
			}
			if (!driveTrail)
			{
				return driveRoute[driveRoute.Count - 1];
			}
			return driveGoal;
		}
		if (Time.time >= driveWayAt || driveWay.Length == 0)
		{
			driveWayAt = Time.time + 1f;
			bool flag = false;
			try
			{
				if (drivePath == null)
				{
					drivePath = new NavMeshPath();
				}
				if (NavMesh.SamplePosition(at, out var hit, 2f, -1) && Mathf.Abs(hit.position.y - at.y) < 1.5f && NavMesh.SamplePosition(driveGoal, out var hit2, 4f, -1) && NavMesh.CalculatePath(hit.position, hit2.position, -1, drivePath) && drivePath.status == NavMeshPathStatus.PathComplete)
				{
					Vector3[] array = drivePath.corners;
					if (array != null && array.Length >= 2 && Flat(array[^1] - driveGoal).magnitude < 4f)
					{
						driveWay = array;
						driveWayNext = 1;
						driveWayFound = Time.time;
						flag = true;
					}
				}
			}
			catch (System.Exception)
			{
			}
			if (!flag && (driveWay.Length == 0 || Time.time - driveWayFound > 6f || driveAfter != null))
			{
				driveWay = new Vector3[2] { at, driveGoal };
				driveWayNext = 1;
			}
		}
		while (driveWayNext < driveWay.Length - 1 && Flat(driveWay[driveWayNext] - at).magnitude < 2.5f)
		{
			driveWayNext++;
		}
		Vector3 vector3 = at;
		float num2 = reach;
		for (int j = driveWayNext; j < driveWay.Length; j++)
		{
			Vector3 vector4 = Flat(driveWay[j] - vector3);
			float magnitude2 = vector4.magnitude;
			if (magnitude2 >= num2 && magnitude2 > 0.001f)
			{
				return vector3 + vector4 / magnitude2 * num2;
			}
			num2 -= magnitude2;
			vector3 = new Vector3(driveWay[j].x, vector3.y, driveWay[j].z);
		}
		return driveWay[driveWay.Length - 1];
	}

	private void DriveFeel(Seat seat, Vector3 at, Vector3 ahead, float length)
	{
		Poolee componentInParent = seat.GetComponentInParent<Poolee>();
		Transform parent = ((componentInParent != null) ? componentInParent.transform : seat.transform.root);
		Vector3 vector = at + Vector3.up * Tuning.F("drive.feel.up", 0.35f);
		for (int i = 0; i < feelAngles.Length; i++)
		{
			Vector3 vector2 = Quaternion.Euler(0f, feelAngles[i], 0f) * ahead;
			float num = length;
			foreach (RaycastHit item in Physics.SphereCastAll(vector, 0.25f, vector2, length, -1, QueryTriggerInteraction.Ignore))
			{
				Collider collider = item.collider;
				if (!(collider == null) && !(item.distance >= num) && !(item.distance <= 0f) && !collider.transform.IsChildOf(parent) && !(collider.GetComponentInParent<RigManager>() == rig) && !(item.normal.y > 0.55f))
				{
					Rigidbody attachedRigidbody = collider.attachedRigidbody;
					if (!(attachedRigidbody != null) || attachedRigidbody.isKinematic || !(attachedRigidbody.mass < 8f) || !(collider.GetComponentInParent<RigManager>() == null))
					{
						num = item.distance;
					}
				}
			}
			float num2 = Mathf.Min(num, 4f);
			if (num2 > 1.5f && !Physics.Raycast(vector + vector2 * num2 + Vector3.up * 0.8f, Vector3.down, 3.2f, -1, QueryTriggerInteraction.Ignore))
			{
				num = Mathf.Min(num, 1.2f);
			}
			feelClear[i] = num;
		}
		feelBehind = 5f;
		foreach (RaycastHit item2 in Physics.SphereCastAll(vector, 0.3f, -ahead, 5f, -1, QueryTriggerInteraction.Ignore))
		{
			Collider collider2 = item2.collider;
			if (!(collider2 == null) && !(item2.distance <= 0f) && !collider2.transform.IsChildOf(parent) && !(collider2.GetComponentInParent<RigManager>() == rig) && !(item2.normal.y > 0.55f))
			{
				feelBehind = Mathf.Min(feelBehind, item2.distance);
			}
		}
		if (!Physics.Raycast(vector - ahead * 2.5f + Vector3.up * 0.8f, Vector3.down, 3.2f, -1, QueryTriggerInteraction.Ignore))
		{
			feelBehind = Mathf.Min(feelBehind, 1f);
		}
	}

	private void DrivePose()
	{
		Driving.Model model = driveModel;
		Seat activeSeat = rig.activeSeat;
		poseWhy = ((model == null) ? "no model" : ((activeSeat == null) ? "no seat" : ""));
		if (model == null || activeSeat == null || !CloneAt(driveClockAt, out var a, out var b, out var k))
		{
			if (poseWhy.Length == 0)
			{
				poseWhy = "no frame at " + driveClockAt.ToString("0.0");
			}
			return;
		}
		posed++;
		rig.remapHeptaRig.inWeight = 1f;
		Pose obj = DriveFrame(activeSeat);
		driveTurnNow = Mathf.MoveTowards(maxDelta: (model.turnRight - model.turnLeft) * Tuning.F("drive.hands", 1.6f) * Time.deltaTime, current: driveTurnNow, target: driveTurn);
		model.Hands(driveTurnNow, out var l, out var r);
		Pose pose = obj * l;
		Pose pose2 = obj * r;
		if (!Bad(pose.position) && !Bad(pose2.position))
		{
			controls.leftController.transform.SetPositionAndRotation(pose.position, pose.rotation);
			controls.rightController.transform.SetPositionAndRotation(pose2.position, pose2.rotation);
		}
		Vector3 position = a.head.position;
		if (Mathf.Abs(cloneSize - 1f) > 0.02f)
		{
			position.y *= cloneSize;
		}
		controls.headset.localPosition = position;
		float y = Mathf.Clamp(driveTurnNow * Tuning.F("drive.look", 70f), -35f, 35f);
		Vector3 vector = Flat(activeSeat.transform.forward);
		float value = (0f - Mathf.Asin(Mathf.Clamp((model.head * Vector3.forward).y, -1f, 1f))) * 57.29578f;
		if (vector.sqrMagnitude > 0.001f)
		{
			controls.headset.rotation = Quaternion.LookRotation(vector.normalized * model.forward, Vector3.up) * Quaternion.Euler(Mathf.Clamp(value, -25f, 35f), y, 0f);
		}
		driveAimLeft = pose.position;
		driveAimRight = pose2.position;
		PutInput(controls.leftController, a.leftIn, b.leftIn, k, trigger: false);
		PutInput(controls.rightController, a.rightIn, b.rightIn, k, trigger: false);
		rig.remapHeptaRig._crouchTarget = a.crouch;
		rig.remapHeptaRig._feetOffset = a.feet;
		Vector2 leftStick = Vector2.zero;
		Vector2 rightStick = Vector2.zero;
		float leftPull = 0f;
		float rightPull = 0f;
		Put(model.go, model.goSign * driveGo);
		if (model.hasBack)
		{
			Put(model.back, model.backSign * driveBack);
		}
		if (model.stickSteer && Mathf.Abs(model.steerGain) > 0.001f)
		{
			Put(model.steer, Mathf.Clamp(driveTurnNow / model.steerGain, -1f, 1f));
		}
		Pull(controls.leftController, Mathf.Clamp01(leftPull));
		Pull(controls.rightController, Mathf.Clamp01(rightPull));
		leftStick = Vector2.ClampMagnitude(leftStick, 1f);
		rightStick = Vector2.ClampMagnitude(rightStick, 1f);
		if (controls.leftController != null)
		{
			controls.leftController._thumbstickAxis = leftStick;
		}
		if (controls.rightController != null)
		{
			controls.rightController._thumbstickAxis = rightStick;
		}
		controls._axisPrimary = leftStick;
		controls._axisPrimaryDirty = leftStick != Vector2.zero;
		void Put(Driving.Channel c, float v)
		{
			switch (c)
			{
			case Driving.Channel.LeftStickY:
				leftStick.y += v;
				break;
			case Driving.Channel.RightStickY:
				rightStick.y += v;
				break;
			case Driving.Channel.LeftStickX:
				leftStick.x += v;
				break;
			case Driving.Channel.RightStickX:
				rightStick.x += v;
				break;
			case Driving.Channel.LeftTrigger:
				leftPull += v;
				break;
			case Driving.Channel.RightTrigger:
				rightPull += v;
				break;
			}
		}
	}

	private static void Pull(BaseController c, float pull)
	{
		if (!(c == null))
		{
			bool primaryInteractionButton = c._primaryInteractionButton;
			bool flag = pull > 0.5f;
			c._primaryAxis = pull;
			c._primaryInteractionButton = flag;
			c._primaryInteractionButtonDown = flag && !primaryInteractionButton;
			c._primaryInteractionButtonUp = !flag && primaryInteractionButton;
		}
	}

	private Poolee AnyVehicle(float within)
	{
		Poolee result = null;
		float num = within;
		foreach (Lessons.Lesson item in Lessons.Drives())
		{
			Poolee poolee = FreeVehicle(Driving.Of(item).barcode, within);
			if (!(poolee == null))
			{
				float magnitude = Flat(poolee.transform.position - Feet).magnitude;
				if (magnitude < num)
				{
					num = magnitude;
					result = poolee;
				}
			}
		}
		return result;
	}

	private bool ToAVehicle(Transform you, float far, float dt)
	{
		if (BotManager.OwnSpawns || you == null || far < Tuning.F("drive.follow", 10f) || InLesson)
		{
			return false;
		}
		RigManager rigManager = Player.RigManager;
		if (rigManager == null || rigManager.activeSeat == null)
		{
			kartSought = null;
			return false;
		}
		if (Time.time >= kartSeekAt)
		{
			kartSeekAt = Time.time + 2f;
			kartSought = AnyVehicle(Tuning.F("drive.follow.seek", 35f));
			if (kartSought != null)
			{
				nav.SetGoal(kartSought.transform.position - Flat(kartSought.transform.right).normalized * 1.2f);
			}
		}
		if (kartSought == null)
		{
			return false;
		}
		if (Flat(kartSought.transform.position - Feet).magnitude < Tuning.F("drive.spare", 5f) - 1f)
		{
			driveFollowAt = 0f;
			return DriveAfterYou(you, far);
		}
		following = true;
		running = true;
		Travel(MotionLibrary.RunPush, 1.5f, dt);
		return true;
	}

	private bool DriveAfterYou(Transform you, float far)
	{
		if (you == null || far < Tuning.F("drive.follow", 10f) || Time.time < driveFollowAt)
		{
			return false;
		}
		driveFollowAt = Time.time + 4f;
		RigManager rigManager = Player.RigManager;
		if (rigManager == null || rigManager.activeSeat == null)
		{
			return false;
		}
		return DriveTo(you.position - Vector3.up * 1.2f, "after you", you);
	}

	public void LabDrive(string where)
	{
		if (Lessons.Drive() == null)
		{
			Lab.Event("driving: nothing it has been shown how to drive (" + Lessons.DriveSaid() + ")");
			return;
		}
		Vector3 vector = Feet;
		Transform head = Player.Head;
		bool flag;
		switch (where)
		{
		case "skill":
		case "practice":
		case "practice off":
		case "skill reset":
			if (where.StartsWith("practice"))
			{
				DriveSkill.Practice = where == "practice";
			}
			if (where == "skill reset")
			{
				DriveSkill.Reset();
			}
			if (DriveSkill.Practice && mode != Mode.Wander && mode != Mode.Brain)
			{
				SetMode(Mode.Wander);
			}
			Lab.Event("drive skill: " + DriveSkill.Said());
			return;
		case "tidy":
		{
			int num = 0;
			HashSet<System.IntPtr> hashSet = new HashSet<System.IntPtr>();
			foreach (Seat item in UnityEngine.Object.FindObjectsOfType<Seat>())
			{
				Poolee poolee = ((item != null) ? item.GetComponentInParent<Poolee>() : null);
				if (!(poolee == null) && hashSet.Add(poolee.Pointer) && !(item.rigManager != null) && poolee.gameObject.activeInHierarchy && (!(head != null) || !(Flat(poolee.transform.position - head.position).magnitude < 6f)))
				{
					poolee.Despawn();
					num++;
				}
			}
			Lab.Event("driving: cleared away " + num + " left standing about");
			return;
		}
		case "road":
			if (!DriveTheirWay())
			{
				Lab.Event("driving: no way of yours kept on this level");
			}
			return;
		case "me":
		case "follow":
			if (head != null)
			{
				flag = DriveTo(head.position - Vector3.up * 1.2f, (where == "me") ? "to you" : "after you", (where == "follow") ? head : null);
				break;
			}
			goto default;
		default:
		{
			if (!FarPlace(vector, out var place))
			{
				place = vector + Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward * 30f;
			}
			flag = DriveTo(place, "going for a drive");
			break;
		}
		}
		if (!flag)
		{
			Lab.Event("driving: could not set off (" + (InLesson ? "in a lesson" : (dead ? "dead" : (enemy ? "an enemy" : (climbing ? "climbing" : "?")))) + ")");
		}
	}

	private void IgnoreGunCar(Transform car, bool ignore)
	{
		try
		{
			if (car == null || gun == null)
			{
				return;
			}
			Transform transform = GunWeight.RootOf(gun);
			foreach (Collider componentsInChild in car.GetComponentsInChildren<Collider>(includeInactive: true))
			{
				if (componentsInChild == null || componentsInChild.isTrigger)
				{
					continue;
				}
				foreach (Collider componentsInChild2 in transform.GetComponentsInChildren<Collider>(includeInactive: true))
				{
					if (componentsInChild2 != null && !componentsInChild2.isTrigger)
					{
						Physics.IgnoreCollision(componentsInChild, componentsInChild2, ignore);
					}
				}
			}
		}
		catch (System.Exception)
		{
		}
	}

	private bool RideLineClear(Vector3 from, Vector3 to, out string blockedBy)
	{
		blockedBy = "";
		Vector3 vector = to - from;
		float magnitude = vector.magnitude;
		if (magnitude < 0.05f)
		{
			return true;
		}
		foreach (RaycastHit item in Physics.RaycastAll(from, vector / magnitude, magnitude - 0.3f, -1, QueryTriggerInteraction.Ignore))
		{
			Collider collider = item.collider;
			if (collider == null || (gun != null && collider.transform.IsChildOf(GunWeight.RootOf(gun))))
			{
				continue;
			}
			RigManager componentInParent = collider.GetComponentInParent<RigManager>();
			if (!(componentInParent == rig))
			{
				if (ridingCar != null && collider.transform.IsChildOf(ridingCar))
				{
					blockedBy = "its own car (" + collider.name + ", " + item.distance.ToString("0.00") + " m out)";
					return false;
				}
				if (componentInParent != null && (rideTarget.bot == null || componentInParent != rideTarget.bot.rig) && !rideTarget.player)
				{
					blockedBy = ((componentInParent == Player.RigManager) ? "you" : "a body");
					return false;
				}
			}
		}
		return true;
	}

	private void RideShoot(Transform head, float dt)
	{
		if (gun == null || !Holding() || head == null || Tuning.F("ride.shoot", 0f) < 0.5f)
		{
			handOverride = false;
			return;
		}
		Transform firePointTransform = gun.firePointTransform;
		if (firePointTransform == null)
		{
			return;
		}
		Seat activeSeat = rig.activeSeat;
		Vector3 forward = ((activeSeat != null) ? Flat(activeSeat.transform.forward) : Vector3.zero);
		if (forward.sqrMagnitude > 0.01f)
		{
			bodyYaw = Recorder.Yaw(forward);
		}
		float num = 0f;
		if (activeSeat != null && ridingCar != null)
		{
			num = Mathf.Sign(ridingCar.InverseTransformPoint(activeSeat.transform.position).x);
		}
		if (Time.time >= rideLookAt)
		{
			rideLookAt = Time.time + 0.3f;
			Targets.Target target = rideTarget;
			if (!Targets.Find(head.position, Tuning.F("ride.shoot.range", 50f), rig, out rideTarget))
			{
				rideTarget = default(Targets.Target);
			}
			if (rideTarget.transform != null && num != 0f && ridingCar != null)
			{
				Vector3 vector = ridingCar.InverseTransformPoint(rideTarget.Point);
				float num2 = Mathf.Atan2(vector.x, vector.z) * 57.29578f * num;
				if (num2 < Tuning.F("ride.side.from", 25f) || num2 > Tuning.F("ride.side.to", 155f))
				{
					rideTarget = default(Targets.Target);
				}
			}
			if (rideTarget.transform != null && rideTarget.transform != target.transform)
			{
				rideTargetSince = Time.time;
				if (Time.time - rideSaidAt > 3f)
				{
					rideSaidAt = Time.time;
					Lab.Event("drive-by: taking aim at " + rideTarget.transform.name + " from the car (" + (rideTarget.Point - head.position).magnitude.ToString("0") + " m)");
				}
			}
		}
		if (rideTarget.transform == null || !Targets.Alive(rideTarget))
		{
			Pull(on: false);
			Aim(head.position + Quaternion.Euler(0f, bodyYaw, 0f) * new Vector3(0f, -1f, 3f), head, Hold.Low);
			return;
		}
		Vector3 vector2 = rideTarget.Point + AimSpot(head.position);
		Aim(vector2, head, Hold.Aimed);
		lastAimPoint = vector2;
		float num3 = Vector3.Angle(firePointTransform.forward, vector2 - firePointTransform.position);
		string blockedBy;
		bool flag = RideLineClear(firePointTransform.position, vector2, out blockedBy);
		if (!flag && Time.time - rideBlockedSaidAt > 3f)
		{
			rideBlockedSaidAt = Time.time;
			Lab.Event("drive-by: holding fire, " + blockedBy + " in the way");
		}
		bool flag2 = Time.time - rideTargetSince > reaction && num3 < Tuning.F("ride.shoot.within", 6f);
		if (Time.time - rideStatusAt > 2f)
		{
			rideStatusAt = Time.time;
			Rigidbody rigidbody = Pelvis();
			Vector3 v = ((rigidbody != null) ? (Quaternion.Inverse(Quaternion.Euler(0f, bodyYaw, 0f)) * (vector2 - rigidbody.position)) : Vector3.zero);
			Lab.Event("drive-by aim: muzzle " + num3.ToString("0") + " deg off, line " + (flag ? "clear" : ("blocked by " + blockedBy)) + ", target " + Recorder.Yaw(Flat(v)).ToString("0") + " deg from its facing, its yaw " + bodyYaw.ToString("0") + ", gun " + (Holding() ? "held" : "NOT held") + (lockSet ? ", kept fast" : ", not kept fast") + ", hand " + ((RightHand != null && controls.rightController != null) ? (RightHand.transform.position - controls.rightController.transform.position).magnitude.ToString("0.00") : "?") + " m from where sent");
		}
		if (flag2 && flag && trigger <= 0.5f && Time.time >= nextPullAt)
		{
			LabRideShots++;
			Lab.Event("drive-by: fires at " + rideTarget.transform.name + " (" + (vector2 - firePointTransform.position).magnitude.ToString("0") + " m, " + num3.ToString("0.0") + " deg off, the car at " + CarSpeed(ridingCar).ToString("0.0") + " m/s, shot " + LabRideShots + ")");
		}
		Fire(flag2 && flag);
	}

	public void PlayExact(ExactClone.Take take, bool loop)
	{
		if (take == null || take.frames.Count < 10)
		{
			Spawnables.Notify("No exact clone recorded yet");
			return;
		}
		ExactClone.Fields();
		if (take.fields == null || string.Join(",", take.fields) != string.Join(",", ExactClone.FieldNames))
		{
			Spawnables.Notify("That exact clone was made by another game version: record again");
			Lab.Event("exact: the recording's controller values are not this game's");
			return;
		}
		StopPlaying();
		ReleaseCloneHands();
		SetMode(Mode.Exact);
		exactTake = take;
		exactLoop = loop;
		exactReady = false;
		string text = BotRig.AvatarOf(rig);
		bool flag = rig.avatar != null && (rig.avatar.transform.localScale - take.avatarScale).sqrMagnitude < 0.0001f;
		if (!string.IsNullOrEmpty(take.avatar) && (take.avatar != text || !flag))
		{
			exactReadyBy = float.MaxValue;
			ExactClone.Take playing = take;
			BotRig.UseAvatar(rig, take.avatar, take.avatarScale, delegate
			{
				if (exactTake == playing && mode == Mode.Exact)
				{
					ExactBegin();
				}
			});
			Lab.Event("exact: putting on your avatar first");
		}
		else
		{
			ExactBegin();
		}
	}

	private void ExactBegin()
	{
		rig.Teleport(exactTake.startFeet);
		exactReady = false;
		exactReadyBy = Time.time + 3f;
		exactBegunAt = Time.time;
		exactCursor = 0;
		exactPulsedTo = -1;
		exactChecked = 0;
		exactDid.Clear();
		exactAsked.Clear();
		exactPouched.Clear();
		exactPending.Clear();
		foreach (Poolee value in exactItems.Values)
		{
			try
			{
				if (value != null && value.gameObject.activeInHierarchy && !HeldByPlayer(value.transform))
				{
					value.Despawn();
				}
			}
			catch (System.Exception)
			{
			}
		}
		exactItems.Clear();
		exBody = (exBodyMost = (exHands = (exHandsMost = (exHeldSame = (exN = 0f)))));
		exAllBody = (exAllBodyMost = (exAllHands = (exAllHandsMost = (exAllHeld = (exAllN = 0f)))));
		exMissed = (exLate = (exExtra = (exOnTime = 0)));
		foreach (ExactClone.Item item in exactTake.items)
		{
			if (!(item.spawnT >= 0f) && !item.pouch)
			{
				Poolee poolee = ExactFindThere(item);
				if (poolee != null)
				{
					exactItems[item.id] = poolee;
				}
				else
				{
					ExactSpawn(item, item.at);
				}
			}
		}
		Lab.Event("exact: getting ready (" + exactTake.Length.ToString("0.0") + " s, " + exactTake.items.Count + " things)");
	}

	private static Poolee ExactFindThere(ExactClone.Item i)
	{
		foreach (Collider item in Physics.OverlapSphere(i.at.position, 0.15f, -1, QueryTriggerInteraction.Ignore))
		{
			Poolee poolee = ((item != null) ? item.GetComponentInParent<Poolee>() : null);
			if (poolee != null && poolee.SpawnableCrate != null && poolee.SpawnableCrate.Barcode.ID == i.barcode && !HeldByPlayer(poolee.transform))
			{
				return poolee;
			}
		}
		return null;
	}

	private void ExactSpawn(ExactClone.Item i, Pose at)
	{
		if (!exactAsked.Add(i.id))
		{
			return;
		}
		HashSet<System.IntPtr> hashSet = new HashSet<System.IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(at.position, 1.5f, -1, QueryTriggerInteraction.Ignore))
		{
			Poolee poolee = ((item != null) ? item.GetComponentInParent<Poolee>() : null);
			if (poolee != null)
			{
				hashSet.Add(poolee.Pointer);
			}
		}
		foreach (Poolee value in exactItems.Values)
		{
			if (value != null)
			{
				hashSet.Add(value.Pointer);
			}
		}
		ExactClone.Suppress();
		Clone.Suppress();
		HelperMethods.SpawnCrate(i.barcode, at.position, at.rotation, Vector3.one);
		exactPending.Add((i, at, hashSet, Time.time));
	}

	private void ExactFindSpawns()
	{
		for (int num = exactPending.Count - 1; num >= 0; num--)
		{
			(ExactClone.Item, Pose, HashSet<System.IntPtr>, float) tuple = exactPending[num];
			Poolee poolee = null;
			foreach (Collider item in Physics.OverlapSphere(tuple.Item2.position, 1.5f, -1, QueryTriggerInteraction.Ignore))
			{
				Poolee poolee2 = ((item != null) ? item.GetComponentInParent<Poolee>() : null);
				if (poolee2 != null && !tuple.Item3.Contains(poolee2.Pointer) && poolee2.SpawnableCrate != null && poolee2.SpawnableCrate.Barcode.ID == tuple.Item1.barcode)
				{
					poolee = poolee2;
					break;
				}
			}
			if (poolee != null)
			{
				exactPending.RemoveAt(num);
				poolee.transform.SetPositionAndRotation(tuple.Item2.position, tuple.Item2.rotation);
				exactItems[tuple.Item1.id] = poolee;
				Lab.Event("exact: its " + poolee.gameObject.name + " came (" + (Time.time - tuple.Item4).ToString("0.00") + " s)");
				foreach (var item2 in exactPending)
				{
					item2.before.Add(poolee.Pointer);
				}
			}
			else if (Time.time - tuple.Item4 > 3f)
			{
				exactPending.RemoveAt(num);
				Lab.Event("exact: its " + tuple.Item1.barcode + " NEVER came");
			}
		}
	}

	private int ExactIdOf(GameObject held)
	{
		Poolee poolee = ((held != null) ? held.GetComponentInParent<Poolee>() : null);
		if (poolee == null)
		{
			return -1;
		}
		foreach (System.Collections.Generic.KeyValuePair<int, Poolee> exactItem in exactItems)
		{
			if (exactItem.Value != null && exactItem.Value.Pointer == poolee.Pointer)
			{
				return exactItem.Key;
			}
		}
		return -2;
	}

	private void ExactPutInHand(ExactClone.Item i)
	{
		if (!exactPouched.Add(i.id) || !exactItems.TryGetValue(i.id, out var value) || value == null)
		{
			return;
		}
		Hand hand = ((i.pouchHand == 0) ? LeftHand : RightHand);
		Grip grip = null;
		foreach (Grip componentsInChild in value.GetComponentsInChildren<Grip>())
		{
			if (componentsInChild != null && componentsInChild.name == i.pouchGrip)
			{
				grip = componentsInChild;
			}
		}
		bool flag = hand != null && grip != null && hand.m_CurrentAttachedGO == null && Grab(hand, grip);
		Lab.Event("exact: " + value.gameObject.name + " put in its " + ((i.pouchHand == 0) ? "left" : "right") + " hand" + ((i.spawnT < 0f) ? " (yours as you began)" : " (from your pouch)") + (flag ? "" : ": NOT taken"));
	}

	private bool ExactAt(float t, out int a, out int b, out float k)
	{
		System.Collections.Generic.List<ExactClone.Frame> frames = exactTake.frames;
		while (exactCursor < frames.Count - 2 && frames[exactCursor + 1].t <= t)
		{
			exactCursor++;
		}
		a = exactCursor;
		b = Mathf.Min(a + 1, frames.Count - 1);
		float num = frames[b].t - frames[a].t;
		k = ((num > 0f) ? Mathf.Clamp01((t - frames[a].t) / num) : 0f);
		return true;
	}

	private void ExactUpdate()
	{
		if (exactTake == null || exactReadyBy == float.MaxValue)
		{
			return;
		}
		ExactFindSpawns();
		if (!exactReady)
		{
			if (Time.time - exactBegunAt < 0.6f)
			{
				return;
			}
			foreach (ExactClone.Item item in exactTake.items)
			{
				Hand hand = ((item.pouchHand == 0) ? LeftHand : RightHand);
				if (item.spawnT < 0f && item.pouch && hand != null)
				{
					ExactSpawn(item, Pose.Of(hand.transform) * (item.handAt.Inverse() * item.at));
				}
			}
			bool flag = true;
			foreach (ExactClone.Item item2 in exactTake.items)
			{
				if (item2.spawnT < 0f && !exactItems.ContainsKey(item2.id))
				{
					flag = false;
				}
			}
			if (!flag && Time.time < exactReadyBy)
			{
				return;
			}
			foreach (ExactClone.Item item3 in exactTake.items)
			{
				if (item3.spawnT < 0f && item3.pouch)
				{
					ExactPutInHand(item3);
				}
			}
			exactReady = true;
			exactStart = Time.time;
			exNextReport = 5f;
			Lab.Event("exact: playing" + (flag ? "" : " (not all your things came in time)"));
		}
		float exactTime = ExactTime;
		ExactClone.Take take = exactTake;
		foreach (ExactClone.Item item4 in take.items)
		{
			if (item4.spawnT < 0f || exactAsked.Contains(item4.id) || exactTime < item4.spawnT - Tuning.F("exact.spawn.lead", 0.05f))
			{
				continue;
			}
			Pose at = item4.at;
			if (item4.pouch && ExactAt(item4.spawnT, out var a, out var _, out var _))
			{
				Pose pose = ((item4.pouchHand == 0) ? take.frames[a].leftHand : take.frames[a].rightHand);
				Hand hand2 = ((item4.pouchHand == 0) ? LeftHand : RightHand);
				if (hand2 != null)
				{
					at = Pose.Of(hand2.transform) * (pose.Inverse() * item4.at);
				}
			}
			ExactSpawn(item4, at);
		}
		foreach (ExactClone.Item item5 in take.items)
		{
			if (item5.pouch && !exactPouched.Contains(item5.id) && !(exactTime < item5.pouchT) && exactItems.TryGetValue(item5.id, out var value) && !(value == null))
			{
				ExactPutInHand(item5);
			}
		}
		ExactMeasure(exactTime);
		ExactCheckGrabs(exactTime, end: false);
		if (exactTime > take.Length)
		{
			ExactCheckGrabs(exactTime, end: true);
			ExactReport(end: true);
			if (exactLoop)
			{
				ExactBegin();
				return;
			}
			exactTake = null;
			SetMode(Mode.Stand);
		}
	}

	private void ExactDrive()
	{
		if (exactTake == null || controls == null || controls.vrRoot == null)
		{
			return;
		}
		ExactClone.Take take = exactTake;
		ExactAt(ExactTime, out var a, out var b, out var k);
		ExactClone.Frame frame = take.frames[a];
		ExactClone.Frame frame2 = take.frames[b];
		Transform vrRoot = controls.vrRoot;
		rig.remapHeptaRig.inWeight = 1f;
		vrRoot.rotation = Pose.Lerp(frame.root, frame2.root, k).rotation;
		if (exactReady)
		{
			Rig physicsRig = rig.physicsRig;
			Vector3 vector = ((physicsRig != null && physicsRig.m_pelvis != null) ? (Vector3.Lerp(frame.pelvis, frame2.pelvis, k) - physicsRig.m_pelvis.position) : Vector3.zero);
			vector.y = 0f;
			if (vector.magnitude > Tuning.F("exact.putback", 1.5f))
			{
				if (Time.time - exactPutBackAt > 2f)
				{
					exactPutBackAt = Time.time;
					Vector3 vector2 = ((physicsRig.centerOfPressure != null) ? physicsRig.centerOfPressure.position : vrRoot.position);
					rig.Teleport(vector2 + vector);
					Lab.Event("exact: put back, " + vector.magnitude.ToString("0.0") + " m off your way");
				}
			}
			else if (vector.magnitude > Tuning.F("exact.ease.from", 0.02f))
			{
				float maxLength = Tuning.F("exact.ease.most", 0.6f) * Time.deltaTime;
				vrRoot.position += Vector3.ClampMagnitude(vector * Mathf.Clamp01(Time.deltaTime * Tuning.F("exact.ease", 3f)), maxLength);
			}
		}
		Pose obj = Pose.Of(vrRoot);
		Pose pose = obj * Pose.Lerp(frame.head, frame2.head, k);
		Pose pose2 = obj * Pose.Lerp(frame.left, frame2.left, k);
		Pose pose3 = obj * Pose.Lerp(frame.right, frame2.right, k);
		controls.headset.SetPositionAndRotation(pose.position, pose.rotation);
		controls.leftController.transform.SetPositionAndRotation(pose2.position, pose2.rotation);
		controls.rightController.transform.SetPositionAndRotation(pose3.position, pose3.rotation);
		if (exactReady)
		{
			ExactClone.Put(controls.leftController, take, exactPulsedTo, a, b, k, left: true);
			ExactClone.Put(controls.rightController, take, exactPulsedTo, a, b, k, left: false);
			exactPulsedTo = a;
			if (Tuning.F("exact.turnstick", 0f) < 0.5f)
			{
				Vector2 thumbstickAxis = controls.rightController._thumbstickAxis;
				controls.rightController._thumbstickAxis = new Vector2(0f, thumbstickAxis.y);
			}
			exactMove = Vector2.Lerp(frame.move, frame2.move, k);
			exactMoving = frame.moving;
		}
		else
		{
			ExactClone.Put(controls.leftController, take, 0, 0, 0, 0f, left: true);
			ExactClone.Put(controls.rightController, take, 0, 0, 0, 0f, left: false);
			BaseController leftController = controls.leftController;
			float solvedGrip = (controls.rightController._solvedGrip = 0f);
			leftController._solvedGrip = solvedGrip;
			exactMove = Vector2.zero;
			exactMoving = false;
		}
	}

	public bool ExactMove(ref Vector2 axis, ref bool pressed)
	{
		if (mode != Mode.Exact)
		{
			return false;
		}
		axis = exactMove;
		pressed = exactMoving;
		return true;
	}

	public void ExactGrip(Grip grip, Hand hand, bool on)
	{
		if (mode == Mode.Exact && exactReady && !(grip == null))
		{
			int num = ((!(hand == LeftHand)) ? ((hand == RightHand) ? 1 : (-1)) : 0);
			if (num >= 0)
			{
				Poolee componentInParent = grip.GetComponentInParent<Poolee>();
				exactDid.Add(new ExactClone.Grab
				{
					t = ExactTime,
					hand = num,
					item = ((componentInParent != null) ? (-2) : (-1)),
					thing = ((componentInParent != null) ? componentInParent.Pointer : System.IntPtr.Zero),
					grip = grip.name,
					on = on
				});
			}
		}
	}

	private void ExactCheckGrabs(float t, bool end)
	{
		System.Collections.Generic.List<ExactClone.Grab> grabs = exactTake.grabs;
		float num = Tuning.F("exact.grab.window", 0.3f);
		foreach (ExactClone.Grab item in exactDid)
		{
			if (item.item != -2 || item.thing == System.IntPtr.Zero)
			{
				continue;
			}
			foreach (System.Collections.Generic.KeyValuePair<int, Poolee> exactItem in exactItems)
			{
				if (exactItem.Value != null && exactItem.Value.Pointer == item.thing)
				{
					item.item = exactItem.Key;
				}
			}
		}
		while (exactChecked < grabs.Count && (end || grabs[exactChecked].t + num < t))
		{
			ExactClone.Grab grab = grabs[exactChecked++];
			ExactClone.Grab grab2 = null;
			foreach (ExactClone.Grab item2 in exactDid)
			{
				if (item2.hand == grab.hand && item2.on == grab.on && item2.item == grab.item && Mathf.Abs(item2.t - grab.t) <= num && (grab2 == null || Mathf.Abs(item2.t - grab.t) < Mathf.Abs(grab2.t - grab.t)))
				{
					grab2 = item2;
				}
			}
			string text = ((grab.hand == 0) ? "left " : "right ") + (grab.on ? "took " : "let go of ") + ExactName(grab.item) + "/" + grab.grip + " at " + grab.t.ToString("0.00") + " s";
			if (grab2 == null)
			{
				exMissed++;
				ExactClone.Grab grab3 = null;
				foreach (ExactClone.Grab item3 in exactDid)
				{
					if (item3.hand == grab.hand && item3.on == grab.on && Mathf.Abs(item3.t - grab.t) <= 1f)
					{
						grab3 = item3;
					}
				}
				Lab.Event("exact: MISSED: you " + text + ((grab3 != null) ? ("; it " + (grab3.on ? "took " : "let go of ") + ExactName(grab3.item) + "/" + grab3.grip + " at " + grab3.t.ToString("0.00") + " s") : "; it did nothing then"));
			}
			else
			{
				exactDid.Remove(grab2);
				if (Mathf.Abs(grab2.t - grab.t) > 0.1f)
				{
					exLate++;
				}
				else
				{
					exOnTime++;
				}
				if (grab2.grip != grab.grip)
				{
					Lab.Event("exact: you " + text + "; it took the " + grab2.grip + " instead");
				}
			}
		}
		for (int num2 = exactDid.Count - 1; num2 >= 0; num2--)
		{
			ExactClone.Grab grab4 = exactDid[num2];
			if (end || !(grab4.t + num * 2f > t))
			{
				exExtra++;
				Lab.Event("exact: EXTRA: it " + ((grab4.hand == 0) ? "left " : "right ") + (grab4.on ? "took " : "let go of ") + ExactName(grab4.item) + "/" + grab4.grip + " at " + grab4.t.ToString("0.00") + " s, you did not");
				exactDid.RemoveAt(num2);
			}
		}
	}

	private string ExactName(int id)
	{
		switch (id)
		{
		case -1:
			return "nothing";
		case -2:
			return "something not yours";
		default:
		{
			if (!exactItems.TryGetValue(id, out var value) || !(value != null))
			{
				return "thing " + id;
			}
			return value.gameObject.name;
		}
		}
	}

	private void ExactMeasure(float t)
	{
		Rig physicsRig = rig.physicsRig;
		if (!(physicsRig == null) && !(physicsRig.m_pelvis == null) && !(LeftHand == null) && !(RightHand == null))
		{
			ExactAt(t, out var a, out var b, out var k);
			ExactClone.Frame frame = exactTake.frames[a];
			ExactClone.Frame frame2 = exactTake.frames[b];
			Vector3 position = physicsRig.m_pelvis.position;
			Vector3 vector = Vector3.Lerp(frame.pelvis, frame2.pelvis, k);
			float magnitude = (position - vector).magnitude;
			Vector3 vector2 = Vector3.Lerp(frame.leftHand.position, frame2.leftHand.position, k) - vector;
			Vector3 vector3 = Vector3.Lerp(frame.rightHand.position, frame2.rightHand.position, k) - vector;
			float num = Mathf.Max((LeftHand.transform.position - position - vector2).magnitude, (RightHand.transform.position - position - vector3).magnitude);
			bool flag = ExactIdOf(LeftHand.m_CurrentAttachedGO) == frame.leftHeld && ExactIdOf(RightHand.m_CurrentAttachedGO) == frame.rightHeld;
			exBody += magnitude;
			exBodyMost = Mathf.Max(exBodyMost, magnitude);
			exHands += num;
			exHandsMost = Mathf.Max(exHandsMost, num);
			exHeldSame += (flag ? 1f : 0f);
			exN += 1f;
			exAllBody += magnitude;
			exAllBodyMost = Mathf.Max(exAllBodyMost, magnitude);
			exAllHands += num;
			exAllHandsMost = Mathf.Max(exAllHandsMost, num);
			exAllHeld += (flag ? 1f : 0f);
			exAllN += 1f;
			if (t >= exNextReport)
			{
				exNextReport += 5f;
				ExactReport(end: false);
			}
		}
	}

	private void ExactReport(bool end)
	{
		float num = (end ? exAllN : exN);
		if (!(num < 1f))
		{
			Lab.Event(end ? ("exact: DONE: body off avg " + Cm(exAllBody / num) + " (most " + Cm(exAllBodyMost) + "), hands off avg " + Cm(exAllHands / num) + " (most " + Cm(exAllHandsMost) + "), held what you held " + (100f * exAllHeld / num).ToString("0") + "% of the time; grabs and lets go: " + exOnTime + " on time, " + exLate + " late, " + exMissed + " missed, " + exExtra + " extra") : ("exact: to " + ExactTime.ToString("0") + " s: body off avg " + Cm(exBody / num) + " (most " + Cm(exBodyMost) + "), hands off avg " + Cm(exHands / num) + " (most " + Cm(exHandsMost) + "), held as you " + (100f * exHeldSame / num).ToString("0") + "%"));
			if (end)
			{
				Spawnables.Notify((exMissed + exExtra == 0 && exAllHandsMost < 0.1f) ? "Exact clone: matched you" : ("Exact clone: " + exMissed + " missed, " + exExtra + " extra grabs (see lab log)"));
			}
			exBody = (exBodyMost = (exHands = (exHandsMost = (exHeldSame = (exN = 0f)))));
		}
	}

	private static string Cm(float m)
	{
		return (m * 100f).ToString("0") + " cm";
	}

	private void LeaveExact()
	{
		exactTake = null;
		exactReady = false;
		exactMove = Vector2.zero;
		exactMoving = false;
		try
		{
			if (controls != null)
			{
				BaseController leftController = controls.leftController;
				float solvedGrip = (controls.rightController._solvedGrip = 0f);
				leftController._solvedGrip = solvedGrip;
				BaseController leftController2 = controls.leftController;
				solvedGrip = (controls.rightController._primaryAxis = 0f);
				leftController2._primaryAxis = solvedGrip;
				BaseController leftController3 = controls.leftController;
				bool primaryInteractionButton = (controls.rightController._primaryInteractionButton = false);
				leftController3._primaryInteractionButton = primaryInteractionButton;
			}
		}
		catch (System.Exception)
		{
		}
	}

	private string FidgetLabel()
	{
		return fid switch
		{
			Fid.Hand => "looking at its hand", 
			Fid.Scratch => "scratching its head", 
			Fid.Neck => "rubbing its neck", 
			Fid.Stretch => "stretching", 
			Fid.Hips => "hands on hips", 
			Fid.Shake => "shaking its hands out", 
			Fid.Down => "looking at the ground", 
			Fid.Behind => "looking behind it", 
			_ => "", 
		};
	}

	private float FidgetTaste(Fid what)
	{
		float num = (float)(personality.seed % 9973) * 0.6180339f * (float)(what + 1);
		return 0.35f + 1.3f * (num - Mathf.Floor(num));
	}

	private void Fidget(Transform you, bool moving, float dt)
	{
		bool flag = !moving && HandsFree && Time.time >= waveUntil && waveDue < 0f && !falling && nearestSeen.transform == null && brainTarget.transform == null && Time.time - flinchAt > 0.6f && Time.time - heardTime > 1.5f && Time.time - landAt > 1.2f && (mode == Mode.Stand || mode == Mode.Follow || mode == Mode.Wander || mode == Mode.Brain);
		if (fid == Fid.None)
		{
			if (!flag || Tuning.F("fidget.every", 0f) <= 0f)
			{
				fidNext = -1f;
				return;
			}
			if (fidNext < 0f)
			{
				fidNext = Time.time + Tuning.F("fidget.every", 0f) * Random(0.5f, 1.6f) * Mathf.Lerp(0.8f, 1.3f, Mathf.InverseLerp(0.5f, 1.8f, personality.calm));
			}
			if (Time.time < fidNext)
			{
				return;
			}
			BeginFidget();
		}
		float num = Time.time - fidStart;
		bool flag2 = flag && num < fidLong;
		fidLevel = Mathf.MoveTowards(fidLevel, flag2 ? 1f : 0f, dt / (flag2 ? 0.45f : (flag ? 0.4f : 0.2f)));
		if (!flag2 && fidLevel <= 0f)
		{
			fid = Fid.None;
			fidNext = -1f;
			return;
		}
		float num2 = Mathf.SmoothStep(0f, 1f, fidLevel);
		float h = pose.height;
		float num3 = fidSide;
		float i = h / 1.7f;
		Transform vrRoot = controls.vrRoot;
		switch (fid)
		{
		case Fid.Hand:
		{
			Vector3 vector = P(num3 * 0.13f, -0.36f, 0.3f);
			Quaternion turn = Quaternion.Euler(-35f, (0f - num3) * 15f, num3 * (-70f + Mathf.Sin(num * 1.9f) * 75f));
			OneHand(num3, vector, turn, num2);
			if (vrRoot != null && fidLevel > 0.5f)
			{
				Look(vrRoot.TransformPoint(vector), null, "its hand", 0.3f);
			}
			curl = Mathf.Lerp(curl, 0.12f, num2);
			break;
		}
		case Fid.Scratch:
		{
			Vector3 at2 = P(num3 * 0.12f + Mathf.Sin(num * 31f) * 0.012f, 0.05f + Mathf.Cos(num * 27f) * 0.012f, -0.05f);
			OneHand(num3, at2, Quaternion.Euler(-105f, (0f - num3) * 40f, 0f), num2);
			pose.head = Quaternion.Euler(9f * num2, 0f, (0f - num3) * 6f * num2) * pose.head;
			curl = Mathf.Lerp(curl, 0.55f, num2);
			break;
		}
		case Fid.Neck:
		{
			Vector3 at = P(num3 * 0.09f + Mathf.Sin(num * 8f) * 0.02f, -0.1f, -0.1f);
			OneHand(num3, at, Quaternion.Euler(-125f, (0f - num3) * 35f, 0f), num2);
			pose.head = Quaternion.Euler(-8f * num2, num3 * 10f * Mathf.Sin(num * 1.6f) * num2, num3 * 8f * num2) * pose.head;
			curl = Mathf.Lerp(curl, 0.45f, num2);
			break;
		}
		case Fid.Stretch:
		{
			float num4 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fidLong * 0.45f, fidLong * 0.9f, num));
			Vector3 b = P(0.24f + 0.3f * num4, 0.4f - 0.3f * num4, 0.02f - 0.1f * num4);
			pose.rightPos = Vector3.Lerp(pose.rightPos, b, num2);
			pose.leftPos = Vector3.Lerp(pose.leftPos, new Vector3(0f - b.x, b.y, b.z), num2);
			pose.rightRot = Quaternion.Slerp(pose.rightRot, Quaternion.Euler(-85f + 50f * num4, 30f * num4, 0f), num2);
			pose.leftRot = Quaternion.Slerp(pose.leftRot, Quaternion.Euler(-85f + 50f * num4, -30f * num4, 0f), num2);
			pose.head = Quaternion.Euler(-14f * num2 * (1f - num4), 0f, 0f) * pose.head;
			curl = Mathf.Lerp(curl, 0.05f, num2);
			break;
		}
		case Fid.Hips:
			pose.rightPos = Vector3.Lerp(pose.rightPos, new Vector3(0.23f * i, h * 0.61f, -0.01f), num2);
			pose.leftPos = Vector3.Lerp(pose.leftPos, new Vector3(-0.23f * i, h * 0.61f, -0.01f), num2);
			pose.rightRot = Quaternion.Slerp(pose.rightRot, Quaternion.Euler(50f, -75f, 0f), num2);
			pose.leftRot = Quaternion.Slerp(pose.leftRot, Quaternion.Euler(50f, 75f, 0f), num2);
			curl = Mathf.Lerp(curl, 0.5f, num2);
			break;
		case Fid.Shake:
		{
			Vector3 vector2 = new Vector3(Mathf.Sin(num * 44f) * 0.03f, 0.1f + Mathf.Cos(num * 39f) * 0.03f, 0.08f) * (num2 * i);
			pose.rightPos += vector2;
			pose.leftPos += new Vector3(0f - vector2.x, vector2.y, vector2.z);
			curl = Mathf.Lerp(curl, 0.05f, num2);
			break;
		}
		case Fid.Down:
			if (vrRoot != null && fidLevel > 0.3f && flag2)
			{
				Look(vrRoot.TransformPoint(new Vector3(num3 * 0.2f * i, 0f, 1.1f * i)), null, "the ground", 0.3f);
			}
			break;
		case Fid.Behind:
			if (vrRoot != null && flag2)
			{
				Look(vrRoot.TransformPoint(new Vector3(num3 * 5f, h, -3f)), null, "behind it", 0.3f);
			}
			break;
		}
		Vector3 P(float x, float fromHead, float z)
		{
			return new Vector3(x * i, h + fromHead * i, z * i);
		}
	}

	private void OneHand(float side, Vector3 at, Quaternion turn, float e)
	{
		if (side > 0f)
		{
			pose.rightPos = Vector3.Lerp(pose.rightPos, at, e);
			pose.rightRot = Quaternion.Slerp(pose.rightRot, turn, e);
		}
		else
		{
			pose.leftPos = Vector3.Lerp(pose.leftPos, at, e);
			pose.leftRot = Quaternion.Slerp(pose.leftRot, turn, e);
		}
	}

	private void BeginFidget()
	{
		(Fid, float, float, float)[] array = new(Fid, float, float, float)[8]
		{
			(Fid.Hand, 1f, 2.4f, 4.2f),
			(Fid.Scratch, 1f, 1.4f, 2.4f),
			(Fid.Neck, 0.8f, 1.8f, 3f),
			(Fid.Stretch, 0.6f, 2.6f, 3.4f),
			(Fid.Hips, 1f, 3f, 7f),
			(Fid.Shake, 0.6f, 0.9f, 1.4f),
			(Fid.Down, 1f, 1.4f, 2.6f),
			(Fid.Behind, 0.9f, 1.3f, 2f)
		};
		float num = 0f;
		(Fid, float, float, float)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			var (fid, num2, _, _) = array2[i];
			if (fid != lastFid)
			{
				num += num2 * FidgetTaste(fid);
			}
		}
		float num3 = UnityEngine.Random.value * num;
		array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			var (fid2, num4, a, b) = array2[i];
			if (fid2 != lastFid)
			{
				num3 -= num4 * FidgetTaste(fid2);
				if (!(num3 > 0f))
				{
					this.fid = (lastFid = fid2);
					fidLong = Random(a, b);
					break;
				}
			}
		}
		if (this.fid != 0)
		{
			fidStart = Time.time;
			fidLevel = 0f;
			bool flag = personality.mirror > 0.5f;
			fidSide = ((UnityEngine.Random.value < 0.75f == flag) ? (-1f) : 1f);
			Lab.Event("fidget: " + FidgetLabel());
		}
	}

	private void FistsOnly(Transform you, Transform head, float dt)
	{
		if (Time.time >= thinkAt)
		{
			thinkAt = Time.time + 0.15f;
			Sense();
		}
		handOverride = false;
		leftOverride = false;
		fists = false;
		running = false;
		if (nearestSeen.transform == null || nearestSeenDistance > 30f)
		{
			walk = Vector2.zero;
			Watch(you, head, dt, 90f);
		}
		else
		{
			Brawl(head, dt);
		}
	}

	private bool Chase(Transform head, Vector3 point, float dt)
	{
		_ = Feet;
		float magnitude = Flat(point - head.position).magnitude;
		float num = point.y - head.position.y;
		if (climbing)
		{
			Travel(MotionLibrary.RunPush, 0.8f, dt);
			return true;
		}
		if (magnitude < Tuning.F("chase.from", 3.2f) && num < Tuning.F("chase.above", 1.1f))
		{
			return false;
		}
		Vector3 vector = new Vector3(point.x, point.y - 1.3f, point.z) + ((magnitude > 4f) ? OwnSide(1.3f) : Vector3.zero);
		if (!nav.HasGoal || (nav.Goal - vector).magnitude > 1.2f || Time.time - chaseSetAt > 2f)
		{
			nav.SetGoal(vector);
			chaseSetAt = Time.time;
		}
		running = magnitude > 2.5f;
		Travel(running ? MotionLibrary.RunPush : MotionLibrary.WalkPush, 0.8f, dt);
		return true;
	}

	private float CloseIn(Transform head, Vector3 point, float reach, float dt)
	{
		Face(point, dt, 320f);
		Vector3 vector = Flat(point - head.position);
		float magnitude = vector.magnitude;
		Vector3 vector2 = ((vector.sqrMagnitude > 0.001f) ? vector.normalized : (Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward));
		Vector3 vector3 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * vector2;
		if (magnitude > reach * 0.85f)
		{
			running = magnitude > 3f;
			float speed = (running ? MotionLibrary.RunPush : MotionLibrary.WalkPush);
			Vector3 vector4 = KeepApart(vector2, ref speed);
			vector3 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * vector4;
			walk = new Vector2(vector3.x, vector3.z) * speed;
		}
		else
		{
			walk = ((magnitude < reach * 0.4f) ? (-new Vector2(vector3.x, vector3.z) * MotionLibrary.WalkPush * 0.6f) : Vector2.zero);
		}
		Vector3 vector5 = Quaternion.Inverse(Quaternion.Euler(0f, bodyYaw, 0f)) * (point - head.position);
		playPitch = Mathf.Clamp((0f - Mathf.Atan2(vector5.y, Mathf.Max(new Vector2(vector5.x, vector5.z).magnitude, 0.1f))) * 57.29578f, -40f, 50f);
		playYaw = Mathf.Clamp(Mathf.Atan2(vector5.x, vector5.z) * 57.29578f, -40f, 40f);
		playHeight = standingHeight * 0.97f;
		return magnitude;
	}

	private bool HitOfTheirs(float since)
	{
		return HitOfTheirs(since, 1f, fist: false, Vector3.zero);
	}

	private bool HitOfTheirs(float since, float pace, bool fist, Vector3 at)
	{
		if (hitClip == null || hitClip.Length < 2)
		{
			return false;
		}
		float num = hitClip[hitClip.Length - 1].time / Mathf.Max(0.2f, pace);
		try
		{
			Hand rightHand = RightHand;
			Hand leftHand = LeftHand;
			Rigidbody rigidbody = Pelvis();
			if (rightHand != null && rightHand.rb != null && leftHand != null && leftHand.rb != null && rigidbody != null)
			{
				hitFast = Mathf.Max(hitFast, Mathf.Max((rightHand.rb.velocity - rigidbody.velocity).magnitude, (leftHand.rb.velocity - rigidbody.velocity).magnitude));
				hitSpin = Mathf.Max(hitSpin, Mathf.Max(rightHand.rb.angularVelocity.magnitude, leftHand.rb.angularVelocity.magnitude));
				hitBody = Mathf.Max(hitBody, rigidbody.velocity.magnitude);
			}
		}
		catch (System.Exception)
		{
		}
		if (since > num)
		{
			Lab.Event("fists: that blow (" + num.ToString("0.0") + " s): hands up to " + hitFast.ToString("0.0") + " m/s, turning " + (hitSpin * 57.29578f).ToString("0") + " deg/s, body " + hitBody.ToString("0.0") + " m/s");
			hitFast = (hitSpin = (hitBody = 0f));
			hitClip = null;
			return false;
		}
		if (Time.time - hitPosedAt < 0.12f)
		{
			handAt = hitRightWas;
			leftAt = hitLeftWas;
			handTurn = hitRightTurnWas;
			leftTurn = hitLeftTurnWas;
		}
		Frame frame = MotionLibrary.Sample(hitClip, since * pace);
		float num2 = ((frame.height > 0.3f) ? (playHeight / frame.height) : 1f);
		Quaternion quaternion = Quaternion.Euler(playPitch, playYaw, 0f);
		Vector3 vector = new Vector3(0f, playHeight, 0f);
		Vector3 vector2 = new Vector3(0f, frame.height * num2, 0f);
		float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(since, num - since) / 0.15f));
		handAt = Vector3.Lerp(handAt, vector + quaternion * (frame.rightPos * num2 - vector2), t);
		handTurn = Quaternion.Slerp(handTurn, (play == Play.Melee) ? (quaternion * frame.rightRot * ToolFix) : (quaternion * frame.rightRot), t);
		leftAt = Vector3.Lerp(leftAt, vector + quaternion * (frame.leftPos * num2 - vector2), t);
		leftTurn = Quaternion.Slerp(leftTurn, quaternion * frame.leftRot, t);
		if (fist)
		{
			float num3 = Mathf.Max(Time.deltaTime, 0.001f);
			handTurn = Lined(handTurn, (handAt - hitRightBefore) / num3, fistOffRight);
			leftTurn = Lined(leftTurn, (leftAt - hitLeftBefore) / num3, fistOffLeft);
		}
		hitRightBefore = handAt;
		hitLeftBefore = leftAt;
		hitRightWas = handAt;
		hitLeftWas = leftAt;
		hitRightTurnWas = handTurn;
		hitLeftTurnWas = leftTurn;
		hitPosedAt = Time.time;
		return true;
	}

	private void MeasureFists()
	{
		try
		{
			Quaternion quaternion = Quaternion.Euler(0f, bodyYaw, 0f);
			Hand rightHand = RightHand;
			Hand leftHand = LeftHand;
			if (rightHand != null && rightHand.rb != null && rightHand.rb.angularVelocity.magnitude < 2f)
			{
				fistOffRight = Quaternion.Slerp(fistOffRight, Quaternion.Inverse(quaternion * handTurn) * rightHand.transform.rotation, 0.1f);
			}
			if (leftHand != null && leftHand.rb != null && leftHand.rb.angularVelocity.magnitude < 2f)
			{
				fistOffLeft = Quaternion.Slerp(fistOffLeft, Quaternion.Inverse(quaternion * leftTurn) * leftHand.transform.rotation, 0.1f);
			}
		}
		catch (System.Exception)
		{
		}
	}

	private Quaternion Lined(Quaternion turn, Vector3 going, Quaternion sits)
	{
		float magnitude = going.magnitude;
		if (magnitude < Tuning.F("punch.line.from", 1.2f) || going.z < 0.3f)
		{
			return turn;
		}
		Vector3 toDirection = going / magnitude;
		float t = Mathf.Clamp01((magnitude - Tuning.F("punch.line.from", 1.2f)) / 2.5f) * Tuning.F("punch.line", 0f);
		Quaternion quaternion = turn * sits;
		Quaternion b = Quaternion.FromToRotation(quaternion * Vector3.up, toDirection) * quaternion * Quaternion.Inverse(sits);
		return Quaternion.Slerp(turn, b, t);
	}

	private Frame[] PickHit(MoveSlots.Kind kind)
	{
		Frame[] array = MoveSlots.Pick(kind, hitLately, avatarCode);
		if (array != null)
		{
			hitLately.Add(array);
			if (hitLately.Count > 1)
			{
				hitLately.RemoveAt(0);
			}
		}
		return array;
	}

	private void Brawl(Transform head, float dt)
	{
		Targets.Target t = nearestSeen;
		if (t.transform == null || head == null)
		{
			walk = Vector2.zero;
			hitClip = null;
			return;
		}
		Vector3 point = t.Point;
		if (Chase(head, point, dt))
		{
			hitClip = null;
			return;
		}
		float num = CloseIn(head, point, FistReach, dt);
		if (hitClip != null && num < 1.3f)
		{
			walk = Vector2.zero;
			running = false;
		}
		handOverride = true;
		leftOverride = true;
		handAt = new Vector3(0.17f, playHeight - 0.3f, 0.28f);
		handTurn = Quaternion.Euler(-10f, -10f, 0f);
		leftAt = new Vector3(-0.17f, playHeight - 0.28f, 0.3f);
		leftTurn = Quaternion.Euler(-10f, 10f, 0f);
		fists = true;
		if (hitClip == null && Time.time - punchAt > 0.6f)
		{
			MeasureFists();
		}
		bool flag = Defend(t, head);
		if (flag && hitClip == null)
		{
			return;
		}
		float num2 = ((hitClip != null && strikePace > 0f) ? strikePace : Tuning.F("punch.speed", 1.2f));
		if (hitClip == null && num < FistReach + 0.15f && Time.time >= nextPunchAt && !flag)
		{
			punchRight = !punchRight;
			hitClip = PickStrike();
			if (strikePace > 0f)
			{
				num2 = strikePace;
			}
			punchAt = Time.time;
			if (Time.time - punchSaidAt > 3f)
			{
				punchSaidAt = Time.time;
				Lab.Event("fists: punches" + ((hitClip != null) ? (" as you do (" + hitClip[hitClip.Length - 1].time.ToString("0.0") + " s)") : " (a plain jab: no Punch slot recorded)"));
			}
			float num3 = ((hitClip != null) ? (hitClip[hitClip.Length - 1].time / num2) : 0.32f);
			if (comboLeft <= 0)
			{
				comboLeft = ((UnityEngine.Random.value < 0.3f) ? 1 : ((UnityEngine.Random.value < 0.6f) ? 2 : 3));
			}
			comboLeft--;
			nextPunchAt = Time.time + num3 + ((comboLeft > 0) ? 0.05f : UnityEngine.Random.Range(0.7f, 1.5f));
		}
		float num4 = Time.time - punchAt;
		if (!HitOfTheirs(num4, num2, fist: true, point) && num4 < 0.32f)
		{
			float t2 = ((num4 < 0.12f) ? (num4 / 0.12f) : Mathf.Clamp01(1f - (num4 - 0.12f) / 0.2f));
			Vector3 b = controls.vrRoot.InverseTransformPoint(point);
			if (punchRight)
			{
				handAt = Vector3.Lerp(handAt, b, Mathf.SmoothStep(0f, 1f, t2));
				handTurn = Quaternion.Euler(-5f, 0f, -80f);
			}
			else
			{
				leftAt = Vector3.Lerp(leftAt, b, Mathf.SmoothStep(0f, 1f, t2));
				leftTurn = Quaternion.Euler(-5f, 0f, 80f);
			}
		}
	}

	private static bool IsMelee(Rigidbody rb)
	{
		try
		{
			Poolee componentInParent = rb.GetComponentInParent<Poolee>();
			if (componentInParent == null || componentInParent.SpawnableCrate == null)
			{
				return false;
			}
			if (componentInParent.GetComponentInChildren<Gun>() != null || componentInParent.GetComponent<Magazine>() != null)
			{
				return false;
			}
			Il2CppSystem.Collections.Generic.List<string> tags = componentInParent.SpawnableCrate.Tags;
			if (tags != null)
			{
				Il2CppSystem.Collections.Generic.List<string>.Enumerator enumerator = tags.GetEnumerator();
				while (enumerator.MoveNext())
				{
					string text = (enumerator.Current ?? "").ToLowerInvariant();
					if (text.Contains("melee") || text.Contains("blade") || text.Contains("blunt"))
					{
						return true;
					}
				}
			}
			if (componentInParent.GetComponentInChildren<StabSlash>() != null)
			{
				return true;
			}
			string text2 = (componentInParent.gameObject.name + " " + componentInParent.SpawnableCrate.Barcode.ID).ToLowerInvariant();
			string[] array = meleeWords;
			foreach (string value in array)
			{
				if (text2.Contains(value))
				{
					return true;
				}
			}
		}
		catch (System.Exception)
		{
		}
		return false;
	}

	private static Grip HandleOf(Rigidbody rb)
	{
		MarrowEntity componentInParent = rb.GetComponentInParent<MarrowEntity>();
		Component obj = ((componentInParent != null) ? ((Component)componentInParent) : ((Component)rb));
		Grip result = null;
		float num = float.MinValue;
		foreach (Grip componentsInChild in obj.GetComponentsInChildren<Grip>(includeInactive: false))
		{
			if (!(componentsInChild == null) && componentsInChild.enabled && !componentsInChild.IsInteractionDisabled && !componentsInChild.HasAttachedHands() && !(componentsInChild.TryCast<WorldGrip>() != null))
			{
				string text = componentsInChild.name.ToLowerInvariant();
				float num2 = (componentsInChild.transform.position - rb.worldCenterOfMass).magnitude;
				if (text.Contains("handle") || text.Contains("hilt") || text.Contains("grip_main") || text.Contains("primary"))
				{
					num2 += 10f;
				}
				if (text.Contains("blade") || text.Contains("head") || text.Contains("rim") || text.Contains("pan"))
				{
					num2 -= 5f;
				}
				if (num2 > num)
				{
					num = num2;
					result = componentsInChild;
				}
			}
		}
		return result;
	}

	private Rigidbody FindMelee(out Grip itsGrip, out float far)
	{
		itsGrip = null;
		far = float.MaxValue;
		Transform head = Head;
		if (head == null)
		{
			return null;
		}
		Vector3 vector = Feet;
		Rigidbody result = null;
		HashSet<System.IntPtr> hashSet = new HashSet<System.IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(vector + Vector3.up * 0.5f, Tuning.F("melee.seek", 22f), -1, QueryTriggerInteraction.Ignore))
		{
			Rigidbody rigidbody = ((item != null) ? item.attachedRigidbody : null);
			if (rigidbody == null || rigidbody.isKinematic || !hashSet.Add(rigidbody.Pointer) || rigidbody.mass < 0.25f || rigidbody.mass > 14f || rigidbody.GetComponentInParent<RigManager>() != null)
			{
				continue;
			}
			float num = rigidbody.worldCenterOfMass.y - vector.y;
			float magnitude = (rigidbody.worldCenterOfMass - vector).magnitude;
			if (!(num < -0.5f) && !(num > 1.6f) && !(magnitude >= far) && IsMelee(rigidbody))
			{
				Grip grip = HandleOf(rigidbody);
				if (!(grip == null) && !HeldByAnyone((rigidbody.transform.root == rigidbody.transform) ? rigidbody.transform : ((rigidbody.GetComponentInParent<Poolee>() != null) ? rigidbody.GetComponentInParent<Poolee>().transform : rigidbody.transform), null) && Targets.InSight(head.position, rigidbody.worldCenterOfMass, rigidbody.transform, rig))
				{
					result = rigidbody;
					itsGrip = grip;
					far = magnitude;
				}
			}
		}
		return result;
	}

	private bool GetAWeapon()
	{
		if (spawningGun)
		{
			return GetAGun();
		}
		Grip itsGrip;
		float far;
		Rigidbody rigidbody = FindMelee(out itsGrip, out far);
		float far2;
		Gun gun = NearestGun(out far2);
		if (rigidbody != null && (gun == null || far < far2 * 0.6f))
		{
			item = rigidbody;
			grip = itsGrip;
			this.gun = null;
			support = null;
			meleeWanted = true;
			tinkering = false;
			play = Play.GoTo;
			playAt = Time.time;
			nav.SetGoal(item.worldCenterOfMass);
			Lab.Event("weapon: going for the " + ThingName(rigidbody) + " (" + far.ToString("0") + " m) to hit with");
			return true;
		}
		meleeWanted = false;
		return GetAGun();
	}

	public void LabLay(string which)
	{
		string text = which;
		if (!which.Contains("."))
		{
			text = null;
			System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
			FieldInfo[] fields = typeof(CommonBarcodes.Melee).GetFields(BindingFlags.Static | BindingFlags.Public);
			foreach (FieldInfo fieldInfo in fields)
			{
				if (!(fieldInfo.FieldType != typeof(string)))
				{
					list.Add(fieldInfo.Name);
					if (string.Equals(fieldInfo.Name, which, System.StringComparison.OrdinalIgnoreCase))
					{
						text = fieldInfo.GetValue(null) as string;
					}
				}
			}
			if (text == null)
			{
				Lab.Event("lay: no such thing; there are " + string.Join(", ", list));
				return;
			}
		}
		Vector3 position = Feet + Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward * 3f + Vector3.up * 0.4f;
		Clone.Suppress();
		HelperMethods.SpawnCrate(text, position, Quaternion.identity, Vector3.one);
		Lab.Event("lay: a " + which + " put 3 m in front of it");
	}

	public void LabArm()
	{
		if (gun != null)
		{
			wontLoad[gun.Pointer] = Time.time;
		}
		if (ownGun != null)
		{
			wontLoad[ownGun.Pointer] = Time.time;
		}
		StopPlaying();
		ownGun = null;
		if (mode != Mode.Brain)
		{
			SetMode(Mode.Brain);
		}
		Grip itsGrip;
		float far;
		Rigidbody rigidbody = FindMelee(out itsGrip, out far);
		if (rigidbody == null)
		{
			Lab.Event("weapon: nothing to hit with in sight");
			return;
		}
		item = rigidbody;
		grip = itsGrip;
		gun = null;
		support = null;
		meleeWanted = true;
		play = Play.GoTo;
		playAt = Time.time;
		nav.SetGoal(item.worldCenterOfMass);
		Lab.Event("weapon: going for the " + ThingName(rigidbody) + " (" + far.ToString("0") + " m) to hit with");
	}

	private static Quaternion ToolTurn(Vector3 points, Vector3 forearm)
	{
		return Quaternion.LookRotation(-points.normalized, forearm.normalized);
	}

	private Quaternion Pointing(Vector3 points, Vector3 forearm)
	{
		try
		{
			if (toolTurnHas && item != null && grip != null && Holding() && item.angularVelocity.magnitude < 3f)
			{
				Vector3 vector = Quaternion.Inverse(Quaternion.Euler(0f, bodyYaw, 0f)) * (item.worldCenterOfMass - grip.transform.position);
				if (vector.sqrMagnitude > 0.0004f)
				{
					Vector3 b = Quaternion.Inverse(toolTurnWas) * vector.normalized;
					toolLocal = Vector3.Slerp(toolLocal, b, Tuning.F("melee.fix", 0.12f)).normalized;
				}
			}
		}
		catch (System.Exception)
		{
		}
		toolTurnWas = ToolTurn(points, forearm) * ToolFix;
		toolTurnHas = true;
		return toolTurnWas;
	}

	private void MeasureMelee()
	{
		float num = 0.3f;
		try
		{
			Vector3 position = grip.transform.position;
			foreach (Collider componentsInChild in item.GetComponentsInChildren<Collider>())
			{
				if (!(componentsInChild == null) && !componentsInChild.isTrigger)
				{
					Bounds bounds = componentsInChild.bounds;
					num = Mathf.Max(num, Mathf.Max((bounds.min - position).magnitude, (bounds.max - position).magnitude));
				}
			}
		}
		catch (System.Exception)
		{
		}
		meleeReach = 0.75f + Mathf.Clamp(num, 0.2f, 1.6f) * 0.8f;
		meleeSeenAt = Time.time;
		toolLocal = Vector3.back;
		toolTurnHas = false;
		hitClip = null;
		punchAt = -10f;
		Lab.Event("weapon: has the " + ThingName(item) + " to hit with, by its " + ((grip != null) ? grip.name : "?") + " (reaches " + meleeReach.ToString("0.0") + " m)");
	}

	private bool MeleeStep(Transform you, Transform head, float dt)
	{
		if (!Holding() || head == null)
		{
			return EndPlay();
		}
		if (mode != Mode.Brain && Time.time >= meleeSenseAt)
		{
			meleeSenseAt = Time.time + 0.15f;
			Sense();
		}
		Targets.Target target = nearestSeen;
		handOverride = true;
		leftOverride = false;
		running = false;
		if (target.transform == null || nearestSeenDistance > 30f)
		{
			walk = Vector2.zero;
			playHeight = standingHeight;
			playPitch = 0f;
			handAt = new Vector3(0.24f, standingHeight - 0.72f, 0.12f);
			handTurn = Pointing(new Vector3(0f, -0.35f, 0.94f), new Vector3(0f, -0.94f, -0.35f));
			hitClip = null;
			if (Time.time - meleeSeenAt > Tuning.F("melee.keep", 12f))
			{
				Next(Play.Drop);
			}
			return true;
		}
		meleeSeenAt = Time.time;
		Vector3 point = target.Point;
		if (Chase(head, point, dt))
		{
			handAt = new Vector3(0.24f, standingHeight - 0.72f, 0.12f);
			handTurn = Pointing(new Vector3(0f, -0.35f, 0.94f), new Vector3(0f, -0.94f, -0.35f));
			playHeight = standingHeight;
			playPitch = 0f;
			hitClip = null;
			return true;
		}
		float num = CloseIn(head, point, meleeReach, dt);
		handAt = new Vector3(0.2f, playHeight - 0.32f, 0.3f);
		handTurn = ((Time.time - punchAt > 0.7f && hitClip == null) ? Pointing(new Vector3(-0.1f, 0.85f, 0.5f), new Vector3(0f, -0.5f, 0.85f)) : (ToolTurn(new Vector3(-0.1f, 0.85f, 0.5f), new Vector3(0f, -0.5f, 0.85f)) * ToolFix));
		leftOverride = true;
		leftAt = new Vector3(-0.17f, playHeight - 0.3f, 0.26f);
		leftTurn = Quaternion.Euler(-10f, 10f, 0f);
		if (hitClip == null && num < meleeReach && Time.time >= nextPunchAt)
		{
			hitClip = PickHit(MoveSlots.Kind.Melee);
			punchAt = Time.time;
			Lab.Event("weapon: strikes with the " + ThingName(item) + ((hitClip != null) ? (" as you do (" + hitClip[hitClip.Length - 1].time.ToString("0.0") + " s)") : " (a plain chop: no Melee slot recorded)") + ", " + num.ToString("0.0") + " m off");
			nextPunchAt = Time.time + ((hitClip != null) ? hitClip[hitClip.Length - 1].time : 0.55f) + UnityEngine.Random.Range(0.25f, 0.7f);
		}
		float num2 = Time.time - punchAt;
		if (HitOfTheirs(num2))
		{
			return true;
		}
		if (num2 < 0.55f)
		{
			float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(num2 / 0.2f));
			float t2 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((num2 - 0.2f) / 0.14f));
			float t3 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((num2 - 0.36f) / 0.19f));
			Vector3 vector = handAt;
			Vector3 b = new Vector3(0.28f, playHeight + 0.12f, -0.05f);
			Vector3 a = Vector3.Lerp(b: new Vector3(0.14f, playHeight - 0.45f, 0.58f), a: Vector3.Lerp(vector, b, t), t: t2);
			Quaternion a2 = Quaternion.Slerp(Quaternion.Slerp(handTurn, ToolTurn(new Vector3(0f, 0.6f, -0.8f), new Vector3(0f, 0.8f, 0.6f)) * ToolFix, t), ToolTurn(new Vector3(0f, -0.25f, 0.97f), new Vector3(0f, -0.8f, 0.6f)) * ToolFix, t2);
			handAt = Vector3.Lerp(a, vector, t3);
			handTurn = Quaternion.Slerp(a2, handTurn, t3);
		}
		return true;
	}

	public bool Holds(Gun g)
	{
		if (gun != null && g != null)
		{
			return gun.Pointer == g.Pointer;
		}
		return false;
	}

	public void Heard(Vector3 at)
	{
		Transform head = Head;
		if (!(head == null))
		{
			float magnitude = (at - head.position).magnitude;
			Startle(at, magnitude);
			if (!(magnitude > 45f) && !(magnitude < 1.5f))
			{
				heardAt = at;
				heardTime = Time.time;
				gazeUntil = 0f;
			}
		}
	}

	private void Gaze(Transform you, bool moving, float dt)
	{
		Transform head = Head;
		if (head == null || handOverride || Playing || climbing || brainTarget.transform != null)
		{
			return;
		}
		if (Time.time >= gazeUntil)
		{
			ChooseGaze(you, head, moving);
		}
		Vector3 vector = ((gazeFollow != null) ? gazeFollow.position : gazeAt);
		if (gazeOn == "ahead" && moving)
		{
			vector = (nav.HasGoal ? (nav.LookAhead(Feet, 4f) + Vector3.up * (head.position.y - Feet.y - 0.15f)) : (head.position + Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward * 4f));
		}
		Vector3 vector2 = vector - head.position;
		float magnitude = Flat(vector2).magnitude;
		float num = Mathf.Clamp(Mathf.DeltaAngle(bodyYaw, Recorder.Yaw(vector2)), -80f, 80f);
		float num2 = Mathf.Clamp((0f - Mathf.Atan2(vector2.y, Mathf.Max(magnitude, 0.1f))) * 57.29578f, -35f, 45f);
		float num3 = Tuning.F("gaze.quick", 9f);
		gazeYaw = Mathf.SmoothDamp(gazeYaw, num, ref gazeYawSpeed, 1f / num3, 400f, dt);
		gazePitch = Mathf.SmoothDamp(gazePitch, num2, ref gazePitchSpeed, 1f / num3, 300f, dt);
		float num4 = Tuning.F("gaze.drift", 1.5f);
		float y = gazeYaw + (Mathf.PerlinNoise(Time.time * 0.6f, born) - 0.5f) * 2f * num4;
		float x = gazePitch + (Mathf.PerlinNoise(born, Time.time * 0.5f) - 0.5f) * 2f * num4;
		if (!moving && Mathf.Abs(gazeYaw) > 55f)
		{
			if (gazeFarSince < 0f)
			{
				gazeFarSince = Time.time;
			}
			if (Time.time - gazeFarSince > 0.6f)
			{
				float num5 = Mathf.Sign(gazeYaw) * Mathf.Min(Mathf.Abs(gazeYaw) - 20f, 90f * dt);
				bodyYaw += num5;
				gazeYaw -= num5;
			}
		}
		else
		{
			gazeFarSince = -1f;
		}
		pose.head = Quaternion.Euler(x, y, 0f) * Quaternion.Slerp(Quaternion.identity, pose.head, 0.25f);
	}

	private void ChooseGaze(Transform you, Transform head, bool moving)
	{
		gazeFollow = null;
		float num = Mathf.Min(MotionLibrary.GlanceEvery, Tuning.F("gaze.every.most", 3.5f)) * Random(0.6f, 1.6f);
		if (Time.time - heardTime < 1.5f)
		{
			Look(heardAt, null, "a shot", Random(1.2f, 2.5f));
			heardTime = -100f;
			return;
		}
		System.Collections.Generic.List<(Vector3, Transform, string, float)> list = new System.Collections.Generic.List<(Vector3, Transform, string, float)>();
		if (moving)
		{
			list.Add((Vector3.zero, null, "ahead", 3f));
		}
		if (you != null)
		{
			float magnitude = (you.position - head.position).magnitude;
			if (magnitude < 40f)
			{
				list.Add((you.position, you, "you", ((magnitude < 8f) ? 2.2f : 1f) / (1f + magnitude / 20f) * (moving ? 0.4f : 1f)));
			}
		}
		foreach (Bot item2 in BotManager.All)
		{
			if (item2 != this && !(item2.Head == null))
			{
				float magnitude2 = (item2.Head.position - head.position).magnitude;
				if (magnitude2 < 25f)
				{
					list.Add((item2.Head.position, item2.Head, "a bot", 0.7f / (1f + magnitude2 / 10f)));
				}
			}
		}
		foreach (Collider item3 in Physics.OverlapSphere(head.position, 14f, -1, QueryTriggerInteraction.Ignore))
		{
			Rigidbody rigidbody = ((item3 != null) ? item3.attachedRigidbody : null);
			if (!(rigidbody == null) && !rigidbody.isKinematic && !(rigidbody.velocity.magnitude < 1.5f) && !(rigidbody.GetComponentInParent<RigManager>() != null))
			{
				list.Add((rigidbody.worldCenterOfMass, rigidbody.transform, "something moving", 2f));
				break;
			}
		}
		Vector3 item = head.position + Quaternion.Euler(Random(-8f, 18f), bodyYaw + Random(-110f, 110f), 0f) * Vector3.forward * Random(5f, 15f);
		list.Add((item, null, "about", moving ? 0.3f : 1.1f));
		float num2 = 0f;
		foreach (var item4 in list)
		{
			num2 += item4.Item4;
		}
		float num3 = UnityEngine.Random.value * num2;
		foreach (var item5 in list)
		{
			num3 -= item5.Item4;
			if (!(num3 > 0f))
			{
				float seconds = ((item5.Item3 == "ahead") ? Random(0.8f, 2f) : ((item5.Item3 == "you") ? (num * 1.4f) : num));
				Look(item5.Item1, item5.Item2, item5.Item3, seconds);
				break;
			}
		}
	}

	private void Look(Vector3 at, Transform follow, string what, float seconds)
	{
		gazeAt = at;
		gazeFollow = follow;
		gazeOn = what;
		gazeUntil = Time.time + Mathf.Max(0.4f, seconds);
	}

	private GunMoves.Hold LearnedHold(bool aimed)
	{
		if (gun == null || Tuning.F("learned.hold", 1f) < 0.5f)
		{
			return null;
		}
		return GunMoves.Held(GunKey, LongGun, aimed);
	}

	private bool HoldLearned(Vector3 point, Transform head, bool aimed)
	{
		if (aimed && Tuning.F("learned.hold.aim", 0f) < 0.5f)
		{
			return false;
		}
		GunMoves.Hold hold = LearnedHold(aimed);
		if (hold == null && !aimed)
		{
			hold = LearnedHold(aimed: true);
		}
		holdAimedNow = aimed && hold != null;
		Transform transform = ((gun != null) ? gun.firePointTransform : null);
		if (hold == null || transform == null || head == null)
		{
			return false;
		}
		Quaternion quaternion = Quaternion.Euler(0f, bodyYaw, 0f);
		Pose pose = GunMoves.U(hold.right, Pose.identity);
		Vector3 vector = (aimed ? ChestHold() : Vector3.zero);
		pose.position += vector;
		Quaternion quaternion2 = GunMoves.UQ(hold.muzzle, Quaternion.identity);
		float pitch;
		float yaw;
		if (aimed)
		{
			Vector3 vector2 = Quaternion.Inverse(quaternion) * (point - transform.position).normalized;
			Vector3 vector3 = quaternion2 * Vector3.forward;
			bool flag = lockSet && item != null;
			if (flag)
			{
				Quaternion quaternion3 = Quaternion.Inverse(item.rotation) * transform.rotation;
				vector3 = pose.rotation * (gunInController.rotation * quaternion3) * Vector3.forward;
			}
			if (!SolveLook(vector3.normalized, vector2, out pitch, out yaw))
			{
				Vector3 eulerAngles = (Quaternion.LookRotation(vector2, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(vector3, Vector3.up))).eulerAngles;
				pitch = Mathf.DeltaAngle(0f, eulerAngles.x);
				yaw = Mathf.DeltaAngle(0f, eulerAngles.y);
			}
			pitch += aimFix.x;
			yaw += aimFix.y;
			if (Time.time - aimSaidAt > 15f)
			{
				aimSaidAt = Time.time;
				Vector3 v = Quaternion.Inverse(quaternion) * transform.forward;
				Lab.Event("aim: wants pitch " + PitchOf(vector2).ToString("0") + " yaw " + YawOf(vector2).ToString("0") + ", barrel at pitch " + PitchOf(v).ToString("0") + " yaw " + YawOf(v).ToString("0") + "; head asked " + pitch.ToString("0") + "/" + yaw.ToString("0") + " (has " + aimPitchNow.ToString("0") + "/" + aimYawNow.ToString("0") + "), fix " + aimFix.x.ToString("0.0") + "/" + aimFix.y.ToString("0.0") + ", barrel in head " + PitchOf(vector3).ToString("0") + "/" + YawOf(vector3).ToString("0") + (flag ? " (known)" : " (learned)") + ", step " + step);
			}
			bool num = item == null || (item.angularVelocity.magnitude < Tuning.F("hold.steady.spin", 1.5f) * 2.5f && item.velocity.magnitude < Tuning.F("hold.steady.speed", 0.8f) * 2f);
			if (Mathf.Abs(aimFix.y) > 30f || Mathf.Abs(aimFix.x) > 30f)
			{
				aimFix = Vector2.zero;
			}
			if (num && Time.time - firedAt > 0.35f)
			{
				Vector3 v2 = Quaternion.Inverse(quaternion) * transform.forward;
				float num2 = Mathf.Clamp01(Time.deltaTime * Tuning.F("learned.aimfix.rate", 2.5f) * (GunSteady() ? 1f : 0.4f));
				float num3 = Tuning.F("learned.aimfix.most", 25f);
				aimFix.x = Mathf.Clamp(aimFix.x + num2 * (PitchOf(vector2) - PitchOf(v2)), 0f - num3, num3);
				aimFix.y = Mathf.Clamp(aimFix.y + num2 * Mathf.DeltaAngle(YawOf(v2), YawOf(vector2)), 0f - num3, num3);
			}
		}
		else
		{
			pitch = 15f;
			yaw = 0f;
		}
		pitch = Mathf.Clamp(pitch, -60f, 70f);
		yaw = Mathf.Clamp(yaw, -60f, 60f);
		float b = 6f;
		float b2 = 0f;
		if (aimed)
		{
			Vector3 v3 = Quaternion.Inverse(quaternion) * (point - head.position).normalized;
			b = Mathf.Clamp(PitchOf(v3), -40f, 40f);
			b2 = Mathf.Clamp(YawOf(v3), -50f, 50f);
		}
		float t = Mathf.Clamp01(Time.deltaTime * Tuning.F("learned.look.ease", 8f));
		aimPitchNow = Mathf.LerpAngle(aimPitchNow, pitch, t);
		aimYawNow = Mathf.LerpAngle(aimYawNow, yaw, t);
		lookPitchNow = Mathf.LerpAngle(lookPitchNow, b, t);
		lookYawNow = Mathf.LerpAngle(lookYawNow, b2, t);
		playPitch = lookPitchNow;
		playYaw = lookYawNow;
		playHeight = standingHeight * 0.985f;
		Quaternion quaternion4 = Quaternion.Euler(aimPitchNow, aimYawNow, 0f);
		Vector3 vector4 = new Vector3(0f, playHeight, 0f);
		handOverride = true;
		learnedHoldAt = Time.time;
		handAt = vector4 + quaternion4 * pose.position;
		handTurn = quaternion4 * pose.rotation;
		float num4 = Time.time - firedAt;
		if (lockSet && num4 >= 0f && num4 < 0.4f)
		{
			float num5 = Mathf.Exp((0f - num4) / Tuning.F("gun.kick.time", 0.07f)) * (LongGun ? 0.6f : 1f);
			handTurn *= Quaternion.Euler((0f - Tuning.F("gun.kick", 4f)) * num5, 0f, 0f);
			handAt -= quaternion4 * Vector3.forward * (0.02f * num5);
		}
		if (lockSet && leftOnGunSet && supportHeld && step == Step.Ready && item != null)
		{
			Transform vrRoot = controls.vrRoot;
			Pose pose2 = new Pose(vrRoot.TransformPoint(handAt), quaternion * handTurn) * gunInController * leftOnGun;
			leftOverride = true;
			leftAt = vrRoot.InverseTransformPoint(pose2.position);
			leftTurn = Quaternion.Inverse(quaternion) * pose2.rotation;
			return true;
		}
		if (support != null && hold.left != null && step != 0)
		{
			Pose pose3 = GunMoves.U(hold.left, Pose.identity);
			Vector3 b3 = vector4 + quaternion4 * (pose3.position + vector);
			Quaternion b4 = quaternion4 * pose3.rotation;
			leftOverride = true;
			if (step == Step.Reach)
			{
				float t2 = Mathf.SmoothStep(0f, 1f, (Time.time - stepAt) / Tuning.F("hold.reach.time", 0.7f));
				Vector3 a = controls.vrRoot.InverseTransformPoint(leftStart.position);
				Quaternion a2 = Quaternion.Inverse(quaternion) * leftStart.rotation;
				leftAt = Vector3.Lerp(a, b3, t2) + Quaternion.Inverse(quaternion) * reachFix;
				leftTurn = Quaternion.Slerp(a2, b4, t2);
			}
			else
			{
				leftAt = b3;
				leftTurn = b4;
			}
		}
		return true;
	}

	private Vector3 ChestHold()
	{
		float num = Mathf.Clamp(standingHeight / 1.6f, 0.5f, 2f);
		int num2;
		float num3;
		if (LongGun && gun != null && gun.triggerGrip != null)
		{
			num2 = ((Vector3.Distance(support.transform.position, gun.triggerGrip.transform.position) > 0.2f) ? 1 : 0);
			if (num2 != 0)
			{
				num3 = Tuning.F("aim.chest.rifle", 0.27f);
				goto IL_009c;
			}
		}
		else
		{
			num2 = 0;
		}
		num3 = Tuning.F("aim.chest.pistol", 0.25f);
		goto IL_009c;
		IL_009c:
		float num4 = num3;
		float num5 = ((num2 != 0) ? Tuning.F("aim.chest.in.rifle", 0.14f) : Tuning.F("aim.chest.in.pistol", 0.16f));
		return new Vector3(0f, 0f - num4, 0f - num5) * num;
	}

	private static bool SolveLook(Vector3 from, Vector3 to, out float pitch, out float yaw)
	{
		pitch = (yaw = 0f);
		float num = Mathf.Sqrt(from.y * from.y + from.z * from.z);
		if (num < 0.0001f || Mathf.Abs(to.y) > num)
		{
			return false;
		}
		float num2 = Mathf.Atan2(from.z, from.y);
		float num3 = Mathf.Acos(Mathf.Clamp(to.y / num, -1f, 1f));
		float num4 = float.MaxValue;
		float[] array = new float[2]
		{
			num3 - num2,
			0f - num3 - num2
		};
		foreach (float num5 in array)
		{
			float num6 = from.y * Mathf.Sin(num5) + from.z * Mathf.Cos(num5);
			float num7 = Mathf.DeltaAngle(0f, num5 * 57.29578f);
			float num8 = Mathf.Abs(num7) + ((num6 < 0f) ? 1000f : 0f);
			if (num8 < num4)
			{
				num4 = num8;
				pitch = num7;
				float x = from.x;
				yaw = Mathf.DeltaAngle(Mathf.Atan2(x, num6) * 57.29578f, Mathf.Atan2(to.x, to.z) * 57.29578f);
			}
		}
		return num4 < 1000f;
	}

	public void GunFixed(float dt)
	{
		if (gun == null || item == null || dead || down || mode == Mode.Clone || mode == Mode.Exact || Tuning.F("gun.lock", 1f) < 0.5f)
		{
			lockSet = false;
			leftOnGunSet = false;
			lockGun = System.IntPtr.Zero;
			return;
		}
		BaseController rightController = controls.rightController;
		if (rightController == null || !Holding())
		{
			lockSet = false;
			leftOnGunSet = false;
			lockGun = System.IntPtr.Zero;
			return;
		}
		if (lockGun != gun.Pointer)
		{
			lockGun = gun.Pointer;
			lockSince = Time.time;
			lockSet = false;
			leftOnGunSet = false;
		}
		if (OthersHoldIt())
		{
			return;
		}
		Transform transform = rightController.transform;
		if (!lockSet)
		{
			float num = Time.time - lockSince;
			if (num < 0.5f || (!GunSteady() && num < 1.5f))
			{
				return;
			}
			Pose pose = Pose.Of(transform).Inverse() * Pose.Of(item.transform);
			Hand rightHand = RightHand;
			float num2 = ((gun.triggerGrip != null) ? (gun.triggerGrip.transform.position - transform.position).magnitude : pose.position.magnitude);
			float num3 = ((rightHand != null) ? Quaternion.Angle(rightHand.transform.rotation, transform.rotation) : 999f);
			if (num3 > Tuning.F("gun.lock.twist", 45f) && num < 6f)
			{
				return;
			}
			if (num2 > Tuning.F("gun.lock.most", 0.35f) || rightHand == null || (rightHand.transform.position - transform.position).magnitude > 0.25f)
			{
				if (num > 4f && Time.time - lockSaidAt > 5f)
				{
					lockSaidAt = Time.time;
					Lab.Event("gun: not kept fast (its handle is " + num2.ToString("0.00") + " m from the controller, the hand " + ((rightHand != null) ? (rightHand.transform.position - transform.position).magnitude.ToString("0.00") : "?") + " m)");
				}
				return;
			}
			gunInController = pose;
			lockSet = true;
			Lab.Event("gun: kept fast to its hand from here (" + gunInController.position.magnitude.ToString("0.00") + " m from the controller, the hand turned " + num3.ToString("0") + " deg from it)");
		}
		if (Time.time - learnedHoldAt > 0.3f && !Reloading)
		{
			return;
		}
		if (supportHeld && step == Step.Ready && !leftOnGunSet && controls.leftController != null)
		{
			Hand leftHand = LeftHand;
			Pose pose2 = ((leftHand != null) ? Pose.Of(leftHand.transform) : Pose.Of(controls.leftController.transform));
			Pose pose3 = Pose.Of(controls.leftController.transform);
			leftOnGun = Pose.Of(item.transform).Inverse() * pose3;
			leftHandOnController = pose3.Inverse() * pose2;
			leftOnGunSet = true;
		}
		else if (!supportHeld)
		{
			leftOnGunSet = false;
		}
		if (Tuning.F("gun.lock.drive", 0f) < 0.5f)
		{
			return;
		}
		Pose pose4 = Pose.Of(transform) * gunInController;
		if (!Bad(pose4.position) && !Bad(pose4.rotation))
		{
			float num4 = Tuning.F("gun.lock.pull", 0.55f);
			Vector3 vector = (pose4.position - item.position) / dt * num4;
			item.velocity = Vector3.ClampMagnitude(vector, 15f);
			(pose4.rotation * Quaternion.Inverse(item.rotation)).ToAngleAxis(out var angle, out var axis);
			if (angle > 180f)
			{
				angle -= 360f;
			}
			Vector3 vector2 = ((float.IsNaN(axis.x) || float.IsInfinity(axis.x)) ? Vector3.zero : (axis * (angle * ((float)System.Math.PI / 180f) / dt * num4)));
			item.angularVelocity = Vector3.ClampMagnitude(vector2, 30f);
		}
	}

	private bool OthersHoldIt()
	{
		try
		{
			MarrowEntity componentInParent = item.GetComponentInParent<MarrowEntity>();
			foreach (Grip componentsInChild in ((componentInParent != null) ? ((Component)componentInParent) : ((Component)item)).GetComponentsInChildren<Grip>(includeInactive: false))
			{
				if (componentsInChild == null || componentsInChild.attachedHands == null)
				{
					continue;
				}
				Il2CppSystem.Collections.Generic.List<Hand>.Enumerator enumerator2 = componentsInChild.attachedHands.GetEnumerator();
				while (enumerator2.MoveNext())
				{
					Hand current2 = enumerator2.Current;
					if (current2 != null && current2 != LeftHand && current2 != RightHand)
					{
						return true;
					}
				}
			}
		}
		catch (System.Exception)
		{
		}
		return false;
	}

	private Grip NearestGripTo(Hand hand)
	{
		if (item == null)
		{
			return null;
		}
		MarrowEntity componentInParent = item.GetComponentInParent<MarrowEntity>();
		Component obj = ((componentInParent != null) ? ((Component)componentInParent) : ((Component)item));
		Grip result = null;
		float num = float.MaxValue;
		foreach (Grip componentsInChild in obj.GetComponentsInChildren<Grip>(includeInactive: false))
		{
			if (componentsInChild == null || componentsInChild == gun.triggerGrip || componentsInChild.HasAttachedHands() || componentsInChild.GetComponentInParent<Magazine>() != null || WorksBolt(gun, componentsInChild))
			{
				continue;
			}
			string text = componentsInChild.name.ToLowerInvariant();
			if (!text.Contains("slide") && !text.Contains("charg") && !text.Contains("bolt") && !text.Contains("mag") && !text.Contains("capsule") && !text.Contains("stock") && (!(gun.ammoSocket != null) || !componentsInChild.transform.IsChildOf(gun.ammoSocket.transform)))
			{
				float magnitude = (hand.transform.position - HandOnGrip(componentsInChild, hand).position).magnitude;
				if (magnitude < num)
				{
					num = magnitude;
					result = componentsInChild;
				}
			}
		}
		return result;
	}

	private static float PitchOf(Vector3 v)
	{
		return (0f - Mathf.Asin(Mathf.Clamp(v.normalized.y, -1f, 1f))) * 57.29578f;
	}

	private static float YawOf(Vector3 v)
	{
		return Mathf.Atan2(v.x, v.z) * 57.29578f;
	}

	private void HoldSupportLearned()
	{
		Hand leftHand = LeftHand;
		if (support == null || leftHand == null)
		{
			return;
		}
		supportHeld = leftHand.m_CurrentAttachedGO != null && support.attachedHands != null && support.attachedHands.Contains(leftHand);
		float num = Time.time - stepAt;
		switch (step)
		{
		case Step.OneHand:
			leftOverride = false;
			if (num > 0.6f && (GunSteady() || num > Tuning.F("hold.onehand.most", 1.4f)))
			{
				NextStep(Step.Reach);
			}
			break;
		case Step.Reach:
		{
			if (supportHeld)
			{
				NextStep(Step.Settle);
				break;
			}
			if (lockSet && num > Tuning.F("hold.reach.time", 0.7f) && controls.leftController != null && !supportHeld)
			{
				Vector3 vector = HandOnGrip(support, leftHand).position - leftHand.transform.position;
				reachFix = Vector3.ClampMagnitude(reachFix + vector * Mathf.Clamp01(Time.deltaTime * 6f), 0.35f);
			}
			if (leftHand.m_CurrentAttachedGO != null && !Reloading && !supportHeld)
			{
				FreeHand(leftHand);
			}
			float num2 = (leftHand.transform.position - HandOnGrip(support, leftHand).position).magnitude;
			if (num > 0.8f && num2 >= Tuning.F("learned.grab", 0.12f))
			{
				Grip grip = NearestGripTo(leftHand);
				if (grip != null && grip != support)
				{
					float magnitude = (leftHand.transform.position - HandOnGrip(grip, leftHand).position).magnitude;
					if (magnitude < num2)
					{
						Lab.Event("front hand: nearer to " + grip.name + " (" + magnitude.ToString("0.00") + " m) than " + support.name + " (" + num2.ToString("0.00") + " m)");
						support = grip;
						num2 = magnitude;
					}
				}
			}
			if (num > 0.5f && num2 < Tuning.F("learned.grab", 0.12f) + ((num > 1.5f) ? 0.06f : 0f) && Time.time - supportTriedAt > 0.4f)
			{
				supportTriedAt = Time.time;
				supportHeld = Grab(leftHand, support);
				Lab.Event((supportHeld ? "front hand on " : "front hand missed ") + support.name + " (learned hold, gap " + num2.ToString("0.00") + " m" + (supportHeld ? "" : (", hand holds " + ((leftHand.m_CurrentAttachedGO != null) ? leftHand.m_CurrentAttachedGO.name : "nothing") + ", grip held by " + ((support.attachedHands != null) ? support.attachedHands.Count : (-1)) + " hands, " + support.GetIl2CppType().Name)) + ")");
				if (supportHeld)
				{
					NextStep(Step.Settle);
				}
				else if (++supportTries >= 3)
				{
					support = null;
					leftOverride = false;
					NextStep(Step.Ready);
				}
			}
			else if (num > 3f)
			{
				Lab.Event("front hand gave up (learned hold, gap " + num2.ToString("0.00") + " m)");
				support = null;
				leftOverride = false;
				NextStep(Step.Ready);
			}
			break;
		}
		case Step.Settle:
			if (!supportHeld)
			{
				NextStep(Step.Reach);
			}
			else if (num > 0.4f)
			{
				NextStep(Step.Ready);
			}
			break;
		default:
			if (!supportHeld)
			{
				NextStep(Step.Reach);
			}
			break;
		}
	}

	private bool StartLearnedReload()
	{
		if (gun == null || Tuning.F("learned.reload", 1f) < 0.5f)
		{
			return false;
		}
		learnedReload = GunMoves.ReloadFor(GunKey, LongGun);
		learnedInsertTried = false;
		Hand leftHand = LeftHand;
		if (learnedReload != null && leftHand != null && leftHand.m_CurrentAttachedGO != null && !supportHeld)
		{
			FreeHand(leftHand);
		}
		if (learnedReload != null)
		{
			Lab.Event("reload the player's way (" + learnedReload.end.ToString("0.0") + " s)");
		}
		return learnedReload != null;
	}

	private void LearnedReloadStep(Transform head)
	{
		GunMoves.Reload reload = learnedReload;
		AmmoSocket ammoSocket = gun.ammoSocket;
		Hand leftHand = LeftHand;
		float num = (Time.time - reloadAt) * Tuning.F("learned.reload.speed", 1f);
		System.Collections.Generic.List<GunMoves.ReloadFrame> frames = reload.frames;
		int i;
		for (i = 0; i < frames.Count - 2 && frames[i + 1].t <= num; i++)
		{
		}
		GunMoves.ReloadFrame reloadFrame = frames[i];
		GunMoves.ReloadFrame reloadFrame2 = frames[Mathf.Min(i + 1, frames.Count - 1)];
		float k = ((reloadFrame2.t > reloadFrame.t) ? Mathf.Clamp01((num - reloadFrame.t) / (reloadFrame2.t - reloadFrame.t)) : 0f);
		Pose pose = Pose.Lerp(GunMoves.U(reloadFrame.head, Pose.identity), GunMoves.U(reloadFrame2.head, Pose.identity), k);
		Pose pose2 = Pose.Lerp(GunMoves.U(reloadFrame.right, Pose.identity), GunMoves.U(reloadFrame2.right, Pose.identity), k);
		Pose pose3 = Pose.Lerp(GunMoves.U(reloadFrame.left, Pose.identity), GunMoves.U(reloadFrame2.left, Pose.identity), k);
		Vector3 eulerAngles = pose.rotation.eulerAngles;
		playPitch = Mathf.DeltaAngle(0f, eulerAngles.x);
		playYaw = Mathf.DeltaAngle(0f, eulerAngles.y);
		playHeight = standingHeight * 0.985f;
		Vector3 vector = new Vector3(0f, playHeight, 0f);
		handOverride = true;
		handAt = vector + pose2.position;
		handTurn = pose2.rotation;
		leftOverride = true;
		leftAt = vector + pose3.position;
		leftTurn = pose3.rotation;
		if (ammoSocket == null)
		{
			learnedReload = null;
			FinishReload(quick: true);
			return;
		}
		if (!ejected && num >= reload.eject)
		{
			ejected = true;
			try
			{
				if (gun.HasMagazine())
				{
					Magazine magazine = ((ammoSocket._magazinePlug != null) ? ammoSocket._magazinePlug.magazine : null);
					ammoSocket.EjectMagazine();
					if (magazine != null)
					{
						dropped.Add((magazine, Time.time));
					}
				}
			}
			catch (System.Exception)
			{
			}
		}
		float num2 = ((reload.grab >= 0f) ? reload.grab : Mathf.Max(0.3f, reload.insert - 0.8f));
		if (!magAsked && num >= num2 - 0.25f && leftHand != null)
		{
			magAsked = true;
			this.reload = Reload.Fetch;
			SpawnMag(leftHand.transform.position, leftHand.transform.rotation);
		}
		if (newMag == null && magAsked && Time.time - magAskedAt > 0.15f)
		{
			newMag = SpawnedMag();
		}
		if (newMag != null && newMag.grip != null && leftHand != null && leftHand.m_CurrentAttachedGO == null && !gun.HasMagazine() && num >= num2 - 0.1f)
		{
			Grab(leftHand, newMag.grip);
		}
		if (newMag != null && !gun.HasMagazine() && num >= reload.insert && newMag.magazinePlug != null)
		{
			this.reload = Reload.Insert;
			float magnitude = (newMag.magazinePlug.transform.position - ammoSocket.transform.position).magnitude;
			try
			{
				if (magnitude < 0.15f && !learnedInsertTried)
				{
					learnedInsertTried = true;
					newMag.magazinePlug.InsertPlug(ammoSocket);
				}
				else if (num > reload.insert + 0.12f)
				{
					newMag.magazinePlug.ForceInSocket(ammoSocket);
				}
			}
			catch (System.Exception)
			{
			}
		}
		if (newMag != null && gun.HasMagazine() && leftHand != null)
		{
			LetGoOf(newMag.grip, leftHand);
			newMag = null;
		}
		if (reload.rack >= 0f && gun.HasMagazine() && gun.cartridgeState != Gun.CartridgeStates.UNSPENT && leftHand != null)
		{
			this.reload = Reload.Rack;
			if (slide == null)
			{
				slide = SlideGrip(gun);
			}
			if (slide != null && !slideHeld && num >= reload.rack - 0.3f && num < reload.rack && (leftHand.transform.position - slide.transform.position).magnitude < 0.15f && leftHand.m_CurrentAttachedGO == null)
			{
				slideHeld = Grab(leftHand, slide);
			}
		}
		if (slideHeld && num >= reload.rack + 0.1f)
		{
			LetGoOf(slide, leftHand);
			slideHeld = false;
		}
		if (reload.rack >= 0f && num >= reload.rack + 0.15f && gun.HasMagazine() && gun.cartridgeState != Gun.CartridgeStates.UNSPENT && !slideHeld && Time.time - chargedAt > 0.6f)
		{
			chargedAt = Time.time;
			TryCharge();
		}
		if (!(num >= reload.end))
		{
			return;
		}
		learnedReload = null;
		if (!gun.HasMagazine() && newMag != null && newMag.magazinePlug != null)
		{
			try
			{
				newMag.magazinePlug.ForceInSocket(ammoSocket);
			}
			catch (System.Exception)
			{
			}
		}
		DropNewMag();
		if (gun.HasMagazine() && gun.cartridgeState != Gun.CartridgeStates.UNSPENT)
		{
			TryCharge();
		}
		FinishReload(!gun.HasMagazine());
	}

	public void OnAnyGunFired(Gun fired)
	{
		if (!(gun == null) && !(fired == null) && !(fired.Pointer != gun.Pointer))
		{
			NoteShot();
			ZombieShot();
			firedAt = Time.time;
			shotsAtTarget++;
			burstRoundsLeft--;
			LabShots++;
			if (Time.time - firedByHandAt > 0.05f)
			{
				triggerWorks = true;
			}
		}
	}

	private Gun NearestGun(out float far)
	{
		far = Tuning.F("gun.seek", 45f);
		Gun result = null;
		Vector3 vector = Feet;
		HashSet<System.IntPtr> hashSet = new HashSet<System.IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(vector + Vector3.up * 0.5f, far, -1, QueryTriggerInteraction.Ignore))
		{
			Rigidbody rigidbody = ((item != null) ? item.attachedRigidbody : null);
			if (rigidbody == null || !hashSet.Add(rigidbody.Pointer) || rigidbody.GetComponentInParent<RigManager>() != null)
			{
				continue;
			}
			Gun gun = rigidbody.GetComponentInParent<Gun>();
			if (gun == null)
			{
				MarrowEntity componentInParent = rigidbody.GetComponentInParent<MarrowEntity>();
				gun = ((componentInParent != null) ? componentInParent.GetComponentInChildren<Gun>() : null);
			}
			if (Usable(gun))
			{
				float num = rigidbody.worldCenterOfMass.y - vector.y;
				float magnitude = (rigidbody.worldCenterOfMass - vector).magnitude;
				if (!(num < -0.5f) && !(num > 1.6f) && !(magnitude >= far))
				{
					far = magnitude;
					result = gun;
				}
			}
		}
		return result;
	}

	private bool GetAGun()
	{
		if (spawningGun && Time.time - spawnAskedAt > 0.2f && TakeSpawnedGun())
		{
			return true;
		}
		if (spawningGun && Time.time - spawnAskedAt > 4f)
		{
			spawningGun = false;
			Spawnables.Notify("The gun the bot asked for never came");
		}
		if (spawningGun)
		{
			return true;
		}
		if (HasStowed && stowedGun != null)
		{
			return Draw();
		}
		Vector3 vector = Feet;
		if (ownGun != null && ownGun.triggerGrip != null && ownGun.gameObject.activeInHierarchy)
		{
			Rigidbody componentInParent = ownGun.triggerGrip.GetComponentInParent<Rigidbody>();
			if (!Usable(ownGun) || OwnGunTaken())
			{
				ownGun = null;
			}
			else if (componentInParent != null && (componentInParent.worldCenterOfMass - vector).magnitude < 60f)
			{
				Take(ownGun);
				play = Play.GoTo;
				playAt = Time.time;
				nav.SetGoal(item.worldCenterOfMass);
				return true;
			}
		}
		Gun gun = null;
		float num = Tuning.F("gun.seek", 30f);
		HashSet<System.IntPtr> hashSet = new HashSet<System.IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(vector + Vector3.up * 0.5f, num, -1, QueryTriggerInteraction.Ignore))
		{
			Rigidbody rigidbody = ((item != null) ? item.attachedRigidbody : null);
			if (rigidbody == null || !hashSet.Add(rigidbody.Pointer) || rigidbody.GetComponentInParent<RigManager>() != null)
			{
				continue;
			}
			Gun gun2 = rigidbody.GetComponentInParent<Gun>();
			if (gun2 == null)
			{
				MarrowEntity componentInParent2 = rigidbody.GetComponentInParent<MarrowEntity>();
				gun2 = ((componentInParent2 != null) ? componentInParent2.GetComponentInChildren<Gun>() : null);
			}
			if (Usable(gun2) && !ClaimedByAnother(gun2) && (!wontLoad.TryGetValue(gun2.Pointer, out var value) || !(Time.time - value < 120f)))
			{
				float num2 = rigidbody.worldCenterOfMass.y - vector.y;
				float magnitude = (rigidbody.worldCenterOfMass - vector).magnitude;
				if (!(num2 < -0.5f) && !(num2 > 1.6f) && !(magnitude >= num))
				{
					num = magnitude;
					gun = gun2;
				}
			}
		}
		if (gun != null)
		{
			Take(gun);
			play = Play.GoTo;
			playAt = Time.time;
			nav.SetGoal(this.item.worldCenterOfMass);
			return true;
		}
		if (!BotManager.OwnSpawns || Time.time - lastSpawnAt < 20f)
		{
			return false;
		}
		lastSpawnAt = Time.time;
		return SpawnGun();
	}

	public void AdoptHeldGun(Gun g)
	{
		Take(g);
		if (Holding())
		{
			StartShooting();
		}
		else
		{
			EndPlay();
		}
	}

	public void AdoptGun(Gun g, bool inHand)
	{
		if (inHand)
		{
			AdoptHeldGun(g);
		}
		else
		{
			ownGun = g;
		}
	}

	private bool ClaimedByAnother(Gun g)
	{
		if (g == null || !claims.TryGetValue(g.Pointer, out (Bot, float) value))
		{
			return false;
		}
		if (value.Item1 != this && value.Item1.Alive && !value.Item1.dead && value.Item1.gun == g)
		{
			return Time.time - value.Item2 < Tuning.F("gun.claim", 12f);
		}
		return false;
	}

	private static bool Usable(Gun g)
	{
		if (g == null || g.triggerGrip == null || g.triggerGrip.HasAttachedHands() || g.fireMode == Gun.FireMode.MANUAL)
		{
			return false;
		}
		if (wontLoad.TryGetValue(g.Pointer, out var value) && Time.time - value < 120f)
		{
			return false;
		}
		string text = g.name.ToUpperInvariant();
		if (text.Contains("SPAWN") || text.Contains("NIMBUS") || text.Contains("TOOL"))
		{
			return false;
		}
		Rigidbody componentInParent = g.triggerGrip.GetComponentInParent<Rigidbody>();
		if (componentInParent != null)
		{
			return !componentInParent.isKinematic;
		}
		return false;
	}

	private bool OwnGunTaken()
	{
		try
		{
			MarrowEntity componentInParent = ownGun.GetComponentInParent<MarrowEntity>();
			foreach (Grip componentsInChild in ((componentInParent != null) ? ((MonoBehaviour)componentInParent) : ((MonoBehaviour)ownGun)).GetComponentsInChildren<Grip>(includeInactive: false))
			{
				if (componentsInChild == null || componentsInChild.attachedHands == null)
				{
					continue;
				}
				Il2CppSystem.Collections.Generic.List<Hand>.Enumerator enumerator2 = componentsInChild.attachedHands.GetEnumerator();
				while (enumerator2.MoveNext())
				{
					Hand current2 = enumerator2.Current;
					if (current2 != null && current2 != LeftHand && current2 != RightHand)
					{
						return true;
					}
				}
			}
		}
		catch (System.Exception)
		{
		}
		return false;
	}

	private void Take(Gun g)
	{
		loadSettleUntil = 0f;
		loadFails = 0;
		directAsked = false;
		ownGun = g;
		step = Step.OneHand;
		stepAt = Time.time;
		aimFix = Vector2.zero;
		gun = g;
		claims[g.Pointer] = (this, Time.time);
		grip = g.triggerGrip;
		item = grip.GetComponentInParent<Rigidbody>();
		support = SupportGrip(g);
		frontRetry = null;
		if (support == null)
		{
			GunMoves.Hold hold = GunMoves.Held(GunMoves.KeyOf(g, twoHanded: false), twoHanded: false, aimed: true);
			if (hold != null && hold.left != null)
			{
				support = AnySecondGrip(g);
			}
		}
		GunMoves.Hold hold2 = GunMoves.Held(GunMoves.KeyOf(g, twoHanded: false), twoHanded: false, aimed: true);
		if (hold2 != null && hold2.left == null && support != null)
		{
			Lab.Event("holding " + g.name + " in one hand, as you showed it (a " + GunMoves.KindOf(g) + ")");
			support = null;
		}
		gunIsLong = support != null;
		if (support != null && Tuning.F("hold.twohands", 0f) < 0.5f)
		{
			support = null;
		}
		GunWeight.Lighten(g);
		supportHeld = false;
		supportTries = 0;
		supportTriedAt = -10f;
		triggerWorks = false;
		handMeasured = false;
		dryPulls = 0;
		gunWantSet = false;
		rightFix = Pose.identity;
		leftFix = Pose.identity;
		rightInGunSet = false;
		leftInGunSet = false;
		leftMeasured = false;
		wantedFireSince = -1f;
	}

	private static Grip AnySecondGrip(Gun g)
	{
		MarrowEntity componentInParent = g.GetComponentInParent<MarrowEntity>();
		foreach (Grip componentsInChild in ((componentInParent != null) ? ((MonoBehaviour)componentInParent) : ((MonoBehaviour)g)).GetComponentsInChildren<Grip>(includeInactive: false))
		{
			if (!(componentsInChild == null) && !(componentsInChild == g.triggerGrip) && !componentsInChild.HasAttachedHands() && !(componentsInChild.GetComponentInParent<Magazine>() != null))
			{
				string text = componentsInChild.name.ToLowerInvariant();
				if (!text.Contains("slide") && !text.Contains("charg") && !text.Contains("bolt") && !text.Contains("mag") && !text.Contains("capsule"))
				{
					return componentsInChild;
				}
			}
		}
		return null;
	}

	private static Grip SupportGrip(Gun g)
	{
		if (g.firePointTransform == null || g.triggerGrip == null)
		{
			return null;
		}
		Grip grip = GripBook.Steadying(GripBook.ThingOf(g.triggerGrip), g.triggerGrip);
		if (grip != null && !WorksBolt(g, grip))
		{
			Lab.Event("gun: front grip " + grip.name + ", as you hold it");
			return grip;
		}
		Vector3 forward = g.firePointTransform.forward;
		Vector3 position = g.triggerGrip.transform.position;
		System.Collections.Generic.List<Grip> list = new System.Collections.Generic.List<Grip>();
		if (g.otherGrips != null)
		{
			foreach (Grip otherGrip in g.otherGrips)
			{
				list.Add(otherGrip);
			}
		}
		MarrowEntity componentInParent = g.GetComponentInParent<MarrowEntity>();
		foreach (Grip componentsInChild in ((componentInParent != null) ? ((MonoBehaviour)componentInParent) : ((MonoBehaviour)g)).GetComponentsInChildren<Grip>(includeInactive: false))
		{
			if (!list.Contains(componentsInChild))
			{
				list.Add(componentsInChild);
			}
		}
		Grip grip2 = null;
		float num = float.MaxValue;
		System.Collections.Generic.List<string> list2 = new System.Collections.Generic.List<string>();
		foreach (Grip item in list)
		{
			if (item == null || item == g.triggerGrip || !item.gameObject.activeInHierarchy || WorksBolt(g, item))
			{
				continue;
			}
			list2.Add(item.name + " (" + Vector3.Dot(item.transform.position - position, forward).ToString("0.00") + " m ahead)");
			string text = item.name.ToLowerInvariant();
			bool flag = text.Contains("forwardgrip") || text.Contains("foregrip") || text.Contains("forward_grip") || text.Contains("verticalgrip");
			if (text.Contains("slide") || text.Contains("charg") || text.Contains("bolt") || text.Contains("mag") || text.Contains("pump") || text.Contains("sight") || (text.Contains("attachment") && !flag) || text.Contains("scope") || text.Contains("stock") || text.Contains("capsule") || item.GetComponentInParent<Magazine>() != null || (g.ammoSocket != null && item.transform.IsChildOf(g.ammoSocket.transform)))
			{
				continue;
			}
			float num2 = Vector3.Dot(item.transform.position - position, forward);
			if (!(num2 < 0.15f) && !(num2 > 0.5f))
			{
				float num3 = Mathf.Abs(num2 - 0.3f) - (flag ? 0.3f : ((text.Contains("fore") || text.Contains("hand")) ? 0.1f : 0f));
				if (num3 < num)
				{
					num = num3;
					grip2 = item;
				}
			}
		}
		MelonLogger.Msg("Bot Player: " + g.name + " grips: " + ((list2.Count > 0) ? string.Join(", ", list2) : "none but the handle") + "; front hand on " + ((grip2 != null) ? grip2.name : "nothing (one-handed)"));
		return grip2;
	}

	private bool SpawnGun(string forced = null)
	{
		Hand rightHand = RightHand;
		Transform head = Head;
		if (rightHand == null || head == null)
		{
			return false;
		}
		System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
		AssetWarehouse instance = AssetWarehouse.Instance;
		string[] spawnGuns = SpawnGuns;
		foreach (string newId in spawnGuns)
		{
			if (instance != null && instance.HasCrate(new Barcode(newId)))
			{
				list.Add(newId);
			}
		}
		if (forced != null)
		{
			if (instance == null || !instance.HasCrate(new Barcode(forced)))
			{
				return false;
			}
			list.Clear();
			list.Add(forced);
		}
		if (list.Count == 0)
		{
			Spawnables.Notify("None of the guns the bot knows are in this game");
			return false;
		}
		spawningGun = true;
		spawnAskedAt = Time.time;
		spawnAt = rightHand.transform.position;
		gunsBefore.Clear();
		foreach (Collider item in Physics.OverlapSphere(spawnAt, 1.5f, -1, QueryTriggerInteraction.Ignore))
		{
			Gun gun = ((item != null) ? item.GetComponentInParent<Gun>() : null);
			if (gun != null)
			{
				gunsBefore.Add(gun.Pointer);
			}
		}
		string text = forced ?? Knowledge.PickGun(list);
		MelonLogger.Msg("Bot Player: spawning a gun (" + text + ")");
		Lab.Event("spawning gun " + text);
		Vector3 position = rightHand.transform.position + Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward * 0.1f;
		try
		{
			Clone.Suppress();
			HelperMethods.SpawnCrate(text, position, Quaternion.Euler(0f, bodyYaw, 0f), Vector3.one, ignorePolicy: false, delegate(GameObject spawned)
			{
				if (spawningGun)
				{
					spawningGun = false;
					Gun gun2 = ((spawned != null) ? spawned.GetComponentInChildren<Gun>() : null);
					if (gun2 == null || !Alive || RightHand == null)
					{
						MelonLogger.Warning("Bot Player: the spawned gun could not be used (" + ((spawned != null) ? spawned.name : "nothing came") + ")");
					}
					else
					{
						Take(gun2);
						if (this.item != null && Grab(RightHand, grip))
						{
							AfterSpawnedGun();
						}
						else
						{
							EndPlay();
						}
					}
				}
			});
		}
		catch (System.Exception ex)
		{
			spawningGun = false;
			Spawnables.Notify("The bot could not spawn a gun: " + ex.Message);
			return false;
		}
		return true;
	}

	private bool TakeSpawnedGun()
	{
		Hand rightHand = RightHand;
		if (rightHand == null)
		{
			return false;
		}
		foreach (Collider item in Physics.OverlapSphere(spawnAt, 1.5f, -1, QueryTriggerInteraction.Ignore))
		{
			Gun gun = ((item != null) ? item.GetComponentInParent<Gun>() : null);
			if (!(gun == null) && !gunsBefore.Contains(gun.Pointer) && Usable(gun))
			{
				spawningGun = false;
				Take(gun);
				if (this.item != null && Grab(rightHand, grip))
				{
					AfterSpawnedGun();
				}
				else
				{
					EndPlay();
				}
				return true;
			}
		}
		return false;
	}

	private void AfterSpawnedGun()
	{
		if (!tossNext)
		{
			StartShooting();
			return;
		}
		tossNext = false;
		gun = null;
		support = null;
		holdFor = 0.8f;
		forceToss = true;
		Next(Play.Hold);
		Lab.Event("tossing " + item.name + " to the player");
	}

	private void StartShooting()
	{
		play = Play.Shoot;
		playAt = Time.time;
		target = default(Targets.Target);
		targetCheckAt = 0f;
		shootUntil = ((mode == Mode.Gunplay || (mode == Mode.Brain && hostiles.Count > 0)) ? float.MaxValue : (Time.time + UnityEngine.Random.Range(10f, 20f)));
		Load();
		loadSettleUntil = Time.time + Tuning.F("gun.settle", 1.5f);
		Lab.Event("holding " + gun.name + ((support != null) ? (", front grip " + support.name) : ", one-handed"));
		NoPushOnItself(gun.transform);
	}

	private static void FeedPouch(Gun g)
	{
		try
		{
			AmmoInventory instance = AmmoInventory.Instance;
			if (!(g == null) && !(instance == null) && !(g.defaultCartridge == null))
			{
				int count = ((g.defaultMagazine != null) ? Mathf.Max(1, g.defaultMagazine.rounds) : 30);
				instance.AddCartridge(g.defaultCartridge, count);
			}
		}
		catch (System.Exception)
		{
		}
	}

	private void Chamber()
	{
		try
		{
			if (gun.cartridgeState != Gun.CartridgeStates.UNSPENT && !(Time.time - firedAt < 1f / Mathf.Max(1f, gun.roundsPerSecond)))
			{
				gun.chamberedCartridge = gun.defaultCartridge;
				gun.cartridgeState = Gun.CartridgeStates.UNSPENT;
				gun.isCharged = true;
				if (gun.slideState == Gun.SlideStates.LOCKED)
				{
					gun.SlideRelease();
				}
			}
		}
		catch (System.Exception)
		{
		}
	}

	private void DirectLoad()
	{
		try
		{
			if (!(gun == null) && !(gun.ammoSocket == null) && !(gun.defaultMagazine == null))
			{
				FeedPouch(gun);
				directAsked = true;
				Transform transform = gun.ammoSocket.transform;
				SpawnMag(transform.position - transform.up * 0.1f, transform.rotation);
			}
		}
		catch (System.Exception)
		{
			directAsked = false;
		}
	}

	private bool SettleLoad()
	{
		try
		{
			if (gun == null)
			{
				return true;
			}
			if (directAsked && !gun.HasMagazine() && gun.ammoSocket != null && Time.time - magAskedAt > 0.15f)
			{
				Magazine magazine = SpawnedMag();
				if (magazine != null && magazine.magazinePlug != null)
				{
					directAsked = false;
					magazine.transform.SetPositionAndRotation(gun.ammoSocket.transform.position, gun.ammoSocket.transform.rotation);
					try
					{
						magazine.magazinePlug.InsertPlug(gun.ammoSocket);
					}
					catch (System.Exception)
					{
					}
					if (!gun.HasMagazine())
					{
						magazine.magazinePlug.ForceInSocket(gun.ammoSocket);
					}
					if (!gun.HasMagazine())
					{
						dropped.Add((magazine, Time.time));
					}
				}
			}
			if (gun.HasMagazine())
			{
				if (gun.MagazineState != null && gun.MagazineState.AmmoCount <= 0)
				{
					gun.MagazineState.Refill();
				}
				if (gun.slideState == Gun.SlideStates.LOCKED)
				{
					gun.SlideRelease();
				}
				if (gun.cartridgeState != Gun.CartridgeStates.UNSPENT)
				{
					gun.Charge();
				}
			}
			return gun.cartridgeState == Gun.CartridgeStates.UNSPENT || (gun.HasMagazine() && gun.AmmoCount() > 0);
		}
		catch (System.Exception)
		{
			return true;
		}
	}

	private void Load(bool reload = false)
	{
		FeedPouch(gun);
		try
		{
			if (!gun.HasMagazine())
			{
				gun.InstantLoadAsync();
			}
			else if (gun.MagazineState != null && (reload || gun.MagazineState.AmmoCount <= 0))
			{
				gun.MagazineState.Refill();
			}
			if (reload || !gun.isCharged || gun.cartridgeState != Gun.CartridgeStates.UNSPENT)
			{
				gun.Charge();
			}
			if (reload)
			{
				MelonLogger.Msg("Bot Player: reloaded " + gun.name + " (" + gun.AmmoCount() + " rounds)");
			}
		}
		catch (System.Exception ex)
		{
			MelonLogger.Warning("Bot Player: could not load " + gun.name + ": " + ex.Message);
		}
	}

	private bool NeedsRacking()
	{
		try
		{
			float num = Time.time - firedAt;
			bool num2 = gun.cartridgeState == Gun.CartridgeStates.NONE && num > 0.6f;
			bool flag = gun.cartridgeState == Gun.CartridgeStates.SPENT && num > 1.5f;
			return (num2 || flag) && gun.HasMagazine() && gun.AmmoCount() > 0;
		}
		catch (System.Exception)
		{
			return false;
		}
	}

	private bool LoadedButSilent()
	{
		try
		{
			return !magless.Contains(gun.Pointer) && gun.cartridgeState == Gun.CartridgeStates.UNSPENT && gun.slideState == Gun.SlideStates.RETURNED;
		}
		catch (System.Exception)
		{
			return false;
		}
	}

	private bool Unstick()
	{
		if (firedAt > unstuckAt)
		{
			unsticks = 0;
		}
		if (unsticks >= 2)
		{
			unsticks = 0;
			return false;
		}
		unsticks++;
		unstuckAt = Time.time;
		triggerWorks = false;
		string text = "";
		try
		{
			Hand rightHand = RightHand;
			bool flag = gun.triggerGrip != null && gun.triggerGrip.attachedHands != null && rightHand != null && gun.triggerGrip.attachedHands.Contains(rightHand);
			text = "hammer " + gun.hammerState.ToString() + ", charged " + gun.isCharged + ", fired once " + gun.hasFiredOnce + ", trigger pulled " + gun.isTriggerPulled + ", mode " + gun.fireMode.ToString() + ", its hand on the handle " + flag;
			gun.hasFiredOnce = false;
			gun.isTriggerPulled = false;
			gun.hammerState = Gun.HammerStates.COCKED;
			gun.isCharged = true;
			if (!flag && rightHand != null && gun.triggerGrip != null)
			{
				Grab(rightHand, gun.triggerGrip);
			}
		}
		catch (System.Exception ex)
		{
			text = text + " (" + ex.Message + ")";
		}
		Lab.Event("gun: loaded but it would not fire (" + text + "): woken");
		return true;
	}

	private bool Empty()
	{
		try
		{
			if (magless.Contains(gun.Pointer))
			{
				return false;
			}
			return gun.cartridgeState != Gun.CartridgeStates.UNSPENT && gun.AmmoCount() <= 0;
		}
		catch (System.Exception)
		{
			return false;
		}
	}

	private bool Shoot(Transform you, Transform head, float dt)
	{
		if (gun == null || !Holding() || head == null)
		{
			try
			{
				Hand rightHand = RightHand;
				Lab.Event("gun: out of its hand (" + ((gun == null) ? "no gun" : ((head == null) ? "no head" : ("its hand holds " + ((rightHand != null && rightHand.m_CurrentAttachedGO != null) ? rightHand.m_CurrentAttachedGO.name : "nothing") + ", hands on the grip " + ((grip != null && grip.attachedHands != null) ? grip.attachedHands.Count : (-1)) + ", step " + step.ToString() + (Reloading ? (", reloading " + reload) : "")))) + ")");
			}
			catch (System.Exception)
			{
			}
			return EndPlay();
		}
		if (Time.time > shootUntil)
		{
			Lab.Event("gun: done shooting (its time is up), put down");
			Next(Play.Drop);
			return true;
		}
		Transform firePointTransform = gun.firePointTransform;
		if (firePointTransform == null)
		{
			return EndPlay();
		}
		bool flag = mode == Mode.Brain;
		bool flag2 = goal == Goal.Explore || goal == Goal.Follow || goal == Goal.Idle || goal == Goal.Investigate || goal == Goal.Guard;
		bool flag3 = goal == Goal.TakeCover && coverReachedAt > 0f;
		bool flag4 = flag && ((flag2 && nearestSeen.transform == null && AmmoFraction() < ReloadBelow) || (flag3 && AmmoFraction() < 0.6f));
		bool flag5 = wantedFireSince > 0f && Time.time - wantedFireSince > 2f && Time.time - firedAt > 2f;
		if (magless.Contains(gun.Pointer))
		{
			flag5 = (flag4 = false);
		}
		if (Time.time - reloadedAt < Tuning.F("reload.rest", 6f))
		{
			flag5 = (flag4 = false);
		}
		bool flag6 = false;
		try
		{
			flag6 = !gun.HasMagazine();
		}
		catch (System.Exception)
		{
		}
		if (!flag6)
		{
			noMagSince = Time.time;
		}
		if (!Reloading && Time.time < loadSettleUntil)
		{
			if (!SettleLoad())
			{
				Pull(on: false);
				noMagSince = Time.time;
				dryPulls = 0;
				wantedFireSince = -1f;
				return true;
			}
			loadSettleUntil = 0f;
			loadFails = 0;
			NoPushOnItself(gun.transform, again: true);
		}
		else if (!Reloading && loadSettleUntil > 0f)
		{
			loadSettleUntil = 0f;
			if (!SettleLoad())
			{
				loadFails++;
				if (loadFails < 2)
				{
					Lab.Event("gun: the " + gun.name + " is still empty after loading: a magazine put straight into it");
					DirectLoad();
					loadSettleUntil = Time.time + Tuning.F("gun.settle", 1.5f) + 0.5f;
					return true;
				}
				loadFails = 0;
				magless.Add(gun.Pointer);
				Lab.Event("gun: no magazine to be had for the " + gun.name + ": its rounds go straight into its chamber");
			}
			else
			{
				loadFails = 0;
			}
		}
		if (!Reloading && magless.Contains(gun.Pointer))
		{
			float num = 1f / Mathf.Max(1f, gun.roundsPerSecond);
			if (trigger > 0.5f && Time.time - triggerSince > 0.1f && Time.time - firedAt > num && Time.time - forcedAt > Mathf.Max(0.12f, num) && (firedAt < triggerSince || gun.fireMode == Gun.FireMode.AUTOMATIC))
			{
				forcedAt = Time.time;
				FeedPouch(gun);
				try
				{
					gun.chamberedCartridge = gun.defaultCartridge;
					gun.cartridgeState = Gun.CartridgeStates.UNSPENT;
					gun.isCharged = true;
					gun.Fire();
				}
				catch (System.Exception)
				{
				}
				forcedDry++;
			}
			if (firedAt > forcedSeen)
			{
				forcedSeen = firedAt;
				forcedDry = 0;
			}
			dryPulls = 0;
			if (forcedDry >= 8)
			{
				forcedDry = 0;
				magless.Remove(gun.Pointer);
				wontLoad[gun.Pointer] = Time.time;
				Lab.Event("gun: the " + gun.name + " will not fire, putting it down for another");
				Next(Play.Drop);
				return true;
			}
			Chamber();
			noMagSince = Time.time;
		}
		WatchBolt();
		if (!Reloading && dryPulls >= 2 && LoadedButSilent() && Unstick())
		{
			dryPulls = 0;
		}
		if (!Reloading && (Empty() || dryPulls >= 3 || flag4 || flag5 || Time.time - noMagSince > 1.2f))
		{
			StartReload();
		}
		else if (!Reloading && NeedsRacking())
		{
			StartReload(rackOnly: true);
		}
		if (Reloading)
		{
			Pull(on: false);
			ReloadStep(head);
			if (!flag || !CoverWhileReloading(dt))
			{
				Targets.Target shooter;
				if (flag && goal == Goal.Retreat && hasGoalPlace)
				{
					if (!nav.HasGoal)
					{
						nav.SetGoal(goalPlace);
					}
					if (Travel(MotionLibrary.RunPush, 1f, dt))
					{
						hasGoalPlace = false;
					}
				}
				else if (flag && GunThreat(out shooter) && Tuning.F("fight.reload.move", 1f) > 0.5f)
				{
					Vector2 vector = Footwork(head, shooter, dt) * 0.6f;
					PlanCrouch(head, shooter, vector);
					walk = Vector2.ClampMagnitude(vector, crouchPlan ? (MotionLibrary.WalkPush * 0.4f) : MotionLibrary.WalkPush);
					Ducking(dt);
				}
				else
				{
					walk = Vector2.zero;
				}
			}
			return true;
		}
		HoldSupport();
		if (step != Step.Ready)
		{
			Pull(on: false);
			walk = Vector2.zero;
			bool flag7 = (step == Step.Reach || step == Step.Settle) && LearnedHold(aimed: true) != null;
			Vector3 point = (flag7 ? (head.position + Quaternion.Euler(0f, bodyYaw, 0f) * new Vector3(0f, -0.3f, 5f)) : (head.position + Quaternion.Euler(0f, bodyYaw, 0f) * new Vector3(0f, -1.3f, 2.5f)));
			Aim(point, head, (!flag7) ? Hold.Low : Hold.Aimed);
			labAimError = -1f;
			return true;
		}
		if (target.transform != null && (!Targets.Alive(target) || (target.prop != null && shotsAtTarget >= 4)))
		{
			Targets.Shot(target);
			target = default(Targets.Target);
			doneWithTargetAt = Time.time;
			Pull(on: false);
		}
		if (Time.time - doneWithTargetAt < 0.6f)
		{
			walk = Vector2.zero;
			Pull(on: false);
			Aim((lastAimPoint != Vector3.zero) ? lastAimPoint : Lookout(head), head, Hold.Aimed);
			return true;
		}
		if (flag && goal != Goal.Practice)
		{
			FollowBrainsTarget();
		}
		else
		{
			PickOwnTarget(head);
		}
		if (play != Play.Shoot || gun == null)
		{
			return false;
		}
		bool flag8 = flag && (goal == Goal.Retreat || (goal == Goal.TakeCover && coverReachedAt < 0f)) && hasGoalPlace;
		if (target.transform == null || flag8)
		{
			Pull(on: false);
			bool flag9 = Time.time - lastTargetAt < Tuning.F("fire.ready", 3f) && lastAimPoint != Vector3.zero && !flag8;
			Aim(flag9 ? lastAimPoint : Lookout(head), head, (!flag9) ? Hold.Low : Hold.Aimed);
			labAimError = -1f;
			if (flag)
			{
				if (goal == Goal.Follow)
				{
					Follow(you, head, dt);
				}
				else if (hasGoalPlace)
				{
					if (!nav.HasGoal)
					{
						nav.SetGoal(goalPlace);
					}
					running = goal == Goal.Retreat || goal == Goal.TakeCover || ((goal == Goal.Investigate || goal == Goal.ArmUp || goal == Goal.Guard || goal == Goal.Fight) && nav.HasGoal && Flat(nav.Goal - Feet).magnitude > 6f);
					if (Travel(running ? MotionLibrary.RunPush : (MotionLibrary.WalkPush * 0.85f), 1f, dt) || nav.StuckCount >= 3)
					{
						Arrived();
					}
				}
				else
				{
					walk = Vector2.zero;
					if (Time.time < lookUntil || (goal == Goal.Guard && PlayerWatch.Fighting))
					{
						Face(Lookout(head), dt, 120f);
					}
				}
				return true;
			}
			if (!nav.HasGoal && Navigator.RandomPlace(Feet, 3f, 10f, out var place))
			{
				nav.SetGoal(place);
			}
			if (nav.HasGoal && (Travel(MotionLibrary.WalkPush * 0.8f, 0.8f, dt) || nav.StuckCount >= 3))
			{
				nav.Clear();
			}
			return true;
		}
		nav.Clear();
		Vector3 vector2 = TargetPoint() + AimSpot(head.position);
		float magnitude = Flat(vector2 - head.position).magnitude;
		float num2 = (flag ? Knowledge.FightDistance : 8f) * personality.distance;
		int odds = 0;
		bool flag10 = false;
		if (flag && target.hostile)
		{
			num2 *= TeamKeep(out odds);
		}
		Vector2 move = Vector2.zero;
		if (target.hostile)
		{
			float num3 = Knowledge.Style.Mean(Knowledge.Style.giveGround, 0.6f);
			bool flag11 = Vector3.Dot(Tactics.Velocity(target), (head.position - vector2).normalized) > 1f;
			if (magnitude < Mathf.Max(3f, num2 - 2.5f) || (flag11 && magnitude < num2 && num3 > 0.4f))
			{
				move.y = (0f - Mathf.Clamp01((num2 - magnitude) / 3f + 0.3f)) * ((odds < 0) ? (MotionLibrary.RunPush * Tuning.F("team.fall.run", 0.85f)) : MotionLibrary.WalkPush);
				Vector3 back = Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.back;
				flag10 = !Tactics.RoomBehind(head.position - Vector3.up * 0.5f, back);
				if (flag10)
				{
					move.y = 0f;
					if (Time.time > sidestepUntil)
					{
						sidestep = new Vector2((UnityEngine.Random.value < 0.5f) ? (-1f) : 1f, 0f) * MotionLibrary.WalkPush;
						sidestepUntil = Time.time + 0.8f;
					}
				}
			}
			else if (odds > 0 && magnitude > num2 + 1f && magnitude <= num2 + 6f)
			{
				move.y = MotionLibrary.WalkPush;
			}
			else if (magnitude > num2 + 6f)
			{
				move.y = ((magnitude > num2 + 10f) ? MotionLibrary.RunPush : MotionLibrary.WalkPush);
			}
		}
		bool num4 = flag && target.hostile && ShootsBack(target) && Tuning.F("fight.move", 1f) > 0.5f;
		bool holdFire = false;
		if (num4)
		{
			bool flag12 = odds < 0 && magnitude < num2 - 2.5f;
			if (flag12 || !CoverStep(head, vector2, ref move, ref holdFire, dt))
			{
				float y = move.y;
				move += Footwork(head, target, dt);
				if (flag12)
				{
					move.y = Mathf.Min(move.y, y);
				}
				if (!flag12)
				{
					PlanCrouch(head, target, move);
				}
				else
				{
					crouchPlan = false;
				}
				if (crouchPlan)
				{
					move = Vector2.ClampMagnitude(move, MotionLibrary.WalkPush * 0.4f);
				}
				running = move.magnitude > MotionLibrary.WalkPush + 0.05f;
			}
			Ducking(dt);
		}
		else
		{
			float num5 = Mathf.Lerp(0.25f, 1.6f, Knowledge.Style.Mean(Knowledge.Style.strafe, 0.25f));
			if (Time.time > sidestepUntil + 1.5f && UnityEngine.Random.value < dt * num5)
			{
				sidestep = new Vector2((UnityEngine.Random.value < 0.5f) ? (-1f) : 1f, 0f) * MotionLibrary.WalkPush * 0.7f;
				sidestepUntil = Time.time + UnityEngine.Random.Range(0.4f, 0.9f);
			}
			if (Time.time < sidestepUntil)
			{
				move += sidestep;
			}
		}
		running = move.magnitude > MotionLibrary.WalkPush + 0.05f;
		walk = Vector2.ClampMagnitude(move, 1f);
		if (flag && Time.time - teamSaidAt > 3f)
		{
			teamSaidAt = Time.time;
			Lab.Event("team: odds " + odds + ", keeps " + num2.ToString("0.0") + " m, is " + magnitude.ToString("0.0") + " m off, moving " + move.y.ToString("0.00") + " ahead" + (running ? ", running" : "") + (flag10 ? ", no room behind" : "") + (holdFire ? ", in cover" : ""));
		}
		Face(vector2, dt, 260f);
		wobble = new Vector2(Mathf.PerlinNoise(Time.time * 0.7f, 3.1f) - 0.5f, Mathf.PerlinNoise(5.7f, Time.time * 0.7f) - 0.5f) * Mathf.Lerp(Tuning.F("aim.shake.poor", 3.4f), Tuning.F("aim.shake.good", 0.9f), personality.aim);
		lastAimPoint = vector2;
		lastTargetAt = Time.time;
		Aim(vector2, head, Hold.Aimed);
		Vector3 normalized = (vector2 - firePointTransform.position).normalized;
		float num6 = (labAimError = Vector3.Angle(firePointTransform.forward, normalized));
		bool flag13 = Targets.NearPlayer(vector2, 1.2f);
		bool flag14 = target.player || enemy || ((!flag13) ? (!Targets.PlayerInLine(firePointTransform.position, firePointTransform.forward, 1f)) : (target.hostile && Targets.ClearOfPlayer(firePointTransform.position, vector2) && !Targets.PlayerInLine(firePointTransform.position, firePointTransform.forward, 0.5f)));
		float b = Mathf.Atan2(Skill[1], Mathf.Max(1f, (vector2 - firePointTransform.position).magnitude)) * 57.29578f;
		bool flag15 = Time.time - targetSince > reaction && num6 < Mathf.Max(0.8f, Mathf.Min(Skill[0], b));
		shotRange = (vector2 - firePointTransform.position).magnitude;
		flag15 = flag15 && !holdFire;
		Fire(flag15 && flag14);
		bool flag16 = target.hostile && (vector2 - head.position).magnitude < Tuning.F("assist.close", 2.3f) && num6 > Tuning.F("assist.close.off", 20f);
		if (target.hostile && (!flag14 || flag16))
		{
			if (!flag13 && (vector2 - head.position).magnitude >= Tuning.F("assist.within", 14f) && Time.time > sidestepUntil + 0.5f)
			{
				sidestep = new Vector2((UnityEngine.Random.value < 0.5f) ? (-1f) : 1f, 0f) * MotionLibrary.WalkPush;
				sidestepUntil = Time.time + 1.2f;
			}
			if (unsafeSince < 0f)
			{
				unsafeSince = Time.time;
			}
			if (Time.time - unsafeSince > Tuning.F("assist.after", 1.5f) && (vector2 - head.position).magnitude < Tuning.F("assist.within", 14f))
			{
				unsafeSince = -1f;
				assistUntil = Time.time + Tuning.F("assist.for", 7f);
				assistBegan = (assistSawAt = Time.time);
				Lab.Event("assist: no safe shot at what is on you, in with its hands");
				Stow();
				return false;
			}
		}
		else
		{
			unsafeSince = -1f;
		}
		if (flag15 && flag14 && wantedFireSince < 0f)
		{
			wantedFireSince = Time.time;
		}
		else if (!(flag15 && flag14) || Time.time - firedAt < 0.2f)
		{
			wantedFireSince = ((flag15 && flag14) ? Time.time : (-1f));
		}
		if (flag15 && flag14)
		{
			engaged = target;
			engagedAt = Time.time;
		}
		return true;
	}

	private static bool ShootsBack(Targets.Target t)
	{
		if (t.transform == null)
		{
			return false;
		}
		if (t.player)
		{
			return Targets.MuzzleOf(t.transform) != null;
		}
		if (t.bot != null && !t.bot.dead && t.bot.gun != null)
		{
			return t.bot.Holding();
		}
		return false;
	}

	private static Transform MuzzleOf(Targets.Target t)
	{
		if (t.player)
		{
			return Targets.MuzzleOf(t.transform);
		}
		if (t.bot == null || !(t.bot.gun != null))
		{
			return null;
		}
		return t.bot.gun.firePointTransform;
	}

	private bool GunThreat(out Targets.Target shooter)
	{
		shooter = default(Targets.Target);
		if (ShootsBack(mostDangerous))
		{
			shooter = mostDangerous;
		}
		else if (ShootsBack(nearestSeen))
		{
			shooter = nearestSeen;
		}
		else if (ShootsBack(target))
		{
			shooter = target;
		}
		return shooter.transform != null;
	}

	private void Duck(float seconds)
	{
		duckUntil = Time.time + seconds;
		duckDepth = 0f - UnityEngine.Random.Range(Tuning.F("fight.duck.least", 0.45f), Tuning.F("fight.duck.most", 0.85f));
	}

	private void PlanCrouch(Transform head, Targets.Target foe, Vector2 move)
	{
		crouchPlannedAt = Time.time;
		if (head == null || foe.transform == null || Tuning.F("fight.crouch.on", 1f) < 0.5f)
		{
			crouchPlan = false;
			duckUntil = 0f;
			return;
		}
		if (Time.time >= crouchLookAt)
		{
			crouchLookAt = Time.time + 0.4f;
			float num = Mathf.Clamp(standingHeight / 1.6f, 0.5f, 2f);
			Vector3 vector = Feet;
			Vector3 vector2 = vector + Vector3.up * standingHeight;
			Vector3 eye = vector + Vector3.up * (standingHeight - Tuning.F("fight.crouch.drop", 0.55f) * num);
			Vector3 point = foe.Point;
			bool flag = Targets.InSight(vector2, point, foe.transform, rig);
			bool flag2 = Targets.InSight(eye, point, foe.transform, rig);
			float magnitude = (point - vector2).magnitude;
			bool flag3 = move.magnitude > MotionLibrary.WalkPush * 0.75f;
			bool flag4 = Time.time - hurtAt < 1.5f;
			bool flag5 = flag && !flag2;
			bool flag6;
			string text;
			if (flag3)
			{
				flag6 = false;
				text = "running";
			}
			else if (Reloading || flag4)
			{
				flag6 = flag5 || magnitude > Tuning.F("fight.crouch.open", 8f);
				text = (Reloading ? "reloading" : "hit");
			}
			else if (!flag2)
			{
				flag6 = false;
				text = "seeing over";
			}
			else
			{
				float num2 = Mathf.InverseLerp(6f, 22f, magnitude) * Tuning.F("fight.crouch.like", 0.8f) * Mathf.Lerp(0.6f, 1.3f, personality.calm / 1.8f);
				flag6 = move.magnitude < MotionLibrary.WalkPush * 0.5f && UnityEngine.Random.value < num2;
				text = "a steadier shot";
			}
			bool flag7 = Time.time - dodgedAt < 0.6f;
			bool flag8 = (crouchPlan && !flag6 && ((flag3 && flag7) || !flag2)) || (!crouchPlan && flag6 && (Reloading || flag4) && Time.time - crouchPlannedSince > 0.8f);
			if (flag6 != crouchPlan && (Time.time >= crouchPlanUntil || flag8))
			{
				crouchPlan = flag6;
				crouchPlannedSince = Time.time;
				crouchPlanUntil = Time.time + (flag6 ? UnityEngine.Random.Range(2.5f, 5f) : UnityEngine.Random.Range(2f, 4.5f));
				if (flag6)
				{
					duckDepth = 0f - UnityEngine.Random.Range(0.5f, 0.8f);
					footMove = Vector2.zero;
					footUntil = crouchPlanUntil;
				}
				Lab.Event("crouch: " + (flag6 ? "down" : "up") + " (" + text + (flag5 ? ", behind something" : "") + ", " + magnitude.ToString("0") + " m off)");
			}
		}
		duckUntil = (crouchPlan ? (Time.time + 0.5f) : 0f);
	}

	private void Ducking(float dt)
	{
		duckNow = Mathf.MoveTowards(duckNow, (Time.time < duckUntil) ? duckDepth : 0f, dt * 2.2f);
		if (duckNow < -0.02f)
		{
			crouching = true;
			crouch = duckNow;
			feet = 0f;
			crouchReset = true;
		}
	}

	private Vector2 Footwork(Transform head, Targets.Target shooter, float dt)
	{
		if (footLikes == null)
		{
			footLikes = new float[6];
			for (int i = 0; i < footLikes.Length; i++)
			{
				footLikes[i] = UnityEngine.Random.Range(0.25f, 1.6f);
			}
			footLikes[5] *= Tuning.F("fight.still", 0.35f);
			footPace = UnityEngine.Random.Range(0.75f, 1f);
			footLong = UnityEngine.Random.Range(0.7f, 1.5f);
			footSide = ((UnityEngine.Random.value < 0.5f) ? (-1f) : 1f);
			footUntil = Time.time + UnityEngine.Random.Range(0f, 0.6f);
		}
		Vector3 vector = head.position - Vector3.up * 0.4f;
		bool flag = Time.time - hurtAt < 0.35f;
		Vector3 outOfIt = Vector3.zero;
		bool flag2 = Tactics.InLineOf(MuzzleOf(shooter), vector, Tuning.F("fight.dodge.wide", 0.55f), out outOfIt);
		if ((flag2 || flag) && Time.time - dodgedAt > Tuning.F("fight.dodge.every", 0.8f))
		{
			dodgedAt = Time.time;
			if (flag2)
			{
				Vector3 vector2 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * outOfIt;
				footSide = ((Mathf.Abs(vector2.x) > 0.15f) ? Mathf.Sign(vector2.x) : (0f - footSide));
			}
			else
			{
				footSide = 0f - footSide;
			}
			footMove = new Vector2(footSide, UnityEngine.Random.Range(-0.35f, 0.2f)).normalized * MotionLibrary.RunPush;
			footUntil = Time.time + UnityEngine.Random.Range(0.5f, 0.9f);
		}
		else if (Time.time > footUntil)
		{
			float num = 0f;
			for (int j = 0; j < footLikes.Length; j++)
			{
				num += ((j == 5 && footMove == Vector2.zero) ? 0f : footLikes[j]);
			}
			float num2 = UnityEngine.Random.value * num;
			int num3 = 0;
			for (int k = 0; k < footLikes.Length; k++)
			{
				float num4 = ((k == 5 && footMove == Vector2.zero) ? 0f : footLikes[k]);
				if (num2 < num4)
				{
					num3 = k;
					break;
				}
				num2 -= num4;
			}
			if (FriendBeside(vector, out var side) && UnityEngine.Random.value < 0.8f)
			{
				footSide = 0f - side;
			}
			else if (UnityEngine.Random.value < 0.5f)
			{
				footSide = 0f - footSide;
			}
			float num5 = Mathf.Lerp(MotionLibrary.WalkPush, MotionLibrary.RunPush, UnityEngine.Random.Range(0.5f, 1f)) * footPace;
			float num6 = UnityEngine.Random.Range(0.6f, 1.4f) * footLong;
			switch (num3)
			{
			case 0:
				footMove = new Vector2(footSide, UnityEngine.Random.Range(-0.2f, 0.2f)).normalized * num5;
				break;
			case 1:
				footMove = new Vector2(footSide, UnityEngine.Random.Range(0.5f, 1f)).normalized * num5;
				break;
			case 2:
				footMove = new Vector2(footSide, 0f - UnityEngine.Random.Range(0.5f, 1f)).normalized * num5;
				break;
			case 3:
				footMove = new Vector2(footSide, UnityEngine.Random.Range(-0.15f, 0.25f)).normalized * MotionLibrary.RunPush;
				num6 = UnityEngine.Random.Range(1.6f, 3.2f);
				break;
			case 4:
				footMove = new Vector2(footSide * UnityEngine.Random.Range(0f, 0.35f), 1f).normalized * num5;
				num6 *= 0.7f;
				break;
			default:
				footMove = Vector2.zero;
				num6 = UnityEngine.Random.Range(0.25f, 0.7f);
				break;
			}
			footUntil = Time.time + num6;
		}
		if (footMove != Vector2.zero && !RoomToStep(vector, footMove))
		{
			footSide = 0f - footSide;
			footMove = new Vector2(0f - footMove.x, footMove.y);
			if (!RoomToStep(vector, footMove))
			{
				footMove = Vector2.zero;
				footUntil = Time.time + 0.4f;
			}
		}
		return footMove;
	}

	private bool FriendBeside(Vector3 chest, out float side)
	{
		side = 0f;
		float num = Tuning.F("fight.spread", 5f);
		foreach (Bot item in BotManager.All)
		{
			if (item == this || item.dead || item.enemy != enemy || !item.Whole || item.Head == null)
			{
				continue;
			}
			Vector3 vector = Flat(item.Head.position - chest);
			if (!(vector.magnitude >= num))
			{
				Vector3 vector2 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * vector;
				if (!(Mathf.Abs(vector2.x) < 0.2f))
				{
					num = vector.magnitude;
					side = Mathf.Sign(vector2.x);
				}
			}
		}
		return side != 0f;
	}

	private bool RoomToStep(Vector3 chest, Vector2 push)
	{
		Vector3 vector = Quaternion.Euler(0f, bodyYaw, 0f) * new Vector3(push.x, 0f, push.y).normalized;
		if (Physics.SphereCast(chest, 0.2f, vector, out var hitInfo, 1.1f, -1, QueryTriggerInteraction.Ignore))
		{
			Rigidbody rigidbody = hitInfo.rigidbody;
			if ((!(hitInfo.collider != null) || !(hitInfo.collider.GetComponentInParent<RigManager>() == rig)) && (rigidbody == null || rigidbody.isKinematic || rigidbody.mass > 25f))
			{
				return false;
			}
		}
		return Physics.Raycast(chest + vector * 0.9f, Vector3.down, 2.6f, -1, QueryTriggerInteraction.Ignore);
	}

	private void FollowBrainsTarget()
	{
		Targets.Target target = ((goal == Goal.Fight || goal == Goal.Guard || (goal == Goal.TakeCover && (coverReachedAt > 0f || !hasGoalPlace))) ? brainTarget : default(Targets.Target));
		if (!(target.transform == this.target.transform))
		{
			this.target = target;
			targetSince = Time.time;
			shotsAtTarget = 0;
			reaction = UnityEngine.Random.Range(0.15f, 0.4f) * personality.reaction * ((enemy && Furious) ? Tuning.F("fury.reaction", 0.5f) : 1f);
		}
	}

	private void PickOwnTarget(Transform head)
	{
		if (target.transform == null && Targets.OnPlayerFor == rig.Pointer && Time.time - Targets.OnPlayerAt < 0.6f && !Reloading && Time.time - firedAt > Tuning.F("assist.quiet", 5f) && Time.time - lastEnemyTime > Tuning.F("assist.quiet", 5f))
		{
			if (unsafeSince < 0f)
			{
				unsafeSince = Time.time;
			}
			if (Time.time - unsafeSince > Tuning.F("assist.after", 1.5f))
			{
				unsafeSince = -1f;
				assistUntil = Time.time + Tuning.F("assist.for", 7f);
				assistBegan = (assistSawAt = Time.time);
				Lab.Event("assist: no safe shot at what is on you, in with its hands");
				Stow();
				return;
			}
		}
		else if (target.transform != null || Time.time - Targets.OnPlayerAt > 1.5f)
		{
			unsafeSince = -1f;
		}
		if (Time.time < targetCheckAt)
		{
			return;
		}
		targetCheckAt = Time.time + Tuning.F("brain.look.every", 0.15f);
		if (target.transform != null && Targets.InSight(head.position, TargetPoint(), target.transform, rig) && (target.player || !Targets.NearPlayer(TargetPoint(), target.hostile ? 1.2f : 2.5f)))
		{
			lostTargetAt = -1f;
			if (!(Time.time - targetSince > Tuning.F("target.hold", 1.2f)) || !(Time.time >= nearerCheckAt))
			{
				return;
			}
			nearerCheckAt = Time.time + 0.6f;
			if (Targets.Find(head.position, 50f, rig, out var found) && found.transform != target.transform && found.hostile)
			{
				float magnitude = (TargetPoint() - head.position).magnitude;
				float magnitude2 = (found.Point - head.position).magnitude;
				if (magnitude2 < magnitude * Tuning.F("target.nearer", 0.65f) && magnitude - magnitude2 > 3f)
				{
					Lab.Event("target: one nearer (" + magnitude2.ToString("0") + " m against " + magnitude.ToString("0") + " m): that one");
					target = found;
					targetSince = Time.time;
					shotsAtTarget = 0;
					reaction = UnityEngine.Random.Range(0.15f, 0.4f) * personality.reaction * ((enemy && Furious) ? Tuning.F("fury.reaction", 0.5f) : 1f);
				}
			}
			return;
		}
		if (lostTargetAt < 0f)
		{
			lostTargetAt = Time.time;
		}
		if (target.transform != null && Time.time - lostTargetAt <= ((coverPhase != 0) ? (Tuning.F("cover.keep", 15f) * 0.8f) : Tuning.F("target.lost", 1.5f)))
		{
			return;
		}
		if (Targets.Find(head.position, 50f, rig, out var found2))
		{
			bool num = target.transform != null && found2.transform == target.transform;
			target = found2;
			if (!num)
			{
				targetSince = Time.time;
				shotsAtTarget = 0;
				reaction = UnityEngine.Random.Range(0.15f, 0.4f) * personality.reaction * ((enemy && Furious) ? Tuning.F("fury.reaction", 0.5f) : 1f);
			}
			lostTargetAt = -1f;
		}
		else
		{
			target = default(Targets.Target);
		}
	}

	private void ZombieShot()
	{
		try
		{
			if (!zombieLooked)
			{
				zombieLooked = true;
				Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
				for (int i = 0; i < assemblies.Length; i++)
				{
					System.Type type = assemblies[i].GetType("ThirdEyeBonelabMod.Zombies.ZombieManager", throwOnError: false);
					if (!(type == null))
					{
						zombieHit = type.GetMethod("ArrowHit", BindingFlags.Static | BindingFlags.Public);
						FieldInfo field = type.GetField("BulletDamage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
						if (field != null)
						{
							zombieDamage = System.Convert.ToSingle(field.GetValue(null));
						}
					}
				}
			}
			if (zombieHit == null || gun == null || gun.firePointTransform == null)
			{
				return;
			}
			Transform firePointTransform = gun.firePointTransform;
			Transform transform = ((Player.RigManager != null) ? Player.RigManager.transform : null);
			float num = float.MaxValue;
			float num2 = float.MaxValue;
			bool flag = false;
			RaycastHit raycastHit = default(RaycastHit);
			foreach (RaycastHit item in Physics.RaycastAll(firePointTransform.position, firePointTransform.forward, 150f, -1, QueryTriggerInteraction.Ignore))
			{
				Collider collider = item.collider;
				if (collider == null)
				{
					continue;
				}
				Transform transform2 = collider.transform;
				if (!transform2.IsChildOf(gun.transform) && (!(transform != null) || !transform2.IsChildOf(transform)))
				{
					bool flag2 = transform2.IsChildOf(rig.transform);
					if (item.distance < num)
					{
						num = item.distance;
						flag = flag2;
					}
					if (!flag2 && item.distance < num2)
					{
						num2 = item.distance;
						raycastHit = item;
					}
				}
			}
			if (Time.time - zombieSaidAt > 2f)
			{
				zombieSaidAt = Time.time;
				Lab.Event("shot: along the barrel first " + ((num < float.MaxValue) ? (num.ToString("0.0") + " m" + (flag ? " (its own body)" : "")) : "nothing") + "; past itself: " + ((raycastHit.collider != null) ? (raycastHit.collider.name + " of " + raycastHit.collider.transform.root.name + " at " + num2.ToString("0.0") + " m") : "nothing") + "; aiming at " + ((target.transform != null) ? (target.transform.name + " " + (target.Point - firePointTransform.position).magnitude.ToString("0.0") + " m off, " + Vector3.Angle(firePointTransform.forward, target.Point - firePointTransform.position).ToString("0.0") + " deg off the barrel") : "nothing"));
			}
			if (flag && !(raycastHit.collider == null))
			{
				zombieHit.Invoke(null, new object[4] { raycastHit.collider, raycastHit.point, firePointTransform.forward, zombieDamage });
			}
		}
		catch (System.Exception)
		{
		}
	}

	private void NoteShot()
	{
		try
		{
			if (!(target.transform == null) && !(gun == null) && !(gun.firePointTransform == null) && !target.player && !(target.prop != null) && target.hostile)
			{
				_ = Skill;
				Transform firePointTransform = gun.firePointTransform;
				bool flag = false;
				if (Physics.Raycast(firePointTransform.position, firePointTransform.forward, out var hitInfo, 120f, -1, QueryTriggerInteraction.Ignore) && hitInfo.collider != null)
				{
					Transform transform = hitInfo.collider.transform;
					flag = transform == target.transform || transform.IsChildOf(target.transform) || (target.prop != null && hitInfo.rigidbody == target.prop) || (target.aim != null && (hitInfo.point - target.Point).magnitude < 0.45f);
				}
				skillShots++;
				if (flag)
				{
					skillHits++;
				}
				skillShooting += Mathf.Min(Time.time - skillLastShot, 2f);
				skillLastShot = Time.time;
				if (skillShots >= 15)
				{
					ShootSkill.End(rig.Pointer, shootTrying, skillShots, skillHits, skillShooting);
					skillShots = (skillHits = 0);
					skillShooting = 0f;
					shootSkill = null;
				}
			}
		}
		catch (System.Exception)
		{
		}
	}

	private Vector3 TargetPoint()
	{
		if (!(target.prop != null))
		{
			return target.Point;
		}
		return target.prop.worldCenterOfMass;
	}

	private void NewAimSpot()
	{
		float num = Mathf.Clamp01(personality.aim * Tuning.F("aim.skill", 1f));
		(string, Vector2, float)[] array = new(string, Vector2, float)[8]
		{
			("head", new Vector2(0f, 0.42f), 0.06f + 0.18f * num),
			("chest", new Vector2(0f, 0f), 0.3f + 0.3f * num),
			("belly", new Vector2(0f, -0.22f), 0.2f),
			("hips", new Vector2(0f, -0.42f), 0.05f + 0.12f * (1f - num)),
			("left shoulder", new Vector2(-0.2f, 0.12f), 0.04f + 0.1f * (1f - num)),
			("right shoulder", new Vector2(0.2f, 0.12f), 0.04f + 0.1f * (1f - num)),
			("left leg", new Vector2(-0.12f, -0.72f), 0.03f + 0.1f * (1f - num)),
			("right leg", new Vector2(0.12f, -0.72f), 0.03f + 0.1f * (1f - num))
		};
		float num2 = 0f;
		(string, Vector2, float)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(string, Vector2, float) tuple = array2[i];
			num2 += tuple.Item3;
		}
		float num3 = UnityEngine.Random.value * num2;
		array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(string, Vector2, float) tuple2 = array2[i];
			if (num3 < tuple2.Item3)
			{
				(aimPart, aimSpot, _) = tuple2;
				break;
			}
			num3 -= tuple2.Item3;
		}
		aimSpot += UnityEngine.Random.insideUnitCircle * Tuning.F("aim.scatter", 0.22f) * (1f - num);
	}

	private Vector3 AimSpot(Vector3 from)
	{
		if (!AimsAtABody || Tuning.F("aim.spread", 1f) < 0.5f)
		{
			return Vector3.zero;
		}
		Vector3 vector = Flat(TargetPoint() - from);
		return ((vector.sqrMagnitude > 0.0001f) ? Vector3.Cross(Vector3.up, vector.normalized) : Vector3.right) * aimSpot.x + Vector3.up * aimSpot.y;
	}

	private void Fire(bool want)
	{
		if (!want)
		{
			Pull(on: false);
			return;
		}
		bool flag = gun.fireMode == Gun.FireMode.AUTOMATIC;
		if (trigger > 0.5f)
		{
			if ((!flag && Time.time - triggerSince > tapLength) || (flag && (burstRoundsLeft <= 0 || Time.time > burstUntil)))
			{
				Pull(on: false);
				nextPullAt = Time.time + (flag ? NextPause() : PacedGap());
			}
		}
		else if (Time.time >= nextPullAt)
		{
			NewAimSpot();
			Pull(on: true);
			tapLength = UnityEngine.Random.Range(0.06f, 0.11f);
			burstUntil = Time.time + (flag ? Tuning.F("fire.burst.most", 1.2f) : 0f);
			burstRoundsLeft = ((!flag) ? 1 : Mathf.Clamp(Mathf.RoundToInt((float)NextBurstRounds() * Skill[3] * ((shotRange > 18f) ? 0.6f : 1f)), 1, 12));
		}
		bool flag2 = (flag ? (Time.time - firedAt > 0.1f) : (firedAt < triggerSince));
		if (!(trigger > 0.5f && !triggerWorks && Time.time - triggerSince > 0.05f && flag2) || Empty())
		{
			return;
		}
		try
		{
			firedByHandAt = Time.time;
			if (!gun.isCharged)
			{
				gun.Charge();
			}
			gun.Fire();
		}
		catch (System.Exception)
		{
		}
	}

	private float PacedGap()
	{
		float num = 0.16f + Mathf.Clamp(shotRange, 0f, 25f) * 0.018f;
		try
		{
			int num2 = ((gun.defaultMagazine != null) ? gun.defaultMagazine.rounds : 15);
			if (num2 <= 10)
			{
				num *= 1.25f;
			}
			else if (num2 <= 17)
			{
				num *= 1.15f;
			}
		}
		catch (System.Exception)
		{
		}
		num *= Skill[2] * UnityEngine.Random.Range(0.85f, 1.25f);
		if (enemy && Furious)
		{
			num *= Tuning.F("fury.gap", 0.7f);
		}
		return Mathf.Max(NextGap(), num);
	}

	private static float NextGap()
	{
		float num = Knowledge.Style.Sample(Knowledge.Style.gaps, -1f);
		if (num > 0f)
		{
			return Mathf.Max(0.1f, num);
		}
		float value = UnityEngine.Random.value;
		if (!(value < 0.22f))
		{
			if (!(value < 0.85f))
			{
				return UnityEngine.Random.Range(0.9f, 1.6f);
			}
			return UnityEngine.Random.Range(0.32f, 0.7f);
		}
		return UnityEngine.Random.Range(0.14f, 0.22f);
	}

	private int NextBurstRounds()
	{
		float num = Knowledge.Style.Sample(Knowledge.Style.burstRounds, -1f);
		if (num > 0f)
		{
			return Mathf.Clamp(Mathf.RoundToInt(num * personality.burst), 1, 12);
		}
		Transform head = Head;
		bool flag = target.transform != null && head != null && (TargetPoint() - head.position).magnitude < 5f;
		float value = UnityEngine.Random.value;
		if (!(value < 0.3f))
		{
			if (!(value < 0.65f))
			{
				if (!(value < (flag ? 0.8f : 0.9f)))
				{
					return UnityEngine.Random.Range(4, 7);
				}
				return 3;
			}
			return 2;
		}
		return 1;
	}

	private float NextBurst()
	{
		float num = Knowledge.Style.Sample(Knowledge.Style.bursts, -1f);
		if (num > 0f)
		{
			return Mathf.Clamp(num, 0.05f, 1.5f);
		}
		Transform head = Head;
		bool flag = target.transform != null && head != null && (TargetPoint() - head.position).magnitude < 5f;
		float value = UnityEngine.Random.value;
		if (!(value < 0.15f))
		{
			if (!(value < (flag ? 0.6f : 0.85f)))
			{
				return UnityEngine.Random.Range(0.45f, 0.9f);
			}
			return UnityEngine.Random.Range(0.12f, 0.35f);
		}
		return 0.05f;
	}

	private static float NextPause()
	{
		float num = Knowledge.Style.Sample(Knowledge.Style.pauses, -1f);
		if (num > 0f)
		{
			return Mathf.Clamp(num, 0.12f, 2.5f);
		}
		if (!(UnityEngine.Random.value < 0.15f))
		{
			return UnityEngine.Random.Range(0.3f, 0.9f);
		}
		return UnityEngine.Random.Range(1f, 1.8f);
	}

	private void Pull(bool on)
	{
		if (on && trigger <= 0.5f)
		{
			triggerSince = Time.time;
		}
		if (!on && trigger > 0.5f && gun != null)
		{
			pullToJudge = triggerSince;
			judgeAt = Time.time + 0.35f;
		}
		if (pullToJudge > 0f && Time.time >= judgeAt)
		{
			if (firedAt >= pullToJudge)
			{
				dryPulls = 0;
			}
			else
			{
				dryPulls++;
			}
			pullToJudge = -1f;
		}
		trigger = (on ? 1f : 0f);
	}

	private Quaternion GunTurnAlong(Vector3 dir)
	{
		Transform firePointTransform = gun.firePointTransform;
		Transform transform = item.transform;
		Pose pose = Pose.Of(transform).Inverse() * Pose.Of(firePointTransform);
		Vector3 vector = pose.rotation * Vector3.forward;
		Transform transform2 = ((gun.ammoSocket != null) ? gun.ammoSocket.transform : gun.triggerGrip.transform);
		Vector3 vector2 = Quaternion.Inverse(transform.rotation) * (transform2.position - transform.position);
		Vector3 vector3 = Vector3.ProjectOnPlane(pose.position - vector2, vector);
		if (vector3.sqrMagnitude < 1E-06f)
		{
			vector3 = Vector3.ProjectOnPlane(pose.rotation * Vector3.up, vector);
		}
		Quaternion rotation = Quaternion.LookRotation(vector, vector3.normalized);
		return Quaternion.LookRotation(dir.normalized, Vector3.up) * Quaternion.Euler(wobble.y, wobble.x, 0f) * Quaternion.Euler(0f, 0f, Tuning.F(LongGun ? "hold.rifle.roll" : "hold.pistol.roll", 0f)) * Quaternion.Inverse(rotation);
	}

	private bool AimLocked(Vector3 point, Transform head)
	{
		Transform firePointTransform = gun.firePointTransform;
		if (firePointTransform == null || gun.triggerGrip == null || head == null)
		{
			return false;
		}
		Transform vrRoot = controls.vrRoot;
		Quaternion rotation = Quaternion.Euler(0f, bodyYaw, 0f);
		bool longGun = LongGun;
		float num = standingHeight;
		Vector3 position = (longGun ? Tuning.V("aim.rifle", new Vector3(0.12f, -0.33f, 0.3f)) : Tuning.V("aim.pistol", new Vector3(0.06f, -0.32f, 0.36f)));
		position.y += num;
		Vector3 vector = vrRoot.TransformPoint(position);
		if (rig.activeSeat != null)
		{
			Vector3 vector2 = Flat(point - head.position);
			vector2 = ((vector2.sqrMagnitude > 0.01f) ? vector2.normalized : (Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward));
			vector = head.position + vector2 * Tuning.F("ride.reach", 0.45f) - Vector3.up * Tuning.F("ride.hand.down", 0.25f);
		}
		Pose pose = Pose.Of(item.transform);
		if (aimLockGun != gun.Pointer)
		{
			aimLockGun = gun.Pointer;
			aimLockFix = Quaternion.identity;
		}
		Vector3 vector3 = Quaternion.Inverse(pose.rotation) * (gun.triggerGrip.transform.position - pose.position);
		Vector3 vector4 = Quaternion.Inverse(pose.rotation) * (firePointTransform.position - pose.position);
		Quaternion quaternion = GunTurnAlong(point - firePointTransform.position);
		for (int i = 0; i < 2; i++)
		{
			Vector3 vector5 = vector - quaternion * vector3;
			quaternion = GunTurnAlong(point - (vector5 + quaternion * vector4));
		}
		Vector3 vector6 = point - firePointTransform.position;
		if (vector6.sqrMagnitude > 0.01f && Time.time - firedAt > 0.35f)
		{
			Quaternion b = Quaternion.FromToRotation(firePointTransform.forward, vector6.normalized);
			aimLockFix = Quaternion.Slerp(Quaternion.identity, b, Mathf.Clamp01(Time.deltaTime * Tuning.F("aim.fix.rate", 2.5f))) * aimLockFix;
			if (Quaternion.Angle(Quaternion.identity, aimLockFix) > Tuning.F("aim.fix.most", 12f))
			{
				aimLockFix = Quaternion.Slerp(Quaternion.identity, aimLockFix, 0.5f);
			}
		}
		quaternion = aimLockFix * quaternion;
		Pose pose2 = new Pose(vector - quaternion * vector3, quaternion);
		if (Time.time - aimedSince > 0.3f)
		{
			aimedWantSet = false;
		}
		aimedSince = Time.time;
		float t = Mathf.Clamp01(Time.deltaTime * Tuning.F("aim.ease", 10f));
		aimedWant = (aimedWantSet ? new Pose(Vector3.Lerp(aimedWant.position, pose2.position, t), Quaternion.Slerp(aimedWant.rotation, pose2.rotation, t)) : pose2);
		if (!aimedWantSet)
		{
			aimedWant = new Pose(Vector3.Lerp(pose.position, pose2.position, 0.15f), Quaternion.Slerp(pose.rotation, pose2.rotation, 0.15f));
			aimedWantSet = true;
		}
		Pose pose3 = aimedWant;
		float num2 = Time.time - firedAt;
		if (num2 >= 0f && num2 < 0.4f)
		{
			float num3 = Mathf.Exp((0f - num2) / Tuning.F("gun.kick.time", 0.07f)) * (longGun ? 0.6f : 1f);
			pose3 = new Pose(pose3.position - pose3.rotation * Vector3.forward * (0.02f * num3), pose3.rotation * Quaternion.Euler((0f - Tuning.F("gun.kick", 4f)) * num3, 0f, 0f));
		}
		Pose pose4 = pose3 * gunInController.Inverse();
		if (Time.time - aimLockSaidAt > 2f)
		{
			aimLockSaidAt = Time.time;
			Vector3 from = pose3.rotation * (Quaternion.Inverse(pose.rotation) * firePointTransform.forward);
			Lab.Event("aimlock: gun off its place " + Quaternion.Angle(pose.rotation, pose3.rotation).ToString("0.0") + " deg, " + (pose.position - pose3.position).magnitude.ToString("0.00") + " m; planned barrel off the point " + Vector3.Angle(from, point - (pose3.position + pose3.rotation * vector4)).ToString("0.0") + " deg; muzzle off the point " + Vector3.Angle(firePointTransform.forward, point - firePointTransform.position).ToString("0.0") + " deg; controller off its want " + Quaternion.Angle(controls.rightController.transform.rotation, pose4.rotation).ToString("0.0") + " deg; made up " + Quaternion.Angle(Quaternion.identity, aimLockFix).ToString("0.0") + " deg; steady " + GunSteady());
		}
		if (!(Quaternion.Angle(pose.rotation, pose3.rotation) > Tuning.F("gun.lock.retake", 25f)) || !(Quaternion.Angle(controls.rightController.transform.rotation, pose4.rotation) < 10f))
		{
			heldWrongSince = -1f;
		}
		else if (heldWrongSince < 0f)
		{
			heldWrongSince = Time.time;
		}
		else if (Time.time - heldWrongSince > 1.5f)
		{
			Lab.Event("gun: held " + Quaternion.Angle(pose.rotation, pose3.rotation).ToString("0") + " deg from its place for 1.5 s: how it sits in the hand taken again");
			heldWrongSince = -1f;
			lockSet = false;
			return false;
		}
		OneHandMeter();
		handOverride = true;
		learnedHoldAt = Time.time;
		holdAimedNow = true;
		handAt = vrRoot.InverseTransformPoint(pose4.position);
		handTurn = Quaternion.Inverse(rotation) * pose4.rotation;
		playHeight = num * 0.985f;
		if (supportHeld && step == Step.Ready && leftOnGunSet)
		{
			Pose pose5 = Pose.Of(item.transform) * leftOnGun;
			leftOverride = true;
			leftAt = vrRoot.InverseTransformPoint(pose5.position);
			leftTurn = Quaternion.Inverse(rotation) * pose5.rotation;
		}
		Vector3 vector7 = Quaternion.Inverse(rotation) * (point - head.position);
		playPitch = Mathf.Clamp((0f - Mathf.Atan2(vector7.y, Mathf.Max(new Vector2(vector7.x, vector7.z).magnitude, 0.1f))) * 57.29578f, -50f, 60f);
		playYaw = Mathf.Clamp(Mathf.Atan2(vector7.x, vector7.z) * 57.29578f, -50f, 50f);
		return true;
	}

	private void Aim(Vector3 point, Transform head, Hold hold)
	{
		Hand rightHand = RightHand;
		Transform firePointTransform = gun.firePointTransform;
		BaseController rightController = controls.rightController;
		if (rightHand == null || firePointTransform == null || rightController == null || item == null)
		{
			return;
		}
		bool flag = rig.activeSeat != null;
		if ((hold != Hold.Reload && !flag && HoldFromRecording(point, head, hold == Hold.Aimed)) || (hold != Hold.Reload && !flag && HoldLearned(point, head, hold == Hold.Aimed)) || (hold == Hold.Aimed && lockSet && Tuning.F("aim.locked", 1f) > 0.5f && AimLocked(point, head)))
		{
			return;
		}
		Transform vrRoot = controls.vrRoot;
		Quaternion rotation = Quaternion.Euler(0f, bodyYaw, 0f);
		Transform transform = item.transform;
		if (!supportHeld)
		{
			Pose pose = Pose.Of(rightController.transform).Inverse() * Pose.Of(rightHand.transform);
			if (pose.position.magnitude < Tuning.F("hold.free.near", 0.15f))
			{
				rightOnController = (handMeasured ? Pose.Lerp(rightOnController, pose, 0.08f) : pose);
				handMeasured = true;
			}
		}
		if (!supportHeld || !rightInGunSet)
		{
			rightInGun = Pose.Of(transform).Inverse() * Pose.Of(rightHand.transform);
			rightInGunSet = true;
		}
		Pose pose2 = rightInGun;
		Pose pose3 = Pose.Of(transform).Inverse() * Pose.Of(firePointTransform);
		Vector3 vector = point - firePointTransform.position;
		if (vector.sqrMagnitude < 0.0001f)
		{
			return;
		}
		Vector3 vector2 = pose3.rotation * Vector3.forward;
		Transform transform2 = ((gun.ammoSocket != null) ? gun.ammoSocket.transform : gun.triggerGrip.transform);
		Vector3 vector3 = Quaternion.Inverse(transform.rotation) * (transform2.position - transform.position);
		Vector3 vector4 = Vector3.ProjectOnPlane(pose3.position - vector3, vector2);
		if (vector4.sqrMagnitude < 1E-06f)
		{
			vector4 = Vector3.ProjectOnPlane(pose3.rotation * Vector3.up, vector2);
		}
		Quaternion rotation2 = Quaternion.LookRotation(vector2, vector4.normalized);
		labGunUp = transform.rotation * vector4.normalized;
		Quaternion quaternion = Quaternion.LookRotation(vector.normalized, Vector3.up) * Quaternion.Euler(wobble.y, wobble.x, 0f) * Quaternion.Euler(0f, 0f, Tuning.F(LongGun ? "hold.rifle.roll" : "hold.pistol.roll", 0f)) * Quaternion.Inverse(rotation2);
		labWantUp = quaternion * vector4.normalized;
		bool longGun = LongGun;
		float num = standingHeight;
		Vector3 position = hold switch
		{
			Hold.Low => longGun ? Tuning.V("hold.rifle.low", new Vector3(0.14f, -0.55f, 0.22f)) : Tuning.V("hold.pistol.low", new Vector3(0.15f, -0.6f, 0.28f)), 
			Hold.Reload => Tuning.V("hold.reload", new Vector3(0.07f, -0.42f, 0.24f)), 
			_ => longGun ? Tuning.V("aim.rifle", new Vector3(0.12f, -0.33f, 0.3f)) : Tuning.V("aim.pistol", new Vector3(0.06f, -0.32f, 0.36f)), 
		};
		position.y += num;
		Vector3 vector5 = vrRoot.TransformPoint(position);
		Pose pose4 = new Pose(vector5 - quaternion * pose2.position, quaternion);
		float k = Mathf.Clamp01(Time.deltaTime * ((step == Step.Ready) ? Tuning.F("hold.ease", 6f) : Tuning.F("hold.ease.getting", 3f)));
		if (!supportHeld || step != Step.Settle || !gunWantSet)
		{
			gunWant = (gunWantSet ? Pose.Lerp(gunWant, pose4, k) : pose4);
		}
		gunWantSet = true;
		Pose pose5 = gunWant * pose2;
		if (supportHeld)
		{
			if (GunSteady() && Time.time - firedAt > Tuning.F("hold.fix.afterShot", 0.4f) && Quaternion.Angle(gunWant.rotation, transform.rotation) < Tuning.F("hold.fix.within", 35f))
			{
				Correct(ref rightFix, gunWant, transform);
			}
		}
		else
		{
			rightFix = Pose.identity;
		}
		Pose pose6 = ((supportHeld && freeRightSet) ? freeRight : rightOnController);
		Pose pose7 = new Pose(pose5.position + rightFix.position, rightFix.rotation * pose5.rotation) * pose6.Inverse();
		handOverride = true;
		handAt = vrRoot.InverseTransformPoint(pose7.position);
		handTurn = Quaternion.Inverse(rotation) * pose7.rotation;
		playHeight = num * 0.985f;
		Vector3 vector6 = Quaternion.Inverse(rotation) * (point - head.position);
		if (hold == Hold.Reload)
		{
			playPitch = 28f;
			playYaw = 6f;
		}
		else
		{
			playPitch = Mathf.Clamp((0f - Mathf.Atan2(vector6.y, Mathf.Max(new Vector2(vector6.x, vector6.z).magnitude, 0.1f))) * 57.29578f, -50f, 60f);
			playYaw = Mathf.Clamp(Mathf.Atan2(vector6.x, vector6.z) * 57.29578f, -50f, 50f);
		}
	}

	private void MeasureFreeHands()
	{
		Hand rightHand = RightHand;
		BaseController rightController = controls.rightController;
		MeasureFreeLeft();
		if (!(rightHand == null) && !(rightController == null) && !(rightHand.m_CurrentAttachedGO != null))
		{
			Pose pose = Pose.Of(rightController.transform).Inverse() * Pose.Of(rightHand.transform);
			freeRight = (freeRightSet ? Pose.Lerp(freeRight, pose, 0.05f) : pose);
			freeRightSet = true;
		}
	}

	private void MeasureFreeLeft()
	{
		Hand leftHand = LeftHand;
		BaseController leftController = controls.leftController;
		if (!(leftHand == null) && !(leftController == null) && !(leftHand.m_CurrentAttachedGO != null))
		{
			Pose pose = Pose.Of(leftController.transform).Inverse() * Pose.Of(leftHand.transform);
			freeLeft = (freeLeftSet ? Pose.Lerp(freeLeft, pose, 0.05f) : pose);
			freeLeftSet = true;
		}
	}

	private static void Correct(ref Pose fix, Pose want, Transform hand)
	{
		float num = Mathf.Clamp01(Time.deltaTime * Tuning.F("hold.fix.rate", 1.5f));
		float maxLength = Tuning.F("hold.fix.most", 0.5f);
		fix.position = Vector3.ClampMagnitude(fix.position + (want.position - hand.position) * num, maxLength);
		Quaternion b = want.rotation * Quaternion.Inverse(hand.rotation);
		fix.rotation = Quaternion.Slerp(Quaternion.identity, b, num) * fix.rotation;
		float num2 = Quaternion.Angle(Quaternion.identity, fix.rotation);
		float num3 = Tuning.F("hold.fix.degrees", 120f);
		if (num2 > num3)
		{
			fix.rotation = Quaternion.Slerp(Quaternion.identity, fix.rotation, num3 / num2);
		}
	}

	private void NextStep(Step next)
	{
		Lab.Event("hold: " + step.ToString() + " -> " + next.ToString() + " after " + (Time.time - stepAt).ToString("0.00") + " s");
		step = next;
		stepAt = Time.time;
		reachFix = Vector3.zero;
		if (next == Step.Reach && controls.leftController != null)
		{
			leftStart = Pose.Of(controls.leftController.transform);
		}
	}

	private Pose FrontHandTarget(Hand left, out string how)
	{
		if (GripBook.HandOn(support, leftHand: true, out var hand, out var mirrored))
		{
			how = (mirrored ? "the mirror of your hand" : "your hand on it");
			return hand;
		}
		Pose result = HandOnGrip(support, left);
		how = "the grip gives it";
		if (!lockSet || !leftMeasured || item == null || Tuning.F("front.yours", 1f) < 0.5f)
		{
			return result;
		}
		GunMoves.Hold hold = GunMoves.Held(GunKey, twoHanded: true, aimed: true) ?? GunMoves.Held(GunKey, twoHanded: true, aimed: false);
		if (hold == null || hold.left == null || hold.right == null)
		{
			return result;
		}
		Pose pose = GunMoves.U(hold.right, Pose.identity);
		Pose pose2 = GunMoves.U(hold.left, Pose.identity);
		Pose pose3 = gunInController.Inverse() * (pose.Inverse() * pose2) * leftOnController;
		Pose pose4 = Pose.Of(item.transform) * pose3;
		how = "you hold it";
		return new Pose(((pose4.position - result.position).magnitude < Tuning.F("front.yours.near", 0.12f)) ? pose4.position : result.position, pose4.rotation);
	}

	private Vector3 GunUp()
	{
		try
		{
			Transform firePointTransform = gun.firePointTransform;
			Transform transform = ((gun.ammoSocket != null) ? gun.ammoSocket.transform : gun.triggerGrip.transform);
			Vector3 vector = Vector3.ProjectOnPlane(firePointTransform.position - transform.position, firePointTransform.forward);
			if (vector.sqrMagnitude > 1E-06f)
			{
				return vector.normalized;
			}
		}
		catch (System.Exception)
		{
		}
		return Vector3.up;
	}

	private static Pose HandOnGrip(Grip g, Hand hand)
	{
		try
		{
			SimpleTransform simpleTransform = g.SolveHandTarget(hand);
			if (simpleTransform.rotation.x != 0f || simpleTransform.rotation.y != 0f || simpleTransform.rotation.z != 0f || simpleTransform.rotation.w != 0f)
			{
				return new Pose(simpleTransform.position, simpleTransform.rotation);
			}
		}
		catch (System.Exception)
		{
		}
		return Pose.Of(g.transform);
	}

	private bool GunSteady()
	{
		if (!(item == null))
		{
			if (item.angularVelocity.magnitude < Tuning.F("hold.steady.spin", 1.5f))
			{
				return item.velocity.magnitude < Tuning.F("hold.steady.speed", 0.8f);
			}
			return false;
		}
		return true;
	}

	private void HoldSupport()
	{
		GunMoves.Hold hold = ((support != null) ? LearnedHold(aimed: true) : null);
		if (hold != null && hold.left != null && Tuning.F("learned.hold.aim", 0f) > 0.5f)
		{
			HoldSupportLearned();
			return;
		}
		Hand hand = ((rig.physicsRig != null) ? rig.physicsRig.leftHand : null);
		if (support == null && frontRetry != null && item != null && Time.time >= frontRetryAt)
		{
			Rigidbody rigidbody = Pelvis();
			bool flag = rigidbody == null || Flat(rigidbody.velocity).magnitude < Tuning.F("front.retry.speed", 1.2f);
			if (frontRetry.GetComponentInParent<Gun>() != gun)
			{
				frontRetry = null;
			}
			else if (flag)
			{
				support = frontRetry;
				frontRetry = null;
				Lab.Event("front hand: trying " + support.name + " again");
				NextStep(Step.Reach);
			}
			else
			{
				frontRetryAt = Time.time + 0.5f;
			}
		}
		float num = Time.time - stepAt;
		if (support == null || hand == null || item == null)
		{
			supportHeld = false;
			if (step != Step.Ready && num > 0.7f && (GunSteady() || num > Tuning.F("hold.onehand.most", 1.4f)))
			{
				NextStep(Step.Ready);
			}
			return;
		}
		supportHeld = hand.m_CurrentAttachedGO != null && support.attachedHands != null && support.attachedHands.Contains(hand);
		Transform vrRoot = controls.vrRoot;
		Transform transform = item.transform;
		BaseController leftController = controls.leftController;
		Quaternion rotation = Quaternion.Euler(0f, bodyYaw, 0f);
		if (!supportHeld && leftController != null)
		{
			Pose pose = Pose.Of(leftController.transform).Inverse() * Pose.Of(hand.transform);
			if (pose.position.magnitude < Tuning.F("hold.free.near", 0.15f) && (hand.rb == null || hand.rb.velocity.magnitude < Tuning.F("hold.free.calm", 0.5f) + ((Pelvis() != null) ? Pelvis().velocity.magnitude : 0f)))
			{
				leftOnController = (leftMeasured ? Pose.Lerp(leftOnController, pose, 0.08f) : pose);
				leftMeasured = true;
			}
			else if (leftMeasured && leftOnController.position.magnitude > Tuning.F("hold.free.near", 0.15f))
			{
				leftOnController = Pose.identity;
			}
		}
		switch (step)
		{
		case Step.OneHand:
			leftOverride = false;
			if (num > Tuning.F("hold.onehand.time", 0.7f) && (GunSteady() || num > Tuning.F("hold.onehand.most", 1.4f)))
			{
				NextStep(Step.Reach);
			}
			return;
		case Step.Reach:
		{
			if (supportHeld)
			{
				Lab.Event("front hand on " + support.name + ": taken by the game (hand " + (frontGap * 100f).ToString("0.0") + " cm, " + frontOff.ToString("0") + " deg off as it arrived)");
				twoHandSince = Time.time;
				leftInGunSet = false;
				leftOnHeldSet = false;
				frontForced = false;
				frontJolt = 0f;
				frontJoltSaid = false;
				frontArrivedAt = -1f;
				frontLetUntil = -10f;
				NextStep(Step.Settle);
				break;
			}
			if (hand.m_CurrentAttachedGO != null)
			{
				Lab.Event("front hand: the game took hold of " + hand.m_CurrentAttachedGO.name + ", not " + support.name + ": let go");
				FreeHand(hand);
				frontLetUntil = -10f;
				frontArrivedAt = -1f;
			}
			leftOverride = true;
			float num2 = Mathf.SmoothStep(0f, 1f, num / Tuning.F("hold.reach.time", 0.7f));
			string how;
			Pose pose2 = FrontHandTarget(hand, out how);
			Pose b = pose2 * leftOnController.Inverse();
			Vector3 vector = GunUp() * (0f - Tuning.F("front.under", 0.1f));
			Pose pose3 = new Pose(b.position + vector, b.rotation);
			Pose pose4 = ((num2 < 0.65f) ? Pose.Lerp(leftStart, pose3, Mathf.SmoothStep(0f, 1f, num2 / 0.65f)) : Pose.Lerp(pose3, b, Mathf.SmoothStep(0f, 1f, (num2 - 0.65f) / 0.35f)));
			leftAt = vrRoot.InverseTransformPoint(pose4.position);
			leftTurn = Quaternion.Inverse(rotation) * pose4.rotation;
			float magnitude = (hand.transform.position - pose2.position).magnitude;
			frontClosing = Mathf.Clamp01(1f - (magnitude - 0.03f) / 0.15f);
			frontClosingAt = Time.time;
			float num3 = Quaternion.Angle(hand.transform.rotation, pose2.rotation);
			if (item != null)
			{
				frontBefore = Mathf.Max(frontBefore * 0.97f, item.angularVelocity.magnitude * 57.29578f);
			}
			bool flag2 = num > 1.6f;
			if (num2 >= 1f && magnitude < (flag2 ? Tuning.F("front.there.late", 0.04f) : Tuning.F("front.there", 0.025f)) && num3 < (flag2 ? 15f : Tuning.F("front.there.turn", 10f)))
			{
				if (frontArrivedAt < 0f)
				{
					frontArrivedAt = Time.time;
					frontGap = magnitude;
					frontOff = num3;
				}
				frontLetUntil = Time.time + 0.1f;
			}
			else if (frontArrivedAt >= 0f && magnitude > 0.05f)
			{
				frontArrivedAt = -1f;
			}
			if (frontArrivedAt >= 0f && Time.time - frontArrivedAt > Tuning.F("front.wait", 0.3f))
			{
				supportHeld = Grab(hand, support, pose2.rotation);
				Lab.Event((supportHeld ? "front hand on " : "front hand missed ") + support.name + ": FORCED, the game had not taken hold (hand " + (magnitude * 100f).ToString("0.0") + " cm, " + num3.ToString("0") + " deg off, turned as " + how + ")");
				frontArrivedAt = -1f;
				frontLetUntil = -10f;
				if (supportHeld)
				{
					twoHandSince = Time.time;
					leftInGunSet = false;
					leftOnHeldSet = false;
					frontForced = true;
					frontJolt = 0f;
					frontJoltSaid = false;
					NextStep(Step.Settle);
				}
			}
			if (!supportHeld && num > 2.5f)
			{
				BaseController leftController2 = controls.leftController;
				Vector3 vector2 = vrRoot.TransformPoint(leftAt);
				Transform transform2 = ((rig.physicsRig != null && rig.physicsRig.m_shoulderLf != null) ? rig.physicsRig.m_shoulderLf : null);
				Lab.Event("front hand gave up on " + support.name + " (gap " + magnitude.ToString("0.00") + " m), for now: its controller " + ((leftController2 != null) ? (leftController2.transform.position - vector2).magnitude.ToString("0.00") : "?") + " m from where sent, the hand " + ((leftController2 != null) ? (hand.transform.position - leftController2.transform.position).magnitude.ToString("0.00") : "?") + " m from its controller, the grip " + ((transform2 != null) ? (pose2.position - transform2.position).magnitude.ToString("0.00") : "?") + " m from its shoulder, the gun " + (gunWantSet ? Quaternion.Angle(gunWant.rotation, transform.rotation).ToString("0") : "?") + " deg off its want; the hand holds " + ((hand.m_CurrentAttachedGO != null) ? hand.m_CurrentAttachedGO.name : "nothing") + ", at " + hand.transform.position.ToString("0.0") + " moving " + ((hand.rb != null) ? hand.rb.velocity.magnitude.ToString("0.0") : "?") + " m/s, its controller at " + ((leftController2 != null) ? leftController2.transform.position.ToString("0.0") : "?") + ", the bot at " + Feet.ToString("0.0"));
				frontRetry = support;
				frontRetryAt = Time.time + Tuning.F("front.retry", 3f);
				support = null;
				leftOverride = false;
				NextStep(Step.Ready);
			}
			return;
		}
		}
		if (!supportHeld)
		{
			NextStep(Step.Reach);
			return;
		}
		if (!frontJoltSaid && item != null)
		{
			if (!frontCounted)
			{
				frontCounted = true;
				frontHolds++;
				frontKept = false;
			}
			float num4 = item.angularVelocity.magnitude * 57.29578f;
			if (num4 > frontJolt)
			{
				frontJolt = num4;
				BaseController leftController3 = controls.leftController;
				BaseController rightController = controls.rightController;
				frontLeftOff = ((leftController3 != null) ? (hand.transform.position - (Pose.Of(leftController3.transform) * (leftOnHeldSet ? leftOnHeld : leftOnController)).position).magnitude : (-1f));
				Hand rightHand = RightHand;
				frontRightOff = ((rightController != null && rightHand != null) ? (rightHand.transform.position - rightController.transform.position).magnitude : (-1f));
			}
			if (Time.time - twoHandSince > 0.6f)
			{
				frontJoltSaid = true;
				frontCounted = false;
				Lab.Event("front hand HOLD #" + frontHolds + " (" + (frontKept ? "hand KEPT as it took hold" : "hand measured again for 0.6 s") + ", " + (frontForced ? "forced" : "by the game") + "): gun turned at most " + frontJolt.ToString("0") + " deg/s after, " + frontBefore.ToString("0") + " before; at the worst, left hand " + (frontLeftOff * 100f).ToString("0") + " cm from where sent, right hand " + (frontRightOff * 100f).ToString("0") + " cm from its controller");
				frontBefore = 0f;
			}
		}
		float num5 = Time.time - twoHandSince;
		float num6 = Tuning.F("hold.settle.time", 0.45f);
		Pose pose5 = ((leftController != null) ? (Pose.Of(leftController.transform).Inverse() * Pose.Of(hand.transform)) : Pose.identity);
		Rigidbody rigidbody2 = Pelvis();
		if (!(item.angularVelocity.magnitude * 57.29578f < Tuning.F("hold.calm.spin", 20f)) || !((item.velocity - ((rigidbody2 != null) ? rigidbody2.velocity : Vector3.zero)).magnitude < Tuning.F("hold.calm.speed", 0.12f)))
		{
			settleCalmSince = -1f;
		}
		else if (settleCalmSince < 0f)
		{
			settleCalmSince = Time.time;
		}
		bool flag3 = num5 >= num6 && ((settleCalmSince >= 0f && Time.time - settleCalmSince >= 0.2f) || num5 >= Tuning.F("hold.settle.most", 1.5f));
		if (step == Step.Settle && !flag3)
		{
			leftOnHeld = ((num5 < 0.06f || !leftOnHeldSet) ? pose5 : Pose.Lerp(leftOnHeld, pose5, 0.25f));
			leftOnHeldSet = true;
			leftOverride = true;
			if (Time.time - twoHandSince < 0.6f || !leftInGunSet)
			{
				leftInGun = Pose.Of(transform).Inverse() * Pose.Of(hand.transform);
			}
			return;
		}
		if (step == Step.Settle)
		{
			leftInGun = Pose.Of(transform).Inverse() * Pose.Of(hand.transform);
			leftInGunSet = true;
			leftOnHeld = pose5;
			leftOnHeldSet = true;
			Lab.Event("front hand settled on " + support.name + " after " + num5.ToString("0.00") + " s (" + ((settleCalmSince >= 0f) ? "calm" : "NOT calm") + "), its place on the gun taken");
			NextStep(Step.Ready);
		}
		leftOverride = true;
		SteadyMeter();
		if (frontKept && !leftInGunSet)
		{
			leftInGun = Pose.Of(transform).Inverse() * Pose.Of(hand.transform);
			leftInGunSet = true;
		}
		else if (!frontKept && !leftInGunSet)
		{
			leftInGun = Pose.Of(transform).Inverse() * Pose.Of(hand.transform);
			leftInGunSet = true;
		}
		Pose pose6 = Pose.Of(transform) * leftInGun;
		Pose obj = (gunWantSet ? Pose.Lerp(pose6, gunWant * leftInGun, Tuning.F("hold.left.want", 1f)) : pose6);
		leftFix = Pose.identity;
		Pose pose7 = obj * (leftOnHeldSet ? leftOnHeld : leftOnController).Inverse();
		leftAt = vrRoot.InverseTransformPoint(pose7.position);
		leftTurn = Quaternion.Inverse(rotation) * pose7.rotation;
	}

	private void SteadyMeter()
	{
		if (!(item == null) && !(Time.time - twoHandSince < 1f) && !(Time.time - firedAt < 0.5f))
		{
			if (steadyFrom < 0f)
			{
				steadyFrom = Time.time;
			}
			float num = item.angularVelocity.magnitude * 57.29578f;
			steadyMost = Mathf.Max(steadyMost, num);
			steadySum += num;
			steadyN++;
			if (Time.time - steadyFrom >= 5f)
			{
				Lab.Event("two-hand steady: gun turning at most " + steadyMost.ToString("0") + " deg/s, mean " + (steadySum / (float)Mathf.Max(1, steadyN)).ToString("0") + " deg/s over 5 s (" + ((support != null) ? support.name : "?") + ")");
				steadyFrom = -1f;
				steadyMost = (steadySum = 0f);
				steadyN = 0;
			}
		}
	}

	private void OneHandMeter()
	{
		BaseController rightController = controls.rightController;
		if (item == null || rightController == null || support != null)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		Hand rightHand = RightHand;
		Rigidbody rigidbody = Pelvis();
		if (rightHand == null || rigidbody == null)
		{
			return;
		}
		Quaternion a = Quaternion.Inverse(rightHand.transform.rotation) * item.rotation;
		Quaternion a2 = Quaternion.Inverse(rigidbody.rotation) * item.rotation;
		if (meterSet && deltaTime > 0.0001f)
		{
			float num = Quaternion.Angle(a, meterRel) / deltaTime;
			if (Time.time - firedAt < 0.5f)
			{
				meterShotSum += num;
				meterShotN++;
			}
			else
			{
				meterRelSum += num;
				meterRelMost = Mathf.Max(meterRelMost, num);
				meterCtlSum += Quaternion.Angle(a2, meterCtl) / deltaTime;
				meterN++;
			}
		}
		meterRel = a;
		meterCtl = a2;
		meterSet = true;
		if (meterFrom < 0f)
		{
			meterFrom = Time.time;
		}
		if (!(Time.time - meterFrom >= 5f))
		{
			return;
		}
		float num2 = 0f;
		foreach (Rigidbody componentsInChild in GunWeight.RootOf(gun).GetComponentsInChildren<Rigidbody>())
		{
			num2 += ((componentsInChild != null && !componentsInChild.isKinematic) ? componentsInChild.mass : 0f);
		}
		Lab.Event("one-hand steady (" + (LongGun ? "long" : "pistol") + ", " + num2.ToString("0.00") + " kg): gun turning in its grip mean " + (meterRelSum / (float)Mathf.Max(1, meterN)).ToString("0") + " deg/s, most " + meterRelMost.ToString("0") + "; gun turning against its hips mean " + (meterCtlSum / (float)Mathf.Max(1, meterN)).ToString("0") + " deg/s; after shots in hand " + (meterShotSum / (float)Mathf.Max(1, meterShotN)).ToString("0") + " deg/s (" + meterShotN + " frames)");
		meterFrom = -1f;
		meterRelSum = (meterRelMost = (meterCtlSum = (meterShotSum = 0f)));
		meterN = (meterShotN = 0);
	}

	private static string GripKey(Gun g, Grip grip)
	{
		string text = g.name;
		int num = text.IndexOf(" [", System.StringComparison.Ordinal);
		if (num > 0)
		{
			text = text.Substring(0, num);
		}
		return text.Replace("(Clone)", "").Trim() + "/" + grip.name;
	}

	private static bool WorksBolt(Gun g, Grip grip)
	{
		if (g != null && grip != null)
		{
			return boltGrips.Contains(GripKey(g, grip));
		}
		return false;
	}

	private void WatchBolt()
	{
		bool flag = false;
		try
		{
			flag = gun != null && support != null && supportHeld && !Reloading && (gun.slideState == Gun.SlideStates.PULLING || gun.slideState == Gun.SlideStates.PULLED);
		}
		catch (System.Exception)
		{
		}
		if (!flag)
		{
			boltOffSince = -1f;
			return;
		}
		if (boltOffSince < 0f)
		{
			boltOffSince = Time.time;
		}
		if (!(Time.time - boltOffSince < 0.3f))
		{
			boltOffSince = -1f;
			boltGrips.Add(GripKey(gun, support));
			Lab.Event("front hand: the " + support.name + " works the bolt of the " + gun.name + " (" + gun.slideState.ToString() + "): let go, another grip");
			LetGoOfSupport();
			support = SupportGrip(gun);
			step = ((support != null) ? Step.Reach : Step.Ready);
			stepAt = Time.time;
			if (controls.leftController != null)
			{
				leftStart = Pose.Of(controls.leftController.transform);
			}
		}
	}

	private void LetGoOfSupport()
	{
		Hand hand = ((rig.physicsRig != null) ? rig.physicsRig.leftHand : null);
		try
		{
			if (hand != null && support != null && support.attachedHands != null && support.attachedHands.Contains(hand))
			{
				support.ForceDetach(hand);
			}
		}
		catch (System.Exception)
		{
		}
		supportHeld = false;
		leftInGunSet = false;
		leftOverride = false;
	}

	private bool FindSomethingToPlayWith()
	{
		Vector3 vector = Feet;
		Rigidbody rigidbody = null;
		Grip grip = null;
		float num = float.MaxValue;
		HashSet<System.IntPtr> hashSet = new HashSet<System.IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(vector + Vector3.up * 0.5f, 10f, -1, QueryTriggerInteraction.Ignore))
		{
			Rigidbody rigidbody2 = ((item != null) ? item.attachedRigidbody : null);
			if (rigidbody2 == null || rigidbody2.isKinematic || !hashSet.Add(rigidbody2.Pointer) || rigidbody2.mass < 0.05f || rigidbody2.mass > 15f || rigidbody2.GetComponentInParent<RigManager>() != null)
			{
				continue;
			}
			Grip grip2 = GripBook.Holding(GripBook.ThingOf(GripOf(rigidbody2))) ?? GripOf(rigidbody2);
			if (grip2 == null || NotForBots(rigidbody2) || rigidbody2.GetComponentInParent<Poolee>() == null)
			{
				continue;
			}
			float num2 = rigidbody2.worldCenterOfMass.y - vector.y;
			if (!(num2 < -0.5f) && !(num2 > 1.6f))
			{
				float num3 = (rigidbody2.worldCenterOfMass - vector).magnitude + UnityEngine.Random.value * 3f;
				if (num3 < num)
				{
					num = num3;
					rigidbody = rigidbody2;
					grip = grip2;
				}
			}
		}
		if (rigidbody == null)
		{
			return false;
		}
		this.item = rigidbody;
		this.grip = grip;
		this.gun = null;
		Gun gun = rigidbody.GetComponentInParent<Gun>();
		if (gun == null)
		{
			MarrowEntity componentInParent = rigidbody.GetComponentInParent<MarrowEntity>();
			gun = ((componentInParent != null) ? componentInParent.GetComponentInChildren<Gun>() : null);
		}
		if (Usable(gun))
		{
			Take(gun);
		}
		play = Play.GoTo;
		playAt = Time.time;
		nav.SetGoal(this.item.worldCenterOfMass);
		return true;
	}

	private static Grip GripOf(Rigidbody rb)
	{
		MarrowEntity componentInParent = rb.GetComponentInParent<MarrowEntity>();
		Component obj = ((componentInParent != null) ? ((Component)componentInParent) : ((Component)rb));
		Grip result = null;
		float num = float.MaxValue;
		foreach (Grip componentsInChild in obj.GetComponentsInChildren<Grip>(includeInactive: false))
		{
			if (!(componentsInChild == null) && componentsInChild.enabled && !componentsInChild.IsInteractionDisabled && !componentsInChild.HasAttachedHands() && !(componentsInChild.TryCast<WorldGrip>() != null))
			{
				float sqrMagnitude = (componentsInChild.transform.position - rb.worldCenterOfMass).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = componentsInChild;
				}
			}
		}
		return result;
	}

	private bool PlayStep(Transform you, Transform head, float dt)
	{
		handOverride = false;
		leftOverride = false;
		playYaw = 0f;
		if (play == Play.Shove)
		{
			return ShoveStep(head, dt);
		}
		if (play == Play.Catch)
		{
			return CatchStep(head, dt);
		}
		if (item == null || grip == null)
		{
			return EndPlay();
		}
		float num = Time.time - playAt;
		switch (play)
		{
		case Play.GoTo:
			if (grip.HasAttachedHands() || num > 20f)
			{
				Lab.Event("hands: gave up going to the " + item.name + (grip.HasAttachedHands() ? " (someone has it)" : (" (not there in 20 s, " + Flat(item.worldCenterOfMass - Feet).magnitude.ToString("0.0") + " m off)")));
				return EndPlay();
			}
			nav.SetGoal(item.worldCenterOfMass);
			if (Travel(MotionLibrary.WalkPush, 0.7f, dt) || nav.Remaining(Feet) < 0.7f)
			{
				Next(Play.Reach);
			}
			else if (nav.StuckCount >= 3)
			{
				Lab.Event("hands: could not get to the " + item.name + " (stuck)");
				return EndPlay();
			}
			return true;
		case Play.Reach:
		{
			walk = Vector2.zero;
			if (pickFor != grip.Pointer || Time.time - pickChosenAt > 6f)
			{
				ChoosePickUp();
			}
			float num5 = ((pickClip != null) ? pickTakeAt : 0.35f);
			if (grip.HasAttachedHands() || num > Mathf.Max(3f, num5 + 1.8f))
			{
				Lab.Event("hands: gave up reaching for the " + item.name + (grip.HasAttachedHands() ? " (a hand is on it)" : (" (never got to it in " + num.ToString("0.0") + " s)")));
				return EndPlay();
			}
			Vector3 position = grip.transform.position;
			Face(position, dt, 240f);
			if (pickClip != null)
			{
				ReachOfTheirs(position, num);
			}
			else
			{
				Reach(position, Mathf.Clamp01(num / 0.6f));
			}
			Hand rightHand = RightHand;
			if (rightHand != null && num > num5 && ((rightHand.transform.position - position).magnitude < 0.2f || num > num5 + 0.85f))
			{
				Quaternion? turned = null;
				if (GripBook.HandOn(grip, leftHand: false, out var hand, out var _))
				{
					turned = hand.rotation;
				}
				else if (meleeWanted && item != null)
				{
					Vector3 vector2 = item.worldCenterOfMass - grip.transform.position;
					if (vector2.sqrMagnitude > 0.0004f)
					{
						turned = Quaternion.FromToRotation(rightHand.transform.rotation * Vector3.back, vector2.normalized) * rightHand.transform.rotation;
					}
				}
				if (!Grab(rightHand, grip, turned))
				{
					Lab.Event("hands: could not take the " + grip.name + " of the " + item.name + " (hand " + (rightHand.transform.position - position).magnitude.ToString("0.00") + " m from it, " + ((rightHand.m_CurrentAttachedGO != null) ? ("holding " + rightHand.m_CurrentAttachedGO.name) : "empty") + (grip.HasAttachedHands() ? ", a hand on it already" : "") + ", after " + num.ToString("0.0") + " s)");
					return EndPlay();
				}
				holdFor = UnityEngine.Random.Range(2f, 5f);
				if (bringNext)
				{
					bringNext = false;
					offerSince = -1f;
					Lab.Event("play: bringing the " + item.name + " over to you (asked)");
					Next(Play.Carry);
				}
				else if (catchNext && gun != null)
				{
					gun = null;
					Next(Play.Hold);
				}
				else if (gun != null)
				{
					StartShooting();
				}
				else if (meleeWanted)
				{
					meleeWanted = false;
					MeasureMelee();
					Next(Play.Melee);
				}
				else if (tinkering)
				{
					StartTrials();
				}
				else
				{
					Next(Play.Hold);
				}
			}
			return true;
		}
		case Play.Hold:
			walk = Vector2.zero;
			if (!Holding())
			{
				return EndPlay();
			}
			handOverride = true;
			handAt = new Vector3(0.18f, standingHeight - 0.42f + Mathf.Sin(num * 1.3f) * 0.03f, 0.36f);
			handTurn = Quaternion.Euler(20f + Mathf.Sin(num * 0.9f) * 25f, -20f, Mathf.Sin(num * 0.7f) * 40f);
			playHeight = standingHeight;
			playPitch = 32f;
			if (num > holdFor)
			{
				float num2 = ((you != null && head != null) ? Flat(you.position - head.position).magnitude : 99f);
				if (catchWith != null && catchThrowsLeft > 0 && !catchWith.dead && StartCatchThrow(head, catchWith, newGame: false))
				{
					return true;
				}
				if (catchWith != null && catchThrowsLeft <= 0)
				{
					Lab.Event("catch: over (" + catchesMade + " caught, " + catchesMissed + " missed)");
					if (catchWith.catchWith == this)
					{
						catchWith.catchWith = null;
					}
					catchWith = null;
				}
				if ((catchNext || UnityEngine.Random.value < Tuning.F("play.catch", 0.35f)) && StartCatchThrow(head, CatchPartner(head), newGame: true))
				{
					catchNext = false;
					return true;
				}
				if (catchNext)
				{
					Lab.Event("catch: nobody to play with (" + CatchWhyNot(head) + ")");
				}
				catchNext = false;
				if (bringNext && you != null)
				{
					bringNext = false;
					offerSince = -1f;
					Lab.Event("play: bringing the " + item.name + " over to you (asked)");
					Next(Play.Carry);
					return true;
				}
				if (!forceToss && you != null && num2 > 3f && num2 < Tuning.F("play.bring.range", 30f) && UnityEngine.Random.value < Tuning.F("play.bring", 0.3f))
				{
					offerSince = -1f;
					Lab.Event("play: bringing the " + item.name + " over to you");
					Next(Play.Carry);
					return true;
				}
				float value = UnityEngine.Random.value;
				then = ((forceToss && you != null) ? Then.TossToYou : ((num2 < 9f && value < 0.35f) ? Then.TossToYou : ((!(value < 0.75f)) ? Then.Drop : Then.Throw)));
				forceToss = false;
				Vector3 vector = Quaternion.Euler(0f, bodyYaw + UnityEngine.Random.Range(-70f, 70f), 0f) * Vector3.forward;
				throwFar = 0f;
				throwDir = ((then == Then.TossToYou && you != null) ? Flat(you.position - head.position).normalized : ((then == Then.Throw && head != null) ? PickThrowTarget(head) : vector));
				Next((then == Then.Drop) ? Play.Drop : Play.WindUp);
			}
			return true;
		case Play.WindUp:
		{
			walk = Vector2.zero;
			if (!Holding())
			{
				return EndPlay();
			}
			Face((head != null) ? (head.position + throwDir) : (Feet + throwDir), dt, 300f);
			handOverride = true;
			float num3 = Mathf.SmoothStep(0f, 1f, num / 0.45f);
			handAt = Vector3.Lerp(new Vector3(0.18f, standingHeight - 0.42f, 0.36f), new Vector3(0.3f, standingHeight - 0.12f, -0.22f), num3);
			handTurn = Quaternion.Euler(-30f * num3, 0f, 0f);
			playHeight = standingHeight;
			playPitch = ((then == Then.TossToYou) ? LookPitch(you, head) : (-5f));
			if (num > 0.5f && Mathf.Abs(Mathf.DeltaAngle(bodyYaw, Recorder.Yaw(throwDir))) < 20f)
			{
				released = false;
				Next(Play.Throw);
			}
			else if (num > 2f)
			{
				Next(Play.Drop);
			}
			return true;
		}
		case Play.Throw:
		{
			walk = Vector2.zero;
			handOverride = true;
			float num4 = Mathf.Clamp01(num / 0.16f);
			handAt = Vector3.Lerp(new Vector3(0.3f, standingHeight - 0.12f, -0.22f), new Vector3(0.24f, standingHeight - 0.3f, 0.62f), num4);
			handTurn = Quaternion.Euler(Mathf.Lerp(-30f, 40f, num4), 0f, 0f);
			playHeight = standingHeight;
			playPitch = ((then == Then.TossToYou) ? LookPitch(you, head) : (-5f));
			if (!released && num4 > 0.55f)
			{
				released = true;
				Let(go: true, you);
			}
			if (num > 0.45f)
			{
				return EndPlay();
			}
			return true;
		}
		case Play.Carry:
			return CarryStep(you, head, dt);
		case Play.Shoot:
			return Shoot(you, head, dt);
		case Play.Tinker:
			return TinkerStep(you, head, dt);
		case Play.Melee:
			return MeleeStep(you, head, dt);
		case Play.Drop:
		{
			walk = Vector2.zero;
			LetGoOfSupport();
			Pull(on: false);
			handOverride = true;
			float t = Mathf.SmoothStep(0f, 1f, num / 0.7f);
			handAt = Vector3.Lerp(new Vector3(0.18f, standingHeight - 0.42f, 0.36f), new Vector3(0.15f, 0.35f, 0.45f), t);
			handTurn = Quaternion.Euler(60f, 0f, 0f);
			playHeight = Mathf.Lerp(standingHeight, Mathf.Max(0.95f, standingHeight * 0.62f), t);
			playPitch = 45f;
			if (num > 0.8f)
			{
				Let(go: false, you);
				return EndPlay();
			}
			return true;
		}
		default:
			return EndPlay();
		}
	}

	private void Next(Play state)
	{
		play = state;
		playAt = Time.time;
	}

	private bool EndPlay()
	{
		meleeWanted = false;
		LetGoOfSupport();
		Pull(on: false);
		if (Reloading)
		{
			DropNewMag();
			reload = Reload.None;
			learnedReload = null;
		}
		if (Holding())
		{
			Release();
		}
		gun = null;
		support = null;
		tinkering = false;
		play = Play.None;
		item = null;
		grip = null;
		handOverride = false;
		nav.Clear();
		return false;
	}

	public void StopPlaying()
	{
		if (play != 0)
		{
			EndPlay();
		}
	}

	private void Face(Vector3 point, float dt, float speed)
	{
		Transform head = Head;
		Vector3 forward = Flat(point - ((head != null) ? head.position : Feet));
		if (forward.sqrMagnitude > 0.0001f)
		{
			bodyYaw = Mathf.MoveTowardsAngle(bodyYaw, Recorder.Yaw(forward), speed * dt);
		}
	}

	private void Reach(Vector3 target, float k)
	{
		Transform vrRoot = controls.vrRoot;
		Vector3 vector = Feet;
		float num = target.y - vector.y;
		playHeight = Mathf.Clamp(num + 0.85f, Mathf.Max(0.9f, standingHeight * 0.55f), standingHeight);
		Vector3 vector2 = vrRoot.InverseTransformPoint(target);
		Vector3 a = new Vector3(0.2f, playHeight - 0.5f, 0.25f);
		handOverride = true;
		handAt = Vector3.Lerp(a, vector2, Mathf.SmoothStep(0f, 1f, k));
		Vector3 vector3 = new Vector3(0.18f, playHeight - 0.25f, 0f);
		Vector3 vector4 = handAt - vector3;
		handTurn = ((vector4.sqrMagnitude > 0.0001f) ? Quaternion.LookRotation(vector4.normalized, Vector3.up) : Quaternion.identity);
		playPitch = Mathf.Clamp((0f - Mathf.Atan2(num - playHeight, Mathf.Max(Flat(vector2).magnitude, 0.2f))) * 57.29578f, -20f, 70f);
	}

	private void ChoosePickUp()
	{
		pickFor = grip.Pointer;
		pickChosenAt = Time.time;
		pickClip = MoveSlots.Pick(MoveSlots.Kind.PickUp, null, avatarCode);
		if (pickClip == null || pickClip.Length < 4)
		{
			pickClip = null;
			return;
		}
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = 0f;
		float num4 = 0f;
		Frame[] array = pickClip;
		for (int i = 0; i < array.Length; i++)
		{
			Frame frame = array[i];
			if (frame.leftPos.y < num)
			{
				num = frame.leftPos.y;
				num3 = frame.time;
			}
			if (frame.rightPos.y < num2)
			{
				num2 = frame.rightPos.y;
				num4 = frame.time;
			}
		}
		pickMirror = num < num2 - 0.05f;
		pickTakeAt = Mathf.Max(0.25f, pickMirror ? num3 : num4);
		pickLow = (pickMirror ? num : num2);
		Lab.Event("picks it up as you do (" + pickClip[pickClip.Length - 1].time.ToString("0.0") + " s, hand on it at " + pickTakeAt.ToString("0.0") + " s" + (pickMirror ? ", your left hand's, mirrored" : "") + ")");
	}

	private static Vector3 Across(Vector3 p)
	{
		return new Vector3(0f - p.x, p.y, p.z);
	}

	private static Quaternion Across(Quaternion q)
	{
		return new Quaternion(q.x, 0f - q.y, 0f - q.z, q.w);
	}

	private void ReachOfTheirs(Vector3 target, float since)
	{
		Transform vrRoot = controls.vrRoot;
		Frame frame = MotionLibrary.Sample(t: Mathf.Min(since, Mathf.Min(pickClip[pickClip.Length - 1].time, pickTakeAt + 0.1f)), clip: pickClip);
		Frame frame2 = pickClip[0];
		float num = ((frame2.height > 0.3f) ? (standingHeight / frame2.height) : 1f);
		Vector3 vector = (pickMirror ? Across(frame.leftPos) : frame.rightPos);
		Vector3 vector2 = (pickMirror ? Across(frame.rightPos) : frame.leftPos);
		Quaternion quaternion = (pickMirror ? Across(frame.leftRot) : frame.rightRot);
		Quaternion quaternion2 = (pickMirror ? Across(frame.rightRot) : frame.leftRot);
		float num2 = target.y - Feet.y;
		float num3 = standingHeight - 0.62f;
		float num4 = Mathf.Clamp((num3 - num2) / Mathf.Max(0.15f, num3 - pickLow * num), 0f, 1.25f);
		float num5 = Mathf.Max(0f, frame2.height - frame.height) * num * num4;
		float num6 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(since / pickTakeAt));
		playHeight = Mathf.Clamp(standingHeight - num5, Mathf.Max(0.75f, standingHeight * 0.45f), standingHeight);
		float b = Mathf.Clamp(num2 + 0.85f, Mathf.Max(0.9f, standingHeight * 0.55f), standingHeight);
		playHeight = Mathf.Min(playHeight, Mathf.Lerp(standingHeight, b, num6));
		Vector3 vector3 = new Vector3(0f, playHeight, 0f);
		Vector3 vector4 = new Vector3(0f, frame.height * num, 0f);
		Vector3 b2 = vrRoot.InverseTransformPoint(target);
		float t2 = num6 * num6;
		handOverride = true;
		handAt = Vector3.Lerp(vector3 + (vector * num - vector4), b2, t2);
		handTurn = quaternion;
		leftOverride = true;
		leftAt = vector3 + (vector2 * num - vector4);
		leftTurn = quaternion2;
		playPitch = Mathf.Clamp((0f - Mathf.Asin(Mathf.Clamp((frame.head * Vector3.forward).y, -1f, 1f))) * 57.29578f, -20f, 70f);
	}

	private static bool Grab(Hand hand, Grip grip)
	{
		return Grab(hand, grip, null);
	}

	private static bool Grab(Hand hand, Grip grip, Quaternion? turned)
	{
		try
		{
			if (hand.m_CurrentAttachedGO != null)
			{
				return false;
			}
			hand.GrabLock = false;
			InventoryHand component = hand.GetComponent<InventoryHand>();
			if (component != null)
			{
				component.IgnoreUnlock();
			}
			hand.HoverLock();
			SimpleTransform handTransform = SimpleTransform.Create(hand.transform);
			if (GripBook.HandOn(grip, hand.handedness == Handedness.LEFT, out var hand2, out var _))
			{
				handTransform.position = hand2.position;
				handTransform.rotation = hand2.rotation;
			}
			else if (turned.HasValue)
			{
				handTransform.rotation = turned.Value;
			}
			grip.ValidateGripScore(hand, handTransform);
			grip.OnHandHoverBegin(hand, isOverride: true);
			grip.ValidateGripScore(hand, handTransform);
			hand._mHoveringReceiver = grip;
			grip.OnGrabConfirm(hand, isOverride: false);
			return hand.m_CurrentAttachedGO != null;
		}
		catch (System.Exception ex2)
		{
			bool flag = false;
			try
			{
				flag = hand.m_CurrentAttachedGO != null && grip.attachedHands != null && grip.attachedHands.Contains(hand);
			}
			catch (System.Exception)
			{
			}
			if (!flag)
			{
				MelonLogger.Warning("Bot Player: could not grab " + grip.name + ": " + ex2.Message);
			}
			return flag;
		}
	}

	private bool Holding()
	{
		Hand rightHand = RightHand;
		if (rightHand != null && grip != null && rightHand.m_CurrentAttachedGO != null && grip.attachedHands != null)
		{
			return grip.attachedHands.Contains(rightHand);
		}
		return false;
	}

	private void Release()
	{
		Hand rightHand = RightHand;
		try
		{
			if (rightHand != null && grip != null)
			{
				grip.ForceDetach(rightHand);
			}
		}
		catch (System.Exception)
		{
		}
	}

	private void Let(bool go, Transform you)
	{
		Rigidbody rigidbody = item;
		Grip itsGrip = grip;
		Hand rightHand = RightHand;
		Vector3 vector = ((rightHand != null && rightHand.rb != null) ? rightHand.rb.velocity : Vector3.zero);
		Release();
		if (!(rigidbody == null) && go)
		{
			Vector3 vector2 = ((then == Then.ToPartner && catchWith != null) ? ThrowToPartner(rigidbody) : ((then == Then.TossToYou && you != null) ? Lob(rigidbody.worldCenterOfMass, you.position - Vector3.up * 0.35f, 0.75f) : ((!(throwFar > 0f)) ? (throwDir * UnityEngine.Random.Range(7f, 11f) + Vector3.up * UnityEngine.Random.Range(2f, 4f)) : ThrowVelocity(rigidbody))));
			rigidbody.velocity = ((flying != rigidbody && Vector3.Dot(vector, vector2.normalized) > vector2.magnitude) ? vector : vector2);
			rigidbody.angularVelocity = UnityEngine.Random.insideUnitSphere * 4f;
			if (then == Then.ToPartner && catchWith != null)
			{
				rigidbody.velocity = vector2;
				rigidbody.angularVelocity = UnityEngine.Random.insideUnitSphere * 1.5f;
				catchWith.ComingToYou(rigidbody, itsGrip);
				Lab.Event("catch: thrown to " + catchWith.Name + " at " + vector2.magnitude.ToString("0.0") + " m/s");
			}
		}
	}

	private static Vector3 Lob(Vector3 a, Vector3 b, float t)
	{
		Vector3 gravity = Physics.gravity;
		return (b - a) / t - 0.5f * gravity * t;
	}

	private void OverHands()
	{
		if (handOverride)
		{
			pose.height = playHeight;
			pose.rightPos = handAt;
			pose.rightRot = handTurn;
			pose.head = Quaternion.Euler(playPitch, playYaw, 0f);
			if (leftOverride)
			{
				pose.leftPos = leftAt;
				pose.leftRot = leftTurn;
			}
			else
			{
				pose.leftPos = new Vector3(-0.2f, playHeight - 0.62f, 0.12f);
				pose.leftRot = Quaternion.Euler(60f, 10f, 0f);
			}
			curl = ((Holding() || fists) ? 1f : 0.3f);
		}
	}

	private void NotCounted(string why, Attack attack)
	{
		if (!(Time.time - unCountedSaidAt < 2f))
		{
			unCountedSaidAt = Time.time;
			Lab.Event("[" + Name + "] a hit not counted (" + why + "): " + ((attack != null) ? (attack.attackType.ToString() + " " + attack.damage.ToString("0.00")) : "?"));
		}
	}

	public void TakeHit(float damage, Attack attack = null, PlayerDamageReceiver.BodyPart part = PlayerDamageReceiver.BodyPart.Chest)
	{
		if (Puppet)
		{
			NetStruck(damage, attack, part);
		}
		else
		{
			if (dead || damage <= 0f)
			{
				return;
			}
			try
			{
				if (attack != null && attack.proxy != null && NetBots.PuppetCount + NetBots.OtherPlayers().Count > 0)
				{
					RigManager componentInParent = attack.proxy.GetComponentInParent<RigManager>();
					Bot bot = ((componentInParent != null) ? BotManager.Of(componentInParent) : null);
					if (componentInParent != null && (NetBots.IsOther(componentInParent) || (bot != null && bot.Puppet)))
					{
						return;
					}
				}
			}
			catch (System.Exception)
			{
			}
			string text = "";
			if (attack != null)
			{
				text = attack.attackType.ToString();
				bool flag = text.IndexOf("Blunt", System.StringComparison.OrdinalIgnoreCase) >= 0;
				bool flag2 = !flag && text.IndexOf("Pierc", System.StringComparison.OrdinalIgnoreCase) >= 0 && Time.time - LastShotAt < 0.3f;
				if (!flag2 && OwnThingAt(attack.origin))
				{
					NotCounted("its own thing there", attack);
					return;
				}
				try
				{
					if (attack.proxy != null && attack.proxy.GetComponentInParent<RigManager>() == rig)
					{
						return;
					}
				}
				catch (System.Exception)
				{
				}
				if (!flag2 && LyingThingAt(attack.origin))
				{
					NotCounted("a thing lying there", attack);
					return;
				}
				if (flag)
				{
					try
					{
						Collider collider = attack.collider;
						Rigidbody rigidbody = Pelvis();
						if (attack.proxy == null && collider != null && (collider.name == "Feet" || collider.name == "Knee") && collider.GetComponentInParent<RigManager>() == rig && (rigidbody == null || rigidbody.velocity.y > 0f - Tuning.F("foot.knock.fall", 3f)))
						{
							return;
						}
					}
					catch (System.Exception)
					{
					}
					if (DrivingNow || Down || Time.time - upAt < 1.5f || Time.time < Clone.SuppressUntil + 1f || Time.time - spawnAskedAt < 2f || Time.time - bluntAt < 0.35f)
					{
						return;
					}
					bluntAt = Time.time;
					damage = Mathf.Min(damage, MaxHp * Tuning.F("bot.knock.most", 0.3f));
				}
			}
			if (text.IndexOf("Pierc", System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				damage = BulletHarm(damage, part);
			}
			if (!HitWhileSurrendered())
			{
				Lessons.NoteHarm();
				if (hp < 0f)
				{
					hp = MaxHp;
				}
				hp -= damage * (enemy ? Tuning.F("enemy.hurt", 1f) : Tuning.F("bot.hurt", 1f));
				Lab.Event("[" + Name + "] " + (enemy ? "enemy bot" : "bot") + " hit" + ((text.Length > 0) ? (" (" + text + ")") : "") + ": " + damage.ToString("0.00") + ", " + Mathf.Max(hp, 0f).ToString("0.00") + " left" + HitBy(attack));
				if (hp <= 0f)
				{
					Die();
					return;
				}
				Hurt(damage, attack);
				TripByHit(damage, part, attack);
				MaybeSurrender();
			}
		}
	}

	public void TakeKnock(float damage)
	{
		if (!(Time.time < Clone.SuppressUntil + 1f) && !(Time.time - spawnAskedAt < 2f) && !Down && !Puppet)
		{
			TakeHit(Mathf.Min(damage, MaxHp * Tuning.F("bot.knock.most", 0.3f)));
		}
	}

	private string HitBy(Attack attack)
	{
		try
		{
			if (attack == null)
			{
				return "";
			}
			string text = "";
			Collider collider = attack.collider;
			if (collider != null)
			{
				MarrowEntity componentInParent = collider.GetComponentInParent<MarrowEntity>();
				RigManager componentInParent2 = collider.GetComponentInParent<RigManager>();
				text = text + "; collider " + collider.name + " of " + ((componentInParent != null) ? componentInParent.name : collider.transform.root.name) + ((componentInParent2 == rig) ? " (its own body)" : ((componentInParent2 != null) ? " (another body)" : ""));
			}
			if (attack.proxy != null)
			{
				text = text + "; from " + attack.proxy.transform.root.name + ((attack.proxy.GetComponentInParent<RigManager>() == rig) ? " (itself)" : "");
			}
			float num = 99f;
			string text2 = "";
			foreach (Collider item in Physics.OverlapSphere(attack.origin, 1.2f, -1, QueryTriggerInteraction.Ignore))
			{
				Rigidbody rigidbody = ((item != null) ? item.attachedRigidbody : null);
				if (!(rigidbody == null) && !(rigidbody.GetComponentInParent<RigManager>() != null))
				{
					float magnitude = (item.bounds.ClosestPoint(attack.origin) - attack.origin).magnitude;
					if (magnitude < num)
					{
						num = magnitude;
						MarrowEntity componentInParent3 = rigidbody.GetComponentInParent<MarrowEntity>();
						Transform transform = ((componentInParent3 != null) ? componentInParent3.transform : rigidbody.transform);
						text2 = transform.name + (HeldByAnyone(transform, null) ? " (held)" : (" (loose, " + rigidbody.velocity.magnitude.ToString("0.0") + " m/s)"));
					}
				}
			}
			Transform head = Head;
			return text + "; " + ((head != null) ? (attack.origin - head.position).magnitude.ToString("0.0") : "?") + " m from its head; nearest thing " + ((text2.Length > 0) ? (text2 + " " + num.ToString("0.00") + " m") : "none") + "; holding " + ((RightHand != null && RightHand.m_CurrentAttachedGO != null) ? RightHand.m_CurrentAttachedGO.transform.root.name : "nothing") + ((this.item != null) ? (", busy with " + this.item.name) : "");
		}
		catch (System.Exception ex)
		{
			return "; from ? (" + ex.Message + ")";
		}
	}

	private bool LyingThingAt(Vector3 at)
	{
		try
		{
			foreach (Collider item in Physics.OverlapSphere(at, 0.2f, -1, QueryTriggerInteraction.Ignore))
			{
				Rigidbody rigidbody = ((item != null) ? item.attachedRigidbody : null);
				if (!(rigidbody == null) && !rigidbody.isKinematic && !(rigidbody.GetComponentInParent<RigManager>() != null))
				{
					if (rigidbody.velocity.magnitude > Tuning.F("lying.speed", 1.5f))
					{
						return false;
					}
					MarrowEntity componentInParent = rigidbody.GetComponentInParent<MarrowEntity>();
					if (!HeldByAnyone((componentInParent != null) ? componentInParent.transform : rigidbody.transform, null))
					{
						return true;
					}
				}
			}
		}
		catch (System.Exception)
		{
		}
		return false;
	}

	private bool OwnThingAt(Vector3 at)
	{
		try
		{
			if (item != null && (Playing || Time.time - playAt < 2f))
			{
				MarrowEntity componentInParent = item.GetComponentInParent<MarrowEntity>();
				foreach (Collider componentsInChild in ((componentInParent != null) ? componentInParent.transform : item.transform).GetComponentsInChildren<Collider>())
				{
					if (componentsInChild != null && !componentsInChild.isTrigger && (componentsInChild.ClosestPoint(at) - at).magnitude < 0.4f)
					{
						return true;
					}
				}
			}
		}
		catch (System.Exception)
		{
		}
		Hand[] array = new Hand[2] { LeftHand, RightHand };
		foreach (Hand hand in array)
		{
			GameObject gameObject = ((hand != null) ? hand.m_CurrentAttachedGO : null);
			if (gameObject == null)
			{
				continue;
			}
			MarrowEntity componentInParent2 = gameObject.GetComponentInParent<MarrowEntity>();
			Transform transform = ((componentInParent2 != null) ? componentInParent2.transform : gameObject.transform.root);
			if (transform == rig.transform || transform.GetComponent<RigManager>() != null)
			{
				continue;
			}
			foreach (Collider componentsInChild2 in transform.GetComponentsInChildren<Collider>())
			{
				if (!(componentsInChild2 == null) && !componentsInChild2.isTrigger)
				{
					Bounds bounds = componentsInChild2.bounds;
					bounds.Expand(Tuning.F("own.thing.room", 0.5f));
					if (bounds.Contains(at) || (componentsInChild2.ClosestPoint(at) - at).magnitude < 0.3f)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private static float BulletHarm(float damage, PlayerDamageReceiver.BodyPart part)
	{
		string text = part.ToString();
		float num = Tuning.F("bullet.harm", 5f);
		if (part == PlayerDamageReceiver.BodyPart.Head)
		{
			num *= Tuning.F("bullet.harm.head", 3f);
		}
		else if (text.Contains("Arm") || text.Contains("Hand"))
		{
			num *= Tuning.F("bullet.harm.arm", 0.6f);
		}
		else if (part >= PlayerDamageReceiver.BodyPart.LegUpperLf)
		{
			num *= Tuning.F("bullet.harm.leg", 0.75f);
		}
		return damage * num;
	}

	private void Mend(float dt)
	{
		if (!(hp < 0f) && !(hp >= MaxHp) && !(Time.time - hurtAt < (enemy ? Tuning.F("enemy.mend.after", 30f) : Tuning.F("bot.mend.after", 8f))))
		{
			hp = Mathf.Min(MaxHp, hp + MaxHp * Tuning.F("bot.mend", 0.1f) * dt);
		}
	}

	private void Die()
	{
		dead = true;
		diedAt = Time.time;
		DropStowed();
		Voice(big: false, death: true);
		try
		{
			StopPlaying();
			ReleaseCloneHands();
			Hand[] array = new Hand[2] { LeftHand, RightHand };
			foreach (Hand hand in array)
			{
				if (hand != null && hand.m_CurrentAttachedGO != null)
				{
					Grip component = hand.m_CurrentAttachedGO.GetComponent<Grip>();
					if (component != null)
					{
						LetGoOf(component, hand);
					}
				}
			}
			Limp(on: true);
		}
		catch (System.Exception ex)
		{
			MelonLogger.Warning("Bot Player: the enemy bot did not fall cleanly: " + ex.Message);
		}
		Spawnables.Notify(enemy ? "Enemy bot down" : (DownForRescue() ? "Bot down: another bot, or you standing over it, can get it up" : "Bot down"));
		Lab.Event((enemy ? "enemy bot" : "bot") + " died");
	}

	private bool Dead()
	{
		if (!dead)
		{
			return false;
		}
		if (WaitingForRescue())
		{
			return true;
		}
		if (Time.time - diedAt > Tuning.F("enemy.gone", 6f) * (enemy ? 1f : 2f))
		{
			Remove();
		}
		return true;
	}

	private bool Stow()
	{
		try
		{
			if (!WeaponInHand || HasStowed || item == null || this.grip == null || rig.physicsRig == null)
			{
				return false;
			}
			Rigidbody rigidbody = item;
			Gun gun = ((play == Play.Shoot) ? this.gun : null);
			Grip grip = this.grip;
			Vector3 fromDirection = ((gun != null && gun.firePointTransform != null) ? gun.firePointTransform.forward : (rigidbody.worldCenterOfMass - grip.transform.position).normalized);
			float a = 0.3f;
			foreach (Collider componentsInChild in rigidbody.GetComponentsInChildren<Collider>())
			{
				if (componentsInChild != null && !componentsInChild.isTrigger)
				{
					a = Mathf.Max(a, componentsInChild.bounds.size.magnitude);
				}
			}
			bool flag = gun != null && GunMoves.KindOf(gun) == "pistol";
			Transform transform = (flag ? rig.physicsRig.m_pelvis : rig.physicsRig.m_chest);
			if (transform == null)
			{
				transform = rig.physicsRig.m_pelvis;
			}
			if (transform == null)
			{
				return false;
			}
			LetGoOfSupport();
			Pull(on: false);
			if (Reloading)
			{
				DropNewMag();
				reload = Reload.None;
				learnedReload = null;
			}
			Release();
			Hand[] array = new Hand[2] { LeftHand, RightHand };
			foreach (Hand hand in array)
			{
				if (hand != null && hand.m_CurrentAttachedGO != null && hand.m_CurrentAttachedGO.transform.IsChildOf(rigidbody.transform.root) && rigidbody.transform.root != rig.transform)
				{
					FreeHand(hand);
				}
			}
			Vector3 toDirection = (flag ? (-transform.up) : (-transform.up + transform.right * 0.45f).normalized);
			Quaternion quaternion = Quaternion.FromToRotation(fromDirection, toDirection) * rigidbody.rotation;
			Vector3 vector = rigidbody.worldCenterOfMass - rigidbody.position;
			Vector3 position = (flag ? transform.TransformPoint(new Vector3(0.21f, -0.02f, 0f)) : transform.TransformPoint(new Vector3(0f, 0.02f, -0.19f))) - Quaternion.FromToRotation(fromDirection, toDirection) * vector;
			stowedOff.Clear();
			foreach (Collider componentsInChild2 in rigidbody.GetComponentsInChildren<Collider>())
			{
				if (componentsInChild2 != null && componentsInChild2.enabled && !componentsInChild2.isTrigger)
				{
					componentsInChild2.enabled = false;
					stowedOff.Add(componentsInChild2);
				}
			}
			stowedKinematic = rigidbody.isKinematic;
			rigidbody.isKinematic = true;
			stowOn = transform;
			stowAt = transform.InverseTransformPoint(position);
			stowTurn = Quaternion.Inverse(transform.rotation) * quaternion;
			stowed = rigidbody;
			stowedGun = gun;
			stowedGrip = grip;
			stowedSince = Time.time;
			this.gun = null;
			support = null;
			item = null;
			this.grip = null;
			play = Play.None;
			handOverride = false;
			leftOverride = false;
			hitClip = null;
			Lab.Event("holster: the " + rigidbody.name + " put away " + (flag ? "on its hip" : "across its back"));
			StowFollow();
			return true;
		}
		catch (System.Exception ex)
		{
			Lab.Event("holster: could not put it away: " + ex.Message);
			return false;
		}
	}

	public void StowFollow()
	{
		if (stowed == null)
		{
			return;
		}
		try
		{
			if (!stowed.gameObject.activeInHierarchy || stowOn == null)
			{
				stowed = null;
				return;
			}
			Vector3 vector = stowOn.TransformPoint(stowAt);
			Quaternion quaternion = stowOn.rotation * stowTurn;
			if (!Bad(vector) && !Bad(quaternion))
			{
				stowed.position = vector;
				stowed.rotation = quaternion;
				stowed.transform.SetPositionAndRotation(vector, quaternion);
			}
		}
		catch (System.Exception)
		{
			stowed = null;
		}
	}

	private Rigidbody Unstow()
	{
		Rigidbody rigidbody = stowed;
		stowed = null;
		if (rigidbody == null)
		{
			return null;
		}
		try
		{
			foreach (Collider item in stowedOff)
			{
				if (item != null)
				{
					item.enabled = true;
				}
			}
			stowedOff.Clear();
			rigidbody.isKinematic = stowedKinematic;
			rigidbody.velocity = Vector3.zero;
			rigidbody.angularVelocity = Vector3.zero;
		}
		catch (System.Exception)
		{
		}
		return rigidbody;
	}

	public void DropStowed()
	{
		if (stowed != null)
		{
			Unstow();
			stowedGun = null;
			stowedGrip = null;
		}
	}

	public object[] KeepStowed()
	{
		if (!HasStowed)
		{
			return null;
		}
		object[] result = new object[8]
		{
			stowed,
			stowedGun,
			stowedGrip,
			stowOn,
			stowAt,
			stowTurn,
			stowedKinematic,
			stowedOff.ToArray()
		};
		stowed = null;
		stowedGun = null;
		stowedGrip = null;
		return result;
	}

	public void TakeStowed(object[] kept)
	{
		try
		{
			if (kept != null && kept.Length >= 8 && kept[0] != null)
			{
				stowed = kept[0] as Rigidbody;
				stowedGun = kept[1] as Gun;
				stowedGrip = kept[2] as Grip;
				stowOn = kept[3] as Transform;
				stowAt = (Vector3)kept[4];
				stowTurn = (Quaternion)kept[5];
				stowedKinematic = (bool)kept[6];
				stowedOff.Clear();
				if (kept[7] is Collider[] collection)
				{
					stowedOff.AddRange(collection);
				}
				stowedSince = Time.time;
				if (stowed == null || stowOn == null || stowedGrip == null)
				{
					stowed = null;
				}
			}
		}
		catch (System.Exception)
		{
			stowed = null;
		}
	}

	private bool Draw()
	{
		try
		{
			Hand rightHand = RightHand;
			if (!HasStowed || rightHand == null || stowedGrip == null)
			{
				return false;
			}
			if (rightHand.m_CurrentAttachedGO != null)
			{
				FreeHand(rightHand);
			}
			Gun gun = stowedGun;
			Grip grip = stowedGrip;
			Rigidbody rigidbody = Unstow();
			stowedGun = null;
			stowedGrip = null;
			if (rigidbody == null)
			{
				return false;
			}
			Vector3 vector = grip.transform.position - rigidbody.position;
			rigidbody.position = rightHand.transform.position - vector;
			rigidbody.transform.position = rightHand.transform.position - vector;
			drawnAt = Time.time;
			if (gun != null)
			{
				Take(gun);
				if (!Grab(rightHand, this.grip))
				{
					Lab.Event("holster: could not take the " + rigidbody.name + " in hand again");
					EndPlay();
					return false;
				}
				StartShooting();
			}
			else
			{
				item = rigidbody;
				this.grip = grip;
				this.gun = null;
				support = null;
				Quaternion? turned = null;
				Vector3 vector2 = rigidbody.worldCenterOfMass - grip.transform.position;
				if (vector2.sqrMagnitude > 0.0004f)
				{
					turned = Quaternion.FromToRotation(rightHand.transform.rotation * Vector3.back, vector2.normalized) * rightHand.transform.rotation;
				}
				if (!Grab(rightHand, this.grip, turned))
				{
					Lab.Event("holster: could not take the " + rigidbody.name + " in hand again");
					item = null;
					this.grip = null;
					return false;
				}
				MeasureMelee();
				Next(Play.Melee);
			}
			Lab.Event("holster: the " + rigidbody.name + " drawn");
			return true;
		}
		catch (System.Exception ex)
		{
			Lab.Event("holster: could not draw: " + ex.Message);
			return false;
		}
	}

	private void KeepAwake()
	{
		if (Time.time < awakeAt)
		{
			return;
		}
		awakeAt = Time.time + 1f;
		try
		{
			if (ownEntities == null)
			{
				ownEntities = rig.GetComponentsInChildren<MarrowEntity>(includeInactive: true);
			}
			MarrowEntity[] array = ownEntities;
			foreach (MarrowEntity marrowEntity in array)
			{
				if (marrowEntity == null)
				{
					continue;
				}
				marrowEntity.PreventDisableOnCull();
				if (!marrowEntity.IsCulled && !marrowEntity.IsHibernating)
				{
					continue;
				}
				wakings++;
				if (wakings <= 3 || wakings % 20 == 0)
				{
					Lab.Event("awake: " + marrowEntity.name + " had been put to sleep by the level (" + (marrowEntity.IsCulled ? "culled" : "hibernating") + "), woken (" + wakings + ")");
				}
				try
				{
					if (marrowEntity.IsCulled && marrowEntity._inactiveStatus != null)
					{
						marrowEntity._inactiveStatus.Cull(isInactive: false);
					}
					marrowEntity.Hibernate(setHibernating: false, (MarrowEntity.HibernationSources)(-1));
				}
				catch (System.Exception)
				{
				}
			}
		}
		catch (System.Exception)
		{
		}
	}

	private string AwakeSaid()
	{
		try
		{
			if (ownEntities == null)
			{
				return "entities ?";
			}
			int num = 0;
			int num2 = 0;
			string text = "";
			MarrowEntity[] array = ownEntities;
			foreach (MarrowEntity marrowEntity in array)
			{
				if (!(marrowEntity == null))
				{
					if (marrowEntity.IsCulled)
					{
						num++;
						text = text + " " + marrowEntity.name;
					}
					if (marrowEntity.IsHibernating)
					{
						num2++;
					}
				}
			}
			return ownEntities.Length + " part(s), " + num + " culled" + ((text.Length > 0) ? (" (" + text.Trim() + ")") : "") + ", " + num2 + " asleep";
		}
		catch (System.Exception)
		{
			return "entities ?";
		}
	}

	public string LabStatus()
	{
		Transform head = Head;
		float num = 999f;
		bool flag = false;
		string text = "";
		try
		{
			foreach (Targets.Target item in Targets.Hostiles())
			{
				float num2 = ((head != null) ? (item.Point - head.position).magnitude : 999f);
				if (num2 < num)
				{
					num = num2;
					flag = head != null && Targets.InSight(head.position, item.Point, item.transform, rig);
					text = ((item.transform != null) ? item.transform.name : "?") + ((item.brain != null) ? (" (an NPC" + (item.brain.isDead ? ", dead" : "") + ")") : ((item.zombieHead != null) ? " (a zombie)" : "")) + ", its point " + (item.Point.y - Feet.y).ToString("0.0") + " m above its feet's level";
				}
			}
		}
		catch (System.Exception)
		{
		}
		return Name + " " + mode.ToString() + "/" + goal.ToString() + "/" + play.ToString() + ((act != 0) ? ("/" + act) : "") + ", health " + ((hp < 0f) ? "full" : hp.ToString("0.0")) + ", nearest hostile " + ((num > 900f) ? "none" : (num.ToString("0.0") + " m" + (flag ? " in sight" : (" NOT in sight (behind " + Targets.LastBlocker + ")")))) + " [" + text + "], sees " + ((nearestSeen.transform != null) ? (nearestSeenDistance.ToString("0.0") + " m") : "none") + ", " + AwakeSaid() + (WeaponInHand ? ", weapon in hand" : (HasStowed ? ", weapon put away" : ", unarmed")) + (climbing ? ", climbing" : "") + (InLesson ? ", in a lesson" : "") + (down ? ", down" : "");
	}

	public void LabHolster()
	{
		if (HasStowed)
		{
			Draw();
		}
		else if (!Stow())
		{
			Lab.Event("holster: nothing in its hand to put away");
		}
	}

	private Vector3 KeepApart(Vector3 dir, ref float speed)
	{
		try
		{
			Transform head = Head;
			if (head == null || dir.sqrMagnitude < 0.0001f)
			{
				return dir;
			}
			float num = Tuning.F("apart.room", 1.4f);
			Vector3 zero = Vector3.zero;
			float num2 = 1f;
			foreach (Bot item in BotManager.All)
			{
				if (item == this || item.dead || item.Head == null)
				{
					continue;
				}
				Vector3 vector = Flat(head.position - item.Head.position);
				float magnitude = vector.magnitude;
				if (!(magnitude > num) && !(Mathf.Abs(head.position.y - item.Head.position.y) > 1.5f))
				{
					Vector3 vector2 = ((magnitude > 0.05f) ? (vector / magnitude) : (Quaternion.Euler(0f, rig.Pointer.ToInt64() % 360, 0f) * Vector3.forward));
					zero += vector2 * (1f - magnitude / num);
					if (Vector3.Dot(-vector2, dir) > 0.6f && magnitude < num * 0.8f && rig.Pointer.ToInt64() < item.rig.Pointer.ToInt64())
					{
						num2 = Mathf.Min(num2, Mathf.Clamp01(magnitude / num - 0.2f));
					}
				}
			}
			if (zero.sqrMagnitude < 0.0001f)
			{
				return dir;
			}
			speed *= Mathf.Max(0.15f, num2);
			Vector3 v = dir + zero * Tuning.F("apart.push", 0.8f);
			return (v.sqrMagnitude > 0.0001f) ? Flat(v).normalized : dir;
		}
		catch (System.Exception)
		{
			return dir;
		}
	}

	private void NoPushOnItself(Transform thing, bool again = false)
	{
		try
		{
			MarrowEntity componentInParent = thing.GetComponentInParent<MarrowEntity>();
			Transform transform = ((componentInParent != null) ? componentInParent.transform : thing);
			if (!noPushDone.Add(transform.Pointer) && !again)
			{
				return;
			}
			Collider[]? array = transform.GetComponentsInChildren<Collider>(includeInactive: true);
			Collider[] array2 = rig.physicsRig.GetComponentsInChildren<Collider>(includeInactive: true);
			Collider[] array3 = array;
			foreach (Collider collider in array3)
			{
				if (collider == null || collider.isTrigger)
				{
					continue;
				}
				Collider[] array4 = array2;
				foreach (Collider collider2 in array4)
				{
					if (collider2 != null && !collider2.isTrigger)
					{
						Physics.IgnoreCollision(collider, collider2, ignore: true);
					}
				}
			}
		}
		catch (System.Exception)
		{
		}
	}

	private bool OffTheGround(Rigidbody hips)
	{
		float maxDistance = Tuning.F("guard.rise.clear", 1.6f);
		foreach (RaycastHit item in Physics.RaycastAll(hips.position, Vector3.down, maxDistance, -1, QueryTriggerInteraction.Ignore))
		{
			if (!(item.collider == null) && !(item.collider.GetComponentInParent<RigManager>() == rig) && (!(gun != null) || !item.collider.transform.IsChildOf(gun.transform)))
			{
				return false;
			}
		}
		return true;
	}

	private void RiseGuard(Rigidbody hips)
	{
		Vector3 velocity = hips.velocity;
		bool flag = climbing || InLesson || lessonGliding || Time.time - jumpedAt < 0.9f;
		bool flag2 = velocity.y > Tuning.F("guard.rise", 3.2f);
		risingFor = ((flag2 && !flag) ? (risingFor + Time.fixedDeltaTime) : 0f);
		if (flag || (!(risingFor >= Tuning.F("guard.rise.for", 0.3f)) && (!(velocity.y > Tuning.F("guard.rise.slow", 1.2f)) || !OffTheGround(hips))))
		{
			if (Time.time - liftedAt > 3f)
			{
				liftedFor = 0f;
			}
			return;
		}
		liftedFor += Time.fixedDeltaTime;
		liftedAt = Time.time;
		if (Time.time - fastSaidAt > 1f)
		{
			fastSaidAt = Time.time;
			Lab.Event("guard: going up at " + velocity.y.ToString("0.0") + " m/s with nothing taking it up, in " + mode.ToString() + "/" + goal.ToString() + "/" + play.ToString() + (WeaponInHand ? ", weapon in hand" : "") + ": stopped (" + liftedFor.ToString("0.0") + " s of it)");
		}
		foreach (Rigidbody componentsInChild in rig.physicsRig.GetComponentsInChildren<Rigidbody>())
		{
			if (!(componentsInChild == null) && !componentsInChild.isKinematic)
			{
				Vector3 velocity2 = componentsInChild.velocity;
				componentsInChild.velocity = new Vector3(velocity2.x, Mathf.Min(velocity2.y, 0f), velocity2.z);
			}
		}
		if (gun != null && Time.time > noPushAgainAt)
		{
			noPushAgainAt = Time.time + 1f;
			NoPushOnItself(gun.transform, again: true);
		}
		if (liftedFor > Tuning.F("guard.rise.drop", 1f))
		{
			Lab.Event("guard: still going up: let go of what it holds");
			FreeHand(LeftHand);
			FreeHand(RightHand);
			liftedFor = 0f;
		}
		else if (liftedFor > Tuning.F("guard.rise.free", 0.4f))
		{
			FreeHand(LeftHand);
		}
	}

	public void SpeedGuard()
	{
		try
		{
			if (dead || down || rig.physicsRig == null || rig.activeSeat != null)
			{
				return;
			}
			Rigidbody rigidbody = Pelvis();
			if (rigidbody == null)
			{
				return;
			}
			RiseGuard(rigidbody);
			Vector3 velocity = rigidbody.velocity;
			float magnitude = Flat(velocity).magnitude;
			float num = Tuning.F("guard.speed", 9.5f);
			if (magnitude <= num && velocity.y <= Tuning.F("guard.up", 9f))
			{
				return;
			}
			if (Time.time - fastSaidAt > 1f)
			{
				fastSaidAt = Time.time;
				Lab.Event("guard: going " + magnitude.ToString("0.0") + " m/s (up " + velocity.y.ToString("0.0") + ") in " + mode.ToString() + "/" + goal.ToString() + "/" + play.ToString() + (climbing ? ", climbing" : "") + (climbHang ? ", hanging" : "") + (InLesson ? ", in a lesson" : "") + (lessonGliding ? ", carried up" : "") + (HasStowed ? ", weapon put away" : "") + (running ? ", running" : "") + ", legs' most " + ((rig.remapHeptaRig != null) ? rig.remapHeptaRig.maxVelocity.ToString("0.0") : "?") + ": held back");
			}
			float num2 = ((magnitude > num) ? (num / magnitude) : 1f);
			foreach (Rigidbody componentsInChild in rig.physicsRig.GetComponentsInChildren<Rigidbody>())
			{
				if (!(componentsInChild == null) && !componentsInChild.isKinematic)
				{
					Vector3 velocity2 = componentsInChild.velocity;
					componentsInChild.velocity = new Vector3(velocity2.x * num2, Mathf.Min(velocity2.y, Tuning.F("guard.up", 9f)), velocity2.z * num2);
				}
			}
		}
		catch (System.Exception)
		{
		}
	}

	private Vector3 OwnSide(float far)
	{
		float y = Mathf.Abs(rig.Pointer.ToInt64() / 16) % 8f * 45f;
		return Quaternion.Euler(0f, y, 0f) * Vector3.forward * far;
	}

	private void Flesh()
	{
		Il2CppSLZ.VRMK.Avatar avatar = rig.avatar;
		if (avatar == null || avatar.Pointer == fleshOf || rig.physicsRig == null)
		{
			return;
		}
		fleshOf = avatar.Pointer;
		avatarCode = BotRig.AvatarOf(rig);
		clip = null;
		lately.Clear();
		try
		{
			SurfaceData surfaceData = avatar.surfaceData;
			if (surfaceData == null && Player.RigManager != null && Player.RigManager.avatar != null)
			{
				surfaceData = Player.RigManager.avatar.surfaceData;
			}
			if (Tuning.F("flesh.blood", 1f) > 0f)
			{
				AskForBlood();
				if (bloodData != null)
				{
					surfaceData = bloodData;
				}
			}
			string text = ((surfaceData != null) ? (surfaceData.name + ", resists a blade " + surfaceData.PenetrationResistance.ToString("0.00") + ", " + surfaceData.megaPascal.ToString("0.0") + " MPa") : "none");
			if (surfaceData != null)
			{
				float num = Tuning.F("flesh.resist", 0.6f);
				if (surfaceData.PenetrationResistance > num)
				{
					SurfaceData surfaceData2 = UnityEngine.Object.Instantiate(surfaceData);
					surfaceData2.name = surfaceData.name;
					surfaceData2.megaPascal = surfaceData.megaPascal * Mathf.Clamp01(num / Mathf.Max(0.01f, surfaceData.PenetrationResistance)) * Tuning.F("flesh.mpa", 1f);
					surfaceData2.PenetrationResistance = num;
					surfaceData = surfaceData2;
				}
			}
			bool flag = false;
			try
			{
				flag = AssetWarehouse.Instance.HasDataCard(new Barcode("SLZ.Backlot.SurfaceDataCard.Blood"));
			}
			catch (System.Exception)
			{
			}
			Lab.Event("flesh: its avatar's surface was " + text + "; a blood card in the game: " + flag);
			int num2 = 0;
			foreach (PlayerDamageReceiver componentsInChild in rig.GetComponentsInChildren<PlayerDamageReceiver>(includeInactive: true))
			{
				if (componentsInChild == null || componentsInChild.GetComponent<Collider>() == null)
				{
					continue;
				}
				ImpactProperties impactProperties = componentsInChild.GetComponent<ImpactProperties>();
				if (impactProperties == null)
				{
					if (surfaceData == null)
					{
						continue;
					}
					impactProperties = componentsInChild.gameObject.AddComponent<ImpactProperties>();
				}
				try
				{
					if (bloodData != null && surfaceData == bloodData)
					{
						impactProperties.SurfaceDataCard = new DataCardReference<SurfaceDataCard>(new Barcode("SLZ.Backlot.SurfaceDataCard.Blood"));
					}
				}
				catch (System.Exception)
				{
				}
				if (surfaceData != null)
				{
					impactProperties.surfaceData = surfaceData;
					impactProperties._cachedSurfaceData = surfaceData;
					impactProperties._isSurfaceDataLoaded = true;
				}
				impactProperties.decalType = ImpactProperties.DecalType.None;
				num2++;
			}
			Lab.Event("flesh: " + avatar.name + " is made of " + ((surfaceData != null) ? (surfaceData.name + " (resists a blade " + surfaceData.PenetrationResistance.ToString("0.00") + ")") : "nothing it says") + ", on " + num2 + " parts of its body; voice: " + Clips(avatar.smallPain) + " small pain, " + Clips(avatar.bigPain) + " big pain, " + Clips(avatar.dying) + " dying, " + Clips(avatar.dead) + " dead");
		}
		catch (System.Exception ex3)
		{
			Lab.Event("flesh: could not be given to its body: " + ex3.Message);
		}
	}

	public void FleshAgain()
	{
		fleshOf = System.IntPtr.Zero;
	}

	private static void AskForBlood()
	{
		if (bloodAsked)
		{
			return;
		}
		bloodAsked = true;
		try
		{
			if (!AssetWarehouse.Instance.TryGetDataCard(new Barcode("SLZ.Backlot.SurfaceDataCard.Blood"), out SurfaceDataCard dataCardT) || dataCardT == null)
			{
				Lab.Event("flesh: no blood card to be had");
				return;
			}
			dataCardT.SurfaceData.LoadAsset((System.Action<SurfaceData>)delegate(SurfaceData loaded)
			{
				if (loaded == null)
				{
					return;
				}
				bloodData = loaded;
				string text = "none";
				try
				{
					text = ((loaded.impactEffectSpawnable != null && loaded.impactEffectSpawnable.crateRef != null) ? loaded.impactEffectSpawnable.crateRef.Barcode.ID : "none");
				}
				catch (System.Exception)
				{
				}
				Lab.Event("flesh: the game's blood is " + loaded.name + ", resists a blade " + loaded.PenetrationResistance.ToString("0.00") + ", " + loaded.megaPascal.ToString("0.0") + " MPa, effect " + text);
				foreach (Bot item in BotManager.All)
				{
					item.FleshAgain();
				}
			});
		}
		catch (System.Exception ex)
		{
			Lab.Event("flesh: the blood could not be asked for: " + ex.Message);
		}
	}

	private static int Clips(AudioVarianceData data)
	{
		if (!(data != null) || data.audioClips == null)
		{
			return 0;
		}
		return data.audioClips.Length;
	}

	private void Hurt(float damage, Attack attack)
	{
		hurtAt = Time.time;
		bool big = damage > MaxHp * Tuning.F("bot.bigpain", 0.2f);
		Voice(big, death: false);
		Transform head = Head;
		if (!(head == null))
		{
			Vector3 v = ((attack != null && attack.direction.sqrMagnitude > 0.01f) ? (-attack.direction) : head.forward);
			flinchSide = Mathf.Clamp((Quaternion.Euler(0f, 0f - bodyYaw, 0f) * Flat(v).normalized).x, -1f, 1f);
			flinchSize = Mathf.Clamp(0.6f + damage / MaxHp, 0.6f, 1.4f);
			flinchAt = Time.time;
			if (attack != null && attack.direction.sqrMagnitude > 0.01f)
			{
				heardAt = head.position - attack.direction.normalized * 8f;
				heardTime = Time.time;
				gazeUntil = 0f;
			}
		}
	}

	private void Voice(bool big, bool death)
	{
		if (!death && Time.time - voiceAt < Tuning.F("bot.voice.gap", 0.5f))
		{
			return;
		}
		voiceAt = Time.time;
		try
		{
			Il2CppSLZ.VRMK.Avatar avatar = rig.avatar;
			Transform head = Head;
			if (avatar == null || head == null)
			{
				return;
			}
			AudioVarianceData audioVarianceData = ((!death) ? (big ? avatar.bigPain : avatar.smallPain) : ((Clips(avatar.dead) > 0) ? avatar.dead : avatar.dying));
			if (Clips(audioVarianceData) == 0)
			{
				audioVarianceData = ((Clips(avatar.bigPain) > 0) ? avatar.bigPain : avatar.smallPain);
			}
			if (Clips(audioVarianceData) == 0)
			{
				HeadSFX componentInChildren = rig.GetComponentInChildren<HeadSFX>(includeInactive: true);
				if (componentInChildren != null)
				{
					if (death)
					{
						componentInChildren.DeathVocal();
					}
					else if (big)
					{
						componentInChildren.BigDamageVocal();
					}
					else
					{
						componentInChildren.SmallDamageVocal(1f);
					}
				}
			}
			else
			{
				Audio3dManager.PlayAtPoint(audioVarianceData.audioClips, head.position, Audio3dManager.hardInteraction, Tuning.F("bot.voice", 0.9f), 1f, new Il2CppSystem.Nullable<float>(0.06f), new Il2CppSystem.Nullable<float>(1f), new Il2CppSystem.Nullable<float>(1f));
			}
		}
		catch (System.Exception ex)
		{
			Lab.Event("voice failed: " + ex.Message);
		}
	}

	public void LabHurt(float share)
	{
		TakeHit(MaxHp * share);
	}

	public string LabState()
	{
		StringBuilder stringBuilder = new StringBuilder(512);
		stringBuilder.Append("{\"t\":").Append(N(Time.time));
		stringBuilder.Append(",\"bot\":\"").Append(Name).Append('"');
		stringBuilder.Append(",\"mode\":\"").Append(mode).Append('"');
		stringBuilder.Append(",\"goal\":\"").Append(goal).Append('"');
		stringBuilder.Append(",\"why\":\"").Append(why.Replace("\"", "'")).Append('"');
		Vector3 vector = Feet;
		stringBuilder.Append(",\"pos\":[").Append(N(vector.x)).Append(',')
			.Append(N(vector.y))
			.Append(',')
			.Append(N(vector.z))
			.Append(']');
		stringBuilder.Append(",\"yaw\":").Append(N(bodyYaw));
		stringBuilder.Append(",\"play\":\"").Append(play).Append('"');
		bool flag = gun != null && Holding();
		stringBuilder.Append(",\"armed\":").Append(flag ? "true" : "false");
		if (gun != null)
		{
			stringBuilder.Append(",\"gun\":\"").Append(gun.name).Append('"');
			stringBuilder.Append(",\"ammo\":").Append(N(AmmoFraction()));
			stringBuilder.Append(",\"chamber\":\"").Append(SafeState()).Append('"');
			stringBuilder.Append(",\"reload\":\"").Append(reload).Append('"');
			stringBuilder.Append(",\"support\":\"").Append((support != null) ? support.name : "none").Append('"');
			stringBuilder.Append(",\"supportHeld\":").Append(supportHeld ? "true" : "false");
			stringBuilder.Append(",\"step\":\"").Append(step).Append('"');
			if (item != null)
			{
				stringBuilder.Append(",\"spin\":").Append(N(item.angularVelocity.magnitude));
			}
			Transform firePointTransform = gun.firePointTransform;
			if (firePointTransform != null)
			{
				Vector3 forward = firePointTransform.forward;
				stringBuilder.Append(",\"muzzlePitch\":").Append(N((0f - Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f))) * 57.29578f));
				if (labWantUp != Vector3.zero)
				{
					stringBuilder.Append(",\"rollErr\":").Append(N(Vector3.SignedAngle(labWantUp, labGunUp, forward)));
				}
				stringBuilder.Append(",\"gunUpY\":").Append(N(labGunUp.y));
				stringBuilder.Append(",\"muzzleYawOff\":").Append(N(Mathf.DeltaAngle(bodyYaw, Recorder.Yaw(forward))));
			}
			if (gunWantSet)
			{
				Hand rightHand = RightHand;
				if (rightHand != null)
				{
					stringBuilder.Append(",\"rightOff\":").Append(N(((gunWant * rightInGun).position - rightHand.transform.position).magnitude));
				}
				Hand leftHand = LeftHand;
				if (leftHand != null && supportHeld)
				{
					stringBuilder.Append(",\"leftOff\":").Append(N(((gunWant * leftInGun).position - leftHand.transform.position).magnitude));
				}
				if (item != null)
				{
					stringBuilder.Append(",\"gunOffDeg\":").Append(N(Quaternion.Angle(gunWant.rotation, item.transform.rotation)));
				}
			}
		}
		stringBuilder.Append(",\"target\":\"").Append((target.transform == null) ? "none" : ((target.zombieHead != null) ? "zombie" : ((target.brain != null) ? "npc" : "prop"))).Append('"');
		if (target.transform != null && Head != null)
		{
			stringBuilder.Append(",\"targetDist\":").Append(N((TargetPoint() - Head.position).magnitude));
		}
		stringBuilder.Append(",\"aimErr\":").Append(N(labAimError));
		stringBuilder.Append(",\"trigger\":").Append(N(trigger));
		stringBuilder.Append(",\"sinceShot\":").Append(N(Mathf.Min(Time.time - firedAt, 999f)));
		stringBuilder.Append(",\"shots\":").Append(LabShots);
		stringBuilder.Append(",\"dryPulls\":").Append(dryPulls);
		stringBuilder.Append(",\"seenEnemies\":").Append(seenEnemies.Count);
		stringBuilder.Append(",\"walk\":[").Append(N(walk.x)).Append(',')
			.Append(N(walk.y))
			.Append(']');
		stringBuilder.Append(",\"stick\":[").Append(N(stick.x)).Append(',')
			.Append(N(stick.y))
			.Append(']');
		stringBuilder.Append(",\"expects\":").Append(N(Knowledge.Worth(goal.ToString(), seen)));
		stringBuilder.Append('}');
		return stringBuilder.ToString();
	}

	public string LabSummary()
	{
		return mode.ToString() + "/" + goal.ToString() + ((gun != null) ? (" gun " + gun.name + " support " + (supportHeld ? "held" : ((support != null) ? "not held" : "none")) + " aimErr " + N(labAimError)) : " unarmed");
	}

	private string SafeState()
	{
		try
		{
			return gun.cartridgeState.ToString() + "/" + gun.slideState.ToString() + "/mag " + (gun.HasMagazine() ? gun.AmmoCount().ToString() : "none");
		}
		catch (System.Exception)
		{
			return "?";
		}
	}

	private static string N(float v)
	{
		if (!float.IsNaN(v) && !float.IsInfinity(v))
		{
			return v.ToString("0.###", CultureInfo.InvariantCulture);
		}
		return "0";
	}

	public void LabGive(string which)
	{
		string text = which;
		if (!which.Contains(".") && !which.Contains("-"))
		{
			text = null;
			FieldInfo[] fields = typeof(CommonBarcodes.Guns).GetFields(BindingFlags.Static | BindingFlags.Public);
			foreach (FieldInfo fieldInfo in fields)
			{
				if (fieldInfo.FieldType == typeof(string) && string.Equals(fieldInfo.Name, which, System.StringComparison.OrdinalIgnoreCase))
				{
					text = fieldInfo.GetValue(null) as string;
				}
			}
			if (text == null && which.Length > 0)
			{
				Lab.Event("no gun called " + which);
				return;
			}
		}
		StopPlaying();
		if (mode != Mode.Brain && mode != Mode.Gunplay)
		{
			SetMode(Mode.Gunplay);
		}
		DropStowed();
		StopPlaying();
		if (!SpawnGun(string.IsNullOrEmpty(text) ? null : text))
		{
			Lab.Event("could not spawn " + which);
		}
	}

	public bool UseLesson(Lessons.Lesson lesson, bool atSpot)
	{
		return UseLesson(lesson, atSpot, 0f, 0, null);
	}

	private bool LandedOnTop()
	{
		if (rig.physicsRig.centerOfPressure == null || lessonTopY == float.MinValue)
		{
			return true;
		}
		Vector3 position = rig.physicsRig.centerOfPressure.position;
		float num = Vector3.Dot(position - lessonWallFoot, lessonWallIn);
		if (!lessonLanding)
		{
			if (num > Tuning.F("tool.climb.stand", 0.45f) + 0.5f && position.y > lessonTopY - 0.2f && FeetOnGround())
			{
				return true;
			}
			lessonLanding = true;
			lessonLandSince = Time.time;
			Vector3 vector = lessonWallFoot + lessonWallIn * (Tuning.F("tool.climb.stand", 0.45f) + Tuning.F("tool.climb.onto", 0.9f));
			lessonLandAt = new Vector3(vector.x, lessonTopY + 0.05f, vector.z);
			if (carried.Count == 0)
			{
				CarryThings(on: true);
			}
			Lab.Event("lesson: set down on the top of its wall (" + (lessonLandAt - position).magnitude.ToString("0.0") + " m to go)");
			return false;
		}
		if ((position - lessonLandAt).magnitude > 0.08f && Time.time - lessonLandSince < 5f)
		{
			return false;
		}
		lessonLanding = false;
		return true;
	}

	private static float SizeOf(string barcode)
	{
		try
		{
			AssetWarehouse instance = AssetWarehouse.Instance;
			if (instance != null && instance.TryGetCrate(new Barcode(barcode), out var crate) && crate != null)
			{
				GameObjectCrate gameObjectCrate = crate.TryCast<GameObjectCrate>();
				if (gameObjectCrate != null)
				{
					return gameObjectCrate.ColliderBounds.size.magnitude;
				}
			}
		}
		catch (System.Exception)
		{
		}
		return 0f;
	}

	private void PlaceBigThing(Clone.Event e, ref Vector3 where, ref Quaternion faces)
	{
		float num = SizeOf(e.barcode);
		if (num < Tuning.F("lesson.big", 1.1f))
		{
			return;
		}
		Transform head = Head;
		Rigidbody rigidbody = Pelvis();
		if (head == null || rigidbody == null)
		{
			return;
		}
		Vector3 vector = ((rig.physicsRig.centerOfPressure != null) ? rig.physicsRig.centerOfPressure.position : Feet);
		Vector3 vector2 = ((Flat(head.forward).sqrMagnitude > 0.01f) ? Flat(head.forward).normalized : (Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward));
		float num2 = Tuning.F("lesson.front", 1.5f) + num * 0.25f;
		bool flag = e.turn != Quaternion.identity && e.at != Vector3.zero;
		if (placeAhead.sqrMagnitude > 0.01f)
		{
			vector2 = placeAhead;
			flag = false;
		}
		if (placeAtSet && placeAhead.sqrMagnitude > 0.01f)
		{
			Vector3 point = placeAt;
			if (Physics.Raycast(point + Vector3.up * 2f, Vector3.down, out var hitInfo, 6f, -1, QueryTriggerInteraction.Ignore))
			{
				point = hitInfo.point;
			}
			where = point + Vector3.up * 0.15f;
			faces = Quaternion.LookRotation(placeAhead, Vector3.up);
			lessonBigAsked.Add(-1000 - cloneStandIns.Count);
			Lab.Event("lesson: the " + Clone.ThingName(e) + " put on the way it is to drive, " + Flat(point - vector).magnitude.ToString("0.0") + " m off");
			return;
		}
		Vector3 vector3 = (flag ? LessonPlace(e.at) : (vector + vector2 * num2));
		if (flag && Flat(vector3 - vector).magnitude < 0.9f)
		{
			flag = false;
		}
		if (!flag)
		{
			vector3 = vector + vector2 * num2;
			if (Physics.Raycast(vector3 + Vector3.up * 2f, Vector3.down, out var hitInfo2, 6f, -1, QueryTriggerInteraction.Ignore))
			{
				vector3 = hitInfo2.point + Vector3.up * 0.15f;
			}
			faces = Quaternion.LookRotation(vector2, Vector3.up);
		}
		else
		{
			faces = LessonTurn(e.turn);
		}
		where = vector3;
		lessonBigAsked.Add(-1000 - cloneStandIns.Count);
		Lab.Event("lesson: the " + Clone.ThingName(e) + " is " + num.ToString("0.0") + " m across: put " + Flat(vector3 - vector).magnitude.ToString("0.0") + " m off" + (flag ? ", where yours was from you" : ", in front of it"));
	}

	private void SeatCheck()
	{
		try
		{
			if (seatAsked == null || Time.time - seatAskedAt < 1f)
			{
				return;
			}
			Seat seat = seatAsked;
			seatAsked = null;
			Rigidbody rigidbody = Pelvis();
			float num = ((rigidbody != null) ? Flat(rigidbody.position - seat.transform.position).magnitude : 0f);
			if ((rig.activeSeat == seat && num < Tuning.F("seat.off", 0.45f)) || !InLesson)
			{
				seatRetries = 0;
				return;
			}
			if (seatRetries >= 2)
			{
				Lab.Event("lesson: still not sat right (" + num.ToString("0.00") + " m from its seat), left at that");
				seatRetries = 0;
				return;
			}
			seatRetries++;
			Lab.Event("lesson: not sat right (" + ((rig.activeSeat == seat) ? "" : "not in its seat, ") + num.ToString("0.00") + " m from it): out and in again (" + seatRetries + ")");
			if (rig.activeSeat != null)
			{
				rig.activeSeat.EgressRig(immediate: true);
			}
			else if (rig.bodyState == RigManager.BodyState.Seated)
			{
				rig.SwitchBodyState(RigManager.BodyState.OnFoot);
			}
			Vector3 vector = seat.transform.position - Flat(seat.transform.right).normalized * 1.1f;
			if (Physics.Raycast(vector + Vector3.up * 1.5f, Vector3.down, out var hitInfo, 4f, -1, QueryTriggerInteraction.Ignore))
			{
				vector = hitInfo.point;
			}
			rig.Teleport(vector);
			lessonPelvisHas = false;
			seatMoves = 2;
			seatWaiting = seatAskedOf;
			seatWaitUntil = Time.time + 4f;
			seatSitAt = Time.time + 0.4f;
		}
		catch (System.Exception)
		{
		}
	}

	private void LessonSeat(Clone.Event e)
	{
		try
		{
			if (e.hand == 0)
			{
				Seat activeSeat = rig.activeSeat;
				if (activeSeat != null)
				{
					activeSeat.EgressRig(immediate: true);
				}
				Lab.Event("lesson: gets out");
				return;
			}
			string key = e.barcode + "#" + Mathf.Max(0, Clone.ThingNumber(e));
			if (!lessonThings.TryGetValue(key, out var value) || value == null)
			{
				foreach (Poolee value2 in cloneSpawns.Values)
				{
					if (value2 != null && value2.SpawnableCrate != null && value2.SpawnableCrate.Barcode.ID == e.barcode)
					{
						value = value2;
					}
				}
			}
			if (value == null)
			{
				if (seatWaiting == null)
				{
					seatWaiting = e;
					seatWaitUntil = Time.time + 4f;
					if (cloneSpawns.Count == 0 && clonePending.Count == 0 && e.barcode.Length > 0)
					{
						Vector3 where = ((Head != null) ? Head.position : Feet);
						Quaternion faces = Quaternion.identity;
						PlaceBigThing(e, ref where, ref faces);
						int num = -1000 - cloneStandIns.Count - 500;
						CloneSpawn(num, e.barcode, where, faces);
						lessonStandIns[key] = num;
					}
				}
				else if (Time.time > seatWaitUntil)
				{
					seatWaiting = null;
					Lab.Event("lesson: nothing of its own to sit in");
				}
			}
			else
			{
				if (Time.time < seatSitAt)
				{
					return;
				}
				seatWaiting = null;
				Seat[] array = value.GetComponentsInChildren<Seat>(includeInactive: true);
				Seat seat = ((e.grip >= 0 && e.grip < array.Length) ? array[e.grip] : ((array.Length != 0) ? array[0] : null));
				if (seat == null)
				{
					return;
				}
				Vector3 vector = ((rig.physicsRig.centerOfPressure != null) ? rig.physicsRig.centerOfPressure.position : Feet);
				if (Flat(seat.transform.position - vector).magnitude > 3.5f)
				{
					Vector3 vector2 = seat.transform.position - Flat(seat.transform.right).normalized * 1.1f;
					if (Physics.Raycast(vector2 + Vector3.up * 1.5f, Vector3.down, out var hitInfo, 4f, -1, QueryTriggerInteraction.Ignore))
					{
						vector2 = hitInfo.point;
					}
					rig.Teleport(vector2);
					lessonPelvisHas = false;
					if (seatMoves < 2)
					{
						seatMoves++;
						seatWaiting = e;
						seatWaitUntil = Time.time + 4f;
						seatSitAt = Time.time + 0.35f;
						return;
					}
				}
				seatMoves = 0;
				if (lessonCar != value.transform)
				{
					if (lessonCar != null)
					{
						IgnoreCar(lessonCar, ignore: false);
					}
					lessonCar = value.transform;
					IgnoreCar(lessonCar, ignore: true);
				}
				seat.IngressRig(rig);
				seatAsked = seat;
				seatAskedAt = Time.time;
				seatAskedOf = e;
				Lab.Event("lesson: sits down in the " + value.name);
			}
		}
		catch (System.Exception ex)
		{
			Lab.Event("lesson: could not sit: " + ex.Message);
		}
	}

	public bool UseLesson(Lessons.Lesson lesson, bool atSpot, float startAt, int loops, (Vector3 foot, Vector3 inward)? wall)
	{
		if (lesson == null || lesson.take == null || lesson.take.frames.Count < 2 || dead || (enemy && !wall.HasValue) || climbing)
		{
			return false;
		}
		Rigidbody rigidbody = Pelvis();
		Transform head = Head;
		if (rigidbody == null || head == null)
		{
			return false;
		}
		Mode mode = (InLesson ? lessonReturn : ((this.mode != Mode.Clone) ? this.mode : Mode.Stand));
		if (WeaponInHand)
		{
			Stow();
		}
		StopPlaying();
		ReleaseCloneHands();
		SetMode(Mode.Clone);
		FreeHand(LeftHand);
		FreeHand(RightHand);
		cloneTake = lesson.take;
		cloneLesson = lesson;
		cloneLoop = false;
		cloneDressing = false;
		lessonReturn = mode;
		lastLessonItem = lesson.item;
		cloneStart = Time.time - startAt;
		lessonLoops = loops;
		lessonTopY = (wall.HasValue ? wallTopY : float.MinValue);
		lessonThings.Clear();
		lessonStandIns.Clear();
		seatWaiting = null;
		seatAsked = null;
		seatRetries = 0;
		seatMoves = 0;
		seatSitAt = 0f;
		lessonBig.Clear();
		lessonBigAsked.Clear();
		lessonLoopsDone = 0;
		lessonLanding = false;
		wallHeld[0] = (wallHeld[1] = false);
		driveMeant = (driving = false);
		placeAhead = Vector3.zero;
		placeAtSet = false;
		if (wall.HasValue)
		{
			lessonWallFoot = wall.Value.foot;
			lessonWallIn = wall.Value.inward;
		}
		lessonSeamAt = -10f;
		lessonPurposeful = wall.HasValue;
		lessonGliding = false;
		cloneEvent = 0;
		cloneFrame = 0;
		cloneDriven = -1;
		cloneSpawns.Clear();
		clonePending.Clear();
		cloneGrabs.Clear();
		cloneStandIns.Clear();
		lessonRootSet = false;
		lessonPelvisHas = false;
		lessonJumpAt = -1;
		lessonSenseAt = Time.time + 1f;
		lessonJumps.Clear();
		lessonTurnsSeen = 0;
		System.Collections.Generic.List<Clone.Frame> frames = lesson.take.frames;
		for (int i = 1; i < frames.Count; i++)
		{
			if ((frames[i].pelvis - frames[i - 1].pelvis).magnitude > 1.5f)
			{
				lessonJumps.Add(frames[i].t);
			}
		}
		Clone.Frame frame = lesson.take.frames[0];
		cloneFrom = frame.pelvis;
		if (atSpot)
		{
			cloneTo = cloneFrom;
			cloneYaw = 0f;
		}
		else
		{
			cloneTo = rigidbody.position;
			cloneYaw = Mathf.DeltaAngle(Recorder.Yaw(frame.playspace * frame.head.rotation * Vector3.forward), Recorder.Yaw(head.forward));
		}
		if (wall.HasValue && !atSpot)
		{
			Clone.Frame frame2 = FrameAt(lesson.take, lesson.riseAt);
			cloneFrom = frame2.pelvis;
			cloneTo = new Vector3(wall.Value.foot.x, rigidbody.position.y, wall.Value.foot.z);
			cloneYaw = Mathf.DeltaAngle(Recorder.Yaw(frame2.playspace * frame2.head.rotation * Vector3.forward), Recorder.Yaw(wall.Value.inward));
		}
		float num = StandingEyes();
		cloneSize = ((lesson.take.eyes > 0.3f && num > 0.3f) ? Mathf.Clamp(num / lesson.take.eyes, 0.35f, 3f) : 1f);
		Lab.Event("lesson: using the " + lesson.name + " as you did (" + lesson.length.ToString("0.0") + " s" + (atSpot ? ", where you did" : (", where it stands, turned " + cloneYaw.ToString("0") + " deg")) + ", size " + cloneSize.ToString("0.00") + ")");
		return true;
	}

	private static Clone.Frame FrameAt(Clone.Take take, float t)
	{
		System.Collections.Generic.List<Clone.Frame> frames = take.frames;
		int i;
		for (i = 0; i < frames.Count - 1 && frames[i + 1].t <= t; i++)
		{
		}
		return frames[i];
	}

	private void LessonLoop()
	{
		if (cloneLesson == null)
		{
			return;
		}
		float num = Time.time - cloneStart;
		Rigidbody rigidbody = Pelvis();
		if (lessonPurposeful && lessonTopY > float.MinValue && cloneLesson.purpose == "climb" && rig.physicsRig.centerOfPressure != null && rigidbody != null)
		{
			float y = rig.physicsRig.centerOfPressure.position.y;
			if (cloneLesson.item == "wall" && num > cloneLesson.riseAt && lessonTopY - y <= Tuning.F("wall.finish", 1.55f) && lessonTopY - y > 0.3f)
			{
				FinishOverEdge();
				return;
			}
			if (num > cloneLesson.riseAt && num < cloneLesson.topAt - 0.2f && y + cloneLesson.overRise >= lessonTopY + 0.1f)
			{
				cloneStart -= cloneLesson.topAt - num;
				LessonFromHere(FrameAt(cloneTake, cloneLesson.topAt).pelvis, rigidbody.position);
				lessonLoops = 0;
				cloneFrame = 0;
				cloneDriven = -1;
				lessonTurnsSeen = 0;
				cloneEvent = 0;
				while (cloneEvent < cloneTake.events.Count && cloneTake.events[cloneEvent].t < cloneLesson.topAt)
				{
					cloneEvent++;
				}
				seamHead = lastHeadPose;
				seamLeft = lastLeftPose;
				seamRight = lastRightPose;
				lessonSeamAt = Time.time;
				Lab.Event("lesson: at the top of its wall, on to getting over it");
				return;
			}
			if (cloneLesson.loopRise > 0.2f && num >= cloneLesson.loopTo && num < cloneLesson.loopTo + 0.5f)
			{
				lessonLoops = ((y + cloneLesson.afterLoop < lessonTopY - 0.15f && lessonLoopsDone < 60) ? 1 : 0);
			}
		}
		if (lessonLoops > 0 && !(cloneLesson.loopRise <= 0f) && !(num < cloneLesson.loopTo))
		{
			lessonLoops--;
			lessonLoopsDone++;
			cloneStart += cloneLesson.loopTo - cloneLesson.loopFrom;
			Rigidbody rigidbody2 = Pelvis();
			if (rigidbody2 != null)
			{
				LessonFromHere(FrameAt(cloneTake, cloneLesson.loopFrom).pelvis, rigidbody2.position);
			}
			cloneEvent = 0;
			while (cloneEvent < cloneTake.events.Count && cloneTake.events[cloneEvent].t < cloneLesson.loopFrom)
			{
				cloneEvent++;
			}
			cloneFrame = 0;
			cloneDriven = -1;
			lessonTurnsSeen = 0;
			seamHead = lastHeadPose;
			seamLeft = lastLeftPose;
			seamRight = lastRightPose;
			lessonSeamAt = Time.time;
			Lab.Event("lesson: that stretch again (" + lessonLoops + " more to go)");
		}
	}

	private void WallHands()
	{
		if (cloneLesson == null || cloneLesson.item != "wall" || !lessonPurposeful || lessonTopY == float.MinValue)
		{
			wallHeld[0] = (wallHeld[1] = false);
			return;
		}
		Transform headset = controls.headset;
		Vector3 vector = lessonWallFoot + lessonWallIn * Tuning.F("tool.climb.stand", 0.45f);
		BaseController[] array = new BaseController[2] { controls.leftController, controls.rightController };
		for (int i = 0; i < 2; i++)
		{
			if (wallHeld[i] && !(array[i] == null))
			{
				Transform transform = array[i].transform;
				if (!wallPinned[i])
				{
					Vector3 position = transform.position;
					position -= lessonWallIn * (Vector3.Dot(position - vector, lessonWallIn) + Tuning.F("wall.off", 0.06f));
					position.y = Mathf.Min(position.y, lessonTopY - 0.03f);
					wallPin[i] = position;
					wallPinned[i] = true;
				}
				if (headset != null && (wallPin[i] - headset.position).magnitude > Tuning.F("wall.arm", 0.85f) && wallPin[i].y < headset.position.y - 0.3f)
				{
					wallHeld[i] = false;
				}
				else if (!Bad(wallPin[i]))
				{
					transform.position = wallPin[i];
				}
			}
		}
	}

	private void FinishOverEdge()
	{
		Vector3 position = rig.physicsRig.centerOfPressure.position;
		float height = lessonTopY - position.y;
		Vector3 vector = lessonWallIn;
		Vector3 vector2 = lessonWallFoot + vector * Tuning.F("tool.climb.stand", 0.45f);
		Vector3 position2 = new Vector3(vector2.x, lessonTopY, vector2.z);
		Pose pose = ((Head != null) ? Pose.Of(Head) : new Pose(position2, Quaternion.LookRotation(vector, Vector3.up)));
		Pose pose2 = ((LeftHand != null) ? Pose.Of(LeftHand.transform) : pose);
		Pose pose3 = ((RightHand != null) ? Pose.Of(RightHand.transform) : pose);
		EndLesson("at the edge of its wall (" + height.ToString("0.0") + " m to the top): up onto it its own way");
		lessonDueAt = Time.time + 20f;
		nav.Clear();
		climbing = true;
		climbLifting = false;
		climbGliding = false;
		climbPhase = 1;
		climbEdge = position2;
		climbIn = vector;
		climbHeight = height;
		climbFacing = Quaternion.LookRotation(vector, Vector3.up);
		climbSince = Time.time;
		climbMove = ClimbMoves.Pick(height);
		climbLastEdge = position2;
		climbT = 0f;
		climbReach = 0f;
		climbWait = 0f;
		climbFrom = this.pose;
		climbStartHead = pose;
		climbStartLeft = pose2;
		climbStartRight = pose3;
		bodyYaw = Recorder.Yaw(vector);
		walk = Vector2.zero;
		climbHang = true;
		climbHangFeet = position;
	}

	private Rigidbody Pelvis()
	{
		if (!(rig.physicsRig != null) || !(rig.physicsRig.m_pelvis != null))
		{
			return null;
		}
		return rig.physicsRig.m_pelvis.GetComponent<Rigidbody>();
	}

	private Vector3 LessonPlace(Vector3 p)
	{
		if (cloneLesson != null)
		{
			return cloneTo + Quaternion.Euler(0f, cloneYaw, 0f) * (p - cloneFrom);
		}
		return p;
	}

	private Quaternion LessonTurn(Quaternion q)
	{
		if (cloneLesson != null)
		{
			return Quaternion.Euler(0f, cloneYaw, 0f) * q;
		}
		return q;
	}

	private bool LessonPortalTurns()
	{
		System.Collections.Generic.List<Clone.Frame> frames = cloneTake.frames;
		bool flag = false;
		for (int i = Mathf.Max(1, lessonTurnsSeen + 1); i <= cloneFrame + 1 && i < frames.Count; i++)
		{
			lessonTurnsSeen = i;
			float num = Mathf.DeltaAngle(frames[i - 1].playspace.eulerAngles.y, frames[i].playspace.eulerAngles.y);
			if (Mathf.Abs(num) < 12f)
			{
				continue;
			}
			bool flag2 = false;
			foreach (float lessonJump in lessonJumps)
			{
				flag2 |= Mathf.Abs(lessonJump - frames[i].t) < 0.3f;
			}
			if (flag2)
			{
				cloneYaw -= num;
				flag |= i == cloneFrame + 1;
			}
		}
		if (lessonTurnsSeen == cloneFrame + 1 && cloneFrame + 1 < frames.Count && Mathf.Abs(Mathf.DeltaAngle(frames[cloneFrame].playspace.eulerAngles.y, frames[cloneFrame + 1].playspace.eulerAngles.y)) >= 12f)
		{
			foreach (float lessonJump2 in lessonJumps)
			{
				flag |= Mathf.Abs(lessonJump2 - frames[cloneFrame + 1].t) < 0.3f;
			}
		}
		return flag;
	}

	private void LessonFromHere(Vector3 there, Vector3 here)
	{
		cloneFrom = there;
		cloneTo = here;
	}

	private void EndLesson(string how)
	{
		Lab.Event("lesson: " + how);
		lessonGliding = false;
		lessonDueAt = Mathf.Max(lessonDueAt, Time.time + Random(25f, 50f));
		foreach (Poolee value in cloneSpawns.Values)
		{
			Lessons.Litter(value);
		}
		Mode mode = lessonReturn;
		SetMode(mode);
		Transform head = Head;
		if (head != null)
		{
			bodyYaw = Recorder.Yaw(head.forward);
		}
		pose = Standing(0f, 0f, 0f, 0f);
		if (mode == Mode.Wander || mode == Mode.Brain)
		{
			Idle(Random(1.5f, 4f));
		}
	}

	private void LeaveClone()
	{
		lessonLanding = false;
		try
		{
			if (rig.activeSeat != null)
			{
				rig.activeSeat.EgressRig(immediate: true);
			}
		}
		catch (System.Exception)
		{
		}
		CarryThings(on: false);
		driveMeant = (driving = false);
		ReleaseCloneHands();
		cloneTake = null;
		cloneLesson = null;
		lessonNext = null;
		ClearButtons(controls.leftController);
		ClearButtons(controls.rightController);
		try
		{
			rig.remapHeptaRig._crouchTarget = 0f;
			rig.remapHeptaRig._feetOffset = 0f;
		}
		catch (System.Exception)
		{
		}
	}

	private static void ClearButtons(BaseController c)
	{
		if (!(c == null))
		{
			c._primaryAxis = 0f;
			bool flag2 = (c._primaryInteractionButtonUp = false);
			bool primaryInteractionButton = (c._primaryInteractionButtonDown = flag2);
			c._primaryInteractionButton = primaryInteractionButton;
			flag2 = (c._secondaryInteractionButtonUp = false);
			primaryInteractionButton = (c._secondaryInteractionButtonDown = flag2);
			c._secondaryInteractionButton = primaryInteractionButton;
			flag2 = (c._aButtonUp = false);
			primaryInteractionButton = (c._aButtonDown = flag2);
			c._aButton = primaryInteractionButton;
			flag2 = (c._bButtonUp = false);
			primaryInteractionButton = (c._bButtonDown = flag2);
			c._bButton = primaryInteractionButton;
			flag2 = (c._thumbstickUp = false);
			primaryInteractionButton = (c._thumbstickDown = flag2);
			c._thumbstick = primaryInteractionButton;
			flag2 = (c._touchPadUp = false);
			primaryInteractionButton = (c._touchPadDown = flag2);
			c._touchPad = primaryInteractionButton;
			c._menuTap = false;
			c._touchPadAxis = Vector2.zero;
		}
	}

	private void CarryThings(bool on)
	{
		if (on)
		{
			if (carried.Count > 0)
			{
				return;
			}
			Hand[] array = new Hand[2] { LeftHand, RightHand };
			for (int i = 0; i < 2; i++)
			{
				Grip grip = cloneHeld[i];
				Hand hand = array[i];
				Rigidbody rigidbody = ((grip != null) ? grip.GetComponentInParent<Rigidbody>() : null);
				if (hand == null || rigidbody == null)
				{
					continue;
				}
				bool flag = false;
				foreach (var item2 in carried)
				{
					flag |= item2.body == rigidbody;
				}
				Pose item = Pose.Of(hand.transform).Inverse() * Pose.Of(rigidbody.transform);
				if (hand.m_CurrentAttachedGO != null)
				{
					FreeHand(hand);
				}
				if (!flag)
				{
					carried.Add((rigidbody, hand, grip, item, rigidbody.isKinematic));
					rigidbody.isKinematic = true;
				}
			}
			if (carried.Count > 0)
			{
				Lab.Event("lesson: its " + carried.Count + " thing(s) carried in its hands while it is taken up");
			}
			return;
		}
		foreach (var item3 in carried)
		{
			try
			{
				if (!(item3.body == null))
				{
					item3.body.isKinematic = item3.wasKinematic;
					item3.body.velocity = Vector3.zero;
					item3.body.angularVelocity = Vector3.zero;
					if (item3.hand != null && item3.grip != null && item3.hand.m_CurrentAttachedGO == null && mode == Mode.Clone && cloneLesson != null)
					{
						Grab(item3.hand, item3.grip);
					}
				}
			}
			catch (System.Exception)
			{
			}
		}
		carried.Clear();
	}

	private void CarriedFollow()
	{
		foreach (var item in carried)
		{
			if (!(item.body == null) && !(item.hand == null))
			{
				Pose pose = Pose.Of(item.hand.transform) * item.inHand;
				if (!Bad(pose.position) && !Bad(pose.rotation))
				{
					item.body.position = pose.position;
					item.body.rotation = pose.rotation;
					item.body.transform.SetPositionAndRotation(pose.position, pose.rotation);
				}
			}
		}
	}

	private void LessonKeepHold()
	{
		CarriedFollow();
		if (Time.time < regrabAt || carried.Count > 0)
		{
			return;
		}
		regrabAt = Time.time + 0.3f;
		Hand[] array = new Hand[2] { LeftHand, RightHand };
		for (int i = 0; i < 2; i++)
		{
			Grip grip = cloneHeld[i];
			Hand hand = array[i];
			if (grip == null || hand == null || hand.m_CurrentAttachedGO != null)
			{
				continue;
			}
			try
			{
				Rigidbody componentInParent = grip.GetComponentInParent<Rigidbody>();
				if (componentInParent != null)
				{
					Vector3 vector = grip.transform.position - componentInParent.position;
					componentInParent.velocity = Vector3.zero;
					componentInParent.angularVelocity = Vector3.zero;
					componentInParent.position = hand.transform.position - vector;
					componentInParent.transform.position = hand.transform.position - vector;
				}
				if (Grab(hand, grip))
				{
					Lab.Event("lesson: the " + grip.name + " had come out of its " + ((i == 0) ? "left" : "right") + " hand, taken again");
				}
			}
			catch (System.Exception)
			{
			}
		}
	}

	private void LessonWatch()
	{
		if (lessonReturn == Mode.Brain && !(Time.time < lessonSenseAt) && !lessonPurposeful)
		{
			lessonSenseAt = Time.time + 0.5f;
			Sense();
			if (nearestSeen.transform != null && nearestSeenDistance < 25f)
			{
				EndLesson("broken off: an enemy " + nearestSeenDistance.ToString("0") + " m off");
			}
		}
	}

	private bool LessonUnsafe(Hand hand)
	{
		GameObject gameObject = ((hand != null) ? hand.m_CurrentAttachedGO : null);
		Gun gun = ((gameObject != null) ? gameObject.GetComponentInParent<Gun>() : null);
		Transform head = Player.Head;
		if (gun == null || gun.firePointTransform == null || head == null)
		{
			return false;
		}
		Vector3 to = head.position - Vector3.up * 0.3f - gun.firePointTransform.position;
		if (to.magnitude < 60f)
		{
			return Vector3.Angle(gun.firePointTransform.forward, to) < Mathf.Max(10f, Mathf.Atan2(0.6f, to.magnitude) * 57.29578f);
		}
		return false;
	}

	private Clone.HandInput LessonInput(bool left, Clone.Frame now, out bool tapped)
	{
		Clone.HandInput result = (left ? now.leftIn : now.rightIn);
		tapped = false;
		System.Collections.Generic.List<Clone.Frame> frames = cloneTake.frames;
		for (int i = Mathf.Max(cloneDriven + 1, cloneFrame - 30); i <= cloneFrame && i < frames.Count; i++)
		{
			Clone.HandInput handInput = (left ? frames[i].leftIn : frames[i].rightIn);
			tapped |= handInput.menu;
			if (i == cloneFrame)
			{
				break;
			}
			result.primary |= handInput.primary;
			result.secondary |= handInput.secondary;
			result.a |= handInput.a;
			result.b |= handInput.b;
			result.click |= handInput.click;
			result.pad |= handInput.pad;
		}
		return result;
	}

	private static void Edge(bool now, bool was, out bool down, out bool up)
	{
		down = now && !was;
		up = !now && was;
	}

	private void PutButtons(BaseController c, Clone.HandInput h, bool tapped, bool hold)
	{
		if (!(c == null))
		{
			bool secondaryInteractionButton = c._secondaryInteractionButton;
			Edge(h.secondary, secondaryInteractionButton, out var flag, out var up);
			c._secondaryInteractionButton = h.secondary;
			c._secondaryInteractionButtonDown = flag;
			c._secondaryInteractionButtonUp = up;
			Edge(h.a, c._aButton, out flag, out up);
			c._aButton = h.a;
			c._aButtonDown = flag;
			c._aButtonUp = up;
			Edge(h.b, c._bButton, out flag, out up);
			c._bButton = h.b;
			c._bButtonDown = flag;
			c._bButtonUp = up;
			Edge(h.pad, c._touchPad, out flag, out up);
			c._touchPad = h.pad;
			c._touchPadDown = flag;
			c._touchPadUp = up;
			c._touchPadAxis = h.padAxis;
			c._menuTap = tapped;
			if (c == controls.rightController)
			{
				Edge(h.click, c._thumbstick, out flag, out up);
				c._thumbstick = h.click;
				c._thumbstickDown = flag;
				c._thumbstickUp = up;
				c._thumbstickAxis = Vector2.zero;
			}
		}
	}

	public void LessonSoon()
	{
		lessonDueAt = Time.time + Random(4f, 16f);
	}

	private void MaybeLesson()
	{
		if (Lessons.Count == 0 || enemy || Tuning.F("lesson.every", 90f) <= 0f)
		{
			return;
		}
		if (lessonDueAt < 0f)
		{
			lessonDueAt = Time.time + Random(20f, 60f);
		}
		if (!(Time.time < lessonDueAt) && (act != Act.Go || planSight != Sight.Tall) && !Playing && !climbing && !Reloading && !portalGoing && !falling && Time.time >= waveUntil && nearestSeen.transform == null && brainTarget.transform == null && (mode == Mode.Wander || mode == Mode.Brain || (mode == Mode.Follow && !following)))
		{
			lessonDueAt = Time.time + Tuning.F("lesson.every", 90f) * Random(0.6f, 1.6f);
			Lessons.Lesson lesson = Lessons.Pick(lastLessonItem);
			if (lesson != null && (BotManager.OwnSpawns || !NeedsThings(lesson)))
			{
				UseLesson(lesson, atSpot: false);
			}
		}
	}

	private bool BeginLessonAct(Vector3 feet)
	{
		lessonNext = Lessons.Pick(lastLessonItem);
		if (lessonNext == null || enemy || (!BotManager.OwnSpawns && NeedsThings(lessonNext)))
		{
			lessonNext = null;
			return false;
		}
		Vector3 startFeet = lessonNext.take.startFeet;
		lessonAtSpot = lessonNext.level.Length > 0 && lessonNext.level == Lessons.Level && Flat(startFeet - feet).magnitude < Tuning.F("lesson.spot", 60f) && Mathf.Abs(startFeet.y - feet.y) < 6f && UnityEngine.Random.value < Tuning.F("lesson.spot.chance", 0.6f);
		if (lessonAtSpot)
		{
			nav.SetGoal(startFeet);
		}
		actUntil = Time.time + 45f;
		return true;
	}

	private void LessonAct(float dt)
	{
		if (lessonNext == null)
		{
			Idle(Random(1f, 3f));
			return;
		}
		if (lessonAtSpot)
		{
			running = false;
			if (!Travel(MotionLibrary.WalkPush, 0.5f, dt) && nav.StuckCount < 3)
			{
				return;
			}
		}
		walk = Vector2.zero;
		Lessons.Lesson lesson = lessonNext;
		bool atSpot = lessonAtSpot && Flat(lesson.take.startFeet - Feet).magnitude < 1.5f;
		lessonNext = null;
		nav.Clear();
		if (!UseLesson(lesson, atSpot))
		{
			Idle(Random(1f, 3f));
		}
	}

	private string ActLabel()
	{
		if (climbing)
		{
			return "climbing up";
		}
		if (Playing)
		{
			if (play != Play.Shoot)
			{
				if (!tinkering)
				{
					return "playing with something";
				}
				return "trying out the " + tinkerName;
			}
			return "shooting practice";
		}
		if (Time.time < waveUntil)
		{
			return "waving";
		}
		if (fid != 0)
		{
			return FidgetLabel();
		}
		switch (act)
		{
		case Act.Idle:
			return "looking around";
		case Act.Sit:
			return "sitting down for a rest";
		case Act.Stroll:
			return "strolling";
		case Act.Jog:
			return "jogging";
		case Act.Sprint:
			return "sprinting";
		case Act.Climb:
			return "going up high";
		case Act.Visit:
			return "coming to see you";
		case Act.Explore:
			return "exploring somewhere new";
		case Act.Go:
			if (!thinking2)
			{
				if (!(Time.time < pauseUntil))
				{
					return planWhy;
				}
				return "stopping to look";
			}
			return "thinking... " + planWhy;
		case Act.Use:
			if (lessonNext == null)
			{
				return "";
			}
			return "going to use the " + lessonNext.name;
		default:
			return "";
		}
	}

	private void Life(Transform you, Transform head, float dt)
	{
		visited.Add(Cell(Feet));
		if (Playing)
		{
			running = false;
			if (!PlayStep(you, head, dt))
			{
				Idle(Random(1.5f, 4f));
			}
			return;
		}
		if (!climbing && Time.time >= actUntil)
		{
			NextAct(you);
		}
		if (act == Act.Go)
		{
			GoStep(you, head, dt);
			return;
		}
		switch (act)
		{
		case Act.Idle:
			walk = Vector2.zero;
			LookAbout(you, head, dt);
			return;
		case Act.Use:
			headYaw = 0f;
			LessonAct(dt);
			return;
		case Act.Sit:
			walk = Vector2.zero;
			running = false;
			SitFrame();
			LookAbout(you, head, dt);
			return;
		case Act.Tinker:
			walk = Vector2.zero;
			if (!TinkerPending())
			{
				Idle(Random(1f, 3f));
			}
			return;
		}
		headYaw = 0f;
		if (!nav.HasGoal && !climbing)
		{
			Idle(Random(1f, 3f));
			return;
		}
		running = act == Act.Sprint || (act == Act.Explore && personality.energy > 1.1f);
		float speed = (running ? MotionLibrary.RunPush : ((act == Act.Stroll) ? Tuning.F("life.stroll", 0.45f) : MotionLibrary.WalkPush));
		if (Travel(speed, (act == Act.Visit) ? (BotManager.FollowDistance + 0.5f) : 0.8f, dt) || nav.StuckCount >= 3)
		{
			running = false;
			nav.Clear();
			if (act == Act.Climb)
			{
				Lab.Event("life: up high");
			}
			Idle((act == Act.Climb) ? Random(4f, 10f) : (Random(1.5f, 5f) * personality.calm));
		}
	}

	private void Idle(float seconds)
	{
		act = Act.Idle;
		actUntil = Time.time + seconds;
		walk = Vector2.zero;
		running = false;
	}

	private void LookAbout(Transform you, Transform head, float dt)
	{
		if (((you != null && head != null) ? Flat(you.position - head.position).magnitude : 99f) < 6f)
		{
			float value = Watch(you, head, dt, 90f);
			headYaw = Mathf.MoveTowards(headYaw, Mathf.Clamp(value, -60f, 60f), 120f * dt);
		}
		else
		{
			headYaw = Mathf.Sin((Time.time - born) * 0.5f) * 45f + Mathf.Sin((Time.time - born) * 0.17f) * 20f;
		}
	}

	private void NextAct(Transform you)
	{
		Vector3 vector = ((rig.physicsRig != null && rig.physicsRig.centerOfPressure != null) ? rig.physicsRig.centerOfPressure.position : Feet);
		bool flag = gun != null;
		if (you != null)
		{
			_ = Flat(you.position - vector).magnitude < 25f;
		}
		else
			_ = 0;
		(Act, float)[] array = new(Act, float)[7]
		{
			(Act.Go, Tuning.F("life.go", 4f)),
			(Act.Idle, 0.45f * personality.calm),
			(Act.Sit, (!enemy) ? (Tuning.F("life.sit", 0.6f) * personality.calm) : 0f),
			(Act.Play, 0.5f * personality.curiosity),
			(Act.Shoot, (flag || BotManager.OwnSpawns) ? (0.6f * personality.shooter) : 0f),
			(Act.Use, (Lessons.Count > 0 && !enemy) ? (Tuning.F("life.use", 0.8f) * personality.curiosity) : 0f),
			(Act.Tinker, (!enemy && !flag) ? (Tuning.F("life.tinker", 1.7f) * personality.curiosity) : 0f)
		};
		if (tinkerWant != null && !enemy && Begin(Act.Tinker, you, vector))
		{
			lastAct = Act.Tinker;
			Lab.Event("life: Tinker (heard of it)");
			return;
		}
		for (int i = 0; i < 4; i++)
		{
			float num = 0f;
			(Act, float)[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				var (act, num2) = array2[j];
				if (act != lastAct || act == Act.Go)
				{
					num += num2;
				}
			}
			float num3 = UnityEngine.Random.value * num;
			Act what = Act.Go;
			array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				var (act2, num4) = array2[j];
				if (act2 != lastAct || act2 == Act.Go)
				{
					num3 -= num4;
					if (num3 <= 0f)
					{
						what = act2;
						break;
					}
				}
			}
			if (Begin(what, you, vector))
			{
				lastAct = what;
				Lab.Event("life: " + what);
				return;
			}
		}
		Idle(Random(2f, 5f));
	}

	private bool Begin(Act what, Transform you, Vector3 feet)
	{
		nav.Clear();
		act = what;
		actUntil = Time.time + 30f;
		if (what == Act.Stroll && UnityEngine.Random.value < Tuning.F("life.sneak", 0.22f))
		{
			Squat(Random(3f, 6f), 0.5f);
		}
		float num;
		float num2;
		Vector3 place3;
		switch (what)
		{
		case Act.Idle:
			Idle(Random(3f, 8f) * personality.calm);
			if (UnityEngine.Random.value < Tuning.F("life.squat", 0.5f))
			{
				StartCrouchOfTheirs();
			}
			return true;
		case Act.Sprint:
			num = Random(15f, 35f);
			goto IL_010e;
		case Act.Jog:
			num = Random(10f, 22f);
			goto IL_010e;
		case Act.Stroll:
			num = Random(4f, 12f);
			goto IL_010e;
		case Act.Visit:
		{
			if (you == null || !Navigator.RandomPlace(you.position - Vector3.up * 1.4f, 1.5f, 3f, out var place))
			{
				return false;
			}
			nav.SetGoal(place);
			return true;
		}
		case Act.Tinker:
			return BeginTinker(feet);
		case Act.Sit:
			actUntil = Time.time + Random(Tuning.F("life.sit.least", 20f), Tuning.F("life.sit.most", 60f));
			return true;
		case Act.Go:
			return BeginGo(you, feet);
		case Act.Climb:
		{
			if (Time.time >= labClimbUntil)
			{
				return false;
			}
			if (UnityEngine.Random.value < 0.35f && SelfTaught.KnownTop(feet, 50f, out var top))
			{
				nav.SetGoal(top);
				Lab.Event("life: back up to a high place it knows");
				return true;
			}
			if (!FindHighGround(feet, out var top2))
			{
				return false;
			}
			nav.SetGoal(top2);
			return true;
		}
		case Act.Explore:
		{
			if (!FarPlace(feet, out var place2))
			{
				return false;
			}
			nav.SetGoal(place2);
			Lab.Event("life: exploring " + Flat(place2 - feet).magnitude.ToString("0") + " m off" + (visited.Contains(Cell(place2)) ? "" : ", somewhere new"));
			return true;
		}
		case Act.Use:
			return BeginLessonAct(feet);
		case Act.Play:
			if (UnityEngine.Random.value < Tuning.F("play.shove", 0.35f) && FindSomethingToShove())
			{
				return true;
			}
			return FindSomethingToPlayWith();
		case Act.Shoot:
			return GetAGun();
		default:
			{
				return false;
			}
			IL_010e:
			num2 = num;
			if (!Navigator.RandomPlace(feet, num2 * 0.6f, num2, out place3))
			{
				return false;
			}
			for (int i = 0; i < 4; i++)
			{
				if (SelfTaught.BadAt(place3) < 2)
				{
					break;
				}
				if (Navigator.RandomPlace(feet, num2 * 0.6f, num2, out var place4))
				{
					place3 = place4;
				}
			}
			nav.SetGoal(place3);
			return true;
		}
	}

	private bool FarPlace(Vector3 feet, out Vector3 place)
	{
		place = feet;
		float num = float.MinValue;
		float a = Tuning.F("explore.near", 20f);
		float num2 = Tuning.F("explore.far", 60f);
		for (int i = 0; i < 28; i++)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle.normalized * Random(a, num2);
			Vector3 vector2 = feet + new Vector3(vector.x, 0f, vector.y);
			Vector3 ground;
			try
			{
				if (NavMesh.SamplePosition(vector2, out var hit, 4f, -1))
				{
					ground = Navigator.Surface(hit.position, feet.y);
				}
				else if (!Ground(vector2, out ground))
				{
					continue;
				}
			}
			catch (System.Exception)
			{
				if (!Ground(vector2, out ground))
				{
					continue;
				}
			}
			float num3 = (visited.Contains(Cell(ground)) ? 0f : 3f) + Flat(ground - feet).magnitude / num2 + UnityEngine.Random.value * 0.6f - Mathf.Abs(ground.y - feet.y) * 0.02f - (float)SelfTaught.BadAt(ground) * 1.5f;
			if (num3 > num)
			{
				num = num3;
				place = ground;
			}
		}
		return num > float.MinValue;
	}

	private static bool Ground(Vector3 p, out Vector3 ground)
	{
		ground = p;
		if (!Physics.Raycast(p + Vector3.up * 25f, Vector3.down, out var hitInfo, 60f, -1, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		if (hitInfo.normal.y < 0.65f || hitInfo.collider.attachedRigidbody != null)
		{
			return false;
		}
		if (Physics.CheckCapsule(hitInfo.point + Vector3.up * 0.4f, hitInfo.point + Vector3.up * 1.6f, 0.25f, -1, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		ground = hitInfo.point;
		return true;
	}

	private bool FindHighGround(Vector3 feet, out Vector3 top)
	{
		top = Vector3.zero;
		float num = UnityEngine.Random.Range(0f, 360f);
		for (int i = 0; i < 16; i++)
		{
			Vector3 direction = Quaternion.Euler(0f, num + (float)i * 22.5f, 0f) * Vector3.forward;
			if (!Physics.Raycast(feet + Vector3.up * 0.6f, direction, out var hitInfo, 15f, -1, QueryTriggerInteraction.Ignore) || hitInfo.distance < 1.5f || hitInfo.collider.attachedRigidbody != null || hitInfo.collider.GetComponentInParent<RigManager>() != null)
			{
				continue;
			}
			Vector3 vector = -Flat(hitInfo.normal);
			if (vector.sqrMagnitude < 0.01f)
			{
				continue;
			}
			vector.Normalize();
			if (Physics.Raycast(hitInfo.point + vector * 0.6f + Vector3.up * 3.3f, Vector3.down, out var hitInfo2, 3.6f, -1, QueryTriggerInteraction.Ignore))
			{
				float num2 = hitInfo2.point.y - feet.y;
				if (!(hitInfo2.normal.y < 0.7f) && !(num2 < Tuning.F("climb.min", 1.2f)) && !(num2 > Tuning.F("climb.max", 2.5f)) && !Physics.Raycast(hitInfo2.point + Vector3.up * 0.05f, Vector3.up, 1.6f, -1, QueryTriggerInteraction.Ignore) && SelfTaught.ClimbFails(hitInfo2.point) < 2)
				{
					top = hitInfo2.point + Vector3.up * 0.05f;
					return true;
				}
			}
		}
		return false;
	}

	private static float Random(float a, float b)
	{
		return UnityEngine.Random.Range(a, b);
	}

	public void BecomePuppet(byte owner, byte id)
	{
		netOwner = owner;
		netId = id;
		netFrames.Clear();
		netOffsetSet = false;
		if (rig.transform.Find("ThirdEyeNetPuppet") == null)
		{
			new GameObject("ThirdEyeNetPuppet").transform.SetParent(rig.transform, worldPositionStays: false);
		}
	}

	public void NetWear(string avatar, Vector3 scale)
	{
		if (string.IsNullOrEmpty(avatar) || avatar == netWearing || NetBots.Now - netWoreAt < 4f)
		{
			return;
		}
		netWoreAt = NetBots.Now;
		if (BotRig.AvatarOf(rig) == avatar)
		{
			netWearing = avatar;
			return;
		}
		string asked = avatar;
		BotRig.UseAvatar(rig, avatar, scale, delegate
		{
			netWearing = asked;
		});
	}

	public byte[] NetPose()
	{
		if (rig.physicsRig == null || controls == null || controls.vrRoot == null || controls.headset == null || controls.leftController == null || controls.rightController == null)
		{
			return null;
		}
		Rigidbody rigidbody = Pelvis();
		if (rigidbody == null)
		{
			return null;
		}
		Clone.Frame frame = default(Clone.Frame);
		frame.t = NetBots.Now;
		frame.pelvis = rigidbody.position;
		frame.pelvisTurn = rigidbody.rotation;
		frame.pelvisVelocity = rigidbody.velocity;
		frame.playspace = controls.vrRoot.rotation;
		frame.head = new Pose(controls.headset.localPosition, controls.headset.localRotation);
		frame.left = new Pose(controls.leftController.transform.localPosition, controls.leftController.transform.localRotation);
		frame.right = new Pose(controls.rightController.transform.localPosition, controls.rightController.transform.localRotation);
		frame.leftIn = NetHand(controls.leftController);
		frame.rightIn = NetHand(controls.rightController);
		frame.crouch = ((rig.remapHeptaRig != null) ? rig.remapHeptaRig._crouchTarget : 0f);
		frame.feet = ((rig.remapHeptaRig != null) ? rig.remapHeptaRig._feetOffset : 0f);
		Clone.Frame f = frame;
		if (NetBots.Now - netOwnAt > 1f)
		{
			netOwnAt = NetBots.Now;
			netHolds[0] = NetHeld(LeftHand);
			netHolds[1] = NetHeld(RightHand);
		}
		return NetBots.Pack((byte)netId, dead, down, f, netHolds[0], netHolds[1]);
	}

	private static Clone.HandInput NetHand(BaseController c)
	{
		Clone.HandInput result = default(Clone.HandInput);
		result.index = c._processedIndex;
		result.middle = c._processedMiddle;
		result.ring = c._processedRing;
		result.pinky = c._processedPinky;
		result.thumb = c._processedThumb;
		result.grip = c._solvedGrip;
		result.trigger = c._primaryAxis;
		return result;
	}

	private static ushort NetHeld(Hand hand)
	{
		try
		{
			GameObject gameObject = ((hand != null) ? hand.m_CurrentAttachedGO : null);
			MarrowEntity marrowEntity = ((gameObject != null) ? gameObject.GetComponentInParent<MarrowEntity>() : null);
			return (marrowEntity != null) ? NetBots.Own(marrowEntity) : ushort.MaxValue;
		}
		catch (System.Exception)
		{
			return ushort.MaxValue;
		}
	}

	public void NetFrame(Clone.Frame f, bool deadThere, bool downThere, ushort leftHolds, ushort rightHolds)
	{
		float num = NetBots.Now - f.t;
		if (!netOffsetSet || Mathf.Abs(num - netOffset) > 0.5f)
		{
			netOffset = num;
			netOffsetSet = true;
			netFrames.Clear();
			netAt = 0;
		}
		else
		{
			netOffset = Mathf.Lerp(netOffset, num, 0.05f);
		}
		if (netFrames.Count > 0 && f.t <= netFrames[netFrames.Count - 1].t)
		{
			return;
		}
		netFrames.Add(f);
		if (netFrames.Count > 60)
		{
			netFrames.RemoveRange(0, 20);
			netAt = Mathf.Max(0, netAt - 20);
		}
		try
		{
			if (deadThere && !dead)
			{
				dead = true;
				diedAt = Time.time;
				Limp(on: true);
				Lab.Event("net: its owner's bot died");
			}
			else if (downThere != netDown && !dead)
			{
				netDown = downThere;
				Limp(netDown);
			}
			NetHolding(0, leftHolds);
			NetHolding(1, rightHolds);
		}
		catch (System.Exception)
		{
		}
	}

	private void NetHolding(int hand, ushort thing)
	{
		if (netHolds[hand] == thing)
		{
			return;
		}
		netHolds[hand] = thing;
		if (thing != ushort.MaxValue)
		{
			Transform transform = NetBots.Shared(thing);
			if (transform != null)
			{
				NoPushOnItself(transform, again: true);
			}
		}
	}

	private bool NetNow(out Clone.Frame a, out Clone.Frame b, out float k)
	{
		a = (b = default(Clone.Frame));
		k = 0f;
		if (netFrames.Count == 0)
		{
			return false;
		}
		float num = NetBots.Now - netOffset - 0.12f;
		if (netAt >= netFrames.Count || netFrames[netAt].t > num)
		{
			netAt = 0;
		}
		while (netAt < netFrames.Count - 2 && netFrames[netAt + 1].t <= num)
		{
			netAt++;
		}
		a = netFrames[netAt];
		b = netFrames[Mathf.Min(netAt + 1, netFrames.Count - 1)];
		k = ((b.t > a.t) ? Mathf.Clamp01((num - a.t) / (b.t - a.t)) : 0f);
		return true;
	}

	private void NetUpdate()
	{
		StayWhole();
		if (Whole)
		{
			KeepAwake();
		}
		if (!dead && !netDown)
		{
			HurtPlayer();
		}
	}

	public void NetHit(float damage, int part, bool bullet, byte from)
	{
		if (!dead && !(damage <= 0f))
		{
			damage = ((!bullet) ? Mathf.Min(damage, MaxHp * Tuning.F("bot.knock.most", 0.3f)) : BulletHarm(damage, (PlayerDamageReceiver.BodyPart)part));
			if (hp < 0f)
			{
				hp = MaxHp;
			}
			hp -= damage * (enemy ? Tuning.F("enemy.hurt", 1f) : Tuning.F("bot.hurt", 1f));
			Lab.Event("[" + Name + "] " + (enemy ? "enemy bot" : "bot") + " hit by player " + from + (bullet ? " (a bullet)" : " (a blow)") + ": " + damage.ToString("0.00") + ", " + Mathf.Max(hp, 0f).ToString("0.00") + " left");
			if (hp <= 0f)
			{
				Die();
				return;
			}
			Hurt(damage, null);
			TripByHit(damage, (PlayerDamageReceiver.BodyPart)part, null, bullet);
		}
	}

	private void NetStruck(float damage, Attack attack, PlayerDamageReceiver.BodyPart part)
	{
		if (dead || damage <= 0f || Time.time - netStruckAt < 0.05f)
		{
			return;
		}
		bool bullet = false;
		bool flag = false;
		try
		{
			if (attack != null)
			{
				bullet = attack.attackType.ToString().IndexOf("Pierc", System.StringComparison.OrdinalIgnoreCase) >= 0;
				RigManager rigManager = ((attack.proxy != null) ? attack.proxy.GetComponentInParent<RigManager>() : null);
				if (rigManager != null)
				{
					Bot bot = BotManager.Of(rigManager);
					flag = rigManager == Player.RigManager || (bot != null && !bot.Puppet);
				}
				else
				{
					Hand[] array = new Hand[2]
					{
						Player.LeftHand,
						Player.RightHand
					};
					foreach (Hand hand in array)
					{
						if (hand != null && hand.rb != null && (hand.transform.position - attack.origin).magnitude < 0.5f && hand.rb.velocity.magnitude > 1.2f)
						{
							flag = true;
						}
					}
				}
			}
		}
		catch (System.Exception)
		{
		}
		if (flag)
		{
			netStruckAt = Time.time;
			NetBots.SendHit((byte)netOwner, (byte)netId, damage, (int)part, bullet);
		}
	}

	private void NetDrive()
	{
		if (!netDown && NetNow(out var a, out var b, out var k))
		{
			Transform vrRoot = controls.vrRoot;
			rig.remapHeptaRig.inWeight = 1f;
			vrRoot.rotation = Quaternion.Slerp(a.playspace, b.playspace, k);
			Pose pose = Pose.Lerp(a.head, b.head, k);
			Pose pose2 = Pose.Lerp(a.left, b.left, k);
			Pose pose3 = Pose.Lerp(a.right, b.right, k);
			controls.headset.SetLocalPositionAndRotation(pose.position, pose.rotation);
			controls.leftController.transform.SetLocalPositionAndRotation(pose2.position, pose2.rotation);
			controls.rightController.transform.SetLocalPositionAndRotation(pose3.position, pose3.rotation);
			PutInput(controls.leftController, a.leftIn, b.leftIn, k, trigger: false);
			PutInput(controls.rightController, a.rightIn, b.rightIn, k, trigger: false);
			rig.remapHeptaRig._crouchTarget = Mathf.Lerp(a.crouch, b.crouch, k);
			rig.remapHeptaRig._feetOffset = Mathf.Lerp(a.feet, b.feet, k);
			controls.leftController._thumbstickAxis = Vector2.zero;
			controls._axisPrimary = Vector2.zero;
			bodyYaw = vrRoot.eulerAngles.y;
		}
	}

	public void NetFixed(float dt)
	{
		if (dead || !Whole || !NetNow(out var a, out var b, out var k))
		{
			return;
		}
		Rigidbody rigidbody = Pelvis();
		if (rigidbody == null)
		{
			return;
		}
		Vector3 vector = Vector3.Lerp(a.pelvis, b.pelvis, k);
		Vector3 vector2 = Vector3.Lerp(a.pelvisVelocity, b.pelvisVelocity, k);
		Vector3 vector3 = vector - rigidbody.position;
		if (vector3.magnitude > Tuning.F("net.snap", 2.5f))
		{
			try
			{
				rig.Teleport(rig.physicsRig.centerOfPressure.position + vector3, zeroVelocity: false);
				return;
			}
			catch (System.Exception)
			{
				return;
			}
		}
		float num = Tuning.F("net.pull", 14f);
		Vector3 vector4 = vector2 + new Vector3(vector3.x, 0f, vector3.z) * num - rigidbody.velocity;
		if (Mathf.Abs(vector2.y) < Tuning.F("net.vertical", 1.2f) && Mathf.Abs(vector3.y) < 0.5f && !netDown)
		{
			vector4.y = 0f;
		}
		else
		{
			vector4.y += vector3.y * num * 0.5f;
		}
		rigidbody.AddForce(Vector3.ClampMagnitude(vector4 * Tuning.F("net.push", 18f), 120f), ForceMode.Acceleration);
	}

	public static bool IsWeapon(Transform whole)
	{
		Rigidbody componentInChildren = whole.GetComponentInChildren<Rigidbody>();
		if (componentInChildren != null)
		{
			return IsMelee(componentInChildren);
		}
		return false;
	}

	public void Ready()
	{
		waveUntil = Time.time + 1.2f;
	}

	public void Ordered(Transform what)
	{
		Bot bot = null;
		foreach (Bot item in BotManager.All)
		{
			if (item.rig != null && item.rig.transform == what)
			{
				bot = item;
			}
		}
		Targets.Target target = default(Targets.Target);
		if (bot != null)
		{
			Targets.Target target2 = default(Targets.Target);
			target2.transform = bot.rig.transform;
			target2.aim = bot.Head;
			target2.offset = -Vector3.up * 0.3f;
			target2.hostile = true;
			target2.bot = bot;
			target = target2;
		}
		else
		{
			foreach (Targets.Target item2 in Targets.Hostiles())
			{
				if (item2.transform == what)
				{
					target = item2;
				}
			}
		}
		if (target.transform == null)
		{
			Lab.Event("orders: could not make out what was pointed at");
			return;
		}
		ordered = target;
		orderedUntil = Time.time + Tuning.F("orders.for", 60f);
		if (mode != Mode.Brain && mode != Mode.Fists)
		{
			SetMode(Mode.Brain);
		}
	}

	private void ObeyOrder(Vector3 eye)
	{
		if (ordered.transform == null)
		{
			return;
		}
		if (Time.time > orderedUntil || !Targets.Alive(ordered))
		{
			bool flag = Time.time <= orderedUntil;
			Lab.Event("orders: " + (flag ? "what it was sent after is down" : "gave up on what it was sent after"));
			if (flag)
			{
				Spawnables.Notify(Name + ": target down");
			}
			ordered = default(Targets.Target);
		}
		else
		{
			nearestSeen = ordered;
			mostDangerous = ordered;
			nearestSeenDistance = (ordered.Point - eye).magnitude;
			lastEnemyAt = ordered.Point;
			lastEnemyTime = Time.time;
		}
	}

	public void TakeFromYou(Transform thing)
	{
		try
		{
			Hand rightHand = RightHand;
			if (rightHand == null)
			{
				return;
			}
			StopPlaying();
			if (rightHand.m_CurrentAttachedGO != null)
			{
				FreeHand(rightHand);
			}
			Gun componentInChildren = thing.GetComponentInChildren<Gun>();
			bool flag;
			if (componentInChildren != null && componentInChildren.triggerGrip != null)
			{
				Take(componentInChildren);
				flag = Grab(rightHand, componentInChildren.triggerGrip);
				if (flag)
				{
					StartShooting();
				}
			}
			else
			{
				Rigidbody componentInChildren2 = thing.GetComponentInChildren<Rigidbody>();
				Grip grip = ((componentInChildren2 != null) ? GripOf(componentInChildren2) : null);
				item = componentInChildren2;
				this.grip = grip;
				gun = null;
				flag = grip != null && Grab(rightHand, grip);
				if (flag)
				{
					MeasureMelee();
					Next(Play.Melee);
				}
			}
			Lab.Event("give: " + (flag ? ("took the " + thing.name + " from your hand") : ("could not take the " + thing.name)));
			if (flag)
			{
				Spawnables.Notify(Name + " took the " + thing.name);
			}
		}
		catch (System.Exception ex)
		{
			Lab.Event("give: " + ex.Message);
		}
	}

	public static void LabOrder()
	{
		Bot bot = null;
		foreach (Bot item in BotManager.All)
		{
			if (!item.enemy && !item.dead)
			{
				bot = item;
				break;
			}
		}
		if (bot == null || bot.Head == null)
		{
			return;
		}
		Transform transform = null;
		float num = float.MaxValue;
		foreach (Targets.Target item2 in Targets.Hostiles())
		{
			float num2 = ((item2.transform != null) ? (item2.Point - bot.Head.position).magnitude : float.MaxValue);
			if (num2 < num)
			{
				num = num2;
				transform = item2.transform;
			}
		}
		if (transform == null)
		{
			foreach (Bot item3 in BotManager.All)
			{
				if (item3 != bot && !item3.dead)
				{
					transform = item3.rig.transform;
					break;
				}
			}
		}
		if (transform == null)
		{
			Lab.Event("orders: nothing about to send it after");
			return;
		}
		bot.Ordered(transform);
		Lab.Event("orders: " + bot.Name + " sent after " + transform.name + " (lab)");
	}

	private bool FindSomethingToShove()
	{
		Vector3 vector = Feet;
		Rigidbody rigidbody = null;
		float num = float.MaxValue;
		HashSet<System.IntPtr> hashSet = new HashSet<System.IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(vector + Vector3.up * 0.5f, Tuning.F("play.shove.seek", 12f), -1, QueryTriggerInteraction.Ignore))
		{
			Rigidbody rigidbody2 = ((item != null) ? item.attachedRigidbody : null);
			if (rigidbody2 == null || rigidbody2.isKinematic || !hashSet.Add(rigidbody2.Pointer) || rigidbody2.mass < 1f || rigidbody2.mass > Tuning.F("play.shove.most", 80f) || rigidbody2.GetComponentInParent<RigManager>() != null || NotForBots(rigidbody2) || HeldByAnyone(rigidbody2.transform, null) || rigidbody2.velocity.magnitude > 0.5f || rigidbody2.GetComponentInParent<Poolee>() == null || rigidbody2.GetComponentInParent<Poolee>().GetComponentInChildren<Seat>(includeInactive: true) != null)
			{
				continue;
			}
			float num2 = rigidbody2.worldCenterOfMass.y - vector.y;
			if (!(num2 < -0.3f) && !(num2 > 1.4f))
			{
				float num3 = Flat(rigidbody2.worldCenterOfMass - vector).magnitude + UnityEngine.Random.value * 4f;
				if (num3 < num)
				{
					num = num3;
					rigidbody = rigidbody2;
				}
			}
		}
		if (rigidbody == null)
		{
			return false;
		}
		this.item = rigidbody;
		grip = null;
		gun = null;
		shoveStage = 0;
		shoveStageAt = Time.time;
		shoved = false;
		play = Play.Shove;
		playAt = Time.time;
		nav.SetGoal(rigidbody.worldCenterOfMass);
		Lab.Event("play: going to shove the " + rigidbody.name + " (" + rigidbody.mass.ToString("0.0") + " kg)");
		return true;
	}

	private bool ShoveStep(Transform head, float dt)
	{
		if (item == null || head == null)
		{
			return EndPlay();
		}
		float num = Time.time - shoveStageAt;
		Vector3 worldCenterOfMass = item.worldCenterOfMass;
		float magnitude = Flat(worldCenterOfMass - head.position).magnitude;
		Transform vrRoot = controls.vrRoot;
		switch (shoveStage)
		{
		case 0:
			if (Time.time - playAt > 20f || nav.StuckCount >= 3)
			{
				return EndPlay();
			}
			if (magnitude > Tuning.F("play.shove.reach", 1f))
			{
				nav.SetGoal(worldCenterOfMass);
				Travel(MotionLibrary.WalkPush, 0.6f, dt);
				return true;
			}
			nav.Clear();
			shoveStage = 1;
			shoveStageAt = Time.time;
			return true;
		case 1:
		{
			walk = Vector2.zero;
			Face(worldCenterOfMass, dt, 240f);
			handOverride = true;
			float t = Mathf.SmoothStep(0f, 1f, num / 0.4f);
			handAt = Vector3.Lerp(new Vector3(0.2f, standingHeight - 0.5f, 0.25f), new Vector3(0.22f, standingHeight - 0.45f, -0.05f), t);
			handTurn = Quaternion.Euler(0f, 0f, -90f);
			playHeight = standingHeight;
			playPitch = 25f;
			if (num > 0.5f)
			{
				shoveStage = 2;
				shoveStageAt = Time.time;
			}
			return true;
		}
		case 2:
		{
			walk = Vector2.zero;
			handOverride = true;
			Vector3 b = ((vrRoot != null) ? vrRoot.InverseTransformPoint(worldCenterOfMass) : new Vector3(0.2f, standingHeight - 0.5f, 0.6f));
			float num2 = Mathf.Clamp01(num / 0.18f);
			handAt = Vector3.Lerp(new Vector3(0.22f, standingHeight - 0.45f, -0.05f), b, num2);
			handTurn = Quaternion.Euler(0f, 0f, -90f);
			playHeight = standingHeight;
			if (!shoved && num2 > 0.8f)
			{
				shoved = true;
				Vector3 normalized = Flat(worldCenterOfMass - head.position).normalized;
				float num3 = Mathf.Clamp(Tuning.F("play.shove.push", 30f) / Mathf.Max(1f, item.mass), Tuning.F("play.shove.least", 1.2f), Tuning.F("play.shove.fastest", 6f));
				item.AddForceAtPosition((normalized + Vector3.up * 0.25f) * num3 * item.mass, worldCenterOfMass + Vector3.up * 0.25f, ForceMode.Impulse);
				Lab.Event("play: shoved the " + item.name + " (" + item.mass.ToString("0.0") + " kg) at " + num3.ToString("0.0") + " m/s");
			}
			if (num > 0.35f)
			{
				shoveStage = 3;
				shoveStageAt = Time.time;
			}
			return true;
		}
		default:
			walk = Vector2.zero;
			Face(worldCenterOfMass, dt, 120f);
			playPitch = 20f;
			if (num > Tuning.F("play.shove.watch", 1.6f))
			{
				return EndPlay();
			}
			return true;
		}
	}

	private bool CarryStep(Transform you, Transform head, float dt)
	{
		if (!Holding() || you == null || head == null)
		{
			return EndPlay();
		}
		float num = Time.time - playAt;
		if (OthersHoldIt())
		{
			Release();
			Lab.Event("play: you took the " + item.name + " from it");
			return EndPlay();
		}
		Vector3 vector = you.position - Vector3.up * 1.5f;
		float magnitude = Flat(you.position - head.position).magnitude;
		handOverride = true;
		playHeight = standingHeight;
		if (magnitude > Tuning.F("play.bring.near", 1.6f) && num < Tuning.F("play.bring.most", 25f))
		{
			handAt = new Vector3(0.18f, standingHeight - 0.5f, 0.35f);
			handTurn = Quaternion.Euler(20f, -20f, 0f);
			playPitch = 10f;
			nav.SetGoal(vector);
			running = magnitude > 8f;
			if (Travel(running ? MotionLibrary.RunPush : MotionLibrary.WalkPush, 1.2f, dt) || nav.StuckCount >= 3)
			{
				nav.Clear();
			}
			offerSince = -1f;
			return true;
		}
		nav.Clear();
		walk = Vector2.zero;
		Face(you.position, dt, 200f);
		if (offerSince < 0f)
		{
			offerSince = Time.time;
			Lab.Event("play: holding the " + item.name + " out to you");
		}
		handAt = new Vector3(0.12f, standingHeight - 0.35f, 0.5f);
		handTurn = Quaternion.Euler(10f, -10f, 0f);
		playPitch = LookPitch(you, head);
		if (Time.time - offerSince > Tuning.F("play.bring.offer", 2f))
		{
			Lab.Event("play: put the " + item.name + " down at your feet");
			Next(Play.Drop);
		}
		return true;
	}

	public void LabShove()
	{
		StopPlaying();
		if (!FindSomethingToShove())
		{
			Lab.Event("play: nothing near to shove");
		}
	}

	public void LabBring()
	{
		StopPlaying();
		bringNext = true;
		if (!FindSomethingToPlayWith())
		{
			bringNext = false;
			Lab.Event("play: nothing near to bring");
		}
	}

	private void KeepPortalTurn(Transform root)
	{
		float num = PortalTurn(root);
		if (num != 0f)
		{
			bodyYaw += num;
			steer = Quaternion.Euler(0f, num, 0f) * steer;
		}
	}

	private float PortalTurn(Transform root)
	{
		if (droveHeadHas && (root.position - droveHead).magnitude > 1f)
		{
			droveJumpAt = Time.time;
		}
		droveHead = root.position;
		droveHeadHas = true;
		if (climbing)
		{
			droveHas = false;
		}
		if (droveHas)
		{
			float f = Mathf.DeltaAngle(droveYaw, root.eulerAngles.y);
			if (Mathf.Abs(f) > 3f)
			{
				droveTurn = f;
				droveTurnAt = Time.time;
			}
		}
		if (Time.time - droveTurnAt < 0.5f && Time.time - droveJumpAt < 0.5f)
		{
			Lab.Event("portals: turned " + droveTurn.ToString("0") + " deg by one");
			droveTurnAt = -10f;
			return droveTurn;
		}
		return 0f;
	}

	private void NotePortalTurn(Transform root)
	{
		droveYaw = root.eulerAngles.y;
		droveHas = true;
		droveHead = root.position;
	}

	private bool PortalCarried()
	{
		Transform head = Head;
		if (head == null)
		{
			return false;
		}
		Vector3 position = head.position;
		bool result = portalHeadHas && (position - portalHeadWas).magnitude > 1.5f;
		portalHeadWas = position;
		portalHeadHas = true;
		return result;
	}

	private bool PortalStep(Vector3 goal, bool afterPlayer, float dt)
	{
		bool flag = PortalCarried();
		if (climbing || Time.time < portalNotBefore)
		{
			portalGoing = false;
			return false;
		}
		Vector3 vector = Feet;
		if (!portalGoing)
		{
			bool flag2 = false;
			PortalWays.Way best = default(PortalWays.Way);
			if (afterPlayer && PortalWays.PlayerTookAt > portalTookSeen && Time.time - PortalWays.PlayerTookAt < 12f)
			{
				portalTookSeen = PortalWays.PlayerTookAt;
				best = PortalWays.PlayerTook;
				flag2 = (best.inAt - vector).magnitude < Tuning.F("portal.follow", 30f) && (best.inAt - vector).magnitude < (goal - vector).magnitude + 2f;
			}
			if (!flag2 && Time.time - portalThroughAt > 4f)
			{
				flag2 = PortalWays.Shorter(vector, goal, out best);
			}
			if (!flag2)
			{
				return false;
			}
			portalGoing = true;
			portalWay = best;
			portalSince = Time.time;
			portalPhase = 0;
			portalPhaseAt = Time.time;
			nav.Clear();
			Lab.Event("portals: going through one, " + (best.inAt - vector).magnitude.ToString("0") + " m off" + (afterPlayer ? " (after you)" : ""));
		}
		if (flag)
		{
			portalGoing = false;
			portalThroughAt = Time.time;
			nav.Clear();
			Lab.Event("portals: through");
			return false;
		}
		Vector3 inOpen = portalWay.inOpen;
		bool flag3 = inOpen.y > 0.7f;
		Vector3 vector2 = (flag3 ? portalWay.inAt : (portalWay.inAt + Flat(inOpen).normalized * 1.1f));
		if (!flag3 && Physics.Raycast(vector2, Vector3.down, out var hitInfo, 4f, -1, QueryTriggerInteraction.Ignore))
		{
			vector2 = hitInfo.point;
		}
		if (portalPhase == 0)
		{
			if (!nav.HasGoal || (nav.Goal - vector2).magnitude > 0.5f)
			{
				nav.SetGoal(vector2);
			}
			running = (vector2 - vector).magnitude > 6f;
			if (Travel(running ? MotionLibrary.RunPush : MotionLibrary.WalkPush, 0.45f, dt) || Flat(vector2 - vector).magnitude < 0.6f || (flag3 && Flat(vector2 - vector).magnitude < 0.4f))
			{
				portalPhase = 1;
				portalPhaseAt = Time.time;
			}
			else if (Time.time - portalSince > 20f || nav.StuckCount >= 4)
			{
				GiveUpPortal("could not get to it");
			}
			return portalGoing;
		}
		running = false;
		Vector3 vector3 = Flat(vector - portalWay.inAt);
		Vector3 vector4 = ((Flat(inOpen).sqrMagnitude > 0.01f) ? Flat(inOpen).normalized : Vector3.zero);
		Vector3 vector5 = Flat((((vector3 - vector4 * Vector3.Dot(vector3, vector4)).magnitude > 0.2f && !flag3) ? (portalWay.inAt + vector4 * 0.8f) : (portalWay.inAt - inOpen * 0.6f)) - vector);
		if (vector5.sqrMagnitude < 0.01f)
		{
			vector5 = -Flat(inOpen);
		}
		Go(vector5.normalized, MotionLibrary.WalkPush, dt);
		string state = "";
		bool flag4 = false;
		try
		{
			flag4 = PortalPush(Time.time - portalPhaseAt, out state);
		}
		catch (System.Exception ex)
		{
			state = ex.Message;
		}
		if (flag4)
		{
			portalGoing = false;
			portalThroughAt = Time.time;
			portalHeadHas = false;
			nav.Clear();
			Lab.Event("portals: through (" + state + ")");
			return false;
		}
		if (Time.time - portalPhaseAt > 7f)
		{
			GiveUpPortal("walked at it and did not go through: " + state);
		}
		return portalGoing;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool PortalPush(float tried, out string state)
	{
		state = "";
		Transform head = Head;
		Portal portal = ((portalWay.portal != null) ? portalWay.portal.TryCast<Portal>() : null);
		Portal portal2 = ((portalWay.other != null) ? portalWay.other.TryCast<Portal>() : null);
		if (head == null || portal == null || portal2 == null)
		{
			state = "the portal is gone";
			return false;
		}
		Vector3 vector = portal.transform.InverseTransformPoint(head.position);
		Vector2 vector2 = portal.Size * 0.5f;
		bool flag = Mathf.Abs(vector.x) <= vector2.x && Mathf.Abs(vector.y) <= vector2.y;
		TeleportableRigManager component = rig.GetComponent<TeleportableRigManager>();
		state = "head " + vector.ToString("0.00") + " to it, its half size " + vector2.ToString("0.00") + ((component == null) ? ", its body is not one the portals know" : (", bodies " + component.Bodies.Count + (component.HasPortals ? ", in a portal" : ", in none")));
		if (component != null && flag && vector.z < -0.02f)
		{
			component.Teleport(portal, portal2);
			state += "; carried by the portal on being asked";
			return true;
		}
		Vector2 vector3 = vector2 + new Vector2(0.35f, 0.9f);
		bool flag2 = Mathf.Abs(vector.x) <= vector3.x && Mathf.Abs(vector.y) <= vector3.y;
		if (tried > Tuning.F("portal.patience", 3.5f) && flag2 && vector.z < Tuning.F("portal.reach", 0.9f))
		{
			Vector3 outOpen = portalWay.outOpen;
			Vector3 feetDestinationWorld;
			if (outOpen.y > 0.7f)
			{
				feetDestinationWorld = portalWay.outAt + Vector3.up * 0.1f;
			}
			else if (outOpen.y < -0.7f)
			{
				feetDestinationWorld = portalWay.outAt + outOpen * 2f;
			}
			else
			{
				Vector3 vector4 = portalWay.outAt + Flat(outOpen).normalized * 0.9f;
				feetDestinationWorld = (Physics.Raycast(vector4, Vector3.down, out var hitInfo, 5f, -1, QueryTriggerInteraction.Ignore) ? hitInfo.point : (vector4 - Vector3.up));
			}
			Vector3 forward = ((Flat(outOpen).sqrMagnitude > 0.01f) ? Flat(outOpen).normalized : (Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward));
			bodyYaw = Recorder.Yaw(forward);
			steer = forward;
			droveHas = false;
			droveHeadHas = false;
			rig.Teleport(feetDestinationWorld);
			state += "; put out of the other one";
			return true;
		}
		return false;
	}

	private void GiveUpPortal(string why)
	{
		portalGoing = false;
		portalNotBefore = Time.time + 20f;
		nav.Clear();
		Lab.Event("portals: gave up (" + why + ")");
	}

	public void LabPortal()
	{
		float num = float.MaxValue;
		foreach (PortalWays.Way item in PortalWays.All)
		{
			float magnitude = (item.inAt - Feet).magnitude;
			if (magnitude < num && item.inOpen.y > -0.5f)
			{
				num = magnitude;
				portalWay = item;
			}
		}
		Lab.Event("portals: " + PortalWays.All.Count + " ways about" + ((num < float.MaxValue) ? (", nearest " + num.ToString("0.0") + " m, opens " + portalWay.inOpen.ToString("0.00")) : ""));
		if (num != float.MaxValue)
		{
			portalGoing = true;
			portalSince = Time.time;
			portalPhase = 0;
			portalNotBefore = 0f;
			labPortal = true;
			if (mode != Mode.Follow)
			{
				SetMode(Mode.Follow);
			}
		}
	}

	private Frame[] PickStrike()
	{
		strikePace = 0f;
		Frame[] array = PickHit(MoveSlots.Kind.Punch);
		if (array == null)
		{
			return null;
		}
		if (!strikesOf.TryGetValue(array, out var value))
		{
			value = Split(array);
			strikesOf[array] = value;
			string text = "";
			foreach (Strike item in value)
			{
				text = text + (item.right ? "R " : "L ") + item.clip[item.clip.Length - 1].time.ToString("0.00") + " s at " + item.peak.ToString("0.0") + " m/s; ";
			}
			Lab.Event("fists: the recorded punches (" + array[^1].time.ToString("0.0") + " s) cut into " + value.Count + ": " + text);
		}
		if (value.Count == 0)
		{
			return array;
		}
		Strike strike = value[UnityEngine.Random.Range(0, value.Count)];
		if (strike == lastStrike && value.Count > 1)
		{
			strike = value[(value.IndexOf(strike) + 1) % value.Count];
		}
		lastStrike = strike;
		punchRight = strike.right;
		strikePace = Mathf.Clamp(Tuning.F("punch.peak", 6.5f) / Mathf.Max(0.5f, strike.peak), 1f, Tuning.F("punch.pace.most", 2.5f));
		return strike.clip;
	}

	private static System.Collections.Generic.List<Strike> Split(Frame[] clip)
	{
		System.Collections.Generic.List<Strike> list = new System.Collections.Generic.List<Strike>();
		int num = clip.Length;
		if (num < 4)
		{
			return list;
		}
		float[] array = new float[num];
		bool[] array2 = new bool[num];
		for (int i = 1; i < num; i++)
		{
			float num2 = Mathf.Max(0.001f, clip[i].time - clip[i - 1].time);
			float num3 = (clip[i].leftPos - clip[i - 1].leftPos).magnitude / num2;
			float num4 = (clip[i].rightPos - clip[i - 1].rightPos).magnitude / num2;
			array[i] = Mathf.Max(num3, num4);
			array2[i] = num4 > num3;
		}
		float num5 = Tuning.F("punch.cut.rise", 1.2f);
		float num6 = Tuning.F("punch.cut.fall", 0.6f);
		int num7 = -1;
		float num8 = 0f;
		int num9 = 0;
		int num10 = 0;
		for (int j = 1; j < num; j++)
		{
			if (num7 < 0)
			{
				if (!(array[j] > num5))
				{
					continue;
				}
				num7 = j;
				num8 = 0f;
				num9 = (num10 = 0);
			}
			num8 = Mathf.Max(num8, array[j]);
			num10++;
			if (array2[j])
			{
				num9++;
			}
			if ((!(array[j] < num6) || (j + 1 < num && !(array[j + 1] < num6))) && j != num - 1)
			{
				continue;
			}
			float num11 = clip[num7].time - Tuning.F("punch.cut.before", 0.1f);
			float num12 = clip[j].time + Tuning.F("punch.cut.after", 0.12f);
			if (num12 - num11 >= 0.25f && num8 > num5 * 1.5f)
			{
				System.Collections.Generic.List<Frame> list2 = new System.Collections.Generic.List<Frame>();
				for (int k = 0; k < clip.Length; k++)
				{
					Frame frame = clip[k];
					if (!(frame.time < num11) && !(frame.time > num12))
					{
						Frame frame2 = frame;
						frame2.time = frame.time - Mathf.Max(0f, num11);
						list2.Add(frame2);
					}
				}
				if (list2.Count >= 3)
				{
					list.Add(new Strike
					{
						clip = list2.ToArray(),
						peak = num8,
						right = (num9 * 2 >= num10)
					});
				}
			}
			num7 = -1;
		}
		return list;
	}

	private bool Defend(Targets.Target t, Transform head)
	{
		bool flag = false;
		if (t.bot != null)
		{
			flag = t.bot.hitClip != null && Time.time - t.bot.punchAt < Tuning.F("fist.read", 0.25f);
		}
		else if (t.player)
		{
			Hand[] array = new Hand[2]
			{
				Player.LeftHand,
				Player.RightHand
			};
			foreach (Hand hand in array)
			{
				Rigidbody rigidbody = ((hand != null) ? hand.rb : null);
				if (!(rigidbody == null))
				{
					Vector3 vector = head.position - rigidbody.position;
					if (vector.magnitude < 1.1f && Vector3.Dot(rigidbody.velocity, vector.normalized) > Tuning.F("fist.read.speed", 2.5f))
					{
						flag = true;
					}
				}
			}
		}
		if (flag && Time.time > defendUntil && Time.time - defendSeenAt > 0.5f)
		{
			defendSeenAt = Time.time;
			if (UnityEngine.Random.value < Tuning.F("fist.defend", 0.55f) * Mathf.Lerp(0.7f, 1.3f, personality.aim))
			{
				defendBlock = UnityEngine.Random.value < Tuning.F("fist.block", 0.6f);
				defendUntil = Time.time + Tuning.F("fist.defend.time", 0.45f);
				if (defendBlock)
				{
					LabBlocks++;
				}
				else
				{
					LabSteps++;
				}
				Lab.Event("fists: " + (defendBlock ? "guard up" : "steps back") + " from a punch");
			}
			else
			{
				LabTaken++;
				Lab.Event("fists: did not see that punch coming");
			}
		}
		if (Time.time >= defendUntil)
		{
			return false;
		}
		if (defendBlock)
		{
			handAt = new Vector3(0.11f, playHeight - 0.08f, 0.2f);
			leftAt = new Vector3(-0.11f, playHeight - 0.06f, 0.2f);
			handTurn = Quaternion.Euler(-60f, -25f, 0f);
			leftTurn = Quaternion.Euler(-60f, 25f, 0f);
			walk = Vector2.zero;
		}
		else
		{
			walk = new Vector2(UnityEngine.Random.Range(-0.3f, 0.3f), -1f) * MotionLibrary.WalkPush * Tuning.F("fist.step", 1.2f);
		}
		return true;
	}

	private bool FindTallWall(Vector3 feet, Vector3 dir, float within, out Vector3 foot, out Vector3 inward, out float height, out Vector3 top)
	{
		foot = (inward = (top = Vector3.zero));
		height = 0f;
		if (!Physics.Raycast(feet + Vector3.up * 0.8f, dir, out var hitInfo, within, -1, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		if (hitInfo.collider.GetComponentInParent<RigManager>() != null || hitInfo.collider.attachedRigidbody != null || Mathf.Abs(hitInfo.normal.y) > 0.35f)
		{
			return false;
		}
		inward = -Flat(hitInfo.normal);
		if (inward.sqrMagnitude < 0.01f)
		{
			return false;
		}
		inward.Normalize();
		Vector3 vector = hitInfo.point + inward * 0.45f;
		if (!Physics.Raycast(new Vector3(vector.x, feet.y + Tuning.F("tool.climb.most", 30f), vector.z), Vector3.down, out var hitInfo2, Tuning.F("tool.climb.most", 30f), -1, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		height = hitInfo2.point.y - feet.y;
		if (hitInfo2.normal.y < 0.7f || height <= Tuning.F("climb.max", 2.5f) || hitInfo2.collider.attachedRigidbody != null)
		{
			return false;
		}
		if (Physics.Raycast(hitInfo2.point + Vector3.up * 0.05f, Vector3.up, 1.6f, -1, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		if (!Physics.Raycast(new Vector3(hitInfo.point.x, hitInfo2.point.y - 0.4f, hitInfo.point.z) - inward * 0.6f, inward, 1.2f, -1, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		top = hitInfo2.point;
		foot = new Vector3(hitInfo.point.x, feet.y, hitInfo.point.z) - inward * Tuning.F("tool.climb.stand", 0.45f);
		return true;
	}

	private bool TallWallAbout(Vector3 feet, out Vector3 foot, out Vector3 inward, out float height, out Vector3 top)
	{
		foot = (inward = (top = Vector3.zero));
		height = 0f;
		if (Lessons.For("climb") == null)
		{
			return false;
		}
		float num = UnityEngine.Random.Range(0f, 360f);
		for (int i = 0; i < 12; i++)
		{
			Vector3 dir = Quaternion.Euler(0f, num + (float)i * 30f, 0f) * Vector3.forward;
			if (FindTallWall(feet, dir, 25f, out foot, out inward, out height, out top) && SelfTaught.ClimbFails(top) < 2 && height < Tuning.F("climb.roam.most", 5f))
			{
				return true;
			}
		}
		return false;
	}

	private bool ClimbWithTool(Vector3 foot, Vector3 inward, float height)
	{
		Lessons.Lesson lesson = Lessons.For("climb");
		if (lesson == null || InLesson || Time.time - purposeAt < 4f)
		{
			return false;
		}
		if (SomeoneClimbingAt(foot, Tuning.F("climb.apart", 1.8f)))
		{
			return false;
		}
		purposeAt = Time.time;
		float num = height + 0.3f - lesson.rise;
		int num2 = ((num > 0f && lesson.loopRise > 0.2f) ? Mathf.CeilToInt(num / lesson.loopRise) : 0);
		if (num > 0.5f && num2 == 0)
		{
			Lab.Event("purpose: the wall is " + height.ToString("0.0") + " m and the " + lesson.name + " lesson only goes up " + lesson.rise.ToString("0.0") + " m, with no stretch to do again");
			return false;
		}
		bodyYaw = Recorder.Yaw(inward);
		wallTopY = foot.y + height;
		Lab.Event("purpose: a " + height.ToString("0.0") + " m wall, up it with the " + lesson.name + " (" + num2 + " stretches more than you did)");
		return UseLesson(lesson, atSpot: false, Mathf.Max(0f, lesson.riseAt - 1.2f), num2, (foot, inward));
	}

	private bool MaybePurpose(Transform head)
	{
		if (InLesson || enemy || climbing || Playing || head == null || Time.time - purposeAt < Tuning.F("tool.every", 14f) || Lessons.Count == 0)
		{
			return false;
		}
		if (nearestSeen.transform != null && nearestSeenDistance < 4f && gun == null)
		{
			Lessons.Lesson lesson = Lessons.For("fight");
			if (lesson != null)
			{
				purposeAt = Time.time;
				Face(nearestSeen.Point, 1f, 720f);
				Lab.Event("purpose: something to fight, with the " + lesson.name);
				lessonPurposeful = true;
				return lessonPurposeful = UseLesson(lesson, atSpot: false);
			}
		}
		return false;
	}

	public void LabPurpose()
	{
		Lab.Event("purpose: " + Lessons.Purposes());
		Vector3 vector = ((rig.physicsRig != null && rig.physicsRig.centerOfPressure != null) ? rig.physicsRig.centerOfPressure.position : Feet);
		if (TallWallAbout(vector, out var foot, out var inward, out var height, out var top))
		{
			Lab.Event("purpose: a wall " + height.ToString("0.0") + " m high, " + Flat(foot - vector).magnitude.ToString("0.0") + " m off");
			if (mode != Mode.Wander)
			{
				SetMode(Mode.Wander);
			}
			StopPlaying();
			planPlace = foot;
			planLook = top;
			planSight = Sight.Tall;
			planWhy = "going up that wall with what you showed it";
			planWall = (foot: foot, inward: inward, height: height);
			planPush = MotionLibrary.WalkPush;
			planRun = false;
			viaLeft = false;
			onDetour = false;
			detours = 0;
			thinking2 = false;
			act = Act.Go;
			actUntil = Time.time + 90f;
			nav.Clear();
			nav.SetGoal(foot);
		}
		else
		{
			Lab.Event("purpose: no high wall about that it has anything to climb with");
		}
	}

	public void RaceLineUp(Vector3 spot)
	{
		raceSpot = spot;
		raceLineUp = true;
		raceReady = false;
		nav.Clear();
	}

	public void RaceLineUpOff()
	{
		raceLineUp = false;
	}

	private bool RaceStep(Transform head, float dt)
	{
		if (!raceLineUp || dead || down || head == null)
		{
			return false;
		}
		float magnitude = Flat(raceSpot - head.position).magnitude;
		if (magnitude > Tuning.F("race.ready.near", 1.2f) && !raceReady)
		{
			if (!nav.HasGoal)
			{
				nav.SetGoal(raceSpot);
			}
			running = magnitude > 4f;
			if (Travel(running ? MotionLibrary.RunPush : MotionLibrary.WalkPush, 0.6f, dt) || nav.StuckCount >= 3)
			{
				nav.Clear();
			}
			return true;
		}
		if (!raceReady)
		{
			Lab.Event("race: by its kart");
		}
		raceReady = true;
		nav.Clear();
		walk = Vector2.zero;
		running = false;
		Face(raceSpot + (raceSpot - head.position), dt, 90f);
		return true;
	}

	public bool RaceDrive(Vector3 to)
	{
		Vector3? bodyAt = BodyAt;
		if (bodyAt.HasValue)
		{
			driveFacing = Flat(to - bodyAt.Value).normalized;
		}
		return DriveTo(to, "racing");
	}

	private void Startle(Vector3 from, float far)
	{
		float num = Tuning.F("flinch.range", 14f);
		if (far > num || !HandsFree)
		{
			return;
		}
		nerves = Mathf.Max(0f, nerves - (Time.time - nervesAt) / Tuning.F("flinch.calm", 6f));
		nervesAt = Time.time;
		float num2 = Mathf.Clamp01(1.15f - far / num) / (1f + nerves) * Mathf.Lerp(1.2f, 0.7f, Mathf.InverseLerp(0.5f, 1.8f, personality.calm));
		nerves += 0.8f;
		if (!(num2 < 0.12f) && !(Time.time - flinchAt < 0.25f))
		{
			Transform head = Head;
			flinchSide = Mathf.Clamp((Quaternion.Euler(0f, 0f - bodyYaw, 0f) * Flat(from - head.position).normalized).x, -1f, 1f);
			flinchSize = num2;
			flinchAt = Time.time + Random(0.06f, 0.13f) * personality.reaction;
			if (num2 > 0.45f && UnityEngine.Random.value < Tuning.F("flinch.squat", 0.55f))
			{
				Squat(Random(0.9f, 2f), 0.55f);
			}
			Lab.Event("flinch " + num2.ToString("0.00") + " at a bang " + far.ToString("0") + " m off");
		}
	}

	private void StartWave(int times, float after)
	{
		waveTimes = times;
		waveDue = Time.time + after;
		waveLeft = personality.mirror > 0.5f;
	}

	public void LabFidget()
	{
		fidNext = Time.time;
	}

	public void LabStartle()
	{
		if (Head != null)
		{
			Startle(Head.position + Quaternion.Euler(0f, bodyYaw + 90f, 0f) * Vector3.forward * 3f, 3f);
		}
	}

	public void LabWaveBack()
	{
		StartWave(3, 0.3f);
	}

	private void Notice(Transform you, Transform head)
	{
		if (head == null)
		{
			return;
		}
		Fall(head);
		if (you == null || enemy)
		{
			return;
		}
		Vector3 vector = you.position - head.position;
		float magnitude = vector.magnitude;
		bool flag = HandsFree && nearestSeen.transform == null && brainTarget.transform == null && Time.time >= waveUntil && waveDue < 0f;
		if (Gestures.WavedAt > waveSeen)
		{
			waveSeen = Gestures.WavedAt;
			bool flag2 = magnitude < Tuning.F("wave.far", 25f) && Vector3.Angle(Flat(Gestures.WaveFacing), Flat(-vector)) < Tuning.F("wave.cone", 35f);
			if (flag && flag2 && Targets.InSight(head.position, you.position, (Player.RigManager != null) ? Player.RigManager.transform : null, rig))
			{
				StartWave(UnityEngine.Random.Range(2, 4), Random(0.35f, 0.8f) * personality.reaction);
				Lab.Event("waves back");
			}
		}
		bool flag3 = magnitude < 8f;
		if (flag3 && !nearYou && Time.time - lastNearYou > Tuning.F("wave.hello.after", 45f) && Time.time - born > 20f && flag && UnityEngine.Random.value < Tuning.F("wave.hello.chance", 0.5f))
		{
			StartWave(2, Random(0.5f, 1.2f));
			Lab.Event("waves hello");
		}
		if (flag3)
		{
			lastNearYou = Time.time;
		}
		nearYou = flag3;
		if (!(waveDue >= 0f) || !(Time.time >= waveDue))
		{
			return;
		}
		waveDue = -1f;
		if (HandsFree)
		{
			waveStart = Time.time;
			waveUntil = Time.time + 0.35f + (float)waveTimes * 0.7f;
			WaveOfTheirs();
			Look(you.position, you, "you", waveUntil - Time.time + 0.6f);
			if (mode == Mode.Wander || (mode == Mode.Brain && act != 0 && nearestSeen.transform == null))
			{
				nav.Clear();
				Idle(waveUntil - Time.time + Random(0.5f, 1.5f));
			}
		}
	}

	private void Fall(Transform head)
	{
		Vector3 position = head.position;
		float deltaTime = Time.deltaTime;
		if (!fallHas || deltaTime <= 0f || (position - fallLast).magnitude > 3f || climbing)
		{
			fallHas = true;
			fallLast = position;
			fallSpeed = 0f;
			falling = false;
			if (climbing)
			{
				noLandUntil = Time.time + 1f;
			}
			return;
		}
		fallSpeed = Mathf.Lerp(fallSpeed, (position.y - fallLast.y) / deltaTime, Mathf.Clamp01(deltaTime * 12f));
		fallLast = position;
		if (!falling && fallSpeed < 0f - Tuning.F("land.falling", 2.6f))
		{
			falling = true;
			fallTop = position.y;
			fallSince = Time.time;
		}
		else
		{
			if (!falling || !(fallSpeed > -0.8f))
			{
				return;
			}
			falling = false;
			float num = (fallTop - position.y + 0.3f) / Mathf.Max(0.4f, pose.height / 1.7f);
			if (num > Tuning.F("land.from", 0.7f) && Time.time > noLandUntil && Time.time - jumpedAt > 0.2f)
			{
				landAt = Time.time;
				landSize = Mathf.Clamp01(num / Tuning.F("land.full", 3f));
				Lab.Event("landed from " + num.ToString("0.0") + " m");
				if (num > Tuning.F("trip.fall", 4.5f))
				{
					Trip(Mathf.InverseLerp(4.5f, 9f, num), head.forward, "a landing from " + num.ToString("0.0") + " m");
				}
			}
		}
	}

	private void React(float dt)
	{
		float height = pose.height;
		float num = height / 1.7f;
		if (falling && HandsFree)
		{
			float num2 = Mathf.Clamp01((Time.time - fallSince) / 0.25f) * Tuning.F("land.arms", 1f);
			pose.leftPos = Vector3.Lerp(pose.leftPos, new Vector3(-0.42f * num, height - 0.45f * num, 0.18f * num), num2 * 0.8f);
			pose.rightPos = Vector3.Lerp(pose.rightPos, new Vector3(0.42f * num, height - 0.45f * num, 0.18f * num), num2 * 0.8f);
		}
		float num3 = Time.time - landAt;
		float num4 = Mathf.Lerp(0.5f, 1.1f, landSize);
		if (num3 >= 0f && num3 < num4)
		{
			float num5 = num3 / num4;
			float num6 = ((num5 < 0.18f) ? Mathf.SmoothStep(0f, 1f, num5 / 0.18f) : (1f - Mathf.SmoothStep(0f, 1f, (num5 - 0.18f) / 0.82f)));
			float num7 = Tuning.F("land.dip", 0.32f) * landSize * num6 * num;
			pose.height = height - num7;
			pose.head = Quaternion.Euler(14f * landSize * num6, 0f, 0f) * pose.head;
			if (HandsFree)
			{
				pose.leftPos = Vector3.Lerp(pose.leftPos, new Vector3(-0.4f * num, height - num7 - 0.5f * num, 0.22f * num), num6 * 0.85f);
				pose.rightPos = Vector3.Lerp(pose.rightPos, new Vector3(0.4f * num, height - num7 - 0.5f * num, 0.22f * num), num6 * 0.85f);
			}
			else
			{
				pose.leftPos.y -= num7;
				pose.rightPos.y -= num7;
			}
			if (landSize > 0.4f)
			{
				walk *= 1f - num6 * landSize;
			}
		}
		num3 = Time.time - flinchAt;
		if (num3 >= 0f && num3 < 0.55f && HandsFree)
		{
			float num8 = num3 / 0.55f;
			float num9 = ((num8 < 0.14f) ? (num8 / 0.14f) : (1f - Mathf.SmoothStep(0f, 1f, (num8 - 0.14f) / 0.86f))) * flinchSize * Tuning.F("flinch.size", 1f);
			pose.height -= 0.09f * num9 * num;
			pose.head = Quaternion.Euler(16f * num9, (0f - flinchSide) * 22f * num9, flinchSide * 6f * num9) * pose.head;
			Vector3 vector = new Vector3(0f, 0.07f * num9, 0.1f * num9) * num;
			pose.leftPos += vector + new Vector3(0.05f * num9 * num, 0f, 0f);
			pose.rightPos += vector + new Vector3(-0.05f * num9 * num, 0f, 0f);
			curl = Mathf.Lerp(curl, 0.8f, num9);
		}
	}

	private void StartReload(bool rackOnly = false)
	{
		Lab.Event((rackOnly ? "racking: " : "reload starts: ") + ((gun != null) ? SafeState() : "?") + ", dry pulls " + dryPulls);
		if (!rackOnly)
		{
			FeedPouch(gun);
		}
		LetGoOfSupport();
		reload = ((!rackOnly) ? Reload.Eject : Reload.Rack);
		learnedReload = null;
		if (!rackOnly)
		{
			StartLearnedReload();
		}
		reloadAt = Time.time;
		newMag = null;
		magAsked = false;
		ejected = false;
		slide = null;
		slideHeld = false;
		dryPulls = 0;
	}

	private void NextReload(Reload stage)
	{
		Lab.Event("reload " + reload.ToString() + " -> " + stage.ToString() + " after " + (Time.time - reloadAt).ToString("0.00") + " s");
		reload = stage;
		reloadAt = Time.time;
	}

	private void ReloadStep(Transform head)
	{
		if (learnedReload != null)
		{
			LearnedReloadStep(head);
			return;
		}
		float num = Time.time - reloadAt;
		AmmoSocket ammoSocket = gun.ammoSocket;
		Aim(head.position + Quaternion.Euler(0f, bodyYaw - 15f, 0f) * new Vector3(0f, -0.9f, 2.5f), head, Hold.Reload);
		leftOverride = true;
		Vector3 a = new Vector3(-0.17f, standingHeight - 0.88f, 0.04f);
		if (ammoSocket == null || gun.defaultMagazine == null)
		{
			leftAt = Vector3.Lerp(a, new Vector3(0.05f, standingHeight - 0.55f, 0.32f), Mathf.PingPong(num * 2f, 1f));
			leftTurn = Quaternion.Euler(60f, 20f, 0f);
			if (num > 1f)
			{
				FinishReload(quick: true);
			}
			return;
		}
		if (num > 3.5f && reload != Reload.Rack)
		{
			MelonLogger.Msg("Bot Player: the reload of " + gun.name + " did not go in by hand (" + reload.ToString() + "); loading it");
			DropNewMag();
			FinishReload(quick: true);
			return;
		}
		switch (reload)
		{
		case Reload.Eject:
			leftAt = controls.vrRoot.InverseTransformPoint(ammoSocket.transform.position) + new Vector3(-0.05f, -0.06f, 0f);
			leftTurn = Quaternion.Euler(70f, 30f, 0f);
			if (!ejected && num > 0.25f)
			{
				ejected = true;
				try
				{
					if (gun.HasMagazine())
					{
						Magazine magazine = ((ammoSocket._magazinePlug != null) ? ammoSocket._magazinePlug.magazine : null);
						ammoSocket.EjectMagazine();
						if (magazine != null)
						{
							dropped.Add((magazine, Time.time));
						}
					}
				}
				catch (System.Exception ex)
				{
					MelonLogger.Warning("Bot Player: could not drop the magazine: " + ex.Message);
				}
			}
			if (num > 0.4f)
			{
				NextReload(Reload.Fetch);
			}
			break;
		case Reload.Fetch:
		{
			leftAt = a;
			leftTurn = Quaternion.Euler(80f, 0f, 0f);
			Hand leftHand = LeftHand;
			if (!magAsked && num > 0.3f && leftHand != null)
			{
				magAsked = true;
				SpawnMag(leftHand.transform.position, leftHand.transform.rotation);
			}
			if (newMag == null && magAsked && Time.time - magAskedAt > 0.15f)
			{
				newMag = SpawnedMag();
			}
			if (newMag != null && newMag.grip != null && leftHand != null)
			{
				if (leftHand.m_CurrentAttachedGO == null && !Grab(leftHand, newMag.grip))
				{
					DropNewMag();
					FinishReload(quick: true);
				}
				else
				{
					leftMeasured = false;
					NextReload(Reload.Insert);
				}
			}
			break;
		}
		case Reload.Insert:
			Insert(ammoSocket, num);
			break;
		case Reload.Rack:
			RackSlide(num);
			break;
		}
	}

	private void SpawnMag(Vector3 at, Quaternion turn)
	{
		try
		{
			SpawnableCrateReference spawnableCrateReference = ((gun.defaultMagazine.spawnable != null) ? gun.defaultMagazine.spawnable.crateRef : null);
			if (spawnableCrateReference == null)
			{
				FinishReload(quick: true);
				return;
			}
			magAt = at;
			magAskedAt = Time.time;
			magsBefore.Clear();
			foreach (Collider item in Physics.OverlapSphere(at, 1f, -1, QueryTriggerInteraction.Ignore))
			{
				Magazine magazine = ((item != null) ? item.GetComponentInParent<Magazine>() : null);
				if (magazine != null)
				{
					magsBefore.Add(magazine.Pointer);
				}
			}
			Clone.Suppress();
			HelperMethods.SpawnCrate(spawnableCrateReference, at, turn, Vector3.one);
		}
		catch (System.Exception ex)
		{
			MelonLogger.Warning("Bot Player: no magazine for " + gun.name + ": " + ex.Message);
			FinishReload(quick: true);
		}
	}

	private Magazine SpawnedMag()
	{
		foreach (Collider item in Physics.OverlapSphere(magAt, 1f, -1, QueryTriggerInteraction.Ignore))
		{
			Magazine magazine = ((item != null) ? item.GetComponentInParent<Magazine>() : null);
			if (!(magazine == null) && !magsBefore.Contains(magazine.Pointer) && !magazine.isMagazineInserted && (!(magazine.grip != null) || !magazine.grip.HasAttachedHands()))
			{
				try
				{
					magazine.Initialize(gun.defaultCartridge, gun.defaultMagazine.rounds);
				}
				catch (System.Exception)
				{
				}
				return magazine;
			}
		}
		return null;
	}

	public static void ClearDropped()
	{
		for (int num = dropped.Count - 1; num >= 0; num--)
		{
			var (magazine, num2) = dropped[num];
			if (magazine == null)
			{
				dropped.RemoveAt(num);
			}
			else if (!(Time.time - num2 < 20f))
			{
				dropped.RemoveAt(num);
				try
				{
					if (!magazine.isMagazineInserted && (!(magazine.grip != null) || !magazine.grip.HasAttachedHands()))
					{
						Poolee componentInParent = magazine.GetComponentInParent<Poolee>();
						if (componentInParent != null)
						{
							componentInParent.Despawn();
						}
						else
						{
							UnityEngine.Object.Destroy(magazine.gameObject);
						}
					}
				}
				catch (System.Exception)
				{
				}
			}
		}
	}

	public static void ForgetDropped()
	{
		dropped.Clear();
	}

	private void Insert(AmmoSocket socket, float since)
	{
		Hand leftHand = LeftHand;
		if (newMag == null || leftHand == null || newMag.magazinePlug == null)
		{
			FinishReload(quick: true);
			return;
		}
		if (gun.HasMagazine())
		{
			LetGoOf(newMag.grip, leftHand);
			newMag = null;
			NextReload(Reload.Rack);
			return;
		}
		Transform transform = newMag.magazinePlug.transform;
		Transform transform2 = socket.transform;
		Vector3 vector = ((socket.endTransform != null && (socket.endTransform.position - transform2.position).sqrMagnitude > 1E-06f) ? (socket.endTransform.position - transform2.position).normalized : (-transform2.up));
		float t = Mathf.SmoothStep(0f, 1f, since / 0.7f);
		Pose obj = new Pose(transform2.position - vector * Mathf.Lerp(0.12f, -0.01f, t), transform2.rotation);
		Transform transform3 = newMag.transform;
		Pose pose = Pose.Of(transform3).Inverse() * Pose.Of(transform);
		Pose pose2 = Pose.Of(leftHand.transform).Inverse() * Pose.Of(transform3);
		Pose obj2 = obj * pose.Inverse() * pose2.Inverse();
		Pose pose3 = Pose.Of(controls.leftController.transform).Inverse() * Pose.Of(leftHand.transform);
		leftOnController = (leftMeasured ? Pose.Lerp(leftOnController, pose3, 0.1f) : pose3);
		leftMeasured = true;
		Pose pose4 = obj2 * leftOnController.Inverse();
		leftAt = controls.vrRoot.InverseTransformPoint(pose4.position);
		leftTurn = Quaternion.Inverse(Quaternion.Euler(0f, bodyYaw, 0f)) * pose4.rotation;
		if ((transform.position - transform2.position).magnitude < 0.06f && since > 0.4f)
		{
			try
			{
				newMag.magazinePlug.InsertPlug(socket);
			}
			catch (System.Exception)
			{
			}
		}
		if (!(since > 2.2f) || gun.HasMagazine())
		{
			return;
		}
		try
		{
			newMag.magazinePlug.ForceInSocket(socket);
		}
		catch (System.Exception)
		{
		}
	}

	private void RackSlide(float since)
	{
		Hand leftHand = LeftHand;
		if (gun.cartridgeState == Gun.CartridgeStates.UNSPENT && !slideHeld)
		{
			FinishReload(quick: false);
			return;
		}
		if (slide == null)
		{
			slide = SlideGrip(gun);
		}
		if (slide == null || leftHand == null)
		{
			if (since > 0.3f)
			{
				TryCharge();
				FinishReload(quick: false);
			}
			return;
		}
		Vector3 vector = ((gun.firePointTransform != null) ? (-gun.firePointTransform.forward) : (Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.back));
		if (!slideHeld)
		{
			leftAt = controls.vrRoot.InverseTransformPoint(slide.transform.position);
			leftTurn = Quaternion.Inverse(Quaternion.Euler(0f, bodyYaw, 0f)) * slide.transform.rotation;
			if (since > 0.3f)
			{
				slideHeld = Grab(leftHand, slide);
				slideFrom = slide.transform.position;
				reloadAt = Time.time;
				if (!slideHeld)
				{
					TryCharge();
					FinishReload(quick: false);
				}
			}
			return;
		}
		leftAt = controls.vrRoot.InverseTransformPoint(slideFrom + vector * Mathf.Clamp01(since / 0.15f) * 0.1f);
		if (since > 0.22f)
		{
			LetGoOf(slide, leftHand);
			slideHeld = false;
			if (gun.cartridgeState != Gun.CartridgeStates.UNSPENT)
			{
				TryCharge();
			}
			FinishReload(quick: false);
		}
	}

	private static Grip SlideGrip(Gun g)
	{
		if (g.otherGrips == null)
		{
			return null;
		}
		foreach (Grip otherGrip in g.otherGrips)
		{
			if (!(otherGrip == null) && otherGrip.gameObject.activeInHierarchy)
			{
				string text = otherGrip.name.ToLowerInvariant();
				if (text.Contains("slide") || text.Contains("charg") || text.Contains("bolt"))
				{
					return otherGrip;
				}
			}
		}
		return null;
	}

	private void TryCharge()
	{
		try
		{
			if (gun.slideState == Gun.SlideStates.LOCKED)
			{
				gun.SlideRelease();
			}
			if (gun.cartridgeState != Gun.CartridgeStates.UNSPENT)
			{
				gun.Charge();
			}
		}
		catch (System.Exception)
		{
		}
	}

	private void FinishReload(bool quick)
	{
		LabReloads++;
		if (!quick && gun != null)
		{
			try
			{
				if (!gun.HasMagazine() || (gun.AmmoCount() <= 0 && gun.cartridgeState != Gun.CartridgeStates.UNSPENT))
				{
					quick = true;
				}
			}
			catch (System.Exception)
			{
			}
		}
		reloadedAt = Time.time;
		if (gun != null)
		{
			NoPushOnItself(gun.transform, again: true);
		}
		if (!quick && gun != null)
		{
			try
			{
				int num = ((gun.defaultMagazine != null) ? gun.defaultMagazine.rounds : 0);
				if (gun.HasMagazine() && gun.MagazineState != null && num > 0 && gun.MagazineState.AmmoCount < num - 1)
				{
					Lab.Event("reload: the magazine in it had " + gun.MagazineState.AmmoCount + " of " + num + ": filled");
					gun.MagazineState.Refill();
				}
			}
			catch (System.Exception)
			{
			}
		}
		Lab.Event("chamber after: " + SafeState());
		Lab.Event("reload done " + (quick ? "QUICK (fallback)" : "by hand") + " at stage " + reload);
		if (quick && gun != null)
		{
			Load(reload: true);
			loadSettleUntil = Time.time + Tuning.F("gun.settle", 1.5f);
		}
		else if (gun != null)
		{
			MelonLogger.Msg("Bot Player: reloaded " + gun.name + " by hand (" + SafeAmmo() + " rounds)");
		}
		Hand leftHand = LeftHand;
		if (slideHeld && slide != null && leftHand != null)
		{
			LetGoOf(slide, leftHand);
		}
		slideHeld = false;
		reload = Reload.None;
		leftOverride = false;
		supportTries = 0;
		supportTriedAt = -10f;
		if (gun != null)
		{
			step = ((support != null) ? Step.Reach : Step.Ready);
			stepAt = Time.time;
			if (controls.leftController != null)
			{
				leftStart = Pose.Of(controls.leftController.transform);
			}
		}
		dryPulls = 0;
		wantedFireSince = -1f;
		Feel("reloaded");
	}

	private int SafeAmmo()
	{
		try
		{
			return gun.AmmoCount();
		}
		catch (System.Exception)
		{
			return -1;
		}
	}

	private void DropNewMag()
	{
		Hand leftHand = LeftHand;
		if (newMag != null && newMag.grip != null && leftHand != null)
		{
			LetGoOf(newMag.grip, leftHand);
		}
		if (newMag != null)
		{
			dropped.Add((newMag, Time.time));
		}
		newMag = null;
	}

	private static void FreeHand(Hand hand)
	{
		try
		{
			GameObject gameObject = ((hand != null) ? hand.m_CurrentAttachedGO : null);
			if (gameObject == null)
			{
				return;
			}
			MarrowEntity componentInParent = gameObject.GetComponentInParent<MarrowEntity>();
			Transform transform = ((componentInParent != null) ? componentInParent.transform : gameObject.transform.root);
			if (transform.GetComponent<RigManager>() == null)
			{
				foreach (Grip componentsInChild in transform.GetComponentsInChildren<Grip>(includeInactive: true))
				{
					LetGoOf(componentsInChild, hand);
				}
			}
			if (!(hand.m_CurrentAttachedGO == null))
			{
				Grip grip = gameObject.GetComponent<Grip>() ?? gameObject.GetComponentInParent<Grip>();
				if (grip != null)
				{
					grip.ForceDetach(hand);
				}
				if (hand.m_CurrentAttachedGO != null)
				{
					hand.DetachObject();
				}
			}
		}
		catch (System.Exception ex)
		{
			Lab.Event("hand could not be emptied: " + ex.Message);
		}
	}

	private static void LetGoOf(Grip g, Hand hand)
	{
		try
		{
			if (g != null && hand != null && g.attachedHands != null && g.attachedHands.Contains(hand))
			{
				g.ForceDetach(hand);
			}
		}
		catch (System.Exception)
		{
		}
	}

	private bool DownForRescue()
	{
		if (enemy || Puppet || Tuning.F("rescue", 1f) < 0.5f)
		{
			return false;
		}
		downedForRescue = true;
		rescuedBy = null;
		youOverSince = -1f;
		return true;
	}

	private bool WaitingForRescue()
	{
		if (!downedForRescue)
		{
			return false;
		}
		if (Time.time - diedAt > RescueWait)
		{
			downedForRescue = false;
			Lab.Event("rescue: nobody came, gone");
			return false;
		}
		Rigidbody rigidbody = Pelvis();
		Transform head = Player.Head;
		if (!(rigidbody != null) || !(head != null) || !((head.position - rigidbody.position).magnitude < Tuning.F("rescue.you.near", 1.6f)))
		{
			youOverSince = -1f;
		}
		else if (youOverSince < 0f)
		{
			youOverSince = Time.time;
		}
		else if (Time.time - youOverSince > Tuning.F("rescue.time", 3f))
		{
			Revive("you");
		}
		return true;
	}

	private void Revive(string by)
	{
		downedForRescue = false;
		dead = false;
		rescuedBy = null;
		hp = MaxHp * Tuning.F("rescue.health", 0.4f);
		hurtAt = Time.time;
		down = true;
		downAt = Time.time;
		downFor = 0.3f;
		Spawnables.Notify("A bot is back on its feet");
		Lab.Event("rescue: got up again by " + by);
	}

	private Bot BuddyDown(Vector3 from)
	{
		Bot result = null;
		float num = Tuning.F("rescue.range", 40f);
		foreach (Bot item in BotManager.All)
		{
			if (item == this || !item.downedForRescue || (item.rescuedBy != null && item.rescuedBy != this) || item.enemy != enemy)
			{
				continue;
			}
			Rigidbody rigidbody = item.Pelvis();
			if (!(rigidbody == null))
			{
				float magnitude = Flat(rigidbody.position - from).magnitude;
				if (magnitude < num)
				{
					num = magnitude;
					result = item;
				}
			}
		}
		return result;
	}

	private bool RescueStep(Transform head, float dt)
	{
		if (enemy || dead || down || surrendered || climbing || Puppet || head == null || (mode != Mode.Brain && mode != Mode.Follow && mode != Mode.Wander))
		{
			StopRescue(null);
			return false;
		}
		if (target.transform != null || Reloading)
		{
			StopRescue("busy fighting");
			return false;
		}
		if (rescuing == null || !rescuing.downedForRescue)
		{
			if (rescuing != null)
			{
				StopRescue(null);
			}
			rescuing = BuddyDown(head.position);
			if (rescuing == null)
			{
				return false;
			}
			rescuing.rescuedBy = this;
			rescueSince = Time.time;
			kneelSince = -1f;
			nav.Clear();
			Lab.Event("rescue: going to get " + rescuing.Name + " up");
		}
		Rigidbody rigidbody = rescuing.Pelvis();
		if (rigidbody == null || Time.time - rescueSince > Tuning.F("rescue.most", 25f))
		{
			StopRescue("could not get there");
			return false;
		}
		float magnitude = Flat(rigidbody.position - head.position).magnitude;
		if (kneelSince < 0f && magnitude > Tuning.F("rescue.reach", 1.1f))
		{
			if (!nav.HasGoal)
			{
				nav.SetGoal(rigidbody.position);
			}
			running = magnitude > 3f;
			if (Travel(running ? MotionLibrary.RunPush : MotionLibrary.WalkPush, 0.9f, dt) || nav.StuckCount >= 3)
			{
				nav.Clear();
			}
			return true;
		}
		nav.Clear();
		walk = Vector2.zero;
		running = false;
		Face(rigidbody.position, dt, 180f);
		if (kneelSince < 0f)
		{
			kneelSince = Time.time;
		}
		if (Time.time - kneelSince > Tuning.F("rescue.time", 3f))
		{
			Bot bot = rescuing;
			rescuing = null;
			kneelSince = -1f;
			bot.Revive(Name);
		}
		return true;
	}

	private void StopRescue(string why)
	{
		if (rescuing != null)
		{
			if (rescuing.rescuedBy == this)
			{
				rescuing.rescuedBy = null;
			}
			if (why != null)
			{
				Lab.Event("rescue: left off getting " + rescuing.Name + " up (" + why + ")");
			}
			rescuing = null;
			kneelSince = -1f;
		}
	}

	private void KneelPose()
	{
		if (rescuing == null || kneelSince < 0f)
		{
			return;
		}
		Rigidbody rigidbody = rescuing.Pelvis();
		Transform vrRoot = controls.vrRoot;
		if (!(rigidbody == null) && !(vrRoot == null))
		{
			float height = standingHeight - Tuning.F("rescue.kneel.drop", 1.15f);
			pose.height = height;
			Transform transform = null;
			try
			{
				transform = rescuing.rig.physicsRig.m_chest;
			}
			catch (System.Exception)
			{
			}
			Vector3 vector = vrRoot.InverseTransformPoint(((transform != null) ? transform.position : rigidbody.position) + Vector3.up * 0.1f);
			float y = Mathf.Abs(Mathf.Sin((Time.time - kneelSince) * 6f)) * 0.06f;
			pose.leftPos = vector + new Vector3(-0.1f, y, 0f);
			pose.rightPos = vector + new Vector3(0.1f, y, 0f);
			pose.leftRot = Quaternion.Euler(90f, 0f, 0f);
			pose.rightRot = Quaternion.Euler(90f, 0f, 0f);
		}
	}

	private void IgnoreCar(Transform car, bool ignore)
	{
		try
		{
			if (car == null || rig.physicsRig == null)
			{
				return;
			}
			Collider[]? array = car.GetComponentsInChildren<Collider>(includeInactive: true);
			Collider[] array2 = rig.physicsRig.GetComponentsInChildren<Collider>(includeInactive: true);
			Collider[] array3 = array;
			foreach (Collider collider in array3)
			{
				if (collider == null || collider.isTrigger)
				{
					continue;
				}
				Collider[] array4 = array2;
				foreach (Collider collider2 in array4)
				{
					if (collider2 != null && !collider2.isTrigger)
					{
						Physics.IgnoreCollision(collider, collider2, ignore);
					}
				}
			}
		}
		catch (System.Exception ex)
		{
			Lab.Event("ride: car collisions not " + (ignore ? "ignored" : "back") + ": " + ex.Message);
		}
	}

	public static void LabSpawnCrate(string barcode)
	{
		Transform head = Player.Head;
		if (!(head == null))
		{
			Vector3 normalized = Flat(head.forward).normalized;
			Vector3 vector = head.position + normalized * 6f;
			if (Physics.Raycast(vector + Vector3.up * 3f, Vector3.down, out var hitInfo, 10f, -1, QueryTriggerInteraction.Ignore))
			{
				vector = hitInfo.point + Vector3.up * 0.5f;
			}
			Lab.Event("crate: " + barcode + " put 6 m in front of you");
			HelperMethods.SpawnCrate(barcode, vector, Quaternion.LookRotation(Vector3.Cross(normalized, Vector3.up)), Vector3.one);
		}
	}

	public static void LabSeats()
	{
		Transform head = Player.Head;
		System.Collections.Generic.Dictionary<System.IntPtr, System.Collections.Generic.List<Seat>> dictionary = new System.Collections.Generic.Dictionary<System.IntPtr, System.Collections.Generic.List<Seat>>();
		System.Collections.Generic.Dictionary<System.IntPtr, string> dictionary2 = new System.Collections.Generic.Dictionary<System.IntPtr, string>();
		foreach (Seat item in UnityEngine.Object.FindObjectsOfType<Seat>())
		{
			if (!(item == null) && !(head == null) && !((item.transform.position - head.position).magnitude > 30f))
			{
				Poolee componentInParent = item.GetComponentInParent<Poolee>();
				System.IntPtr key = ((componentInParent != null) ? componentInParent.Pointer : item.transform.root.Pointer);
				if (!dictionary.ContainsKey(key))
				{
					dictionary[key] = new System.Collections.Generic.List<Seat>();
					dictionary2[key] = ((componentInParent != null) ? componentInParent.name : item.transform.root.name);
				}
				dictionary[key].Add(item);
			}
		}
		foreach (System.Collections.Generic.KeyValuePair<System.IntPtr, System.Collections.Generic.List<Seat>> item2 in dictionary)
		{
			string text = "";
			foreach (Seat item3 in item2.Value)
			{
				text = text + item3.name + ((item3.rigManager != null) ? " (taken)" : "") + "; ";
			}
			Lab.Event("seats: " + dictionary2[item2.Key] + " has " + item2.Value.Count + ": " + text);
		}
		if (dictionary.Count == 0)
		{
			Lab.Event("seats: no vehicles near you");
		}
	}

	private Seat FreeSeat(float within, bool driverToo)
	{
		Transform head = Head;
		Seat result = null;
		float num = within;
		foreach (Seat item in UnityEngine.Object.FindObjectsOfType<Seat>())
		{
			if (!(item == null) && !(head == null) && !(item.rigManager != null) && item.gameObject.activeInHierarchy && (driverToo || !item.name.ToLowerInvariant().Contains("driver")))
			{
				float magnitude = (item.transform.position - head.position).magnitude;
				if (magnitude < num)
				{
					num = magnitude;
					result = item;
				}
			}
		}
		return result;
	}

	public void LabRide()
	{
		if (rig.activeSeat != null)
		{
			Lab.Event("ride: sat in " + rig.activeSeat.name + " already");
			return;
		}
		Seat seat = FreeSeat(Tuning.F("ride.seek", 15f), driverToo: true);
		if (seat == null)
		{
			Lab.Event("ride: no free seat near");
			return;
		}
		float num = ((Head != null) ? (seat.transform.position - Head.position).magnitude : (-1f));
		try
		{
			StopPlaying();
			nav.Clear();
			Poolee componentInParent = seat.GetComponentInParent<Poolee>();
			ridingCar = ((componentInParent != null) ? componentInParent.transform : seat.transform.root);
			IgnoreCar(ridingCar, ignore: true);
			seat.IngressRig(rig);
			riding = seat;
			Lab.Event("ride: into " + seat.name + " of " + ((seat.GetComponentInParent<Poolee>() != null) ? seat.GetComponentInParent<Poolee>().name : "?") + " from " + num.ToString("0.0") + " m off");
		}
		catch (System.Exception ex)
		{
			Lab.Event("ride: the way in failed: " + ex.Message);
		}
	}

	public void LabRideSay(string when)
	{
		Rigidbody rigidbody = Pelvis();
		Seat activeSeat = rig.activeSeat;
		Lab.Event("ride (" + when + "): seat " + ((activeSeat != null) ? activeSeat.name : "none") + ", state " + rig.bodyState.ToString() + ((riding != null && rigidbody != null) ? (", hips " + (rigidbody.position - riding.transform.position).magnitude.ToString("0.00") + " m from the seat") : ""));
	}

	public void LabUnride()
	{
		try
		{
			if (rig.activeSeat != null)
			{
				rig.activeSeat.EgressRig(immediate: true);
			}
			IgnoreCar(ridingCar, ignore: false);
			ridingCar = null;
			Lab.Event("ride: out");
		}
		catch (System.Exception ex)
		{
			Lab.Event("ride: the way out failed: " + ex.Message);
		}
		riding = null;
	}

	private static Transform CarOf(Seat seat)
	{
		if (seat == null)
		{
			return null;
		}
		Poolee componentInParent = seat.GetComponentInParent<Poolee>();
		if (!(componentInParent != null))
		{
			return seat.transform.root;
		}
		return componentInParent.transform;
	}

	private static bool HasDriver(Transform car)
	{
		if (car == null)
		{
			return false;
		}
		Seat seat = ((Player.RigManager != null) ? Player.RigManager.activeSeat : null);
		if (seat != null && CarOf(seat) == car)
		{
			return true;
		}
		foreach (Bot item in BotManager.All)
		{
			Seat activeSeat = item.rig.activeSeat;
			if (activeSeat != null && (item.DrivingNow || item.labHost) && CarOf(activeSeat) == car)
			{
				return true;
			}
		}
		return false;
	}

	private static int BoardingInto(Transform car)
	{
		int num = 0;
		foreach (Bot item in BotManager.All)
		{
			if (item.boardingCar == car && item.boardingSeat != null)
			{
				num++;
			}
		}
		return num;
	}

	private static float CarSpeed(Transform car)
	{
		Rigidbody rigidbody = ((car != null) ? car.GetComponentInChildren<Rigidbody>() : null);
		if (!(rigidbody != null))
		{
			return 0f;
		}
		return rigidbody.velocity.magnitude;
	}

	private Seat SeatIn(Transform car)
	{
		Transform head = Head;
		Seat result = null;
		float num = float.MaxValue;
		foreach (Seat componentsInChild in car.GetComponentsInChildren<Seat>(includeInactive: false))
		{
			if (componentsInChild == null || componentsInChild.rigManager != null)
			{
				continue;
			}
			bool flag = false;
			foreach (Bot item in BotManager.All)
			{
				flag |= item != this && item.boardingSeat == componentsInChild;
			}
			if (!flag)
			{
				float num2 = ((head != null) ? (componentsInChild.transform.position - head.position).magnitude : 0f);
				if (num2 < num)
				{
					num = num2;
					result = componentsInChild;
				}
			}
		}
		return result;
	}

	private bool FreeToBoard()
	{
		if (!enemy && !dead && !down && !surrendered && !Puppet && !climbing && !InLesson && !raceLineUp && rescuing == null && target.transform == null && !Reloading && (mode == Mode.Brain || mode == Mode.Follow || mode == Mode.Wander))
		{
			return Tuning.F("ride.along", 1f) > 0.5f;
		}
		return false;
	}

	private bool FindRide(Transform head, out Transform car, out Seat seat)
	{
		car = null;
		seat = null;
		float num = float.MaxValue;
		System.Collections.Generic.List<Transform> list = new System.Collections.Generic.List<Transform>();
		Seat seat2 = ((Player.RigManager != null) ? Player.RigManager.activeSeat : null);
		if (seat2 != null)
		{
			list.Add(CarOf(seat2));
		}
		foreach (Bot item in BotManager.All)
		{
			if (item != this && (item.DrivingNow || item.labHost) && item.rig.activeSeat != null && !item.enemy)
			{
				list.Add(CarOf(item.rig.activeSeat));
			}
		}
		foreach (Transform item2 in list)
		{
			if (item2 == null || CarSpeed(item2) > Tuning.F("ride.board.speed", 1.5f))
			{
				continue;
			}
			bool flag = seat2 != null && CarOf(seat2) == item2;
			float magnitude = Flat(item2.position - head.position).magnitude;
			if (!(magnitude > (flag ? Tuning.F("ride.you.range", 30f) : Tuning.F("ride.bot.range", 15f))) && !(magnitude >= num))
			{
				Seat seat3 = SeatIn(item2);
				if (!(seat3 == null))
				{
					num = magnitude;
					car = item2;
					seat = seat3;
				}
			}
		}
		return car != null;
	}

	private void GetOut(string why)
	{
		Lab.Event("ride: getting out (" + why + ")");
		try
		{
			if (rig.activeSeat != null)
			{
				rig.activeSeat.EgressRig(immediate: true);
			}
		}
		catch (System.Exception ex)
		{
			Lab.Event("ride: the way out failed: " + ex.Message);
		}
		IgnoreCar(ridingCar, ignore: false);
		IgnoreGunCar(ridingCar, ignore: false);
		riding = null;
		ridingCar = null;
		noDriverSince = -1f;
		Pull(on: false);
	}

	private bool RideStep(Transform head, float dt)
	{
		if (lessonCar != null && rig.activeSeat == null && !InLesson)
		{
			IgnoreCar(lessonCar, ignore: false);
			lessonCar = null;
		}
		if (head == null || dead || down)
		{
			return false;
		}
		if (riding != null)
		{
			if (rig.activeSeat == null)
			{
				IgnoreCar(ridingCar, ignore: false);
				riding = null;
				ridingCar = null;
				return false;
			}
			walk = Vector2.zero;
			running = false;
			headYaw = Mathf.Sin(Time.time * 0.4f + born) * 50f;
			RideShoot(head, dt);
			if (HasDriver(ridingCar))
			{
				noDriverSince = -1f;
			}
			else if (noDriverSince < 0f)
			{
				noDriverSince = Time.time;
			}
			else if (Time.time - noDriverSince > Tuning.F("ride.out.after", 1.2f) && CarSpeed(ridingCar) < 2f)
			{
				GetOut("nobody at the wheel");
			}
			return true;
		}
		if (boardingSeat != null)
		{
			bool flag = boardingSeat.rigManager == null && HasDriver(boardingCar) && CarSpeed(boardingCar) < Tuning.F("ride.board.speed", 1.5f) + 1f;
			if (!flag || !FreeToBoard() || Time.time - boardingSince > Tuning.F("ride.board.most", 20f))
			{
				Lab.Event("ride: not getting in after all (" + ((!flag) ? "the seat or the car went" : ((!FreeToBoard()) ? "something else came up" : "too long")) + ")");
				boardingSeat = null;
				boardingCar = null;
				return false;
			}
			float magnitude = Flat(boardingSeat.transform.position - head.position).magnitude;
			if (magnitude > Tuning.F("ride.in", 2.5f))
			{
				nav.SetGoal(boardingSeat.transform.position);
				running = magnitude > 4f;
				if (Travel(running ? MotionLibrary.RunPush : MotionLibrary.WalkPush, 1.5f, dt) || nav.StuckCount >= 3)
				{
					nav.Clear();
				}
				return true;
			}
			nav.Clear();
			walk = Vector2.zero;
			try
			{
				if (gun == null)
				{
					StopPlaying();
				}
				ridingCar = boardingCar;
				IgnoreCar(ridingCar, ignore: true);
				IgnoreGunCar(ridingCar, ignore: true);
				boardingSeat.IngressRig(rig);
				riding = boardingSeat;
				noDriverSince = -1f;
				Lab.Event("ride: got in (" + boardingSeat.name + ")" + ((gun != null) ? (", its " + gun.name + " in hand") : ", nothing in hand"));
			}
			catch (System.Exception ex)
			{
				IgnoreCar(ridingCar, ignore: false);
				ridingCar = null;
				Lab.Event("ride: could not get in: " + ex.Message);
			}
			boardingSeat = null;
			boardingCar = null;
			return true;
		}
		if (Time.time < boardLookAt)
		{
			return false;
		}
		boardLookAt = Time.time + 0.5f;
		if (!FreeToBoard() || !FindRide(head, out var car, out var seat))
		{
			return false;
		}
		boardingCar = car;
		boardingSeat = seat;
		boardingSince = Time.time;
		nav.Clear();
		bool flag2 = Player.RigManager != null && Player.RigManager.activeSeat != null && CarOf(Player.RigManager.activeSeat) == car;
		Lab.Event("ride: going to get in with " + (flag2 ? "you" : "a bot driving") + " (" + seat.name + ")");
		return true;
	}

	private bool WaitForRiders(Seat seat)
	{
		if (Time.time - driveTookAt > Tuning.F("ride.wait", 8f))
		{
			return false;
		}
		return BoardingInto(CarOf(seat)) > 0;
	}

	private bool BeginGo(Transform you, Vector3 feet)
	{
		Transform head = Head;
		if (head == null)
		{
			return false;
		}
		Vector3 vector = feet + Vector3.up * 1.3f;
		System.Collections.Generic.List<(Vector3, Vector3, Sight, string, float)> list = new System.Collections.Generic.List<(Vector3, Vector3, Sight, string, float)>();
		Vector3 vector2 = ((roamHeading.sqrMagnitude > 0.01f) ? roamHeading : (Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward));
		float num = UnityEngine.Random.Range(0f, 22.5f);
		for (int i = 0; i < 16; i++)
		{
			Vector3 vector3 = Quaternion.Euler(0f, num + (float)i * 22.5f, 0f) * Vector3.forward;
			RaycastHit hitInfo;
			float num2 = (Physics.Raycast(vector, vector3, out hitInfo, 45f, -1, QueryTriggerInteraction.Ignore) ? hitInfo.distance : 45f);
			if (!(num2 < 9f) && Ground(vector + vector3 * (num2 * UnityEngine.Random.Range(0.5f, 0.85f)), out var ground) && !(Mathf.Abs(ground.y - feet.y) > 4f))
			{
				list.Add((ground, ground + vector3 * 6f + Vector3.up * 1.4f, Sight.Open, "seeing what is over there", 0.9f + num2 / 45f));
			}
		}
		HashSet<System.IntPtr> hashSet = new HashSet<System.IntPtr>();
		int num3 = 0;
		foreach (Collider item2 in Physics.OverlapSphere(vector, Tuning.F("roam.thing.far", 40f), -1, QueryTriggerInteraction.Ignore))
		{
			Rigidbody rigidbody = ((item2 != null) ? item2.attachedRigidbody : null);
			if (rigidbody == null || rigidbody.isKinematic || !hashSet.Add(rigidbody.Pointer) || rigidbody.mass > 60f || rigidbody.GetComponentInParent<RigManager>() != null)
			{
				continue;
			}
			string text = rigidbody.name.ToLowerInvariant();
			if (rigidbody.mass < 0.25f || text.Contains("cartridge") || text.Contains("casing") || text.Contains("shell") || text.Contains("bullet") || text.StartsWith("mag_") || NotForBots(rigidbody))
			{
				continue;
			}
			Vector3 worldCenterOfMass = rigidbody.worldCenterOfMass;
			if (!((worldCenterOfMass - feet).magnitude < 3f) && !(Mathf.Abs(worldCenterOfMass.y - feet.y) > 2.5f) && !Targets.NearPlayer(worldCenterOfMass, 2f) && Ground(worldCenterOfMass - Flat(worldCenterOfMass - feet).normalized * 1.1f, out var ground2))
			{
				list.Add((ground2, worldCenterOfMass, Sight.Thing, "having a look at the " + ThingName(rigidbody), Tuning.F("roam.thing", 2.6f)));
				if (++num3 >= 6)
				{
					break;
				}
			}
		}
		foreach (Bot item3 in BotManager.All)
		{
			if (item3 != this && !(item3.Head == null) && !item3.dead && item3.enemy == enemy)
			{
				Vector3 v = item3.Head.position - head.position;
				if (!(v.magnitude < 5f) && !(v.magnitude > 45f) && Ground(item3.Head.position - Flat(v).normalized * 2f, out var ground3))
				{
					list.Add((ground3, item3.Head.position, Sight.Bot, "going over to the other one", 0.8f));
				}
			}
		}
		if (FarPlace(feet, out var place))
		{
			list.Add((place, place + Vector3.up * 1.4f, Sight.New, "going somewhere new", 1.1f * personality.curiosity));
		}
		bool flag = false;
		foreach (Bot item4 in BotManager.All)
		{
			flag |= item4 != this && item4.DrivingNow;
		}
		if (Time.time > driveNextAt && Tuning.F("drive.want", 1.3f) > 0f && !flag)
		{
			Lessons.Lesson lesson = Lessons.Drive();
			Poolee poolee = ((lesson != null && !BotManager.OwnSpawns) ? AnyVehicle(Tuning.F("drive.seek", 60f)) : null);
			Vector3 place2;
			if (lesson != null && !BotManager.OwnSpawns)
			{
				if (poolee != null && Ground(poolee.transform.position - Flat(poolee.transform.right).normalized * 1.3f + Vector3.up, out var ground4))
				{
					list.Add((ground4, poolee.transform.position + Vector3.up * 0.6f, Sight.Drive, "going for a drive in the " + lesson.name + " over there", Tuning.F("drive.want.map", 2.6f) * personality.curiosity));
				}
			}
			else if (lesson != null && FarPlace(feet, out place2) && Flat(place2 - feet).magnitude > 18f)
			{
				list.Add((place2, place2 + Vector3.up * 1.4f, Sight.Drive, DriveSkill.Practice ? "practising its driving" : ("going for a drive in the " + lesson.name), DriveSkill.Practice ? 1000f : (Tuning.F("drive.want", 1.3f) * personality.energy)));
			}
		}
		if (you != null && Flat(you.position - feet).magnitude > 6f && Flat(you.position - feet).magnitude < 40f && Ground(you.position - Vector3.up * 1.4f - Flat(you.position - feet).normalized * 2.5f, out var ground5))
		{
			list.Add((ground5, you.position, Sight.You, (YouStoodFor > Tuning.F("you.still", 50f)) ? "coming over, you have been there a while" : "coming to see what you are doing", BotManager.StayNear ? 1.6f : ((YouStoodFor > Tuning.F("you.still", 50f) && Time.time - cameOverAt > Tuning.F("you.still.every", 150f)) ? 1.4f : 0.2f)));
		}
		if (list.Count == 0)
		{
			return false;
		}
		float num4 = 0f;
		float[] array = new float[list.Count];
		for (int j = 0; j < list.Count; j++)
		{
			(Vector3, Vector3, Sight, string, float) tuple = list[j];
			Vector3 vector4 = Flat(tuple.Item1 - feet);
			float item = tuple.Item5;
			item *= (visited.Contains(Cell(tuple.Item1)) ? 0.6f : 1.5f);
			item *= 1f + 0.55f * Vector3.Dot(vector2.normalized, vector4.normalized);
			item /= 1f + (float)SelfTaught.BadAt(tuple.Item1);
			if (tuple.Item3 == lastSight && tuple.Item3 != 0)
			{
				item *= 0.45f;
			}
			item *= UnityEngine.Random.Range(0.7f, 1.3f);
			array[j] = item * item;
			num4 += array[j];
		}
		float num5 = UnityEngine.Random.value * num4;
		int index = list.Count - 1;
		for (int k = 0; k < list.Count; k++)
		{
			num5 -= array[k];
			if (num5 <= 0f)
			{
				index = k;
				break;
			}
		}
		if (list[index].Item3 == Sight.You)
		{
			cameOverAt = Time.time;
		}
		planPlace = list[index].Item1;
		planLook = list[index].Item2;
		planSight = list[index].Item3;
		planWhy = list[index].Item4;
		lastSight = planSight;
		float magnitude = Flat(planPlace - feet).magnitude;
		planRun = magnitude > 22f && UnityEngine.Random.value < 0.18f * personality.energy;
		planPush = ((magnitude < 9f) ? (Tuning.F("life.stroll", 0.45f) * UnityEngine.Random.Range(0.9f, 1.3f)) : (MotionLibrary.WalkPush * UnityEngine.Random.Range(0.8f, 1f)));
		detours = 0;
		onDetour = false;
		viaLeft = false;
		if (magnitude > 9f && !Physics.Linecast(vector, planPlace + Vector3.up * 1.2f, -1, QueryTriggerInteraction.Ignore))
		{
			Vector3 vector5 = Vector3.Cross(Vector3.up, Flat(planPlace - feet).normalized) * (magnitude * UnityEngine.Random.Range(-1f, 1f) * Tuning.F("roam.bend", 0.13f));
			Vector3 p = feet + Flat(planPlace - feet) * UnityEngine.Random.Range(0.4f, 0.6f) + vector5;
			if (vector5.magnitude > 1.2f && Ground(p, out var ground6) && Mathf.Abs(ground6.y - feet.y) < 1.5f && !Physics.Linecast(vector, ground6 + Vector3.up * 1.2f, -1, QueryTriggerInteraction.Ignore))
			{
				viaPlace = ground6;
				viaLeft = true;
			}
		}
		thinking2 = true;
		thinkUntil = Time.time + UnityEngine.Random.Range(0.9f, 2.4f) * Mathf.Lerp(0.8f, 1.3f, Mathf.InverseLerp(0.5f, 1.8f, personality.calm));
		actUntil = Time.time + 90f;
		planSince = Time.time;
		pauseNext = Time.time + UnityEngine.Random.Range(6f, 14f);
		Look(planLook, null, "where it means to go", thinkUntil - Time.time + 0.6f);
		Lab.Event("roam: " + planWhy + " (" + magnitude.ToString("0") + " m" + (viaLeft ? ", round by the side" : "") + (planRun ? ", at a run" : "") + ")");
		return true;
	}

	public static bool NotForBots(Rigidbody rb)
	{
		try
		{
			string text = (ThingName(rb) + " " + rb.name).ToLowerInvariant().Replace(" ", "").Replace("_", "");
			return text.Contains("spawngun") || text.Contains("nimbus") || text.Contains("devmanipulator") || text.Contains("utilitygun");
		}
		catch (System.Exception)
		{
			return false;
		}
	}

	private static string ThingName(Rigidbody rb)
	{
		try
		{
			Poolee componentInParent = rb.GetComponentInParent<Poolee>();
			if (componentInParent != null && componentInParent.SpawnableCrate != null)
			{
				return Lessons.NameOf(componentInParent);
			}
		}
		catch (System.Exception)
		{
		}
		return "thing there";
	}

	private void GoStep(Transform you, Transform head, float dt)
	{
		headYaw = 0f;
		Vector3 vector = ((rig.physicsRig != null && rig.physicsRig.centerOfPressure != null) ? rig.physicsRig.centerOfPressure.position : Feet);
		if (thinking2)
		{
			walk = Vector2.zero;
			running = false;
			Face(planPlace, dt, 70f);
			if (Time.time < thinkUntil)
			{
				return;
			}
			thinking2 = false;
			nav.Clear();
			driveOnRoad = false;
			if ((planSight != Sight.Drive || BotManager.OwnSpawns) && planSight == Sight.Drive)
			{
				Driving.Model model = Lessons.Road();
				if (model == null || (!DriveSkill.Practice && !(UnityEngine.Random.value < Tuning.F("drive.theirway", 0.7f))))
				{
					if (!DriveTo(planPlace, planWhy))
					{
						Idle(UnityEngine.Random.Range(1f, 2.5f));
					}
					return;
				}
				Vector3 vector2 = model.road[0];
				foreach (Vector3 item in model.road)
				{
					if (Flat(item - vector).sqrMagnitude < Flat(vector2 - vector).sqrMagnitude)
					{
						vector2 = item;
					}
				}
				if (Flat(vector2 - vector).magnitude < 8f)
				{
					if (!DriveTheirWay())
					{
						Idle(UnityEngine.Random.Range(1f, 2.5f));
					}
					return;
				}
				driveOnRoad = true;
				viaLeft = false;
				planPlace = (Ground(vector2 + Vector3.up, out var ground) ? ground : vector2);
				Lab.Event("roam: to the way you drove first (" + Flat(planPlace - vector).magnitude.ToString("0") + " m), to set off from there");
			}
			nav.SetGoal(viaLeft ? viaPlace : planPlace);
		}
		if (Time.time < pauseUntil)
		{
			walk = Vector2.zero;
			running = false;
			return;
		}
		if (Time.time >= pauseNext && !climbing)
		{
			pauseNext = Time.time + UnityEngine.Random.Range(7f, 16f);
			if (UnityEngine.Random.value < Tuning.F("roam.pause", 0.4f) && Flat(planPlace - vector).magnitude > 5f)
			{
				pauseUntil = Time.time + UnityEngine.Random.Range(1.2f, 3f) * personality.calm;
				gazeUntil = 0f;
				return;
			}
		}
		if (viaLeft && Flat(viaPlace - vector).magnitude < Tuning.F("roam.bend.round", 4.5f))
		{
			viaLeft = false;
			nav.SetGoal(planPlace);
		}
		if (onDetour && Flat(nav.Goal - vector).magnitude < 1.5f)
		{
			onDetour = false;
			nav.SetGoal(detourFor);
		}
		if (!nav.HasGoal && !climbing)
		{
			nav.SetGoal(planPlace);
		}
		running = planRun && !onDetour;
		float magnitude = Flat(planPlace - vector).magnitude;
		float speed = (running ? MotionLibrary.RunPush : (planPush * Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(magnitude / 3f))));
		bool num = Travel(speed, (planSight == Sight.Bot || planSight == Sight.You) ? 2.2f : 0.9f, dt) || nav.StuckCount >= 3;
		if (walk.sqrMagnitude > 0.01f)
		{
			roamHeading = Quaternion.Euler(0f, bodyYaw, 0f) * new Vector3(walk.x, 0f, walk.y).normalized;
		}
		if (!num || climbing)
		{
			return;
		}
		running = false;
		if (viaLeft || onDetour || !(Flat(planPlace - vector).magnitude < 3f) || !(Mathf.Abs(planPlace.y - vector.y) < 1.6f))
		{
			if (viaLeft)
			{
				viaLeft = false;
				nav.Clear();
				nav.SetGoal(planPlace);
				return;
			}
			Vector3 vector3 = (onDetour ? detourFor : planPlace);
			if (detours < 3 && WayRound(vector, vector3, out var side))
			{
				detours++;
				onDetour = true;
				detourFor = vector3;
				nav.Clear();
				nav.SetGoal(side);
				Lab.Event("roam: something in the way, going round");
			}
			else if (planSight != Sight.Drive || !driveOnRoad || !(Flat(planPlace - vector).magnitude < 15f) || !DriveTheirWay())
			{
				Lab.Event("roam: no way there, another idea (" + Flat(planPlace - vector).magnitude.ToString("0") + " m short; walking ended: " + travelEnded + ", stuck " + nav.StuckCount + ")");
				SelfTaught.Bad(planPlace);
				nav.Clear();
				Idle(UnityEngine.Random.Range(1f, 2.5f));
			}
			return;
		}
		nav.Clear();
		visited.Add(Cell(vector));
		float num2 = UnityEngine.Random.Range(4f, 10f) * personality.calm;
		switch (planSight)
		{
		case Sight.Thing:
			if (UnityEngine.Random.value < Tuning.F("roam.take", 0.9f) && FindSomethingToPlayWith())
			{
				act = Act.Play;
				actUntil = Time.time + 30f;
				return;
			}
			break;
		case Sight.High:
			num2 *= 1.5f;
			break;
		case Sight.Drive:
		{
			if ((driveOnRoad && DriveTheirWay()) || (!BotManager.OwnSpawns && ((Lessons.Road() != null && DriveTheirWay()) || (FarPlace(vector, out var place) && DriveTo(place, "going for a drive")))))
			{
				return;
			}
			break;
		}
		case Sight.Tall:
			if (ClimbWithTool(planWall.foot, planWall.inward, planWall.height))
			{
				return;
			}
			break;
		}
		Idle(num2);
		Look(planLook, null, "what it came to see", Mathf.Min(num2, UnityEngine.Random.Range(2.5f, 5f)));
		Lab.Event("roam: there (" + planWhy + "), staying " + num2.ToString("0") + " s");
	}

	private bool WayRound(Vector3 feet, Vector3 goal, out Vector3 side)
	{
		side = feet;
		Vector3 origin = feet + Vector3.up * 1.2f;
		Vector3 normalized = Flat(goal - feet).normalized;
		float num = 0f;
		float[] array = new float[8] { 40f, -40f, 70f, -70f, 100f, -100f, 130f, -130f };
		foreach (float num2 in array)
		{
			Vector3 vector = Quaternion.Euler(0f, num2, 0f) * normalized;
			RaycastHit hitInfo;
			float num3 = (Physics.SphereCast(origin, 0.3f, vector, out hitInfo, 8f, -1, QueryTriggerInteraction.Ignore) ? hitInfo.distance : 8f);
			if (!(num3 < 2.5f) && Ground(feet + vector * Mathf.Min(num3 * 0.75f, 6f), out var ground) && !(Mathf.Abs(ground.y - feet.y) > 1.2f) && SelfTaught.BadAt(ground) < 2)
			{
				float num4 = num3 * (1f - Mathf.Abs(num2) / 220f) + (Physics.Linecast(ground + Vector3.up * 1.2f, goal + Vector3.up * 1.2f, -1, QueryTriggerInteraction.Ignore) ? 0f : 4f);
				if (num4 > num)
				{
					num = num4;
					side = ground;
				}
			}
		}
		return num > 0f;
	}

	private void SitFrame()
	{
		sitLifeFrame = Time.frameCount;
	}

	public void LabSit(bool on)
	{
		labSitting = on;
		LabSitSay(on ? "sit asked" : "stand asked");
	}

	private void SitPose(float t)
	{
		bool flag = labSitting || Time.frameCount - sitLifeFrame <= 2;
		if (flag != sitting)
		{
			sitting = flag;
			sitSince = Time.time;
			Lab.Event(flag ? "sit: down on the ground for a rest" : "sit: up again");
		}
		if (sitting)
		{
			walk = Vector2.zero;
			float num = Mathf.Sin(t * 1.4f);
			float num2 = standingHeight - Tuning.F("sit.drop", 1.3f) + num * 0.004f;
			pose.height = num2;
			pose.leftPos = new Vector3(-0.2f, num2 - 0.5f, 0.32f);
			pose.rightPos = new Vector3(0.2f, num2 - 0.5f, 0.32f);
			pose.leftRot = Quaternion.Euler(80f, 10f, 0f);
			pose.rightRot = Quaternion.Euler(80f, -10f, 0f);
		}
	}

	public void LabSitSay(string when)
	{
		PhysicsRig physicsRig = rig.physicsRig;
		Rigidbody rigidbody = Pelvis();
		Vector3 origin = ((rigidbody != null) ? rigidbody.position : Feet);
		foreach (RaycastHit item in Physics.RaycastAll(origin, Vector3.down, 3f, -1, QueryTriggerInteraction.Ignore))
		{
			if (item.collider != null && item.collider.GetComponentInParent<RigManager>() != rig && item.point.y < origin.y)
			{
				origin = item.point;
			}
		}
		Lab.Event("sit (" + when + "): state " + rig.bodyState.ToString() + ", loco " + (physicsRig != null && physicsRig.ballLocoEnabled) + ", hips " + ((rigidbody != null) ? (rigidbody.position.y - origin.y).ToString("0.00") : "?") + " m over the ground, head " + ((physicsRig != null && physicsRig.m_head != null) ? (physicsRig.m_head.position.y - origin.y).ToString("0.00") : "?") + " m, headset at " + ((controls.headset != null && controls.vrRoot != null) ? controls.vrRoot.InverseTransformPoint(controls.headset.position).y.ToString("0.00") : "?") + " m, pose " + pose.height.ToString("0.00"));
	}

	private void WaveOfTheirs()
	{
		waveClip = MoveSlots.Pick(MoveSlots.Kind.Wave, null, avatarCode);
		if (waveClip != null)
		{
			waveUntil = waveStart + waveClip[waveClip.Length - 1].time;
		}
	}

	private void OverClip(Frame[] from, float t, float head)
	{
		float time = from[^1].time;
		Frame frame = MotionLibrary.Sample(from, t);
		float num = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(t, time - t) / 0.25f));
		float num2 = ((frame.height > 0.3f) ? (pose.height / frame.height) : 1f);
		pose.leftPos = Vector3.Lerp(pose.leftPos, frame.leftPos * num2, num);
		pose.rightPos = Vector3.Lerp(pose.rightPos, frame.rightPos * num2, num);
		pose.leftRot = Quaternion.Slerp(pose.leftRot, frame.leftRot, num);
		pose.rightRot = Quaternion.Slerp(pose.rightRot, frame.rightRot, num);
		pose.head = Quaternion.Slerp(pose.head, frame.head, num * head);
		curl = -1f;
		pose.leftGrip = frame.leftGrip;
		pose.rightGrip = frame.rightGrip;
	}

	private void JumpOfTheirs()
	{
		if (jumpClip != null)
		{
			float num = Time.time - jumpedAt + MoveSlots.JumpAt(jumpClip);
			if (num >= jumpClip[jumpClip.Length - 1].time || Time.time - jumpedAt > 3f)
			{
				jumpClip = null;
			}
			else
			{
				OverClip(jumpClip, num, 0.6f);
			}
		}
	}

	private void StartCrouchOfTheirs()
	{
		crouchClip = MoveSlots.Pick(MoveSlots.Kind.Crouch, null, avatarCode);
		if (crouchClip == null)
		{
			float num = Random(2f, 5f);
			Squat(num, Random(0.4f, 0.75f));
			actUntil = Mathf.Max(actUntil, Time.time + num + 0.5f);
			Lab.Event("crouches");
		}
		else
		{
			crouchStart = Time.time;
			actUntil = Mathf.Max(actUntil, Time.time + crouchClip[crouchClip.Length - 1].time + 0.5f);
			Lab.Event("crouches as you do");
		}
	}

	private void CrouchOfTheirs()
	{
		if (crouchClip != null)
		{
			float num = Time.time - crouchStart;
			bool flag = walk.sqrMagnitude > 0.01f;
			if (num >= crouchClip[crouchClip.Length - 1].time || flag || act != 0 || Playing || climbing)
			{
				crouchClip = null;
				crouchReset = true;
				return;
			}
			Frame frame = MotionLibrary.Sample(crouchClip, num);
			float height = pose.height;
			OverClip(crouchClip, num, 0.5f);
			float num2 = ((crouchClip[0].height > 0.3f) ? crouchClip[0].height : standingHeight);
			pose.height = height * Mathf.Clamp(frame.height / num2, 0.35f, 1.05f);
			crouching = true;
			crouch = frame.crouch;
			feet = frame.feet;
		}
	}

	public void Preview(Frame[] clip)
	{
		if (clip != null && clip.Length >= 2 && !dead && !Down && !climbing && !InLesson)
		{
			Mode mode = ((this.mode != Mode.Replay && this.mode != Mode.Clone) ? this.mode : Mode.Stand);
			previewClip = clip;
			SetMode(Mode.Replay);
			previewing = this.mode == Mode.Replay;
			previewBack = mode;
			Lab.Event("moves: shown on a bot (" + clip[^1].time.ToString("0.0") + " s)");
		}
	}

	private void SmoothReset()
	{
		smoothHas = false;
	}

	private static float Left(float since, float time)
	{
		float num = Mathf.Clamp01(since / Mathf.Max(0.01f, time));
		return 1f - num * num * num * (num * (num * 6f - 15f) + 10f);
	}

	private void Smooth(float dt)
	{
		float num = Tuning.F("smooth.time", 0.32f);
		if (Time.frameCount - smoothFrame > 2)
		{
			smoothHas = false;
		}
		smoothFrame = Time.frameCount;
		if (num <= 0f || dt <= 0f || !smoothHas || Time.time < smoothSkipUntil)
		{
			smoothOut = pose;
			smoothHas = true;
			easeLeft.moving = (easeLeft.turning = (easeRight.moving = (easeRight.turning = (easeHead.turning = false))));
			easeHeightAt = -10f;
			return;
		}
		int num2 = (handOverride ? 1 : 0) | (Reloading ? 2 : 0) | ((Time.time < waveUntil) ? 4 : 0) | ((crouchClip != null) ? 8 : 0) | (holdAimedNow ? 16 : 0) | (leftOverride ? 32 : 0) | ((jumpClip != null) ? 64 : 0) | ((int)play << 8);
		bool flag = num2 != smoothDoing;
		smoothDoing = num2;
		bool flag2 = Tuning.F("smooth.auto", 0f) > 0.5f;
		float far = (flag ? 0.02f : (flag2 ? Mathf.Max(Tuning.F("smooth.jump", 0.07f), Tuning.F("smooth.speed", 6.5f) * dt) : float.MaxValue));
		float round = (flag ? 3f : (flag2 ? Mathf.Max(Tuning.F("smooth.turn", 22f), 900f * dt) : float.MaxValue));
		pose.leftPos = EasePlace(ref easeLeft, pose.leftPos, smoothOut.leftPos, far, num);
		pose.rightPos = EasePlace(ref easeRight, pose.rightPos, smoothOut.rightPos, far, num);
		pose.leftRot = EaseTurn(ref easeLeft, pose.leftRot, smoothOut.leftRot, round, num);
		pose.rightRot = EaseTurn(ref easeRight, pose.rightRot, smoothOut.rightRot, round, num);
		pose.head = EaseTurn(ref easeHead, pose.head, smoothOut.head, round, num);
		float height = pose.height;
		if (Mathf.Abs(height - smoothOut.height) > (flag ? 0.02f : (flag2 ? Mathf.Max(0.05f, 2.5f * dt) : float.MaxValue)))
		{
			easeHeightFrom = smoothOut.height - height;
			easeHeightAt = Time.time;
		}
		float num3 = Left(Time.time - easeHeightAt, num);
		if (num3 > 0f)
		{
			pose.height = height + easeHeightFrom * num3;
		}
		smoothOut = pose;
	}

	private static Vector3 EasePlace(ref Eased e, Vector3 asked, Vector3 was, float far, float time)
	{
		Vector3 vector = asked;
		if (e.moving)
		{
			float num = Left(Time.time - e.at, time);
			if (num <= 0f)
			{
				e.moving = false;
			}
			else
			{
				vector = asked + e.from * num;
			}
		}
		if ((vector - was).magnitude > far)
		{
			e.from = was - asked;
			e.at = Time.time;
			e.moving = true;
			vector = was;
		}
		return vector;
	}

	private static Quaternion EaseTurn(ref Eased e, Quaternion asked, Quaternion was, float round, float time)
	{
		Quaternion quaternion = asked;
		if (e.turning)
		{
			float num = Left(Time.time - e.turnAt, time);
			if (num <= 0f)
			{
				e.turning = false;
			}
			else
			{
				quaternion = Quaternion.Slerp(Quaternion.identity, e.turnFrom, num) * asked;
			}
		}
		if (Quaternion.Angle(quaternion, was) > round)
		{
			e.turnFrom = was * Quaternion.Inverse(asked);
			e.turnAt = Time.time;
			e.turning = true;
			quaternion = was;
		}
		return quaternion;
	}

	private void Squat(float seconds, float deep)
	{
		if (!climbing && !Down && !InLesson && !(Tuning.F("squat.on", 1f) < 0.5f))
		{
			squatUntil = Time.time + seconds;
			squatDepth = 0f - Mathf.Clamp(deep, 0.2f, 0.9f);
		}
	}

	private void Squatting(float dt)
	{
		bool flag = Time.time < squatUntil && !climbing && !Down && !InLesson && Time.time - crouchPlannedAt > 0.6f;
		squatNow = Mathf.MoveTowards(squatNow, flag ? squatDepth : 0f, dt * 2.2f);
		if (squatNow < -0.02f && squatNow < duckNow && crouchClip == null)
		{
			crouching = true;
			crouch = squatNow;
			feet = 0f;
			crouchReset = true;
		}
	}

	private void NoticePeople(Transform head, float dt)
	{
		Squatting(dt);
		if (head == null)
		{
			return;
		}
		if (friend != null)
		{
			MeetStep(head);
		}
		if (runnerLookAt > 0f && Time.time >= runnerLookAt)
		{
			runnerLookAt = -1f;
			if (CalmNow)
			{
				Look(runnerAlarm ? runnerFrom : runnerPoint, null, runnerAlarm ? "what they ran from" : "where they ran", Random(1f, 1.8f));
			}
		}
		if (Time.time < peopleAt || Tuning.F("people.on", 1f) < 0.5f)
		{
			return;
		}
		peopleAt = Time.time + 0.12f;
		bool calmNow = CalmNow;
		float num = Tuning.F("people.run", 3f);
		foreach (Movers.Mover item in Movers.All)
		{
			if (item.rig == rig || item.rig == null || item.head == null)
			{
				continue;
			}
			Vector3 vector = Flat(item.velocity);
			float magnitude = vector.magnitude;
			if (magnitude < num)
			{
				continue;
			}
			Vector3 vector2 = Flat(head.position - item.head.position);
			float magnitude2 = vector2.magnitude;
			if (magnitude2 > Tuning.F("people.far", 18f) || magnitude2 < 0.3f || Mathf.Abs(head.position.y - item.head.position.y) > 3f)
			{
				continue;
			}
			Vector3 vector3 = vector / magnitude;
			float num2 = Vector3.Dot(vector3, vector2 / magnitude2);
			bool flag = ((!enemy) ? (item.bot != null && item.bot.enemy) : (item.you || item.bot == null || !item.bot.enemy));
			if (!calmNow && !flag)
			{
				continue;
			}
			if (num2 > 0.9f && magnitude2 < 7f && magnitude2 / magnitude < Tuning.F("people.rush.time", 1.3f) && Time.time - rushedAt > 4f && !Down && !climbing && !InLesson && (magnitude2 < 3f || Targets.InSight(head.position, item.head.position, item.rig.transform, rig)))
			{
				Rushed(item, head, vector3, magnitude2);
				continue;
			}
			bool flag2 = ((!enemy) ? (item.bot != null && item.bot.enemy) : (item.you || item.bot == null || !item.bot.enemy));
			if (!(!calmNow || flag2) && !(num2 > 0.6f) && !(magnitude < num + 0.4f) && !(magnitude2 > 16f) && Time.time - runnerAt > Tuning.F("people.runner.every", 7f) && Targets.InSight(head.position, item.head.position, item.rig.transform, rig))
			{
				SawRunner(item, head, vector3);
			}
		}
		if (friend == null && calmNow && Time.time >= meetTryAt)
		{
			meetTryAt = Time.time + 0.6f;
			LookForFriend(head);
		}
	}

	private void Rushed(Movers.Mover m, Transform head, Vector3 way, float far)
	{
		rushedAt = Time.time;
		Vector3 vector = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * Flat(m.head.position - head.position).normalized;
		if (HandsFree)
		{
			flinchSide = Mathf.Clamp(vector.x, -1f, 1f);
			flinchSize = Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(7f, 2f, far));
			flinchAt = Time.time + Random(0.04f, 0.1f) * personality.reaction;
			braceFrom = flinchAt + 0.1f;
			braceUntil = braceFrom + Random(0.7f, 1.2f);
		}
		Look(m.head.position, m.head, "someone running at it", Random(1.2f, 2f));
		if (!Playing && nearestSeen.transform == null && (mode == Mode.Follow || mode == Mode.Wander || mode == Mode.Brain) && Tuning.F("people.rush.step", 1f) > 0.5f)
		{
			Vector3 vector2 = Vector3.Cross(Vector3.up, way);
			if (Vector3.Dot(vector2, head.position - m.head.position) < 0f)
			{
				vector2 = -vector2;
			}
			Vector3 vector3 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * vector2;
			Vector2 push = new Vector2(vector3.x, vector3.z) * MotionLibrary.WalkPush;
			if (RoomToStep(head.position - Vector3.up * 0.4f, push))
			{
				rushStep = vector2;
				rushStepUntil = Time.time + Random(0.35f, 0.6f);
			}
		}
		Lab.Event("people: " + Who(m) + " comes running at it, " + far.ToString("0.0") + " m off: it starts" + ((Time.time < rushStepUntil) ? " and steps aside" : ""));
	}

	private void SawRunner(Movers.Mover m, Transform head, Vector3 way)
	{
		runnerAt = Time.time;
		float num = Random(1.2f, 2f);
		Look(m.head.position, m.head, "someone running", num);
		runnerPoint = m.head.position + way * 10f;
		runnerFrom = m.head.position - way * 9f;
		int num2 = 0;
		foreach (Movers.Mover item in Movers.All)
		{
			Vector3 vector = Flat(item.velocity);
			if (item.rig != rig && vector.magnitude > 3f && Vector3.Dot(vector.normalized, way) > 0.6f && (item.head.position - head.position).magnitude < 22f)
			{
				num2++;
			}
		}
		runnerAlarm = num2 >= 2 || Time.time - heardTime < 6f || Time.time - flinchAt < 6f;
		runnerLookAt = Time.time + num;
		bool flag = (mode == Mode.Wander || mode == Mode.Brain) && (act == Act.Idle || act == Act.Stroll || act == Act.Jog) && friend == null;
		string text = "";
		Vector3 place;
		if (flag && runnerAlarm)
		{
			nav.Clear();
			Idle(num + Random(1.5f, 3f));
			if (UnityEngine.Random.value < Tuning.F("people.alarm.squat", 0.5f))
			{
				Squat(Random(1.5f, 3f), 0.55f);
			}
			text = ", and takes it that something is up";
		}
		else if (flag && !enemy && UnityEngine.Random.value < Tuning.F("people.runner.follow", 0.3f) * personality.curiosity && Navigator.RandomPlace(runnerPoint - Vector3.up * 1.5f, 0.5f, 3f, out place))
		{
			nav.Clear();
			act = Act.Jog;
			actUntil = Time.time + 12f;
			nav.SetGoal(place);
			text = ", and goes after them to see";
		}
		Lab.Event("people: sees " + Who(m) + " running" + text);
	}

	private static string Who(Movers.Mover m)
	{
		if (!m.you)
		{
			if (m.bot == null)
			{
				return "another player";
			}
			return "the bot " + m.bot.Name;
		}
		return "you";
	}

	private void LookForFriend(Transform head)
	{
		if (!CanMeet || Tuning.F("meet.on", 1f) < 0.5f)
		{
			return;
		}
		foreach (Bot item in BotManager.All)
		{
			if (item == this || !item.Alive || item.Puppet || item.enemy != enemy || !item.CanMeet)
			{
				continue;
			}
			Transform head2 = item.Head;
			if (!(head2 == null) && !((head2.position - head.position).magnitude > Tuning.F("meet.far", 6f)) && !(Mathf.Abs(head2.position.y - head.position.y) > 1.2f) && Targets.InSight(head.position, head2.position, item.rig.transform, rig))
			{
				if (UnityEngine.Random.value > Tuning.F("meet.chance", 0.6f))
				{
					metAt = Time.time - Tuning.F("meet.every", 40f) + 12f;
					break;
				}
				float time = Random(7f, 12f);
				BeginMeet(item, lead: true, time);
				item.BeginMeet(this, lead: false, time);
				Lab.Event("meet: " + Name + " goes up to " + item.Name + ", for " + time.ToString("0") + " s");
				break;
			}
		}
	}

	private void BeginMeet(Bot other, bool lead, float time)
	{
		friend = other;
		meetLead = lead;
		meetFrom = Time.time;
		meetUntil = Time.time + time;
		metAt = Time.time;
		meetStage = 0;
		meetLookAt = 0f;
		meetWaveDue = -1f;
		meetTalking = false;
		if (mode == Mode.Wander || mode == Mode.Brain)
		{
			nav.Clear();
			Idle(time + 1f);
		}
	}

	private void EndMeet()
	{
		friend = null;
		metAt = Time.time;
		meetStage = 0;
		meetTalking = false;
		meetWaveDue = -1f;
	}

	private void WaveNow(int times)
	{
		if (HandsFree && !(Time.time < waveUntil))
		{
			waveStart = Time.time;
			waveUntil = Time.time + 0.35f + (float)times * 0.7f;
			waveLeft = personality.mirror > 0.5f;
			WaveOfTheirs();
		}
	}

	private void MeetStep(Transform head)
	{
		Bot bot = friend;
		Transform transform = ((bot != null && bot.Alive) ? bot.Head : null);
		bool num = transform == null || bot.dead || bot.Down || Down || bot.friend != this || Time.time > meetUntil || nearestSeen.transform != null || brainTarget.transform != null || climbing || InLesson || Playing || Time.time - flinchAt < 0.6f || ((mode == Mode.Wander || mode == Mode.Brain) && act != Act.Idle);
		float num2 = ((transform != null) ? Flat(transform.position - head.position).magnitude : 0f);
		if (num || num2 > 10f)
		{
			EndMeet();
			return;
		}
		if (Time.time >= meetLookAt)
		{
			meetLookAt = Time.time + 0.8f;
			Look(transform.position, transform, "another bot", 1.3f);
		}
		float num3 = Time.time - meetFrom;
		if (meetStage == 0 && (num2 < Tuning.F("meet.near", 1.7f) + 0.5f || num3 > 3f))
		{
			meetStage = 1;
			meetNearAt = Time.time;
			if (meetLead)
			{
				WaveNow(2);
			}
			else
			{
				meetWaveDue = Time.time + Random(0.9f, 1.5f) * personality.reaction;
			}
		}
		if (meetWaveDue > 0f && Time.time >= meetWaveDue)
		{
			meetWaveDue = -1f;
			WaveNow(2);
		}
		meetTalking = false;
		if (meetStage != 1)
		{
			return;
		}
		float num4 = Time.time - meetNearAt - 2.4f;
		if (meetUntil - Time.time < 1.6f)
		{
			meetStage = 2;
			if (!meetLead)
			{
				WaveNow(1);
			}
		}
		else if (num4 >= 0f)
		{
			meetTalking = (int)(num4 / 1.9f) % 2 == 0 == meetLead;
		}
	}

	private void SocialStep(float dt)
	{
		if (Down || climbing)
		{
			return;
		}
		if (Time.time < rushStepUntil && !Playing)
		{
			Vector3 vector = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * rushStep;
			walk = new Vector2(vector.x, vector.z) * MotionLibrary.WalkPush;
			running = false;
		}
		if (friend == null)
		{
			return;
		}
		Transform head = Head;
		Transform transform = (friend.Alive ? friend.Head : null);
		if (!(head == null) && !(transform == null))
		{
			Vector3 vector2 = Flat(transform.position - head.position);
			float magnitude = vector2.magnitude;
			Face(transform.position, dt, 170f);
			running = false;
			if (meetLead && mode != 0 && magnitude > Tuning.F("meet.near", 1.7f) && meetStage == 0)
			{
				Vector3 vector3 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * (vector2 / magnitude);
				walk = new Vector2(vector3.x, vector3.z) * MotionLibrary.WalkPush * 0.85f;
			}
			else if (magnitude < 0.9f)
			{
				Vector3 vector4 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * (-vector2 / Mathf.Max(magnitude, 0.01f));
				walk = new Vector2(vector4.x, vector4.z) * MotionLibrary.WalkPush * 0.5f;
			}
			else
			{
				walk = Vector2.zero;
			}
		}
	}

	private void SocialPose(float dt)
	{
		float height = pose.height;
		float num = height / 1.7f;
		if (Time.time >= braceFrom && Time.time < braceUntil && HandsFree)
		{
			float num2 = Mathf.SmoothStep(0f, 1f, Mathf.Min((Time.time - braceFrom) * 7f, (braceUntil - Time.time) * 3.5f)) * Tuning.F("people.brace", 1f);
			pose.height = height - 0.06f * num2 * num;
			pose.leftPos = Vector3.Lerp(pose.leftPos, new Vector3(-0.17f * num, height - 0.36f * num, 0.3f * num), num2);
			pose.rightPos = Vector3.Lerp(pose.rightPos, new Vector3(0.17f * num, height - 0.36f * num, 0.3f * num), num2);
			pose.leftRot = Quaternion.Slerp(pose.leftRot, Quaternion.Euler(-70f, 12f, 0f), num2);
			pose.rightRot = Quaternion.Slerp(pose.rightRot, Quaternion.Euler(-70f, -12f, 0f), num2);
			curl = Mathf.Lerp(curl, 0.05f, num2);
		}
		bool flag = friend != null && meetTalking && HandsFree && Time.time >= waveUntil;
		talkK = Mathf.MoveTowards(talkK, flag ? 1f : 0f, dt * 3f);
		if (friend == null && talkK <= 0f)
		{
			return;
		}
		float num3 = Time.time - meetFrom + (float)(personality.seed % 17) * 0.37f;
		if (talkK > 0f && HandsFree)
		{
			float num4 = Mathf.SmoothStep(0f, 1f, talkK);
			Vector3 b = new Vector3((0.2f + 0.05f * Mathf.Cos(num3 * 3.7f)) * num, height - (0.5f - 0.06f * Mathf.Sin(num3 * 5.2f)) * num, (0.3f + 0.03f * Mathf.Sin(num3 * 2.9f)) * num);
			Quaternion b2 = Quaternion.Euler(-35f + 14f * Mathf.Sin(num3 * 5.2f), 0f, -55f + 18f * Mathf.Cos(num3 * 3.7f));
			if (personality.mirror > 0.5f)
			{
				pose.leftPos = Vector3.Lerp(pose.leftPos, new Vector3(0f - b.x, b.y, b.z), num4);
				pose.leftRot = Quaternion.Slerp(pose.leftRot, Quaternion.Euler(b2.eulerAngles.x, 0f, 0f - b2.eulerAngles.z), num4);
			}
			else
			{
				pose.rightPos = Vector3.Lerp(pose.rightPos, b, num4);
				pose.rightRot = Quaternion.Slerp(pose.rightRot, b2, num4);
			}
			curl = Mathf.Lerp(curl, 0.3f, num4);
			pose.head = Quaternion.Euler(Mathf.Sin(num3 * 2.3f) * 3f * num4, Mathf.Sin(num3 * 1.7f) * 5f * num4, 0f) * pose.head;
		}
		else if (friend != null && meetStage == 1 && !meetTalking)
		{
			pose.head = Quaternion.Euler(Mathf.Max(0f, Mathf.Sin(num3 * 4.4f)) * 7f, 0f, 0f) * pose.head;
		}
	}

	private void MaybeSurrender()
	{
		if (enemy && !surrendered && !dead && !Puppet && !(hp <= 0f) && !(hp > MaxHp * Tuning.F("surrender.below", 0.35f)) && !surrenderRolled)
		{
			surrenderRolled = true;
			if (!(UnityEngine.Random.value > Tuning.F("surrender.chance", 0.5f)))
			{
				Surrender();
			}
		}
	}

	private void Surrender()
	{
		surrendered = true;
		surrenderedAt = Time.time;
		reload = Reload.None;
		spareNearSince = -1f;
		try
		{
			StopPlaying();
			Pull(on: false);
			Hand[] array = new Hand[2] { LeftHand, RightHand };
			foreach (Hand hand in array)
			{
				if (hand != null && hand.m_CurrentAttachedGO != null)
				{
					Grip component = hand.m_CurrentAttachedGO.GetComponent<Grip>();
					if (component != null)
					{
						LetGoOf(component, hand);
					}
				}
			}
			DropStowed();
		}
		catch (System.Exception ex)
		{
			Lab.Event("surrender: letting go: " + ex.Message);
		}
		gun = null;
		item = null;
		support = null;
		handOverride = false;
		leftOverride = false;
		target = default(Targets.Target);
		Spawnables.Notify("An enemy bot surrenders: come close to spare it, or shoot it");
		Lab.Event("surrender: hands up, its gun dropped (" + Mathf.Max(hp, 0f).ToString("0.00") + " health left)");
	}

	private bool HitWhileSurrendered()
	{
		if (!surrendered)
		{
			return false;
		}
		bool flag = Time.time - PlayerWatch.FiredAt < Tuning.F("surrender.you.within", 0.5f);
		Lab.Event("surrender: shot with its hands up" + (flag ? " by you" : ""));
		surrendered = false;
		hp = 0f;
		Die();
		if (flag)
		{
			FuriousUntil = Time.time + Tuning.F("surrender.fury", 60f);
			bool flag2 = false;
			foreach (Bot item in BotManager.All)
			{
				flag2 |= item != this && item.enemy && !item.dead;
			}
			if (flag2)
			{
				Spawnables.Notify("You shot one with its hands up: the enemy bots are furious");
			}
			Lab.Event("surrender: the enemy bots are furious for " + Tuning.F("surrender.fury", 60f).ToString("0") + " s");
		}
		return true;
	}

	private bool SurrenderStep(Transform you, Transform head, float dt)
	{
		if (!surrendered)
		{
			return false;
		}
		walk = Vector2.zero;
		running = false;
		Pull(on: false);
		handOverride = false;
		leftOverride = false;
		if (you != null)
		{
			Face(you.position, dt, 90f);
		}
		playPitch = 0f;
		playYaw = 0f;
		if (!(((you != null && head != null) ? Flat(you.position - head.position).magnitude : 99f) < Tuning.F("surrender.spare.near", 6f)) || !(Time.time - PlayerWatch.FiredAt > 2f))
		{
			spareNearSince = -1f;
		}
		else if (spareNearSince < 0f)
		{
			spareNearSince = Time.time;
		}
		else if (Time.time - spareNearSince > Tuning.F("surrender.spare.time", 4f))
		{
			Spared();
		}
		return true;
	}

	private void Spared()
	{
		surrendered = false;
		enemy = false;
		try
		{
			Transform transform = rig.transform.Find("BotEnemyMark");
			if (transform != null)
			{
				UnityEngine.Object.Destroy(transform.gameObject);
			}
		}
		catch (System.Exception)
		{
		}
		hp = Mathf.Max(hp, MaxHp * Tuning.F("surrender.spared.health", 0.5f));
		hurtAt = Time.time;
		Spawnables.Notify("Spared: the bot is on your side now");
		Lab.Event("surrender: spared, on the player's side now");
	}

	private void HandsUpPose()
	{
		if (surrendered)
		{
			float height = pose.height;
			pose.leftPos = new Vector3(-0.28f, height + 0.12f, 0.08f);
			pose.rightPos = new Vector3(0.28f, height + 0.12f, 0.08f);
			pose.leftRot = Quaternion.Euler(-90f, 0f, 20f);
			pose.rightRot = Quaternion.Euler(-90f, 0f, -20f);
		}
	}

	public void LabSurrender()
	{
		if (enemy && !surrendered && !dead)
		{
			Surrender();
		}
	}

	private int TeamOdds()
	{
		if (Time.time - teamsAt > 0.5f)
		{
			teamsAt = Time.time;
			ours = (theirs = 0);
			foreach (Bot item in BotManager.All)
			{
				if (!item.dead && !item.surrendered && item.Alive && !item.Puppet)
				{
					if (item.enemy)
					{
						theirs++;
					}
					else
					{
						ours++;
					}
				}
			}
			if (Player.RigManager != null)
			{
				ours++;
			}
		}
		if (!enemy)
		{
			return ours - theirs;
		}
		return theirs - ours;
	}

	private float TeamKeep(out int odds)
	{
		odds = TeamOdds();
		if (Tuning.F("team.tactics", 1f) < 0.5f)
		{
			odds = 0;
			return 1f;
		}
		if (odds >= 1)
		{
			return Tuning.F("team.push", 0.55f);
		}
		if (odds <= -1)
		{
			return Tuning.F("team.fall", 1.7f);
		}
		return 1f;
	}

	public void Trip(float hard, Vector3 push, string why)
	{
		if (dead || down || rising || climbing || Time.time - upAt < Tuning.F("trip.rest", 1.5f) || Tuning.F("trip.on", 1f) <= 0f)
		{
			return;
		}
		hard = Mathf.Clamp01(hard);
		try
		{
			if (InLesson)
			{
				EndLesson("broken off: knocked down");
			}
			else if (mode == Mode.Clone)
			{
				SetMode(Mode.Stand);
			}
			portalGoing = false;
			Pull(on: false);
			walk = Vector2.zero;
			stick = Vector2.zero;
			Limp(on: true);
			Rigidbody rigidbody = Pelvis();
			if (rigidbody != null && push.sqrMagnitude > 0.0001f)
			{
				rigidbody.AddForce(Flat(push).normalized * Mathf.Lerp(0.6f, 2.2f, hard) * Tuning.F("trip.shove", 1f), ForceMode.VelocityChange);
			}
		}
		catch (System.Exception ex)
		{
			Lab.Event("trip: could not be knocked down: " + ex.Message);
			return;
		}
		down = true;
		downAt = Time.time;
		downFor = Mathf.Lerp(Tuning.F("trip.least", 1.6f), Tuning.F("trip.most", 4.5f), hard) * Random(0.85f, 1.2f);
		Voice(hard > 0.6f, death: false);
		Lab.Event("trip: down (" + why + "), for " + downFor.ToString("0.0") + " s");
	}

	public void Bumped(PlayerDamageReceiver.BodyPart part, Collision c)
	{
		if (dead || down || rising || c == null || Puppet || part == PlayerDamageReceiver.BodyPart.HandLf || part == PlayerDamageReceiver.BodyPart.HandRt || part == PlayerDamageReceiver.BodyPart.ArmLowerLf || part == PlayerDamageReceiver.BodyPart.ArmLowerRt || Time.time < Clone.SuppressUntil || spawningGun || Time.time - spawnAskedAt < 1.5f || InLesson)
		{
			return;
		}
		Rigidbody rigidbody = c.rigidbody;
		if (rigidbody == null || rigidbody.isKinematic)
		{
			return;
		}
		RigManager componentInParent = rigidbody.GetComponentInParent<RigManager>();
		if (componentInParent == rig || (componentInParent != null && BotRig.IsBot(componentInParent)))
		{
			return;
		}
		if (item != null && (Playing || Time.time - playAt < 2f))
		{
			MarrowEntity componentInParent2 = item.GetComponentInParent<MarrowEntity>();
			if (rigidbody == item || (componentInParent2 != null && rigidbody.transform.IsChildOf(componentInParent2.transform)))
			{
				return;
			}
		}
		if (leftVehicle != null && Time.time - leftVehicleAt < 5f && rigidbody.transform.IsChildOf(leftVehicle))
		{
			return;
		}
		Hand[] array = new Hand[2] { LeftHand, RightHand };
		foreach (Hand hand in array)
		{
			GameObject gameObject = ((hand != null) ? hand.m_CurrentAttachedGO : null);
			if (gameObject != null && rigidbody.transform.IsChildOf(gameObject.transform.root) && gameObject.transform.root != rig.transform)
			{
				return;
			}
		}
		if (!(componentInParent == null) || !(rigidbody.velocity.magnitude < Tuning.F("lying.speed", 1.5f)))
		{
			float magnitude = c.relativeVelocity.magnitude;
			float magnitude2 = c.impulse.magnitude;
			bool flag = part >= PlayerDamageReceiver.BodyPart.LegUpperLf;
			bool flag2 = part == PlayerDamageReceiver.BodyPart.Head || part == PlayerDamageReceiver.BodyPart.Neck;
			float num = Mathf.Clamp(pose.height / 1.7f, 0.5f, 2f);
			float num2 = Tuning.F("trip.impulse", 30f) * Mathf.Max(0.5f, num * num) * (flag ? Tuning.F("trip.legs", 0.6f) : (flag2 ? 0.8f : 1f));
			if (magnitude2 > num2 * 0.4f && magnitude > 1.5f && Time.time - bumpSaidAt > 0.5f)
			{
				bumpSaidAt = Time.time;
				Lab.Event("trip: bumped on " + part.ToString() + " by " + rigidbody.name + ", " + magnitude2.ToString("0") + " Ns at " + magnitude.ToString("0.0") + " m/s (down at " + num2.ToString("0") + ")");
			}
			if (!(magnitude < Tuning.F("trip.speed", 2.2f)) && !(magnitude2 < num2) && (!(rigidbody.velocity.magnitude < Tuning.F("trip.speed", 2.2f) * 0.7f) || !(componentInParent == null)))
			{
				Trip(Mathf.InverseLerp(num2, num2 * 3f, magnitude2), -c.relativeVelocity, "hit on " + part.ToString() + " by " + rigidbody.name);
			}
		}
	}

	private void TripByHit(float damage, PlayerDamageReceiver.BodyPart part, Attack attack, bool bullet = false)
	{
		float num = damage / Mathf.Max(0.01f, MaxHp);
		bool flag = part >= PlayerDamageReceiver.BodyPart.LegUpperLf;
		Vector3 push = attack?.direction ?? Vector3.zero;
		try
		{
			bullet |= attack != null && attack.attackType.ToString().IndexOf("Pierc", System.StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch (System.Exception)
		{
		}
		if (bullet)
		{
			if (num > Tuning.F("trip.bullet", 0.65f))
			{
				Trip(num, push, "a hard hit");
			}
			else if (flag && UnityEngine.Random.value < Tuning.F("trip.bullet.leg", 0.25f))
			{
				Trip(0.3f + num * 0.5f, push, "shot in the leg");
			}
		}
		else if (num > Tuning.F("trip.damage", 0.3f))
		{
			Trip(num, push, "a hard hit");
		}
		else if (flag && num > 0.06f && UnityEngine.Random.value < Tuning.F("trip.leg.chance", 0.6f))
		{
			Trip(0.3f + num, push, "hit in the leg");
		}
	}

	private void TripByPull()
	{
		if (down || rising || dead)
		{
			return;
		}
		Hand[] array = new Hand[2]
		{
			Player.LeftHand,
			Player.RightHand
		};
		foreach (Hand hand in array)
		{
			GameObject gameObject = ((hand != null) ? hand.m_CurrentAttachedGO : null);
			if (!(gameObject == null) && !(hand.rb == null) && gameObject.transform.IsChildOf(rig.transform))
			{
				Rigidbody rigidbody = Pelvis();
				Vector3 push = hand.rb.velocity - ((rigidbody != null) ? (Flat(rigidbody.velocity) * 0.5f) : Vector3.zero);
				if (push.magnitude > Tuning.F("trip.pull", 2.4f))
				{
					Trip(Mathf.InverseLerp(2.4f, 6f, push.magnitude), push, "you pulled it over");
					break;
				}
			}
		}
	}

	private void Calm(float most)
	{
		if (bodyParts == null || bodyParts.Length == 0 || bodyParts[0] == null)
		{
			bodyParts = ((rig.physicsRig != null) ? rig.physicsRig.GetComponentsInChildren<Rigidbody>() : null);
		}
		if (bodyParts == null)
		{
			return;
		}
		Rigidbody[] array = bodyParts;
		foreach (Rigidbody rigidbody in array)
		{
			if (!(rigidbody == null) && !rigidbody.isKinematic)
			{
				Vector3 velocity = rigidbody.velocity;
				if (velocity.y > most * 0.5f)
				{
					velocity.y = most * 0.5f;
				}
				if (velocity.magnitude > most)
				{
					velocity = velocity.normalized * most;
				}
				rigidbody.velocity = velocity;
				if (most <= 0f)
				{
					rigidbody.angularVelocity *= 0.5f;
				}
			}
		}
	}

	private void Limp(bool on)
	{
		PhysicsRig physicsRig = rig.physicsRig;
		if (physicsRig == null)
		{
			return;
		}
		bool flag = Tuning.F("trip.limp", 1f) > 0.5f;
		if (on)
		{
			if (flag)
			{
				physicsRig.ShutdownRig();
				limp = true;
			}
			physicsRig.RagdollRig();
		}
		else
		{
			if (limp)
			{
				physicsRig.TurnOnRig();
			}
			limp = false;
			physicsRig.UnRagdollRig();
		}
	}

	private bool RagdolledByOthers()
	{
		PhysicsRig physicsRig = rig.physicsRig;
		if (physicsRig != null && !dead && !climbing && !Puppet && !physicsRig.ballLocoEnabled && rig.activeSeat == null)
		{
			return rig.bodyState != RigManager.BodyState.Seated;
		}
		return false;
	}

	private bool DownStep(float dt)
	{
		if (!down && !rising)
		{
			TripByPull();
			if (!down && RagdolledByOthers())
			{
				try
				{
					Limp(on: true);
				}
				catch (System.Exception)
				{
				}
				down = true;
				downAt = Time.time;
				downFor = Tuning.F("trip.most", 4.5f);
				walk = Vector2.zero;
				stick = Vector2.zero;
				Lab.Event("trip: ragdolled by something else: limp till it lies still");
			}
			if (!down)
			{
				return false;
			}
		}
		walk = Vector2.zero;
		stick = Vector2.zero;
		running = false;
		jump = (otherA = false);
		Transform head = Head;
		if (down)
		{
			Rigidbody rigidbody = Pelvis();
			bool flag = rigidbody == null || rigidbody.velocity.magnitude < 1.2f;
			bool flag2 = false;
			Hand[] array = new Hand[2]
			{
				Player.LeftHand,
				Player.RightHand
			};
			foreach (Hand hand in array)
			{
				GameObject gameObject = ((hand != null) ? hand.m_CurrentAttachedGO : null);
				flag2 |= gameObject != null && gameObject.transform.IsChildOf(rig.transform);
			}
			if (Time.time - downAt < downFor || !flag || (flag2 && Time.time - downAt < downFor + 6f))
			{
				return true;
			}
			if (limp)
			{
				if (wakeAt < 0f)
				{
					try
					{
						rig.physicsRig.TurnOnRig();
					}
					catch (System.Exception)
					{
					}
					wakeAt = Time.time;
					Calm(0f);
					return true;
				}
				if (Time.time - wakeAt < Tuning.F("trip.wake", 0.35f))
				{
					Calm(0f);
					return true;
				}
			}
			wakeAt = -1f;
			try
			{
				Limp(on: false);
			}
			catch (System.Exception ex3)
			{
				Lab.Event("trip: could not get up: " + ex3.Message);
			}
			down = false;
			rising = true;
			riseAt = Time.time;
			if (head != null && rigidbody != null && Flat(head.position - rigidbody.position).magnitude > 0.25f)
			{
				bodyYaw = Recorder.Yaw(Flat(head.position - rigidbody.position));
			}
			droveHas = false;
			droveHeadHas = false;
			portalHeadHas = false;
			fallHas = false;
			noLandUntil = Time.time + 2.5f;
			nav.Clear();
			steer = Vector3.zero;
			Lab.Event("trip: getting up");
			Call("up");
			return true;
		}
		float num = Tuning.F("trip.rise", 1.7f);
		float num2 = Mathf.Clamp01((Time.time - riseAt) / num);
		if (Time.time - riseAt < Tuning.F("trip.calm", 0.9f))
		{
			Calm(Tuning.F("trip.calm.speed", 2.5f));
		}
		float num3 = StandingEyes();
		float num4 = ((num3 > 0.3f) ? (num3 * 0.97f) : standingHeight);
		float num5 = num4 / 1.7f;
		float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 1f, num2));
		float num6 = Mathf.Lerp(num4 * Tuning.F("trip.low", 0.38f), num4, t);
		float t2 = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.85f, num2));
		Frame frame = Standing(Time.time - born, Mathf.Lerp(35f, 0f, t), 0f, 0f);
		frame.height = num6;
		float y = Mathf.Lerp(num6 - 0.78f * num5, Mathf.Max(0.08f * num5, num6 - 0.62f * num5), t2);
		frame.leftPos = new Vector3(-0.24f * num5, y, Mathf.Lerp(0.06f, 0.34f, t2) * num5);
		frame.rightPos = new Vector3(0.24f * num5, y, Mathf.Lerp(0.06f, 0.34f, t2) * num5);
		frame.leftRot = Quaternion.Euler(Mathf.Lerp(65f, 80f, t2), 10f, 0f);
		frame.rightRot = Quaternion.Euler(Mathf.Lerp(65f, 80f, t2), -10f, 0f);
		pose = frame;
		curl = Mathf.Lerp(0.25f, 0.05f, t2);
		if (num2 >= 1f)
		{
			rising = false;
			upAt = Time.time;
			Lab.Event("trip: up");
			return false;
		}
		return true;
	}

	public void LabTrip(float hard)
	{
		Trip(hard, Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.back, "the lab");
	}

	public void LockGunInHand()
	{
		try
		{
			Hand rightHand = RightHand;
			if (rightHand == null || item == null || gun == null || !LongGun || support != null || Tuning.F("hold.lock.long", 1f) < 0.5f)
			{
				lockedJoint = System.IntPtr.Zero;
				return;
			}
			Transform parent = GunWeight.RootOf(item);
			foreach (ConfigurableJoint component in rightHand.GetComponents<ConfigurableJoint>())
			{
				if (!(component == null) && !(component.connectedBody == null) && component.connectedBody.transform.IsChildOf(parent) && (component.angularXMotion != 0 || component.angularYMotion != 0 || component.angularZMotion != 0))
				{
					ConfigurableJointMotion configurableJointMotion2 = (component.angularZMotion = ConfigurableJointMotion.Locked);
					ConfigurableJointMotion angularXMotion = (component.angularYMotion = configurableJointMotion2);
					component.angularXMotion = angularXMotion;
					if (lockedJoint != component.Pointer)
					{
						lockedJoint = component.Pointer;
						Lab.Event("gun: " + gun.name + " locked fast in its hand (no turning in the grip)");
					}
				}
			}
		}
		catch (System.Exception)
		{
		}
	}

	public void LabJoints()
	{
		Hand rightHand = RightHand;
		if (rightHand == null || item == null)
		{
			Lab.Event("joints: no gun in hand");
			return;
		}
		Transform transform = GunWeight.RootOf(item);
		System.Collections.Generic.List<ConfigurableJoint> list = new System.Collections.Generic.List<ConfigurableJoint>();
		foreach (ConfigurableJoint componentsInChild in rig.physicsRig.GetComponentsInChildren<ConfigurableJoint>(includeInactive: true))
		{
			list.Add(componentsInChild);
		}
		foreach (ConfigurableJoint componentsInChild2 in transform.GetComponentsInChildren<ConfigurableJoint>(includeInactive: true))
		{
			list.Add(componentsInChild2);
		}
		Rigidbody rb = rightHand.rb;
		foreach (ConfigurableJoint item in list)
		{
			if (!(item == null))
			{
				Rigidbody component = item.GetComponent<Rigidbody>();
				Rigidbody connectedBody = item.connectedBody;
				bool num = (component != null && component.transform.IsChildOf(transform)) || (connectedBody != null && connectedBody.transform.IsChildOf(transform));
				bool flag = component == rb || connectedBody == rb || item.transform.IsChildOf(rightHand.transform);
				if (num)
				{
					Lab.Event("joint on " + item.name + " to " + ((connectedBody != null) ? connectedBody.name : "world") + (flag ? " (the hand)" : "") + ": move " + item.xMotion.ToString() + "/" + item.yMotion.ToString() + "/" + item.zMotion.ToString() + ", turn " + item.angularXMotion.ToString() + "/" + item.angularYMotion.ToString() + "/" + item.angularZMotion.ToString() + ", drive mode " + item.rotationDriveMode.ToString() + ", slerp " + item.slerpDrive.positionSpring.ToString("0") + "/" + item.slerpDrive.positionDamper.ToString("0") + " max " + item.slerpDrive.maximumForce.ToString("0") + ", angX " + item.angularXDrive.positionSpring.ToString("0") + "/" + item.angularXDrive.positionDamper.ToString("0") + ", proj " + item.projectionMode.ToString() + ", enabled " + item.enableCollision);
				}
			}
		}
		Lab.Event("joints: " + list.Count + " looked at, hand body " + ((rb != null) ? rb.name : "none"));
	}

	public System.Collections.Generic.IEnumerable<Gun> GunsInHand()
	{
		System.Collections.Generic.List<Gun> list = new System.Collections.Generic.List<Gun>();
		Hand[] array = new Hand[2] { RightHand, LeftHand };
		foreach (Hand hand in array)
		{
			try
			{
				GameObject gameObject = ((hand != null) ? hand.m_CurrentAttachedGO : null);
				Gun gun = ((gameObject != null) ? gameObject.GetComponentInParent<Gun>() : null);
				if (gun == null && gameObject != null)
				{
					MarrowEntity componentInParent = gameObject.GetComponentInParent<MarrowEntity>();
					gun = ((componentInParent != null) ? componentInParent.GetComponentInChildren<Gun>() : null);
				}
				if (gun != null)
				{
					list.Add(gun);
				}
			}
			catch (System.Exception)
			{
			}
		}
		return list;
	}

	private bool HoldFromRecording(Vector3 point, Transform head, bool aimed)
	{
		if (Tuning.F("hold.recorded", 1f) < 0.5f || !lockSet || gun == null || item == null || gun.triggerGrip == null || gun.firePointTransform == null)
		{
			return false;
		}
		Transform transform = GripBook.ThingOf(gun.triggerGrip);
		Transform headset = controls.headset;
		if (transform == null || headset == null)
		{
			return false;
		}
		string text = GripBook.KeyOf(transform);
		if (text != recKey)
		{
			recKey = text;
			recFrames = HoldLibrary.For(text);
			recAimed.Clear();
			recCarried.Clear();
			if (recFrames != null)
			{
				Vector3 vector = Quaternion.Inverse(transform.rotation) * gun.firePointTransform.forward;
				for (int i = 0; i < recFrames.Count; i++)
				{
					((Vector3.Angle(recFrames[i].thingInHead.rotation * vector, Vector3.forward) < Tuning.F("hold.recorded.aimed", 20f)) ? recAimed : recCarried).Add(i);
				}
			}
			recCursor = UnityEngine.Random.Range(0f, 1000f);
		}
		System.Collections.Generic.List<int> list = (aimed ? recAimed : recCarried);
		if (recFrames == null || list.Count < 10)
		{
			return false;
		}
		if (aimed != recWasAimed)
		{
			recWasAimed = aimed;
			recCursor = UnityEngine.Random.Range(0f, list.Count);
		}
		recCursor = (recCursor + Time.deltaTime * 15f) % (float)list.Count;
		HoldLibrary.Frame frame = recFrames[list[(int)recCursor]];
		Pose pose = Pose.Of(headset) * frame.thingInHead;
		Pose pose2 = Pose.Of(transform).Inverse() * Pose.Of(item.transform);
		Pose pose3 = Pose.Of(transform).Inverse() * Pose.Of(gun.firePointTransform);
		Vector3 position = (Pose.Of(transform).Inverse() * Pose.Of(gun.triggerGrip.transform)).position;
		if (aimed)
		{
			Vector3 vector2 = pose.position + pose.rotation * position;
			Vector3 vector3 = pose.position + pose.rotation * pose3.position;
			Quaternion quaternion = Quaternion.FromToRotation(pose.rotation * (pose3.rotation * Vector3.forward), (point - vector3).normalized);
			pose = new Pose(vector2 + quaternion * (pose.position - vector2), quaternion * pose.rotation);
		}
		Pose b = pose * pose2;
		if (Time.time - aimedSince > 0.3f)
		{
			aimedWantSet = false;
		}
		aimedSince = Time.time;
		float k = Mathf.Clamp01(Time.deltaTime * Tuning.F("hold.recorded.ease", 9f));
		aimedWant = (aimedWantSet ? Pose.Lerp(aimedWant, b, k) : Pose.Lerp(Pose.Of(item.transform), b, 0.15f));
		aimedWantSet = true;
		Transform vrRoot = controls.vrRoot;
		Quaternion rotation = Quaternion.Euler(0f, bodyYaw, 0f);
		Pose pose4 = aimedWant * gunInController.Inverse();
		handOverride = true;
		learnedHoldAt = Time.time;
		holdAimedNow = aimed;
		handAt = vrRoot.InverseTransformPoint(pose4.position);
		handTurn = Quaternion.Inverse(rotation) * pose4.rotation;
		playHeight = standingHeight * 0.985f;
		if (supportHeld && step == Step.Ready)
		{
			Pose pose5 = Pose.Of(transform);
			Pose pose6 = ((frame.two && leftOnGunSet) ? (pose5 * frame.leftInThing * leftHandOnController.Inverse()) : (leftOnGunSet ? (aimedWant * leftOnGun) : default(Pose)));
			if (leftOnGunSet)
			{
				leftOverride = true;
				leftAt = vrRoot.InverseTransformPoint(pose6.position);
				leftTurn = Quaternion.Inverse(rotation) * pose6.rotation;
			}
		}
		Vector3 vector4 = (aimed ? (Quaternion.Inverse(rotation) * (point - head.position)) : new Vector3(0f, -0.15f, 1f));
		playPitch = Mathf.Clamp((0f - Mathf.Atan2(vector4.y, Mathf.Max(new Vector2(vector4.x, vector4.z).magnitude, 0.1f))) * 57.29578f, -50f, 60f);
		playYaw = Mathf.Clamp(Mathf.Atan2(vector4.x, vector4.z) * 57.29578f, -50f, 50f);
		return true;
	}
}
