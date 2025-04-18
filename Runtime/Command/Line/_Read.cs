﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace _COBRA_
{
    partial class Command
    {
        partial class Line
        {
            public string ReadAll()
            {
                string read = text[read_i..];
                read_i = text.Length;
                LintToThisPosition(linter._readall_);
                return read;
            }

            public bool TryReadCommandSeparator(out string argument)
            {
                if (!HasNext(true))
                {
                    argument = string.Empty;
                    return false;
                }

                LintToThisPosition(linter._default_);

                int start_i = read_i;
                char c = text[read_i];

                switch (c)
                {
                    case '&':
                        ++read_i;
                        if (read_i < text.Length && text[read_i] == '&')
                        {
                            ++read_i;
                            LintToThisPosition(linter.chain);
                        }
                        else
                            LintToThisPosition(linter.background);
                        argument = text[start_i..read_i];
                        return true;

                    case '|':
                        ++read_i;
                        LintToThisPosition(linter.pipe);
                        argument = text[start_i..read_i];
                        return true;

                    default:
                        argument = string.Empty;
                        return false;
                }
            }

            public bool HasNext(in bool save_move)
            {
                int read_i = this.read_i;
                text.SkipSpaces(ref read_i);

                if (linter == null)
                    LintToThisPosition(Color.gray);
                else
                    LintToThisPosition(linter._default_);

                if (save_move)
                    this.read_i = read_i;

                return read_i < text.Length;
            }

            public void ReadBack()
            {
                --arg_i;

                if (cpl_start_i == read_i)
                {
                    cpl_done = false;
                    is_cursor_on_path = false;
                }

                end_i = read_i = start_i;
                text.GroupedErase(ref start_i);

                if (!cpl_done)
                    cpl_start_i = start_i;
            }

            public bool TryReadArgument(
                out string argument,
                out bool seems_valid,
                IEnumerable<string> completions = null,
                in bool complete_if_option = false,
                in PATH_FLAGS path_mode = 0,
                in bool lint = true)
            {
                if (lint)
                    LintToThisPosition(linter._default_);

                bool isNotEmpty = Util_cobra.TryReadArgument(text, out start_i, ref read_i, out argument, true);
                end_i = read_i;

                if (isNotEmpty)
                {
                    arg_last = argument;
                    ++arg_i;

                    if (lint)
                        if (path_mode.HasFlags_any(PATH_FLAGS.BOTH))
                        {
                            is_cursor_on_path = IsOnCursor;
                            if (is_cursor_on_path)
                            {
                                path_i = start_i;
                                path_last = arg_last;
                            }
                            LintPath();
                        }
                        else
                            LintToThisPosition(linter.argument);
                }

                if (!cpl_done)
                    if (signal.HasFlag(SIGNALS.CPL))
                        if (IsOnCursor)
                            if (path_mode.HasFlags_any(PATH_FLAGS.BOTH) || completions != null)
                                if (!complete_if_option || argument.StartsWith('-'))
                                {
                                    cpl_done = true;
                                    cpl_start_i = read_i;
                                    if (signal.HasFlag(SIGNALS.CPL_TAB))
                                        if (path_mode.HasFlags_any(PATH_FLAGS.BOTH))
                                            PathCompletion_tab(argument, path_mode, out completions);
                                        else
                                            ComputeCompletion_tab(argument, completions);
                                    else if (signal.HasFlag(SIGNALS.CPL_ALT))
                                        if (path_mode.HasFlags_any(PATH_FLAGS.BOTH))
                                            PathCompletion_alt(argument, path_mode, out completions);
                                        else
                                            ComputeCompletion_alt(argument, completions);
                                }

                if (path_mode == PATH_FLAGS._none_)
                    seems_valid = completions != null && completions.Contains(argument);
                else
                {
                    string full_path = shell.PathCheck(argument, PathModes.ForceFull);
                    if (path_mode.HasFlag(PATH_FLAGS.FILE) && File.Exists(full_path))
                        seems_valid = true;
                    else if (path_mode.HasFlag(PATH_FLAGS.DIRECTORY) && Directory.Exists(full_path))
                        seems_valid = true;
                    else
                        seems_valid = false;
                }

                return isNotEmpty;
            }
        }
    }
}