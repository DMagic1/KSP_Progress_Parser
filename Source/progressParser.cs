using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using KSPAchievements;
using FinePrint;
using FinePrint.Utilities;

namespace ProgressParser
{
	public static class progressParser
	{
		//Store a list of each celestial body sub node; accessible by the celestial body name
		private static Dictionary<string, progressBodyCollection> bodySubTrees = new Dictionary<string, progressBodyCollection>();

		//Store a list of each progress node type; accessible by the progress node ID
		private static Dictionary<string, progressStandard> pointsOfInterest = new Dictionary<string, progressStandard>();
		private static Dictionary<string, progressInterval> intervalNodes = new Dictionary<string, progressInterval>();
		private static Dictionary<string, progressStandard> standardNodes = new Dictionary<string, progressStandard>();

		//Store each general progress node type here; POI nodes excepted
		private static progressInterval altitude;
		private static progressInterval depth;
		private static progressInterval speed;
		private static progressInterval distance;
		private static progressStandard crewRecovery;
		private static progressStandard firstLaunch;
		private static progressStandard reachSpace;
		private static progressStandard runwayLanding;
		private static progressStandard launchpadLanding;
		private static progressStandard towerBuzz;

		private static bool anyInterval;
		private static bool anyStandard;
		private static bool anyPOI;
		private static bool anyBody;

		//A series of simple string descriptions for each progress node; the body-specific nodes substitute the body's name for the {0}
		public const string altitudeDescriptor = "Altitude";
		public const string speedDescriptor = "Speed";
		public const string depthDescriptor = "Depth";
		public const string distanceDescriptor = "Distance";

		public const string crewRecoveryDescriptor = "Recovered First Crew";
		public const string firstLaunchDescriptor = "First Launch";
		public const string reachSpaceDescriptor = "Reached Space";
		public const string runwayDescriptor = "Landed On The Runway";
		public const string launchpadDescriptor = "Landed On The Launchpad";
		public const string towerBuzzDescriptor = "Buzzed The Tower";

		public const string flybyDescriptor = "Conducted A Fly By Of {0}";
		public const string orbitDescriptor = "Entered Orbit Around {0}";
		public const string landingDescriptor = "Landed On {0}";
		public const string escapeDescriptor = "Escaped {0}'s Gravity";
		public const string scienceDescriptor = "Collected Science From {0}";
		public const string flightDescriptor = "Flew In {0}'s Atmosphere";
		public const string returnLandingDescriptor = "Returned From The Surface Of {0}";
		public const string returnFlybyDescriptor = "Returned After A Fly By Of {0}";
		public const string returnOrbitDescriptor = "Returned From Orbit Around {0}";
		public const string splashdownDescriptor = "Splashed Down On {0}";
		public const string suborbitDescriptor = "Entered A Sub-Orbital Trajectory At {0}";
		public const string crewTransferDescriptor = "Transferred Crew At {0}";
		public const string dockingDescriptor = "Perfomed A Docking At {0}";
		public const string flagDescriptor = "Planted A Flag On {0}";
		public const string spacewalkDescriptor = "Conducted A Space Walk At {0}";
		public const string EVADescriptor = "Performed A Surface EVA On {0}";
		public const string stationDescriptor = "Construced A Space Station At {0}";
		public const string baseDescriptor = "Escaped {0}'s Gravity";
		public const string rendezvousDescriptor = "Performed A Rendezvous At {0}";

		public const string POIBopKrakenDescriptor = "Dead Kraken";
		public const string POIDunaFaceDescriptor = "Face";
		public const string POIDunaMSLDescriptor = "MSL";
		public const string POIDunaPyramidDescriptor = "Pyramid";
		public const string POIKerbinIslandAirfieldDescriptor = "Island Airfield";
		public const string POIKerbinKSC2Descriptor = "KSC 2";
		public const string POIKerbinMonolith1Descriptor = "Monolith 1";
		public const string POIKerbinMonolith2Descriptor = "Monolith 2";
		public const string POIKerbinMonolith3Descriptor = "Monolith 3";
		public const string POIKerbinPyramidsDescriptor = "Pyramids";
		public const string POIKerbinUFODescriptor = "UFO";
		public const string POIMinmusMonolithDescriptor = "Monolith";
		public const string POIMunArmstrongDescriptor = "Neil Armstrong Memorial";
		public const string POIMunMonolith1Descriptor = "Monolith 1";
		public const string POIMunMonolith2Descriptor = "Monolith 2";
		public const string POIMunMonolith3Descriptor = "Monolith 3";
		public const string POIMunRockArch1Descriptor = "Rock Arch 1";
		public const string POIMunRockArch2Descriptor = "Rock Arch 2";
		public const string POIMunRockArch3Descriptor = "Rock Arch 3";
		public const string POIMunUFODescriptor = "UFO";
		public const string POITyloCaveDescriptor = "Cave";
		public const string POIValIceHengeDescriptor = "Ice Henge";

