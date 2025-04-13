﻿using _ARK_;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace _COBRA_
{
    public sealed partial class Shell : MonoBehaviour
    {
        internal static readonly HashSet<Shell> instances = new();

        readonly Queue<Command.Executor> pending_executors = new();
        readonly List<Command.Executor.Janitor> front_janitors = new();
        internal static readonly List<Command.Executor.Janitor> background_janitors = new();

        static byte id_counter = 0;
        public readonly byte SID = ++id_counter;

        public CMD_STATUS current_status;
        public CMD_STATES previous_state;
        public bool state_changed;
        public bool IsIdle => front_janitors.Count == 0;
        public bool IsBusy => front_janitors.Count > 0;
        public ITerminal terminal;
        public override string ToString() => $"{GetType().FullName}[{SID}]";

        //--------------------------------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad()
        {
            id_counter = 0;
            OnNucleorQuit();
        }

        //--------------------------------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoad()
        {
            NUCLEOR.delegates.onApplicationQuit -= OnNucleorQuit;
            NUCLEOR.delegates.onApplicationQuit += OnNucleorQuit;
            NUCLEOR.delegates.shell_tick -= UpdateBackgroundJanitors;
            NUCLEOR.delegates.shell_tick += UpdateBackgroundJanitors;
        }

        //--------------------------------------------------------------------------------------------------------------

        private void Awake()
        {
            terminal = GetComponentInParent<ITerminal>();
            NUCLEOR.delegates.shell_tick += UpdateUpdateJanitors;
            instances.Add(this);
        }

        //--------------------------------------------------------------------------------------------------------------

        internal static string Monitor()
        {
            StringBuilder sb = new();

            sb.AppendLine($" {nameof(SID),5} {nameof(Command.Executor.PEID),5} {nameof(Command.Executor.EID),5} {nameof(Command.name),35} {nameof(CMD_STATUS.state),15} {nameof(Command.Executor.background),15} {nameof(Command.Executor.disposed),15}");

            void LogExe(in Command.Executor exe) =>
                sb.AppendLine($" {exe.shell.SID,5} {exe.PEID,5} {exe.EID,5} {exe.cmd_path,35} {exe.routine?.Current.state ?? CMD_STATES.DONE,15} {exe.background,15} {exe.disposed,15}");

            foreach (Shell shell in instances)
                for (int i = 0; i < shell.front_janitors.Count; ++i)
                {
                    var janitor = shell.front_janitors[i];
                    for (int j = 0; j < janitor._executors.Count; ++j)
                        LogExe(janitor._executors[j]);
                }

            for (int i = 0; i < background_janitors.Count; ++i)
            {
                var janitor = background_janitors[i];
                for (int j = 0; j <= janitor._executors.Count; ++j)
                    LogExe(janitor._executors[j]);
            }

            return sb.ToString();
        }

        //--------------------------------------------------------------------------------------------------------------

        private void OnDestroy()
        {
            NUCLEOR.delegates.shell_tick -= UpdateUpdateJanitors;
            instances.Remove(this);

            foreach (Command.Executor executor in pending_executors)
                executor.Dispose();
            pending_executors.Clear();

            for (int i = 0; i < front_janitors.Count; i++)
                front_janitors[i].Dispose();
            front_janitors.Clear();
        }

        static void OnNucleorQuit()
        {
            NUCLEOR.delegates.shell_tick -= UpdateBackgroundJanitors;
            for (int i = 0; i < background_janitors.Count; i++)
                background_janitors[i].Dispose();
            background_janitors.Clear();
        }
    }
}