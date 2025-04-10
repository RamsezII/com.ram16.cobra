﻿using System.Collections.Generic;

namespace _COBRA_
{
    partial class CmdUtils
    {
        static void Init_Skip()
        {
            Shell.static_domain.AddPipe(
                "skip",
                manual: new("skip <int> first entries from pipe"),
                max_args: 1,
                args: static exe =>
                {
                    if (exe.line.TryReadArgument(out string arg))
                        if (int.TryParse(arg, out int count))
                            exe.args.Add(count);
                        else
                            exe.error = $"could not parse into int value: '{arg}'";
                    exe.args.Add(0);
                },
                on_pipe: static (exe, args, data) =>
                {
                    int skips = (int)exe.args[0];
                    int iterations = (int)exe.args[1];

                    bool Check() => iterations++ >= skips;

                    switch (data)
                    {
                        case string str:
                            foreach (string line in str.TextToLines(true))
                                if (Check())
                                    exe.Stdout(line);
                            break;

                        case IEnumerable<object> objects:
                            foreach (object obj in objects)
                                if (Check())
                                    exe.Stdout(obj);
                            break;

                        default:
                            if (Check())
                                exe.Stdout(data);
                            break;
                    }

                    exe.args[1] = iterations;
                });
        }
    }
}