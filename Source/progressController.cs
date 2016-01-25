using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KSPAchievements;

namespace ProgressParser
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class progressController : MonoBehaviour
	{
		private static bool initialized;
		private static bool messageIconLoaded;
		private static Texture2D messageIcon = new Texture2D(32, 32);
		
		public static progressController instance;

		public static Texture2D MessageIcon
		{
			get { return messageIcon; }
		}

		private void Start()
		{
			if (initialized)
				Destroy(gameObject);

			instance = this;

			DontDestroyOnLoad(gameObject);

			Debug.Log("[Progress Tracking Parser] Starting Progress Controller...");
			initialized = true;

			GameEvents.onLevelWasLoaded.Add(onSceneChange);
			GameEvents.OnProgressReached.Add(onReach);
			GameEvents.OnProgressAchieved.Add(onAchieve);
			GameEvents.OnProgressComplete.Add(onComplete);
		}

		private void loadMessageSystemIcon()
		{
			MessageSystem.Message m = new MessageSystem.Message("", "", MessageSystemButton.MessageButtonColor.BLUE, MessageSystemButton.ButtonIcons.MESSAGE);

			if (m == null)
				return;

			if (m.button == null)
			{
				m = null;
				return;
			}

			MessageSystemButton b = m.button;

			if (b.iconAchieve == null)
			{
				m = null;
				return;
			}

			Debug.Log("[Progress Tracking Parser] Message System Icon Loaded");

			messageIcon = (Texture2D)b.iconAchieve;

			messageIconLoaded = true;

			m = null;
		}

		private void onSceneChange(GameScenes g)
		{
			switch (g)
			{
				case GameScenes.LOADING:
				case GameScenes.CREDITS:
				case GameScenes.LOADINGBUFFER:
				case GameScenes.MAINMENU:
				case GameScenes.PSYSTEM:
				case GameScenes.SETTINGS:
					return;
			}

			if (!messageIconLoaded)
				loadMessageSystemIcon();

			Debug.Log("[Progress Tracking Parser] Initializing Progress Parser...");

			progressParser.initialize(HighLogic.CurrentGame);
		}

		private void onReach(ProgressNode node)
		{
			Debug.Log("Reaching A Node...");

			if (node == null)
				return;

			if (isIntervalType(node))
			{
				progressInterval i = progressParser.getIntervalNode(node.Id);

				if (i != null)
				{
					double nodeRecord = getIntervalRecord(node);

					if (i.getRecord(i.Interval) >= nodeRecord)
						return;

					Debug.Log("Interval Node Processing On Reach...: " + i.Interval.ToString());

					if (node.IsReached)
					{
						i.calculateRewards(i.Interval);
						i.Interval += 1;
					}
				}
			}

			progressParser.updateCompletionRecord();
		}

		private void onAchieve(ProgressNode node)
		{
			Debug.Log("Achieving A Node...");

			if (node == null)
				return;

			if (isIntervalType(node))
			{
				progressInterval i = progressParser.getIntervalNode(node.Id);

				if (i != null)
				{
					double nodeRecord = getIntervalRecord(node);

					if (i.getRecord(i.Interval) >= nodeRecord)
						return;

					Debug.Log("Interval Node Processing On Achieve...: " + i.Interval.ToString());

					if (node.IsReached)
					{
						i.calculateRewards(i.Interval);
						i.Interval += 1;
					}
				}
			}

			progressParser.updateCompletionRecord();
		}

		private double getIntervalRecord(ProgressNode n)
		{
			Type t = n.GetType();

			if (t == typeof(RecordsAltitude))
				return ((RecordsAltitude)n).record;
			else if (t == typeof(RecordsDepth))
				return ((RecordsDepth)n).record;
			else if (t == typeof(RecordsDistance))
				return ((RecordsDistance)n).record;
			else if (t == typeof(RecordsSpeed))
				return ((RecordsSpeed)n).record;

			return 0;
		}

		private void onComplete(ProgressNode node)
		{
			if (node == null)
				return;

			if (!node.IsComplete)
				return;

			if (!isIntervalType(node))
			{
				if (isPOI(node))
				{
					progressStandard s = progressParser.getPOINode(node.Id);

					if (s != null)
					{
						s.calculateRewards(null);
						s.NoteReference = progressParser.vesselNameFromNode(node);
					}
				}
				else
				{
					progressStandard s = progressParser.getStandardNode(node.Id);

					if (s != null)
					{
						s.calculateRewards(null);
						string note = progressParser.crewNameFromNode(node);

						if (string.IsNullOrEmpty(note))
							note = progressParser.vesselNameFromNode(node);

						s.NoteReference = note;
					}
					else
					{
						CelestialBody body = getBodyFromType(node);

						if (body == null)
						{
							Debug.Log("[Progress Tracking Parser] Body From Progress Node Null...");
						}
						else
						{
							progressBodyCollection b = progressParser.getProgressBody(body);

							if (b != null)
							{
								progressStandard sb = b.getNode(node.Id);

								if (sb == null)
								{
									Debug.Log("[Progress Tracking Parser] Body Sub Progress Node Not Found");
								}
								else
								{
									sb.calculateRewards(body);
									string note = progressParser.crewNameFromNode(node);

									if (string.IsNullOrEmpty(note))
										note = progressParser.vesselNameFromNode(node);

									sb.NoteReference = note;
								}
							}
						}
					}
				}
			}
			else
			{
				progressInterval i = progressParser.getIntervalNode(node.Id);

				if (i != null)
				{
					if (node.IsReached)
					{
						i.calculateRewards(i.Interval);
						i.Interval += 1;
					}
				}
			}

			progressParser.updateCompletionRecord();
		}	

		private bool isIntervalType(ProgressNode n)
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

		private bool isPOI(ProgressNode n)
		{
			if (n.GetType() == typeof(PointOfInterest))
				return true;

			return false;
		}

		private CelestialBody getBodyFromType(ProgressNode n)
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
				Debug.LogWarning("[Progress Tracking Parser] Error In Finding Progress Node Celestial Body Reference\n" + e);
			}

			return null;
		}

	}
}
