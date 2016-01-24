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
		private string bodyName;
		private ProgressType pType;
		private string id;
		private string descriptor;
		private string note;
		private string noteReference;
		private double time;

		private float fundsReward;
		private float sciReward;
		private float repReward;

		private bool hasRewards;
		private bool isComplete;

		public progressStandard() {	}

		public progressStandard(CelestialBody b, ProgressType t, ProgressNode n, string s = "", string g = "", string r = "", bool rewards = true)
		{
			if (b != null)
				bodyName = b.bodyName;

			pType = t;
			hasRewards = rewards;
			id = n.Id;
			descriptor = s;
			note = g;
			noteReference = r;

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
		}

		public CelestialBody Body
		{
			get
			{
				if (string.IsNullOrEmpty(bodyName))
					return null;

				return FlightGlobals.Bodies.FirstOrDefault(b => b.bodyName == bodyName);
			}
		}

		public string BodyName
		{
			get { return bodyName; }
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
		}

		public int[] KSPDate
		{
			get
			{
				if (GameSettings.KERBIN_TIME)
					return KSPUtil.GetKerbinDateFromUT((int)time);
				else
					return KSPUtil.GetEarthDateFromUT((int)time);
			}
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
	}
}
