﻿using System;

class Program {
  public static void Main() {
    Console.Write(nameof(Console.WriteLine));
    System.Threading.Tasks.Task.Run(() => { });
  }
}