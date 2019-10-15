﻿//******************************************************************************************************
//  ReportOperationsHubClient.cs - Gbtc
//
//  Copyright © 2016, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  10/10/2019 - Christoph Lackner
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GSF;
using GSF.Configuration;
using GSF.Data;
using GSF.Data.Model;
using GSF.IO;
using GSF.Snap.Services;
using GSF.Threading;
using GSF.Web.Hubs;
using GSF.Web.Model;
using openHistorian.Model;
using openHistorian.Net;
using openHistorian.Snap;
using CancellationToken = GSF.Threading.CancellationToken;
using Random = GSF.Security.Cryptography.Random;

namespace openHistorian.Adapters
{
    /// <summary>
    /// Defines enumeration of supported Report types.
    /// </summary>
    public enum ReportType
    {
        /// <summary>
        /// Signal-to-Noise Ratio.
        /// </summary>
        SNR,
        /// <summary>
        /// Voltage Unbalances.
        /// </summary>
        Unbalance_V,
        /// <summary>
        /// Current Unbalances.
        /// </summary>
        Unbalance_I
    }

    /// <summary>
    /// Defines enumeration of supported Report Crtieria.
    /// </summary>
    public enum ReportCriteria
    {
        /// <summary>
        /// Mean.
        /// </summary>
        Mean,

        /// <summary>
        /// Maximum.
        /// </summary>
        Maximum,

        /// <summary>
        /// Time in Alert.
        /// </summary>
        AlertTime,
    }

    /// <summary>
    /// Represents a client instance of a SignalR Hub for report data operations.
    /// </summary>
    public class ReportOperationsHubClient : HubClientBase
    {
        #region [ Members ]

        // Fields
        private ReportType reportType;
        private int numberOfRecords;
        private ReportHistorianOperations historianOperations;
        private string databaseFile;
        private AdoDataConnection connection;
        private bool m_disposed;
        private double threshold;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="ReportOperationsHubClient"/>.
        /// </summary>
        public ReportOperationsHubClient()
        {
            this.historianOperations = new ReportHistorianOperations();

            // Override the Historian Instance with the value from the config File
            // Any changes after this will take effect

            // Access needed settings from specified categories in configuration file
            CategorizedSettingsElementCollection reportSettings = ConfigurationFile.Current.Settings["reportSettings"];
            reportSettings.Add("historianInstance", "PPA" , "Default historian instance used for reporting");
            string historian = reportSettings["historianInstance"].ValueAs("PPA");
            this.SetSelectedInstanceName(historian);
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="ReportOperationsHubClient"/> object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                // Dispose of Historian operations and connection
                // This is called on endSession and should be triggered in all cases
                this.historianOperations.Dispose();
                this.connection.Dispose();

                //also remove Database File to avoid filling up cache
                try
                {
                    if (disposing)
                    {
                        try
                        {
                            System.IO.File.Delete(this.databaseFile);
                        }
                        catch
                        {
                            throw new Exception("Unable to delete temporary SQL Lite DB for Reports");
                        }
                    }
                }
                finally
                {
                    m_disposed = true;          // Prevent duplicate dispose.
                    base.Dispose(disposing);    // Call base class Dispose().
                }
            }
        }

        /// <summary>
        /// Set selected Historian instance name.
        /// </summary>
        /// <param name="instanceName">Historian instance name that is selected by user.</param>
        public void SetSelectedInstanceName(string instanceName)
        {
            this.historianOperations.SetSelectedInstanceName(instanceName);
        }

        /// <summary>
        /// Gets selected historian instance name.
        /// </summary>
        /// <returns>Selected Historian instance name.</returns>
        public string GetSelectedInstanceName()
        {
            return this.historianOperations.GetSelectedInstanceName();
        }

        /// <summary>
        /// Gets loaded historian adapter instance names.
        /// </summary>
        /// <returns>Historian adapter instance names.</returns>
        public IEnumerable<string> GetInstanceNames()
        {
            return this.historianOperations.GetInstanceNames();
        }

