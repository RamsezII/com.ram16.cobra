﻿using _ARK_;
using UnityEngine;

namespace _COBRA_
{
    partial class Command
    {
        public sealed partial class Line
        {
            public readonly Shell shell;
            public readonly Linter linter = new();
            public string text;
            public bool notEmpty;
            public int cpl_index;
            public SIGNALS signal;
            public bool cpl_stop, cpl_done, is_cursor_on_path;
            public int cursor_i, read_i, start_i, path_i, end_i, arg_i = -1, cpl_start_i;
            public string arg_last, path_last;
            public CMDLINE_DATA data;
            public bool HasFlags_any(in SIGNALS flags) => (signal & flags) != 0;
            bool IsOnCursor => cursor_i >= start_i && cursor_i <= read_i;

            //--------------------------------------------------------------------------------------------------------------

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
            static void OnBeforeSceneLoad()
            {
                LoadHistory();
            }

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
            static void OnAfterSceneLoad()
            {
                NUCLEOR.delegates.onApplicationQuit += SaveHistory;
            }

            //--------------------------------------------------------------------------------------------------------------

            public Line(in string text, in SIGNALS signal, in Shell shell, in int cursor_i = default, in int cpl_index = default)
            {
                notEmpty = !string.IsNullOrWhiteSpace(text);
                this.text = notEmpty ? text : string.Empty;
                this.signal = signal;
                this.shell = shell;
                this.cursor_i = cursor_i;
                this.cpl_index = cpl_index;

                if (shell != null)
                {
                    linter = shell.terminal?.GetLinter;
                    linter?.Clear();
                }
            }
        }
    }
}