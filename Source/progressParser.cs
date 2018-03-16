#region license
/*The MIT License (MIT)
Progress Parser - A static class responsible for parsing the stock progress tree and caching relevant values
Copyright (c) 2016 DMagic

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

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
using KSP.Localization;

namespace ProgressParser
{
	public static class progressParser
	{
		//Store a list of each celestial body sub node; accessible by the celestial body name
		private static DictionaryValueList<string, progressBodyCollection> bodySubTrees = new DictionaryValueList<string, progressBodyCollection>();

		//Store a list of each progress node type; accessible by the progress node ID
		private static DictionaryValueList<string, progressStandard> pointsOfInterest = new DictionaryValueList<string, progressStandard>();
		private static DictionaryValueList<string, progressInterval> intervalNodes = new DictionaryValueList<string, progressInterval>();
		private static DictionaryValueList<string, progressStandard> standardNodes = new DictionaryValueList<string, progressStandard>();

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

		public static EventVoid onProgressParsed = new EventVoid("onProgressParsed");

		//A series of simple string descriptions for each progress node; the body-specific nodes substitute the body's name for the {0}
		public static string altitudeTitle = "#autoLOC_463493"; // "Altitude";
		public static string speedTitle = "#autoLOC_900381"; //"Speed";
		public static string depthTitle = "#autoLOC_ProgressParser_Depth";
		public static string distanceTitle = "#autoLOC_196906"; //"Distance";  //#autoLOC_196906 = Distance: <<1>>m

		public static string altitudeDescriptor = "#autoLOC_297833";
		public static string speedDescriptor = "#autoLOC_298174";
		public static string depthDescriptor = "#autoLOC_297943";
		public static string distanceDescriptor = "#autoLOC_298052";

		public static string altitudeUnits = "m";
		public static string speedUnits = "m/s";
		public static string depthUnits = "m";
		public static string distanceUnits = "m";

		public static string crewRecoveryDescriptor = "#autoLOC_296238"; // = We have recovered our first crew from a mission. "Recovered First Crew";
		public static string firstLaunchDescriptor = "#autoLOC_296348"; // = We have launched our first vessel. "First Launch";
		public static string reachSpaceDescriptor = "#autoLOC_297725"; //= We have reached space. "Reached Space";
		public static string runwayDescriptor = "";  //"Landed On The Runway";
		public static string kscDescriptor = ""; //"Landed At The KSC";
		public static string launchpadDescriptor = ""; // "Landed On The Launchpad";
		public static string towerBuzzDescriptor = "#autoLOC_298640"; //= You have buzzed the tower at the space center! "Buzzed The Tower";

		public static string flybyDescriptor = "#autoLOC_295360"; //  = We have initiated the first fly by of <<1>>.. "Conducted A Fly By Of {0}";
		public static string orbitDescriptor = "#autoLOC_6001940"; // We have entered orbit of <<1>>."Entered Orbit Around {0}";
		public static string landingDescriptor = "#autoLOC_6001944";  // We have landed on the surface of <<1>>."Landed On {0}";
		public static string escapeDescriptor = "#autoLOC_295242";// = We have escaped the gravitational influence of <<1>>. "Escaped {0}'s Gravity";
		public static string scienceDescriptor = "#autoLOC_6001950"; // We have gathered the first scientific data from <<1>>."Collected Science From {0}";
		public static string flightDescriptor = "#autoLOC_6001938"; //  We have entered into atmospheric flight above <<1>>."Flew In {0}'s Atmosphere";
		public static string returnLandingDescriptor = "#autoLOC_295693";// = We have returned home from the surface of <<1>>."Returned From The Surface Of {0}";
		public static string returnFlybyDescriptor = "#autoLOC_295665";// = We have returned home from a fly by of <<1>>."Returned After A Fly By Of {0}";
		public static string returnOrbitDescriptor = "#autoLOC_300432"; //= Recovery of a vessel returned from <<1>> orbit "Returned From Orbit Around {0}";
		public static string splashdownDescriptor = "#autoLOC_6001946"; //We have splashed down in the oceans of <<1>>. "Splashed Down On {0}";
		public static string suborbitDescriptor = "#autoLOC_6001942"; //We have entered into suborbital spaceflight above <<1>>. "Entered A Sub-Orbital Trajectory At {0}";
		public static string crewTransferDescriptor = "#autoLOC_296191";// = We have performed a crew transfer near <<1>>."Transferred Crew At {0}";
		public static string dockingDescriptor = "#autoLOC_296294";// = We have performed a docking maneuver on <<1>>. "Perfomed A Docking At {0}";
		public static string flagDescriptor = "#autoLOC_6001954"; //We have planted a flag on <<1>>. "Planted A Flag On {0}";
		public static string spacewalkDescriptor = "#autoLOC_298311";// = We have performed a spacewalk in orbit of <<1>>. "Conducted A Space Walk At {0}";
		public static string EVADescriptor = "#autoLOC_6001956"; //We have walked on the surface of <<1>>. "Performed A Surface EVA On {0}";
		public static string stationDescriptor = "#autoLOC_298375";// = We have started staticructing the first station around <<1>>. "staticruced A Space Station At {0}";
		public static string baseDescriptor = "#autoLOC_295188";// = We have started staticructing the first outpost on <<1>>.
		public static string rendezvousDescriptor = "#autoLOC_298266";// = We have performed a rendezvous maneuver around <<1>>. "Performed A Rendezvous At {0}";

		public static string POIBopKrakenDescriptor = "#autoLOC_297154";
		public static string POIDunaFaceDescriptor = "#autoLOC_297150";
		public static string POIDunaMSLDescriptor = "#autoLOC_297149";
		public static string POIDunaPyramidDescriptor = "#autoLOC_297148";
		public static string POIKerbinIslandAirfieldDescriptor = "#autoLOC_297134";
		public static string POIKerbinKSC2Descriptor = "#autoLOC_297133";
		public static string POIKerbinMonolith1Descriptor = "#autoLOC_297137";
		public static string POIKerbinMonolith2Descriptor = "#autoLOC_297138";
		public static string POIKerbinMonolith3Descriptor = "#autoLOC_297139";
		public static string POIKerbinPyramidsDescriptor = "#autoLOC_297136";
		public static string POIKerbinUFODescriptor = "#autoLOC_297135";
		public static string POIMinmusMonolithDescriptor = "#autoLOC_297151";
		public static string POIMunArmstrongDescriptor = "#autoLOC_297140";
		public static string POIMunMonolith1Descriptor = "#autoLOC_297145";
		public static string POIMunMonolith2Descriptor = "#autoLOC_297146";
		public static string POIMunMonolith3Descriptor = "#autoLOC_297147";
		public static string POIMunRockArch1Descriptor = "#autoLOC_297142";
		public static string POIMunRockArch2Descriptor = "#autoLOC_297143";
		public static string POIMunRockArch3Descriptor = "#autoLOC_297144";
		public static string POIMunUFODescriptor = "#autoLOC_297141";
		public static string POITyloCaveDescriptor = "#autoLOC_297152";
		public static string POIValIceHengeDescriptor = "#autoLOC_297153";
		public static string POIRandolithDescriptor = "#autoLOC_297156";

		//Notes for completed progress nodes where available; the {0} is replaced by the vessel or crew involved; the {1} is replaced by the date of completion
		public static string StandardNote = "#autoLOC_ProgressParser_StandardNote";
		public static string CrewNote = "#autoLOC_ProgressParser_CrewNote";
		public static string RecoveryNote = "#autoLOC_ProgressParser_RecoveryNote";
		public static string FlagNote = "#autoLOC_ProgressParser_FlagNote";
		public static string FacilityNote = "#autoLOC_ProgressParser_FacilityNote";
		public static string POINote = "#autoLOC_ProgressParser_POINote";

		private static bool loaded;

		public static void initialize()
		{
			initializeStrings();

			progressController.instance.StartCoroutine(parseProgressTree());
		}

		private static void initializeStrings()
		{
			altitudeTitle = Localizer.Format(altitudeTitle);
			speedTitle = Localizer.Format(speedTitle);
			depthTitle = Localizer.Format(depthTitle);
			distanceTitle = Localizer.Format(distanceTitle);

			int colon = distanceTitle.LastIndexOf(':');

			if (colon == -1)
				colon = distanceTitle.LastIndexOf('：');

			if (colon > 0)
				distanceTitle = distanceTitle.Substring(0, colon);

			POIBopKrakenDescriptor = Localizer.Format(POIBopKrakenDescriptor);
			POIDunaFaceDescriptor = Localizer.Format(POIDunaFaceDescriptor);
			POIDunaMSLDescriptor = Localizer.Format(POIDunaMSLDescriptor);
			POIDunaPyramidDescriptor = Localizer.Format(POIDunaPyramidDescriptor);
			POIKerbinIslandAirfieldDescriptor = Localizer.Format(POIKerbinIslandAirfieldDescriptor);
			POIKerbinKSC2Descriptor = Localizer.Format(POIKerbinKSC2Descriptor);
			POIKerbinMonolith1Descriptor = Localizer.Format(POIKerbinMonolith1Descriptor);
			POIKerbinMonolith2Descriptor = Localizer.Format(POIKerbinMonolith2Descriptor);
			POIKerbinMonolith3Descriptor = Localizer.Format(POIKerbinMonolith3Descriptor);
			POIKerbinPyramidsDescriptor = Localizer.Format(POIKerbinPyramidsDescriptor);
			POIKerbinUFODescriptor = Localizer.Format(POIKerbinUFODescriptor);
			POIMinmusMonolithDescriptor = Localizer.Format(POIMinmusMonolithDescriptor);
			POIMunArmstrongDescriptor = Localizer.Format(POIMunArmstrongDescriptor);
			POIMunMonolith1Descriptor = Localizer.Format(POIMunMonolith1Descriptor);
			POIMunMonolith2Descriptor = Localizer.Format(POIMunMonolith2Descriptor);
			POIMunMonolith3Descriptor = Localizer.Format(POIMunMonolith3Descriptor);
			POIMunRockArch1Descriptor = Localizer.Format(POIMunRockArch1Descriptor);
			POIMunRockArch2Descriptor = Localizer.Format(POIMunRockArch2Descriptor);
			POIMunRockArch3Descriptor = Localizer.Format(POIMunRockArch3Descriptor);
			POIMunUFODescriptor = Localizer.Format(POIMunUFODescriptor);
			POITyloCaveDescriptor = Localizer.Format(POITyloCaveDescriptor);
			POIValIceHengeDescriptor = Localizer.Format(POIValIceHengeDescriptor);
			POIRandolithDescriptor = Localizer.Format(POIRandolithDescriptor);
		}

		private static IEnumerator parseProgressTree()
		{
			loaded = false;

			int timer = 0;

			while (ProgressTracking.Instance == null && timer < 500)
			{
				timer++;
				yield return null;
			}

			if (timer >= 500)
			{
				Debug.Log("[Progress Tracking Parser] Progress Parser Timed Out");
				loaded = false;
				yield break;
			}

			while (timer < 10)
			{
				timer++;
				yield return null;
			}

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

			updateCompletionRecord();

			loaded = true;

			onProgressParsed.Fire();

			Debug.Log("[Progress Tracking Parser] Progress Nodes Loaded...");
		}

		private static void loadIntervalNodes()
		{
			addProgressInterval(ProgressType.ALTITUDERECORD, ProgressTracking.Instance.altitudeRecords, altitudeDescriptor, altitudeTitle, altitudeUnits);
			addProgressInterval(ProgressType.SPEEDRECORD, ProgressTracking.Instance.speedRecords, speedDescriptor, speedTitle, speedUnits);
			addProgressInterval(ProgressType.DISTANCERECORD, ProgressTracking.Instance.distanceRecords, distanceDescriptor, distanceTitle, distanceUnits);
			addProgressInterval(ProgressType.DEPTHRECORD, ProgressTracking.Instance.depthRecords, depthDescriptor, depthTitle, depthUnits);
		}

		private static void loadStandardNodes()
		{
			addProgressStandard(ProgressType.CREWRECOVERY, ProgressTracking.Instance.firstCrewToSurvive, "", crewRecoveryDescriptor, RecoveryNote, crewNameFromNode(ProgressTracking.Instance.firstCrewToSurvive));
			addProgressStandard(ProgressType.FIRSTLAUNCH, ProgressTracking.Instance.firstLaunch, "", firstLaunchDescriptor);
			addProgressStandard(ProgressType.REACHSPACE, ProgressTracking.Instance.reachSpace, "", reachSpaceDescriptor, StandardNote, vesselNameFromNode(ProgressTracking.Instance.reachSpace));
			addProgressStandard(ProgressType.STUNT, ProgressTracking.Instance.towerBuzz, ProgressTracking.Instance.towerBuzz.Id, towerBuzzDescriptor, StandardNote, vesselNameFromNode(ProgressTracking.Instance.towerBuzz));
			addProgressStandard(ProgressType.STUNT, ProgressTracking.Instance.KSCLanding, ProgressTracking.Instance.KSCLanding.Id, kscDescriptor);
			addProgressStandard(ProgressType.STUNT, ProgressTracking.Instance.launchpadLanding, ProgressTracking.Instance.launchpadLanding.Id, launchpadDescriptor);
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
			
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIBopRandolith, ProgressTracking.Instance.POIBopRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIBopRandolith));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIDresRandolith, ProgressTracking.Instance.POIDresRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIDresRandolith));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIDunaRandolith, ProgressTracking.Instance.POIDunaRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIDunaRandolith));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIEelooRandolith, ProgressTracking.Instance.POIEelooRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIEelooRandolith));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIEveRandolith, ProgressTracking.Instance.POIEveRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIEveRandolith));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIGillyRandolith, ProgressTracking.Instance.POIGillyRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIGillyRandolith));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIIkeRandolith, ProgressTracking.Instance.POIIkeRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIIkeRandolith));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIKerbinRandolith, ProgressTracking.Instance.POIKerbinRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIKerbinRandolith));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POILaytheRandolith, ProgressTracking.Instance.POILaytheRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POILaytheRandolith));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIMinmusRandolith, ProgressTracking.Instance.POIMinmusRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIMinmusRandolith));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIMohoRandolith, ProgressTracking.Instance.POIMohoRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIMohoRandolith));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIMunRandolith, ProgressTracking.Instance.POIMunRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIMunRandolith));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIPolRandolith, ProgressTracking.Instance.POIPolRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIPolRandolith));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POITyloRandolith, ProgressTracking.Instance.POITyloRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POITyloRandolith));
			addProgressStandard(ProgressType.POINTOFINTEREST, ProgressTracking.Instance.POIVallRandolith, ProgressTracking.Instance.POIVallRandolith.Id, POIRandolithDescriptor, POINote, vesselNameFromNode(ProgressTracking.Instance.POIVallRandolith));
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

		private static void addProgressInterval(ProgressType p, ProgressNode n, string d, string t, string u)
		{
			if (n == null)
				return;

			if (intervalNodes.Contains(n.Id))
				return;

			progressInterval i = null;
			
			switch (p)
			{
				case ProgressType.ALTITUDERECORD:
					i = new progressInterval(ProgressType.ALTITUDERECORD, n, ((RecordsAltitude)n).record, getMaxAltitudeRecord, 500, ContractDefs.Progression.RecordSplit, d, t, u);
					altitude = i;
					break;
				case ProgressType.DEPTHRECORD:
					i = new progressInterval(ProgressType.DEPTHRECORD, n, ((RecordsDepth)n).record, ContractDefs.Progression.MaxDepthRecord, 10, ContractDefs.Progression.RecordSplit, d, t, u);
					depth = i;
					break;
				case ProgressType.DISTANCERECORD:
					i = new progressInterval(ProgressType.DISTANCERECORD, n, ((RecordsDistance)n).record, ContractDefs.Progression.MaxDistanceRecord, 1000, ContractDefs.Progression.RecordSplit, d, t, u);
					distance = i;
					break;
				case ProgressType.SPEEDRECORD:
					i = new progressInterval(ProgressType.SPEEDRECORD, n, ((RecordsSpeed)n).record, ContractDefs.Progression.MaxSpeedRecord, 5, ContractDefs.Progression.RecordSplit, d, t, u);
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

			if (standardNodes.Contains(n.Id))
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
			else if (id == ProgressTracking.Instance.launchpadLanding.Id)
			{
				s = new progressStandard(null, ProgressType.STUNT, n, d);
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
			if (pointsOfInterest.Contains(name))
				return;

			progressStandard s = new progressStandard(null, ProgressType.POINTOFINTEREST, n, d, note, r);

			pointsOfInterest.Add(name, s);
		}

		public static void addBodySubTree(CelestialBodySubtree body)
		{
			if (body == null)
				return;

			if (body.Body == null)
				return;

			if (bodySubTrees.Contains(body.Body.displayName.LocalizeBodyName()))
				return;

			bodySubTrees.Add(body.Body.displayName.LocalizeBodyName(), new progressBodyCollection(body));
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
                    return (VesselRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[4].GetValue(n);
                else if (t == typeof(TowerBuzz))
                    return (VesselRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[3].GetValue(n);
			}
			catch (Exception e)
			{
				Debug.LogWarning("[Progress Tracking Parser] Error In Finding Progress Node [" + t.Name + "] Vessel Reference\n" + e);
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
				//if (t == typeof(FlagPlant))
				//	return (CrewRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[1].GetValue(n);
				if (t == typeof(Spacewalk))
					return (CrewRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[1].GetValue(n);
				else if (t == typeof(SurfaceEVA))
					return (CrewRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[1].GetValue(n);
				else if (t == typeof(CrewRecovery))
					return (CrewRef)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
			}
			catch (Exception e)
			{
				Debug.LogWarning("[Progress Tracking Parser] Error In Finding Progress Node [" + t.Name + "] Crew Reference\n" + e);
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

			StringBuilder s = StringBuilderCache.Acquire();
			
			for (int i = 0; i < c.Crews.Count; i++)
			{
				s.Append(c.Crews[i].name);

				if (i < c.Crews.Count - 2)
					s.Append(", ");
				else if (i == c.Crews.Count - 2)
					s.Append(", and ");
			}

			return s.ToStringAndRelease();
		}

		public static double getIntervalRecord(ProgressNode n)
		{
			string descr = "";
			return getIntervalRecord(n, ref descr);
		}

		public static double getIntervalRecord(ProgressNode n, ref string descr)
		{
			Type t = n.GetType();

			if (t == typeof(RecordsAltitude))
			{
				descr = progressParser.altitudeTitle;
				return ((RecordsAltitude)n).record;
			}
			else if (t == typeof(RecordsDepth))
			{
				descr = progressParser.depthTitle;
				return ((RecordsDepth)n).record;
			}
			else if (t == typeof(RecordsDistance))
			{
				descr = progressParser.distanceTitle;
				return ((RecordsDistance)n).record;
			}
			else if (t == typeof(RecordsSpeed))
			{
				descr = progressParser.speedTitle;
				return ((RecordsSpeed)n).record;
			}
			descr = "";
			return 0;
		}

		public static bool isIntervalType(ProgressNode n)
		{
			Type t = n.GetType();

			if (t == typeof(RecordsAltitude))
				return true;
			else if (t == typeof(RecordsDepth))
				return true;
			else if (t == typeof(RecordsDistance))
				return true;
			else if (t == typeof(RecordsSpeed))
				return true;

			return false;
		}

		public static bool isPOI(ProgressNode n)
		{
			if (n.GetType() == typeof(PointOfInterest))
				return true;

			return false;
		}

		public static CelestialBody getBodyFromType(ProgressNode n)
		{
			Type t = n.GetType();

			try
			{
				if (t == typeof(BaseConstruction))
					return (CelestialBody)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
				else if (t == typeof(CelestialBodyEscape))
					return (CelestialBody)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
				else if (t == typeof(CelestialBodyFlight))
					return ((CelestialBodyFlight)n).body;
				else if (t == typeof(CelestialBodyFlyby))
					return (CelestialBody)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
				else if (t == typeof(CelestialBodyLanding))
					return (CelestialBody)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
				else if (t == typeof(CelestialBodyOrbit))
					return ((CelestialBodyOrbit)n).body;
				else if (t == typeof(CelestialBodySuborbit))
					return ((CelestialBodySuborbit)n).body;
				else if (t == typeof(CelestialBodyReturn))
					return (CelestialBody)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
				else if (t == typeof(CelestialBodyScience))
					return (CelestialBody)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
				else if (t == typeof(CelestialBodySplashdown))
					return (CelestialBody)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
				else if (t == typeof(CelestialBodyTransfer))
					return (CelestialBody)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
				else if (t == typeof(Docking))
					return (CelestialBody)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
				else if (t == typeof(FlagPlant))
					return (CelestialBody)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
				else if (t == typeof(Rendezvous))
					return (CelestialBody)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
				else if (t == typeof(Spacewalk))
					return (CelestialBody)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
				else if (t == typeof(StationConstruction))
					return (CelestialBody)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
				else if (t == typeof(SurfaceEVA))
					return (CelestialBody)t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue(n);
			}
			catch (Exception e)
			{
				Debug.LogWarning("[Progress Tracking Parser] Error In Finding Progress Node [" + t.Name + "] Celestial Body Reference\n" + e);
			}

			return null;
		}

		public static string LocalizeBodyName(this string input)
		{
			return Localizer.Format("<<1>>", input);
		}

		public static progressBodyCollection getProgressBody(CelestialBody b)
		{
			if (b == null)
				return null;

			if (bodySubTrees.Contains(b.displayName.LocalizeBodyName()))
				return bodySubTrees[b.displayName.LocalizeBodyName()];

			return null;
		}

		public static progressBodyCollection getProgressBody(string bodyName)
		{
			if (bodySubTrees.Contains(bodyName))
				return bodySubTrees[bodyName];

			return null;
		}

		public static List<progressBodyCollection> getAllBodyNodes
		{
			get { return bodySubTrees.Values.ToList(); }
		}

		public static progressStandard getPOINode(string name)
		{
			if (pointsOfInterest.Contains(name))
				return pointsOfInterest[name];

			return null;
		}

		public static progressStandard getStandardNode(string id)
		{
			if (standardNodes.Contains(id))
				return standardNodes[id];

			return null;
		}

		public static progressInterval getIntervalNode(string id)
		{
			if (intervalNodes.Contains(id))
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

		public static bool Loaded
		{
			get { return loaded; }
			internal set { loaded = value; }
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
