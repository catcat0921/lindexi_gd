﻿// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

unsafe
{
    Debugger.Break();

    F* foo = Foo(10);

    var a1 = foo->A1;

    var a2 = foo->A2;
    var a3 = foo->A3;

    GC.KeepAlive(a1);
    GC.KeepAlive(a2);
    GC.KeepAlive(a3);

    F* Foo(int count)
    {
        if (count == 0)
        {
            F f = new F()
            {
                A1 = 100,
                A2 = 200,
                A3 = 300
            };

            return &f;
        }
        else
        {
            count--;
            return Foo(count);
        }
    }
}


Console.WriteLine("Hello, World!");



struct F
{
    public int A1;
    public int A2;
    public int A3;
}