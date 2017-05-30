#region license
/*The MIT License (MIT)
Progress Standard - A storage object for holding all standard-type progress nodes

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
using UnityEngine;
using FinePrint.Utilities;
using KSPAchievements;
using System.Reflection;

namespace ProgressParser
{
	public class progressStandard
	{
		private CelestialBody body;
		private ProgressType pType;
		private string id;
		private string descriptor;
		private string note;
		private string noteReference;
		private string bodyName;
		private double time;

		private float fundsReward, sciReward, repReward;
		private string fundsRewardString, sciRewardString, repRewardString;

		private bool hasRewards;
		private bool isComplete;

		public progressStandard() {	}

		public progressStandard(CelestialBody b, ProgressType t, ProgressNode n, string s = "", string g = "", string r = "", bool rewards = true)
		{
			if (b != null)
				body = b;

			pType = t;
			hasRewards = rewards;
			id = n.Id;
			descriptor = s;
			note = g;
			noteReference = r;

			if (t == ProgressType.POINTOFINTEREST)
			{
				string bodyN = ((PointOfInterest)n).body;

				for (int i = FlightGlobals.Bodies.Count - 1; i >= 0; i--)
				{
					CelestialBody bod = FlightGlobals.Bodies[i];

					if (bod.bodyName != bodyN)
						continue;

					bodyName = bod.displayName.LocalizeBodyName();
					break;
				}
			}

			try
			{
				time = (double)n.GetType().GetField("AchieveDate", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(n);
			}
			catch (Exception e)
			{
				Debug.LogWarning("[Progress Tracking Parser] Error In Detecting Progress Node Achievement Date\n" + e);
			}

			if (n.IsComplete)
				calculateRewards(b);
		}

		public void calculateRewards(CelestialBody body)
		{
			isComplete = true;

			if (!hasRewards)
				return;

			fundsReward = ProgressUtilities.WorldFirstStandardReward(ProgressRewardType.PROGRESS, Currency.Funds, pType, body);
			sciReward = ProgressUtilities.WorldFirstStandardReward(ProgressRewardType.PROGRESS, Currency.Science, pType, body);
			repReward = ProgressUtilities.WorldFirstStandardReward(ProgressRewardType.PROGRESS, Currency.Reputation, pType, body);

			if (fundsReward != 0)
				fundsRewardString = fundsReward.ToString("N0");
			else
				fundsRewardString = "";

			if (sciReward != 0)
				sciRewardString = sciReward.ToString("N0");
			else
				sciRewardString = "";

			if (repReward != 0)
				repRewardString = repReward.ToString("N0");
			else
				repRewardString = "";
		}

		public CelestialBody Body
		{
			get { return body; }
		}

		public bool HasRewards
		{
			get { return hasRewards; }
		}

		public bool IsComplete
		{
			get { return isComplete; }
		}

		public double Time
		{
			get { return time; }
			set { time = value; }
		}

		public string KSPDateString
		{
			get { return KSPUtil.PrintDate((int)time, false, false); }
		}

		public string KSPDateCompact
		{
			get { return KSPUtil.PrintDateCompact((int)time, false, false); }
		}

		public string Note
		{
			get { return note; }
		}

		public string NoteReference
		{
			get { return noteReference; }
			set { noteReference = value; }
		}

		public bool ShowNotes { get; set; }
		
		public ProgressType PType
		{
			get { return pType; }
		}

		public string BodyName
		{
			get { return bodyName; }
		}

		public string ID
		{
			get { return id; }
		}

		public string Descriptor
		{
			get { return descriptor; }
		}

		public float FundsReward
		{
			get { return fundsReward; }
		}

		public float SciReward
		{
			get { return sciReward; }
		}

		public float RepReward
		{
			get { return repReward; }
		}

		public string FundsRewardString
		{
			get { return fundsRewardString; }
		}

		public string SciRewardString
		{
			get { return sciRewardString; }
		}

		public string RepRewardString
		{
			get { return repRewardString; }
		}
	}
}