		public const string StandardNote = "Completed By {0} On {1}";
		public const string CrewNote = "Performed By {0} On {1}";
		public const string RecoveryNote = "Recovered {0} On {1}";
		public const string FlagNote = "Planted By {0} On {1}";
		public const string FacilityNote = "Constructed {0} On {1}";
		public const string POINote = "Discovered By {0} On {1}";

		private static bool loading;
		public static string gameTitle;

		public static void initialize(Game g)
		{
			if (g.Title == gameTitle)
				return;

			gameTitle = g.Title;

			if (!loading)
				progressController.instance.StartCoroutine(parseProgressTree());
		}

		private static IEnumerator parseProgressTree()
		{
			loading = true;

			int timer = 0;

			while (ProgressTracking.Instance == null && timer < 500)
			{
				timer++;
				yield return null;
			}

			if (timer >= 500)
			{
				loading = false;
				yield break;
			}

			while (timer < 10)
			{
				timer++;
				yield return null;
			}

			timer = 0;

			bodySubTrees.Clear();
			standardNodes.Clear();
			intervalNodes.Clear();
			pointsOfInterest.Clear();

			loadIntervalNodes();
			loadStandardNodes();
			loadPOINodes();

			for (int i = 0; i < ProgressTracking.Instance.celestialBodyNodes.Length; i++)
			{
				CelestialBodySubtree b = ProgressTracking.Instance.celestialBodyNodes[i];

				loadNextBodyNode(b);
			}

			if (timer >= 500)
			{
				Debug.Log("[Progress Tracking Parser] Progress Parser Timed Out");
				loading = false;
				yield break;
			}

			updateCompletionRecord();

			loading = false;
		}

		private static void loadIntervalNodes()
		{
			addProgressInterval(ProgressType.ALTITUDERECORD, ProgressTracking.Instance.altitudeRecords, altitudeDescriptor);
			addProgressInterval(ProgressType.SPEEDRECORD, ProgressTracking.Instance.speedRecords, speedDescriptor);
			addProgressInterval(ProgressType.DISTANCERECORD, ProgressTracking.Instance.distanceRecords, distanceDescriptor);
			addProgressInterval(ProgressType.DEPTHRECORD, ProgressTracking.Instance.depthRecords, depthDescriptor);
		}

		private static void loadStandardNodes()
		{
			addProgressStandard(ProgressType.CREWRECOVERY, ProgressTracking.Instance.firstCrewToSurvive, "", crewRecoveryDescriptor, RecoveryNote, crewNameFromNode(ProgressTracking.Instance.firstCrewToSurvive));
			addProgressStandard(ProgressType.FIRSTLAUNCH, ProgressTracking.Instance.firstLaunch, "", firstLaunchDescriptor);
			addProgressStandard(ProgressType.REACHSPACE, ProgressTracking.Instance.reachSpace, "", reachSpaceDescriptor, StandardNote, vesselNameFromNode(ProgressTracking.Instance.reachSpace));
			addProgressStandard(ProgressType.STUNT, ProgressTracking.Instance.towerBuzz, ProgressTracking.Instance.towerBuzz.Id, towerBuzzDescriptor, StandardNote, vesselNameFromNode(ProgressTracking.Instance.towerBuzz));
			addProgressStandard(ProgressType.STUNT, ProgressTracking.Instance.KSCLanding, ProgressTracking.Instance.KSCLanding.Id, launchpadDescriptor);
			addProgressStandard(ProgressType.STUNT, ProgressTracking.Instance.runwayLanding, ProgressTracking.Instance.runwayLanding.Id, runwayDescriptor);
		}

