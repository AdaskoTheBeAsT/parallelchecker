﻿using System;
using System.Threading.Tasks;

namespace ParallelChecker._Test {
  class Program {
    static void Main(string[] args) {
      int x = 4;
      switch (x) {
        case 0:
        case 1:
          return;
        case 2:
          return;
      }
      var race = 0;
      Task.Run(() => race = 1);
      Console.WriteLine(race);
    }
  }
}