        /// <summary>
        /// Updates the Report Data Source.
        /// </summary>
        /// <param name="startDate">Start date of the Report.</param>
        /// <param name="endDate">End date of the Report.</param>
        /// <param name="reportType"> Type of Report <see cref="ReportType"/>.</param>
        /// /// <param name="reportCriteria"> Criteria to create Report <see cref="ReportCriteria"/>.</param>
        /// <param name="number">Number of records included 0 for all records.</param>
        /// <param name="dataContext">DataContext from which the available reportingParameters are pulled <see cref="DataContext"/>.</param>
        public void UpdateReportSource(DateTime startDate, DateTime endDate, ReportCriteria reportCriteria, ReportType reportType, int number, DataContext dataContext)
        {

            string sourceFile = string.Format("{0}{1}ReportTemplate.db", FilePath.GetAbsolutePath(""), Path.DirectorySeparatorChar);
            this.databaseFile = string.Format("{0}{1}{2}.db",ConfigurationCachePath, Path.DirectorySeparatorChar,this.ConnectionID) ;
            System.IO.File.Copy(sourceFile, this.databaseFile, true);

            this.reportType = reportType;
            this.numberOfRecords = number;

            string filterstring = "";

            if (reportType == ReportType.SNR)
                filterstring = "%-SNR";
            else if (reportType == ReportType.Unbalance_I)
                filterstring = "%I-UBAL";
            else if (reportType == ReportType.Unbalance_V)
                filterstring = "%V-UBAL";

            //Get AlertThreshold
            CategorizedSettingsElementCollection reportSettings = ConfigurationFile.Current.Settings["reportSettings"];
            if (reportType == ReportType.SNR)
            {
                reportSettings.Add("DefaultSNRThreshold", "4.0", "Default SNR Alert threshold.");
                this.threshold = reportSettings["DefaultSNRThreshold"].ValueAs(this.threshold);
            }
            else if (reportType == ReportType.Unbalance_V)
            {
                reportSettings.Add("VUnbalanceThreshold", "4.0", "Voltage Unbalance Alert threshold.");
                this.threshold = reportSettings["VUnbalanceThreshold"].ValueAs(this.threshold);
            }
            else if (reportType == ReportType.Unbalance_I)
            {
                reportSettings.Add("IUnbalanceThreshold", "4.0", "Current Unbalance Alert threshold.");
                this.threshold = reportSettings["IUnbalanceThreshold"].ValueAs(this.threshold);
            }


            List<ReportMeasurements> reportingMeasurements = GetFromStats(dataContext, startDate, endDate, reportType);

            if (reportingMeasurements.Count() < 1)
            {
               
                List <ActiveMeasurement> activeMeasuremnts = new TableOperations<ActiveMeasurement>(dataContext.Connection).QueryRecordsWhere("PointTag LIKE {0}", filterstring).ToList();
                reportingMeasurements = activeMeasuremnts.Select(point => new ReportMeasurements(point)).ToList();

                // Pull Data From the Open Historian

                List<CondensedDataPoint> historiandata = this.historianOperations.ReadCondensed(startDate, endDate, activeMeasuremnts,this.threshold).ToList();

                //remove any that don't have data
                reportingMeasurements = reportingMeasurements.Where(item => historiandata.Select(point => point.PointID).Contains(item.PointID)).ToList();

                // Deal with the not-aggregated signals
                reportingMeasurements = reportingMeasurements.Select(item =>
                {
                    CondensedDataPoint data = historiandata.Find(point => point.PointID == item.PointID);
                    item.Max = data.max;
                    item.Min = data.min;
                    item.Mean = data.sum / data.totalPoints;
                    item.NumberOfAlarms = data.alert;
                    item.PercentAlarms = (double)data.alert * 100.0D / (double)data.totalPoints;
                    item.StandardDeviation = Math.Sqrt((data.sqrsum - 2 * data.sum * item.Mean + (double)data.totalPoints * item.Mean * item.Mean) / (double)data.totalPoints);
                    item.TimeInAlarm = item.NumberOfAlarms * 1000.0D / (double)item.FramesPerSecond;

                    return item;
                }
                ).ToList();

            }


            if (reportCriteria == ReportCriteria.Mean)
                reportingMeasurements = reportingMeasurements.OrderByDescending(item => item.Mean).ToList();
            if (reportCriteria == ReportCriteria.AlertTime)
                reportingMeasurements = reportingMeasurements.OrderByDescending(item => item.TimeInAlarm).ToList();
            if (reportCriteria == ReportCriteria.Maximum)
                reportingMeasurements = reportingMeasurements.OrderByDescending(item => item.Max).ToList();

            string connectionstring = String.Format("Data Source={0}; Version=3; Foreign Keys=True; FailIfMissing=True", this.databaseFile);
            string dataProviderstring = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
            this.connection = new AdoDataConnection(connectionstring, dataProviderstring);

            if (this.numberOfRecords == 0)
                this.numberOfRecords = reportingMeasurements.Count();

            // Create Original Point Tag
            reportingMeasurements.Select(item =>
            {
                item.OriginalTag = item.PointTag;
                if (reportType == ReportType.SNR)
                    item.OriginalTag = item.OriginalTag.Remove(item.OriginalTag.Length - 4);
                else
                    item.OriginalTag = item.OriginalTag.Remove(item.OriginalTag.Length - 5);
                return item;
            }).ToList();

            TableOperations<ReportMeasurements> tbl = new TableOperations<ReportMeasurements>(connection);
            for (int i=0; i < Math.Min(this.numberOfRecords, reportingMeasurements.Count());i++)
            {
                    tbl.AddNewRecord(reportingMeasurements[i]);
            }

        }


