using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parkour.Cecil.Tests;

public class Test
{
    public static object? Run()
    {
        var a = new List<int>(new int[] { 7, 11 });
        return a[0];
    }
}
