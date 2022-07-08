﻿using System;
using System.Threading.Tasks;

namespace ParallelChecker._Test {
  class RefOutParameters {
    class Test {
      public int y;
    }

    static void Main() {
      var array = new Test[1];
      array[0] = new Test();
      OutTest(out array[0].y);
      if (array[0].y == 42) {
        var first = 0;
        Task.Run(() => first = 1);
        Console.WriteLine(first);
      }
      long y = 1010;
      RefTest(ref y);
      if (y == 2020) {
        var second = 0;
        Task.Run(() => second = 1);
        Console.WriteLine(second);
      }
    }

    static void OutTest(out int value) {
      value = 42;
    }

    static void RefTest(ref long value) {
      if (value == 1010) {
        value = 2020;
      }
    }
  }
}