        /// <summary>
        /// Returns the Table Operation Object that Queries are build against.
        /// </summary>
        /// <returns> Table Operations Object that is used to query report data.</returns>
        public TableOperations<ReportMeasurements> Table()
        {
            if (this.connection == null)
                return null;

            return (new TableOperations<ReportMeasurements>(this.connection));
        }


        /// <summary>
        /// Gets the path for storing Report Database configurations.
        /// </summary>
        private static string ConfigurationCachePath
        {
            get
            {
                // Define default configuration cache directory relative to path of host application
                string s_configurationCachePath = string.Format("{0}{1}ConfigurationCache{1}", FilePath.GetAbsolutePath(""), Path.DirectorySeparatorChar);

                // Make sure configuration cache path setting exists within system settings section of config file
                ConfigurationFile configFile = ConfigurationFile.Current;
                CategorizedSettingsElementCollection systemSettings = configFile.Settings["systemSettings"];
                systemSettings.Add("ConfigurationCachePath", s_configurationCachePath, "Defines the path used to cache serialized phasor protocol configurations");

                // Retrieve configuration cache directory as defined in the config file
                s_configurationCachePath = FilePath.AddPathSuffix(systemSettings["ConfigurationCachePath"].Value);

                // Make sure configuration cache directory exists
                if (!Directory.Exists(s_configurationCachePath))
                    Directory.CreateDirectory(s_configurationCachePath);

                return s_configurationCachePath;
            }
        }

