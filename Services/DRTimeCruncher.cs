using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DR2Rallymaster.Services
{
    /// <summary>
    /// Holds all stage times
    /// </summary>
    public class Rally : IEnumerable
    {
		public int StageCount { get { return stages.Count; } }
        public Dictionary<string, int> VehicleCounts { get; set; }
        public int DriverCount { get { return DriverInfoDict.Count; } }
        public int DriversDnf { get; set; }
        public Dictionary<string, DriverInfo> DriverInfoDict = new Dictionary<string, DriverInfo>();

        private List<Stage> stages = new List<Stage>();

        /// <summary>
        /// Adds a stage to the rally's stage collection
		/// Enumerating returns Stages
        /// </summary>
        /// <returns>true</returns>
        public bool AddStage(Stage stage)
        {
            stages.Add(stage);
            return true;
        }

        public bool ProcessResults()
        {
            if (stages.Count < 1)
                return true;

            // get a list of all drivers that ran a stage, this is used as the master entry list
            ProcessDriverInfo();

            var previousStageTimes = new Dictionary<string, DriverTime>();
            var currentStageTimes = new Dictionary<string, DriverTime>();

            // the "previous" stage's inital value is the first stage
            previousStageTimes = stages[0].DriverTimes;

            CalculateDeltas(stages[0], true);

			// for each stage
			for (int i = 0; i < stages.Count; i++)
			{
				currentStageTimes = stages[i].DriverTimes;

                // if a driver's PC crashes or they alt+f4, they will not score a stage time
                // and they will not show up as DNF, but will continue on
                // go thru all results and add zero times as needed

                // for each driver that started the rally
                foreach (var driver in DriverInfoDict.Keys)
                {
                    // get the driver time for the previous and current stage
                    DriverTime currentDriverTime;
                    DriverTime previousDriverTime;
                    var hasCurrentDriverTime = currentStageTimes.TryGetValue(driver, out currentDriverTime);
                    var hasPreviousDriverTime = previousStageTimes.TryGetValue(driver, out previousDriverTime);

                    // if the driver does not have a current stage, create a DNF entry for them
                    if (!hasCurrentDriverTime)
                    {
                        var dnfEntry = new DriverTime()
                        {
                            IsDnf = true,
                            DriverName = driver,
                            Vehicle = DriverInfoDict[driver].Vehicle,
                            OverallPosition = 0
                        };

                        currentStageTimes.Add(driver, dnfEntry);
                    }

                    // if the driver has a current stage, but not a previous stage, mark the current stage as DNF
                    else if (hasCurrentDriverTime && !hasPreviousDriverTime)
                    {
                        currentDriverTime.IsDnf = true;
                        currentDriverTime.OverallPosition = 0;
                    }

                    // the driver has a current stage and a previous stage, do stuff
                    else if (hasCurrentDriverTime && hasPreviousDriverTime)
                    {
                        if (previousDriverTime.IsDnf)
                        {
                            currentDriverTime.IsDnf = true;
                            currentDriverTime.OverallPosition = 0;
                        }

                        if (!previousDriverTime.IsDnf || !currentDriverTime.IsDnf)
                            currentDriverTime.PositionChange = previousDriverTime.OverallPosition - currentDriverTime.OverallPosition;
                    }

                    // set the overall position on the DriverInfo
                    if (hasCurrentDriverTime)
                        DriverInfoDict[driver].OverallPosition = currentDriverTime.OverallPosition;
                    else
                        DriverInfoDict[driver].OverallPosition = 0;
                }

				previousStageTimes = currentStageTimes;

				CalculateDeltas(stages[i], false);
			}

            return true;
        }

		private void CalculateDeltas(Stage currentStage, bool isFirstStage)
		{
			// order by overall time and calculate overall time deltas
			TimeSpan fastestOverallTime = new TimeSpan();
			TimeSpan previousOverallTime = new TimeSpan();
            TimeSpan fastestStageTime = new TimeSpan();
            TimeSpan previousStageTime = new TimeSpan();
            bool firstDriverProcessed = false;

            foreach (DriverTime driverTime in currentStage.DriverTimes.Values.Where(x => x != null).OrderBy(x => x.OverallTime))
			{
				if (driverTime == null)
					continue;

                // if this is a DNF stage, skip it
                if (driverTime.IsDnf)
                    continue;

				if (driverTime.OverallPosition == 1)
				{
					fastestOverallTime = driverTime.OverallTime;
					previousOverallTime = driverTime.OverallTime;
                    firstDriverProcessed = true;

					if (isFirstStage == true)
						driverTime.StagePosition = driverTime.OverallPosition;

					continue;
				}

                // error - if the first driver has not been processed at this point,
                // we can't calculate deltas for the other drivers.
                if (firstDriverProcessed == false)
                    return;

				driverTime.OverallDiffPrevious = driverTime.OverallTime - previousOverallTime;

				if (isFirstStage == true)
				{
					driverTime.StagePosition = driverTime.OverallPosition;
					driverTime.StageDiffPrevious = driverTime.StageDiffPrevious;
				}

				previousOverallTime = driverTime.OverallTime;
			}

            // reset error flag for reuse
            firstDriverProcessed = false;

            // order by stage time and calculate stage time deltas
            if (isFirstStage == false)  // skip for the first stage, there is no stage delta to calculate
			{
				int stagePosition = 1;
				foreach (DriverTime driverTime in currentStage.DriverTimes.Values.Where(x => x != null).OrderBy(x => x.StageTime))
				{
                    // skip DNF entries
					if (driverTime.IsDnf)
						continue;

					if (stagePosition == 1)
					{
						fastestStageTime = driverTime.StageTime;
						previousStageTime = driverTime.StageTime;
						driverTime.StagePosition = stagePosition;
						stagePosition++;
                        firstDriverProcessed = true;
                        continue;
					}

                    // error - if the first driver has not been processed at this point,
                    // we can't calculate deltas for the other drivers.
                    if (firstDriverProcessed == false)
                        return;

					driverTime.StageDiffPrevious = driverTime.StageTime - previousStageTime;
					driverTime.StagePosition = stagePosition;
					previousStageTime = driverTime.StageTime;
					stagePosition++;
				}
			}
		}

        // creates all stats for drivers
        private void ProcessDriverInfo()
        {
            // first compile all the driver info
            DriverInfoDict = new Dictionary<string, DriverInfo>();
            VehicleCounts = new Dictionary<string, int>();
            foreach (var stage in stages)
            {
                foreach(var driver in stage.DriverTimes.Keys)
                {
                    var driverTime = stage.DriverTimes[driver];

                    if (!DriverInfoDict.ContainsKey(driver))
                    {
                        var driverInfo = new DriverInfo() { Name = driver, Vehicle = driverTime.Vehicle, StagesRecorded = 1, IsDnf = driverTime.IsDnf };
                        DriverInfoDict.Add(driver, driverInfo);
                    }
                    else
                    {
                        if (driverTime.IsDnf)
                            DriverInfoDict[driver].IsDnf = true;

                       DriverInfoDict[driver].StagesRecorded++;
                    }
                }
            }

            // now go thru all the driver info and generate stats
            DriversDnf = 0;
            foreach(var driver in DriverInfoDict.Values)
            {
                // is dnf?
                if (driver.StagesRecorded < StageCount || driver.IsDnf)
                {
                    driver.IsDnf = true;
                    DriversDnf++;
                }

                // car driven
                if (!VehicleCounts.ContainsKey(driver.Vehicle))
                    VehicleCounts.Add(driver.Vehicle, 1);
                else
                    VehicleCounts[driver.Vehicle]++;
            }
        }

        public IEnumerator GetEnumerator()
		{
			return ((IEnumerable)stages).GetEnumerator();
		}
	}

    /// <summary>
    /// Holds all driver times for a single stage
	/// Enumeration returns DriverTimes
    /// </summary>
    public class Stage : IEnumerable
    {
		public Dictionary<string, DriverTime> DriverTimes { get; private set; }

        /// <summary>
        /// Adds a driver's stage results to the stage's collection
        /// </summary>
        /// <returns>true</returns>
        public bool AddDriver(DriverTime driverTime)
        {
			DriverTimes.Add(driverTime.DriverName, driverTime);
            return true;
        }

        public IEnumerator GetEnumerator()
        {
			return ((IEnumerable)DriverTimes).GetEnumerator();
        }

		public Stage()
		{
			DriverTimes = new Dictionary<string, DriverTime>();
		}

        public int Count
        {
            get { return DriverTimes.Count; }
        }
    }

    /// <summary>
    /// Holds the data for a single driver on a single stage
    /// </summary>
    public class DriverTime
    {
        public bool IsDnf { get; set; }
        public int OverallPosition { get; set; }
        public string DriverName { get; set; }
        public string Vehicle { get; set; }
        public TimeSpan OverallTime { get; set; }
		public TimeSpan OverallDiffPrevious { get; set; }
		public TimeSpan OverallDiffFirst { get; set; }
		public int PositionChange { get; set; }
		public int StagePosition { get; set; }
		public TimeSpan StageTime { get; set; }
		public TimeSpan StageDiffPrevious { get; set; }
		public TimeSpan StageDiffFirst { get; set; }
    }

    public class DriverInfo
    {
        public string Name { get; set; }
        public string Vehicle { get; set; }
        public int StagesRecorded { get; set; }
        public bool IsDnf { get; set; }
        public int OverallPosition { get; set; }
    }
}
