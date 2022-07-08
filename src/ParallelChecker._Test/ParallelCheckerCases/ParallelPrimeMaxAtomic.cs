﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelWithoutLocks {
  class Program {
    static int CalculatePrimes(int[] array) {
      int max = 0;
      Parallel.ForEach(array, number => {
        if (IsPrime(number)) {
          while (true) {
            var old = Volatile.Read(ref max);
            if (number <= old || Interlocked.CompareExchange(ref max, number, old) == old) {
              break;
            }
          }
        }
      });
      return max;
    }

    static bool IsPrime(int number) {
      if (number < 2) {
        return false;
      }
      if (number == 2) {
        return true;
      }
      if (number % 2 == 0) {
        return false;
      }
      for (int divisor = 3; divisor * divisor <= number; divisor += 2) {
        if (number % divisor == 0) {
          return false;
        }
      }
      return true;
    }

    static void Main() {
      var array = Initialize(100);
      int last = CalculatePrimes(array);
      while (true) {
        Console.WriteLine(last);
        var actual = CalculatePrimes(array);
        if (actual != last) {
          Console.WriteLine(actual);
          Console.WriteLine("Race condition!");
          return;
        }
      }
    }

    static int[] Initialize(int amount) {
      var result = new int[amount];
      var random = new Random(4711);
      for (int i = 0; i < amount; i++) {
        result[i] = i * 2 + 1;
      }
      return result;
    }
  }
}