        /// <summary>
        /// Gets the reporting Measurment List from Aggregate Channels if available.
        /// </summary>
        /// <returns> List of ReportMeasurements to be added to Report.</returns>
        /// <param name="start">Start date of the Report.</param>
        /// <param name="end">End date of the Report.</param>
        /// <param name="type"> Type of Report <see cref="ReportType"/>.</param>
        /// <param name="dataContext">DataContext from which the available reportingParameters are pulled <see cref="DataContext"/>.</param>
        private List<ReportMeasurements> GetFromStats(DataContext dataContext, DateTime start, DateTime end, ReportType type)
        {
            List<ReportMeasurements> result = new List<ReportMeasurements>();

            string filterstring = "";

            if (reportType == ReportType.SNR)
                filterstring = "%-SNR";
            else if (reportType == ReportType.Unbalance_I)
                filterstring = "%I-UBAL";
            else if (reportType == ReportType.Unbalance_V)
                filterstring = "%V-UBAL";



            if (new TableOperations<ActiveMeasurement>(dataContext.Connection).QueryRecordCountWhere("PointTag LIKE {0}", (filterstring + ":SUM")) > 0)
            {
                List<ActiveMeasurement> sums = new TableOperations<ActiveMeasurement>(dataContext.Connection).QueryRecordsWhere("PointTag LIKE {0}", filterstring + ":SUM").ToList();
                List<ActiveMeasurement> squaredsums = new TableOperations<ActiveMeasurement>(dataContext.Connection).QueryRecordsWhere("PointTag LIKE {0}", filterstring + ":SQR").ToList();
                List<ActiveMeasurement> minimums = new TableOperations<ActiveMeasurement>(dataContext.Connection).QueryRecordsWhere("PointTag LIKE {0}", filterstring + ":MIN").ToList();
                List<ActiveMeasurement> maximums = new TableOperations<ActiveMeasurement>(dataContext.Connection).QueryRecordsWhere("PointTag LIKE {0}", filterstring + ":MAX").ToList();
                List<ActiveMeasurement> number = new TableOperations<ActiveMeasurement>(dataContext.Connection).QueryRecordsWhere("PointTag LIKE {0}", filterstring + ":NUM").ToList();

                result = sums.Select(point => new ReportMeasurements(point)).ToList();

                // Pull Data From the Open Historian
                List<ActiveMeasurement> all = sums.Concat(squaredsums).Concat(minimums).Concat(maximums).Concat(number).ToList();
                List<CondensedDataPoint> historiandata = new List<CondensedDataPoint>();
                try
                {
                    historiandata = this.historianOperations.ReadCondensed(start, end, all, double.MaxValue).ToList();
                }
                catch
                {
                    return new List<ReportMeasurements>();
                }

                result = result.Where((item,index) =>
                {
                    ReportMeasurements sum = item;
                    string tag = item.PointTag.Remove(item.PointTag.Length - 4);
                    ActiveMeasurement squared = squaredsums.Find(meas => meas.PointTag.Remove(meas.PointTag.Length - 4) == tag);
                    ActiveMeasurement min = minimums.Find(meas => meas.PointTag.Remove(meas.PointTag.Length - 4) == tag);
                    ActiveMeasurement max = maximums.Find(meas => meas.PointTag.Remove(meas.PointTag.Length - 4) == tag);
                    ActiveMeasurement total = number.Find(meas => meas.PointTag.Remove(meas.PointTag.Length - 4) == tag);

                    return (historiandata.Select(point => point.PointID).Contains(sum.PointID) &&
                        historiandata.Select(point => point.PointID).Contains(squared.PointID) &&
                        historiandata.Select(point => point.PointID).Contains(min.PointID) &&
                        historiandata.Select(point => point.PointID).Contains(max.PointID) &&
                        historiandata.Select(point => point.PointID).Contains(total.PointID));
                }).ToList();

                result = result.Select(item =>
                {

                    string tag = item.PointTag.Remove(item.PointTag.Length - 4);
                    ActiveMeasurement sum = sums.Find(meas => meas.PointTag.Remove(meas.PointTag.Length - 4) == tag);
                    ActiveMeasurement squared = squaredsums.Find(meas => meas.PointTag.Remove(meas.PointTag.Length - 4) == tag);
                    ActiveMeasurement min = minimums.Find(meas => meas.PointTag.Remove(meas.PointTag.Length - 4) == tag);
                    ActiveMeasurement max = maximums.Find(meas => meas.PointTag.Remove(meas.PointTag.Length - 4) == tag);
                    ActiveMeasurement total = number.Find(meas => meas.PointTag.Remove(meas.PointTag.Length - 4) == tag);

                    double minimum = historiandata.Find(point => point.PointID == min.PointID).min;
                    double maximum = historiandata.Find(point => point.PointID == max.PointID).max;
                    double npoints = historiandata.Find(point => point.PointID == total.PointID).sum;
                    double summation = historiandata.Find(point => point.PointID == sum.PointID).sum;
                    double squaredsum = historiandata.Find(point => point.PointID == squared.PointID).sum;

                    item.Max = maximum;
                    item.Min = minimum;
                    item.Mean = summation / npoints;
                    item.NumberOfAlarms = 0;
                    item.PercentAlarms = 0;
                    item.StandardDeviation = Math.Sqrt((squaredsum - 2 * summation * item.Mean + npoints * item.Mean * item.Mean) / npoints);

                    item.PointTag = item.PointTag.Remove(item.PointTag.Length - 4);
                    return item;
                }).ToList();
            }

            return result;
        }
        


        #endregion
    }
}