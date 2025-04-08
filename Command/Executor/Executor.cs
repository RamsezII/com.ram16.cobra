﻿using _ARK_;
using _UTIL_;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace _COBRA_
{
    partial class Command
    {
        public class Executor : IDisposable
        {
            //prefixe = $"{MachineSettings.machine_name.Value.SetColor("#73CC26")}:{NUCLEOR.terminal_path.SetColor("#73B2D9")}$",

            static readonly Executor echo_executor = new(
                null,
                new() { new("echo", new("echo", on_pipe: (exe, stdin) => Debug.Log(stdin))), },
                new Line(string.Empty, CMD_SIGNALS.EXEC, null)
                );

            public readonly string cmd_name;
            public readonly Command command;
            public readonly string cmd_path;

            public readonly Executor root;
            public Line line;
            readonly Executor stdout_exe = echo_executor, stderr_exe = echo_executor;
            public readonly List<object> args;
            public readonly HashSet<string> opts = new(StringComparer.OrdinalIgnoreCase);
            public IEnumerator<STDIN_INFOS> routine;

            public readonly ThreadSafe_struct<bool> disposed = new();

            public int executions = -1;
            static byte id_counter;
            public byte id = ++id_counter;

            public string error;
            public void Stdout(in object data) => stdout_exe.command.on_pipe(stdout_exe, data);
            void Stderr(in object data) => stderr_exe.command.on_pipe(stderr_exe, data);

            //--------------------------------------------------------------------------------------------------------------

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
            static void OnBeforeSceneLoad()
            {
                id_counter = 0;
            }

            //--------------------------------------------------------------------------------------------------------------

            public Executor(in Executor root, in List<KeyValuePair<string, Command>> path, in Line line)
            {
                this.root = root ?? this;
                this.line = line;
                cmd_name = path[^1].Key;
                command = path[^1].Value;

                switch (path.Count)
                {
                    case 0:
                        throw new ArgumentException($"Command path is empty.", nameof(path));

                    case 1 when path[0].Value == cmd_root_shell:
                        cmd_path = "~";
                        break;

                    case 1:
                        cmd_path = path[0].Key;
                        break;

                    default:
                        StringBuilder path_sb = new();
                        for (int i = 0; i < path.Count; i++)
                            path_sb.Append(path[i].Key + "/");
                        path_sb.Remove(path_sb.Length - 1, 1);
                        cmd_path = path_sb.ToString();
                        break;
                }

                if (command.args != null)
                {
                    args = new(command.pipe_min_args_required);
                    command.args(this);

                    if (error == null)
                        if (args.Count < command.pipe_min_args_required)
                            error = $"[{nameof(command.pipe_min_args_required)}] '{cmd_name}' ({cmd_path}) requires {command.pipe_min_args_required} arguments to init, {args.Count} were given.";
                }

                if (error == null)
                    if (line.TryReadPipe())
                        if (cmd_root_shell.TryReadCommand_path(line, out var path2))
                        {
                            Executor executor = new(root, path2, line);
                            error = executor.error;
                            if (error == null)
                                if (executor.command.on_pipe == null)
                                    error = $"Command '{executor.cmd_name}' ({executor.cmd_path}) has no '{nameof(executor.command.on_pipe)}' callback, it can not be piped into.";
                                else
                                    stdout_exe = executor;
                        }
                        else if (line.signal.HasFlag(CMD_SIGNALS.EXEC) && line.start_i != line.cpl_start_i)
                            error = $"Command '{cmd_name}' ({cmd_path}) failed to parse pipe.";

                if (error != null)
                    if (CanLogError())
                        Debug.LogWarning($"[ERROR] '{cmd_name}' ({cmd_path}): {error}");
            }

            //--------------------------------------------------------------------------------------------------------------

            public string GetPrefixe(string user_name = null, string cmd_path = null)
            {
                user_name ??= MachineSettings.machine_name.Value;
                cmd_path ??= this.cmd_path;
                return $"{user_name.SetColor("#73CC26")}:{cmd_path.SetColor("#73B2D9")}$";
            }

            public bool CanLogError()
            {
                if (command != null && line != null)
                    if (line.HasFlags_any(CMD_SIGNALS.CHECK | CMD_SIGNALS.EXEC))
                        return command.log_error;
                return false;
            }

            public bool TryKill()
            {
                if (routine != null)
                    if (routine.Current.immortal)
                        Debug.LogWarning($"'{cmd_name}' ({cmd_path}) {typeof(STDIN_INFOS).FullName}.{nameof(routine.Current.immortal)}: {routine.Current.immortal}");
                    else
                    {
                        routine.Dispose();
                        routine = null;
                        return true;
                    }
                return false;
            }

            public IEnumerator<STDIN_INFOS> Executate(in Line line)
            {
                error = null;
                this.line = line;

                if (line.signal.HasFlag(CMD_SIGNALS.EXEC))
                    ++executions;

                if (routine == null)
                    if (line.notEmpty)
                        if (command._commands.Count > 0)
                        {
                            if (command.TryReadCommand_path(line, out var path))
                            {
                                Executor exe = new(root, path, line);
                                error = exe.error;

                                if (exe.error == null && line.signal.HasFlag(CMD_SIGNALS.EXEC))
                                    return routine = exe.Executate(line);
                                else
                                {
                                    if (exe.error != null)
                                        error = $"'{exe.cmd_name}' ({exe.cmd_path}): {exe.error}";
                                    exe.Dispose();
                                }
                            }
                            else
                                error = $"Could not find '{line.arg_last}' in '{cmd_name}' ({cmd_path})";
                        }

                if (error == null)
                    if (line.signal.HasFlag(CMD_SIGNALS.EXEC))
                        if (command.action != null)
                            if (command.action_min_args_required > 0 && args != null && args.Count < command.action_min_args_required)
                                error = $"[{nameof(command.action_min_args_required)}] '{cmd_name}' ({cmd_path}) requires {command.action_min_args_required} arguments to execute, {args.Count} were given.";
                            else
                                try
                                {
                                    command.action(this);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogException(e);
                                    error = $"[{nameof(command.action)}] '{cmd_name}' ({cmd_path}) failed to execute: \"{e.TrimMessage()}\"";
                                }

                if (error == null)
                    try
                    {
                        if (executions == 0 && line.signal.HasFlag(CMD_SIGNALS.EXEC))
                        {
                            if (command.routine != null)
                            {
                                routine = command.routine(this);
                                routine.MoveNext();
                                if (error != null)
                                    error = $"[{nameof(command.routine)}] {error}";
                                return routine;
                            }
                        }
                        else if (routine != null)
                        {
                            if (!routine.MoveNext())
                                routine = null;

                            if (error != null)
                                error = $"[{nameof(command.routine)}] {error}";
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        error = $"[{nameof(command.routine)}] '{cmd_name}' ({cmd_path}) failed to execute: \"{e.TrimMessage()}\"";
                    }

                if (error == null)
                    return routine;
                else
                {
                    if (CanLogError())
                        Debug.LogWarning($"[ERROR] '{cmd_name}': {error}");
                    return null;
                }
            }

            //--------------------------------------------------------------------------------------------------------------

            public void Dispose()
            {
                routine?.Dispose();

                lock (disposed)
                {
                    if (disposed._value)
                    {
                        Debug.LogWarning($"[{nameof(Dispose)}] '{cmd_name}' ({cmd_path}) is already disposed.");
                        return;
                    }
                    disposed._value = true;
                }
            }
        }
    }
}