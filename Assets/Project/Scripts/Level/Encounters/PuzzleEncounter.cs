using System.Collections.Generic;
using UnityEngine;
using CGD.Feedback;
using CGD.Interaction;

namespace CGD.Level
{
    // Puzzle: the doors leading deeper are locked until the room's calibration ring is
    // solved (every node green). The way back stays open, so the player can always leave
    // and return. A puzzle room with nowhere deeper to go pays out a reward instead.
    public class PuzzleEncounter : RoomEncounter
    {
        private readonly List<PuzzleSwitch>  _switches = new();
        private readonly List<ConditionLock> _doors    = new();
        private LightsOutPuzzle _puzzle;

        protected override void OnBegin()
        {
            EncounterSettings settings = Context.Settings;
            if (settings.PuzzleSwitchPrefab == null) return;

            if (settings.PuzzleDoorPrefab != null)
                foreach (Pose doorway in Context.ForwardDoorways)
                {
                    GameObject door = Instantiate(settings.PuzzleDoorPrefab, doorway.position, doorway.rotation, Context.Level);
                    ConditionLock conditionLock = door.GetComponentInChildren<ConditionLock>();
                    if (conditionLock != null) _doors.Add(conditionLock);
                }

            _puzzle = new LightsOutPuzzle(settings.PuzzleNodes.Evaluate(Context.Random));
            _puzzle.Scramble(Context.Random, settings.PuzzleScramble);

            for (int i = 0; i < _puzzle.Count; i++)
            {
                GameObject node = Instantiate(settings.PuzzleSwitchPrefab, Context.TakeSpot(), Quaternion.Euler(0f, Context.Random.Range(0, 360), 0f), Context.Level);
                if (!node.TryGetComponent(out PuzzleSwitch puzzleSwitch)) continue;

                var (left, right) = _puzzle.Neighbours(i);
                puzzleSwitch.Setup(i, $"Calibrate node {i + 1}  (also flips {left + 1} and {right + 1})", Press);
                _switches.Add(puzzleSwitch);
            }
            ShowState();
        }

        protected override void OnPlayerEntered()
        {
            if (_puzzle == null || _puzzle.IsSolved) return;
            Notify(_doors.Count > 0
                ? "Calibrate every node to green to unlock the way ahead"
                : "Calibrate every node to green");
        }

        private void Press(int index)
        {
            if (_puzzle.IsSolved) return;
            _puzzle.Press(index);
            ShowState();
            if (!_puzzle.IsSolved) return;

            foreach (PuzzleSwitch puzzleSwitch in _switches) puzzleSwitch.Lock();
            foreach (ConditionLock door in _doors) door.Satisfy(this);

            if (_doors.Count > 0)
            {
                Notify("Calibrated — the way ahead is open", NotificationStyle.Success);
                return;
            }
            Notify("Calibrated", NotificationStyle.Success);
            SpawnReward(Context.Center);
        }

        private void ShowState()
        {
            for (int i = 0; i < _switches.Count; i++)
                _switches[i].ShowState(_puzzle.IsOn(i));
        }
    }
}
