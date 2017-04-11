#region license
/*The MIT License (MIT)
Progress Body Collection - A storage object for holding all body-specific progress nodes

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
using System.Collections.Generic;
using System.Linq;
using FinePrint.Utilities;
using KSPAchievements;
using UnityEngine;

namespace ProgressParser
{
	public class progressBodyCollection
	{
		//Store a list of each progress node type; accessible by the progress node ID
		private DictionaryValueList<string, progressStandard> bodyNodes = new DictionaryValueList<string, progressStandard>();

		//Store each body-specific progress node type here
		private progressStandard flyby;
		private progressStandard orbit;
		private progressStandard landing;
		private progressStandard escape;
		private progressStandard science;
		private progressStandard flight;
		private progressStandard landingReturn;
		private progressStandard flybyReturn;
		private progressStandard orbitReturn;
		private progressStandard splashdown;
		private progressStandard suborbit;
		private progressStandard transfer;
		private progressStandard docking;
		private progressStandard flag;
		private progressStandard spacewalk;
		private progressStandard surfaceEVA;
		private progressStandard station;
		private progressStandard baseConstruct;
		private progressStandard rendezvous;

		private bool homeworld = false;
		private CelestialBody body;
		private bool isReached;

		public progressBodyCollection() { }

		public progressBodyCollection(CelestialBodySubtree b)
		{
			body = b.Body;

			if (b.Body.isHomeWorld)
			{
				homeworld = true;
			}
			else
			{
				addProgressStandard(ProgressType.FLYBY, b.Body, b.flyBy, progressParser.flybyDescriptor);
				addProgressStandard(ProgressType.FLYBYRETURN, b.Body, b.returnFromFlyby, progressParser.returnFlybyDescriptor, progressParser.RecoveryNote, progressParser.vesselNameFromNode(b.returnFromFlyby));
				addProgressStandard(ProgressType.LANDINGRETURN, b.Body, b.returnFromSurface, progressParser.returnLandingDescriptor, progressParser.RecoveryNote, progressParser.vesselNameFromNode(b.returnFromSurface));
			}

			addProgressStandard(ProgressType.FLIGHT, b.Body, b.flight, progressParser.flightDescriptor, progressParser.StandardNote, progressParser.vesselNameFromNode(b.flight));
			addProgressStandard(ProgressType.SUBORBIT, b.Body, b.suborbit, progressParser.suborbitDescriptor, progressParser.StandardNote, progressParser.vesselNameFromNode(b.suborbit));
			addProgressStandard(ProgressType.ESCAPE, b.Body, b.escape, progressParser.escapeDescriptor);
			addProgressStandard(ProgressType.BASECONSTRUCTION, b.Body, b.baseConstruction, progressParser.baseDescriptor, progressParser.FacilityNote, progressParser.vesselNameFromNode(b.baseConstruction));
			addProgressStandard(ProgressType.CREWTRANSFER, b.Body, b.crewTransfer, progressParser.crewTransferDescriptor);
			addProgressStandard(ProgressType.DOCKING, b.Body, b.docking, progressParser.dockingDescriptor);
			addProgressStandard(ProgressType.FLAGPLANT, b.Body, b.flagPlant, progressParser.flagDescriptor);
			addProgressStandard(ProgressType.LANDING, b.Body, b.landing, progressParser.landingDescriptor, progressParser.StandardNote, progressParser.vesselNameFromNode(b.landing));
			addProgressStandard(ProgressType.ORBIT, b.Body, b.orbit, progressParser.orbitDescriptor, progressParser.StandardNote, progressParser.vesselNameFromNode(b.orbit));
			addProgressStandard(ProgressType.ORBITRETURN, b.Body, b.returnFromOrbit, progressParser.returnOrbitDescriptor, progressParser.RecoveryNote, progressParser.vesselNameFromNode(b.returnFromOrbit));
			addProgressStandard(ProgressType.RENDEZVOUS, b.Body, b.rendezvous, progressParser.rendezvousDescriptor);
			addProgressStandard(ProgressType.SCIENCE, b.Body, b.science, progressParser.scienceDescriptor, progressParser.StandardNote, progressParser.vesselNameFromNode(b.science));
			addProgressStandard(ProgressType.SPACEWALK, b.Body, b.spacewalk, progressParser.spacewalkDescriptor, progressParser.CrewNote, progressParser.crewNameFromNode(b.spacewalk));
			addProgressStandard(ProgressType.SPLASHDOWN, b.Body, b.splashdown, progressParser.splashdownDescriptor);
			addProgressStandard(ProgressType.STATIONCONSTRUCTION, b.Body, b.stationConstruction, progressParser.stationDescriptor, progressParser.FacilityNote, progressParser.vesselNameFromNode(b.stationConstruction));
			addProgressStandard(ProgressType.SURFACEEVA, b.Body, b.surfaceEVA, progressParser.EVADescriptor, progressParser.CrewNote, progressParser.crewNameFromNode(b.surfaceEVA));
		}

		public void addProgressStandard(ProgressType p, CelestialBody b, ProgressNode n, string d = "", string g = "", string r = "")
		{
			if (n == null)
				return;

			if (bodyNodes.Contains(n.Id))
				return;

			progressStandard s = null;

			switch (p)
			{
				case ProgressType.FLYBYRETURN:
					s = new progressStandard(b, ProgressType.FLYBYRETURN, n, d, g, r);
					flybyReturn = s;
					break;
				case ProgressType.LANDINGRETURN:
					s = new progressStandard(b, ProgressType.LANDINGRETURN, n, d, g, r);
					landingReturn = s;
					break;
				case ProgressType.ORBITRETURN:
					s = new progressStandard(b, ProgressType.ORBITRETURN, n, d, g, r);
					orbitReturn = s;
					break;
				case ProgressType.BASECONSTRUCTION:
					s = new progressStandard(b, ProgressType.BASECONSTRUCTION, n, d, g, r);
					baseConstruct = s;
					break;
				case ProgressType.CREWTRANSFER:
					s = new progressStandard(b, ProgressType.CREWTRANSFER, n, d, g, r);
					transfer = s;
					break;
				case ProgressType.DOCKING:
					s = new progressStandard(b, ProgressType.DOCKING, n, d, g, r);
					docking = s;
					break;
				case ProgressType.ESCAPE:
					s = new progressStandard(b, ProgressType.ESCAPE, n, d, g, r);
					escape = s;
					break;
				case ProgressType.FLAGPLANT:
					s = new progressStandard(b, ProgressType.FLAGPLANT, n, d, g, r);
					flag = s;
					break;
				case ProgressType.FLIGHT:
					s = new progressStandard(b, ProgressType.FLIGHT, n, d, g, r);
					flight = s;
					break;
				case ProgressType.FLYBY:
					s = new progressStandard(b, ProgressType.FLYBY, n, d, g, r);
					flyby = s;
					break;
				case ProgressType.LANDING:
					s = new progressStandard(b, ProgressType.LANDING, n, d, g, r);
					landing = s;
					break;
				case ProgressType.ORBIT:
					s = new progressStandard(b, ProgressType.ORBIT, n, d, g, r);
					orbit = s;
					break;
				case ProgressType.RENDEZVOUS:
					s = new progressStandard(b, ProgressType.RENDEZVOUS, n, d, g, r);
					rendezvous = s;
					break;
				case ProgressType.SCIENCE:
					s = new progressStandard(b, ProgressType.SCIENCE, n, d, g, r);
					science = s;
					break;
				case ProgressType.SPACEWALK:
					s = new progressStandard(b, ProgressType.SPACEWALK, n, d, g, r);
					spacewalk = s;
					break;
				case ProgressType.SPLASHDOWN:
					s = new progressStandard(b, ProgressType.SPLASHDOWN, n, d, g, r);
					splashdown = s;
					break;
				case ProgressType.STATIONCONSTRUCTION:
					s = new progressStandard(b, ProgressType.STATIONCONSTRUCTION, n, d, g, r);
					station = s;
					break;
				case ProgressType.SUBORBIT:
					s = new progressStandard(b, ProgressType.SUBORBIT, n, d, g, r);
					suborbit = s;
					break;
				case ProgressType.SURFACEEVA:
					s = new progressStandard(b, ProgressType.SURFACEEVA, n, d, g, r);
					surfaceEVA = s;
					break;
				default:
					return;
			}

			if (s == null)
				return;

			bodyNodes.Add(n.Id, s);
		}

		public CelestialBody Body
		{
			get { return body; }
		}

		public bool HomeWorld
		{
			get { return homeworld; }
		}

		public void UpdateReachedStatus()
		{
			isReached = bodyNodes.Values.Any(n => n.IsComplete);
		}

		public bool IsReached
		{
			get { return isReached; }
		}

		public progressStandard getNode(string id)
		{
			if (bodyNodes.Contains(id))
				return bodyNodes[id];

			return null;
		}

		public List<progressStandard> getAllNodes
		{
			get { return bodyNodes.Values.ToList(); }
		}

		public progressStandard Flyby
		{
			get { return flyby; }
		}

		public progressStandard Orbit
		{
			get { return orbit; }
		}

		public progressStandard Landing
		{
			get { return landing; }
		}

		public progressStandard Escape
		{
			get { return escape; }
		}

		public progressStandard Science
		{
			get { return science; }
		}

		public progressStandard Flight
		{
			get { return flight; }
		}

		public progressStandard FlybyReturn
		{
			get { return flybyReturn; }
		}

		public progressStandard OrbitReturn
		{
			get { return orbitReturn; }
		}

		public progressStandard LandingReturn
		{
			get { return landingReturn; }
		}

		public progressStandard Splashdown
		{
			get { return splashdown; }
		}

		public progressStandard SubOrbit
		{
			get { return suborbit; }
		}

		public progressStandard Transfer
		{
			get { return transfer; }
		}

		public progressStandard Docking
		{
			get { return docking; }
		}

		public progressStandard Flag
		{
			get { return flag; }
		}

		public progressStandard SpaceWalk
		{
			get { return spacewalk; }
		}

		public progressStandard SurfaceEVA
		{
			get { return surfaceEVA; }
		}

		public progressStandard Station
		{
			get { return station; }
		}

		public progressStandard BaseConstruct
		{
			get { return baseConstruct; }
		}

		public progressStandard Rendezvous
		{
			get { return rendezvous; }
		}
	}
}
