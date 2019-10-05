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

        public bool CalculateTimes()
        {
            if (stages.Count < 1)
                return true;

			// create lookup tables for comparing times
            var previousStage = new Dictionary<string, DriverTime>();
            var currentStage = new Dictionary<string, DriverTime>();

			previousStage = stages[0].DriverTimes;

			CalculateDeltas(stages[0], true);

			// for each stage after SS1
			for (int i = 1; i < stages.Count; i++)
			{
				currentStage = stages[i].DriverTimes;

				// for each driver on the previous stage
				foreach (KeyValuePair<string,DriverTime> previousDriverTimeKvp in previousStage)
				{
					// get the current time and compute the stage time and position change
					DriverTime currentDriverTime;
					if (true == currentStage.TryGetValue(previousDriverTimeKvp.Key, out currentDriverTime))
					{
                        // in DR2.0 a driver can alt+F4 when they crash and continue a rally
                        // in this case, they will not have a previous time
                        if (currentDriverTime != null && previousDriverTimeKvp.Value != null)
						{
							currentDriverTime.CalculatedStageTime = currentDriverTime.CalculatedOverallTime - previousDriverTimeKvp.Value.CalculatedOverallTime;
							currentDriverTime.CalculatedPositionChange = previousDriverTimeKvp.Value.OverallPosition - currentDriverTime.OverallPosition;
						}
					}
					else
					{
						currentStage.Add(previousDriverTimeKvp.Key, null); // track DNFs
					}
				}

				previousStage = currentStage;

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

            foreach (DriverTime driverTime in currentStage.DriverTimes.Values.Where(x => x != null).OrderBy(x => x.CalculatedOverallTime))
			{
				if (driverTime == null)
					continue;

				if (driverTime.OverallPosition == 1)
				{
					fastestOverallTime = driverTime.CalculatedOverallTime;
					previousOverallTime = driverTime.CalculatedOverallTime;
                    firstDriverProcessed = true;

					if (isFirstStage == true)
					{
						driverTime.CaclulatedStagePosition = driverTime.OverallPosition;
						driverTime.CalculatedStageTime = driverTime.CalculatedOverallTime;
					}

					continue;
				}

                // error - if the first driver has not been processed at this point,
                // we can't calculate deltas for the other drivers.
                if (firstDriverProcessed == false)
                    return;

				driverTime.CalculatedOverallDiffFirst = driverTime.CalculatedOverallTime - fastestOverallTime;
				driverTime.CalculatedOverallDiffPrevious = driverTime.CalculatedOverallTime - previousOverallTime;

				if (isFirstStage == true)
				{
					driverTime.CaclulatedStagePosition = driverTime.OverallPosition;
					driverTime.CalculatedStageTime = driverTime.CalculatedOverallTime;
					driverTime.CalculatedStageDiffFirst = driverTime.CalculatedOverallDiffFirst;
					driverTime.CalculatedStageDiffPrevious = driverTime.CalculatedOverallDiffPrevious;
				}

				previousOverallTime = driverTime.CalculatedOverallTime;
			}

            // reset error flag for reuse
            firstDriverProcessed = false;

            // order by stage time and calculate stage time deltas
            if (isFirstStage == false)  // skip for the first stage, there is no stage delta to calculate
			{
				int stagePosition = 1;
				foreach (DriverTime driverTime in currentStage.DriverTimes.Values.Where(x => x != null).OrderBy(x => x.CalculatedStageTime))
				{
					if (driverTime == null)
					{
						stagePosition++;
						continue;
					}
					if (stagePosition == 1)
					{
						fastestStageTime = driverTime.CalculatedStageTime;
						previousStageTime = driverTime.CalculatedStageTime;
						driverTime.CaclulatedStagePosition = stagePosition;
						stagePosition++;
                        firstDriverProcessed = true;
                        continue;
					}

                    // error - if the first driver has not been processed at this point,
                    // we can't calculate deltas for the other drivers.
                    if (firstDriverProcessed == false)
                        return;

                    driverTime.CalculatedStageDiffFirst = driverTime.CalculatedStageTime - fastestStageTime;
					driverTime.CalculatedStageDiffPrevious = driverTime.CalculatedStageTime - previousStageTime;
					driverTime.CaclulatedStagePosition = stagePosition;
					previousStageTime = driverTime.CalculatedStageTime;
					stagePosition++;
				}
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
        // raw data supplied by parser
        public int OverallPosition { get; private set; }
        public string Tags { get; private set; }
        public int PlayerID { get; private set; }
        public string DriverName { get; private set; }
        public string Vehicle { get; private set; }
        public string OverallTime { get; private set; }
        public string OverallDiffFirst { get; private set; }

        // calculated overall data
        public TimeSpan CalculatedOverallTime { get; internal set; }
		public TimeSpan CalculatedOverallDiffPrevious { get; internal set; }
		public TimeSpan CalculatedOverallDiffFirst { get; internal set; }
		public int CalculatedPositionChange { get; internal set; }

        // calculated stage data
		public int CaclulatedStagePosition { get; internal set; }
		public TimeSpan CalculatedStageTime { get; internal set; }
		public TimeSpan CalculatedStageDiffPrevious { get; internal set; }
		public TimeSpan CalculatedStageDiffFirst { get; internal set; }

        /// <summary>
        /// Creates a new DriverData object that represents a single driver's time on a single stage.
        /// Data is the 'raw' string data taken from the results
        /// Tags are optional (not all drivers will have tags)
        /// </summary>
        public DriverTime(int overallPosition, string driverName, string vehicle, string overallTime, string overallDiffFirst, string tags = null)
        {
            OverallPosition = overallPosition;
            Tags = tags;
            DriverName = driverName;
            Vehicle = vehicle;
            OverallTime = overallTime;
            OverallDiffFirst = overallDiffFirst;

            // TODO: this parsing code should not be here (sparation of concerns)
            TimeSpan parsedOverallTime;

            if (TimeSpan.TryParseExact(OverallTime, @"mm\:ss\.fff", CultureInfo.InvariantCulture, out parsedOverallTime))
                CalculatedOverallTime = parsedOverallTime;
            else if (TimeSpan.TryParseExact(OverallTime, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture, out parsedOverallTime))
                CalculatedOverallTime = parsedOverallTime;
            else
                throw new ArgumentException("Could not parse overall time");
        }
    }
}
