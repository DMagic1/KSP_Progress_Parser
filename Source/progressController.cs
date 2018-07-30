#region license
/*The MIT License (MIT)
Progress Controller - A Monobehaviour for montioring progress node activity and loading the parser

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
using System.Reflection;
using UnityEngine;
using KSPAchievements;

namespace ProgressParser
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class progressController : MonoBehaviour
	{
		private static bool initialized;
		
		public static progressController instance;

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

        private void onSceneChange(GameScenes g)
        {
            switch (g)
            {
                case GameScenes.LOADING:
                case GameScenes.LOADINGBUFFER:
                case GameScenes.MAINMENU:
                case GameScenes.SETTINGS:
                case GameScenes.CREDITS:
                case GameScenes.PSYSTEM:
                case GameScenes.MISSIONBUILDER:
                    return;
                case GameScenes.EDITOR:
                    progressParser.Loaded = false;

                    Debug.Log("[Progress Tracking Parser] Initializing Progress Parser For Editor...");

                    progressParser.editorInitialize();
                    return;
                default:
                    progressParser.Loaded = false;

                    Debug.Log("[Progress Tracking Parser] Initializing Progress Parser...");

                    progressParser.initialize();
                    return;
            }
        }

		private void onReach(ProgressNode node)
		{
			if (node == null)
				return;

			if (progressParser.isIntervalType(node))
			{
				progressInterval i = progressParser.getIntervalNode(node.Id);

				if (i != null)
				{
					double nodeRecord = progressParser.getIntervalRecord(node);

					if (i.getRecord(i.Interval) >= nodeRecord)
						return;

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
			if (node == null)
				return;

			if (progressParser.isIntervalType(node))
			{
				progressInterval i = progressParser.getIntervalNode(node.Id);

				if (i != null)
				{
					double nodeRecord = progressParser.getIntervalRecord(node);

					if (i.getRecord(i.Interval) >= nodeRecord)
						return;

					if (node.IsReached)
					{
						i.calculateRewards(i.Interval);
						i.Interval += 1;
					}
				}
			}

			progressParser.updateCompletionRecord();
		}

        private void onComplete(ProgressNode node)
		{
			if (node == null)
				return;

			if (!node.IsComplete)
				return;

			if (!progressParser.isIntervalType(node))
			{
				if (progressParser.isPOI(node))
				{
					progressStandard s = progressParser.getPOINode(node.Id);

					if (s == null)
					{
						Debug.Log("[Progress Tracking Parser] POI Progress Node Not Found");
					}
					else
					{
						s.calculateRewards(null);
						s.NoteReference = progressParser.vesselNameFromNode(node);

						try
						{
							s.Time = (double)node.GetType().GetField("AchieveDate", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(node);
						}
						catch (Exception e)
						{
							Debug.LogWarning("[Progress Tracking Parser] Error In Detecting Progress Node Achievement Date\n" + e);
						}
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

						try
						{
							s.Time = (double)node.GetType().GetField("AchieveDate", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(node);
						}
						catch (Exception e)
						{
							Debug.LogWarning("[Progress Tracking Parser] Error In Detecting Progress Node Achievement Date\n" + e);
						}
					}
					else
					{
						CelestialBody body = progressParser.getBodyFromType(node);

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

									try
									{
										sb.Time = (double)node.GetType().GetField("AchieveDate", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(node);
									}
									catch (Exception e)
									{
										Debug.LogWarning("[Progress Tracking Parser] Error In Detecting Progress Node Achievement Date\n" + e);
									}
								}
							}
						}
					}
				}
			}
			else
			{
				progressInterval i = progressParser.getIntervalNode(node.Id);

				if (i == null)
				{
					Debug.Log("[Progress Tracking Parser] Interval Progress Node Not Found");
				}
				else
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

	}
}