		private static void loadPOINodes()
		{
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIBopDeadKraken, ProgressTracking.Instance.POIBopDeadKraken.Id, POIBopKrakenDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIBopDeadKraken));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIDunaFace, ProgressTracking.Instance.POIDunaFace.Id, POIDunaFaceDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIDunaFace));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIDunaMSL, ProgressTracking.Instance.POIDunaMSL.Id, POIDunaMSLDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIDunaMSL));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIDunaPyramid, ProgressTracking.Instance.POIDunaPyramid.Id, POIDunaPyramidDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIDunaPyramid));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIKerbinIslandAirfield, ProgressTracking.Instance.POIKerbinIslandAirfield.Id, POIKerbinIslandAirfieldDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIKerbinIslandAirfield));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIKerbinKSC2, ProgressTracking.Instance.POIKerbinKSC2.Id, POIKerbinKSC2Descriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIKerbinKSC2));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIKerbinMonolith00, ProgressTracking.Instance.POIKerbinMonolith00.Id, POIKerbinMonolith1Descriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIKerbinMonolith00));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIKerbinMonolith01, ProgressTracking.Instance.POIKerbinMonolith01.Id, POIKerbinMonolith2Descriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIKerbinMonolith01));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIKerbinMonolith02, ProgressTracking.Instance.POIKerbinMonolith02.Id, POIKerbinMonolith3Descriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIKerbinMonolith02));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIKerbinPyramids, ProgressTracking.Instance.POIKerbinPyramids.Id, POIKerbinPyramidsDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIKerbinPyramids));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIKerbinUFO, ProgressTracking.Instance.POIKerbinUFO.Id, POIKerbinUFODescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIKerbinUFO));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIMinmusMonolith00, ProgressTracking.Instance.POIMinmusMonolith00.Id, POIMinmusMonolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIMinmusMonolith00));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIMunArmstrongMemorial, ProgressTracking.Instance.POIMunArmstrongMemorial.Id, POIMunArmstrongDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIMunArmstrongMemorial));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIMunMonolith00, ProgressTracking.Instance.POIMunMonolith00.Id, POIMunMonolith1Descriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIMunMonolith00));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIMunMonolith01, ProgressTracking.Instance.POIMunMonolith01.Id, POIMunMonolith2Descriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIMunMonolith01));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIMunMonolith02, ProgressTracking.Instance.POIMunMonolith02.Id, POIMunMonolith3Descriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIMunMonolith02));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIMunRockArch00, ProgressTracking.Instance.POIMunRockArch00.Id, POIMunRockArch1Descriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIMunRockArch00));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIMunRockArch01, ProgressTracking.Instance.POIMunRockArch01.Id, POIMunRockArch2Descriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIMunRockArch01));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIMunRockArch02, ProgressTracking.Instance.POIMunRockArch02.Id, POIMunRockArch3Descriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIMunRockArch02));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIMunUFO, ProgressTracking.Instance.POIMunUFO.Id, POIMunUFODescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIMunUFO));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POITyloCave, ProgressTracking.Instance.POITyloCave.Id, POITyloCaveDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POITyloCave));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIVallIcehenge, ProgressTracking.Instance.POIVallIcehenge.Id, POIValIceHengeDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIVallIcehenge));
		}

		private static void loadNextBodyNode(CelestialBodySubtree b)
		{
			if (b == null)
				return;

			if (b.Body == null)
				return;

			addBodySubTree(b);
		}

		public static void updateCompletionRecord()
		{
			anyInterval = intervalNodes.Values.Any(n => n.IsReached);
			anyStandard = standardNodes.Values.Any(n => n.IsComplete);
			anyPOI = pointsOfInterest.Values.Any(n => n.IsComplete);
			foreach (progressBodyCollection b in bodySubTrees.Values)
				b.UpdateReachedStatus();
			anyBody = bodySubTrees.Values.Any(n => n.IsReached);
		}

		private static void addProgressInterval(ProgressType p, ProgressNode n, string d = "")
		{
			if (n == null)
				return;

			if (intervalNodes.ContainsKey(n.Id))
				return;

			progressInterval i = null;
			
			switch (p)
			{
				case ProgressType.ALTITUDERECORD:
					i = new progressInterval(ProgressType.ALTITUDERECORD, n, ((RecordsAltitude)n).record, getMaxAltitudeRecord, 500, ContractDefs.Progression.RecordSplit, d);
					altitude = i;
					break;
				case ProgressType.DEPTHRECORD:
					i = new progressInterval(ProgressType.DEPTHRECORD, n, ((RecordsDepth)n).record, ContractDefs.Progression.MaxDepthRecord, 10, ContractDefs.Progression.RecordSplit, d);
					depth = i;
					break;
				case ProgressType.DISTANCERECORD:
					i = new progressInterval(ProgressType.DISTANCERECORD, n, ((RecordsDistance)n).record, ContractDefs.Progression.MaxDistanceRecord, 1000, ContractDefs.Progression.RecordSplit, d);
					distance = i;
					break;
				case ProgressType.SPEEDRECORD:
					i = new progressInterval(ProgressType.SPEEDRECORD, n, ((RecordsSpeed)n).record, ContractDefs.Progression.MaxSpeedRecord, 5, ContractDefs.Progression.RecordSplit, d);
					speed = i;
					break;
				default:
					return;
			}

			if (i == null)
				return;

			intervalNodes.Add(n.Id, i);
		}

		private static double getMaxAltitudeRecord
		{
			get
			{
				CelestialBody b = FlightGlobals.GetHomeBody();

				if (b == null)
					return 70000;

				return b.atmosphereDepth;
			}
		}

		private static void addProgressStandard(ProgressType p, ProgressNode n, string id = "", string d = "", string g = "", string r = "")
		{
			if (n == null)
				return;

			if (standardNodes.ContainsKey(n.Id))
				return;

			progressStandard s = null;

			switch (p)
			{
				case ProgressType.CREWRECOVERY:
					s = new progressStandard(null, ProgressType.CREWRECOVERY, n, d, g, r);
					crewRecovery = s;
					standardNodes.Add(n.Id, s);
					break;
				case ProgressType.FIRSTLAUNCH:
					s = new progressStandard(null, ProgressType.FIRSTLAUNCH, n, d, r);
					firstLaunch = s;
					standardNodes.Add(n.Id, s);
					break;
				case ProgressType.REACHSPACE:
					s= new progressStandard(null, ProgressType.REACHSPACE, n, d, g, r);
					reachSpace = s;
					standardNodes.Add(n.Id, s);
					break;
				case ProgressType.STUNT:					
					addStunt(n, d, id, g, r);
					break;
				case ProgressType.POINTOFINTEREST:
					addPointOfInterest(n, d, id, g, r);
					break;
				default:
					return;
			}
		}

		public static void addStunt(ProgressNode n, string d, string id, string note, string r = "")
		{
			progressStandard s = null;

			if (id == ProgressTracking.Instance.runwayLanding.Id)
			{
				s = new progressStandard(null, ProgressType.STUNT, n, d);
				runwayLanding = s;
			}
			else if (id == ProgressTracking.Instance.KSCLanding.Id)
			{
				s = new progressStandard(null, ProgressType.STUNT, n, d);
				launchpadLanding = s;
			}
			else if (id == ProgressTracking.Instance.towerBuzz.Id)
			{
				s = new progressStandard(null, ProgressType.STUNT, n, d, note, r);
				towerBuzz = s;
			}

			if (s == null)
				return;

			standardNodes.Add(n.Id, s);
		}

		public static void addPointOfInterest(ProgressNode n, string d, string name, string note, string r = "")
		{
			if (pointsOfInterest.ContainsKey(name))
				return;

			progressStandard s = new progressStandard(null, ProgressType.POINTOFINTEREST, n, d, note, r);

			pointsOfInterest.Add(name, s);
		}

		public static void addBodySubTree(CelestialBodySubtree body)
		{
			if (body.Body == null)
				return;

			if (bodySubTrees.ContainsKey(body.Body.theName))
				return;			

			bodySubTrees.Add(body.Body.theName, new progressBodyCollection(body));
		}

		public static VesselRef vesselFromNode(ProgressNode n)
		{
			Type t = n.GetType();

			try
			{
				if (t == typeof(BaseConstruction))
					return (VesselRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[1].GetValue(n);
				else if (t == typeof(CelestialBodyFlight))
					return ((CelestialBodyFlight)n).firstVessel;
				else if (t == typeof(CelestialBodyLanding))
					return (VesselRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[1].GetValue(n);
				else if (t == typeof(CelestialBodyOrbit))
					return ((CelestialBodyOrbit)n).firstVessel;
				else if (t == typeof(CelestialBodySuborbit))
					return ((CelestialBodySuborbit)n).firstVessel;
				else if (t == typeof(CelestialBodyReturn))
					return ((CelestialBodyReturn)n).firstVessel;
				else if (t == typeof(CelestialBodyScience))
					return (VesselRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[1].GetValue(n);
				else if (t == typeof(StationConstruction))
					return (VesselRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[1].GetValue(n);
				else if (t == typeof(ReachSpace))
					return ((ReachSpace)n).firstVessel;
				else if (t == typeof(PointOfInterest))
					return (VesselRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[3].GetValue(n);
				else if (t == typeof(TowerBuzz))
					return (VesselRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[3].GetValue(n);
			}
			catch (Exception e)
			{
				Debug.LogWarning("[Progress Tracking Parser] Error In Finding Progress Node Vessel Reference\n" + e);
			}			

			return null;
		}

		public static string vesselNameFromNode(ProgressNode n)
		{
			VesselRef v = vesselFromNode(n);

			if (v == null)
				return "";

			return v.Name;
		}

		public static CrewRef crewFromNode(ProgressNode n)
		{
			Type t = n.GetType();

			try
			{				
				if (t == typeof(FlagPlant))
					return (CrewRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[1].GetValue(n);
				else if (t == typeof(Spacewalk))
					return (CrewRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[1].GetValue(n);
				else if (t == typeof(SurfaceEVA))
					return (CrewRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[1].GetValue(n);
				else if (t == typeof(CrewRecovery))
					return (CrewRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
			}
			catch (Exception e)
			{
				Debug.LogWarning("[Progress Tracking Parser] Error In Finding Progress Node Crew Reference\n" + e);
			}

			return null;
		}

		public static string crewNameFromNode(ProgressNode n)
		{
			CrewRef c = crewFromNode(n);

			if (c == null)
				return "";

			if (c.Crews.Count <= 0)
				return "";

			StringBuilder s = new StringBuilder();
			
			for (int i = 0; i < c.Crews.Count; i++)
			{
				s.Append(c.Crews[i].name);

				if (i < c.Crews.Count - 2)
					s.Append(", ");
				else if (i == c.Crews.Count - 2)
					s.Append(", and ");
			}

			return s.ToString();
		}

		public static progressBodyCollection getProgressBody(CelestialBody b)
		{
			if (b == null)
				return null;

			if (bodySubTrees.ContainsKey(b.theName))
				return bodySubTrees[b.theName];

			return null;
		}

		public static progressBodyCollection getProgressBody(string bodyName)
		{
			if (bodySubTrees.ContainsKey(bodyName))
				return bodySubTrees[bodyName];

			return null;
		}

		public static List<progressBodyCollection> getAllBodyNodes
		{
			get { return bodySubTrees.Values.ToList(); }
		}

		public static progressStandard getPOINode(string name)
		{
			if (pointsOfInterest.ContainsKey(name))
				return pointsOfInterest[name];

			return null;
		}

		public static progressStandard getStandardNode(string id)
		{
			if (standardNodes.ContainsKey(id))
				return standardNodes[id];

			return null;
		}

		public static progressInterval getIntervalNode(string id)
		{
			if (intervalNodes.ContainsKey(id))
				return intervalNodes[id];

			return null;
		}

		public static List<progressStandard> getAllPOINodes
		{
			get { return pointsOfInterest.Values.ToList(); }
		}

		public static List<progressStandard> getAllStandardNodes
		{
			get { return standardNodes.Values.ToList(); }
		}

		public static List<progressInterval> getAllIntervalNodes
		{
			get { return intervalNodes.Values.ToList(); }
		}

		public static bool AnyInterval
		{
			get { return anyInterval; }
		}

		public static bool AnyStandard
		{
			get { return anyStandard; }
		}

		public static bool AnyPOI
		{
			get { return anyPOI; }
		}

		public static bool AnyBody
		{
			get { return anyBody; }
		}

		public static bool Loading
		{
			get { return loading; }
		}

		public static progressInterval Altitude
		{
			get { return altitude; }
		}

		public static progressInterval Depth
		{
			get { return depth; }
		}

		public static progressInterval Distance
		{
			get { return distance; }
		}

		public static progressInterval Speed
		{
			get { return speed; }
		}

		public static progressStandard CrewRecovery
		{
			get { return crewRecovery; }
		}

		public static progressStandard FirstLaunch
		{
			get { return firstLaunch; }
		}

		public static progressStandard ReachSpace
		{
			get { return reachSpace; }
		}

		public static progressStandard RunwayLanding
		{
			get { return runwayLanding; }
		}

		public static progressStandard LaunchpadLanding
		{
			get { return launchpadLanding; }
		}

		public static progressStandard TowerBuzz
		{
			get { return towerBuzz; }
		}
	}
}
