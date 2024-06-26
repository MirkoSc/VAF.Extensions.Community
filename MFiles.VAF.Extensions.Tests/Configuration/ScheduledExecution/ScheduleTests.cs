﻿using MFiles.VAF.Extensions.ScheduledExecution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MFiles.VAF.Extensions.Tests.ScheduledExecution
{
	[TestClass]
	public class ScheduleTests
	{
		public ScheduleTests()
		{
			Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
			Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		}

		[TestMethod]
		public void ScheduleIsEnabledByDefault()
		{
			Assert.IsTrue(new Schedule().Enabled);
		}

		[TestMethod]
		public void ScheduleTriggersAreNotNullByDefault()
		{
			Assert.IsNotNull(new Schedule().Triggers);
		}

		[TestMethod]
		// https://community.m-files.com/groups-1602070992/developers/f/developer-forum/9824/trouble-with-daily-scheduling-repeating
		public void ForumPost9824()
		{
			using (var x = new LocalTimeZoneInfoMocker("FLE Standard Time"))
			{
				var schedule = new Schedule()
				{
					Enabled = true,
					Triggers = new List<Trigger>()
					{
						new DailyTrigger()
						{
							TriggerTimes = new List<TimeSpan>()
							{
								TimeSpan.FromHours(1)
							}
						}
					},
					RunOnVaultStartup = false
				};

				Assert.AreEqual
				(
					// Lets say we run just after the time above.
					new DateTime(2024, 01, 02, 01, 00, 00, DateTimeKind.Local),
					// We expect to run the next day at 1am local time.
					schedule.GetNextExecution(new DateTime(2024, 01, 01, 01, 00, 01, DateTimeKind.Local))
				);

				Assert.AreEqual
				(
					// Lets say we run just after the time above but without an explicit datetimekind.
					new DateTime(2024, 01, 02, 01, 00, 00),
					// We expect to run the next day at 1am local time.
					schedule.GetNextExecution(new DateTime(2024, 01, 01, 01, 00, 01))
				);
			}
		}

		[TestMethod]
		[DynamicData(nameof(GetNextExecutionData_UTC), DynamicDataSourceType.Method)]
		public void GetNextExecution_UTC
		(
			IEnumerable<TriggerBase> triggers,
			DateTimeOffset? after,
			DateTimeOffset? expected
		)
		{
			Assert.AreEqual
			(
				expected,
				new Schedule()
				{
					Enabled = true,
					Triggers = triggers
						.Select(t => new Trigger(t))
						.Where(t => t != null)
						.ToList(),
					TriggerTimeType = TriggerTimeType.Utc,
				}.GetNextExecution(after)
			);
		}

		[TestMethod]
		[DynamicData(nameof(GetNextExecutionData_Finnish), DynamicDataSourceType.Method)]
		public void GetNextExecution_Finnish
		(
			IEnumerable<TriggerBase> triggers,
			DateTime? after,
			DateTime? expected
		)
		{
			var execution = new Schedule()
			{
				Enabled = true,
				Triggers = triggers
						.Select(t => new Trigger(t))
						.Where(t => t != null)
						.ToList(),
				TriggerTimeType = TriggerTimeType.Custom,
				TriggerTimeCustomTimeZone = "FLE Standard Time"
			}.GetNextExecution(after);
			Assert.AreEqual(expected?.ToUniversalTime(), execution?.ToUniversalTime());
		}

		[TestMethod]
		[DynamicData(nameof(GetNextExecutionData_AusEastern), DynamicDataSourceType.Method)]
		public void GetNextExecution_AusEastern
		(
			IEnumerable<TriggerBase> triggers,
			DateTime? after,
			DateTime? expected
		)
		{
			var execution = new Schedule()
			{
				Enabled = true,
				Triggers = triggers
						.Select(t => new Trigger(t))
						.Where(t => t != null)
						.ToList(),
				TriggerTimeType = TriggerTimeType.Custom,
				TriggerTimeCustomTimeZone = "AUS Eastern Standard Time"
			}.GetNextExecution(after);
			Assert.AreEqual(expected?.ToUniversalTime(), execution?.ToUniversalTime());
		}

		[TestMethod]
		[DynamicData(nameof(GetNextExecutionData_PacificStandard), DynamicDataSourceType.Method)]
		public void GetNextExecution_PacificStandard
		(
			IEnumerable<TriggerBase> triggers,
			DateTime? after,
			DateTime? expected
		)
		{
			var execution = new Schedule()
			{
				Enabled = true,
				Triggers = triggers
						.Select(t => new Trigger(t))
						.Where(t => t != null)
						.ToList(),
				TriggerTimeType = TriggerTimeType.Custom,
				TriggerTimeCustomTimeZone = "Pacific Standard Time"
			}.GetNextExecution(after);
			Assert.AreEqual(expected?.ToUniversalTime(), execution?.ToUniversalTime());
		}

		[TestMethod]
		[DynamicData(nameof(GetNextExecutionData_GMT), DynamicDataSourceType.Method)]
		public void GetNextExecution_GMT
		(
			IEnumerable<TriggerBase> triggers,
			DateTimeOffset? after,
			DateTimeOffset? expected
		)
		{
			var trigger = new Schedule()
			{
				Enabled = true,
				Triggers = triggers
						.Select(t => new Trigger(t))
						.Where(t => t != null)
						.ToList(),
				TriggerTimeType = TriggerTimeType.Custom,
				TriggerTimeCustomTimeZone = "GMT Standard Time"
			};
			var execution = trigger.GetNextExecution(after);
			Assert.AreEqual(expected?.ToUniversalTime(), execution?.ToUniversalTime());
		}

		[TestMethod]
		[DynamicData(nameof(GetNextExecutionData_UTC), DynamicDataSourceType.Method)]
		public void GetNextExecution_NotEnabled
		(
			IEnumerable<TriggerBase> triggers,
			DateTimeOffset? after,
			DateTimeOffset? expected
		)
		{
			// We use the same data as the GetNextExecution, but
			// set the schedule to be disabled.
			// In this case the next execution date should be
			// overridden to be null in all cases.
			Assert.IsNull
			(
				new Schedule()
				{
					Enabled = false,
					Triggers = triggers
						.Select(t => new Trigger(t))
						.Where(t => t != null)
						.ToList(),
					TriggerTimeType = TriggerTimeType.Utc
				}.GetNextExecution(after)
			);
		}

		public static IEnumerable<object[]> GetNextExecutionData_UTC()
		{
			// Single trigger.
			yield return new object[]
			{
				new TriggerBase[]
				{
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(17, 0, 0)
						}.ToList()
					}
				},
				new DateTimeOffset(2021, 03, 17, 01, 00, 00, TimeSpan.Zero), // Wednesday @ 1am
				new DateTimeOffset(2021, 03, 17, 17, 00, 00, TimeSpan.Zero), // Wednesday @ 5pm
			};

			// Multiple triggers returns earliest.
			yield return new object[]
			{
				new TriggerBase[]
				{
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(17, 0, 0)
						}.ToList()
					},
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(12, 0, 0)
						}.ToList()
					}
				},
				new DateTimeOffset(2021, 03, 17, 01, 00, 00, TimeSpan.Zero), // Wednesday @ 1am
				new DateTimeOffset(2021, 03, 17, 12, 00, 00, TimeSpan.Zero), // Wednesday @ 5pm
			};

			// No triggers = null.
			yield return new object[]
			{
				new TriggerBase[0],
				new DateTimeOffset(2021, 03, 17, 01, 00, 00, TimeSpan.Zero), // Wednesday @ 1am
				(DateTimeOffset?)null
			};

			// Trigger at exact current time returns now.
			yield return new object[]
			{
				new TriggerBase[]
				{
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(17, 0, 0)
						}.ToList()
					}
				},
				new DateTimeOffset(2021, 03, 17, 17, 00, 00, TimeSpan.Zero), // Wednesday @ 1am
				new DateTimeOffset(2021, 03, 17, 17, 00, 00, TimeSpan.Zero)
			};
		}

		/// <summary>
		/// Check what happens at daylight saving changes
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<object[]> GetNextExecutionData_GMT()
		{
			// Just before clocks go forward.
			yield return new object[]
			{
				new TriggerBase[]
				{
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(1, 30, 0) // The trigger for 0130 is because this is between when the clocks change
						}.ToList()
					}
				},
				new DateTimeOffset(2022, 03, 26, 00, 00, 00, 0, TimeSpan.Zero), // This is still in GMT (UTC+0)
				new DateTimeOffset(2022, 03, 26, 01, 30, 00, 0, TimeSpan.Zero), // So it should run at 0130 UTC
			};

			// Just after clocks go forward.
			yield return new object[]
			{
				new TriggerBase[]
				{
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(1, 30, 0) // The trigger for 0130 is because this is between when the clocks change
						}.ToList()
					}
				},
				new DateTimeOffset(2022, 03, 27, 02, 01, 00, 0, TimeSpan.Zero), // The next run time will be in BST (UTC+1)
				new DateTimeOffset(2022, 03, 28, 00, 30, 00, 0, TimeSpan.Zero), // Check that it runs at 0030 the next day.
			};

			// Just before clocks go backwards.
			yield return new object[]
			{
				new TriggerBase[]
				{
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(2, 30, 0) // The trigger for 0230 is because this is after when the clocks change
						}.ToList()
					}
				},
				new DateTimeOffset(2022, 10, 30, 00, 00, 00, 0, TimeSpan.FromHours(1)), // This is 0100 BST
				new DateTimeOffset(2022, 10, 30, 02, 30, 00, 0, TimeSpan.Zero), // So it should run at 0230 GMT
			};

			// Just before clocks go backwards.
			yield return new object[]
			{
				new TriggerBase[]
				{
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(2, 30, 0) // The trigger for 0230 is because this is after when the clocks change
						}.ToList()
					}
				},
				new DateTimeOffset(2022, 10, 30, 01, 00, 00, 0, TimeSpan.Zero), // This is 0200 BST
				new DateTimeOffset(2022, 10, 30, 02, 30, 00, 0, TimeSpan.Zero), // So it should run at 0030UTC / 0130 BST
			};

			// Just after clocks go backwards.
			yield return new object[]
			{
				new TriggerBase[]
				{
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(1, 30, 0) // The trigger for 0130 is because this is between when the clocks change
						}.ToList()
					}
				},
				new DateTimeOffset(2022, 10, 30, 02, 01, 00, 0, TimeSpan.Zero), // This is 0301 in GMT
				new DateTimeOffset(2022, 10, 31, 01, 30, 00, 0, TimeSpan.Zero), // Check that it runs at 0130 the next day.
			};
		}

		/// <summary>
		/// Returns data for a high UTC offset where the trigger times are the previous day in UTC.
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<object[]> GetNextExecutionData_AusEastern()
		{
			// Single trigger.
			yield return new object[]
			{
				new TriggerBase[]
				{
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(5, 0, 0) // 5am Aus Eastern is UTC+10, so this should equate to the previous day in UTC.
						}.ToList()
					}
				},
				new DateTime(2021, 08, 18, 18, 00, 00, DateTimeKind.Utc), // This is two hours before the time it should run
				new DateTime(2021, 08, 18, 19, 00, 00, DateTimeKind.Utc), // This is 5am on the 19th in Sydney
			};
		}

		/// <summary>
		/// Returns data for a high UTC offset where the trigger times are the next day in UTC.
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<object[]> GetNextExecutionData_PacificStandard()
		{
			// Single trigger.
			yield return new object[]
			{
				new TriggerBase[]
				{
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(22, 30, 0) // 2230 Pacific Standard is UTC-8, so this should equate to the next day in UTC.
						}.ToList()
					}
				},
				new DateTime(2021, 08, 18, 20, 30, 00, DateTimeKind.Utc), // This is two hours before the time it should run
				new DateTime(2021, 08, 19, 05, 30, 00, DateTimeKind.Utc), // This is 1030pm on the 18th in LA
			};
		}

		/// <summary>
		/// This is the same data as <see cref="GetNextExecutionData_UTC"/>, but the
		/// trigger times are expected to be in Finnish timezone (UTC+2), so the resulting
		/// times should be accordingly offset.
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<object[]> GetNextExecutionData_Finnish()
		{
			// Single trigger.
			yield return new object[]
			{
				new TriggerBase[]
				{
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(17, 0, 0)
						}.ToList()
					}
				},
				new DateTime(2021, 03, 17, 01, 00, 00, DateTimeKind.Utc), // Wednesday @ 1am
				new DateTime(2021, 03, 17, 15, 00, 00, DateTimeKind.Utc), // Wednesday @ 5pm
			};

			// Multiple triggers returns earliest.
			yield return new object[]
			{
				new TriggerBase[]
				{
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(17, 0, 0)
						}.ToList()
					},
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(12, 0, 0)
						}.ToList()
					}
				},
				new DateTime(2021, 03, 17, 01, 00, 00, DateTimeKind.Utc), // Wednesday @ 1am
				new DateTime(2021, 03, 17, 10, 00, 00, DateTimeKind.Utc), // Wednesday @ 10am
			};

			// No triggers = null.
			yield return new object[]
			{
				new TriggerBase[0],
				new DateTime(2021, 03, 17, 01, 00, 00, DateTimeKind.Utc), // Wednesday @ 1am
				(DateTime?)null
			};

			// Trigger at exact current time returns now.
			yield return new object[]
			{
				new TriggerBase[]
				{
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(17, 0, 0)
						}.ToList()
					}
				},
				new DateTime(2021, 03, 17, 15, 00, 00, DateTimeKind.Utc),
				new DateTime(2021, 03, 17, 15, 00, 00, DateTimeKind.Utc)
			};

			// Trigger at one minute past current time returns next day.
			yield return new object[]
			{
				new TriggerBase[]
				{
					new DailyTrigger(){
						TriggerTimes = new List<TimeSpan>()
						{
							new TimeSpan(17, 0, 0)
						}.ToList()
					}
				},
				new DateTime(2021, 03, 17, 15, 01, 00, DateTimeKind.Utc),
				new DateTime(2021, 03, 18, 15, 00, 00, DateTimeKind.Utc)
			};
		}

		[TestMethod]
		public void DailyTriggerToDashboardDisplayString_RunOnStartup_False()
		{
			var schedule = new Schedule()
			{
				Triggers = new List<Trigger>()
				{
					new DailyTrigger()
					{
							TriggerTimes = new List<TimeSpan>(){ new TimeSpan(1, 30, 32) }
					}
				},
				RunOnVaultStartup = false
			};
			Assert.AreEqual
			(
				"<p>Runs according to the following schedule:<ul><li>Daily at the following times: 01:30:32 (server time).</li></ul></p>",
				schedule.ToDashboardDisplayString()
			);
		}

		[TestMethod]
		public void DailyTriggerToDashboardDisplayString_RunOnStartup_True()
		{
			var schedule = new Schedule()
			{
				Triggers = new List<Trigger>()
				{
					new DailyTrigger()
					{
							TriggerTimes = new List<TimeSpan>(){ new TimeSpan(1, 30, 32) }
					}
				},
				RunOnVaultStartup = true
			};
			Assert.AreEqual
			(
				"<p>Runs when the vault starts and according to the following schedule:<ul><li>Daily at the following times: 01:30:32 (server time).</li></ul></p>",
				schedule.ToDashboardDisplayString()
			);
		}

		[TestMethod]
		public void ScheduleToDashboardDisplayString_Disabled()
		{
			var schedule = new Schedule()
			{
				Enabled = false
			};
			Assert.AreEqual
			(
				"<p>Will not run as the schedule is not enabled.</p>",
				schedule.ToDashboardDisplayString()
			);
		}
	}
}
