﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Concurrency;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ReactiveUI;
using ReactiveUI.Testing;
using ReactiveUI.Sample.Models;
using ReactiveUI.Sample.ViewModels;

namespace ReactiveUI.Sample.Tests
{
    [TestClass()]
    public class BlockTimerViewModelTest : IEnableLogger
    {
        [TestMethod]
        public void TimerShouldFinishAfterThirtyMinutes()
        {
            (new TestScheduler()).With(sched => {
                var lastState = BlockTimerViewState.Initialized;
                bool isTimerStateDone = false;
                var fixture = new BlockTimerViewModel(new BlockItem() { Description = "Test Item" });

                // Watch the timer state
                fixture.TimerState.Subscribe(
                    state => lastState = state,
                    () => isTimerStateDone = true);

                // Click the Start button
                fixture.Start.Execute(null);

                // Fast forward to 25 minutes in, the timer should *not* be done
                sched.RunToMilliseconds(24 * 60 * 1000);
                Assert.IsFalse(isTimerStateDone);

                // Let's go to 31 minutes
                sched.RunToMilliseconds(35 * 60 * 1000);

                // Make sure our model duration took 30 minutes(ish)
                var pomodoroLength = (fixture.Model.EndedAt.Value - fixture.Model.StartedAt.Value);
                Assert.IsTrue(isTimerStateDone);
                Assert.AreEqual(30, (int)pomodoroLength.TotalMinutes);
            });
        }


        [TestMethod]
        public void MakeSureWeAreInBreakMode()
        {
            (new TestScheduler()).With(sched => {
                var lastState = BlockTimerViewState.Initialized;
                bool isTimerStateDone = false;
                var fixture = new BlockTimerViewModel(new BlockItem() { Description = "Test Item" });

                // Watch the timer state
                fixture.TimerState.Subscribe(
                    state => lastState = state,
                    () => isTimerStateDone = true);

                fixture.Start.Execute(null);

                sched.RunToMilliseconds(26 * 60 * 1000);
                Assert.AreEqual(BlockTimerViewState.StartedInBreak, lastState);
                Assert.IsFalse(isTimerStateDone);
                Assert.AreEqual(4, (int)fixture.TimeRemaining.TotalMinutes);
            });
        }

        [TestMethod]
        public void TheTimerDoesntAdvanceWhenItIsPaused()
        {
            (new TestScheduler()).With(sched => {
                var lastState = BlockTimerViewState.Initialized;
                bool isTimerStateDone = false;
                var fixture = new BlockTimerViewModel(new BlockItem() { Description = "Test Item" });

                // Watch the timer state
                fixture.TimerState.Subscribe(
                    state => lastState = state,
                    () => isTimerStateDone = true);

                fixture.Start.Execute(null);

                // Five minutes in, hit the pause button
                sched.RunToMilliseconds(5 * 60 * 1000);
                var timeRemaining = fixture.TimeRemaining;

                fixture.Pause.Execute(null);

                // Fast forward five more minutes - since we're paused, we 
                // TimeRemaining shouldn'tve moved
                sched.RunToMilliseconds(10 * 60 * 1000);
                Assert.AreEqual((int)timeRemaining.TotalMinutes, (int)fixture.TimeRemaining.TotalMinutes);

                fixture.Start.Execute(null);

                // Make sure the TimeRemaining has only advanced 1 minute since 
                // we resumed (i.e. we shouldn't count paused time as working)
                sched.RunToMilliseconds(11 * 60 * 1000);

                // We should have one pause, and it should be 5 minutes long
                Assert.AreEqual(1, fixture.Model.PauseList.Count);
                var deltaTime = (fixture.Model.PauseList[0].EndedAt - fixture.Model.PauseList[0].StartedAt).TotalMinutes;
                this.Log().InfoFormat("Pause Time: {0} mins", deltaTime);
                Assert.AreEqual(5, (int)deltaTime);

                // The timer display should have advanced only one more minute 
                // (i.e. not six minutes, since we were paused for 5 of them)
                deltaTime = (timeRemaining - fixture.TimeRemaining).TotalMinutes;
                deltaTime.IsWithinEpsilonOf(1.0);
            });
        }

        [TestMethod]
        public void ProgressBarValueIsAccurate()
        {
            (new TestScheduler()).With(sched => {
                double lastPercentage = -1.0;
                var fixture = new BlockTimerViewModel(new BlockItem() { Description = "Test Item" });

                fixture.WhenAny(x => x.ProgressPercentage, x => x.Value).Subscribe(x => lastPercentage = x);

                fixture.Start.Execute(null);

                // At the beginning we should be zero
                sched.RunToMilliseconds(10);
                lastPercentage.IsWithinEpsilonOf(0.0);

                // Run to exactly half of the work time 25 mins / 2
                sched.RunToMilliseconds((12 * 60 + 30) * 1000);
                lastPercentage.IsWithinEpsilonOf(0.5);

                // Run to a little before the end, should be near 1.0
                sched.RunToMilliseconds(25 * 60 * 1000 - 10);
                lastPercentage.IsWithinEpsilonOf(1.0);

                // Step to the beginning of the break, we should've moved back 
                // to zero
                sched.RunToMilliseconds(25 * 60 * 1000 + 1010);
                lastPercentage.IsWithinEpsilonOf(0.0);

                // Finally run to the end of the break
                sched.RunToMilliseconds(30 * 60 * 1000 - 10);
                lastPercentage.IsWithinEpsilonOf(1.0);
            });
        }
    }

    static class DoubleTestMixin
    {
        public static void IsWithinEpsilonOf(this double lhs, double rhs, double epsilon = 0.01)
        {
            Assert.IsTrue(Math.Abs(lhs - rhs) <= epsilon, String.Format("Left: {0}, Right: {1}", lhs, rhs));
        }
    }
}
