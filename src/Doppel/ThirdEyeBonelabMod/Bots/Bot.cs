using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSLZ.Data;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.AI;
using Il2CppSLZ.Marrow.Audio;
using Il2CppSLZ.Marrow.Combat;
using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Utilities;
using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.VRMK;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Reflection;
using Il2CppTMPro;
using MelonLoader;
using Portals.MonoBehaviours;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

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
			Quaternion val = Quaternion.Inverse(rotation);
			return new Pose(val * -position, val);
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

	private List<Frame> replay;

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

	private readonly List<Frame[]> lately = new List<Frame[]>();

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

	private IntPtr eyesOf;

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

	private readonly Dictionary<string, float> calledAt = new Dictionary<string, float>();

	private const float ThinkEvery = 0.15f;

	private const float LearnEvery = 3f;

	private const float CloseCallRange = 1.6f;

	private const float CellSize = 5f;

	private readonly List<Targets.Target> hostiles = new List<Targets.Target>();

	private readonly List<Vector3> seenEnemies = new List<Vector3>();

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

	private readonly Dictionary<IntPtr, float> sightAt = new Dictionary<IntPtr, float>();

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

	private readonly List<Rigidbody> climbBodies = new List<Rigidbody>();

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

	private readonly Dictionary<int, Poolee> cloneSpawns = new Dictionary<int, Poolee>();

	private readonly List<(int id, string barcode, Vector3 at, float askedAt, HashSet<IntPtr> before)> clonePending = new List<(int, string, Vector3, float, HashSet<IntPtr>)>();

	private readonly List<(Clone.Event e, float until)> cloneGrabs = new List<(Clone.Event, float)>();

	private readonly Grip[] cloneHeld = (Grip[])(object)new Grip[2];

	private float cloneFiredAt = -10f;

	private readonly float[] punchedAt = new float[2] { -10f, -10f };

	private bool cloneDressing;

	private float cloneDressedBy;

	private float noSpawnSaidAt = -10f;

	private string grabWhy = "";

	private readonly Dictionary<Clone.Event, float> grabWaitFrom = new Dictionary<Clone.Event, float>();

	private readonly List<(Hand hand, Rigidbody thing, float at, float handFastest, float thingFastest, string what)> grabWatch = new List<(Hand, Rigidbody, float, float, float, string)>();

	private int holdingPlayer = -1;

	private Vector3 heldOffset;

	private float playerHeldSince;

	private readonly List<Rigidbody> heldBodies = new List<Rigidbody>();

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

	private readonly List<(Collider a, Collider b)> coverNoTouch = new List<(Collider, Collider)>();

	private static readonly Dictionary<Bot, Vector3> coverTaken = new Dictionary<Bot, Vector3>();

	private static readonly List<(Vector3 at, float t)> recentSpawns = new List<(Vector3, float)>();

	private bool tinkering;

	private string tinkerCode = "";

	private string tinkerName = "";

	private int trialN;

	private int trialsWanted;

	private string trialAction = "";

	private string lastTrialAction = "";

	private int trialPhase;

	private float trialAt;

	private readonly List<(Rigidbody rb, Vector3 at)> trialBodies = new List<(Rigidbody, Vector3)>();

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

	private Vector3[] driveWay = (Vector3[])(object)new Vector3[0];

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

	private List<Vector3> driveRoute;

	private List<float> driveRouteSpeed;

	private int driveRouteAt;

	private bool driveTrail;

	private Vector3 driveFacing;

	private static readonly Dictionary<IntPtr, KeyValuePair<Vector3, float>> driveSpots = new Dictionary<IntPtr, KeyValuePair<Vector3, float>>();

	private Vector3 driveAimLeft;

	private Vector3 driveAimRight;

	private Transform leftVehicle;

	private float leftVehicleAt = -100f;

	private float driveYawWas;

	private float driveSpin;

	private bool driveYawHas;

	private readonly List<Vector3> bendWay = new List<Vector3>();

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

	private readonly Dictionary<int, Poolee> exactItems = new Dictionary<int, Poolee>();

	private readonly HashSet<int> exactAsked = new HashSet<int>();

	private readonly HashSet<int> exactPouched = new HashSet<int>();

	private readonly List<(ExactClone.Item item, Pose at, HashSet<IntPtr> before, float askedAt)> exactPending = new List<(ExactClone.Item, Pose, HashSet<IntPtr>, float)>();

	private readonly List<ExactClone.Grab> exactDid = new List<ExactClone.Grab>();

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

	private readonly List<Frame[]> hitLately = new List<Frame[]>();

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

	private IntPtr lockGun;

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

	private readonly HashSet<IntPtr> gunsBefore = new HashSet<IntPtr>();

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

	private static readonly Dictionary<IntPtr, (Bot bot, float at)> claims = new Dictionary<IntPtr, (Bot, float)>();

	private float unsafeSince = -1f;

	private float assistUntil = -10f;

	private float assistBegan = -10f;

	private float assistSawAt = -10f;

	private bool directAsked;

	private float forcedAt;

	private int forcedDry;

	private float forcedSeen;

	private static readonly HashSet<IntPtr> magless = new HashSet<IntPtr>();

	private float loadSettleUntil;

	private int loadFails;

	private static readonly Dictionary<IntPtr, float> wontLoad = new Dictionary<IntPtr, float>();

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

	private IntPtr aimLockGun;

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

	private IntPtr pickFor;

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

	private readonly List<Collider> stowedOff = new List<Collider>();

	private float stowedSince;

	private float drawnAt = -10f;

	private MarrowEntity[] ownEntities;

	private float awakeAt;

	private int wakings;

	private float fastSaidAt = -10f;

	private readonly HashSet<IntPtr> noPushDone = new HashSet<IntPtr>();

	private float risingFor;

	private float liftedFor;

	private float liftedAt = -100f;

	private float noPushAgainAt;

	private IntPtr fleshOf;

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

	private readonly List<float> lessonJumps = new List<float>();

	private int lessonTurnsSeen;

	private int cloneDriven = -1;

	private float lessonSenseAt;

	private string lastLessonItem;

	private readonly Dictionary<Clone.Event, int> cloneStandIns = new Dictionary<Clone.Event, int>();

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

	private readonly Dictionary<string, Poolee> lessonThings = new Dictionary<string, Poolee>();

	private float regrabAt;

	private Vector3 lessonWallFoot;

	private Vector3 lessonWallIn;

	private Vector3 lessonLandAt;

	private bool lessonLanding;

	private float lessonLandSince;

	private int lessonLoopsDone;

	private readonly Dictionary<string, int> lessonStandIns = new Dictionary<string, int>();

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

	private readonly Vector3[] wallPin = (Vector3[])(object)new Vector3[2];

	private readonly List<(Rigidbody body, Hand hand, Grip grip, Pose inHand, bool wasKinematic)> carried = new List<(Rigidbody, Hand, Grip, Pose, bool)>();

	private float glidedAt;

	private float lessonDueAt = -1f;

	private Act act;

	private Act lastAct;

	private float actUntil;

	public int netId;

	private int netOwner = -1;

	private readonly List<Clone.Frame> netFrames = new List<Clone.Frame>();

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

	private static readonly Dictionary<Frame[], List<Strike>> strikesOf = new Dictionary<Frame[], List<Strike>>();

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

	private readonly HashSet<IntPtr> magsBefore = new HashSet<IntPtr>();

	private static readonly List<(Magazine mag, float at)> dropped = new List<(Magazine, float)>();

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

	private IntPtr lockedJoint;

	private string recKey;

	private List<HoldLibrary.Frame> recFrames;

	private readonly List<int> recAimed = new List<int>();

	private readonly List<int> recCarried = new List<int>();

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
			if ((Object)(object)rig != (Object)null && (Object)(object)controls != (Object)null && (Object)(object)controls.vrRoot != (Object)null)
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
			if ((Object)(object)rig != (Object)null && (Object)(object)rig.physicsRig != (Object)null)
			{
				return ((Component)rig.physicsRig).gameObject.activeInHierarchy;
			}
			return false;
		}
	}

	public Transform Head
	{
		get
		{
			if (!((Object)(object)rig.physicsRig != (Object)null))
			{
				return null;
			}
			return ((Rig)rig.physicsRig).m_head;
		}
	}

	private Vector3 Feet
	{
		get
		{
			if (!((Object)(object)Head != (Object)null))
			{
				return ((Component)rig).transform.position;
			}
			return Head.position - Vector3.up * pose.height;
		}
	}

	public Vector2 Walk
	{
		get
		{
			if (!(((Vector2)(ref stick)).sqrMagnitude < 0.0001f))
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
			if ((Object)(object)gun != (Object)null)
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
			if (!((Object)(object)Head != (Object)null))
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
			if (!((Object)(object)gun != (Object)null))
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
				shootSkill = ShootSkill.Begin(((Il2CppObjectBase)(object)rig).Pointer, out shootTrying);
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
				return (Object)(object)target.brain != (Object)null;
			}
			return true;
		}
	}

	public bool FrontLetGrab => Time.time < frontLetUntil;

	private bool LongGun
	{
		get
		{
			if (!gunIsLong && !((Object)(object)support != (Object)null))
			{
				return (Object)(object)frontRetry != (Object)null;
			}
			return true;
		}
	}

	public bool Playing => play != Play.None;

	private Hand RightHand
	{
		get
		{
			if (!((Object)(object)rig.physicsRig != (Object)null))
			{
				return null;
			}
			return rig.physicsRig.rightHand;
		}
	}

	private float MaxHp => (((Object)(object)rig.health != (Object)null && rig.health.max_Health > 0f) ? rig.health.max_Health : 10f) * Tuning.F("bot.hp", 1.44f);

	public bool HasStowed
	{
		get
		{
			if ((Object)(object)stowed != (Object)null)
			{
				return ((Component)stowed).gameObject.activeInHierarchy;
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
				if (!((Object)(object)gun != (Object)null) || play != Play.Shoot)
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
			if (!((Object)(object)RightHand != (Object)null))
			{
				return Head;
			}
			return ((Component)RightHand).transform;
		}
	}

	public Transform FrontHandTransform
	{
		get
		{
			if (!((Object)(object)LeftHand != (Object)null))
			{
				return null;
			}
			return ((Component)LeftHand).transform;
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
			if (!((Object)(object)rig.physicsRig != (Object)null) || !((Object)(object)((Rig)rig.physicsRig).centerOfPressure != (Object)null))
			{
				return Feet;
			}
			return ((Rig)rig.physicsRig).centerOfPressure.position;
		}
	}

	public float NetYaw => bodyYaw;

	public Transform Chest
	{
		get
		{
			try
			{
				return ((Object)(object)rig.physicsRig != (Object)null) ? ((Rig)rig.physicsRig).m_chest : null;
			}
			catch (Exception)
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
			Rigidbody val = Pelvis();
			if (!((Object)(object)val != (Object)null))
			{
				return null;
			}
			return val.position;
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
			if (!((Object)(object)rig.physicsRig != (Object)null))
			{
				return null;
			}
			return rig.physicsRig.leftHand;
		}
	}

	private float RescueWait => Tuning.F("rescue.wait", 40f);

	public bool Riding => (Object)(object)riding != (Object)null;

	public bool Sitting => sitting;

	private bool CalmNow
	{
		get
		{
			if ((Object)(object)nearestSeen.transform == (Object)null && (Object)(object)brainTarget.transform == (Object)null && !Reloading && !climbing && !InLesson && !Playing)
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
		enemy = (Object)(object)((Component)rig).transform.Find("BotEnemyMark") != (Object)null;
		personality = Personality.Of(rig);
		controls = ((Il2CppObjectBase)(object)rig.ControllerRig).TryCast<OpenControllerRig>();
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
			foreach (Rig componentsInChild in ((Component)rig).GetComponentsInChildren<Rig>(true))
			{
				Transform val = ((Component)componentsInChild).transform;
				while ((Object)(object)val != (Object)null && (Object)(object)val != (Object)(object)((Component)rig).transform)
				{
					if (!((Component)val).gameObject.activeSelf)
					{
						((Component)val).gameObject.SetActive(true);
						num++;
					}
					val = val.parent;
				}
				if (!((Behaviour)componentsInChild).enabled)
				{
					((Behaviour)componentsInChild).enabled = true;
				}
			}
		}
		catch (Exception ex)
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
			if ((Object)(object)head == (Object)null || ((Vector2)(ref forBody)).sqrMagnitude < 0.0001f || Tuning.F("move.byhead", 1f) < 0.5f)
			{
				return forBody;
			}
			Vector3 forward = Flat(head.forward);
			if (((Vector3)(ref forward)).sqrMagnitude < 0.04f)
			{
				return forBody;
			}
			float num = Mathf.DeltaAngle(Recorder.Yaw(forward), bodyYaw) * ((float)Math.PI / 180f);
			float num2 = Mathf.Cos(num);
			float num3 = Mathf.Sin(num);
			return new Vector2(forBody.x * num2 + forBody.y * num3, (0f - forBody.x) * num3 + forBody.y * num2);
		}
		catch (Exception)
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
			replay = new List<Frame>(previewClip);
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
		catch (Exception ex)
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
			if ((Object)(object)riding != (Object)null && (Object)(object)rig.activeSeat != (Object)(object)riding)
			{
				riding = null;
			}
			if (mode != Mode.Clone && mode != Mode.Exact && (Object)(object)rig.activeSeat != (Object)null && (Object)(object)riding == (Object)null && Time.time - seatOutAt > 1.5f)
			{
				seatOutAt = Time.time;
				rig.activeSeat.EgressRig(false);
			}
		}
		catch (Exception)
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
		float magnitude = ((Vector2)(ref walk)).magnitude;
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
			Rigidbody val = Pelvis();
			string[] obj = new string[21]
			{
				"run trace (",
				where,
				"): running ",
				running.ToString(),
				", walk ",
				((Vector2)(ref walk)).magnitude.ToString("0.00"),
				", stick ",
				((Vector2)(ref stick)).magnitude.ToString("0.00"),
				" (",
				stick.x.ToString("0.0"),
				", ",
				stick.y.ToString("0.0"),
				"), going ",
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null
			};
			object obj2;
			if (!((Object)(object)val != (Object)null))
			{
				obj2 = "?";
			}
			else
			{
				Vector3 val2 = Flat(val.velocity);
				obj2 = ((Vector3)(ref val2)).magnitude.ToString("0.0");
			}
			obj[13] = (string)obj2;
			obj[14] = " m/s";
			object obj3;
			if (!((Object)(object)remapHeptaRig != (Object)null))
			{
				obj3 = "";
			}
			else
			{
				string[] obj4 = new string[6] { ", legs ", null, null, null, null, null };
				TraversalState travState = remapHeptaRig.travState;
				obj4[1] = ((object)(TraversalState)(ref travState)).ToString();
				obj4[2] = ", top ";
				obj4[3] = remapHeptaRig.maxVelocity.ToString("0.0");
				obj4[4] = ", its own ";
				Vector2 currentVelocity = remapHeptaRig._currentVelocity;
				obj4[5] = ((Vector2)(ref currentVelocity)).magnitude.ToString("0.0");
				obj3 = string.Concat(obj4);
			}
			obj[15] = (string)obj3;
			obj[16] = ", game sprints ";
			obj[17] = gameSprints.ToString();
			obj[18] = ", clicked ";
			obj[19] = (Time.time - sprintClickAt).ToString("0.0");
			obj[20] = " s ago";
			Lab.Event(string.Concat(obj));
		}
	}

	private bool SprintClick()
	{
		RemapRig remapHeptaRig = rig.remapHeptaRig;
		Vector2 currentVelocity;
		if ((Object)(object)remapHeptaRig != (Object)null)
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
			else
			{
				currentVelocity = remapHeptaRig._currentVelocity;
				if (((Vector2)(ref currentVelocity)).magnitude > num * 1.12f)
				{
					gameSprints = true;
				}
			}
			float num2 = ((running && !gameSprints) ? num : walkMost);
			remapHeptaRig.maxVelocity = Mathf.MoveTowards(remapHeptaRig.maxVelocity, num2, Time.deltaTime * (gameSprints ? 30f : 6f));
		}
		Vector3 val2;
		try
		{
			Rigidbody val = Pelvis();
			if ((Object)(object)remapHeptaRig != (Object)null && (int)remapHeptaRig.travState == 2)
			{
				if (!running || (Object)(object)val == (Object)null)
				{
					goto IL_014e;
				}
				val2 = Flat(val.velocity);
				if (((Vector3)(ref val2)).magnitude < Tuning.F("sprint.gait", 2.6f) - 0.6f)
				{
					goto IL_014e;
				}
			}
			goto end_IL_00f6;
			IL_014e:
			remapHeptaRig.travState = (TraversalState)0;
			end_IL_00f6:;
		}
		catch (Exception)
		{
		}
		if (!running || ((Vector2)(ref stick)).magnitude < 0.85f || stick.y < 0.6f)
		{
			RunTrace("no click");
			return false;
		}
		RunTrace("clicking");
		Rigidbody val3 = Pelvis();
		float num3;
		if (!((Object)(object)val3 != (Object)null))
		{
			num3 = 0f;
		}
		else
		{
			val2 = Flat(val3.velocity);
			num3 = ((Vector3)(ref val2)).magnitude;
		}
		float num4 = num3;
		if (num4 < Tuning.F("sprint.speed", 2.9f) && Time.time - sprintClickAt > Tuning.F("sprint.again", 0.7f))
		{
			sprintClickAt = Time.time;
		}
		if (Time.time - sprintSaidAt > 3f)
		{
			sprintSaidAt = Time.time;
			RemapRig remapHeptaRig2 = rig.remapHeptaRig;
			string[] obj = new string[10]
			{
				"run: going ",
				num4.ToString("0.0"),
				" m/s, stick ",
				((Vector2)(ref stick)).magnitude.ToString("0.00"),
				" (",
				stick.x.ToString("0.0"),
				", ",
				stick.y.ToString("0.0"),
				")",
				null
			};
			object obj2;
			if (!((Object)(object)remapHeptaRig2 != (Object)null))
			{
				obj2 = "";
			}
			else
			{
				string[] obj3 = new string[8] { ", ", null, null, null, null, null, null, null };
				TraversalState travState = remapHeptaRig2.travState;
				obj3[1] = ((object)(TraversalState)(ref travState)).ToString();
				obj3[2] = ", most ";
				obj3[3] = remapHeptaRig2.currentMaxVelocity.ToString("0.0");
				obj3[4] = " of ";
				obj3[5] = remapHeptaRig2.maxVelocity.ToString("0.0");
				obj3[6] = ", its own ";
				currentVelocity = remapHeptaRig2._currentVelocity;
				obj3[7] = ((Vector2)(ref currentVelocity)).magnitude.ToString("0.0");
				obj2 = string.Concat(obj3);
			}
			obj[9] = (string)obj2;
			Lab.Event(string.Concat(obj));
		}
		try
		{
			if ((num4 > Tuning.F("sprint.gait", 2.6f) && (Object)(object)remapHeptaRig != (Object)null && (int)remapHeptaRig.travState == 0) || ((Object)(object)remapHeptaRig != (Object)null && (int)remapHeptaRig.travState == 1 && num4 > Tuning.F("sprint.gait", 2.6f)))
			{
				remapHeptaRig.travState = (TraversalState)2;
			}
		}
		catch (Exception)
		{
		}
		return Time.time - sprintClickAt < 0.15f;
	}

	public static string LegsOf(RigManager who)
	{
		try
		{
			RemapRig val = (((Object)(object)who != (Object)null) ? who.remapHeptaRig : null);
			if ((Object)(object)val == (Object)null)
			{
				return "none";
			}
			OpenControllerRig val2 = ((Il2CppObjectBase)(object)who.ControllerRig).TryCast<OpenControllerRig>();
			string[] array = new string[23];
			TraversalState travState = val.travState;
			array[0] = ((object)(TraversalState)(ref travState)).ToString();
			array[1] = ", top speed ";
			array[2] = val.maxVelocity.ToString("0.00");
			array[3] = " (now ";
			array[4] = val.currentMaxVelocity.ToString("0.00");
			array[5] = "), accel ";
			array[6] = val.maxAcceleration.ToString("0.0");
			array[7] = ", jump ";
			array[8] = val.jumpVelocity.ToString("0.00");
			array[9] = ", jumping ";
			array[10] = val._jumping.ToString();
			array[11] = ", crouch ";
			array[12] = val._crouchTarget.ToString("0.00");
			array[13] = ", feet ";
			array[14] = val._feetOffset.ToString("0.00");
			array[15] = ", in weight ";
			array[16] = val.inWeight.ToString("0.00");
			array[17] = ", enabled ";
			array[18] = ((Behaviour)val).enabled.ToString();
			array[19] = ", ball loco ";
			array[20] = ((Object)(object)who.physicsRig != (Object)null && who.physicsRig.ballLocoEnabled).ToString();
			array[21] = (((Object)(object)val2 != (Object)null) ? (", A " + ((ControllerRig)val2)._primaryAButton + "/" + ((ControllerRig)val2)._secondaryAButton + ", stick click " + ((ControllerRig)val2)._primaryStick + "/" + ((ControllerRig)val2)._secondaryStick) : "");
			array[22] = BodiesOf(who);
			return string.Concat(array);
		}
		catch (Exception ex)
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
			foreach (Rigidbody componentsInChild in ((Component)who.physicsRig).GetComponentsInChildren<Rigidbody>(true))
			{
				num++;
				bool isKinematic = componentsInChild.isKinematic;
				bool flag = !((Component)componentsInChild).gameObject.activeInHierarchy;
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
					text = text + " " + ((Object)componentsInChild).name + (flag ? "(off)" : "");
				}
				foreach (Joint component in ((Component)componentsInChild).GetComponents<Joint>())
				{
					if ((Object)(object)component.connectedBody == (Object)null || (Object)(object)((Component)component.connectedBody).GetComponentInParent<RigManager>() != (Object)(object)who)
					{
						num5++;
						if (text.Length < 220)
						{
							text = text + " " + ((Object)componentsInChild).name + "->" + (((Object)(object)component.connectedBody != (Object)null) ? ((Object)component.connectedBody).name : "world");
						}
					}
				}
			}
			return ", seat " + (((Object)(object)who.activeSeat != (Object)null) ? ((Object)who.activeSeat).name : "none") + ", bodies " + num + " (" + num2 + " held still, " + num3 + " asleep, " + num4 + " off, " + num5 + " tied to something else:" + text + "), physics rig on " + (((Behaviour)who.physicsRig).enabled && ((Component)who.physicsRig).gameObject.activeInHierarchy) + PartsOf(who);
		}
		catch (Exception ex)
		{
			return ", bodies failed: " + ex.Message;
		}
	}

	private void OnItsFeet()
	{
		try
		{
			if ((Object)(object)rig.activeSeat != (Object)null || (int)rig.bodyState != 1 || InLesson || sitting)
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
				rig.SwitchBodyState((BodyState)0);
				text = "asked for";
			}
			else if (seatedTries == 2)
			{
				rig.bodyState = (BodyState)0;
				text = "set";
			}
			else
			{
				((Rig)rig.physicsRig).SetBodyState((BodyState)0, (BodyState)1);
				rig._bodyState = (BodyState)0;
				rig._bodyStateOnDeck = (BodyState)0;
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
		catch (Exception ex)
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
			Vector3 position = ((Component)who).transform.position;
			string text = ", rig root " + ((Vector3)(ref position)).ToString("0.00");
			Rigidbody val = (((Object)(object)((Rig)physicsRig).m_pelvis != (Object)null) ? ((Component)((Rig)physicsRig).m_pelvis).GetComponent<Rigidbody>() : null);
			Vector3 val2;
			if ((Object)(object)val != (Object)null)
			{
				string[] obj = new string[9] { text, ", hips at ", null, null, null, null, null, null, null };
				val2 = val.position;
				obj[2] = ((Vector3)(ref val2)).ToString("0.00");
				obj[3] = " going ";
				val2 = val.velocity;
				obj[4] = ((Vector3)(ref val2)).ToString("0.00");
				obj[5] = " drag ";
				obj[6] = val.drag.ToString("0.0");
				obj[7] = " constraints ";
				RigidbodyConstraints constraints = val.constraints;
				obj[8] = ((object)(RigidbodyConstraints)(ref constraints)).ToString();
				text = string.Concat(obj);
			}
			foreach (Rigidbody componentsInChild in ((Component)physicsRig).GetComponentsInChildren<Rigidbody>(true))
			{
				string text2 = ((Object)componentsInChild).name.ToLowerInvariant();
				if (!componentsInChild.isKinematic && (text2.Contains("knee") || text2.Contains("feet") || text2.Contains("foot") || text2.Contains("loco")))
				{
					string[] obj2 = new string[12]
					{
						text,
						", ",
						((Object)componentsInChild).name,
						" y ",
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null
					};
					val2 = componentsInChild.position;
					obj2[4] = val2.y.ToString("0.00");
					obj2[5] = " going ";
					val2 = componentsInChild.velocity;
					obj2[6] = ((Vector3)(ref val2)).magnitude.ToString("0.00");
					obj2[7] = " spin ";
					val2 = componentsInChild.angularVelocity;
					obj2[8] = ((Vector3)(ref val2)).magnitude.ToString("0.0");
					obj2[9] = (componentsInChild.detectCollisions ? "" : " NO COLLISIONS");
					obj2[10] = " layer ";
					obj2[11] = ((Component)componentsInChild).gameObject.layer.ToString();
					text = string.Concat(obj2);
				}
			}
			foreach (Rig componentsInChild2 in ((Component)who).GetComponentsInChildren<Rig>(true))
			{
				text = text + ", " + ((MemberInfo)((Object)componentsInChild2).GetIl2CppType()).Name + (((Behaviour)componentsInChild2).enabled ? "" : " OFF") + (((Component)componentsInChild2).gameObject.activeInHierarchy ? "" : " (object off)");
			}
			string[] obj3 = new string[12]
			{
				text,
				", manager ",
				((Behaviour)who).enabled ? "on" : "OFF",
				", body state ",
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null
			};
			BodyState val3 = who.bodyState;
			obj3[4] = ((object)(BodyState)(ref val3)).ToString();
			obj3[5] = " (next ";
			val3 = who._bodyStateOnDeck;
			obj3[6] = ((object)(BodyState)(ref val3)).ToString();
			obj3[7] = (who._bodyStateDirty ? ", due" : "");
			obj3[8] = ")";
			obj3[9] = (who._leashed ? ", LEASHED" : "");
			obj3[10] = ", phys input ";
			obj3[11] = (((Object)(object)who.GetPhysInputRig() != (Object)null) ? ((MemberInfo)((Object)who.GetPhysInputRig()).GetIl2CppType()).Name : "none");
			return string.Concat(obj3);
		}
		catch (Exception ex)
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
		Vector3 val = Feet;
		if (Begin(Act.Sprint, Player.Head, val))
		{
			runTraceUntil = Time.time + 12f;
			Vector3 val2 = Flat(nav.Goal - val);
			Lab.Event("run: sent sprinting " + ((Vector3)(ref val2)).magnitude.ToString("0") + " m");
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
		float num2 = Mathf.Clamp01(Mathf.Min((Time.time - waveStart) * 4f, num * 4f));
		float num3 = Mathf.Sin(t * (float)Math.PI * 2f / 0.7f);
		float num4 = pose.height / 1.7f;
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector((0.32f + num3 * 0.12f) * num4, pose.height + 0.08f * num4, 0.12f * num4);
		Quaternion val2 = Quaternion.Euler(-75f, 0f, num3 * 25f);
		if (waveLeft)
		{
			pose.leftPos = Vector3.Lerp(pose.leftPos, new Vector3(0f - val.x, val.y, val.z), Mathf.SmoothStep(0f, 1f, num2));
			pose.leftRot = Quaternion.Slerp(pose.leftRot, val2, num2);
		}
		else
		{
			pose.rightPos = Vector3.Lerp(pose.rightPos, val, Mathf.SmoothStep(0f, 1f, num2));
			pose.rightRot = Quaternion.Slerp(pose.rightRot, val2, num2);
		}
		curl = 0.1f;
	}

	public static string RandomGun()
	{
		List<string> list = new List<string>();
		AssetWarehouse instance = AssetWarehouse.Instance;
		string[] spawnGuns = SpawnGuns;
		foreach (string text in spawnGuns)
		{
			if (instance != null && instance.HasCrate(new Barcode(text)))
			{
				list.Add(text);
			}
		}
		if (list.Count <= 0)
		{
			return null;
		}
		return list[Random.Range(0, list.Count)];
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
			bool tense = (mode == Mode.Brain || mode == Mode.Fists || mode == Mode.Gunplay) && ((Object)(object)nearestSeen.transform != (Object)null || Time.time - lastEnemyTime < 8f);
			Frame[] array = MotionLibrary.Pick((clip == null) ? want : wantGait, lately, personality, tense, ((Vector2)(ref walk)).magnitude < 0.6f, avatarCode);
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
			clipMirror = Random.value < personality.mirror;
			clipRate = Random.Range(0.92f, 1.08f) * personality.tempo;
			float time = clip[clip.Length - 1].time;
			float num = Mathf.Min(time, Random.Range(2.5f, 6f));
			clipT = Random.Range(0f, time - num);
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
			Rigidbody val = Pelvis();
			float num3;
			if (!((Object)(object)val != (Object)null))
			{
				num3 = num2;
			}
			else
			{
				Vector3 val2 = Flat(val.velocity);
				num3 = ((Vector3)(ref val2)).magnitude;
			}
			float num4 = num3;
			clipRate = Mathf.Lerp(clipRate, Mathf.Clamp(num4 / num2, 0.7f, 1.4f), Mathf.Clamp01(dt * 4f));
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
			float num5 = MoveSlots.Size(clip, StandingEyes());
			if (Mathf.Abs(num5 - 1f) > 0.03f)
			{
				f.height *= num5;
				ref Vector3 leftPos = ref f.leftPos;
				leftPos *= num5;
				ref Vector3 rightPos = ref f.rightPos;
				rightPos *= num5;
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
		if ((Object)(object)you == (Object)null || (Object)(object)head == (Object)null)
		{
			walk = Vector2.zero;
			return;
		}
		Vector3 val = Feet;
		Vector3 val2 = you.position - Vector3.up * 1.4f;
		if (PortalStep(val2, afterPlayer: true, dt))
		{
			following = true;
			return;
		}
		Vector3 val3 = Flat(val2 - val);
		float magnitude = ((Vector3)(ref val3)).magnitude;
		if (DriveAfterYou(you, magnitude) || ToAVehicle(you, magnitude, dt))
		{
			return;
		}
		PhysicsRig physicsRig = rig.physicsRig;
		float num = (((Object)(object)physicsRig != (Object)null && (Object)(object)((Rig)physicsRig).centerOfPressure != (Object)null) ? ((Rig)physicsRig).centerOfPressure.position.y : val.y);
		bool flag = val2.y - num > Tuning.F("follow.above", 1f);
		bool flag2 = num - val2.y > Tuning.F("follow.below", 1f);
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
		Vector3 val4 = val2 + FollowSide(val2, val);
		if (nav.HasGoal)
		{
			val3 = Flat(nav.Goal - val4);
			if (!(((Vector3)(ref val3)).magnitude > Tuning.F("follow.regoal", 1.5f)))
			{
				goto IL_01a3;
			}
		}
		nav.SetGoal(val4);
		goto IL_01a3;
		IL_01a3:
		bool flag3 = running;
		bool flag4 = false;
		try
		{
			Rigidbody val5 = (((Object)(object)Player.PhysicsRig != (Object)null && (Object)(object)((Rig)Player.PhysicsRig).m_pelvis != (Object)null) ? ((Component)((Rig)Player.PhysicsRig).m_pelvis).GetComponent<Rigidbody>() : null);
			int num2;
			if ((Object)(object)val5 != (Object)null)
			{
				val3 = Flat(val5.velocity);
				num2 = ((((Vector3)(ref val3)).magnitude > Tuning.F("follow.yourun", 2.8f)) ? 1 : 0);
			}
			else
			{
				num2 = 0;
			}
			flag4 = (byte)num2 != 0;
		}
		catch (Exception)
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
			val3 = Flat(val2 - val);
			TryStartClimb(((Vector3)(ref val3)).normalized);
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
		Vector3 val = Flat(yourFeet - feet);
		if (((Vector3)(ref val)).sqrMagnitude < 4f)
		{
			return followSideWas;
		}
		Vector3 val2 = Vector3.Cross(Vector3.up, ((Vector3)(ref val)).normalized);
		float num3 = ((num % 2 == 0) ? (-1f) : 1f) * (0.9f + 0.9f * (float)(num / 2));
		followSideWas = Vector3.Lerp(followSideWas, val2 * num3 * Tuning.F("follow.side", 1f), 0.05f);
		return followSideWas;
	}

	private void Gunplay(Transform you, Transform head, float dt)
	{
		running = false;
		if (Assisting && !Playing)
		{
			FistsOnly(you, head, dt);
			if ((Object)(object)nearestSeen.transform != (Object)null)
			{
				assistSawAt = Time.time;
			}
			else if (Time.time - assistSawAt > 2.5f && Time.time - assistBegan > 2.5f)
			{
				assistUntil = 0f;
			}
			if ((Object)(object)nearestSeen.transform != (Object)null && nearestSeenDistance < 3f)
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
		Vector3 val = Feet;
		if (nav.Remaining(val) < arrive)
		{
			walk = Vector2.zero;
			travelEnded = "there (" + nav.Remaining(val).ToString("0.0") + " m left)";
			return true;
		}
		Transform head = Head;
		Vector3 chest = (((Object)(object)head != (Object)null) ? (head.position - Vector3.up * 0.45f) : (val + Vector3.up * 1.2f));
		float rise;
		bool drop;
		Vector3 val2 = nav.Steer(val, chest, Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward, out rise, out drop);
		if (drop && val2 != Vector3.zero && nav.HasGoal && nav.Goal.y < val.y - 0.5f && nav.DropDepth <= Tuning.F("drop.max", 5f))
		{
			if (Time.time - dropSaidAt > 3f)
			{
				dropSaidAt = Time.time;
				Lab.Event("drop: down a " + nav.DropDepth.ToString("0.0") + " m edge");
			}
			Go(val2, speed, dt);
			return false;
		}
		if (val2 == Vector3.zero || drop)
		{
			walk = Vector2.zero;
			travelEnded = (drop ? ("an edge ahead, " + nav.DropDepth.ToString("0.0") + " m down") : "no way to turn") + (nav.OnMesh ? ", on the level's ways" : ", by feel") + ", feet at " + val.y.ToString("0.00");
			return true;
		}
		if (TryStartClimb(val2))
		{
			return false;
		}
		Go(val2, speed, dt);
		if (rise > 0.4f && rise < 1.3f && Time.time - ledgeJumpAt > 1.5f)
		{
			ledgeJumpAt = Time.time;
			AskJump();
		}
		if (nav.CheckStuck(val, ((Vector2)(ref walk)).magnitude, dt))
		{
			AskJump();
			if (nav.StuckCount == 2)
			{
				SelfTaught.Bad(nav.HasGoal ? nav.Goal : val);
			}
		}
		return false;
	}

	private void Go(Vector3 dir, float speed, float dt)
	{
		Vector3 val = dir;
		if (Time.time - goAt < 0.2f)
		{
			goByOthers += Mathf.Abs(Mathf.DeltaAngle(goYawWas, bodyYaw));
			goByWay += ((((Vector3)(ref goDirWas)).sqrMagnitude > 0.01f) ? Vector3.Angle(goDirWas, val) : 0f);
		}
		else
		{
			goByOthers = (goByWay = (goByApart = 0f));
			goSaidAt = Time.time;
		}
		goDirWas = val;
		dir = KeepApart(dir, ref speed);
		goByApart += Vector3.Angle(val, dir) * dt * 5f;
		if (Time.time - goSaidAt > 3f)
		{
			if (goByOthers > 120f || goByWay > 240f || goByApart > 60f)
			{
				Lab.Event("walk: in 3 s its body was turned " + goByOthers.ToString("0") + " deg by something other than its walking, its way swung " + goByWay.ToString("0") + " deg in all, kept off others by " + goByApart.ToString("0") + "; " + act.ToString() + "/" + play.ToString() + ((Time.time < gazeUntil) ? ", looking at something" : "") + (thinking2 ? ", thinking" : ""));
			}
			goByOthers = (goByWay = (goByApart = 0f));
			goSaidAt = Time.time;
		}
		steer = ((((Vector3)(ref steer)).sqrMagnitude < 0.01f) ? dir : Vector3.Slerp(steer, dir, 1f - Mathf.Exp((0f - dt) * Tuning.F("move.curve", 2.6f))));
		Vector3 val2 = Flat(steer);
		Vector3 val3;
		if (!(((Vector3)(ref val2)).sqrMagnitude > 0.001f))
		{
			val3 = dir;
		}
		else
		{
			val2 = Flat(steer);
			val3 = ((Vector3)(ref val2)).normalized;
		}
		dir = val3;
		float num = Recorder.Yaw(dir);
		float num2 = Mathf.Lerp(Tuning.F("move.turn.slow", 200f), Tuning.F("move.turn.fast", 110f), Mathf.Clamp01(((Vector2)(ref stick)).magnitude));
		bodyYaw = Mathf.MoveTowardsAngle(bodyYaw, num, num2 * dt);
		float num3 = Mathf.Abs(Mathf.DeltaAngle(bodyYaw, num));
		Vector3 val4 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * dir;
		walk = new Vector2(val4.x, val4.z) * (speed * Mathf.Clamp01(1.3f - num3 / 90f));
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
			rig.Teleport(feet, true);
		}
		catch (Exception ex)
		{
			Spawnables.Notify("The bot could not be brought over: " + ex.Message);
		}
	}

	private float Watch(Transform you, Transform head, float dt, float speed)
	{
		if ((Object)(object)you == (Object)null || (Object)(object)head == (Object)null)
		{
			return 0f;
		}
		Vector3 forward = Flat(you.position - head.position);
		if (((Vector3)(ref forward)).sqrMagnitude < 0.01f)
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
		if ((Object)(object)you == (Object)null || (Object)(object)head == (Object)null)
		{
			return 0f;
		}
		Vector3 val = you.position - head.position;
		Vector3 val2 = Flat(val);
		float magnitude = ((Vector3)(ref val2)).magnitude;
		return Mathf.Clamp((0f - Mathf.Atan2(val.y, Mathf.Max(magnitude, 0.1f))) * 57.29578f, -40f, 40f);
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
		bool flag = (Object)(object)remapHeptaRig != (Object)null && remapHeptaRig._jumping;
		if (flag && !wasJumping)
		{
			jumpedAt = Time.time;
			jumpClip = MoveSlots.Pick(MoveSlots.Kind.Jump, null, avatarCode);
		}
		wasJumping = flag;
		try
		{
			if (flag && (Object)(object)remapHeptaRig != (Object)null && Time.time - jumpedAt > Tuning.F("jump.longest", 1.6f) && !climbing && !InLesson)
			{
				Rigidbody val = Pelvis();
				if ((Object)(object)val != (Object)null && Mathf.Abs(val.velocity.y) < 0.6f && (Object)(object)((Rig)rig.physicsRig).centerOfPressure != (Object)null && FeetOnGround())
				{
					remapHeptaRig._jumping = false;
					remapHeptaRig._jumpCycle = 0f;
					remapHeptaRig._timeOfFlight = 0f;
					if ((int)remapHeptaRig.travState != 0)
					{
						remapHeptaRig.travState = (TraversalState)0;
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
		catch (Exception)
		{
		}
		if (jumpDue > 0f && Time.time >= jumpDue)
		{
			jumpDue = -1f;
			if (Time.time - jumpedAt > 0.3f && (Object)(object)remapHeptaRig != (Object)null && !remapHeptaRig._jumping)
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
		if ((Object)(object)vrRoot == (Object)null || (Object)(object)headset == (Object)null)
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
		Vector2 val = Vector2.ClampMagnitude(running ? walk : (walk * personality.pace), 1f);
		if (mode == Mode.Copy || mode == Mode.Replay)
		{
			stick = Vector2.MoveTowards(stick, val, 30f * Time.deltaTime);
		}
		else
		{
			float magnitude = ((Vector2)(ref stick)).magnitude;
			float magnitude2 = ((Vector2)(ref val)).magnitude;
			float num = ((magnitude2 > magnitude) ? (1f / Mathf.Max(0.1f, MotionLibrary.StartTime)) : (1f / Mathf.Max(0.1f, MotionLibrary.StopTime)));
			float num2 = Mathf.MoveTowards(magnitude, magnitude2, num * Time.deltaTime);
			Vector2 val2 = ((magnitude2 > 0.01f) ? (val / magnitude2) : ((magnitude > 0.01f) ? (stick / magnitude) : Vector2.up));
			Vector2 val3 = ((magnitude > 0.01f) ? (stick / magnitude) : val2);
			float num3 = Mathf.Clamp(Vector2.SignedAngle(val3, val2), (0f - Tuning.F("move.swing", 360f)) * Time.deltaTime, Tuning.F("move.swing", 360f) * Time.deltaTime);
			Vector2 val4 = Vector2.op_Implicit(Quaternion.Euler(0f, 0f, num3) * Vector2.op_Implicit(val3));
			stick = ((Vector2)(ref val4)).normalized * num2;
		}
		if (((Vector2)(ref stick)).sqrMagnitude < 0.0004f && walk == Vector2.zero)
		{
			stick = Vector2.zero;
		}
		gameStick = ((mode == Mode.Copy || mode == Mode.Replay) ? stick : ForItsHead(stick));
		if ((Object)(object)((ControllerRig)controls).leftController != (Object)null)
		{
			((ControllerRig)controls).leftController._thumbstickAxis = gameStick;
		}
		((ControllerRig)controls)._axisPrimary = gameStick;
		((ControllerRig)controls)._axisPrimaryDirty = stick != Vector2.zero;
		KeepPortalTurn(vrRoot);
		Quaternion val6 = (vrRoot.rotation = Quaternion.Euler(0f, bodyYaw, 0f));
		NotePortalTurn(vrRoot);
		headset.SetPositionAndRotation(vrRoot.TransformPoint(new Vector3(0f, pose.height + personality.posture, 0f)), val6 * Norm(pose.head));
		KeepHeadDown(vrRoot, headset);
		Place(((ControllerRig)controls).leftController, vrRoot, val6, pose.leftPos, pose.leftRot, pose.leftGrip);
		Place(((ControllerRig)controls).rightController, vrRoot, val6, pose.rightPos, pose.rightRot, pose.rightGrip);
		bool secondaryAButton = ((ControllerRig)controls)._secondaryAButton;
		((ControllerRig)controls)._secondaryAButtonDown = jump && !secondaryAButton;
		((ControllerRig)controls)._secondaryAButtonUp = !jump && secondaryAButton;
		((ControllerRig)controls)._secondaryAButton = jump;
		secondaryAButton = ((ControllerRig)controls)._primaryAButton;
		((ControllerRig)controls)._primaryAButtonDown = otherA && !secondaryAButton;
		((ControllerRig)controls)._primaryAButtonUp = !otherA && secondaryAButton;
		((ControllerRig)controls)._primaryAButton = otherA;
		secondaryAButton = ((ControllerRig)controls)._primaryStick;
		((ControllerRig)controls)._primaryStickDown = primaryClick && !secondaryAButton;
		((ControllerRig)controls)._primaryStickUp = !primaryClick && secondaryAButton;
		((ControllerRig)controls)._primaryStick = primaryClick;
		BaseController leftController = ((ControllerRig)controls).leftController;
		if ((Object)(object)leftController != (Object)null)
		{
			bool thumbstick = leftController._thumbstick;
			leftController._thumbstickDown = primaryClick && !thumbstick;
			leftController._thumbstickUp = !primaryClick && thumbstick;
			leftController._thumbstick = primaryClick;
		}
		secondaryAButton = ((ControllerRig)controls)._secondaryStick;
		((ControllerRig)controls)._secondaryStickDown = secondaryClick && !secondaryAButton;
		((ControllerRig)controls)._secondaryStickUp = !secondaryClick && secondaryAButton;
		((ControllerRig)controls)._secondaryStick = secondaryClick;
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
			return (((Object)(object)hand.AttachedReceiver != (Object)null) ? ((Il2CppObjectBase)(object)hand.AttachedReceiver).TryCast<Grip>() : null) ?? (((Object)(object)hand.m_CurrentAttachedGO != (Object)null) ? hand.m_CurrentAttachedGO.GetComponent<Grip>() : null);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private void Place(BaseController controller, Transform root, Quaternion body, Vector3 at, Quaternion turn, float grip)
	{
		if ((Object)(object)controller == (Object)null)
		{
			return;
		}
		((Component)controller).transform.SetPositionAndRotation(root.TransformPoint(at), body * Norm(turn));
		float num = ((curl >= 0f) ? curl : Mathf.Clamp01(grip));
		Hand val = (((Object)(object)controller == (Object)(object)((ControllerRig)controls).leftController) ? LeftHand : RightHand);
		bool flag = (Object)(object)controller == (Object)(object)((ControllerRig)controls).leftController;
		float[] array = null;
		float num2 = -1f;
		if ((Object)(object)val != (Object)null && (Object)(object)val.m_CurrentAttachedGO != (Object)null)
		{
			num2 = 1f;
			array = GripBook.FingersOn(HeldGrip(val), flag);
		}
		else if (flag && Time.time - frontClosingAt < 0.1f && (Object)(object)support != (Object)null)
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
		controller._solvedGrip = ((flag && FrontLetGrab && (Object)(object)val != (Object)null && (Object)(object)val.m_CurrentAttachedGO == (Object)null) ? 1f : Mathf.Min(num, OpenController.grabThreshold - 0.01f));
		float num3 = (((Object)(object)controller == (Object)(object)((ControllerRig)controls).rightController) ? trigger : 0f);
		bool primaryInteractionButton = controller._primaryInteractionButton;
		bool flag2 = num3 > 0.5f;
		controller._primaryAxis = num3;
		controller._primaryInteractionButtonDown = flag2 && !primaryInteractionButton;
		controller._primaryInteractionButtonUp = !flag2 && primaryInteractionButton;
		controller._primaryInteractionButton = flag2;
		if ((Object)(object)controller == (Object)(object)((ControllerRig)controls).rightController)
		{
			PutPresses(controller);
		}
	}

	private float StandingEyes()
	{
		Avatar avatar = rig.avatar;
		if ((Object)(object)avatar == (Object)null)
		{
			return 0f;
		}
		float result = avatar.eyeHeight * ((Component)avatar).transform.lossyScale.y;
		if (((Il2CppObjectBase)(object)avatar).Pointer != eyesOf)
		{
			eyesOf = ((Il2CppObjectBase)(object)avatar).Pointer;
			Lab.Event("head limit: " + ((Object)avatar).name + " eye height " + avatar.eyeHeight.ToString("0.000") + " x scale " + ((Component)avatar).transform.lossyScale.y.ToString("0.000") + " = " + result.ToString("0.000") + " m");
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
			if (walkMost > 0f && (Object)(object)rig != (Object)null && (Object)(object)rig.remapHeptaRig != (Object)null)
			{
				rig.remapHeptaRig.maxVelocity = walkMost;
			}
		}
		catch (Exception)
		{
		}
		if (InLesson)
		{
			ReleaseCloneHands();
		}
		if ((Object)(object)gun != (Object)null && Holding())
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
		if ((Object)(object)rig == (Object)null)
		{
			return;
		}
		try
		{
			StopPlaying();
		}
		catch (Exception)
		{
		}
		foreach (Grip componentsInChild in ((Component)rig).GetComponentsInChildren<Grip>(true))
		{
			try
			{
				if ((Object)(object)componentsInChild != (Object)null)
				{
					componentsInChild.ForceDetach();
				}
			}
			catch (Exception)
			{
			}
		}
		Object.Destroy((Object)(object)((Component)rig).gameObject);
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
			Vector3 val = Flat(you.position - youStoodAt);
			if (((Vector3)(ref val)).magnitude > 2.5f)
			{
				youStoodAt = you.position;
				youStoodSince = Time.time;
			}
			OpenControllerRig controllerRig = Player.ControllerRig;
			Hand[] array = (Hand[])(object)new Hand[2]
			{
				Player.LeftHand,
				Player.RightHand
			};
			BaseController[] array2 = (BaseController[])(object)new BaseController[2]
			{
				((Object)(object)controllerRig != (Object)null) ? ((ControllerRig)controllerRig).leftController : null,
				((Object)(object)controllerRig != (Object)null) ? ((ControllerRig)controllerRig).rightController : null
			};
			bool flag = false;
			Vector3 val2 = Vector3.zero;
			Vector3 val3 = Vector3.zero;
			for (int i = 0; i < 2; i++)
			{
				if (flag)
				{
					break;
				}
				Hand val4 = array[i];
				BaseController val5 = array2[i];
				if (!((Object)(object)val4 == (Object)null) && !((Object)(object)val5 == (Object)null) && !((Object)(object)val4.m_CurrentAttachedGO != (Object)null) && !WallGrab.Holding(val4))
				{
					bool num = val5._processedIndex < 0.3f && val5._processedMiddle > 0.6f && val5._processedRing > 0.6f;
					Vector3 val6 = you.position - Vector3.up * 0.22f;
					Vector3 val7 = ((Component)val4).transform.position - val6;
					if (num && !(((Vector3)(ref val7)).magnitude < Tuning.F("point.arm", 0.45f)))
					{
						flag = true;
						val2 = ((Component)val4).transform.position;
						val3 = ((Vector3)(ref val7)).normalized;
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
			foreach (RaycastHit item in Physics.RaycastAll(val2 + val3 * 0.3f, val3, 45f, -1, (QueryTriggerInteraction)1))
			{
				RaycastHit current = item;
				if ((Object)(object)((RaycastHit)(ref current)).collider == (Object)null || (Object)(object)((Component)((RaycastHit)(ref current)).collider).GetComponentInParent<RigManager>() == (Object)(object)Player.RigManager)
				{
					continue;
				}
				if (pointHas)
				{
					val = ((RaycastHit)(ref current)).point - val2;
					float magnitude = ((Vector3)(ref val)).magnitude;
					val = pointPlace - val2;
					if (!(magnitude < ((Vector3)(ref val)).magnitude))
					{
						continue;
					}
				}
				pointPlace = ((RaycastHit)(ref current)).point;
				pointHas = true;
			}
			if (!pointHas)
			{
				pointPlace = val2 + val3 * 20f;
				pointHas = true;
			}
			pointedAt = Time.time;
			val = pointPlace - you.position;
			Lab.Event("you pointed (" + ((Vector3)(ref val)).magnitude.ToString("0") + " m off)");
			PointedThing(you);
		}
		catch (Exception)
		{
		}
	}

	private static void PointedThing(Transform you)
	{
		try
		{
			Rigidbody val = null;
			string code = "";
			string name = "";
			float num = Tuning.F("point.thing", 0.9f);
			Vector3 val3;
			foreach (Collider item in Physics.OverlapSphere(pointPlace, num, -1, (QueryTriggerInteraction)1))
			{
				Rigidbody val2 = (((Object)(object)item != (Object)null) ? item.attachedRigidbody : null);
				if (!((Object)(object)val2 == (Object)null) && !val2.isKinematic && !((Object)(object)((Component)val2).GetComponentInParent<RigManager>() != (Object)null) && !(val2.mass > 15f) && !NotForBots(val2) && Tryable(val2, out var code2, out var name2) && !((Object)(object)GripOf(val2) == (Object)null))
				{
					val3 = item.ClosestPoint(pointPlace) - pointPlace;
					float magnitude = ((Vector3)(ref val3)).magnitude;
					if (magnitude < num)
					{
						num = magnitude;
						val = val2;
						code = code2;
						name = name2;
					}
				}
			}
			if ((Object)(object)val == (Object)null)
			{
				return;
			}
			Bot bot = null;
			float num2 = Tuning.F("point.thing.far", 40f);
			foreach (Bot item2 in BotManager.All)
			{
				if (!item2.dead && !item2.enemy && !item2.InLesson && !item2.climbing && !item2.Reloading && !((Object)(object)item2.nearestSeen.transform != (Object)null) && !((Object)(object)item2.brainTarget.transform != (Object)null) && !((Object)(object)item2.rig.activeSeat != (Object)null))
				{
					val3 = val.worldCenterOfMass - item2.Feet;
					float magnitude2 = ((Vector3)(ref val3)).magnitude;
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
		catch (Exception)
		{
		}
	}

	private void Aware(Transform you, Transform head)
	{
		if ((Object)(object)you == (Object)null || (Object)(object)head == (Object)null || enemy)
		{
			return;
		}
		WatchPlayer(you);
		Vector3 val = you.position - head.position;
		float magnitude = ((Vector3)(ref val)).magnitude;
		bool flag = (Object)(object)nearestSeen.transform == (Object)null && (Object)(object)brainTarget.transform == (Object)null && !Reloading && !climbing && !InLesson && Time.time >= waveUntil;
		if (pointedAt > pointSeen && Time.time - pointedAt < 1f)
		{
			pointSeen = pointedAt;
			if (flag && magnitude < Tuning.F("point.far", 14f) && Targets.InSight(head.position, you.position, ((Object)(object)Player.RigManager != (Object)null) ? ((Component)Player.RigManager).transform : null, rig))
			{
				Look(pointPlace, null, "where you pointed", Random(2.2f, 3.5f));
				Lab.Event("looks where you pointed");
				if ((mode == Mode.Wander || (mode == Mode.Brain && (goal == Goal.Explore || goal == Goal.Idle || goal == Goal.Play))) && !Playing && Ground(pointPlace + Vector3.up * 0.5f, out var ground))
				{
					Vector3 val2 = Flat(ground - Feet);
					if (((Vector3)(ref val2)).magnitude > 3f && Random.value < Tuning.F("point.go", 0.7f))
					{
						Vector3 val3 = ground;
						val2 = Flat(ground - Feet);
						Vector3 val4 = val3 - ((Vector3)(ref val2)).normalized * 1.2f;
						planPlace = (Ground(val4 + Vector3.up * 0.5f, out var ground2) ? ground2 : ground);
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
						string[] obj = new string[5] { "roam: ", planWhy, " (", null, null };
						val2 = Flat(planPlace - Feet);
						obj[3] = ((Vector3)(ref val2)).magnitude.ToString("0");
						obj[4] = " m)";
						Lab.Event(string.Concat(obj));
					}
				}
			}
		}
		bool flag2 = magnitude < 9f && Vector3.Angle(you.forward, -val) < Tuning.F("stare.cone", 9f);
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
			if (!((Object)(object)head == (Object)null) && Tactics.InPlayersLine(head.position - Vector3.up * 0.4f, out var outOfIt))
			{
				Vector3 val = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * outOfIt;
				walk = new Vector2(val.x, val.z) * MotionLibrary.WalkPush;
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
			Avatar avatar = rig.avatar;
			Transform head = Head;
			if ((Object)(object)avatar == (Object)null || (Object)(object)head == (Object)null)
			{
				return;
			}
			AudioVarianceData val = (AudioVarianceData)(what switch
			{
				"heave" => (Clips(avatar.bigEffort) > 0) ? avatar.bigEffort : avatar.smallEffort, 
				"effort" => avatar.smallEffort, 
				"clear" => avatar.unAgro, 
				"spot" => avatar.agro, 
				_ => avatar.recovery, 
			});
			if (Clips(val) == 0)
			{
				HeadSFX val2 = ((what == "spot" || what == "clear") ? null : ((Component)rig).GetComponentInChildren<HeadSFX>(true));
				if (!((Object)(object)val2 == (Object)null))
				{
					calledAt[what] = Time.time;
					if (what == "up")
					{
						val2.RecoveryVocal();
					}
					else
					{
						val2.JumpEffort();
					}
				}
			}
			else
			{
				calledAt[what] = Time.time;
				voiceAt = Time.time;
				Audio3dManager.PlayAtPoint(val.audioClips, head.position, Audio3dManager.hardInteraction, Tuning.F("bot.voice", 0.9f), 1f, new Nullable<float>(0.06f), new Nullable<float>(1f), new Nullable<float>(1f));
			}
		}
		catch (Exception)
		{
		}
	}

	public void LabVoice(string what)
	{
		Avatar avatar = rig.avatar;
		if (!((Object)(object)avatar == (Object)null))
		{
			Lab.Event("voice: " + ((Object)avatar).name + " has " + Clips(avatar.agro) + " for spotting, " + Clips(avatar.unAgro) + " for all clear, " + Clips(avatar.smallEffort) + " small effort, " + Clips(avatar.bigEffort) + " big effort, " + Clips(avatar.recovery) + " recovery, " + Clips(avatar.spawn) + " spawn");
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
		if ((Object)(object)head == (Object)null)
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
		Vector3 val;
		foreach (Targets.Target hostile in hostiles)
		{
			Vector3 point = hostile.Point;
			val = point - position;
			float magnitude = ((Vector3)(ref val)).magnitude;
			if (magnitude < nearestAnyDistance)
			{
				nearestAnyDistance = magnitude;
				nearestAnyAt = point;
			}
			if (!(magnitude > Tuning.F("brain.see.far", 55f)))
			{
				IntPtr key = (((Object)(object)hostile.transform != (Object)null) ? ((Il2CppObjectBase)(object)hostile.transform).Pointer : IntPtr.Zero);
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
		if ((Object)(object)nearestSeen.transform != (Object)null)
		{
			if (!wasSeen && goal == Goal.Investigate)
			{
				Feel("found");
			}
			lastEnemyAt = mostDangerous.Point;
			lastEnemyVelocity = Tactics.Velocity(mostDangerous);
			lastEnemyTime = Time.time;
		}
		if ((Object)(object)nearestSeen.transform != (Object)null && !wasSeen)
		{
			Call("spot");
		}
		wasSeen = (Object)(object)nearestSeen.transform != (Object)null;
		if (Time.time - PlayerWatch.FiredAt < 0.5f && (Object)(object)nearestSeen.transform == (Object)null)
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
		if ((Object)(object)engaged.transform != (Object)null && !Targets.Alive(engaged))
		{
			if (Time.time - engagedAt < 2f)
			{
				if ((Object)(object)engaged.prop != (Object)null)
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
					string barcode = heldBarcode;
					val = engaged.Point - position;
					Knowledge.KilledWith(barcode, ((Vector3)(ref val)).magnitude);
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
		float num3;
		if (!((Object)(object)Player.Head != (Object)null))
		{
			num3 = 0f;
		}
		else
		{
			val = Flat(Player.Head.position - position);
			num3 = ((Vector3)(ref val)).magnitude;
		}
		float num4 = num3;
		seen[0] = 1f;
		seen[1] = (((Object)(object)nearestSeen.transform != (Object)null) ? 1f : 0f);
		seen[2] = ((nearestSeenDistance < 5f) ? 1f : 0f);
		seen[3] = Mathf.Clamp01((float)seenEnemies.Count / 4f);
		seen[4] = (Armed ? 1f : 0f);
		seen[5] = ((Armed && AmmoFraction() < 0.3f) ? 1f : 0f);
		seen[6] = ((num4 > 10f) ? 1f : 0f);
		seen[7] = (((Object)(object)nearestSeen.transform == (Object)null && Time.time - lastEnemyTime < 25f) ? 1f : 0f);
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
			Poolee componentInParent = ((Component)g).GetComponentInParent<Poolee>();
			return ((Object)(object)componentInParent != (Object)null && (Object)(object)componentInParent.SpawnableCrate != (Object)null) ? ((Scannable)componentInParent.SpawnableCrate).Barcode.ID : ((Object)g).name;
		}
		catch (Exception)
		{
			return ((Object)g).name;
		}
	}

	private float AmmoFraction()
	{
		try
		{
			int num = (((Object)(object)gun.defaultMagazine != (Object)null) ? Mathf.Max(1, gun.defaultMagazine.rounds) : 15);
			return Mathf.Clamp01((float)gun.AmmoCount() / (float)num);
		}
		catch (Exception)
		{
			return 1f;
		}
	}

	private float Fit(Goal g, out string reason)
	{
		bool flag = (Object)(object)nearestSeen.transform != (Object)null;
		bool flag2 = !flag && Time.time - lastEnemyTime < 25f;
		float num;
		Vector3 val;
		if (!((Object)(object)Player.Head != (Object)null) || !((Object)(object)Head != (Object)null))
		{
			num = 0f;
		}
		else
		{
			val = Flat(Player.Head.position - Head.position);
			num = ((Vector3)(ref val)).magnitude;
		}
		float num2 = num;
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
			float num3 = ((coverPhase != 0) ? Tuning.F("cover.keep", 15f) : Tuning.F("brain.keep", 4.5f));
			if (!Armed || !(Time.time - lastEnemyTime < num3))
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
			if (enemy && (Object)(object)Hunted != (Object)null && (Object)(object)Head != (Object)null)
			{
				val = Hunted.position - Head.position;
				if (((Vector3)(ref val)).magnitude < Tuning.F("enemy.hunt", 90f))
				{
					return 0.62f;
				}
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
				if (!(num2 > 18f))
				{
					if (!(num2 > 10f))
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
			if (!Armed || !(num2 < 25f))
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
			if ((Object)(object)nearestSeen.transform == (Object)null)
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
		if (InLesson || down || Reloading || Time.time < climbAfterAt || (Object)(object)rig.physicsRig == (Object)null || (Object)(object)((Rig)rig.physicsRig).centerOfPressure == (Object)null)
		{
			return false;
		}
		Vector3 val;
		if (enemy && (Object)(object)Hunted != (Object)null)
		{
			val = Hunted.position - Vector3.up * 1.4f;
		}
		else if ((Object)(object)nearestSeen.transform != (Object)null)
		{
			val = nearestSeen.Point - Vector3.up * 1.3f;
		}
		else
		{
			if (!(Time.time - lastEnemyTime < 20f) || !(lastEnemyTime > 0f))
			{
				return false;
			}
			val = lastEnemyAt - Vector3.up * 1.3f;
		}
		Vector3 position = ((Rig)rig.physicsRig).centerOfPressure.position;
		float num = val.y - position.y;
		Vector3 val2 = Flat(val - position);
		if (num < Tuning.F("climbafter.up", 1.2f) || ((Vector3)(ref val2)).magnitude > Tuning.F("climbafter.near", 12f) || ((Vector3)(ref val2)).magnitude < 0.05f)
		{
			return false;
		}
		Vector3 normalized = ((Vector3)(ref val2)).normalized;
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
			if (((Vector3)(ref val2)).magnitude > 1.2f && !Playing)
			{
				running = ((Vector3)(ref val2)).magnitude > 2.5f;
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
		nav.SetGoal(val);
		walk = Vector2.zero;
		climbLookAt = 0f;
		climbKeenAt = Time.time;
		if (TryStartClimb(normalized))
		{
			Lab.Event("climb: up after " + (enemy ? "you" : "it") + " (" + num.ToString("0.0") + " m above, " + ((Vector3)(ref val2)).magnitude.ToString("0.0") + " m off)");
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
			coverWantAt = Time.time + Random.Range(2.5f, 6f);
		}
		gunThreatAt = Time.time;
		if (Time.time < coverRestUntil)
		{
			return false;
		}
		if (coverReachedAt > 0f && Time.time - coverReachedAt > coverStay)
		{
			CoverDone(Random.Range(4f, 9f));
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
		coverWantAt = coverRestUntil + Random.Range(0f, 5f);
		coverStay = Random.Range(1.5f, 3.5f);
	}

	private void Think()
	{
		Goal goal = this.goal;
		float num = float.MinValue;
		string text = "";
		foreach (Goal value in Enum.GetValues(typeof(Goal)))
		{
			string reason;
			float num2 = Fit(value, out reason);
			if (!(num2 <= 0f))
			{
				float num3 = Knowledge.Worth(value.ToString(), seen);
				float num4 = num2 * (1f + 0.6f * num3) + Random.Range(-0.03f, 0.03f);
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
		Array.Copy(seen, windowSeen, seen.Length);
	}

	private void SwitchGoal(Goal next)
	{
		Lab.Event("goal " + goal.ToString() + " -> " + next);
		LearnWindow();
		if (goal == Goal.TakeCover && Time.time > coverRestUntil)
		{
			CoverDone(Random.Range(6f, 12f));
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
		if (WeaponInHand && (Object)(object)nearestSeen.transform == (Object)null && Time.time - lastEnemyTime > Tuning.F("stow.after", 7f) && Time.time - drawnAt > 3f && !Reloading && (!Targets.Practice || enemy))
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
		{
			Vector3 val;
			if (goal == Goal.Investigate && enemy && hasGoalPlace && (Object)(object)Hunted != (Object)null)
			{
				val = Hunted.position - huntedAt;
				if (((Vector3)(ref val)).magnitude > 2.5f && !climbing)
				{
					hasGoalPlace = false;
					nav.Clear();
				}
			}
			PlaceFor(goal);
			if (hasGoalPlace)
			{
				if (!nav.HasGoal)
				{
					nav.SetGoal(goalPlace);
				}
				int num;
				if (goal != Goal.Retreat && goal != Goal.TakeCover)
				{
					if ((goal == Goal.Investigate || goal == Goal.ArmUp || goal == Goal.Guard || goal == Goal.Fight) && nav.HasGoal)
					{
						val = Flat(nav.Goal - Feet);
						num = ((((Vector3)(ref val)).magnitude > 6f) ? 1 : 0);
					}
					else
					{
						num = 0;
					}
				}
				else
				{
					num = 1;
				}
				running = (byte)num != 0;
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
		}
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
		Vector3 val = Feet;
		switch (g)
		{
		case Goal.Retreat:
		{
			Vector3 val6 = Flat(val - nearestAnyAt);
			val6 = ((((Vector3)(ref val6)).sqrMagnitude < 0.01f) ? (Quaternion.Euler(0f, bodyYaw + 180f, 0f) * Vector3.forward) : ((Vector3)(ref val6)).normalized);
			hasGoalPlace = Navigator.RandomPlace(val + val6 * 7f, 0f, 2f, out goalPlace);
			break;
		}
		case Goal.TakeCover:
		{
			if (coverReachedAt < 0f && Tactics.FindCover(val, seenEnemies, out var cover))
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
			if (enemy && (Object)(object)nearestSeen.transform == (Object)null && (Object)(object)Hunted != (Object)null)
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
			if ((Object)(object)head != (Object)null)
			{
				Vector3 val2 = Flat(PlayerWatch.Fighting ? PlayerWatch.AimAlong : head.forward);
				val2 = ((((Vector3)(ref val2)).sqrMagnitude < 0.01f) ? Vector3.forward : ((Vector3)(ref val2)).normalized);
				Vector3 val3 = Vector3.Cross(Vector3.up, val2) * ((Vector3.Dot(val - head.position, Vector3.Cross(Vector3.up, val2)) >= 0f) ? 1f : (-1f));
				Vector3 val4 = head.position - Vector3.up * 1.4f + val3 * 2.2f - val2 * 1.2f;
				Vector3 val5 = Flat(val4 - val);
				if (((Vector3)(ref val5)).magnitude > 1.5f && Navigator.RandomPlace(val4, 0f, 0.8f, out var place2))
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
				if (Navigator.RandomPlace(val, 5f, 18f, out var place) && (!visited.Contains(Cell(place)) || i == 7))
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
		if (!((Object)(object)head == (Object)null) && Tactics.InPlayersLine(head.position - Vector3.up * 0.4f, out var outOfIt))
		{
			Vector3 val = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * outOfIt;
			walk = new Vector2(val.x, val.z) * MotionLibrary.WalkPush;
			if (Time.time - closeCallAt > 1f && Random.value < dt)
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
			Vector3 position = head.position;
			Vector3 val = Flat(PlayerWatch.AimAlong);
			return position - ((Vector3)(ref val)).normalized * 8f - Vector3.up * 0.8f;
		}
		float num = Mathf.Sin((Time.time - born) * 0.55f) * 40f;
		return head.position + Quaternion.Euler(0f, bodyYaw + num, 0f) * new Vector3(0f, -1.3f, 2.5f);
	}

	private void Show()
	{
		if (!BotManager.ShowThoughts)
		{
			if ((Object)(object)thought != (Object)null)
			{
				((Component)thought).gameObject.SetActive(false);
			}
			return;
		}
		Transform head = Head;
		if ((Object)(object)head == (Object)null)
		{
			return;
		}
		if ((Object)(object)thought == (Object)null)
		{
			GameObject val = new GameObject("ThirdEyeBotThought");
			thought = val.AddComponent<TextMeshPro>();
			if ((Object)(object)font == (Object)null)
			{
				foreach (TMP_FontAsset item in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
				{
					if ((Object)(object)item != (Object)null)
					{
						font = item;
						break;
					}
				}
			}
			if ((Object)(object)font != (Object)null)
			{
				((TMP_Text)thought).font = font;
			}
			((TMP_Text)thought).fontSize = 0.5f;
			((TMP_Text)thought).rectTransform.sizeDelta = new Vector2(3f, 0.6f);
			((TMP_Text)thought).alignment = (TextAlignmentOptions)1026;
			((TMP_Text)thought).enableWordWrapping = false;
			((TMP_Text)thought).richText = true;
			((Graphic)thought).color = new Color(0.75f, 0.95f, 1f, 0.95f);
		}
		((Component)thought).gameObject.SetActive(true);
		Transform transform = thought.transform;
		transform.position = head.position + Vector3.up * 0.55f;
		if ((Object)(object)Player.Head != (Object)null)
		{
			transform.rotation = Quaternion.LookRotation(transform.position - Player.Head.position, Vector3.up);
		}
		string text = goal.ToString() + ((why.Length > 0) ? (": " + why) : "");
		if (Reloading)
		{
			text = "Reloading (" + reload.ToString() + ")";
		}
		float num = Knowledge.Worth(goal.ToString(), seen);
		((TMP_Text)thought).text = text + "\n<size=60%>expects " + num.ToString("+0.00;-0.00", CultureInfo.InvariantCulture) + (Armed ? ("  ammo " + Mathf.RoundToInt(AmmoFraction() * 100f) + "%") : "") + "</size>";
	}

	private void HideThought()
	{
		if ((Object)(object)thought != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)thought).gameObject);
		}
		thought = null;
	}

	private bool FreeToCatch()
	{
		Hand rightHand = RightHand;
		if (!dead && !down && !surrendered && !Puppet && !climbing && !Playing && (Object)(object)gun == (Object)null && (Object)(object)rightHand != (Object)null && (Object)(object)rightHand.m_CurrentAttachedGO == (Object)null && (Object)(object)target.transform == (Object)null && !InLesson && !sitting && rescuing == null)
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
		if ((Object)(object)head == (Object)null)
		{
			return null;
		}
		Bot result = null;
		float num = Tuning.F("catch.far", 14f);
		foreach (Bot item in BotManager.All)
		{
			if (item != this && item.enemy == enemy && item.FreeToCatch() && !((Object)(object)item.Head == (Object)null))
			{
				Vector3 val = Flat(item.Head.position - head.position);
				float magnitude = ((Vector3)(ref val)).magnitude;
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
				float num;
				if (!((Object)(object)item.Head != (Object)null) || !((Object)(object)head != (Object)null))
				{
					num = -1f;
				}
				else
				{
					Vector3 val = Flat(item.Head.position - head.position);
					num = ((Vector3)(ref val)).magnitude;
				}
				float num2 = num;
				text = text + item.Name + ": " + num2.ToString("0.0") + " m, " + ((item.enemy != enemy) ? "other side" : ((!item.FreeToCatch()) ? ("busy (playing " + item.play.ToString() + ", gun " + ((Object)(object)item.gun != (Object)null) + ", hand " + (((Object)(object)item.RightHand != (Object)null && (Object)(object)item.RightHand.m_CurrentAttachedGO != (Object)null) ? "full" : "empty") + ", mode " + item.mode.ToString() + ")") : "free")) + "; ";
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
		if (partner == null || (Object)(object)head == (Object)null || (Object)(object)partner.Head == (Object)null)
		{
			return false;
		}
		if (newGame)
		{
			catchThrowsLeft = Random.Range(3, 7);
			Lab.Event("catch: a game of catch with " + partner.Name + " (" + catchThrowsLeft + " throws)");
		}
		catchWith = partner;
		partner.catchWith = this;
		partner.catchThrowsLeft = catchThrowsLeft;
		partner.WaitToCatch();
		then = Then.ToPartner;
		throwFar = 0f;
		Vector3 val = Flat(partner.Head.position - head.position);
		throwDir = ((Vector3)(ref val)).normalized;
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
		Transform val = ((catchWith != null) ? catchWith.Head : null);
		if ((Object)(object)val == (Object)null)
		{
			return throwDir * 6f + Vector3.up * 3f;
		}
		Vector3 val2 = val.position - Vector3.up * Tuning.F("catch.chest", 0.35f);
		Vector3 val3 = val2 - thing.worldCenterOfMass;
		float t = Mathf.Clamp(((Vector3)(ref val3)).magnitude / Tuning.F("catch.pace", 6f), Tuning.F("catch.air", 0.75f), 1.3f);
		return Lob(thing.worldCenterOfMass, val2, t);
	}

	private bool CatchStep(Transform head, float dt)
	{
		Bot bot = catchWith;
		Hand rightHand = RightHand;
		Transform vrRoot = controls.vrRoot;
		if (bot == null || bot.dead || (Object)(object)head == (Object)null || (Object)(object)rightHand == (Object)null || (Object)(object)vrRoot == (Object)null)
		{
			return EndCatch("the other one is gone");
		}
		walk = Vector2.zero;
		running = false;
		Transform head2 = bot.Head;
		if ((Object)(object)head2 != (Object)null)
		{
			Face(head2.position, dt, 200f);
		}
		handOverride = true;
		playHeight = standingHeight;
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector(0.18f, standingHeight - 0.45f, 0.35f);
		if ((Object)(object)catchComing == (Object)null)
		{
			handAt = val;
			handTurn = Quaternion.Euler(-10f, -20f, 0f);
			playPitch = (((Object)(object)head2 != (Object)null) ? LookPitch(head2, head) : 0f);
			if (Time.time - catchWaitSince > Tuning.F("catch.wait", 15f))
			{
				return EndCatch("no throw came");
			}
			return true;
		}
		Vector3 worldCenterOfMass = catchComing.worldCenterOfMass;
		Vector3 val2 = vrRoot.InverseTransformPoint(worldCenterOfMass);
		Vector3 val3 = default(Vector3);
		((Vector3)(ref val3))._002Ector(0.18f, standingHeight - 0.25f, 0f);
		Vector3 val4 = val2 - val3;
		handAt = val3 + Vector3.ClampMagnitude(val4, Tuning.F("catch.arm", 0.65f));
		handTurn = Quaternion.Euler(-20f, -20f, 0f);
		float num = worldCenterOfMass.y - head.position.y;
		Vector3 val5 = Flat(worldCenterOfMass - head.position);
		playPitch = Mathf.Clamp((0f - Mathf.Atan2(num, Mathf.Max(0.3f, ((Vector3)(ref val5)).magnitude))) * 57.29578f, -50f, 60f);
		val5 = ((Component)rightHand).transform.position - worldCenterOfMass;
		float magnitude = ((Vector3)(ref val5)).magnitude;
		if (magnitude < Tuning.F("catch.reach", 0.45f) && (Object)(object)catchComingGrip != (Object)null && !catchComingGrip.HasAttachedHands() && Grab(rightHand, catchComingGrip, null))
		{
			catchesMade++;
			item = catchComing;
			grip = catchComingGrip;
			catchComing = null;
			catchThrowsLeft--;
			Lab.Event("catch: caught it (" + magnitude.ToString("0.00") + " m from its hand; " + catchesMade + " caught, " + catchesMissed + " missed)");
			holdFor = Random.Range(0.6f, 1.5f);
			Next(Play.Hold);
			return true;
		}
		if (!(Time.time - catchFlyingSince > Tuning.F("catch.flight", 2.5f)))
		{
			val5 = catchComing.velocity;
			if (!(((Vector3)(ref val5)).magnitude < 0.5f) || !(Time.time - catchFlyingSince > 0.5f))
			{
				goto IL_045e;
			}
		}
		catchesMissed++;
		catchThrowsLeft--;
		Lab.Event("catch: missed it (closest " + magnitude.ToString("0.00") + " m; " + catchesMade + " caught, " + catchesMissed + " missed)");
		Rigidbody val6 = catchComing;
		Grip val7 = catchComingGrip;
		catchComing = null;
		if (catchThrowsLeft <= 0 || (Object)(object)val6 == (Object)null || (Object)(object)val7 == (Object)null)
		{
			return EndCatch("that was the last throw");
		}
		item = val6;
		grip = val7;
		play = Play.GoTo;
		playAt = Time.time;
		nav.SetGoal(val6.worldCenterOfMass);
		goto IL_045e;
		IL_045e:
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
		Vector3 val = (((Object)(object)physicsRig != (Object)null && (Object)(object)((Rig)physicsRig).centerOfPressure != (Object)null) ? ((Rig)physicsRig).centerOfPressure.position : Feet);
		bool flag = nav.HasGoal && nav.Goal.y - val.y > 0.8f;
		if (!climbKeen || !flag)
		{
			if (!climbKeen && nav.StuckCount >= 2)
			{
				climbLookAt = Time.time + 12f;
				nav.GiveUp();
			}
			return false;
		}
		Vector3 val2 = (nav.HasGoal ? Flat(nav.Goal - val) : Vector3.zero);
		Vector3 edge;
		Vector3 inward;
		float height;
		bool flag2 = FindEdge(val, dir, out edge, out inward, out height);
		if (!flag2 && flag && ((Vector3)(ref val2)).magnitude > 0.3f && ((Vector3)(ref val2)).magnitude < 10f)
		{
			flag2 = FindEdge(val, ((Vector3)(ref val2)).normalized, out edge, out inward, out height);
		}
		if ((!flag2 || height > Tuning.F("climb.max", 2.5f)) && flag && FindTallWall(val, (((Vector3)(ref val2)).sqrMagnitude > 0.09f) ? ((Vector3)(ref val2)).normalized : dir, 1.8f, out var foot, out var inward2, out var height2, out var top))
		{
			if (val.y + height2 > nav.Goal.y + 1f)
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
			if (!climbKeen && Random.value > Tuning.F("climb.roam.chance", 0.3f))
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
		if (!climbKeen && Random.value > Tuning.F("climb.roam.chance", 0.3f))
		{
			Lab.Event("climb: a " + height.ToString("0.0") + " m wall, not worth it while going about: somewhere else");
			climbLookAt = Time.time + 12f;
			nav.GiveUp();
			return false;
		}
		if ((((Object)(object)LeftHand != (Object)null && (Object)(object)LeftHand.m_CurrentAttachedGO != (Object)null) || ((Object)(object)RightHand != (Object)null && (Object)(object)RightHand.m_CurrentAttachedGO != (Object)null)) && (!WeaponInHand || !Stow()))
		{
			return false;
		}
		top = edge - climbLastEdge;
		if (((Vector3)(ref top)).magnitude < 1.5f || Time.time - climbUpAt < 10f)
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
		if (!climbKeen && SelfTaught.ClimbFails(edge) >= 2 && Random.value > 0.15f)
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
		Vector3 val;
		if (climbing)
		{
			val = Flat(climbEdge - at);
			if (((Vector3)(ref val)).magnitude < within)
			{
				return true;
			}
		}
		if (InLesson && cloneLesson != null && cloneLesson.purpose == "climb" && lessonPurposeful)
		{
			val = Flat(lessonWallFoot - at);
			return ((Vector3)(ref val)).magnitude < within;
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
		RaycastHit val = default(RaycastHit);
		if (!Physics.Raycast(feet + Vector3.up * 0.6f, dir, ref val, 1.6f, -1, (QueryTriggerInteraction)1))
		{
			return false;
		}
		if ((Object)(object)((Component)((RaycastHit)(ref val)).collider).GetComponentInParent<RigManager>() != (Object)null || (Object)(object)((RaycastHit)(ref val)).collider.attachedRigidbody != (Object)null)
		{
			return false;
		}
		inward = -Flat(((RaycastHit)(ref val)).normal);
		if (((Vector3)(ref inward)).sqrMagnitude < 0.01f)
		{
			return false;
		}
		((Vector3)(ref inward)).Normalize();
		RaycastHit val2 = default(RaycastHit);
		if (!Physics.Raycast(((RaycastHit)(ref val)).point + inward * 0.3f + Vector3.up * 3.3f, Vector3.down, ref val2, 3.6f, -1, (QueryTriggerInteraction)1))
		{
			return false;
		}
		if (((RaycastHit)(ref val2)).normal.y < 0.7f || ((RaycastHit)(ref val2)).point.y < feet.y + 0.5f)
		{
			return false;
		}
		if (Physics.Raycast(((RaycastHit)(ref val2)).point + Vector3.up * 0.05f, Vector3.up, 1.5f, -1, (QueryTriggerInteraction)1))
		{
			return false;
		}
		RaycastHit val3 = default(RaycastHit);
		if (!Physics.Raycast(((RaycastHit)(ref val)).point + inward * 0.9f + Vector3.up * 3.3f, Vector3.down, ref val3, 3.6f, -1, (QueryTriggerInteraction)1) || Mathf.Abs(((RaycastHit)(ref val3)).point.y - ((RaycastHit)(ref val2)).point.y) > 0.3f)
		{
			return false;
		}
		height = ((RaycastHit)(ref val2)).point.y - feet.y;
		edge = new Vector3(((RaycastHit)(ref val)).point.x, ((RaycastHit)(ref val2)).point.y, ((RaycastHit)(ref val)).point.z) + inward * 0.04f;
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
			Vector3 val = (((Object)(object)rig.physicsRig != (Object)null && (Object)(object)((Rig)rig.physicsRig).centerOfPressure != (Object)null) ? ((Rig)rig.physicsRig).centerOfPressure.position : Feet);
			Vector3 val2 = Flat(climbEdge - val);
			if (((Vector3)(ref val2)).magnitude > Tuning.F("climb.near", 0.45f) && Time.time - climbSince < 4f)
			{
				Go(((Vector3)(ref val2)).normalized, MotionLibrary.WalkPush * 0.85f, dt);
				break;
			}
			walk = Vector2.zero;
			bodyYaw = Recorder.Yaw(climbIn);
			climbPhase = 1;
			climbT = 0f;
			climbReach = 0f;
			climbWait = 0f;
			climbFrom = pose;
			climbStartHead = (((Object)(object)Head != (Object)null) ? Pose.Of(Head) : new Pose(climbEdge, climbFacing));
			climbStartLeft = (((Object)(object)LeftHand != (Object)null) ? Pose.Of(((Component)LeftHand).transform) : new Pose(climbEdge, climbFacing));
			climbStartRight = (((Object)(object)RightHand != (Object)null) ? Pose.Of(((Component)RightHand).transform) : new Pose(climbEdge, climbFacing));
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
				if ((Object)(object)Head != (Object)null)
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
				float num = (((Object)(object)physicsRig != (Object)null && (Object)(object)((Rig)physicsRig).centerOfPressure != (Object)null) ? (((Rig)physicsRig).centerOfPressure.position.y - (climbEdge.y - climbHeight)) : 0f);
				StopClimb((num > climbHeight * 0.6f) ? "up" : "did not get up");
			}
			break;
		}
	}

	public void ClimbFixed(float dt)
	{
		if (climbing && climbHang && climbPhase == 1 && !climbGliding && (Object)(object)rig.physicsRig != (Object)null && !Bad(climbHangFeet))
		{
			rig.Teleport(climbHangFeet, true);
		}
		if (!climbing || climbPhase != 1 || climbReach < Tuning.F("climb.reach", 0.5f))
		{
			return;
		}
		PhysicsRig physicsRig = rig.physicsRig;
		Rigidbody val = (((Object)(object)physicsRig != (Object)null && (Object)(object)((Rig)physicsRig).m_pelvis != (Object)null) ? ((Component)((Rig)physicsRig).m_pelvis).GetComponent<Rigidbody>() : null);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		if (!climbLifting)
		{
			climbLifting = true;
			climbGliding = false;
			climbLiftFrom = val.position;
			climbFeetFrom = (climbHang ? climbHangFeet : (((Object)(object)((Rig)physicsRig).centerOfPressure != (Object)null) ? ((Rig)physicsRig).centerOfPressure.position : (val.position - Vector3.up * 0.95f)));
			climbGlide = climbFeetFrom;
			climbBodies.Clear();
			Rigidbody val2 = (((Object)(object)LeftHand != (Object)null) ? LeftHand.rb : null);
			Rigidbody val3 = (((Object)(object)RightHand != (Object)null) ? RightHand.rb : null);
			foreach (Rigidbody componentsInChild in ((Component)physicsRig).GetComponentsInChildren<Rigidbody>())
			{
				if ((Object)(object)componentsInChild != (Object)null && !componentsInChild.isKinematic && (Object)(object)componentsInChild != (Object)(object)val2 && (Object)(object)componentsInChild != (Object)(object)val3)
				{
					climbBodies.Add(componentsInChild);
				}
			}
		}
		LiftShape(climbT, out var up, out var over);
		float num = climbHeight + Tuning.F("climb.extra", 0.12f);
		Vector3 val4 = Flat(climbEdge - climbLiftFrom);
		float num2 = ((Vector3)(ref val4)).magnitude + Tuning.F("climb.onto", 0.45f);
		Vector3 val5 = climbLiftFrom + Vector3.up * (up * num) + climbIn * (over * num2);
		if (Bad(val5) || Bad(val.position))
		{
			StopClimb("its body went wild");
			return;
		}
		if (Time.time - climbLiftSaidAt > 0.5f)
		{
			climbLiftSaidAt = Time.time;
			string[] obj = new string[15]
			{
				"climb lift: hips ",
				(val.position.y - climbLiftFrom.y).ToString("0.00"),
				" m up of ",
				(up * num).ToString("0.00"),
				" wanted, ",
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null
			};
			val4 = Flat(val.position - climbLiftFrom);
			obj[5] = ((Vector3)(ref val4)).magnitude.ToString("0.00");
			obj[6] = " m over of ";
			obj[7] = (over * num2).ToString("0.00");
			obj[8] = ", hands holding ";
			obj[9] = climbHolding[0].ToString();
			obj[10] = "/";
			obj[11] = climbHolding[1].ToString();
			obj[12] = ", from the wall ";
			val4 = Flat(climbEdge - val.position);
			obj[13] = ((Vector3)(ref val4)).magnitude.ToString("0.00");
			obj[14] = " m";
			Lab.Event(string.Concat(obj));
		}
		if (Tuning.F("climb.glide", 1f) > 0.5f)
		{
			Vector3 val6 = climbFeetFrom + Vector3.up * (up * num);
			Vector3 val7 = climbIn;
			float num3 = over;
			val4 = Flat(climbEdge - climbFeetFrom);
			Vector3 val8 = val6 + val7 * (num3 * (((Vector3)(ref val4)).magnitude + Tuning.F("climb.onto", 0.45f)));
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
				float num4 = Mathf.Clamp01((up - 0.25f) / 0.25f) * (1f - Mathf.Clamp01((over - 0.3f) / 0.4f));
				rig.remapHeptaRig._crouchTarget = Tuning.F("climb.tuck", -0.25f) * num4;
			}
			climbGlide = Vector3.MoveTowards(climbGlide, val8, Tuning.F("climb.glide.step", 0.03f));
			if (climbGliding)
			{
				rig.Teleport(climbGlide, true);
			}
			return;
		}
		Vector3 val9 = Vector3.ClampMagnitude((val5 - val.position) * Tuning.F("climb.pull", 8f) - val.velocity, Tuning.F("climb.step", 0.35f));
		foreach (Rigidbody climbBody in climbBodies)
		{
			if ((Object)(object)climbBody != (Object)null)
			{
				climbBody.AddForce(val9, (ForceMode)2);
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
		List<ClimbMoves.Frame> frames = climbMove.frames;
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
		Hand val = ((which == 0) ? LeftHand : RightHand);
		if (want && !climbHolding[which] && (Object)(object)val != (Object)null)
		{
			Vector3 val2 = ((Component)val).transform.position - target;
			return ((Vector3)(ref val2)).magnitude < Tuning.F("climb.close", 0.45f);
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
		Vector3 val = (((Object)(object)rig.physicsRig != (Object)null && (Object)(object)((Rig)rig.physicsRig).centerOfPressure != (Object)null) ? ((Rig)rig.physicsRig).centerOfPressure.position : Feet);
		climbLookAt = 0f;
		climbTries = 0;
		if (FindHighGround(val, out var top))
		{
			nav.Clear();
			nav.SetGoal(top);
			labClimbUntil = Time.time + 40f;
			act = Act.Climb;
			actUntil = Time.time + 40f;
			string[] obj = new string[5] { "climb: sent up to high ground ", null, null, null, null };
			Vector3 val2 = Flat(top - val);
			obj[1] = ((Vector3)(ref val2)).magnitude.ToString("0.0");
			obj[2] = " m off, ";
			obj[3] = (top.y - val.y).ToString("0.00");
			obj[4] = " m up";
			Lab.Event(string.Concat(obj));
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
		List<ClimbMoves.Frame> frames = climbMove.frames;
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
		Vector3 val = Vector3.Cross(Vector3.up, climbIn);
		Vector3 val2 = climbEdge - val * 0.22f + Vector3.up * 0.03f;
		Vector3 val3 = climbEdge + val * 0.22f + Vector3.up * 0.03f;
		Quaternion rotation = climbFacing * Quaternion.Euler(70f, 0f, 0f);
		float num = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.6f));
		float num2 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.6f) / 1.2f));
		float num3 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 1.8f) / 0.6f));
		Vector3 val4 = Vector3.down * (0.9f * num2 + 0.4f * num3) + climbIn * (0.25f * num3);
		climbLeft = new Pose(val2 + val4, rotation);
		climbRight = new Pose(val3 + val4, rotation);
		Vector3 position = climbEdge - climbIn * 0.3f + Vector3.up * Mathf.Lerp(-0.25f, 0.45f, num2) + climbIn * (0.5f * num3) + Vector3.up * (0.4f * num3);
		climbHead = new Pose(position, climbFacing * Quaternion.Euler(Mathf.Lerp(-25f, 15f, num2), 0f, 0f));
		climbWantRight = (climbWantLeft = t > 0.5f && t < 2.35f);
		if (num < 1f)
		{
			climbLeft.position = Vector3.Lerp(((Object)(object)Head != (Object)null) ? (Head.position - Vector3.up * 0.4f - val * 0.2f) : val2, climbLeft.position, num);
			climbRight.position = Vector3.Lerp(((Object)(object)Head != (Object)null) ? (Head.position - Vector3.up * 0.4f + val * 0.2f) : val3, climbRight.position, num);
		}
	}

	private void HoldWorld(int which, bool want, Vector3 target)
	{
		Hand val = ((which == 0) ? LeftHand : RightHand);
		Grip worldGrip = (Grip)(object)rig.worldGrip;
		if ((Object)(object)val == (Object)null || (Object)(object)worldGrip == (Object)null)
		{
			return;
		}
		if (want && !climbHolding[which])
		{
			Vector3 val2 = ((Component)val).transform.position - target;
			float magnitude = ((Vector3)(ref val2)).magnitude;
			bool flag = magnitude < Tuning.F("climb.grab", 0.3f) || (Touching(val) && ((Component)val).transform.position.y > climbEdge.y - Tuning.F("climb.top", 0.3f));
			if ((Object)(object)val.m_CurrentAttachedGO == (Object)null && flag && TakeWorld(val))
			{
				climbHolding[which] = true;
				Lab.Event("climb: " + ((which == 0) ? "left" : "right") + " hand took hold (" + magnitude.ToString("0.00") + " m from the player's spot)");
			}
			else if (Time.time - climbSaidAt[which] > 0.5f)
			{
				climbSaidAt[which] = Time.time;
				Lab.Event("climb: " + ((which == 0) ? "left" : "right") + " hand " + magnitude.ToString("0.00") + " m from the edge" + (flag ? ", near but the grab did not take" : "") + (((Object)(object)val.m_CurrentAttachedGO != (Object)null) ? ", holding something" : ""));
			}
		}
		else if (!want && climbHolding[which])
		{
			LetGoOf(worldGrip, val);
			climbHolding[which] = false;
		}
	}

	private bool Touching(Hand hand)
	{
		foreach (Collider item in Physics.OverlapSphere(((Component)hand).transform.position, Tuning.F("climb.touch", 0.1f), -1, (QueryTriggerInteraction)1))
		{
			if ((Object)(object)item != (Object)null && (Object)(object)item.attachedRigidbody == (Object)null && (Object)(object)((Component)item).GetComponentInParent<RigManager>() == (Object)null)
			{
				return true;
			}
		}
		return false;
	}

	private bool TakeWorld(Hand hand)
	{
		WorldGrip worldGrip = rig.worldGrip;
		if ((Object)(object)worldGrip == (Object)null)
		{
			return false;
		}
		try
		{
			hand.GrabLock = false;
			hand.HoverLock();
			SimpleTransform val = SimpleTransform.Create(((Component)hand).transform);
			((Grip)worldGrip).OnHandHoverBegin(hand, true);
			((HandReciever)worldGrip).OnHandHoverUpdate(hand);
			((HandReciever)worldGrip).ValidateGripScore(hand, val);
			hand._mHoveringReceiver = (HandReciever)(object)worldGrip;
			((Grip)worldGrip).OnGrabConfirm(hand, false);
			return (Object)(object)hand.m_CurrentAttachedGO != (Object)null;
		}
		catch (Exception ex)
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
		if (!((Object)(object)root == (Object)null))
		{
			Quaternion val = Quaternion.Euler(0f, bodyYaw, 0f);
			toBody = Quaternion.Inverse(val);
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
		int num;
		if ((Object)(object)rig.avatar != (Object)null)
		{
			Vector3 val = ((Component)rig.avatar).transform.localScale - take.avatarScale;
			num = ((((Vector3)(ref val)).sqrMagnitude < 0.0001f) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
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
				if ((Object)(object)value != (Object)null && ((Component)value).gameObject.activeInHierarchy && !HeldByPlayer(((Component)value).transform))
				{
					value.Despawn();
				}
			}
			catch (Exception)
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
			rig.Teleport(cloneTake.startFeet, true);
		}
		catch (Exception ex2)
		{
			Lab.Event("clone: could not start where you did: " + ex2.Message);
		}
		Lab.Event("clone: playing " + cloneTake.Length.ToString("0.0") + " s");
	}

	private void ReleaseCloneHands()
	{
		LetGoOfPlayer("the clone stopped");
		Hand[] array = (Hand[])(object)new Hand[2] { LeftHand, RightHand };
		for (int i = 0; i < 2; i++)
		{
			if ((Object)(object)cloneHeld[i] != (Object)null && (Object)(object)array[i] != (Object)null)
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
		List<Clone.Frame> frames = cloneTake.frames;
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
		List<Clone.Event> events = cloneTake.events;
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
			if (e.name != null && e.name.StartsWith("@wall", StringComparison.Ordinal))
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
			Hand val = ((e.hand == 0) ? LeftHand : RightHand);
			if ((Object)(object)cloneHeld[e.hand] != (Object)null && (Object)(object)val != (Object)null)
			{
				LetGoOf(cloneHeld[e.hand], val);
			}
			if ((Object)(object)val != (Object)null && (Object)(object)val.m_CurrentAttachedGO != (Object)null)
			{
				FreeHand(val);
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
		catch (Exception)
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
			HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
			foreach (Collider item in Physics.OverlapSphere(at, 1.5f, -1, (QueryTriggerInteraction)1))
			{
				Poolee val = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Poolee>() : null);
				if ((Object)(object)val != (Object)null)
				{
					hashSet.Add(((Il2CppObjectBase)(object)val).Pointer);
				}
			}
			if (cloneLesson != null && cloneLesson.purpose == "drive")
			{
				foreach (Seat item2 in Object.FindObjectsOfType<Seat>())
				{
					Poolee val2 = (((Object)(object)item2 != (Object)null) ? ((Component)item2).GetComponentInParent<Poolee>() : null);
					if ((Object)(object)val2 != (Object)null)
					{
						hashSet.Add(((Il2CppObjectBase)(object)val2).Pointer);
					}
				}
			}
			Clone.Suppress();
			HelperMethods.SpawnCrate(barcode, at, turn, Vector3.one, false, (Action<GameObject>)null, (Action<GameObject>)null);
			clonePending.Add((id, barcode, at, Time.time, hashSet));
		}
		catch (Exception ex)
		{
			Lab.Event("clone: spawn failed: " + ex.Message);
		}
	}

	private void FindCloneSpawns()
	{
		for (int num = clonePending.Count - 1; num >= 0; num--)
		{
			(int, string, Vector3, float, HashSet<IntPtr>) tuple = clonePending[num];
			if (!(Time.time - tuple.Item4 < 0.05f))
			{
				foreach (Collider item in Physics.OverlapSphere(tuple.Item3, 1.5f, -1, (QueryTriggerInteraction)1))
				{
					Poolee val = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Poolee>() : null);
					if (!((Object)(object)val == (Object)null) && !tuple.Item5.Contains(((Il2CppObjectBase)(object)val).Pointer) && !((Object)(object)val.SpawnableCrate == (Object)null) && !(((Scannable)val.SpawnableCrate).Barcode.ID != tuple.Item2) && !cloneSpawns.ContainsValue(val) && (cloneLesson == null || !(cloneLesson.purpose == "drive") || !Occupied(val)))
					{
						cloneSpawns[tuple.Item1] = val;
						if (lessonBigAsked.Contains(tuple.Item1))
						{
							lessonBig.Add(((Object)((Component)val).transform).GetInstanceID());
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
			foreach (Seat componentsInChild in ((Component)p).GetComponentsInChildren<Seat>(true))
			{
				if ((Object)(object)componentsInChild != (Object)null && (Object)(object)componentsInChild.rigManager != (Object)null)
				{
					return true;
				}
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	private static bool NotItsToTake(Poolee p)
	{
		try
		{
			foreach (Seat componentsInChild in ((Component)p).GetComponentsInChildren<Seat>(true))
			{
				if ((Object)(object)componentsInChild != (Object)null && (Object)(object)componentsInChild.rigManager != (Object)null)
				{
					return true;
				}
			}
			Transform head = Player.Head;
			int result;
			if ((Object)(object)head != (Object)null)
			{
				Vector3 val = Flat(((Component)p).transform.position - head.position);
				result = ((((Vector3)(ref val)).magnitude < (BotManager.OwnSpawns ? Tuning.F("drive.yours", 5f) : Tuning.F("drive.yours.map", 1.6f))) ? 1 : 0);
			}
			else
			{
				result = 0;
			}
			return (byte)result != 0;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static bool FromPouch(string barcode)
	{
		try
		{
			AssetWarehouse instance = AssetWarehouse.Instance;
			SpawnableCrate val = default(SpawnableCrate);
			if (instance != null && instance.TryGetCrate<SpawnableCrate>(new Barcode(barcode), ref val) && (Object)(object)val != (Object)null)
			{
				string text = (((Scannable)val).Title ?? "").ToLowerInvariant();
				return text.Contains("mag") || text.Contains("12g") || text.Contains("shell") || text.Contains("ammo") || text.Contains("clip");
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	private void CloneGrabWatch()
	{
		for (int num = grabWatch.Count - 1; num >= 0; num--)
		{
			(Hand, Rigidbody, float, float, float, string) value = grabWatch[num];
			Vector3 val;
			if ((Object)(object)value.Item1 != (Object)null && (Object)(object)value.Item1.rb != (Object)null)
			{
				float item = value.Item4;
				val = value.Item1.rb.velocity;
				value.Item4 = Mathf.Max(item, ((Vector3)(ref val)).magnitude);
			}
			if ((Object)(object)value.Item2 != (Object)null)
			{
				float item2 = value.Item5;
				val = value.Item2.angularVelocity;
				value.Item5 = Mathf.Max(item2, ((Vector3)(ref val)).magnitude * 57.29578f);
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
		Hand val = ((e.hand == 0) ? LeftHand : RightHand);
		if ((Object)(object)val == (Object)null)
		{
			return true;
		}
		if (e.name != null && e.name.StartsWith("@wall", StringComparison.Ordinal))
		{
			return true;
		}
		Transform val2 = null;
		int num = e.spawn;
		if (num == -1 && cloneStandIns.TryGetValue(e, out var value))
		{
			num = value;
		}
		Vector3 where = ((cloneLesson != null) ? ((Component)val).transform.position : e.at);
		Vector3 val5;
		if (num != -1)
		{
			if (!cloneSpawns.TryGetValue(num, out var value2) || (Object)(object)value2 == (Object)null)
			{
				grabWhy = "its own one (" + num + ") has not appeared";
				return false;
			}
			val2 = ((Component)value2).transform;
		}
		else if (cloneLesson == null || !((Object)(object)(val2 = LessonOwn(e, val)) != (Object)null))
		{
			float num2 = ((cloneLesson != null) ? 1.2f : 2f);
			bool flag = cloneLesson != null;
			foreach (Collider item in Physics.OverlapSphere(where, flag ? 0.001f : num2, (!flag) ? (-1) : 0, (QueryTriggerInteraction)1))
			{
				Poolee val3 = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Poolee>() : null);
				Transform val4 = (((Object)(object)val3 != (Object)null) ? ((Component)val3).transform : null);
				if ((Object)(object)val4 == (Object)null)
				{
					continue;
				}
				bool num3;
				if (e.barcode.Length <= 0)
				{
					num3 = ((Object)val4).name == Clone.ThingName(e);
				}
				else
				{
					if (!((Object)(object)val3.SpawnableCrate != (Object)null))
					{
						continue;
					}
					num3 = ((Scannable)val3.SpawnableCrate).Barcode.ID == e.barcode;
				}
				if (num3 && !HeldByPlayer(val4) && !((Object)(object)((Component)val4).GetComponentInChildren<RigManager>() != (Object)null) && (cloneLesson == null || (!HeldByAnyone(val4, rig) && !HeldByAnyone(val4, null) && !lessonThings.ContainsValue(val3))))
				{
					val5 = val4.position - where;
					float magnitude = ((Vector3)(ref val5)).magnitude;
					if (magnitude < num2)
					{
						num2 = magnitude;
						val2 = val4;
					}
				}
			}
			if ((Object)(object)val2 == (Object)null && e.barcode.Length > 0 && !Clone.IsBody(e.barcode, Clone.ThingName(e)))
			{
				string text = ((cloneLesson != null) ? LessonThingKey(e, val) : null);
				if (text != null && lessonStandIns.TryGetValue(text, out var value3))
				{
					cloneStandIns[e] = value3;
					return false;
				}
				int num4 = -1000 - cloneStandIns.Count;
				Quaternion faces = ((cloneLesson != null) ? ((Component)val).transform.rotation : Quaternion.identity);
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
			if ((Object)(object)val2 == (Object)null)
			{
				return true;
			}
		}
		try
		{
			Seat activeSeat = rig.activeSeat;
			Poolee val6 = (((Object)(object)activeSeat != (Object)null) ? ((Component)activeSeat).GetComponentInParent<Poolee>() : null);
			if ((Object)(object)val6 != (Object)null && e.barcode.Length > 0 && (Object)(object)val6.SpawnableCrate != (Object)null && ((Scannable)val6.SpawnableCrate).Barcode.ID == e.barcode && (Object)(object)val2 != (Object)(object)((Component)val6).transform)
			{
				Lab.Event("lesson: the " + Clone.ThingName(e) + " it sits in, not " + ((Object)val2).name);
				val2 = ((Component)val6).transform;
			}
		}
		catch (Exception)
		{
		}
		Grip[] array = Clone.GripsOf(val2);
		Grip val7 = null;
		string text2 = Clone.GripName(e);
		if (text2 != null)
		{
			Grip[] array2 = array;
			foreach (Grip val8 in array2)
			{
				if ((Object)(object)val8 != (Object)null && ((Object)val8).name == text2 && ((Object)(object)val7 == (Object)null || !val8.HasAttachedHands()))
				{
					val7 = val8;
				}
			}
		}
		if ((Object)(object)val7 == (Object)null && cloneLesson != null && e.t < 0.3f)
		{
			Gun componentInChildren = ((Component)val2).GetComponentInChildren<Gun>();
			if ((Object)(object)componentInChildren != (Object)null && (Object)(object)componentInChildren.triggerGrip != (Object)null)
			{
				val7 = componentInChildren.triggerGrip;
			}
		}
		if ((Object)(object)val7 == (Object)null)
		{
			val7 = ((e.grip >= 0 && e.grip < array.Length) ? array[e.grip] : null);
		}
		if ((Object)(object)val7 == (Object)null)
		{
			float num5 = float.MaxValue;
			Grip[] array2 = array;
			foreach (Grip val9 in array2)
			{
				val5 = ((Component)val9).transform.position - ((Component)val).transform.position;
				float magnitude2 = ((Vector3)(ref val5)).magnitude;
				if (magnitude2 < num5)
				{
					num5 = magnitude2;
					val7 = val9;
				}
			}
		}
		if ((Object)(object)val7 == (Object)null)
		{
			return true;
		}
		if ((Object)(object)val.m_CurrentAttachedGO != (Object)null)
		{
			FreeHand(val);
		}
		Magazine componentInChildren2 = ((Component)val2).GetComponentInChildren<Magazine>();
		if ((Object)(object)componentInChildren2 != (Object)null && componentInChildren2.magazineState != null)
		{
			try
			{
				componentInChildren2.magazineState.Refill();
			}
			catch (Exception)
			{
			}
		}
		if (val7.attachedHands != null && val7.attachedHands.Contains(val))
		{
			if ((Object)(object)cloneHeld[e.hand] != (Object)(object)val7)
			{
				Lab.Event("clone grab: " + ((e.hand == 0) ? "left" : "right") + " on " + Clone.ThingName(e) + "/" + ((Object)val7).name + ": the game's own grab, from your squeeze");
			}
			cloneHeld[e.hand] = val7;
			return true;
		}
		if (cloneLesson != null && lessonBig.Contains(((Object)val2).GetInstanceID()))
		{
			val5 = ((Component)val7).transform.position - ((Component)val).transform.position;
			if (((Vector3)(ref val5)).magnitude > Tuning.F("lesson.reach", 0.4f))
			{
				val5 = ((Component)val7).transform.position - ((Component)val).transform.position;
				grabWhy = "its hand is " + ((Vector3)(ref val5)).magnitude.ToString("0.0") + " m from the " + ((Object)val7).name;
				return false;
			}
		}
		Pose hand;
		bool mirrored;
		Pose pose = (GripBook.HandOn(val7, e.hand == 0, out hand, out mirrored) ? hand : HandOnGrip(val7, val));
		val5 = pose.position - ((Component)val).transform.position;
		float magnitude3 = ((Vector3)(ref val5)).magnitude;
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
		if (!flag2 && cloneLesson != null && (Object)(object)rig.activeSeat != (Object)null && Time.time - value4 >= Tuning.F("lesson.seated.wait", 0.3f) && magnitude3 <= Tuning.F("lesson.seated.reach", 0.6f))
		{
			Poolee componentInParent = ((Component)rig.activeSeat).GetComponentInParent<Poolee>();
			if ((Object)(object)componentInParent != (Object)null && ((Component)val7).transform.IsChildOf(((Component)componentInParent).transform))
			{
				flag2 = true;
				Lab.Event("lesson: takes the " + ((Object)val7).name + " of the car it sits in from " + magnitude3.ToString("0.00") + " m");
			}
		}
		if (!flag2)
		{
			grabWhy = "its hand is " + magnitude3.ToString("0.00") + " m from where it goes on the " + ((Object)val7).name;
			return false;
		}
		grabWaitFrom.Remove(e);
		try
		{
			BaseController val10 = ((e.hand == 0) ? ((ControllerRig)controls).leftController : ((ControllerRig)controls).rightController);
			Lab.Event("clone grab: " + ((e.hand == 0) ? "left" : "right") + " on " + ((Object)val2).name + "/" + ((Object)val7).name + ": hand " + (magnitude3 * 100f).ToString("0") + " cm off, " + Quaternion.Angle(((Component)val).transform.rotation, pose.rotation).ToString("0") + " deg turned, waited " + (Time.time - value4).ToString("0.00") + " s, fingers " + (((Object)(object)val10 != (Object)null) ? (val10._processedIndex.ToString("0.0") + "/" + val10._processedMiddle.ToString("0.0") + "/" + val10._processedThumb.ToString("0.0") + " grip " + val10._solvedGrip.ToString("0.00")) : "?") + ((!GripBook.HandOn(val7, e.hand == 0, out var _, out var mirrored2)) ? ", the game's pose" : (mirrored2 ? ", your pose mirrored" : ", your pose")));
			grabWatch.Add((val, ((Component)val2).GetComponentInParent<Rigidbody>(), Time.time, 0f, 0f, ((Object)val2).name + "/" + ((Object)val7).name));
		}
		catch (Exception)
		{
		}
		grabWhy = "the " + ((Object)val2).name + "'s " + ((Object)val7).name + " would not be taken" + (val7.HasAttachedHands() ? " (a hand is on it)" : "") + (((Object)(object)val.m_CurrentAttachedGO != (Object)null) ? " (its hand is full)" : "");
		if (Grab(val, val7))
		{
			cloneHeld[e.hand] = val7;
			if (cloneLesson != null)
			{
				Poolee componentInParent2 = ((Component)val2).GetComponentInParent<Poolee>();
				if ((Object)(object)componentInParent2 != (Object)null)
				{
					lessonThings[LessonThingKey(e, val)] = componentInParent2;
				}
			}
			if (cloneLesson != null)
			{
				Lab.Event("lesson: took the " + ((Object)val2).name + " by its " + ((Object)val7).name + " (" + ((e.hand == 0) ? "left" : "right") + " hand)");
			}
			return true;
		}
		return false;
	}

	private static bool HeldByAnyone(Transform thing, RigManager but)
	{
		foreach (Grip componentsInChild in ((Component)thing).GetComponentsInChildren<Grip>())
		{
			if ((Object)(object)componentsInChild == (Object)null || componentsInChild.attachedHands == null)
			{
				continue;
			}
			Enumerator<Hand> enumerator2 = componentsInChild.attachedHands.GetEnumerator();
			while (enumerator2.MoveNext())
			{
				Hand current2 = enumerator2.Current;
				if ((Object)(object)current2 != (Object)null && ((Object)(object)but == (Object)null || (Object)(object)((Component)current2).GetComponentInParent<RigManager>() != (Object)(object)but))
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
		Hand val = (((Object)(object)hand == (Object)(object)LeftHand) ? RightHand : LeftHand);
		GameObject val2 = (((Object)(object)val != (Object)null) ? val.m_CurrentAttachedGO : null);
		Poolee val3 = (((Object)(object)val2 != (Object)null) ? val2.GetComponentInParent<Poolee>() : null);
		if ((Object)(object)val3 != (Object)null && (Object)(object)val3.SpawnableCrate != (Object)null && ((Scannable)val3.SpawnableCrate).Barcode.ID == e.barcode)
		{
			Grip[] array = Clone.GripsOf(((Component)val3).transform);
			Grip val4 = ((e.grip >= 0 && e.grip < array.Length) ? array[e.grip] : null);
			string text = Clone.GripName(e);
			Grip val5 = val2.GetComponent<Grip>() ?? val2.GetComponentInParent<Grip>();
			if ((Object)(object)val5 != (Object)null && (((Object)(object)val4 != (Object)null && (Object)(object)val4 == (Object)(object)val5) || (text != null && ((Object)val5).name == text)))
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
		if (lessonThings.TryGetValue(text, out var value) && (Object)(object)value != (Object)null && ((Component)value).gameObject.activeInHierarchy)
		{
			return ((Component)value).transform;
		}
		if (text.EndsWith("#0", StringComparison.Ordinal) && Clone.ThingNumber(e) < 0)
		{
			Hand[] array = (Hand[])(object)new Hand[2] { LeftHand, RightHand };
			foreach (Hand val in array)
			{
				GameObject val2 = (((Object)(object)val != (Object)null) ? val.m_CurrentAttachedGO : null);
				Poolee val3 = (((Object)(object)val2 != (Object)null) ? val2.GetComponentInParent<Poolee>() : null);
				if ((Object)(object)val3 != (Object)null && (Object)(object)val3.SpawnableCrate != (Object)null && ((Scannable)val3.SpawnableCrate).Barcode.ID == e.barcode)
				{
					return ((Component)val3).transform;
				}
			}
		}
		return null;
	}

	private static bool HeldByPlayer(Transform thing)
	{
		Hand[] array = (Hand[])(object)new Hand[2]
		{
			Player.LeftHand,
			Player.RightHand
		};
		foreach (Hand val in array)
		{
			GameObject val2 = (((Object)(object)val != (Object)null) ? val.m_CurrentAttachedGO : null);
			if ((Object)(object)val2 != (Object)null && val2.transform.IsChildOf(thing))
			{
				return true;
			}
		}
		return false;
	}

	private void CloneFireCheck(int which)
	{
		Hand val = ((which == 0) ? LeftHand : RightHand);
		GameObject val2 = (((Object)(object)val != (Object)null) ? val.m_CurrentAttachedGO : null);
		Gun val3 = (((Object)(object)val2 != (Object)null) ? val2.GetComponentInParent<Gun>() : null);
		if ((Object)(object)val3 == (Object)null || Time.time - cloneFiredAt < 0.12f)
		{
			return;
		}
		try
		{
			if ((int)val3.cartridgeState != 1)
			{
				TryChargeGun(val3);
			}
			val3.Fire();
		}
		catch (Exception)
		{
		}
	}

	private static void TryChargeGun(Gun g)
	{
		try
		{
			if ((int)g.slideState == 4)
			{
				g.SlideRelease();
			}
			else
			{
				g.Charge();
			}
		}
		catch (Exception)
		{
		}
	}

	private bool FeetOnGround()
	{
		foreach (RaycastHit item in Physics.RaycastAll(((Rig)rig.physicsRig).centerOfPressure.position + Vector3.up * 0.3f, Vector3.down, 0.55f, -1, (QueryTriggerInteraction)1))
		{
			RaycastHit current = item;
			if ((Object)(object)((RaycastHit)(ref current)).collider != (Object)null && (Object)(object)((Component)((RaycastHit)(ref current)).collider).GetComponentInParent<RigManager>() == (Object)null)
			{
				return true;
			}
		}
		return false;
	}

	public void OnCloneGunFired(Gun fired)
	{
		Hand[] array = (Hand[])(object)new Hand[2] { LeftHand, RightHand };
		foreach (Hand val in array)
		{
			GameObject val2 = (((Object)(object)val != (Object)null) ? val.m_CurrentAttachedGO : null);
			if ((Object)(object)val2 != (Object)null && (Object)(object)fired != (Object)null && (Object)(object)val2.GetComponentInParent<Gun>() == (Object)(object)fired)
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
			Quaternion val = Quaternion.Slerp(a.playspace, b.playspace, k);
			Pose b2 = Pose.Lerp(a.head, b.head, k);
			Pose b3 = Pose.Lerp(a.left, b.left, k);
			Pose b4 = Pose.Lerp(a.right, b.right, k);
			if (cloneLesson != null)
			{
				cloneYaw += PortalTurn(vrRoot);
				if (LessonPortalTurns())
				{
					val = b.playspace;
				}
				val = Quaternion.Euler(0f, cloneYaw, 0f) * val;
				if (Mathf.Abs(cloneSize - 1f) > 0.02f)
				{
					Vector3 val2 = default(Vector3);
					((Vector3)(ref val2))._002Ector(b2.position.x, 0f, b2.position.z);
					b2.position = new Vector3(b2.position.x, b2.position.y * cloneSize, b2.position.z);
					b3.position = val2 + (b3.position - val2) * cloneSize;
					b4.position = val2 + (b4.position - val2) * cloneSize;
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
			vrRoot.rotation = val;
			NotePortalTurn(vrRoot);
			lessonRootYaw = vrRoot.eulerAngles.y;
			lessonRootSet = true;
			controls.headset.SetLocalPositionAndRotation(b2.position, b2.rotation);
			KeepHeadDown(vrRoot, controls.headset);
			((Component)((ControllerRig)controls).leftController).transform.SetLocalPositionAndRotation(b3.position, b3.rotation);
			((Component)((ControllerRig)controls).rightController).transform.SetLocalPositionAndRotation(b4.position, b4.rotation);
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
			PutInput(((ControllerRig)controls).leftController, handInput, b.leftIn, k, flag, gameGrabs);
			PutInput(((ControllerRig)controls).rightController, handInput2, b.rightIn, k, flag2, gameGrabs);
			PutButtons(((ControllerRig)controls).leftController, handInput, tapped, (Object)(object)cloneHeld[0] != (Object)null);
			PutButtons(((ControllerRig)controls).rightController, handInput2, tapped2, (Object)(object)cloneHeld[1] != (Object)null);
			((ControllerRig)controls)._primaryAButton = a.aPrimary;
			((ControllerRig)controls)._secondaryAButton = a.aSecondary;
			rig.remapHeptaRig._crouchTarget = Mathf.Lerp(a.crouch, b.crouch, k);
			rig.remapHeptaRig._feetOffset = Mathf.Lerp(a.feet, b.feet, k);
			if ((Object)(object)((ControllerRig)controls).leftController != (Object)null)
			{
				((ControllerRig)controls).leftController._thumbstickAxis = Vector2.zero;
			}
			((ControllerRig)controls)._axisPrimary = Vector2.zero;
			if (cloneLesson != null && (Object)(object)rig.activeSeat != (Object)null)
			{
				Vector2 val3 = Vector2.Lerp(a.leftIn.stick, b.leftIn.stick, k);
				Vector2 thumbstickAxis = Vector2.Lerp(a.rightIn.stick, b.rightIn.stick, k);
				if ((Object)(object)((ControllerRig)controls).leftController != (Object)null)
				{
					((ControllerRig)controls).leftController._thumbstickAxis = val3;
				}
				if ((Object)(object)((ControllerRig)controls).rightController != (Object)null)
				{
					((ControllerRig)controls).rightController._thumbstickAxis = thumbstickAxis;
				}
				((ControllerRig)controls)._axisPrimary = val3;
				((ControllerRig)controls)._axisPrimaryDirty = val3 != Vector2.zero;
			}
		}
	}

	private static void PutInput(BaseController c, Clone.HandInput a, Clone.HandInput b, float k, bool trigger, bool gameGrabs = false)
	{
		if (!((Object)(object)c == (Object)null))
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
		Rigidbody val = (((Object)(object)rig.physicsRig != (Object)null && (Object)(object)((Rig)rig.physicsRig).m_pelvis != (Object)null) ? ((Component)((Rig)rig.physicsRig).m_pelvis).GetComponent<Rigidbody>() : null);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		Vector3 val2 = Vector3.Lerp(a.pelvis, b.pelvis, k);
		Vector3 val3 = Vector3.Lerp(a.pelvisVelocity, b.pelvisVelocity, k);
		if (cloneLesson != null)
		{
			Vector3 val4;
			int num;
			if (lessonPelvisHas)
			{
				val4 = val.position - lessonPelvisWas;
				num = ((((Vector3)(ref val4)).magnitude > 1.5f) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool flag = (byte)num != 0;
			val4 = b.pelvis - a.pelvis;
			bool num2 = ((Vector3)(ref val4)).magnitude > 1.5f;
			if (num2)
			{
				val2 = b.pelvis;
				val3 = b.pelvisVelocity;
			}
			bool flag2 = num2 && lessonJumpAt != cloneFrame;
			if (flag2)
			{
				lessonJumpAt = cloneFrame;
			}
			if (flag || flag2)
			{
				LessonFromHere(val2, val.position);
				Lab.Event("lesson: " + (flag ? "carried off somewhere (a portal?)" : "you were carried off here") + ", going on from where it is");
			}
			lessonPelvisWas = val.position;
			lessonPelvisHas = true;
			Vector3 there = val2;
			val2 = LessonPlace(val2);
			val3 = Quaternion.Euler(0f, cloneYaw, 0f) * val3;
			val4 = val2 - val.position;
			if (((Vector3)(ref val4)).magnitude > Tuning.F("clone.snap", 2.5f))
			{
				LessonFromHere(there, val.position);
				return;
			}
		}
		if (lessonLanding && (Object)(object)((Rig)rig.physicsRig).centerOfPressure != (Object)null)
		{
			Vector3 position = ((Rig)rig.physicsRig).centerOfPressure.position;
			rig.Teleport(Vector3.MoveTowards(position, lessonLandAt, Tuning.F("climb.glide.step", 0.03f)), true);
			lessonPelvisHas = false;
			CarriedFollow();
			return;
		}
		if (cloneLesson != null && (Object)(object)rig.activeSeat != (Object)null)
		{
			lessonPelvisHas = false;
			return;
		}
		if (cloneLesson != null && lessonPurposeful && cloneLesson.purpose == "climb" && (Object)(object)((Rig)rig.physicsRig).centerOfPressure != (Object)null)
		{
			float num3 = val2.y - val.position.y;
			if (Time.time - cloneStart >= cloneLesson.riseAt - 0.1f && (num3 > 0.12f || (lessonGliding && num3 > -0.25f && !FeetOnGround())))
			{
				if (!lessonGliding)
				{
					CarryThings(on: true);
				}
				lessonGliding = true;
				glidedAt = Time.time;
				Vector3 position2 = ((Rig)rig.physicsRig).centerOfPressure.position;
				Vector3 val5 = position2 + (val2 - val.position);
				rig.Teleport(Vector3.MoveTowards(position2, val5, Tuning.F("climb.glide.step", 0.03f)), true);
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
		Vector3 val6 = val2 - val.position;
		if (((Vector3)(ref val6)).magnitude > Tuning.F("clone.snap", 2.5f))
		{
			try
			{
				Vector3 val7 = ((Rig)rig.physicsRig).centerOfPressure.position + val6;
				rig.Teleport(val7, false);
				return;
			}
			catch (Exception)
			{
				return;
			}
		}
		float num4 = Tuning.F("clone.pull", 14f);
		Vector3 val8 = val3 + new Vector3(val6.x, 0f, val6.z) * num4 - val.velocity;
		if (Mathf.Abs(val3.y) < Tuning.F("clone.vertical", 1.2f))
		{
			val8.y = 0f;
		}
		val.AddForce(Vector3.ClampMagnitude(val8 * Tuning.F("clone.push", 18f), 120f), (ForceMode)5);
		if (!rig.physicsRig.ballLocoEnabled)
		{
			Quaternion val9 = LessonTurn(Quaternion.Slerp(a.pelvisTurn, b.pelvisTurn, k)) * Quaternion.Inverse(val.rotation);
			float num5 = default(float);
			Vector3 val10 = default(Vector3);
			((Quaternion)(ref val9)).ToAngleAxis(ref num5, ref val10);
			if (num5 > 180f)
			{
				num5 -= 360f;
			}
			Vector3 val11 = Vector3.Lerp(a.pelvisSpin, b.pelvisSpin, k) + val10 * (num5 * ((float)Math.PI / 180f) * num4);
			if (!float.IsNaN(val11.x))
			{
				val.AddTorque(Vector3.ClampMagnitude((val11 - val.angularVelocity) * Tuning.F("clone.push", 18f), 200f), (ForceMode)5);
			}
		}
	}

	private void HurtPlayer()
	{
		Transform head = Player.Head;
		Health val = (((Object)(object)Player.RigManager != (Object)null) ? Player.RigManager.health : null);
		if ((Object)(object)head == (Object)null || (Object)(object)val == (Object)null)
		{
			return;
		}
		Hand[] array = (Hand[])(object)new Hand[2] { LeftHand, RightHand };
		for (int i = 0; i < 2; i++)
		{
			Hand val2 = array[i];
			if ((Object)(object)val2 == (Object)null || (Object)(object)val2.rb == (Object)null || Time.time - punchedAt[i] < 0.5f)
			{
				continue;
			}
			Vector3 position = ((Component)val2).transform.position;
			Vector3 val3 = position - head.position;
			float magnitude = ((Vector3)(ref val3)).magnitude;
			val3 = position - (head.position - Vector3.up * 0.35f);
			float magnitude2 = ((Vector3)(ref val3)).magnitude;
			val3 = val2.rb.velocity;
			float magnitude3 = ((Vector3)(ref val3)).magnitude;
			if ((magnitude < 0.2f || magnitude2 < 0.28f) && magnitude3 > Tuning.F("hurt.punch.speed", 2.5f))
			{
				punchedAt[i] = Time.time;
				float num = val.max_Health * Tuning.F("hurt.punch", 0.06f) * Mathf.Clamp(magnitude3 / 3f, 0.5f, 2.5f);
				try
				{
					val.TAKEDAMAGE(num);
				}
				catch (Exception)
				{
				}
				Lab.Event("clone: punched you (" + magnitude3.ToString("0.0") + " m/s, " + num.ToString("0.00") + ")");
			}
		}
	}

	private void GrabPlayerUpdate(Clone.Frame a, Clone.Frame b, float k)
	{
		Hand[] array = (Hand[])(object)new Hand[2] { LeftHand, RightHand };
		float[] array2 = new float[2]
		{
			Mathf.Lerp(a.leftIn.grip, b.leftIn.grip, k),
			Mathf.Lerp(a.rightIn.grip, b.rightIn.grip, k)
		};
		for (int i = 0; i < 2; i++)
		{
			float num = cloneGrip[i];
			cloneGrip[i] = array2[i];
			Hand val = array[i];
			if ((Object)(object)val == (Object)null || (Object)(object)val.rb == (Object)null)
			{
				continue;
			}
			if (holdingPlayer == i)
			{
				if (array2[i] < Tuning.F("grab.open", 0.35f))
				{
					ThrowPlayer();
				}
				else if (Time.time - playerHeldSince > Tuning.F("grab.longest", 12f) || !PlayerStillNear(val))
				{
					LetGoOfPlayer("slipped free");
				}
			}
			else
			{
				float num2 = Tuning.F("grab.shut", 0.6f);
				if (holdingPlayer < 0 && array2[i] >= num2 && num < num2 && (Object)(object)val.m_CurrentAttachedGO == (Object)null && PlayerWithin(val))
				{
					TakePlayer(i, val);
				}
			}
		}
	}

	private static bool PlayerWithin(Hand hand)
	{
		RigManager rigManager = Player.RigManager;
		if ((Object)(object)rigManager == (Object)null)
		{
			return false;
		}
		float num = Tuning.F("grab.reach", 0.25f);
		foreach (Collider item in Physics.OverlapSphere(((Component)hand).transform.position, num, -1, (QueryTriggerInteraction)1))
		{
			if ((Object)(object)item != (Object)null && (Object)(object)((Component)item).GetComponentInParent<RigManager>() == (Object)(object)rigManager)
			{
				return true;
			}
		}
		return false;
	}

	private static Rigidbody PlayerPelvis()
	{
		PhysicsRig physicsRig = Player.PhysicsRig;
		if (!((Object)(object)physicsRig != (Object)null) || !((Object)(object)((Rig)physicsRig).m_pelvis != (Object)null))
		{
			return null;
		}
		return ((Component)((Rig)physicsRig).m_pelvis).GetComponent<Rigidbody>();
	}

	private bool PlayerStillNear(Hand hand)
	{
		Rigidbody val = PlayerPelvis();
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		Vector3 val2 = ((Component)hand).transform.position + ((Component)hand).transform.rotation * heldOffset;
		Vector3 val3 = val.position - val2;
		return ((Vector3)(ref val3)).magnitude < Tuning.F("grab.slip", 1.5f);
	}

	private void TakePlayer(int which, Hand hand)
	{
		Rigidbody val = PlayerPelvis();
		if ((Object)(object)val == (Object)null || (Object)(object)Player.PhysicsRig == (Object)null)
		{
			return;
		}
		holdingPlayer = which;
		playerHeldSince = Time.time;
		heldOffset = Vector3.ClampMagnitude(Quaternion.Inverse(((Component)hand).transform.rotation) * (val.position - ((Component)hand).transform.position), Tuning.F("grab.offset", 0.6f));
		heldBodies.Clear();
		foreach (Rigidbody componentsInChild in ((Component)Player.PhysicsRig).GetComponentsInChildren<Rigidbody>())
		{
			if ((Object)(object)componentsInChild != (Object)null && !componentsInChild.isKinematic)
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
		Hand val = ((holdingPlayer == 0) ? LeftHand : RightHand);
		Rigidbody val2 = PlayerPelvis();
		if ((Object)(object)val == (Object)null || (Object)(object)val.rb == (Object)null || (Object)(object)val2 == (Object)null)
		{
			LetGoOfPlayer("lost hold");
			return;
		}
		Vector3 val3 = ((Component)val).transform.position + ((Component)val).transform.rotation * heldOffset;
		Vector3 val4 = Vector3.ClampMagnitude(val.rb.velocity + (val3 - val2.position) * Tuning.F("grab.pull", 12f) - val2.velocity, Tuning.F("grab.step", 2.5f));
		foreach (Rigidbody heldBody in heldBodies)
		{
			if ((Object)(object)heldBody != (Object)null)
			{
				heldBody.AddForce(val4, (ForceMode)2);
			}
		}
	}

	private void ThrowPlayer()
	{
		Hand val = ((holdingPlayer == 0) ? LeftHand : RightHand);
		Vector3 val2 = (((Object)(object)val != (Object)null && (Object)(object)val.rb != (Object)null) ? (val.rb.velocity * Tuning.F("grab.throw", 1.4f)) : Vector3.zero);
		Rigidbody val3 = PlayerPelvis();
		if ((Object)(object)val3 != (Object)null)
		{
			Vector3 val4 = val2 - val3.velocity;
			foreach (Rigidbody heldBody in heldBodies)
			{
				if ((Object)(object)heldBody != (Object)null)
				{
					heldBody.AddForce(val4, (ForceMode)2);
				}
			}
		}
		LetGoOfPlayer("threw you at " + ((Vector3)(ref val2)).magnitude.ToString("0.0") + " m/s");
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
					if ((Object)(object)item.a != (Object)null && (Object)(object)item.b != (Object)null)
					{
						Physics.IgnoreCollision(item.a, item.b, false);
					}
				}
				coverNoTouch.Clear();
			}
			else
			{
				if ((Object)(object)coverThing == (Object)null || coverNoTouch.Count > 0)
				{
					return;
				}
				MarrowEntity componentInParent = ((Component)coverThing).GetComponentInParent<MarrowEntity>();
				Transform obj = (((Object)(object)componentInParent != (Object)null) ? ((Component)componentInParent).transform : ((Component)coverThing).transform);
				List<Collider> list = new List<Collider>(((Component)rig.physicsRig).GetComponentsInChildren<Collider>(false));
				if ((Object)(object)this.item != (Object)null)
				{
					MarrowEntity componentInParent2 = ((Component)this.item).GetComponentInParent<MarrowEntity>();
					list.AddRange(((Component)(((Object)(object)componentInParent2 != (Object)null) ? ((Component)componentInParent2).transform : ((Component)this.item).transform)).GetComponentsInChildren<Collider>(false));
				}
				{
					foreach (Collider componentsInChild in ((Component)obj).GetComponentsInChildren<Collider>(false))
					{
						if ((Object)(object)componentsInChild == (Object)null || componentsInChild.isTrigger)
						{
							continue;
						}
						foreach (Collider item2 in list)
						{
							if (!((Object)(object)item2 == (Object)null) && !item2.isTrigger)
							{
								Physics.IgnoreCollision(componentsInChild, item2, true);
								coverNoTouch.Add((componentsInChild, item2));
							}
						}
					}
					return;
				}
			}
		}
		catch (Exception)
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
		if (Tuning.F("cover.on", 1f) < 0.5f || (Object)(object)head == (Object)null)
		{
			LeaveCover(0f, "off");
			return false;
		}
		Vector3 val = Feet;
		Vector3 val2 = Flat(point - head.position);
		if (((Vector3)(ref val2)).magnitude < Tuning.F("cover.close", 5f))
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
			List<Vector3> threats = Threats(point);
			if (!FindSpot(val, threats, out var best, out var bestLow, out var bestPeek, out var bestBehind, out var bestThing))
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
			string[] obj = new string[6]
			{
				"cover: making for ",
				bestLow ? "low" : "tall",
				" cover ",
				null,
				null,
				null
			};
			val2 = Flat(best - val);
			obj[3] = ((Vector3)(ref val2)).magnitude.ToString("0.0");
			obj[4] = " m off, behind ";
			obj[5] = bestBehind;
			Lab.Event(string.Concat(obj));
		}
		Vector3 val3 = Flat(coverAt - val);
		switch (coverPhase)
		{
		case CoverPhase.Going:
		{
			if (((Vector3)(ref val3)).magnitude < 0.45f)
			{
				coverPhase = CoverPhase.Hidden;
				coverPhaseUntil = Time.time + HideFor();
				coverLeaveAt = Time.time + Random.Range(Tuning.F("cover.stay.least", 10f), Tuning.F("cover.stay.most", 22f));
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
			float magnitude = ((Vector3)(ref val3)).magnitude;
			if (magnitude < 2.5f)
			{
				CoverNoPush(on: true);
			}
			move = Local(((Vector3)(ref val3)).normalized) * ((magnitude > 2.5f) ? MotionLibrary.RunPush : (MotionLibrary.WalkPush * Mathf.Lerp(0.45f, 1f, magnitude / 2.5f)));
			running = magnitude > 2.5f;
			holdFire = false;
			CrouchTo(down: false);
			return true;
		}
		case CoverPhase.Hidden:
			if (!Reloading && (Object)(object)gun != (Object)null && AmmoFraction() < Tuning.F("cover.reload.below", 0.5f))
			{
				StartReload();
			}
			if (Time.time >= coverPhaseUntil && !Reloading)
			{
				if (Time.time >= coverLeaveAt)
				{
					LeaveCover(Random.Range(2f, 5f), "time to move on");
					return false;
				}
				coverPhase = CoverPhase.Peeking;
				coverPhaseUntil = Time.time + Random.Range(Tuning.F("cover.peek.least", 1.3f), Tuning.F("cover.peek.most", 2.8f));
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
		Vector3 val4 = Flat(((flag2 && !coverLow) ? peekAt : coverAt) - val);
		move = ((((Vector3)(ref val4)).magnitude > 0.15f) ? (Local(((Vector3)(ref val4)).normalized) * Mathf.Lerp(MotionLibrary.WalkPush * 0.4f, MotionLibrary.WalkPush, Mathf.Clamp01(((Vector3)(ref val4)).magnitude / 1.2f))) : Vector2.zero);
		CoverNoPush(on: true);
		running = false;
		CrouchTo(coverLow && !flag2);
		holdFire = !flag2 || ((Vector3)(ref val4)).magnitude > 0.35f;
		return true;
	}

	private bool CoverWhileReloading(float dt)
	{
		if (coverPhase == CoverPhase.None || Tuning.F("cover.on", 1f) < 0.5f)
		{
			return false;
		}
		coverStepAt = Time.time;
		Vector3 val = Flat(coverAt - Feet);
		if (coverPhase == CoverPhase.Going && ((Vector3)(ref val)).magnitude < 0.45f)
		{
			coverPhase = CoverPhase.Hidden;
			coverLeaveAt = Time.time + Random.Range(Tuning.F("cover.stay.least", 10f), Tuning.F("cover.stay.most", 22f));
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
		walk = ((((Vector3)(ref val)).magnitude > 0.15f) ? (Local(((Vector3)(ref val)).normalized) * (flag ? MotionLibrary.WalkPush : (MotionLibrary.WalkPush * 0.7f))) : Vector2.zero);
		running = false;
		CrouchTo(coverLow && !flag);
		Ducking(dt);
		return true;
	}

	private float HideFor()
	{
		return Random.Range(Tuning.F("cover.hide.least", 0.8f), Tuning.F("cover.hide.most", 2.2f)) * Mathf.Lerp(0.7f, 1.3f, personality.calm / 1.8f);
	}

	private void CrouchTo(bool down)
	{
		crouchPlannedAt = Time.time;
		if (down && !crouchPlan)
		{
			duckDepth = 0f - Random.Range(0.6f, 0.85f);
		}
		crouchPlan = down;
		duckUntil = (down ? (Time.time + 0.5f) : 0f);
	}

	private Vector2 Local(Vector3 world)
	{
		Vector3 val = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * world;
		return new Vector2(val.x, val.z);
	}

	private List<Vector3> Threats(Vector3 point)
	{
		List<Vector3> list = new List<Vector3> { point + Vector3.up * 0.25f };
		foreach (Vector3 seenEnemy in seenEnemies)
		{
			if (list.Count >= 4)
			{
				break;
			}
			Vector3 val = seenEnemy - point;
			if (((Vector3)(ref val)).magnitude > 1.5f)
			{
				list.Add(seenEnemy + Vector3.up * 0.25f);
			}
		}
		return list;
	}

	private bool Covered(Vector3 eye, Vector3 threat)
	{
		RaycastHit val = default(RaycastHit);
		if (!Physics.Linecast(eye, threat, ref val, -1, (QueryTriggerInteraction)1))
		{
			return false;
		}
		Vector3 val2 = ((RaycastHit)(ref val)).point - eye;
		if (((Vector3)(ref val2)).magnitude > Tuning.F("cover.near", 2.5f))
		{
			return false;
		}
		string name;
		return SolidCover(((RaycastHit)(ref val)).collider, out name);
	}

	private bool SolidCover(Collider c, out string name)
	{
		name = (((Object)(object)c != (Object)null) ? ((Object)c).name : "");
		if ((Object)(object)c == (Object)null)
		{
			return false;
		}
		Rigidbody attachedRigidbody = c.attachedRigidbody;
		if ((Object)(object)attachedRigidbody == (Object)null || attachedRigidbody.isKinematic)
		{
			return (Object)(object)((Component)c).GetComponentInParent<RigManager>() == (Object)null;
		}
		if ((Object)(object)((Component)attachedRigidbody).GetComponentInParent<RigManager>() != (Object)null || HeldByAnyone(((Component)attachedRigidbody).transform, null))
		{
			return false;
		}
		MarrowEntity componentInParent = ((Component)attachedRigidbody).GetComponentInParent<MarrowEntity>();
		if ((Object)(object)componentInParent != (Object)null)
		{
			name = ((Object)componentInParent).name;
		}
		Bounds bounds = c.bounds;
		Vector3 size = ((Bounds)(ref bounds)).size;
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

	private bool FindSpot(Vector3 feet, List<Vector3> threats, out Vector3 best, out bool bestLow, out Vector3 bestPeek, out string bestBehind, out Rigidbody bestThing)
	{
		bestThing = null;
		best = feet;
		bestLow = false;
		bestPeek = feet;
		bestBehind = "";
		Vector3 val = threats[0];
		Vector3 val2 = Flat(feet - val);
		val2 = ((((Vector3)(ref val2)).sqrMagnitude > 0.01f) ? ((Vector3)(ref val2)).normalized : (Quaternion.Euler(0f, bodyYaw + 180f, 0f) * Vector3.forward));
		float num = Tuning.F("cover.reach", 13f);
		List<Vector3> list = new List<Vector3>();
		List<(Collider, float)> list2 = new List<(Collider, float)>();
		Vector3 val3;
		foreach (Collider item2 in Physics.OverlapSphere(feet + Vector3.up, num, -1, (QueryTriggerInteraction)1))
		{
			if (!((Object)(object)item2 == (Object)null))
			{
				Bounds bounds = item2.bounds;
				if (!(((Bounds)(ref bounds)).size.y < 0.6f) && !(Mathf.Max(((Bounds)(ref bounds)).size.x, ((Bounds)(ref bounds)).size.z) < 0.45f) && !(((Bounds)(ref bounds)).min.y > feet.y + 1f) && SolidCover(item2, out var _))
				{
					val3 = Flat(item2.ClosestPoint(feet) - feet);
					list2.Add((item2, ((Vector3)(ref val3)).magnitude));
				}
			}
		}
		list2.Sort(((Collider c, float d) a, (Collider c, float d) b) => a.d.CompareTo(b.d));
		for (int i = 0; i < list2.Count && i < 24; i++)
		{
			Collider item = list2[i].Item1;
			Bounds bounds2 = item.bounds;
			Vector3 val4 = Flat(((Bounds)(ref bounds2)).center - val);
			val4 = ((((Vector3)(ref val4)).sqrMagnitude > 0.01f) ? ((Vector3)(ref val4)).normalized : val2);
			float num2 = Mathf.Abs(((Bounds)(ref bounds2)).extents.x * val4.x) + Mathf.Abs(((Bounds)(ref bounds2)).extents.z * val4.z);
			float num3 = (((Object)(object)item.attachedRigidbody != (Object)null && !item.attachedRigidbody.isKinematic) ? Tuning.F("cover.room.loose", 0.8f) : 0.55f);
			if (num2 < 4f)
			{
				list.Add(new Vector3(((Bounds)(ref bounds2)).center.x, feet.y, ((Bounds)(ref bounds2)).center.z) + val4 * (num2 + num3));
			}
			Vector3 val5 = item.ClosestPoint(feet + Vector3.up * 0.8f);
			list.Add(new Vector3(val5.x, feet.y, val5.z) + val4 * num3);
		}
		for (int j = 0; j < 16; j++)
		{
			list.Add(feet + Quaternion.Euler(0f, (float)j * 22.5f + Random.Range(-8f, 8f), 0f) * Vector3.forward * Random.Range(2f, num));
		}
		float size = Size;
		float num4 = float.MaxValue;
		RaycastHit val10 = default(RaycastHit);
		RaycastHit val14 = default(RaycastHit);
		foreach (Vector3 item3 in list)
		{
			if (!StandAt(item3, feet, out var val6))
			{
				continue;
			}
			val3 = Flat(val6 - feet);
			float magnitude = ((Vector3)(ref val3)).magnitude;
			if (magnitude > num)
			{
				continue;
			}
			val3 = Flat(val - val6);
			float magnitude2 = ((Vector3)(ref val3)).magnitude;
			if (magnitude2 < Tuning.F("cover.close", 5f) + 1f)
			{
				continue;
			}
			Vector3 val7 = val6 + Vector3.up * CrouchedEyes;
			Vector3 val8 = val6 + Vector3.up * (standingHeight - 0.1f * size);
			bool flag = true;
			bool flag2 = true;
			string name2 = "";
			Rigidbody val9 = null;
			foreach (Vector3 threat in threats)
			{
				if (!Covered(val7, threat))
				{
					flag = false;
					break;
				}
				if (flag2 && !Covered(val8, threat))
				{
					flag2 = false;
				}
			}
			if (!flag)
			{
				continue;
			}
			if (Physics.Linecast(val7, val, ref val10, -1, (QueryTriggerInteraction)1))
			{
				SolidCover(((RaycastHit)(ref val10)).collider, out name2);
				Rigidbody attachedRigidbody = ((RaycastHit)(ref val10)).collider.attachedRigidbody;
				val9 = (((Object)(object)attachedRigidbody != (Object)null && !attachedRigidbody.isKinematic) ? attachedRigidbody : null);
			}
			bool flag3 = !flag2;
			Vector3 val11 = val6;
			bool flag4;
			if (flag3)
			{
				flag4 = Seen(val8 + Vector3.up * 0.1f * size, val);
			}
			else
			{
				flag4 = false;
				Vector3 up = Vector3.up;
				val3 = Flat(val - val6);
				Vector3 val12 = Vector3.Cross(up, ((Vector3)(ref val3)).normalized);
				float[] array = new float[4] { 0.9f, -0.9f, 1.3f, -1.3f };
				foreach (float num5 in array)
				{
					if (StandAt(val6 + val12 * num5 * size, val6, out var val13) && Seen(val13 + Vector3.up * (standingHeight - 0.1f * size), val))
					{
						val11 = val13;
						flag4 = true;
						break;
					}
				}
			}
			if (Physics.Linecast(feet + Vector3.up * 0.5f, val6 + Vector3.up * 0.5f, ref val14, -1, (QueryTriggerInteraction)1) && (Object)(object)((Component)((RaycastHit)(ref val14)).collider).GetComponentInParent<RigManager>() == (Object)null)
			{
				continue;
			}
			float num6 = magnitude + (flag4 ? 0f : Tuning.F("cover.noshot.cost", 6f)) + (flag3 ? 0f : 0.5f) + Mathf.Max(0f, 9f - magnitude2) * 1.5f;
			foreach (KeyValuePair<Bot, Vector3> item4 in coverTaken)
			{
				if (item4.Key != this && item4.Key.Alive)
				{
					val3 = Flat(item4.Value - val6);
					if (((Vector3)(ref val3)).magnitude < 1.4f)
					{
						num6 += 8f;
					}
				}
			}
			if (num6 < num4)
			{
				num4 = num6;
				best = val6;
				bestLow = flag3;
				bestPeek = val11;
				bestBehind = (string.IsNullOrEmpty(name2) ? "something" : name2);
				bestThing = val9;
			}
		}
		return num4 < float.MaxValue;
	}

	private static bool StandAt(Vector3 near, Vector3 from, out Vector3 feet)
	{
		feet = near;
		RaycastHit val = default(RaycastHit);
		if (!Physics.Raycast(near + Vector3.up * 1.5f, Vector3.down, ref val, 3.5f, -1, (QueryTriggerInteraction)1) || ((RaycastHit)(ref val)).normal.y < 0.75f)
		{
			return false;
		}
		if ((Object)(object)((Component)((RaycastHit)(ref val)).collider).GetComponentInParent<RigManager>() != (Object)null || Mathf.Abs(((RaycastHit)(ref val)).point.y - from.y) > 1.2f)
		{
			return false;
		}
		feet = ((RaycastHit)(ref val)).point;
		return !Physics.CheckCapsule(feet + Vector3.up * 0.45f, feet + Vector3.up * 1.5f, 0.24f, -1, (QueryTriggerInteraction)1);
	}

	private static bool Seen(Vector3 eye, Vector3 point)
	{
		RaycastHit val = default(RaycastHit);
		if (!Physics.Linecast(eye, point, ref val, -1, (QueryTriggerInteraction)1))
		{
			return true;
		}
		if (!((Object)(object)((Component)((RaycastHit)(ref val)).collider).GetComponentInParent<RigManager>() != (Object)null))
		{
			Vector3 val2 = ((RaycastHit)(ref val)).point - point;
			return ((Vector3)(ref val2)).magnitude < 0.6f;
		}
		return true;
	}

	public static void NoteSpawn(Poolee poolee)
	{
		if (!((Object)(object)poolee == (Object)null) && !(Time.time < Clone.SuppressUntil))
		{
			recentSpawns.Add((((Component)poolee).transform.position, Time.time));
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
		Poolee componentInParent = ((Component)rb).GetComponentInParent<Poolee>();
		if ((Object)(object)componentInParent == (Object)null || (Object)(object)componentInParent.SpawnableCrate == (Object)null)
		{
			return false;
		}
		code = ((Scannable)componentInParent.SpawnableCrate).Barcode.ID;
		name = Lessons.NameOf(componentInParent);
		string text = ((Object)((Component)componentInParent).gameObject).name.ToUpperInvariant();
		if (Clone.IsBody(code, ((Object)((Component)componentInParent).gameObject).name) || text.Contains("SPAWN") || text.Contains("NIMBUS"))
		{
			return false;
		}
		if ((Object)(object)((Component)componentInParent).GetComponentInChildren<Gun>() != (Object)null || (Object)(object)((Component)componentInParent).GetComponentInChildren<Magazine>() != (Object)null || (Object)(object)((Component)componentInParent).GetComponentInChildren<AIBrain>() != (Object)null)
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
			List<string> list = new List<string>();
			List<float> list2 = new List<float>();
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
			float num3 = Random.value * num;
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
			if (instance == null || (Object)(object)head == (Object)null || !instance.HasCrate(new Barcode(text2)))
			{
				return false;
			}
			Vector3 val = head.position + Quaternion.Euler(0f, bodyYaw, 0f) * new Vector3(0f, -0.5f, 1f);
			Clone.Suppress();
			HelperMethods.SpawnCrate(text2, val, Quaternion.identity, Vector3.one, false, (Action<GameObject>)null, (Action<GameObject>)null);
			pendingCode = text2;
			pendingAt = val;
			pendingUntil = Time.time + 4f;
			Lab.Event("curious: spawned a " + text2 + " to try");
			return true;
		}
		catch (Exception ex)
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
			Crate val = default(Crate);
			if (instance == null || !instance.TryGetCrate(new Barcode(code), ref val) || (Object)(object)val == (Object)null)
			{
				return false;
			}
			return !((Scannable)val).Redacted && code.IndexOf("FusionContent", StringComparison.OrdinalIgnoreCase) < 0;
		}
		catch (Exception)
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
			Lessons.Litter(((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Poolee>() : null);
			pendingCode = null;
			return true;
		}
		if (Time.time > pendingUntil)
		{
			Lab.Event("curious: the " + pendingCode + " it spawned is not something it can pick up and try: cleared away, and not spawned again");
			SelfTaught.Never(pendingCode);
			try
			{
				HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
				foreach (Collider item in Physics.OverlapSphere(pendingAt, 2.5f, -1, (QueryTriggerInteraction)2))
				{
					Poolee val = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Poolee>() : null);
					if ((Object)(object)val != (Object)null && hashSet.Add(((Il2CppObjectBase)(object)val).Pointer) && (Object)(object)val.SpawnableCrate != (Object)null && ((Scannable)val.SpawnableCrate).Barcode.ID == pendingCode)
					{
						val.Despawn();
					}
				}
			}
			catch (Exception)
			{
			}
			pendingCode = null;
			return false;
		}
		return true;
	}

	private bool FindTinker(Vector3 feet, string wanted, float within)
	{
		Rigidbody val = null;
		Grip val2 = null;
		string barcode = "";
		string text = "";
		float num = float.MaxValue;
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(feet + Vector3.up * 0.5f, within, -1, (QueryTriggerInteraction)1))
		{
			Rigidbody val3 = (((Object)(object)item != (Object)null) ? item.attachedRigidbody : null);
			if ((Object)(object)val3 == (Object)null || val3.isKinematic || !hashSet.Add(((Il2CppObjectBase)(object)val3).Pointer) || val3.mass < 0.05f || val3.mass > 15f || (Object)(object)((Component)val3).GetComponentInParent<RigManager>() != (Object)null || !Tryable(val3, out var code, out var name) || ((wanted != null) ? (code != wanted) : SelfTaught.Dull(code)) || NotForBots(val3))
			{
				continue;
			}
			Grip val4 = GripOf(val3);
			if ((Object)(object)val4 == (Object)null)
			{
				continue;
			}
			float num2 = val3.worldCenterOfMass.y - feet.y;
			if (!(num2 < -0.5f) && !(num2 > 1.8f))
			{
				Vector3 val5 = val3.worldCenterOfMass - feet;
				float num3 = ((Vector3)(ref val5)).magnitude + SelfTaught.Known(code) * 8f + Random.value * 2f;
				if (num3 < num)
				{
					num = num3;
					val = val3;
					val2 = val4;
					barcode = code;
					text = name;
				}
			}
		}
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		this.item = val;
		grip = val2;
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
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		Transform val = (((Object)(object)item != (Object)null) ? ((Component)item).transform.root : null);
		foreach (Collider item in Physics.OverlapSphere(head.position, 12f, -1, (QueryTriggerInteraction)1))
		{
			Rigidbody val2 = (((Object)(object)item != (Object)null) ? item.attachedRigidbody : null);
			if ((Object)(object)val2 == (Object)null || val2.isKinematic || !hashSet.Add(((Il2CppObjectBase)(object)val2).Pointer) || (Object)(object)((Component)val2).transform.root == (Object)(object)val || (Object)(object)((Component)val2).GetComponentInParent<RigManager>() != (Object)null)
			{
				continue;
			}
			Vector3 velocity = val2.velocity;
			if (!(((Vector3)(ref velocity)).magnitude > 0.3f))
			{
				trialBodies.Add((val2, val2.worldCenterOfMass));
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
		if ((Object)(object)item == (Object)null)
		{
			return 0;
		}
		Transform root = ((Component)item).transform.root;
		foreach (AudioSource componentsInChild in ((Component)root).GetComponentsInChildren<AudioSource>())
		{
			sound |= (Object)(object)componentsInChild != (Object)null && componentsInChild.isPlaying;
		}
		return ((Component)root).GetComponentsInChildren<Transform>(false).Length;
	}

	private float TrialAfter(out string what)
	{
		List<string> list = new List<string>();
		float num = 0f;
		Transform head = Head;
		int num2 = 0;
		int num3 = 0;
		Vector3 val = (((Object)(object)this.item != (Object)null) ? this.item.worldCenterOfMass : head.position);
		Vector3 val2;
		foreach (var recentSpawn in recentSpawns)
		{
			Vector3 item = recentSpawn.at;
			float item2 = recentSpawn.t;
			val2 = item - val;
			if (!(((Vector3)(ref val2)).magnitude > Tuning.F("curious.near", 3f)))
			{
				num2 += ((item2 >= trialBegan) ? 1 : 0);
				num3 += ((item2 < trialBegan && item2 > trialBegan - 2f) ? 1 : 0);
			}
		}
		if (num2 > 0 && num3 == 0)
		{
			num += 0.6f;
			list.Add("makes things appear");
		}
		int num4 = 0;
		using (List<(Rigidbody, Vector3)>.Enumerator enumerator2 = trialBodies.GetEnumerator())
		{
			int num5;
			int num6;
			for (; enumerator2.MoveNext(); num4 = num5 + num6)
			{
				(Rigidbody, Vector3) current2 = enumerator2.Current;
				Rigidbody item3 = current2.Item1;
				Vector3 item4 = current2.Item2;
				num5 = num4;
				if ((Object)(object)item3 != (Object)null)
				{
					val2 = item3.worldCenterOfMass - item4;
					if (((Vector3)(ref val2)).magnitude > 0.4f)
					{
						num6 = 1;
						continue;
					}
				}
				num6 = 0;
			}
		}
		if (num4 > 0)
		{
			num += 0.25f * (float)Mathf.Min(num4, 3);
			list.Add("moves things about");
		}
		bool sound;
		int num7 = Parts(out sound);
		if (num7 != trialParts || (sound && !trialSound))
		{
			num += 0.4f;
			list.Add((num7 != trialParts) ? "it changes" : "it makes a sound");
		}
		if (PortalWays.All.Count != trialPortals)
		{
			num += 0.8f;
			list.Add("opens a portal");
		}
		val2 = head.position - trialHead;
		if (((Vector3)(ref val2)).magnitude > 1.5f)
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
		if (!Holding() || (Object)(object)head == (Object)null)
		{
			return EndPlay();
		}
		float num = Time.time - trialAt;
		string text = trialAction.Split('/')[0];
		bool flag = trialAction.EndsWith("/down", StringComparison.Ordinal);
		float num2 = standingHeight;
		float num3 = num2 / 1.7f;
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector(0.2f * num3, num2 - 0.38f * num3, 0.42f * num3);
		Quaternion val2 = Quaternion.Euler(flag ? 42f : 0f, 0f, 0f);
		handOverride = true;
		playHeight = num2;
		playPitch = (flag ? 32f : 10f);
		handAt = val;
		handTurn = val2;
		switch (trialPhase)
		{
		case 0:
		{
			float num7 = ((trialN == 0) ? 1.1f : 0.45f);
			if (trialN == 0 && num < num7 - 0.3f)
			{
				handAt = new Vector3(0.16f * num3, num2 - 0.36f * num3, 0.34f * num3);
				handTurn = Quaternion.Euler(20f + Mathf.Sin(num * 5f) * 25f, -20f, Mathf.Sin(num * 4f) * 45f);
				playPitch = 34f;
			}
			if (num >= num7)
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
				handAt = val + new Vector3(Mathf.Sin(num * 38f) * 0.07f, Mathf.Cos(num * 33f) * 0.07f, 0f) * num3;
				break;
			case "swing":
			{
				float num6 = Mathf.Clamp01(num / num5);
				handAt = Vector3.Lerp(new Vector3(0.3f * num3, num2 - 0.1f * num3, -0.15f * num3), new Vector3(0.1f * num3, num2 - 0.7f * num3, 0.55f * num3), num6);
				handTurn = Quaternion.Euler(Mathf.Lerp(-50f, 60f, num6), 0f, 0f);
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
			GameObject val = (((Object)(object)rightHand != (Object)null) ? rightHand.m_CurrentAttachedGO : null);
			Poolee val2 = (((Object)(object)val != (Object)null) ? val.GetComponentInParent<Poolee>() : null);
			if (lesson == null || (Object)(object)val2 == (Object)null || enemy)
			{
				return false;
			}
			Rigidbody componentInChildren = ((Component)val2).GetComponentInChildren<Rigidbody>();
			string text = tinkerName;
			if (!UseLesson(lesson, atSpot: false))
			{
				return false;
			}
			foreach (Clone.Event @event in lesson.take.events)
			{
				if (@event.barcode == tinkerCode)
				{
					lessonThings[@event.barcode + "#" + Mathf.Max(0, Clone.ThingNumber(@event))] = val2;
				}
			}
			if ((Object)(object)componentInChildren != (Object)null)
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
		catch (Exception ex)
		{
			Lab.Event("curious: could not do it your way: " + ex.Message);
			return false;
		}
	}

	private void TheirWayStep()
	{
		try
		{
			if ((Object)(object)heldForLesson != (Object)null)
			{
				Poolee componentInParent = ((Component)heldForLesson).GetComponentInParent<Poolee>();
				if (((Object)(object)componentInParent != (Object)null && HeldByAnyone(((Component)componentInParent).transform, null)) || Time.time > heldForLessonUntil || !InLesson)
				{
					heldForLesson.isKinematic = heldForLessonWasKinematic;
					heldForLesson = null;
				}
			}
		}
		catch (Exception)
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
		} + (action.EndsWith("/down", StringComparison.Ordinal) ? " (pointed at the ground)" : "");
	}

	private void HearNews()
	{
		if (SelfTaught.NewsTime <= newsSeen || enemy)
		{
			return;
		}
		newsSeen = SelfTaught.NewsTime;
		Transform head = Head;
		if ((Object)(object)head == (Object)null)
		{
			return;
		}
		Vector3 val = SelfTaught.NewsAt - head.position;
		if (!(((Vector3)(ref val)).magnitude > Tuning.F("curious.news", 40f)))
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
		Vector3 val = Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward;
		Vector3 val2 = Vector3.zero;
		float num = float.MaxValue;
		Vector3 val4;
		RaycastHit val5 = default(RaycastHit);
		foreach (Collider item in Physics.OverlapSphere(position + val * 9f, 9f, -1, (QueryTriggerInteraction)1))
		{
			Rigidbody val3 = (((Object)(object)item != (Object)null) ? item.attachedRigidbody : null);
			if ((Object)(object)val3 == (Object)null || (Object)(object)val3 == (Object)(object)this.item || val3.isKinematic || (Object)(object)((Component)val3).GetComponentInParent<RigManager>() != (Object)null)
			{
				continue;
			}
			Vector3 worldCenterOfMass = val3.worldCenterOfMass;
			val4 = Flat(worldCenterOfMass - position);
			float magnitude = ((Vector3)(ref val4)).magnitude;
			if (!(magnitude < 4f) && !(magnitude > 16f) && !Targets.NearPlayer(worldCenterOfMass, 2.5f) && !(Vector3.Angle(val, Flat(worldCenterOfMass - position)) > 75f))
			{
				float value = Random.value;
				if ((!Physics.Linecast(position, worldCenterOfMass, ref val5, -1, (QueryTriggerInteraction)1) || (Object)(object)((RaycastHit)(ref val5)).rigidbody == (Object)(object)val3) && value < num)
				{
					num = value;
					val2 = worldCenterOfMass;
				}
			}
		}
		if (num == float.MaxValue)
		{
			Vector3 val6 = Quaternion.Euler(0f, bodyYaw + Random(-60f, 60f), 0f) * Vector3.forward;
			Vector3 val7 = position + val6 * Random(5f, 13f);
			RaycastHit val8 = default(RaycastHit);
			val2 = (Physics.Raycast(val7 + Vector3.up * 2f, Vector3.down, ref val8, 8f, -1, (QueryTriggerInteraction)1) ? ((RaycastHit)(ref val8)).point : (val7 - Vector3.up * 1.5f));
		}
		throwAt = val2;
		val4 = Flat(val2 - position);
		throwFar = ((Vector3)(ref val4)).magnitude;
		val4 = Flat(val2 - position);
		return ((Vector3)(ref val4)).normalized;
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
		float num = loft * ((float)Math.PI / 180f);
		Look(flyTarget, null, "its throw", 2.5f);
		return throwDir * (Mathf.Cos(num) * speed) + Vector3.up * (Mathf.Sin(num) * speed);
	}

	private void TrackThrow()
	{
		if ((Object)(object)flying == (Object)null)
		{
			flySince = 0f;
			return;
		}
		float num = Time.time - flySince;
		float num2 = flyNearest;
		Vector3 val = flying.worldCenterOfMass - flyTarget;
		flyNearest = Mathf.Min(num2, ((Vector3)(ref val)).magnitude);
		if (num > 0.5f)
		{
			val = flying.velocity;
			if (((Vector3)(ref val)).magnitude < 0.6f)
			{
				goto IL_0086;
			}
		}
		if (num < 4f)
		{
			return;
		}
		goto IL_0086;
		IL_0086:
		string text = SelfTaught.ThrowResult(flyFar, flySpeed, flyLoft, flyNearest);
		Lab.Event("throw: at " + flyFar.ToString("0") + " m, " + flySpeed.ToString("0.0") + " m/s at " + flyLoft.ToString("0") + " deg: " + text);
		flying = null;
	}

	public bool DriveTheirWay()
	{
		Driving.Model model = Lessons.Road();
		if (model == null)
		{
			return false;
		}
		Vector3 val = Feet;
		int num = 0;
		Vector3 val2;
		for (int i = 1; i < model.road.Count; i++)
		{
			val2 = Flat(model.road[i] - val);
			float sqrMagnitude = ((Vector3)(ref val2)).sqrMagnitude;
			val2 = Flat(model.road[num] - val);
			if (sqrMagnitude < ((Vector3)(ref val2)).sqrMagnitude)
			{
				num = i;
			}
		}
		List<Vector3> list = model.road;
		List<float> list2 = model.roadSpeed;
		val2 = Flat(list[0] - list[list.Count - 1]);
		bool flag = ((Vector3)(ref val2)).magnitude < 15f && list.Count > 30;
		if (flag)
		{
			List<Vector3> list3 = new List<Vector3>();
			List<float> list4 = new List<float>();
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
		Vector3 val3 = Flat(list[Mathf.Min(num + 4, list.Count - 1)] - list[num]);
		driveFacing = ((((Vector3)(ref val3)).sqrMagnitude > 0.01f) ? ((Vector3)(ref val3)).normalized : Vector3.zero);
		if (!DriveTo(list[list.Count - 1], flag ? "a lap of the way you drove" : "driving the way you drove"))
		{
			return false;
		}
		driveRoute = list;
		driveRouteSpeed = list2;
		driveRouteAt = num;
		Vector3 val4 = Flat(list[Mathf.Min(num + 4, list.Count - 1)] - list[num]);
		if (((Vector3)(ref placeAhead)).sqrMagnitude > 0.01f && ((Vector3)(ref val4)).sqrMagnitude > 0.01f)
		{
			int num2 = Mathf.Min(num + 1, list.Count - 1);
			for (int k = num2; k < list.Count - 6 && k < num2 + 14; k++)
			{
				bool flag2 = true;
				foreach (Collider item in Physics.OverlapSphere(list[k] + Vector3.up * 0.7f, 1.4f, -1, (QueryTriggerInteraction)1))
				{
					if ((Object)(object)item != (Object)null && (Object)(object)((Component)item).GetComponentInParent<RigManager>() != (Object)(object)rig && ((Object)(object)item.attachedRigidbody != (Object)null || (Object)(object)((Component)item).GetComponentInParent<RigManager>() != (Object)null))
					{
						flag2 = false;
					}
				}
				foreach (KeyValuePair<IntPtr, KeyValuePair<Vector3, float>> driveSpot in driveSpots)
				{
					if (driveSpot.Key != ((Il2CppObjectBase)(object)rig).Pointer && Time.time < driveSpot.Value.Value)
					{
						val2 = Flat(driveSpot.Value.Key - list[k]);
						if (((Vector3)(ref val2)).magnitude < Tuning.F("drive.apart", 4f))
						{
							flag2 = false;
						}
					}
				}
				if (flag2)
				{
					num2 = k;
					break;
				}
			}
			driveSpots[((Il2CppObjectBase)(object)rig).Pointer] = new KeyValuePair<Vector3, float>(list[num2], Time.time + 10f);
			Vector3 val5 = Flat(list[Mathf.Min(num2 + 4, list.Count - 1)] - list[num2]);
			placeAhead = ((((Vector3)(ref val5)).sqrMagnitude > 0.01f) ? ((Vector3)(ref val5)).normalized : ((Vector3)(ref val4)).normalized);
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
		if (lesson == null || InLesson || dead || enemy || climbing || (Object)(object)head == (Object)null)
		{
			return false;
		}
		Driving.Model model = Driving.Of(lesson);
		Poolee val = FreeVehicle(model.barcode, Tuning.F("drive.spare", 5f));
		if ((Object)(object)val == (Object)null)
		{
			foreach (Lessons.Lesson item in Lessons.Drives())
			{
				Driving.Model model2 = Driving.Of(item);
				Poolee val2 = ((item != lesson) ? FreeVehicle(model2.barcode, Tuning.F("drive.spare", 5f)) : null);
				if ((Object)(object)val2 != (Object)null)
				{
					lesson = item;
					model = model2;
					val = val2;
					break;
				}
			}
		}
		Vector3 val3;
		if (BotManager.OwnSpawns && (Object)(object)val != (Object)null && ((Vector3)(ref driveFacing)).sqrMagnitude > 0.01f)
		{
			val3 = Flat(((Component)val).transform.forward);
			if (Vector3.Dot(((Vector3)(ref val3)).normalized * model.forward, driveFacing) < 0.5f)
			{
				val = null;
			}
		}
		if ((Object)(object)val == (Object)null && !BotManager.OwnSpawns)
		{
			driveFacing = Vector3.zero;
			Poolee val4 = FreeVehicle(model.barcode, 200f);
			string[] obj = new string[7]
			{
				"driving: no ",
				lesson.name,
				" standing by within ",
				Tuning.F("drive.spare", 5f).ToString("0"),
				" m (nearest free one ",
				null,
				null
			};
			object obj2;
			if (!((Object)(object)val4 != (Object)null))
			{
				obj2 = "none";
			}
			else
			{
				val3 = Flat(((Component)val4).transform.position - Feet);
				obj2 = ((Vector3)(ref val3)).magnitude.ToString("0.0") + " m off";
			}
			obj[5] = (string)obj2;
			obj[6] = "), and it may not bring one of its own";
			Lab.Event(string.Concat(obj));
			return false;
		}
		Vector3 val5 = driveFacing;
		driveFacing = Vector3.zero;
		float num = Mathf.Max(0f, Mathf.Max(model.seatAt - 0.5f, model.driveFrom - 3f));
		Vector3 val6 = (((Object)(object)val == (Object)null) ? DriveOffWay(goal) : Vector3.zero);
		if ((Object)(object)val == (Object)null && ((Vector3)(ref val5)).sqrMagnitude > 0.01f && ((Vector3)(ref val6)).sqrMagnitude > 0.01f)
		{
			val6 = val5;
		}
		if ((Object)(object)val == (Object)null && ((Vector3)(ref val6)).sqrMagnitude < 0.01f)
		{
			if (Time.time - driveNoRoomSaid > 20f)
			{
				driveNoRoomSaid = Time.time;
				Lab.Event("driving: no room here for a " + lesson.name + ", not from here");
			}
			driveNextAt = Time.time + 12f;
			return false;
		}
		if ((Object)(object)val == (Object)null)
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
		driveRoute = (((Object)(object)after != (Object)null) ? new List<Vector3>() : null);
		driveRouteSpeed = null;
		driveRouteAt = 0;
		driveTrail = (Object)(object)after != (Object)null;
		driveWaitSince = -1f;
		driveNextAt = Time.time + (DriveSkill.Practice ? 4f : (Tuning.F("drive.every", 150f) * Random(0.7f, 1.5f)));
		placeAhead = val6;
		if ((Object)(object)val != (Object)null && model.sit != null)
		{
			lessonThings[model.sit.barcode + "#" + Mathf.Max(0, Clone.ThingNumber(model.sit))] = val;
		}
		string[] obj3 = new string[6] { "driving: ", why, " (", null, null, null };
		val3 = Flat(goal - Feet);
		obj3[3] = ((Vector3)(ref val3)).magnitude.ToString("0");
		obj3[4] = " m), in ";
		obj3[5] = (((Object)(object)val != (Object)null) ? ("the " + lesson.name + " standing by") : ("a " + lesson.name + " of its own"));
		Lab.Event(string.Concat(obj3));
		return true;
	}

	private Vector3 DriveOffWay(Vector3 goal)
	{
		Vector3 val = (((Object)(object)rig.physicsRig != (Object)null && (Object)(object)((Rig)rig.physicsRig).centerOfPressure != (Object)null) ? ((Rig)rig.physicsRig).centerOfPressure.position : Feet);
		Vector3 val2 = Flat(goal - val);
		try
		{
			if (drivePath == null)
			{
				drivePath = new NavMeshPath();
			}
			NavMeshHit val3 = default(NavMeshHit);
			NavMeshHit val4 = default(NavMeshHit);
			if (NavMesh.SamplePosition(val, ref val3, 2f, -1) && NavMesh.SamplePosition(goal, ref val4, 4f, -1) && NavMesh.CalculatePath(((NavMeshHit)(ref val3)).position, ((NavMeshHit)(ref val4)).position, -1, drivePath) && (int)drivePath.status != 2)
			{
				Vector3[] array = drivePath.corners;
				if (array != null && array.Length >= 2)
				{
					Vector3 val5 = Flat(array[1] - val);
					if (((Vector3)(ref val5)).magnitude > 1f)
					{
						val2 = Flat(array[1] - val);
					}
				}
			}
		}
		catch (Exception)
		{
		}
		if (((Vector3)(ref val2)).sqrMagnitude < 0.01f)
		{
			val2 = Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward;
		}
		((Vector3)(ref val2)).Normalize();
		Vector3 val6 = val + Vector3.up * 0.6f;
		Vector3 result = Vector3.zero;
		float num = float.MinValue;
		for (int i = 0; i < 16; i++)
		{
			Vector3 val7 = Quaternion.Euler(0f, (float)i * 22.5f, 0f) * val2;
			float num2 = 14f;
			foreach (RaycastHit item in Physics.SphereCastAll(val6, 0.45f, val7, 14f, -1, (QueryTriggerInteraction)1))
			{
				RaycastHit current = item;
				if (!((Object)(object)((RaycastHit)(ref current)).collider == (Object)null) && !(((RaycastHit)(ref current)).distance <= 0f) && !((Object)(object)((Component)((RaycastHit)(ref current)).collider).GetComponentInParent<RigManager>() == (Object)(object)rig) && !(((RaycastHit)(ref current)).normal.y > 0.55f))
				{
					num2 = Mathf.Min(num2, ((RaycastHit)(ref current)).distance);
				}
			}
			if (!(num2 < 4.2f) && Physics.Raycast(val + val7 * 2.2f + Vector3.up * 1f, Vector3.down, 2.5f, -1, (QueryTriggerInteraction)1))
			{
				float num3 = num2 / 14f + 0.8f * Vector3.Dot(val7, val2);
				if (num3 > num)
				{
					num = num3;
					result = val7;
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
			HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
			foreach (Collider item in Physics.OverlapSphere(Feet, within, -1, (QueryTriggerInteraction)1))
			{
				Poolee val = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Poolee>() : null);
				if ((Object)(object)val == (Object)null || !hashSet.Add(((Il2CppObjectBase)(object)val).Pointer) || (Object)(object)val.SpawnableCrate == (Object)null || ((Scannable)val.SpawnableCrate).Barcode.ID != barcode)
				{
					continue;
				}
				bool flag = HeldByAnyone(((Component)val).transform, null);
				foreach (Seat componentsInChild in ((Component)val).GetComponentsInChildren<Seat>(true))
				{
					flag |= (Object)(object)componentsInChild != (Object)null && (Object)(object)componentsInChild.rigManager != (Object)null;
				}
				if (flag)
				{
					continue;
				}
				if ((Object)(object)head != (Object)null)
				{
					Vector3 val2 = Flat(((Component)val).transform.position - head.position);
					if (((Vector3)(ref val2)).magnitude < 4f)
					{
						continue;
					}
				}
				if (!AnotherDriverBy(((Component)val).transform.position, Tuning.F("drive.theirs", 5f)))
				{
					val.Despawn();
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private bool AnotherDriverBy(Vector3 at, float within)
	{
		foreach (Bot item in BotManager.All)
		{
			if (item != this && !item.dead && item.DrivingLesson)
			{
				Vector3 val = Flat(item.Feet - at);
				if (((Vector3)(ref val)).magnitude < within)
				{
					return true;
				}
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
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(Feet, within, -1, (QueryTriggerInteraction)1))
		{
			Poolee val = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Poolee>() : null);
			if ((Object)(object)val == (Object)null || !hashSet.Add(((Il2CppObjectBase)(object)val).Pointer) || (Object)(object)val.SpawnableCrate == (Object)null || ((Scannable)val.SpawnableCrate).Barcode.ID != barcode)
			{
				continue;
			}
			bool flag = false;
			foreach (Seat componentsInChild in ((Component)val).GetComponentsInChildren<Seat>(true))
			{
				flag |= (Object)(object)componentsInChild != (Object)null && (Object)(object)componentsInChild.rigManager != (Object)null;
			}
			if (!flag && !HeldByAnyone(((Component)val).transform, null) && !(((Component)val).transform.up.y < 0.9f) && !NotItsToTake(val) && !AnotherDriverBy(((Component)val).transform.position, 2.5f))
			{
				Vector3 val2 = Flat(((Component)val).transform.position - Feet);
				float magnitude = ((Vector3)(ref val2)).magnitude;
				if (magnitude < num)
				{
					num = magnitude;
					result = val;
				}
			}
		}
		return result;
	}

	private Pose DriveFrame(Seat seat)
	{
		if (driveModel.inSeat)
		{
			return Pose.Of(((Component)seat).transform);
		}
		Rigidbody val = Pelvis();
		Vector3 val2 = (((Object)(object)val != (Object)null) ? Flat(val.rotation * Vector3.forward) : Flat(((Component)seat).transform.forward));
		return new Pose(((Object)(object)val != (Object)null) ? val.position : ((Component)seat).transform.position, (((Vector3)(ref val2)).sqrMagnitude > 0.001f) ? Quaternion.LookRotation(((Vector3)(ref val2)).normalized, Vector3.up) : Quaternion.identity);
	}

	private bool DriveStep(float dt)
	{
		Driving.Model model = driveModel;
		if (model == null)
		{
			return false;
		}
		Seat activeSeat = rig.activeSeat;
		Vector3 val;
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
			if ((Object)(object)activeSeat == (Object)null || !activeSeat.seated)
			{
				if (Time.time - driveWaitSince > 10f)
				{
					EndLesson("driving: could not get into it");
				}
				return false;
			}
			driving = true;
			driveTookAt = Time.time;
			skill = DriveSkill.Begin(((Il2CppObjectBase)(object)rig).Pointer, out skillTrying);
			val = Flat(driveGoal - ((Component)activeSeat).transform.position);
			skillFar = ((Vector3)(ref val)).magnitude;
			skillScored = (Object)(object)driveAfter == (Object)null && (driveRoute != null || skillFar > 18f);
			skillRouteFrom = driveRouteAt;
			skillBacks = (skillAbouts = 0);
			driveGo = (driveBack = (driveTurn = (driveTurnNow = 0f)));
			driveStuckFor = (driveOverFor = (driveYouOutFor = 0f));
			driveBackUntil = 0f;
			driveBacks = 0;
			driveWayAt = 0f;
			driveWay = (Vector3[])(object)new Vector3[0];
			driveWayFound = -100f;
			driveHandOff[0] = (driveHandOff[1] = 0f);
			driveBackFrom = ((Component)activeSeat).transform.position;
			driveYawHas = false;
			driveAbout = false;
			driveAboutAt = Time.time - 2f;
			driveSpin = 0f;
			Lab.Event("driving: sat in it (" + ((Object)activeSeat).name + "), off it goes");
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
		if ((Object)(object)activeSeat == (Object)null)
		{
			EndLesson("driving: out of its seat");
			return true;
		}
		val = Flat(((Component)activeSeat).transform.forward);
		Vector3 val2 = ((Vector3)(ref val)).normalized * model.forward;
		Rigidbody val3 = (((Object)(object)activeSeat.seatRb != (Object)null) ? activeSeat.seatRb : ((Component)activeSeat).GetComponentInParent<Rigidbody>());
		Vector3 position = ((Component)activeSeat).transform.position;
		float num = (((Object)(object)val3 != (Object)null) ? Vector3.Dot(val3.velocity, val2) : 0f);
		DriveHold(activeSeat, dt);
		if (WaitForRiders(activeSeat))
		{
			driveGo = (driveBack = (driveTurn = 0f));
			return true;
		}
		float num2 = Recorder.Yaw(val2);
		if (driveYawHas && dt > 0.0001f)
		{
			float num3 = Mathf.DeltaAngle(driveYawWas, num2) * ((float)Math.PI / 180f) / dt;
			driveSpin = Mathf.Lerp(driveSpin, num3, Mathf.Clamp01(dt * 4f));
			if (num > 2f && Mathf.Abs(driveTurnNow) > 0.05f && Mathf.Abs(driveTurnNow - driveTurn) < 0.05f && Time.time > driveBackUntil + 1f)
			{
				float num4 = Mathf.Clamp(driveSpin / num / driveTurnNow, 0.5f, 1.5f);
				model.answers = Mathf.Lerp(model.answers, num4, Mathf.Clamp01(dt * 0.4f));
			}
		}
		driveYawWas = num2;
		driveYawHas = true;
		_ = Player.Head;
		bool flag = (Object)(object)Player.RigManager != (Object)null && (Object)(object)Player.RigManager.activeSeat != (Object)null;
		if ((Object)(object)driveAfter != (Object)null)
		{
			driveGoal = driveAfter.position - Vector3.up * 1.2f;
		}
		val = Flat(driveGoal - position);
		float magnitude = ((Vector3)(ref val)).magnitude;
		if (driveTrail && driveRoute != null)
		{
			if (driveRoute.Count != 0)
			{
				val = Flat(driveGoal - driveRoute[driveRoute.Count - 1]);
				if (!(((Vector3)(ref val)).magnitude > 1.5f))
				{
					goto IL_0606;
				}
			}
			driveRoute.Add(driveGoal);
			if (driveRoute.Count > 400)
			{
				driveRoute.RemoveAt(0);
				driveRouteAt = Mathf.Max(0, driveRouteAt - 1);
			}
		}
		goto IL_0606;
		IL_0606:
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
		driveOverFor = ((((Component)activeSeat).transform.up.y < 0.4f) ? (driveOverFor + dt) : 0f);
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
		if ((Object)(object)driveAfter == (Object)null && !flag2 && magnitude < Tuning.F("drive.arrive", 3.5f))
		{
			DriveStop("there (" + driveWhy + ")");
			return true;
		}
		if ((Object)(object)driveAfter != (Object)null)
		{
			driveYouOutFor = (flag ? 0f : (driveYouOutFor + dt));
			float num5 = Tuning.F("drive.near", 6f);
			if (magnitude < num5)
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
		Vector3 val4 = Flat(DriveAim(position, reach) - position);
		float num6 = ((((Vector3)(ref val4)).sqrMagnitude > 0.01f) ? Vector3.SignedAngle(val2, ((Vector3)(ref val4)).normalized, Vector3.up) : 0f);
		if (Time.time >= driveFeelAt)
		{
			driveFeelAt = Time.time + 0.1f;
			feelLength = Mathf.Clamp(4f + Mathf.Abs(num) * 1.6f, 5f, 22f);
			DriveFeel(activeSeat, position, val2, feelLength);
		}
		float num7 = Tuning.F("drive.turn", 1.45f);
		float num8 = model.turnLeft * num7;
		float num9 = model.turnRight * num7;
		float num10 = feelClear[0];
		if (Time.time < driveBackUntil)
		{
			driveGo = ((num < 0f - Tuning.F("drive.back.fast", 2.5f) - 1f) ? 0.4f : 0f);
			driveBack = ((num > 0f - Tuning.F("drive.back.fast", 2.5f)) ? 0.75f : 0f);
			driveTurn = driveBackTurn;
			driveStuckFor = ((Mathf.Abs(num) < 0.25f) ? (driveStuckFor + dt) : 0f);
			if (driveStuckFor > 1.2f || feelBehind < 1.3f || (driveAbout && Mathf.Abs(num6) < 25f))
			{
				driveBackUntil = 0f;
				driveAboutAt = Time.time;
				driveStuckFor = 0f;
			}
			DriveSay(magnitude, num, num10, num6, activeSeat);
			return true;
		}
		bool flag3 = driveGo > 0.45f && Mathf.Abs(num) < 0.35f;
		driveStuckFor = (flag3 ? (driveStuckFor + dt) : Mathf.Max(0f, driveStuckFor - dt * 2f));
		bool flag4 = num10 < Tuning.F("drive.wall", 1.5f) && num < 1.5f && Mathf.Abs(num6) < 60f && feelBehind > 1.5f;
		if (driveStuckFor > 1.3f || (flag4 && Time.time - driveTookAt > 1.5f && Time.time - driveAboutAt > 1.5f))
		{
			val = Flat(position - driveBackFrom);
			if (((Vector3)(ref val)).magnitude > 6f)
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
			float num11 = 0f;
			for (int i = 1; i < feelAngles.Length; i++)
			{
				num11 += feelClear[i] * Mathf.Sign(feelAngles[i]);
			}
			float num12 = ((Mathf.Abs(num11) > 2f) ? Mathf.Sign(num11) : ((num6 >= 0f) ? 1f : (-1f)));
			driveBackTurn = ((num12 > 0f) ? num8 : num9);
			driveAbout = false;
			driveBackUntil = Time.time + Tuning.F("drive.back", 1.7f);
			Lab.Event("driving: stopped by something, backing out (" + driveBacks + ")");
			return true;
		}
		float num13 = Mathf.Max(0.05f, ((num6 > 0f) ? num9 : (0f - num8)) * Mathf.Clamp(model.answers, 0.5f, 1f));
		float num14 = 2f * Mathf.Sin(Mathf.Min(90f, Mathf.Abs(num6)) * ((float)Math.PI / 180f)) / Mathf.Max(2f, ((Vector3)(ref val4)).magnitude);
		float num15 = ((driveRoute != null) ? Tuning.F("drive.about.road", 120f) : 75f);
		if (model.hasBack && Mathf.Abs(num6) > num15 && num14 > num13 * 1.2f && Time.time - driveAboutAt > 2.5f && feelBehind > 2.5f && num < 4f)
		{
			driveAbout = true;
			driveBackTurn = ((num6 > 0f) ? num8 : num9);
			driveBackUntil = Time.time + Tuning.F("drive.about", 3.5f);
			driveStuckFor = 0f;
			skillAbouts++;
			if ((float)skillAbouts > Tuning.F("drive.lost", 8f))
			{
				DriveStop("it cannot get on (lost its way)");
				return true;
			}
			Lab.Event("driving: its way is " + num6.ToString("0") + " deg off, backing round to face it");
			return true;
		}
		bool flag5 = model.hasBack && Mathf.Abs(num6) > 75f && num14 > num13 * 1.2f;
		float num16 = Mathf.Min(feelLength, Mathf.Max(3f, ((Vector3)(ref val4)).magnitude));
		float num17 = Mathf.Clamp(num6, -75f, 75f);
		int num18 = 0;
		float num19 = float.MinValue;
		for (int j = 0; j < feelAngles.Length; j++)
		{
			float num20 = Mathf.Min(feelClear[j], num16) / num16 * 1.6f - Mathf.Abs(feelAngles[j] - num17) / 75f;
			if (num20 > num19)
			{
				num19 = num20;
				num18 = j;
			}
		}
		float num21 = num6;
		float num22 = Mathf.Max(3f, ((Vector3)(ref val4)).magnitude);
		if (Mathf.Abs(feelAngles[num18] - num17) > 10f)
		{
			num21 = feelAngles[num18];
			num22 = Mathf.Max(2.5f, feelClear[num18] * 0.7f);
		}
		float num23 = ((Mathf.Abs(num21) > 100f) ? (Mathf.Sign(num21) * 10f) : (2f * Mathf.Sin(num21 * ((float)Math.PI / 180f)) / num22));
		float num24 = num23 * skill[3] / Mathf.Clamp(model.answers, 0.6f, 1.5f);
		float num25 = (model.inSeat ? 0.05f : 0.3f);
		if (num > 2.5f && Mathf.Abs(num23) < 0.05f && Mathf.Abs(driveTurnNow - driveTurn) < 0.02f && Time.time > driveBackUntil + 2f)
		{
			model.trim = Mathf.Clamp(model.trim + (num23 - driveSpin / num) * Tuning.F("drive.trim", 0.9f) * (model.inSeat ? 0.3f : 1f) * dt, 0f - num25, num25);
		}
		model.trim = Mathf.Clamp(model.trim, 0f - num25, num25);
		driveTurn = Mathf.Clamp(num24 + model.trim, num8, num9);
		float num26 = model.side * skill[5] / Mathf.Max(4f, num * num);
		driveTurn = Mathf.Clamp(driveTurn, 0f - num26, num26);
		float num27 = model.top * skill[0];
		float num28 = Mathf.Sqrt(model.side * skill[1] / Mathf.Max(0.02f, Mathf.Abs(driveTurnNow)));
		float num29 = ((num18 == 0) ? feelClear[0] : Mathf.Lerp(feelClear[0], feelClear[num18], 0.5f));
		float num30 = Mathf.Sqrt(2f * skill[2] * Mathf.Max(0.15f, num29 - 2.5f));
		float num31 = 1f + Mathf.Sqrt(5f * Mathf.Max(0f, magnitude - (((Object)(object)driveAfter != (Object)null) ? Tuning.F("drive.near", 6f) : Tuning.F("drive.arrive", 3.5f))));
		if (flag2)
		{
			num31 = 99f;
		}
		float num32 = Mathf.Min(Mathf.Min(num27, num28), Mathf.Min(num30, num31));
		if (flag2 && driveRouteSpeed != null && driveRouteAt < driveRouteSpeed.Count)
		{
			float num33 = driveRouteSpeed[driveRouteAt];
			for (int k = driveRouteAt; k < driveRouteSpeed.Count && k < driveRouteAt + 5; k++)
			{
				num33 = Mathf.Min(num33, driveRouteSpeed[k]);
			}
			num32 = Mathf.Min(num32, Mathf.Max(2f, num33 + 0.5f));
		}
		if (flag5)
		{
			num32 = Mathf.Min(num32, 2.5f);
		}
		if (Time.time - driveAboutAt < Tuning.F("drive.careful", 3.5f) && driveBacks > 0)
		{
			num32 = Mathf.Min(num32, 2.8f);
		}
		num32 = Mathf.Min(num32, BendsAhead(position, num, model));
		float num34 = Mathf.Clamp((num32 - num) * 0.6f, -1f, 1f);
		float num35 = Mathf.Max(0f, num34);
		if (num < 0.6f && num32 > 0.6f)
		{
			num35 = Mathf.Max(num35, 0.6f);
		}
		driveGo = Mathf.MoveTowards(driveGo, num35, 3f * dt);
		driveBack = ((model.hasBack && num > num32 + 0.8f && num > 1f) ? Mathf.Clamp01((num - num32) * 0.5f) : 0f);
		if (driveBack > 0f)
		{
			driveGo = 0f;
		}
		DriveSay(magnitude, num, num10, num6, activeSeat);
		return true;
	}

	private void DriveSay(float far, float speed, float clear, float off, Seat seat)
	{
		if (!(Time.time - driveSaidAt < Tuning.F("drive.say", 2f)))
		{
			driveSaidAt = Time.time;
			Transform vrRoot = controls.vrRoot;
			Vector3 val = Flat(((Component)seat).transform.forward);
			Vector3 val2 = ((Vector3)(ref val)).normalized * ((driveModel != null) ? driveModel.forward : 1f);
			Rigidbody componentInParent = ((Component)seat).GetComponentInParent<Rigidbody>();
			float num = (((Object)(object)componentInParent != (Object)null) ? Vector3.Dot(componentInParent.velocity, val2) : 0f);
			Lab.Event("driving: yaw " + Recorder.Yaw(val2).ToString("0") + ", along " + num.ToString("0.0") + " m/s, " + far.ToString("0") + " m to go, " + speed.ToString("0.0") + " m/s, go " + driveGo.ToString("0.0") + " back " + driveBack.ToString("0.0") + ", turning " + driveTurnNow.ToString("0.00") + " (wants " + driveTurn.ToString("0.00") + "), its way " + off.ToString("0") + " deg off, clear " + clear.ToString("0.0") + " m, way of " + driveWay.Length + ", behind " + feelBehind.ToString("0.0") + ((BotManager.Missed > 0) ? (", put on by hand " + BotManager.Missed) : "") + ", answers " + driveModel.answers.ToString("0.00") + " trim " + driveModel.trim.ToString("0.00") + ((driveRoute != null) ? (", along a way " + driveRouteAt + "/" + driveRoute.Count) : "") + ((Time.time < driveBackUntil) ? ", backing" : "") + ", hands " + (((Object)(object)LeftHand != (Object)null && (Object)(object)LeftHand.m_CurrentAttachedGO != (Object)null) ? "L" : "-") + (((Object)(object)RightHand != (Object)null && (Object)(object)RightHand.m_CurrentAttachedGO != (Object)null) ? "R" : "-") + ", play space " + Mathf.DeltaAngle(((Component)seat).transform.eulerAngles.y, vrRoot.eulerAngles.y).ToString("0") + " deg from its seat");
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
			float num2;
			if (driveRoute == null)
			{
				float num = skillFar;
				Vector3 val = Flat(driveGoal - ((Component)seat).transform.position);
				num2 = Mathf.Max(0f, num - ((Vector3)(ref val)).magnitude);
			}
			else
			{
				num2 = (float)Mathf.Max(0, driveRouteAt - skillRouteFrom) * 1.5f;
			}
			float num3 = num2;
			if (num3 > 12f && (!flag || !(num3 < 60f)))
			{
				DriveSkill.End(((Il2CppObjectBase)(object)rig).Pointer, skillTrying, num3, Time.time - driveTookAt, skillBacks, skillAbouts, flag);
			}
		}
		driveGo = (driveBack = 0f);
		Hand[] array = (Hand[])(object)new Hand[2] { LeftHand, RightHand };
		foreach (Hand val2 in array)
		{
			if ((Object)(object)val2 != (Object)null && (Object)(object)val2.m_CurrentAttachedGO != (Object)null)
			{
				FreeHand(val2);
			}
		}
		cloneHeld[0] = (cloneHeld[1] = null);
		try
		{
			seat.EgressRig(atOnce);
		}
		catch (Exception)
		{
		}
		Poolee componentInParent = ((Component)seat).GetComponentInParent<Poolee>();
		leftVehicle = (((Object)(object)componentInParent != (Object)null) ? ((Component)componentInParent).transform : ((Component)seat).transform.root);
		if ((Object)(object)componentInParent != (Object)null)
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
		Poolee componentInParent = ((Component)seat).GetComponentInParent<Poolee>();
		Transform part = (((Object)(object)componentInParent != (Object)null) ? ((Component)componentInParent).transform : ((Component)seat).transform.root);
		Hand[] array = (Hand[])(object)new Hand[2] { LeftHand, RightHand };
		Clone.Event[] array2 = new Clone.Event[2] { driveModel.leftGrab, driveModel.rightGrab };
		for (int i = 0; i < 2; i++)
		{
			Hand val = array[i];
			if ((Object)(object)val == (Object)null || array2[i] == null)
			{
				continue;
			}
			if ((Object)(object)val.m_CurrentAttachedGO != (Object)null)
			{
				driveHandOff[i] = 0f;
				continue;
			}
			driveHandOff[i] += 0.25f;
			Grip val2 = null;
			Grip[] array3 = Clone.GripsOf(part);
			string text = Clone.GripName(array2[i]);
			Grip[] array4 = array3;
			foreach (Grip val3 in array4)
			{
				if ((Object)(object)val3 != (Object)null && text != null && ((Object)val3).name == text && ((Object)(object)val2 == (Object)null || !val3.HasAttachedHands()))
				{
					val2 = val3;
				}
			}
			if ((Object)(object)val2 == (Object)null && array2[i].grip >= 0 && array2[i].grip < array3.Length)
			{
				val2 = array3[array2[i].grip];
			}
			if (!((Object)(object)val2 == (Object)null))
			{
				Vector3 val4 = ((Component)val2).transform.position - ((Component)val).transform.position;
				float magnitude = ((Vector3)(ref val4)).magnitude;
				if ((!(magnitude > Tuning.F("lesson.reach", 0.4f)) || !(driveHandOff[i] < 1.5f)) && Grab(val, val2))
				{
					cloneHeld[i] = val2;
					Lab.Event("driving: its " + ((i == 0) ? "left" : "right") + " hand on the " + ((Object)val2).name + " (" + magnitude.ToString("0.00") + " m off)");
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
		List<Vector3> list = driveRoute;
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
		Vector3 val = Flat(list[Mathf.Min(num5, list.Count - 1)] - at);
		float num6 = ((Vector3)(ref val)).magnitude;
		for (int j = Mathf.Max(1, num5); j < list.Count - 1; j++)
		{
			if (!(num6 < num4))
			{
				break;
			}
			int index = Mathf.Max(0, j - 2);
			int index2 = Mathf.Min(list.Count - 1, j + 2);
			Vector3 val2 = Flat(list[j] - list[index]);
			Vector3 val3 = Flat(list[index2] - list[j]);
			if (j > num5)
			{
				float num7 = num6;
				val = Flat(list[j] - list[j - 1]);
				num6 = num7 + ((Vector3)(ref val)).magnitude;
			}
			if (!(((Vector3)(ref val2)).sqrMagnitude < 0.04f) && !(((Vector3)(ref val3)).sqrMagnitude < 0.04f))
			{
				float num8 = Vector3.Angle(val2, val3) * ((float)Math.PI / 180f);
				float num9 = Mathf.Max(3f, (((Vector3)(ref val2)).magnitude + ((Vector3)(ref val3)).magnitude) * 0.5f);
				float num10 = num8 / num9;
				if (!(num10 < 0.04f))
				{
					float num11 = Mathf.Sqrt(num2 / num10);
					float num12 = Mathf.Sqrt(num11 * num11 + 2f * num3 * Mathf.Max(0f, num6 - 1.5f));
					num = Mathf.Min(num, num12);
				}
			}
		}
		return Mathf.Max(2f, num);
	}

	private Vector3 DriveAim(Vector3 at, float reach)
	{
		Vector3 val;
		if (driveRoute != null && driveRoute.Count > 1)
		{
			while (driveRouteAt < driveRoute.Count - 1)
			{
				val = Flat(driveRoute[driveRouteAt] - at);
				if (!(((Vector3)(ref val)).magnitude < 3f))
				{
					val = Flat(driveRoute[driveRouteAt + 1] - at);
					float magnitude = ((Vector3)(ref val)).magnitude;
					val = Flat(driveRoute[driveRouteAt] - at);
					if (!(magnitude < ((Vector3)(ref val)).magnitude) && !(Vector3.Dot(Flat(driveRoute[driveRouteAt + 1] - driveRoute[driveRouteAt]), Flat(at - driveRoute[driveRouteAt])) > 0f))
					{
						break;
					}
				}
				driveRouteAt++;
			}
			Vector3 val2 = at;
			float num = reach;
			for (int i = driveRouteAt; i < driveRoute.Count; i++)
			{
				Vector3 val3 = Flat(driveRoute[i] - val2);
				float magnitude2 = ((Vector3)(ref val3)).magnitude;
				if (magnitude2 >= num && magnitude2 > 0.001f)
				{
					return val2 + val3 / magnitude2 * num;
				}
				num -= magnitude2;
				((Vector3)(ref val2))._002Ector(driveRoute[i].x, val2.y, driveRoute[i].z);
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
				NavMeshHit val4 = default(NavMeshHit);
				NavMeshHit val5 = default(NavMeshHit);
				if (NavMesh.SamplePosition(at, ref val4, 2f, -1) && Mathf.Abs(((NavMeshHit)(ref val4)).position.y - at.y) < 1.5f && NavMesh.SamplePosition(driveGoal, ref val5, 4f, -1) && NavMesh.CalculatePath(((NavMeshHit)(ref val4)).position, ((NavMeshHit)(ref val5)).position, -1, drivePath) && (int)drivePath.status == 0)
				{
					Vector3[] array = drivePath.corners;
					if (array != null && array.Length >= 2)
					{
						val = Flat(array[^1] - driveGoal);
						if (((Vector3)(ref val)).magnitude < 4f)
						{
							driveWay = array;
							driveWayNext = 1;
							driveWayFound = Time.time;
							flag = true;
						}
					}
				}
			}
			catch (Exception)
			{
			}
			if (!flag && (driveWay.Length == 0 || Time.time - driveWayFound > 6f || (Object)(object)driveAfter != (Object)null))
			{
				driveWay = (Vector3[])(object)new Vector3[2] { at, driveGoal };
				driveWayNext = 1;
			}
		}
		while (driveWayNext < driveWay.Length - 1)
		{
			val = Flat(driveWay[driveWayNext] - at);
			if (!(((Vector3)(ref val)).magnitude < 2.5f))
			{
				break;
			}
			driveWayNext++;
		}
		Vector3 val6 = at;
		float num2 = reach;
		for (int j = driveWayNext; j < driveWay.Length; j++)
		{
			Vector3 val7 = Flat(driveWay[j] - val6);
			float magnitude3 = ((Vector3)(ref val7)).magnitude;
			if (magnitude3 >= num2 && magnitude3 > 0.001f)
			{
				return val6 + val7 / magnitude3 * num2;
			}
			num2 -= magnitude3;
			((Vector3)(ref val6))._002Ector(driveWay[j].x, val6.y, driveWay[j].z);
		}
		return driveWay[driveWay.Length - 1];
	}

	private void DriveFeel(Seat seat, Vector3 at, Vector3 ahead, float length)
	{
		Poolee componentInParent = ((Component)seat).GetComponentInParent<Poolee>();
		Transform val = (((Object)(object)componentInParent != (Object)null) ? ((Component)componentInParent).transform : ((Component)seat).transform.root);
		Vector3 val2 = at + Vector3.up * Tuning.F("drive.feel.up", 0.35f);
		for (int i = 0; i < feelAngles.Length; i++)
		{
			Vector3 val3 = Quaternion.Euler(0f, feelAngles[i], 0f) * ahead;
			float num = length;
			foreach (RaycastHit item in Physics.SphereCastAll(val2, 0.25f, val3, length, -1, (QueryTriggerInteraction)1))
			{
				RaycastHit current = item;
				Collider collider = ((RaycastHit)(ref current)).collider;
				if (!((Object)(object)collider == (Object)null) && !(((RaycastHit)(ref current)).distance >= num) && !(((RaycastHit)(ref current)).distance <= 0f) && !((Component)collider).transform.IsChildOf(val) && !((Object)(object)((Component)collider).GetComponentInParent<RigManager>() == (Object)(object)rig) && !(((RaycastHit)(ref current)).normal.y > 0.55f))
				{
					Rigidbody attachedRigidbody = collider.attachedRigidbody;
					if (!((Object)(object)attachedRigidbody != (Object)null) || attachedRigidbody.isKinematic || !(attachedRigidbody.mass < 8f) || !((Object)(object)((Component)collider).GetComponentInParent<RigManager>() == (Object)null))
					{
						num = ((RaycastHit)(ref current)).distance;
					}
				}
			}
			float num2 = Mathf.Min(num, 4f);
			if (num2 > 1.5f && !Physics.Raycast(val2 + val3 * num2 + Vector3.up * 0.8f, Vector3.down, 3.2f, -1, (QueryTriggerInteraction)1))
			{
				num = Mathf.Min(num, 1.2f);
			}
			feelClear[i] = num;
		}
		feelBehind = 5f;
		foreach (RaycastHit item2 in Physics.SphereCastAll(val2, 0.3f, -ahead, 5f, -1, (QueryTriggerInteraction)1))
		{
			RaycastHit current2 = item2;
			Collider collider2 = ((RaycastHit)(ref current2)).collider;
			if (!((Object)(object)collider2 == (Object)null) && !(((RaycastHit)(ref current2)).distance <= 0f) && !((Component)collider2).transform.IsChildOf(val) && !((Object)(object)((Component)collider2).GetComponentInParent<RigManager>() == (Object)(object)rig) && !(((RaycastHit)(ref current2)).normal.y > 0.55f))
			{
				feelBehind = Mathf.Min(feelBehind, ((RaycastHit)(ref current2)).distance);
			}
		}
		if (!Physics.Raycast(val2 - ahead * 2.5f + Vector3.up * 0.8f, Vector3.down, 3.2f, -1, (QueryTriggerInteraction)1))
		{
			feelBehind = Mathf.Min(feelBehind, 1f);
		}
	}

	private void DrivePose()
	{
		Driving.Model model = driveModel;
		Seat activeSeat = rig.activeSeat;
		poseWhy = ((model == null) ? "no model" : (((Object)(object)activeSeat == (Object)null) ? "no seat" : ""));
		if (model == null || (Object)(object)activeSeat == (Object)null || !CloneAt(driveClockAt, out var a, out var b, out var k))
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
		float num = (model.turnRight - model.turnLeft) * Tuning.F("drive.hands", 1.6f);
		driveTurnNow = Mathf.MoveTowards(driveTurnNow, driveTurn, num * Time.deltaTime);
		model.Hands(driveTurnNow, out var l, out var r);
		Pose pose = obj * l;
		Pose pose2 = obj * r;
		if (!Bad(pose.position) && !Bad(pose2.position))
		{
			((Component)((ControllerRig)controls).leftController).transform.SetPositionAndRotation(pose.position, pose.rotation);
			((Component)((ControllerRig)controls).rightController).transform.SetPositionAndRotation(pose2.position, pose2.rotation);
		}
		Vector3 position = a.head.position;
		if (Mathf.Abs(cloneSize - 1f) > 0.02f)
		{
			position.y *= cloneSize;
		}
		controls.headset.localPosition = position;
		float num2 = Mathf.Clamp(driveTurnNow * Tuning.F("drive.look", 70f), -35f, 35f);
		Vector3 val = Flat(((Component)activeSeat).transform.forward);
		float num3 = (0f - Mathf.Asin(Mathf.Clamp((model.head * Vector3.forward).y, -1f, 1f))) * 57.29578f;
		if (((Vector3)(ref val)).sqrMagnitude > 0.001f)
		{
			controls.headset.rotation = Quaternion.LookRotation(((Vector3)(ref val)).normalized * model.forward, Vector3.up) * Quaternion.Euler(Mathf.Clamp(num3, -25f, 35f), num2, 0f);
		}
		driveAimLeft = pose.position;
		driveAimRight = pose2.position;
		PutInput(((ControllerRig)controls).leftController, a.leftIn, b.leftIn, k, trigger: false);
		PutInput(((ControllerRig)controls).rightController, a.rightIn, b.rightIn, k, trigger: false);
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
		Pull(((ControllerRig)controls).leftController, Mathf.Clamp01(leftPull));
		Pull(((ControllerRig)controls).rightController, Mathf.Clamp01(rightPull));
		leftStick = Vector2.ClampMagnitude(leftStick, 1f);
		rightStick = Vector2.ClampMagnitude(rightStick, 1f);
		if ((Object)(object)((ControllerRig)controls).leftController != (Object)null)
		{
			((ControllerRig)controls).leftController._thumbstickAxis = leftStick;
		}
		if ((Object)(object)((ControllerRig)controls).rightController != (Object)null)
		{
			((ControllerRig)controls).rightController._thumbstickAxis = rightStick;
		}
		((ControllerRig)controls)._axisPrimary = leftStick;
		((ControllerRig)controls)._axisPrimaryDirty = leftStick != Vector2.zero;
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
		if (!((Object)(object)c == (Object)null))
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
			Poolee val = FreeVehicle(Driving.Of(item).barcode, within);
			if (!((Object)(object)val == (Object)null))
			{
				Vector3 val2 = Flat(((Component)val).transform.position - Feet);
				float magnitude = ((Vector3)(ref val2)).magnitude;
				if (magnitude < num)
				{
					num = magnitude;
					result = val;
				}
			}
		}
		return result;
	}

	private bool ToAVehicle(Transform you, float far, float dt)
	{
		if (BotManager.OwnSpawns || (Object)(object)you == (Object)null || far < Tuning.F("drive.follow", 10f) || InLesson)
		{
			return false;
		}
		RigManager rigManager = Player.RigManager;
		if ((Object)(object)rigManager == (Object)null || (Object)(object)rigManager.activeSeat == (Object)null)
		{
			kartSought = null;
			return false;
		}
		Vector3 val;
		if (Time.time >= kartSeekAt)
		{
			kartSeekAt = Time.time + 2f;
			kartSought = AnyVehicle(Tuning.F("drive.follow.seek", 35f));
			if ((Object)(object)kartSought != (Object)null)
			{
				Navigator navigator = nav;
				Vector3 position = ((Component)kartSought).transform.position;
				val = Flat(((Component)kartSought).transform.right);
				navigator.SetGoal(position - ((Vector3)(ref val)).normalized * 1.2f);
			}
		}
		if ((Object)(object)kartSought == (Object)null)
		{
			return false;
		}
		val = Flat(((Component)kartSought).transform.position - Feet);
		if (((Vector3)(ref val)).magnitude < Tuning.F("drive.spare", 5f) - 1f)
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
		if ((Object)(object)you == (Object)null || far < Tuning.F("drive.follow", 10f) || Time.time < driveFollowAt)
		{
			return false;
		}
		driveFollowAt = Time.time + 4f;
		RigManager rigManager = Player.RigManager;
		if ((Object)(object)rigManager == (Object)null || (Object)(object)rigManager.activeSeat == (Object)null)
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
		Vector3 val = Feet;
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
			HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
			foreach (Seat item in Object.FindObjectsOfType<Seat>())
			{
				Poolee val2 = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Poolee>() : null);
				if ((Object)(object)val2 == (Object)null || !hashSet.Add(((Il2CppObjectBase)(object)val2).Pointer) || (Object)(object)item.rigManager != (Object)null || !((Component)val2).gameObject.activeInHierarchy)
				{
					continue;
				}
				if ((Object)(object)head != (Object)null)
				{
					Vector3 val3 = Flat(((Component)val2).transform.position - head.position);
					if (((Vector3)(ref val3)).magnitude < 6f)
					{
						continue;
					}
				}
				val2.Despawn();
				num++;
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
			if ((Object)(object)head != (Object)null)
			{
				flag = DriveTo(head.position - Vector3.up * 1.2f, (where == "me") ? "to you" : "after you", (where == "follow") ? head : null);
				break;
			}
			goto default;
		default:
		{
			if (!FarPlace(val, out var place))
			{
				place = val + Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward * 30f;
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
			if ((Object)(object)car == (Object)null || (Object)(object)gun == (Object)null)
			{
				return;
			}
			Transform val = GunWeight.RootOf((Component)(object)gun);
			foreach (Collider componentsInChild in ((Component)car).GetComponentsInChildren<Collider>(true))
			{
				if ((Object)(object)componentsInChild == (Object)null || componentsInChild.isTrigger)
				{
					continue;
				}
				foreach (Collider componentsInChild2 in ((Component)val).GetComponentsInChildren<Collider>(true))
				{
					if ((Object)(object)componentsInChild2 != (Object)null && !componentsInChild2.isTrigger)
					{
						Physics.IgnoreCollision(componentsInChild, componentsInChild2, ignore);
					}
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private bool RideLineClear(Vector3 from, Vector3 to, out string blockedBy)
	{
		blockedBy = "";
		Vector3 val = to - from;
		float magnitude = ((Vector3)(ref val)).magnitude;
		if (magnitude < 0.05f)
		{
			return true;
		}
		foreach (RaycastHit item in Physics.RaycastAll(from, val / magnitude, magnitude - 0.3f, -1, (QueryTriggerInteraction)1))
		{
			RaycastHit current = item;
			Collider collider = ((RaycastHit)(ref current)).collider;
			if ((Object)(object)collider == (Object)null || ((Object)(object)gun != (Object)null && ((Component)collider).transform.IsChildOf(GunWeight.RootOf((Component)(object)gun))))
			{
				continue;
			}
			RigManager componentInParent = ((Component)collider).GetComponentInParent<RigManager>();
			if (!((Object)(object)componentInParent == (Object)(object)rig))
			{
				if ((Object)(object)ridingCar != (Object)null && ((Component)collider).transform.IsChildOf(ridingCar))
				{
					blockedBy = "its own car (" + ((Object)collider).name + ", " + ((RaycastHit)(ref current)).distance.ToString("0.00") + " m out)";
					return false;
				}
				if ((Object)(object)componentInParent != (Object)null && (rideTarget.bot == null || (Object)(object)componentInParent != (Object)(object)rideTarget.bot.rig) && !rideTarget.player)
				{
					blockedBy = (((Object)(object)componentInParent == (Object)(object)Player.RigManager) ? "you" : "a body");
					return false;
				}
			}
		}
		return true;
	}

	private void RideShoot(Transform head, float dt)
	{
		if ((Object)(object)gun == (Object)null || !Holding() || (Object)(object)head == (Object)null || Tuning.F("ride.shoot", 0f) < 0.5f)
		{
			handOverride = false;
			return;
		}
		Transform firePointTransform = gun.firePointTransform;
		if ((Object)(object)firePointTransform == (Object)null)
		{
			return;
		}
		Seat activeSeat = rig.activeSeat;
		Vector3 forward = (((Object)(object)activeSeat != (Object)null) ? Flat(((Component)activeSeat).transform.forward) : Vector3.zero);
		if (((Vector3)(ref forward)).sqrMagnitude > 0.01f)
		{
			bodyYaw = Recorder.Yaw(forward);
		}
		float num = 0f;
		if ((Object)(object)activeSeat != (Object)null && (Object)(object)ridingCar != (Object)null)
		{
			num = Mathf.Sign(ridingCar.InverseTransformPoint(((Component)activeSeat).transform.position).x);
		}
		Vector3 val2;
		if (Time.time >= rideLookAt)
		{
			rideLookAt = Time.time + 0.3f;
			Targets.Target target = rideTarget;
			if (!Targets.Find(head.position, Tuning.F("ride.shoot.range", 50f), rig, out rideTarget))
			{
				rideTarget = default(Targets.Target);
			}
			if ((Object)(object)rideTarget.transform != (Object)null && num != 0f && (Object)(object)ridingCar != (Object)null)
			{
				Vector3 val = ridingCar.InverseTransformPoint(rideTarget.Point);
				float num2 = Mathf.Atan2(val.x, val.z) * 57.29578f * num;
				if (num2 < Tuning.F("ride.side.from", 25f) || num2 > Tuning.F("ride.side.to", 155f))
				{
					rideTarget = default(Targets.Target);
				}
			}
			if ((Object)(object)rideTarget.transform != (Object)null && (Object)(object)rideTarget.transform != (Object)(object)target.transform)
			{
				rideTargetSince = Time.time;
				if (Time.time - rideSaidAt > 3f)
				{
					rideSaidAt = Time.time;
					string[] obj = new string[5]
					{
						"drive-by: taking aim at ",
						((Object)rideTarget.transform).name,
						" from the car (",
						null,
						null
					};
					val2 = rideTarget.Point - head.position;
					obj[3] = ((Vector3)(ref val2)).magnitude.ToString("0");
					obj[4] = " m)";
					Lab.Event(string.Concat(obj));
				}
			}
		}
		if ((Object)(object)rideTarget.transform == (Object)null || !Targets.Alive(rideTarget))
		{
			Pull(on: false);
			Aim(head.position + Quaternion.Euler(0f, bodyYaw, 0f) * new Vector3(0f, -1f, 3f), head, Hold.Low);
			return;
		}
		Vector3 val3 = rideTarget.Point + AimSpot(head.position);
		Aim(val3, head, Hold.Aimed);
		lastAimPoint = val3;
		float num3 = Vector3.Angle(firePointTransform.forward, val3 - firePointTransform.position);
		string blockedBy;
		bool flag = RideLineClear(firePointTransform.position, val3, out blockedBy);
		if (!flag && Time.time - rideBlockedSaidAt > 3f)
		{
			rideBlockedSaidAt = Time.time;
			Lab.Event("drive-by: holding fire, " + blockedBy + " in the way");
		}
		bool flag2 = Time.time - rideTargetSince > reaction && num3 < Tuning.F("ride.shoot.within", 6f);
		if (Time.time - rideStatusAt > 2f)
		{
			rideStatusAt = Time.time;
			Rigidbody val4 = Pelvis();
			Vector3 v = (((Object)(object)val4 != (Object)null) ? (Quaternion.Inverse(Quaternion.Euler(0f, bodyYaw, 0f)) * (val3 - val4.position)) : Vector3.zero);
			string[] obj2 = new string[14]
			{
				"drive-by aim: muzzle ",
				num3.ToString("0"),
				" deg off, line ",
				flag ? "clear" : ("blocked by " + blockedBy),
				", target ",
				Recorder.Yaw(Flat(v)).ToString("0"),
				" deg from its facing, its yaw ",
				bodyYaw.ToString("0"),
				", gun ",
				Holding() ? "held" : "NOT held",
				lockSet ? ", kept fast" : ", not kept fast",
				", hand ",
				null,
				null
			};
			object obj3;
			if (!((Object)(object)RightHand != (Object)null) || !((Object)(object)((ControllerRig)controls).rightController != (Object)null))
			{
				obj3 = "?";
			}
			else
			{
				val2 = ((Component)RightHand).transform.position - ((Component)((ControllerRig)controls).rightController).transform.position;
				obj3 = ((Vector3)(ref val2)).magnitude.ToString("0.00");
			}
			obj2[12] = (string)obj3;
			obj2[13] = " m from where sent";
			Lab.Event(string.Concat(obj2));
		}
		if (flag2 && flag && trigger <= 0.5f && Time.time >= nextPullAt)
		{
			LabRideShots++;
			string[] obj4 = new string[11]
			{
				"drive-by: fires at ",
				((Object)rideTarget.transform).name,
				" (",
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null
			};
			val2 = val3 - firePointTransform.position;
			obj4[3] = ((Vector3)(ref val2)).magnitude.ToString("0");
			obj4[4] = " m, ";
			obj4[5] = num3.ToString("0.0");
			obj4[6] = " deg off, the car at ";
			obj4[7] = CarSpeed(ridingCar).ToString("0.0");
			obj4[8] = " m/s, shot ";
			obj4[9] = LabRideShots.ToString();
			obj4[10] = ")";
			Lab.Event(string.Concat(obj4));
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
		int num;
		if ((Object)(object)rig.avatar != (Object)null)
		{
			Vector3 val = ((Component)rig.avatar).transform.localScale - take.avatarScale;
			num = ((((Vector3)(ref val)).sqrMagnitude < 0.0001f) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
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
		rig.Teleport(exactTake.startFeet, true);
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
				if ((Object)(object)value != (Object)null && ((Component)value).gameObject.activeInHierarchy && !HeldByPlayer(((Component)value).transform))
				{
					value.Despawn();
				}
			}
			catch (Exception)
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
				Poolee val = ExactFindThere(item);
				if ((Object)(object)val != (Object)null)
				{
					exactItems[item.id] = val;
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
		foreach (Collider item in Physics.OverlapSphere(i.at.position, 0.15f, -1, (QueryTriggerInteraction)1))
		{
			Poolee val = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Poolee>() : null);
			if ((Object)(object)val != (Object)null && (Object)(object)val.SpawnableCrate != (Object)null && ((Scannable)val.SpawnableCrate).Barcode.ID == i.barcode && !HeldByPlayer(((Component)val).transform))
			{
				return val;
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
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(at.position, 1.5f, -1, (QueryTriggerInteraction)1))
		{
			Poolee val = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Poolee>() : null);
			if ((Object)(object)val != (Object)null)
			{
				hashSet.Add(((Il2CppObjectBase)(object)val).Pointer);
			}
		}
		foreach (Poolee value in exactItems.Values)
		{
			if ((Object)(object)value != (Object)null)
			{
				hashSet.Add(((Il2CppObjectBase)(object)value).Pointer);
			}
		}
		ExactClone.Suppress();
		Clone.Suppress();
		HelperMethods.SpawnCrate(i.barcode, at.position, at.rotation, Vector3.one, false, (Action<GameObject>)null, (Action<GameObject>)null);
		exactPending.Add((i, at, hashSet, Time.time));
	}

	private void ExactFindSpawns()
	{
		for (int num = exactPending.Count - 1; num >= 0; num--)
		{
			(ExactClone.Item, Pose, HashSet<IntPtr>, float) tuple = exactPending[num];
			Poolee val = null;
			foreach (Collider item in Physics.OverlapSphere(tuple.Item2.position, 1.5f, -1, (QueryTriggerInteraction)1))
			{
				Poolee val2 = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Poolee>() : null);
				if ((Object)(object)val2 != (Object)null && !tuple.Item3.Contains(((Il2CppObjectBase)(object)val2).Pointer) && (Object)(object)val2.SpawnableCrate != (Object)null && ((Scannable)val2.SpawnableCrate).Barcode.ID == tuple.Item1.barcode)
				{
					val = val2;
					break;
				}
			}
			if ((Object)(object)val != (Object)null)
			{
				exactPending.RemoveAt(num);
				((Component)val).transform.SetPositionAndRotation(tuple.Item2.position, tuple.Item2.rotation);
				exactItems[tuple.Item1.id] = val;
				Lab.Event("exact: its " + ((Object)((Component)val).gameObject).name + " came (" + (Time.time - tuple.Item4).ToString("0.00") + " s)");
				foreach (var item2 in exactPending)
				{
					item2.before.Add(((Il2CppObjectBase)(object)val).Pointer);
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
		Poolee val = (((Object)(object)held != (Object)null) ? held.GetComponentInParent<Poolee>() : null);
		if ((Object)(object)val == (Object)null)
		{
			return -1;
		}
		foreach (KeyValuePair<int, Poolee> exactItem in exactItems)
		{
			if ((Object)(object)exactItem.Value != (Object)null && ((Il2CppObjectBase)(object)exactItem.Value).Pointer == ((Il2CppObjectBase)(object)val).Pointer)
			{
				return exactItem.Key;
			}
		}
		return -2;
	}

	private void ExactPutInHand(ExactClone.Item i)
	{
		if (!exactPouched.Add(i.id) || !exactItems.TryGetValue(i.id, out var value) || (Object)(object)value == (Object)null)
		{
			return;
		}
		Hand val = ((i.pouchHand == 0) ? LeftHand : RightHand);
		Grip val2 = null;
		foreach (Grip componentsInChild in ((Component)value).GetComponentsInChildren<Grip>())
		{
			if ((Object)(object)componentsInChild != (Object)null && ((Object)componentsInChild).name == i.pouchGrip)
			{
				val2 = componentsInChild;
			}
		}
		bool flag = (Object)(object)val != (Object)null && (Object)(object)val2 != (Object)null && (Object)(object)val.m_CurrentAttachedGO == (Object)null && Grab(val, val2);
		Lab.Event("exact: " + ((Object)((Component)value).gameObject).name + " put in its " + ((i.pouchHand == 0) ? "left" : "right") + " hand" + ((i.spawnT < 0f) ? " (yours as you began)" : " (from your pouch)") + (flag ? "" : ": NOT taken"));
	}

	private bool ExactAt(float t, out int a, out int b, out float k)
	{
		List<ExactClone.Frame> frames = exactTake.frames;
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
				Hand val = ((item.pouchHand == 0) ? LeftHand : RightHand);
				if (item.spawnT < 0f && item.pouch && (Object)(object)val != (Object)null)
				{
					ExactSpawn(item, Pose.Of(((Component)val).transform) * (item.handAt.Inverse() * item.at));
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
				Hand val2 = ((item4.pouchHand == 0) ? LeftHand : RightHand);
				if ((Object)(object)val2 != (Object)null)
				{
					at = Pose.Of(((Component)val2).transform) * (pose.Inverse() * item4.at);
				}
			}
			ExactSpawn(item4, at);
		}
		foreach (ExactClone.Item item5 in take.items)
		{
			if (item5.pouch && !exactPouched.Contains(item5.id) && !(exactTime < item5.pouchT) && exactItems.TryGetValue(item5.id, out var value) && !((Object)(object)value == (Object)null))
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
		if (exactTake == null || (Object)(object)controls == (Object)null || (Object)(object)controls.vrRoot == (Object)null)
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
			Rig physicsRig = (Rig)(object)rig.physicsRig;
			Vector3 val = (((Object)(object)physicsRig != (Object)null && (Object)(object)physicsRig.m_pelvis != (Object)null) ? (Vector3.Lerp(frame.pelvis, frame2.pelvis, k) - physicsRig.m_pelvis.position) : Vector3.zero);
			val.y = 0f;
			if (((Vector3)(ref val)).magnitude > Tuning.F("exact.putback", 1.5f))
			{
				if (Time.time - exactPutBackAt > 2f)
				{
					exactPutBackAt = Time.time;
					Vector3 val2 = (((Object)(object)physicsRig.centerOfPressure != (Object)null) ? physicsRig.centerOfPressure.position : vrRoot.position);
					rig.Teleport(val2 + val, true);
					Lab.Event("exact: put back, " + ((Vector3)(ref val)).magnitude.ToString("0.0") + " m off your way");
				}
			}
			else if (((Vector3)(ref val)).magnitude > Tuning.F("exact.ease.from", 0.02f))
			{
				float num = Tuning.F("exact.ease.most", 0.6f) * Time.deltaTime;
				vrRoot.position += Vector3.ClampMagnitude(val * Mathf.Clamp01(Time.deltaTime * Tuning.F("exact.ease", 3f)), num);
			}
		}
		Pose obj = Pose.Of(vrRoot);
		Pose pose = obj * Pose.Lerp(frame.head, frame2.head, k);
		Pose pose2 = obj * Pose.Lerp(frame.left, frame2.left, k);
		Pose pose3 = obj * Pose.Lerp(frame.right, frame2.right, k);
		controls.headset.SetPositionAndRotation(pose.position, pose.rotation);
		((Component)((ControllerRig)controls).leftController).transform.SetPositionAndRotation(pose2.position, pose2.rotation);
		((Component)((ControllerRig)controls).rightController).transform.SetPositionAndRotation(pose3.position, pose3.rotation);
		if (exactReady)
		{
			ExactClone.Put(((ControllerRig)controls).leftController, take, exactPulsedTo, a, b, k, left: true);
			ExactClone.Put(((ControllerRig)controls).rightController, take, exactPulsedTo, a, b, k, left: false);
			exactPulsedTo = a;
			if (Tuning.F("exact.turnstick", 0f) < 0.5f)
			{
				Vector2 thumbstickAxis = ((ControllerRig)controls).rightController._thumbstickAxis;
				((ControllerRig)controls).rightController._thumbstickAxis = new Vector2(0f, thumbstickAxis.y);
			}
			exactMove = Vector2.Lerp(frame.move, frame2.move, k);
			exactMoving = frame.moving;
		}
		else
		{
			ExactClone.Put(((ControllerRig)controls).leftController, take, 0, 0, 0, 0f, left: true);
			ExactClone.Put(((ControllerRig)controls).rightController, take, 0, 0, 0, 0f, left: false);
			BaseController leftController = ((ControllerRig)controls).leftController;
			float solvedGrip = (((ControllerRig)controls).rightController._solvedGrip = 0f);
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
		if (mode == Mode.Exact && exactReady && !((Object)(object)grip == (Object)null))
		{
			int num = ((!((Object)(object)hand == (Object)(object)LeftHand)) ? (((Object)(object)hand == (Object)(object)RightHand) ? 1 : (-1)) : 0);
			if (num >= 0)
			{
				Poolee componentInParent = ((Component)grip).GetComponentInParent<Poolee>();
				exactDid.Add(new ExactClone.Grab
				{
					t = ExactTime,
					hand = num,
					item = (((Object)(object)componentInParent != (Object)null) ? (-2) : (-1)),
					thing = (((Object)(object)componentInParent != (Object)null) ? ((Il2CppObjectBase)(object)componentInParent).Pointer : IntPtr.Zero),
					grip = ((Object)grip).name,
					on = on
				});
			}
		}
	}

	private void ExactCheckGrabs(float t, bool end)
	{
		List<ExactClone.Grab> grabs = exactTake.grabs;
		float num = Tuning.F("exact.grab.window", 0.3f);
		foreach (ExactClone.Grab item in exactDid)
		{
			if (item.item != -2 || item.thing == IntPtr.Zero)
			{
				continue;
			}
			foreach (KeyValuePair<int, Poolee> exactItem in exactItems)
			{
				if ((Object)(object)exactItem.Value != (Object)null && ((Il2CppObjectBase)(object)exactItem.Value).Pointer == item.thing)
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
			if (!exactItems.TryGetValue(id, out var value) || !((Object)(object)value != (Object)null))
			{
				return "thing " + id;
			}
			return ((Object)((Component)value).gameObject).name;
		}
		}
	}

	private void ExactMeasure(float t)
	{
		Rig physicsRig = (Rig)(object)rig.physicsRig;
		if (!((Object)(object)physicsRig == (Object)null) && !((Object)(object)physicsRig.m_pelvis == (Object)null) && !((Object)(object)LeftHand == (Object)null) && !((Object)(object)RightHand == (Object)null))
		{
			ExactAt(t, out var a, out var b, out var k);
			ExactClone.Frame frame = exactTake.frames[a];
			ExactClone.Frame frame2 = exactTake.frames[b];
			Vector3 position = physicsRig.m_pelvis.position;
			Vector3 val = Vector3.Lerp(frame.pelvis, frame2.pelvis, k);
			Vector3 val2 = position - val;
			float magnitude = ((Vector3)(ref val2)).magnitude;
			Vector3 val3 = Vector3.Lerp(frame.leftHand.position, frame2.leftHand.position, k) - val;
			Vector3 val4 = Vector3.Lerp(frame.rightHand.position, frame2.rightHand.position, k) - val;
			val2 = ((Component)LeftHand).transform.position - position - val3;
			float magnitude2 = ((Vector3)(ref val2)).magnitude;
			val2 = ((Component)RightHand).transform.position - position - val4;
			float num = Mathf.Max(magnitude2, ((Vector3)(ref val2)).magnitude);
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
			if ((Object)(object)controls != (Object)null)
			{
				BaseController leftController = ((ControllerRig)controls).leftController;
				float solvedGrip = (((ControllerRig)controls).rightController._solvedGrip = 0f);
				leftController._solvedGrip = solvedGrip;
				BaseController leftController2 = ((ControllerRig)controls).leftController;
				solvedGrip = (((ControllerRig)controls).rightController._primaryAxis = 0f);
				leftController2._primaryAxis = solvedGrip;
				BaseController leftController3 = ((ControllerRig)controls).leftController;
				bool primaryInteractionButton = (((ControllerRig)controls).rightController._primaryInteractionButton = false);
				leftController3._primaryInteractionButton = primaryInteractionButton;
			}
		}
		catch (Exception)
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
		bool flag = !moving && HandsFree && Time.time >= waveUntil && waveDue < 0f && !falling && (Object)(object)nearestSeen.transform == (Object)null && (Object)(object)brainTarget.transform == (Object)null && Time.time - flinchAt > 0.6f && Time.time - heardTime > 1.5f && Time.time - landAt > 1.2f && (mode == Mode.Stand || mode == Mode.Follow || mode == Mode.Wander || mode == Mode.Brain);
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
			Vector3 val2 = P(num3 * 0.13f, -0.36f, 0.3f);
			Quaternion turn = Quaternion.Euler(-35f, (0f - num3) * 15f, num3 * (-70f + Mathf.Sin(num * 1.9f) * 75f));
			OneHand(num3, val2, turn, num2);
			if ((Object)(object)vrRoot != (Object)null && fidLevel > 0.5f)
			{
				Look(vrRoot.TransformPoint(val2), null, "its hand", 0.3f);
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
			Vector3 val = P(0.24f + 0.3f * num4, 0.4f - 0.3f * num4, 0.02f - 0.1f * num4);
			pose.rightPos = Vector3.Lerp(pose.rightPos, val, num2);
			pose.leftPos = Vector3.Lerp(pose.leftPos, new Vector3(0f - val.x, val.y, val.z), num2);
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
			Vector3 val3 = new Vector3(Mathf.Sin(num * 44f) * 0.03f, 0.1f + Mathf.Cos(num * 39f) * 0.03f, 0.08f) * (num2 * i);
			ref Vector3 rightPos = ref pose.rightPos;
			rightPos += val3;
			ref Vector3 leftPos = ref pose.leftPos;
			leftPos += new Vector3(0f - val3.x, val3.y, val3.z);
			curl = Mathf.Lerp(curl, 0.05f, num2);
			break;
		}
		case Fid.Down:
			if ((Object)(object)vrRoot != (Object)null && fidLevel > 0.3f && flag2)
			{
				Look(vrRoot.TransformPoint(new Vector3(num3 * 0.2f * i, 0f, 1.1f * i)), null, "the ground", 0.3f);
			}
			break;
		case Fid.Behind:
			if ((Object)(object)vrRoot != (Object)null && flag2)
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
		float num3 = Random.value * num;
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
			fidSide = ((Random.value < 0.75f == flag) ? (-1f) : 1f);
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
		if ((Object)(object)nearestSeen.transform == (Object)null || nearestSeenDistance > 30f)
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
		Vector3 val = Flat(point - head.position);
		float magnitude = ((Vector3)(ref val)).magnitude;
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
		Vector3 val2 = new Vector3(point.x, point.y - 1.3f, point.z) + ((magnitude > 4f) ? OwnSide(1.3f) : Vector3.zero);
		if (nav.HasGoal)
		{
			val = nav.Goal - val2;
			if (!(((Vector3)(ref val)).magnitude > 1.2f) && !(Time.time - chaseSetAt > 2f))
			{
				goto IL_010a;
			}
		}
		nav.SetGoal(val2);
		chaseSetAt = Time.time;
		goto IL_010a;
		IL_010a:
		running = magnitude > 2.5f;
		Travel(running ? MotionLibrary.RunPush : MotionLibrary.WalkPush, 0.8f, dt);
		return true;
	}

	private float CloseIn(Transform head, Vector3 point, float reach, float dt)
	{
		Face(point, dt, 320f);
		Vector3 val = Flat(point - head.position);
		float magnitude = ((Vector3)(ref val)).magnitude;
		Vector3 val2 = ((((Vector3)(ref val)).sqrMagnitude > 0.001f) ? ((Vector3)(ref val)).normalized : (Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward));
		Vector3 val3 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * val2;
		if (magnitude > reach * 0.85f)
		{
			running = magnitude > 3f;
			float speed = (running ? MotionLibrary.RunPush : MotionLibrary.WalkPush);
			Vector3 val4 = KeepApart(val2, ref speed);
			val3 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * val4;
			walk = new Vector2(val3.x, val3.z) * speed;
		}
		else
		{
			walk = ((magnitude < reach * 0.4f) ? (-new Vector2(val3.x, val3.z) * MotionLibrary.WalkPush * 0.6f) : Vector2.zero);
		}
		Vector3 val5 = Quaternion.Inverse(Quaternion.Euler(0f, bodyYaw, 0f)) * (point - head.position);
		float y = val5.y;
		Vector2 val6 = new Vector2(val5.x, val5.z);
		playPitch = Mathf.Clamp((0f - Mathf.Atan2(y, Mathf.Max(((Vector2)(ref val6)).magnitude, 0.1f))) * 57.29578f, -40f, 50f);
		playYaw = Mathf.Clamp(Mathf.Atan2(val5.x, val5.z) * 57.29578f, -40f, 40f);
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
			Rigidbody val = Pelvis();
			if ((Object)(object)rightHand != (Object)null && (Object)(object)rightHand.rb != (Object)null && (Object)(object)leftHand != (Object)null && (Object)(object)leftHand.rb != (Object)null && (Object)(object)val != (Object)null)
			{
				float num2 = hitFast;
				Vector3 val2 = rightHand.rb.velocity - val.velocity;
				float magnitude = ((Vector3)(ref val2)).magnitude;
				val2 = leftHand.rb.velocity - val.velocity;
				hitFast = Mathf.Max(num2, Mathf.Max(magnitude, ((Vector3)(ref val2)).magnitude));
				float num3 = hitSpin;
				val2 = rightHand.rb.angularVelocity;
				float magnitude2 = ((Vector3)(ref val2)).magnitude;
				val2 = leftHand.rb.angularVelocity;
				hitSpin = Mathf.Max(num3, Mathf.Max(magnitude2, ((Vector3)(ref val2)).magnitude));
				float num4 = hitBody;
				val2 = val.velocity;
				hitBody = Mathf.Max(num4, ((Vector3)(ref val2)).magnitude);
			}
		}
		catch (Exception)
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
		float num5 = ((frame.height > 0.3f) ? (playHeight / frame.height) : 1f);
		Quaternion val3 = Quaternion.Euler(playPitch, playYaw, 0f);
		Vector3 val4 = default(Vector3);
		((Vector3)(ref val4))._002Ector(0f, playHeight, 0f);
		Vector3 val5 = default(Vector3);
		((Vector3)(ref val5))._002Ector(0f, frame.height * num5, 0f);
		float num6 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(since, num - since) / 0.15f));
		handAt = Vector3.Lerp(handAt, val4 + val3 * (frame.rightPos * num5 - val5), num6);
		handTurn = Quaternion.Slerp(handTurn, (play == Play.Melee) ? (val3 * frame.rightRot * ToolFix) : (val3 * frame.rightRot), num6);
		leftAt = Vector3.Lerp(leftAt, val4 + val3 * (frame.leftPos * num5 - val5), num6);
		leftTurn = Quaternion.Slerp(leftTurn, val3 * frame.leftRot, num6);
		if (fist)
		{
			float num7 = Mathf.Max(Time.deltaTime, 0.001f);
			handTurn = Lined(handTurn, (handAt - hitRightBefore) / num7, fistOffRight);
			leftTurn = Lined(leftTurn, (leftAt - hitLeftBefore) / num7, fistOffLeft);
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
			Quaternion val = Quaternion.Euler(0f, bodyYaw, 0f);
			Hand rightHand = RightHand;
			Hand leftHand = LeftHand;
			Vector3 angularVelocity;
			if ((Object)(object)rightHand != (Object)null && (Object)(object)rightHand.rb != (Object)null)
			{
				angularVelocity = rightHand.rb.angularVelocity;
				if (((Vector3)(ref angularVelocity)).magnitude < 2f)
				{
					fistOffRight = Quaternion.Slerp(fistOffRight, Quaternion.Inverse(val * handTurn) * ((Component)rightHand).transform.rotation, 0.1f);
				}
			}
			if ((Object)(object)leftHand != (Object)null && (Object)(object)leftHand.rb != (Object)null)
			{
				angularVelocity = leftHand.rb.angularVelocity;
				if (((Vector3)(ref angularVelocity)).magnitude < 2f)
				{
					fistOffLeft = Quaternion.Slerp(fistOffLeft, Quaternion.Inverse(val * leftTurn) * ((Component)leftHand).transform.rotation, 0.1f);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private Quaternion Lined(Quaternion turn, Vector3 going, Quaternion sits)
	{
		float magnitude = ((Vector3)(ref going)).magnitude;
		if (magnitude < Tuning.F("punch.line.from", 1.2f) || going.z < 0.3f)
		{
			return turn;
		}
		Vector3 val = going / magnitude;
		float num = Mathf.Clamp01((magnitude - Tuning.F("punch.line.from", 1.2f)) / 2.5f) * Tuning.F("punch.line", 0f);
		Quaternion val2 = turn * sits;
		Quaternion val3 = Quaternion.FromToRotation(val2 * Vector3.up, val) * val2 * Quaternion.Inverse(sits);
		return Quaternion.Slerp(turn, val3, num);
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
		if ((Object)(object)t.transform == (Object)null || (Object)(object)head == (Object)null)
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
				comboLeft = ((Random.value < 0.3f) ? 1 : ((Random.value < 0.6f) ? 2 : 3));
			}
			comboLeft--;
			nextPunchAt = Time.time + num3 + ((comboLeft > 0) ? 0.05f : Random.Range(0.7f, 1.5f));
		}
		float num4 = Time.time - punchAt;
		if (!HitOfTheirs(num4, num2, fist: true, point) && num4 < 0.32f)
		{
			float num5 = ((num4 < 0.12f) ? (num4 / 0.12f) : Mathf.Clamp01(1f - (num4 - 0.12f) / 0.2f));
			Vector3 val = controls.vrRoot.InverseTransformPoint(point);
			if (punchRight)
			{
				handAt = Vector3.Lerp(handAt, val, Mathf.SmoothStep(0f, 1f, num5));
				handTurn = Quaternion.Euler(-5f, 0f, -80f);
			}
			else
			{
				leftAt = Vector3.Lerp(leftAt, val, Mathf.SmoothStep(0f, 1f, num5));
				leftTurn = Quaternion.Euler(-5f, 0f, 80f);
			}
		}
	}

	private static bool IsMelee(Rigidbody rb)
	{
		try
		{
			Poolee componentInParent = ((Component)rb).GetComponentInParent<Poolee>();
			if ((Object)(object)componentInParent == (Object)null || (Object)(object)componentInParent.SpawnableCrate == (Object)null)
			{
				return false;
			}
			if ((Object)(object)((Component)componentInParent).GetComponentInChildren<Gun>() != (Object)null || (Object)(object)((Component)componentInParent).GetComponent<Magazine>() != (Object)null)
			{
				return false;
			}
			List<string> tags = ((Crate)componentInParent.SpawnableCrate).Tags;
			if (tags != null)
			{
				Enumerator<string> enumerator = tags.GetEnumerator();
				while (enumerator.MoveNext())
				{
					string text = (enumerator.Current ?? "").ToLowerInvariant();
					if (text.Contains("melee") || text.Contains("blade") || text.Contains("blunt"))
					{
						return true;
					}
				}
			}
			if ((Object)(object)((Component)componentInParent).GetComponentInChildren<StabSlash>() != (Object)null)
			{
				return true;
			}
			string text2 = (((Object)((Component)componentInParent).gameObject).name + " " + ((Scannable)componentInParent.SpawnableCrate).Barcode.ID).ToLowerInvariant();
			string[] array = meleeWords;
			foreach (string value in array)
			{
				if (text2.Contains(value))
				{
					return true;
				}
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	private static Grip HandleOf(Rigidbody rb)
	{
		MarrowEntity componentInParent = ((Component)rb).GetComponentInParent<MarrowEntity>();
		object obj = (((Object)(object)componentInParent != (Object)null) ? ((object)componentInParent) : ((object)rb));
		Grip result = null;
		float num = float.MinValue;
		foreach (Grip componentsInChild in ((Component)obj).GetComponentsInChildren<Grip>(false))
		{
			if (!((Object)(object)componentsInChild == (Object)null) && ((Behaviour)componentsInChild).enabled && !componentsInChild.IsInteractionDisabled && !componentsInChild.HasAttachedHands() && !((Object)(object)((Il2CppObjectBase)(object)componentsInChild).TryCast<WorldGrip>() != (Object)null))
			{
				string text = ((Object)componentsInChild).name.ToLowerInvariant();
				Vector3 val = ((Component)componentsInChild).transform.position - rb.worldCenterOfMass;
				float num2 = ((Vector3)(ref val)).magnitude;
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
		if ((Object)(object)head == (Object)null)
		{
			return null;
		}
		Vector3 val = Feet;
		Rigidbody result = null;
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(val + Vector3.up * 0.5f, Tuning.F("melee.seek", 22f), -1, (QueryTriggerInteraction)1))
		{
			Rigidbody val2 = (((Object)(object)item != (Object)null) ? item.attachedRigidbody : null);
			if ((Object)(object)val2 == (Object)null || val2.isKinematic || !hashSet.Add(((Il2CppObjectBase)(object)val2).Pointer) || val2.mass < 0.25f || val2.mass > 14f || (Object)(object)((Component)val2).GetComponentInParent<RigManager>() != (Object)null)
			{
				continue;
			}
			float num = val2.worldCenterOfMass.y - val.y;
			Vector3 val3 = val2.worldCenterOfMass - val;
			float magnitude = ((Vector3)(ref val3)).magnitude;
			if (!(num < -0.5f) && !(num > 1.6f) && !(magnitude >= far) && IsMelee(val2))
			{
				Grip val4 = HandleOf(val2);
				if (!((Object)(object)val4 == (Object)null) && !HeldByAnyone(((Object)(object)((Component)val2).transform.root == (Object)(object)((Component)val2).transform) ? ((Component)val2).transform : (((Object)(object)((Component)val2).GetComponentInParent<Poolee>() != (Object)null) ? ((Component)((Component)val2).GetComponentInParent<Poolee>()).transform : ((Component)val2).transform), null) && Targets.InSight(head.position, val2.worldCenterOfMass, ((Component)val2).transform, rig))
				{
					result = val2;
					itsGrip = val4;
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
		Rigidbody val = FindMelee(out itsGrip, out far);
		float far2;
		Gun val2 = NearestGun(out far2);
		if ((Object)(object)val != (Object)null && ((Object)(object)val2 == (Object)null || far < far2 * 0.6f))
		{
			item = val;
			grip = itsGrip;
			gun = null;
			support = null;
			meleeWanted = true;
			tinkering = false;
			play = Play.GoTo;
			playAt = Time.time;
			nav.SetGoal(item.worldCenterOfMass);
			Lab.Event("weapon: going for the " + ThingName(val) + " (" + far.ToString("0") + " m) to hit with");
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
			List<string> list = new List<string>();
			FieldInfo[] fields = typeof(Melee).GetFields(BindingFlags.Static | BindingFlags.Public);
			foreach (FieldInfo fieldInfo in fields)
			{
				if (!(fieldInfo.FieldType != typeof(string)))
				{
					list.Add(fieldInfo.Name);
					if (string.Equals(fieldInfo.Name, which, StringComparison.OrdinalIgnoreCase))
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
		Vector3 val = Feet + Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward * 3f + Vector3.up * 0.4f;
		Clone.Suppress();
		HelperMethods.SpawnCrate(text, val, Quaternion.identity, Vector3.one, false, (Action<GameObject>)null, (Action<GameObject>)null);
		Lab.Event("lay: a " + which + " put 3 m in front of it");
	}

	public void LabArm()
	{
		if ((Object)(object)gun != (Object)null)
		{
			wontLoad[((Il2CppObjectBase)(object)gun).Pointer] = Time.time;
		}
		if ((Object)(object)ownGun != (Object)null)
		{
			wontLoad[((Il2CppObjectBase)(object)ownGun).Pointer] = Time.time;
		}
		StopPlaying();
		ownGun = null;
		if (mode != Mode.Brain)
		{
			SetMode(Mode.Brain);
		}
		Grip itsGrip;
		float far;
		Rigidbody val = FindMelee(out itsGrip, out far);
		if ((Object)(object)val == (Object)null)
		{
			Lab.Event("weapon: nothing to hit with in sight");
			return;
		}
		item = val;
		grip = itsGrip;
		gun = null;
		support = null;
		meleeWanted = true;
		play = Play.GoTo;
		playAt = Time.time;
		nav.SetGoal(item.worldCenterOfMass);
		Lab.Event("weapon: going for the " + ThingName(val) + " (" + far.ToString("0") + " m) to hit with");
	}

	private static Quaternion ToolTurn(Vector3 points, Vector3 forearm)
	{
		return Quaternion.LookRotation(-((Vector3)(ref points)).normalized, ((Vector3)(ref forearm)).normalized);
	}

	private Quaternion Pointing(Vector3 points, Vector3 forearm)
	{
		try
		{
			if (toolTurnHas && (Object)(object)item != (Object)null && (Object)(object)grip != (Object)null && Holding())
			{
				Vector3 val = item.angularVelocity;
				if (((Vector3)(ref val)).magnitude < 3f)
				{
					Vector3 val2 = Quaternion.Inverse(Quaternion.Euler(0f, bodyYaw, 0f)) * (item.worldCenterOfMass - ((Component)grip).transform.position);
					if (((Vector3)(ref val2)).sqrMagnitude > 0.0004f)
					{
						Vector3 val3 = Quaternion.Inverse(toolTurnWas) * ((Vector3)(ref val2)).normalized;
						val = Vector3.Slerp(toolLocal, val3, Tuning.F("melee.fix", 0.12f));
						toolLocal = ((Vector3)(ref val)).normalized;
					}
				}
			}
		}
		catch (Exception)
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
			Vector3 position = ((Component)grip).transform.position;
			foreach (Collider componentsInChild in ((Component)item).GetComponentsInChildren<Collider>())
			{
				if (!((Object)(object)componentsInChild == (Object)null) && !componentsInChild.isTrigger)
				{
					Bounds bounds = componentsInChild.bounds;
					float num2 = num;
					Vector3 val = ((Bounds)(ref bounds)).min - position;
					float magnitude = ((Vector3)(ref val)).magnitude;
					val = ((Bounds)(ref bounds)).max - position;
					num = Mathf.Max(num2, Mathf.Max(magnitude, ((Vector3)(ref val)).magnitude));
				}
			}
		}
		catch (Exception)
		{
		}
		meleeReach = 0.75f + Mathf.Clamp(num, 0.2f, 1.6f) * 0.8f;
		meleeSeenAt = Time.time;
		toolLocal = Vector3.back;
		toolTurnHas = false;
		hitClip = null;
		punchAt = -10f;
		Lab.Event("weapon: has the " + ThingName(item) + " to hit with, by its " + (((Object)(object)grip != (Object)null) ? ((Object)grip).name : "?") + " (reaches " + meleeReach.ToString("0.0") + " m)");
	}

	private bool MeleeStep(Transform you, Transform head, float dt)
	{
		if (!Holding() || (Object)(object)head == (Object)null)
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
		if ((Object)(object)target.transform == (Object)null || nearestSeenDistance > 30f)
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
			nextPunchAt = Time.time + ((hitClip != null) ? hitClip[hitClip.Length - 1].time : 0.55f) + Random.Range(0.25f, 0.7f);
		}
		float num2 = Time.time - punchAt;
		if (HitOfTheirs(num2))
		{
			return true;
		}
		if (num2 < 0.55f)
		{
			float num3 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(num2 / 0.2f));
			float num4 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((num2 - 0.2f) / 0.14f));
			float num5 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((num2 - 0.36f) / 0.19f));
			Vector3 val = handAt;
			Vector3 val2 = default(Vector3);
			((Vector3)(ref val2))._002Ector(0.28f, playHeight + 0.12f, -0.05f);
			Vector3 val3 = default(Vector3);
			((Vector3)(ref val3))._002Ector(0.14f, playHeight - 0.45f, 0.58f);
			Vector3 val4 = Vector3.Lerp(Vector3.Lerp(val, val2, num3), val3, num4);
			Quaternion val5 = Quaternion.Slerp(Quaternion.Slerp(handTurn, ToolTurn(new Vector3(0f, 0.6f, -0.8f), new Vector3(0f, 0.8f, 0.6f)) * ToolFix, num3), ToolTurn(new Vector3(0f, -0.25f, 0.97f), new Vector3(0f, -0.8f, 0.6f)) * ToolFix, num4);
			handAt = Vector3.Lerp(val4, val, num5);
			handTurn = Quaternion.Slerp(val5, handTurn, num5);
		}
		return true;
	}

	public bool Holds(Gun g)
	{
		if ((Object)(object)gun != (Object)null && (Object)(object)g != (Object)null)
		{
			return ((Il2CppObjectBase)(object)gun).Pointer == ((Il2CppObjectBase)(object)g).Pointer;
		}
		return false;
	}

	public void Heard(Vector3 at)
	{
		Transform head = Head;
		if (!((Object)(object)head == (Object)null))
		{
			Vector3 val = at - head.position;
			float magnitude = ((Vector3)(ref val)).magnitude;
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
		if ((Object)(object)head == (Object)null || handOverride || Playing || climbing || (Object)(object)brainTarget.transform != (Object)null)
		{
			return;
		}
		if (Time.time >= gazeUntil)
		{
			ChooseGaze(you, head, moving);
		}
		Vector3 val = (((Object)(object)gazeFollow != (Object)null) ? gazeFollow.position : gazeAt);
		if (gazeOn == "ahead" && moving)
		{
			val = (nav.HasGoal ? (nav.LookAhead(Feet, 4f) + Vector3.up * (head.position.y - Feet.y - 0.15f)) : (head.position + Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward * 4f));
		}
		Vector3 val2 = val - head.position;
		Vector3 val3 = Flat(val2);
		float magnitude = ((Vector3)(ref val3)).magnitude;
		float num = Mathf.Clamp(Mathf.DeltaAngle(bodyYaw, Recorder.Yaw(val2)), -80f, 80f);
		float num2 = Mathf.Clamp((0f - Mathf.Atan2(val2.y, Mathf.Max(magnitude, 0.1f))) * 57.29578f, -35f, 45f);
		float num3 = Tuning.F("gaze.quick", 9f);
		gazeYaw = Mathf.SmoothDamp(gazeYaw, num, ref gazeYawSpeed, 1f / num3, 400f, dt);
		gazePitch = Mathf.SmoothDamp(gazePitch, num2, ref gazePitchSpeed, 1f / num3, 300f, dt);
		float num4 = Tuning.F("gaze.drift", 1.5f);
		float num5 = gazeYaw + (Mathf.PerlinNoise(Time.time * 0.6f, born) - 0.5f) * 2f * num4;
		float num6 = gazePitch + (Mathf.PerlinNoise(born, Time.time * 0.5f) - 0.5f) * 2f * num4;
		if (!moving && Mathf.Abs(gazeYaw) > 55f)
		{
			if (gazeFarSince < 0f)
			{
				gazeFarSince = Time.time;
			}
			if (Time.time - gazeFarSince > 0.6f)
			{
				float num7 = Mathf.Sign(gazeYaw) * Mathf.Min(Mathf.Abs(gazeYaw) - 20f, 90f * dt);
				bodyYaw += num7;
				gazeYaw -= num7;
			}
		}
		else
		{
			gazeFarSince = -1f;
		}
		pose.head = Quaternion.Euler(num6, num5, 0f) * Quaternion.Slerp(Quaternion.identity, pose.head, 0.25f);
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
		List<(Vector3, Transform, string, float)> list = new List<(Vector3, Transform, string, float)>();
		if (moving)
		{
			list.Add((Vector3.zero, null, "ahead", 3f));
		}
		Vector3 val;
		if ((Object)(object)you != (Object)null)
		{
			val = you.position - head.position;
			float magnitude = ((Vector3)(ref val)).magnitude;
			if (magnitude < 40f)
			{
				list.Add((you.position, you, "you", ((magnitude < 8f) ? 2.2f : 1f) / (1f + magnitude / 20f) * (moving ? 0.4f : 1f)));
			}
		}
		foreach (Bot item2 in BotManager.All)
		{
			if (item2 != this && !((Object)(object)item2.Head == (Object)null))
			{
				val = item2.Head.position - head.position;
				float magnitude2 = ((Vector3)(ref val)).magnitude;
				if (magnitude2 < 25f)
				{
					list.Add((item2.Head.position, item2.Head, "a bot", 0.7f / (1f + magnitude2 / 10f)));
				}
			}
		}
		foreach (Collider item3 in Physics.OverlapSphere(head.position, 14f, -1, (QueryTriggerInteraction)1))
		{
			Rigidbody val2 = (((Object)(object)item3 != (Object)null) ? item3.attachedRigidbody : null);
			if (!((Object)(object)val2 == (Object)null) && !val2.isKinematic)
			{
				val = val2.velocity;
				if (!(((Vector3)(ref val)).magnitude < 1.5f) && !((Object)(object)((Component)val2).GetComponentInParent<RigManager>() != (Object)null))
				{
					list.Add((val2.worldCenterOfMass, ((Component)val2).transform, "something moving", 2f));
					break;
				}
			}
		}
		Vector3 item = head.position + Quaternion.Euler(Random(-8f, 18f), bodyYaw + Random(-110f, 110f), 0f) * Vector3.forward * Random(5f, 15f);
		list.Add((item, null, "about", moving ? 0.3f : 1.1f));
		float num2 = 0f;
		foreach (var item4 in list)
		{
			num2 += item4.Item4;
		}
		float num3 = Random.value * num2;
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
		if ((Object)(object)gun == (Object)null || Tuning.F("learned.hold", 1f) < 0.5f)
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
		Transform val = (((Object)(object)gun != (Object)null) ? gun.firePointTransform : null);
		if (hold == null || (Object)(object)val == (Object)null || (Object)(object)head == (Object)null)
		{
			return false;
		}
		Quaternion val2 = Quaternion.Euler(0f, bodyYaw, 0f);
		Pose pose = GunMoves.U(hold.right, Pose.identity);
		Vector3 val3 = (aimed ? ChestHold() : Vector3.zero);
		ref Vector3 position = ref pose.position;
		position += val3;
		Quaternion val4 = GunMoves.UQ(hold.muzzle, Quaternion.identity);
		Vector3 val6;
		float pitch;
		float yaw;
		if (aimed)
		{
			Quaternion val5 = Quaternion.Inverse(val2);
			val6 = point - val.position;
			Vector3 val7 = val5 * ((Vector3)(ref val6)).normalized;
			Vector3 val8 = val4 * Vector3.forward;
			bool flag = lockSet && (Object)(object)item != (Object)null;
			if (flag)
			{
				Quaternion val9 = Quaternion.Inverse(item.rotation) * val.rotation;
				val8 = pose.rotation * (gunInController.rotation * val9) * Vector3.forward;
			}
			if (!SolveLook(((Vector3)(ref val8)).normalized, val7, out pitch, out yaw))
			{
				Quaternion val10 = Quaternion.LookRotation(val7, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(val8, Vector3.up));
				Vector3 eulerAngles = ((Quaternion)(ref val10)).eulerAngles;
				pitch = Mathf.DeltaAngle(0f, eulerAngles.x);
				yaw = Mathf.DeltaAngle(0f, eulerAngles.y);
			}
			pitch += aimFix.x;
			yaw += aimFix.y;
			if (Time.time - aimSaidAt > 15f)
			{
				aimSaidAt = Time.time;
				Vector3 v = Quaternion.Inverse(val2) * val.forward;
				Lab.Event("aim: wants pitch " + PitchOf(val7).ToString("0") + " yaw " + YawOf(val7).ToString("0") + ", barrel at pitch " + PitchOf(v).ToString("0") + " yaw " + YawOf(v).ToString("0") + "; head asked " + pitch.ToString("0") + "/" + yaw.ToString("0") + " (has " + aimPitchNow.ToString("0") + "/" + aimYawNow.ToString("0") + "), fix " + aimFix.x.ToString("0.0") + "/" + aimFix.y.ToString("0.0") + ", barrel in head " + PitchOf(val8).ToString("0") + "/" + YawOf(val8).ToString("0") + (flag ? " (known)" : " (learned)") + ", step " + step);
			}
			int num;
			if (!((Object)(object)item == (Object)null))
			{
				val6 = item.angularVelocity;
				if (((Vector3)(ref val6)).magnitude < Tuning.F("hold.steady.spin", 1.5f) * 2.5f)
				{
					val6 = item.velocity;
					num = ((((Vector3)(ref val6)).magnitude < Tuning.F("hold.steady.speed", 0.8f) * 2f) ? 1 : 0);
				}
				else
				{
					num = 0;
				}
			}
			else
			{
				num = 1;
			}
			if (Mathf.Abs(aimFix.y) > 30f || Mathf.Abs(aimFix.x) > 30f)
			{
				aimFix = Vector2.zero;
			}
			if (num != 0 && Time.time - firedAt > 0.35f)
			{
				Vector3 v2 = Quaternion.Inverse(val2) * val.forward;
				float num2 = Mathf.Clamp01(Time.deltaTime * Tuning.F("learned.aimfix.rate", 2.5f) * (GunSteady() ? 1f : 0.4f));
				float num3 = Tuning.F("learned.aimfix.most", 25f);
				aimFix.x = Mathf.Clamp(aimFix.x + num2 * (PitchOf(val7) - PitchOf(v2)), 0f - num3, num3);
				aimFix.y = Mathf.Clamp(aimFix.y + num2 * Mathf.DeltaAngle(YawOf(v2), YawOf(val7)), 0f - num3, num3);
			}
		}
		else
		{
			pitch = 15f;
			yaw = 0f;
		}
		pitch = Mathf.Clamp(pitch, -60f, 70f);
		yaw = Mathf.Clamp(yaw, -60f, 60f);
		float num4 = 6f;
		float num5 = 0f;
		if (aimed)
		{
			Quaternion val11 = Quaternion.Inverse(val2);
			val6 = point - head.position;
			Vector3 v3 = val11 * ((Vector3)(ref val6)).normalized;
			num4 = Mathf.Clamp(PitchOf(v3), -40f, 40f);
			num5 = Mathf.Clamp(YawOf(v3), -50f, 50f);
		}
		float num6 = Mathf.Clamp01(Time.deltaTime * Tuning.F("learned.look.ease", 8f));
		aimPitchNow = Mathf.LerpAngle(aimPitchNow, pitch, num6);
		aimYawNow = Mathf.LerpAngle(aimYawNow, yaw, num6);
		lookPitchNow = Mathf.LerpAngle(lookPitchNow, num4, num6);
		lookYawNow = Mathf.LerpAngle(lookYawNow, num5, num6);
		playPitch = lookPitchNow;
		playYaw = lookYawNow;
		playHeight = standingHeight * 0.985f;
		Quaternion val12 = Quaternion.Euler(aimPitchNow, aimYawNow, 0f);
		Vector3 val13 = default(Vector3);
		((Vector3)(ref val13))._002Ector(0f, playHeight, 0f);
		handOverride = true;
		learnedHoldAt = Time.time;
		handAt = val13 + val12 * pose.position;
		handTurn = val12 * pose.rotation;
		float num7 = Time.time - firedAt;
		if (lockSet && num7 >= 0f && num7 < 0.4f)
		{
			float num8 = Mathf.Exp((0f - num7) / Tuning.F("gun.kick.time", 0.07f)) * (LongGun ? 0.6f : 1f);
			handTurn *= Quaternion.Euler((0f - Tuning.F("gun.kick", 4f)) * num8, 0f, 0f);
			handAt -= val12 * Vector3.forward * (0.02f * num8);
		}
		if (lockSet && leftOnGunSet && supportHeld && step == Step.Ready && (Object)(object)item != (Object)null)
		{
			Transform vrRoot = controls.vrRoot;
			Pose pose2 = new Pose(vrRoot.TransformPoint(handAt), val2 * handTurn) * gunInController * leftOnGun;
			leftOverride = true;
			leftAt = vrRoot.InverseTransformPoint(pose2.position);
			leftTurn = Quaternion.Inverse(val2) * pose2.rotation;
			return true;
		}
		if ((Object)(object)support != (Object)null && hold.left != null && step != 0)
		{
			Pose pose3 = GunMoves.U(hold.left, Pose.identity);
			Vector3 val14 = val13 + val12 * (pose3.position + val3);
			Quaternion val15 = val12 * pose3.rotation;
			leftOverride = true;
			if (step == Step.Reach)
			{
				float num9 = Mathf.SmoothStep(0f, 1f, (Time.time - stepAt) / Tuning.F("hold.reach.time", 0.7f));
				Vector3 val16 = controls.vrRoot.InverseTransformPoint(leftStart.position);
				Quaternion val17 = Quaternion.Inverse(val2) * leftStart.rotation;
				leftAt = Vector3.Lerp(val16, val14, num9) + Quaternion.Inverse(val2) * reachFix;
				leftTurn = Quaternion.Slerp(val17, val15, num9);
			}
			else
			{
				leftAt = val14;
				leftTurn = val15;
			}
		}
		return true;
	}

	private Vector3 ChestHold()
	{
		float num = Mathf.Clamp(standingHeight / 1.6f, 0.5f, 2f);
		int num2;
		float num3;
		if (LongGun && (Object)(object)gun != (Object)null && (Object)(object)gun.triggerGrip != (Object)null)
		{
			num2 = ((Vector3.Distance(((Component)support).transform.position, ((Component)gun.triggerGrip).transform.position) > 0.2f) ? 1 : 0);
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
		if ((Object)(object)gun == (Object)null || (Object)(object)item == (Object)null || dead || down || mode == Mode.Clone || mode == Mode.Exact || Tuning.F("gun.lock", 1f) < 0.5f)
		{
			lockSet = false;
			leftOnGunSet = false;
			lockGun = IntPtr.Zero;
			return;
		}
		BaseController rightController = ((ControllerRig)controls).rightController;
		if ((Object)(object)rightController == (Object)null || !Holding())
		{
			lockSet = false;
			leftOnGunSet = false;
			lockGun = IntPtr.Zero;
			return;
		}
		if (lockGun != ((Il2CppObjectBase)(object)gun).Pointer)
		{
			lockGun = ((Il2CppObjectBase)(object)gun).Pointer;
			lockSince = Time.time;
			lockSet = false;
			leftOnGunSet = false;
		}
		if (OthersHoldIt())
		{
			return;
		}
		Transform transform = ((Component)rightController).transform;
		if (!lockSet)
		{
			float num = Time.time - lockSince;
			if (num < 0.5f || (!GunSteady() && num < 1.5f))
			{
				return;
			}
			Pose pose = Pose.Of(transform).Inverse() * Pose.Of(((Component)item).transform);
			Hand rightHand = RightHand;
			float magnitude;
			Vector3 val;
			if (!((Object)(object)gun.triggerGrip != (Object)null))
			{
				magnitude = ((Vector3)(ref pose.position)).magnitude;
			}
			else
			{
				val = ((Component)gun.triggerGrip).transform.position - transform.position;
				magnitude = ((Vector3)(ref val)).magnitude;
			}
			float num2 = magnitude;
			float num3 = (((Object)(object)rightHand != (Object)null) ? Quaternion.Angle(((Component)rightHand).transform.rotation, transform.rotation) : 999f);
			if (num3 > Tuning.F("gun.lock.twist", 45f) && num < 6f)
			{
				return;
			}
			if (!(num2 > Tuning.F("gun.lock.most", 0.35f)) && !((Object)(object)rightHand == (Object)null))
			{
				val = ((Component)rightHand).transform.position - transform.position;
				if (!(((Vector3)(ref val)).magnitude > 0.25f))
				{
					gunInController = pose;
					lockSet = true;
					Lab.Event("gun: kept fast to its hand from here (" + ((Vector3)(ref gunInController.position)).magnitude.ToString("0.00") + " m from the controller, the hand turned " + num3.ToString("0") + " deg from it)");
					goto IL_0341;
				}
			}
			if (num > 4f && Time.time - lockSaidAt > 5f)
			{
				lockSaidAt = Time.time;
				string[] obj = new string[5]
				{
					"gun: not kept fast (its handle is ",
					num2.ToString("0.00"),
					" m from the controller, the hand ",
					null,
					null
				};
				object obj2;
				if (!((Object)(object)rightHand != (Object)null))
				{
					obj2 = "?";
				}
				else
				{
					val = ((Component)rightHand).transform.position - transform.position;
					obj2 = ((Vector3)(ref val)).magnitude.ToString("0.00");
				}
				obj[3] = (string)obj2;
				obj[4] = " m)";
				Lab.Event(string.Concat(obj));
			}
			return;
		}
		goto IL_0341;
		IL_0341:
		if (Time.time - learnedHoldAt > 0.3f && !Reloading)
		{
			return;
		}
		if (supportHeld && step == Step.Ready && !leftOnGunSet && (Object)(object)((ControllerRig)controls).leftController != (Object)null)
		{
			Hand leftHand = LeftHand;
			Pose pose2 = (((Object)(object)leftHand != (Object)null) ? Pose.Of(((Component)leftHand).transform) : Pose.Of(((Component)((ControllerRig)controls).leftController).transform));
			Pose pose3 = Pose.Of(((Component)((ControllerRig)controls).leftController).transform);
			leftOnGun = Pose.Of(((Component)item).transform).Inverse() * pose3;
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
			Vector3 val2 = (pose4.position - item.position) / dt * num4;
			item.velocity = Vector3.ClampMagnitude(val2, 15f);
			Quaternion val3 = pose4.rotation * Quaternion.Inverse(item.rotation);
			float num5 = default(float);
			Vector3 val4 = default(Vector3);
			((Quaternion)(ref val3)).ToAngleAxis(ref num5, ref val4);
			if (num5 > 180f)
			{
				num5 -= 360f;
			}
			Vector3 val5 = ((float.IsNaN(val4.x) || float.IsInfinity(val4.x)) ? Vector3.zero : (val4 * (num5 * ((float)Math.PI / 180f) / dt * num4)));
			item.angularVelocity = Vector3.ClampMagnitude(val5, 30f);
		}
	}

	private bool OthersHoldIt()
	{
		try
		{
			MarrowEntity componentInParent = ((Component)item).GetComponentInParent<MarrowEntity>();
			foreach (Grip componentsInChild in ((Component)(((Object)(object)componentInParent != (Object)null) ? ((object)componentInParent) : ((object)item))).GetComponentsInChildren<Grip>(false))
			{
				if ((Object)(object)componentsInChild == (Object)null || componentsInChild.attachedHands == null)
				{
					continue;
				}
				Enumerator<Hand> enumerator2 = componentsInChild.attachedHands.GetEnumerator();
				while (enumerator2.MoveNext())
				{
					Hand current2 = enumerator2.Current;
					if ((Object)(object)current2 != (Object)null && (Object)(object)current2 != (Object)(object)LeftHand && (Object)(object)current2 != (Object)(object)RightHand)
					{
						return true;
					}
				}
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	private Grip NearestGripTo(Hand hand)
	{
		if ((Object)(object)item == (Object)null)
		{
			return null;
		}
		MarrowEntity componentInParent = ((Component)item).GetComponentInParent<MarrowEntity>();
		object obj = (((Object)(object)componentInParent != (Object)null) ? ((object)componentInParent) : ((object)item));
		Grip result = null;
		float num = float.MaxValue;
		foreach (Grip componentsInChild in ((Component)obj).GetComponentsInChildren<Grip>(false))
		{
			if ((Object)(object)componentsInChild == (Object)null || (Object)(object)componentsInChild == (Object)(object)gun.triggerGrip || componentsInChild.HasAttachedHands() || (Object)(object)((Component)componentsInChild).GetComponentInParent<Magazine>() != (Object)null || WorksBolt(gun, componentsInChild))
			{
				continue;
			}
			string text = ((Object)componentsInChild).name.ToLowerInvariant();
			if (!text.Contains("slide") && !text.Contains("charg") && !text.Contains("bolt") && !text.Contains("mag") && !text.Contains("capsule") && !text.Contains("stock") && (!((Object)(object)gun.ammoSocket != (Object)null) || !((Component)componentsInChild).transform.IsChildOf(((Component)gun.ammoSocket).transform)))
			{
				Vector3 val = ((Component)hand).transform.position - HandOnGrip(componentsInChild, hand).position;
				float magnitude = ((Vector3)(ref val)).magnitude;
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
		return (0f - Mathf.Asin(Mathf.Clamp(((Vector3)(ref v)).normalized.y, -1f, 1f))) * 57.29578f;
	}

	private static float YawOf(Vector3 v)
	{
		return Mathf.Atan2(v.x, v.z) * 57.29578f;
	}

	private void HoldSupportLearned()
	{
		Hand leftHand = LeftHand;
		if ((Object)(object)support == (Object)null || (Object)(object)leftHand == (Object)null)
		{
			return;
		}
		supportHeld = (Object)(object)leftHand.m_CurrentAttachedGO != (Object)null && support.attachedHands != null && support.attachedHands.Contains(leftHand);
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
			if (lockSet && num > Tuning.F("hold.reach.time", 0.7f) && (Object)(object)((ControllerRig)controls).leftController != (Object)null && !supportHeld)
			{
				Vector3 val = HandOnGrip(support, leftHand).position - ((Component)leftHand).transform.position;
				reachFix = Vector3.ClampMagnitude(reachFix + val * Mathf.Clamp01(Time.deltaTime * 6f), 0.35f);
			}
			if ((Object)(object)leftHand.m_CurrentAttachedGO != (Object)null && !Reloading && !supportHeld)
			{
				FreeHand(leftHand);
			}
			Vector3 val2 = ((Component)leftHand).transform.position - HandOnGrip(support, leftHand).position;
			float num2 = ((Vector3)(ref val2)).magnitude;
			if (num > 0.8f && num2 >= Tuning.F("learned.grab", 0.12f))
			{
				Grip val3 = NearestGripTo(leftHand);
				if ((Object)(object)val3 != (Object)null && (Object)(object)val3 != (Object)(object)support)
				{
					val2 = ((Component)leftHand).transform.position - HandOnGrip(val3, leftHand).position;
					float magnitude = ((Vector3)(ref val2)).magnitude;
					if (magnitude < num2)
					{
						Lab.Event("front hand: nearer to " + ((Object)val3).name + " (" + magnitude.ToString("0.00") + " m) than " + ((Object)support).name + " (" + num2.ToString("0.00") + " m)");
						support = val3;
						num2 = magnitude;
					}
				}
			}
			if (num > 0.5f && num2 < Tuning.F("learned.grab", 0.12f) + ((num > 1.5f) ? 0.06f : 0f) && Time.time - supportTriedAt > 0.4f)
			{
				supportTriedAt = Time.time;
				supportHeld = Grab(leftHand, support);
				Lab.Event((supportHeld ? "front hand on " : "front hand missed ") + ((Object)support).name + " (learned hold, gap " + num2.ToString("0.00") + " m" + (supportHeld ? "" : (", hand holds " + (((Object)(object)leftHand.m_CurrentAttachedGO != (Object)null) ? ((Object)leftHand.m_CurrentAttachedGO).name : "nothing") + ", grip held by " + ((support.attachedHands != null) ? support.attachedHands.Count : (-1)) + " hands, " + ((MemberInfo)((Object)support).GetIl2CppType()).Name)) + ")");
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
		if ((Object)(object)gun == (Object)null || Tuning.F("learned.reload", 1f) < 0.5f)
		{
			return false;
		}
		learnedReload = GunMoves.ReloadFor(GunKey, LongGun);
		learnedInsertTried = false;
		Hand leftHand = LeftHand;
		if (learnedReload != null && (Object)(object)leftHand != (Object)null && (Object)(object)leftHand.m_CurrentAttachedGO != (Object)null && !supportHeld)
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
		List<GunMoves.ReloadFrame> frames = reload.frames;
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
		Vector3 eulerAngles = ((Quaternion)(ref pose.rotation)).eulerAngles;
		playPitch = Mathf.DeltaAngle(0f, eulerAngles.x);
		playYaw = Mathf.DeltaAngle(0f, eulerAngles.y);
		playHeight = standingHeight * 0.985f;
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector(0f, playHeight, 0f);
		handOverride = true;
		handAt = val + pose2.position;
		handTurn = pose2.rotation;
		leftOverride = true;
		leftAt = val + pose3.position;
		leftTurn = pose3.rotation;
		if ((Object)(object)ammoSocket == (Object)null)
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
					Magazine val2 = (((Object)(object)ammoSocket._magazinePlug != (Object)null) ? ammoSocket._magazinePlug.magazine : null);
					ammoSocket.EjectMagazine();
					if ((Object)(object)val2 != (Object)null)
					{
						dropped.Add((val2, Time.time));
					}
				}
			}
			catch (Exception)
			{
			}
		}
		float num2 = ((reload.grab >= 0f) ? reload.grab : Mathf.Max(0.3f, reload.insert - 0.8f));
		if (!magAsked && num >= num2 - 0.25f && (Object)(object)leftHand != (Object)null)
		{
			magAsked = true;
			this.reload = Reload.Fetch;
			SpawnMag(((Component)leftHand).transform.position, ((Component)leftHand).transform.rotation);
		}
		if ((Object)(object)newMag == (Object)null && magAsked && Time.time - magAskedAt > 0.15f)
		{
			newMag = SpawnedMag();
		}
		if ((Object)(object)newMag != (Object)null && (Object)(object)newMag.grip != (Object)null && (Object)(object)leftHand != (Object)null && (Object)(object)leftHand.m_CurrentAttachedGO == (Object)null && !gun.HasMagazine() && num >= num2 - 0.1f)
		{
			Grab(leftHand, newMag.grip);
		}
		Vector3 val3;
		if ((Object)(object)newMag != (Object)null && !gun.HasMagazine() && num >= reload.insert && (Object)(object)newMag.magazinePlug != (Object)null)
		{
			this.reload = Reload.Insert;
			val3 = ((Component)newMag.magazinePlug).transform.position - ((Component)ammoSocket).transform.position;
			float magnitude = ((Vector3)(ref val3)).magnitude;
			try
			{
				if (magnitude < 0.15f && !learnedInsertTried)
				{
					learnedInsertTried = true;
					((AlignPlug)newMag.magazinePlug).InsertPlug((Socket)(object)ammoSocket);
				}
				else if (num > reload.insert + 0.12f)
				{
					((AlignPlug)newMag.magazinePlug).ForceInSocket((Socket)(object)ammoSocket);
				}
			}
			catch (Exception)
			{
			}
		}
		if ((Object)(object)newMag != (Object)null && gun.HasMagazine() && (Object)(object)leftHand != (Object)null)
		{
			LetGoOf(newMag.grip, leftHand);
			newMag = null;
		}
		if (reload.rack >= 0f && gun.HasMagazine() && (int)gun.cartridgeState != 1 && (Object)(object)leftHand != (Object)null)
		{
			this.reload = Reload.Rack;
			if ((Object)(object)slide == (Object)null)
			{
				slide = SlideGrip(gun);
			}
			if ((Object)(object)slide != (Object)null && !slideHeld && num >= reload.rack - 0.3f && num < reload.rack)
			{
				val3 = ((Component)leftHand).transform.position - ((Component)slide).transform.position;
				if (((Vector3)(ref val3)).magnitude < 0.15f && (Object)(object)leftHand.m_CurrentAttachedGO == (Object)null)
				{
					slideHeld = Grab(leftHand, slide);
				}
			}
		}
		if (slideHeld && num >= reload.rack + 0.1f)
		{
			LetGoOf(slide, leftHand);
			slideHeld = false;
		}
		if (reload.rack >= 0f && num >= reload.rack + 0.15f && gun.HasMagazine() && (int)gun.cartridgeState != 1 && !slideHeld && Time.time - chargedAt > 0.6f)
		{
			chargedAt = Time.time;
			TryCharge();
		}
		if (!(num >= reload.end))
		{
			return;
		}
		learnedReload = null;
		if (!gun.HasMagazine() && (Object)(object)newMag != (Object)null && (Object)(object)newMag.magazinePlug != (Object)null)
		{
			try
			{
				((AlignPlug)newMag.magazinePlug).ForceInSocket((Socket)(object)ammoSocket);
			}
			catch (Exception)
			{
			}
		}
		DropNewMag();
		if (gun.HasMagazine() && (int)gun.cartridgeState != 1)
		{
			TryCharge();
		}
		FinishReload(!gun.HasMagazine());
	}

	public void OnAnyGunFired(Gun fired)
	{
		if (!((Object)(object)gun == (Object)null) && !((Object)(object)fired == (Object)null) && !(((Il2CppObjectBase)(object)fired).Pointer != ((Il2CppObjectBase)(object)gun).Pointer))
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
		Vector3 val = Feet;
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(val + Vector3.up * 0.5f, far, -1, (QueryTriggerInteraction)1))
		{
			Rigidbody val2 = (((Object)(object)item != (Object)null) ? item.attachedRigidbody : null);
			if ((Object)(object)val2 == (Object)null || !hashSet.Add(((Il2CppObjectBase)(object)val2).Pointer) || (Object)(object)((Component)val2).GetComponentInParent<RigManager>() != (Object)null)
			{
				continue;
			}
			Gun val3 = ((Component)val2).GetComponentInParent<Gun>();
			if ((Object)(object)val3 == (Object)null)
			{
				MarrowEntity componentInParent = ((Component)val2).GetComponentInParent<MarrowEntity>();
				val3 = (((Object)(object)componentInParent != (Object)null) ? ((Component)componentInParent).GetComponentInChildren<Gun>() : null);
			}
			if (Usable(val3))
			{
				float num = val2.worldCenterOfMass.y - val.y;
				Vector3 val4 = val2.worldCenterOfMass - val;
				float magnitude = ((Vector3)(ref val4)).magnitude;
				if (!(num < -0.5f) && !(num > 1.6f) && !(magnitude >= far))
				{
					far = magnitude;
					result = val3;
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
		if (HasStowed && (Object)(object)stowedGun != (Object)null)
		{
			return Draw();
		}
		Vector3 val = Feet;
		Vector3 val2;
		if ((Object)(object)ownGun != (Object)null && (Object)(object)ownGun.triggerGrip != (Object)null && ((Component)ownGun).gameObject.activeInHierarchy)
		{
			Rigidbody componentInParent = ((Component)ownGun.triggerGrip).GetComponentInParent<Rigidbody>();
			if (!Usable(ownGun) || OwnGunTaken())
			{
				ownGun = null;
			}
			else if ((Object)(object)componentInParent != (Object)null)
			{
				val2 = componentInParent.worldCenterOfMass - val;
				if (((Vector3)(ref val2)).magnitude < 60f)
				{
					Take(ownGun);
					play = Play.GoTo;
					playAt = Time.time;
					nav.SetGoal(item.worldCenterOfMass);
					return true;
				}
			}
		}
		Gun val3 = null;
		float num = Tuning.F("gun.seek", 30f);
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(val + Vector3.up * 0.5f, num, -1, (QueryTriggerInteraction)1))
		{
			Rigidbody val4 = (((Object)(object)item != (Object)null) ? item.attachedRigidbody : null);
			if ((Object)(object)val4 == (Object)null || !hashSet.Add(((Il2CppObjectBase)(object)val4).Pointer) || (Object)(object)((Component)val4).GetComponentInParent<RigManager>() != (Object)null)
			{
				continue;
			}
			Gun val5 = ((Component)val4).GetComponentInParent<Gun>();
			if ((Object)(object)val5 == (Object)null)
			{
				MarrowEntity componentInParent2 = ((Component)val4).GetComponentInParent<MarrowEntity>();
				val5 = (((Object)(object)componentInParent2 != (Object)null) ? ((Component)componentInParent2).GetComponentInChildren<Gun>() : null);
			}
			if (Usable(val5) && !ClaimedByAnother(val5) && (!wontLoad.TryGetValue(((Il2CppObjectBase)(object)val5).Pointer, out var value) || !(Time.time - value < 120f)))
			{
				float num2 = val4.worldCenterOfMass.y - val.y;
				val2 = val4.worldCenterOfMass - val;
				float magnitude = ((Vector3)(ref val2)).magnitude;
				if (!(num2 < -0.5f) && !(num2 > 1.6f) && !(magnitude >= num))
				{
					num = magnitude;
					val3 = val5;
				}
			}
		}
		if ((Object)(object)val3 != (Object)null)
		{
			Take(val3);
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
		if ((Object)(object)g == (Object)null || !claims.TryGetValue(((Il2CppObjectBase)(object)g).Pointer, out (Bot, float) value))
		{
			return false;
		}
		if (value.Item1 != this && value.Item1.Alive && !value.Item1.dead && (Object)(object)value.Item1.gun == (Object)(object)g)
		{
			return Time.time - value.Item2 < Tuning.F("gun.claim", 12f);
		}
		return false;
	}

	private static bool Usable(Gun g)
	{
		if ((Object)(object)g == (Object)null || (Object)(object)g.triggerGrip == (Object)null || g.triggerGrip.HasAttachedHands() || (int)g.fireMode == 0)
		{
			return false;
		}
		if (wontLoad.TryGetValue(((Il2CppObjectBase)(object)g).Pointer, out var value) && Time.time - value < 120f)
		{
			return false;
		}
		string text = ((Object)g).name.ToUpperInvariant();
		if (text.Contains("SPAWN") || text.Contains("NIMBUS") || text.Contains("TOOL"))
		{
			return false;
		}
		Rigidbody componentInParent = ((Component)g.triggerGrip).GetComponentInParent<Rigidbody>();
		if ((Object)(object)componentInParent != (Object)null)
		{
			return !componentInParent.isKinematic;
		}
		return false;
	}

	private bool OwnGunTaken()
	{
		try
		{
			MarrowEntity componentInParent = ((Component)ownGun).GetComponentInParent<MarrowEntity>();
			foreach (Grip componentsInChild in ((Component)(((Object)(object)componentInParent != (Object)null) ? ((object)componentInParent) : ((object)ownGun))).GetComponentsInChildren<Grip>(false))
			{
				if ((Object)(object)componentsInChild == (Object)null || componentsInChild.attachedHands == null)
				{
					continue;
				}
				Enumerator<Hand> enumerator2 = componentsInChild.attachedHands.GetEnumerator();
				while (enumerator2.MoveNext())
				{
					Hand current2 = enumerator2.Current;
					if ((Object)(object)current2 != (Object)null && (Object)(object)current2 != (Object)(object)LeftHand && (Object)(object)current2 != (Object)(object)RightHand)
					{
						return true;
					}
				}
			}
		}
		catch (Exception)
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
		claims[((Il2CppObjectBase)(object)g).Pointer] = (this, Time.time);
		grip = g.triggerGrip;
		item = ((Component)grip).GetComponentInParent<Rigidbody>();
		support = SupportGrip(g);
		frontRetry = null;
		if ((Object)(object)support == (Object)null)
		{
			GunMoves.Hold hold = GunMoves.Held(GunMoves.KeyOf(g, twoHanded: false), twoHanded: false, aimed: true);
			if (hold != null && hold.left != null)
			{
				support = AnySecondGrip(g);
			}
		}
		GunMoves.Hold hold2 = GunMoves.Held(GunMoves.KeyOf(g, twoHanded: false), twoHanded: false, aimed: true);
		if (hold2 != null && hold2.left == null && (Object)(object)support != (Object)null)
		{
			Lab.Event("holding " + ((Object)g).name + " in one hand, as you showed it (a " + GunMoves.KindOf(g) + ")");
			support = null;
		}
		gunIsLong = (Object)(object)support != (Object)null;
		if ((Object)(object)support != (Object)null && Tuning.F("hold.twohands", 0f) < 0.5f)
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
		MarrowEntity componentInParent = ((Component)g).GetComponentInParent<MarrowEntity>();
		foreach (Grip componentsInChild in ((Component)(((Object)(object)componentInParent != (Object)null) ? ((object)componentInParent) : ((object)g))).GetComponentsInChildren<Grip>(false))
		{
			if (!((Object)(object)componentsInChild == (Object)null) && !((Object)(object)componentsInChild == (Object)(object)g.triggerGrip) && !componentsInChild.HasAttachedHands() && !((Object)(object)((Component)componentsInChild).GetComponentInParent<Magazine>() != (Object)null))
			{
				string text = ((Object)componentsInChild).name.ToLowerInvariant();
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
		if ((Object)(object)g.firePointTransform == (Object)null || (Object)(object)g.triggerGrip == (Object)null)
		{
			return null;
		}
		Grip val = GripBook.Steadying(GripBook.ThingOf(g.triggerGrip), g.triggerGrip);
		if ((Object)(object)val != (Object)null && !WorksBolt(g, val))
		{
			Lab.Event("gun: front grip " + ((Object)val).name + ", as you hold it");
			return val;
		}
		Vector3 forward = g.firePointTransform.forward;
		Vector3 position = ((Component)g.triggerGrip).transform.position;
		List<Grip> list = new List<Grip>();
		if (g.otherGrips != null)
		{
			foreach (Grip otherGrip in g.otherGrips)
			{
				list.Add(otherGrip);
			}
		}
		MarrowEntity componentInParent = ((Component)g).GetComponentInParent<MarrowEntity>();
		foreach (Grip componentsInChild in ((Component)(((Object)(object)componentInParent != (Object)null) ? ((object)componentInParent) : ((object)g))).GetComponentsInChildren<Grip>(false))
		{
			if (!list.Contains(componentsInChild))
			{
				list.Add(componentsInChild);
			}
		}
		Grip val2 = null;
		float num = float.MaxValue;
		List<string> list2 = new List<string>();
		foreach (Grip item in list)
		{
			if ((Object)(object)item == (Object)null || (Object)(object)item == (Object)(object)g.triggerGrip || !((Component)item).gameObject.activeInHierarchy || WorksBolt(g, item))
			{
				continue;
			}
			list2.Add(((Object)item).name + " (" + Vector3.Dot(((Component)item).transform.position - position, forward).ToString("0.00") + " m ahead)");
			string text = ((Object)item).name.ToLowerInvariant();
			bool flag = text.Contains("forwardgrip") || text.Contains("foregrip") || text.Contains("forward_grip") || text.Contains("verticalgrip");
			if (text.Contains("slide") || text.Contains("charg") || text.Contains("bolt") || text.Contains("mag") || text.Contains("pump") || text.Contains("sight") || (text.Contains("attachment") && !flag) || text.Contains("scope") || text.Contains("stock") || text.Contains("capsule") || (Object)(object)((Component)item).GetComponentInParent<Magazine>() != (Object)null || ((Object)(object)g.ammoSocket != (Object)null && ((Component)item).transform.IsChildOf(((Component)g.ammoSocket).transform)))
			{
				continue;
			}
			float num2 = Vector3.Dot(((Component)item).transform.position - position, forward);
			if (!(num2 < 0.15f) && !(num2 > 0.5f))
			{
				float num3 = Mathf.Abs(num2 - 0.3f) - (flag ? 0.3f : ((text.Contains("fore") || text.Contains("hand")) ? 0.1f : 0f));
				if (num3 < num)
				{
					num = num3;
					val2 = item;
				}
			}
		}
		MelonLogger.Msg("Bot Player: " + ((Object)g).name + " grips: " + ((list2.Count > 0) ? string.Join(", ", list2) : "none but the handle") + "; front hand on " + (((Object)(object)val2 != (Object)null) ? ((Object)val2).name : "nothing (one-handed)"));
		return val2;
	}

	private bool SpawnGun(string forced = null)
	{
		Hand rightHand = RightHand;
		Transform head = Head;
		if ((Object)(object)rightHand == (Object)null || (Object)(object)head == (Object)null)
		{
			return false;
		}
		List<string> list = new List<string>();
		AssetWarehouse instance = AssetWarehouse.Instance;
		string[] spawnGuns = SpawnGuns;
		foreach (string text in spawnGuns)
		{
			if (instance != null && instance.HasCrate(new Barcode(text)))
			{
				list.Add(text);
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
		spawnAt = ((Component)rightHand).transform.position;
		gunsBefore.Clear();
		foreach (Collider item in Physics.OverlapSphere(spawnAt, 1.5f, -1, (QueryTriggerInteraction)1))
		{
			Gun val = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Gun>() : null);
			if ((Object)(object)val != (Object)null)
			{
				gunsBefore.Add(((Il2CppObjectBase)(object)val).Pointer);
			}
		}
		string text2 = forced ?? Knowledge.PickGun(list);
		MelonLogger.Msg("Bot Player: spawning a gun (" + text2 + ")");
		Lab.Event("spawning gun " + text2);
		Vector3 val2 = ((Component)rightHand).transform.position + Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward * 0.1f;
		try
		{
			Clone.Suppress();
			HelperMethods.SpawnCrate(text2, val2, Quaternion.Euler(0f, bodyYaw, 0f), Vector3.one, false, (Action<GameObject>)delegate(GameObject spawned)
			{
				if (spawningGun)
				{
					spawningGun = false;
					Gun val3 = (((Object)(object)spawned != (Object)null) ? spawned.GetComponentInChildren<Gun>() : null);
					if ((Object)(object)val3 == (Object)null || !Alive || (Object)(object)RightHand == (Object)null)
					{
						MelonLogger.Warning("Bot Player: the spawned gun could not be used (" + (((Object)(object)spawned != (Object)null) ? ((Object)spawned).name : "nothing came") + ")");
					}
					else
					{
						Take(val3);
						if ((Object)(object)this.item != (Object)null && Grab(RightHand, grip))
						{
							AfterSpawnedGun();
						}
						else
						{
							EndPlay();
						}
					}
				}
			}, (Action<GameObject>)null);
		}
		catch (Exception ex)
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
		if ((Object)(object)rightHand == (Object)null)
		{
			return false;
		}
		foreach (Collider item in Physics.OverlapSphere(spawnAt, 1.5f, -1, (QueryTriggerInteraction)1))
		{
			Gun val = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Gun>() : null);
			if (!((Object)(object)val == (Object)null) && !gunsBefore.Contains(((Il2CppObjectBase)(object)val).Pointer) && Usable(val))
			{
				spawningGun = false;
				Take(val);
				if ((Object)(object)this.item != (Object)null && Grab(rightHand, grip))
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
		Lab.Event("tossing " + ((Object)item).name + " to the player");
	}

	private void StartShooting()
	{
		play = Play.Shoot;
		playAt = Time.time;
		target = default(Targets.Target);
		targetCheckAt = 0f;
		shootUntil = ((mode == Mode.Gunplay || (mode == Mode.Brain && hostiles.Count > 0)) ? float.MaxValue : (Time.time + Random.Range(10f, 20f)));
		Load();
		loadSettleUntil = Time.time + Tuning.F("gun.settle", 1.5f);
		Lab.Event("holding " + ((Object)gun).name + (((Object)(object)support != (Object)null) ? (", front grip " + ((Object)support).name) : ", one-handed"));
		NoPushOnItself(((Component)gun).transform);
	}

	private static void FeedPouch(Gun g)
	{
		try
		{
			AmmoInventory instance = AmmoInventory.Instance;
			if (!((Object)(object)g == (Object)null) && !((Object)(object)instance == (Object)null) && !((Object)(object)g.defaultCartridge == (Object)null))
			{
				int num = (((Object)(object)g.defaultMagazine != (Object)null) ? Mathf.Max(1, g.defaultMagazine.rounds) : 30);
				instance.AddCartridge(g.defaultCartridge, num);
			}
		}
		catch (Exception)
		{
		}
	}

	private void Chamber()
	{
		try
		{
			if ((int)gun.cartridgeState != 1 && !(Time.time - firedAt < 1f / Mathf.Max(1f, gun.roundsPerSecond)))
			{
				gun.chamberedCartridge = gun.defaultCartridge;
				gun.cartridgeState = (CartridgeStates)1;
				gun.isCharged = true;
				if ((int)gun.slideState == 4)
				{
					gun.SlideRelease();
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private void DirectLoad()
	{
		try
		{
			if (!((Object)(object)gun == (Object)null) && !((Object)(object)gun.ammoSocket == (Object)null) && !((Object)(object)gun.defaultMagazine == (Object)null))
			{
				FeedPouch(gun);
				directAsked = true;
				Transform transform = ((Component)gun.ammoSocket).transform;
				SpawnMag(transform.position - transform.up * 0.1f, transform.rotation);
			}
		}
		catch (Exception)
		{
			directAsked = false;
		}
	}

	private bool SettleLoad()
	{
		try
		{
			if ((Object)(object)gun == (Object)null)
			{
				return true;
			}
			if (directAsked && !gun.HasMagazine() && (Object)(object)gun.ammoSocket != (Object)null && Time.time - magAskedAt > 0.15f)
			{
				Magazine val = SpawnedMag();
				if ((Object)(object)val != (Object)null && (Object)(object)val.magazinePlug != (Object)null)
				{
					directAsked = false;
					((Component)val).transform.SetPositionAndRotation(((Component)gun.ammoSocket).transform.position, ((Component)gun.ammoSocket).transform.rotation);
					try
					{
						((AlignPlug)val.magazinePlug).InsertPlug((Socket)(object)gun.ammoSocket);
					}
					catch (Exception)
					{
					}
					if (!gun.HasMagazine())
					{
						((AlignPlug)val.magazinePlug).ForceInSocket((Socket)(object)gun.ammoSocket);
					}
					if (!gun.HasMagazine())
					{
						dropped.Add((val, Time.time));
					}
				}
			}
			if (gun.HasMagazine())
			{
				if (gun.MagazineState != null && gun.MagazineState.AmmoCount <= 0)
				{
					gun.MagazineState.Refill();
				}
				if ((int)gun.slideState == 4)
				{
					gun.SlideRelease();
				}
				if ((int)gun.cartridgeState != 1)
				{
					gun.Charge();
				}
			}
			return (int)gun.cartridgeState == 1 || (gun.HasMagazine() && gun.AmmoCount() > 0);
		}
		catch (Exception)
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
			if (reload || !gun.isCharged || (int)gun.cartridgeState != 1)
			{
				gun.Charge();
			}
			if (reload)
			{
				MelonLogger.Msg("Bot Player: reloaded " + ((Object)gun).name + " (" + gun.AmmoCount() + " rounds)");
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: could not load " + ((Object)gun).name + ": " + ex.Message);
		}
	}

	private bool NeedsRacking()
	{
		try
		{
			float num = Time.time - firedAt;
			bool num2 = (int)gun.cartridgeState == 0 && num > 0.6f;
			bool flag = (int)gun.cartridgeState == 2 && num > 1.5f;
			return (num2 || flag) && gun.HasMagazine() && gun.AmmoCount() > 0;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private bool LoadedButSilent()
	{
		try
		{
			return !magless.Contains(((Il2CppObjectBase)(object)gun).Pointer) && (int)gun.cartridgeState == 1 && (int)gun.slideState == 1;
		}
		catch (Exception)
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
			bool flag = (Object)(object)gun.triggerGrip != (Object)null && gun.triggerGrip.attachedHands != null && (Object)(object)rightHand != (Object)null && gun.triggerGrip.attachedHands.Contains(rightHand);
			string[] obj = new string[12]
			{
				"hammer ", null, null, null, null, null, null, null, null, null,
				null, null
			};
			HammerStates hammerState = gun.hammerState;
			obj[1] = ((object)(HammerStates)(ref hammerState)).ToString();
			obj[2] = ", charged ";
			obj[3] = gun.isCharged.ToString();
			obj[4] = ", fired once ";
			obj[5] = gun.hasFiredOnce.ToString();
			obj[6] = ", trigger pulled ";
			obj[7] = gun.isTriggerPulled.ToString();
			obj[8] = ", mode ";
			FireMode fireMode = gun.fireMode;
			obj[9] = ((object)(FireMode)(ref fireMode)).ToString();
			obj[10] = ", its hand on the handle ";
			obj[11] = flag.ToString();
			text = string.Concat(obj);
			gun.hasFiredOnce = false;
			gun.isTriggerPulled = false;
			gun.hammerState = (HammerStates)1;
			gun.isCharged = true;
			if (!flag && (Object)(object)rightHand != (Object)null && (Object)(object)gun.triggerGrip != (Object)null)
			{
				Grab(rightHand, gun.triggerGrip);
			}
		}
		catch (Exception ex)
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
			if (magless.Contains(((Il2CppObjectBase)(object)gun).Pointer))
			{
				return false;
			}
			return (int)gun.cartridgeState != 1 && gun.AmmoCount() <= 0;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private bool Shoot(Transform you, Transform head, float dt)
	{
		if ((Object)(object)gun == (Object)null || !Holding() || (Object)(object)head == (Object)null)
		{
			try
			{
				Hand rightHand = RightHand;
				Lab.Event("gun: out of its hand (" + (((Object)(object)gun == (Object)null) ? "no gun" : (((Object)(object)head == (Object)null) ? "no head" : ("its hand holds " + (((Object)(object)rightHand != (Object)null && (Object)(object)rightHand.m_CurrentAttachedGO != (Object)null) ? ((Object)rightHand.m_CurrentAttachedGO).name : "nothing") + ", hands on the grip " + (((Object)(object)grip != (Object)null && grip.attachedHands != null) ? grip.attachedHands.Count : (-1)) + ", step " + step.ToString() + (Reloading ? (", reloading " + reload) : "")))) + ")");
			}
			catch (Exception)
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
		if ((Object)(object)firePointTransform == (Object)null)
		{
			return EndPlay();
		}
		bool flag = mode == Mode.Brain;
		bool flag2 = goal == Goal.Explore || goal == Goal.Follow || goal == Goal.Idle || goal == Goal.Investigate || goal == Goal.Guard;
		bool flag3 = goal == Goal.TakeCover && coverReachedAt > 0f;
		bool flag4 = flag && ((flag2 && (Object)(object)nearestSeen.transform == (Object)null && AmmoFraction() < ReloadBelow) || (flag3 && AmmoFraction() < 0.6f));
		bool flag5 = wantedFireSince > 0f && Time.time - wantedFireSince > 2f && Time.time - firedAt > 2f;
		if (magless.Contains(((Il2CppObjectBase)(object)gun).Pointer))
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
		catch (Exception)
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
			NoPushOnItself(((Component)gun).transform, again: true);
		}
		else if (!Reloading && loadSettleUntil > 0f)
		{
			loadSettleUntil = 0f;
			if (!SettleLoad())
			{
				loadFails++;
				if (loadFails < 2)
				{
					Lab.Event("gun: the " + ((Object)gun).name + " is still empty after loading: a magazine put straight into it");
					DirectLoad();
					loadSettleUntil = Time.time + Tuning.F("gun.settle", 1.5f) + 0.5f;
					return true;
				}
				loadFails = 0;
				magless.Add(((Il2CppObjectBase)(object)gun).Pointer);
				Lab.Event("gun: no magazine to be had for the " + ((Object)gun).name + ": its rounds go straight into its chamber");
			}
			else
			{
				loadFails = 0;
			}
		}
		if (!Reloading && magless.Contains(((Il2CppObjectBase)(object)gun).Pointer))
		{
			float num = 1f / Mathf.Max(1f, gun.roundsPerSecond);
			if (trigger > 0.5f && Time.time - triggerSince > 0.1f && Time.time - firedAt > num && Time.time - forcedAt > Mathf.Max(0.12f, num) && (firedAt < triggerSince || (int)gun.fireMode == 2))
			{
				forcedAt = Time.time;
				FeedPouch(gun);
				try
				{
					gun.chamberedCartridge = gun.defaultCartridge;
					gun.cartridgeState = (CartridgeStates)1;
					gun.isCharged = true;
					gun.Fire();
				}
				catch (Exception)
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
				magless.Remove(((Il2CppObjectBase)(object)gun).Pointer);
				wontLoad[((Il2CppObjectBase)(object)gun).Pointer] = Time.time;
				Lab.Event("gun: the " + ((Object)gun).name + " will not fire, putting it down for another");
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
					Vector2 val = Footwork(head, shooter, dt) * 0.6f;
					PlanCrouch(head, shooter, val);
					walk = Vector2.ClampMagnitude(val, crouchPlan ? (MotionLibrary.WalkPush * 0.4f) : MotionLibrary.WalkPush);
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
		if ((Object)(object)target.transform != (Object)null && (!Targets.Alive(target) || ((Object)(object)target.prop != (Object)null && shotsAtTarget >= 4)))
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
		if (play != Play.Shoot || (Object)(object)gun == (Object)null)
		{
			return false;
		}
		bool flag8 = flag && (goal == Goal.Retreat || (goal == Goal.TakeCover && coverReachedAt < 0f)) && hasGoalPlace;
		Vector3 val2;
		if ((Object)(object)target.transform == (Object)null || flag8)
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
					int num2;
					if (goal != Goal.Retreat && goal != Goal.TakeCover)
					{
						if ((goal == Goal.Investigate || goal == Goal.ArmUp || goal == Goal.Guard || goal == Goal.Fight) && nav.HasGoal)
						{
							val2 = Flat(nav.Goal - Feet);
							num2 = ((((Vector3)(ref val2)).magnitude > 6f) ? 1 : 0);
						}
						else
						{
							num2 = 0;
						}
					}
					else
					{
						num2 = 1;
					}
					running = (byte)num2 != 0;
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
		Vector3 val3 = TargetPoint() + AimSpot(head.position);
		val2 = Flat(val3 - head.position);
		float magnitude = ((Vector3)(ref val2)).magnitude;
		float num3 = (flag ? Knowledge.FightDistance : 8f) * personality.distance;
		int odds = 0;
		bool flag10 = false;
		if (flag && target.hostile)
		{
			num3 *= TeamKeep(out odds);
		}
		Vector2 move = Vector2.zero;
		if (target.hostile)
		{
			float num4 = Knowledge.Style.Mean(Knowledge.Style.giveGround, 0.6f);
			Vector3 val4 = Tactics.Velocity(target);
			val2 = head.position - val3;
			bool flag11 = Vector3.Dot(val4, ((Vector3)(ref val2)).normalized) > 1f;
			if (magnitude < Mathf.Max(3f, num3 - 2.5f) || (flag11 && magnitude < num3 && num4 > 0.4f))
			{
				move.y = (0f - Mathf.Clamp01((num3 - magnitude) / 3f + 0.3f)) * ((odds < 0) ? (MotionLibrary.RunPush * Tuning.F("team.fall.run", 0.85f)) : MotionLibrary.WalkPush);
				Vector3 back = Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.back;
				flag10 = !Tactics.RoomBehind(head.position - Vector3.up * 0.5f, back);
				if (flag10)
				{
					move.y = 0f;
					if (Time.time > sidestepUntil)
					{
						sidestep = new Vector2((Random.value < 0.5f) ? (-1f) : 1f, 0f) * MotionLibrary.WalkPush;
						sidestepUntil = Time.time + 0.8f;
					}
				}
			}
			else if (odds > 0 && magnitude > num3 + 1f && magnitude <= num3 + 6f)
			{
				move.y = MotionLibrary.WalkPush;
			}
			else if (magnitude > num3 + 6f)
			{
				move.y = ((magnitude > num3 + 10f) ? MotionLibrary.RunPush : MotionLibrary.WalkPush);
			}
		}
		bool num5 = flag && target.hostile && ShootsBack(target) && Tuning.F("fight.move", 1f) > 0.5f;
		bool holdFire = false;
		if (num5)
		{
			bool flag12 = odds < 0 && magnitude < num3 - 2.5f;
			if (flag12 || !CoverStep(head, val3, ref move, ref holdFire, dt))
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
				running = ((Vector2)(ref move)).magnitude > MotionLibrary.WalkPush + 0.05f;
			}
			Ducking(dt);
		}
		else
		{
			float num6 = Mathf.Lerp(0.25f, 1.6f, Knowledge.Style.Mean(Knowledge.Style.strafe, 0.25f));
			if (Time.time > sidestepUntil + 1.5f && Random.value < dt * num6)
			{
				sidestep = new Vector2((Random.value < 0.5f) ? (-1f) : 1f, 0f) * MotionLibrary.WalkPush * 0.7f;
				sidestepUntil = Time.time + Random.Range(0.4f, 0.9f);
			}
			if (Time.time < sidestepUntil)
			{
				move += sidestep;
			}
		}
		running = ((Vector2)(ref move)).magnitude > MotionLibrary.WalkPush + 0.05f;
		walk = Vector2.ClampMagnitude(move, 1f);
		if (flag && Time.time - teamSaidAt > 3f)
		{
			teamSaidAt = Time.time;
			Lab.Event("team: odds " + odds + ", keeps " + num3.ToString("0.0") + " m, is " + magnitude.ToString("0.0") + " m off, moving " + move.y.ToString("0.00") + " ahead" + (running ? ", running" : "") + (flag10 ? ", no room behind" : "") + (holdFire ? ", in cover" : ""));
		}
		Face(val3, dt, 260f);
		wobble = new Vector2(Mathf.PerlinNoise(Time.time * 0.7f, 3.1f) - 0.5f, Mathf.PerlinNoise(5.7f, Time.time * 0.7f) - 0.5f) * Mathf.Lerp(Tuning.F("aim.shake.poor", 3.4f), Tuning.F("aim.shake.good", 0.9f), personality.aim);
		lastAimPoint = val3;
		lastTargetAt = Time.time;
		Aim(val3, head, Hold.Aimed);
		val2 = val3 - firePointTransform.position;
		Vector3 normalized = ((Vector3)(ref val2)).normalized;
		float num7 = (labAimError = Vector3.Angle(firePointTransform.forward, normalized));
		bool flag13 = Targets.NearPlayer(val3, 1.2f);
		bool flag14 = target.player || enemy || ((!flag13) ? (!Targets.PlayerInLine(firePointTransform.position, firePointTransform.forward, 1f)) : (target.hostile && Targets.ClearOfPlayer(firePointTransform.position, val3) && !Targets.PlayerInLine(firePointTransform.position, firePointTransform.forward, 0.5f)));
		float num8 = Skill[1];
		val2 = val3 - firePointTransform.position;
		float num9 = Mathf.Atan2(num8, Mathf.Max(1f, ((Vector3)(ref val2)).magnitude)) * 57.29578f;
		bool flag15 = Time.time - targetSince > reaction && num7 < Mathf.Max(0.8f, Mathf.Min(Skill[0], num9));
		val2 = val3 - firePointTransform.position;
		shotRange = ((Vector3)(ref val2)).magnitude;
		flag15 = flag15 && !holdFire;
		Fire(flag15 && flag14);
		int num10;
		if (target.hostile)
		{
			val2 = val3 - head.position;
			if (((Vector3)(ref val2)).magnitude < Tuning.F("assist.close", 2.3f))
			{
				num10 = ((num7 > Tuning.F("assist.close.off", 20f)) ? 1 : 0);
				goto IL_1290;
			}
		}
		num10 = 0;
		goto IL_1290;
		IL_1290:
		bool flag16 = (byte)num10 != 0;
		if (target.hostile && (!flag14 || flag16))
		{
			if (!flag13)
			{
				val2 = val3 - head.position;
				if (((Vector3)(ref val2)).magnitude >= Tuning.F("assist.within", 14f) && Time.time > sidestepUntil + 0.5f)
				{
					sidestep = new Vector2((Random.value < 0.5f) ? (-1f) : 1f, 0f) * MotionLibrary.WalkPush;
					sidestepUntil = Time.time + 1.2f;
				}
			}
			if (unsafeSince < 0f)
			{
				unsafeSince = Time.time;
			}
			if (Time.time - unsafeSince > Tuning.F("assist.after", 1.5f))
			{
				val2 = val3 - head.position;
				if (((Vector3)(ref val2)).magnitude < Tuning.F("assist.within", 14f))
				{
					unsafeSince = -1f;
					assistUntil = Time.time + Tuning.F("assist.for", 7f);
					assistBegan = (assistSawAt = Time.time);
					Lab.Event("assist: no safe shot at what is on you, in with its hands");
					Stow();
					return false;
				}
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
		if ((Object)(object)t.transform == (Object)null)
		{
			return false;
		}
		if (t.player)
		{
			return (Object)(object)Targets.MuzzleOf(t.transform) != (Object)null;
		}
		if (t.bot != null && !t.bot.dead && (Object)(object)t.bot.gun != (Object)null)
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
		if (t.bot == null || !((Object)(object)t.bot.gun != (Object)null))
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
		return (Object)(object)shooter.transform != (Object)null;
	}

	private void Duck(float seconds)
	{
		duckUntil = Time.time + seconds;
		duckDepth = 0f - Random.Range(Tuning.F("fight.duck.least", 0.45f), Tuning.F("fight.duck.most", 0.85f));
	}

	private void PlanCrouch(Transform head, Targets.Target foe, Vector2 move)
	{
		crouchPlannedAt = Time.time;
		if ((Object)(object)head == (Object)null || (Object)(object)foe.transform == (Object)null || Tuning.F("fight.crouch.on", 1f) < 0.5f)
		{
			crouchPlan = false;
			duckUntil = 0f;
			return;
		}
		if (Time.time >= crouchLookAt)
		{
			crouchLookAt = Time.time + 0.4f;
			float num = Mathf.Clamp(standingHeight / 1.6f, 0.5f, 2f);
			Vector3 val = Feet;
			Vector3 val2 = val + Vector3.up * standingHeight;
			Vector3 eye = val + Vector3.up * (standingHeight - Tuning.F("fight.crouch.drop", 0.55f) * num);
			Vector3 point = foe.Point;
			bool flag = Targets.InSight(val2, point, foe.transform, rig);
			bool flag2 = Targets.InSight(eye, point, foe.transform, rig);
			Vector3 val3 = point - val2;
			float magnitude = ((Vector3)(ref val3)).magnitude;
			bool flag3 = ((Vector2)(ref move)).magnitude > MotionLibrary.WalkPush * 0.75f;
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
				flag6 = ((Vector2)(ref move)).magnitude < MotionLibrary.WalkPush * 0.5f && Random.value < num2;
				text = "a steadier shot";
			}
			bool flag7 = Time.time - dodgedAt < 0.6f;
			bool flag8 = (crouchPlan && !flag6 && ((flag3 && flag7) || !flag2)) || (!crouchPlan && flag6 && (Reloading || flag4) && Time.time - crouchPlannedSince > 0.8f);
			if (flag6 != crouchPlan && (Time.time >= crouchPlanUntil || flag8))
			{
				crouchPlan = flag6;
				crouchPlannedSince = Time.time;
				crouchPlanUntil = Time.time + (flag6 ? Random.Range(2.5f, 5f) : Random.Range(2f, 4.5f));
				if (flag6)
				{
					duckDepth = 0f - Random.Range(0.5f, 0.8f);
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
				footLikes[i] = Random.Range(0.25f, 1.6f);
			}
			footLikes[5] *= Tuning.F("fight.still", 0.35f);
			footPace = Random.Range(0.75f, 1f);
			footLong = Random.Range(0.7f, 1.5f);
			footSide = ((Random.value < 0.5f) ? (-1f) : 1f);
			footUntil = Time.time + Random.Range(0f, 0.6f);
		}
		Vector3 val = head.position - Vector3.up * 0.4f;
		bool flag = Time.time - hurtAt < 0.35f;
		Vector3 outOfIt = Vector3.zero;
		bool flag2 = Tactics.InLineOf(MuzzleOf(shooter), val, Tuning.F("fight.dodge.wide", 0.55f), out outOfIt);
		Vector2 val3;
		if ((flag2 || flag) && Time.time - dodgedAt > Tuning.F("fight.dodge.every", 0.8f))
		{
			dodgedAt = Time.time;
			if (flag2)
			{
				Vector3 val2 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * outOfIt;
				footSide = ((Mathf.Abs(val2.x) > 0.15f) ? Mathf.Sign(val2.x) : (0f - footSide));
			}
			else
			{
				footSide = 0f - footSide;
			}
			val3 = new Vector2(footSide, Random.Range(-0.35f, 0.2f));
			footMove = ((Vector2)(ref val3)).normalized * MotionLibrary.RunPush;
			footUntil = Time.time + Random.Range(0.5f, 0.9f);
		}
		else if (Time.time > footUntil)
		{
			float num = 0f;
			for (int j = 0; j < footLikes.Length; j++)
			{
				num += ((j == 5 && footMove == Vector2.zero) ? 0f : footLikes[j]);
			}
			float num2 = Random.value * num;
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
			if (FriendBeside(val, out var side) && Random.value < 0.8f)
			{
				footSide = 0f - side;
			}
			else if (Random.value < 0.5f)
			{
				footSide = 0f - footSide;
			}
			float num5 = Mathf.Lerp(MotionLibrary.WalkPush, MotionLibrary.RunPush, Random.Range(0.5f, 1f)) * footPace;
			float num6 = Random.Range(0.6f, 1.4f) * footLong;
			switch (num3)
			{
			case 0:
				val3 = new Vector2(footSide, Random.Range(-0.2f, 0.2f));
				footMove = ((Vector2)(ref val3)).normalized * num5;
				break;
			case 1:
				val3 = new Vector2(footSide, Random.Range(0.5f, 1f));
				footMove = ((Vector2)(ref val3)).normalized * num5;
				break;
			case 2:
				val3 = new Vector2(footSide, 0f - Random.Range(0.5f, 1f));
				footMove = ((Vector2)(ref val3)).normalized * num5;
				break;
			case 3:
				val3 = new Vector2(footSide, Random.Range(-0.15f, 0.25f));
				footMove = ((Vector2)(ref val3)).normalized * MotionLibrary.RunPush;
				num6 = Random.Range(1.6f, 3.2f);
				break;
			case 4:
				val3 = new Vector2(footSide * Random.Range(0f, 0.35f), 1f);
				footMove = ((Vector2)(ref val3)).normalized * num5;
				num6 *= 0.7f;
				break;
			default:
				footMove = Vector2.zero;
				num6 = Random.Range(0.25f, 0.7f);
				break;
			}
			footUntil = Time.time + num6;
		}
		if (footMove != Vector2.zero && !RoomToStep(val, footMove))
		{
			footSide = 0f - footSide;
			footMove = new Vector2(0f - footMove.x, footMove.y);
			if (!RoomToStep(val, footMove))
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
			if (item == this || item.dead || item.enemy != enemy || !item.Whole || (Object)(object)item.Head == (Object)null)
			{
				continue;
			}
			Vector3 val = Flat(item.Head.position - chest);
			if (!(((Vector3)(ref val)).magnitude >= num))
			{
				Vector3 val2 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * val;
				if (!(Mathf.Abs(val2.x) < 0.2f))
				{
					num = ((Vector3)(ref val)).magnitude;
					side = Mathf.Sign(val2.x);
				}
			}
		}
		return side != 0f;
	}

	private bool RoomToStep(Vector3 chest, Vector2 push)
	{
		Quaternion val = Quaternion.Euler(0f, bodyYaw, 0f);
		Vector3 val2 = new Vector3(push.x, 0f, push.y);
		Vector3 val3 = val * ((Vector3)(ref val2)).normalized;
		RaycastHit val4 = default(RaycastHit);
		if (Physics.SphereCast(chest, 0.2f, val3, ref val4, 1.1f, -1, (QueryTriggerInteraction)1))
		{
			Rigidbody rigidbody = ((RaycastHit)(ref val4)).rigidbody;
			if ((!((Object)(object)((RaycastHit)(ref val4)).collider != (Object)null) || !((Object)(object)((Component)((RaycastHit)(ref val4)).collider).GetComponentInParent<RigManager>() == (Object)(object)rig)) && ((Object)(object)rigidbody == (Object)null || rigidbody.isKinematic || rigidbody.mass > 25f))
			{
				return false;
			}
		}
		return Physics.Raycast(chest + val3 * 0.9f, Vector3.down, 2.6f, -1, (QueryTriggerInteraction)1);
	}

	private void FollowBrainsTarget()
	{
		Targets.Target target = ((goal == Goal.Fight || goal == Goal.Guard || (goal == Goal.TakeCover && (coverReachedAt > 0f || !hasGoalPlace))) ? brainTarget : default(Targets.Target));
		if (!((Object)(object)target.transform == (Object)(object)this.target.transform))
		{
			this.target = target;
			targetSince = Time.time;
			shotsAtTarget = 0;
			reaction = Random.Range(0.15f, 0.4f) * personality.reaction * ((enemy && Furious) ? Tuning.F("fury.reaction", 0.5f) : 1f);
		}
	}

	private void PickOwnTarget(Transform head)
	{
		if ((Object)(object)target.transform == (Object)null && Targets.OnPlayerFor == ((Il2CppObjectBase)(object)rig).Pointer && Time.time - Targets.OnPlayerAt < 0.6f && !Reloading && Time.time - firedAt > Tuning.F("assist.quiet", 5f) && Time.time - lastEnemyTime > Tuning.F("assist.quiet", 5f))
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
		else if ((Object)(object)target.transform != (Object)null || Time.time - Targets.OnPlayerAt > 1.5f)
		{
			unsafeSince = -1f;
		}
		if (Time.time < targetCheckAt)
		{
			return;
		}
		targetCheckAt = Time.time + Tuning.F("brain.look.every", 0.15f);
		if ((Object)(object)target.transform != (Object)null && Targets.InSight(head.position, TargetPoint(), target.transform, rig) && (target.player || !Targets.NearPlayer(TargetPoint(), target.hostile ? 1.2f : 2.5f)))
		{
			lostTargetAt = -1f;
			if (!(Time.time - targetSince > Tuning.F("target.hold", 1.2f)) || !(Time.time >= nearerCheckAt))
			{
				return;
			}
			nearerCheckAt = Time.time + 0.6f;
			if (Targets.Find(head.position, 50f, rig, out var found) && (Object)(object)found.transform != (Object)(object)target.transform && found.hostile)
			{
				Vector3 val = TargetPoint() - head.position;
				float magnitude = ((Vector3)(ref val)).magnitude;
				val = found.Point - head.position;
				float magnitude2 = ((Vector3)(ref val)).magnitude;
				if (magnitude2 < magnitude * Tuning.F("target.nearer", 0.65f) && magnitude - magnitude2 > 3f)
				{
					Lab.Event("target: one nearer (" + magnitude2.ToString("0") + " m against " + magnitude.ToString("0") + " m): that one");
					target = found;
					targetSince = Time.time;
					shotsAtTarget = 0;
					reaction = Random.Range(0.15f, 0.4f) * personality.reaction * ((enemy && Furious) ? Tuning.F("fury.reaction", 0.5f) : 1f);
				}
			}
			return;
		}
		if (lostTargetAt < 0f)
		{
			lostTargetAt = Time.time;
		}
		if ((Object)(object)target.transform != (Object)null && Time.time - lostTargetAt <= ((coverPhase != 0) ? (Tuning.F("cover.keep", 15f) * 0.8f) : Tuning.F("target.lost", 1.5f)))
		{
			return;
		}
		if (Targets.Find(head.position, 50f, rig, out var found2))
		{
			bool num = (Object)(object)target.transform != (Object)null && (Object)(object)found2.transform == (Object)(object)target.transform;
			target = found2;
			if (!num)
			{
				targetSince = Time.time;
				shotsAtTarget = 0;
				reaction = Random.Range(0.15f, 0.4f) * personality.reaction * ((enemy && Furious) ? Tuning.F("fury.reaction", 0.5f) : 1f);
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
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				for (int i = 0; i < assemblies.Length; i++)
				{
					Type type = assemblies[i].GetType("ThirdEyeBonelabMod.Zombies.ZombieManager", throwOnError: false);
					if (!(type == null))
					{
						zombieHit = type.GetMethod("ArrowHit", BindingFlags.Static | BindingFlags.Public);
						FieldInfo field = type.GetField("BulletDamage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
						if (field != null)
						{
							zombieDamage = Convert.ToSingle(field.GetValue(null));
						}
					}
				}
			}
			if (zombieHit == null || (Object)(object)gun == (Object)null || (Object)(object)gun.firePointTransform == (Object)null)
			{
				return;
			}
			Transform firePointTransform = gun.firePointTransform;
			Transform val = (((Object)(object)Player.RigManager != (Object)null) ? ((Component)Player.RigManager).transform : null);
			float num = float.MaxValue;
			float num2 = float.MaxValue;
			bool flag = false;
			RaycastHit val2 = default(RaycastHit);
			foreach (RaycastHit item in Physics.RaycastAll(firePointTransform.position, firePointTransform.forward, 150f, -1, (QueryTriggerInteraction)1))
			{
				RaycastHit current = item;
				Collider collider = ((RaycastHit)(ref current)).collider;
				if ((Object)(object)collider == (Object)null)
				{
					continue;
				}
				Transform transform = ((Component)collider).transform;
				if (!transform.IsChildOf(((Component)gun).transform) && (!((Object)(object)val != (Object)null) || !transform.IsChildOf(val)))
				{
					bool flag2 = transform.IsChildOf(((Component)rig).transform);
					if (((RaycastHit)(ref current)).distance < num)
					{
						num = ((RaycastHit)(ref current)).distance;
						flag = flag2;
					}
					if (!flag2 && ((RaycastHit)(ref current)).distance < num2)
					{
						num2 = ((RaycastHit)(ref current)).distance;
						val2 = current;
					}
				}
			}
			if (Time.time - zombieSaidAt > 2f)
			{
				zombieSaidAt = Time.time;
				string[] obj = new string[6]
				{
					"shot: along the barrel first ",
					(num < float.MaxValue) ? (num.ToString("0.0") + " m" + (flag ? " (its own body)" : "")) : "nothing",
					"; past itself: ",
					((Object)(object)((RaycastHit)(ref val2)).collider != (Object)null) ? (((Object)((RaycastHit)(ref val2)).collider).name + " of " + ((Object)((Component)((RaycastHit)(ref val2)).collider).transform.root).name + " at " + num2.ToString("0.0") + " m") : "nothing",
					"; aiming at ",
					null
				};
				object obj2;
				if (!((Object)(object)target.transform != (Object)null))
				{
					obj2 = "nothing";
				}
				else
				{
					string[] obj3 = new string[6]
					{
						((Object)target.transform).name,
						" ",
						null,
						null,
						null,
						null
					};
					Vector3 val3 = target.Point - firePointTransform.position;
					obj3[2] = ((Vector3)(ref val3)).magnitude.ToString("0.0");
					obj3[3] = " m off, ";
					obj3[4] = Vector3.Angle(firePointTransform.forward, target.Point - firePointTransform.position).ToString("0.0");
					obj3[5] = " deg off the barrel";
					obj2 = string.Concat(obj3);
				}
				obj[5] = (string)obj2;
				Lab.Event(string.Concat(obj));
			}
			if (flag && !((Object)(object)((RaycastHit)(ref val2)).collider == (Object)null))
			{
				zombieHit.Invoke(null, new object[4]
				{
					((RaycastHit)(ref val2)).collider,
					((RaycastHit)(ref val2)).point,
					firePointTransform.forward,
					zombieDamage
				});
			}
		}
		catch (Exception)
		{
		}
	}

	private void NoteShot()
	{
		try
		{
			if ((Object)(object)target.transform == (Object)null || (Object)(object)gun == (Object)null || (Object)(object)gun.firePointTransform == (Object)null || target.player || (Object)(object)target.prop != (Object)null || !target.hostile)
			{
				return;
			}
			_ = Skill;
			Transform firePointTransform = gun.firePointTransform;
			bool flag = false;
			RaycastHit val = default(RaycastHit);
			if (Physics.Raycast(firePointTransform.position, firePointTransform.forward, ref val, 120f, -1, (QueryTriggerInteraction)1) && (Object)(object)((RaycastHit)(ref val)).collider != (Object)null)
			{
				Transform transform = ((Component)((RaycastHit)(ref val)).collider).transform;
				int num;
				if (!((Object)(object)transform == (Object)(object)target.transform) && !transform.IsChildOf(target.transform) && (!((Object)(object)target.prop != (Object)null) || !((Object)(object)((RaycastHit)(ref val)).rigidbody == (Object)(object)target.prop)))
				{
					if ((Object)(object)target.aim != (Object)null)
					{
						Vector3 val2 = ((RaycastHit)(ref val)).point - target.Point;
						num = ((((Vector3)(ref val2)).magnitude < 0.45f) ? 1 : 0);
					}
					else
					{
						num = 0;
					}
				}
				else
				{
					num = 1;
				}
				flag = (byte)num != 0;
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
				ShootSkill.End(((Il2CppObjectBase)(object)rig).Pointer, shootTrying, skillShots, skillHits, skillShooting);
				skillShots = (skillHits = 0);
				skillShooting = 0f;
				shootSkill = null;
			}
		}
		catch (Exception)
		{
		}
	}

	private Vector3 TargetPoint()
	{
		if (!((Object)(object)target.prop != (Object)null))
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
		float num3 = Random.value * num2;
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
		aimSpot += Random.insideUnitCircle * Tuning.F("aim.scatter", 0.22f) * (1f - num);
	}

	private Vector3 AimSpot(Vector3 from)
	{
		if (!AimsAtABody || Tuning.F("aim.spread", 1f) < 0.5f)
		{
			return Vector3.zero;
		}
		Vector3 val = Flat(TargetPoint() - from);
		return ((((Vector3)(ref val)).sqrMagnitude > 0.0001f) ? Vector3.Cross(Vector3.up, ((Vector3)(ref val)).normalized) : Vector3.right) * aimSpot.x + Vector3.up * aimSpot.y;
	}

	private void Fire(bool want)
	{
		if (!want)
		{
			Pull(on: false);
			return;
		}
		bool flag = (int)gun.fireMode == 2;
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
			tapLength = Random.Range(0.06f, 0.11f);
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
		catch (Exception)
		{
		}
	}

	private float PacedGap()
	{
		float num = 0.16f + Mathf.Clamp(shotRange, 0f, 25f) * 0.018f;
		try
		{
			int num2 = (((Object)(object)gun.defaultMagazine != (Object)null) ? gun.defaultMagazine.rounds : 15);
			if (num2 <= 10)
			{
				num *= 1.25f;
			}
			else if (num2 <= 17)
			{
				num *= 1.15f;
			}
		}
		catch (Exception)
		{
		}
		num *= Skill[2] * Random.Range(0.85f, 1.25f);
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
		float value = Random.value;
		if (!(value < 0.22f))
		{
			if (!(value < 0.85f))
			{
				return Random.Range(0.9f, 1.6f);
			}
			return Random.Range(0.32f, 0.7f);
		}
		return Random.Range(0.14f, 0.22f);
	}

	private int NextBurstRounds()
	{
		float num = Knowledge.Style.Sample(Knowledge.Style.burstRounds, -1f);
		if (num > 0f)
		{
			return Mathf.Clamp(Mathf.RoundToInt(num * personality.burst), 1, 12);
		}
		Transform head = Head;
		int num2;
		if ((Object)(object)target.transform != (Object)null && (Object)(object)head != (Object)null)
		{
			Vector3 val = TargetPoint() - head.position;
			num2 = ((((Vector3)(ref val)).magnitude < 5f) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		bool flag = (byte)num2 != 0;
		float value = Random.value;
		if (!(value < 0.3f))
		{
			if (!(value < 0.65f))
			{
				if (!(value < (flag ? 0.8f : 0.9f)))
				{
					return Random.Range(4, 7);
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
		int num2;
		if ((Object)(object)target.transform != (Object)null && (Object)(object)head != (Object)null)
		{
			Vector3 val = TargetPoint() - head.position;
			num2 = ((((Vector3)(ref val)).magnitude < 5f) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		bool flag = (byte)num2 != 0;
		float value = Random.value;
		if (!(value < 0.15f))
		{
			if (!(value < (flag ? 0.6f : 0.85f)))
			{
				return Random.Range(0.45f, 0.9f);
			}
			return Random.Range(0.12f, 0.35f);
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
		if (!(Random.value < 0.15f))
		{
			return Random.Range(0.3f, 0.9f);
		}
		return Random.Range(1f, 1.8f);
	}

	private void Pull(bool on)
	{
		if (on && trigger <= 0.5f)
		{
			triggerSince = Time.time;
		}
		if (!on && trigger > 0.5f && (Object)(object)gun != (Object)null)
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
		Transform transform = ((Component)item).transform;
		Pose pose = Pose.Of(transform).Inverse() * Pose.Of(firePointTransform);
		Vector3 val = pose.rotation * Vector3.forward;
		Transform val2 = (((Object)(object)gun.ammoSocket != (Object)null) ? ((Component)gun.ammoSocket).transform : ((Component)gun.triggerGrip).transform);
		Vector3 val3 = Quaternion.Inverse(transform.rotation) * (val2.position - transform.position);
		Vector3 val4 = Vector3.ProjectOnPlane(pose.position - val3, val);
		if (((Vector3)(ref val4)).sqrMagnitude < 1E-06f)
		{
			val4 = Vector3.ProjectOnPlane(pose.rotation * Vector3.up, val);
		}
		Quaternion val5 = Quaternion.LookRotation(val, ((Vector3)(ref val4)).normalized);
		return Quaternion.LookRotation(((Vector3)(ref dir)).normalized, Vector3.up) * Quaternion.Euler(wobble.y, wobble.x, 0f) * Quaternion.Euler(0f, 0f, Tuning.F(LongGun ? "hold.rifle.roll" : "hold.pistol.roll", 0f)) * Quaternion.Inverse(val5);
	}

	private bool AimLocked(Vector3 point, Transform head)
	{
		Transform firePointTransform = gun.firePointTransform;
		if ((Object)(object)firePointTransform == (Object)null || (Object)(object)gun.triggerGrip == (Object)null || (Object)(object)head == (Object)null)
		{
			return false;
		}
		Transform vrRoot = controls.vrRoot;
		Quaternion val = Quaternion.Euler(0f, bodyYaw, 0f);
		bool longGun = LongGun;
		float num = standingHeight;
		Vector3 val2 = (longGun ? Tuning.V("aim.rifle", new Vector3(0.12f, -0.33f, 0.3f)) : Tuning.V("aim.pistol", new Vector3(0.06f, -0.32f, 0.36f)));
		val2.y += num;
		Vector3 val3 = vrRoot.TransformPoint(val2);
		if ((Object)(object)rig.activeSeat != (Object)null)
		{
			Vector3 val4 = Flat(point - head.position);
			val4 = ((((Vector3)(ref val4)).sqrMagnitude > 0.01f) ? ((Vector3)(ref val4)).normalized : (Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward));
			val3 = head.position + val4 * Tuning.F("ride.reach", 0.45f) - Vector3.up * Tuning.F("ride.hand.down", 0.25f);
		}
		Pose pose = Pose.Of(((Component)item).transform);
		if (aimLockGun != ((Il2CppObjectBase)(object)gun).Pointer)
		{
			aimLockGun = ((Il2CppObjectBase)(object)gun).Pointer;
			aimLockFix = Quaternion.identity;
		}
		Vector3 val5 = Quaternion.Inverse(pose.rotation) * (((Component)gun.triggerGrip).transform.position - pose.position);
		Vector3 val6 = Quaternion.Inverse(pose.rotation) * (firePointTransform.position - pose.position);
		Quaternion val7 = GunTurnAlong(point - firePointTransform.position);
		for (int i = 0; i < 2; i++)
		{
			Vector3 val8 = val3 - val7 * val5;
			val7 = GunTurnAlong(point - (val8 + val7 * val6));
		}
		Vector3 val9 = point - firePointTransform.position;
		if (((Vector3)(ref val9)).sqrMagnitude > 0.01f && Time.time - firedAt > 0.35f)
		{
			Quaternion val10 = Quaternion.FromToRotation(firePointTransform.forward, ((Vector3)(ref val9)).normalized);
			aimLockFix = Quaternion.Slerp(Quaternion.identity, val10, Mathf.Clamp01(Time.deltaTime * Tuning.F("aim.fix.rate", 2.5f))) * aimLockFix;
			if (Quaternion.Angle(Quaternion.identity, aimLockFix) > Tuning.F("aim.fix.most", 12f))
			{
				aimLockFix = Quaternion.Slerp(Quaternion.identity, aimLockFix, 0.5f);
			}
		}
		val7 = aimLockFix * val7;
		Pose pose2 = new Pose(val3 - val7 * val5, val7);
		if (Time.time - aimedSince > 0.3f)
		{
			aimedWantSet = false;
		}
		aimedSince = Time.time;
		float num2 = Mathf.Clamp01(Time.deltaTime * Tuning.F("aim.ease", 10f));
		aimedWant = (aimedWantSet ? new Pose(Vector3.Lerp(aimedWant.position, pose2.position, num2), Quaternion.Slerp(aimedWant.rotation, pose2.rotation, num2)) : pose2);
		if (!aimedWantSet)
		{
			aimedWant = new Pose(Vector3.Lerp(pose.position, pose2.position, 0.15f), Quaternion.Slerp(pose.rotation, pose2.rotation, 0.15f));
			aimedWantSet = true;
		}
		Pose pose3 = aimedWant;
		float num3 = Time.time - firedAt;
		if (num3 >= 0f && num3 < 0.4f)
		{
			float num4 = Mathf.Exp((0f - num3) / Tuning.F("gun.kick.time", 0.07f)) * (longGun ? 0.6f : 1f);
			pose3 = new Pose(pose3.position - pose3.rotation * Vector3.forward * (0.02f * num4), pose3.rotation * Quaternion.Euler((0f - Tuning.F("gun.kick", 4f)) * num4, 0f, 0f));
		}
		Pose pose4 = pose3 * gunInController.Inverse();
		if (Time.time - aimLockSaidAt > 2f)
		{
			aimLockSaidAt = Time.time;
			Vector3 val11 = pose3.rotation * (Quaternion.Inverse(pose.rotation) * firePointTransform.forward);
			string[] obj = new string[14]
			{
				"aimlock: gun off its place ",
				Quaternion.Angle(pose.rotation, pose3.rotation).ToString("0.0"),
				" deg, ",
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null
			};
			Vector3 val12 = pose.position - pose3.position;
			obj[3] = ((Vector3)(ref val12)).magnitude.ToString("0.00");
			obj[4] = " m; planned barrel off the point ";
			obj[5] = Vector3.Angle(val11, point - (pose3.position + pose3.rotation * val6)).ToString("0.0");
			obj[6] = " deg; muzzle off the point ";
			obj[7] = Vector3.Angle(firePointTransform.forward, point - firePointTransform.position).ToString("0.0");
			obj[8] = " deg; controller off its want ";
			obj[9] = Quaternion.Angle(((Component)((ControllerRig)controls).rightController).transform.rotation, pose4.rotation).ToString("0.0");
			obj[10] = " deg; made up ";
			obj[11] = Quaternion.Angle(Quaternion.identity, aimLockFix).ToString("0.0");
			obj[12] = " deg; steady ";
			obj[13] = GunSteady().ToString();
			Lab.Event(string.Concat(obj));
		}
		if (!(Quaternion.Angle(pose.rotation, pose3.rotation) > Tuning.F("gun.lock.retake", 25f)) || !(Quaternion.Angle(((Component)((ControllerRig)controls).rightController).transform.rotation, pose4.rotation) < 10f))
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
		handTurn = Quaternion.Inverse(val) * pose4.rotation;
		playHeight = num * 0.985f;
		if (supportHeld && step == Step.Ready && leftOnGunSet)
		{
			Pose pose5 = Pose.Of(((Component)item).transform) * leftOnGun;
			leftOverride = true;
			leftAt = vrRoot.InverseTransformPoint(pose5.position);
			leftTurn = Quaternion.Inverse(val) * pose5.rotation;
		}
		Vector3 val13 = Quaternion.Inverse(val) * (point - head.position);
		float y = val13.y;
		Vector2 val14 = new Vector2(val13.x, val13.z);
		playPitch = Mathf.Clamp((0f - Mathf.Atan2(y, Mathf.Max(((Vector2)(ref val14)).magnitude, 0.1f))) * 57.29578f, -50f, 60f);
		playYaw = Mathf.Clamp(Mathf.Atan2(val13.x, val13.z) * 57.29578f, -50f, 50f);
		return true;
	}

	private void Aim(Vector3 point, Transform head, Hold hold)
	{
		Hand rightHand = RightHand;
		Transform firePointTransform = gun.firePointTransform;
		BaseController rightController = ((ControllerRig)controls).rightController;
		if ((Object)(object)rightHand == (Object)null || (Object)(object)firePointTransform == (Object)null || (Object)(object)rightController == (Object)null || (Object)(object)item == (Object)null)
		{
			return;
		}
		bool flag = (Object)(object)rig.activeSeat != (Object)null;
		if ((hold != Hold.Reload && !flag && HoldFromRecording(point, head, hold == Hold.Aimed)) || (hold != Hold.Reload && !flag && HoldLearned(point, head, hold == Hold.Aimed)) || (hold == Hold.Aimed && lockSet && Tuning.F("aim.locked", 1f) > 0.5f && AimLocked(point, head)))
		{
			return;
		}
		Transform vrRoot = controls.vrRoot;
		Quaternion val = Quaternion.Euler(0f, bodyYaw, 0f);
		Transform transform = ((Component)item).transform;
		if (!supportHeld)
		{
			Pose pose = Pose.Of(((Component)rightController).transform).Inverse() * Pose.Of(((Component)rightHand).transform);
			if (((Vector3)(ref pose.position)).magnitude < Tuning.F("hold.free.near", 0.15f))
			{
				rightOnController = (handMeasured ? Pose.Lerp(rightOnController, pose, 0.08f) : pose);
				handMeasured = true;
			}
		}
		if (!supportHeld || !rightInGunSet)
		{
			rightInGun = Pose.Of(transform).Inverse() * Pose.Of(((Component)rightHand).transform);
			rightInGunSet = true;
		}
		Pose pose2 = rightInGun;
		Pose pose3 = Pose.Of(transform).Inverse() * Pose.Of(firePointTransform);
		Vector3 val2 = point - firePointTransform.position;
		if (((Vector3)(ref val2)).sqrMagnitude < 0.0001f)
		{
			return;
		}
		Vector3 val3 = pose3.rotation * Vector3.forward;
		Transform val4 = (((Object)(object)gun.ammoSocket != (Object)null) ? ((Component)gun.ammoSocket).transform : ((Component)gun.triggerGrip).transform);
		Vector3 val5 = Quaternion.Inverse(transform.rotation) * (val4.position - transform.position);
		Vector3 val6 = Vector3.ProjectOnPlane(pose3.position - val5, val3);
		if (((Vector3)(ref val6)).sqrMagnitude < 1E-06f)
		{
			val6 = Vector3.ProjectOnPlane(pose3.rotation * Vector3.up, val3);
		}
		Quaternion val7 = Quaternion.LookRotation(val3, ((Vector3)(ref val6)).normalized);
		labGunUp = transform.rotation * ((Vector3)(ref val6)).normalized;
		Quaternion val8 = Quaternion.LookRotation(((Vector3)(ref val2)).normalized, Vector3.up) * Quaternion.Euler(wobble.y, wobble.x, 0f) * Quaternion.Euler(0f, 0f, Tuning.F(LongGun ? "hold.rifle.roll" : "hold.pistol.roll", 0f)) * Quaternion.Inverse(val7);
		labWantUp = val8 * ((Vector3)(ref val6)).normalized;
		bool longGun = LongGun;
		float num = standingHeight;
		Vector3 val9 = (Vector3)(hold switch
		{
			Hold.Low => longGun ? Tuning.V("hold.rifle.low", new Vector3(0.14f, -0.55f, 0.22f)) : Tuning.V("hold.pistol.low", new Vector3(0.15f, -0.6f, 0.28f)), 
			Hold.Reload => Tuning.V("hold.reload", new Vector3(0.07f, -0.42f, 0.24f)), 
			_ => longGun ? Tuning.V("aim.rifle", new Vector3(0.12f, -0.33f, 0.3f)) : Tuning.V("aim.pistol", new Vector3(0.06f, -0.32f, 0.36f)), 
		});
		val9.y += num;
		Vector3 val10 = vrRoot.TransformPoint(val9);
		Pose pose4 = new Pose(val10 - val8 * pose2.position, val8);
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
		handTurn = Quaternion.Inverse(val) * pose7.rotation;
		playHeight = num * 0.985f;
		Vector3 val11 = Quaternion.Inverse(val) * (point - head.position);
		if (hold == Hold.Reload)
		{
			playPitch = 28f;
			playYaw = 6f;
			return;
		}
		float y = val11.y;
		Vector2 val12 = new Vector2(val11.x, val11.z);
		playPitch = Mathf.Clamp((0f - Mathf.Atan2(y, Mathf.Max(((Vector2)(ref val12)).magnitude, 0.1f))) * 57.29578f, -50f, 60f);
		playYaw = Mathf.Clamp(Mathf.Atan2(val11.x, val11.z) * 57.29578f, -50f, 50f);
	}

	private void MeasureFreeHands()
	{
		Hand rightHand = RightHand;
		BaseController rightController = ((ControllerRig)controls).rightController;
		MeasureFreeLeft();
		if (!((Object)(object)rightHand == (Object)null) && !((Object)(object)rightController == (Object)null) && !((Object)(object)rightHand.m_CurrentAttachedGO != (Object)null))
		{
			Pose pose = Pose.Of(((Component)rightController).transform).Inverse() * Pose.Of(((Component)rightHand).transform);
			freeRight = (freeRightSet ? Pose.Lerp(freeRight, pose, 0.05f) : pose);
			freeRightSet = true;
		}
	}

	private void MeasureFreeLeft()
	{
		Hand leftHand = LeftHand;
		BaseController leftController = ((ControllerRig)controls).leftController;
		if (!((Object)(object)leftHand == (Object)null) && !((Object)(object)leftController == (Object)null) && !((Object)(object)leftHand.m_CurrentAttachedGO != (Object)null))
		{
			Pose pose = Pose.Of(((Component)leftController).transform).Inverse() * Pose.Of(((Component)leftHand).transform);
			freeLeft = (freeLeftSet ? Pose.Lerp(freeLeft, pose, 0.05f) : pose);
			freeLeftSet = true;
		}
	}

	private static void Correct(ref Pose fix, Pose want, Transform hand)
	{
		float num = Mathf.Clamp01(Time.deltaTime * Tuning.F("hold.fix.rate", 1.5f));
		float num2 = Tuning.F("hold.fix.most", 0.5f);
		fix.position = Vector3.ClampMagnitude(fix.position + (want.position - hand.position) * num, num2);
		Quaternion val = want.rotation * Quaternion.Inverse(hand.rotation);
		fix.rotation = Quaternion.Slerp(Quaternion.identity, val, num) * fix.rotation;
		float num3 = Quaternion.Angle(Quaternion.identity, fix.rotation);
		float num4 = Tuning.F("hold.fix.degrees", 120f);
		if (num3 > num4)
		{
			fix.rotation = Quaternion.Slerp(Quaternion.identity, fix.rotation, num4 / num3);
		}
	}

	private void NextStep(Step next)
	{
		Lab.Event("hold: " + step.ToString() + " -> " + next.ToString() + " after " + (Time.time - stepAt).ToString("0.00") + " s");
		step = next;
		stepAt = Time.time;
		reachFix = Vector3.zero;
		if (next == Step.Reach && (Object)(object)((ControllerRig)controls).leftController != (Object)null)
		{
			leftStart = Pose.Of(((Component)((ControllerRig)controls).leftController).transform);
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
		if (!lockSet || !leftMeasured || (Object)(object)item == (Object)null || Tuning.F("front.yours", 1f) < 0.5f)
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
		Pose pose4 = Pose.Of(((Component)item).transform) * pose3;
		how = "you hold it";
		Vector3 val = pose4.position - result.position;
		return new Pose((((Vector3)(ref val)).magnitude < Tuning.F("front.yours.near", 0.12f)) ? pose4.position : result.position, pose4.rotation);
	}

	private Vector3 GunUp()
	{
		try
		{
			Transform firePointTransform = gun.firePointTransform;
			Transform val = (((Object)(object)gun.ammoSocket != (Object)null) ? ((Component)gun.ammoSocket).transform : ((Component)gun.triggerGrip).transform);
			Vector3 val2 = Vector3.ProjectOnPlane(firePointTransform.position - val.position, firePointTransform.forward);
			if (((Vector3)(ref val2)).sqrMagnitude > 1E-06f)
			{
				return ((Vector3)(ref val2)).normalized;
			}
		}
		catch (Exception)
		{
		}
		return Vector3.up;
	}

	private static Pose HandOnGrip(Grip g, Hand hand)
	{
		try
		{
			SimpleTransform val = g.SolveHandTarget(hand);
			if (val.rotation.x != 0f || val.rotation.y != 0f || val.rotation.z != 0f || val.rotation.w != 0f)
			{
				return new Pose(val.position, val.rotation);
			}
		}
		catch (Exception)
		{
		}
		return Pose.Of(((Component)g).transform);
	}

	private bool GunSteady()
	{
		if (!((Object)(object)item == (Object)null))
		{
			Vector3 val = item.angularVelocity;
			if (((Vector3)(ref val)).magnitude < Tuning.F("hold.steady.spin", 1.5f))
			{
				val = item.velocity;
				return ((Vector3)(ref val)).magnitude < Tuning.F("hold.steady.speed", 0.8f);
			}
			return false;
		}
		return true;
	}

	private void HoldSupport()
	{
		GunMoves.Hold hold = (((Object)(object)support != (Object)null) ? LearnedHold(aimed: true) : null);
		if (hold != null && hold.left != null && Tuning.F("learned.hold.aim", 0f) > 0.5f)
		{
			HoldSupportLearned();
			return;
		}
		Hand val = (((Object)(object)rig.physicsRig != (Object)null) ? rig.physicsRig.leftHand : null);
		Vector3 val3;
		if ((Object)(object)support == (Object)null && (Object)(object)frontRetry != (Object)null && (Object)(object)item != (Object)null && Time.time >= frontRetryAt)
		{
			Rigidbody val2 = Pelvis();
			int num;
			if (!((Object)(object)val2 == (Object)null))
			{
				val3 = Flat(val2.velocity);
				num = ((((Vector3)(ref val3)).magnitude < Tuning.F("front.retry.speed", 1.2f)) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
			bool flag = (byte)num != 0;
			if ((Object)(object)((Component)frontRetry).GetComponentInParent<Gun>() != (Object)(object)gun)
			{
				frontRetry = null;
			}
			else if (flag)
			{
				support = frontRetry;
				frontRetry = null;
				Lab.Event("front hand: trying " + ((Object)support).name + " again");
				NextStep(Step.Reach);
			}
			else
			{
				frontRetryAt = Time.time + 0.5f;
			}
		}
		float num2 = Time.time - stepAt;
		if ((Object)(object)support == (Object)null || (Object)(object)val == (Object)null || (Object)(object)item == (Object)null)
		{
			supportHeld = false;
			if (step != Step.Ready && num2 > 0.7f && (GunSteady() || num2 > Tuning.F("hold.onehand.most", 1.4f)))
			{
				NextStep(Step.Ready);
			}
			return;
		}
		supportHeld = (Object)(object)val.m_CurrentAttachedGO != (Object)null && support.attachedHands != null && support.attachedHands.Contains(val);
		Transform vrRoot = controls.vrRoot;
		Transform transform = ((Component)item).transform;
		BaseController leftController = ((ControllerRig)controls).leftController;
		Quaternion val4 = Quaternion.Euler(0f, bodyYaw, 0f);
		if (!supportHeld && (Object)(object)leftController != (Object)null)
		{
			Pose pose = Pose.Of(((Component)leftController).transform).Inverse() * Pose.Of(((Component)val).transform);
			if (!(((Vector3)(ref pose.position)).magnitude < Tuning.F("hold.free.near", 0.15f)))
			{
				goto IL_0325;
			}
			if (!((Object)(object)val.rb == (Object)null))
			{
				val3 = val.rb.velocity;
				float magnitude = ((Vector3)(ref val3)).magnitude;
				float num3 = Tuning.F("hold.free.calm", 0.5f);
				float num4;
				if (!((Object)(object)Pelvis() != (Object)null))
				{
					num4 = 0f;
				}
				else
				{
					val3 = Pelvis().velocity;
					num4 = ((Vector3)(ref val3)).magnitude;
				}
				if (!(magnitude < num3 + num4))
				{
					goto IL_0325;
				}
			}
			leftOnController = (leftMeasured ? Pose.Lerp(leftOnController, pose, 0.08f) : pose);
			leftMeasured = true;
		}
		goto IL_0359;
		IL_0ec3:
		float num5;
		float num6;
		bool flag2 = num5 >= num6 && ((settleCalmSince >= 0f && Time.time - settleCalmSince >= 0.2f) || num5 >= Tuning.F("hold.settle.most", 1.5f));
		Pose pose2;
		if (step == Step.Settle && !flag2)
		{
			leftOnHeld = ((num5 < 0.06f || !leftOnHeldSet) ? pose2 : Pose.Lerp(leftOnHeld, pose2, 0.25f));
			leftOnHeldSet = true;
			leftOverride = true;
			if (Time.time - twoHandSince < 0.6f || !leftInGunSet)
			{
				leftInGun = Pose.Of(transform).Inverse() * Pose.Of(((Component)val).transform);
			}
			return;
		}
		if (step == Step.Settle)
		{
			leftInGun = Pose.Of(transform).Inverse() * Pose.Of(((Component)val).transform);
			leftInGunSet = true;
			leftOnHeld = pose2;
			leftOnHeldSet = true;
			Lab.Event("front hand settled on " + ((Object)support).name + " after " + num5.ToString("0.00") + " s (" + ((settleCalmSince >= 0f) ? "calm" : "NOT calm") + "), its place on the gun taken");
			NextStep(Step.Ready);
		}
		leftOverride = true;
		SteadyMeter();
		if (frontKept && !leftInGunSet)
		{
			leftInGun = Pose.Of(transform).Inverse() * Pose.Of(((Component)val).transform);
			leftInGunSet = true;
		}
		else if (!frontKept && !leftInGunSet)
		{
			leftInGun = Pose.Of(transform).Inverse() * Pose.Of(((Component)val).transform);
			leftInGunSet = true;
		}
		Pose pose3 = Pose.Of(transform) * leftInGun;
		Pose obj = (gunWantSet ? Pose.Lerp(pose3, gunWant * leftInGun, Tuning.F("hold.left.want", 1f)) : pose3);
		leftFix = Pose.identity;
		Pose pose4 = obj * (leftOnHeldSet ? leftOnHeld : leftOnController).Inverse();
		leftAt = vrRoot.InverseTransformPoint(pose4.position);
		leftTurn = Quaternion.Inverse(val4) * pose4.rotation;
		return;
		IL_0325:
		if (leftMeasured && ((Vector3)(ref leftOnController.position)).magnitude > Tuning.F("hold.free.near", 0.15f))
		{
			leftOnController = Pose.identity;
		}
		goto IL_0359;
		IL_0359:
		switch (step)
		{
		case Step.OneHand:
			leftOverride = false;
			if (num2 > Tuning.F("hold.onehand.time", 0.7f) && (GunSteady() || num2 > Tuning.F("hold.onehand.most", 1.4f)))
			{
				NextStep(Step.Reach);
			}
			return;
		case Step.Reach:
		{
			if (supportHeld)
			{
				Lab.Event("front hand on " + ((Object)support).name + ": taken by the game (hand " + (frontGap * 100f).ToString("0.0") + " cm, " + frontOff.ToString("0") + " deg off as it arrived)");
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
			if ((Object)(object)val.m_CurrentAttachedGO != (Object)null)
			{
				Lab.Event("front hand: the game took hold of " + ((Object)val.m_CurrentAttachedGO).name + ", not " + ((Object)support).name + ": let go");
				FreeHand(val);
				frontLetUntil = -10f;
				frontArrivedAt = -1f;
			}
			leftOverride = true;
			float num7 = Mathf.SmoothStep(0f, 1f, num2 / Tuning.F("hold.reach.time", 0.7f));
			string how;
			Pose pose5 = FrontHandTarget(val, out how);
			Pose b = pose5 * leftOnController.Inverse();
			Vector3 val5 = GunUp() * (0f - Tuning.F("front.under", 0.1f));
			Pose pose6 = new Pose(b.position + val5, b.rotation);
			Pose pose7 = ((num7 < 0.65f) ? Pose.Lerp(leftStart, pose6, Mathf.SmoothStep(0f, 1f, num7 / 0.65f)) : Pose.Lerp(pose6, b, Mathf.SmoothStep(0f, 1f, (num7 - 0.65f) / 0.35f)));
			leftAt = vrRoot.InverseTransformPoint(pose7.position);
			leftTurn = Quaternion.Inverse(val4) * pose7.rotation;
			val3 = ((Component)val).transform.position - pose5.position;
			float magnitude2 = ((Vector3)(ref val3)).magnitude;
			frontClosing = Mathf.Clamp01(1f - (magnitude2 - 0.03f) / 0.15f);
			frontClosingAt = Time.time;
			float num8 = Quaternion.Angle(((Component)val).transform.rotation, pose5.rotation);
			if ((Object)(object)item != (Object)null)
			{
				float num9 = frontBefore * 0.97f;
				val3 = item.angularVelocity;
				frontBefore = Mathf.Max(num9, ((Vector3)(ref val3)).magnitude * 57.29578f);
			}
			bool flag3 = num2 > 1.6f;
			if (num7 >= 1f && magnitude2 < (flag3 ? Tuning.F("front.there.late", 0.04f) : Tuning.F("front.there", 0.025f)) && num8 < (flag3 ? 15f : Tuning.F("front.there.turn", 10f)))
			{
				if (frontArrivedAt < 0f)
				{
					frontArrivedAt = Time.time;
					frontGap = magnitude2;
					frontOff = num8;
				}
				frontLetUntil = Time.time + 0.1f;
			}
			else if (frontArrivedAt >= 0f && magnitude2 > 0.05f)
			{
				frontArrivedAt = -1f;
			}
			if (frontArrivedAt >= 0f && Time.time - frontArrivedAt > Tuning.F("front.wait", 0.3f))
			{
				supportHeld = Grab(val, support, pose5.rotation);
				Lab.Event((supportHeld ? "front hand on " : "front hand missed ") + ((Object)support).name + ": FORCED, the game had not taken hold (hand " + (magnitude2 * 100f).ToString("0.0") + " cm, " + num8.ToString("0") + " deg off, turned as " + how + ")");
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
			if (!supportHeld && num2 > 2.5f)
			{
				BaseController leftController2 = ((ControllerRig)controls).leftController;
				Vector3 val6 = vrRoot.TransformPoint(leftAt);
				Transform val7 = (((Object)(object)rig.physicsRig != (Object)null && (Object)(object)((Rig)rig.physicsRig).m_shoulderLf != (Object)null) ? ((Rig)rig.physicsRig).m_shoulderLf : null);
				string[] obj2 = new string[22]
				{
					"front hand gave up on ",
					((Object)support).name,
					" (gap ",
					magnitude2.ToString("0.00"),
					" m), for now: its controller ",
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null
				};
				object obj3;
				if (!((Object)(object)leftController2 != (Object)null))
				{
					obj3 = "?";
				}
				else
				{
					val3 = ((Component)leftController2).transform.position - val6;
					obj3 = ((Vector3)(ref val3)).magnitude.ToString("0.00");
				}
				obj2[5] = (string)obj3;
				obj2[6] = " m from where sent, the hand ";
				object obj4;
				if (!((Object)(object)leftController2 != (Object)null))
				{
					obj4 = "?";
				}
				else
				{
					val3 = ((Component)val).transform.position - ((Component)leftController2).transform.position;
					obj4 = ((Vector3)(ref val3)).magnitude.ToString("0.00");
				}
				obj2[7] = (string)obj4;
				obj2[8] = " m from its controller, the grip ";
				object obj5;
				if (!((Object)(object)val7 != (Object)null))
				{
					obj5 = "?";
				}
				else
				{
					val3 = pose5.position - val7.position;
					obj5 = ((Vector3)(ref val3)).magnitude.ToString("0.00");
				}
				obj2[9] = (string)obj5;
				obj2[10] = " m from its shoulder, the gun ";
				obj2[11] = (gunWantSet ? Quaternion.Angle(gunWant.rotation, transform.rotation).ToString("0") : "?");
				obj2[12] = " deg off its want; the hand holds ";
				obj2[13] = (((Object)(object)val.m_CurrentAttachedGO != (Object)null) ? ((Object)val.m_CurrentAttachedGO).name : "nothing");
				obj2[14] = ", at ";
				val3 = ((Component)val).transform.position;
				obj2[15] = ((Vector3)(ref val3)).ToString("0.0");
				obj2[16] = " moving ";
				object obj6;
				if (!((Object)(object)val.rb != (Object)null))
				{
					obj6 = "?";
				}
				else
				{
					val3 = val.rb.velocity;
					obj6 = ((Vector3)(ref val3)).magnitude.ToString("0.0");
				}
				obj2[17] = (string)obj6;
				obj2[18] = " m/s, its controller at ";
				object obj7;
				if (!((Object)(object)leftController2 != (Object)null))
				{
					obj7 = "?";
				}
				else
				{
					val3 = ((Component)leftController2).transform.position;
					obj7 = ((Vector3)(ref val3)).ToString("0.0");
				}
				obj2[19] = (string)obj7;
				obj2[20] = ", the bot at ";
				val3 = Feet;
				obj2[21] = ((Vector3)(ref val3)).ToString("0.0");
				Lab.Event(string.Concat(obj2));
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
		if (!frontJoltSaid && (Object)(object)item != (Object)null)
		{
			if (!frontCounted)
			{
				frontCounted = true;
				frontHolds++;
				frontKept = false;
			}
			val3 = item.angularVelocity;
			float num10 = ((Vector3)(ref val3)).magnitude * 57.29578f;
			if (num10 > frontJolt)
			{
				frontJolt = num10;
				BaseController leftController3 = ((ControllerRig)controls).leftController;
				BaseController rightController = ((ControllerRig)controls).rightController;
				float num11;
				if (!((Object)(object)leftController3 != (Object)null))
				{
					num11 = -1f;
				}
				else
				{
					val3 = ((Component)val).transform.position - (Pose.Of(((Component)leftController3).transform) * (leftOnHeldSet ? leftOnHeld : leftOnController)).position;
					num11 = ((Vector3)(ref val3)).magnitude;
				}
				frontLeftOff = num11;
				Hand rightHand = RightHand;
				float num12;
				if (!((Object)(object)rightController != (Object)null) || !((Object)(object)rightHand != (Object)null))
				{
					num12 = -1f;
				}
				else
				{
					val3 = ((Component)rightHand).transform.position - ((Component)rightController).transform.position;
					num12 = ((Vector3)(ref val3)).magnitude;
				}
				frontRightOff = num12;
			}
			if (Time.time - twoHandSince > 0.6f)
			{
				frontJoltSaid = true;
				frontCounted = false;
				Lab.Event("front hand HOLD #" + frontHolds + " (" + (frontKept ? "hand KEPT as it took hold" : "hand measured again for 0.6 s") + ", " + (frontForced ? "forced" : "by the game") + "): gun turned at most " + frontJolt.ToString("0") + " deg/s after, " + frontBefore.ToString("0") + " before; at the worst, left hand " + (frontLeftOff * 100f).ToString("0") + " cm from where sent, right hand " + (frontRightOff * 100f).ToString("0") + " cm from its controller");
				frontBefore = 0f;
			}
		}
		num5 = Time.time - twoHandSince;
		num6 = Tuning.F("hold.settle.time", 0.45f);
		pose2 = (((Object)(object)leftController != (Object)null) ? (Pose.Of(((Component)leftController).transform).Inverse() * Pose.Of(((Component)val).transform)) : Pose.identity);
		Rigidbody val8 = Pelvis();
		val3 = item.angularVelocity;
		if (((Vector3)(ref val3)).magnitude * 57.29578f < Tuning.F("hold.calm.spin", 20f))
		{
			val3 = item.velocity - (((Object)(object)val8 != (Object)null) ? val8.velocity : Vector3.zero);
			if (((Vector3)(ref val3)).magnitude < Tuning.F("hold.calm.speed", 0.12f))
			{
				if (settleCalmSince < 0f)
				{
					settleCalmSince = Time.time;
				}
				goto IL_0ec3;
			}
		}
		settleCalmSince = -1f;
		goto IL_0ec3;
	}

	private void SteadyMeter()
	{
		if (!((Object)(object)item == (Object)null) && !(Time.time - twoHandSince < 1f) && !(Time.time - firedAt < 0.5f))
		{
			if (steadyFrom < 0f)
			{
				steadyFrom = Time.time;
			}
			Vector3 angularVelocity = item.angularVelocity;
			float num = ((Vector3)(ref angularVelocity)).magnitude * 57.29578f;
			steadyMost = Mathf.Max(steadyMost, num);
			steadySum += num;
			steadyN++;
			if (Time.time - steadyFrom >= 5f)
			{
				Lab.Event("two-hand steady: gun turning at most " + steadyMost.ToString("0") + " deg/s, mean " + (steadySum / (float)Mathf.Max(1, steadyN)).ToString("0") + " deg/s over 5 s (" + (((Object)(object)support != (Object)null) ? ((Object)support).name : "?") + ")");
				steadyFrom = -1f;
				steadyMost = (steadySum = 0f);
				steadyN = 0;
			}
		}
	}

	private void OneHandMeter()
	{
		BaseController rightController = ((ControllerRig)controls).rightController;
		if ((Object)(object)item == (Object)null || (Object)(object)rightController == (Object)null || (Object)(object)support != (Object)null)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		Hand rightHand = RightHand;
		Rigidbody val = Pelvis();
		if ((Object)(object)rightHand == (Object)null || (Object)(object)val == (Object)null)
		{
			return;
		}
		Quaternion val2 = Quaternion.Inverse(((Component)rightHand).transform.rotation) * item.rotation;
		Quaternion val3 = Quaternion.Inverse(val.rotation) * item.rotation;
		if (meterSet && deltaTime > 0.0001f)
		{
			float num = Quaternion.Angle(val2, meterRel) / deltaTime;
			if (Time.time - firedAt < 0.5f)
			{
				meterShotSum += num;
				meterShotN++;
			}
			else
			{
				meterRelSum += num;
				meterRelMost = Mathf.Max(meterRelMost, num);
				meterCtlSum += Quaternion.Angle(val3, meterCtl) / deltaTime;
				meterN++;
			}
		}
		meterRel = val2;
		meterCtl = val3;
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
		foreach (Rigidbody componentsInChild in ((Component)GunWeight.RootOf((Component)(object)gun)).GetComponentsInChildren<Rigidbody>())
		{
			num2 += (((Object)(object)componentsInChild != (Object)null && !componentsInChild.isKinematic) ? componentsInChild.mass : 0f);
		}
		Lab.Event("one-hand steady (" + (LongGun ? "long" : "pistol") + ", " + num2.ToString("0.00") + " kg): gun turning in its grip mean " + (meterRelSum / (float)Mathf.Max(1, meterN)).ToString("0") + " deg/s, most " + meterRelMost.ToString("0") + "; gun turning against its hips mean " + (meterCtlSum / (float)Mathf.Max(1, meterN)).ToString("0") + " deg/s; after shots in hand " + (meterShotSum / (float)Mathf.Max(1, meterShotN)).ToString("0") + " deg/s (" + meterShotN + " frames)");
		meterFrom = -1f;
		meterRelSum = (meterRelMost = (meterCtlSum = (meterShotSum = 0f)));
		meterN = (meterShotN = 0);
	}

	private static string GripKey(Gun g, Grip grip)
	{
		string text = ((Object)g).name;
		int num = text.IndexOf(" [", StringComparison.Ordinal);
		if (num > 0)
		{
			text = text.Substring(0, num);
		}
		return text.Replace("(Clone)", "").Trim() + "/" + ((Object)grip).name;
	}

	private static bool WorksBolt(Gun g, Grip grip)
	{
		if ((Object)(object)g != (Object)null && (Object)(object)grip != (Object)null)
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
			flag = (Object)(object)gun != (Object)null && (Object)(object)support != (Object)null && supportHeld && !Reloading && ((int)gun.slideState == 2 || (int)gun.slideState == 3);
		}
		catch (Exception)
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
			string[] obj = new string[7]
			{
				"front hand: the ",
				((Object)support).name,
				" works the bolt of the ",
				((Object)gun).name,
				" (",
				null,
				null
			};
			SlideStates slideState = gun.slideState;
			obj[5] = ((object)(SlideStates)(ref slideState)).ToString();
			obj[6] = "): let go, another grip";
			Lab.Event(string.Concat(obj));
			LetGoOfSupport();
			support = SupportGrip(gun);
			step = (((Object)(object)support != (Object)null) ? Step.Reach : Step.Ready);
			stepAt = Time.time;
			if ((Object)(object)((ControllerRig)controls).leftController != (Object)null)
			{
				leftStart = Pose.Of(((Component)((ControllerRig)controls).leftController).transform);
			}
		}
	}

	private void LetGoOfSupport()
	{
		Hand val = (((Object)(object)rig.physicsRig != (Object)null) ? rig.physicsRig.leftHand : null);
		try
		{
			if ((Object)(object)val != (Object)null && (Object)(object)support != (Object)null && support.attachedHands != null && support.attachedHands.Contains(val))
			{
				support.ForceDetach(val);
			}
		}
		catch (Exception)
		{
		}
		supportHeld = false;
		leftInGunSet = false;
		leftOverride = false;
	}

	private bool FindSomethingToPlayWith()
	{
		Vector3 val = Feet;
		Rigidbody val2 = null;
		Grip val3 = null;
		float num = float.MaxValue;
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(val + Vector3.up * 0.5f, 10f, -1, (QueryTriggerInteraction)1))
		{
			Rigidbody val4 = (((Object)(object)item != (Object)null) ? item.attachedRigidbody : null);
			if ((Object)(object)val4 == (Object)null || val4.isKinematic || !hashSet.Add(((Il2CppObjectBase)(object)val4).Pointer) || val4.mass < 0.05f || val4.mass > 15f || (Object)(object)((Component)val4).GetComponentInParent<RigManager>() != (Object)null)
			{
				continue;
			}
			Grip val5 = GripBook.Holding(GripBook.ThingOf(GripOf(val4))) ?? GripOf(val4);
			if ((Object)(object)val5 == (Object)null || NotForBots(val4) || (Object)(object)((Component)val4).GetComponentInParent<Poolee>() == (Object)null)
			{
				continue;
			}
			float num2 = val4.worldCenterOfMass.y - val.y;
			if (!(num2 < -0.5f) && !(num2 > 1.6f))
			{
				Vector3 val6 = val4.worldCenterOfMass - val;
				float num3 = ((Vector3)(ref val6)).magnitude + Random.value * 3f;
				if (num3 < num)
				{
					num = num3;
					val2 = val4;
					val3 = val5;
				}
			}
		}
		if ((Object)(object)val2 == (Object)null)
		{
			return false;
		}
		this.item = val2;
		grip = val3;
		gun = null;
		Gun val7 = ((Component)val2).GetComponentInParent<Gun>();
		if ((Object)(object)val7 == (Object)null)
		{
			MarrowEntity componentInParent = ((Component)val2).GetComponentInParent<MarrowEntity>();
			val7 = (((Object)(object)componentInParent != (Object)null) ? ((Component)componentInParent).GetComponentInChildren<Gun>() : null);
		}
		if (Usable(val7))
		{
			Take(val7);
		}
		play = Play.GoTo;
		playAt = Time.time;
		nav.SetGoal(this.item.worldCenterOfMass);
		return true;
	}

	private static Grip GripOf(Rigidbody rb)
	{
		MarrowEntity componentInParent = ((Component)rb).GetComponentInParent<MarrowEntity>();
		object obj = (((Object)(object)componentInParent != (Object)null) ? ((object)componentInParent) : ((object)rb));
		Grip result = null;
		float num = float.MaxValue;
		foreach (Grip componentsInChild in ((Component)obj).GetComponentsInChildren<Grip>(false))
		{
			if (!((Object)(object)componentsInChild == (Object)null) && ((Behaviour)componentsInChild).enabled && !componentsInChild.IsInteractionDisabled && !componentsInChild.HasAttachedHands() && !((Object)(object)((Il2CppObjectBase)(object)componentsInChild).TryCast<WorldGrip>() != (Object)null))
			{
				Vector3 val = ((Component)componentsInChild).transform.position - rb.worldCenterOfMass;
				float sqrMagnitude = ((Vector3)(ref val)).sqrMagnitude;
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
		if ((Object)(object)item == (Object)null || (Object)(object)grip == (Object)null)
		{
			return EndPlay();
		}
		float num = Time.time - playAt;
		Vector3 val;
		switch (play)
		{
		case Play.GoTo:
			if (grip.HasAttachedHands() || num > 20f)
			{
				string name = ((Object)item).name;
				object obj2;
				if (!grip.HasAttachedHands())
				{
					val = Flat(item.worldCenterOfMass - Feet);
					obj2 = " (not there in 20 s, " + ((Vector3)(ref val)).magnitude.ToString("0.0") + " m off)";
				}
				else
				{
					obj2 = " (someone has it)";
				}
				Lab.Event("hands: gave up going to the " + name + (string?)obj2);
				return EndPlay();
			}
			nav.SetGoal(item.worldCenterOfMass);
			if (Travel(MotionLibrary.WalkPush, 0.7f, dt) || nav.Remaining(Feet) < 0.7f)
			{
				Next(Play.Reach);
			}
			else if (nav.StuckCount >= 3)
			{
				Lab.Event("hands: could not get to the " + ((Object)item).name + " (stuck)");
				return EndPlay();
			}
			return true;
		case Play.Reach:
		{
			walk = Vector2.zero;
			if (pickFor != ((Il2CppObjectBase)(object)grip).Pointer || Time.time - pickChosenAt > 6f)
			{
				ChoosePickUp();
			}
			float num7 = ((pickClip != null) ? pickTakeAt : 0.35f);
			if (grip.HasAttachedHands() || num > Mathf.Max(3f, num7 + 1.8f))
			{
				Lab.Event("hands: gave up reaching for the " + ((Object)item).name + (grip.HasAttachedHands() ? " (a hand is on it)" : (" (never got to it in " + num.ToString("0.0") + " s)")));
				return EndPlay();
			}
			Vector3 position = ((Component)grip).transform.position;
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
			if ((Object)(object)rightHand != (Object)null && num > num7)
			{
				val = ((Component)rightHand).transform.position - position;
				if (((Vector3)(ref val)).magnitude < 0.2f || num > num7 + 0.85f)
				{
					Quaternion? turned = null;
					if (GripBook.HandOn(grip, leftHand: false, out var hand, out var _))
					{
						turned = hand.rotation;
					}
					else if (meleeWanted && (Object)(object)item != (Object)null)
					{
						Vector3 val4 = item.worldCenterOfMass - ((Component)grip).transform.position;
						if (((Vector3)(ref val4)).sqrMagnitude > 0.0004f)
						{
							turned = Quaternion.FromToRotation(((Component)rightHand).transform.rotation * Vector3.back, ((Vector3)(ref val4)).normalized) * ((Component)rightHand).transform.rotation;
						}
					}
					if (!Grab(rightHand, grip, turned))
					{
						string[] obj = new string[12]
						{
							"hands: could not take the ",
							((Object)grip).name,
							" of the ",
							((Object)item).name,
							" (hand ",
							null,
							null,
							null,
							null,
							null,
							null,
							null
						};
						val = ((Component)rightHand).transform.position - position;
						obj[5] = ((Vector3)(ref val)).magnitude.ToString("0.00");
						obj[6] = " m from it, ";
						obj[7] = (((Object)(object)rightHand.m_CurrentAttachedGO != (Object)null) ? ("holding " + ((Object)rightHand.m_CurrentAttachedGO).name) : "empty");
						obj[8] = (grip.HasAttachedHands() ? ", a hand on it already" : "");
						obj[9] = ", after ";
						obj[10] = num.ToString("0.0");
						obj[11] = " s)";
						Lab.Event(string.Concat(obj));
						return EndPlay();
					}
					holdFor = Random.Range(2f, 5f);
					if (bringNext)
					{
						bringNext = false;
						offerSince = -1f;
						Lab.Event("play: bringing the " + ((Object)item).name + " over to you (asked)");
						Next(Play.Carry);
					}
					else if (catchNext && (Object)(object)gun != (Object)null)
					{
						gun = null;
						Next(Play.Hold);
					}
					else if ((Object)(object)gun != (Object)null)
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
				float num3;
				if (!((Object)(object)you != (Object)null) || !((Object)(object)head != (Object)null))
				{
					num3 = 99f;
				}
				else
				{
					val = Flat(you.position - head.position);
					num3 = ((Vector3)(ref val)).magnitude;
				}
				float num4 = num3;
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
				if ((catchNext || Random.value < Tuning.F("play.catch", 0.35f)) && StartCatchThrow(head, CatchPartner(head), newGame: true))
				{
					catchNext = false;
					return true;
				}
				if (catchNext)
				{
					Lab.Event("catch: nobody to play with (" + CatchWhyNot(head) + ")");
				}
				catchNext = false;
				if (bringNext && (Object)(object)you != (Object)null)
				{
					bringNext = false;
					offerSince = -1f;
					Lab.Event("play: bringing the " + ((Object)item).name + " over to you (asked)");
					Next(Play.Carry);
					return true;
				}
				if (!forceToss && (Object)(object)you != (Object)null && num4 > 3f && num4 < Tuning.F("play.bring.range", 30f) && Random.value < Tuning.F("play.bring", 0.3f))
				{
					offerSince = -1f;
					Lab.Event("play: bringing the " + ((Object)item).name + " over to you");
					Next(Play.Carry);
					return true;
				}
				float value = Random.value;
				then = ((forceToss && (Object)(object)you != (Object)null) ? Then.TossToYou : ((num4 < 9f && value < 0.35f) ? Then.TossToYou : ((!(value < 0.75f)) ? Then.Drop : Then.Throw)));
				forceToss = false;
				Vector3 val2 = Quaternion.Euler(0f, bodyYaw + Random.Range(-70f, 70f), 0f) * Vector3.forward;
				throwFar = 0f;
				_003F val3;
				if (then != Then.TossToYou || !((Object)(object)you != (Object)null))
				{
					val3 = ((then == Then.Throw && (Object)(object)head != (Object)null) ? PickThrowTarget(head) : val2);
				}
				else
				{
					val = Flat(you.position - head.position);
					val3 = ((Vector3)(ref val)).normalized;
				}
				throwDir = (Vector3)val3;
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
			Face(((Object)(object)head != (Object)null) ? (head.position + throwDir) : (Feet + throwDir), dt, 300f);
			handOverride = true;
			float num5 = Mathf.SmoothStep(0f, 1f, num / 0.45f);
			handAt = Vector3.Lerp(new Vector3(0.18f, standingHeight - 0.42f, 0.36f), new Vector3(0.3f, standingHeight - 0.12f, -0.22f), num5);
			handTurn = Quaternion.Euler(-30f * num5, 0f, 0f);
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
			float num6 = Mathf.Clamp01(num / 0.16f);
			handAt = Vector3.Lerp(new Vector3(0.3f, standingHeight - 0.12f, -0.22f), new Vector3(0.24f, standingHeight - 0.3f, 0.62f), num6);
			handTurn = Quaternion.Euler(Mathf.Lerp(-30f, 40f, num6), 0f, 0f);
			playHeight = standingHeight;
			playPitch = ((then == Then.TossToYou) ? LookPitch(you, head) : (-5f));
			if (!released && num6 > 0.55f)
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
			float num2 = Mathf.SmoothStep(0f, 1f, num / 0.7f);
			handAt = Vector3.Lerp(new Vector3(0.18f, standingHeight - 0.42f, 0.36f), new Vector3(0.15f, 0.35f, 0.45f), num2);
			handTurn = Quaternion.Euler(60f, 0f, 0f);
			playHeight = Mathf.Lerp(standingHeight, Mathf.Max(0.95f, standingHeight * 0.62f), num2);
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
		Vector3 forward = Flat(point - (((Object)(object)head != (Object)null) ? head.position : Feet));
		if (((Vector3)(ref forward)).sqrMagnitude > 0.0001f)
		{
			bodyYaw = Mathf.MoveTowardsAngle(bodyYaw, Recorder.Yaw(forward), speed * dt);
		}
	}

	private void Reach(Vector3 target, float k)
	{
		Transform vrRoot = controls.vrRoot;
		Vector3 val = Feet;
		float num = target.y - val.y;
		playHeight = Mathf.Clamp(num + 0.85f, Mathf.Max(0.9f, standingHeight * 0.55f), standingHeight);
		Vector3 val2 = vrRoot.InverseTransformPoint(target);
		Vector3 val3 = default(Vector3);
		((Vector3)(ref val3))._002Ector(0.2f, playHeight - 0.5f, 0.25f);
		handOverride = true;
		handAt = Vector3.Lerp(val3, val2, Mathf.SmoothStep(0f, 1f, k));
		Vector3 val4 = default(Vector3);
		((Vector3)(ref val4))._002Ector(0.18f, playHeight - 0.25f, 0f);
		Vector3 val5 = handAt - val4;
		handTurn = ((((Vector3)(ref val5)).sqrMagnitude > 0.0001f) ? Quaternion.LookRotation(((Vector3)(ref val5)).normalized, Vector3.up) : Quaternion.identity);
		float num2 = num - playHeight;
		Vector3 val6 = Flat(val2);
		playPitch = Mathf.Clamp((0f - Mathf.Atan2(num2, Mathf.Max(((Vector3)(ref val6)).magnitude, 0.2f))) * 57.29578f, -20f, 70f);
	}

	private void ChoosePickUp()
	{
		pickFor = ((Il2CppObjectBase)(object)grip).Pointer;
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
		Vector3 val = (pickMirror ? Across(frame.leftPos) : frame.rightPos);
		Vector3 val2 = (pickMirror ? Across(frame.rightPos) : frame.leftPos);
		Quaternion val3 = (pickMirror ? Across(frame.leftRot) : frame.rightRot);
		Quaternion val4 = (pickMirror ? Across(frame.rightRot) : frame.leftRot);
		float num2 = target.y - Feet.y;
		float num3 = standingHeight - 0.62f;
		float num4 = Mathf.Clamp((num3 - num2) / Mathf.Max(0.15f, num3 - pickLow * num), 0f, 1.25f);
		float num5 = Mathf.Max(0f, frame2.height - frame.height) * num * num4;
		float num6 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(since / pickTakeAt));
		playHeight = Mathf.Clamp(standingHeight - num5, Mathf.Max(0.75f, standingHeight * 0.45f), standingHeight);
		float num7 = Mathf.Clamp(num2 + 0.85f, Mathf.Max(0.9f, standingHeight * 0.55f), standingHeight);
		playHeight = Mathf.Min(playHeight, Mathf.Lerp(standingHeight, num7, num6));
		Vector3 val5 = default(Vector3);
		((Vector3)(ref val5))._002Ector(0f, playHeight, 0f);
		Vector3 val6 = default(Vector3);
		((Vector3)(ref val6))._002Ector(0f, frame.height * num, 0f);
		Vector3 val7 = vrRoot.InverseTransformPoint(target);
		float num8 = num6 * num6;
		handOverride = true;
		handAt = Vector3.Lerp(val5 + (val * num - val6), val7, num8);
		handTurn = val3;
		leftOverride = true;
		leftAt = val5 + (val2 * num - val6);
		leftTurn = val4;
		Vector3 val8 = frame.head * Vector3.forward;
		playPitch = Mathf.Clamp((0f - Mathf.Asin(Mathf.Clamp(val8.y, -1f, 1f))) * 57.29578f, -20f, 70f);
	}

	private static bool Grab(Hand hand, Grip grip)
	{
		return Grab(hand, grip, null);
	}

	private static bool Grab(Hand hand, Grip grip, Quaternion? turned)
	{
		try
		{
			if ((Object)(object)hand.m_CurrentAttachedGO != (Object)null)
			{
				return false;
			}
			hand.GrabLock = false;
			InventoryHand component = ((Component)hand).GetComponent<InventoryHand>();
			if ((Object)(object)component != (Object)null)
			{
				component.IgnoreUnlock();
			}
			hand.HoverLock();
			SimpleTransform val = SimpleTransform.Create(((Component)hand).transform);
			if (GripBook.HandOn(grip, (int)hand.handedness == 1, out var hand2, out var _))
			{
				val.position = hand2.position;
				val.rotation = hand2.rotation;
			}
			else if (turned.HasValue)
			{
				val.rotation = turned.Value;
			}
			((HandReciever)grip).ValidateGripScore(hand, val);
			grip.OnHandHoverBegin(hand, true);
			((HandReciever)grip).ValidateGripScore(hand, val);
			hand._mHoveringReceiver = (HandReciever)(object)grip;
			grip.OnGrabConfirm(hand, false);
			return (Object)(object)hand.m_CurrentAttachedGO != (Object)null;
		}
		catch (Exception ex2)
		{
			bool flag = false;
			try
			{
				flag = (Object)(object)hand.m_CurrentAttachedGO != (Object)null && grip.attachedHands != null && grip.attachedHands.Contains(hand);
			}
			catch (Exception)
			{
			}
			if (!flag)
			{
				MelonLogger.Warning("Bot Player: could not grab " + ((Object)grip).name + ": " + ex2.Message);
			}
			return flag;
		}
	}

	private bool Holding()
	{
		Hand rightHand = RightHand;
		if ((Object)(object)rightHand != (Object)null && (Object)(object)grip != (Object)null && (Object)(object)rightHand.m_CurrentAttachedGO != (Object)null && grip.attachedHands != null)
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
			if ((Object)(object)rightHand != (Object)null && (Object)(object)grip != (Object)null)
			{
				grip.ForceDetach(rightHand);
			}
		}
		catch (Exception)
		{
		}
	}

	private void Let(bool go, Transform you)
	{
		Rigidbody val = item;
		Grip itsGrip = grip;
		Hand rightHand = RightHand;
		Vector3 val2 = (((Object)(object)rightHand != (Object)null && (Object)(object)rightHand.rb != (Object)null) ? rightHand.rb.velocity : Vector3.zero);
		Release();
		if (!((Object)(object)val == (Object)null) && go)
		{
			Vector3 val3 = ((then == Then.ToPartner && catchWith != null) ? ThrowToPartner(val) : ((then == Then.TossToYou && (Object)(object)you != (Object)null) ? Lob(val.worldCenterOfMass, you.position - Vector3.up * 0.35f, 0.75f) : ((!(throwFar > 0f)) ? (throwDir * Random.Range(7f, 11f) + Vector3.up * Random.Range(2f, 4f)) : ThrowVelocity(val))));
			val.velocity = (((Object)(object)flying != (Object)(object)val && Vector3.Dot(val2, ((Vector3)(ref val3)).normalized) > ((Vector3)(ref val3)).magnitude) ? val2 : val3);
			val.angularVelocity = Random.insideUnitSphere * 4f;
			if (then == Then.ToPartner && catchWith != null)
			{
				val.velocity = val3;
				val.angularVelocity = Random.insideUnitSphere * 1.5f;
				catchWith.ComingToYou(val, itsGrip);
				Lab.Event("catch: thrown to " + catchWith.Name + " at " + ((Vector3)(ref val3)).magnitude.ToString("0.0") + " m/s");
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
			string[] obj = new string[6] { "[", Name, "] a hit not counted (", why, "): ", null };
			object obj2;
			if (attack == null)
			{
				obj2 = "?";
			}
			else
			{
				AttackType attackType = attack.attackType;
				obj2 = ((object)(AttackType)(ref attackType)).ToString() + " " + attack.damage.ToString("0.00");
			}
			obj[5] = (string)obj2;
			Lab.Event(string.Concat(obj));
		}
	}

	public void TakeHit(float damage, Attack attack = null, BodyPart part = 2)
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
				if (attack != null && (Object)(object)attack.proxy != (Object)null && NetBots.PuppetCount + NetBots.OtherPlayers().Count > 0)
				{
					RigManager componentInParent = ((Component)attack.proxy).GetComponentInParent<RigManager>();
					Bot bot = (((Object)(object)componentInParent != (Object)null) ? BotManager.Of(componentInParent) : null);
					if ((Object)(object)componentInParent != (Object)null && (NetBots.IsOther(componentInParent) || (bot != null && bot.Puppet)))
					{
						return;
					}
				}
			}
			catch (Exception)
			{
			}
			string text = "";
			if (attack != null)
			{
				AttackType attackType = attack.attackType;
				text = ((object)(AttackType)(ref attackType)).ToString();
				bool flag = text.IndexOf("Blunt", StringComparison.OrdinalIgnoreCase) >= 0;
				bool flag2 = !flag && text.IndexOf("Pierc", StringComparison.OrdinalIgnoreCase) >= 0 && Time.time - LastShotAt < 0.3f;
				if (!flag2 && OwnThingAt(attack.origin))
				{
					NotCounted("its own thing there", attack);
					return;
				}
				try
				{
					if ((Object)(object)attack.proxy != (Object)null && (Object)(object)((Component)attack.proxy).GetComponentInParent<RigManager>() == (Object)(object)rig)
					{
						return;
					}
				}
				catch (Exception)
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
						Rigidbody val = Pelvis();
						if ((Object)(object)attack.proxy == (Object)null && (Object)(object)collider != (Object)null && (((Object)collider).name == "Feet" || ((Object)collider).name == "Knee") && (Object)(object)((Component)collider).GetComponentInParent<RigManager>() == (Object)(object)rig && ((Object)(object)val == (Object)null || val.velocity.y > 0f - Tuning.F("foot.knock.fall", 3f)))
						{
							return;
						}
					}
					catch (Exception)
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
			if (text.IndexOf("Pierc", StringComparison.OrdinalIgnoreCase) >= 0)
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
			TakeHit(Mathf.Min(damage, MaxHp * Tuning.F("bot.knock.most", 0.3f)), null, (BodyPart)2);
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
			if ((Object)(object)collider != (Object)null)
			{
				MarrowEntity componentInParent = ((Component)collider).GetComponentInParent<MarrowEntity>();
				RigManager componentInParent2 = ((Component)collider).GetComponentInParent<RigManager>();
				text = text + "; collider " + ((Object)collider).name + " of " + (((Object)(object)componentInParent != (Object)null) ? ((Object)componentInParent).name : ((Object)((Component)collider).transform.root).name) + (((Object)(object)componentInParent2 == (Object)(object)rig) ? " (its own body)" : (((Object)(object)componentInParent2 != (Object)null) ? " (another body)" : ""));
			}
			if ((Object)(object)attack.proxy != (Object)null)
			{
				text = text + "; from " + ((Object)((Component)attack.proxy).transform.root).name + (((Object)(object)((Component)attack.proxy).GetComponentInParent<RigManager>() == (Object)(object)rig) ? " (itself)" : "");
			}
			float num = 99f;
			string text2 = "";
			Vector3 val2;
			foreach (Collider item in Physics.OverlapSphere(attack.origin, 1.2f, -1, (QueryTriggerInteraction)1))
			{
				Rigidbody val = (((Object)(object)item != (Object)null) ? item.attachedRigidbody : null);
				if ((Object)(object)val == (Object)null || (Object)(object)((Component)val).GetComponentInParent<RigManager>() != (Object)null)
				{
					continue;
				}
				Bounds bounds = item.bounds;
				val2 = ((Bounds)(ref bounds)).ClosestPoint(attack.origin) - attack.origin;
				float magnitude = ((Vector3)(ref val2)).magnitude;
				if (magnitude < num)
				{
					num = magnitude;
					MarrowEntity componentInParent3 = ((Component)val).GetComponentInParent<MarrowEntity>();
					Transform val3 = (((Object)(object)componentInParent3 != (Object)null) ? ((Component)componentInParent3).transform : ((Component)val).transform);
					string name = ((Object)val3).name;
					object obj;
					if (!HeldByAnyone(val3, null))
					{
						val2 = val.velocity;
						obj = " (loose, " + ((Vector3)(ref val2)).magnitude.ToString("0.0") + " m/s)";
					}
					else
					{
						obj = " (held)";
					}
					text2 = name + (string?)obj;
				}
			}
			Transform head = Head;
			string[] obj2 = new string[8] { text, "; ", null, null, null, null, null, null };
			object obj3;
			if (!((Object)(object)head != (Object)null))
			{
				obj3 = "?";
			}
			else
			{
				val2 = attack.origin - head.position;
				obj3 = ((Vector3)(ref val2)).magnitude.ToString("0.0");
			}
			obj2[2] = (string)obj3;
			obj2[3] = " m from its head; nearest thing ";
			obj2[4] = ((text2.Length > 0) ? (text2 + " " + num.ToString("0.00") + " m") : "none");
			obj2[5] = "; holding ";
			obj2[6] = (((Object)(object)RightHand != (Object)null && (Object)(object)RightHand.m_CurrentAttachedGO != (Object)null) ? ((Object)RightHand.m_CurrentAttachedGO.transform.root).name : "nothing");
			obj2[7] = (((Object)(object)this.item != (Object)null) ? (", busy with " + ((Object)this.item).name) : "");
			return string.Concat(obj2);
		}
		catch (Exception ex)
		{
			return "; from ? (" + ex.Message + ")";
		}
	}

	private bool LyingThingAt(Vector3 at)
	{
		try
		{
			foreach (Collider item in Physics.OverlapSphere(at, 0.2f, -1, (QueryTriggerInteraction)1))
			{
				Rigidbody val = (((Object)(object)item != (Object)null) ? item.attachedRigidbody : null);
				if (!((Object)(object)val == (Object)null) && !val.isKinematic && !((Object)(object)((Component)val).GetComponentInParent<RigManager>() != (Object)null))
				{
					Vector3 velocity = val.velocity;
					if (((Vector3)(ref velocity)).magnitude > Tuning.F("lying.speed", 1.5f))
					{
						return false;
					}
					MarrowEntity componentInParent = ((Component)val).GetComponentInParent<MarrowEntity>();
					if (!HeldByAnyone(((Object)(object)componentInParent != (Object)null) ? ((Component)componentInParent).transform : ((Component)val).transform, null))
					{
						return true;
					}
				}
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	private bool OwnThingAt(Vector3 at)
	{
		Vector3 val;
		try
		{
			if ((Object)(object)item != (Object)null && (Playing || Time.time - playAt < 2f))
			{
				MarrowEntity componentInParent = ((Component)item).GetComponentInParent<MarrowEntity>();
				foreach (Collider componentsInChild in ((Component)(((Object)(object)componentInParent != (Object)null) ? ((Component)componentInParent).transform : ((Component)item).transform)).GetComponentsInChildren<Collider>())
				{
					if ((Object)(object)componentsInChild != (Object)null && !componentsInChild.isTrigger)
					{
						val = componentsInChild.ClosestPoint(at) - at;
						if (((Vector3)(ref val)).magnitude < 0.4f)
						{
							return true;
						}
					}
				}
			}
		}
		catch (Exception)
		{
		}
		Hand[] array = (Hand[])(object)new Hand[2] { LeftHand, RightHand };
		foreach (Hand val2 in array)
		{
			GameObject val3 = (((Object)(object)val2 != (Object)null) ? val2.m_CurrentAttachedGO : null);
			if ((Object)(object)val3 == (Object)null)
			{
				continue;
			}
			MarrowEntity componentInParent2 = val3.GetComponentInParent<MarrowEntity>();
			Transform val4 = (((Object)(object)componentInParent2 != (Object)null) ? ((Component)componentInParent2).transform : val3.transform.root);
			if ((Object)(object)val4 == (Object)(object)((Component)rig).transform || (Object)(object)((Component)val4).GetComponent<RigManager>() != (Object)null)
			{
				continue;
			}
			foreach (Collider componentsInChild2 in ((Component)val4).GetComponentsInChildren<Collider>())
			{
				if ((Object)(object)componentsInChild2 == (Object)null || componentsInChild2.isTrigger)
				{
					continue;
				}
				Bounds bounds = componentsInChild2.bounds;
				((Bounds)(ref bounds)).Expand(Tuning.F("own.thing.room", 0.5f));
				if (!((Bounds)(ref bounds)).Contains(at))
				{
					val = componentsInChild2.ClosestPoint(at) - at;
					if (!(((Vector3)(ref val)).magnitude < 0.3f))
					{
						continue;
					}
				}
				return true;
			}
		}
		return false;
	}

	private static float BulletHarm(float damage, BodyPart part)
	{
		string text = ((object)(BodyPart)(ref part)).ToString();
		float num = Tuning.F("bullet.harm", 5f);
		if ((int)part == 0)
		{
			num *= Tuning.F("bullet.harm.head", 3f);
		}
		else if (text.Contains("Arm") || text.Contains("Hand"))
		{
			num *= Tuning.F("bullet.harm.arm", 0.6f);
		}
		else if ((int)part >= 11)
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
			Hand[] array = (Hand[])(object)new Hand[2] { LeftHand, RightHand };
			foreach (Hand val in array)
			{
				if ((Object)(object)val != (Object)null && (Object)(object)val.m_CurrentAttachedGO != (Object)null)
				{
					Grip component = val.m_CurrentAttachedGO.GetComponent<Grip>();
					if ((Object)(object)component != (Object)null)
					{
						LetGoOf(component, val);
					}
				}
			}
			Limp(on: true);
		}
		catch (Exception ex)
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
			if (!WeaponInHand || HasStowed || (Object)(object)item == (Object)null || (Object)(object)grip == (Object)null || (Object)(object)rig.physicsRig == (Object)null)
			{
				return false;
			}
			Rigidbody val = item;
			Gun val2 = ((play == Play.Shoot) ? gun : null);
			Grip val3 = grip;
			Vector3 val4;
			Vector3 val5;
			if (!((Object)(object)val2 != (Object)null) || !((Object)(object)val2.firePointTransform != (Object)null))
			{
				val4 = val.worldCenterOfMass - ((Component)val3).transform.position;
				val5 = ((Vector3)(ref val4)).normalized;
			}
			else
			{
				val5 = val2.firePointTransform.forward;
			}
			Vector3 val6 = val5;
			float num = 0.3f;
			foreach (Collider componentsInChild in ((Component)val).GetComponentsInChildren<Collider>())
			{
				if ((Object)(object)componentsInChild != (Object)null && !componentsInChild.isTrigger)
				{
					float num2 = num;
					Bounds bounds = componentsInChild.bounds;
					val4 = ((Bounds)(ref bounds)).size;
					num = Mathf.Max(num2, ((Vector3)(ref val4)).magnitude);
				}
			}
			bool flag = (Object)(object)val2 != (Object)null && GunMoves.KindOf(val2) == "pistol";
			Transform val7 = (flag ? ((Rig)rig.physicsRig).m_pelvis : ((Rig)rig.physicsRig).m_chest);
			if ((Object)(object)val7 == (Object)null)
			{
				val7 = ((Rig)rig.physicsRig).m_pelvis;
			}
			if ((Object)(object)val7 == (Object)null)
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
			Hand[] array = (Hand[])(object)new Hand[2] { LeftHand, RightHand };
			foreach (Hand val8 in array)
			{
				if ((Object)(object)val8 != (Object)null && (Object)(object)val8.m_CurrentAttachedGO != (Object)null && val8.m_CurrentAttachedGO.transform.IsChildOf(((Component)val).transform.root) && (Object)(object)((Component)val).transform.root != (Object)(object)((Component)rig).transform)
				{
					FreeHand(val8);
				}
			}
			Vector3 val9;
			if (!flag)
			{
				val4 = -val7.up + val7.right * 0.45f;
				val9 = ((Vector3)(ref val4)).normalized;
			}
			else
			{
				val9 = -val7.up;
			}
			Vector3 val10 = val9;
			Quaternion val11 = Quaternion.FromToRotation(val6, val10) * val.rotation;
			Vector3 val12 = val.worldCenterOfMass - val.position;
			Vector3 val13 = (flag ? val7.TransformPoint(new Vector3(0.21f, -0.02f, 0f)) : val7.TransformPoint(new Vector3(0f, 0.02f, -0.19f))) - Quaternion.FromToRotation(val6, val10) * val12;
			stowedOff.Clear();
			foreach (Collider componentsInChild2 in ((Component)val).GetComponentsInChildren<Collider>())
			{
				if ((Object)(object)componentsInChild2 != (Object)null && componentsInChild2.enabled && !componentsInChild2.isTrigger)
				{
					componentsInChild2.enabled = false;
					stowedOff.Add(componentsInChild2);
				}
			}
			stowedKinematic = val.isKinematic;
			val.isKinematic = true;
			stowOn = val7;
			stowAt = val7.InverseTransformPoint(val13);
			stowTurn = Quaternion.Inverse(val7.rotation) * val11;
			stowed = val;
			stowedGun = val2;
			stowedGrip = val3;
			stowedSince = Time.time;
			gun = null;
			support = null;
			item = null;
			grip = null;
			play = Play.None;
			handOverride = false;
			leftOverride = false;
			hitClip = null;
			Lab.Event("holster: the " + ((Object)val).name + " put away " + (flag ? "on its hip" : "across its back"));
			StowFollow();
			return true;
		}
		catch (Exception ex)
		{
			Lab.Event("holster: could not put it away: " + ex.Message);
			return false;
		}
	}

	public void StowFollow()
	{
		if ((Object)(object)stowed == (Object)null)
		{
			return;
		}
		try
		{
			if (!((Component)stowed).gameObject.activeInHierarchy || (Object)(object)stowOn == (Object)null)
			{
				stowed = null;
				return;
			}
			Vector3 val = stowOn.TransformPoint(stowAt);
			Quaternion val2 = stowOn.rotation * stowTurn;
			if (!Bad(val) && !Bad(val2))
			{
				stowed.position = val;
				stowed.rotation = val2;
				((Component)stowed).transform.SetPositionAndRotation(val, val2);
			}
		}
		catch (Exception)
		{
			stowed = null;
		}
	}

	private Rigidbody Unstow()
	{
		Rigidbody val = stowed;
		stowed = null;
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		try
		{
			foreach (Collider item in stowedOff)
			{
				if ((Object)(object)item != (Object)null)
				{
					item.enabled = true;
				}
			}
			stowedOff.Clear();
			val.isKinematic = stowedKinematic;
			val.velocity = Vector3.zero;
			val.angularVelocity = Vector3.zero;
		}
		catch (Exception)
		{
		}
		return val;
	}

	public void DropStowed()
	{
		if ((Object)(object)stowed != (Object)null)
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
				ref Rigidbody reference = ref stowed;
				object obj = kept[0];
				reference = (Rigidbody)((obj is Rigidbody) ? obj : null);
				ref Gun reference2 = ref stowedGun;
				object obj2 = kept[1];
				reference2 = (Gun)((obj2 is Gun) ? obj2 : null);
				ref Grip reference3 = ref stowedGrip;
				object obj3 = kept[2];
				reference3 = (Grip)((obj3 is Grip) ? obj3 : null);
				ref Transform reference4 = ref stowOn;
				object obj4 = kept[3];
				reference4 = (Transform)((obj4 is Transform) ? obj4 : null);
				stowAt = (Vector3)kept[4];
				stowTurn = (Quaternion)kept[5];
				stowedKinematic = (bool)kept[6];
				stowedOff.Clear();
				if (kept[7] is Collider[] collection)
				{
					stowedOff.AddRange(collection);
				}
				stowedSince = Time.time;
				if ((Object)(object)stowed == (Object)null || (Object)(object)stowOn == (Object)null || (Object)(object)stowedGrip == (Object)null)
				{
					stowed = null;
				}
			}
		}
		catch (Exception)
		{
			stowed = null;
		}
	}

	private bool Draw()
	{
		try
		{
			Hand rightHand = RightHand;
			if (!HasStowed || (Object)(object)rightHand == (Object)null || (Object)(object)stowedGrip == (Object)null)
			{
				return false;
			}
			if ((Object)(object)rightHand.m_CurrentAttachedGO != (Object)null)
			{
				FreeHand(rightHand);
			}
			Gun val = stowedGun;
			Grip val2 = stowedGrip;
			Rigidbody val3 = Unstow();
			stowedGun = null;
			stowedGrip = null;
			if ((Object)(object)val3 == (Object)null)
			{
				return false;
			}
			Vector3 val4 = ((Component)val2).transform.position - val3.position;
			val3.position = ((Component)rightHand).transform.position - val4;
			((Component)val3).transform.position = ((Component)rightHand).transform.position - val4;
			drawnAt = Time.time;
			if ((Object)(object)val != (Object)null)
			{
				Take(val);
				if (!Grab(rightHand, grip))
				{
					Lab.Event("holster: could not take the " + ((Object)val3).name + " in hand again");
					EndPlay();
					return false;
				}
				StartShooting();
			}
			else
			{
				item = val3;
				grip = val2;
				gun = null;
				support = null;
				Quaternion? turned = null;
				Vector3 val5 = val3.worldCenterOfMass - ((Component)val2).transform.position;
				if (((Vector3)(ref val5)).sqrMagnitude > 0.0004f)
				{
					turned = Quaternion.FromToRotation(((Component)rightHand).transform.rotation * Vector3.back, ((Vector3)(ref val5)).normalized) * ((Component)rightHand).transform.rotation;
				}
				if (!Grab(rightHand, grip, turned))
				{
					Lab.Event("holster: could not take the " + ((Object)val3).name + " in hand again");
					item = null;
					grip = null;
					return false;
				}
				MeasureMelee();
				Next(Play.Melee);
			}
			Lab.Event("holster: the " + ((Object)val3).name + " drawn");
			return true;
		}
		catch (Exception ex)
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
				ownEntities = ((Component)rig).GetComponentsInChildren<MarrowEntity>(true);
			}
			MarrowEntity[] array = ownEntities;
			foreach (MarrowEntity val in array)
			{
				if ((Object)(object)val == (Object)null)
				{
					continue;
				}
				val.PreventDisableOnCull(true);
				if (!val.IsCulled && !val.IsHibernating)
				{
					continue;
				}
				wakings++;
				if (wakings <= 3 || wakings % 20 == 0)
				{
					Lab.Event("awake: " + ((Object)val).name + " had been put to sleep by the level (" + (val.IsCulled ? "culled" : "hibernating") + "), woken (" + wakings + ")");
				}
				try
				{
					if (val.IsCulled && val._inactiveStatus != null)
					{
						val._inactiveStatus.Cull(false);
					}
					val.Hibernate(false, (HibernationSources)(-1));
				}
				catch (Exception)
				{
				}
			}
		}
		catch (Exception)
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
			foreach (MarrowEntity val in array)
			{
				if (!((Object)(object)val == (Object)null))
				{
					if (val.IsCulled)
					{
						num++;
						text = text + " " + ((Object)val).name;
					}
					if (val.IsHibernating)
					{
						num2++;
					}
				}
			}
			return ownEntities.Length + " part(s), " + num + " culled" + ((text.Length > 0) ? (" (" + text.Trim() + ")") : "") + ", " + num2 + " asleep";
		}
		catch (Exception)
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
				float num2;
				if (!((Object)(object)head != (Object)null))
				{
					num2 = 999f;
				}
				else
				{
					Vector3 val = item.Point - head.position;
					num2 = ((Vector3)(ref val)).magnitude;
				}
				float num3 = num2;
				if (num3 < num)
				{
					num = num3;
					flag = (Object)(object)head != (Object)null && Targets.InSight(head.position, item.Point, item.transform, rig);
					text = (((Object)(object)item.transform != (Object)null) ? ((Object)item.transform).name : "?") + (((Object)(object)item.brain != (Object)null) ? (" (an NPC" + (item.brain.isDead ? ", dead" : "") + ")") : (((Object)(object)item.zombieHead != (Object)null) ? " (a zombie)" : "")) + ", its point " + (item.Point.y - Feet.y).ToString("0.0") + " m above its feet's level";
				}
			}
		}
		catch (Exception)
		{
		}
		return Name + " " + mode.ToString() + "/" + goal.ToString() + "/" + play.ToString() + ((act != 0) ? ("/" + act) : "") + ", health " + ((hp < 0f) ? "full" : hp.ToString("0.0")) + ", nearest hostile " + ((num > 900f) ? "none" : (num.ToString("0.0") + " m" + (flag ? " in sight" : (" NOT in sight (behind " + Targets.LastBlocker + ")")))) + " [" + text + "], sees " + (((Object)(object)nearestSeen.transform != (Object)null) ? (nearestSeenDistance.ToString("0.0") + " m") : "none") + ", " + AwakeSaid() + (WeaponInHand ? ", weapon in hand" : (HasStowed ? ", weapon put away" : ", unarmed")) + (climbing ? ", climbing" : "") + (InLesson ? ", in a lesson" : "") + (down ? ", down" : "");
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
			if ((Object)(object)head == (Object)null || ((Vector3)(ref dir)).sqrMagnitude < 0.0001f)
			{
				return dir;
			}
			float num = Tuning.F("apart.room", 1.4f);
			Vector3 val = Vector3.zero;
			float num2 = 1f;
			foreach (Bot item in BotManager.All)
			{
				if (item == this || item.dead || (Object)(object)item.Head == (Object)null)
				{
					continue;
				}
				Vector3 val2 = Flat(head.position - item.Head.position);
				float magnitude = ((Vector3)(ref val2)).magnitude;
				if (!(magnitude > num) && !(Mathf.Abs(head.position.y - item.Head.position.y) > 1.5f))
				{
					Vector3 val3 = ((magnitude > 0.05f) ? (val2 / magnitude) : (Quaternion.Euler(0f, (float)(((Il2CppObjectBase)(object)rig).Pointer.ToInt64() % 360), 0f) * Vector3.forward));
					val += val3 * (1f - magnitude / num);
					if (Vector3.Dot(-val3, dir) > 0.6f && magnitude < num * 0.8f && ((Il2CppObjectBase)(object)rig).Pointer.ToInt64() < ((Il2CppObjectBase)(object)item.rig).Pointer.ToInt64())
					{
						num2 = Mathf.Min(num2, Mathf.Clamp01(magnitude / num - 0.2f));
					}
				}
			}
			if (((Vector3)(ref val)).sqrMagnitude < 0.0001f)
			{
				return dir;
			}
			speed *= Mathf.Max(0.15f, num2);
			Vector3 v = dir + val * Tuning.F("apart.push", 0.8f);
			Vector3 result;
			if (!(((Vector3)(ref v)).sqrMagnitude > 0.0001f))
			{
				result = dir;
			}
			else
			{
				Vector3 val4 = Flat(v);
				result = ((Vector3)(ref val4)).normalized;
			}
			return result;
		}
		catch (Exception)
		{
			return dir;
		}
	}

	private void NoPushOnItself(Transform thing, bool again = false)
	{
		try
		{
			MarrowEntity componentInParent = ((Component)thing).GetComponentInParent<MarrowEntity>();
			Transform val = (((Object)(object)componentInParent != (Object)null) ? ((Component)componentInParent).transform : thing);
			if (!noPushDone.Add(((Il2CppObjectBase)(object)val).Pointer) && !again)
			{
				return;
			}
			Collider[]? array = ((Component)val).GetComponentsInChildren<Collider>(true);
			Collider[] array2 = ((Component)rig.physicsRig).GetComponentsInChildren<Collider>(true);
			Collider[] array3 = array;
			foreach (Collider val2 in array3)
			{
				if ((Object)(object)val2 == (Object)null || val2.isTrigger)
				{
					continue;
				}
				Collider[] array4 = array2;
				foreach (Collider val3 in array4)
				{
					if ((Object)(object)val3 != (Object)null && !val3.isTrigger)
					{
						Physics.IgnoreCollision(val2, val3, true);
					}
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private bool OffTheGround(Rigidbody hips)
	{
		float num = Tuning.F("guard.rise.clear", 1.6f);
		foreach (RaycastHit item in Physics.RaycastAll(hips.position, Vector3.down, num, -1, (QueryTriggerInteraction)1))
		{
			RaycastHit current = item;
			if (!((Object)(object)((RaycastHit)(ref current)).collider == (Object)null) && !((Object)(object)((Component)((RaycastHit)(ref current)).collider).GetComponentInParent<RigManager>() == (Object)(object)rig) && (!((Object)(object)gun != (Object)null) || !((Component)((RaycastHit)(ref current)).collider).transform.IsChildOf(((Component)gun).transform)))
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
		foreach (Rigidbody componentsInChild in ((Component)rig.physicsRig).GetComponentsInChildren<Rigidbody>())
		{
			if (!((Object)(object)componentsInChild == (Object)null) && !componentsInChild.isKinematic)
			{
				Vector3 velocity2 = componentsInChild.velocity;
				componentsInChild.velocity = new Vector3(velocity2.x, Mathf.Min(velocity2.y, 0f), velocity2.z);
			}
		}
		if ((Object)(object)gun != (Object)null && Time.time > noPushAgainAt)
		{
			noPushAgainAt = Time.time + 1f;
			NoPushOnItself(((Component)gun).transform, again: true);
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
			if (dead || down || (Object)(object)rig.physicsRig == (Object)null || (Object)(object)rig.activeSeat != (Object)null)
			{
				return;
			}
			Rigidbody val = Pelvis();
			if ((Object)(object)val == (Object)null)
			{
				return;
			}
			RiseGuard(val);
			Vector3 velocity = val.velocity;
			Vector3 val2 = Flat(velocity);
			float magnitude = ((Vector3)(ref val2)).magnitude;
			float num = Tuning.F("guard.speed", 9.5f);
			if (magnitude <= num && velocity.y <= Tuning.F("guard.up", 9f))
			{
				return;
			}
			if (Time.time - fastSaidAt > 1f)
			{
				fastSaidAt = Time.time;
				Lab.Event("guard: going " + magnitude.ToString("0.0") + " m/s (up " + velocity.y.ToString("0.0") + ") in " + mode.ToString() + "/" + goal.ToString() + "/" + play.ToString() + (climbing ? ", climbing" : "") + (climbHang ? ", hanging" : "") + (InLesson ? ", in a lesson" : "") + (lessonGliding ? ", carried up" : "") + (HasStowed ? ", weapon put away" : "") + (running ? ", running" : "") + ", legs' most " + (((Object)(object)rig.remapHeptaRig != (Object)null) ? rig.remapHeptaRig.maxVelocity.ToString("0.0") : "?") + ": held back");
			}
			float num2 = ((magnitude > num) ? (num / magnitude) : 1f);
			foreach (Rigidbody componentsInChild in ((Component)rig.physicsRig).GetComponentsInChildren<Rigidbody>())
			{
				if (!((Object)(object)componentsInChild == (Object)null) && !componentsInChild.isKinematic)
				{
					Vector3 velocity2 = componentsInChild.velocity;
					componentsInChild.velocity = new Vector3(velocity2.x * num2, Mathf.Min(velocity2.y, Tuning.F("guard.up", 9f)), velocity2.z * num2);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private Vector3 OwnSide(float far)
	{
		float num = Mathf.Abs((float)(((Il2CppObjectBase)(object)rig).Pointer.ToInt64() / 16)) % 8f * 45f;
		return Quaternion.Euler(0f, num, 0f) * Vector3.forward * far;
	}

	private void Flesh()
	{
		Avatar avatar = rig.avatar;
		if ((Object)(object)avatar == (Object)null || ((Il2CppObjectBase)(object)avatar).Pointer == fleshOf || (Object)(object)rig.physicsRig == (Object)null)
		{
			return;
		}
		fleshOf = ((Il2CppObjectBase)(object)avatar).Pointer;
		avatarCode = BotRig.AvatarOf(rig);
		clip = null;
		lately.Clear();
		try
		{
			SurfaceData val = avatar.surfaceData;
			if ((Object)(object)val == (Object)null && (Object)(object)Player.RigManager != (Object)null && (Object)(object)Player.RigManager.avatar != (Object)null)
			{
				val = Player.RigManager.avatar.surfaceData;
			}
			if (Tuning.F("flesh.blood", 1f) > 0f)
			{
				AskForBlood();
				if ((Object)(object)bloodData != (Object)null)
				{
					val = bloodData;
				}
			}
			string text = (((Object)(object)val != (Object)null) ? (((Object)val).name + ", resists a blade " + val.PenetrationResistance.ToString("0.00") + ", " + val.megaPascal.ToString("0.0") + " MPa") : "none");
			if ((Object)(object)val != (Object)null)
			{
				float num = Tuning.F("flesh.resist", 0.6f);
				if (val.PenetrationResistance > num)
				{
					SurfaceData obj = Object.Instantiate<SurfaceData>(val);
					((Object)obj).name = ((Object)val).name;
					obj.megaPascal = val.megaPascal * Mathf.Clamp01(num / Mathf.Max(0.01f, val.PenetrationResistance)) * Tuning.F("flesh.mpa", 1f);
					obj.PenetrationResistance = num;
					val = obj;
				}
			}
			bool flag = false;
			try
			{
				flag = AssetWarehouse.Instance.HasDataCard(new Barcode("SLZ.Backlot.SurfaceDataCard.Blood"));
			}
			catch (Exception)
			{
			}
			Lab.Event("flesh: its avatar's surface was " + text + "; a blood card in the game: " + flag);
			int num2 = 0;
			foreach (PlayerDamageReceiver componentsInChild in ((Component)rig).GetComponentsInChildren<PlayerDamageReceiver>(true))
			{
				if ((Object)(object)componentsInChild == (Object)null || (Object)(object)((Component)componentsInChild).GetComponent<Collider>() == (Object)null)
				{
					continue;
				}
				ImpactProperties val2 = ((Component)componentsInChild).GetComponent<ImpactProperties>();
				if ((Object)(object)val2 == (Object)null)
				{
					if ((Object)(object)val == (Object)null)
					{
						continue;
					}
					val2 = ((Component)componentsInChild).gameObject.AddComponent<ImpactProperties>();
				}
				try
				{
					if ((Object)(object)bloodData != (Object)null && (Object)(object)val == (Object)(object)bloodData)
					{
						val2.SurfaceDataCard = new DataCardReference<SurfaceDataCard>(new Barcode("SLZ.Backlot.SurfaceDataCard.Blood"));
					}
				}
				catch (Exception)
				{
				}
				if ((Object)(object)val != (Object)null)
				{
					val2.surfaceData = val;
					val2._cachedSurfaceData = val;
					val2._isSurfaceDataLoaded = true;
				}
				val2.decalType = (DecalType)(-1);
				num2++;
			}
			Lab.Event("flesh: " + ((Object)avatar).name + " is made of " + (((Object)(object)val != (Object)null) ? (((Object)val).name + " (resists a blade " + val.PenetrationResistance.ToString("0.00") + ")") : "nothing it says") + ", on " + num2 + " parts of its body; voice: " + Clips(avatar.smallPain) + " small pain, " + Clips(avatar.bigPain) + " big pain, " + Clips(avatar.dying) + " dying, " + Clips(avatar.dead) + " dead");
		}
		catch (Exception ex3)
		{
			Lab.Event("flesh: could not be given to its body: " + ex3.Message);
		}
	}

	public void FleshAgain()
	{
		fleshOf = IntPtr.Zero;
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
			SurfaceDataCard val = default(SurfaceDataCard);
			if (!AssetWarehouse.Instance.TryGetDataCard<SurfaceDataCard>(new Barcode("SLZ.Backlot.SurfaceDataCard.Blood"), ref val) || (Object)(object)val == (Object)null)
			{
				Lab.Event("flesh: no blood card to be had");
				return;
			}
			val.SurfaceData.LoadAsset(Action<SurfaceData>.op_Implicit((Action<SurfaceData>)delegate(SurfaceData loaded)
			{
				if ((Object)(object)loaded == (Object)null)
				{
					return;
				}
				bloodData = loaded;
				string text = "none";
				try
				{
					text = ((loaded.impactEffectSpawnable != null && loaded.impactEffectSpawnable.crateRef != null) ? ((ScannableReference)loaded.impactEffectSpawnable.crateRef).Barcode.ID : "none");
				}
				catch (Exception)
				{
				}
				Lab.Event("flesh: the game's blood is " + ((Object)loaded).name + ", resists a blade " + loaded.PenetrationResistance.ToString("0.00") + ", " + loaded.megaPascal.ToString("0.0") + " MPa, effect " + text);
				foreach (Bot item in BotManager.All)
				{
					item.FleshAgain();
				}
			}));
		}
		catch (Exception ex)
		{
			Lab.Event("flesh: the blood could not be asked for: " + ex.Message);
		}
	}

	private static int Clips(AudioVarianceData data)
	{
		if (!((Object)(object)data != (Object)null) || data.audioClips == null)
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
		if ((Object)(object)head == (Object)null)
		{
			return;
		}
		Vector3 val;
		Vector3 val2;
		if (attack != null)
		{
			val = attack.direction;
			if (((Vector3)(ref val)).sqrMagnitude > 0.01f)
			{
				val2 = -attack.direction;
				goto IL_006a;
			}
		}
		val2 = head.forward;
		goto IL_006a;
		IL_006a:
		Vector3 v = val2;
		Quaternion val3 = Quaternion.Euler(0f, 0f - bodyYaw, 0f);
		val = Flat(v);
		Vector3 val4 = val3 * ((Vector3)(ref val)).normalized;
		flinchSide = Mathf.Clamp(val4.x, -1f, 1f);
		flinchSize = Mathf.Clamp(0.6f + damage / MaxHp, 0.6f, 1.4f);
		flinchAt = Time.time;
		if (attack != null)
		{
			val = attack.direction;
			if (((Vector3)(ref val)).sqrMagnitude > 0.01f)
			{
				Vector3 position = head.position;
				val = attack.direction;
				heardAt = position - ((Vector3)(ref val)).normalized * 8f;
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
			Avatar avatar = rig.avatar;
			Transform head = Head;
			if ((Object)(object)avatar == (Object)null || (Object)(object)head == (Object)null)
			{
				return;
			}
			AudioVarianceData val = ((!death) ? (big ? avatar.bigPain : avatar.smallPain) : ((Clips(avatar.dead) > 0) ? avatar.dead : avatar.dying));
			if (Clips(val) == 0)
			{
				val = ((Clips(avatar.bigPain) > 0) ? avatar.bigPain : avatar.smallPain);
			}
			if (Clips(val) == 0)
			{
				HeadSFX componentInChildren = ((Component)rig).GetComponentInChildren<HeadSFX>(true);
				if ((Object)(object)componentInChildren != (Object)null)
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
				Audio3dManager.PlayAtPoint(val.audioClips, head.position, Audio3dManager.hardInteraction, Tuning.F("bot.voice", 0.9f), 1f, new Nullable<float>(0.06f), new Nullable<float>(1f), new Nullable<float>(1f));
			}
		}
		catch (Exception ex)
		{
			Lab.Event("voice failed: " + ex.Message);
		}
	}

	public void LabHurt(float share)
	{
		TakeHit(MaxHp * share, null, (BodyPart)2);
	}

	public string LabState()
	{
		StringBuilder stringBuilder = new StringBuilder(512);
		stringBuilder.Append("{\"t\":").Append(N(Time.time));
		stringBuilder.Append(",\"bot\":\"").Append(Name).Append('"');
		stringBuilder.Append(",\"mode\":\"").Append(mode).Append('"');
		stringBuilder.Append(",\"goal\":\"").Append(goal).Append('"');
		stringBuilder.Append(",\"why\":\"").Append(why.Replace("\"", "'")).Append('"');
		Vector3 val = Feet;
		stringBuilder.Append(",\"pos\":[").Append(N(val.x)).Append(',')
			.Append(N(val.y))
			.Append(',')
			.Append(N(val.z))
			.Append(']');
		stringBuilder.Append(",\"yaw\":").Append(N(bodyYaw));
		stringBuilder.Append(",\"play\":\"").Append(play).Append('"');
		bool flag = (Object)(object)gun != (Object)null && Holding();
		stringBuilder.Append(",\"armed\":").Append(flag ? "true" : "false");
		Vector3 val2;
		if ((Object)(object)gun != (Object)null)
		{
			stringBuilder.Append(",\"gun\":\"").Append(((Object)gun).name).Append('"');
			stringBuilder.Append(",\"ammo\":").Append(N(AmmoFraction()));
			stringBuilder.Append(",\"chamber\":\"").Append(SafeState()).Append('"');
			stringBuilder.Append(",\"reload\":\"").Append(reload).Append('"');
			stringBuilder.Append(",\"support\":\"").Append(((Object)(object)support != (Object)null) ? ((Object)support).name : "none").Append('"');
			stringBuilder.Append(",\"supportHeld\":").Append(supportHeld ? "true" : "false");
			stringBuilder.Append(",\"step\":\"").Append(step).Append('"');
			if ((Object)(object)item != (Object)null)
			{
				StringBuilder stringBuilder2 = stringBuilder.Append(",\"spin\":");
				val2 = item.angularVelocity;
				stringBuilder2.Append(N(((Vector3)(ref val2)).magnitude));
			}
			Transform firePointTransform = gun.firePointTransform;
			if ((Object)(object)firePointTransform != (Object)null)
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
				if ((Object)(object)rightHand != (Object)null)
				{
					StringBuilder stringBuilder3 = stringBuilder.Append(",\"rightOff\":");
					val2 = (gunWant * rightInGun).position - ((Component)rightHand).transform.position;
					stringBuilder3.Append(N(((Vector3)(ref val2)).magnitude));
				}
				Hand leftHand = LeftHand;
				if ((Object)(object)leftHand != (Object)null && supportHeld)
				{
					StringBuilder stringBuilder4 = stringBuilder.Append(",\"leftOff\":");
					val2 = (gunWant * leftInGun).position - ((Component)leftHand).transform.position;
					stringBuilder4.Append(N(((Vector3)(ref val2)).magnitude));
				}
				if ((Object)(object)item != (Object)null)
				{
					stringBuilder.Append(",\"gunOffDeg\":").Append(N(Quaternion.Angle(gunWant.rotation, ((Component)item).transform.rotation)));
				}
			}
		}
		stringBuilder.Append(",\"target\":\"").Append(((Object)(object)target.transform == (Object)null) ? "none" : (((Object)(object)target.zombieHead != (Object)null) ? "zombie" : (((Object)(object)target.brain != (Object)null) ? "npc" : "prop"))).Append('"');
		if ((Object)(object)target.transform != (Object)null && (Object)(object)Head != (Object)null)
		{
			StringBuilder stringBuilder5 = stringBuilder.Append(",\"targetDist\":");
			val2 = TargetPoint() - Head.position;
			stringBuilder5.Append(N(((Vector3)(ref val2)).magnitude));
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
		return mode.ToString() + "/" + goal.ToString() + (((Object)(object)gun != (Object)null) ? (" gun " + ((Object)gun).name + " support " + (supportHeld ? "held" : (((Object)(object)support != (Object)null) ? "not held" : "none")) + " aimErr " + N(labAimError)) : " unarmed");
	}

	private string SafeState()
	{
		try
		{
			string[] array = new string[5];
			CartridgeStates cartridgeState = gun.cartridgeState;
			array[0] = ((object)(CartridgeStates)(ref cartridgeState)).ToString();
			array[1] = "/";
			SlideStates slideState = gun.slideState;
			array[2] = ((object)(SlideStates)(ref slideState)).ToString();
			array[3] = "/mag ";
			array[4] = (gun.HasMagazine() ? gun.AmmoCount().ToString() : "none");
			return string.Concat(array);
		}
		catch (Exception)
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
			FieldInfo[] fields = typeof(Guns).GetFields(BindingFlags.Static | BindingFlags.Public);
			foreach (FieldInfo fieldInfo in fields)
			{
				if (fieldInfo.FieldType == typeof(string) && string.Equals(fieldInfo.Name, which, StringComparison.OrdinalIgnoreCase))
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
		if ((Object)(object)((Rig)rig.physicsRig).centerOfPressure == (Object)null || lessonTopY == float.MinValue)
		{
			return true;
		}
		Vector3 position = ((Rig)rig.physicsRig).centerOfPressure.position;
		float num = Vector3.Dot(position - lessonWallFoot, lessonWallIn);
		Vector3 val2;
		if (!lessonLanding)
		{
			if (num > Tuning.F("tool.climb.stand", 0.45f) + 0.5f && position.y > lessonTopY - 0.2f && FeetOnGround())
			{
				return true;
			}
			lessonLanding = true;
			lessonLandSince = Time.time;
			Vector3 val = lessonWallFoot + lessonWallIn * (Tuning.F("tool.climb.stand", 0.45f) + Tuning.F("tool.climb.onto", 0.9f));
			lessonLandAt = new Vector3(val.x, lessonTopY + 0.05f, val.z);
			if (carried.Count == 0)
			{
				CarryThings(on: true);
			}
			val2 = lessonLandAt - position;
			Lab.Event("lesson: set down on the top of its wall (" + ((Vector3)(ref val2)).magnitude.ToString("0.0") + " m to go)");
			return false;
		}
		val2 = position - lessonLandAt;
		if (((Vector3)(ref val2)).magnitude > 0.08f && Time.time - lessonLandSince < 5f)
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
			Crate val = default(Crate);
			if (instance != null && instance.TryGetCrate(new Barcode(barcode), ref val) && (Object)(object)val != (Object)null)
			{
				GameObjectCrate val2 = ((Il2CppObjectBase)(object)val).TryCast<GameObjectCrate>();
				if ((Object)(object)val2 != (Object)null)
				{
					Bounds colliderBounds = val2.ColliderBounds;
					Vector3 size = ((Bounds)(ref colliderBounds)).size;
					return ((Vector3)(ref size)).magnitude;
				}
			}
		}
		catch (Exception)
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
		Rigidbody val = Pelvis();
		if ((Object)(object)head == (Object)null || (Object)(object)val == (Object)null)
		{
			return;
		}
		Vector3 val2 = (((Object)(object)((Rig)rig.physicsRig).centerOfPressure != (Object)null) ? ((Rig)rig.physicsRig).centerOfPressure.position : Feet);
		Vector3 val3 = Flat(head.forward);
		Vector3 val4;
		if (!(((Vector3)(ref val3)).sqrMagnitude > 0.01f))
		{
			val4 = Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward;
		}
		else
		{
			val3 = Flat(head.forward);
			val4 = ((Vector3)(ref val3)).normalized;
		}
		Vector3 val5 = val4;
		float num2 = Tuning.F("lesson.front", 1.5f) + num * 0.25f;
		bool flag = e.turn != Quaternion.identity && e.at != Vector3.zero;
		if (((Vector3)(ref placeAhead)).sqrMagnitude > 0.01f)
		{
			val5 = placeAhead;
			flag = false;
		}
		if (placeAtSet && ((Vector3)(ref placeAhead)).sqrMagnitude > 0.01f)
		{
			Vector3 point = placeAt;
			RaycastHit val6 = default(RaycastHit);
			if (Physics.Raycast(point + Vector3.up * 2f, Vector3.down, ref val6, 6f, -1, (QueryTriggerInteraction)1))
			{
				point = ((RaycastHit)(ref val6)).point;
			}
			where = point + Vector3.up * 0.15f;
			faces = Quaternion.LookRotation(placeAhead, Vector3.up);
			lessonBigAsked.Add(-1000 - cloneStandIns.Count);
			string[] obj = new string[5]
			{
				"lesson: the ",
				Clone.ThingName(e),
				" put on the way it is to drive, ",
				null,
				null
			};
			val3 = Flat(point - val2);
			obj[3] = ((Vector3)(ref val3)).magnitude.ToString("0.0");
			obj[4] = " m off";
			Lab.Event(string.Concat(obj));
			return;
		}
		Vector3 val7 = (flag ? LessonPlace(e.at) : (val2 + val5 * num2));
		if (flag)
		{
			val3 = Flat(val7 - val2);
			if (((Vector3)(ref val3)).magnitude < 0.9f)
			{
				flag = false;
			}
		}
		if (!flag)
		{
			val7 = val2 + val5 * num2;
			RaycastHit val8 = default(RaycastHit);
			if (Physics.Raycast(val7 + Vector3.up * 2f, Vector3.down, ref val8, 6f, -1, (QueryTriggerInteraction)1))
			{
				val7 = ((RaycastHit)(ref val8)).point + Vector3.up * 0.15f;
			}
			faces = Quaternion.LookRotation(val5, Vector3.up);
		}
		else
		{
			faces = LessonTurn(e.turn);
		}
		where = val7;
		lessonBigAsked.Add(-1000 - cloneStandIns.Count);
		string[] obj2 = new string[8]
		{
			"lesson: the ",
			Clone.ThingName(e),
			" is ",
			num.ToString("0.0"),
			" m across: put ",
			null,
			null,
			null
		};
		val3 = Flat(val7 - val2);
		obj2[5] = ((Vector3)(ref val3)).magnitude.ToString("0.0");
		obj2[6] = " m off";
		obj2[7] = (flag ? ", where yours was from you" : ", in front of it");
		Lab.Event(string.Concat(obj2));
	}

	private void SeatCheck()
	{
		try
		{
			if ((Object)(object)seatAsked == (Object)null || Time.time - seatAskedAt < 1f)
			{
				return;
			}
			Seat val = seatAsked;
			seatAsked = null;
			Rigidbody val2 = Pelvis();
			float num;
			Vector3 val3;
			if (!((Object)(object)val2 != (Object)null))
			{
				num = 0f;
			}
			else
			{
				val3 = Flat(val2.position - ((Component)val).transform.position);
				num = ((Vector3)(ref val3)).magnitude;
			}
			float num2 = num;
			if (((Object)(object)rig.activeSeat == (Object)(object)val && num2 < Tuning.F("seat.off", 0.45f)) || !InLesson)
			{
				seatRetries = 0;
				return;
			}
			if (seatRetries >= 2)
			{
				Lab.Event("lesson: still not sat right (" + num2.ToString("0.00") + " m from its seat), left at that");
				seatRetries = 0;
				return;
			}
			seatRetries++;
			Lab.Event("lesson: not sat right (" + (((Object)(object)rig.activeSeat == (Object)(object)val) ? "" : "not in its seat, ") + num2.ToString("0.00") + " m from it): out and in again (" + seatRetries + ")");
			if ((Object)(object)rig.activeSeat != (Object)null)
			{
				rig.activeSeat.EgressRig(true);
			}
			else if ((int)rig.bodyState == 1)
			{
				rig.SwitchBodyState((BodyState)0);
			}
			Vector3 position = ((Component)val).transform.position;
			val3 = Flat(((Component)val).transform.right);
			Vector3 val4 = position - ((Vector3)(ref val3)).normalized * 1.1f;
			RaycastHit val5 = default(RaycastHit);
			if (Physics.Raycast(val4 + Vector3.up * 1.5f, Vector3.down, ref val5, 4f, -1, (QueryTriggerInteraction)1))
			{
				val4 = ((RaycastHit)(ref val5)).point;
			}
			rig.Teleport(val4, true);
			lessonPelvisHas = false;
			seatMoves = 2;
			seatWaiting = seatAskedOf;
			seatWaitUntil = Time.time + 4f;
			seatSitAt = Time.time + 0.4f;
		}
		catch (Exception)
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
				if ((Object)(object)activeSeat != (Object)null)
				{
					activeSeat.EgressRig(true);
				}
				Lab.Event("lesson: gets out");
				return;
			}
			string key = e.barcode + "#" + Mathf.Max(0, Clone.ThingNumber(e));
			if (!lessonThings.TryGetValue(key, out var value) || (Object)(object)value == (Object)null)
			{
				foreach (Poolee value2 in cloneSpawns.Values)
				{
					if ((Object)(object)value2 != (Object)null && (Object)(object)value2.SpawnableCrate != (Object)null && ((Scannable)value2.SpawnableCrate).Barcode.ID == e.barcode)
					{
						value = value2;
					}
				}
			}
			if ((Object)(object)value == (Object)null)
			{
				if (seatWaiting == null)
				{
					seatWaiting = e;
					seatWaitUntil = Time.time + 4f;
					if (cloneSpawns.Count == 0 && clonePending.Count == 0 && e.barcode.Length > 0)
					{
						Vector3 where = (((Object)(object)Head != (Object)null) ? Head.position : Feet);
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
				Seat[] array = ((Component)value).GetComponentsInChildren<Seat>(true);
				Seat val = ((e.grip >= 0 && e.grip < array.Length) ? array[e.grip] : ((array.Length != 0) ? array[0] : null));
				if ((Object)(object)val == (Object)null)
				{
					return;
				}
				Vector3 val2 = (((Object)(object)((Rig)rig.physicsRig).centerOfPressure != (Object)null) ? ((Rig)rig.physicsRig).centerOfPressure.position : Feet);
				Vector3 val3 = Flat(((Component)val).transform.position - val2);
				if (((Vector3)(ref val3)).magnitude > 3.5f)
				{
					Vector3 position = ((Component)val).transform.position;
					val3 = Flat(((Component)val).transform.right);
					Vector3 val4 = position - ((Vector3)(ref val3)).normalized * 1.1f;
					RaycastHit val5 = default(RaycastHit);
					if (Physics.Raycast(val4 + Vector3.up * 1.5f, Vector3.down, ref val5, 4f, -1, (QueryTriggerInteraction)1))
					{
						val4 = ((RaycastHit)(ref val5)).point;
					}
					rig.Teleport(val4, true);
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
				if ((Object)(object)lessonCar != (Object)(object)((Component)value).transform)
				{
					if ((Object)(object)lessonCar != (Object)null)
					{
						IgnoreCar(lessonCar, ignore: false);
					}
					lessonCar = ((Component)value).transform;
					IgnoreCar(lessonCar, ignore: true);
				}
				val.IngressRig(rig);
				seatAsked = val;
				seatAskedAt = Time.time;
				seatAskedOf = e;
				Lab.Event("lesson: sits down in the " + ((Object)value).name);
			}
		}
		catch (Exception ex)
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
		Rigidbody val = Pelvis();
		Transform head = Head;
		if ((Object)(object)val == (Object)null || (Object)(object)head == (Object)null)
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
		List<Clone.Frame> frames = lesson.take.frames;
		for (int i = 1; i < frames.Count; i++)
		{
			Vector3 val2 = frames[i].pelvis - frames[i - 1].pelvis;
			if (((Vector3)(ref val2)).magnitude > 1.5f)
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
			cloneTo = val.position;
			cloneYaw = Mathf.DeltaAngle(Recorder.Yaw(frame.playspace * frame.head.rotation * Vector3.forward), Recorder.Yaw(head.forward));
		}
		if (wall.HasValue && !atSpot)
		{
			Clone.Frame frame2 = FrameAt(lesson.take, lesson.riseAt);
			cloneFrom = frame2.pelvis;
			cloneTo = new Vector3(wall.Value.foot.x, val.position.y, wall.Value.foot.z);
			cloneYaw = Mathf.DeltaAngle(Recorder.Yaw(frame2.playspace * frame2.head.rotation * Vector3.forward), Recorder.Yaw(wall.Value.inward));
		}
		float num = StandingEyes();
		cloneSize = ((lesson.take.eyes > 0.3f && num > 0.3f) ? Mathf.Clamp(num / lesson.take.eyes, 0.35f, 3f) : 1f);
		Lab.Event("lesson: using the " + lesson.name + " as you did (" + lesson.length.ToString("0.0") + " s" + (atSpot ? ", where you did" : (", where it stands, turned " + cloneYaw.ToString("0") + " deg")) + ", size " + cloneSize.ToString("0.00") + ")");
		return true;
	}

	private static Clone.Frame FrameAt(Clone.Take take, float t)
	{
		List<Clone.Frame> frames = take.frames;
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
		Rigidbody val = Pelvis();
		if (lessonPurposeful && lessonTopY > float.MinValue && cloneLesson.purpose == "climb" && (Object)(object)((Rig)rig.physicsRig).centerOfPressure != (Object)null && (Object)(object)val != (Object)null)
		{
			float y = ((Rig)rig.physicsRig).centerOfPressure.position.y;
			if (cloneLesson.item == "wall" && num > cloneLesson.riseAt && lessonTopY - y <= Tuning.F("wall.finish", 1.55f) && lessonTopY - y > 0.3f)
			{
				FinishOverEdge();
				return;
			}
			if (num > cloneLesson.riseAt && num < cloneLesson.topAt - 0.2f && y + cloneLesson.overRise >= lessonTopY + 0.1f)
			{
				cloneStart -= cloneLesson.topAt - num;
				LessonFromHere(FrameAt(cloneTake, cloneLesson.topAt).pelvis, val.position);
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
			Rigidbody val2 = Pelvis();
			if ((Object)(object)val2 != (Object)null)
			{
				LessonFromHere(FrameAt(cloneTake, cloneLesson.loopFrom).pelvis, val2.position);
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
		Vector3 val = lessonWallFoot + lessonWallIn * Tuning.F("tool.climb.stand", 0.45f);
		BaseController[] array = (BaseController[])(object)new BaseController[2]
		{
			((ControllerRig)controls).leftController,
			((ControllerRig)controls).rightController
		};
		for (int i = 0; i < 2; i++)
		{
			if (!wallHeld[i] || (Object)(object)array[i] == (Object)null)
			{
				continue;
			}
			Transform transform = ((Component)array[i]).transform;
			if (!wallPinned[i])
			{
				Vector3 position = transform.position;
				position -= lessonWallIn * (Vector3.Dot(position - val, lessonWallIn) + Tuning.F("wall.off", 0.06f));
				position.y = Mathf.Min(position.y, lessonTopY - 0.03f);
				wallPin[i] = position;
				wallPinned[i] = true;
			}
			if ((Object)(object)headset != (Object)null)
			{
				Vector3 val2 = wallPin[i] - headset.position;
				if (((Vector3)(ref val2)).magnitude > Tuning.F("wall.arm", 0.85f) && wallPin[i].y < headset.position.y - 0.3f)
				{
					wallHeld[i] = false;
					continue;
				}
			}
			if (!Bad(wallPin[i]))
			{
				transform.position = wallPin[i];
			}
		}
	}

	private void FinishOverEdge()
	{
		Vector3 position = ((Rig)rig.physicsRig).centerOfPressure.position;
		float height = lessonTopY - position.y;
		Vector3 val = lessonWallIn;
		Vector3 val2 = lessonWallFoot + val * Tuning.F("tool.climb.stand", 0.45f);
		Vector3 position2 = default(Vector3);
		((Vector3)(ref position2))._002Ector(val2.x, lessonTopY, val2.z);
		Pose pose = (((Object)(object)Head != (Object)null) ? Pose.Of(Head) : new Pose(position2, Quaternion.LookRotation(val, Vector3.up)));
		Pose pose2 = (((Object)(object)LeftHand != (Object)null) ? Pose.Of(((Component)LeftHand).transform) : pose);
		Pose pose3 = (((Object)(object)RightHand != (Object)null) ? Pose.Of(((Component)RightHand).transform) : pose);
		EndLesson("at the edge of its wall (" + height.ToString("0.0") + " m to the top): up onto it its own way");
		lessonDueAt = Time.time + 20f;
		nav.Clear();
		climbing = true;
		climbLifting = false;
		climbGliding = false;
		climbPhase = 1;
		climbEdge = position2;
		climbIn = val;
		climbHeight = height;
		climbFacing = Quaternion.LookRotation(val, Vector3.up);
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
		bodyYaw = Recorder.Yaw(val);
		walk = Vector2.zero;
		climbHang = true;
		climbHangFeet = position;
	}

	private Rigidbody Pelvis()
	{
		if (!((Object)(object)rig.physicsRig != (Object)null) || !((Object)(object)((Rig)rig.physicsRig).m_pelvis != (Object)null))
		{
			return null;
		}
		return ((Component)((Rig)rig.physicsRig).m_pelvis).GetComponent<Rigidbody>();
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
		List<Clone.Frame> frames = cloneTake.frames;
		bool flag = false;
		Clone.Frame frame;
		for (int i = Mathf.Max(1, lessonTurnsSeen + 1); i <= cloneFrame + 1 && i < frames.Count; i++)
		{
			lessonTurnsSeen = i;
			frame = frames[i - 1];
			float y = ((Quaternion)(ref frame.playspace)).eulerAngles.y;
			frame = frames[i];
			float num = Mathf.DeltaAngle(y, ((Quaternion)(ref frame.playspace)).eulerAngles.y);
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
		if (lessonTurnsSeen == cloneFrame + 1 && cloneFrame + 1 < frames.Count)
		{
			frame = frames[cloneFrame];
			float y2 = ((Quaternion)(ref frame.playspace)).eulerAngles.y;
			frame = frames[cloneFrame + 1];
			if (Mathf.Abs(Mathf.DeltaAngle(y2, ((Quaternion)(ref frame.playspace)).eulerAngles.y)) >= 12f)
			{
				foreach (float lessonJump2 in lessonJumps)
				{
					flag |= Mathf.Abs(lessonJump2 - frames[cloneFrame + 1].t) < 0.3f;
				}
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
		if ((Object)(object)head != (Object)null)
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
			if ((Object)(object)rig.activeSeat != (Object)null)
			{
				rig.activeSeat.EgressRig(true);
			}
		}
		catch (Exception)
		{
		}
		CarryThings(on: false);
		driveMeant = (driving = false);
		ReleaseCloneHands();
		cloneTake = null;
		cloneLesson = null;
		lessonNext = null;
		ClearButtons(((ControllerRig)controls).leftController);
		ClearButtons(((ControllerRig)controls).rightController);
		try
		{
			rig.remapHeptaRig._crouchTarget = 0f;
			rig.remapHeptaRig._feetOffset = 0f;
		}
		catch (Exception)
		{
		}
	}

	private static void ClearButtons(BaseController c)
	{
		if (!((Object)(object)c == (Object)null))
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
			Hand[] array = (Hand[])(object)new Hand[2] { LeftHand, RightHand };
			for (int i = 0; i < 2; i++)
			{
				Grip val = cloneHeld[i];
				Hand val2 = array[i];
				Rigidbody val3 = (((Object)(object)val != (Object)null) ? ((Component)val).GetComponentInParent<Rigidbody>() : null);
				if ((Object)(object)val2 == (Object)null || (Object)(object)val3 == (Object)null)
				{
					continue;
				}
				bool flag = false;
				foreach (var item2 in carried)
				{
					flag |= (Object)(object)item2.body == (Object)(object)val3;
				}
				Pose item = Pose.Of(((Component)val2).transform).Inverse() * Pose.Of(((Component)val3).transform);
				if ((Object)(object)val2.m_CurrentAttachedGO != (Object)null)
				{
					FreeHand(val2);
				}
				if (!flag)
				{
					carried.Add((val3, val2, val, item, val3.isKinematic));
					val3.isKinematic = true;
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
				if (!((Object)(object)item3.body == (Object)null))
				{
					item3.body.isKinematic = item3.wasKinematic;
					item3.body.velocity = Vector3.zero;
					item3.body.angularVelocity = Vector3.zero;
					if ((Object)(object)item3.hand != (Object)null && (Object)(object)item3.grip != (Object)null && (Object)(object)item3.hand.m_CurrentAttachedGO == (Object)null && mode == Mode.Clone && cloneLesson != null)
					{
						Grab(item3.hand, item3.grip);
					}
				}
			}
			catch (Exception)
			{
			}
		}
		carried.Clear();
	}

	private void CarriedFollow()
	{
		foreach (var item in carried)
		{
			if (!((Object)(object)item.body == (Object)null) && !((Object)(object)item.hand == (Object)null))
			{
				Pose pose = Pose.Of(((Component)item.hand).transform) * item.inHand;
				if (!Bad(pose.position) && !Bad(pose.rotation))
				{
					item.body.position = pose.position;
					item.body.rotation = pose.rotation;
					((Component)item.body).transform.SetPositionAndRotation(pose.position, pose.rotation);
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
		Hand[] array = (Hand[])(object)new Hand[2] { LeftHand, RightHand };
		for (int i = 0; i < 2; i++)
		{
			Grip val = cloneHeld[i];
			Hand val2 = array[i];
			if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null || (Object)(object)val2.m_CurrentAttachedGO != (Object)null)
			{
				continue;
			}
			try
			{
				Rigidbody componentInParent = ((Component)val).GetComponentInParent<Rigidbody>();
				if ((Object)(object)componentInParent != (Object)null)
				{
					Vector3 val3 = ((Component)val).transform.position - componentInParent.position;
					componentInParent.velocity = Vector3.zero;
					componentInParent.angularVelocity = Vector3.zero;
					componentInParent.position = ((Component)val2).transform.position - val3;
					((Component)componentInParent).transform.position = ((Component)val2).transform.position - val3;
				}
				if (Grab(val2, val))
				{
					Lab.Event("lesson: the " + ((Object)val).name + " had come out of its " + ((i == 0) ? "left" : "right") + " hand, taken again");
				}
			}
			catch (Exception)
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
			if ((Object)(object)nearestSeen.transform != (Object)null && nearestSeenDistance < 25f)
			{
				EndLesson("broken off: an enemy " + nearestSeenDistance.ToString("0") + " m off");
			}
		}
	}

	private bool LessonUnsafe(Hand hand)
	{
		GameObject val = (((Object)(object)hand != (Object)null) ? hand.m_CurrentAttachedGO : null);
		Gun val2 = (((Object)(object)val != (Object)null) ? val.GetComponentInParent<Gun>() : null);
		Transform head = Player.Head;
		if ((Object)(object)val2 == (Object)null || (Object)(object)val2.firePointTransform == (Object)null || (Object)(object)head == (Object)null)
		{
			return false;
		}
		Vector3 val3 = head.position - Vector3.up * 0.3f - val2.firePointTransform.position;
		if (((Vector3)(ref val3)).magnitude < 60f)
		{
			return Vector3.Angle(val2.firePointTransform.forward, val3) < Mathf.Max(10f, Mathf.Atan2(0.6f, ((Vector3)(ref val3)).magnitude) * 57.29578f);
		}
		return false;
	}

	private Clone.HandInput LessonInput(bool left, Clone.Frame now, out bool tapped)
	{
		Clone.HandInput result = (left ? now.leftIn : now.rightIn);
		tapped = false;
		List<Clone.Frame> frames = cloneTake.frames;
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
		if (!((Object)(object)c == (Object)null))
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
			if ((Object)(object)c == (Object)(object)((ControllerRig)controls).rightController)
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
		if (!(Time.time < lessonDueAt) && (act != Act.Go || planSight != Sight.Tall) && !Playing && !climbing && !Reloading && !portalGoing && !falling && Time.time >= waveUntil && (Object)(object)nearestSeen.transform == (Object)null && (Object)(object)brainTarget.transform == (Object)null && (mode == Mode.Wander || mode == Mode.Brain || (mode == Mode.Follow && !following)))
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
		int num;
		if (lessonNext.level.Length > 0 && lessonNext.level == Lessons.Level)
		{
			Vector3 val = Flat(startFeet - feet);
			if (((Vector3)(ref val)).magnitude < Tuning.F("lesson.spot", 60f) && Mathf.Abs(startFeet.y - feet.y) < 6f)
			{
				num = ((Random.value < Tuning.F("lesson.spot.chance", 0.6f)) ? 1 : 0);
				goto IL_00d2;
			}
		}
		num = 0;
		goto IL_00d2;
		IL_00d2:
		lessonAtSpot = (byte)num != 0;
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
		int num;
		if (lessonAtSpot)
		{
			Vector3 val = Flat(lesson.take.startFeet - Feet);
			num = ((((Vector3)(ref val)).magnitude < 1.5f) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool atSpot = (byte)num != 0;
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
		float num;
		if (!((Object)(object)you != (Object)null) || !((Object)(object)head != (Object)null))
		{
			num = 99f;
		}
		else
		{
			Vector3 val = Flat(you.position - head.position);
			num = ((Vector3)(ref val)).magnitude;
		}
		if (num < 6f)
		{
			float num2 = Watch(you, head, dt, 90f);
			headYaw = Mathf.MoveTowards(headYaw, Mathf.Clamp(num2, -60f, 60f), 120f * dt);
		}
		else
		{
			headYaw = Mathf.Sin((Time.time - born) * 0.5f) * 45f + Mathf.Sin((Time.time - born) * 0.17f) * 20f;
		}
	}

	private void NextAct(Transform you)
	{
		Vector3 val = (((Object)(object)rig.physicsRig != (Object)null && (Object)(object)((Rig)rig.physicsRig).centerOfPressure != (Object)null) ? ((Rig)rig.physicsRig).centerOfPressure.position : Feet);
		bool flag = (Object)(object)gun != (Object)null;
		int num;
		if ((Object)(object)you != (Object)null)
		{
			Vector3 val2 = Flat(you.position - val);
			num = ((((Vector3)(ref val2)).magnitude < 25f) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
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
		if (tinkerWant != null && !enemy && Begin(Act.Tinker, you, val))
		{
			lastAct = Act.Tinker;
			Lab.Event("life: Tinker (heard of it)");
			return;
		}
		for (int i = 0; i < 4; i++)
		{
			float num2 = 0f;
			(Act, float)[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				var (act, num3) = array2[j];
				if (act != lastAct || act == Act.Go)
				{
					num2 += num3;
				}
			}
			float num4 = Random.value * num2;
			Act what = Act.Go;
			array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				var (act2, num5) = array2[j];
				if (act2 != lastAct || act2 == Act.Go)
				{
					num4 -= num5;
					if (num4 <= 0f)
					{
						what = act2;
						break;
					}
				}
			}
			if (Begin(what, you, val))
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
		if (what == Act.Stroll && Random.value < Tuning.F("life.sneak", 0.22f))
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
			if (Random.value < Tuning.F("life.squat", 0.5f))
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
			if ((Object)(object)you == (Object)null || !Navigator.RandomPlace(you.position - Vector3.up * 1.4f, 1.5f, 3f, out var place))
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
			if (Random.value < 0.35f && SelfTaught.KnownTop(feet, 50f, out var top))
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
			Vector3 val = Flat(place2 - feet);
			Lab.Event("life: exploring " + ((Vector3)(ref val)).magnitude.ToString("0") + " m off" + (visited.Contains(Cell(place2)) ? "" : ", somewhere new"));
			return true;
		}
		case Act.Use:
			return BeginLessonAct(feet);
		case Act.Play:
			if (Random.value < Tuning.F("play.shove", 0.35f) && FindSomethingToShove())
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
		NavMeshHit val3 = default(NavMeshHit);
		for (int i = 0; i < 28; i++)
		{
			Vector2 insideUnitCircle = Random.insideUnitCircle;
			Vector2 val = ((Vector2)(ref insideUnitCircle)).normalized * Random(a, num2);
			Vector3 val2 = feet + new Vector3(val.x, 0f, val.y);
			Vector3 ground;
			try
			{
				if (NavMesh.SamplePosition(val2, ref val3, 4f, -1))
				{
					ground = Navigator.Surface(((NavMeshHit)(ref val3)).position, feet.y);
				}
				else if (!Ground(val2, out ground))
				{
					continue;
				}
			}
			catch (Exception)
			{
				if (!Ground(val2, out ground))
				{
					continue;
				}
			}
			float num3 = (visited.Contains(Cell(ground)) ? 0f : 3f);
			Vector3 val4 = Flat(ground - feet);
			float num4 = num3 + ((Vector3)(ref val4)).magnitude / num2 + Random.value * 0.6f - Mathf.Abs(ground.y - feet.y) * 0.02f - (float)SelfTaught.BadAt(ground) * 1.5f;
			if (num4 > num)
			{
				num = num4;
				place = ground;
			}
		}
		return num > float.MinValue;
	}

	private static bool Ground(Vector3 p, out Vector3 ground)
	{
		ground = p;
		RaycastHit val = default(RaycastHit);
		if (!Physics.Raycast(p + Vector3.up * 25f, Vector3.down, ref val, 60f, -1, (QueryTriggerInteraction)1))
		{
			return false;
		}
		if (((RaycastHit)(ref val)).normal.y < 0.65f || (Object)(object)((RaycastHit)(ref val)).collider.attachedRigidbody != (Object)null)
		{
			return false;
		}
		if (Physics.CheckCapsule(((RaycastHit)(ref val)).point + Vector3.up * 0.4f, ((RaycastHit)(ref val)).point + Vector3.up * 1.6f, 0.25f, -1, (QueryTriggerInteraction)1))
		{
			return false;
		}
		ground = ((RaycastHit)(ref val)).point;
		return true;
	}

	private bool FindHighGround(Vector3 feet, out Vector3 top)
	{
		top = Vector3.zero;
		float num = Random.Range(0f, 360f);
		RaycastHit val2 = default(RaycastHit);
		RaycastHit val4 = default(RaycastHit);
		for (int i = 0; i < 16; i++)
		{
			Vector3 val = Quaternion.Euler(0f, num + (float)i * 22.5f, 0f) * Vector3.forward;
			if (!Physics.Raycast(feet + Vector3.up * 0.6f, val, ref val2, 15f, -1, (QueryTriggerInteraction)1) || ((RaycastHit)(ref val2)).distance < 1.5f || (Object)(object)((RaycastHit)(ref val2)).collider.attachedRigidbody != (Object)null || (Object)(object)((Component)((RaycastHit)(ref val2)).collider).GetComponentInParent<RigManager>() != (Object)null)
			{
				continue;
			}
			Vector3 val3 = -Flat(((RaycastHit)(ref val2)).normal);
			if (((Vector3)(ref val3)).sqrMagnitude < 0.01f)
			{
				continue;
			}
			((Vector3)(ref val3)).Normalize();
			if (Physics.Raycast(((RaycastHit)(ref val2)).point + val3 * 0.6f + Vector3.up * 3.3f, Vector3.down, ref val4, 3.6f, -1, (QueryTriggerInteraction)1))
			{
				float num2 = ((RaycastHit)(ref val4)).point.y - feet.y;
				if (!(((RaycastHit)(ref val4)).normal.y < 0.7f) && !(num2 < Tuning.F("climb.min", 1.2f)) && !(num2 > Tuning.F("climb.max", 2.5f)) && !Physics.Raycast(((RaycastHit)(ref val4)).point + Vector3.up * 0.05f, Vector3.up, 1.6f, -1, (QueryTriggerInteraction)1) && SelfTaught.ClimbFails(((RaycastHit)(ref val4)).point) < 2)
				{
					top = ((RaycastHit)(ref val4)).point + Vector3.up * 0.05f;
					return true;
				}
			}
		}
		return false;
	}

	private static float Random(float a, float b)
	{
		return Random.Range(a, b);
	}

	public void BecomePuppet(byte owner, byte id)
	{
		netOwner = owner;
		netId = id;
		netFrames.Clear();
		netOffsetSet = false;
		if ((Object)(object)((Component)rig).transform.Find("ThirdEyeNetPuppet") == (Object)null)
		{
			new GameObject("ThirdEyeNetPuppet").transform.SetParent(((Component)rig).transform, false);
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
		if ((Object)(object)rig.physicsRig == (Object)null || (Object)(object)controls == (Object)null || (Object)(object)controls.vrRoot == (Object)null || (Object)(object)controls.headset == (Object)null || (Object)(object)((ControllerRig)controls).leftController == (Object)null || (Object)(object)((ControllerRig)controls).rightController == (Object)null)
		{
			return null;
		}
		Rigidbody val = Pelvis();
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		Clone.Frame frame = default(Clone.Frame);
		frame.t = NetBots.Now;
		frame.pelvis = val.position;
		frame.pelvisTurn = val.rotation;
		frame.pelvisVelocity = val.velocity;
		frame.playspace = controls.vrRoot.rotation;
		frame.head = new Pose(controls.headset.localPosition, controls.headset.localRotation);
		frame.left = new Pose(((Component)((ControllerRig)controls).leftController).transform.localPosition, ((Component)((ControllerRig)controls).leftController).transform.localRotation);
		frame.right = new Pose(((Component)((ControllerRig)controls).rightController).transform.localPosition, ((Component)((ControllerRig)controls).rightController).transform.localRotation);
		frame.leftIn = NetHand(((ControllerRig)controls).leftController);
		frame.rightIn = NetHand(((ControllerRig)controls).rightController);
		frame.crouch = (((Object)(object)rig.remapHeptaRig != (Object)null) ? rig.remapHeptaRig._crouchTarget : 0f);
		frame.feet = (((Object)(object)rig.remapHeptaRig != (Object)null) ? rig.remapHeptaRig._feetOffset : 0f);
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
			GameObject val = (((Object)(object)hand != (Object)null) ? hand.m_CurrentAttachedGO : null);
			MarrowEntity val2 = (((Object)(object)val != (Object)null) ? val.GetComponentInParent<MarrowEntity>() : null);
			return ((Object)(object)val2 != (Object)null) ? NetBots.Own(val2) : ushort.MaxValue;
		}
		catch (Exception)
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
		catch (Exception)
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
			Transform val = NetBots.Shared(thing);
			if ((Object)(object)val != (Object)null)
			{
				NoPushOnItself(val, again: true);
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
			BodyPart part2 = (BodyPart)part;
			damage = ((!bullet) ? Mathf.Min(damage, MaxHp * Tuning.F("bot.knock.most", 0.3f)) : BulletHarm(damage, part2));
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
			TripByHit(damage, part2, null, bullet);
		}
	}

	private void NetStruck(float damage, Attack attack, BodyPart part)
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
				AttackType attackType = attack.attackType;
				bullet = ((object)(AttackType)(ref attackType)).ToString().IndexOf("Pierc", StringComparison.OrdinalIgnoreCase) >= 0;
				RigManager val = (((Object)(object)attack.proxy != (Object)null) ? ((Component)attack.proxy).GetComponentInParent<RigManager>() : null);
				if ((Object)(object)val != (Object)null)
				{
					Bot bot = BotManager.Of(val);
					flag = (Object)(object)val == (Object)(object)Player.RigManager || (bot != null && !bot.Puppet);
				}
				else
				{
					Hand[] array = (Hand[])(object)new Hand[2]
					{
						Player.LeftHand,
						Player.RightHand
					};
					foreach (Hand val2 in array)
					{
						if (!((Object)(object)val2 != (Object)null) || !((Object)(object)val2.rb != (Object)null))
						{
							continue;
						}
						Vector3 val3 = ((Component)val2).transform.position - attack.origin;
						if (((Vector3)(ref val3)).magnitude < 0.5f)
						{
							val3 = val2.rb.velocity;
							if (((Vector3)(ref val3)).magnitude > 1.2f)
							{
								flag = true;
							}
						}
					}
				}
			}
		}
		catch (Exception)
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
			((Component)((ControllerRig)controls).leftController).transform.SetLocalPositionAndRotation(pose2.position, pose2.rotation);
			((Component)((ControllerRig)controls).rightController).transform.SetLocalPositionAndRotation(pose3.position, pose3.rotation);
			PutInput(((ControllerRig)controls).leftController, a.leftIn, b.leftIn, k, trigger: false);
			PutInput(((ControllerRig)controls).rightController, a.rightIn, b.rightIn, k, trigger: false);
			rig.remapHeptaRig._crouchTarget = Mathf.Lerp(a.crouch, b.crouch, k);
			rig.remapHeptaRig._feetOffset = Mathf.Lerp(a.feet, b.feet, k);
			((ControllerRig)controls).leftController._thumbstickAxis = Vector2.zero;
			((ControllerRig)controls)._axisPrimary = Vector2.zero;
			bodyYaw = vrRoot.eulerAngles.y;
		}
	}

	public void NetFixed(float dt)
	{
		if (dead || !Whole || !NetNow(out var a, out var b, out var k))
		{
			return;
		}
		Rigidbody val = Pelvis();
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		Vector3 val2 = Vector3.Lerp(a.pelvis, b.pelvis, k);
		Vector3 val3 = Vector3.Lerp(a.pelvisVelocity, b.pelvisVelocity, k);
		Vector3 val4 = val2 - val.position;
		if (((Vector3)(ref val4)).magnitude > Tuning.F("net.snap", 2.5f))
		{
			try
			{
				rig.Teleport(((Rig)rig.physicsRig).centerOfPressure.position + val4, false);
				return;
			}
			catch (Exception)
			{
				return;
			}
		}
		float num = Tuning.F("net.pull", 14f);
		Vector3 val5 = val3 + new Vector3(val4.x, 0f, val4.z) * num - val.velocity;
		if (Mathf.Abs(val3.y) < Tuning.F("net.vertical", 1.2f) && Mathf.Abs(val4.y) < 0.5f && !netDown)
		{
			val5.y = 0f;
		}
		else
		{
			val5.y += val4.y * num * 0.5f;
		}
		val.AddForce(Vector3.ClampMagnitude(val5 * Tuning.F("net.push", 18f), 120f), (ForceMode)5);
	}

	public static bool IsWeapon(Transform whole)
	{
		Rigidbody componentInChildren = ((Component)whole).GetComponentInChildren<Rigidbody>();
		if ((Object)(object)componentInChildren != (Object)null)
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
			if ((Object)(object)item.rig != (Object)null && (Object)(object)((Component)item.rig).transform == (Object)(object)what)
			{
				bot = item;
			}
		}
		Targets.Target target = default(Targets.Target);
		if (bot != null)
		{
			Targets.Target target2 = default(Targets.Target);
			target2.transform = ((Component)bot.rig).transform;
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
				if ((Object)(object)item2.transform == (Object)(object)what)
				{
					target = item2;
				}
			}
		}
		if ((Object)(object)target.transform == (Object)null)
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
		if ((Object)(object)ordered.transform == (Object)null)
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
			Vector3 val = ordered.Point - eye;
			nearestSeenDistance = ((Vector3)(ref val)).magnitude;
			lastEnemyAt = ordered.Point;
			lastEnemyTime = Time.time;
		}
	}

	public void TakeFromYou(Transform thing)
	{
		try
		{
			Hand rightHand = RightHand;
			if ((Object)(object)rightHand == (Object)null)
			{
				return;
			}
			StopPlaying();
			if ((Object)(object)rightHand.m_CurrentAttachedGO != (Object)null)
			{
				FreeHand(rightHand);
			}
			Gun componentInChildren = ((Component)thing).GetComponentInChildren<Gun>();
			bool flag;
			if ((Object)(object)componentInChildren != (Object)null && (Object)(object)componentInChildren.triggerGrip != (Object)null)
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
				Rigidbody componentInChildren2 = ((Component)thing).GetComponentInChildren<Rigidbody>();
				Grip val = (((Object)(object)componentInChildren2 != (Object)null) ? GripOf(componentInChildren2) : null);
				item = componentInChildren2;
				grip = val;
				gun = null;
				flag = (Object)(object)val != (Object)null && Grab(rightHand, val);
				if (flag)
				{
					MeasureMelee();
					Next(Play.Melee);
				}
			}
			Lab.Event("give: " + (flag ? ("took the " + ((Object)thing).name + " from your hand") : ("could not take the " + ((Object)thing).name)));
			if (flag)
			{
				Spawnables.Notify(Name + " took the " + ((Object)thing).name);
			}
		}
		catch (Exception ex)
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
		if (bot == null || (Object)(object)bot.Head == (Object)null)
		{
			return;
		}
		Transform val = null;
		float num = float.MaxValue;
		foreach (Targets.Target item2 in Targets.Hostiles())
		{
			float num2;
			if (!((Object)(object)item2.transform != (Object)null))
			{
				num2 = float.MaxValue;
			}
			else
			{
				Vector3 val2 = item2.Point - bot.Head.position;
				num2 = ((Vector3)(ref val2)).magnitude;
			}
			float num3 = num2;
			if (num3 < num)
			{
				num = num3;
				val = item2.transform;
			}
		}
		if ((Object)(object)val == (Object)null)
		{
			foreach (Bot item3 in BotManager.All)
			{
				if (item3 != bot && !item3.dead)
				{
					val = ((Component)item3.rig).transform;
					break;
				}
			}
		}
		if ((Object)(object)val == (Object)null)
		{
			Lab.Event("orders: nothing about to send it after");
			return;
		}
		bot.Ordered(val);
		Lab.Event("orders: " + bot.Name + " sent after " + ((Object)val).name + " (lab)");
	}

	private bool FindSomethingToShove()
	{
		Vector3 val = Feet;
		Rigidbody val2 = null;
		float num = float.MaxValue;
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		foreach (Collider item in Physics.OverlapSphere(val + Vector3.up * 0.5f, Tuning.F("play.shove.seek", 12f), -1, (QueryTriggerInteraction)1))
		{
			Rigidbody val3 = (((Object)(object)item != (Object)null) ? item.attachedRigidbody : null);
			if ((Object)(object)val3 == (Object)null || val3.isKinematic || !hashSet.Add(((Il2CppObjectBase)(object)val3).Pointer) || val3.mass < 1f || val3.mass > Tuning.F("play.shove.most", 80f) || (Object)(object)((Component)val3).GetComponentInParent<RigManager>() != (Object)null || NotForBots(val3) || HeldByAnyone(((Component)val3).transform, null))
			{
				continue;
			}
			Vector3 val4 = val3.velocity;
			if (((Vector3)(ref val4)).magnitude > 0.5f || (Object)(object)((Component)val3).GetComponentInParent<Poolee>() == (Object)null || (Object)(object)((Component)((Component)val3).GetComponentInParent<Poolee>()).GetComponentInChildren<Seat>(true) != (Object)null)
			{
				continue;
			}
			float num2 = val3.worldCenterOfMass.y - val.y;
			if (!(num2 < -0.3f) && !(num2 > 1.4f))
			{
				val4 = Flat(val3.worldCenterOfMass - val);
				float num3 = ((Vector3)(ref val4)).magnitude + Random.value * 4f;
				if (num3 < num)
				{
					num = num3;
					val2 = val3;
				}
			}
		}
		if ((Object)(object)val2 == (Object)null)
		{
			return false;
		}
		this.item = val2;
		grip = null;
		gun = null;
		shoveStage = 0;
		shoveStageAt = Time.time;
		shoved = false;
		play = Play.Shove;
		playAt = Time.time;
		nav.SetGoal(val2.worldCenterOfMass);
		Lab.Event("play: going to shove the " + ((Object)val2).name + " (" + val2.mass.ToString("0.0") + " kg)");
		return true;
	}

	private bool ShoveStep(Transform head, float dt)
	{
		if ((Object)(object)item == (Object)null || (Object)(object)head == (Object)null)
		{
			return EndPlay();
		}
		float num = Time.time - shoveStageAt;
		Vector3 worldCenterOfMass = item.worldCenterOfMass;
		Vector3 val = Flat(worldCenterOfMass - head.position);
		float magnitude = ((Vector3)(ref val)).magnitude;
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
			float num2 = Mathf.SmoothStep(0f, 1f, num / 0.4f);
			handAt = Vector3.Lerp(new Vector3(0.2f, standingHeight - 0.5f, 0.25f), new Vector3(0.22f, standingHeight - 0.45f, -0.05f), num2);
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
			Vector3 val2 = (Vector3)(((Object)(object)vrRoot != (Object)null) ? vrRoot.InverseTransformPoint(worldCenterOfMass) : new Vector3(0.2f, standingHeight - 0.5f, 0.6f));
			float num3 = Mathf.Clamp01(num / 0.18f);
			handAt = Vector3.Lerp(new Vector3(0.22f, standingHeight - 0.45f, -0.05f), val2, num3);
			handTurn = Quaternion.Euler(0f, 0f, -90f);
			playHeight = standingHeight;
			if (!shoved && num3 > 0.8f)
			{
				shoved = true;
				val = Flat(worldCenterOfMass - head.position);
				Vector3 normalized = ((Vector3)(ref val)).normalized;
				float num4 = Mathf.Clamp(Tuning.F("play.shove.push", 30f) / Mathf.Max(1f, item.mass), Tuning.F("play.shove.least", 1.2f), Tuning.F("play.shove.fastest", 6f));
				item.AddForceAtPosition((normalized + Vector3.up * 0.25f) * num4 * item.mass, worldCenterOfMass + Vector3.up * 0.25f, (ForceMode)1);
				Lab.Event("play: shoved the " + ((Object)item).name + " (" + item.mass.ToString("0.0") + " kg) at " + num4.ToString("0.0") + " m/s");
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
		if (!Holding() || (Object)(object)you == (Object)null || (Object)(object)head == (Object)null)
		{
			return EndPlay();
		}
		float num = Time.time - playAt;
		if (OthersHoldIt())
		{
			Release();
			Lab.Event("play: you took the " + ((Object)item).name + " from it");
			return EndPlay();
		}
		Vector3 val = you.position - Vector3.up * 1.5f;
		Vector3 val2 = Flat(you.position - head.position);
		float magnitude = ((Vector3)(ref val2)).magnitude;
		handOverride = true;
		playHeight = standingHeight;
		if (magnitude > Tuning.F("play.bring.near", 1.6f) && num < Tuning.F("play.bring.most", 25f))
		{
			handAt = new Vector3(0.18f, standingHeight - 0.5f, 0.35f);
			handTurn = Quaternion.Euler(20f, -20f, 0f);
			playPitch = 10f;
			nav.SetGoal(val);
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
			Lab.Event("play: holding the " + ((Object)item).name + " out to you");
		}
		handAt = new Vector3(0.12f, standingHeight - 0.35f, 0.5f);
		handTurn = Quaternion.Euler(10f, -10f, 0f);
		playPitch = LookPitch(you, head);
		if (Time.time - offerSince > Tuning.F("play.bring.offer", 2f))
		{
			Lab.Event("play: put the " + ((Object)item).name + " down at your feet");
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
		if (droveHeadHas)
		{
			Vector3 val = root.position - droveHead;
			if (((Vector3)(ref val)).magnitude > 1f)
			{
				droveJumpAt = Time.time;
			}
		}
		droveHead = root.position;
		droveHeadHas = true;
		if (climbing)
		{
			droveHas = false;
		}
		if (droveHas)
		{
			float num = Mathf.DeltaAngle(droveYaw, root.eulerAngles.y);
			if (Mathf.Abs(num) > 3f)
			{
				droveTurn = num;
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
		if ((Object)(object)head == (Object)null)
		{
			return false;
		}
		Vector3 position = head.position;
		int result;
		if (portalHeadHas)
		{
			Vector3 val = position - portalHeadWas;
			result = ((((Vector3)(ref val)).magnitude > 1.5f) ? 1 : 0);
		}
		else
		{
			result = 0;
		}
		portalHeadWas = position;
		portalHeadHas = true;
		return (byte)result != 0;
	}

	private bool PortalStep(Vector3 goal, bool afterPlayer, float dt)
	{
		bool flag = PortalCarried();
		if (climbing || Time.time < portalNotBefore)
		{
			portalGoing = false;
			return false;
		}
		Vector3 val = Feet;
		Vector3 val2;
		if (!portalGoing)
		{
			bool flag2 = false;
			PortalWays.Way best = default(PortalWays.Way);
			if (afterPlayer && PortalWays.PlayerTookAt > portalTookSeen && Time.time - PortalWays.PlayerTookAt < 12f)
			{
				portalTookSeen = PortalWays.PlayerTookAt;
				best = PortalWays.PlayerTook;
				val2 = best.inAt - val;
				int num;
				if (((Vector3)(ref val2)).magnitude < Tuning.F("portal.follow", 30f))
				{
					val2 = best.inAt - val;
					float magnitude = ((Vector3)(ref val2)).magnitude;
					val2 = goal - val;
					num = ((magnitude < ((Vector3)(ref val2)).magnitude + 2f) ? 1 : 0);
				}
				else
				{
					num = 0;
				}
				flag2 = (byte)num != 0;
			}
			if (!flag2 && Time.time - portalThroughAt > 4f)
			{
				flag2 = PortalWays.Shorter(val, goal, out best);
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
			val2 = best.inAt - val;
			Lab.Event("portals: going through one, " + ((Vector3)(ref val2)).magnitude.ToString("0") + " m off" + (afterPlayer ? " (after you)" : ""));
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
		Vector3 val3;
		if (!flag3)
		{
			Vector3 inAt = portalWay.inAt;
			val2 = Flat(inOpen);
			val3 = inAt + ((Vector3)(ref val2)).normalized * 1.1f;
		}
		else
		{
			val3 = portalWay.inAt;
		}
		Vector3 val4 = val3;
		RaycastHit val5 = default(RaycastHit);
		if (!flag3 && Physics.Raycast(val4, Vector3.down, ref val5, 4f, -1, (QueryTriggerInteraction)1))
		{
			val4 = ((RaycastHit)(ref val5)).point;
		}
		if (portalPhase == 0)
		{
			if (nav.HasGoal)
			{
				val2 = nav.Goal - val4;
				if (!(((Vector3)(ref val2)).magnitude > 0.5f))
				{
					goto IL_0264;
				}
			}
			nav.SetGoal(val4);
			goto IL_0264;
		}
		running = false;
		Vector3 val6 = Flat(val - portalWay.inAt);
		val2 = Flat(inOpen);
		Vector3 val7;
		if (!(((Vector3)(ref val2)).sqrMagnitude > 0.01f))
		{
			val7 = Vector3.zero;
		}
		else
		{
			val2 = Flat(inOpen);
			val7 = ((Vector3)(ref val2)).normalized;
		}
		Vector3 val8 = val7;
		val2 = val6 - val8 * Vector3.Dot(val6, val8);
		Vector3 val9 = Flat(((((Vector3)(ref val2)).magnitude > 0.2f && !flag3) ? (portalWay.inAt + val8 * 0.8f) : (portalWay.inAt - inOpen * 0.6f)) - val);
		if (((Vector3)(ref val9)).sqrMagnitude < 0.01f)
		{
			val9 = -Flat(inOpen);
		}
		Go(((Vector3)(ref val9)).normalized, MotionLibrary.WalkPush, dt);
		string state = "";
		bool flag4 = false;
		try
		{
			flag4 = PortalPush(Time.time - portalPhaseAt, out state);
		}
		catch (Exception ex)
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
		IL_02e6:
		portalPhase = 1;
		portalPhaseAt = Time.time;
		goto IL_0326;
		IL_0264:
		val2 = val4 - val;
		running = ((Vector3)(ref val2)).magnitude > 6f;
		if (!Travel(running ? MotionLibrary.RunPush : MotionLibrary.WalkPush, 0.45f, dt))
		{
			val2 = Flat(val4 - val);
			if (!(((Vector3)(ref val2)).magnitude < 0.6f))
			{
				if (flag3)
				{
					val2 = Flat(val4 - val);
					if (((Vector3)(ref val2)).magnitude < 0.4f)
					{
						goto IL_02e6;
					}
				}
				if (Time.time - portalSince > 20f || nav.StuckCount >= 4)
				{
					GiveUpPortal("could not get to it");
				}
				goto IL_0326;
			}
		}
		goto IL_02e6;
		IL_0326:
		return portalGoing;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool PortalPush(float tried, out string state)
	{
		state = "";
		Transform head = Head;
		Portal val = (((Object)(object)portalWay.portal != (Object)null) ? ((Il2CppObjectBase)(object)portalWay.portal).TryCast<Portal>() : null);
		Portal val2 = (((Object)(object)portalWay.other != (Object)null) ? ((Il2CppObjectBase)(object)portalWay.other).TryCast<Portal>() : null);
		if ((Object)(object)head == (Object)null || (Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null)
		{
			state = "the portal is gone";
			return false;
		}
		Vector3 val3 = ((Component)val).transform.InverseTransformPoint(head.position);
		Vector2 val4 = val.Size * 0.5f;
		bool flag = Mathf.Abs(val3.x) <= val4.x && Mathf.Abs(val3.y) <= val4.y;
		TeleportableRigManager component = ((Component)rig).GetComponent<TeleportableRigManager>();
		state = "head " + ((Vector3)(ref val3)).ToString("0.00") + " to it, its half size " + ((Vector2)(ref val4)).ToString("0.00") + (((Object)(object)component == (Object)null) ? ", its body is not one the portals know" : (", bodies " + ((Teleportable)component).Bodies.Count + (((Teleportable)component).HasPortals ? ", in a portal" : ", in none")));
		if ((Object)(object)component != (Object)null && flag && val3.z < -0.02f)
		{
			((Teleportable)component).Teleport(val, val2);
			state += "; carried by the portal on being asked";
			return true;
		}
		Vector2 val5 = val4 + new Vector2(0.35f, 0.9f);
		bool flag2 = Mathf.Abs(val3.x) <= val5.x && Mathf.Abs(val3.y) <= val5.y;
		if (tried > Tuning.F("portal.patience", 3.5f) && flag2 && val3.z < Tuning.F("portal.reach", 0.9f))
		{
			Vector3 outOpen = portalWay.outOpen;
			Vector3 val6;
			Vector3 val7;
			if (outOpen.y > 0.7f)
			{
				val6 = portalWay.outAt + Vector3.up * 0.1f;
			}
			else if (outOpen.y < -0.7f)
			{
				val6 = portalWay.outAt + outOpen * 2f;
			}
			else
			{
				Vector3 outAt = portalWay.outAt;
				val7 = Flat(outOpen);
				Vector3 val8 = outAt + ((Vector3)(ref val7)).normalized * 0.9f;
				RaycastHit val9 = default(RaycastHit);
				val6 = (Physics.Raycast(val8, Vector3.down, ref val9, 5f, -1, (QueryTriggerInteraction)1) ? ((RaycastHit)(ref val9)).point : (val8 - Vector3.up));
			}
			val7 = Flat(outOpen);
			Vector3 val10;
			if (!(((Vector3)(ref val7)).sqrMagnitude > 0.01f))
			{
				val10 = Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward;
			}
			else
			{
				val7 = Flat(outOpen);
				val10 = ((Vector3)(ref val7)).normalized;
			}
			Vector3 forward = val10;
			bodyYaw = Recorder.Yaw(forward);
			steer = forward;
			droveHas = false;
			droveHeadHas = false;
			rig.Teleport(val6, true);
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
			Vector3 val = item.inAt - Feet;
			float magnitude = ((Vector3)(ref val)).magnitude;
			if (magnitude < num && item.inOpen.y > -0.5f)
			{
				num = magnitude;
				portalWay = item;
			}
		}
		Lab.Event("portals: " + PortalWays.All.Count + " ways about" + ((num < float.MaxValue) ? (", nearest " + num.ToString("0.0") + " m, opens " + ((Vector3)(ref portalWay.inOpen)).ToString("0.00")) : ""));
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
		Strike strike = value[Random.Range(0, value.Count)];
		if (strike == lastStrike && value.Count > 1)
		{
			strike = value[(value.IndexOf(strike) + 1) % value.Count];
		}
		lastStrike = strike;
		punchRight = strike.right;
		strikePace = Mathf.Clamp(Tuning.F("punch.peak", 6.5f) / Mathf.Max(0.5f, strike.peak), 1f, Tuning.F("punch.pace.most", 2.5f));
		return strike.clip;
	}

	private static List<Strike> Split(Frame[] clip)
	{
		List<Strike> list = new List<Strike>();
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
			Vector3 val = clip[i].leftPos - clip[i - 1].leftPos;
			float num3 = ((Vector3)(ref val)).magnitude / num2;
			val = clip[i].rightPos - clip[i - 1].rightPos;
			float num4 = ((Vector3)(ref val)).magnitude / num2;
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
				List<Frame> list2 = new List<Frame>();
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
			Hand[] array = (Hand[])(object)new Hand[2]
			{
				Player.LeftHand,
				Player.RightHand
			};
			foreach (Hand val in array)
			{
				Rigidbody val2 = (((Object)(object)val != (Object)null) ? val.rb : null);
				if (!((Object)(object)val2 == (Object)null))
				{
					Vector3 val3 = head.position - val2.position;
					if (((Vector3)(ref val3)).magnitude < 1.1f && Vector3.Dot(val2.velocity, ((Vector3)(ref val3)).normalized) > Tuning.F("fist.read.speed", 2.5f))
					{
						flag = true;
					}
				}
			}
		}
		if (flag && Time.time > defendUntil && Time.time - defendSeenAt > 0.5f)
		{
			defendSeenAt = Time.time;
			if (Random.value < Tuning.F("fist.defend", 0.55f) * Mathf.Lerp(0.7f, 1.3f, personality.aim))
			{
				defendBlock = Random.value < Tuning.F("fist.block", 0.6f);
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
			walk = new Vector2(Random.Range(-0.3f, 0.3f), -1f) * MotionLibrary.WalkPush * Tuning.F("fist.step", 1.2f);
		}
		return true;
	}

	private bool FindTallWall(Vector3 feet, Vector3 dir, float within, out Vector3 foot, out Vector3 inward, out float height, out Vector3 top)
	{
		foot = (inward = (top = Vector3.zero));
		height = 0f;
		RaycastHit val = default(RaycastHit);
		if (!Physics.Raycast(feet + Vector3.up * 0.8f, dir, ref val, within, -1, (QueryTriggerInteraction)1))
		{
			return false;
		}
		if ((Object)(object)((Component)((RaycastHit)(ref val)).collider).GetComponentInParent<RigManager>() != (Object)null || (Object)(object)((RaycastHit)(ref val)).collider.attachedRigidbody != (Object)null || Mathf.Abs(((RaycastHit)(ref val)).normal.y) > 0.35f)
		{
			return false;
		}
		inward = -Flat(((RaycastHit)(ref val)).normal);
		if (((Vector3)(ref inward)).sqrMagnitude < 0.01f)
		{
			return false;
		}
		((Vector3)(ref inward)).Normalize();
		Vector3 val2 = ((RaycastHit)(ref val)).point + inward * 0.45f;
		RaycastHit val3 = default(RaycastHit);
		if (!Physics.Raycast(new Vector3(val2.x, feet.y + Tuning.F("tool.climb.most", 30f), val2.z), Vector3.down, ref val3, Tuning.F("tool.climb.most", 30f), -1, (QueryTriggerInteraction)1))
		{
			return false;
		}
		height = ((RaycastHit)(ref val3)).point.y - feet.y;
		if (((RaycastHit)(ref val3)).normal.y < 0.7f || height <= Tuning.F("climb.max", 2.5f) || (Object)(object)((RaycastHit)(ref val3)).collider.attachedRigidbody != (Object)null)
		{
			return false;
		}
		if (Physics.Raycast(((RaycastHit)(ref val3)).point + Vector3.up * 0.05f, Vector3.up, 1.6f, -1, (QueryTriggerInteraction)1))
		{
			return false;
		}
		if (!Physics.Raycast(new Vector3(((RaycastHit)(ref val)).point.x, ((RaycastHit)(ref val3)).point.y - 0.4f, ((RaycastHit)(ref val)).point.z) - inward * 0.6f, inward, 1.2f, -1, (QueryTriggerInteraction)1))
		{
			return false;
		}
		top = ((RaycastHit)(ref val3)).point;
		foot = new Vector3(((RaycastHit)(ref val)).point.x, feet.y, ((RaycastHit)(ref val)).point.z) - inward * Tuning.F("tool.climb.stand", 0.45f);
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
		float num = Random.Range(0f, 360f);
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
		if (InLesson || enemy || climbing || Playing || (Object)(object)head == (Object)null || Time.time - purposeAt < Tuning.F("tool.every", 14f) || Lessons.Count == 0)
		{
			return false;
		}
		if ((Object)(object)nearestSeen.transform != (Object)null && nearestSeenDistance < 4f && (Object)(object)gun == (Object)null)
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
		Vector3 val = (((Object)(object)rig.physicsRig != (Object)null && (Object)(object)((Rig)rig.physicsRig).centerOfPressure != (Object)null) ? ((Rig)rig.physicsRig).centerOfPressure.position : Feet);
		if (TallWallAbout(val, out var foot, out var inward, out var height, out var top))
		{
			string[] obj = new string[5]
			{
				"purpose: a wall ",
				height.ToString("0.0"),
				" m high, ",
				null,
				null
			};
			Vector3 val2 = Flat(foot - val);
			obj[3] = ((Vector3)(ref val2)).magnitude.ToString("0.0");
			obj[4] = " m off";
			Lab.Event(string.Concat(obj));
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
		if (!raceLineUp || dead || down || (Object)(object)head == (Object)null)
		{
			return false;
		}
		Vector3 val = Flat(raceSpot - head.position);
		float magnitude = ((Vector3)(ref val)).magnitude;
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
			Vector3 val = Flat(to - bodyAt.Value);
			driveFacing = ((Vector3)(ref val)).normalized;
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
			Quaternion val = Quaternion.Euler(0f, 0f - bodyYaw, 0f);
			Vector3 val2 = Flat(from - head.position);
			Vector3 val3 = val * ((Vector3)(ref val2)).normalized;
			flinchSide = Mathf.Clamp(val3.x, -1f, 1f);
			flinchSize = num2;
			flinchAt = Time.time + Random(0.06f, 0.13f) * personality.reaction;
			if (num2 > 0.45f && Random.value < Tuning.F("flinch.squat", 0.55f))
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
		if ((Object)(object)Head != (Object)null)
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
		if ((Object)(object)head == (Object)null)
		{
			return;
		}
		Fall(head);
		if ((Object)(object)you == (Object)null || enemy)
		{
			return;
		}
		Vector3 val = you.position - head.position;
		float magnitude = ((Vector3)(ref val)).magnitude;
		bool flag = HandsFree && (Object)(object)nearestSeen.transform == (Object)null && (Object)(object)brainTarget.transform == (Object)null && Time.time >= waveUntil && waveDue < 0f;
		if (Gestures.WavedAt > waveSeen)
		{
			waveSeen = Gestures.WavedAt;
			bool flag2 = magnitude < Tuning.F("wave.far", 25f) && Vector3.Angle(Flat(Gestures.WaveFacing), Flat(-val)) < Tuning.F("wave.cone", 35f);
			if (flag && flag2 && Targets.InSight(head.position, you.position, ((Object)(object)Player.RigManager != (Object)null) ? ((Component)Player.RigManager).transform : null, rig))
			{
				StartWave(Random.Range(2, 4), Random(0.35f, 0.8f) * personality.reaction);
				Lab.Event("waves back");
			}
		}
		bool flag3 = magnitude < 8f;
		if (flag3 && !nearYou && Time.time - lastNearYou > Tuning.F("wave.hello.after", 45f) && Time.time - born > 20f && flag && Random.value < Tuning.F("wave.hello.chance", 0.5f))
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
			if (mode == Mode.Wander || (mode == Mode.Brain && act != 0 && (Object)(object)nearestSeen.transform == (Object)null))
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
		if (fallHas && !(deltaTime <= 0f))
		{
			Vector3 val = position - fallLast;
			if (!(((Vector3)(ref val)).magnitude > 3f) && !climbing)
			{
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
				return;
			}
		}
		fallHas = true;
		fallLast = position;
		fallSpeed = 0f;
		falling = false;
		if (climbing)
		{
			noLandUntil = Time.time + 1f;
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
			Vector3 val = new Vector3(0f, 0.07f * num9, 0.1f * num9) * num;
			ref Vector3 leftPos = ref pose.leftPos;
			leftPos += val + new Vector3(0.05f * num9 * num, 0f, 0f);
			ref Vector3 rightPos = ref pose.rightPos;
			rightPos += val + new Vector3(-0.05f * num9 * num, 0f, 0f);
			curl = Mathf.Lerp(curl, 0.8f, num9);
		}
	}

	private void StartReload(bool rackOnly = false)
	{
		Lab.Event((rackOnly ? "racking: " : "reload starts: ") + (((Object)(object)gun != (Object)null) ? SafeState() : "?") + ", dry pulls " + dryPulls);
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
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector(-0.17f, standingHeight - 0.88f, 0.04f);
		if ((Object)(object)ammoSocket == (Object)null || (Object)(object)gun.defaultMagazine == (Object)null)
		{
			leftAt = Vector3.Lerp(val, new Vector3(0.05f, standingHeight - 0.55f, 0.32f), Mathf.PingPong(num * 2f, 1f));
			leftTurn = Quaternion.Euler(60f, 20f, 0f);
			if (num > 1f)
			{
				FinishReload(quick: true);
			}
			return;
		}
		if (num > 3.5f && reload != Reload.Rack)
		{
			MelonLogger.Msg("Bot Player: the reload of " + ((Object)gun).name + " did not go in by hand (" + reload.ToString() + "); loading it");
			DropNewMag();
			FinishReload(quick: true);
			return;
		}
		switch (reload)
		{
		case Reload.Eject:
			leftAt = controls.vrRoot.InverseTransformPoint(((Component)ammoSocket).transform.position) + new Vector3(-0.05f, -0.06f, 0f);
			leftTurn = Quaternion.Euler(70f, 30f, 0f);
			if (!ejected && num > 0.25f)
			{
				ejected = true;
				try
				{
					if (gun.HasMagazine())
					{
						Magazine val2 = (((Object)(object)ammoSocket._magazinePlug != (Object)null) ? ammoSocket._magazinePlug.magazine : null);
						ammoSocket.EjectMagazine();
						if ((Object)(object)val2 != (Object)null)
						{
							dropped.Add((val2, Time.time));
						}
					}
				}
				catch (Exception ex)
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
			leftAt = val;
			leftTurn = Quaternion.Euler(80f, 0f, 0f);
			Hand leftHand = LeftHand;
			if (!magAsked && num > 0.3f && (Object)(object)leftHand != (Object)null)
			{
				magAsked = true;
				SpawnMag(((Component)leftHand).transform.position, ((Component)leftHand).transform.rotation);
			}
			if ((Object)(object)newMag == (Object)null && magAsked && Time.time - magAskedAt > 0.15f)
			{
				newMag = SpawnedMag();
			}
			if ((Object)(object)newMag != (Object)null && (Object)(object)newMag.grip != (Object)null && (Object)(object)leftHand != (Object)null)
			{
				if ((Object)(object)leftHand.m_CurrentAttachedGO == (Object)null && !Grab(leftHand, newMag.grip))
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
			SpawnableCrateReference val = ((gun.defaultMagazine.spawnable != null) ? gun.defaultMagazine.spawnable.crateRef : null);
			if (val == null)
			{
				FinishReload(quick: true);
				return;
			}
			magAt = at;
			magAskedAt = Time.time;
			magsBefore.Clear();
			foreach (Collider item in Physics.OverlapSphere(at, 1f, -1, (QueryTriggerInteraction)1))
			{
				Magazine val2 = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Magazine>() : null);
				if ((Object)(object)val2 != (Object)null)
				{
					magsBefore.Add(((Il2CppObjectBase)(object)val2).Pointer);
				}
			}
			Clone.Suppress();
			HelperMethods.SpawnCrate(val, at, turn, Vector3.one, false, (Action<GameObject>)null, (Action<GameObject>)null);
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Bot Player: no magazine for " + ((Object)gun).name + ": " + ex.Message);
			FinishReload(quick: true);
		}
	}

	private Magazine SpawnedMag()
	{
		foreach (Collider item in Physics.OverlapSphere(magAt, 1f, -1, (QueryTriggerInteraction)1))
		{
			Magazine val = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Magazine>() : null);
			if (!((Object)(object)val == (Object)null) && !magsBefore.Contains(((Il2CppObjectBase)(object)val).Pointer) && !val.isMagazineInserted && (!((Object)(object)val.grip != (Object)null) || !val.grip.HasAttachedHands()))
			{
				try
				{
					val.Initialize(gun.defaultCartridge, gun.defaultMagazine.rounds);
				}
				catch (Exception)
				{
				}
				return val;
			}
		}
		return null;
	}

	public static void ClearDropped()
	{
		for (int num = dropped.Count - 1; num >= 0; num--)
		{
			var (val, num2) = dropped[num];
			if ((Object)(object)val == (Object)null)
			{
				dropped.RemoveAt(num);
			}
			else if (!(Time.time - num2 < 20f))
			{
				dropped.RemoveAt(num);
				try
				{
					if (!val.isMagazineInserted && (!((Object)(object)val.grip != (Object)null) || !val.grip.HasAttachedHands()))
					{
						Poolee componentInParent = ((Component)val).GetComponentInParent<Poolee>();
						if ((Object)(object)componentInParent != (Object)null)
						{
							componentInParent.Despawn();
						}
						else
						{
							Object.Destroy((Object)(object)((Component)val).gameObject);
						}
					}
				}
				catch (Exception)
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
		if ((Object)(object)newMag == (Object)null || (Object)(object)leftHand == (Object)null || (Object)(object)newMag.magazinePlug == (Object)null)
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
		Transform transform = ((Component)newMag.magazinePlug).transform;
		Transform transform2 = ((Component)socket).transform;
		Vector3 val;
		Vector3 val2;
		if ((Object)(object)((Socket)socket).endTransform != (Object)null)
		{
			val = ((Socket)socket).endTransform.position - transform2.position;
			if (((Vector3)(ref val)).sqrMagnitude > 1E-06f)
			{
				val = ((Socket)socket).endTransform.position - transform2.position;
				val2 = ((Vector3)(ref val)).normalized;
				goto IL_00de;
			}
		}
		val2 = -transform2.up;
		goto IL_00de;
		IL_00de:
		Vector3 val3 = val2;
		float num = Mathf.SmoothStep(0f, 1f, since / 0.7f);
		Pose obj = new Pose(transform2.position - val3 * Mathf.Lerp(0.12f, -0.01f, num), transform2.rotation);
		Transform transform3 = ((Component)newMag).transform;
		Pose pose = Pose.Of(transform3).Inverse() * Pose.Of(transform);
		Pose pose2 = Pose.Of(((Component)leftHand).transform).Inverse() * Pose.Of(transform3);
		Pose obj2 = obj * pose.Inverse() * pose2.Inverse();
		Pose pose3 = Pose.Of(((Component)((ControllerRig)controls).leftController).transform).Inverse() * Pose.Of(((Component)leftHand).transform);
		leftOnController = (leftMeasured ? Pose.Lerp(leftOnController, pose3, 0.1f) : pose3);
		leftMeasured = true;
		Pose pose4 = obj2 * leftOnController.Inverse();
		leftAt = controls.vrRoot.InverseTransformPoint(pose4.position);
		leftTurn = Quaternion.Inverse(Quaternion.Euler(0f, bodyYaw, 0f)) * pose4.rotation;
		val = transform.position - transform2.position;
		if (((Vector3)(ref val)).magnitude < 0.06f && since > 0.4f)
		{
			try
			{
				((AlignPlug)newMag.magazinePlug).InsertPlug((Socket)(object)socket);
			}
			catch (Exception)
			{
			}
		}
		if (since > 2.2f && !gun.HasMagazine())
		{
			try
			{
				((AlignPlug)newMag.magazinePlug).ForceInSocket((Socket)(object)socket);
			}
			catch (Exception)
			{
			}
		}
	}

	private void RackSlide(float since)
	{
		Hand leftHand = LeftHand;
		if ((int)gun.cartridgeState == 1 && !slideHeld)
		{
			FinishReload(quick: false);
			return;
		}
		if ((Object)(object)slide == (Object)null)
		{
			slide = SlideGrip(gun);
		}
		if ((Object)(object)slide == (Object)null || (Object)(object)leftHand == (Object)null)
		{
			if (since > 0.3f)
			{
				TryCharge();
				FinishReload(quick: false);
			}
			return;
		}
		Vector3 val = (((Object)(object)gun.firePointTransform != (Object)null) ? (-gun.firePointTransform.forward) : (Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.back));
		if (!slideHeld)
		{
			leftAt = controls.vrRoot.InverseTransformPoint(((Component)slide).transform.position);
			leftTurn = Quaternion.Inverse(Quaternion.Euler(0f, bodyYaw, 0f)) * ((Component)slide).transform.rotation;
			if (since > 0.3f)
			{
				slideHeld = Grab(leftHand, slide);
				slideFrom = ((Component)slide).transform.position;
				reloadAt = Time.time;
				if (!slideHeld)
				{
					TryCharge();
					FinishReload(quick: false);
				}
			}
			return;
		}
		leftAt = controls.vrRoot.InverseTransformPoint(slideFrom + val * Mathf.Clamp01(since / 0.15f) * 0.1f);
		if (since > 0.22f)
		{
			LetGoOf(slide, leftHand);
			slideHeld = false;
			if ((int)gun.cartridgeState != 1)
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
			if (!((Object)(object)otherGrip == (Object)null) && ((Component)otherGrip).gameObject.activeInHierarchy)
			{
				string text = ((Object)otherGrip).name.ToLowerInvariant();
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
			if ((int)gun.slideState == 4)
			{
				gun.SlideRelease();
			}
			if ((int)gun.cartridgeState != 1)
			{
				gun.Charge();
			}
		}
		catch (Exception)
		{
		}
	}

	private void FinishReload(bool quick)
	{
		LabReloads++;
		if (!quick && (Object)(object)gun != (Object)null)
		{
			try
			{
				if (!gun.HasMagazine() || (gun.AmmoCount() <= 0 && (int)gun.cartridgeState != 1))
				{
					quick = true;
				}
			}
			catch (Exception)
			{
			}
		}
		reloadedAt = Time.time;
		if ((Object)(object)gun != (Object)null)
		{
			NoPushOnItself(((Component)gun).transform, again: true);
		}
		if (!quick && (Object)(object)gun != (Object)null)
		{
			try
			{
				int num = (((Object)(object)gun.defaultMagazine != (Object)null) ? gun.defaultMagazine.rounds : 0);
				if (gun.HasMagazine() && gun.MagazineState != null && num > 0 && gun.MagazineState.AmmoCount < num - 1)
				{
					Lab.Event("reload: the magazine in it had " + gun.MagazineState.AmmoCount + " of " + num + ": filled");
					gun.MagazineState.Refill();
				}
			}
			catch (Exception)
			{
			}
		}
		Lab.Event("chamber after: " + SafeState());
		Lab.Event("reload done " + (quick ? "QUICK (fallback)" : "by hand") + " at stage " + reload);
		if (quick && (Object)(object)gun != (Object)null)
		{
			Load(reload: true);
			loadSettleUntil = Time.time + Tuning.F("gun.settle", 1.5f);
		}
		else if ((Object)(object)gun != (Object)null)
		{
			MelonLogger.Msg("Bot Player: reloaded " + ((Object)gun).name + " by hand (" + SafeAmmo() + " rounds)");
		}
		Hand leftHand = LeftHand;
		if (slideHeld && (Object)(object)slide != (Object)null && (Object)(object)leftHand != (Object)null)
		{
			LetGoOf(slide, leftHand);
		}
		slideHeld = false;
		reload = Reload.None;
		leftOverride = false;
		supportTries = 0;
		supportTriedAt = -10f;
		if ((Object)(object)gun != (Object)null)
		{
			step = (((Object)(object)support != (Object)null) ? Step.Reach : Step.Ready);
			stepAt = Time.time;
			if ((Object)(object)((ControllerRig)controls).leftController != (Object)null)
			{
				leftStart = Pose.Of(((Component)((ControllerRig)controls).leftController).transform);
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
		catch (Exception)
		{
			return -1;
		}
	}

	private void DropNewMag()
	{
		Hand leftHand = LeftHand;
		if ((Object)(object)newMag != (Object)null && (Object)(object)newMag.grip != (Object)null && (Object)(object)leftHand != (Object)null)
		{
			LetGoOf(newMag.grip, leftHand);
		}
		if ((Object)(object)newMag != (Object)null)
		{
			dropped.Add((newMag, Time.time));
		}
		newMag = null;
	}

	private static void FreeHand(Hand hand)
	{
		try
		{
			GameObject val = (((Object)(object)hand != (Object)null) ? hand.m_CurrentAttachedGO : null);
			if ((Object)(object)val == (Object)null)
			{
				return;
			}
			MarrowEntity componentInParent = val.GetComponentInParent<MarrowEntity>();
			Transform val2 = (((Object)(object)componentInParent != (Object)null) ? ((Component)componentInParent).transform : val.transform.root);
			if ((Object)(object)((Component)val2).GetComponent<RigManager>() == (Object)null)
			{
				foreach (Grip componentsInChild in ((Component)val2).GetComponentsInChildren<Grip>(true))
				{
					LetGoOf(componentsInChild, hand);
				}
			}
			if (!((Object)(object)hand.m_CurrentAttachedGO == (Object)null))
			{
				Grip val3 = val.GetComponent<Grip>() ?? val.GetComponentInParent<Grip>();
				if ((Object)(object)val3 != (Object)null)
				{
					val3.ForceDetach(hand);
				}
				if ((Object)(object)hand.m_CurrentAttachedGO != (Object)null)
				{
					hand.DetachObject();
				}
			}
		}
		catch (Exception ex)
		{
			Lab.Event("hand could not be emptied: " + ex.Message);
		}
	}

	private static void LetGoOf(Grip g, Hand hand)
	{
		try
		{
			if ((Object)(object)g != (Object)null && (Object)(object)hand != (Object)null && g.attachedHands != null && g.attachedHands.Contains(hand))
			{
				g.ForceDetach(hand);
			}
		}
		catch (Exception)
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
		Rigidbody val = Pelvis();
		Transform head = Player.Head;
		if ((Object)(object)val != (Object)null && (Object)(object)head != (Object)null)
		{
			Vector3 val2 = head.position - val.position;
			if (((Vector3)(ref val2)).magnitude < Tuning.F("rescue.you.near", 1.6f))
			{
				if (youOverSince < 0f)
				{
					youOverSince = Time.time;
				}
				else if (Time.time - youOverSince > Tuning.F("rescue.time", 3f))
				{
					Revive("you");
				}
				goto IL_00ce;
			}
		}
		youOverSince = -1f;
		goto IL_00ce;
		IL_00ce:
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
			Rigidbody val = item.Pelvis();
			if (!((Object)(object)val == (Object)null))
			{
				Vector3 val2 = Flat(val.position - from);
				float magnitude = ((Vector3)(ref val2)).magnitude;
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
		if (enemy || dead || down || surrendered || climbing || Puppet || (Object)(object)head == (Object)null || (mode != Mode.Brain && mode != Mode.Follow && mode != Mode.Wander))
		{
			StopRescue(null);
			return false;
		}
		if ((Object)(object)target.transform != (Object)null || Reloading)
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
		Rigidbody val = rescuing.Pelvis();
		if ((Object)(object)val == (Object)null || Time.time - rescueSince > Tuning.F("rescue.most", 25f))
		{
			StopRescue("could not get there");
			return false;
		}
		Vector3 val2 = Flat(val.position - head.position);
		float magnitude = ((Vector3)(ref val2)).magnitude;
		if (kneelSince < 0f && magnitude > Tuning.F("rescue.reach", 1.1f))
		{
			if (!nav.HasGoal)
			{
				nav.SetGoal(val.position);
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
		Face(val.position, dt, 180f);
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
		Rigidbody val = rescuing.Pelvis();
		Transform vrRoot = controls.vrRoot;
		if (!((Object)(object)val == (Object)null) && !((Object)(object)vrRoot == (Object)null))
		{
			float height = standingHeight - Tuning.F("rescue.kneel.drop", 1.15f);
			pose.height = height;
			Transform val2 = null;
			try
			{
				val2 = ((Rig)rescuing.rig.physicsRig).m_chest;
			}
			catch (Exception)
			{
			}
			Vector3 val3 = vrRoot.InverseTransformPoint((((Object)(object)val2 != (Object)null) ? val2.position : val.position) + Vector3.up * 0.1f);
			float num = Mathf.Abs(Mathf.Sin((Time.time - kneelSince) * 6f)) * 0.06f;
			pose.leftPos = val3 + new Vector3(-0.1f, num, 0f);
			pose.rightPos = val3 + new Vector3(0.1f, num, 0f);
			pose.leftRot = Quaternion.Euler(90f, 0f, 0f);
			pose.rightRot = Quaternion.Euler(90f, 0f, 0f);
		}
	}

	private void IgnoreCar(Transform car, bool ignore)
	{
		try
		{
			if ((Object)(object)car == (Object)null || (Object)(object)rig.physicsRig == (Object)null)
			{
				return;
			}
			Collider[]? array = ((Component)car).GetComponentsInChildren<Collider>(true);
			Collider[] array2 = ((Component)rig.physicsRig).GetComponentsInChildren<Collider>(true);
			Collider[] array3 = array;
			foreach (Collider val in array3)
			{
				if ((Object)(object)val == (Object)null || val.isTrigger)
				{
					continue;
				}
				Collider[] array4 = array2;
				foreach (Collider val2 in array4)
				{
					if ((Object)(object)val2 != (Object)null && !val2.isTrigger)
					{
						Physics.IgnoreCollision(val, val2, ignore);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Lab.Event("ride: car collisions not " + (ignore ? "ignored" : "back") + ": " + ex.Message);
		}
	}

	public static void LabSpawnCrate(string barcode)
	{
		Transform head = Player.Head;
		if (!((Object)(object)head == (Object)null))
		{
			Vector3 val = Flat(head.forward);
			Vector3 normalized = ((Vector3)(ref val)).normalized;
			Vector3 val2 = head.position + normalized * 6f;
			RaycastHit val3 = default(RaycastHit);
			if (Physics.Raycast(val2 + Vector3.up * 3f, Vector3.down, ref val3, 10f, -1, (QueryTriggerInteraction)1))
			{
				val2 = ((RaycastHit)(ref val3)).point + Vector3.up * 0.5f;
			}
			Lab.Event("crate: " + barcode + " put 6 m in front of you");
			HelperMethods.SpawnCrate(barcode, val2, Quaternion.LookRotation(Vector3.Cross(normalized, Vector3.up)), Vector3.one, false, (Action<GameObject>)null, (Action<GameObject>)null);
		}
	}

	public static void LabSeats()
	{
		Transform head = Player.Head;
		Dictionary<IntPtr, List<Seat>> dictionary = new Dictionary<IntPtr, List<Seat>>();
		Dictionary<IntPtr, string> dictionary2 = new Dictionary<IntPtr, string>();
		foreach (Seat item in Object.FindObjectsOfType<Seat>())
		{
			if ((Object)(object)item == (Object)null || (Object)(object)head == (Object)null)
			{
				continue;
			}
			Vector3 val = ((Component)item).transform.position - head.position;
			if (!(((Vector3)(ref val)).magnitude > 30f))
			{
				Poolee componentInParent = ((Component)item).GetComponentInParent<Poolee>();
				IntPtr key = (((Object)(object)componentInParent != (Object)null) ? ((Il2CppObjectBase)(object)componentInParent).Pointer : ((Il2CppObjectBase)(object)((Component)item).transform.root).Pointer);
				if (!dictionary.ContainsKey(key))
				{
					dictionary[key] = new List<Seat>();
					dictionary2[key] = (((Object)(object)componentInParent != (Object)null) ? ((Object)componentInParent).name : ((Object)((Component)item).transform.root).name);
				}
				dictionary[key].Add(item);
			}
		}
		foreach (KeyValuePair<IntPtr, List<Seat>> item2 in dictionary)
		{
			string text = "";
			foreach (Seat item3 in item2.Value)
			{
				text = text + ((Object)item3).name + (((Object)(object)item3.rigManager != (Object)null) ? " (taken)" : "") + "; ";
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
		foreach (Seat item in Object.FindObjectsOfType<Seat>())
		{
			if (!((Object)(object)item == (Object)null) && !((Object)(object)head == (Object)null) && !((Object)(object)item.rigManager != (Object)null) && ((Component)item).gameObject.activeInHierarchy && (driverToo || !((Object)item).name.ToLowerInvariant().Contains("driver")))
			{
				Vector3 val = ((Component)item).transform.position - head.position;
				float magnitude = ((Vector3)(ref val)).magnitude;
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
		if ((Object)(object)rig.activeSeat != (Object)null)
		{
			Lab.Event("ride: sat in " + ((Object)rig.activeSeat).name + " already");
			return;
		}
		Seat val = FreeSeat(Tuning.F("ride.seek", 15f), driverToo: true);
		if ((Object)(object)val == (Object)null)
		{
			Lab.Event("ride: no free seat near");
			return;
		}
		float num;
		if (!((Object)(object)Head != (Object)null))
		{
			num = -1f;
		}
		else
		{
			Vector3 val2 = ((Component)val).transform.position - Head.position;
			num = ((Vector3)(ref val2)).magnitude;
		}
		float num2 = num;
		try
		{
			StopPlaying();
			nav.Clear();
			Poolee componentInParent = ((Component)val).GetComponentInParent<Poolee>();
			ridingCar = (((Object)(object)componentInParent != (Object)null) ? ((Component)componentInParent).transform : ((Component)val).transform.root);
			IgnoreCar(ridingCar, ignore: true);
			val.IngressRig(rig);
			riding = val;
			Lab.Event("ride: into " + ((Object)val).name + " of " + (((Object)(object)((Component)val).GetComponentInParent<Poolee>() != (Object)null) ? ((Object)((Component)val).GetComponentInParent<Poolee>()).name : "?") + " from " + num2.ToString("0.0") + " m off");
		}
		catch (Exception ex)
		{
			Lab.Event("ride: the way in failed: " + ex.Message);
		}
	}

	public void LabRideSay(string when)
	{
		Rigidbody val = Pelvis();
		Seat activeSeat = rig.activeSeat;
		string[] obj = new string[7]
		{
			"ride (",
			when,
			"): seat ",
			((Object)(object)activeSeat != (Object)null) ? ((Object)activeSeat).name : "none",
			", state ",
			null,
			null
		};
		BodyState bodyState = rig.bodyState;
		obj[5] = ((object)(BodyState)(ref bodyState)).ToString();
		object obj2;
		if (!((Object)(object)riding != (Object)null) || !((Object)(object)val != (Object)null))
		{
			obj2 = "";
		}
		else
		{
			Vector3 val2 = val.position - ((Component)riding).transform.position;
			obj2 = ", hips " + ((Vector3)(ref val2)).magnitude.ToString("0.00") + " m from the seat";
		}
		obj[6] = (string)obj2;
		Lab.Event(string.Concat(obj));
	}

	public void LabUnride()
	{
		try
		{
			if ((Object)(object)rig.activeSeat != (Object)null)
			{
				rig.activeSeat.EgressRig(true);
			}
			IgnoreCar(ridingCar, ignore: false);
			ridingCar = null;
			Lab.Event("ride: out");
		}
		catch (Exception ex)
		{
			Lab.Event("ride: the way out failed: " + ex.Message);
		}
		riding = null;
	}

	private static Transform CarOf(Seat seat)
	{
		if ((Object)(object)seat == (Object)null)
		{
			return null;
		}
		Poolee componentInParent = ((Component)seat).GetComponentInParent<Poolee>();
		if (!((Object)(object)componentInParent != (Object)null))
		{
			return ((Component)seat).transform.root;
		}
		return ((Component)componentInParent).transform;
	}

	private static bool HasDriver(Transform car)
	{
		if ((Object)(object)car == (Object)null)
		{
			return false;
		}
		Seat val = (((Object)(object)Player.RigManager != (Object)null) ? Player.RigManager.activeSeat : null);
		if ((Object)(object)val != (Object)null && (Object)(object)CarOf(val) == (Object)(object)car)
		{
			return true;
		}
		foreach (Bot item in BotManager.All)
		{
			Seat activeSeat = item.rig.activeSeat;
			if ((Object)(object)activeSeat != (Object)null && (item.DrivingNow || item.labHost) && (Object)(object)CarOf(activeSeat) == (Object)(object)car)
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
			if ((Object)(object)item.boardingCar == (Object)(object)car && (Object)(object)item.boardingSeat != (Object)null)
			{
				num++;
			}
		}
		return num;
	}

	private static float CarSpeed(Transform car)
	{
		Rigidbody val = (((Object)(object)car != (Object)null) ? ((Component)car).GetComponentInChildren<Rigidbody>() : null);
		if (!((Object)(object)val != (Object)null))
		{
			return 0f;
		}
		Vector3 velocity = val.velocity;
		return ((Vector3)(ref velocity)).magnitude;
	}

	private Seat SeatIn(Transform car)
	{
		Transform head = Head;
		Seat result = null;
		float num = float.MaxValue;
		foreach (Seat componentsInChild in ((Component)car).GetComponentsInChildren<Seat>(false))
		{
			if ((Object)(object)componentsInChild == (Object)null || (Object)(object)componentsInChild.rigManager != (Object)null)
			{
				continue;
			}
			bool flag = false;
			foreach (Bot item in BotManager.All)
			{
				flag |= item != this && (Object)(object)item.boardingSeat == (Object)(object)componentsInChild;
			}
			if (!flag)
			{
				float num2;
				if (!((Object)(object)head != (Object)null))
				{
					num2 = 0f;
				}
				else
				{
					Vector3 val = ((Component)componentsInChild).transform.position - head.position;
					num2 = ((Vector3)(ref val)).magnitude;
				}
				float num3 = num2;
				if (num3 < num)
				{
					num = num3;
					result = componentsInChild;
				}
			}
		}
		return result;
	}

	private bool FreeToBoard()
	{
		if (!enemy && !dead && !down && !surrendered && !Puppet && !climbing && !InLesson && !raceLineUp && rescuing == null && (Object)(object)target.transform == (Object)null && !Reloading && (mode == Mode.Brain || mode == Mode.Follow || mode == Mode.Wander))
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
		List<Transform> list = new List<Transform>();
		Seat val = (((Object)(object)Player.RigManager != (Object)null) ? Player.RigManager.activeSeat : null);
		if ((Object)(object)val != (Object)null)
		{
			list.Add(CarOf(val));
		}
		foreach (Bot item in BotManager.All)
		{
			if (item != this && (item.DrivingNow || item.labHost) && (Object)(object)item.rig.activeSeat != (Object)null && !item.enemy)
			{
				list.Add(CarOf(item.rig.activeSeat));
			}
		}
		foreach (Transform item2 in list)
		{
			if ((Object)(object)item2 == (Object)null || CarSpeed(item2) > Tuning.F("ride.board.speed", 1.5f))
			{
				continue;
			}
			bool flag = (Object)(object)val != (Object)null && (Object)(object)CarOf(val) == (Object)(object)item2;
			Vector3 val2 = Flat(item2.position - head.position);
			float magnitude = ((Vector3)(ref val2)).magnitude;
			if (!(magnitude > (flag ? Tuning.F("ride.you.range", 30f) : Tuning.F("ride.bot.range", 15f))) && !(magnitude >= num))
			{
				Seat val3 = SeatIn(item2);
				if (!((Object)(object)val3 == (Object)null))
				{
					num = magnitude;
					car = item2;
					seat = val3;
				}
			}
		}
		return (Object)(object)car != (Object)null;
	}

	private void GetOut(string why)
	{
		Lab.Event("ride: getting out (" + why + ")");
		try
		{
			if ((Object)(object)rig.activeSeat != (Object)null)
			{
				rig.activeSeat.EgressRig(true);
			}
		}
		catch (Exception ex)
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
		if ((Object)(object)lessonCar != (Object)null && (Object)(object)rig.activeSeat == (Object)null && !InLesson)
		{
			IgnoreCar(lessonCar, ignore: false);
			lessonCar = null;
		}
		if ((Object)(object)head == (Object)null || dead || down)
		{
			return false;
		}
		if ((Object)(object)riding != (Object)null)
		{
			if ((Object)(object)rig.activeSeat == (Object)null)
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
		if ((Object)(object)boardingSeat != (Object)null)
		{
			bool flag = (Object)(object)boardingSeat.rigManager == (Object)null && HasDriver(boardingCar) && CarSpeed(boardingCar) < Tuning.F("ride.board.speed", 1.5f) + 1f;
			if (!flag || !FreeToBoard() || Time.time - boardingSince > Tuning.F("ride.board.most", 20f))
			{
				Lab.Event("ride: not getting in after all (" + ((!flag) ? "the seat or the car went" : ((!FreeToBoard()) ? "something else came up" : "too long")) + ")");
				boardingSeat = null;
				boardingCar = null;
				return false;
			}
			Vector3 val = Flat(((Component)boardingSeat).transform.position - head.position);
			float magnitude = ((Vector3)(ref val)).magnitude;
			if (magnitude > Tuning.F("ride.in", 2.5f))
			{
				nav.SetGoal(((Component)boardingSeat).transform.position);
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
				if ((Object)(object)gun == (Object)null)
				{
					StopPlaying();
				}
				ridingCar = boardingCar;
				IgnoreCar(ridingCar, ignore: true);
				IgnoreGunCar(ridingCar, ignore: true);
				boardingSeat.IngressRig(rig);
				riding = boardingSeat;
				noDriverSince = -1f;
				Lab.Event("ride: got in (" + ((Object)boardingSeat).name + ")" + (((Object)(object)gun != (Object)null) ? (", its " + ((Object)gun).name + " in hand") : ", nothing in hand"));
			}
			catch (Exception ex)
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
		bool flag2 = (Object)(object)Player.RigManager != (Object)null && (Object)(object)Player.RigManager.activeSeat != (Object)null && (Object)(object)CarOf(Player.RigManager.activeSeat) == (Object)(object)car;
		Lab.Event("ride: going to get in with " + (flag2 ? "you" : "a bot driving") + " (" + ((Object)seat).name + ")");
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
		if ((Object)(object)head == (Object)null)
		{
			return false;
		}
		Vector3 val = feet + Vector3.up * 1.3f;
		List<(Vector3, Vector3, Sight, string, float)> list = new List<(Vector3, Vector3, Sight, string, float)>();
		Vector3 val2 = ((((Vector3)(ref roamHeading)).sqrMagnitude > 0.01f) ? roamHeading : (Quaternion.Euler(0f, bodyYaw, 0f) * Vector3.forward));
		float num = Random.Range(0f, 22.5f);
		RaycastHit val4 = default(RaycastHit);
		for (int i = 0; i < 16; i++)
		{
			Vector3 val3 = Quaternion.Euler(0f, num + (float)i * 22.5f, 0f) * Vector3.forward;
			float num2 = (Physics.Raycast(val, val3, ref val4, 45f, -1, (QueryTriggerInteraction)1) ? ((RaycastHit)(ref val4)).distance : 45f);
			if (!(num2 < 9f) && Ground(val + val3 * (num2 * Random.Range(0.5f, 0.85f)), out var ground) && !(Mathf.Abs(ground.y - feet.y) > 4f))
			{
				list.Add((ground, ground + val3 * 6f + Vector3.up * 1.4f, Sight.Open, "seeing what is over there", 0.9f + num2 / 45f));
			}
		}
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		int num3 = 0;
		Vector3 val6;
		foreach (Collider item2 in Physics.OverlapSphere(val, Tuning.F("roam.thing.far", 40f), -1, (QueryTriggerInteraction)1))
		{
			Rigidbody val5 = (((Object)(object)item2 != (Object)null) ? item2.attachedRigidbody : null);
			if ((Object)(object)val5 == (Object)null || val5.isKinematic || !hashSet.Add(((Il2CppObjectBase)(object)val5).Pointer) || val5.mass > 60f || (Object)(object)((Component)val5).GetComponentInParent<RigManager>() != (Object)null)
			{
				continue;
			}
			string text = ((Object)val5).name.ToLowerInvariant();
			if (val5.mass < 0.25f || text.Contains("cartridge") || text.Contains("casing") || text.Contains("shell") || text.Contains("bullet") || text.StartsWith("mag_") || NotForBots(val5))
			{
				continue;
			}
			Vector3 worldCenterOfMass = val5.worldCenterOfMass;
			val6 = worldCenterOfMass - feet;
			if (((Vector3)(ref val6)).magnitude < 3f || Mathf.Abs(worldCenterOfMass.y - feet.y) > 2.5f || Targets.NearPlayer(worldCenterOfMass, 2f))
			{
				continue;
			}
			val6 = Flat(worldCenterOfMass - feet);
			if (Ground(worldCenterOfMass - ((Vector3)(ref val6)).normalized * 1.1f, out var ground2))
			{
				list.Add((ground2, worldCenterOfMass, Sight.Thing, "having a look at the " + ThingName(val5), Tuning.F("roam.thing", 2.6f)));
				if (++num3 >= 6)
				{
					break;
				}
			}
		}
		foreach (Bot item3 in BotManager.All)
		{
			if (item3 == this || (Object)(object)item3.Head == (Object)null || item3.dead || item3.enemy != enemy)
			{
				continue;
			}
			Vector3 v = item3.Head.position - head.position;
			if (!(((Vector3)(ref v)).magnitude < 5f) && !(((Vector3)(ref v)).magnitude > 45f))
			{
				Vector3 position = item3.Head.position;
				val6 = Flat(v);
				if (Ground(position - ((Vector3)(ref val6)).normalized * 2f, out var ground3))
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
			Poolee val7 = ((lesson != null && !BotManager.OwnSpawns) ? AnyVehicle(Tuning.F("drive.seek", 60f)) : null);
			Vector3 place2;
			if (lesson != null && !BotManager.OwnSpawns)
			{
				if ((Object)(object)val7 != (Object)null)
				{
					Vector3 position2 = ((Component)val7).transform.position;
					val6 = Flat(((Component)val7).transform.right);
					if (Ground(position2 - ((Vector3)(ref val6)).normalized * 1.3f + Vector3.up, out var ground4))
					{
						list.Add((ground4, ((Component)val7).transform.position + Vector3.up * 0.6f, Sight.Drive, "going for a drive in the " + lesson.name + " over there", Tuning.F("drive.want.map", 2.6f) * personality.curiosity));
					}
				}
			}
			else if (lesson != null && FarPlace(feet, out place2))
			{
				val6 = Flat(place2 - feet);
				if (((Vector3)(ref val6)).magnitude > 18f)
				{
					list.Add((place2, place2 + Vector3.up * 1.4f, Sight.Drive, DriveSkill.Practice ? "practising its driving" : ("going for a drive in the " + lesson.name), DriveSkill.Practice ? 1000f : (Tuning.F("drive.want", 1.3f) * personality.energy)));
				}
			}
		}
		if ((Object)(object)you != (Object)null)
		{
			val6 = Flat(you.position - feet);
			if (((Vector3)(ref val6)).magnitude > 6f)
			{
				val6 = Flat(you.position - feet);
				if (((Vector3)(ref val6)).magnitude < 40f)
				{
					Vector3 val8 = you.position - Vector3.up * 1.4f;
					val6 = Flat(you.position - feet);
					if (Ground(val8 - ((Vector3)(ref val6)).normalized * 2.5f, out var ground5))
					{
						list.Add((ground5, you.position, Sight.You, (YouStoodFor > Tuning.F("you.still", 50f)) ? "coming over, you have been there a while" : "coming to see what you are doing", BotManager.StayNear ? 1.6f : ((YouStoodFor > Tuning.F("you.still", 50f) && Time.time - cameOverAt > Tuning.F("you.still.every", 150f)) ? 1.4f : 0.2f)));
					}
				}
			}
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
			Vector3 val9 = Flat(tuple.Item1 - feet);
			float item = tuple.Item5;
			item *= (visited.Contains(Cell(tuple.Item1)) ? 0.6f : 1.5f);
			item *= 1f + 0.55f * Vector3.Dot(((Vector3)(ref val2)).normalized, ((Vector3)(ref val9)).normalized);
			item /= 1f + (float)SelfTaught.BadAt(tuple.Item1);
			if (tuple.Item3 == lastSight && tuple.Item3 != 0)
			{
				item *= 0.45f;
			}
			item *= Random.Range(0.7f, 1.3f);
			array[j] = item * item;
			num4 += array[j];
		}
		float num5 = Random.value * num4;
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
		val6 = Flat(planPlace - feet);
		float magnitude = ((Vector3)(ref val6)).magnitude;
		planRun = magnitude > 22f && Random.value < 0.18f * personality.energy;
		planPush = ((magnitude < 9f) ? (Tuning.F("life.stroll", 0.45f) * Random.Range(0.9f, 1.3f)) : (MotionLibrary.WalkPush * Random.Range(0.8f, 1f)));
		detours = 0;
		onDetour = false;
		viaLeft = false;
		if (magnitude > 9f && !Physics.Linecast(val, planPlace + Vector3.up * 1.2f, -1, (QueryTriggerInteraction)1))
		{
			Vector3 up = Vector3.up;
			val6 = Flat(planPlace - feet);
			Vector3 val10 = Vector3.Cross(up, ((Vector3)(ref val6)).normalized) * (magnitude * Random.Range(-1f, 1f) * Tuning.F("roam.bend", 0.13f));
			Vector3 p = feet + Flat(planPlace - feet) * Random.Range(0.4f, 0.6f) + val10;
			if (((Vector3)(ref val10)).magnitude > 1.2f && Ground(p, out var ground6) && Mathf.Abs(ground6.y - feet.y) < 1.5f && !Physics.Linecast(val, ground6 + Vector3.up * 1.2f, -1, (QueryTriggerInteraction)1))
			{
				viaPlace = ground6;
				viaLeft = true;
			}
		}
		thinking2 = true;
		thinkUntil = Time.time + Random.Range(0.9f, 2.4f) * Mathf.Lerp(0.8f, 1.3f, Mathf.InverseLerp(0.5f, 1.8f, personality.calm));
		actUntil = Time.time + 90f;
		planSince = Time.time;
		pauseNext = Time.time + Random.Range(6f, 14f);
		Look(planLook, null, "where it means to go", thinkUntil - Time.time + 0.6f);
		Lab.Event("roam: " + planWhy + " (" + magnitude.ToString("0") + " m" + (viaLeft ? ", round by the side" : "") + (planRun ? ", at a run" : "") + ")");
		return true;
	}

	public static bool NotForBots(Rigidbody rb)
	{
		try
		{
			string text = (ThingName(rb) + " " + ((Object)rb).name).ToLowerInvariant().Replace(" ", "").Replace("_", "");
			return text.Contains("spawngun") || text.Contains("nimbus") || text.Contains("devmanipulator") || text.Contains("utilitygun");
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static string ThingName(Rigidbody rb)
	{
		try
		{
			Poolee componentInParent = ((Component)rb).GetComponentInParent<Poolee>();
			if ((Object)(object)componentInParent != (Object)null && (Object)(object)componentInParent.SpawnableCrate != (Object)null)
			{
				return Lessons.NameOf(componentInParent);
			}
		}
		catch (Exception)
		{
		}
		return "thing there";
	}

	private void GoStep(Transform you, Transform head, float dt)
	{
		headYaw = 0f;
		Vector3 val = (((Object)(object)rig.physicsRig != (Object)null && (Object)(object)((Rig)rig.physicsRig).centerOfPressure != (Object)null) ? ((Rig)rig.physicsRig).centerOfPressure.position : Feet);
		Vector3 val3;
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
				if (model == null || (!DriveSkill.Practice && !(Random.value < Tuning.F("drive.theirway", 0.7f))))
				{
					if (!DriveTo(planPlace, planWhy))
					{
						Idle(Random.Range(1f, 2.5f));
					}
					return;
				}
				Vector3 val2 = model.road[0];
				foreach (Vector3 item in model.road)
				{
					val3 = Flat(item - val);
					float sqrMagnitude = ((Vector3)(ref val3)).sqrMagnitude;
					val3 = Flat(val2 - val);
					if (sqrMagnitude < ((Vector3)(ref val3)).sqrMagnitude)
					{
						val2 = item;
					}
				}
				val3 = Flat(val2 - val);
				if (((Vector3)(ref val3)).magnitude < 8f)
				{
					if (!DriveTheirWay())
					{
						Idle(Random.Range(1f, 2.5f));
					}
					return;
				}
				driveOnRoad = true;
				viaLeft = false;
				planPlace = (Ground(val2 + Vector3.up, out var ground) ? ground : val2);
				val3 = Flat(planPlace - val);
				Lab.Event("roam: to the way you drove first (" + ((Vector3)(ref val3)).magnitude.ToString("0") + " m), to set off from there");
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
			pauseNext = Time.time + Random.Range(7f, 16f);
			if (Random.value < Tuning.F("roam.pause", 0.4f))
			{
				val3 = Flat(planPlace - val);
				if (((Vector3)(ref val3)).magnitude > 5f)
				{
					pauseUntil = Time.time + Random.Range(1.2f, 3f) * personality.calm;
					gazeUntil = 0f;
					return;
				}
			}
		}
		if (viaLeft)
		{
			val3 = Flat(viaPlace - val);
			if (((Vector3)(ref val3)).magnitude < Tuning.F("roam.bend.round", 4.5f))
			{
				viaLeft = false;
				nav.SetGoal(planPlace);
			}
		}
		if (onDetour)
		{
			val3 = Flat(nav.Goal - val);
			if (((Vector3)(ref val3)).magnitude < 1.5f)
			{
				onDetour = false;
				nav.SetGoal(detourFor);
			}
		}
		if (!nav.HasGoal && !climbing)
		{
			nav.SetGoal(planPlace);
		}
		running = planRun && !onDetour;
		val3 = Flat(planPlace - val);
		float magnitude = ((Vector3)(ref val3)).magnitude;
		float speed = (running ? MotionLibrary.RunPush : (planPush * Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(magnitude / 3f))));
		bool num = Travel(speed, (planSight == Sight.Bot || planSight == Sight.You) ? 2.2f : 0.9f, dt) || nav.StuckCount >= 3;
		if (((Vector2)(ref walk)).sqrMagnitude > 0.01f)
		{
			Quaternion val4 = Quaternion.Euler(0f, bodyYaw, 0f);
			val3 = new Vector3(walk.x, 0f, walk.y);
			roamHeading = val4 * ((Vector3)(ref val3)).normalized;
		}
		if (!num || climbing)
		{
			return;
		}
		running = false;
		if (!viaLeft && !onDetour)
		{
			val3 = Flat(planPlace - val);
			if (((Vector3)(ref val3)).magnitude < 3f && Mathf.Abs(planPlace.y - val.y) < 1.6f)
			{
				nav.Clear();
				visited.Add(Cell(val));
				float num2 = Random.Range(4f, 10f) * personality.calm;
				switch (planSight)
				{
				case Sight.Thing:
					if (Random.value < Tuning.F("roam.take", 0.9f) && FindSomethingToPlayWith())
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
					if ((driveOnRoad && DriveTheirWay()) || (!BotManager.OwnSpawns && ((Lessons.Road() != null && DriveTheirWay()) || (FarPlace(val, out var place) && DriveTo(place, "going for a drive")))))
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
				Look(planLook, null, "what it came to see", Mathf.Min(num2, Random.Range(2.5f, 5f)));
				Lab.Event("roam: there (" + planWhy + "), staying " + num2.ToString("0") + " s");
				return;
			}
		}
		if (viaLeft)
		{
			viaLeft = false;
			nav.Clear();
			nav.SetGoal(planPlace);
			return;
		}
		Vector3 val5 = (onDetour ? detourFor : planPlace);
		if (detours < 3 && WayRound(val, val5, out var side))
		{
			detours++;
			onDetour = true;
			detourFor = val5;
			nav.Clear();
			nav.SetGoal(side);
			Lab.Event("roam: something in the way, going round");
			return;
		}
		if (planSight == Sight.Drive && driveOnRoad)
		{
			val3 = Flat(planPlace - val);
			if (((Vector3)(ref val3)).magnitude < 15f && DriveTheirWay())
			{
				return;
			}
		}
		string[] obj = new string[7] { "roam: no way there, another idea (", null, null, null, null, null, null };
		val3 = Flat(planPlace - val);
		obj[1] = ((Vector3)(ref val3)).magnitude.ToString("0");
		obj[2] = " m short; walking ended: ";
		obj[3] = travelEnded;
		obj[4] = ", stuck ";
		obj[5] = nav.StuckCount.ToString();
		obj[6] = ")";
		Lab.Event(string.Concat(obj));
		SelfTaught.Bad(planPlace);
		nav.Clear();
		Idle(Random.Range(1f, 2.5f));
	}

	private bool WayRound(Vector3 feet, Vector3 goal, out Vector3 side)
	{
		side = feet;
		Vector3 val = feet + Vector3.up * 1.2f;
		Vector3 val2 = Flat(goal - feet);
		Vector3 normalized = ((Vector3)(ref val2)).normalized;
		float num = 0f;
		float[] array = new float[8] { 40f, -40f, 70f, -70f, 100f, -100f, 130f, -130f };
		RaycastHit val4 = default(RaycastHit);
		foreach (float num2 in array)
		{
			Vector3 val3 = Quaternion.Euler(0f, num2, 0f) * normalized;
			float num3 = (Physics.SphereCast(val, 0.3f, val3, ref val4, 8f, -1, (QueryTriggerInteraction)1) ? ((RaycastHit)(ref val4)).distance : 8f);
			if (!(num3 < 2.5f) && Ground(feet + val3 * Mathf.Min(num3 * 0.75f, 6f), out var ground) && !(Mathf.Abs(ground.y - feet.y) > 1.2f) && SelfTaught.BadAt(ground) < 2)
			{
				float num4 = num3 * (1f - Mathf.Abs(num2) / 220f) + (Physics.Linecast(ground + Vector3.up * 1.2f, goal + Vector3.up * 1.2f, -1, (QueryTriggerInteraction)1) ? 0f : 4f);
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
		Rigidbody val = Pelvis();
		Vector3 val2 = (((Object)(object)val != (Object)null) ? val.position : Feet);
		foreach (RaycastHit item in Physics.RaycastAll(val2, Vector3.down, 3f, -1, (QueryTriggerInteraction)1))
		{
			RaycastHit current = item;
			if ((Object)(object)((RaycastHit)(ref current)).collider != (Object)null && (Object)(object)((Component)((RaycastHit)(ref current)).collider).GetComponentInParent<RigManager>() != (Object)(object)rig && ((RaycastHit)(ref current)).point.y < val2.y)
			{
				val2 = ((RaycastHit)(ref current)).point;
			}
		}
		string[] obj = new string[14]
		{
			"sit (", when, "): state ", null, null, null, null, null, null, null,
			null, null, null, null
		};
		BodyState bodyState = rig.bodyState;
		obj[3] = ((object)(BodyState)(ref bodyState)).ToString();
		obj[4] = ", loco ";
		obj[5] = ((Object)(object)physicsRig != (Object)null && physicsRig.ballLocoEnabled).ToString();
		obj[6] = ", hips ";
		obj[7] = (((Object)(object)val != (Object)null) ? (val.position.y - val2.y).ToString("0.00") : "?");
		obj[8] = " m over the ground, head ";
		obj[9] = (((Object)(object)physicsRig != (Object)null && (Object)(object)((Rig)physicsRig).m_head != (Object)null) ? (((Rig)physicsRig).m_head.position.y - val2.y).ToString("0.00") : "?");
		obj[10] = " m, headset at ";
		obj[11] = (((Object)(object)controls.headset != (Object)null && (Object)(object)controls.vrRoot != (Object)null) ? controls.vrRoot.InverseTransformPoint(controls.headset.position).y.ToString("0.00") : "?");
		obj[12] = " m, pose ";
		obj[13] = pose.height.ToString("0.00");
		Lab.Event(string.Concat(obj));
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
			bool flag = ((Vector2)(ref walk)).sqrMagnitude > 0.01f;
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
		Vector3 val = asked;
		if (e.moving)
		{
			float num = Left(Time.time - e.at, time);
			if (num <= 0f)
			{
				e.moving = false;
			}
			else
			{
				val = asked + e.from * num;
			}
		}
		Vector3 val2 = val - was;
		if (((Vector3)(ref val2)).magnitude > far)
		{
			e.from = was - asked;
			e.at = Time.time;
			e.moving = true;
			val = was;
		}
		return val;
	}

	private static Quaternion EaseTurn(ref Eased e, Quaternion asked, Quaternion was, float round, float time)
	{
		Quaternion val = asked;
		if (e.turning)
		{
			float num = Left(Time.time - e.turnAt, time);
			if (num <= 0f)
			{
				e.turning = false;
			}
			else
			{
				val = Quaternion.Slerp(Quaternion.identity, e.turnFrom, num) * asked;
			}
		}
		if (Quaternion.Angle(val, was) > round)
		{
			e.turnFrom = was * Quaternion.Inverse(asked);
			e.turnAt = Time.time;
			e.turning = true;
			val = was;
		}
		return val;
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
		if ((Object)(object)head == (Object)null)
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
			if ((Object)(object)item.rig == (Object)(object)rig || (Object)(object)item.rig == (Object)null || (Object)(object)item.head == (Object)null)
			{
				continue;
			}
			Vector3 val = Flat(item.velocity);
			float magnitude = ((Vector3)(ref val)).magnitude;
			if (magnitude < num)
			{
				continue;
			}
			Vector3 val2 = Flat(head.position - item.head.position);
			float magnitude2 = ((Vector3)(ref val2)).magnitude;
			if (magnitude2 > Tuning.F("people.far", 18f) || magnitude2 < 0.3f || Mathf.Abs(head.position.y - item.head.position.y) > 3f)
			{
				continue;
			}
			Vector3 val3 = val / magnitude;
			float num2 = Vector3.Dot(val3, val2 / magnitude2);
			bool flag = ((!enemy) ? (item.bot != null && item.bot.enemy) : (item.you || item.bot == null || !item.bot.enemy));
			if (!calmNow && !flag)
			{
				continue;
			}
			if (num2 > 0.9f && magnitude2 < 7f && magnitude2 / magnitude < Tuning.F("people.rush.time", 1.3f) && Time.time - rushedAt > 4f && !Down && !climbing && !InLesson && (magnitude2 < 3f || Targets.InSight(head.position, item.head.position, ((Component)item.rig).transform, rig)))
			{
				Rushed(item, head, val3, magnitude2);
				continue;
			}
			bool flag2 = ((!enemy) ? (item.bot != null && item.bot.enemy) : (item.you || item.bot == null || !item.bot.enemy));
			if (!(!calmNow || flag2) && !(num2 > 0.6f) && !(magnitude < num + 0.4f) && !(magnitude2 > 16f) && Time.time - runnerAt > Tuning.F("people.runner.every", 7f) && Targets.InSight(head.position, item.head.position, ((Component)item.rig).transform, rig))
			{
				SawRunner(item, head, val3);
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
		Quaternion val = Quaternion.Euler(0f, 0f - bodyYaw, 0f);
		Vector3 val2 = Flat(m.head.position - head.position);
		Vector3 val3 = val * ((Vector3)(ref val2)).normalized;
		if (HandsFree)
		{
			flinchSide = Mathf.Clamp(val3.x, -1f, 1f);
			flinchSize = Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(7f, 2f, far));
			flinchAt = Time.time + Random(0.04f, 0.1f) * personality.reaction;
			braceFrom = flinchAt + 0.1f;
			braceUntil = braceFrom + Random(0.7f, 1.2f);
		}
		Look(m.head.position, m.head, "someone running at it", Random(1.2f, 2f));
		if (!Playing && (Object)(object)nearestSeen.transform == (Object)null && (mode == Mode.Follow || mode == Mode.Wander || mode == Mode.Brain) && Tuning.F("people.rush.step", 1f) > 0.5f)
		{
			Vector3 val4 = Vector3.Cross(Vector3.up, way);
			if (Vector3.Dot(val4, head.position - m.head.position) < 0f)
			{
				val4 = -val4;
			}
			Vector3 val5 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * val4;
			Vector2 push = new Vector2(val5.x, val5.z) * MotionLibrary.WalkPush;
			if (RoomToStep(head.position - Vector3.up * 0.4f, push))
			{
				rushStep = val4;
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
			Vector3 val = Flat(item.velocity);
			if ((Object)(object)item.rig != (Object)(object)rig && ((Vector3)(ref val)).magnitude > 3f && Vector3.Dot(((Vector3)(ref val)).normalized, way) > 0.6f)
			{
				Vector3 val2 = item.head.position - head.position;
				if (((Vector3)(ref val2)).magnitude < 22f)
				{
					num2++;
				}
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
			if (Random.value < Tuning.F("people.alarm.squat", 0.5f))
			{
				Squat(Random(1.5f, 3f), 0.55f);
			}
			text = ", and takes it that something is up";
		}
		else if (flag && !enemy && Random.value < Tuning.F("people.runner.follow", 0.3f) * personality.curiosity && Navigator.RandomPlace(runnerPoint - Vector3.up * 1.5f, 0.5f, 3f, out place))
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
			if ((Object)(object)head2 == (Object)null)
			{
				continue;
			}
			Vector3 val = head2.position - head.position;
			if (!(((Vector3)(ref val)).magnitude > Tuning.F("meet.far", 6f)) && !(Mathf.Abs(head2.position.y - head.position.y) > 1.2f) && Targets.InSight(head.position, head2.position, ((Component)item.rig).transform, rig))
			{
				if (Random.value > Tuning.F("meet.chance", 0.6f))
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
		Transform val = ((bot != null && bot.Alive) ? bot.Head : null);
		bool num = (Object)(object)val == (Object)null || bot.dead || bot.Down || Down || bot.friend != this || Time.time > meetUntil || (Object)(object)nearestSeen.transform != (Object)null || (Object)(object)brainTarget.transform != (Object)null || climbing || InLesson || Playing || Time.time - flinchAt < 0.6f || ((mode == Mode.Wander || mode == Mode.Brain) && act != Act.Idle);
		float num2;
		if (!((Object)(object)val != (Object)null))
		{
			num2 = 0f;
		}
		else
		{
			Vector3 val2 = Flat(val.position - head.position);
			num2 = ((Vector3)(ref val2)).magnitude;
		}
		float num3 = num2;
		if (num || num3 > 10f)
		{
			EndMeet();
			return;
		}
		if (Time.time >= meetLookAt)
		{
			meetLookAt = Time.time + 0.8f;
			Look(val.position, val, "another bot", 1.3f);
		}
		float num4 = Time.time - meetFrom;
		if (meetStage == 0 && (num3 < Tuning.F("meet.near", 1.7f) + 0.5f || num4 > 3f))
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
		float num5 = Time.time - meetNearAt - 2.4f;
		if (meetUntil - Time.time < 1.6f)
		{
			meetStage = 2;
			if (!meetLead)
			{
				WaveNow(1);
			}
		}
		else if (num5 >= 0f)
		{
			meetTalking = (int)(num5 / 1.9f) % 2 == 0 == meetLead;
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
			Vector3 val = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * rushStep;
			walk = new Vector2(val.x, val.z) * MotionLibrary.WalkPush;
			running = false;
		}
		if (friend == null)
		{
			return;
		}
		Transform head = Head;
		Transform val2 = (friend.Alive ? friend.Head : null);
		if (!((Object)(object)head == (Object)null) && !((Object)(object)val2 == (Object)null))
		{
			Vector3 val3 = Flat(val2.position - head.position);
			float magnitude = ((Vector3)(ref val3)).magnitude;
			Face(val2.position, dt, 170f);
			running = false;
			if (meetLead && mode != 0 && magnitude > Tuning.F("meet.near", 1.7f) && meetStage == 0)
			{
				Vector3 val4 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * (val3 / magnitude);
				walk = new Vector2(val4.x, val4.z) * MotionLibrary.WalkPush * 0.85f;
			}
			else if (magnitude < 0.9f)
			{
				Vector3 val5 = Quaternion.Euler(0f, 0f - bodyYaw, 0f) * (-val3 / Mathf.Max(magnitude, 0.01f));
				walk = new Vector2(val5.x, val5.z) * MotionLibrary.WalkPush * 0.5f;
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
			Vector3 val = default(Vector3);
			((Vector3)(ref val))._002Ector((0.2f + 0.05f * Mathf.Cos(num3 * 3.7f)) * num, height - (0.5f - 0.06f * Mathf.Sin(num3 * 5.2f)) * num, (0.3f + 0.03f * Mathf.Sin(num3 * 2.9f)) * num);
			Quaternion val2 = Quaternion.Euler(-35f + 14f * Mathf.Sin(num3 * 5.2f), 0f, -55f + 18f * Mathf.Cos(num3 * 3.7f));
			if (personality.mirror > 0.5f)
			{
				pose.leftPos = Vector3.Lerp(pose.leftPos, new Vector3(0f - val.x, val.y, val.z), num4);
				pose.leftRot = Quaternion.Slerp(pose.leftRot, Quaternion.Euler(((Quaternion)(ref val2)).eulerAngles.x, 0f, 0f - ((Quaternion)(ref val2)).eulerAngles.z), num4);
			}
			else
			{
				pose.rightPos = Vector3.Lerp(pose.rightPos, val, num4);
				pose.rightRot = Quaternion.Slerp(pose.rightRot, val2, num4);
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
			if (!(Random.value > Tuning.F("surrender.chance", 0.5f)))
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
			Hand[] array = (Hand[])(object)new Hand[2] { LeftHand, RightHand };
			foreach (Hand val in array)
			{
				if ((Object)(object)val != (Object)null && (Object)(object)val.m_CurrentAttachedGO != (Object)null)
				{
					Grip component = val.m_CurrentAttachedGO.GetComponent<Grip>();
					if ((Object)(object)component != (Object)null)
					{
						LetGoOf(component, val);
					}
				}
			}
			DropStowed();
		}
		catch (Exception ex)
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
		if ((Object)(object)you != (Object)null)
		{
			Face(you.position, dt, 90f);
		}
		playPitch = 0f;
		playYaw = 0f;
		float num;
		if (!((Object)(object)you != (Object)null) || !((Object)(object)head != (Object)null))
		{
			num = 99f;
		}
		else
		{
			Vector3 val = Flat(you.position - head.position);
			num = ((Vector3)(ref val)).magnitude;
		}
		if (!(num < Tuning.F("surrender.spare.near", 6f)) || !(Time.time - PlayerWatch.FiredAt > 2f))
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
			Transform val = ((Component)rig).transform.Find("BotEnemyMark");
			if ((Object)(object)val != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)val).gameObject);
			}
		}
		catch (Exception)
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
			if ((Object)(object)Player.RigManager != (Object)null)
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
			Rigidbody val = Pelvis();
			if ((Object)(object)val != (Object)null && ((Vector3)(ref push)).sqrMagnitude > 0.0001f)
			{
				Vector3 val2 = Flat(push);
				val.AddForce(((Vector3)(ref val2)).normalized * Mathf.Lerp(0.6f, 2.2f, hard) * Tuning.F("trip.shove", 1f), (ForceMode)2);
			}
		}
		catch (Exception ex)
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

	public void Bumped(BodyPart part, Collision c)
	{
		if (dead || down || rising || c == null || Puppet || (int)part == 7 || (int)part == 10 || (int)part == 6 || (int)part == 9 || Time.time < Clone.SuppressUntil || spawningGun || Time.time - spawnAskedAt < 1.5f || InLesson)
		{
			return;
		}
		Rigidbody rigidbody = c.rigidbody;
		if ((Object)(object)rigidbody == (Object)null || rigidbody.isKinematic)
		{
			return;
		}
		RigManager componentInParent = ((Component)rigidbody).GetComponentInParent<RigManager>();
		if ((Object)(object)componentInParent == (Object)(object)rig || ((Object)(object)componentInParent != (Object)null && BotRig.IsBot(componentInParent)))
		{
			return;
		}
		if ((Object)(object)item != (Object)null && (Playing || Time.time - playAt < 2f))
		{
			MarrowEntity componentInParent2 = ((Component)item).GetComponentInParent<MarrowEntity>();
			if ((Object)(object)rigidbody == (Object)(object)item || ((Object)(object)componentInParent2 != (Object)null && ((Component)rigidbody).transform.IsChildOf(((Component)componentInParent2).transform)))
			{
				return;
			}
		}
		if ((Object)(object)leftVehicle != (Object)null && Time.time - leftVehicleAt < 5f && ((Component)rigidbody).transform.IsChildOf(leftVehicle))
		{
			return;
		}
		Hand[] array = (Hand[])(object)new Hand[2] { LeftHand, RightHand };
		foreach (Hand val in array)
		{
			GameObject val2 = (((Object)(object)val != (Object)null) ? val.m_CurrentAttachedGO : null);
			if ((Object)(object)val2 != (Object)null && ((Component)rigidbody).transform.IsChildOf(val2.transform.root) && (Object)(object)val2.transform.root != (Object)(object)((Component)rig).transform)
			{
				return;
			}
		}
		Vector3 val3;
		if ((Object)(object)componentInParent == (Object)null)
		{
			val3 = rigidbody.velocity;
			if (((Vector3)(ref val3)).magnitude < Tuning.F("lying.speed", 1.5f))
			{
				return;
			}
		}
		val3 = c.relativeVelocity;
		float magnitude = ((Vector3)(ref val3)).magnitude;
		val3 = c.impulse;
		float magnitude2 = ((Vector3)(ref val3)).magnitude;
		bool flag = (int)part >= 11;
		bool flag2 = (int)part == 0 || (int)part == 1;
		float num = Mathf.Clamp(pose.height / 1.7f, 0.5f, 2f);
		float num2 = Tuning.F("trip.impulse", 30f) * Mathf.Max(0.5f, num * num) * (flag ? Tuning.F("trip.legs", 0.6f) : (flag2 ? 0.8f : 1f));
		if (magnitude2 > num2 * 0.4f && magnitude > 1.5f && Time.time - bumpSaidAt > 0.5f)
		{
			bumpSaidAt = Time.time;
			Lab.Event("trip: bumped on " + ((object)(BodyPart)(ref part)).ToString() + " by " + ((Object)rigidbody).name + ", " + magnitude2.ToString("0") + " Ns at " + magnitude.ToString("0.0") + " m/s (down at " + num2.ToString("0") + ")");
		}
		if (!(magnitude < Tuning.F("trip.speed", 2.2f)) && !(magnitude2 < num2))
		{
			val3 = rigidbody.velocity;
			if (!(((Vector3)(ref val3)).magnitude < Tuning.F("trip.speed", 2.2f) * 0.7f) || !((Object)(object)componentInParent == (Object)null))
			{
				Trip(Mathf.InverseLerp(num2, num2 * 3f, magnitude2), -c.relativeVelocity, "hit on " + ((object)(BodyPart)(ref part)).ToString() + " by " + ((Object)rigidbody).name);
			}
		}
	}

	private void TripByHit(float damage, BodyPart part, Attack attack, bool bullet = false)
	{
		float num = damage / Mathf.Max(0.01f, MaxHp);
		bool flag = (int)part >= 11;
		Vector3 push = ((attack != null) ? attack.direction : Vector3.zero);
		try
		{
			bool num2 = bullet;
			int num3;
			if (attack != null)
			{
				AttackType attackType = attack.attackType;
				num3 = ((((object)(AttackType)(ref attackType)).ToString().IndexOf("Pierc", StringComparison.OrdinalIgnoreCase) >= 0) ? 1 : 0);
			}
			else
			{
				num3 = 0;
			}
			bullet = (byte)((num2 ? 1u : 0u) | (uint)num3) != 0;
		}
		catch (Exception)
		{
		}
		if (bullet)
		{
			if (num > Tuning.F("trip.bullet", 0.65f))
			{
				Trip(num, push, "a hard hit");
			}
			else if (flag && Random.value < Tuning.F("trip.bullet.leg", 0.25f))
			{
				Trip(0.3f + num * 0.5f, push, "shot in the leg");
			}
		}
		else if (num > Tuning.F("trip.damage", 0.3f))
		{
			Trip(num, push, "a hard hit");
		}
		else if (flag && num > 0.06f && Random.value < Tuning.F("trip.leg.chance", 0.6f))
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
		Hand[] array = (Hand[])(object)new Hand[2]
		{
			Player.LeftHand,
			Player.RightHand
		};
		foreach (Hand val in array)
		{
			GameObject val2 = (((Object)(object)val != (Object)null) ? val.m_CurrentAttachedGO : null);
			if (!((Object)(object)val2 == (Object)null) && !((Object)(object)val.rb == (Object)null) && val2.transform.IsChildOf(((Component)rig).transform))
			{
				Rigidbody val3 = Pelvis();
				Vector3 push = val.rb.velocity - (((Object)(object)val3 != (Object)null) ? (Flat(val3.velocity) * 0.5f) : Vector3.zero);
				if (((Vector3)(ref push)).magnitude > Tuning.F("trip.pull", 2.4f))
				{
					Trip(Mathf.InverseLerp(2.4f, 6f, ((Vector3)(ref push)).magnitude), push, "you pulled it over");
					break;
				}
			}
		}
	}

	private void Calm(float most)
	{
		if (bodyParts == null || bodyParts.Length == 0 || (Object)(object)bodyParts[0] == (Object)null)
		{
			bodyParts = (((Object)(object)rig.physicsRig != (Object)null) ? ((Component)rig.physicsRig).GetComponentsInChildren<Rigidbody>() : null);
		}
		if (bodyParts == null)
		{
			return;
		}
		Rigidbody[] array = bodyParts;
		foreach (Rigidbody val in array)
		{
			if (!((Object)(object)val == (Object)null) && !val.isKinematic)
			{
				Vector3 val2 = val.velocity;
				if (val2.y > most * 0.5f)
				{
					val2.y = most * 0.5f;
				}
				if (((Vector3)(ref val2)).magnitude > most)
				{
					val2 = ((Vector3)(ref val2)).normalized * most;
				}
				val.velocity = val2;
				if (most <= 0f)
				{
					val.angularVelocity *= 0.5f;
				}
			}
		}
	}

	private void Limp(bool on)
	{
		PhysicsRig physicsRig = rig.physicsRig;
		if ((Object)(object)physicsRig == (Object)null)
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
		if ((Object)(object)physicsRig != (Object)null && !dead && !climbing && !Puppet && !physicsRig.ballLocoEnabled && (Object)(object)rig.activeSeat == (Object)null)
		{
			return (int)rig.bodyState != 1;
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
				catch (Exception)
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
			Rigidbody val = Pelvis();
			Vector3 val2;
			int num;
			if (!((Object)(object)val == (Object)null))
			{
				val2 = val.velocity;
				num = ((((Vector3)(ref val2)).magnitude < 1.2f) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
			bool flag = (byte)num != 0;
			bool flag2 = false;
			Hand[] array = (Hand[])(object)new Hand[2]
			{
				Player.LeftHand,
				Player.RightHand
			};
			foreach (Hand val3 in array)
			{
				GameObject val4 = (((Object)(object)val3 != (Object)null) ? val3.m_CurrentAttachedGO : null);
				flag2 |= (Object)(object)val4 != (Object)null && val4.transform.IsChildOf(((Component)rig).transform);
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
					catch (Exception)
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
			catch (Exception ex3)
			{
				Lab.Event("trip: could not get up: " + ex3.Message);
			}
			down = false;
			rising = true;
			riseAt = Time.time;
			if ((Object)(object)head != (Object)null && (Object)(object)val != (Object)null)
			{
				val2 = Flat(head.position - val.position);
				if (((Vector3)(ref val2)).magnitude > 0.25f)
				{
					bodyYaw = Recorder.Yaw(Flat(head.position - val.position));
				}
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
		float num2 = Tuning.F("trip.rise", 1.7f);
		float num3 = Mathf.Clamp01((Time.time - riseAt) / num2);
		if (Time.time - riseAt < Tuning.F("trip.calm", 0.9f))
		{
			Calm(Tuning.F("trip.calm.speed", 2.5f));
		}
		float num4 = StandingEyes();
		float num5 = ((num4 > 0.3f) ? (num4 * 0.97f) : standingHeight);
		float num6 = num5 / 1.7f;
		float num7 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 1f, num3));
		float num8 = Mathf.Lerp(num5 * Tuning.F("trip.low", 0.38f), num5, num7);
		float num9 = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.85f, num3));
		Frame frame = Standing(Time.time - born, Mathf.Lerp(35f, 0f, num7), 0f, 0f);
		frame.height = num8;
		float num10 = Mathf.Lerp(num8 - 0.78f * num6, Mathf.Max(0.08f * num6, num8 - 0.62f * num6), num9);
		frame.leftPos = new Vector3(-0.24f * num6, num10, Mathf.Lerp(0.06f, 0.34f, num9) * num6);
		frame.rightPos = new Vector3(0.24f * num6, num10, Mathf.Lerp(0.06f, 0.34f, num9) * num6);
		frame.leftRot = Quaternion.Euler(Mathf.Lerp(65f, 80f, num9), 10f, 0f);
		frame.rightRot = Quaternion.Euler(Mathf.Lerp(65f, 80f, num9), -10f, 0f);
		pose = frame;
		curl = Mathf.Lerp(0.25f, 0.05f, num9);
		if (num3 >= 1f)
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
			if ((Object)(object)rightHand == (Object)null || (Object)(object)item == (Object)null || (Object)(object)gun == (Object)null || !LongGun || (Object)(object)support != (Object)null || Tuning.F("hold.lock.long", 1f) < 0.5f)
			{
				lockedJoint = IntPtr.Zero;
				return;
			}
			Transform val = GunWeight.RootOf((Component)(object)item);
			foreach (ConfigurableJoint component in ((Component)rightHand).GetComponents<ConfigurableJoint>())
			{
				if (!((Object)(object)component == (Object)null) && !((Object)(object)((Joint)component).connectedBody == (Object)null) && ((Component)((Joint)component).connectedBody).transform.IsChildOf(val) && ((int)component.angularXMotion != 0 || (int)component.angularYMotion != 0 || (int)component.angularZMotion != 0))
				{
					ConfigurableJointMotion val2 = (ConfigurableJointMotion)0;
					component.angularZMotion = (ConfigurableJointMotion)0;
					ConfigurableJointMotion angularXMotion = (component.angularYMotion = val2);
					component.angularXMotion = angularXMotion;
					if (lockedJoint != ((Il2CppObjectBase)(object)component).Pointer)
					{
						lockedJoint = ((Il2CppObjectBase)(object)component).Pointer;
						Lab.Event("gun: " + ((Object)gun).name + " locked fast in its hand (no turning in the grip)");
					}
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public void LabJoints()
	{
		Hand rightHand = RightHand;
		if ((Object)(object)rightHand == (Object)null || (Object)(object)item == (Object)null)
		{
			Lab.Event("joints: no gun in hand");
			return;
		}
		Transform val = GunWeight.RootOf((Component)(object)item);
		List<ConfigurableJoint> list = new List<ConfigurableJoint>();
		foreach (ConfigurableJoint componentsInChild in ((Component)rig.physicsRig).GetComponentsInChildren<ConfigurableJoint>(true))
		{
			list.Add(componentsInChild);
		}
		foreach (ConfigurableJoint componentsInChild2 in ((Component)val).GetComponentsInChildren<ConfigurableJoint>(true))
		{
			list.Add(componentsInChild2);
		}
		Rigidbody rb = rightHand.rb;
		foreach (ConfigurableJoint item in list)
		{
			if (!((Object)(object)item == (Object)null))
			{
				Rigidbody component = ((Component)item).GetComponent<Rigidbody>();
				Rigidbody connectedBody = ((Joint)item).connectedBody;
				bool num = ((Object)(object)component != (Object)null && ((Component)component).transform.IsChildOf(val)) || ((Object)(object)connectedBody != (Object)null && ((Component)connectedBody).transform.IsChildOf(val));
				bool flag = (Object)(object)component == (Object)(object)rb || (Object)(object)connectedBody == (Object)(object)rb || ((Component)item).transform.IsChildOf(((Component)rightHand).transform);
				if (num)
				{
					string[] obj = new string[33]
					{
						"joint on ",
						((Object)item).name,
						" to ",
						((Object)(object)connectedBody != (Object)null) ? ((Object)connectedBody).name : "world",
						flag ? " (the hand)" : "",
						": move ",
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null
					};
					ConfigurableJointMotion val2 = item.xMotion;
					obj[6] = ((object)(ConfigurableJointMotion)(ref val2)).ToString();
					obj[7] = "/";
					val2 = item.yMotion;
					obj[8] = ((object)(ConfigurableJointMotion)(ref val2)).ToString();
					obj[9] = "/";
					val2 = item.zMotion;
					obj[10] = ((object)(ConfigurableJointMotion)(ref val2)).ToString();
					obj[11] = ", turn ";
					val2 = item.angularXMotion;
					obj[12] = ((object)(ConfigurableJointMotion)(ref val2)).ToString();
					obj[13] = "/";
					val2 = item.angularYMotion;
					obj[14] = ((object)(ConfigurableJointMotion)(ref val2)).ToString();
					obj[15] = "/";
					val2 = item.angularZMotion;
					obj[16] = ((object)(ConfigurableJointMotion)(ref val2)).ToString();
					obj[17] = ", drive mode ";
					RotationDriveMode rotationDriveMode = item.rotationDriveMode;
					obj[18] = ((object)(RotationDriveMode)(ref rotationDriveMode)).ToString();
					obj[19] = ", slerp ";
					JointDrive val3 = item.slerpDrive;
					obj[20] = ((JointDrive)(ref val3)).positionSpring.ToString("0");
					obj[21] = "/";
					val3 = item.slerpDrive;
					obj[22] = ((JointDrive)(ref val3)).positionDamper.ToString("0");
					obj[23] = " max ";
					val3 = item.slerpDrive;
					obj[24] = ((JointDrive)(ref val3)).maximumForce.ToString("0");
					obj[25] = ", angX ";
					val3 = item.angularXDrive;
					obj[26] = ((JointDrive)(ref val3)).positionSpring.ToString("0");
					obj[27] = "/";
					val3 = item.angularXDrive;
					obj[28] = ((JointDrive)(ref val3)).positionDamper.ToString("0");
					obj[29] = ", proj ";
					JointProjectionMode projectionMode = item.projectionMode;
					obj[30] = ((object)(JointProjectionMode)(ref projectionMode)).ToString();
					obj[31] = ", enabled ";
					obj[32] = ((Joint)item).enableCollision.ToString();
					Lab.Event(string.Concat(obj));
				}
			}
		}
		Lab.Event("joints: " + list.Count + " looked at, hand body " + (((Object)(object)rb != (Object)null) ? ((Object)rb).name : "none"));
	}

	public IEnumerable<Gun> GunsInHand()
	{
		List<Gun> list = new List<Gun>();
		Hand[] array = (Hand[])(object)new Hand[2] { RightHand, LeftHand };
		foreach (Hand val in array)
		{
			try
			{
				GameObject val2 = (((Object)(object)val != (Object)null) ? val.m_CurrentAttachedGO : null);
				Gun val3 = (((Object)(object)val2 != (Object)null) ? val2.GetComponentInParent<Gun>() : null);
				if ((Object)(object)val3 == (Object)null && (Object)(object)val2 != (Object)null)
				{
					MarrowEntity componentInParent = val2.GetComponentInParent<MarrowEntity>();
					val3 = (((Object)(object)componentInParent != (Object)null) ? ((Component)componentInParent).GetComponentInChildren<Gun>() : null);
				}
				if ((Object)(object)val3 != (Object)null)
				{
					list.Add(val3);
				}
			}
			catch (Exception)
			{
			}
		}
		return list;
	}

	private bool HoldFromRecording(Vector3 point, Transform head, bool aimed)
	{
		if (Tuning.F("hold.recorded", 1f) < 0.5f || !lockSet || (Object)(object)gun == (Object)null || (Object)(object)item == (Object)null || (Object)(object)gun.triggerGrip == (Object)null || (Object)(object)gun.firePointTransform == (Object)null)
		{
			return false;
		}
		Transform val = GripBook.ThingOf(gun.triggerGrip);
		Transform headset = controls.headset;
		if ((Object)(object)val == (Object)null || (Object)(object)headset == (Object)null)
		{
			return false;
		}
		string text = GripBook.KeyOf(val);
		if (text != recKey)
		{
			recKey = text;
			recFrames = HoldLibrary.For(text);
			recAimed.Clear();
			recCarried.Clear();
			if (recFrames != null)
			{
				Vector3 val2 = Quaternion.Inverse(val.rotation) * gun.firePointTransform.forward;
				for (int i = 0; i < recFrames.Count; i++)
				{
					((Vector3.Angle(recFrames[i].thingInHead.rotation * val2, Vector3.forward) < Tuning.F("hold.recorded.aimed", 20f)) ? recAimed : recCarried).Add(i);
				}
			}
			recCursor = Random.Range(0f, 1000f);
		}
		List<int> list = (aimed ? recAimed : recCarried);
		if (recFrames == null || list.Count < 10)
		{
			return false;
		}
		if (aimed != recWasAimed)
		{
			recWasAimed = aimed;
			recCursor = Random.Range(0f, (float)list.Count);
		}
		recCursor = (recCursor + Time.deltaTime * 15f) % (float)list.Count;
		HoldLibrary.Frame frame = recFrames[list[(int)recCursor]];
		Pose pose = Pose.Of(headset) * frame.thingInHead;
		Pose pose2 = Pose.Of(val).Inverse() * Pose.Of(((Component)item).transform);
		Pose pose3 = Pose.Of(val).Inverse() * Pose.Of(gun.firePointTransform);
		Vector3 position = (Pose.Of(val).Inverse() * Pose.Of(((Component)gun.triggerGrip).transform)).position;
		if (aimed)
		{
			Vector3 val3 = pose.position + pose.rotation * position;
			Vector3 val4 = pose.position + pose.rotation * pose3.position;
			Vector3 val5 = pose.rotation * (pose3.rotation * Vector3.forward);
			Vector3 val6 = point - val4;
			Quaternion val7 = Quaternion.FromToRotation(val5, ((Vector3)(ref val6)).normalized);
			pose = new Pose(val3 + val7 * (pose.position - val3), val7 * pose.rotation);
		}
		Pose b = pose * pose2;
		if (Time.time - aimedSince > 0.3f)
		{
			aimedWantSet = false;
		}
		aimedSince = Time.time;
		float k = Mathf.Clamp01(Time.deltaTime * Tuning.F("hold.recorded.ease", 9f));
		aimedWant = (aimedWantSet ? Pose.Lerp(aimedWant, b, k) : Pose.Lerp(Pose.Of(((Component)item).transform), b, 0.15f));
		aimedWantSet = true;
		Transform vrRoot = controls.vrRoot;
		Quaternion val8 = Quaternion.Euler(0f, bodyYaw, 0f);
		Pose pose4 = aimedWant * gunInController.Inverse();
		handOverride = true;
		learnedHoldAt = Time.time;
		holdAimedNow = aimed;
		handAt = vrRoot.InverseTransformPoint(pose4.position);
		handTurn = Quaternion.Inverse(val8) * pose4.rotation;
		playHeight = standingHeight * 0.985f;
		if (supportHeld && step == Step.Ready)
		{
			Pose pose5 = Pose.Of(val);
			Pose pose6 = ((frame.two && leftOnGunSet) ? (pose5 * frame.leftInThing * leftHandOnController.Inverse()) : (leftOnGunSet ? (aimedWant * leftOnGun) : default(Pose)));
			if (leftOnGunSet)
			{
				leftOverride = true;
				leftAt = vrRoot.InverseTransformPoint(pose6.position);
				leftTurn = Quaternion.Inverse(val8) * pose6.rotation;
			}
		}
		Vector3 val9 = (Vector3)(aimed ? (Quaternion.Inverse(val8) * (point - head.position)) : new Vector3(0f, -0.15f, 1f));
		float y = val9.y;
		Vector2 val10 = new Vector2(val9.x, val9.z);
		playPitch = Mathf.Clamp((0f - Mathf.Atan2(y, Mathf.Max(((Vector2)(ref val10)).magnitude, 0.1f))) * 57.29578f, -50f, 60f);
		playYaw = Mathf.Clamp(Mathf.Atan2(val9.x, val9.z) * 57.29578f, -50f, 50f);
		return true;
	}
}
