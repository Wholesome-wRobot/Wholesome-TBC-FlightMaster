﻿using robotManager.FiniteStateMachine;
using System;

public class ToolBox
{
    public static void AddState(Engine engine, State state, string replace)
    {
        bool statedAdded = engine.States.Exists(s => s.DisplayName == state.DisplayName);
        if (!statedAdded && engine != null && engine.States.Count > 5)
        {
            try
            {
                State stateToReplace = engine.States.Find(s => s.DisplayName == replace);

                if (stateToReplace == null)
                {
                    Logger.LogError($"Couldn't find state {replace}");
                    return;
                }

                int priorityToSet = stateToReplace.Priority;

                // Move all superior states one slot up
                foreach (State s in engine.States)
                {
                    if (s.Priority >= priorityToSet)
                        s.Priority++;
                }

                state.Priority = priorityToSet;
                engine.AddState(state);
                engine.States.Sort();
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur : {0}" + ex.ToString());
            }
        }
    }
}
