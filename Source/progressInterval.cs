#region license
/*The MIT License (MIT)
Progress Interval - A storage object for holding all interval-type progress nodes

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
using FinePrint;
using KSPAchievements;

namespace ProgressParser
{
	public class progressInterval
	{
		private ProgressType pType;
		private string id;
		private string descriptor;

		private int interval = 1;
		private int totalIntervals;
		private double currentRecord;
		private double max;
		private double round;

		private List<int> records = new List<int>();
		private List<Vector3> rewards = new List<Vector3>();

		public progressInterval() { }

		public progressInterval(ProgressType t, ProgressNode n, double r, double m, double ro, int i, string s = "")
		{
			pType = t;
			id = n.Id;
			totalIntervals = i;
			descriptor = s;
			currentRecord = r;
			max = m;
			round = ro;
			records = new List<int>(i + 1);
			rewards = new List<Vector3>(i + 1);

			for (int k = 0; k < i + 1; k++)
			{
				records.Add(0);
				rewards.Add(new Vector3());
			}

			if (n.IsComplete)
				interval = totalIntervals + 1;
			else
			{
				interval = getNextInterval();

				if (interval > totalIntervals + 1)
					interval = totalIntervals + 1;
			}

			for (int j = 1; j <= totalIntervals; j++)
			{
				calculateRewards(j);
			}
		}

		public void calculateRewards(int i)
		{
			if (i > totalIntervals)
				return;

			records[i] = (int)ProgressUtilities.FindNextRecord(records[i - 1], max, round);

			rewards[i] = new Vector3(ProgressUtilities.WorldFirstIntervalReward(ProgressRewardType.PROGRESS, Currency.Funds, pType, null, i, totalIntervals), ProgressUtilities.WorldFirstIntervalReward(ProgressRewardType.PROGRESS, Currency.Science, pType, null, i, totalIntervals), ProgressUtilities.WorldFirstIntervalReward(ProgressRewardType.PROGRESS, Currency.Reputation, pType, null, i, totalIntervals));
		}

		private int getNextInterval()
		{
			int newInterval = 1;
			double multiplier = Math.Pow(100.0, 1.0 / (totalIntervals - 1.0));

			for (int i = 1; i <= totalIntervals; i++)
			{
				double d = max * (0.01 * Math.Pow(multiplier, (double)(i - 1)));
				d = Math.Round(d / round) * round;

				if (d > currentRecord)
					return i;

				newInterval = i;
			}

			return newInterval;
		}

		public string ID
		{
			get { return id; }
		}

		public string Descriptor
		{
			get { return descriptor; }
		}

		public bool IsReached
		{
			get { return interval > 1; }
		}

		public bool ShowRecords { get; set; }

		public int Interval
		{
			get { return interval; }
			set
			{
				if (value > totalIntervals + 1)
					value = totalIntervals + 1;

				interval = value;
			}
		}

		public int getRecord(int index)
		{
			if (records.Count > index)
				return records[index];

			return 0;
		}

		public Vector3 Rewards(int i)
		{
			if (rewards.Count > i)
				return rewards[i];

			return new Vector3();
		}

		public float getFunds(int i)
		{
			if (rewards.Count > i)
				return rewards[i].x;

			return 0;
		}

		public float getScience(int i)
		{
			if (rewards.Count > i)
				return rewards[i].y;

			return 0;
		}

		public float getRep(int i)
		{
			if (rewards.Count > i)
				return rewards[i].z;

			return 0;
		}

	}
}
