﻿using System;
using System.Threading;
using System.Threading.Tasks;

class ContinuationTest {
  public static void Main() {
    var sem = new SemaphoreSlim(0);
    var race = 0;
    var first = Task.Run(() =>
    {
      sem.Wait();
      return 11;
    });
    var second = Task.Run(() =>
    {
      sem.Wait();
      return 22;
    });
    Task.Factory.ContinueWhenAny(new Task<int>[] { first, second }, previous =>
    {
      if (previous.Result == 11 || previous.Result == 22) {
        Console.Write(race);
      }
    });
    race = 1;
    sem.Release();
    race = 2;
    sem.Release();
    race = 3;
  }
}
