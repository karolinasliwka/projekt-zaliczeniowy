using System;

namespace ConsoleCalculator
{
    // Prosta konsolowa obsługa użytkownika
    public class ConsoleUI
    {
        private readonly CalculatorEngine _engine = new CalculatorEngine();

        public void Run()
        {
            Console.WriteLine("Console Calculator");
            Console.WriteLine("Commands: number X | add | subtract | multiply | divide | power | add_percent | subtract_percent | multiply_percent | divide_percent | sqrt | reciprocal | equals | repeat | reset | exit");
            Console.WriteLine();

            // Run automated tests first
            RunTests();

            while (true)
            {
                Console.Write($"Display: {_engine.GetDisplayValue()} > ");
                var line = Console.ReadLine();
                if (line == null) break;

                var parts = line.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                var cmd = parts[0].ToLowerInvariant();
                var arg = parts.Length > 1 ? parts[1] : null;

                switch (cmd)
                {
                    case "number":
                        if (arg == null) { Console.WriteLine("Usage: number <value>"); break; }
                        _engine.InputNumber(arg.Replace(',', '.'));
                        break;
                    case "add": _engine.SetOperation(Operation.Add); break;
                    case "subtract": _engine.SetOperation(Operation.Subtract); break;
                    case "multiply": _engine.SetOperation(Operation.Multiply); break;
                    case "divide": _engine.SetOperation(Operation.Divide); break;
                    case "power": _engine.SetOperation(Operation.Power); break;
                    case "add_percent": _engine.SetOperation(Operation.AddPercent); break;
                    case "subtract_percent": _engine.SetOperation(Operation.SubtractPercent); break;
                    case "multiply_percent": _engine.SetOperation(Operation.MultiplyPercent); break;
                    case "divide_percent": _engine.SetOperation(Operation.DividePercent); break;
                    case "sqrt": _engine.SetOperation(Operation.Sqrt); break;
                    case "reciprocal": _engine.SetOperation(Operation.Reciprocal); break;
                    case "equals":
                        var res = _engine.Calculate();
                        if (_engine.ErrorState) Console.WriteLine(_engine.GetDisplayValue()); else Console.WriteLine(res);
                        break;
                    case "repeat":
                        var r = _engine.RepeatLastOperation();
                        if (_engine.ErrorState) Console.WriteLine(_engine.GetDisplayValue()); else Console.WriteLine(r);
                        break;
                    case "reset":
                        _engine.Reset();
                        Console.WriteLine("Reset done");
                        break;
                    case "exit":
                        return;
                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
            }
        }

        // Proste testy jednostkowe uruchamiane przy starcie
        private void RunTests()
        {
            Console.WriteLine("Running tests...");
            int failed = 0;

            void AssertEqual(double expected, double? actual, string testName)
            {
                if (!actual.HasValue || Math.Abs(expected - actual.Value) > 1e-9)
                {
                    Console.WriteLine($"FAIL: {testName} Expected={expected} Actual={actual}");
                    failed++;
                }
                else
                {
                    Console.WriteLine($"PASS: {testName}");
                }
            }

            var e = new CalculatorEngine();

            // 1. Add with repeat
            e.Reset();
            e.InputNumber("5"); e.SetOperation(Operation.Add); e.InputNumber("2");
            var r1 = e.Calculate();
            AssertEqual(7, r1, "5+2=7");
            var r2 = e.RepeatLastOperation();
            AssertEqual(9, r2, "repeat -> 9");
            var r3 = e.RepeatLastOperation();
            AssertEqual(11, r3, "repeat -> 11");

            // 2. Subtract
            e.Reset();
            e.InputNumber("10"); e.SetOperation(Operation.Subtract); e.InputNumber("3");
            AssertEqual(7, e.Calculate(), "10-3=7");
            AssertEqual(4, e.RepeatLastOperation(), "repeat -> 4");
            AssertEqual(1, e.RepeatLastOperation(), "repeat -> 1");

            // 3. Multiply
            e.Reset(); e.InputNumber("4"); e.SetOperation(Operation.Multiply); e.InputNumber("5");
            AssertEqual(20, e.Calculate(), "4*5=20");
            AssertEqual(100, e.RepeatLastOperation(), "repeat -> 100");
            AssertEqual(500, e.RepeatLastOperation(), "repeat -> 500");

            // 4. Divide
            e.Reset(); e.InputNumber("100"); e.SetOperation(Operation.Divide); e.InputNumber("2");
            AssertEqual(50, e.Calculate(), "100/2=50");
            AssertEqual(25, e.RepeatLastOperation(), "repeat -> 25");
            AssertEqual(12.5, e.RepeatLastOperation(), "repeat -> 12.5");

            // 5. Power
            e.Reset(); e.InputNumber("2"); e.SetOperation(Operation.Power); e.InputNumber("3");
            AssertEqual(8, e.Calculate(), "2^3=8");
            AssertEqual(512, e.RepeatLastOperation(), "repeat -> 512");

            // 6. Add percent
            e.Reset(); e.InputNumber("200"); e.SetOperation(Operation.AddPercent); e.InputNumber("10");
            AssertEqual(220, e.Calculate(), "200+10%=220");
            AssertEqual(242, e.RepeatLastOperation(), "repeat -> 242");

            // 7. Subtract percent
            e.Reset(); e.InputNumber("200"); e.SetOperation(Operation.SubtractPercent); e.InputNumber("10");
            AssertEqual(180, e.Calculate(), "200-10%=180");
            AssertEqual(162, e.RepeatLastOperation(), "repeat -> 162");

            // 8. Multiply percent
            e.Reset(); e.InputNumber("200"); e.SetOperation(Operation.MultiplyPercent); e.InputNumber("10");
            AssertEqual(20, e.Calculate(), "200*10%=20");
            AssertEqual(2, e.RepeatLastOperation(), "repeat -> 2");

            // 9. Divide percent
            e.Reset(); e.InputNumber("200"); e.SetOperation(Operation.DividePercent); e.InputNumber("10");
            AssertEqual(2000, e.Calculate(), "200/10%=2000");
            AssertEqual(20000, e.RepeatLastOperation(), "repeat -> 20000");

            // 10. Sqrt
            e.Reset(); e.InputNumber("16"); e.SetOperation(Operation.Sqrt);
            AssertEqual(4, e.Calculate(), "sqrt(16)=4");

            // 11. Reciprocal
            e.Reset(); e.InputNumber("4"); e.SetOperation(Operation.Reciprocal);
            AssertEqual(0.25, e.Calculate(), "reciprocal(4)=0.25");

            // 12. Reset
            e.Reset(); e.InputNumber("5"); e.SetOperation(Operation.Add); e.InputNumber("2"); e.Calculate();
            e.Reset();
            if (e.LastResult.HasValue || e.LastOperation != Operation.None) { Console.WriteLine("FAIL: reset"); failed++; } else Console.WriteLine("PASS: reset");

            // 13. Division by zero
            e.Reset(); e.InputNumber("10"); e.SetOperation(Operation.Divide); e.InputNumber("0");
            var divRes = e.Calculate();
            if (!e.ErrorState) { Console.WriteLine("FAIL: division by zero"); failed++; } else Console.WriteLine("PASS: division by zero");

            // 14. Sqrt of negative
            e.Reset(); e.InputNumber("-9"); e.SetOperation(Operation.Sqrt); var sres = e.Calculate(); if (!e.ErrorState) { Console.WriteLine("FAIL: sqrt negative"); failed++; } else Console.WriteLine("PASS: sqrt negative");

            // 15. Reciprocal of zero
            e.Reset(); e.InputNumber("0"); e.SetOperation(Operation.Reciprocal); var rres = e.Calculate(); if (!e.ErrorState) { Console.WriteLine("FAIL: reciprocal zero"); failed++; } else Console.WriteLine("PASS: reciprocal zero");

            Console.WriteLine($"Tests finished. Failed: {failed}");
            Console.WriteLine();
        }
    }
}
