﻿using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace _COBRA_
{
    partial class CmdUtils
    {
        static void InitGrep()
        {
            Shell.static_domain.AddPipe(
                "grep",
                manual: new("regex filter"),
                min_args: 1,
                args: static exe =>
                {
                    if (exe.line.TryReadArgument(out string arg))
                        exe.args.Add(arg);
                },
                on_pipe: static (exe, args, data) =>
                {
                    Regex regex = new((string)exe.args[0], RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    switch (data)
                    {
                        case string str:
                            if (regex.IsMatch(str))
                                exe.Stdout(str);
                            break;

                        case IEnumerable<object> objects:
                            {
                                List<object> filtered = new();
                                foreach (object obj in objects)
                                {
                                    string str = obj switch
                                    {
                                        string s => s,
                                        _ => obj.ToString()
                                    };
                                    if (regex.IsMatch(str))
                                        filtered.Add(obj);
                                }
                                if (filtered.Count > 0)
                                    exe.Stdout(filtered.LinesToText());
                            }
                            break;

                        default:
                            {
                                string str = data.ToString();
                                if (regex.IsMatch(str))
                                    exe.Stdout(str);
                            }
                            break;
                    }
                },
                aliases: "regex");
        }
    }
}